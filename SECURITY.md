# Security Policy

## Supported versions

Security fixes are provided for the latest stable release of SwiftDispatch. Older releases and prerelease versions do not receive security fixes; upgrade to the latest stable release to receive them.

This policy covers the SwiftDispatch runtime, contracts, source generator, and packages maintained in this repository. Reports involving the repository's website or release workflows are also welcome.

## Report a vulnerability

Please report suspected vulnerabilities privately to [valipourkamyab.official@gmail.com](mailto:valipourkamyab.official@gmail.com), with a subject such as `SwiftDispatch security report`.

Do not disclose vulnerability details in public issues, pull requests, or discussions before a fix or mitigation is available and disclosure has been coordinated. Ordinary bugs and feature requests can be reported through public issues as described in [CONTRIBUTING.md](CONTRIBUTING.md). If you are unsure whether an issue has security implications, report it privately first.

Include as much of the following as you can:

- Affected package names and versions, and the relevant commit if testing unreleased code.
- .NET SDK/runtime versions, operating system, and whether Native AOT or trimming is involved.
- A description of the vulnerability, potential impact, and conditions required to exploit it.
- Reproduction steps and a minimal proof of concept, with relevant diagnostics or logs.
- Any suggested fix or mitigation, and whether the issue has already been disclosed elsewhere.
- Whether and how you would like to be credited.

Remove credentials, personal data, and unrelated sensitive information from reports. Use a minimal test environment and avoid accessing or modifying other people's data when reproducing an issue.

## What to expect

The project maintainer will review the report, seek clarification where needed, and work with the reporter to assess its impact and identify a fix or mitigation. Reports will be handled as privately as possible, with details shared only as needed for investigation and remediation.

SwiftDispatch is maintained on a best-effort basis. Response and resolution times depend on maintainer availability and the complexity of the issue; no fixed response or release deadline is guaranteed. If you have not received a response within seven days, please follow up by email.

For confirmed vulnerabilities, the maintainer will coordinate disclosure timing with the reporter and aim to publish a fix or mitigation together with an advisory explaining affected versions and upgrade instructions. Reporter credit will be included with their consent. If the report is not considered a security vulnerability, the maintainer will explain the assessment and discuss whether it belongs in a public bug report.
