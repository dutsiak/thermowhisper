#!/bin/sh
# Pull Request rules (GIT_STRATEGY_EN.md sections 5, 6, 10, 12): the branch name, the target branch and the title must fit together.
# $1 - the head (source) branch, $2 - the base (target) branch, $3 - the title. Prints the reason and exits 1 on a violation.
# The title and branch names are data: they are only compared and matched here, never evaluated.
head=$1
base=$2
title=$3
ID='[A-Z][A-Z0-9]*-[0-9]+'
TYPES='feat|fix|refactor|perf|docs|test|ci|chore|build|style|revert'

fail() { echo "pr-rules: $*" >&2; exit 1; }
matches() { printf '%s' "$1" | grep -Eq "$2"; }
branch_id() { printf '%s' "$1" | sed -nE "s/^($ID)-.*/\\1/p"; }
title_id() { printf '%s' "$1" | sed -nE "s/^($ID) .*/\\1/p"; }
want_base() { case "$base" in dev|stage|main) ;; *) fail "branch '$head' must go to dev, stage or main, not '$base'" ;; esac; }

exact=''        # a title that must match exactly (sync, release and merge-of-branches flows)
id_source=''    # the part of the branch name that may start with the task ID
any_id=no         # yes: the title may carry any task ID

case "$head" in
  main)
    case "$base" in
      dev|stage) exact="chore: Sync main into $base" ;;
      release/v*) exact="chore: Merge main into $base" ;;
      *) fail "main can only go to dev, stage or release/vX.Y, not '$base'" ;;
    esac ;;
  release/v*)
    [ "$base" = main ] || fail "'$head' can only go to main, not '$base'"
    exact="chore: Merge $head into main" ;;
  chore/Release-v*)
    version=${head#chore/Release-v}
    matches "$version" '^[0-9]+\.[0-9]+\.[0-9]+$' || fail "release branch '$head' must be chore/Release-vX.Y.Z"
    [ "$base" = main ] || [ "$base" = "release/v${version%.*}" ] || fail "'$head' must go to main or release/v${version%.*}, not '$base'"
    exact="chore(release): v$version" ;;
  chore/Sync-main-into-*-v*)
    target=${head#chore/Sync-main-into-}
    target=${target%%-v*}
    case "$target" in dev|stage) ;; *) fail "sync branch '$head' must target dev or stage" ;; esac
    [ "$base" = "$target" ] || fail "'$head' must go to $target, not '$base'"
    exact="chore: Sync main into $target" ;;
  chore/Merge-*-into-*)
    target=${head##*-into-}
    case "$target" in dev|stage) ;; *) fail "merge branch '$head' must target dev or stage" ;; esac
    [ "$base" = "$target" ] || fail "'$head' must go to $target, not '$base'"
    id_source=${head#chore/Merge-} ;;
  chore/Setup-repository)
    [ "$base" = main ] || fail "'$head' must go to main, not '$base'"
    any_id=yes ;;   # the setup branch has no task ID in its name
  revert/*)
    case "$head" in
      *-dev) target=dev ;;
      *-stage) target=stage ;;
      *) fail "revert branch '$head' must end with -dev or -stage" ;;
    esac
    [ "$base" = "$target" ] || fail "'$head' must go to $target, not '$base'"
    id_source=${head#revert/} ;;
  hotfix/*)
    version=${head#hotfix/v}
    version=${version%%-*}
    matches "$version" '^[0-9]+[.][0-9]+[.][0-9]+$' || fail "hotfix branch '$head' must be hotfix/vX.Y.Z-..."
    [ "$base" = "release/v${version%.*}" ] || fail "'$head' must go to release/v${version%.*}, not '$base'"
    id_source=$(printf '%s' "${head#hotfix/}" | sed -E 's/^v[0-9]+\.[0-9]+\.[0-9]+-?//') ;;
  feat/*|fix/*|refactor/*|perf/*|docs/*|test/*|ci/*|chore/*)
    want_base
    id_source=${head#*/} ;;
  *) fail "unknown branch type in '$head' (allowed: feat, fix, refactor, perf, docs, test, ci, chore, revert, hotfix, main, release/vX.Y)" ;;
esac

if [ -n "$exact" ]; then
  [ "$title" = "$exact" ] || fail "the title must be exactly: $exact"
  exit 0
fi

matches "$title" '^wip(\(|!|:)' && fail "wip is not allowed in a Pull Request title"
matches "$title" "^($TYPES)(\\([a-z0-9][a-z0-9._-]*\\))?!?: [^ ]" || fail "the title must be '<type>(<scope>): <ID> <Description>' with type one of: $TYPES"

body=${title#*: }
if [ "$any_id" = no ] && [ "$(branch_id "$id_source")" != "$(title_id "$body")" ]; then
  fail "the task ID in the title ('$(title_id "$body")') must equal the one in the branch name ('$(branch_id "$id_source")')"
fi
