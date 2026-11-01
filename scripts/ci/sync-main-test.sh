#!/bin/sh
# Test of sync-main.sh (modes prepare, verify, finish) in a temporary repository with a local bare remote and a stub `gh` on PATH.
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
check() { if [ "$2" != "$3" ]; then echo "FAIL: $1: expected '$3', got '$2'"; failed=1; fi }
count() { n=$(grep -c -- "$1" "$tmp/state/calls" 2>/dev/null); echo "${n:-0}"; }
size() { [ -f "$1" ] && wc -c < "$1" | tr -d " "; true; }

# gh stub: keeps its state in $tmp/state (open sync PR, comments, a log of the calls that change something).
mkdir "$tmp/bin" "$tmp/state"
cat > "$tmp/bin/gh" <<'STUB'
#!/bin/sh
s=$STUB_STATE
case "$1 $2" in
  "pr list") [ -f "$s/open" ] && echo 9 ;;
  "pr create") touch "$s/open"; echo "create" >> "$s/calls"; echo https://example.test/pull/9 ;;
  "pr edit") echo "edit $*" >> "$s/calls" ;;
  "pr view") case "$*" in *comments*) cat "$s/comments" 2>/dev/null ;; *) echo https://example.test/pull/9 ;; esac ;;
  "pr comment") echo "comment" >> "$s/calls"; printf '%s\n' "$*" >> "$s/comments" ;;
  "pr merge") echo "merge $*" >> "$s/calls" ;;
  "workflow run") echo "dispatch $*" >> "$s/calls" ;;
esac
exit 0
STUB
chmod +x "$tmp/bin/gh"
export PATH="$tmp/bin:$PATH" STUB_STATE="$tmp/state" GITHUB_OUTPUT="$tmp/out"
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.autocrlf GIT_CONFIG_VALUE_0=false

# Repository: a criss-cross between dev and task gives two merge bases (noise); main has one commit that dev does not have.
git init -q --bare -b main "$tmp/remote.git"
git init -q -b main "$tmp/work" && cd "$tmp/work" || exit 1
git remote add origin "$tmp/remote.git"
git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
mkdir -p scripts/ci && cp "$dir/check-noise.sh" scripts/ci/
echo base > f.txt; git add -A; git commit -qm A
git switch -q -c p; echo p > p.txt; git add -A; git commit -qm P
git switch -q -c q main; echo q > q.txt; git add -A; git commit -qm Q
git switch -q -c dev p; git merge -q --no-ff q -m "dev merges q"
git switch -q -c task q; git merge -q --no-ff p -m "task merges p"; echo t > t.txt; git add -A; git commit -qm T
git switch -q main; echo m > m.txt; git add -A; git commit -qm M
git switch -q -c quiet dev; echo x > x.txt; git add -A; git commit -qm X
git push -q origin main dev task quiet

