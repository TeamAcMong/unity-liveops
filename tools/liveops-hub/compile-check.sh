#!/bin/bash
# compile-check.sh — compile từng assembly của package bằng Roslyn (C# 9) với DLL của TỪNG bản Unity, không mở Unity.
#
# Vì sao: chặn C# 10, API chỉ có từ 2023+, API Obsolete của 6000.6 trước khi tốn một lượt Unity batchmode (vài phút).
# Bốn phần (PD-30):
#   core     DreamTech.LiveOps — chỉ netstandard.dll, -warnaserror+ → `using UnityEngine` trong core là lỗi compile
#            (chứng minh noEngineReferences thật).
#   runtime  DreamTech.LiveOps.Unity — DLL UnityEngine*, KHÔNG UnityEditor*, KHÔNG define UNITY_EDITOR → code runtime lỡ
#            gọi API Editor là lỗi compile (Unity thật vẫn compile được vì Editor có UnityEditor.dll, nên chỉ ở đây mới bắt).
#   editor   DreamTech.LiveOps.Editor (bỏ qua khi chưa có Editor/).
#   tests    Tests.Support, Tests, Unity.Tests, Editor.Tests — cần nunit + TestRunner cache (make-temp-project-2022.sh --bootstrap).
# Trên 6000.6, Unity/Editor/test bật -warnaserror+:CS0618,CS0619 (API bị Obsolete hoặc đã gỡ ở Unity 6).
# Roslyn dùng bản đi kèm 2022.3 cho cả hai bản với -langversion:9.0: đúng trần ngôn ngữ của package.
# Tên output = tên assembly Unity để InternalsVisibleTo hiện có (DreamTech.LiveOps.Tests...) có hiệu lực; DLL ra
# ~/.cache/unity-liveops/compile/<tên worktree>/<bản>/.
#
# Cách dùng:
#   compile-check.sh [--repository <worktree>] [--runtime-only] [--parts core,runtime,editor,tests] [--unity 2022|6000|all]
# Kho mặc định: git top-level của thư mục hiện tại (nếu có Packages/com.dreamtech.liveops), không thì repo chứa script.
# In "COMPILE CHECK OK" và thoát 0 khi mọi phần xanh; lỗi compile thoát 1; dùng sai hoặc thiếu công cụ thoát 2.

set -euo pipefail

readonly UNITY_2022_CONTENTS=/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents
readonly UNITY_6000_CONTENTS=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents
readonly PACKAGE_RELATIVE_PATH=Packages/com.dreamtech.liveops

script_directory=$(cd "$(dirname "$0")" && pwd)
repository=${REPOSITORY:-}
parts=${PARTS:-core,runtime,editor,tests}
unity_selection=all

fail_usage() { echo "compile-check.sh: $1" >&2; exit 2; }

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repository) [ "$#" -ge 2 ] || fail_usage "--repository cần đường dẫn"; repository=$2; shift 2;;
    --runtime-only) parts=core,runtime; shift;;
    --parts) [ "$#" -ge 2 ] || fail_usage "--parts cần danh sách"; parts=$2; shift 2;;
    --unity) [ "$#" -ge 2 ] || fail_usage "--unity cần 2022|6000|all"; unity_selection=$2; shift 2;;
    --help|-h) sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
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
package=$repository/$PACKAGE_RELATIVE_PATH
[ -d "$package" ] || fail_usage "không thấy package ở $package"

command -v dotnet >/dev/null 2>&1 || fail_usage "thiếu dotnet (Roslyn của Unity chạy bằng dotnet)"
readonly COMPILER_DLL=$UNITY_2022_CONTENTS/DotNetSdkRoslyn/csc.dll
[ -f "$COMPILER_DLL" ] || fail_usage "thiếu Roslyn của 2022.3 ở $COMPILER_DLL"

# Output ngoài worktree: gói khác gọi script này lên worktree của họ trước khi tools/ được merge — ghi vào worktree đó sẽ
# sinh thư mục chưa track mà check-ownership.py coi là file ngoài quyền ghi.
output_root=$HOME/.cache/unity-liveops/compile/$(basename "$repository")
home_test_reference_root=$HOME/.cache/unity-liveops/test-references

