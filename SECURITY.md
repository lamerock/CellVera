# Security Policy

## Supported versions

Security fixes are normally applied to the latest released version of CellVera. Users should update to the newest release before reporting a problem that may already have been fixed.

## Reporting a vulnerability

Please **do not open a public GitHub issue** for a suspected security vulnerability.

Preferred reporting method:

1. Use GitHub's private vulnerability reporting / Security Advisory feature for this repository when available.
2. If private vulnerability reporting is not enabled, contact the maintainer privately through the repository owner's published contact method.

Please include:

- A clear description of the issue
- The affected CellVera version
- Windows version and architecture
- Reproduction steps or a minimal proof of concept
- The potential impact
- Any suggested mitigation, if known

Avoid including passwords, access tokens, signing material, or unnecessary personal information.

## Scope

Useful security reports include issues involving:

- Unsafe file handling or command execution
- Installer or update-chain weaknesses
- Unexpected privilege elevation
- Exposure of locally stored CellVera data
- Dependency vulnerabilities that are actually reachable in CellVera

Battery readings that are inaccurate because firmware or Windows does not expose a value are generally compatibility bugs rather than security vulnerabilities.

## Disclosure

Please allow reasonable time for investigation and remediation before publicly disclosing an unresolved vulnerability.
