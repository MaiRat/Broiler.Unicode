# Human review: Broiler.Unicode

> **Status: APPROVED WITH CONDITIONS for first preview use.**

Broiler.Unicode contains substantial AI-assisted implementation. This record documents
the human review decision for the specific revision below. Approval is scoped to the
first preview and does not claim that the component is free of defects or
vulnerabilities. The Apache License 2.0 warranty disclaimer still applies.

## Review target

- **Component:** Broiler.Unicode
- **Scope:** Unicode property, emoji string-property, CLDR locale-data libraries, generators, data tools, and tests used through this nested checkout.
- **Release:** First preview
- **Commit:** `89f134ed45f1baaf2bc00a86fb5319ac3ee5befe`
- **Reviewer:** Maik Ratzmer
- **Reviewer contact or profile:** MaiRat
- **Review date:** 2026-07-01
- **Intended preview use:** First preview integration and evaluation of Broiler.Unicode for Unicode property lookup, emoji string-property matching, CLDR locale-data helpers, and the associated developer-operated generation/data tooling. Runtime use is limited to committed/generated data.

Any source change after the reviewed commit invalidates this approval until the changed
revision is reviewed again.

## Required evidence

- [x] Build and automated tests completed; minimum expected command: `dotnet test UnicodeEmoji.StringProperties.slnx`.
- [x] Security-sensitive inputs, trust boundaries, file/network access, native interop, and code-execution paths were inspected where applicable.
- [x] Dependency and license notices were checked, including inherited upstream code.
- [x] AI-generated or AI-modified code received source-level review; no AI summary was accepted as a substitute for reading the relevant code.
- [x] Public APIs, failure behavior, known limitations, and preview compatibility risks were assessed.
- [x] Static analysis, dependency/vulnerability scanning, or an explicit reason for omitting each was recorded.
- [x] Open findings and residual risks are listed below.

### Evidence and commands

- `git rev-parse HEAD`
  - Reviewed commit: `89f134ed45f1baaf2bc00a86fb5319ac3ee5befe`.
- `dotnet --info`
  - .NET SDK: 10.0.301.
  - Relevant installed runtimes include Microsoft.NETCore.App 8.0.28 and 10.0.9.
- `dotnet test UnicodeEmoji.StringProperties.slnx`
  - Passed: `UnicodeEmoji.StringProperties.Tests` on `net8.0`: 35 passed, 0 failed, 0 skipped.
  - Passed: `UnicodeEmoji.StringProperties.Tests` on `net10.0`: 35 passed, 0 failed, 0 skipped.
  - Passed: `UnicodeCldr.LocaleData.Tests` on `net8.0`: 73 passed, 0 failed, 0 skipped.
  - Passed: `UnicodeCldr.LocaleData.Tests` on `net10.0`: 73 passed, 0 failed, 0 skipped.
- `dotnet list UnicodeEmoji.StringProperties.slnx package --vulnerable --include-transitive`
  - No vulnerable packages were reported for the solution projects by the configured NuGet sources.
- Source and trust-boundary review:
  - Public runtime APIs are primarily Unicode string/scalar/range/table lookups over committed generated data.
  - No native interop, runtime code execution path, or runtime network access was identified in the reviewed runtime libraries.
  - Network access exists in developer-operated data update tools that download Unicode emoji data and CLDR JSON data over HTTPS before regenerating committed source/data artifacts.
- License/provenance review:
  - Repository license and Unicode third-party notices are present in `LICENSE` and `THIRD_PARTY_NOTICES.md`.
  - Unicode-provided data and generated tables remain subject to the Unicode terms referenced in the notices.

### Findings and residual risks

- **Overall assessment:** Broiler.Unicode is acceptable for the first preview within the intended-use scope above.
- **Runtime security posture:** Security-critical defects are considered unlikely in normal preview runtime use because the reviewed runtime surface primarily performs string operations and lookups over generated tables.
- **Accepted preview risk:** The data update pipeline downloads current Unicode and CLDR definitions from the internet and processes them into generated artifacts. A supply-chain, upstream data, transport, parser, or generator defect could theoretically affect the generated output. This risk is accepted for the first preview because data is not downloaded at runtime, generated data is committed, and future data refreshes can be reviewed as source diffs.
- **Preview compatibility risk:** APIs, generated data shape, and Unicode/CLDR version coverage remain preview-level and may change before a stable release.

## Decision

- [ ] **APPROVED FOR PREVIEW** within the intended-use scope above.
- [x] **APPROVED WITH CONDITIONS** listed below.
- [ ] **NOT APPROVED** for preview use.

**Conditions:**

1. This approval applies only to commit `89f134ed45f1baaf2bc00a86fb5319ac3ee5befe` and only to the first preview.
2. Preview communication must state that Broiler.Unicode is preview software and that its developer-operated data update tools download and process Unicode/CLDR definitions from the internet.
3. Preview communication must also state that the reviewed runtime libraries do not download Unicode/CLDR data at runtime.
4. Any future Unicode/CLDR data refresh or generator change requires review of the downloaded inputs, generated diffs, and tests before release.
5. This review is not an approval for security-critical production use beyond the stated first-preview scope.

## Human attestation

I confirm that I am a human developer, that I personally reviewed the revision and
evidence identified above, and that the decision is my own. I understand that this
attestation is a scoped engineering review, not a warranty or a claim that the component
is free of defects or vulnerabilities.

- **Name:** Maik Ratzmer
- **Signature or attributable commit:** MaiRat / Maik Ratzmer
- **Date:** 2026-07-01

AI tools may help assemble evidence and format this record, but the reviewer identity,
decision, and attestation above are attributable to the human reviewer named here.
