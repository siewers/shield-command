#!/bin/bash
set -euo pipefail

BASE="v$(date +%Y.%-m.%-d)"
TAG="$BASE"
N=1

while git rev-parse "$TAG" >/dev/null 2>&1; do
  TAG="${BASE}.${N}"
  N=$((N + 1))
done

git tag "$TAG"
echo "Tagged: $TAG"
echo "Push with: git push origin $TAG"
