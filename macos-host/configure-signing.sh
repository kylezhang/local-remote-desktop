#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
[[ "$(uname -s)" == Darwin ]] || { echo '仅支持 macOS。' >&2; exit 1; }
[[ $# == 0 ]] || { echo '用法：bash macos-host/configure-signing.sh；也可通过 SIGN_IDENTITY 指定已有证书。' >&2; exit 1; }
identities="$(security find-identity -v -p codesigning)"
identity="${SIGN_IDENTITY:-}"
if [[ -z "$identity" ]]; then identity="$(printf '%s\n' "$identities" | awk '/"Developer ID Application:/ {print $2; exit}')"; fi
if [[ -z "$identity" ]]; then identity="$(printf '%s\n' "$identities" | awk '/"Apple Development:/ {print $2; exit}')"; fi
if [[ -z "$identity" || "$identity" == "-" ]]; then
  echo '没有可用的 Apple 签名证书。请先在 Mac 配置带私钥的 Apple Development 或 Developer ID Application 身份。' >&2
  echo '本脚本不创建证书、不修改信任设置、不重置权限，也不要求提供私钥或登录密码。' >&2
  exit 1
fi
# Verify the selected identity using a disposable local executable before saving
# the choice; never re-sign or replace the installed working application here.
probe_dir="$(mktemp -d "$PWD/.signing-probe.XXXXXX")"
[[ "$probe_dir" == "$PWD/.signing-probe."* ]] || exit 1
trap 'rm -rf -- "$probe_dir"' EXIT
cp /usr/bin/true "$probe_dir/probe"
codesign --force --sign "$identity" "$probe_dir/probe"
codesign --verify --strict "$probe_dir/probe"
printf '%s\n' "$identity" > "$PWD/.signing-identity"
touch "$PWD/.require-stable-signature"
echo '固定签名身份已保存。后续构建复用此身份，失败时中止，不退回临时签名。'
echo '从 ad-hoc 切换到证书签名的首次更新仍需重新授权；之后也不能保证系统永不再次询问。'
