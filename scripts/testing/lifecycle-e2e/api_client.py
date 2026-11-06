import os
import requests
import json
import logging

logger = logging.getLogger("api_client")

class ApiClient:
    def __init__(self, base_url="http://localhost:5225"):
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()

    def get_health(self):
        url = f"{self.base_url}/api/health"
        res = self.session.get(url, timeout=10)
        return res

    def login(self, email, password):
        url = f"{self.base_url}/api/auth/login"
        payload = {"email": email, "password": password}
        res = self.session.post(url, json=payload, timeout=10)
        res.raise_for_status()
        data = res.json()
        if not data.get("success"):
            raise RuntimeError(f"Login failed for {email}: {data.get('message')}")
        return data["data"]["token"], data["data"]["user"]

    def register_procuring_entity(self, full_name, email, password, org_name, tax_code, pdf_path):
        url = f"{self.base_url}/api/auth/register-procuring-entity"
        data = {
            "FullName": full_name,
            "Email": email,
            "Password": password,
            "Phone": "0901234567",
            "OrganizationName": org_name,
            "OrganizationType": "Ban Quản lý Dự án",
            "TaxCode": tax_code,
            "BudgetCode": "1054321",
            "Address": "Số 123 Đường Trần Phú, Quận Ba Đình, Hà Nội",
            "RepresentativeName": full_name,
            "RepresentativeTitle": "Giám đốc Ban QLDA",
            "RepresentativePhone": "0901234567"
        }
        with open(pdf_path, "rb") as f:
            files = {"EstablishmentDecisionFile": ("QuyetDinhThanhLap.pdf", f, "application/pdf")}
            res = self.session.post(url, data=data, files=files, timeout=15)
        res.raise_for_status()
        body = res.json()
        if not body.get("success"):
            raise RuntimeError(f"Register procuring entity failed: {body.get('message')}")
        return body["data"]

    def verify_procuring_entity(self, admin_token, entity_id, is_approved=True, notes="Hồ sơ pháp lý hợp lệ"):
        url = f"{self.base_url}/api/procuring-entities/{entity_id}/verify"
        headers = {"Authorization": f"Bearer {admin_token}"}
        payload = {"isApproved": is_approved, "adminNotes": notes}
        res = self.session.patch(url, json=payload, headers=headers, timeout=10)
        res.raise_for_status()
        return res.json()["data"]

    def send_invitation(self, proc_token, entity_id, email, role_int):
        url = f"{self.base_url}/api/procuring-entities/{entity_id}/invitations"
        headers = {"Authorization": f"Bearer {proc_token}"}
        payload = {"email": email, "roleInEntity": role_int}
        res = self.session.post(url, json=payload, headers=headers, timeout=10)
        res.raise_for_status()
        return res.json()["data"]

    def accept_invitation(self, token, full_name, password):
        url = f"{self.base_url}/api/procuring-entities/invitations/accept"
        payload = {"token": token, "fullName": full_name, "password": password}
        res = self.session.post(url, json=payload, timeout=10)
        res.raise_for_status()
        return res.json()["data"]

    def request_signing_authority(self, proc_token, entity_id, user_id, doc_num, valid_from, valid_to, notes):
        url = f"{self.base_url}/api/procuring-entities/{entity_id}/signing-authorities"
        headers = {"Authorization": f"Bearer {proc_token}"}
        payload = {
            "userId": user_id,
            "authorityType": 0, # DisbursementApproval = 0
            "scope": "EntityWide",
            "validFrom": valid_from,
            "validTo": valid_to,
            "decisionNumber": doc_num,
            "legalBasis": notes
        }
        res = self.session.post(url, json=payload, headers=headers, timeout=10)
        res.raise_for_status()
        return res.json()["data"]

    def verify_signing_authority(self, admin_token, authority_id, is_approved=True, notes="Thẩm định đạt yêu cầu"):
        url = f"{self.base_url}/api/signing-authorities/{authority_id}/verify"
        headers = {"Authorization": f"Bearer {admin_token}"}
        payload = {"isApproved": is_approved, "verificationNotes": notes}
        res = self.session.put(url, json=payload, headers=headers, timeout=10)
        res.raise_for_status()
        return res.json()["data"]

    def register_contractor(self, full_name, email, password, company_name, tax_code, pdf_path):
        url = f"{self.base_url}/api/auth/register-contractor"
        data = {
            "FullName": full_name,
            "Email": email,
            "Password": password,
            "Phone": "0911223344",
            "CompanyName": company_name,
            "TaxCode": tax_code,
            "Address": "Tầng 5, Tòa nhà Innovation, Quận Cầu Giấy, Hà Nội"
        }
        with open(pdf_path, "rb") as f:
            files = {"BusinessLicenseFile": ("GiayPhepKinhDoanh.pdf", f, "application/pdf")}
            res = self.session.post(url, data=data, files=files, timeout=15)
        res.raise_for_status()
        body = res.json()
        if not body.get("success"):
            raise RuntimeError(f"Register contractor failed: {body.get('message')}")
        return body["data"]

    def verify_contractor(self, admin_token, contractor_id, is_approved=True, notes="Đạt thẩm định năng lực"):
        url = f"{self.base_url}/api/contractors/{contractor_id}/verify"
        headers = {"Authorization": f"Bearer {admin_token}"}
        payload = {"isApproved": is_approved, "notes": notes}
        res = self.session.patch(url, json=payload, headers=headers, timeout=10)
        res.raise_for_status()
        return res.json()["data"]
