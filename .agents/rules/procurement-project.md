---
trigger: always_on
description: Mandatory repository rules for Antigravity agents working on the dev branch of Procurement & Contract Tracking System.
---
# Procurement Repository Policy
- Source-of-truth branch for feature work: `dev`; record baseline SHA. Do not silently use `main`.
- Respect user working changes. No destructive Git commands, automatic merge, push to protected branches, deployment or production DB migrations without explicit approval.
- Prefer isolated worktrees for parallel code modifications; no concurrent writers to the same file.
- Read code, DTO and existing scripts before changing behavior. README may be outdated.
- Apply server-side authorization, confidentiality of sealed bids, domain state transitions, and integrity of financial/milestone operations.
- Report real test evidence and classify PASS/FAIL/BLOCKED/NOT RUN distinctly.
- Responses and handoff reports in Vietnamese. Code names in original spelling.
