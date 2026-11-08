# Contributing

## Workflow

Use trunk-based development: start a short-lived branch from `main`, link the work to an issue, and open a pull request to `main`. PRs require the `Build and test` CI check and no mandatory second reviewer.

Repository bootstrap is the exception: the repository had to exist before its first tickets or CI checks could exist. Administrators can bypass the branch protection; follow the PR workflow even when bypass is available.

Before starting work, an issue should state the problem, scope and exclusions, dependencies, safety concerns, and observable acceptance criteria. Research issues should specify the evidence needed to make a decision. A change is done when acceptance criteria and relevant tests pass, documentation and safety impacts are updated, and the PR links the issue. Never put secrets or personal machine details in issues, logs, or artifacts.

Use the issue forms and PR template. If issue creation is unavailable, preserve proposed work as clearly marked drafts; do not claim a draft is a GitHub issue. See [backlog drafts](docs/backlog.md).

## Local checks

Follow the restore, build, and test commands in the [README](README.md). Add focused tests for behavior changes. CI must never change boot settings, invoke DDU, install drivers, or require physical GPU access. Pin and centrally manage dependencies in `Directory.Packages.props`; keep GitHub Actions pinned to reviewed commit SHAs.

## Releases

No release workflow or published binary exists yet. A future release ticket must define versioning, reproducible Windows builds, signing, artifact provenance, release notes, and manual approval before any public binary is distributed. Continue to honor the runtime no-download policy.
