using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamTech.LiveOps.Demo.EditorTools
{
    /// <summary>
    /// Dựng scene bàn thử live-ops. Bảng điều khiển vẽ bằng IMGUI nên scene chỉ cần một camera và một GameObject — không cần prefab
    /// hay font. Scene là sản phẩm sinh ra: sửa ở đây rồi dựng lại, đừng sửa tay trong scene.
    ///
    /// <para>Scene tham chiếu asset lịch <see cref="LiveOpsDemoCalendarAssetBuilder.AssetPath"/> qua field serialize của
    /// <see cref="LiveOpsDemo"/> (lịch mặc định khi remote trống + nguồn loại event). Chưa có asset thì dựng luôn, để dựng scene
    /// từ menu trên máy mới không ra scene thiếu lịch.</para>
    ///
    /// <para>Chạy từ menu hoặc batchmode:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod DreamTech.LiveOps.Demo.EditorTools.LiveOpsDemoSceneBuilder.BuildFromCommandLine</c></para>
    /// </summary>
    public static class LiveOpsDemoSceneBuilder
    {
        public const string ScenePath = "Assets/Demo/LiveOpsDemo.unity";
        private static readonly Color StageColor = new Color32(0x10, 0x1A, 0x33, 0xFF);

        [MenuItem("Tools/DreamTech/LiveOps/Demo/Build Demo Scene")]
        public static void Build()
        {
            BuildScene();
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildScene();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = StageColor;
            cameraObject.tag = "MainCamera";

            var demoObject = new GameObject("LiveOpsDemo", typeof(LiveOpsDemo));
            LiveOpsDemo demo = demoObject.GetComponent<LiveOpsDemo>();
            RequireScriptAsset(demo);
            AssignCalendarAsset(demo, LoadOrBuildCalendarAsset());

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[LiveOps Demo] Đã dựng " + ScenePath);
        }

        /// <summary>
        /// Chặn scene hỏng im lặng: đã gặp (G-DEMO, 6000.6 batchmode) lượt import đầu sau khi sửa LiveOpsDemo.cs mà MonoScript chưa
        /// gắn được class — component vẫn thêm được nhưng scene lưu MonoScript nhúng (<c>m_Script: {fileID: …}</c>) thay vì GUID
        /// của file script, và test PlayMode nạp scene sẽ không thấy LiveOpsDemo. Dừng với mã 1 để người chạy import lại rồi dựng.
        /// </summary>
        private static void RequireScriptAsset(LiveOpsDemo demo)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(demo);
            if (script != null && EditorUtility.IsPersistent(script)) return;
            throw new InvalidOperationException("MonoScript của LiveOpsDemo chưa gắn với file script (import chưa xong) — scene sẽ mất tham chiếu " +
                                                "script. Mở lại project hoặc import lại Assets/Demo/Scripts/LiveOpsDemo.cs rồi dựng lại.");
        }

        private static LiveEventCalendarAsset LoadOrBuildCalendarAsset()
        {
            LiveEventCalendarAsset asset = AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(LiveOpsDemoCalendarAssetBuilder.AssetPath);
            return asset != null ? asset : LiveOpsDemoCalendarAssetBuilder.BuildAsset();
        }

        /// <summary>Gán qua SerializedObject vì field là private — không mở setter công khai chỉ để builder dùng.</summary>
        private static void AssignCalendarAsset(LiveOpsDemo demo, LiveEventCalendarAsset asset)
        {
            var serializedDemo = new SerializedObject(demo);
            SerializedProperty property = serializedDemo.FindProperty(LiveOpsDemo.CalendarAssetFieldName);
            if (property == null)
            {
                throw new InvalidOperationException("LiveOpsDemo không có field serialize " + LiveOpsDemo.CalendarAssetFieldName + ".");
            }
            property.objectReferenceValue = asset;
            serializedDemo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Thêm scene vào Build Settings mà KHÔNG xoá scene khác — test PlayMode nạp scene theo tên.</summary>
        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int index = 0; index < scenes.Count; index++)
            {
                if (!string.Equals(scenes[index].path, scenePath, StringComparison.Ordinal)) continue;
                if (scenes[index].enabled) return;
                scenes[index] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
