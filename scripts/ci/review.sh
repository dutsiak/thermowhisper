#!/usr/bin/env bash
# $1 - the base branch (for example origin/dev), $2 - the title of the Pull Request or Merge Request.
set -euo pipefail
git diff "$1"...HEAD | claude -p --model sonnet "You are a code reviewer. Below is the diff of a pull request titled: $2
Check for bugs, security problems, secrets, missing tests and readability. Check that the title follows Conventional Commits with types feat|fix|refactor|perf|docs|test|ci|chore|build|style|revert.
If the diff changes code (not only docs or config), also check: SOLID (for object-oriented code), DRY, KISS, YAGNI, clear names, small focused functions, no dead or commented-out code, error handling, consistency with the surrounding code style.
Flag only clear violations that matter. Prefer simple code over extra abstractions.
Answer in English. The first line must be exactly 'VERDICT: APPROVE' or 'VERDICT: CHANGES'. After APPROVE write nothing else. After CHANGES - a short list of findings."
