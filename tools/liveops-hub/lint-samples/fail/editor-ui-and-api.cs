// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Shell/LiveOpsShellSample.cs
// Mẫu vi phạm UI + API cấm: chữ hiển thị ngoài thư mục catalog, style inline không phép, DisplayDialog,
// icon winbtn_win_close, GetInstanceID, System.Linq, id luật dạng chuỗi.
// lint-expect: package-forbidden
// lint-expect: display-text-only-in-catalog
// lint-expect: ui-inline-style
// lint-expect: ui-inline-style
// lint-expect: display-dialog
// lint-expect: banned-api
// lint-expect: banned-api
// lint-expect: rule-id-literal
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    internal sealed class LiveOpsShellSample
    {
        public static void Build(VisualElement root, Object asset)
        {
            Label title = new Label("Tổng quan");
            title.style.color = Color.red;
            title.style.left = 12; // style-inline-allowed: 11
            EditorUtility.DisplayDialog("Delete", "Delete?", "OK");
            GUIContent closeIcon = EditorGUIUtility.IconContent("winbtn_win_close");
            int identifier = asset.GetInstanceID();
            string ruleId = "overlap-same-type";
            root.Add(title);
        }
    }
}
