#!/bin/bash
# make-temp-project-2022.sh — dựng project Unity 2022.3 tạm (ngoài repo) trỏ `file:` vào package của một worktree.
#
# Vì sao: package hứa chạy từ Unity 2022.3 nhưng dev project là 6000.6; chỉ chạy test thật trên 2022.3 mới bắt được API
# 2023+ lọt vào và khác biệt UTF 1.1.33. Project tạm nằm ngoài repo (không bẩn git, không lẫn Library của 6000.6) và mỗi gói
# một project riêng để hai gói chạy song song không mở chung một project.
# --bootstrap: mở project một lần bằng 2022.3 (import + compile) qua unity-slot.sh, rồi chép nunit.framework.dll,
# UnityEngine.TestRunner.dll, UnityEditor.TestRunner.dll của từng bản Unity vào cache để compile-check.sh compile được
# assembly test mà không cần mở Unity.
#
# Cách dùng:
#   make-temp-project-2022.sh [--repository <worktree>] [--name <gói>] [--project <đường dẫn>] [--bootstrap] [--timeout GIÂY]
# Mặc định: tên = tên nhánh wt/<gói> (hoặc tên thư mục worktree); project = ~/.cache/unity-liveops/temp-2022/<gói>.
# In đường dẫn project ở dòng cuối "TEMP PROJECT <đường dẫn>".

set -euo pipefail

readonly UNITY_2022=/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity
readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops
readonly MINIMUM_FREE_GIGABYTES=5
readonly DEFAULT_IMPORT_TIMEOUT_SECONDS=1800

script_directory=$(cd "$(dirname "$0")" && pwd)
repository=${REPOSITORY:-}
name=""
project=""
bootstrap=0
timeout_seconds=$DEFAULT_IMPORT_TIMEOUT_SECONDS

fail_usage() { echo "make-temp-project-2022.sh: $1" >&2; exit 2; }

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repository) [ "$#" -ge 2 ] || fail_usage "--repository cần đường dẫn"; repository=$2; shift 2;;
    --name) [ "$#" -ge 2 ] || fail_usage "--name cần giá trị"; name=$2; shift 2;;
    --project) [ "$#" -ge 2 ] || fail_usage "--project cần đường dẫn"; project=$2; shift 2;;
    --bootstrap) bootstrap=1; shift;;
    --timeout) [ "$#" -ge 2 ] || fail_usage "--timeout cần số giây"; timeout_seconds=$2; shift 2;;
    --help|-h) sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) fail_usage "tham số lạ '$1'";;
  esac
done

resolve_repository() {
  if [ -n "$repository" ]; then (cd "$repository" && pwd -P); return; fi
  local top_level
  if top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/$PACKAGE_RELATIVE_PATH" ]; then
    echo "$top_level"; return
  fi
  (cd "$script_directory/../.." && pwd -P)
}
repository=$(resolve_repository)
[ -d "$repository/$PACKAGE_RELATIVE_PATH" ] || fail_usage "không thấy package ở $repository/$PACKAGE_RELATIVE_PATH"

if [ -z "$name" ]; then
  branch=$(git -C "$repository" branch --show-current 2>/dev/null || true)
  case "$branch" in
    wt/*) name=${branch#wt/};;
    *) name=$(basename "$repository");;
  esac
fi
case "$name" in
  ''|*/*|.*) fail_usage "tên project tạm không hợp lệ: '$name'";;
esac
[ -n "$project" ] || project=$HOME/.cache/unity-liveops/temp-2022/$name

mkdir -p "$project/Assets" "$project/Packages" "$project/ProjectSettings"
project=$(cd "$project" && pwd -P)