# Define phiên bản giống Unity sinh: mọi UNITY_X_Y_OR_NEWER tới bản đang compile. Thiếu define thì nhánh
# `#if UNITY_2023_2_OR_NEWER` bị bỏ và 6000.6 báo lỗi UxmlFactory giả — define là bắt buộc.
version_defines() {
  local target_major=$1 target_minor=$2 defines="UNITY_5_3_OR_NEWER;UNITY_5_4_OR_NEWER;UNITY_5_5_OR_NEWER;UNITY_5_6_OR_NEWER"
  local year minor
  for year in 2017 2018 2019 2020 2021 2022 2023; do
    local last_minor=4
    case "$year" in 2020|2021|2022) last_minor=3;; 2023) last_minor=2;; esac
    for minor in $(seq 1 "$last_minor"); do
      if [ "$target_major" -gt "$year" ] || { [ "$target_major" = "$year" ] && [ "$minor" -le "$target_minor" ]; }; then
        defines="$defines;UNITY_${year}_${minor}_OR_NEWER"
      fi
    done
  done
  if [ "$target_major" -ge 6000 ]; then
    for minor in $(seq 0 "$target_minor"); do defines="$defines;UNITY_6000_${minor}_OR_NEWER"; done
  fi
  echo "$defines;UNITY_${target_major};UNITY_${target_major}_${target_minor};NET_STANDARD_2_1;NET_STANDARD;CSHARP_7_3_OR_NEWER"
}

# Danh sách file .cs của một thư mục (sắp tên để lệnh tất định); rỗng khi thư mục không có.
list_sources() {
  local directory=$1; shift
  [ -d "$directory" ] || return 0
  find "$directory" -name '*.cs' "$@" | LC_ALL=C sort
}

# In giá trị khi không rỗng; luôn thoát 0 để `set -e` không dừng trong nhóm lệnh ghi file.
echo_if_set() {
  if [ -n "$1" ]; then echo "$1"; fi
  return 0
}

reference_arguments() {
  local file
  for file in "$@"; do printf -- '-r:%s\n' "$file"; done
}

failures=0

compile_assembly() {
  # $1 phiên bản, $2 tên assembly, $3 define, $4 file chứa danh sách tham chiếu, $5 cờ cảnh báo, $6 file chứa danh sách nguồn
  local version=$1 assembly_name=$2 defines=$3 references_file=$4 warning_flags=$5 sources_file=$6
  local output_file=$output_root/$version/$assembly_name.dll
  local response_file=$output_root/$version/$assembly_name.rsp
  if [ ! -s "$sources_file" ]; then
    echo "   (bỏ qua $assembly_name: không có file .cs)"
    return 0
  fi
  {
    echo "-nologo"; echo "-nostdlib"; echo "-langversion:9.0"; echo "-target:library"; echo "-warn:4"
    echo "-define:$defines"
    echo "-out:$output_file"
    echo_if_set "$warning_flags"
    cat "$references_file"
    sed 's/.*/"&"/' "$sources_file"
  } > "$response_file"
  echo "== $version $assembly_name ($(wc -l < "$sources_file" | tr -d ' ') file)"
  local compiler_log=$output_root/$version/$assembly_name.log compiler_status=0
  rm -f "$output_file"
  # -noconfig phải nằm trên dòng lệnh: trong file .rsp Roslyn bỏ qua nó (CS2023) và nạp csc.rsp mặc định.
  dotnet "$COMPILER_DLL" -noconfig "@$response_file" > "$compiler_log" 2>&1 || compiler_status=$?
  grep -v "^Microsoft (R)\|^Copyright (C)\|^$" "$compiler_log" | sed 's/^/   /' || true
  if [ "$compiler_status" != 0 ] || [ ! -f "$output_file" ]; then
    echo "   LỖI: compile $assembly_name trên $version thất bại" >&2
    failures=$((failures + 1))
    return 1
  fi
  return 0
}

