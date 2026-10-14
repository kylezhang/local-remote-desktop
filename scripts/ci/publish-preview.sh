#!/bin/bash
# Explicit maintainer-authorized preview publication; never runs on pull requests.
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd -P)"
cd "$root"
[[ "$GITHUB_REPOSITORY" == kylezhang/local-remote-desktop && "$GITHUB_REF" == refs/heads/main ]] || exit 0
read -r version notes < <(python3 - <<'PY'
import json, pathlib, re, xml.etree.ElementTree as ET
p=pathlib.Path('.github/release-request.json')
if not p.exists(): print('skip skip'); raise SystemExit
r=json.loads(p.read_text())
v=ET.parse('apps/windows/LocalRemoteDesktop.csproj').findtext('.//Version')
if r.get('version') != v: print('skip skip'); raise SystemExit
assert re.fullmatch(r'\d+\.\d+\.\d+-(alpha|beta|rc)\.[1-9]\d*', v), 'Pre-release required'
assert r.get('prerelease') is True and r.get('nativeAcceptance') == 'deferred-by-maintainer'
assert r.get('notes') == f'docs/release-v{v}.md' and pathlib.Path(r['notes']).is_file()
print(v,r['notes'])
PY
)
[[ "$version" != skip ]] || exit 0
tag="v$version"
if gh release view "$tag" --repo "$GITHUB_REPOSITORY" --json isDraft > "$RUNNER_TEMP/existing-release.json" 2> "$RUNNER_TEMP/release-query-error.txt"; then
    if python3 -c 'import json,sys; sys.exit(0 if json.load(open(sys.argv[1]))["isDraft"] else 1)' "$RUNNER_TEMP/existing-release.json"; then
        echo 'Draft already exists; refusing automatic replacement.' >&2; exit 1
    fi
    echo "Published $tag already exists; immutable assets retained."
    exit 0
fi
# A failed query is not assumed to be a missing release.
gh api "repos/$GITHUB_REPOSITORY/releases" > "$RUNNER_TEMP/release-list.json"
python3 - "$RUNNER_TEMP/release-list.json" "$tag" <<'PY'
import json,sys
assert not any(r['tag_name']==sys.argv[2] for r in json.load(open(sys.argv[1]))), 'Existing release lookup failed'
PY
stage="$(mktemp -d "$RUNNER_TEMP/preview-publication.XXXXXXXX")"
gh run download "$GITHUB_RUN_ID" --repo "$GITHUB_REPOSITORY" --name "windows-x64-$GITHUB_SHA" --dir "$stage/windows"
gh run download "$GITHUB_RUN_ID" --repo "$GITHUB_REPOSITORY" --name "macos-arm64-preview-$GITHUB_SHA" --dir "$stage/macos"
python3 scripts/prepare-cloud-release.py --windows "$stage/windows" --macos "$stage/macos" --output "$stage/assets" --version "$version" --commit "$GITHUB_SHA" --run "$GITHUB_RUN_ID"
cp "$notes" "$stage/notes.md"
printf '\nSource commit: `%s`\n\n[Native CI and package checks](https://github.com/%s/actions/runs/%s). See release.json and SHA256SUMS.txt.\n' "$GITHUB_SHA" "$GITHUB_REPOSITORY" "$GITHUB_RUN_ID" >> "$stage/notes.md"
# Create an annotated tag only after all native build jobs passed. Never retarget.
if gh api "repos/$GITHUB_REPOSITORY/git/ref/tags/$tag" > "$stage/tag.json" 2>/dev/null; then
    python3 - "$stage/tag.json" "$GITHUB_SHA" <<'PY'
import json,subprocess,sys
o=json.load(open(sys.argv[1]))['object']
if o['type']=='tag': o=json.loads(subprocess.check_output(['gh','api',o['url']],text=True))['object']
assert o['type']=='commit' and o['sha']==sys.argv[2], 'Tag exists at different commit'
PY
else
    gh api --method POST "repos/$GITHUB_REPOSITORY/git/tags" -f tag="$tag" -f message="Local Remote Desktop $tag" -f object="$GITHUB_SHA" -f type=commit > "$stage/new-tag.json"
    tag_object="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["sha"])' "$stage/new-tag.json")"
    gh api --method POST "repos/$GITHUB_REPOSITORY/git/refs" -f ref="refs/tags/$tag" -f sha="$tag_object" > /dev/null
fi
gh release create "$tag" "$stage/assets/"* --repo "$GITHUB_REPOSITORY" --verify-tag --draft --prerelease --latest=false --title "$tag" --notes-file "$stage/notes.md"
gh release download "$tag" --repo "$GITHUB_REPOSITORY" --dir "$stage/downloaded"
python3 - "$stage/assets" "$stage/downloaded" <<'PY'
import hashlib,pathlib,sys
a,b=map(pathlib.Path,sys.argv[1:])
assert {p.name for p in a.iterdir()}=={p.name for p in b.iterdir()}, 'Uploaded inventory mismatch'
for p in a.iterdir():
 with p.open('rb') as x,(b/p.name).open('rb') as y:
  assert hashlib.file_digest(x,'sha256').digest()==hashlib.file_digest(y,'sha256').digest(), 'Uploaded bytes mismatch'
PY
gh release edit "$tag" --repo "$GITHUB_REPOSITORY" --draft=false --prerelease --latest=false
printf 'Published verified pre-release [%s](https://github.com/%s/releases/tag/%s). Native interactive acceptance was deferred.\n' "$tag" "$GITHUB_REPOSITORY" "$tag" >> "$GITHUB_STEP_SUMMARY"
