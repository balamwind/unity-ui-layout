// UI Layout Gen - 메뉴 및 batchmode 진입점.
// batchmode: Unity -batchmode -quit -projectPath <경로> -executeMethod UILayoutGen.UIGenMenu.GenerateAndCapture -logFile <로그>
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UILayoutGen
{
    public static class UIGenMenu
    {
        static string SpecDir => Path.Combine(Application.dataPath, "UI_Generated/Specs");
        static string ReportPath => Path.Combine(Application.dataPath, "UI_Generated/Output~/report.txt");

        [MenuItem("Tools/UI Layout Gen/Generate And Capture", priority = 0)]
        public static void GenerateAndCapture()
        {
            Begin("Generate And Capture");
            GenerateInternal();
            CaptureInternal(draft: false);
            End();
        }

        [MenuItem("Tools/UI Layout Gen/Generate Prefabs", priority = 1)]
        public static void GenerateAll()
        {
            Begin("Generate Prefabs");
            GenerateInternal();
            End();
        }

        [MenuItem("Tools/UI Layout Gen/Capture Prefabs", priority = 2)]
        public static void CaptureAll()
        {
            Begin("Capture Prefabs");
            CaptureInternal(draft: false);
            End();
        }

        [MenuItem("Tools/UI Layout Gen/Capture Drafts (Specs Only)", priority = 20)]
        public static void CaptureDrafts()
        {
            Begin("Capture Drafts");
            CaptureInternal(draft: true);
            End();
        }

        static void GenerateInternal()
        {
            foreach (var spec in LoadSpecs())
                UIBuilder.SavePrefab(UISpecBuilder.Build(spec, false), spec.screen);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void CaptureInternal(bool draft)
        {
            foreach (var spec in LoadSpecs())
            {
                foreach (var res in UIScreenshot.TestResolutions(spec))
                {
                    if (draft) UIScreenshot.CaptureDraft(spec, res.x, res.y);
                    else UIScreenshot.CapturePrefab($"{UIBuilder.PrefabFolder}/{spec.screen}.prefab", spec, res.x, res.y);
                }
            }
        }

        static List<UIScreenSpec> LoadSpecs()
        {
            var list = new List<UIScreenSpec>();
            if (!Directory.Exists(SpecDir))
            {
                UIBuilder.Error("스펙 폴더 없음: Assets/UI_Generated/Specs");
                return list;
            }
            foreach (var file in Directory.GetFiles(SpecDir, "*.json"))
            {
                try
                {
                    var spec = JsonUtility.FromJson<UIScreenSpec>(File.ReadAllText(file));
                    if (spec == null || string.IsNullOrEmpty(spec.screen))
                    {
                        UIBuilder.Error("screen 이름이 없는 스펙: " + Path.GetFileName(file));
                        continue;
                    }
                    list.Add(spec);
                }
                catch (System.Exception ex)
                {
                    UIBuilder.Error($"스펙 파싱 실패 {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            return list;
        }

        internal static void Begin(string title)
        {
            UIBuilder.Report.Clear();
            UIBuilder.Report.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}");
            UIBuilder.Report.AppendLine("텍스트 모드: " + (UIBuilder.UsingTMP ? "TextMeshPro" : "Legacy Text"));
        }

        internal static void End()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, UIBuilder.Report.ToString());
            Debug.Log("[UILayoutGen] 완료. 리포트: " + ReportPath);
        }
    }
}