find_test_reference_directory() {
  local version=$1
  local candidate
  for candidate in "$repository/tools/liveops-hub/.cache/test-references/$version" "$home_test_reference_root/$version"; do
    if [ -f "$candidate/nunit.framework.dll" ] && [ -f "$candidate/UnityEngine.TestRunner.dll" ] && [ -f "$candidate/UnityEditor.TestRunner.dll" ]; then
      echo "$candidate"; return 0
    fi
  done
  # 6000.6: dev project/worktree đã import có sẵn DLL trong Library — điền cache luôn để lần sau không phụ thuộc Library.
  if [ "$version" = 6000.6 ] && [ -f "$repository/Library/ScriptAssemblies/UnityEngine.TestRunner.dll" ]; then
    local nunit
    nunit=$(find "$repository/Library/PackageCache" -path '*com.unity.ext.nunit*' -name nunit.framework.dll 2>/dev/null | head -n 1)
    if [ -n "$nunit" ]; then
      mkdir -p "$home_test_reference_root/$version"
      cp "$nunit" "$repository/Library/ScriptAssemblies/UnityEngine.TestRunner.dll" \
        "$repository/Library/ScriptAssemblies/UnityEditor.TestRunner.dll" "$home_test_reference_root/$version/"
      echo "$home_test_reference_root/$version"; return 0
    fi
  fi
  return 1
}

case "$unity_selection" in
  all) versions="2022.3 6000.6";;
  2022|2022.3) versions="2022.3";;
  6000|6000.6) versions="6000.6";;
  *) fail_usage "--unity phải là 2022, 6000 hoặc all";;
esac

has_part() { case ",$parts," in *",$1,"*) return 0;; *) return 1;; esac; }

