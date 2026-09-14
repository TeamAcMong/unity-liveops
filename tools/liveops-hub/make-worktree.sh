#!/bin/bash
# make-worktree.sh — tạo worktree riêng cho một gói việc + nhân bản Library/ bằng APFS clone.
#
# Vì sao: gói trong một đợt chạy song song, mỗi gói import Unity và sinh .meta cho file của mình (PD-27). Chung một thư
# mục thì hai Unity mở cùng project (hai GUID cho một file). Worktree cho mỗi gói một cây riêng trên cùng repo git; Library
# nhân bản bằng `cp -c` (clonefile của APFS: không tốn đĩa tới khi ghi) để Unity không import lại từ đầu mỗi worktree.
#
# Cách dùng:
#   make-worktree.sh <gói> [--repository <repo hoặc worktree bất kỳ>] [--base <nhánh>] [--root <thư mục chứa worktree>]
#                    [--no-library]
#   make-worktree.sh --remove <gói> [--repository …] [--root …] [--force]
# Mặc định: base = feature/liveops-hub-p1; root = <thư mục cha của repo chính>/unity-liveops-wt; nhánh = wt/<gói>.
# --remove xoá worktree + nhánh wt/<gói>; từ chối khi worktree còn thay đổi chưa commit hoặc nhánh có commit chưa merge
# vào base, trừ khi --force (không bao giờ tự xoá worktree của gói khác — phải gọi đúng tên).
# In "WORKTREE <đường dẫn>" ở dòng cuối.

set -euo pipefail

readonly DEFAULT_BASE_BRANCH=feature/liveops-hub-p1

repository=${REPOSITORY:-}
base_branch=$DEFAULT_BASE_BRANCH
worktree_root=""
copy_library=1
remove=0
force=0
package=""

fail_usage() { echo "make-worktree.sh: $1" >&2; exit 2; }

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repository) [ "$#" -ge 2 ] || fail_usage "--repository cần đường dẫn"; repository=$2; shift 2;;
    --base) [ "$#" -ge 2 ] || fail_usage "--base cần nhánh"; base_branch=$2; shift 2;;
    --root) [ "$#" -ge 2 ] || fail_usage "--root cần đường dẫn"; worktree_root=$2; shift 2;;
    --no-library) copy_library=0; shift;;
    --remove) remove=1; shift;;
    --force) force=1; shift;;
    --help|-h) sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    -*) fail_usage "tham số lạ '$1'";;
    *) [ -z "$package" ] || fail_usage "chỉ một tên gói"; package=$1; shift;;
  esac
done

[ -n "$package" ] || fail_usage "thiếu tên gói (vd G-CORE)"
case "$package" in
  G-[A-Z0-9]*) ;;
  *) fail_usage "tên gói phải dạng G-<TÊN>, nhận '$package'";;
esac

# Không rơi về repo chứa script, và không nhận repo git bất kỳ của cwd: cwd ở project khác (vd u-icon-match) sẽ tạo
# worktree của NHẦM repo. Chỉ nhận git top-level có package.
if [ -z "$repository" ]; then
  if top_level=$(git rev-parse --show-toplevel 2>/dev/null) && [ -d "$top_level/Packages/com.dreamtech.liveops" ]; then
    repository=$top_level
  else
    fail_usage "thư mục hiện tại không thuộc repo có Packages/com.dreamtech.liveops — truyền --repository <repo hoặc worktree>"
  fi
fi
[ -d "$repository/Packages/com.dreamtech.liveops" ] || fail_usage "--repository $repository không chứa Packages/com.dreamtech.liveops"
echo "make-worktree.sh: repository=$repository" >&2
# Repo chính = thư mục chứa .git chung (worktree nào gọi cũng ra cùng một nơi).
common_git_directory=$(cd "$repository" && git rev-parse --path-format=absolute --git-common-dir)
main_repository=$(dirname "$common_git_directory")
[ -n "$worktree_root" ] || worktree_root=$(dirname "$main_repository")/$(basename "$main_repository")-wt
worktree=$worktree_root/$package
branch=wt/$package

if [ "$remove" = 1 ]; then
  if ! git -C "$main_repository" worktree list --porcelain | grep -qx "worktree $worktree"; then
    echo "không có worktree $worktree"
  else
    if [ "$force" != 1 ] && [ -n "$(git -C "$worktree" status --porcelain)" ]; then
      echo "LỖI: $worktree còn thay đổi chưa commit — dùng --force nếu chắc chắn bỏ" >&2
      exit 1
    fi
    git -C "$main_repository" worktree remove --force "$worktree"
    echo "đã xoá worktree $worktree"
  fi
  # Kiểm nhánh chưa merge PHẢI chạy dù worktree đã bị gỡ tay từ trước (nhánh commit chưa vào base branch không đổi vì
  # thiếu worktree) — nhánh trước đặt kiểm này trong khối "else" ở trên nên khi worktree không còn, branch -D chạy
  # thẳng không kiểm gì (F7). `git branch -d` là lưới an toàn thứ hai: từ chối xoá nếu HEAD chưa merge nhánh này.
  if git -C "$main_repository" show-ref --verify --quiet "refs/heads/$branch"; then
    if [ "$force" = 1 ]; then
      git -C "$main_repository" branch -D "$branch" >/dev/null
    else
      if [ -n "$(git -C "$main_repository" rev-list "$base_branch..$branch" 2>/dev/null)" ]; then
        echo "LỖI: nhánh $branch có commit chưa vào $base_branch — dùng --force nếu chắc chắn bỏ" >&2
        exit 1
      fi
      git -C "$main_repository" branch -d "$branch" >/dev/null
    fi
    echo "đã xoá nhánh $branch"
  fi
  git -C "$main_repository" worktree prune
  exit 0
fi

git -C "$main_repository" show-ref --verify --quiet "refs/heads/$base_branch" || fail_usage "không có nhánh $base_branch"
mkdir -p "$worktree_root"
if [ -d "$worktree" ]; then
  echo "worktree đã có: $worktree ($(git -C "$worktree" branch --show-current) @ $(git -C "$worktree" rev-parse --short HEAD))"
else
  if git -C "$main_repository" show-ref --verify --quiet "refs/heads/$branch"; then
    git -C "$main_repository" worktree add -q "$worktree" "$branch"
  else
    git -C "$main_repository" worktree add -q "$worktree" -b "$branch" "$base_branch"
  fi
  echo "tạo worktree $worktree ($branch từ $base_branch @ $(git -C "$worktree" rev-parse --short HEAD))"
fi

if [ "$copy_library" = 1 ] && [ ! -d "$worktree/Library" ]; then
  if [ -d "$main_repository/Library" ]; then
    # -c: clonefile APFS; không có APFS thì cp báo lỗi → chép thường (chậm, tốn đĩa) để vẫn chạy được.
    if ! /bin/cp -c -R "$main_repository/Library" "$worktree/Library" 2>/dev/null; then
      echo "cảnh báo: cp -c không được (không phải APFS?) — chép Library thường" >&2
      rm -rf "$worktree/Library"
      /bin/cp -R "$main_repository/Library" "$worktree/Library"
    fi
    # EditorInstance.json ghi pid Editor của repo chính — không mang sang để Unity của worktree không đọc nhầm.
    rm -f "$worktree/Library/EditorInstance.json"
    echo "Library nhân bản từ $main_repository/Library"
  else
    echo "cảnh báo: repo chính chưa có Library — lần import đầu của worktree sẽ lâu" >&2
  fi
fi

echo "WORKTREE $worktree"