# Manifest đúng mục 9.6 của kế hoạch: chỉ module package cần + UTF 1.1.33 đi kèm 2022.3 + testables để test của package chạy.
cat > "$project/Packages/manifest.json" <<EOF
{
  "dependencies": {
    "com.dreamtech.liveops": "file:$repository/$PACKAGE_RELATIVE_PATH",
    "com.unity.test-framework": "1.1.33",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.uielements": "1.0.0"
  },
  "testables": ["com.dreamtech.liveops"]
}
EOF
# Lock cũ có thể còn trỏ worktree khác (project tạo lại cho gói khác cùng tên) — xoá để Unity giải lại từ manifest.
rm -f "$project/Packages/packages-lock.json"
echo "manifest: $project/Packages/manifest.json → file:$repository/$PACKAGE_RELATIVE_PATH"

copy_test_references() {
  # $1 phiên bản (2022.3|6000.6), $2 project đã import
  local version=$1 source_project=$2
  local script_assemblies=$source_project/Library/ScriptAssemblies
  local nunit
  nunit=$(find "$source_project/Library/PackageCache" -path '*com.unity.ext.nunit*' -name nunit.framework.dll 2>/dev/null | grep -v '/net35/\|/net40/' | head -n 1 || true)
  [ -n "$nunit" ] || nunit=$(find "$source_project/Library/PackageCache" -path '*com.unity.ext.nunit*' -name nunit.framework.dll 2>/dev/null | head -n 1 || true)
  if [ -z "$nunit" ] || [ ! -f "$script_assemblies/UnityEngine.TestRunner.dll" ] || [ ! -f "$script_assemblies/UnityEditor.TestRunner.dll" ]; then
    echo "LỖI: $source_project chưa có DLL test ($version) — import chưa xong hoặc thiếu com.unity.test-framework" >&2
    return 1
  fi
  local destination destinations=("$HOME/.cache/unity-liveops/test-references/$version")
  # Bản trong worktree chỉ khi worktree đã có tools/ (đã ignore .cache/); worktree gói khác trước merge thì không ghi vào.
  if [ -d "$repository/tools/liveops-hub" ]; then
    destinations+=("$repository/tools/liveops-hub/.cache/test-references/$version")
  fi
  for destination in "${destinations[@]}"; do
    mkdir -p "$destination"
    cp "$nunit" "$script_assemblies/UnityEngine.TestRunner.dll" "$script_assemblies/UnityEditor.TestRunner.dll" "$destination/"
    echo "test references $version → $destination"
  done
}

if [ "$bootstrap" = 1 ]; then
  [ -x "$UNITY_2022" ] || fail_usage "thiếu Unity 2022.3.62f2 ở $UNITY_2022"
  free_gigabytes=$(df -g /System/Volumes/Data | awk 'NR==2 {print $4}')
  if [ "${free_gigabytes:-0}" -lt "$MINIMUM_FREE_GIGABYTES" ]; then
    echo "LỖI: ổ đĩa còn ${free_gigabytes} GB (< $MINIMUM_FREE_GIGABYTES GB) — không mở Unity" >&2
    exit 3
  fi
  mkdir -p "$project/Logs"
  log_file=$project/Logs/bootstrap-import.log
  echo "bootstrap: import 2022.3 (log $log_file)"
  import_status=0
  "$script_directory/unity-slot.sh" --timeout "$timeout_seconds" --label "bootstrap-2022 $name" -- \
    "$UNITY_2022" -batchmode -nographics -projectPath "$project" -quit -logFile "$log_file" || import_status=$?
  if [ "$import_status" != 0 ]; then
    echo "LỖI: import 2022.3 thoát $import_status — xem $log_file" >&2
    grep -n "error CS\|Error\b" "$log_file" 2>/dev/null | head -n 20 >&2 || true
    exit 1
  fi
  if grep -q "error CS" "$log_file"; then
    echo "LỖI: import 2022.3 có lỗi compile:" >&2
    grep "error CS" "$log_file" | head -n 20 >&2
    exit 1
  fi
  copy_test_references 2022.3 "$project"
  # 6000.6 lấy từ worktree nếu đã import sẵn (Library nhân bản từ dev project) — không mở Unity thêm lần nào.
  if [ -f "$repository/Library/ScriptAssemblies/UnityEngine.TestRunner.dll" ]; then
    copy_test_references 6000.6 "$repository" || true
  fi
fi

echo "TEMP PROJECT $project"