for version in $versions; do
  if [ "$version" = 2022.3 ]; then
    contents=$UNITY_2022_CONTENTS; managed=$contents/Managed/UnityEngine
    netstandard=$contents/NetStandard/ref/2.1.0/netstandard.dll
    defines_version=$(version_defines 2022 3); unity_warnings=""
  else
    contents=$UNITY_6000_CONTENTS; managed=$contents/Resources/Scripting/Managed/UnityEngine
    netstandard=$contents/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll
    defines_version=$(version_defines 6000 6); unity_warnings="-warnaserror+:CS0618,CS0619"
  fi
  [ -f "$netstandard" ] || fail_usage "thiếu $netstandard (Unity $version chưa cài?)"
  version_output=$output_root/$version
  mkdir -p "$version_output"
  rm -f "$version_output"/*.dll

  # Chỉ DLL module (không facade UnityEngine.dll/UnityEditor.dll): facade trùng type với module sinh CS0433.
  engine_references_file=$version_output/engine.references
  editor_references_file=$version_output/editor.references
  # Shim netfx (mscorlib, System, System.Core...) chuyển kiểu về netstandard như Unity làm cho mọi assembly không phải
  # noEngineReferences; thiếu thì DLL build cho .NET Framework (nunit.framework.dll) báo CS0012 "mscorlib chưa tham chiếu".
  {
    echo "-r:$netstandard"
    reference_arguments $(ls "$(dirname "$(dirname "$netstandard")")/../compat/2.1.0/shims/netfx"/*.dll)
    reference_arguments $(ls "$managed"/UnityEngine.*Module.dll)
  } > "$engine_references_file"
  {
    cat "$engine_references_file"
    reference_arguments $(ls "$managed"/UnityEditor.*Module.dll)
  } > "$editor_references_file"
  core_dll=$version_output/DreamTech.LiveOps.dll
  unity_dll=$version_output/DreamTech.LiveOps.Unity.dll
  editor_dll=$version_output/DreamTech.LiveOps.Editor.dll
  support_dll=$version_output/DreamTech.LiveOps.Tests.Support.dll

  if has_part core || has_part runtime || has_part editor || has_part tests; then
    list_sources "$package/Runtime/Core" > "$version_output/core.sources"
    echo "-r:$netstandard" > "$version_output/core.references"
    compile_assembly "$version" DreamTech.LiveOps "$defines_version" "$version_output/core.references" "-warnaserror+" "$version_output/core.sources" || true
  fi
  if has_part runtime || has_part editor || has_part tests; then
    list_sources "$package/Runtime/Unity" > "$version_output/runtime.sources"
    { cat "$engine_references_file"; echo "-r:$core_dll"; } > "$version_output/runtime.references"
    compile_assembly "$version" DreamTech.LiveOps.Unity "$defines_version" "$version_output/runtime.references" "$unity_warnings" "$version_output/runtime.sources" || true
  fi
  if has_part editor || has_part tests; then
    if [ -d "$package/Editor" ]; then
      list_sources "$package/Editor" > "$version_output/editor.sources"
      { cat "$editor_references_file"; echo "-r:$core_dll"; echo "-r:$unity_dll"; } > "$version_output/editor.references"
      compile_assembly "$version" DreamTech.LiveOps.Editor "$defines_version;UNITY_EDITOR;UNITY_EDITOR_OSX" "$version_output/editor.references" "$unity_warnings" "$version_output/editor.sources" || true
    elif has_part editor; then
      echo "== $version DreamTech.LiveOps.Editor: bỏ qua (chưa có $PACKAGE_RELATIVE_PATH/Editor)"
    fi
  fi
  if has_part tests; then
    if ! test_references=$(find_test_reference_directory "$version"); then
      echo "LỖI: thiếu nunit/TestRunner DLL cho $version — chạy make-temp-project-2022.sh --bootstrap (2022.3) hoặc import 6000.6 trên worktree" >&2
      failures=$((failures + 1))
      continue
    fi
    test_defines="$defines_version;UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_INCLUDE_TESTS"
    {
      cat "$editor_references_file"
      echo "-r:$test_references/nunit.framework.dll"
      echo "-r:$test_references/UnityEngine.TestRunner.dll"
      echo "-r:$test_references/UnityEditor.TestRunner.dll"
      echo "-r:$core_dll"
    } > "$version_output/test-base.references"

    list_sources "$package/Tests/Editor/Support" > "$version_output/support.sources"
    { echo "-r:$netstandard"; echo "-r:$core_dll"; } > "$version_output/support.references"
    compile_assembly "$version" DreamTech.LiveOps.Tests.Support "$test_defines" "$version_output/support.references" "" "$version_output/support.sources" || true
    support_reference=""
    if [ -f "$support_dll" ]; then support_reference="-r:$support_dll"; fi

    list_sources "$package/Tests/Editor" -not -path '*/Unity/*' -not -path '*/Hub/*' -not -path '*/Support/*' > "$version_output/tests.sources"
    { cat "$version_output/test-base.references"; echo_if_set "$support_reference"; } > "$version_output/tests.references"
    compile_assembly "$version" DreamTech.LiveOps.Tests "$test_defines" "$version_output/tests.references" "$unity_warnings" "$version_output/tests.sources" || true

    list_sources "$package/Tests/Editor/Unity" > "$version_output/unity-tests.sources"
    { cat "$version_output/test-base.references"; echo "-r:$unity_dll"; echo_if_set "$support_reference"; } > "$version_output/unity-tests.references"
    compile_assembly "$version" DreamTech.LiveOps.Unity.Tests "$test_defines" "$version_output/unity-tests.references" "$unity_warnings" "$version_output/unity-tests.sources" || true

    if [ -d "$package/Tests/Editor/Hub" ]; then
      list_sources "$package/Tests/Editor/Hub" > "$version_output/editor-tests.sources"
      { cat "$version_output/test-base.references"; echo "-r:$unity_dll"; echo "-r:$editor_dll"; echo_if_set "$support_reference"; } > "$version_output/editor-tests.references"
      compile_assembly "$version" DreamTech.LiveOps.Editor.Tests "$test_defines" "$version_output/editor-tests.references" "$unity_warnings" "$version_output/editor-tests.sources" || true
    fi
  fi
done

if [ "$failures" != 0 ]; then
  echo "COMPILE CHECK FAILED ($failures assembly lỗi)"
  exit 1
fi
echo "COMPILE CHECK OK"
