---
name: tag-release
description: Create a date-based git tag for a new release and push it
user_invocable: true
---

Run the tag-release script and push the tag:

1. Run `bash scripts/tag-release.sh` to create the tag
2. Parse the tag name from the output
3. Ask the user for confirmation before pushing
4. Push the tag with `git push origin <tag>`
