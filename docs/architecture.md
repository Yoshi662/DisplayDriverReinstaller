# Reinstallation architecture and safety

## Status and boundaries

This is a proposed design, not implemented behavior. The intended target is Windows 10 and 11 x64 with an AMD GPU. DDU and the AMD installer are provided through explicit local paths; the application must not download programs. Validate paths and the selected files before any disruptive operation. Do not include or redistribute either vendor's software without independently verifying its terms.

## Intended lifecycle

1. Preflight: detect relevant drivers and prerequisites, and validate both local tools and system readiness.
2. Record recovery information and transition into Safe Mode.
3. Resume and execute DDU, checking its actual supported automation and outcome.
4. Restore the intended normal boot configuration and restart into normal Windows.
5. Resume and run the AMD installer, checking its supported automation and outcome.
6. Verify the installed driver and clean up application-owned tasks and transient state.

The original request's phrase "final reinstall and cleanup" needs clarification before specifying an extra reinstall or reboot. Vendor-required restarts and verification must be researched, not inferred.

## State and recovery proposal

Model each transition explicitly in a versioned, durable state record. Persist an intent before disruptive work and a verified result afterward; record enough information to detect an interrupted transition and resume without repeating an unsafe operation. Protect state and scheduled-task configuration from unprivileged modification, enforce a single active workflow, and make cleanup idempotent and limited to application-owned resources. Record the original boot configuration and any changes before changing it, and verify each transition against actual system state.

If a failure occurs in Safe Mode, the agreed policy is to remain there until the user intervenes. Do not automatically loop or report success. The later implementation must provide a documented, tested operator recovery procedure that can restore the original boot configuration even when the application cannot resume. Preserve diagnostics without secrets and make incomplete operations visible.

## Unresolved feasibility and security questions

- Identify supported DDU and AMD installer versions, vendor terms, documented unattended arguments, exit codes, restart requirements, and verifiable success signals.
- Determine whether the needed Task Scheduler service and task triggers run in the selected Safe Mode configuration, and whether DDU or the installer requires an interactive desktop. A startup task running as a service is not equivalent to a logged-on desktop.
- Compare elevated startup tasks, interactive logon tasks, and explicitly approved auto-logon: credentials, persistence, least privilege, exposure, desktop availability, reliability, and cleanup. No approach has been chosen; do not store credentials in workflow state or logs.
- Determine how to validate local executables, version compatibility and authenticity, handle untrusted paths and permissions, and prevent replacement between validation and execution.
- Specify existing boot settings, encryption/recovery-key implications, power loss, partial completion, concurrent launches, driver-install failures, and recovery without network access.
- Confirm supported AMD devices and Windows editions, including the Windows 10 support lifecycle.

## Verification strategy

Use deterministic tests and isolated fakes for state transitions, file validation, processes, scheduler, and boot operations. CI must not perform real driver or boot changes. Research VM limitations before relying on one for Safe Mode or driver behavior. Real end-to-end validation requires a dedicated, recoverable AMD test machine, which is not currently available; no production-readiness claim can be made without it.
