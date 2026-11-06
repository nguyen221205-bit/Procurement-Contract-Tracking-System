import os
import sys
import time
import hashlib
import json
import logging
from api_client import ApiClient
from db_helper import run_sqlcmd

logger = logging.getLogger("fixture_setup")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

SAMPLE_DOCS_DIR = r"D:\Thuc_Tap_backend\TaiLieuMau"
ESTABLISHMENT_PDF = os.path.join(SAMPLE_DOCS_DIR, "QuyetDinhThanhLap_DonViMoiThau.pdf")
LICENSE_PDF = os.path.join(SAMPLE_DOCS_DIR, "GiayPhepKinhDoanh_NhaThau.pdf")

def setup_fixtures(run_id, db_name, base_url="http://localhost:5225"):
    client = ApiClient(base_url)
    manifest = {
        "runId": run_id,
        "database": db_name,
        "timestampUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "actors": {},
        "entities": {}
    }

    # 1. Acquire Admin Login
    logger.info("Step 1: Logging in as System Admin...")
    admin_token, admin_user = client.login("admin@procurement.com", "Admin@123")
    manifest["actors"]["admin"] = {
        "userId": admin_user["id"],
        "email": admin_user["email"],
        "roles": admin_user.get("roles", ["Admin"])
    }

    # Generate numeric seed from run_id for unique tax codes
    num_seed = int(time.time()) % 900000 + 100000

    # 2. Register Entity A
    logger.info("Step 2: Registering Procuring Entity A...")
    manager_email = f"manager_{run_id}@procurement.com"
    tax_code_a = f"0100{num_seed}"
    pe_a_res = client.register_procuring_entity(
        full_name=f"Giám đốc QLDA {run_id}",
        email=manager_email,
        password="Admin@123",
        org_name=f"Ban Quản lý Dự án E2E {run_id}",
        tax_code=tax_code_a,
        pdf_path=ESTABLISHMENT_PDF
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

    # 3. Provision Officer A (Maker)
    logger.info("Step 3: Inviting & provisioning Officer A (Maker)...")
    officer_email = f"officer_{run_id}@procurement.com"
    inv_officer = client.send_invitation(manager_token, entity_a_id, officer_email, role_int=1) # 1 = Officer
    officer_inv_id = inv_officer["id"]

    # Set TokenHash in DB to known token
    token_officer_raw = f"token_officer_{run_id}"
    token_officer_hash = hashlib.sha256(token_officer_raw.encode("utf-8")).hexdigest().lower()
    sql_officer = f"UPDATE ProcuringEntityInvitations SET TokenHash = '{token_officer_hash}' WHERE Id = {officer_inv_id};"
    run_sqlcmd(sql_officer, db_name=db_name)

    # Officer A accepts invitation
    officer_member = client.accept_invitation(
        token=token_officer_raw,
        full_name=f"Cán bộ Kỹ thuật {run_id}",
        password="Admin@123"
    )
    officer_user_id = officer_member["userId"]
    # Also grant SigningAuthority to Officer A so that Case 2 (Maker self-approval) hits the strict Maker != Checker guard directly
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
        "entityId": entity_a_id,
        "authorityId": officer_auth_res["id"],
        "role": "Officer"
    }

    # 4. Provision Checker A (Signer with SigningAuthority)
    logger.info("Step 4: Inviting & provisioning Checker A (Signer)...")
    checker_email = f"checker_{run_id}@procurement.com"
    inv_checker = client.send_invitation(manager_token, entity_a_id, checker_email, role_int=1) # Member of entity
    checker_inv_id = inv_checker["id"]

    token_checker_raw = f"token_checker_{run_id}"
    token_checker_hash = hashlib.sha256(token_checker_raw.encode("utf-8")).hexdigest().lower()
    sql_checker = f"UPDATE ProcuringEntityInvitations SET TokenHash = '{token_checker_hash}' WHERE Id = {checker_inv_id};"
    run_sqlcmd(sql_checker, db_name=db_name)

    checker_member = client.accept_invitation(
        token=token_checker_raw,
        full_name=f"Lãnh đạo Ký duyệt {run_id}",
        password="Admin@123"
    )
    checker_user_id = checker_member["userId"]

    # Request Signing Authority for Checker A
    logger.info("Requesting Signing Authority for Checker A...")
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

    # Admin verifies and approves Signing Authority
    logger.info(f"Admin approving Signing Authority (ID: {authority_id})...")
    client.verify_signing_authority(admin_token, authority_id, is_approved=True)

    manifest["actors"]["checkerA"] = {
        "userId": checker_user_id,
        "email": checker_email,
        "memberId": checker_member["id"],
        "entityId": entity_a_id,
        "authorityId": authority_id,
        "role": "Signer"
    }

    # 5. Register Entity B (For cross-access / IDOR denial testing)
    logger.info("Step 5: Registering Procuring Entity B...")
    proc_b_email = f"procurement_b_{run_id}@procurement.com"
    tax_code_b = f"0101{num_seed}"
    pe_b_res = client.register_procuring_entity(
        full_name=f"Giám đốc QLDA B {run_id}",
        email=proc_b_email,
        password="Admin@123",
        org_name=f"Ban Quản lý Dự án B {run_id}",
        tax_code=tax_code_b,
        pdf_path=ESTABLISHMENT_PDF
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

    # 6. Register Contractor 1 & Contractor 2
    logger.info("Step 6: Provisioning & approving Contractor 1 and Contractor 2...")
    c1_email = f"contractor1_{run_id}@test.com"
    c1_company = f"Công ty CP Công nghệ & Thiết bị E2E {run_id}"
    c1_tax = f"0301{num_seed}"

    c2_email = f"contractor2_{run_id}@test.com"
    c2_company = f"Công ty TNHH Giải pháp Đấu thầu E2E {run_id}"
    c2_tax = f"0302{num_seed}"

    # Configure isolated test contractor accounts with run-specific emails/names/tax codes, then verify via Admin API
    sql_c1 = f"""
    UPDATE Users SET Email = N'{c1_email}', FullName = N'Đại diện Nhà thầu 1 {run_id}' WHERE Email = 'contractor1@test.com';
    UPDATE Contractors SET CompanyName = N'{c1_company}', TaxCode = '{c1_tax}', VerificationStatus = N'Pending'
    WHERE UserId = (SELECT Id FROM Users WHERE Email = N'{c1_email}');
    """
    run_sqlcmd(sql_c1, db_name=db_name)

    sql_c2 = f"""
    UPDATE Users SET Email = N'{c2_email}', FullName = N'Đại diện Nhà thầu 2 {run_id}' WHERE Email = 'contractor2@test.com';
    UPDATE Contractors SET CompanyName = N'{c2_company}', TaxCode = '{c2_tax}', VerificationStatus = N'Pending'
    WHERE UserId = (SELECT Id FROM Users WHERE Email = N'{c2_email}');
    """
    run_sqlcmd(sql_c2, db_name=db_name)

    # Retrieve Contractor IDs and User IDs
    from db_helper import query_db_scalar
    c1_row = query_db_scalar(f"SELECT CONCAT(c.Id, ':', c.UserId) FROM Contractors c INNER JOIN Users u ON c.UserId = u.Id WHERE u.Email = N'{c1_email}'", db_name=db_name)
    c1_id, c1_uid = [int(x) for x in c1_row.split(":")]
    client.verify_contractor(admin_token, c1_id, is_approved=True, notes="Hồ sơ năng lực Nhà thầu 1 đạt chuẩn E2E")

    c2_row = query_db_scalar(f"SELECT CONCAT(c.Id, ':', c.UserId) FROM Contractors c INNER JOIN Users u ON c.UserId = u.Id WHERE u.Email = N'{c2_email}'", db_name=db_name)
    c2_id, c2_uid = [int(x) for x in c2_row.split(":")]
    client.verify_contractor(admin_token, c2_id, is_approved=True, notes="Hồ sơ năng lực Nhà thầu 2 đạt chuẩn E2E")

    manifest["actors"]["contractor1"] = {
        "contractorId": c1_id,
        "userId": c1_uid,
        "email": c1_email,
        "companyName": c1_company,
        "taxCode": c1_tax
    }
    manifest["actors"]["contractor2"] = {
        "contractorId": c2_id,
        "userId": c2_uid,
        "email": c2_email,
        "companyName": c2_company,
        "taxCode": c2_tax
    }

    # 7. Evaluators
    logger.info("Step 7: Recording Evaluator committee...")
    manifest["actors"]["evaluators"] = [
        {"email": "evaluator@procurement.com", "name": "Tran Van Giam Khao Cham Thau"},
        {"email": "evaluator2@procurement.com", "name": "Le Thi Giam Khao Ky Thuat"},
        {"email": "evaluator3@procurement.com", "name": "Pham Minh Tai - Giam Khao Tai Chinh"}
    ]

    logger.info("Fixture setup successfully completed!")
    return manifest

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python fixture_setup.py <run_id> <db_name> [base_url]")
        sys.exit(1)
    run_id_arg = sys.argv[1]
    db_name_arg = sys.argv[2]
    url_arg = sys.argv[3] if len(sys.argv) > 3 else "http://localhost:5225"
    m = setup_fixtures(run_id_arg, db_name_arg, url_arg)
    print(json.dumps(m, indent=2, ensure_ascii=False))
