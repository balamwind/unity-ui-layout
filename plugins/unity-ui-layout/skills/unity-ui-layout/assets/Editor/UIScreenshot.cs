// UI Layout Gen - UI 루트를 Preview Scene에서 렌더링해 PNG로 저장한다.
// 열려 있는 씬은 건드리지 않는다. batchmode에서는 -nographics 없이 실행해야 한다.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UILayoutGen
{
    public static class UIScreenshot
    {
        /// Unity가 임포트하지 않는 폴더 (이름이 ~로 끝남)
        public static string OutputDir => Path.Combine(Application.dataPath, "UI_Generated/Output~/Screenshots");

        /// 저장된 프리팹을 Preview Scene에 인스턴스로 올려 캡처한다.
        public static string CapturePrefab(string prefabPath, UIScreenSpec spec, int width, int height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { UIBuilder.Error("캡처할 프리팹 없음: " + prefabPath); return null; }
            return Run(spec, width, height, "", scene => (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene));
        }

        /// 스펙만으로 임시 색 초안을 만들어 캡처한다 (프리팹 저장 안 함).
        public static string CaptureDraft(UIScreenSpec spec, int width, int height)
        {
            return Run(spec, width, height, "_draft", scene =>
            {
                var r = UISpecBuilder.Build(spec, true);
                SceneManager.MoveGameObjectToScene(r, scene);
                return r;
            });
        }

        static string Run(UIScreenSpec spec, int width, int height, string suffix, System.Func<Scene, GameObject> makeRoot)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                var root = makeRoot(scene);

                var camGo = new GameObject("UILayoutGen_CaptureCam");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 1f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                int uiLayer = LayerMask.NameToLayer("UI");
                cam.cullingMask = uiLayer >= 0 ? 1 << uiLayer : ~0;

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;

                // CanvasScaler는 에디터 캡처에서 화면 크기를 제대로 못 읽을 수 있어 같은 공식으로 직접 계산
                var scaler = root.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                canvas.scaleFactor = ScaleFactor(width, height, spec.refWidth, spec.refHeight, spec.match);

                ApplyPreviewSafeArea(root, spec);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                Canvas.ForceUpdateCanvases();

                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                Directory.CreateDirectory(OutputDir);
                string file = Path.Combine(OutputDir, $"{spec.screen}{suffix}_{width}x{height}.png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                UIBuilder.Report.AppendLine("캡처: " + file);
                return file;
            }
            catch (System.Exception ex)
            {
                UIBuilder.Error($"{spec.screen} 캡처 실패 ({width}x{height}): {ex.Message}");
                return null;
            }
            finally
            {
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                if (tex != null) Object.DestroyImmediate(tex);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        public static float ScaleFactor(float w, float h, float refW, float refH, float match)
        {
            float logW = Mathf.Log(w / refW, 2);
            float logH = Mathf.Log(h / refH, 2);
            return Mathf.Pow(2, Mathf.Lerp(logW, logH, match));
        }

        /// 실기기 SafeArea 컴포넌트는 런타임에만 동작하므로, 캡처 때는 스펙의 previewSafeArea로 노치를 흉내 낸다.
        static void ApplyPreviewSafeArea(GameObject root, UIScreenSpec spec)
        {
            var safe = root.transform.Find("SafeArea") as RectTransform;
            var ins = spec.previewSafeArea;
            if (safe == null || ins == null) return;
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = new Vector2(ins.left, ins.bottom);
            safe.offsetMax = new Vector2(-ins.right, -ins.top);
        }

        /// 기준 해상도 + 비율이 다른 기기 2종 (세로: 20:9 폰, 4:3 태블릿 / 가로: 20:9 폰, 4:3 태블릿)
        public static Vector2Int[] TestResolutions(UIScreenSpec spec)
        {
            int w = Mathf.RoundToInt(spec.refWidth), h = Mathf.RoundToInt(spec.refHeight);
            if (h >= w)
                return new[] { new Vector2Int(w, h), new Vector2Int(w, Mathf.RoundToInt(w * 20f / 9f)), new Vector2Int(Mathf.RoundToInt(h * 3f / 4f), h) };
            return new[] { new Vector2Int(w, h), new Vector2Int(Mathf.RoundToInt(h * 20f / 9f), h), new Vector2Int(Mathf.RoundToInt(h * 4f / 3f), h) };
        }
    }
}
