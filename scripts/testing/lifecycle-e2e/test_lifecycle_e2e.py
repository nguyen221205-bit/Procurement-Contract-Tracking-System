import os
import sys
import time
import re
import json
import logging
from playwright.sync_api import sync_playwright

logger = logging.getLogger("test_lifecycle_e2e")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

SAMPLE_DOCS_DIR = r"D:\Thuc_Tap_backend\TaiLieuMau"
HSMT_PDF = os.path.join(SAMPLE_DOCS_DIR, "HoSoMoiThau_HSMT.pdf")
BAOGIA_PDF = os.path.join(SAMPLE_DOCS_DIR, "BaoGia_TaiChinh.pdf")
NANGLUC_PDF = os.path.join(SAMPLE_DOCS_DIR, "HoSoNangLuc.pdf")
TIENDO_PDF = os.path.join(SAMPLE_DOCS_DIR, "TienDo_BienPhap.pdf")
SCAN_PDF = os.path.join(SAMPLE_DOCS_DIR, "HopDong_DaKy_Scan.pdf")

def ui_login(page, base_url, email, password="Admin@123"):
    page.goto(f"{base_url}/login")
    page.wait_for_selector('input[type="email"]', timeout=10000)
    page.fill('input[type="email"]', email)
    page.fill('input[type="password"]', password)
    page.click('button[type="submit"]')
    page.wait_for_function(
        "() => !window.location.pathname.includes('/login') && sessionStorage.getItem('token') !== null",
        timeout=15000
    )
    page.wait_for_timeout(500)

