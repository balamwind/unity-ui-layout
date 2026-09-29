// UI Layout Gen - 생성된 UI 프리팹을 지정한 씬에 프리팹 인스턴스로 배치하고 저장한다.
// 배치 계획: Assets/UI_Generated/placement.json
// 저장 전에 원본 씬을 Assets/UI_Generated/Output~/SceneBackups/ 에 백업한다.
// batchmode: -executeMethod UILayoutGen.UIApply.ApplyToScenes
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UILayoutGen
{
    [Serializable]
    public class PlacementScreen
    {
        public string screen = "";
        public bool active = true;   // 팝업·일시정지·결과창은 보통 false
    }

    [Serializable]
    public class PlacementScene
    {
        public string scene = "";    // Assets/Scenes/Title.unity
        public PlacementScreen[] screens = new PlacementScreen[0];
    }

    [Serializable]
    public class PlacementConfig
    {
        public PlacementScene[] scenes = new PlacementScene[0];
    }

    public static class UIApply
    {
        static string ConfigPath => Path.Combine(Application.dataPath, "UI_Generated/placement.json");
        static string BackupDir => Path.Combine(Application.dataPath, "UI_Generated/Output~/SceneBackups");
        static string ProjectDir => Directory.GetParent(Application.dataPath).FullName;

        [MenuItem("Tools/UI Layout Gen/Apply To Scenes", priority = 10)]
        public static void ApplyToScenes()
        {
            UIGenMenu.Begin("Apply To Scenes");
            try { ApplyInternal(); }
            finally { UIGenMenu.End(); }
        }

        static void ApplyInternal()
        {
            if (!File.Exists(ConfigPath))
            {
                UIBuilder.Error("배치 계획 없음: Assets/UI_Generated/placement.json");
                return;
            }
            var cfg = JsonUtility.FromJson<PlacementConfig>(File.ReadAllText(ConfigPath));
            if (cfg == null || cfg.scenes == null || cfg.scenes.Length == 0)
            {
                UIBuilder.Error("placement.json에 씬이 없음");
                return;
            }

            // 에디터에서 작업 중인 씬의 저장 안 된 변경을 먼저 처리
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                UIBuilder.Warn("취소됨: 저장되지 않은 씬 변경이 있음");
                return;
            }

            var setup = Application.isBatchMode ? null : EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var entry in cfg.scenes) ApplyScene(entry);
            }
            finally
            {
                if (setup != null && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void ApplyScene(PlacementScene entry)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.scene) == null)
            {
                UIBuilder.Error("씬 없음: " + entry.scene);
                return;
            }

            UIBuilder.Report.AppendLine("씬: " + entry.scene);
            var scene = EditorSceneManager.OpenScene(entry.scene, OpenSceneMode.Single);
            bool changed = false;

            foreach (var s in entry.screens)
            {
                string prefabPath = $"{UIBuilder.PrefabFolder}/{s.screen}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    UIBuilder.Error($"  프리팹 없음: {prefabPath}");
                    continue;
                }
                if (FindInstance(scene, prefabPath) != null)
                {
                    UIBuilder.Report.AppendLine($"  이미 있음 (건너뜀): {s.screen}");
                    continue;
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.SetActive(s.active);
                UIBuilder.Report.AppendLine($"  추가: {s.screen} ({(s.active ? "활성" : "비활성")})");
                changed = true;
            }

            if (EnsureEventSystem(scene)) changed = true;
            ReportOtherCanvases(scene);

            if (!changed)
            {
                UIBuilder.Report.AppendLine("  변경 없음");
                return;
            }

            Backup(entry.scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene)) UIBuilder.Report.AppendLine("  저장 완료");
            else UIBuilder.Error("  저장 실패: " + entry.scene);
        }

        static GameObject FindInstance(Scene scene, string prefabPath)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == prefabPath)
                        return t.gameObject;
            return null;
        }

        static bool EnsureEventSystem(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<EventSystem>(true) != null) return false;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(go, scene);

            Type module = typeof(StandaloneInputModule);
#if ENABLE_INPUT_SYSTEM
            // 새 Input System이 켜져 있으면 StandaloneInputModule은 에러를 내므로 전용 모듈 사용
            var inputSystemModule = TypeCache.GetTypesDerivedFrom<BaseInputModule>()
                .FirstOrDefault(t => t.FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputSystemModule != null) module = inputSystemModule;
#endif
            go.AddComponent(module);
            UIBuilder.Report.AppendLine($"  EventSystem 추가 ({module.Name})");
            return true;
        }

        static void ReportOtherCanvases(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Canvas>() == null) continue;
                string src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                if (!string.IsNullOrEmpty(src) && src.StartsWith(UIBuilder.PrefabFolder)) continue;
                UIBuilder.Report.AppendLine($"  기존 캔버스: {root.name} (새 UI와 겹치는지 확인 필요)");
            }
        }

        static void Backup(string scenePath)
        {
            Directory.CreateDirectory(BackupDir);
            string src = Path.Combine(ProjectDir, scenePath);
            string dst = Path.Combine(BackupDir, $"{Path.GetFileNameWithoutExtension(scenePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(src, dst, true);
            UIBuilder.Report.AppendLine("  백업: " + dst);
        }
    }
}
