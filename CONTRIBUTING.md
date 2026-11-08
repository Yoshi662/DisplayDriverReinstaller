# Contributing

## Workflow

Use trunk-based development: start a short-lived branch from `main`, link the work to an issue, and open a pull request to `main`. PRs require the `Build and test` CI check and no mandatory second reviewer. Do not push directly to `main` once the intended protection is available.

The private repository currently uses GitHub Free, which does not make the requested branch protection available. Until an eligible plan or another approved mechanism is available, these rules are a **manual convention, not enforced protection**. Repository bootstrap is the exception: the repository had to exist before its first tickets or CI checks could exist.

Before starting work, an issue should state the problem, scope and exclusions, dependencies, safety concerns, and observable acceptance criteria. Research issues should specify the evidence needed to make a decision. A change is done when acceptance criteria and relevant tests pass, documentation and safety impacts are updated, and the PR links the issue. Never put secrets or personal machine details in issues, logs, or artifacts.

Use the issue forms and PR template. If issue creation is unavailable, preserve proposed work as clearly marked drafts; do not claim a draft is a GitHub issue. See [backlog drafts](docs/backlog.md).

## Local checks

Follow the restore, build, and test commands in the [README](README.md). Add focused tests for behavior changes. CI must never change boot settings, invoke DDU, install drivers, or require physical GPU access. Pin and centrally manage dependencies in `Directory.Packages.props`; keep GitHub Actions pinned to reviewed commit SHAs.

## Releases

No release workflow or published binary exists yet. A future release ticket must define versioning, reproducible Windows builds, signing, artifact provenance, release notes, and manual approval before any public binary is distributed. Continue to honor the runtime no-download policy.
