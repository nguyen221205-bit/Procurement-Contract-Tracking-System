# Antigravity Procurement Multi-Agent V2

Bộ cấu hình: 1 primary Orchestrator + 10 Custom Subagents + 1 always-on rule.

## Cài đặt
1. Giải nén và sao chép thư mục `.agents/` vào ROOT của repository `Procurement-Contract-Tracking-System` (không vào `frontend/`).
2. Bảo đảm có bản `dev` hiện tại; kiểm tra `git status` trước khi chỉnh sửa.
3. Trong Antigravity (phiên bản hỗ trợ Custom Subagents), mở `/agents` và chọn `orchestrator` làm primary agent.
4. Thử task read-only trước: “Chỉ phân tích trên dev quy trình giám khảo được phân công và chấm điểm; không sửa code.”
5. Đối với coding agent, yêu cầu Orchestrator dùng worktree `branch` khi gọi song song.

## Các file
- `.agents/agents/orchestrator.md` — pro, primary.
- `.agents/agents/repository-analyst.md` — flash.
- `.agents/agents/business-analyst.md` — pro.
- `.agents/agents/backend-engineer.md` — pro.
- `.agents/agents/database-engineer.md` — pro.
- `.agents/agents/frontend-engineer.md` — flash.
- `.agents/agents/api-integration.md` — flash.
- `.agents/agents/security-auditor.md` — pro.
- `.agents/agents/background-jobs-engineer.md` — pro.
- `.agents/agents/qa-engineer.md` — flash.
- `.agents/agents/code-reviewer.md` — pro.
- `.agents/rules/procurement-project.md` — always_on, áp dụng chung.

## Chú ý
- Frontmatter dùng các tier `flash`/`pro`/`inherit`, không gắn tên model không được Antigravity hỗ trợ.
- Không cố định `tools` để tránh lỗi với các tool name không hỗ trợ trong workspace cụ thể; dùng quyền mặc định của agent và policy `sandbox`.
- Số lượng agent trong bộ config không phải số lượng agent phải khởi chạy cho mỗi nhiệm vụ.
- Đây là cấu hình prompt, chưa được test thực tế trong Antigravity/SQL Server; xác minh bằng task thử và kiểm thử theo môi trường của bạn.