clone() { git clone -q "$tmp/remote.git" "$1" && mkdir -p "$1/scripts/ci" && cp "$dir/sync-main.sh" "$1/scripts/ci/"; }
reset_state() { rm -rf "$tmp/state"/*; : > "$tmp/out"; }
prepare() {   # $1 - the head branch of the task Pull Request, $2 - a folder for the clone (one runner)
  clone "$2"
  (cd "$2" && MODE=prepare ASSIGNEE=po sh scripts/ci/sync-main.sh dev 5 "$(git rev-parse "origin/$1")") > /dev/null
}
get() { sed -n "s/^$1=//p" "$tmp/out"; }
run_mode() {  # $1 - the folder; the rest - the environment of the job
  (cd "$1" && shift && env "$@" sh scripts/ci/sync-main.sh dev 5 "") > /dev/null 2>&1
}

# 1. no noise: nothing happens
reset_state; prepare quiet "$tmp/c1"
check "no noise: no output" "$(size "$tmp/out")" "0"
check "no noise: no gh calls" "$(size "$tmp/state/calls")" ""

# 2. noise, clean merge: the sync PR is created with the assignee, the fingerprint is rechecked, the sync is merged, ci.yml dispatched
reset_state; prepare task "$tmp/c2"
base_sha=$(get base_sha); main_sha=$(get main_sha); tree=$(get tree)
check "clean: need" "$(get need)" "verify"
check "clean: sync_pr" "$(get sync_pr)" "9"
check "clean: base_sha" "$base_sha" "$(git -C "$tmp/work" rev-parse dev)"
check "clean: main_sha" "$main_sha" "$(git -C "$tmp/work" rev-parse main)"
check "clean: sync PR created once" "$(count '^create')" "1"
check "clean: assignee" "$(count 'add-assignee po')" "1"
clone "$tmp/c3"
run_mode "$tmp/c3" MODE=verify BASE_SHA="$base_sha" MAIN_SHA="$main_sha" TREE="$tree"; check "clean: verify recreates the same tree" "$?" "0"
run_mode "$tmp/c3" MODE=verify BASE_SHA="$base_sha" MAIN_SHA="$main_sha" TREE=0000; check "clean: a different tree fails verify" "$?" "1"
run_mode "$tmp/c3" MODE=finish NEED=verify SYNC_PR=9 VERIFY=success BASE_SHA="$base_sha" MAIN_SHA="$main_sha"
check "clean: merged with the checked head" "$(count "merge pr merge 9 --merge --match-head-commit $main_sha")" "1"
check "clean: ci.yml dispatched" "$(count 'dispatch workflow run ci.yml --ref dev')" "1"

# 3. the base moved during the check: nothing is merged
reset_state
git -C "$tmp/work" switch -q dev; echo moved > "$tmp/work/moved.txt"; git -C "$tmp/work" add -A; git -C "$tmp/work" commit -qm moved; git -C "$tmp/work" push -q origin dev
run_mode "$tmp/c3" MODE=finish NEED=verify SYNC_PR=9 VERIFY=success BASE_SHA="$base_sha" MAIN_SHA="$main_sha"
check "moved: nothing merged" "$(count '^merge')" "0"

# 4. cancelled check: nothing at all; failing check: one comment, no merge
reset_state; touch "$tmp/state/open"
run_mode "$tmp/c3" MODE=finish NEED=verify SYNC_PR=9 VERIFY=cancelled BASE_SHA="$base_sha" MAIN_SHA="$main_sha"
check "cancelled: no merge, no comment" "$(size "$tmp/state/calls")" ""
run_mode "$tmp/c3" MODE=finish NEED=verify SYNC_PR=9 VERIFY=failure BASE_SHA="$base_sha" MAIN_SHA="$main_sha"
run_mode "$tmp/c3" MODE=finish NEED=verify SYNC_PR=9 VERIFY=failure BASE_SHA="$base_sha" MAIN_SHA="$main_sha"
check "failing check: one comment" "$(count '^comment')" "1"
check "failing check: no merge" "$(count '^merge')" "0"
grep -q 'the linter or tests do not pass' "$tmp/state/comments" || { echo "FAIL: failing check: comment text"; failed=1; }

# 5. conflict: main and dev change the same file differently; the sync PR already exists and is reused
reset_state; touch "$tmp/state/open"
git -C "$tmp/work" switch -q main; echo main-side > "$tmp/work/f.txt"; git -C "$tmp/work" commit -qam "main edits f"
git -C "$tmp/work" switch -q dev; echo dev-side > "$tmp/work/f.txt"; git -C "$tmp/work" commit -qam "dev edits f"
git -C "$tmp/work" push -q origin main dev
prepare task "$tmp/c5"
check "conflict: need" "$(get need)" "conflict"
check "conflict: tree empty" "$(get tree)" ""
check "conflict: sync PR reused" "$(count '^create')" "0"
run_mode "$tmp/c5" MODE=finish NEED=conflict SYNC_PR=9
run_mode "$tmp/c5" MODE=finish NEED=conflict SYNC_PR=9
check "conflict: one comment" "$(count '^comment')" "1"
grep -q 'conflict' "$tmp/state/comments" || { echo "FAIL: conflict: comment text"; failed=1; }
check "conflict: no merge" "$(count '^merge')" "0"

[ "$failed" -eq 0 ] && echo "sync-main-test: OK"
exit $failed
