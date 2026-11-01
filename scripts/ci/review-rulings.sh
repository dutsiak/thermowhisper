#!/bin/sh
# Prints the maintainers' rulings for review.sh: comment lines "RULING: <point> - <reason>" written by the repository
# owner, a member or a collaborator (anyone else is ignored). At most 2048 bytes. $1 - the Pull Request number.
gh pr view "$1" --json comments \
  --jq '.comments[] | select(.authorAssociation == "OWNER" or .authorAssociation == "MEMBER" or .authorAssociation == "COLLABORATOR") | .body' \
  | { grep -E '^RULING: ' || true; } | { head -c 2048; cat > /dev/null; }
