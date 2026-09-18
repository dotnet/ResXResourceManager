# Security Policy

We take the security of ResX Resource Manager seriously. If you discover a vulnerability, please report it responsibly and privately.

## Reporting a Vulnerability

Please do not open a public issue for security vulnerabilities.

Instead, use GitHub's private vulnerability reporting feature:

1. Go to the [GitHub Security Advisories](https://github.com/dotnet/ResXResourceManager/security/advisories) page for this repository.
2. Click "Report a vulnerability".
3. Provide a clear description of the issue, including reproduction steps, impact, and any suggested fix.

If you cannot use GitHub Security Advisories, contact the maintainers through the repository's private channels, but please avoid sharing details in public discussions or issues.

### What to Include

When reporting a vulnerability, please include:

- A description of the vulnerability
- Steps to reproduce the issue
- The affected version(s)
- The impact or risk if it is exploited
- Any suggested mitigation or fix
- Relevant screenshots, logs, or proof-of-concept material if available

### Response Timeline

- We will acknowledge receipt of the report within 48 hours.
- We will provide an initial assessment within 7 days.
- We will keep you informed as we investigate and remediate the issue.
- We will credit reporters in the fix announcement unless they prefer to remain anonymous.

## Supported Versions

The project supports the latest released version and the current default branch.

| Version | Supported |
| ------- | --------- |
| Latest release / default branch | ✅ Yes |
| Older released versions | ❌ No |

If a vulnerability is found in an unsupported version, we may still investigate it, but there is no guarantee of a backport or patch unless explicitly announced.

## Security Best Practices

When contributing to this project:

- Never commit secrets, credentials, API keys, or tokens.
- Keep dependencies up to date.
- Review dependency alerts and security advisories promptly.
- Follow secure coding practices and validate fixes before merging.
- Do not disclose security issues publicly before a fix is available.

## Security Features

This repository is expected to use GitHub's built-in security features, including:

- ✅ Private vulnerability reporting
- ✅ Dependency review / Dependabot security updates where applicable
- ✅ Secret scanning and push protection
- ✅ Code scanning and security analysis where enabled

## Questions?

If you have questions about this security policy, please use the repository's discussion or contact the maintainers through the project channels.

Thank you for helping keep ResX Resource Manager secure.
