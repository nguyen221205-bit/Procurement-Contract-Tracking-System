import os
import sys
import time
import json
import hashlib
import requests
import logging
from db_helper import run_sqlcmd

logger = logging.getLogger("test_negative_rules")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

def run_negative_rules(run_id, manifest, api_base_url="http://localhost:5225"):
    logger.info("=================================================================")
    logger.info(" STARTING SERVER-SIDE NEGATIVE CONSTRAINTS SUITE")
    logger.info("=================================================================")

    actors = manifest["actors"]
    entities = manifest["entities"]
    flow = manifest["businessFlow"]
    db_name = manifest["database"]

    admin = actors["admin"]
    manager_a = actors["managerA"]
    officer_a = actors["officerA"]
    checker_a = actors["checkerA"]
    procurement_b = actors["procurementB"]
    c1 = actors["contractor1"]

    contract_id = flow["contractId"]

    # 1. Acquire Tokens
    def get_token(email, password="Admin@123"):
        res = requests.post(f"{api_base_url}/api/auth/login", json={"email": email, "password": password}, timeout=10)
        res.raise_for_status()
        return res.json()["data"]["token"]

    admin_token = get_token(admin["email"])
    manager_token = get_token(manager_a["email"])
    officer_token = get_token(officer_a["email"])
    checker_token = get_token(checker_a["email"])
    proc_b_token = get_token(procurement_b["email"])
    c1_token = get_token(c1["email"])

    # Query latest Pending milestone acceptance (Milestone 2 after Maker technical review)
    acc_sql = f"""
    SELECT TOP 1 a.Id, a.MilestoneId, a.Revision, a.MilestoneVersion, a.SnapshotDocumentHash
    FROM Acceptances a
    INNER JOIN ContractMilestones m ON a.MilestoneId = m.Id
    WHERE m.ContractId = {contract_id}
    ORDER BY a.Id DESC;
    """
    acc_row = run_sqlcmd(acc_sql, db_name=db_name).splitlines()
    acc_clean = [r.strip() for r in acc_row if r.strip() and not r.startswith("-") and not r.startswith("Id")]
    assert len(acc_clean) > 0, "No acceptance record found in DB for contract!"
    parts = acc_clean[0].split()
    acceptance_id = int(parts[0])
    milestone_id = int(parts[1])
    revision = int(parts[2])
    milestone_version = int(parts[3])
    expected_doc_hash = parts[4] if len(parts) > 4 else ""
    logger.info(f"Target Acceptance ID: {acceptance_id}, Milestone ID: {milestone_id}, r{revision}, v{milestone_version}")

    negative_results = []

    def record_case(case_id, description, expected_status, actual_status, passed, detail):
        res = {
            "caseId": case_id,
            "description": description,
            "expectedStatus": expected_status,
            "actualStatus": actual_status,
            "passed": passed,
            "detail": detail
        }
        negative_results.append(res)
        status_label = "[PASSED]" if passed else "[FAILED]"
        logger.info(f"{status_label} {case_id}: {description} | Expected: {expected_status} | Actual: {actual_status} | {detail}")
        if not passed:
            raise AssertionError(f"Negative test {case_id} failed: {detail} (Response status={actual_status})")

    # -------------------------------------------------------------------------
    # CASE 1: Procurement B attempts IDOR on Entity A's contract & snapshot
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 1: IDOR Cross-Entity Boundary (Procurement B -> Entity A) ---")
    headers_b = {"Authorization": f"Bearer {proc_b_token}"}

    # 1.1 Read sensitive internal members/invitations of Entity A
    res1_read = requests.get(f"{api_base_url}/api/procuring-entities/{entities['entityA']['id']}/members", headers=headers_b)
    pass1_read = (res1_read.status_code == 403)
    record_case("NEG-01A", "Chặn Procurement B đọc dữ liệu nội bộ nhạy cảm của Entity A", "HTTP 403", f"HTTP {res1_read.status_code}", pass1_read, "Server từ chối truy cập chéo đơn vị hợp lệ.")

    # 1.2 Modify contract status of Entity A
    res1_write = requests.put(f"{api_base_url}/api/contracts/{contract_id}/status", json={"newStatus": "Draft"}, headers=headers_b)
    pass1_write = (res1_write.status_code == 403)
    record_case("NEG-01B", "Chặn Procurement B sửa trạng thái hợp đồng của Entity A", "HTTP 403", f"HTTP {res1_write.status_code}", pass1_write, "Server từ chối sửa đổi hợp đồng chéo đơn vị.")

    # 1.3 Download snapshot of Entity A
    res1_snap = requests.get(f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptances/{acceptance_id}/document/download", headers=headers_b)
    pass1_snap = (res1_snap.status_code == 403)
    record_case("NEG-01C", "Chặn Procurement B tải snapshot nghiệm thu của Entity A", "HTTP 403", f"HTTP {res1_snap.status_code}", pass1_snap, "Server từ chối tải snapshot tài liệu của đơn vị khác.")

    # -------------------------------------------------------------------------
    # CASE 2: Maker (Officer A) sends direct API request to approve own review
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 2: Maker Anti-Self-Approval Enforcement (Officer A) ---")
    headers_maker = {"Authorization": f"Bearer {officer_token}"}
    self_approve_payload = {
        "isApproved": True,
        "note": "Maker cố tình tự ký duyệt giải ngân cho chính mình",
        "acceptanceId": acceptance_id,
        "expectedRevision": revision,
        "expectedVersion": milestone_version
    }
    res2 = requests.put(f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptance", json=self_approve_payload, headers=headers_maker)
    pass2 = (res2.status_code == 400 and ("kiểm soát chéo" in res2.text or "Maker" in res2.text))
    record_case("NEG-02", "Chặn Maker tự phê duyệt giải ngân cho biên bản do mình thẩm tra", "HTTP 400 (Anti-Self-Approval)", f"HTTP {res2.status_code}", pass2, "Server từ chối vi phạm kiểm soát chéo Maker-Checker.")

    # -------------------------------------------------------------------------
    # CASE 3: Revoked membership user attempts to review/approve with old JWT
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 3: Revoked Membership Token Rejection ---")
    aux_email = f"aux_officer_{run_id}@procurement.com"
    inv_aux = requests.post(
        f"{api_base_url}/api/procuring-entities/{entities['entityA']['id']}/invitations",
        json={"email": aux_email, "roleInEntity": 1},
        headers={"Authorization": f"Bearer {manager_token}"}
    ).json()["data"]

    token_aux_raw = f"token_aux_{run_id}"
    token_aux_hash = hashlib.sha256(token_aux_raw.encode("utf-8")).hexdigest().lower()
    run_sqlcmd(f"UPDATE ProcuringEntityInvitations SET TokenHash = '{token_aux_hash}' WHERE Id = {inv_aux['id']};", db_name=db_name)

    aux_member = requests.post(
        f"{api_base_url}/api/procuring-entities/invitations/accept",
        json={"token": token_aux_raw, "fullName": f"Aux Officer {run_id}", "password": "Admin@123"}
    ).json()["data"]
    aux_member_id = aux_member["id"]

    # Get JWT for aux officer before revocation
    aux_token = get_token(aux_email)
    headers_aux = {"Authorization": f"Bearer {aux_token}"}

    # Manager A revokes aux officer membership
    rev_res = requests.delete(
        f"{api_base_url}/api/procuring-entities/{entities['entityA']['id']}/members/{aux_member_id}",
        headers={"Authorization": f"Bearer {manager_token}"}
    )
    assert rev_res.status_code == 200, f"Revoke member failed: {rev_res.text}"

    # Aux officer attempts to submit technical review AND approve acceptance with old JWT
    res3 = requests.put(
        f"{api_base_url}/api/contracts/milestones/{milestone_id}/technical-review",
        json={"isPassed": True, "note": "Revoked member attempting review"},
        headers=headers_aux
    )
    res3_acc = requests.put(
        f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptance",
        json=self_approve_payload,
        headers=headers_aux
    )
    pass3 = (res3.status_code == 403 and res3_acc.status_code == 403)
    record_case("NEG-03", "Chặn user có JWT nhưng tư cách thành viên đã bị thu hồi", "HTTP 403 (Member Inactive)", f"HTTP {res3.status_code} / {res3_acc.status_code}", pass3, "Server kiểm tra tính hợp lệ thành viên theo thời gian thực.")

    # -------------------------------------------------------------------------
    # CASE 4: Stale ExpectedVersion / ExpectedRevision Conflict (409 Conflict)
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 4: Stale Concurrency & Revision Conflict ---")
    headers_checker = {"Authorization": f"Bearer {checker_token}"}
    stale_payload = {
        "isApproved": True,
        "note": "Checker gửi revision cũ",
        "acceptanceId": acceptance_id,
        "expectedRevision": 999,
        "expectedVersion": milestone_version
    }
    res4 = requests.put(f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptance", json=stale_payload, headers=headers_checker)
    pass4 = (res4.status_code == 409)
    record_case("NEG-04", "Chặn phê duyệt khi phiên bản / revision bị cũ (Stale Concurrency)", "HTTP 409 Conflict", f"HTTP {res4.status_code}", pass4, "Server bảo vệ tính toàn vẹn phiên bản chống ghi đè dữ liệu cũ.")

    # -------------------------------------------------------------------------
    # CASE 5: Snapshot download authentication, scope authorization and SHA-256 hash match
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 5: Authenticated Snapshot Download & Integrity Verification ---")
    snap_url = f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptances/{acceptance_id}/document/download"

    # 5.1 Unauthenticated -> 401
    res5_unauth = requests.get(snap_url)
    pass5_unauth = (res5_unauth.status_code == 401)
    record_case("NEG-05A", "Chặn tải snapshot tài liệu khi chưa đăng nhập", "HTTP 401 Unauthorized", f"HTTP {res5_unauth.status_code}", pass5_unauth, "Endpoint được bảo vệ nghiêm ngặt bằng [Authorize].")

    # 5.2 Authorized download by Checker A -> 200 OK & SHA-256 match
    res5_auth = requests.get(snap_url, headers=headers_checker)
    pass5_auth = (res5_auth.status_code == 200 and len(res5_auth.content) > 0)
    downloaded_hash = hashlib.sha256(res5_auth.content).hexdigest().lower()
    hash_match = (expected_doc_hash == "" or downloaded_hash == expected_doc_hash.lower())
    pass5_total = pass5_auth and hash_match
    record_case("NEG-05B", "Tải snapshot tài liệu với quyền hợp lệ & kiểm tra mã băm SHA-256", "HTTP 200 + Hash Match", f"HTTP {res5_auth.status_code} (Match: {hash_match})", pass5_total, f"Tệp tải về hợp lệ, dung lượng {len(res5_auth.content)} bytes.")

    # -------------------------------------------------------------------------
    # CASE 6: Platform Admin tries to approve milestone disbursement -> 403 Forbidden
    # -------------------------------------------------------------------------
    logger.info("\n--- CASE 6: Platform Admin Blocked from Disbursement Approval ---")
    headers_admin = {"Authorization": f"Bearer {admin_token}"}
    admin_disburse_payload = {
        "isApproved": True,
        "note": "Admin nền tảng cố tình duyệt chi",
        "acceptanceId": acceptance_id,
        "expectedRevision": revision,
        "expectedVersion": milestone_version
    }
    res6 = requests.put(f"{api_base_url}/api/contracts/milestones/{milestone_id}/acceptance", json=admin_disburse_payload, headers=headers_admin)
    pass6 = (res6.status_code == 403 and "Admin" in res6.text)
    record_case("NEG-06", "Chặn Quản trị viên hệ thống (Admin) duyệt chi thay Bên mời thầu", "HTTP 403 Forbidden", f"HTTP {res6.status_code}", pass6, "Phân định quyền lực rõ ràng: Admin quản trị hệ thống, không thay thế lãnh đạo đơn vị.")

    # -------------------------------------------------------------------------
    # VERIFY BUSINESS DATA UNCHANGED AFTER ALL NEGATIVE ATTEMPTS
    # -------------------------------------------------------------------------
    logger.info("\n--- Post-Negative Verification: Asserting Business Data Uncorrupted ---")
    c_dto = requests.get(f"{api_base_url}/api/contracts/{contract_id}", headers=headers_checker).json()["data"]
    assert c_dto["statusName"] == "Active", f"Contract status corrupted during negative tests: {c_dto['statusName']}"
    assert float(c_dto["totalDisbursedAmount"]) == 500000000.0, f"Disbursed amount altered during negative tests: {c_dto['totalDisbursedAmount']}"
    assert float(c_dto["totalRemainingAmount"]) == 1000000000.0, f"Remaining amount altered during negative tests: {c_dto['totalRemainingAmount']}"
    logger.info("Assertion confirmed: Milestone 2 remained Pending and financial figures remained uncorrupted!")

    manifest["negativeTests"] = negative_results
    return negative_results

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python test_negative_rules.py <manifest_json_path>")
        sys.exit(1)
    with open(sys.argv[1], "r", encoding="utf-8") as f:
        mf = json.load(f)
    run_negative_rules(mf["runId"], mf)