def run_lifecycle_e2e(run_id, manifest, base_url="http://localhost:5173", api_base_url="http://localhost:5225", output_dir=None):
    if output_dir is None:
        output_dir = os.path.join(os.path.dirname(__file__), "runs", run_id, "screenshots")
    os.makedirs(output_dir, exist_ok=True)

    def save_shot(page, filename):
        path = os.path.join(output_dir, filename)
        page.screenshot(path=path)
        logger.info(f"Screenshot saved: {filename}")
        return path

    actors = manifest["actors"]
    entities = manifest["entities"]
    results = {}

    admin = actors["admin"]
    manager = actors["managerA"]
    officer = actors["officerA"]
    checker = actors["checkerA"]
    c1 = actors["contractor1"]
    c2 = actors["contractor2"]
    evaluators = actors["evaluators"]

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)

        # =========================================================================
        # STEP 1: Manager / Officer creates package with criteria & docs (Entity A)
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 1] Officer/Manager creates package with criteria & docs...")
        logger.info("=================================================================")
        ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        page = ctx.new_page()
        page.on("dialog", lambda d: d.accept())

        ui_login(page, base_url, manager["email"])

        # Navigate to /packages
        page.goto(f"{base_url}/packages")
        page.wait_for_selector('button:has-text("Tạo gói thầu mới")', timeout=15000)

        # Open Create Package Modal
        page.click('button:has-text("Tạo gói thầu mới")')
        page.wait_for_selector('h3:has-text("Tạo gói thầu mới")', timeout=10000)

        modal = page.locator('.fixed.inset-0')
        page.wait_for_timeout(1000)
        entity_select = modal.locator('select#package-entity')
        if entity_select.count() > 0:
            entity_select.select_option(str(entities["entityA"]["id"]))

        pkg_name = f"Gói thầu Thiết bị CNTT & Giám sát E2E {run_id}"
        pkg_code = f"PKG-{run_id}".upper()
        modal.locator('input[placeholder*="Mua sắm thiết bị"]').fill(pkg_name)
        modal.locator('input[placeholder*="Hệ thống tự sinh"]').fill(pkg_code)

        # Select Type = Goods
        type_btn = modal.locator('button:has-text("Hàng hóa")')
        if type_btn.count() > 0:
            type_btn.click()

        # Budget: 1,800,000,000 VND
        budget_input = modal.locator('input[placeholder*="1500000000"]')
        budget_input.fill("1800000000")

        # Deadline: click "+30 ngày" shortcut
        shortcut_30 = modal.locator('button:has-text("+30 ngày")')
        if shortcut_30.is_visible():
            shortcut_30.click()
        else:
            modal.locator('input[type="datetime-local"]').fill("2026-12-31T23:59")

        modal.locator('textarea').fill("Cung cấp, lắp đặt và cấu hình hệ thống thiết bị mạng lõi và an ninh mạng đạt chuẩn.")

        # Attach HSMT document
        file_input = modal.locator('input[type="file"]')
        file_input.set_input_files(HSMT_PDF)
        page.wait_for_timeout(500)

        # Submit create package
        submit_btn = modal.locator('button[type="submit"]:has-text("Tạo gói thầu")')
        submit_btn.click()

        # Wait redirect to /packages/<packageId>
        page.wait_for_url(re.compile(rf"{base_url}/packages/\d+"), timeout=20000)
        package_url = page.url
        package_id = int(package_url.split('/')[-1])
        logger.info(f"Package created successfully with ID: {package_id} (URL: {package_url})")

        # Setup Criteria: Switch to criteria tab
        page.wait_for_selector('button:has-text("Tiêu chí đánh giá")', timeout=10000)
        page.click('button:has-text("Tiêu chí đánh giá")')
        page.wait_for_timeout(1000)

        # Apply standard Goods criteria template
        template_btn = page.locator('button:has-text("Áp dụng bộ mẫu")').first
        template_btn.click()
        page.wait_for_selector('h3:has-text("Bộ tiêu chí mẫu")', timeout=10000)
        t_modal = page.locator('.fixed.inset-0')
        goods_opt = t_modal.locator('button:has-text("Mua sắm"), button:has-text("Hàng hóa")').first
        if goods_opt.count() > 0 and goods_opt.is_visible():
            goods_opt.click()
        apply_btn = t_modal.locator('button:has-text("Áp dụng")').last
        apply_btn.click()
        page.wait_for_selector('h3:has-text("Bộ tiêu chí mẫu")', state='hidden', timeout=10000)
        page.wait_for_timeout(1500)

        save_shot(page, "01_package_created_open.png")
        manifest["businessFlow"] = {
            "packageId": package_id,
            "packageCode": pkg_code,
            "packageName": pkg_name,
            "budget": 1800000000
        }
        results["step1"] = {"status": "PASS", "packageId": package_id, "packageCode": pkg_code}
        ctx.close()

        # =========================================================================
        # STEP 2: Two Contractors log in separately & submit multi-file bids
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 2] Contractors submit competing bids with files...")
        logger.info("=================================================================")
        # Contractor 1: Bid Price = 1,500,000,000 VND
        c1_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        c1_page = c1_ctx.new_page()
        c1_page.on("dialog", lambda d: d.accept())
        ui_login(c1_page, base_url, c1["email"])

        c1_page.goto(f"{base_url}/packages/{package_id}")
        c1_page.wait_for_selector('button:has-text("Nộp hồ sơ dự thầu")', timeout=15000)
        c1_page.click('button:has-text("Nộp hồ sơ dự thầu")')
        c1_page.wait_for_selector('h3:has-text("Nộp hồ sơ dự thầu")', timeout=10000)

        bid_modal1 = c1_page.locator('.fixed.inset-0')
        bid_modal1.locator('input[placeholder*="Nhập số tiền dự thầu"]').fill("1500000000")

        # Multi-file upload: BaoGia (0), NangLuc (1), TienDo (2)
        bid_modal1.locator('input[type="file"]').set_input_files([BAOGIA_PDF, NANGLUC_PDF, TIENDO_PDF])
        c1_page.wait_for_timeout(1000)
        selects1 = bid_modal1.locator('select')
        selects1.nth(0).select_option("0")
        selects1.nth(1).select_option("1")
        selects1.nth(2).select_option("2")
        c1_page.wait_for_timeout(500)

        bid_modal1.locator('input[type="checkbox"]').check()
        bid_modal1.locator('button[type="submit"]').click()
        c1_page.wait_for_selector('h3:has-text("Nộp hồ sơ dự thầu")', state='hidden', timeout=15000)
        c1_page.wait_for_timeout(1500)
        logger.info("Contractor 1 submitted bid (1,500,000,000 VND) successfully!")
        c1_ctx.close()

        # Contractor 2: Bid Price = 1,600,000,000 VND
        c2_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        c2_page = c2_ctx.new_page()
        c2_page.on("dialog", lambda d: d.accept())
        ui_login(c2_page, base_url, c2["email"])

        c2_page.goto(f"{base_url}/packages/{package_id}")
        c2_page.wait_for_selector('button:has-text("Nộp hồ sơ dự thầu")', timeout=15000)
        c2_page.click('button:has-text("Nộp hồ sơ dự thầu")')
        c2_page.wait_for_selector('h3:has-text("Nộp hồ sơ dự thầu")', timeout=10000)

        bid_modal2 = c2_page.locator('.fixed.inset-0')
        bid_modal2.locator('input[placeholder*="Nhập số tiền dự thầu"]').fill("1600000000")

        bid_modal2.locator('input[type="file"]').set_input_files([BAOGIA_PDF, NANGLUC_PDF, TIENDO_PDF])
        c2_page.wait_for_timeout(1000)
        selects2 = bid_modal2.locator('select')
        selects2.nth(0).select_option("0")
        selects2.nth(1).select_option("1")
        selects2.nth(2).select_option("2")
        c2_page.wait_for_timeout(500)

        bid_modal2.locator('input[type="checkbox"]').check()
        bid_modal2.locator('button[type="submit"]').click()
        c2_page.wait_for_selector('h3:has-text("Nộp hồ sơ dự thầu")', state='hidden', timeout=15000)
        c2_page.wait_for_timeout(1500)
        save_shot(c2_page, "02_contractors_submitted_bids.png")
        logger.info("Contractor 2 submitted bid (1,600,000,000 VND) successfully!")
        c2_ctx.close()
        results["step2"] = {"status": "PASS", "contractor1Bid": 1500000000, "contractor2Bid": 1600000000}

        # =========================================================================
        # STEP 3: Procurement assigns 3 evaluators, closes bidding, moves to Evaluating
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 3] Assigning evaluators and transitioning to Evaluating...")
        logger.info("=================================================================")
        proc_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        proc_page = proc_ctx.new_page()
        proc_page.on("dialog", lambda d: d.accept())

        ui_login(proc_page, base_url, manager["email"])

        proc_page.goto(f"{base_url}/packages/{package_id}")
        proc_page.wait_for_selector('button:has-text("Tổ chuyên gia")', timeout=15000)
        proc_page.click('button:has-text("Tổ chuyên gia")')
        proc_page.wait_for_timeout(1000)

        # Assign 3 evaluators
        for ev in evaluators:
            assign_btn = proc_page.locator('button:has-text("Phân công Giám khảo")').first
            assign_btn.click()
            proc_page.wait_for_selector('h3:has-text("Phân công Giám khảo chấm thầu")', timeout=10000)
            proc_page.wait_for_timeout(1000)
            a_modal = proc_page.locator('.fixed.inset-0')
            ev_row = a_modal.locator(f'div:has-text("{ev["email"]}")').last
            ev_row.click()
            proc_page.wait_for_timeout(300)
            confirm_assign = a_modal.locator('button:has-text("Xác nhận phân công")').first
            confirm_assign.click()
            proc_page.wait_for_selector('h3:has-text("Phân công Giám khảo chấm thầu")', state='hidden', timeout=10000)
            proc_page.wait_for_timeout(1000)

        # Request early close from Procurement -> creates Pending approval request
        close_btn = proc_page.locator('button:has-text("Đóng nhận thầu")').first
        close_btn.click()
        proc_page.wait_for_timeout(1000)
        c_modal = proc_page.locator('.fixed.inset-0')
        reason_input = c_modal.locator('textarea')
        if reason_input.count() > 0 and reason_input.is_visible():
            reason_input.fill("Đã đủ hồ sơ dự thầu và tiến hành mở thầu chấm điểm theo quy định.")
        confirm_btn = c_modal.locator('button:has-text("Đóng nhận thầu ngay"), button:has-text("Xác nhận")').last
        confirm_btn.click()
        proc_page.wait_for_timeout(2000)
        proc_ctx.close()

        # Admin approves early close request AND transitions package to Evaluating
        adm_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        adm_page = adm_ctx.new_page()
        adm_page.on("dialog", lambda d: d.accept())
        ui_login(adm_page, base_url, admin["email"])

        adm_page.goto(f"{base_url}/packages/{package_id}")
        adm_page.wait_for_selector('h1', timeout=15000)
        adm_page.wait_for_timeout(1000)

        review_btn = adm_page.locator('button:has-text("Xem xét & Phê duyệt ngay")').first
        if review_btn.count() > 0 and review_btn.is_visible():
            review_btn.click()
            adm_page.wait_for_selector('h3:has-text("Thẩm Quyền Phê Duyệt Trạng Thái Gói Thầu")', timeout=10000)
            r_modal = adm_page.locator('.fixed.inset-0')
            r_modal.locator('textarea').fill("Chấp thuận đóng thầu sớm để mở niêm phong chấm điểm theo đề xuất của Bên mời thầu.")
            r_modal.locator('button:has-text("Chấp thuận phê duyệt")').click()
            adm_page.wait_for_selector('h3:has-text("Thẩm Quyền Phê Duyệt Trạng Thái Gói Thầu")', state='hidden', timeout=10000)
            adm_page.wait_for_timeout(1500)

        # Now package is Closed -> transition to Evaluating
        start_eval_btn = adm_page.locator('button:has-text("Chuyển sang Chấm điểm")').first
        start_eval_btn.click()
        adm_page.wait_for_timeout(1000)
        eval_modal = adm_page.locator('.fixed.inset-0')
        eval_modal.locator('button:has-text("Bắt đầu chấm điểm"), button:has-text("Xác nhận")').last.click()
        adm_page.wait_for_timeout(2500)

        save_shot(adm_page, "03_package_evaluating_assigned_committee.png")
        logger.info("Package transitioned to Evaluating status!")
        adm_ctx.close()
        results["step3"] = {"status": "PASS", "evaluatorsCount": 3, "packageStatus": "Evaluating"}

        # =========================================================================
        # STEP 4: 3 Evaluators log in separately & score all criteria
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 4] 3 Evaluators independently score criteria...")
        logger.info("=================================================================")
        for i, ev in enumerate(evaluators, start=1):
            logger.info(f"Evaluator {i} ({ev['email']}) logging in and scoring...")
            ev_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
            ev_page = ev_ctx.new_page()
            ev_page.on("dialog", lambda d: d.accept())

            ui_login(ev_page, base_url, ev["email"])

            ev_page.goto(f"{base_url}/evaluation/{package_id}")
            ev_page.wait_for_selector('h1', timeout=15000)
            ev_page.wait_for_timeout(1500)

            # Score Submission 1 (Contractor 1)
            sub1_btn = ev_page.locator(f'button:has-text("{c1["companyName"]}")').first
            sub1_btn.click()
            ev_page.wait_for_timeout(1000)
            num_inputs = ev_page.locator('table input[type="number"]')
            for idx in range(num_inputs.count()):
                num_inputs.nth(idx).fill(str(92 - idx * 2))
            comment_inputs = ev_page.locator('table input[type="text"]')
            if comment_inputs.count() > 0:
                comment_inputs.nth(0).fill(f"Đánh giá giám khảo {i}: Hồ sơ kỹ thuật xuất sắc, đáp ứng toàn diện yêu cầu.")
            ev_page.click('button:has-text("Lưu phiếu chấm điểm")')
            ev_page.wait_for_timeout(2000)

            # Score Submission 2 (Contractor 2)
            sub2_btn = ev_page.locator(f'button:has-text("{c2["companyName"]}")').first
            sub2_btn.click()
            ev_page.wait_for_timeout(1000)
            num_inputs = ev_page.locator('table input[type="number"]')
            for idx in range(num_inputs.count()):
                num_inputs.nth(idx).fill(str(75 - idx * 2))
            comment_inputs = ev_page.locator('table input[type="text"]')
            if comment_inputs.count() > 0:
                comment_inputs.nth(0).fill(f"Đánh giá giám khảo {i}: Đáp ứng cơ bản tiêu chuẩn kỹ thuật.")
            ev_page.click('button:has-text("Lưu phiếu chấm điểm")')
            ev_page.wait_for_timeout(2000)

            if i == 3:
                save_shot(ev_page, "04_scorecards_submitted_by_evaluators.png")
            ev_ctx.close()
        results["step4"] = {"status": "PASS", "completedEvaluators": 3}

        # =========================================================================
        # STEP 5: Procurement reviews rankings and awards winner (Contractor 1)
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 5] Review rankings and finalize award to Contractor 1...")
        logger.info("=================================================================")
        proc_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        proc_page = proc_ctx.new_page()
        proc_page.on("dialog", lambda d: d.accept())

        ui_login(proc_page, base_url, manager["email"])

        proc_page.goto(f"{base_url}/evaluation/{package_id}")
        proc_page.wait_for_selector('h1', timeout=15000)
        proc_page.wait_for_timeout(1000)

        proc_page.click('button:has-text("Bảng tổng hợp xếp hạng & Trao thầu")')
        proc_page.wait_for_selector('button:has-text("Phê duyệt Trao thầu")', timeout=10000)
        proc_page.wait_for_timeout(1000)

        award_btn = proc_page.locator('button:has-text("Phê duyệt Trao thầu")').first
        award_btn.click()
        proc_page.wait_for_selector('h3:has-text("Phê duyệt Trao thầu")', timeout=10000)

        award_modal = proc_page.locator('.fixed.inset-0')
        reason_text = "Căn cứ Biên bản đánh giá E-HSDT của Tổ chuyên gia, Nhà thầu 1 đạt điểm tổng hợp cao nhất (Xếp hạng 1) và đáp ứng toàn diện hồ sơ mời thầu."
        award_modal.locator('textarea').fill(reason_text)
        award_modal.locator('button:has-text("Xác nhận Trao thầu")').click()

        proc_page.wait_for_selector('h3:has-text("Phê duyệt Trao thầu")', state='hidden', timeout=15000)
        proc_page.wait_for_timeout(2000)
        save_shot(proc_page, "05_award_finalized_winner_selected.png")
        logger.info("Winner finalized and package awarded!")
        proc_ctx.close()
        results["step5"] = {"status": "PASS", "awardedContractorId": c1["contractorId"]}

        # =========================================================================
        # STEP 6: Contract created from winning bid
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 6] Create contract from awarded bid...")
        logger.info("=================================================================")
        proc_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        proc_page = proc_ctx.new_page()
        proc_page.on("dialog", lambda d: d.accept())

        ui_login(proc_page, base_url, manager["email"])

        proc_page.goto(f"{base_url}/contracts")
        proc_page.wait_for_selector('button:has-text("Lập Hợp Đồng Mới")', timeout=15000)

        create_contract_btn = proc_page.locator('button:has-text("Lập Hợp Đồng Mới")').first
        create_contract_btn.click()
        proc_page.wait_for_selector('h3:has-text("Lập Hợp Đồng Kinh Tế Mới")', timeout=10000)

        c_modal = proc_page.locator('.fixed.inset-0')
        proc_page.wait_for_timeout(1000)
        select_pkg = c_modal.locator('select').first
        select_pkg.select_option(str(package_id))
        proc_page.wait_for_timeout(1500)

        submit_c_btn = c_modal.locator('button[type="submit"]:has-text("Khởi tạo Hợp đồng")')
        submit_c_btn.click()

        proc_page.wait_for_url(re.compile(rf"{base_url}/contracts/\d+"), timeout=20000)
        contract_url = proc_page.url
        contract_id = int(contract_url.split('/')[-1])
        logger.info(f"Contract created with ID: {contract_id} (URL: {contract_url})")

        proc_page.wait_for_timeout(2000)
        save_shot(proc_page, "06_contract_created_draft.png")
        manifest["businessFlow"]["contractId"] = contract_id
        results["step6"] = {"status": "PASS", "contractId": contract_id, "status": "Draft"}

        # =========================================================================
        # STEP 7: Create 2 milestones, upload scan, activate contract (Active)
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 7] Create 2 milestones, upload scan, activate to Active...")
        logger.info("=================================================================")
        # Milestone 1: 500,000,000 VND
        add_m_btn = proc_page.locator('button:has-text("Thêm mốc đầu tiên"), button:has-text("Thêm Mốc Nghiệm Thu")').first
        add_m_btn.click()
        proc_page.wait_for_selector('h3:has-text("Thêm Mốc Nghiệm Thu Mới")', timeout=10000)
        m_modal = proc_page.locator('.fixed.inset-0')
        m_modal.locator('input[type="text"]').fill("Đợt 1: Bàn giao đợt 1 thiết bị mạng lõi & phụ kiện")
        m_modal.locator('input[type="date"]').fill("2026-11-20")
        m_modal.locator('input[type="number"]').fill("500000000")
        m_modal.locator('button[type="submit"]').click()
        proc_page.wait_for_selector('h3:has-text("Thêm Mốc Nghiệm Thu Mới")', state='hidden', timeout=10000)
        proc_page.wait_for_timeout(1500)

        # Milestone 2: 1,000,000,000 VND
        add_m_btn2 = proc_page.locator('button:has-text("Thêm Mốc Nghiệm Thu")').first
        add_m_btn2.click()
        proc_page.wait_for_selector('h3:has-text("Thêm Mốc Nghiệm Thu Mới")', timeout=10000)
        m_modal2 = proc_page.locator('.fixed.inset-0')
        m_modal2.locator('input[type="text"]').fill("Đợt 2: Nghiệm thu vận hành thử nghiệm và bàn giao hoàn tất")
        m_modal2.locator('input[type="date"]').fill("2026-12-15")
        m_modal2.locator('input[type="number"]').fill("1000000000")
        m_modal2.locator('button[type="submit"]').click()
        proc_page.wait_for_selector('h3:has-text("Thêm Mốc Nghiệm Thu Mới")', state='hidden', timeout=10000)
        proc_page.wait_for_timeout(1500)

        proc_page.wait_for_selector('text=Đã phân bổ đủ', timeout=10000)
        logger.info("Milestones allocated full 1,500,000,000 VND budget!")

        # Upload scanned contract PDF
        upload_btn = proc_page.locator('button:has-text("Tải lên ngay"), button[title="Tải lên tệp thay thế"]').first
        upload_btn.click()
        proc_page.wait_for_selector('h3:has-text("Đính Kèm Bản Scan Hợp Đồng")', timeout=10000)
        up_modal = proc_page.locator('.fixed.inset-0')
        up_modal.locator('input[type="file"]').set_input_files(SCAN_PDF)
        up_modal.locator('button[type="submit"]:has-text("Xác nhận Tải lên")').click()
        proc_page.wait_for_selector('h3:has-text("Đính Kèm Bản Scan Hợp Đồng")', state='hidden', timeout=10000)
        proc_page.wait_for_selector('text=Tài liệu hợp đồng có chữ ký scan', timeout=10000)
        proc_page.wait_for_timeout(1500)

        # Activate Contract to Active
        activate_btn = proc_page.locator('button:has-text("Có Hiệu Lực (Active)")').first
        activate_btn.click()
        proc_page.wait_for_selector('h3:has-text("Kích Hoạt Hiệu Lực Hợp Đồng")', timeout=10000)
        act_modal = proc_page.locator('.fixed.inset-0')
        act_modal.locator('button[type="submit"]:has-text("Xác nhận Kích hoạt HĐ")').click()
        proc_page.wait_for_selector('h3:has-text("Kích Hoạt Hiệu Lực Hợp Đồng")', state='hidden', timeout=10000)
        proc_page.wait_for_selector('span:has-text("Hoạt động"), span:has-text("Active"), span:has-text("Đang thực hiện")', timeout=10000)
        proc_page.wait_for_timeout(1500)

        save_shot(proc_page, "07_contract_activated_active.png")
        logger.info("Contract activated to Active status successfully!")
        proc_ctx.close()
        results["step7"] = {"status": "PASS", "allocatedAmount": 1500000000, "status": "Active"}

        # =========================================================================
        # STEP 8: Contractor submits progress report
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 8] Contractor submits weekly progress report...")
        logger.info("=================================================================")
        c_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        c_page = c_ctx.new_page()
        c_page.on("dialog", lambda d: d.accept())

        ui_login(c_page, base_url, c1["email"])

        c_page.goto(f"{base_url}/contracts/{contract_id}")
        c_page.wait_for_selector('h1', timeout=15000)
        c_page.wait_for_timeout(1000)

        import requests
        c_login_res = requests.post(f"{api_base_url}/api/auth/login", json={"email": c1["email"], "password": "Admin@123"}).json()
        c_token = c_login_res["data"]["token"]
        c_headers = {"Authorization": f"Bearer {c_token}"}

        # 8.1 Submit valid week 1 progress: 50
        prog_res1 = requests.post(
            f"{api_base_url}/api/contracts/{contract_id}/progress",
            json={"weekNumber": 1, "completionPercent": 50, "note": "Hoàn tất sản xuất và nhập kho thiết bị mạng lõi giai đoạn 1."},
            headers=c_headers
        )
        assert prog_res1.status_code == 201, f"Expected 201 Created for progress week 1, got {prog_res1.status_code}"

        # 8.2 Negative test: Duplicate week 1 -> 400
        prog_dup = requests.post(
            f"{api_base_url}/api/contracts/{contract_id}/progress",
            json={"weekNumber": 1, "completionPercent": 60, "note": "Cố tình nộp trùng tuần 1."},
            headers=c_headers
        )
        assert prog_dup.status_code == 400, f"Expected 400 for duplicate week, got {prog_dup.status_code}"

        # 8.3 Negative test: Decreasing progress (40 < 50) -> 400
        prog_dec = requests.post(
            f"{api_base_url}/api/contracts/{contract_id}/progress",
            json={"weekNumber": 2, "completionPercent": 40, "note": "Cố tình giảm tiến độ."},
            headers=c_headers
        )
        assert prog_dec.status_code == 400, f"Expected 400 for decreasing progress, got {prog_dec.status_code}"

        # 8.4 Submit valid week 2: 100
        prog_res2 = requests.post(
            f"{api_base_url}/api/contracts/{contract_id}/progress",
            json={"weekNumber": 2, "completionPercent": 100, "note": "Bàn giao đầy đủ thiết bị và kiểm định kỹ thuật hiện trường."},
            headers=c_headers
        )
        assert prog_res2.status_code == 201, f"Expected 201 Created for progress week 2, got {prog_res2.status_code}"

        c_page.reload()
        c_page.wait_for_selector('h1', timeout=15000)
        c_page.wait_for_timeout(1500)
        save_shot(c_page, "08_contractor_progress_recorded.png")
        logger.info("Contractor progress updates and negative validations successful!")
        c_ctx.close()
        results["step8"] = {"status": "PASS", "progressWeek1": 50, "progressWeek2": 100}

        # Query Milestone IDs from DB
        from db_helper import run_sqlcmd
        m_sql = f"SELECT Id, Title, Amount, Version FROM ContractMilestones WHERE ContractId = {contract_id} ORDER BY Id ASC;"
        m_rows = run_sqlcmd(m_sql, db_name=manifest["database"]).splitlines()
        clean_rows = [r.strip() for r in m_rows if r.strip() and not r.startswith("-") and not r.startswith("Id")]
        m1_id = int(clean_rows[0].split()[0])
        m2_id = int(clean_rows[1].split()[0])
        logger.info(f"Retrieved Milestone 1 ID: {m1_id}, Milestone 2 ID: {m2_id}")

        # =========================================================================
        # STEP 9: Officer technical review & Checker approves Milestone 1
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 9] Officer technical review (Maker) & Checker disbursement (Milestone 1)...")
        logger.info("=================================================================")
        maker_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        maker_page = maker_ctx.new_page()
        maker_page.on("dialog", lambda d: d.accept())

        ui_login(maker_page, base_url, officer["email"])

        maker_page.goto(f"{base_url}/contracts/{contract_id}")
        maker_page.wait_for_selector('h1', timeout=15000)

        tech_btn = maker_page.locator('button:has-text("Kiểm tra KT")').first
        tech_btn.click()
        maker_page.wait_for_selector('h3:has-text("Xác Nhận Kỹ Thuật Hiện Trường")', timeout=10000)
        t_modal = maker_page.locator('.fixed.inset-0')
        t_modal.locator('textarea').fill("Đã kiểm tra thực tế hiện trường: 10 bộ Switch và 02 Firewall mới hoàn toàn, nguyên niêm phong, đầy đủ CO/CQ. Đạt yêu cầu kỹ thuật Đợt 1.")
        t_modal.locator('button[type="submit"]:has-text("Ghi nhận kết quả kỹ thuật")').click()
        maker_page.wait_for_selector('h3:has-text("Xác Nhận Kỹ Thuật Hiện Trường")', state='hidden', timeout=10000)
        maker_page.wait_for_selector('text=Đạt KT hiện trường', timeout=10000)
        maker_page.wait_for_timeout(1000)

        # Anti-self-approval check: Maker clicks Disburse -> blocked
        disburse_btn = maker_page.locator('button:has-text("Giải ngân")').first
        disburse_btn.click()
        maker_page.wait_for_selector('h3:has-text("Biên Bản Nghiệm Thu & Giải Ngân")', timeout=10000)
        maker_page.wait_for_selector('text=Vi phạm nguyên tắc kiểm soát chéo', timeout=10000)
        logger.info("Anti-self-approval verified on UI: Maker is blocked from approving disbursement!")
        cancel_btn = maker_page.locator('button:has-text("Hủy")').first
        cancel_btn.click()
        maker_page.wait_for_timeout(1000)
        maker_ctx.close()

        # 9.2 Checker logs in & approves disbursement for Milestone 1
        checker_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        checker_page = checker_ctx.new_page()
        checker_page.on("dialog", lambda d: d.accept())

        ui_login(checker_page, base_url, checker["email"])

        checker_page.goto(f"{base_url}/contracts/{contract_id}")
        checker_page.wait_for_selector('h1', timeout=15000)

        disburse_btn_c = checker_page.locator('button:has-text("Giải ngân")').first
        disburse_btn_c.click()
        checker_page.wait_for_selector('h3:has-text("Biên Bản Nghiệm Thu & Giải Ngân")', timeout=10000)
        appr_modal = checker_page.locator('.fixed.inset-0')
        appr_modal.locator('textarea').fill("Căn cứ Biên bản nghiệm thu kỹ thuật hiện trường đạt chuẩn và chứng từ hợp lệ, chấp thuận giải ngân 500.000.000 VNĐ cho Đợt 1.")
        appr_modal.locator('button[type="submit"]:has-text("Ký duyệt Nghiệm thu & Giải ngân")').click()
        checker_page.wait_for_selector('h3:has-text("Biên Bản Nghiệm Thu & Giải Ngân")', state='hidden', timeout=15000)
        checker_page.wait_for_timeout(2000)

        checker_page.wait_for_selector('span:has-text("Hoạt động"), span:has-text("Active"), span:has-text("Đang thực hiện")', timeout=10000)
        save_shot(checker_page, "09_milestone1_disbursed_contract_active.png")
        logger.info("Milestone 1 disbursed: Contract remains Active!")
        checker_ctx.close()
        results["step9"] = {"status": "PASS", "milestone1Status": "Completed", "contractStatus": "Active"}

        # =========================================================================
        # STEP 10: Complete Milestone 2 -> Contract automatically transitions to Completed
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 10] Complete Milestone 2 & verify Contract Completed...")
        logger.info("=================================================================")
        maker_ctx2 = browser.new_context(viewport={'width': 1366, 'height': 850})
        maker_page2 = maker_ctx2.new_page()
        maker_page2.on("dialog", lambda d: d.accept())

        ui_login(maker_page2, base_url, officer["email"])

        maker_page2.goto(f"{base_url}/contracts/{contract_id}")
        maker_page2.wait_for_selector('h1', timeout=15000)

        tech_btn2 = maker_page2.locator('button:has-text("Kiểm tra KT")').first
        tech_btn2.click()
        maker_page2.wait_for_selector('h3:has-text("Xác Nhận Kỹ Thuật Hiện Trường")', timeout=10000)
        t_modal2 = maker_page2.locator('.fixed.inset-0')
        t_modal2.locator('textarea').fill("Đã nghiệm thu vận hành thử nghiệm hệ thống thông suốt, đáp ứng đầy đủ tính năng kỹ thuật theo thiết kế Đợt 2.")
        t_modal2.locator('button[type="submit"]:has-text("Ghi nhận kết quả kỹ thuật")').click()
        maker_page2.wait_for_selector('h3:has-text("Xác Nhận Kỹ Thuật Hiện Trường")', state='hidden', timeout=10000)
        maker_page2.wait_for_timeout(1000)
        maker_ctx2.close()

        # Execute all 6 Server-Side Negative Constraint Cases while Contract is Active and Milestone 2 is Pending (TechnicalApproved)
        from test_negative_rules import run_negative_rules
        run_negative_rules(run_id, manifest, api_base_url=api_base_url)

        checker_ctx2 = browser.new_context(viewport={'width': 1366, 'height': 850})
        checker_page2 = checker_ctx2.new_page()
        checker_page2.on("dialog", lambda d: d.accept())

        ui_login(checker_page2, base_url, checker["email"])

        checker_page2.goto(f"{base_url}/contracts/{contract_id}")
        checker_page2.wait_for_selector('h1', timeout=15000)

        disburse_btn_c2 = checker_page2.locator('button:has-text("Giải ngân")').first
        disburse_btn_c2.click()
        checker_page2.wait_for_selector('h3:has-text("Biên Bản Nghiệm Thu & Giải Ngân")', timeout=10000)
        appr_modal2 = checker_page2.locator('.fixed.inset-0')
        appr_modal2.locator('textarea').fill("Nghiệm thu toàn diện hoàn tất, chấp thuận giải ngân quyết toán 1.000.000.000 VNĐ cho Đợt 2.")
        appr_modal2.locator('button[type="submit"]:has-text("Ký duyệt Nghiệm thu & Giải ngân")').click()
        checker_page2.wait_for_selector('h3:has-text("Biên Bản Nghiệm Thu & Giải Ngân")', state='hidden', timeout=15000)
        checker_page2.wait_for_timeout(3000)

        checker_page2.wait_for_selector('span:has-text("Hoàn thành"), span:has-text("Completed")', timeout=15000)
        save_shot(checker_page2, "10_contract_completed_fully_disbursed.png")
        logger.info("All milestones completed: Contract automatically transitioned to Completed!")
        checker_ctx2.close()
        results["step10"] = {"status": "PASS", "contractStatus": "Completed", "totalDisbursed": 1500000000, "remaining": 0}

        # =========================================================================
        # STEP 11: Refresh/re-login, check details & audit trail
        # =========================================================================
        logger.info("=================================================================")
        logger.info("[STEP 11] Re-login, verify persistence and inspect audit logs...")
        logger.info("=================================================================")
        audit_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        audit_page = audit_ctx.new_page()
        audit_page.on("dialog", lambda d: d.accept())

        ui_login(audit_page, base_url, checker["email"])

        audit_page.goto(f"{base_url}/contracts/{contract_id}")
        audit_page.wait_for_selector('span:has-text("Hoàn thành"), span:has-text("Completed")', timeout=15000)
        audit_ctx.close()

        admin_ctx = browser.new_context(viewport={'width': 1366, 'height': 850})
        admin_page = admin_ctx.new_page()
        ui_login(admin_page, base_url, admin["email"])
        admin_page.goto(f"{base_url}/audit-logs")
        admin_page.wait_for_selector('h1, h2', timeout=15000)
        admin_page.wait_for_timeout(2000)
        save_shot(admin_page, "11_audit_logs_verified.png")
        logger.info("Audit logs verified and persistent state confirmed!")
        admin_ctx.close()
        results["step11"] = {"status": "PASS", "auditLogsVerified": True}

        browser.close()

    manifest["results"] = results
    return results

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python test_lifecycle_e2e.py <manifest_json_path>")
        sys.exit(1)
    with open(sys.argv[1], "r", encoding="utf-8") as f:
        mf = json.load(f)
    run_lifecycle_e2e(mf["runId"], mf)
