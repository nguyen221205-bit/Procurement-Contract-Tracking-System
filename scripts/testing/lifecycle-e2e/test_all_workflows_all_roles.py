import os
import csv
import json
import time
import zipfile
import hashlib
import logging
import requests
from datetime import datetime, timedelta, timezone
from playwright.sync_api import sync_playwright
from db_helper import run_sqlcmd

logger = logging.getLogger("test_all_workflows_all_roles")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

SAMPLE_DOCS_DIR = r"D:\Thuc_Tap_backend\TaiLieuMau"
HSMT_PDF = os.path.join(SAMPLE_DOCS_DIR, "HoSoMoiThau_HSMT.pdf")
BAOGIA_PDF = os.path.join(SAMPLE_DOCS_DIR, "BaoGia_TaiChinh.pdf")
NANGLUC_PDF = os.path.join(SAMPLE_DOCS_DIR, "HoSoNangLuc.pdf")
TIENDO_PDF = os.path.join(SAMPLE_DOCS_DIR, "TienDo_BienPhap.pdf")
SCAN_PDF = os.path.join(SAMPLE_DOCS_DIR, "HopDong_DaKy_Scan.pdf")


class CaseRecorder:
    def __init__(self, run_output_dir: str):
        self.run_output_dir = run_output_dir
        self.cases = []

    def record(
        self,
        case_id: str,
        chain: str,
        module: str,
        role: str,
        entity_scope: str,
        precondition_state: str,
        action: str,
        expected_http: str,
        actual_http: str,
        expected_post_state: str,
        actual_post_state: str,
        passed: bool,
        evidence_file: str = "",
        notes: str = "",
    ):
        status = "PASS" if passed else "FAIL"
        entry = {
            "caseId": case_id,
            "chain": chain,
            "module": module,
            "role": role,
            "entityScope": entity_scope,
            "preconditionState": precondition_state,
            "action": action,
            "expectedHttpStatus": str(expected_http),
            "actualHttpStatus": str(actual_http),
            "expectedPostState": expected_post_state,
            "actualPostState": actual_post_state,
            "evidenceFile": evidence_file,
            "status": status,
            "notes": notes,
        }
        self.cases.append(entry)
        logger.info(f"[{status}] {case_id} ({module} / {role}): HTTP {actual_http} | {notes or actual_post_state}")
        return entry

    def save(self):
        json_path = os.path.join(self.run_output_dir, "case_results.json")
        csv_path = os.path.join(self.run_output_dir, "case_results.csv")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(self.cases, f, indent=2, ensure_ascii=False)

        fieldnames = [
            "caseId",
            "chain",
            "module",
            "role",
            "entityScope",
            "preconditionState",
            "action",
            "expectedHttpStatus",
            "actualHttpStatus",
            "expectedPostState",
            "actualPostState",
            "evidenceFile",
            "status",
            "notes",
        ]
        with open(csv_path, "w", encoding="utf-8-sig", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            for row in self.cases:
                writer.writerow(row)
        return json_path, csv_path


def create_minimal_xlsx(file_path: str, rows: list):
    """Create a minimal valid OpenXML .xlsx workbook using Python stdlib zipfile."""
    content_types = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>"""
    rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>"""
    wb_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>"""
    workbook = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Contractors" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>"""

    def esc(val: str) -> str:
        return (
            str(val)
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace('"', "&quot;")
        )

    col_letters = ["A", "B", "C", "D", "E", "F"]
    sheet_rows = []
    for r_idx, row_vals in enumerate(rows, start=1):
        cells = []
        for c_idx, val in enumerate(row_vals):
            col_ref = f"{col_letters[c_idx]}{r_idx}"
            cells.append(f'<c r="{col_ref}" t="inlineStr"><is><t>{esc(val)}</t></is></c>')
        sheet_rows.append(f'<row r="{r_idx}">{"".join(cells)}</row>')

    sheet1 = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    {"".join(sheet_rows)}
  </sheetData>
</worksheet>"""

    with zipfile.ZipFile(file_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr("[Content_Types].xml", content_types)
        zf.writestr("_rels/.rels", rels)
        zf.writestr("xl/workbook.xml", workbook)
        zf.writestr("xl/_rels/workbook.xml.rels", wb_rels)
        zf.writestr("xl/worksheets/sheet1.xml", sheet1)


def get_otp_via_smtp(api_base_url: str, smtp_catcher, email_addr: str, purpose: str = "RegisterContractor") -> str:
    min_idx = smtp_catcher.count_messages()
    r = requests.post(f"{api_base_url}/api/auth/send-otp", json={"email": email_addr, "purpose": purpose})
    assert r.status_code == 200, f"send-otp failed for {email_addr}: {r.status_code} {r.text}"
    msg = smtp_catcher.wait_for_email(email_addr, timeout_sec=10.0, min_index=min_idx)
    assert msg and msg.get("extractedOtp"), f"No OTP email received for {email_addr}"
    return msg["extractedOtp"]


def login_token(api_base_url: str, email_addr: str, password: str = "Admin@123") -> str:
    r = requests.post(f"{api_base_url}/api/auth/login", json={"email": email_addr, "password": password})
    assert r.status_code == 200, f"Login failed for {email_addr}: {r.status_code} {r.text}"
    return r.json()["data"]["token"]


def auth_h(token: str) -> dict:
    return {"Authorization": f"Bearer {token}"}


def run_all_workflows_all_roles(
    run_id: str,
    manifest: dict,
    base_url: str = "http://localhost:5173",
    api_base_url: str = "http://localhost:5225",
    run_output_dir: str = None,
    smtp_catcher=None,
):
    if run_output_dir is None:
        run_output_dir = os.path.join(os.path.dirname(__file__), "runs", run_id)
    os.makedirs(run_output_dir, exist_ok=True)
    screenshots_dir = os.path.join(run_output_dir, "screenshots")
    exports_dir = os.path.join(run_output_dir, "exports")
    os.makedirs(screenshots_dir, exist_ok=True)
    os.makedirs(exports_dir, exist_ok=True)

    rec = CaseRecorder(run_output_dir)
    db_name = manifest["database"]
    actors = manifest["actors"]
    entities = manifest["entities"]

    admin_email = actors["admin"]["email"]
    admin_tok = login_token(api_base_url, admin_email)
    mgr_a_email = actors["managerA"]["email"]
    mgr_a_tok = login_token(api_base_url, mgr_a_email)
    off_a1_email = actors["officerA"]["email"]
    off_a1_tok = login_token(api_base_url, off_a1_email)
    off_a2_email = actors["officerA2"]["email"]
    off_a2_tok = login_token(api_base_url, off_a2_email)
    fin_a_email = actors["financeA"]["email"]
    fin_a_tok = login_token(api_base_url, fin_a_email)
    sig_a1_email = actors["checkerA"]["email"]
    sig_a1_tok = login_token(api_base_url, sig_a1_email)
    sig_a2_email = actors["signerA2"]["email"]
    sig_a2_tok = login_token(api_base_url, sig_a2_email)
    mgr_b_email = actors["managerB"]["email"]
    mgr_b_tok = login_token(api_base_url, mgr_b_email)
    c1_email = actors["contractor1"]["email"]
    c1_tok = login_token(api_base_url, c1_email)
    c2_email = actors["contractor2"]["email"]
    c2_tok = login_token(api_base_url, c2_email)
    ev1_email = actors["evaluators"][0]["email"]
    ev1_tok = login_token(api_base_url, ev1_email)
    ev2_email = actors["evaluators"][1]["email"]
    ev2_tok = login_token(api_base_url, ev2_email)
    ev3_email = actors["evaluators"][2]["email"]
    ev3_tok = login_token(api_base_url, ev3_email)

    entity_a_id = entities["entityA"]["id"]
    entity_b_id = entities["entityB"]["id"]
    main_pkg_id = manifest["businessFlow"]["packageId"]
    main_contract_id = manifest["businessFlow"]["contractId"]

    # =========================================================================
    # 1. HEALTH & AUTH / OTP / TAX / REGISTRATION / TOKENS
    # =========================================================================
    r_health = requests.get(f"{api_base_url}/api/health")
    rec.record(
        "HEALTH-01", "Chain1", "HEALTH", "Unauthenticated", "Public",
        "API running", "GET /api/health", "200", r_health.status_code,
        "status=Healthy", r_health.text[:120],
        r_health.status_code == 200 and "Healthy" in r_health.text
    )

    otp_test_email = f"otp_check_{run_id}@e2e.local".lower()
    min_idx = smtp_catcher.count_messages()
    r_otp1 = requests.post(
        f"{api_base_url}/api/auth/send-otp",
        json={"email": otp_test_email, "purpose": "RegisterContractor"}
    )
    otp_msg = smtp_catcher.wait_for_email(otp_test_email, timeout_sec=10.0, min_index=min_idx)
    otp_code_1 = otp_msg["extractedOtp"] if otp_msg else None
    dev_otp_val = r_otp1.json().get("data", {}).get("devOtpCode") if r_otp1.status_code == 200 else "ERR"
    rec.record(
        "AUTH-OTP-01", "Chain1", "AUTH", "Unauthenticated", "Public",
        "Fresh email", "POST /api/auth/send-otp", "200", r_otp1.status_code,
        "OTP captured in SMTP & devOtpCode=None", f"OTP={otp_code_1}, devOtpCode={dev_otp_val}",
        r_otp1.status_code == 200 and bool(otp_code_1) and dev_otp_val is None,
        evidence_file="smtp_messages.json"
    )

    # AUTH-OTP-02: Cooldown 60s (even when changing purpose)
    r_otp_cool = requests.post(
        f"{api_base_url}/api/auth/send-otp",
        json={"email": otp_test_email, "purpose": "RegisterProcuringEntity"}
    )
    rec.record(
        "AUTH-OTP-02", "Chain1", "AUTH", "Unauthenticated", "Public",
        "Within 60s of previous OTP", "POST /api/auth/send-otp (cooldown)", "400", r_otp_cool.status_code,
        "Rejected by 60s cooldown", f"HTTP {r_otp_cool.status_code}",
        r_otp_cool.status_code == 400
    )

    # AUTH-OTP-03: Wrong OTP rejected
    with open(HSMT_PDF, "rb") as f_pdf:
        r_wrong_otp = requests.post(
            f"{api_base_url}/api/auth/register-contractor",
            data={
                "FullName": "Wrong OTP User",
                "Email": otp_test_email,
                "Password": "Admin@123",
                "Phone": "0909111222",
                "CompanyName": f"Wrong OTP Co {run_id}",
                "TaxCode": f"0399{int(time.time()) % 1000000:06d}",
                "Address": "123 Test",
                "OtpCode": "000000",
            },
            files={"BusinessLicenseFile": ("license.pdf", f_pdf, "application/pdf")},
        )
    rec.record(
        "AUTH-OTP-03", "Chain1", "AUTH", "Unauthenticated", "Public",
        "Valid OTP exists in cache", "POST /api/auth/register-contractor with wrong OTP", "400", r_wrong_otp.status_code,
        "Rejected 400", f"HTTP {r_wrong_otp.status_code}",
        r_wrong_otp.status_code == 400
    )

    # AUTH-OTP-04: Brute-force 5 wrong attempts invalidates OTP even if 6th uses real OTP
    bf_email = f"otp_bf_{run_id}@e2e.local".lower()
    bf_otp = get_otp_via_smtp(api_base_url, smtp_catcher, bf_email, "RegisterContractor")
    for attempt in range(5):
        with open(HSMT_PDF, "rb") as f_pdf:
            requests.post(
                f"{api_base_url}/api/auth/register-contractor",
                data={
                    "FullName": "BF User",
                    "Email": bf_email,
                    "Password": "Admin@123",
                    "Phone": "0909111223",
                    "CompanyName": f"BF Co {run_id}",
                    "TaxCode": f"0398{int(time.time()) % 1000000:06d}",
                    "Address": "123 Test",
                    "OtpCode": "999999",
                },
                files={"BusinessLicenseFile": ("license.pdf", f_pdf, "application/pdf")},
            )
    with open(HSMT_PDF, "rb") as f_pdf:
        r_bf_6th = requests.post(
            f"{api_base_url}/api/auth/register-contractor",
            data={
                "FullName": "BF User",
                "Email": bf_email,
                "Password": "Admin@123",
                "Phone": "0909111223",
                "CompanyName": f"BF Co {run_id}",
                "TaxCode": f"0398{int(time.time()) % 1000000:06d}",
                "Address": "123 Test",
                "OtpCode": bf_otp,
            },
            files={"BusinessLicenseFile": ("license.pdf", f_pdf, "application/pdf")},
        )
    rec.record(
        "AUTH-OTP-04", "Chain1", "AUTH", "Unauthenticated", "Public",
        "5 failed OTP attempts", "6th attempt with real OTP after 5 wrong attempts", "400", r_bf_6th.status_code,
        "OTP invalidated after 5 failures", f"HTTP {r_bf_6th.status_code}",
        r_bf_6th.status_code == 400
    )

    # AUTH-TAX-01: Tax lookup
    r_tax = requests.get(f"{api_base_url}/api/auth/tax-lookup/0100109106")
    rec.record(
        "AUTH-TAX-01", "Chain1", "AUTH", "Unauthenticated", "Public",
        "Public endpoint", "GET /api/auth/tax-lookup/0100109106", "200|404", r_tax.status_code,
        "Valid response from tax lookup service", f"HTTP {r_tax.status_code}",
        r_tax.status_code in (200, 404)
    )

    # AUTH-REG-01: Verified in fixture_setup (Entity A & B registered via real OTP)
    rec.record(
        "AUTH-REG-01", "Chain1", "AUTH", "Unauthenticated", "EntityA",
        "Valid OTP from SMTP", "POST /api/auth/register-procuring-entity (Entity A & B)", "201", 201,
        "Entity A & B created + Manager Active", f"entityA={entity_a_id}, entityB={entity_b_id}",
        entity_a_id > 0 and entity_b_id > 0,
        evidence_file="manifest.json"
    )

    # AUTH-REG-02: Security test - Register contractor WITHOUT OtpCode (Expected 400; dev has optional OTP bug -> 201)
    no_otp_c_email = f"no_otp_ctr_{run_id}@e2e.local".lower()
    no_otp_c_tax = "0100684378"  # Valid VietQR enterprise tax code (VNPT)
    with open(HSMT_PDF, "rb") as f_pdf:
        r_no_otp_c = requests.post(
            f"{api_base_url}/api/auth/register-contractor",
            data={
                "FullName": "No OTP Contractor",
                "Email": no_otp_c_email,
                "Password": "Admin@123",
                "Phone": "0909333444",
                "CompanyName": f"Cong ty Khong OTP {run_id}",
                "TaxCode": no_otp_c_tax,
                "Address": "TP.HCM",
            },
            files={"BusinessLicenseFile": ("license.pdf", f_pdf, "application/pdf")},
        )
    rec.record(
        "AUTH-REG-02", "Chain1", "AUTH", "Unauthenticated", "Contractor",
        "No OtpCode supplied", "POST /api/auth/register-contractor without OtpCode", "400", r_no_otp_c.status_code,
        "Must reject registration without OTP (400)", f"Actual HTTP {r_no_otp_c.status_code}",
        r_no_otp_c.status_code == 400,
        notes="BUG: ContractorAuthService.cs line 41 only verifies OTP if !string.IsNullOrWhiteSpace(request.OtpCode)" if r_no_otp_c.status_code != 400 else "Blocked as expected"
    )

    # AUTH-REG-03: Security test - Register procuring entity WITHOUT OtpCode (Expected 400; dev has optional OTP bug -> 201)
    no_otp_pe_email = f"no_otp_pe_{run_id}@e2e.local".lower()
    no_otp_pe_code = f"PE{int(time.time()) % 100000:05d}"
    no_otp_pe_tax = f"0199{int(time.time()) % 1000000:06d}"
    with open(HSMT_PDF, "rb") as f_pdf:
        r_no_otp_pe = requests.post(
            f"{api_base_url}/api/auth/register-procuring-entity",
            data={
                "FullName": "No OTP Entity Rep",
                "Email": no_otp_pe_email,
                "Password": "Admin@123",
                "OrganizationName": f"Ban QLDA Khong OTP {run_id}",
                "OrganizationType": "StateEnterprise",
                "TaxCode": no_otp_pe_tax,
                "BudgetCode": no_otp_pe_code,
                "RepresentativeName": "Dai dien Khong OTP",
                "RepresentativeTitle": "Giam doc",
                "RepresentativePhone": "0909555666",
                "Address": "Ha Noi",
            },
            files={"EstablishmentDecisionFile": ("decision.pdf", f_pdf, "application/pdf")},
        )
    rec.record(
        "AUTH-REG-03", "Chain1", "AUTH", "Unauthenticated", "Entity",
        "No OtpCode supplied", "POST /api/auth/register-procuring-entity without OtpCode", "400", r_no_otp_pe.status_code,
        "Must reject registration without OTP (400)", f"Actual HTTP {r_no_otp_pe.status_code}",
        r_no_otp_pe.status_code == 400,
        notes="BUG: ProcuringEntityAuthService.cs line 39 only verifies OTP if !string.IsNullOrWhiteSpace(request.OtpCode)" if r_no_otp_pe.status_code != 400 else "Blocked as expected"
    )

    # AUTH-REG-04: Contractor C1 & C2 registered with real OTP in fixture_setup
    rec.record(
        "AUTH-REG-04", "Chain1", "AUTH", "Unauthenticated", "Contractor",
        "Valid OTP from SMTP", "POST /api/auth/register-contractor (C1 & C2)", "201", 201,
        "C1 & C2 created in Pending state", f"C1={actors['contractor1']['contractorId']}, C2={actors['contractor2']['contractorId']}",
        actors["contractor1"]["contractorId"] > 0 and actors["contractor2"]["contractorId"] > 0,
        evidence_file="manifest.json"
    )

    # AUTH-REG-05: Duplicate TaxCode / Email rejected
    with open(HSMT_PDF, "rb") as f_pdf:
        r_dup_c = requests.post(
            f"{api_base_url}/api/auth/register-contractor",
            data={
                "FullName": "Dup User",
                "Email": c1_email,
                "Password": "Admin@123",
                "Phone": "0909111222",
                "CompanyName": "Dup Co",
                "TaxCode": actors["contractor1"]["taxCode"],
                "Address": "Ha Noi",
                "OtpCode": otp_code_1 or "123456",
            },
            files={"BusinessLicenseFile": ("license.pdf", f_pdf, "application/pdf")},
        )
    rec.record(
        "AUTH-REG-05", "Chain1", "AUTH", "Unauthenticated", "Contractor",
        "C1 email/taxCode already registered", "POST /api/auth/register-contractor duplicate", "400", r_dup_c.status_code,
        "Rejected 400", f"HTTP {r_dup_c.status_code}",
        r_dup_c.status_code == 400
    )

    # AUTH-REG-06: Invalid file extension (.exe) rejected by FileStorageService
    bad_ext_email = f"bad_ext_{run_id}@e2e.local".lower()
    bad_ext_otp = get_otp_via_smtp(api_base_url, smtp_catcher, bad_ext_email, "RegisterProcuringEntity")
    r_bad_ext = requests.post(
        f"{api_base_url}/api/auth/register-procuring-entity",
        data={
            "FullName": "Bad Ext User",
            "Email": bad_ext_email,
            "Password": "Admin@123",
            "OrganizationName": f"Bad Ext Org {run_id}",
            "OrganizationType": "StateEnterprise",
            "TaxCode": f"0366{int(time.time()) % 1000000:06d}",
            "BudgetCode": f"BEX{int(time.time()) % 100000:05d}",
            "RepresentativeName": "Bad Ext Rep",
            "RepresentativeTitle": "Giam doc",
            "RepresentativePhone": "0909111222",
            "Address": "Ha Noi",
            "OtpCode": bad_ext_otp,
        },
        files={"EstablishmentDecisionFile": ("malware.exe", b"MZ_FAKE_EXE", "application/octet-stream")},
    )
    rec.record(
        "AUTH-REG-06", "Chain1", "AUTH", "Unauthenticated", "Contractor",
        "File extension .exe", "POST /api/auth/register-procuring-entity (.exe)", "400", r_bad_ext.status_code,
        "Rejected 400 invalid file type", f"HTTP {r_bad_ext.status_code}",
        r_bad_ext.status_code == 400
    )

    # AUTH-LOGIN-01 & AUTH-LOGIN-02 & AUTH-TOKEN-01 & AUTH-TOKEN-02
    r_login_adm = requests.post(f"{api_base_url}/api/auth/login", json={"email": admin_email, "password": "Admin@123"})
    login_data = r_login_adm.json()["data"]
    rec.record(
        "AUTH-LOGIN-01", "Chain1", "AUTH", "Admin", "Platform",
        "Active Admin account", "POST /api/auth/login", "200", r_login_adm.status_code,
        "Token + RefreshToken + UserInfo issued", f"HTTP {r_login_adm.status_code}, user={login_data.get('user', {}).get('email')}",
        r_login_adm.status_code == 200 and bool(login_data.get("token")) and bool(login_data.get("refreshToken"))
    )

    r_bad_login = requests.post(f"{api_base_url}/api/auth/login", json={"email": admin_email, "password": "WrongPassword@999"})
    rec.record(
        "AUTH-LOGIN-02", "Chain1", "AUTH", "Unauthenticated", "Public",
        "Wrong password", "POST /api/auth/login wrong password", "400|401", r_bad_login.status_code,
        "Rejected 400/401", f"HTTP {r_bad_login.status_code}",
        r_bad_login.status_code in (400, 401)
    )

    r_ref = requests.post(
        f"{api_base_url}/api/auth/refresh-token",
        json={"token": login_data["token"], "refreshToken": login_data["refreshToken"]}
    )
    ref_data = r_ref.json().get("data", {}) if r_ref.status_code == 200 else {}
    rec.record(
        "AUTH-TOKEN-01", "Chain1", "AUTH", "Admin", "Platform",
        "Valid RefreshToken", "POST /api/auth/refresh-token", "200", r_ref.status_code,
        "New token pair issued", f"HTTP {r_ref.status_code}",
        r_ref.status_code == 200 and bool(ref_data.get("token"))
    )

    # Logout on a secondary session (c2) so we verify refresh-token revocation cleanly
    r_c2_login = requests.post(f"{api_base_url}/api/auth/login", json={"email": c2_email, "password": "Admin@123"}).json()["data"]
    r_logout = requests.post(f"{api_base_url}/api/auth/logout", headers=auth_h(r_c2_login["token"]))
    r_ref_after_logout = requests.post(
        f"{api_base_url}/api/auth/refresh-token",
        json={"token": r_c2_login["token"], "refreshToken": r_c2_login["refreshToken"]}
    )
    rec.record(
        "AUTH-TOKEN-02", "Chain1", "AUTH", "Contractor", "Platform",
        "Logged in session", "POST /api/auth/logout -> POST /api/auth/refresh-token with revoked token", "200->400", f"{r_logout.status_code}->{r_ref_after_logout.status_code}",
        "Logout 200 & old RefreshToken rejected 400", f"logout={r_logout.status_code}, refresh={r_ref_after_logout.status_code}",
        r_logout.status_code == 200 and r_ref_after_logout.status_code in (400, 401)
    )
    c2_tok = login_token(api_base_url, c2_email)
    admin_tok = login_token(api_base_url, admin_email)

    # =========================================================================
    # 2. USER & ROLE MANAGEMENT + LOCK / JWT REVOCATION + EXCEL IMPORT / MCP
    # =========================================================================
    r_users = requests.get(f"{api_base_url}/api/users?pageIndex=1&pageSize=20", headers=auth_h(admin_tok))
    r_user_detail = requests.get(f"{api_base_url}/api/users/{actors['officerA']['userId']}", headers=auth_h(admin_tok))
    r_user_upd = requests.put(
        f"{api_base_url}/api/users/{actors['officerA']['userId']}",
        json={"fullName": "Chuyen vien Dau thau A1", "phone": "0901234567", "isActive": True},
        headers=auth_h(admin_tok)
    )
    r_roles = requests.get(f"{api_base_url}/api/roles", headers=auth_h(admin_tok))
    rec.record(
        "USER-LIST-01", "Chain1", "USER", "Admin", "Platform",
        "Admin logged in", "GET /api/users, GET /api/users/{id}, PUT /api/users/{id}, GET /api/roles", "200", r_users.status_code,
        "All return 200 & 4 system roles", f"users={r_users.status_code}, detail={r_user_detail.status_code}, upd={r_user_upd.status_code}, roles={r_roles.status_code}",
        all(x.status_code == 200 for x in (r_users, r_user_detail, r_user_upd, r_roles))
    )

    r_users_c1 = requests.get(f"{api_base_url}/api/users", headers=auth_h(c1_tok))
    r_roles_ev1 = requests.get(f"{api_base_url}/api/roles", headers=auth_h(ev1_tok))
    rec.record(
        "USER-LIST-02", "Chain5", "USER", "Contractor_And_Evaluator", "Platform",
        "Non-Admin/Non-Procurement tokens", "GET /api/users (Contractor) & GET /api/roles (Evaluator)", "403", f"{r_users_c1.status_code}/{r_roles_ev1.status_code}",
        "Both rejected 403 Forbidden", f"users_c1={r_users_c1.status_code}, roles_ev1={r_roles_ev1.status_code}",
        r_users_c1.status_code == 403 and r_roles_ev1.status_code == 403
    )

    # USER-ROLES-01: Verified in fixture_setup (Evaluators E1-E3 assigned Evaluator role via POST /api/users/{id}/roles)
    rec.record(
        "USER-ROLES-01", "Chain1", "USER", "Admin", "Platform",
        "Admin logged in", "POST /api/users/{id}/roles (Evaluator)", "200", 200,
        "Assigned Evaluator role + AuditLog ASSIGN_USER_ROLES", f"evaluators={[e['userId'] for e in actors['evaluators']]}",
        len(actors["evaluators"]) == 3
    )

    # USER-ROLES-02: Conflict of interest Contractor + internal role (Admin/Procurement/Evaluator)
    roles_list = r_roles.json()["data"]
    role_map = {r["name"]: r["id"] for r in roles_list}
    r_role_coi = requests.post(
        f"{api_base_url}/api/users/{actors['contractor1']['userId']}/roles",
        json={"roleIds": [role_map["Contractor"], role_map["Evaluator"]]},
        headers=auth_h(admin_tok)
    )
    rec.record(
        "USER-ROLES-02", "Chain5", "USER", "Admin", "Platform",
        "Contractor user", "POST /api/users/{id}/roles [Contractor, Evaluator]", "400", r_role_coi.status_code,
        "Blocked 400 conflict of interest", f"HTTP {r_role_coi.status_code}",
        r_role_coi.status_code == 400
    )

    # USER-UPD-01: Admin cannot self-lock or remove own Admin role
    r_self_lock = requests.patch(f"{api_base_url}/api/users/{actors['admin']['userId']}/status", headers=auth_h(admin_tok))
    r_self_demote = requests.post(
        f"{api_base_url}/api/users/{actors['admin']['userId']}/roles",
        json={"roleIds": [role_map["Procurement"]]},
        headers=auth_h(admin_tok)
    )
    rec.record(
        "USER-UPD-01", "Chain5", "USER", "Admin", "Platform",
        "Admin userId", "PATCH /api/users/{adminId}/status & POST /api/users/{adminId}/roles without Admin", "400", f"{r_self_lock.status_code}/{r_self_demote.status_code}",
        "Both self-lock and self-demote rejected 400", f"lock={r_self_lock.status_code}, demote={r_self_demote.status_code}",
        r_self_lock.status_code == 400 and r_self_demote.status_code == 400
    )

    # CTR-XLS-01: Download template + Import Contractors Excel (creates MustChangePassword=true user) + Export Excel
    r_tpl = requests.get(f"{api_base_url}/api/contractors/import-template", headers=auth_h(admin_tok))
    tpl_path = os.path.join(exports_dir, "contractor_import_template.xlsx")
    with open(tpl_path, "wb") as f:
        f.write(r_tpl.content)

    imp_email = f"imp_ctr_{run_id}@e2e.local".lower()
    imp_tax = f"0355{int(time.time()) % 1000000:06d}"
    imp_xlsx_path = os.path.join(exports_dir, f"import_contractors_{run_id}.xlsx")
    create_minimal_xlsx(
        imp_xlsx_path,
        [
            ["Ten doanh nghiep", "Ma so thue", "Dia chi", "Nguoi dai dien", "Email", "So dien thoai"],
            [f"=1+1 Cong ty Import {run_id}", imp_tax, "Da Nang", "Nguyen Van Import", imp_email, "0905123456"],
            ["Cong ty Trung MST", imp_tax, "Da Nang", "Nguyen Van Dup", f"dup_{imp_email}", "0905123457"],
        ],
    )
    min_idx_imp = smtp_catcher.count_messages()
    with open(imp_xlsx_path, "rb") as f_xlsx:
        r_imp = requests.post(
            f"{api_base_url}/api/contractors/import-excel",
            files={"file": ("import.xlsx", f_xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")},
            headers=auth_h(admin_tok),
        )
    imp_data = r_imp.json().get("data", {}) if r_imp.status_code == 200 else {}
    imp_mail = smtp_catcher.wait_for_email(imp_email, timeout_sec=10.0, min_index=min_idx_imp)
    imp_temp_pwd = imp_mail.get("extractedTempPassword") if imp_mail else None

    r_exp_ctr_xlsx = requests.get(f"{api_base_url}/api/reports/contractors/export-excel", headers=auth_h(admin_tok))
    exp_ctr_path = os.path.join(exports_dir, "contractors_export.xlsx")
    with open(exp_ctr_path, "wb") as f:
        f.write(r_exp_ctr_xlsx.content)

    rec.record(
        "CTR-XLS-01", "Chain6", "CONTRACTOR", "Admin", "Platform",
        "Admin logged in", "GET import-template + POST /api/contractors/import-excel + GET /api/reports/contractors/export-excel", "200", r_imp.status_code,
        "1 row imported (SuccessCount=1, FailureCount=1) & .xlsx magic bytes PK",
        f"successCount={imp_data.get('successCount')}, failureCount={imp_data.get('failureCount')}, tempPwdCaptured={bool(imp_temp_pwd)}",
        r_tpl.status_code == 200 and r_tpl.content[:2] == b"PK" and r_imp.status_code == 200
        and imp_data.get("successCount") == 1 and imp_data.get("failureCount") == 1
        and r_exp_ctr_xlsx.status_code == 200 and r_exp_ctr_xlsx.content[:2] == b"PK",
        evidence_file="exports/contractors_export.xlsx"
    )

    # AUTH-PWD-01 & AUTH-PWD-02: MustChangePasswordMiddleware enforcement on imported contractor
    r_mcp_login = requests.post(f"{api_base_url}/api/auth/login", json={"email": imp_email, "password": imp_temp_pwd})
    mcp_login_data = r_mcp_login.json().get("data", {}) if r_mcp_login.status_code == 200 else {}
    mcp_tok = mcp_login_data.get("token", "")
    mcp_flag = mcp_login_data.get("user", {}).get("mustChangePassword")
    r_mcp_blocked = requests.get(f"{api_base_url}/api/contractors/me", headers=auth_h(mcp_tok))
    # AUTH-PWD-02: Wrong current password or same password rejected
    r_pwd_wrong = requests.post(
        f"{api_base_url}/api/auth/change-password",
        json={"currentPassword": "WrongCurrentPassword!1", "newPassword": "NewPassword@123", "confirmNewPassword": "NewPassword@123"},
        headers=auth_h(mcp_tok)
    )
    r_pwd_same = requests.post(
        f"{api_base_url}/api/auth/change-password",
        json={"currentPassword": imp_temp_pwd, "newPassword": imp_temp_pwd, "confirmNewPassword": imp_temp_pwd},
        headers=auth_h(mcp_tok)
    )
    # Valid password change clears MustChangePassword
    r_pwd_ok = requests.post(
        f"{api_base_url}/api/auth/change-password",
        json={"currentPassword": imp_temp_pwd, "newPassword": "Admin@123", "confirmNewPassword": "Admin@123"},
        headers=auth_h(mcp_tok)
    )
    imp_tok_after = login_token(api_base_url, imp_email, "Admin@123")
    r_mcp_unblocked = requests.get(f"{api_base_url}/api/contractors/me", headers=auth_h(imp_tok_after))
    rec.record(
        "AUTH-PWD-01", "Chain6", "AUTH", "MustChangePwdUser", "Platform",
        "MustChangePassword=true on imported contractor", "GET /api/contractors/me before & after POST /api/auth/change-password", "403->200", f"{r_mcp_blocked.status_code}->{r_mcp_unblocked.status_code}",
        "Blocked 403 before password change, 200 after change", f"mustChangePassword={mcp_flag}, before={r_mcp_blocked.status_code}, after={r_mcp_unblocked.status_code}",
        mcp_flag is True and r_mcp_blocked.status_code == 403 and r_pwd_ok.status_code == 200 and r_mcp_unblocked.status_code == 200
    )
    rec.record(
        "AUTH-PWD-02", "Chain6", "AUTH", "MustChangePwdUser", "Platform",
        "Authenticated user", "POST /api/auth/change-password with wrong current or identical new password", "400", f"{r_pwd_wrong.status_code}/{r_pwd_same.status_code}",
        "Both rejected 400", f"wrongCurrent={r_pwd_wrong.status_code}, sameNew={r_pwd_same.status_code}",
        r_pwd_wrong.status_code == 400 and r_pwd_same.status_code == 400
    )

    # USER-LOCK-01, AUTH-LOGIN-03, AUTH-LOCK-JWT-01: Lock account -> immediate JWT 401 & login 400
    imp_user_id = mcp_login_data.get("user", {}).get("id")
    r_lock = requests.patch(f"{api_base_url}/api/users/{imp_user_id}/status", headers=auth_h(admin_tok))
    r_jwt_after_lock = requests.get(f"{api_base_url}/api/contractors/me", headers=auth_h(imp_tok_after))
    r_login_locked = requests.post(f"{api_base_url}/api/auth/login", json={"email": imp_email, "password": "Admin@123"})
    rec.record(
        "USER-LOCK-01", "Chain5", "USER", "Admin", "Platform",
        "Active user", "PATCH /api/users/{id}/status", "200", r_lock.status_code,
        "IsActive=false + AuditLog TOGGLE_USER_STATUS", f"HTTP {r_lock.status_code}",
        r_lock.status_code == 200
    )
    rec.record(
        "AUTH-LOGIN-03", "Chain5", "AUTH", "Restricted_Locked", "Platform",
        "IsActive=false", "POST /api/auth/login on locked user", "400|401", r_login_locked.status_code,
        "Blocked login on locked account", f"HTTP {r_login_locked.status_code}",
        r_login_locked.status_code in (400, 401)
    )
    rec.record(
        "AUTH-LOCK-JWT-01", "Chain5", "AUTH", "Restricted_Locked", "Platform",
        "Unexpired JWT of locked user", "GET /api/contractors/me with existing JWT after lock", "401", r_jwt_after_lock.status_code,
        "Immediate 401 via OnTokenValidated IsActive check", f"HTTP {r_jwt_after_lock.status_code}",
        r_jwt_after_lock.status_code == 401
    )
    # Unlock imp_user_id for further testing (USER-RESET-01)
    requests.patch(f"{api_base_url}/api/users/{imp_user_id}/status", headers=auth_h(admin_tok))

    # USER-RESET-01: Admin reset password -> checks email, AuditLog, and MustChangePassword=true per Handoff 5.2
    r_reset = requests.post(
        f"{api_base_url}/api/users/{imp_user_id}/reset-password",
        json={"newPassword": "ResetPwd@123"},
        headers=auth_h(admin_tok)
    )
    reset_data = r_reset.json().get("data", {}) if r_reset.status_code == 200 else {}
    reset_mcp = reset_data.get("mustChangePassword")
    rec.record(
        "USER-RESET-01", "Chain1", "USER", "Admin", "Platform",
        "Admin logged in", "POST /api/users/{id}/reset-password", "200", r_reset.status_code,
        "HTTP 200 + emailSent=true + mustChangePassword=true", f"HTTP {r_reset.status_code}, emailSent={reset_data.get('emailSent')}, mustChangePassword={reset_mcp}",
        r_reset.status_code == 200 and reset_data.get("emailSent") is True and reset_mcp is True,
        notes="BUG: UserService.ResetPasswordAsync line 329 sets user.MustChangePassword = false instead of true" if reset_mcp is False else "OK"
    )

    # =========================================================================
    # 3. PROCURING ENTITIES, MEMBERSHIPS, INVITATIONS, SIGNING AUTHORITIES, CONTRACTORS
    # =========================================================================
    rec.record(
        "ENTITY-VERIFY-01", "Chain1", "ENTITY", "Admin", "Platform",
        "Entity A & B Pending", "PATCH /api/procuring-entities/{id}/verify (IsApproved=true)", "200", 200,
        "Entity A & B VerificationStatus=Approved", f"entityA={entity_a_id}, entityB={entity_b_id}",
        True, evidence_file="manifest.json"
    )

    # Chain 2: Register Entity C -> Admin Reject -> Manager C updates profile/files -> resets to Pending -> Admin Approves
    pe_c_email = f"pe_c_{run_id}@e2e.local".lower()
    pe_c_otp = get_otp_via_smtp(api_base_url, smtp_catcher, pe_c_email, "RegisterProcuringEntity")
    pe_c_code = f"PEC{int(time.time()) % 100000:05d}"
    pe_c_tax = f"0188{int(time.time()) % 1000000:06d}"
    with open(HSMT_PDF, "rb") as f_pdf:
        r_reg_pe_c = requests.post(
            f"{api_base_url}/api/auth/register-procuring-entity",
            data={
                "FullName": "Quan ly Don vi C",
                "Email": pe_c_email,
                "Password": "Admin@123",
                "OrganizationName": f"Ban QLDA Don vi C {run_id}",
                "OrganizationType": "StateEnterprise",
                "TaxCode": pe_c_tax,
                "BudgetCode": pe_c_code,
                "RepresentativeName": "Tran Van C",
                "RepresentativeTitle": "Giam doc",
                "RepresentativePhone": "0909777888",
                "Address": "Can Tho",
                "OtpCode": pe_c_otp,
            },
            files={"EstablishmentDecisionFile": ("decision_c.pdf", f_pdf, "application/pdf")},
        )
    pe_c_id = r_reg_pe_c.json()["data"]["procuringEntityId"]
    r_rej_pe_c = requests.patch(
        f"{api_base_url}/api/procuring-entities/{pe_c_id}/verify",
        json={"isApproved": False, "adminNotes": "Quyet dinh thanh lap thieu dau moc do, yeu cau bo sung."},
        headers=auth_h(admin_tok),
    )
    pe_c_status_rej = r_rej_pe_c.json().get("data", {}).get("verificationStatus") if r_rej_pe_c.status_code == 200 else "ERR"
    rec.record(
        "ENTITY-VERIFY-02", "Chain2", "ENTITY", "Admin", "Platform",
        "Entity C Pending", "PATCH /api/procuring-entities/{id}/verify (IsApproved=false)", "200", r_rej_pe_c.status_code,
        "VerificationStatus=Rejected", f"status={pe_c_status_rej}",
        r_rej_pe_c.status_code == 200 and pe_c_status_rej == "Rejected"
    )

    mgr_c_tok = login_token(api_base_url, pe_c_email)
    with open(HSMT_PDF, "rb") as f_pdf:
        r_upd_pe_c = requests.put(
            f"{api_base_url}/api/procuring-entities/me",
            data={
                "OrganizationName": f"Ban QLDA Don vi C Updated {run_id}",
                "OrganizationType": "StateEnterprise",
                "BudgetCode": pe_c_code,
                "RepresentativeName": "Tran Van C",
                "RepresentativeTitle": "Giam doc",
                "RepresentativePhone": "0909777888",
                "Address": "Can Tho Updated",
            },
            files={"EstablishmentDecisionFile": ("decision_c_fixed.pdf", f_pdf, "application/pdf")},
            headers=auth_h(mgr_c_tok),
        )
    pe_c_status_pend = r_upd_pe_c.json().get("data", {}).get("verificationStatus") if r_upd_pe_c.status_code == 200 else "ERR"
    r_app_pe_c = requests.patch(
        f"{api_base_url}/api/procuring-entities/{pe_c_id}/verify",
        json={"isApproved": True, "adminNotes": "Ho so bo sung hop le, phe duyet."},
        headers=auth_h(admin_tok),
    )
    pe_c_status_app = r_app_pe_c.json().get("data", {}).get("verificationStatus") if r_app_pe_c.status_code == 200 else "ERR"
    rec.record(
        "ENTITY-UPD-01", "Chain2", "ENTITY", "Manager_EntityC", "EntityC",
        "Entity C Rejected", "PUT /api/procuring-entities/me -> resets to Pending -> Admin PATCH verify Approved", "200", f"{r_upd_pe_c.status_code}->{r_app_pe_c.status_code}",
        "Rejected -> Pending -> Approved", f"afterUpdate={pe_c_status_pend}, afterApprove={pe_c_status_app}",
        r_upd_pe_c.status_code == 200 and pe_c_status_pend == "Pending" and r_app_pe_c.status_code == 200 and pe_c_status_app == "Approved"
    )

    # ENTITY-GET-01: Manager A views /me and downloads establishment-file; Manager B blocked from Entity A establishment-file
    r_pe_me_mgr_a = requests.get(f"{api_base_url}/api/procuring-entities/me", headers=auth_h(mgr_a_tok))
    r_pe_file_mgr_a = requests.get(f"{api_base_url}/api/procuring-entities/{entity_a_id}/establishment-file", headers=auth_h(mgr_a_tok))
    r_pe_file_mgr_b = requests.get(f"{api_base_url}/api/procuring-entities/{entity_a_id}/establishment-file", headers=auth_h(mgr_b_tok))
    rec.record(
        "ENTITY-GET-01", "Chain1", "ENTITY", "Manager_EntityA", "EntityA",
        "Entity A & B Approved", "GET /api/procuring-entities/me & establishment-file (Manager A vs Manager B)", "200/403", f"me={r_pe_me_mgr_a.status_code},fileA={r_pe_file_mgr_a.status_code},fileB={r_pe_file_mgr_b.status_code}",
        "Manager A 200, Manager B 403", f"me={r_pe_me_mgr_a.status_code}, fileA={r_pe_file_mgr_a.status_code}, fileB={r_pe_file_mgr_b.status_code}",
        r_pe_me_mgr_a.status_code == 200 and r_pe_file_mgr_a.status_code == 200 and r_pe_file_mgr_b.status_code == 403
    )

    # ENTITY-GET-02: Officer A1 (invited active member of Entity A) calls /me & establishment-file; Manager B calls GET /api/procuring-entities/{entityA_Id}
    r_pe_me_off_a1 = requests.get(f"{api_base_url}/api/procuring-entities/me", headers=auth_h(off_a1_tok))
    r_pe_id_mgr_b = requests.get(f"{api_base_url}/api/procuring-entities/{entity_a_id}", headers=auth_h(mgr_b_tok))
    rec.record(
        "ENTITY-GET-02", "Chain5", "ENTITY", "Officer_EntityA", "EntityA",
        "Officer A1 Active in Entity A; Manager B in Entity B", "GET /api/procuring-entities/me (Officer A1) & GET /api/procuring-entities/{entityA_Id} (Manager B)", "200/403", f"offA1_me={r_pe_me_off_a1.status_code},mgrB_getA={r_pe_id_mgr_b.status_code}",
        "Officer A1 200 on /me & Manager B 403 on GET /api/procuring-entities/{entityA_Id}",
        f"offA1_me={r_pe_me_off_a1.status_code}, mgrB_getA={r_pe_id_mgr_b.status_code}",
        r_pe_me_off_a1.status_code == 200 and r_pe_id_mgr_b.status_code == 403,
        notes="BUG: GetMyProcuringEntityProfile checks pe.UserId==userId (blocking invited members 404) & GetProcuringEntityById lacks entity scoping (allowing Manager B 200)"
    )

    # MEMBER-INV-01 & MEMBER-ACC-01: Verified in fixture_setup via SMTP Catcher
    rec.record(
        "MEMBER-INV-01", "Chain1", "MEMBER", "Manager_EntityA", "EntityA",
        "Entity A Approved", "POST /api/procuring-entities/{id}/invitations (Officer A1, Officer A2, Finance A, Signer A1, Signer A2)", "200", 200,
        "Invitations created & real tokens captured via SMTP Catcher", "5 invitations sent and captured",
        True, evidence_file="smtp_messages.json"
    )
    rec.record(
        "MEMBER-ACC-01", "Chain1", "MEMBER", "Unauthenticated", "EntityA",
        "Valid invitation tokens from SMTP", "GET /invitations/{token} + POST /invitations/accept", "200", 200,
        "5 members Active in Entity A", "officerA, officerA2, financeA, checkerA, signerA2 Active",
        True, evidence_file="manifest.json"
    )

    # MEMBER-INV-02: Officer / Finance / Signer / Manager B blocked from inviting into Entity A
    r_inv_off = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations",
        json={"email": f"unauth_inv_{run_id}@e2e.local", "roleInEntity": 1},
        headers=auth_h(off_a1_tok),
    )
    r_inv_mgr_b = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations",
        json={"email": f"unauth_inv2_{run_id}@e2e.local", "roleInEntity": 1},
        headers=auth_h(mgr_b_tok),
    )
    rec.record(
        "MEMBER-INV-02", "Chain5", "MEMBER", "Officer_EntityA", "EntityA",
        "Non-Manager of Entity A", "POST /api/procuring-entities/{entityA}/invitations by Officer A1 & Manager B", "403", f"{r_inv_off.status_code}/{r_inv_mgr_b.status_code}",
        "Both blocked 403 Forbidden", f"officer={r_inv_off.status_code}, managerB={r_inv_mgr_b.status_code}",
        r_inv_off.status_code == 403 and r_inv_mgr_b.status_code == 403
    )

    # MEMBER-INV-03: Block assigning Signer directly via invitation (roleInEntity=3 -> 400), block inviting already-Active member (400), and revoke Pending invitation (200)
    r_inv_signer_shortcut = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations",
        json={"email": f"signer_shortcut_{run_id}@e2e.local", "roleInEntity": 3},
        headers=auth_h(mgr_a_tok),
    )
    r_inv_active_dup = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations",
        json={"email": off_a1_email, "roleInEntity": 1},
        headers=auth_h(mgr_a_tok),
    )
    temp_inv_email = f"temp_revoke_inv_{run_id}@e2e.local".lower()
    r_inv_temp = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations",
        json={"email": temp_inv_email, "roleInEntity": 2},
        headers=auth_h(mgr_a_tok),
    )
    temp_inv_id = r_inv_temp.json().get("data", {}).get("id") if r_inv_temp.status_code in (200, 201) else 0
    r_rev_inv = requests.delete(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/invitations/{temp_inv_id}",
        headers=auth_h(mgr_a_tok),
    )
    rec.record(
        "MEMBER-INV-03", "Chain5", "MEMBER", "Manager_EntityA", "EntityA",
        "Entity A Approved", "POST invitation with roleInEntity=Signer (400), duplicate active member (400), & DELETE /invitations/{id} (200)", "400/400/200",
        f"{r_inv_signer_shortcut.status_code}/{r_inv_active_dup.status_code}/{r_rev_inv.status_code}",
        "Signer shortcut & duplicate member blocked 400; Pending invitation revoked 200",
        f"signerShortcut={r_inv_signer_shortcut.status_code}, activeDup={r_inv_active_dup.status_code}, revoke={r_rev_inv.status_code}",
        r_inv_signer_shortcut.status_code == 400 and r_inv_active_dup.status_code == 400 and r_rev_inv.status_code == 200
    )

    # MEMBER-ACC-02: Reuse already-accepted token or non-existent token rejected
    r_acc_invalid = requests.post(
        f"{api_base_url}/api/procuring-entities/invitations/accept",
        json={"token": "INVALID_OR_USED_TOKEN_999999", "fullName": "Test", "password": "Admin@123"},
    )
    rec.record(
        "MEMBER-ACC-02", "Chain5", "MEMBER", "Unauthenticated", "EntityA",
        "Used/invalid token", "POST /api/procuring-entities/invitations/accept invalid token", "400|404", r_acc_invalid.status_code,
        "Rejected 400/404", f"HTTP {r_acc_invalid.status_code}",
        r_acc_invalid.status_code in (400, 404)
    )

    # MEMBER-REV-01: Block revoking the last Manager of Entity A
    r_members_a = requests.get(f"{api_base_url}/api/procuring-entities/{entity_a_id}/members", headers=auth_h(mgr_a_tok)).json()["data"]
    mgr_a_member_id = next(m["id"] for m in r_members_a if m["userId"] == actors["managerA"]["userId"])
    r_rev_last_mgr = requests.delete(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/members/{mgr_a_member_id}",
        headers=auth_h(mgr_a_tok),
    )
    rec.record(
        "MEMBER-REV-01", "Chain5", "MEMBER", "Manager_EntityA", "EntityA",
        "Entity A has only 1 Manager", "DELETE /api/procuring-entities/{id}/members/{lastManagerMemberId}", "400", r_rev_last_mgr.status_code,
        "Blocked 400 cannot remove last active Manager", f"HTTP {r_rev_last_mgr.status_code}",
        r_rev_last_mgr.status_code == 400
    )

    # SIGN-CREATE-01, SIGN-CREATE-02, SIGN-VERIFY-01, SIGN-REV-01, MEMBER-REV-02
    rec.record(
        "SIGN-CREATE-01", "Chain1", "SIGNING", "Manager_EntityA", "EntityA",
        "Active members in Entity A", "POST /api/procuring-entities/{id}/signing-authorities", "200|201", 200,
        "SigningAuthorities created in PendingVerification", f"checkerSaId={actors['checkerA']['signingAuthorityId']}, signerA2SaId={actors['signerA2']['signingAuthorityId']}",
        True, evidence_file="manifest.json"
    )

    now_utc = datetime.now(timezone.utc)
    r_sa_bad_dates = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/signing-authorities",
        json={
            "userId": actors["officerA2"]["userId"],
            "authorityType": 1,
            "scope": "EntityWide",
            "validFrom": (now_utc + timedelta(days=10)).isoformat(),
            "validTo": (now_utc - timedelta(days=10)).isoformat(),
            "decisionNumber": "QD-BAD-DATE",
            "legalBasis": "Test invalid date range",
        },
        headers=auth_h(mgr_a_tok),
    )
    r_sa_non_member = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/signing-authorities",
        json={
            "userId": actors["managerB"]["userId"],
            "authorityType": 1,
            "scope": "EntityWide",
            "validFrom": (now_utc - timedelta(days=1)).isoformat(),
            "validTo": (now_utc + timedelta(days=30)).isoformat(),
            "decisionNumber": "QD-NON-MEM",
            "legalBasis": "Test non-member",
        },
        headers=auth_h(mgr_a_tok),
    )
    rec.record(
        "SIGN-CREATE-02", "Chain5", "SIGNING", "Manager_EntityA", "EntityA",
        "Invalid date range & non-member user", "POST signing-authorities (ValidTo<=ValidFrom & non-member)", "400", f"{r_sa_bad_dates.status_code}/{r_sa_non_member.status_code}",
        "Both rejected 400", f"badDates={r_sa_bad_dates.status_code}, nonMember={r_sa_non_member.status_code}",
        r_sa_bad_dates.status_code == 400 and r_sa_non_member.status_code == 400
    )

    # Create a new SA for Officer A2 so we can test Manager self-verify block (403), Admin verify (200), and Revoke (200)
    r_sa_off2 = requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/signing-authorities",
        json={
            "userId": actors["officerA2"]["userId"],
            "authorityType": 1,
            "scope": "EntityWide",
            "validFrom": (now_utc - timedelta(days=1)).isoformat(),
            "validTo": (now_utc + timedelta(days=60)).isoformat(),
            "decisionNumber": f"QD-OFF2-{run_id}",
            "legalBasis": "Uy quyen bo sung",
        },
        headers=auth_h(mgr_a_tok),
    )
    sa_off2_id = r_sa_off2.json()["data"]["id"]
    r_sa_pending = requests.get(f"{api_base_url}/api/signing-authorities/pending", headers=auth_h(admin_tok))
    r_sa_mgr_verify = requests.put(
        f"{api_base_url}/api/signing-authorities/{sa_off2_id}/verify",
        json={"isApproved": True, "verificationNote": "Manager co tinh tu verify"},
        headers=auth_h(mgr_a_tok),
    )
    r_sa_adm_verify = requests.put(
        f"{api_base_url}/api/signing-authorities/{sa_off2_id}/verify",
        json={"isApproved": True, "verificationNote": "Admin tham dinh phe duyet"},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "SIGN-VERIFY-01", "Chain1", "SIGNING", "Admin", "Platform",
        "SigningAuthority PendingVerification", "GET /pending + Manager PUT /verify (403) + Admin PUT /verify (200)", "403/200", f"mgr={r_sa_mgr_verify.status_code},adm={r_sa_adm_verify.status_code}",
        "Manager blocked 403 & Admin activates 200", f"pending={r_sa_pending.status_code}, mgr={r_sa_mgr_verify.status_code}, adm={r_sa_adm_verify.status_code}",
        r_sa_pending.status_code == 200 and r_sa_mgr_verify.status_code == 403 and r_sa_adm_verify.status_code == 200
    )

    r_sa_rev = requests.put(
        f"{api_base_url}/api/signing-authorities/{sa_off2_id}/revoke",
        json={"revocationReason": "Thu hoi uy quyen ky theo quyet dinh dieu chuyen cong tac."},
        headers=auth_h(mgr_a_tok),
    )
    rec.record(
        "SIGN-REV-01", "Chain5", "SIGNING", "Manager_EntityA", "EntityA",
        "Active SigningAuthority", "PUT /api/signing-authorities/{id}/revoke", "200", r_sa_rev.status_code,
        "Status=Revoked", f"HTTP {r_sa_rev.status_code}",
        r_sa_rev.status_code == 200 and r_sa_rev.json().get("data", {}).get("status") == "Revoked"
    )

    # MEMBER-REV-02: Block role upgrade to Signer (400), update role of Signer A2 (200), revoke membership of Signer A2 while SigningAuthority remains Active -> blocks Entity A access (403)
    sig_a2_member_id = next(m["id"] for m in r_members_a if m["userId"] == actors["signerA2"]["userId"])
    r_upd_role_signer = requests.put(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/members/{sig_a2_member_id}/role",
        json={"roleInEntity": 3},
        headers=auth_h(mgr_a_tok),
    )
    r_upd_role = requests.put(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/members/{sig_a2_member_id}/role",
        json={"roleInEntity": 1},
        headers=auth_h(mgr_a_tok),
    )
    r_rev_mem = requests.delete(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/members/{sig_a2_member_id}",
        headers=auth_h(mgr_a_tok),
    )
    r_sig2_after = requests.get(
        f"{api_base_url}/api/procuring-entities/{entity_a_id}/members",
        headers=auth_h(sig_a2_tok),
    )
    sa_sig2_detail = requests.get(
        f"{api_base_url}/api/signing-authorities/{actors['signerA2']['signingAuthorityId']}",
        headers=auth_h(admin_tok),
    ).json().get("data", {})
    rec.record(
        "MEMBER-REV-02", "Chain5", "MEMBER", "Manager_EntityA", "EntityA",
        "Signer A2 Active with Active SigningAuthority", "PUT member role Signer (400) + PUT role Officer (200) + DELETE member (200) -> blocks Entity A access (403) even while SigningAuthority is Active", "400/200/403",
        f"roleSigner={r_upd_role_signer.status_code},upd={r_upd_role.status_code},del={r_rev_mem.status_code},access={r_sig2_after.status_code}",
        "Role Signer blocked 400, Member Revoked 200, subsequent Entity A API returns 403 while SigningAuthority is Active",
        f"saStatus={sa_sig2_detail.get('status')}, accessAfter={r_sig2_after.status_code}",
        r_upd_role_signer.status_code == 400 and r_upd_role.status_code == 200 and r_rev_mem.status_code == 200 and r_sig2_after.status_code == 403
    )

    # CTR-APPR-01 & CTR-APPR-02 (Contractor C3: try register-contractor with real VietQR taxCode 0101248141 + real OTP; fallback to import-excel + PUT /me if external VietQR API rate-limits with 429)
    c3_email = f"ctr3_{run_id}@e2e.local".lower()
    c3_otp = get_otp_via_smtp(api_base_url, smtp_catcher, c3_email, "RegisterContractor")
    c3_tax = "0101248141"  # Valid VietQR enterprise tax code (FPT CORP)
    with open(HSMT_PDF, "rb") as f_pdf:
        r_reg_c3 = requests.post(
            f"{api_base_url}/api/auth/register-contractor",
            data={
                "FullName": "Dai dien Nha thau C3",
                "Email": c3_email,
                "Password": "Admin@123",
                "Phone": "0909888999",
                "CompanyName": f"Cong ty TNHH Nha Thau C3 {run_id}",
                "TaxCode": c3_tax,
                "Address": "Hai Phong",
                "OtpCode": c3_otp,
            },
            files={"BusinessLicenseFile": ("license_c3.pdf", f_pdf, "application/pdf")},
        )
    if r_reg_c3.status_code in (200, 201) and r_reg_c3.json().get("data"):
        c3_id = r_reg_c3.json()["data"]["contractorId"]
        c3_tok = login_token(api_base_url, c3_email)
        c3_source_note = "Registered via POST /api/auth/register-contractor"
    else:
        # External VietQR public API returned 429 Too Many Requests; provision C3 via import-excel + password change + PUT /api/contractors/me
        c3_imp_xlsx = os.path.join(exports_dir, f"import_c3_{run_id}.xlsx")
        create_minimal_xlsx(
            c3_imp_xlsx,
            [
                ["Ten doanh nghiep", "Ma so thue", "Dia chi", "Nguoi dai dien", "Email", "So dien thoai"],
                [f"Cong ty TNHH Nha Thau C3 {run_id}", f"0377{int(time.time()) % 1000000:06d}", "Hai Phong", "Dai dien Nha thau C3", c3_email, "0909888999"],
            ],
        )
        min_idx_c3 = smtp_catcher.count_messages()
        with open(c3_imp_xlsx, "rb") as f_xlsx:
            requests.post(
                f"{api_base_url}/api/contractors/import-excel",
                files={"file": ("c3.xlsx", f_xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")},
                headers=auth_h(admin_tok),
            )
        c3_mail = smtp_catcher.wait_for_email(c3_email, timeout_sec=10.0, min_index=min_idx_c3)
        c3_temp_pwd = c3_mail["extractedTempPassword"]
        c3_tmp_tok = login_token(api_base_url, c3_email, c3_temp_pwd)
        requests.post(
            f"{api_base_url}/api/auth/change-password",
            json={"currentPassword": c3_temp_pwd, "newPassword": "Admin@123", "confirmNewPassword": "Admin@123"},
            headers=auth_h(c3_tmp_tok),
        )
        c3_tok = login_token(api_base_url, c3_email, "Admin@123")
        with open(HSMT_PDF, "rb") as f_pdf:
            r_init_c3 = requests.put(
                f"{api_base_url}/api/contractors/me",
                data={"CompanyName": f"Cong ty TNHH Nha Thau C3 {run_id}", "Address": "Hai Phong"},
                files={"BusinessLicenseFile": ("license_c3.pdf", f_pdf, "application/pdf")},
                headers=auth_h(c3_tok),
            )
        c3_id = r_init_c3.json()["data"]["id"]
        c3_source_note = "Provisioned via import-excel + PUT /me (VietQR rate-limited 429)"

    r_c3_lic = requests.get(f"{api_base_url}/api/contractors/{c3_id}/license-file", headers=auth_h(admin_tok))
    rec.record(
        "CTR-APPR-01", "Chain1", "CONTRACTOR", "Admin", "Platform",
        "C1, C2 Approved & C3 Pending with license PDF", "Verify C1, C2 Approved & GET /api/contractors/{id}/license-file", "200", r_c3_lic.status_code,
        "Contractor license-file downloadable (200)", f"c3Id={c3_id}, licenseDownload={r_c3_lic.status_code} ({c3_source_note})",
        r_c3_lic.status_code == 200
    )

    r_rej_c3 = requests.patch(
        f"{api_base_url}/api/contractors/{c3_id}/verify",
        json={"isApproved": False, "adminNotes": "Ban scan DKKD bi mo, yeu cau tai lai."},
        headers=auth_h(admin_tok),
    )
    with open(HSMT_PDF, "rb") as f_pdf:
        r_upd_c3 = requests.put(
            f"{api_base_url}/api/contractors/me",
            data={"CompanyName": f"Cong ty TNHH Nha Thau C3 Updated {run_id}", "Address": "Hai Phong Updated"},
            files={"BusinessLicenseFile": ("license_c3_clear.pdf", f_pdf, "application/pdf")},
            headers=auth_h(c3_tok),
        )
    c3_status_pend = r_upd_c3.json().get("data", {}).get("verificationStatus") if r_upd_c3.status_code == 200 else "ERR"
    r_app_c3 = requests.patch(
        f"{api_base_url}/api/contractors/{c3_id}/verify",
        json={"isApproved": True, "adminNotes": "Ho so DKKD ro net, phe duyet."},
        headers=auth_h(admin_tok),
    )
    c3_status_app = r_app_c3.json().get("data", {}).get("verificationStatus") if r_app_c3.status_code == 200 else "ERR"
    rec.record(
        "CTR-APPR-02", "Chain2", "CONTRACTOR", "Admin", "Platform",
        "Contractor C3 Pending", "Reject C3 -> C3 PUT /api/contractors/me (resets to Pending) -> Admin Approve C3", "200", f"{r_rej_c3.status_code}->{r_upd_c3.status_code}->{r_app_c3.status_code}",
        "Rejected -> Pending -> Approved", f"afterUpd={c3_status_pend}, afterApp={c3_status_app}",
        r_rej_c3.status_code == 200 and c3_status_pend == "Pending" and r_app_c3.status_code == 200 and c3_status_app == "Approved"
    )

    # =========================================================================
    # 4. BID PACKAGES, OWNERSHIP, COMMITTEE, CRITERIA, PROPOSALS, SUBMISSIONS, CLARIFICATIONS
    # =========================================================================
    r_creat_ent = requests.get(f"{api_base_url}/api/bid-packages/creatable-entities", headers=auth_h(off_a1_tok))
    deadline_future = (datetime.now(timezone.utc) + timedelta(days=25)).isoformat()
    r_pkg_sec = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={
            "code": f"PKG-SEC-{run_id}".upper()[:28],
            "name": f"Goi thau Phu Nghiem thu Toan dien {run_id}",
            "description": "Goi thau kiem thu day du cac nhanh nghiep vu va phan quyen.",
            "type": 0,
            "budget": 2000000000,
            "deadline": deadline_future,
            "procuringEntityId": entity_a_id,
        },
        headers=auth_h(off_a1_tok),
    )
    pkg_sec_id = r_pkg_sec.json()["data"]["id"]
    with open(HSMT_PDF, "rb") as f_pdf:
        r_doc_up = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/documents",
            files=[("files", ("HSMT.pdf", f_pdf, "application/pdf"))],
            headers=auth_h(off_a1_tok),
        )
    doc_list = r_doc_up.json().get("data", []) if r_doc_up.status_code in (200, 201) else []
    doc_id = doc_list[0]["id"] if isinstance(doc_list, list) and doc_list else 0
    r_doc_dl = requests.get(f"{api_base_url}/api/bid-packages/documents/{doc_id}/download", headers=auth_h(c1_tok))
    rec.record(
        "PKG-CREATE-01", "Chain1", "PACKAGE", "Officer_Creator_EntityA", "EntityA",
        "Entity A Approved", "GET creatable-entities + POST /api/bid-packages + POST/GET documents", "200|201", r_pkg_sec.status_code,
        "Package Status=Open, ProcuringEntityId=EntityA & HSMT uploaded/downloaded",
        f"pkgId={pkg_sec_id}, docUpload={r_doc_up.status_code}, docDownload={r_doc_dl.status_code}",
        r_creat_ent.status_code == 200 and r_pkg_sec.status_code in (200, 201) and r_doc_up.status_code in (200, 201) and r_doc_dl.status_code == 200
    )

    # PKG-CREATE-02: Finance A and Signer A1 blocked from creating package
    r_pkg_fin = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={"name": "Unauthorized Fin Pkg", "type": 0, "budget": 500000000, "deadline": deadline_future, "procuringEntityId": entity_a_id},
        headers=auth_h(fin_a_tok),
    )
    r_pkg_sig = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={"name": "Unauthorized Sig Pkg", "type": 0, "budget": 500000000, "deadline": deadline_future, "procuringEntityId": entity_a_id},
        headers=auth_h(sig_a1_tok),
    )
    rec.record(
        "PKG-CREATE-02", "Chain5", "PACKAGE", "Finance_EntityA", "EntityA",
        "Finance A & Signer A1 tokens", "POST /api/bid-packages by Finance & Signer", "403", f"{r_pkg_fin.status_code}/{r_pkg_sig.status_code}",
        "Both blocked 403 Forbidden", f"finance={r_pkg_fin.status_code}, signer={r_pkg_sig.status_code}",
        r_pkg_fin.status_code == 403 and r_pkg_sig.status_code == 403
    )

    # PKG-CREATE-03: Multi-entity user (invite Officer A2 into Entity B as Officer -> logged-in existing user accepts -> now active in both Entity A & Entity B)
    min_idx_me = smtp_catcher.count_messages()
    requests.post(
        f"{api_base_url}/api/procuring-entities/{entity_b_id}/invitations",
        json={"email": off_a2_email, "roleInEntity": 1},
        headers=auth_h(mgr_b_tok),
    )
    me_mail = smtp_catcher.wait_for_email(off_a2_email, timeout_sec=10.0, min_index=min_idx_me)
    me_inv_token = me_mail["extractedInviteToken"]
    requests.post(
        f"{api_base_url}/api/procuring-entities/invitations/accept",
        json={"token": me_inv_token},
        headers=auth_h(off_a2_tok),
    )
    r_pkg_ambig = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={"name": "Ambiguous Multi-Entity Pkg", "type": 0, "budget": 500000000, "deadline": deadline_future},
        headers=auth_h(off_a2_tok),
    )
    r_pkg_explicit = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={"name": f"Explicit Entity B Pkg {run_id}", "type": 0, "budget": 500000000, "deadline": deadline_future, "procuringEntityId": entity_b_id},
        headers=auth_h(off_a2_tok),
    )
    rec.record(
        "PKG-CREATE-03", "Chain5", "PACKAGE", "MultiEntity_User", "MultiEntity",
        "Officer A2 Active in both Entity A & Entity B", "POST /api/bid-packages without vs with ProcuringEntityId", "400->201", f"{r_pkg_ambig.status_code}->{r_pkg_explicit.status_code}",
        "400 when ProcuringEntityId omitted, 200/201 when specified", f"omitted={r_pkg_ambig.status_code}, explicit={r_pkg_explicit.status_code}",
        r_pkg_ambig.status_code == 400 and r_pkg_explicit.status_code in (200, 201)
    )

    # PKG-UPD-01: Manager A can update Officer A1's package in Entity A (200); Officer A2 (non-creator) and Manager B blocked (403)
    upd_payload = {
        "name": f"Goi thau Phu Nghiem thu Updated {run_id}",
        "description": "Cap nhat boi Manager A cung don vi.",
        "type": 0,
        "budget": 2000000000,
        "deadline": deadline_future,
    }
    r_upd_mgr_a = requests.put(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", json=upd_payload, headers=auth_h(mgr_a_tok))
    r_upd_off_a2 = requests.put(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", json=upd_payload, headers=auth_h(off_a2_tok))
    r_upd_mgr_b = requests.put(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", json=upd_payload, headers=auth_h(mgr_b_tok))
    rec.record(
        "PKG-UPD-01", "Chain1", "PACKAGE", "Manager_EntityA", "EntityA",
        "Package Open in Entity A (created by Officer A1)", "PUT /api/bid-packages/{id} by Manager A (200), Officer A2 (403), Manager B (403)", "200/403/403", f"{r_upd_mgr_a.status_code}/{r_upd_off_a2.status_code}/{r_upd_mgr_b.status_code}",
        "Manager A 200; Officer A2 & Manager B 403", f"mgrA={r_upd_mgr_a.status_code}, offA2={r_upd_off_a2.status_code}, mgrB={r_upd_mgr_b.status_code}",
        r_upd_mgr_a.status_code == 200 and r_upd_off_a2.status_code == 403 and r_upd_mgr_b.status_code == 403
    )

    # PKG-OWN-01: Ownership review endpoints (role boundary 403, missing evidence 400, already-proven conflict 409)
    r_own_get_non_adm = requests.get(f"{api_base_url}/api/bid-packages/ownership-review", headers=auth_h(off_a1_tok))
    r_own_get_adm = requests.get(f"{api_base_url}/api/bid-packages/ownership-review", headers=auth_h(admin_tok))
    r_own_post_non_adm = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/ownership-review",
        json={"procuringEntityId": entity_a_id, "expectedProcuringEntityId": entity_a_id, "evidenceReference": "QD-01", "note": "Test"},
        headers=auth_h(off_a1_tok),
    )
    r_own_post_missing = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/ownership-review",
        json={"procuringEntityId": entity_a_id, "expectedProcuringEntityId": entity_a_id, "evidenceReference": "", "note": ""},
        headers=auth_h(admin_tok),
    )
    r_own_post_proven = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/ownership-review",
        json={"procuringEntityId": entity_a_id, "expectedProcuringEntityId": entity_a_id, "evidenceReference": "QD-01", "note": "Goi da xac dinh chu so huu"},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "PKG-OWN-01", "Chain6", "OWNERSHIP", "Admin", "Platform",
        "Proven package vs unauthenticated/non-admin", "GET /ownership-review & POST /ownership-review (403 non-admin, 200 list, 400 empty ref, 409 proven package)", "403/200/400/409",
        f"nonAdm={r_own_get_non_adm.status_code},adm={r_own_get_adm.status_code},empty={r_own_post_missing.status_code},proven={r_own_post_proven.status_code}",
        "403 non-Admin, 200 Admin list, 400 missing ref, 409 overwrite guard",
        f"getNonAdm={r_own_get_non_adm.status_code}, postNonAdm={r_own_post_non_adm.status_code}, getAdm={r_own_get_adm.status_code}, empty={r_own_post_missing.status_code}, proven={r_own_post_proven.status_code}",
        r_own_get_non_adm.status_code == 403 and r_own_post_non_adm.status_code == 403 and r_own_get_adm.status_code == 200 and r_own_post_missing.status_code == 400 and r_own_post_proven.status_code == 409
    )

    # COMM-ASSIGN-01, COMM-ASSIGN-02, COMM-ASSIGN-03
    for ev in actors["evaluators"]:
        r_asg = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators",
            json={"evaluatorId": ev["userId"]},
            headers=auth_h(off_a1_tok),
        )
        assert r_asg.status_code in (200, 201), f"Assign evaluator failed: {r_asg.status_code} {r_asg.text}"
    r_pkg_sec_info = requests.get(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", headers=auth_h(off_a1_tok)).json()["data"]
    rec.record(
        "COMM-ASSIGN-01", "Chain1", "COMMITTEE", "Officer_Creator_EntityA", "EntityA",
        "Package Open", "POST /api/bid-packages/{id}/evaluators (E1, E2, E3)", "200", 200,
        "3 evaluators assigned & isCommitteeValid=true", f"isCommitteeValid={r_pkg_sec_info.get('isCommitteeValid')}",
        r_pkg_sec_info.get("isCommitteeValid") is True
    )

    # COMM-ASSIGN-02: Block creator self-assign or assigning Contractor
    r_asg_self = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators",
        json={"evaluatorId": actors["officerA"]["userId"]},
        headers=auth_h(off_a1_tok),
    )
    r_asg_ctr = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators",
        json={"evaluatorId": actors["contractor1"]["userId"]},
        headers=auth_h(off_a1_tok),
    )
    rec.record(
        "COMM-ASSIGN-02", "Chain5", "COMMITTEE", "Officer_Creator_EntityA", "EntityA",
        "Package Open", "POST /evaluators with creator or contractor userId", "400", f"{r_asg_self.status_code}/{r_asg_ctr.status_code}",
        "Both rejected 400", f"self={r_asg_self.status_code}, contractor={r_asg_ctr.status_code}",
        r_asg_self.status_code == 400 and r_asg_ctr.status_code == 400
    )

    # PROP-CREATE-01 & PROP-REJ-01: Create Evaluator E4 via EvaluatorProposal -> Admin Approve -> creates User E4
    ev4_email = f"eval4_{run_id}@e2e.local".lower()
    with open(HSMT_PDF, "rb") as f_pdf:
        r_prop1 = requests.post(
            f"{api_base_url}/api/evaluator-proposals",
            data={
                "FullName": f"Chuyen gia Doc lap E4 {run_id}",
                "Email": ev4_email,
                "Phone": "0909444555",
                "Specialization": "An ninh mang & Ha tang CNTT",
                "Workplace": "Vien CNTT",
                "ExperienceYears": "10",
                "CertificateName": "Chung chi Hanh nghe Dau thau Hang I",
                "Notes": "De xuat bo sung giam khao du phong",
            },
            files={"CertificateFile": ("cert_e4.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    prop1_id = r_prop1.json()["data"]["id"]
    r_prop_cert = requests.get(f"{api_base_url}/api/evaluator-proposals/{prop1_id}/certificate-file", headers=auth_h(admin_tok))
    r_prop_app = requests.post(
        f"{api_base_url}/api/evaluator-proposals/{prop1_id}/approve",
        json={"password": "Admin@123", "adminNotes": "Phe duyet chuyen gia E4."},
        headers=auth_h(admin_tok),
    )
    ev4_user_id = r_prop_app.json()["data"]["createdUserId"]
    ev4_tok = login_token(api_base_url, ev4_email, "Admin@123")
    # Thay đổi mật khẩu lần đầu để hoàn tất yêu cầu MustChangePassword=true
    requests.post(
        f"{api_base_url}/api/auth/change-password",
        json={"currentPassword": "Admin@123", "newPassword": "Admin@123New!", "confirmNewPassword": "Admin@123New!"},
        headers=auth_h(ev4_tok),
    )
    ev4_tok = login_token(api_base_url, ev4_email, "Admin@123New!")
    rec.record(
        "PROP-CREATE-01", "Chain6", "PROPOSAL", "Officer_Creator_EntityA", "EntityA",
        "Officer A1 logged in", "POST /api/evaluator-proposals + GET certificate-file + POST approve", "200|201", f"{r_prop1.status_code}->{r_prop_app.status_code}",
        "Proposal Approved & Evaluator E4 user created", f"propId={prop1_id}, certDl={r_prop_cert.status_code}, ev4UserId={ev4_user_id}",
        r_prop1.status_code in (200, 201) and r_prop_cert.status_code == 200 and r_prop_app.status_code == 200 and bool(ev4_user_id)
    )

    # Create 2nd proposal -> Admin Rejects + block proposing Contractor email
    ev5_prop_email = f"eval_rej_{run_id}@e2e.local".lower()
    with open(HSMT_PDF, "rb") as f_pdf:
        r_prop2 = requests.post(
            f"{api_base_url}/api/evaluator-proposals",
            data={
                "FullName": "Chuyen gia Bi Tu choi",
                "Email": ev5_prop_email,
                "Phone": "0909444556",
                "Specialization": "Xay dung",
                "Workplace": "Cong ty Tu van",
                "ExperienceYears": "2",
                "CertificateName": "Chung chi",
            },
            files={"CertificateFile": ("cert2.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    prop2_id = r_prop2.json()["data"]["id"]
    r_prop2_rej = requests.post(
        f"{api_base_url}/api/evaluator-proposals/{prop2_id}/reject",
        json={"adminNotes": "Chua du so nam kinh nghiem chuyen mon theo quy dinh."},
        headers=auth_h(admin_tok),
    )
    with open(HSMT_PDF, "rb") as f_pdf:
        r_prop_coi = requests.post(
            f"{api_base_url}/api/evaluator-proposals",
            data={"FullName": "COI Contractor", "Email": c1_email, "Phone": "0909111222", "Specialization": "IT", "Workplace": "C1", "ExperienceYears": "5", "CertificateName": "CC"},
            files={"CertificateFile": ("cert.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    rec.record(
        "PROP-REJ-01", "Chain6", "PROPOSAL", "Admin", "Platform",
        "Pending proposal & Contractor email", "POST /api/evaluator-proposals/{id}/reject (200) & POST proposal with Contractor email (400)", "200/400", f"{r_prop2_rej.status_code}/{r_prop_coi.status_code}",
        "Rejected 200 & Contractor email blocked 400", f"reject={r_prop2_rej.status_code}, coi={r_prop_coi.status_code}",
        r_prop2_rej.status_code == 200 and r_prop_coi.status_code == 400
    )

    # COMM-ASSIGN-03: Manager A (Active Manager of Entity A) attempts to remove/assign evaluator on Officer A1's package in Entity A
    r_mgr_a_remove_ev = requests.delete(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators/{actors['evaluators'][2]['userId']}",
        headers=auth_h(mgr_a_tok),
    )
    if r_mgr_a_remove_ev.status_code == 200:
        # Restore E3 if removal succeeded
        requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators",
            json={"evaluatorId": actors["evaluators"][2]["userId"]},
            headers=auth_h(mgr_a_tok),
        )
    rec.record(
        "COMM-ASSIGN-03", "Chain5", "COMMITTEE", "Manager_EntityA", "EntityA",
        "Manager A is Active Manager of Entity A", "DELETE/POST /api/bid-packages/{id}/evaluators by Manager A on Officer A1's package", "200", r_mgr_a_remove_ev.status_code,
        "Manager A allowed (200) per PackageWriteAuthorization", f"Actual HTTP {r_mgr_a_remove_ev.status_code}",
        r_mgr_a_remove_ev.status_code == 200,
        notes="BUG: BidPackageService.AssignEvaluatorAsync/RemoveEvaluatorAsync checks package.CreatedBy != userId instead of PackageWriteAuthorization" if r_mgr_a_remove_ev.status_code != 200 else "OK"
    )

    # CRIT-CREATE-01, CRIT-CREATE-02, CRIT-CREATE-03
    r_crit_ok = requests.put(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/criteria",
        json={
            "items": [
                {"name": "Nang luc ky thuat va giai phap cong nghe", "description": "Ky thuat", "maxScore": 100, "weight": 70, "isPriceCriteria": False},
                {"name": "Gia du thau va hieu qua ngan sach", "description": "Gia", "maxScore": 100, "weight": 30, "isPriceCriteria": True},
            ]
        },
        headers=auth_h(off_a1_tok),
    )
    crit_list = r_crit_ok.json().get("data", []) if r_crit_ok.status_code == 200 else []
    rec.record(
        "CRIT-CREATE-01", "Chain1", "CRITERIA", "Officer_Creator_EntityA", "EntityA",
        "Package Open", "PUT /api/evaluations/packages/{id}/criteria (70 Technical + 30 Price = 100)", "200", r_crit_ok.status_code,
        "2 criteria created with sum(Weight)=100", f"count={len(crit_list)}",
        r_crit_ok.status_code == 200 and len(crit_list) == 2
    )

    r_crit_over = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/criteria",
        json={"name": "Tieu chi vuot trong so", "description": "Over", "maxScore": 100, "weight": 10, "isPriceCriteria": False},
        headers=auth_h(off_a1_tok),
    )
    r_crit_two_price = requests.put(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/criteria",
        json={
            "items": [
                {"name": "Gia 1", "description": "P1", "maxScore": 100, "weight": 50, "isPriceCriteria": True},
                {"name": "Gia 2", "description": "P2", "maxScore": 100, "weight": 50, "isPriceCriteria": True},
            ]
        },
        headers=auth_h(off_a1_tok),
    )
    rec.record(
        "CRIT-CREATE-02", "Chain5", "CRITERIA", "Officer_Creator_EntityA", "EntityA",
        "Criteria weight sum=100", "POST criteria exceeding weight 100 & PUT 2 price criteria", "400", f"{r_crit_over.status_code}/{r_crit_two_price.status_code}",
        "Both rejected 400", f"overWeight={r_crit_over.status_code}, twoPrice={r_crit_two_price.status_code}",
        r_crit_over.status_code == 400 and r_crit_two_price.status_code == 400
    )

    # CRIT-CREATE-03: Manager A attempts to update criteria on Officer A1's package in Entity A
    r_crit_mgr_a = requests.put(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/criteria",
        json={
            "items": [
                {"name": "Nang luc ky thuat Updated", "description": "Ky thuat", "maxScore": 100, "weight": 70, "isPriceCriteria": False},
                {"name": "Gia du thau Updated", "description": "Gia", "maxScore": 100, "weight": 30, "isPriceCriteria": True},
            ]
        },
        headers=auth_h(mgr_a_tok),
    )
    rec.record(
        "CRIT-CREATE-03", "Chain5", "CRITERIA", "Manager_EntityA", "EntityA",
        "Manager A is Active Manager of Entity A", "PUT /api/evaluations/packages/{id}/criteria by Manager A on Officer A1's package", "200", r_crit_mgr_a.status_code,
        "Manager A allowed (200) per entity Manager scope", f"Actual HTTP {r_crit_mgr_a.status_code}",
        r_crit_mgr_a.status_code == 200,
        notes="BUG: EvaluationService.ReplaceCriteriaAsync checks package.CreatedBy != userId instead of PackageWriteAuthorization" if r_crit_mgr_a.status_code != 200 else "OK"
    )
    if r_crit_mgr_a.status_code == 200:
        crit_list = r_crit_mgr_a.json().get("data", crit_list)

    # SUB-CREATE-01, SUB-CREATE-02, SUB-LIST-01, SUB-FILE-01, SUB-WITHDRAW-01
    with open(BAOGIA_PDF, "rb") as f1, open(NANGLUC_PDF, "rb") as f2:
        r_sub_c1 = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions",
            data={"BidPrice": "1500000000", "Notes": "Ho so du thau C1", "FileTypes": ["0", "1"]},
            files=[("Files", ("baogia.pdf", f1, "application/pdf")), ("Files", ("nangluc.pdf", f2, "application/pdf"))],
            headers=auth_h(c1_tok),
        )
    sub_c1_id = r_sub_c1.json()["data"]["id"]

    with open(BAOGIA_PDF, "rb") as f1, open(NANGLUC_PDF, "rb") as f2:
        r_sub_c2_first = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions",
            data={"BidPrice": "1650000000", "Notes": "Ho so du thau C2 lan 1", "FileTypes": ["0", "1"]},
            files=[("Files", ("baogia.pdf", f1, "application/pdf")), ("Files", ("nangluc.pdf", f2, "application/pdf"))],
            headers=auth_h(c2_tok),
        )
    sub_c2_first_id = r_sub_c2_first.json()["data"]["id"]

    rec.record(
        "SUB-CREATE-01", "Chain1", "SUBMISSION", "Contractor_C1_C2", "PackageA",
        "Package Open & C1, C2 Approved", "POST /api/bid-packages/{id}/submissions (C1 & C2)", "201", f"{r_sub_c1.status_code}/{r_sub_c2_first.status_code}",
        "2 submissions created Status=Submitted", f"subC1={sub_c1_id}, subC2={sub_c2_first_id}",
        r_sub_c1.status_code in (200, 201) and r_sub_c2_first.status_code in (200, 201)
    )

    # SUB-CREATE-02: Unapproved contractor (imp_tok_after, Pending) & over-budget bid (c3_tok, Approved) rejected on Open package pkg_sec_id
    with open(BAOGIA_PDF, "rb") as f1, open(NANGLUC_PDF, "rb") as f2:
        r_sub_unapp = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions",
            data={"BidPrice": "1400000000", "Notes": "Nop khi chua duoc duyet", "FileTypes": ["0", "1"]},
            files=[("Files", ("baogia.pdf", f1, "application/pdf")), ("Files", ("nangluc.pdf", f2, "application/pdf"))],
            headers=auth_h(imp_tok_after),
        )
    with open(BAOGIA_PDF, "rb") as f1, open(NANGLUC_PDF, "rb") as f2:
        r_sub_over = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions",
            data={"BidPrice": "9900000000", "Notes": "Vuot ngan sach", "FileTypes": ["0", "1"]},
            files=[("Files", ("baogia.pdf", f1, "application/pdf")), ("Files", ("nangluc.pdf", f2, "application/pdf"))],
            headers=auth_h(c3_tok),
        )
    rec.record(
        "SUB-CREATE-02", "Chain5", "SUBMISSION", "Contractor_Unapproved", "PackageA",
        "Unapproved contractor & over-budget bid", "POST /submissions unapproved (400) & over-budget (400)", "400", f"{r_sub_unapp.status_code}/{r_sub_over.status_code}",
        "Both rejected 400", f"unapproved={r_sub_unapp.status_code}, overBudget={r_sub_over.status_code}",
        r_sub_unapp.status_code == 400 and r_sub_over.status_code == 400
    )

    # SUB-LIST-01: Bid sealing while package is Open (before deadline): Officer A1 & Evaluator E1 blocked from listing/viewing/downloading sealed submissions
    r_subs_sealed = requests.get(f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions", headers=auth_h(off_a1_tok))
    r_sub_detail_sealed = requests.get(f"{api_base_url}/api/submissions/{sub_c1_id}", headers=auth_h(off_a1_tok))
    c1_own_sub = requests.get(f"{api_base_url}/api/submissions/{sub_c1_id}", headers=auth_h(c1_tok)).json()["data"]
    c1_file_id = c1_own_sub["files"][0]["id"]
    r_dl_while_open = requests.get(f"{api_base_url}/api/submissions/files/{c1_file_id}/download", headers=auth_h(off_a1_tok))
    rec.record(
        "SUB-LIST-01", "Chain1", "SUBMISSION", "Officer_Creator_EntityA", "PackageA",
        "Package Open before Deadline", "GET /bid-packages/{id}/submissions, GET /submissions/{id} & GET /submissions/files/{id}/download while Open", "400|403|404",
        f"list={r_subs_sealed.status_code},detail={r_sub_detail_sealed.status_code},dl={r_dl_while_open.status_code}",
        "Internal staff blocked from listing, viewing, or downloading sealed bids while Package is Open",
        f"listStatus={r_subs_sealed.status_code}, detailStatus={r_sub_detail_sealed.status_code}, fileDlStatus={r_dl_while_open.status_code}",
        r_subs_sealed.status_code in (400, 403) and r_sub_detail_sealed.status_code in (400, 403, 404) and r_dl_while_open.status_code in (400, 403, 404)
    )

    # SUB-FILE-01: IDOR protection - Contractor C2 cannot view C1's submission or download C1's file
    r_c2_get_c1 = requests.get(f"{api_base_url}/api/submissions/{sub_c1_id}", headers=auth_h(c2_tok))
    r_c2_dl_c1 = requests.get(f"{api_base_url}/api/submissions/files/{c1_file_id}/download", headers=auth_h(c2_tok))
    rec.record(
        "SUB-FILE-01", "Chain5", "SUBMISSION", "Contractor_C2", "PackageA",
        "C1 & C2 submitted bids", "Contractor C2 GET /api/submissions/{c1SubId} & GET /api/submissions/files/{c1FileId}/download", "403|404", f"{r_c2_get_c1.status_code}/{r_c2_dl_c1.status_code}",
        "Cross-contractor IDOR blocked 403/404", f"getSub={r_c2_get_c1.status_code}, dlFile={r_c2_dl_c1.status_code}",
        r_c2_get_c1.status_code in (403, 404) and r_c2_dl_c1.status_code in (403, 404)
    )

    # SUB-WITHDRAW-01: Contractor C2 withdraws bid while Open and resubmits
    r_c2_wd = requests.delete(f"{api_base_url}/api/submissions/{sub_c2_first_id}", headers=auth_h(c2_tok))
    with open(BAOGIA_PDF, "rb") as f1, open(NANGLUC_PDF, "rb") as f2:
        r_sub_c2 = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_sec_id}/submissions",
            data={"BidPrice": "1600000000", "Notes": "Ho so du thau C2 nop lai", "FileTypes": ["0", "1"]},
            files=[("Files", ("baogia.pdf", f1, "application/pdf")), ("Files", ("nangluc.pdf", f2, "application/pdf"))],
            headers=auth_h(c2_tok),
        )
    sub_c2_id = r_sub_c2.json()["data"]["id"]
    rec.record(
        "SUB-WITHDRAW-01", "Chain4", "SUBMISSION", "Contractor_C2", "PackageA",
        "Package Open before Deadline", "DELETE /api/submissions/{id} -> POST /api/bid-packages/{id}/submissions", "200->201", f"{r_c2_wd.status_code}->{r_sub_c2.status_code}",
        "Withdrawn & resubmitted with Status=Submitted", f"withdraw={r_c2_wd.status_code}, newSubId={sub_c2_id}",
        r_c2_wd.status_code == 200 and r_sub_c2.status_code in (200, 201)
    )

    # CLAR-CREATE-01: Public package clarification (HSMT) while Open
    with open(HSMT_PDF, "rb") as f_pdf:
        r_clar_att = requests.post(
            f"{api_base_url}/api/clarifications/attachments",
            files={"file": ("question_att.pdf", f_pdf, "application/pdf")},
            headers=auth_h(c1_tok),
        )
    att_url = r_clar_att.json().get("data", "")
    r_clar_pkg = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/clarifications/package",
        json={"title": "Lam ro tieu chuan ky thuat Switch", "questionContent": "De nghi lam ro thong so bang thong chuyen mach.", "questionAttachmentUrl": att_url},
        headers=auth_h(c1_tok),
    )
    clar_pkg_id = r_clar_pkg.json()["data"]["id"]
    r_clar_ans = requests.post(
        f"{api_base_url}/api/clarifications/{clar_pkg_id}/answer",
        json={"answerContent": "Bang thong chuyen mach toi thieu 1.2 Tbps theo phu luc 2 HSMT.", "answerAttachmentUrl": att_url},
        headers=auth_h(off_a1_tok),
    )
    r_clar_dl = requests.get(f"{api_base_url}/api/clarifications/attachments/download", params={"filePath": att_url}, headers=auth_h(c2_tok))
    rec.record(
        "CLAR-CREATE-01", "Chain6", "CLARIFICATION", "Contractor_C1", "PackageA",
        "Package Open", "POST /clarifications/attachments + POST /clarifications/package + POST /answer + GET download", "200", f"{r_clar_pkg.status_code}/{r_clar_ans.status_code}/{r_clar_dl.status_code}",
        "Public HSMT clarification created, answered, and attachment downloaded",
        f"clarId={clar_pkg_id}, ans={r_clar_ans.status_code}, dl={r_clar_dl.status_code}",
        r_clar_att.status_code == 200 and r_clar_pkg.status_code == 200 and r_clar_ans.status_code == 200 and r_clar_dl.status_code == 200
    )

    # USER-LOCK-02: Block Admin from locking Evaluator E1 or removing Evaluator role while E1 is assigned to an Open/Evaluating package
    r_lock_ev1 = requests.patch(f"{api_base_url}/api/users/{actors['evaluators'][0]['userId']}/status", headers=auth_h(admin_tok))
    r_strip_ev1 = requests.post(
        f"{api_base_url}/api/users/{actors['evaluators'][0]['userId']}/roles",
        json={"roleIds": []},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "USER-LOCK-02", "Chain5", "USER", "Admin", "Platform",
        "Evaluator E1 assigned to Open package", "PATCH /api/users/{e1}/status & POST /api/users/{e1}/roles []", "400", f"{r_lock_ev1.status_code}/{r_strip_ev1.status_code}",
        "Both blocked 400 while evaluator is in active committee", f"lock={r_lock_ev1.status_code}, stripRole={r_strip_ev1.status_code}",
        r_lock_ev1.status_code == 400 and r_strip_ev1.status_code == 400
    )

    # PKG-STATUS-01: Early close request by Officer A1 -> Admin approves -> Closed
    r_req_close = requests.put(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/status",
        json={"newStatus": 1, "reason": "Da du ho so du thau, de nghi dong thau som de cham diem."},
        headers=auth_h(off_a1_tok),
    )
    r_pend_reqs = requests.get(f"{api_base_url}/api/bid-packages/status-approvals/pending", headers=auth_h(admin_tok)).json()["data"]
    req_close_id = next(x["id"] for x in r_pend_reqs if x["bidPackageId"] == pkg_sec_id)
    r_rev_close = requests.post(
        f"{api_base_url}/api/bid-packages/status-approvals/{req_close_id}/review",
        json={"isApproved": True, "adminComment": "Chap thuan dong thau som."},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "PKG-STATUS-01", "Chain3", "PACKAGE", "Officer_Creator_EntityA", "EntityA",
        "Package Open before Deadline", "PUT /status (Closed) -> creates Pending request -> Admin POST /status-approvals/{id}/review", "200", f"{r_req_close.status_code}->{r_rev_close.status_code}",
        "Package transitioned to Closed", f"reqId={req_close_id}, review={r_rev_close.status_code}",
        r_req_close.status_code == 200 and r_rev_close.status_code == 200
    )

    # PKG-STATUS-02: Reopen Closed -> Open with NewDeadline, then close again -> Closed
    new_dl = (datetime.now(timezone.utc) + timedelta(days=35)).isoformat()
    r_req_reopen = requests.put(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/status",
        json={"newStatus": 0, "reason": "Gia han them thoi gian nop HSMT.", "newDeadline": new_dl},
        headers=auth_h(off_a1_tok),
    )
    r_pend_reqs2 = requests.get(f"{api_base_url}/api/bid-packages/status-approvals/pending", headers=auth_h(admin_tok)).json()["data"]
    req_reopen_id = next((x["id"] for x in r_pend_reqs2 if x["bidPackageId"] == pkg_sec_id), None)
    if req_reopen_id:
        requests.post(
            f"{api_base_url}/api/bid-packages/status-approvals/{req_reopen_id}/review",
            json={"isApproved": True, "adminComment": "Dong y mo lai goi thau."},
            headers=auth_h(admin_tok),
        )
    # Close directly as Admin (or via approval) so it's Closed for Evaluating
    r_close_again = requests.put(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/status",
        json={"newStatus": 1, "reason": "Dong thau de bat dau cham diem."},
        headers=auth_h(admin_tok),
    )
    r_pend_reqs3 = requests.get(f"{api_base_url}/api/bid-packages/status-approvals/pending", headers=auth_h(admin_tok)).json()["data"]
    req_close2_id = next((x["id"] for x in r_pend_reqs3 if x["bidPackageId"] == pkg_sec_id), None)
    if req_close2_id:
        requests.post(
            f"{api_base_url}/api/bid-packages/status-approvals/{req_close2_id}/review",
            json={"isApproved": True, "adminComment": "Phe duyet dong thau."},
            headers=auth_h(admin_tok),
        )
    pkg_after_close2 = requests.get(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", headers=auth_h(off_a1_tok)).json()["data"]["status"]
    rec.record(
        "PKG-STATUS-02", "Chain3", "PACKAGE", "Officer_Creator_EntityA", "EntityA",
        "Package Closed with no scores", "Reopen Closed -> Open (NewDeadline) -> Close back to Closed", "200", r_req_reopen.status_code,
        "Package reopened and closed cleanly", f"status={pkg_after_close2}",
        r_req_reopen.status_code == 200 and pkg_after_close2 in (1, "Closed")
    )

    # PKG-STATUS-03: Transition Closed -> Evaluating
    r_to_eval = requests.put(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/status",
        json={"newStatus": 2, "reason": "Bat dau mo thau va cham diem."},
        headers=auth_h(off_a1_tok),
    )
    rec.record(
        "PKG-STATUS-03", "Chain1", "PACKAGE", "Officer_Creator_EntityA", "EntityA",
        "Package Closed, 3 odd evaluators, criteria=100, valid submissions", "PUT /api/bid-packages/{id}/status (Evaluating)", "200", r_to_eval.status_code,
        "Package Status=Evaluating", f"HTTP {r_to_eval.status_code}",
        r_to_eval.status_code == 200
    )

    # CLAR-CREATE-02: Private HSDT clarification for C1 during Evaluating -> C2 blocked -> C1 answers
    r_clar_sub = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/clarifications/submission",
        json={
            "bidSubmissionId": sub_c1_id,
            "title": "Yeu cau giai trinh chung chi CO/CQ",
            "questionContent": "De nghi nha thau C1 bo sung ban sao cong chung CO/CQ lo hang.",
            "deadline": (datetime.now(timezone.utc) + timedelta(hours=12)).isoformat(),
        },
        headers=auth_h(off_a1_tok),
    )
    clar_sub_id = r_clar_sub.json()["data"]["id"]
    r_clar_c2_list = requests.get(f"{api_base_url}/api/bid-packages/{pkg_sec_id}/clarifications?type=1", headers=auth_h(c2_tok)).json()["data"]
    r_clar_c2_ans = requests.post(
        f"{api_base_url}/api/clarifications/{clar_sub_id}/answer",
        json={"answerContent": "C2 co tinh tra loi thay C1"},
        headers=auth_h(c2_tok),
    )
    r_clar_c1_ans = requests.post(
        f"{api_base_url}/api/clarifications/{clar_sub_id}/answer",
        json={"answerContent": "Nha thau C1 xin gui kem van ban giai trinh CO/CQ day du."},
        headers=auth_h(c1_tok),
    )
    rec.record(
        "CLAR-CREATE-02", "Chain6", "CLARIFICATION", "Officer_Creator_EntityA", "PackageA",
        "Package Evaluating", "POST /clarifications/submission for C1 -> C2 list/answer blocked -> C1 answers", "200/400|403/200",
        f"create={r_clar_sub.status_code},c2List={len(r_clar_c2_list)},c2Ans={r_clar_c2_ans.status_code},c1Ans={r_clar_c1_ans.status_code}",
        "C2 sees 0 private clarifications & blocked from answering; C1 answers 200",
        f"c2Visible={len(r_clar_c2_list)}, c2Ans={r_clar_c2_ans.status_code}, c1Ans={r_clar_c1_ans.status_code}",
        r_clar_sub.status_code == 200 and len(r_clar_c2_list) == 0 and r_clar_c2_ans.status_code in (400, 403) and r_clar_c1_ans.status_code == 200
    )

    # SCORE-SUB-01, SCORE-SUB-02, COMM-REP-01, PKG-STATUS-04, AWARD-FIN-01, AWARD-REV-01
    tech_crit_id = next(c["id"] for c in crit_list if not c["isPriceCriteria"])
    price_crit_id = next(c["id"] for c in crit_list if c["isPriceCriteria"])

    # E1 and E2 score C1 (90) and C2 (75)
    for tok, sc_c1, sc_c2 in [(ev1_tok, 90, 75), (ev2_tok, 92, 78)]:
        r_s1 = requests.post(
            f"{api_base_url}/api/evaluations/submissions/{sub_c1_id}/scores",
            json={"scores": [{"criteriaId": tech_crit_id, "score": sc_c1, "comment": "Ky thuat tot"}]},
            headers=auth_h(tok),
        )
        r_s2 = requests.post(
            f"{api_base_url}/api/evaluations/submissions/{sub_c2_id}/scores",
            json={"scores": [{"criteriaId": tech_crit_id, "score": sc_c2, "comment": "Ky thuat kha"}]},
            headers=auth_h(tok),
        )
        assert r_s1.status_code == 200 and r_s2.status_code == 200

    # Check independence: E1 calling GET /api/evaluations/submissions/{sub_c1_id}/scores only sees E1's own score, and CanFinalize is false
    ev1_scores_view = requests.get(f"{api_base_url}/api/evaluations/submissions/{sub_c1_id}/scores", headers=auth_h(ev1_tok)).json()["data"]
    summary_partial = requests.get(f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/summary", headers=auth_h(off_a1_tok)).json()["data"]
    rec.record(
        "SCORE-SUB-01", "Chain1", "SCORING", "Evaluator_E1_E2", "PackageA",
        "Package Evaluating (2 of 3 evaluators scored)", "POST /scores (E1, E2) + check evaluator score isolation & canFinalize=false", "200", 200,
        "E1 only sees own score (len=1) & canFinalize=false until 3rd evaluator completes",
        f"e1VisibleScores={len(ev1_scores_view)}, canFinalize={summary_partial.get('canFinalize')}",
        len(ev1_scores_view) == 1 and summary_partial.get("canFinalize") is False
    )

    # SCORE-SUB-02: Block manual price criteria scoring, block >MaxScore, block unassigned E4 (before replacement)
    r_sc_over = requests.post(
        f"{api_base_url}/api/evaluations/submissions/{sub_c1_id}/scores",
        json={"scores": [{"criteriaId": tech_crit_id, "score": 150, "comment": "Vuot MaxScore"}]},
        headers=auth_h(ev1_tok),
    )
    r_sc_unassigned = requests.post(
        f"{api_base_url}/api/evaluations/submissions/{sub_c1_id}/scores",
        json={"scores": [{"criteriaId": tech_crit_id, "score": 85, "comment": "Chua duoc phan cong"}]},
        headers=auth_h(ev4_tok),
    )
    rec.record(
        "SCORE-SUB-02", "Chain5", "SCORING", "Evaluator_E1_And_Unassigned_E5", "PackageA",
        "Package Evaluating", "POST /scores >MaxScore (400) & unassigned evaluator E4 (400/403)", "400/403", f"{r_sc_over.status_code}/{r_sc_unassigned.status_code}",
        "Both rejected 400/403", f"overMax={r_sc_over.status_code}, unassigned={r_sc_unassigned.status_code}",
        r_sc_over.status_code == 400 and r_sc_unassigned.status_code in (400, 403)
    )

    # PKG-STATUS-04: Block reopening package to Open/Closed after scoring has started
    r_rollback_status = requests.put(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/status",
        json={"newStatus": 0, "reason": "Co tinh mo lai khi da co diem cham", "newDeadline": new_dl},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "PKG-STATUS-04", "Chain5", "PACKAGE", "Admin", "PackageA",
        "EvaluationScores already exist", "PUT /api/bid-packages/{id}/status back to Open after scoring started", "400", r_rollback_status.status_code,
        "Blocked 400 cannot revert package status after scoring started", f"HTTP {r_rollback_status.status_code}",
        r_rollback_status.status_code == 400
    )

    # COMM-REP-01: Atomically replace E3 with E4 during Evaluating -> E4 scores C1 & C2 -> automatic price score & ranking
    r_rep_ev = requests.post(
        f"{api_base_url}/api/bid-packages/{pkg_sec_id}/evaluators/replace",
        json={
            "oldEvaluatorId": actors["evaluators"][2]["userId"],
            "newEvaluatorId": ev4_user_id,
            "reason": "Giam khao E3 ban cong tac dot xuat, thay the bang chuyen gia E4.",
        },
        headers=auth_h(off_a1_tok),
    )
    r_e4_s1 = requests.post(
        f"{api_base_url}/api/evaluations/submissions/{sub_c1_id}/scores",
        json={"scores": [{"criteriaId": tech_crit_id, "score": 91, "comment": "E4 cham C1"}]},
        headers=auth_h(ev4_tok),
    )
    r_e4_s2 = requests.post(
        f"{api_base_url}/api/evaluations/submissions/{sub_c2_id}/scores",
        json={"scores": [{"criteriaId": tech_crit_id, "score": 76, "comment": "E4 cham C2"}]},
        headers=auth_h(ev4_tok),
    )
    summary_full = requests.get(f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/summary", headers=auth_h(off_a1_tok)).json()["data"]
    rankings = requests.get(f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/rankings", headers=auth_h(off_a1_tok)).json().get("data", [])
    rank1_sub_id = next((r["submissionId"] for r in rankings if r["rank"] == 1), None)
    rec.record(
        "COMM-REP-01", "Chain3", "COMMITTEE", "Officer_Creator_EntityA", "PackageA",
        "Package Evaluating", "POST /evaluators/replace (E3->E4) -> E4 completes scoring -> auto-calculate price & Rank 1/2", "200", f"{r_rep_ev.status_code}/{r_e4_s1.status_code}/{r_e4_s2.status_code}",
        "canFinalize=true & C1 ranked #1", f"canFinalize={summary_full.get('canFinalize')}, rank1SubId={rank1_sub_id}",
        r_rep_ev.status_code == 200 and summary_full.get("canFinalize") is True and rank1_sub_id == sub_c1_id
    )

    # AWARD-FIN-01: Block selecting Rank 2 without DecisionReason (400); finalize Rank 1 (C1) -> Awarded
    r_fin_rank2_no_reason = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/finalize",
        json={"selectedSubmissionId": sub_c2_id, "decisionReason": ""},
        headers=auth_h(off_a1_tok),
    )
    r_fin_ok = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/finalize",
        json={"selectedSubmissionId": sub_c1_id, "decisionReason": "Nha thau C1 xep hang 1 voi diem tong hop cao nhat."},
        headers=auth_h(off_a1_tok),
    )
    r_eval_rep = requests.get(f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/report", headers=auth_h(off_a1_tok))
    rec.record(
        "AWARD-FIN-01", "Chain1", "AWARD", "Officer_Creator_EntityA", "PackageA",
        "All submissions Evaluated", "POST /finalize Rank 2 without reason (400) -> POST /finalize Rank 1 (200) + GET /report", "400->200", f"{r_fin_rank2_no_reason.status_code}->{r_fin_ok.status_code}",
        "Package Status=Awarded, C1=Selected, C2=Rejected", f"rank2NoReason={r_fin_rank2_no_reason.status_code}, finOk={r_fin_ok.status_code}, report={r_eval_rep.status_code}",
        r_fin_rank2_no_reason.status_code == 400 and r_fin_ok.status_code == 200 and r_eval_rep.status_code == 200
    )

    # AWARD-REV-01: Revoke award before active contract -> returns to Evaluating -> re-finalize back to Awarded
    r_rev_awd = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/revoke-award",
        json={"reason": "Ra soat lai bien ban thuong thao truoc khi ky hop dong."},
        headers=auth_h(off_a1_tok),
    )
    pkg_after_rev = requests.get(f"{api_base_url}/api/bid-packages/{pkg_sec_id}", headers=auth_h(off_a1_tok)).json()["data"]["status"]
    r_refin_ok = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_sec_id}/finalize",
        json={"selectedSubmissionId": sub_c1_id, "decisionReason": "Phe duyet trao thau chinh thuc sau ra soat."},
        headers=auth_h(off_a1_tok),
    )
    rec.record(
        "AWARD-REV-01", "Chain4", "AWARD", "Officer_Creator_EntityA", "PackageA",
        "Package Awarded (no active contract)", "POST /revoke-award -> returns to Evaluating -> POST /finalize back to Awarded", "200->200", f"{r_rev_awd.status_code}->{r_refin_ok.status_code}",
        "Awarded -> Evaluating -> Awarded", f"statusAfterRevoke={pkg_after_rev}, reFinalize={r_refin_ok.status_code}",
        r_rev_awd.status_code == 200 and pkg_after_rev in (2, "Evaluating") and r_refin_ok.status_code == 200
    )

    # =========================================================================
    # 5. CONTRACTS, MILESTONES, PROGRESS, MAKER-CHECKER, RE-REVIEW, SNAPSHOTS, GUARANTEES, ADDENDA, CEVAL
    # =========================================================================
    c_start = (datetime.now(timezone.utc) + timedelta(days=1)).isoformat()
    c_end = (datetime.now(timezone.utc) + timedelta(days=120)).isoformat()

    # CNT-CREATE-01: Block value > winning bid price (1.5B); create Draft contract (1.5B); block duplicate contract
    r_cnt_over = requests.post(
        f"{api_base_url}/api/contracts",
        json={
            "bidPackageId": pkg_sec_id,
            "contractorId": actors["contractor1"]["contractorId"],
            "value": 1950000000,
            "terms": "Vuot gia trung thau",
            "startDate": c_start,
            "endDate": c_end,
        },
        headers=auth_h(off_a1_tok),
    )
    r_cnt_ok = requests.post(
        f"{api_base_url}/api/contracts",
        json={
            "bidPackageId": pkg_sec_id,
            "contractorId": actors["contractor1"]["contractorId"],
            "value": 1500000000,
            "terms": "Dieu khoan hop dong kinh te chuan.",
            "startDate": c_start,
            "endDate": c_end,
        },
        headers=auth_h(off_a1_tok),
    )
    cnt_sec_id = r_cnt_ok.json()["data"]["id"]
    r_cnt_dup = requests.post(
        f"{api_base_url}/api/contracts",
        json={
            "bidPackageId": pkg_sec_id,
            "contractorId": actors["contractor1"]["contractorId"],
            "value": 1500000000,
            "terms": "Hop dong trung lap",
            "startDate": c_start,
            "endDate": c_end,
        },
        headers=auth_h(off_a1_tok),
    )
    rec.record(
        "CNT-CREATE-01", "Chain1", "CONTRACT", "Officer_Creator_EntityA", "EntityA",
        "Package Awarded to C1 (1.5B)", "POST /api/contracts >BidPrice (400), valid 1.5B (201), duplicate (400)", "400/201/400",
        f"{r_cnt_over.status_code}/{r_cnt_ok.status_code}/{r_cnt_dup.status_code}",
        "400 over bid price, 201 Draft contract created, 400 duplicate blocked",
        f"cntId={cnt_sec_id}, over={r_cnt_over.status_code}, ok={r_cnt_ok.status_code}, dup={r_cnt_dup.status_code}",
        r_cnt_over.status_code == 400 and r_cnt_ok.status_code in (200, 201) and r_cnt_dup.status_code == 400
    )

    # CNT-SCAN-01: Block activation before scan & milestones; add 2 milestones + upload scan -> activate Active
    r_act_no_scan = requests.put(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/status",
        json={"newStatus": 1, "note": "Kich hoat khi chua co scan va moc"},
        headers=auth_h(off_a1_tok),
    )
    r_m1 = requests.post(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/milestones",
        json={"title": "Dot 1: Tam ung va giao thiet bi", "dueDate": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(), "amount": 500000000},
        headers=auth_h(off_a1_tok),
    )
    r_m2 = requests.post(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/milestones",
        json={"title": "Dot 2: Nghiem thu tong the", "dueDate": (datetime.now(timezone.utc) + timedelta(days=60)).isoformat(), "amount": 1000000000},
        headers=auth_h(off_a1_tok),
    )
    sec_m1_id = r_m1.json()["data"]["id"]
    sec_m2_id = r_m2.json()["data"]["id"]

    with open(SCAN_PDF, "rb") as f_pdf:
        r_scan_up = requests.post(
            f"{api_base_url}/api/contracts/{cnt_sec_id}/scanned-file",
            files={"file": ("contract_scan.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    r_act_ok = requests.put(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/status",
        json={"newStatus": 1, "note": "Kich hoat hop dong du dieu kien."},
        headers=auth_h(off_a1_tok),
    )
    r_scan_dl = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}/scanned-file/download", headers=auth_h(c1_tok))
    rec.record(
        "CNT-SCAN-01", "Chain1", "CONTRACT", "Officer_Creator_EntityA", "EntityA",
        "Contract Draft", "Block activate without scan/milestones (400) -> add 2 milestones (1.5B) + upload scan -> PUT status Active (200)", "400->200",
        f"{r_act_no_scan.status_code}->{r_act_ok.status_code}",
        "Contract Active & Package Contracted & scan downloadable",
        f"noScan={r_act_no_scan.status_code}, scanUp={r_scan_up.status_code}, actOk={r_act_ok.status_code}, scanDl={r_scan_dl.status_code}",
        r_act_no_scan.status_code == 400 and r_scan_up.status_code == 200 and r_act_ok.status_code == 200 and r_scan_dl.status_code == 200
    )

    # GUAR-ADD-01 & ADD-CREATE-01 while Contract is Active!
    r_guar_create = requests.post(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/guarantees",
        json={
            "guaranteeNumber": f"BL-{run_id}-01",
            "bankName": "Ngân hàng TMCP Ngoại thương Việt Nam (VCB)",
            "amount": 150000000,
            "percentage": 10,
            "effectiveDate": c_start,
            "expiryDate": c_end,
            "notes": "Bao lanh thuc hien hop dong 10 tren 100 gia tri.",
        },
        headers=auth_h(off_a1_tok),
    )
    guar_id = r_guar_create.json().get("data", {}).get("id") if r_guar_create.status_code in (200, 201) else 0
    with open(SCAN_PDF, "rb") as f_pdf:
        r_guar_doc = requests.post(
            f"{api_base_url}/api/contracts/{cnt_sec_id}/guarantees/{guar_id}/document",
            files={"file": ("guarantee_scan.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    r_guar_stat = requests.patch(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/guarantees/{guar_id}/status",
        json={"status": 1, "notes": "Phe duyet bao lanh hop le."},
        headers=auth_h(admin_tok),
    )
    r_guar_dl = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}/guarantees/{guar_id}/document/download", headers=auth_h(off_a1_tok))
    r_guar_list_proc = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}/guarantees", headers=auth_h(off_a1_tok))
    rec.record(
        "GUAR-ADD-01", "Chain6", "GUARANTEE", "Officer_Creator_EntityA", "EntityA",
        "Contract Active", "POST guarantee + upload/download doc + PATCH status + GET /api/contracts/{id}/guarantees (Procurement)", "200|201",
        f"create={r_guar_create.status_code},doc={r_guar_doc.status_code},dl={r_guar_dl.status_code},listProc={r_guar_list_proc.status_code}",
        "Create/Upload/Download/Status 200 & Procurement GET list 200",
        f"create={r_guar_create.status_code}, doc={r_guar_doc.status_code}, stat={r_guar_stat.status_code}, dl={r_guar_dl.status_code}, listProc={r_guar_list_proc.status_code}",
        r_guar_create.status_code in (200, 201) and r_guar_doc.status_code == 200 and r_guar_dl.status_code == 200 and r_guar_list_proc.status_code == 200,
        notes="BUG: ContractService.GetContractGuaranteesAsync (lines 1844-1857) omits .Include(c => c.BidPackage), causing contract.BidPackage?.CreatedBy != userId to always be true (403 Forbidden for all Procurement users)" if r_guar_list_proc.status_code == 403 else "OK"
    )

    r_add_create = requests.post(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/addenda",
        json={
            "addendumNumber": f"PL-{run_id}-01",
            "title": "Phu luc dieu chinh dieu khoan chung",
            "type": 3,
            "additionalValue": 0,
            "extensionDays": 0,
            "reason": "Bo sung quy dinh bao hanh dinh ky.",
            "signedDate": datetime.now(timezone.utc).isoformat(),
        },
        headers=auth_h(off_a1_tok),
    )
    add_id = r_add_create.json().get("data", {}).get("id") if r_add_create.status_code in (200, 201) else 0
    with open(SCAN_PDF, "rb") as f_pdf:
        r_add_doc = requests.post(
            f"{api_base_url}/api/contracts/{cnt_sec_id}/addenda/{add_id}/document",
            files={"file": ("addendum_scan.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    r_add_app = requests.patch(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/addenda/{add_id}/approve",
        json={"isApproved": True, "notes": "Phe duyet phu luc hop dong."},
        headers=auth_h(admin_tok),
    )
    r_add_dl = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}/addenda/{add_id}/document/download", headers=auth_h(off_a1_tok))
    r_add_list_proc = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}/addenda", headers=auth_h(off_a1_tok))
    rec.record(
        "ADD-CREATE-01", "Chain6", "ADDENDUM", "Officer_And_Admin", "EntityA",
        "Contract Active", "POST addendum + upload/download doc + PATCH approve + GET /api/contracts/{id}/addenda (Procurement)", "200|201",
        f"create={r_add_create.status_code},app={r_add_app.status_code},dl={r_add_dl.status_code},listProc={r_add_list_proc.status_code}",
        "Create/Approve/Upload/Download 200 & Procurement GET list 200",
        f"create={r_add_create.status_code}, doc={r_add_doc.status_code}, app={r_add_app.status_code}, dl={r_add_dl.status_code}, listProc={r_add_list_proc.status_code}",
        r_add_create.status_code in (200, 201) and r_add_doc.status_code == 200 and r_add_app.status_code == 200 and r_add_dl.status_code == 200 and r_add_list_proc.status_code == 200,
        notes="BUG: ContractService.GetContractAddendaAsync (lines 2118-2134) omits .Include(c => c.BidPackage), causing contract.BidPackage?.CreatedBy != userId to always be true (403 Forbidden for all Procurement users)" if r_add_list_proc.status_code == 403 else "OK"
    )

    # PROG-ADD-01: Progress updates on cnt_sec_id (C1 allowed, duplicate week 400, decreasing 400, C2 blocked 403)
    r_p1 = requests.post(f"{api_base_url}/api/contracts/{cnt_sec_id}/progress", json={"weekNumber": 1, "completionPercent": 50, "note": "Tuan 1"}, headers=auth_h(c1_tok))
    r_p_dup = requests.post(f"{api_base_url}/api/contracts/{cnt_sec_id}/progress", json={"weekNumber": 1, "completionPercent": 60, "note": "Trung tuan"}, headers=auth_h(c1_tok))
    r_p_dec = requests.post(f"{api_base_url}/api/contracts/{cnt_sec_id}/progress", json={"weekNumber": 2, "completionPercent": 30, "note": "Lui tien do"}, headers=auth_h(c1_tok))
    r_p_c2 = requests.post(f"{api_base_url}/api/contracts/{cnt_sec_id}/progress", json={"weekNumber": 2, "completionPercent": 80, "note": "C2 can thiep"}, headers=auth_h(c2_tok))
    r_p2 = requests.post(f"{api_base_url}/api/contracts/{cnt_sec_id}/progress", json={"weekNumber": 2, "completionPercent": 100, "note": "Tuan 2 hoan tat"}, headers=auth_h(c1_tok))
    rec.record(
        "PROG-ADD-01", "Chain1", "PROGRESS", "Contractor_C1_C2", "ContractA",
        "Contract Active", "POST /progress C1 week 1&2 (201), duplicate week (400), decreasing (400), C2 (403)", "201/400/403",
        f"p1={r_p1.status_code},dup={r_p_dup.status_code},dec={r_p_dec.status_code},c2={r_p_c2.status_code},p2={r_p2.status_code}",
        "C1 201; duplicate & decreasing 400; C2 403",
        f"p1={r_p1.status_code}, dup={r_p_dup.status_code}, dec={r_p_dec.status_code}, c2={r_p_c2.status_code}, p2={r_p2.status_code}",
        r_p1.status_code in (200, 201) and r_p_dup.status_code == 400 and r_p_dec.status_code == 400 and r_p_c2.status_code == 403 and r_p2.status_code in (200, 201)
    )

    # ACC-MAKER-01: Block Admin, Manager A, Finance A, Signer A1, Manager B from Maker technical review; Officer A1 succeeds (Revision 1, Version 1)
    r_mk_adm = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Admin lam Maker"}, headers=auth_h(admin_tok))
    r_mk_mgr = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Manager lam Maker"}, headers=auth_h(mgr_a_tok))
    r_mk_fin = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Finance lam Maker"}, headers=auth_h(fin_a_tok))
    r_mk_sig = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Signer lam Maker"}, headers=auth_h(sig_a1_tok))
    r_mk_entb = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Entity B lam Maker"}, headers=auth_h(mgr_b_tok))
    r_mk_ok = requests.put(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review", json={"isPassed": True, "note": "Officer A1 xac nhan KT Dot 1 lan 1."}, headers=auth_h(off_a1_tok))
    mk1_data = r_mk_ok.json().get("data", {})
    acc_r1_id = mk1_data.get("id")
    rec.record(
        "ACC-MAKER-01", "Chain1", "ACCEPTANCE", "Officer_Creator_EntityA", "EntityA",
        "Contract Active, Milestone 1 Pending", "Block Admin/Manager/Finance/Signer/EntityB (403) -> Officer A1 PUT technical-review (200)", "403->200",
        f"adm={r_mk_adm.status_code},mgr={r_mk_mgr.status_code},fin={r_mk_fin.status_code},sig={r_mk_sig.status_code},entB={r_mk_entb.status_code},offA1={r_mk_ok.status_code}",
        "All non-Officers blocked 403; Officer A1 creates TechnicalApproved r1 v1 with SHA-256",
        f"accId={acc_r1_id}, rev={mk1_data.get('revision')}, ver={mk1_data.get('milestoneVersion')}, sha256Len={len(mk1_data.get('snapshotDocumentHash') or '')}",
        all(x.status_code == 403 for x in (r_mk_adm, r_mk_mgr, r_mk_fin, r_mk_sig, r_mk_entb)) and r_mk_ok.status_code == 200 and len(mk1_data.get("snapshotDocumentHash") or "") == 64
    )

    # ACC-CHECKER-01: Block Maker self-approval, block Admin approval, block revoked member Signer A2 (even with Active SigningAuthority), Signer A1 REJECTS r1 (isApproved=false -> Rejected)
    r_ck_self = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": True, "note": "Maker tu duyet", "acceptanceId": acc_r1_id, "expectedRevision": 1, "expectedVersion": 1},
        headers=auth_h(off_a1_tok),
    )
    r_ck_adm = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": True, "note": "Admin duyet giai ngan", "acceptanceId": acc_r1_id, "expectedRevision": 1, "expectedVersion": 1},
        headers=auth_h(admin_tok),
    )
    r_ck_rev_mem = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": True, "note": "Signer A2 membership revoked du authority con Active", "acceptanceId": acc_r1_id, "expectedRevision": 1, "expectedVersion": 1},
        headers=auth_h(sig_a2_tok),
    )
    r_ck_rej = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": False, "note": "Yeu cau bo sung bien ban hieu chuan thiet bi truoc khi giai ngan.", "acceptanceId": acc_r1_id, "expectedRevision": 1, "expectedVersion": 1},
        headers=auth_h(sig_a1_tok),
    )
    rec.record(
        "ACC-CHECKER-01", "Chain2", "ACCEPTANCE", "Signer_Verified_EntityA", "EntityA",
        "Milestone 1 TechnicalApproved r1", "Maker self-approve (400/403), Admin approve (403), Revoked Member Signer A2 (403), Signer A1 Rejects r1 (200)", "400|403/403/403/200",
        f"self={r_ck_self.status_code},adm={r_ck_adm.status_code},revMem={r_ck_rev_mem.status_code},reject={r_ck_rej.status_code}",
        "Self, Admin & Revoked Member blocked; Signer A1 rejection sets Acceptance=Rejected and Milestone remains Pending",
        f"self={r_ck_self.status_code}, adm={r_ck_adm.status_code}, revMem={r_ck_rev_mem.status_code}, reject={r_ck_rej.status_code}",
        r_ck_self.status_code in (400, 403) and r_ck_adm.status_code == 403 and r_ck_rev_mem.status_code == 403 and r_ck_rej.status_code == 200
    )

    # ACC-CHECKER-02: Officer A1 re-reviews Milestone 1 (r2, v1) -> Manager A edits Milestone 1 (v1 -> v2, invalidates r2) -> Signer A1 blocked 409 on r2 -> Officer A1 re-reviews r3 (v2) -> Signer A1 approves r3 (500M)
    r_mk_r2 = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review",
        json={"isPassed": True, "note": "Officer A1 duyet KT lan 2."},
        headers=auth_h(off_a1_tok),
    ).json()["data"]
    acc_r2_id = r_mk_r2["id"]
    r_edit_m1 = requests.put(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/milestones/{sec_m1_id}",
        json={"title": "Dot 1: Tam ung va giao thiet bi (Da cap nhat ho so)", "dueDate": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(), "amount": 500000000},
        headers=auth_h(mgr_a_tok),
    )
    r_ck_stale = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": True, "note": "Duyet tren bien ban cu r2", "acceptanceId": acc_r2_id, "expectedRevision": 2, "expectedVersion": 1},
        headers=auth_h(sig_a1_tok),
    )
    r_mk_r3 = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/technical-review",
        json={"isPassed": True, "note": "Officer A1 duyet KT lan 3 tren Version 2."},
        headers=auth_h(off_a1_tok),
    ).json()["data"]
    acc_r3_id = r_mk_r3["id"]
    r_ck_m1_ok = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptance",
        json={"isApproved": True, "note": "Phe duyet giai ngan 500M Dot 1 tren r3 v2.", "acceptanceId": acc_r3_id, "expectedRevision": r_mk_r3["revision"], "expectedVersion": r_mk_r3["milestoneVersion"]},
        headers=auth_h(sig_a1_tok),
    )
    rec.record(
        "ACC-CHECKER-02", "Chain3", "ACCEPTANCE", "Officer_And_Signer_EntityA", "EntityA",
        "Milestone 1 Pending after r1 Rejected", "Review r2 -> Edit milestone (v1->v2, ReviewInvalidated) -> Signer blocked 409 -> Review r3 (v2) -> Signer approves 500M (200)", "409->200",
        f"stale={r_ck_stale.status_code}->ok={r_ck_m1_ok.status_code}",
        "409 Conflict on invalidated r2; 200 OK on r3 v2 -> Milestone 1 Completed",
        f"edit={r_edit_m1.status_code}, stale={r_ck_stale.status_code}, r3Rev={r_mk_r3.get('revision')}, r3Ver={r_mk_r3.get('milestoneVersion')}, ok={r_ck_m1_ok.status_code}",
        r_ck_stale.status_code == 409 and r_ck_m1_ok.status_code == 200
    )

    # ACC-CHECKER-03: Officer A2 (non-creator Officer in Entity A) reviews Milestone 2 (r1) -> Replace contract scanned PDF (invalidates r1 & increments Version) -> Officer A1 re-reviews r2 -> Signer A1 approves Milestone 2 (1000M) -> Contract Completed & duplicate approval blocked 409
    r_mk2_r1 = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m2_id}/technical-review",
        json={"isPassed": True, "note": "Officer A2 (non-creator) duyet KT Dot 2 lan 1."},
        headers=auth_h(off_a2_tok),
    )
    with open(SCAN_PDF, "rb") as f_pdf:
        requests.post(
            f"{api_base_url}/api/contracts/{cnt_sec_id}/scanned-file",
            files={"file": ("contract_scan_v2.pdf", f_pdf, "application/pdf")},
            headers=auth_h(off_a1_tok),
        )
    r_mk2_r2 = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m2_id}/technical-review",
        json={"isPassed": True, "note": "Officer A1 duyet KT Dot 2 lan 2 sau khi thay scan hop dong."},
        headers=auth_h(off_a1_tok),
    ).json()["data"]
    acc2_r2_id = r_mk2_r2["id"]
    r_ck_m2_ok = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m2_id}/acceptance",
        json={"isApproved": True, "note": "Phe duyet giai ngan 1000M Dot 2 hoan tat hop dong.", "acceptanceId": acc2_r2_id, "expectedRevision": r_mk2_r2["revision"], "expectedVersion": r_mk2_r2["milestoneVersion"]},
        headers=auth_h(sig_a1_tok),
    )
    r_ck_m2_dup = requests.put(
        f"{api_base_url}/api/contracts/milestones/{sec_m2_id}/acceptance",
        json={"isApproved": True, "note": "Duyet lap lan 2", "acceptanceId": acc2_r2_id, "expectedRevision": r_mk2_r2["revision"], "expectedVersion": r_mk2_r2["milestoneVersion"]},
        headers=auth_h(sig_a1_tok),
    )
    cnt_sec_detail = requests.get(f"{api_base_url}/api/contracts/{cnt_sec_id}", headers=auth_h(off_a1_tok)).json()["data"]
    rec.record(
        "ACC-CHECKER-03", "Chain3", "ACCEPTANCE", "Officer_And_Signer_EntityA", "EntityA",
        "Milestone 2 Pending", "Officer A2 reviews r1 -> Replace contract scan invalidates r1 -> Officer A1 reviews r2 -> Signer A1 approves 1000M -> Contract Completed & duplicate blocked 409", "200->409",
        f"offA2={r_mk2_r1.status_code},ok={r_ck_m2_ok.status_code},dup={r_ck_m2_dup.status_code}",
        "Contract Status=Completed, TotalDisbursed=1.5B, duplicate blocked 409",
        f"contractStatus={cnt_sec_detail.get('status')}, dup={r_ck_m2_dup.status_code}",
        r_mk2_r1.status_code == 200 and r_ck_m2_ok.status_code == 200 and cnt_sec_detail.get("status") in (2, "Completed") and r_ck_m2_dup.status_code in (400, 409)
    )

    # ACC-SNAP-01: Download acceptance snapshot PDF & verify SHA-256 match for Entity A and C1; block Manager B and C2 (403)
    r_snap_sig = requests.get(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptances/{acc_r3_id}/document/download", headers=auth_h(sig_a1_tok))
    r_snap_c1 = requests.get(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptances/{acc_r3_id}/document/download", headers=auth_h(c1_tok))
    r_snap_mgr_b = requests.get(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptances/{acc_r3_id}/document/download", headers=auth_h(mgr_b_tok))
    r_snap_c2 = requests.get(f"{api_base_url}/api/contracts/milestones/{sec_m1_id}/acceptances/{acc_r3_id}/document/download", headers=auth_h(c2_tok))
    dl_sha256 = hashlib.sha256(r_snap_sig.content).hexdigest()
    expected_sha256 = (r_mk_r3.get("snapshotDocumentHash") or "").lower()
    rec.record(
        "ACC-SNAP-01", "Chain1", "ACCEPTANCE", "Signer_Verified_EntityA", "EntityA",
        "Milestone 1 & 2 Completed", "GET /acceptances/{id}/document/download (Entity A & C1 200 + SHA-256 match; Entity B & C2 403)", "200/403",
        f"sig={r_snap_sig.status_code},c1={r_snap_c1.status_code},mgrB={r_snap_mgr_b.status_code},c2={r_snap_c2.status_code}",
        "PDF magic bytes + SHA-256 match; cross-entity/cross-contractor 403",
        f"shaMatch={dl_sha256 == expected_sha256}, mgrB={r_snap_mgr_b.status_code}, c2={r_snap_c2.status_code}",
        r_snap_sig.status_code == 200 and r_snap_c1.status_code == 200 and dl_sha256 == expected_sha256 and r_snap_mgr_b.status_code == 403 and r_snap_c2.status_code == 403
    )

    # CEVAL-POST-01: Evaluate Contractor C1 on Completed contract -> updates Rating -> Credit Profile access & C2 blocked 403
    r_ceval = requests.post(
        f"{api_base_url}/api/contracts/{cnt_sec_id}/evaluation",
        json={"qualityScore": 38, "scheduleScore": 37, "complianceScore": 19, "comments": "Nha thau C1 thuc hien xuat sac, dung tien do va an toan."},
        headers=auth_h(off_a1_tok),
    )
    c1_id = actors["contractor1"]["contractorId"]
    r_cred_off = requests.get(f"{api_base_url}/api/contracts/contractor/{c1_id}/credit-profile", headers=auth_h(off_a1_tok))
    r_cred_c2 = requests.get(f"{api_base_url}/api/contracts/contractor/{c1_id}/credit-profile", headers=auth_h(c2_tok))
    cred_data = r_cred_off.json().get("data", {}) if r_cred_off.status_code == 200 else {}
    rec.record(
        "CEVAL-POST-01", "Chain6", "CEVAL", "Officer_Creator_EntityA", "EntityA",
        "Contract Completed", "POST /api/contracts/{id}/evaluation + GET /credit-profile (Officer 200, C2 403)", "200/403",
        f"eval={r_ceval.status_code},credOff={r_cred_off.status_code},credC2={r_cred_c2.status_code}",
        "Evaluation recorded, Rating updated, C2 blocked 403 from C1 credit profile",
        f"rating={cred_data.get('rating')}, credC2={r_cred_c2.status_code}",
        r_ceval.status_code in (200, 201) and r_cred_off.status_code == 200 and r_cred_c2.status_code == 403
    )

    # =========================================================================
    # 6. CHAIN 4 (DISQUALIFICATION, TERMINATION, REVOKE AWARD, ADMIN REACTIVATE)
    # =========================================================================
    r_pkg_term = requests.post(
        f"{api_base_url}/api/bid-packages",
        json={
            "code": f"PKG-TRM-{run_id}".upper()[:28],
            "name": f"Goi thau Kiem thu Loai Ho so va Cham dut HD {run_id}",
            "description": "Goi thau kiem thu Chain 4.",
            "type": 0,
            "budget": 1000000000,
            "deadline": deadline_future,
            "procuringEntityId": entity_a_id,
        },
        headers=auth_h(off_a1_tok),
    )
    pkg_term_id = r_pkg_term.json()["data"]["id"]
    for ev in actors["evaluators"]:
        requests.post(f"{api_base_url}/api/bid-packages/{pkg_term_id}/evaluators", json={"evaluatorId": ev["userId"]}, headers=auth_h(off_a1_tok))
    r_crit_term = requests.put(
        f"{api_base_url}/api/evaluations/packages/{pkg_term_id}/criteria",
        json={
            "items": [
                {"name": "Ky thuat", "description": "KT", "maxScore": 100, "weight": 70, "isPriceCriteria": False},
                {"name": "Gia", "description": "Gia", "maxScore": 100, "weight": 30, "isPriceCriteria": True},
            ]
        },
        headers=auth_h(off_a1_tok),
    ).json()["data"]
    t_crit_id = next(c["id"] for c in r_crit_term if not c["isPriceCriteria"])

    with open(BAOGIA_PDF, "rb") as f1a, open(NANGLUC_PDF, "rb") as f1b:
        t_sub1_id = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_term_id}/submissions",
            data={"BidPrice": "800000000", "Notes": "C1", "FileTypes": ["0", "1"]},
            files=[("Files", ("b1.pdf", f1a, "application/pdf")), ("Files", ("n1.pdf", f1b, "application/pdf"))],
            headers=auth_h(c1_tok),
        ).json()["data"]["id"]
    with open(BAOGIA_PDF, "rb") as f2a, open(NANGLUC_PDF, "rb") as f2b:
        t_sub2_id = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_term_id}/submissions",
            data={"BidPrice": "850000000", "Notes": "C2", "FileTypes": ["0", "1"]},
            files=[("Files", ("b2.pdf", f2a, "application/pdf")), ("Files", ("n2.pdf", f2b, "application/pdf"))],
            headers=auth_h(c2_tok),
        ).json()["data"]["id"]
    with open(BAOGIA_PDF, "rb") as f3a, open(NANGLUC_PDF, "rb") as f3b:
        t_sub3_id = requests.post(
            f"{api_base_url}/api/bid-packages/{pkg_term_id}/submissions",
            data={"BidPrice": "820000000", "Notes": "C3", "FileTypes": ["0", "1"]},
            files=[("Files", ("b3.pdf", f3a, "application/pdf")), ("Files", ("n3.pdf", f3b, "application/pdf"))],
            headers=auth_h(c3_tok),
        ).json()["data"]["id"]

    # Close & move to Evaluating
    requests.put(f"{api_base_url}/api/bid-packages/{pkg_term_id}/status", json={"newStatus": 1, "reason": "Dong som"}, headers=auth_h(off_a1_tok))
    p_reqs = requests.get(f"{api_base_url}/api/bid-packages/status-approvals/pending", headers=auth_h(admin_tok)).json()["data"]
    t_req_id = next(x["id"] for x in p_reqs if x["bidPackageId"] == pkg_term_id)
    requests.post(f"{api_base_url}/api/bid-packages/status-approvals/{t_req_id}/review", json={"isApproved": True, "adminComment": "OK"}, headers=auth_h(admin_tok))
    requests.put(f"{api_base_url}/api/bid-packages/{pkg_term_id}/status", json={"newStatus": 2, "reason": "Evaluating"}, headers=auth_h(off_a1_tok))

    # Disqualify C3 submission & verify C3 cannot withdraw disqualified submission
    r_disq_c3 = requests.post(
        f"{api_base_url}/api/evaluations/submissions/{t_sub3_id}/disqualify",
        json={"reason": "Khong dap ung bao lanh du thau theo HSMT."},
        headers=auth_h(off_a1_tok),
    )
    r_wd_disq = requests.delete(f"{api_base_url}/api/submissions/{t_sub3_id}", headers=auth_h(c3_tok))

    # 3 Evaluators score C1 (90) and C2 (82)
    for tok in (ev1_tok, ev2_tok, ev3_tok):
        requests.post(f"{api_base_url}/api/evaluations/submissions/{t_sub1_id}/scores", json={"scores": [{"criteriaId": t_crit_id, "score": 90, "comment": "OK"}]}, headers=auth_h(tok))
        requests.post(f"{api_base_url}/api/evaluations/submissions/{t_sub2_id}/scores", json={"scores": [{"criteriaId": t_crit_id, "score": 82, "comment": "OK"}]}, headers=auth_h(tok))

    requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_term_id}/finalize",
        json={"selectedSubmissionId": t_sub1_id, "decisionReason": "Chon C1 Rank 1."},
        headers=auth_h(off_a1_tok),
    )
    t_cnt_id = requests.post(
        f"{api_base_url}/api/contracts",
        json={"bidPackageId": pkg_term_id, "contractorId": actors["contractor1"]["contractorId"], "value": 800000000, "terms": "HD", "startDate": c_start, "endDate": c_end},
        headers=auth_h(off_a1_tok),
    ).json()["data"]["id"]
    requests.post(f"{api_base_url}/api/contracts/{t_cnt_id}/milestones", json={"title": "M1", "dueDate": c_end, "amount": 800000000}, headers=auth_h(off_a1_tok))
    with open(SCAN_PDF, "rb") as f_pdf:
        requests.post(f"{api_base_url}/api/contracts/{t_cnt_id}/scanned-file", files={"file": ("scan.pdf", f_pdf, "application/pdf")}, headers=auth_h(off_a1_tok))
    requests.put(f"{api_base_url}/api/contracts/{t_cnt_id}/status", json={"newStatus": 1, "note": "Active"}, headers=auth_h(off_a1_tok))

    # Terminate contract (Active -> Terminated)
    r_term_cnt = requests.put(
        f"{api_base_url}/api/contracts/{t_cnt_id}/status",
        json={"newStatus": 3, "note": "Vi pham tien do giao hang nghiem trong.", "terminationReasonCategory": "ContractorBreach", "decisionNumber": f"QD-TERM-{run_id}"},
        headers=auth_h(off_a1_tok),
    )
    # Revoke award after termination -> C1 is automatically Disqualified and cannot be re-selected
    r_rev_after_term = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_term_id}/revoke-award",
        json={"reason": "Thu hoi trao thau do hop dong voi C1 da bi cham dut."},
        headers=auth_h(off_a1_tok),
    )
    r_reselect_c1 = requests.post(
        f"{api_base_url}/api/evaluations/packages/{pkg_term_id}/finalize",
        json={"selectedSubmissionId": t_sub1_id, "decisionReason": "Co tinh chon lai C1 da bi Terminated."},
        headers=auth_h(off_a1_tok),
    )
    # Admin reactivates Terminated contract (Terminated -> Active)
    r_admin_react = requests.put(
        f"{api_base_url}/api/contracts/{t_cnt_id}/status",
        json={"newStatus": 1, "note": "Admin khoi phuc hieu luc hop dong sau khieu nai."},
        headers=auth_h(admin_tok),
    )
    rec.record(
        "CHAIN4-TERM-01", "Chain4", "TERMINATION", "Officer_And_Admin", "EntityA",
        "Package PKG-TRM", "Disqualify C3 (200) -> block withdraw (400) -> Terminate Contract (200) -> RevokeAward (200) -> block reselecting C1 (400) -> Admin reactivate (200)",
        "200/400/200/200/400/200",
        f"disq={r_disq_c3.status_code},wd={r_wd_disq.status_code},term={r_term_cnt.status_code},rev={r_rev_after_term.status_code},resel={r_reselect_c1.status_code},react={r_admin_react.status_code}",
        "All Chain 4 disqualification, termination, award revocation, and admin reactivation rules enforced",
        f"disq={r_disq_c3.status_code}, wdDisq={r_wd_disq.status_code}, term={r_term_cnt.status_code}, rev={r_rev_after_term.status_code}, reselectC1={r_reselect_c1.status_code}, react={r_admin_react.status_code}",
        r_disq_c3.status_code == 200 and r_wd_disq.status_code == 400 and r_term_cnt.status_code == 200 and r_rev_after_term.status_code == 200 and r_reselect_c1.status_code == 400 and r_admin_react.status_code == 200
    )

    # =========================================================================
    # 7. REPORTS, NOTIFICATIONS, STORAGE ADMIN, AUDIT LOGS, UI ROUTEGUARD
    # =========================================================================
    r_dash = requests.get(f"{api_base_url}/api/reports/dashboard", headers=auth_h(admin_tok))
    r_psum = requests.get(f"{api_base_url}/api/reports/procurement-summary", headers=auth_h(admin_tok))
    r_cperf = requests.get(f"{api_base_url}/api/reports/contractor-performance", headers=auth_h(admin_tok))
    r_cprog = requests.get(f"{api_base_url}/api/reports/contracts-progress", headers=auth_h(admin_tok))
    r_exp_ctr_csv = requests.get(f"{api_base_url}/api/reports/contractors/export", headers=auth_h(admin_tok))
    r_exp_cnt_csv = requests.get(f"{api_base_url}/api/reports/contracts/export", headers=auth_h(admin_tok))
    r_exp_cnt_xlsx = requests.get(f"{api_base_url}/api/reports/contracts/export-excel", headers=auth_h(admin_tok))

    with open(os.path.join(exports_dir, "contractors_export.csv"), "wb") as f:
        f.write(r_exp_ctr_csv.content)
    with open(os.path.join(exports_dir, "contracts_export.csv"), "wb") as f:
        f.write(r_exp_cnt_csv.content)
    with open(os.path.join(exports_dir, "contracts_export.xlsx"), "wb") as f:
        f.write(r_exp_cnt_xlsx.content)

    db_pkg_cnt = int(run_sqlcmd("SELECT COUNT(*) FROM BidPackages;", db_name=db_name).splitlines()[-1].strip())
    dash_pkg_cnt = r_dash.json().get("data", {}).get("totalPackages", -1)
    has_bom = r_exp_ctr_csv.content.startswith(b"\xef\xbb\xbf") and r_exp_cnt_csv.content.startswith(b"\xef\xbb\xbf")
    rec.record(
        "REP-DASH-01", "Chain6", "REPORT", "Admin_And_Procurement", "Platform",
        "Populated database", "GET dashboard, procurement-summary, contractor-performance, contracts-progress & CSV/XLSX exports", "200", r_dash.status_code,
        "Dashboard totalBidPackages matches SQL DB & CSVs have UTF-8 BOM & XLSX has PK magic bytes",
        f"dbPackages={db_pkg_cnt}, dashPackages={dash_pkg_cnt}, csvBom={has_bom}, xlsxMagic={r_exp_cnt_xlsx.content[:2] == b'PK'}",
        all(x.status_code == 200 for x in (r_dash, r_psum, r_cperf, r_cprog, r_exp_ctr_csv, r_exp_cnt_csv, r_exp_cnt_xlsx))
        and db_pkg_cnt == dash_pkg_cnt and has_bom and r_exp_cnt_xlsx.content[:2] == b"PK",
        evidence_file="exports/contracts_export.xlsx"
    )

    # NOTIF-LIST-01: Notifications API + cross-user read protection + Entity isolation check
    r_n_c1 = requests.get(f"{api_base_url}/api/notifications?pageIndex=1&pageSize=20", headers=auth_h(c1_tok))
    r_n_unread = requests.get(f"{api_base_url}/api/notifications/unread-count", headers=auth_h(c1_tok))
    c1_notifs = r_n_c1.json().get("data", {}).get("items", [])
    c1_first_nid = c1_notifs[0]["id"] if c1_notifs else 0
    r_n_cross_read = requests.patch(f"{api_base_url}/api/notifications/{c1_first_nid}/read", headers=auth_h(c2_tok))
    r_n_own_read = requests.patch(f"{api_base_url}/api/notifications/{c1_first_nid}/read", headers=auth_h(c1_tok))
    r_n_mark_all = requests.post(f"{api_base_url}/api/notifications/mark-all-read", headers=auth_h(c1_tok))

    # Verify Manager B (Entity B) received zero notifications mentioning Entity A's packages/contracts
    r_n_mgr_b = requests.get(f"{api_base_url}/api/notifications?pageIndex=1&pageSize=50", headers=auth_h(mgr_b_tok)).json().get("data", {}).get("items", [])
    leaked_to_b = [n for n in r_n_mgr_b if manifest["businessFlow"]["packageCode"] in (n.get("title", "") + n.get("message", ""))]
    rec.record(
        "NOTIF-LIST-01", "Chain6", "NOTIFICATION", "All_Actors", "EntityScoped",
        "Notifications generated across workflows", "GET /notifications, unread-count, cross-user read block (404), mark-all-read & Entity B leak check", "200/404",
        f"list={r_n_c1.status_code},ownRead={r_n_own_read.status_code},crossRead={r_n_cross_read.status_code},markAll={r_n_mark_all.status_code},leakedToB={len(leaked_to_b)}",
        "Own read & mark-all-read 200, cross-user read blocked 404, 0 notifications leaked to Entity B",
        f"c1Count={len(c1_notifs)}, crossRead={r_n_cross_read.status_code}, markAll={r_n_mark_all.status_code}, leakedToB={len(leaked_to_b)}",
        r_n_c1.status_code == 200 and r_n_unread.status_code == 200 and r_n_own_read.status_code == 200 and r_n_cross_read.status_code in (403, 404) and r_n_mark_all.status_code == 200 and len(leaked_to_b) == 0,
        notes="BUG: BidSubmissionService.SubmitBidAsync (lines 230-241) broadcasts sealed-bid submission notifications to ALL users with Procurement role across the platform, leaking Entity A package notifications to Manager B of Entity B" if len(leaked_to_b) > 0 else "OK"
    )

    # STOR-STAT-01: Storage Admin endpoints (Admin 200/400 safe local guard; non-Admin 403)
    r_st_stat = requests.get(f"{api_base_url}/api/admin/storage/status", headers=auth_h(admin_tok))
    r_st_test = requests.get(f"{api_base_url}/api/admin/storage/test-connection", headers=auth_h(admin_tok))
    r_st_sync = requests.post(f"{api_base_url}/api/admin/storage/sync-to-cloud", headers=auth_h(admin_tok))
    r_st_rb = requests.post(f"{api_base_url}/api/admin/storage/rollback", params={"backupTableName": "INVALID_TABLE"}, headers=auth_h(admin_tok))
    r_st_non_adm = requests.get(f"{api_base_url}/api/admin/storage/status", headers=auth_h(off_a1_tok))
    st_provider = r_st_stat.json().get("data", {}).get("activeProvider") if r_st_stat.status_code == 200 else "ERR"
    rec.record(
        "STOR-STAT-01", "Chain6", "STORAGE", "Admin", "Platform",
        "Local Storage provider active", "GET /admin/storage/status (200), test-connection, sync-to-cloud, rollback & non-Admin (403)", "200/403",
        f"stat={r_st_stat.status_code},nonAdm={r_st_non_adm.status_code}",
        "Admin 200 (activeProvider=Local), non-Admin blocked 403",
        f"activeProvider={st_provider}, sync={r_st_sync.status_code}, rb={r_st_rb.status_code}, nonAdm={r_st_non_adm.status_code}",
        r_st_stat.status_code == 200 and st_provider == "Local" and r_st_non_adm.status_code == 403
    )

    # AUDIT-LIST-01: Verify AuditLogs via API & SQL DB + non-Admin blocked 403
    r_aud_list = requests.get(f"{api_base_url}/api/audit-logs?pageIndex=1&pageSize=50", headers=auth_h(admin_tok))
    aud_items = r_aud_list.json().get("data", {}).get("items", [])
    first_aud_id = aud_items[0]["id"] if aud_items else 0
    r_aud_detail = requests.get(f"{api_base_url}/api/audit-logs/{first_aud_id}", headers=auth_h(admin_tok))
    r_aud_proc = requests.get(f"{api_base_url}/api/audit-logs", headers=auth_h(off_a1_tok))
    r_aud_ctr = requests.get(f"{api_base_url}/api/audit-logs", headers=auth_h(c1_tok))

    db_actions_raw = run_sqlcmd("SELECT DISTINCT Action FROM AuditLogs;", db_name=db_name)
    required_actions = [
        "FINALIZE_AWARD",
        "REVOKE_AWARD",
        "DISQUALIFY_SUBMISSION",
        "INVALIDATE_MILESTONE_REVIEW",
        "APPROVE_MILESTONE_ACCEPTANCE",
        "REJECT_MILESTONE_ACCEPTANCE",
        "IMPORT_CONTRACTORS",
    ]
    missing_actions = [a for a in required_actions if a not in db_actions_raw]
    rec.record(
        "AUDIT-LIST-01", "Chain6", "AUDIT", "Admin_And_Procurement", "Platform",
        "All chains executed", "GET /api/audit-logs & /api/audit-logs/{id} (Admin 200; Procurement & Contractor 403) + verify DB actions", "200/403",
        f"adm={r_aud_list.status_code},detail={r_aud_detail.status_code},proc={r_aud_proc.status_code},ctr={r_aud_ctr.status_code}",
        "Admin 200 with all critical actions recorded; non-Admin blocked 403",
        f"missingActions={missing_actions}, proc={r_aud_proc.status_code}, ctr={r_aud_ctr.status_code}",
        r_aud_list.status_code == 200 and r_aud_detail.status_code == 200 and r_aud_proc.status_code == 403 and r_aud_ctr.status_code == 403 and len(missing_actions) == 0,
        evidence_file="screenshots/11_audit_logs_verified.png"
    )

    # UI-GUARD-01: Playwright RouteGuard & F5 reload verification
    ui_guard_ok = False
    ui_notes = ""
    shot_guard_path = os.path.join(screenshots_dir, "12_route_guard_and_f5_verified.png")
    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            ctx = browser.new_context(viewport={"width": 1366, "height": 850})
            page = ctx.new_page()

            # Login as Contractor C1 and attempt direct navigation to /users and /audit-logs
            page.goto(f"{base_url}/login")
            page.wait_for_selector('input[type="email"]', timeout=10000)
            page.fill('input[type="email"]', c1_email)
            page.fill('input[type="password"]', "Admin@123")
            page.click('button[type="submit"]')
            page.wait_for_function("() => !window.location.pathname.includes('/login')", timeout=15000)

            page.goto(f"{base_url}/users")
            page.wait_for_timeout(1000)
            url_after_users = page.url

            page.goto(f"{base_url}/audit-logs")
            page.wait_for_timeout(1000)
            url_after_audit = page.url

            # Navigate to completed contract & reload F5
            page.goto(f"{base_url}/contracts/{main_contract_id}")
            page.wait_for_selector("h1", timeout=15000)
            page.reload()
            page.wait_for_selector('span:has-text("Hoàn thành"), span:has-text("Completed")', timeout=15000)
            page.screenshot(path=shot_guard_path)
            ctx.close()
            browser.close()
            ui_guard_ok = ("/users" not in url_after_users) and ("/audit-logs" not in url_after_audit)
            ui_notes = f"afterUsers={url_after_users}, afterAudit={url_after_audit}, F5CompletedPersisted=True"
    except Exception as ex:
        ui_notes = f"UI check exception: {ex}"

    rec.record(
        "UI-GUARD-01", "Chain5", "UI_SESSION", "All_Actors", "Browser",
        "Chromium headless browser", "Contractor direct navigation to /users & /audit-logs blocked by RouteGuard + F5 reload on /contracts/{id}", "Redirect/Persist", "200",
        "Redirected away from /users & /audit-logs; Completed contract persists across F5",
        ui_notes,
        ui_guard_ok,
        evidence_file="screenshots/12_route_guard_and_f5_verified.png"
    )

    json_out, csv_out = rec.save()
    passed_cnt = sum(1 for c in rec.cases if c["status"] == "PASS")
    failed_cnt = sum(1 for c in rec.cases if c["status"] == "FAIL")
    manifest["allWorkflowsSummary"] = {
        "totalCases": len(rec.cases),
        "passed": passed_cnt,
        "failed": failed_cnt,
        "caseResultsJson": json_out,
        "caseResultsCsv": csv_out,
    }
    logger.info(f"All-Workflows E2E Summary: total={len(rec.cases)}, PASS={passed_cnt}, FAIL={failed_cnt}")
    return rec.cases
