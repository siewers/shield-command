---
name: tag-release
description: Create a date-based git tag for a new release and push it
user_invocable: true
---

Run the tag-release script and push the tag:

1. Ask the user which stability level to use: alpha, beta, rc, or stable (default)
2. Run `bash scripts/tag-release.sh <stability>` to create the tag
3. Parse the tag name from the output
4. Ask the user for confirmation before pushing
5. Push the tag with `git push origin <tag>`

Stability levels produce these tag formats:
- `stable` (default): `v2026.3.2`
- `alpha`: `v2026.3.2-alpha`
- `beta`: `v2026.3.2-beta`
- `rc`: `v2026.3.2-rc`

If a tag already exists for that date+stability, a numeric suffix is appended (e.g. `v2026.3.2-beta.1`).
