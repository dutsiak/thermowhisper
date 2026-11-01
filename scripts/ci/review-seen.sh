#!/bin/sh
# Prints skip=true when the CI reviewer already commented on this exact head (re-run, reopened PR), else skip=false.
# $1 - the Pull Request number, $2 - the head commit of the Pull Request. The reviewer's comment ends with the marker
# <!-- reviewed: <head sha> -->; a run cancelled before posting leaves no marker, so that head is still reviewed.
if gh pr view "$1" --json comments --jq '.comments[] | select(.author.login == "github-actions") | .body' | grep -qF "<!-- reviewed: $2 -->"; then
  echo skip=true
else
  echo skip=false
fi
