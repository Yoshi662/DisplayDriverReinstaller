# Agent instructions

- Read `CONTRIBUTING.md` for issue, branch, review, and validation workflow.
- Read `docs/architecture.md` before changing workflow, boot, scheduling, DDU, or installer behavior.
- Preserve the runtime no-download policy. Never run actual driver removal, installation, or boot changes in automated tests or CI.
- Do not add credentials or machine-specific personal data to source, logs, issues, or artifacts.
- Implement only behavior supported by a scoped issue and its acceptance criteria; keep design uncertainties explicit.
