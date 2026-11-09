import os
import sys
import time
import json
import logging
from api_client import ApiClient

logger = logging.getLogger("fixture_setup")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

SAMPLE_DOCS_DIR = r"D:\Thuc_Tap_backend\TaiLieuMau"
ESTABLISHMENT_PDF = os.path.join(SAMPLE_DOCS_DIR, "QuyetDinhThanhLap_DonViMoiThau.pdf")
LICENSE_PDF = os.path.join(SAMPLE_DOCS_DIR, "GiayPhepKinhDoanh_NhaThau.pdf")

def setup_fixtures(run_id, db_name, base_url="http://localhost:5225", smtp_catcher=None):
    client = ApiClient(base_url)
    manifest = {
        "runId": run_id,
        "database": db_name,
        "timestampUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "actors": {},
        "entities": {}
    }

    def get_real_otp(email, purpose):
        res = client.send_otp(email, purpose=purpose)
        assert res.status_code == 200, f"send_otp failed for {email}: {res.text}"
        body = res.json()
        assert body["data"].get("devOtpCode") is None, "DevOtpCode must not be leaked in API response!"
        if smtp_catcher is not None:
            msg = smtp_catcher.wait_for_email(email, timeout_sec=10)
            assert msg and msg.get("extractedOtp"), f"No OTP email received by SMTP Catcher for {email}"
            return msg["extractedOtp"]
        return None

    def get_real_invite_token(email):
        if smtp_catcher is None:
            raise RuntimeError("smtp_catcher is required for zero-SQL-update invitation token extraction!")
        msg = smtp_catcher.wait_for_email(email, timeout_sec=10)
        assert msg and msg.get("extractedInviteToken"), f"No invitation token email received for {email}"
        return msg["extractedInviteToken"]

    # 1. Acquire Admin Login
    logger.info("Step 1: Logging in as System Admin...")
    admin_token, admin_user = client.login("admin@procurement.com", "Admin@123")
    manifest["actors"]["admin"] = {
        "userId": admin_user["id"],
        "email": admin_user["email"],
        "roles": admin_user.get("roles", ["Admin"])
    }

    # Generate numeric seed from time for unique 10-digit tax codes
    num_seed = int(time.time()) % 800000 + 100000

    # 2. Register Entity A via Real OTP
    logger.info("Step 2: Registering Procuring Entity A with real SMTP OTP...")
    manager_email = f"manager_{run_id}@procurement.com"
    tax_code_a = f"0100{num_seed}"
    otp_a = get_real_otp(manager_email, "RegisterProcuringEntity")
    pe_a_res = client.register_procuring_entity(
        full_name=f"Giám đốc QLDA {run_id}",
        email=manager_email,
        password="Admin@123",
        org_name=f"Ban Quản lý Dự án E2E {run_id}",
        tax_code=tax_code_a,
        pdf_path=ESTABLISHMENT_PDF,
        otp_code=otp_a
    )
    entity_a_id = pe_a_res["procuringEntityId"]
    manager_user_id = pe_a_res["userId"]

    # Admin approves Entity A
    logger.info(f"Admin approving Procuring Entity A (ID: {entity_a_id})...")
    client.verify_procuring_entity(admin_token, entity_a_id, is_approved=True)

    # Manager A login
    manager_token, manager_user = client.login(manager_email, "Admin@123")
    manifest["entities"]["entityA"] = {
        "id": entity_a_id,
        "name": f"Ban Quản lý Dự án E2E {run_id}",
        "taxCode": tax_code_a,
        "managerUserId": manager_user_id
    }
    manifest["actors"]["managerA"] = {
        "userId": manager_user_id,
        "email": manager_email,
        "entityId": entity_a_id,
        "role": "Manager"
    }

    # 3. Provision Officer A1 (Maker / Creator), Officer A2 (Non-Creator Officer), Finance A via Real SMTP Invite Tokens
    logger.info("Step 3: Inviting & provisioning Officer A1, Officer A2, Finance A via real SMTP tokens...")
    officer_email = f"officer_{run_id}@procurement.com"
    inv_officer = client.send_invitation(manager_token, entity_a_id, officer_email, role_int=1) # 1 = Officer
    token_officer_raw = get_real_invite_token(officer_email)

    officer_member = client.accept_invitation(
        token=token_officer_raw,
        full_name=f"Cán bộ Kỹ thuật 1 {run_id}",
        password="Admin@123"
    )
    officer_user_id = officer_member["userId"]

    # Grant SigningAuthority to Officer A1 so that Maker self-approval hits the strict Maker != Checker guard directly
    officer_auth_res = client.request_signing_authority(
        proc_token=manager_token,
        entity_id=entity_a_id,
        user_id=officer_user_id,
        doc_num=f"QĐ-MK-{run_id}/2026/BQL",
        valid_from="2026-01-01T00:00:00Z",
        valid_to="2027-12-31T23:59:59Z",
        notes="Quyết định kiêm nhiệm để kiểm thử chặn tự phê duyệt Maker-Checker."
    )
    client.verify_signing_authority(admin_token, officer_auth_res["id"], is_approved=True)
    manifest["actors"]["officerA"] = {
        "userId": officer_user_id,
        "email": officer_email,
        "memberId": officer_member["id"],
        "invitationId": inv_officer["id"],
        "inviteToken": token_officer_raw,
        "entityId": entity_a_id,
        "authorityId": officer_auth_res["id"],
        "role": "Officer"
    }

    # Officer A2 (Non-Creator Officer in Entity A)
    officer2_email = f"officer2_{run_id}@procurement.com"
    client.send_invitation(manager_token, entity_a_id, officer2_email, role_int=1)
    token_officer2_raw = get_real_invite_token(officer2_email)
    officer2_member = client.accept_invitation(
        token=token_officer2_raw,
        full_name=f"Cán bộ Kỹ thuật 2 {run_id}",
        password="Admin@123"
    )
    manifest["actors"]["officerA2"] = {
        "userId": officer2_member["userId"],
        "email": officer2_email,
        "memberId": officer2_member["id"],
        "entityId": entity_a_id,
        "role": "Officer"
    }

    # Finance A (Finance role in Entity A: roleInEntity = 2)
    finance_email = f"finance_{run_id}@procurement.com"
    client.send_invitation(manager_token, entity_a_id, finance_email, role_int=2)
    token_finance_raw = get_real_invite_token(finance_email)
    finance_member = client.accept_invitation(
        token=token_finance_raw,
        full_name=f"Cán bộ Tài chính {run_id}",
        password="Admin@123"
    )
    manifest["actors"]["financeA"] = {
        "userId": finance_member["userId"],
        "email": finance_email,
        "memberId": finance_member["id"],
        "entityId": entity_a_id,
        "role": "Finance"
    }

    # 4. Provision Checker A (Signer with Active SigningAuthority) & Signer A2 (for revocation tests)
    logger.info("Step 4: Inviting & provisioning Checker A (Signer) and Signer A2...")
    checker_email = f"checker_{run_id}@procurement.com"
    client.send_invitation(manager_token, entity_a_id, checker_email, role_int=2) # 2 = Finance (then granted SigningAuthority)
    token_checker_raw = get_real_invite_token(checker_email)

    checker_member = client.accept_invitation(
        token=token_checker_raw,
        full_name=f"Lãnh đạo Ký duyệt {run_id}",
        password="Admin@123"
    )
    checker_user_id = checker_member["userId"]

    auth_doc = f"QĐ-{run_id}/2026/BQL"
    auth_res = client.request_signing_authority(
        proc_token=manager_token,
        entity_id=entity_a_id,
        user_id=checker_user_id,
        doc_num=auth_doc,
        valid_from="2026-01-01T00:00:00Z",
        valid_to="2027-12-31T23:59:59Z",
        notes="Quyết định phân công phụ trách kiểm soát tài chính và giải ngân hợp đồng."
    )
    authority_id = auth_res["id"]
    client.verify_signing_authority(admin_token, authority_id, is_approved=True)

    manifest["actors"]["checkerA"] = {
        "userId": checker_user_id,
        "email": checker_email,
        "memberId": checker_member["id"],
        "entityId": entity_a_id,
        "authorityId": authority_id,
        "signingAuthorityId": authority_id,
        "role": "Signer"
    }

    # Signer A2 (Active SigningAuthority -> used for membership & authority revocation cascade test)
    signer2_email = f"signer2_{run_id}@procurement.com"
    client.send_invitation(manager_token, entity_a_id, signer2_email, role_int=2)
    token_signer2_raw = get_real_invite_token(signer2_email)
    signer2_member = client.accept_invitation(
        token=token_signer2_raw,
        full_name=f"Lãnh đạo Dự phòng {run_id}",
        password="Admin@123"
    )
    auth2_res = client.request_signing_authority(
        proc_token=manager_token,
        entity_id=entity_a_id,
        user_id=signer2_member["userId"],
        doc_num=f"QĐ-S2-{run_id}/2026/BQL",
        valid_from="2026-01-01T00:00:00Z",
        valid_to="2027-12-31T23:59:59Z",
        notes="Quyết định ủy quyền dự phòng."
    )
    client.verify_signing_authority(admin_token, auth2_res["id"], is_approved=True)
    manifest["actors"]["signerA2"] = {
        "userId": signer2_member["userId"],
        "email": signer2_email,
        "memberId": signer2_member["id"],
        "entityId": entity_a_id,
        "authorityId": auth2_res["id"],
        "signingAuthorityId": auth2_res["id"],
        "role": "Signer"
    }

    # 5. Register Entity B (For cross-access / IDOR denial testing)
    logger.info("Step 5: Registering Procuring Entity B with real SMTP OTP...")
    proc_b_email = f"procurement_b_{run_id}@procurement.com"
    tax_code_b = f"0101{num_seed}"
    otp_b = get_real_otp(proc_b_email, "RegisterProcuringEntity")
    pe_b_res = client.register_procuring_entity(
        full_name=f"Giám đốc QLDA B {run_id}",
        email=proc_b_email,
        password="Admin@123",
        org_name=f"Ban Quản lý Dự án B {run_id}",
        tax_code=tax_code_b,
        pdf_path=ESTABLISHMENT_PDF,
        otp_code=otp_b
    )
    entity_b_id = pe_b_res["procuringEntityId"]
    client.verify_procuring_entity(admin_token, entity_b_id, is_approved=True)
    manifest["entities"]["entityB"] = {
        "id": entity_b_id,
        "name": f"Ban Quản lý Dự án B {run_id}",
        "taxCode": tax_code_b
    }
    manifest["actors"]["procurementB"] = {
        "userId": pe_b_res["userId"],
        "email": proc_b_email,
        "entityId": entity_b_id,
        "role": "Manager"
    }
    manifest["actors"]["managerB"] = manifest["actors"]["procurementB"]

    # 6. Retrieve seeded Approved Contractors C1 & C2 via GET /api/contractors/me (Zero SQL UPDATEs!)
    logger.info("Step 6: Retrieving Contractor 1 and Contractor 2 profiles via API...")
    import requests
    c1_tok, c1_u = client.login("contractor1@test.com", "Admin@123")
    c1_me = requests.get(f"{base_url}/api/contractors/me", headers={"Authorization": f"Bearer {c1_tok}"}).json()["data"]

    c2_tok, c2_u = client.login("contractor2@test.com", "Admin@123")
    c2_me = requests.get(f"{base_url}/api/contractors/me", headers={"Authorization": f"Bearer {c2_tok}"}).json()["data"]

    manifest["actors"]["contractor1"] = {
        "contractorId": c1_me["id"],
        "userId": c1_u["id"],
        "email": "contractor1@test.com",
        "companyName": c1_me["companyName"],
        "taxCode": c1_me["taxCode"]
    }
    manifest["actors"]["contractor2"] = {
        "contractorId": c2_me["id"],
        "userId": c2_u["id"],
        "email": "contractor2@test.com",
        "companyName": c2_me["companyName"],
        "taxCode": c2_me["taxCode"]
    }

    # 7. Evaluators (3 seeded evaluators with userIds resolved via login)
    logger.info("Step 7: Resolving Evaluator committee userIds via API...")
    ev_list = []
    for ev_email, ev_name in [
        ("evaluator@procurement.com", "Tran Van Giam Khao Cham Thau"),
        ("evaluator2@procurement.com", "Le Thi Giam Khao Ky Thuat"),
        ("evaluator3@procurement.com", "Pham Minh Tai - Giam Khao Tai Chinh"),
    ]:
        _, ev_u = client.login(ev_email, "Admin@123")
        ev_list.append({"userId": ev_u["id"], "email": ev_email, "name": ev_name})
    manifest["actors"]["evaluators"] = ev_list

    logger.info("Fixture setup successfully completed with ZERO SQL UPDATEs!")
    return manifest
