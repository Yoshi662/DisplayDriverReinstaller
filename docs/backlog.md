# Backlog drafts

These are **drafts, not GitHub issues**. Creating issues was blocked by the application's GitHub account link. Once that is repaired, create the foundation and application issues with the forms, replace the dependencies below with issue links, and track their status in GitHub. Priorities reflect sequencing, not time estimates.

## Foundation drafts

- **Governance:** provide feature, bug, and research forms, a PR template, contribution and security guidance, and a prioritized issue backlog. Record the repository bootstrap exception.
- **.NET foundation:** provide a .NET 10 solution, minimal safe CLI, centrally managed dependencies, and meaningful automated tests. No driver or boot behavior.
- **CI and protection:** run Windows restore, build, and tests; require PRs and the stable CI check on `main`. Document administrative bypass.
- **Documentation:** provide human and agent instructions, a future release process, and architecture, safety, and recovery design with uncertainties clearly identified.

## Application drafts

### P0: Research unattended feasibility and vendor support

**Dependencies:** none.

**Acceptance:** identify version-specific primary sources for DDU and AMD automation, licensing, arguments, exit codes, restart requirements and success signals; determine Safe Mode scheduling and desktop/session requirements; compare startup and explicitly approved auto-logon approaches, including security and cleanup; record a decision or unresolved blockers. No system-changing experiments without a recoverable environment.

### P0: Define safe boot and operator recovery

**Dependencies:** unattended feasibility research.

**Acceptance:** specify the supported boot transitions, original-configuration preservation, failure handling and a tested operator recovery procedure; remain in Safe Mode on unexpected failure until user intervention. Document encryption/recovery-key, power-loss, and inability-to-resume cases. Testing requires a recoverable environment.

### P0: Define state, checkpointing, and resume

**Dependencies:** unattended feasibility research and boot/recovery design.

**Acceptance:** define versioned state and transition invariants, atomic persistence and access controls, single-run exclusion, crash/reboot reconciliation, and idempotent handling; implement and test the agreed state model without actual boot or driver changes.

### P1: Validate prerequisites and supplied local tools

**Dependencies:** unattended feasibility research.

**Acceptance:** verify supported OS and AMD driver/device preconditions, explicit local DDU and installer paths, supported versions and documented authenticity criteria; reject invalid or unsafe inputs before disruptive operations and show actionable errors. Cover failure cases without downloading programs.

### P1: Implement elevated scheduling and resume

**Dependencies:** unattended feasibility research, boot/recovery design, and state/resume.

**Acceptance:** implement only the chosen and approved session mechanism; restrict access, avoid credential persistence, register and remove only owned tasks, prevent duplicate runs, and test failure and cleanup behavior using isolated fakes. Demonstrate platform behavior in a recoverable environment before integration.

### P1: Implement Safe Mode transition

**Dependencies:** boot/recovery design, state/resume, and scheduling/resume.

**Acceptance:** preserve and verify original boot configuration, enter only the researched supported mode, checkpoint before restart, and provide operator recovery on failure; exercise interruption and error cases with fakes and a recoverable environment.

### P1: Invoke DDU

**Dependencies:** prerequisite validation, Safe Mode transition, and vendor feasibility research.

**Acceptance:** invoke a supported user-supplied DDU version using verified supported arguments and session conditions; interpret documented results, surface failures, and checkpoint without re-running blindly. Test process behavior with fakes before controlled vendor testing.

### P1: Return to normal Windows

**Dependencies:** DDU invocation, state/resume, and boot/recovery design.

**Acceptance:** restore and verify the intended original normal-boot configuration, checkpoint and restart safely, resume in the required session, and handle interruption without a reboot loop. Test using fakes and a recoverable environment.

### P1: Invoke the AMD installer

**Dependencies:** prerequisite validation, normal-boot transition, and vendor feasibility research.

**Acceptance:** run the user-supplied supported installer with documented unattended options, interpret exit and restart requirements, and record success or actionable failure. Cover process outcomes and interruption using fakes; perform vendor testing only in a recoverable environment.

### P1: Verify completion and clean up

**Dependencies:** AMD installer invocation and state/resume.

**Acceptance:** verify the installed AMD driver against explicit, researched criteria; clean only application-owned tasks and transient state; preserve useful sanitized diagnostics; do not report success when verification or cleanup fails. Resolve whether an additional final reinstall or reboot is required before implementing one.

### P1: Add diagnostics and observability

**Dependencies:** state/resume design.

**Acceptance:** define structured, privacy-conscious progress and error reporting across restarts, retained diagnostics, and actionable recovery messages; exclude credentials, secrets, and unnecessary personal data. Test error and retention behavior.

### P0: Plan safe integration and hardware validation

**Dependencies:** unattended feasibility research and boot/recovery design; end-to-end execution additionally depends on all workflow implementation issues.

**Acceptance:** define an isolated fake-based integration suite and a recoverable VM test matrix, document VM limitations and rollback, and establish dedicated AMD hardware and recovery prerequisites before real end-to-end validation. Track the absent dedicated hardware as a blocker; never run destructive operations in CI.
