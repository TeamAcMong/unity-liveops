#!/bin/bash
# run-playmode.sh — chạy test PlayMode (demo trong Assets/Demo/Tests) qua unity-slot.sh, kết luận từ XML.
#
# Vì sao tách script: PlayMode chỉ chạy trên dev project 6000.6 (demo dùng API Unity 6, không đi theo package) và theo ma
# trận của CLAUDE.md gốc chạy -nographics; phần đọc XML, slot, hạn giờ dùng chung run-editmode.sh để hai đường không lệch.
#
# Cách dùng:
#   run-playmode.sh [--unity 6000] [--project <worktree>] [--repository <worktree>] [--filter <regex>] [--results <xml>]
#                   [--timeout GIÂY] [--graphics]
# Thoát như run-editmode.sh.

set -uo pipefail

script_directory=$(cd "$(dirname "$0")" && pwd)
arguments=()
unity_given=0

while [ "$#" -gt 0 ]; do
  case "$1" in
    --help|-h) sed -n '2,11p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    --unity)
      [ "$#" -ge 2 ] || { echo "run-playmode.sh: --unity cần giá trị" >&2; exit 2; }
      case "$2" in
        6000|6000.6) ;;
        *) echo "run-playmode.sh: PlayMode demo chỉ chạy trên 6000.6 (demo dùng API Unity 6)" >&2; exit 2;;
      esac
      unity_given=1; arguments+=("$1" "$2"); shift 2;;
    --category|--category-mode|--platform)
      echo "run-playmode.sh: '$1' không dùng cho PlayMode" >&2; exit 2;;
    *) arguments+=("$1"); shift;;
  esac
done
[ "$unity_given" = 1 ] || arguments+=(--unity 6000)

exec "$script_directory/run-editmode.sh" --platform PlayMode --category all "${arguments[@]}"
