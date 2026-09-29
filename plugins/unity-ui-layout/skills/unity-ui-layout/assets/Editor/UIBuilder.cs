// UI Layout Gen - unity-ui-layout 스킬이 생성하는 UI 프리팹용 헬퍼.
// Assets/UI_Generated/Editor/ 에 둔다. TextMeshPro에 의존하지 않는다.
// UIBuilderTMP.cs가 함께 있으면 TMP 텍스트, 없으면 레거시 UnityEngine.UI.Text를 사용한다.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UILayoutGen
{
    public static class UIBuilder
    {
        public const string RootFolder = "Assets/UI_Generated";
        public const string PrefabFolder = RootFolder + "/Prefabs";
        const string SafeAreaTypeName = "UILayoutGen.SafeArea";

        public enum Anchor
        {
            TopLeft, TopCenter, TopRight,
            MiddleLeft, Center, MiddleRight,
            BottomLeft, BottomCenter, BottomRight,
            StretchAll, StretchTop, StretchBottom, StretchLeft, StretchRight,
            StretchHorizontal, StretchVertical
        }

        public static class TempColor
        {
            public static readonly Color Background = new Color(0.18f, 0.18f, 0.20f, 1f);
            public static readonly Color Panel      = new Color(0.30f, 0.36f, 0.45f, 1f);
            public static readonly Color Button     = new Color(0.25f, 0.50f, 0.90f, 1f);
            public static readonly Color Icon       = new Color(0.95f, 0.60f, 0.20f, 1f);
            public static readonly Color Bar        = new Color(0.30f, 0.80f, 0.40f, 1f);
            public static readonly Color Dim        = new Color(0f, 0f, 0f, 0.6f);
        }

        public enum TextAlign { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }

        /// UIBuilderTMP.cs가 로드되면 등록된다. null이면 레거시 Text 사용.
        public delegate Graphic TextFactory(GameObject go, string text, float fontSize, TextAlign align);
        public static TextFactory TmpFactory;
        public static bool UsingTMP => TmpFactory != null;
        /// TMP 사용 시 적용할 TMP_FontAsset 경로 (한글 폰트 등). 비어 있으면 TMP 기본 폰트.
        public static string TmpFontPath = "";

        /// 생성·캡처 과정의 로그. UIGenMenu가 Output~/report.txt로 저장한다.
        public static readonly StringBuilder Report = new StringBuilder();

        static string _session;
        static readonly List<string> _tempList = new List<string>();

        public static void Warn(string msg)
        {
            Debug.LogWarning("[UILayoutGen] " + msg);
            Report.AppendLine("WARN: " + msg);
        }

        public static void Error(string msg)
        {
            Debug.LogError("[UILayoutGen] " + msg);
            Report.AppendLine("ERROR: " + msg);
        }

        // ---------- 세션 ----------

        public static void BeginSession(string screenName)
        {
            _session = screenName;
            _tempList.Clear();
        }

        public static void EndSession()
        {
            string line = _tempList.Count > 0
                ? $"{_session}: 임시 이미지 {_tempList.Count}개\n  - " + string.Join("\n  - ", _tempList)
                : $"{_session}: 임시 이미지 없음";
            Debug.Log("[UILayoutGen] " + line);
            Report.AppendLine(line);
            _session = null;
        }

        // ---------- 루트 ----------

        /// 화면 루트(Canvas) + SafeArea 컨테이너 생성. 배경은 root에, 나머지는 safeArea에 붙인다.
        public static GameObject CreateScreenRoot(string name, Vector2 referenceResolution, float match,
            int sortingOrder, out RectTransform safeArea)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = match;

            safeArea = CreateRect("SafeArea", go.transform, Anchor.StretchAll, Vector2.zero, Vector2.zero);
            var safeType = FindType(SafeAreaTypeName);
            if (safeType != null) safeArea.gameObject.AddComponent(safeType);
            else Warn("SafeArea 컴포넌트를 찾지 못함. Assets/UI_Generated/Scripts/SafeArea.cs 확인");

            return go;
        }

        // ---------- 기본 요소 ----------

        public static RectTransform CreateRect(string name, Transform parent, Anchor anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            SetAnchor(rt, anchor);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// sprite가 null이면 tempColor 단색 + 이름에 TEMP_ 접두사.
        public static Image CreateImage(string name, Transform parent, Anchor anchor, Vector2 pos, Vector2 size,
            Sprite sprite, Color tempColor, bool raycastTarget = false)
        {
            bool isTemp = sprite == null;
            var rt = CreateRect(isTemp ? "TEMP_" + name : name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            ApplySprite(img, sprite, tempColor);
            img.raycastTarget = raycastTarget;
            if (isTemp) _tempList.Add($"{name} ({size.x}x{size.y})");
            return img;
        }

        public static Graphic CreateText(string name, Transform parent, Anchor anchor, Vector2 pos, Vector2 size,
            string text, float fontSize, TextAlign align)
        {
            var rt = CreateRect(name, parent, anchor, pos, size);
            Graphic g = TmpFactory != null
                ? TmpFactory(rt.gameObject, text, fontSize, align)
                : CreateLegacyText(rt.gameObject, text, fontSize, align);
            g.raycastTarget = false;
            return g;
        }

        static Graphic CreateLegacyText(GameObject go, string text, float fontSize, TextAlign align)
        {
            var t = go.AddComponent<Text>();
            t.font = LegacyFont();
            t.text = text;
            t.fontSize = Mathf.RoundToInt(fontSize);
            t.alignment = ToTextAnchor(align);
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        static Font LegacyFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        static TextAnchor ToTextAnchor(TextAlign a)
        {
            switch (a)
            {
                case TextAlign.TopLeft: return TextAnchor.UpperLeft;
                case TextAlign.Top: return TextAnchor.UpperCenter;
                case TextAlign.TopRight: return TextAnchor.UpperRight;
                case TextAlign.Left: return TextAnchor.MiddleLeft;
                case TextAlign.Right: return TextAnchor.MiddleRight;
                case TextAlign.BottomLeft: return TextAnchor.LowerLeft;
                case TextAlign.Bottom: return TextAnchor.LowerCenter;
                case TextAlign.BottomRight: return TextAnchor.LowerRight;
                default: return TextAnchor.MiddleCenter;
            }
        }

        /// label이 null이면 텍스트 없는 버튼(아이콘 버튼).
        public static Button CreateButton(string name, Transform parent, Anchor anchor, Vector2 pos, Vector2 size,
            Sprite sprite, Color tempColor, string label, float fontSize = 40)
        {
            var img = CreateImage(name, parent, anchor, pos, size, sprite, tempColor, raycastTarget: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (!string.IsNullOrEmpty(label))
                CreateText("Txt_Label", img.transform, Anchor.StretchAll, Vector2.zero, Vector2.zero,
                    label, fontSize, TextAlign.Center);
            return btn;
        }

        /// 팝업 뒤 어둡게 + 클릭 차단.
        public static Image CreateDim(Transform parent)
        {
            var rt = CreateRect("Img_Dim", parent, Anchor.StretchAll, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = TempColor.Dim;
            img.raycastTarget = true;
            return img;
        }

        // ---------- 레이아웃 ----------

        /// 자식 크기는 SetPreferredSize로 지정한 값을 그대로 쓴다 (배치도 렌더러와 동일한 규칙).
        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rt, float spacing, int padding = 0,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            SetupLayout(g, spacing, padding, align);
            return g;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rt, float spacing, int padding = 0,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            SetupLayout(g, spacing, padding, align);
            return g;
        }

        public static GridLayoutGroup AddGridLayout(RectTransform rt, Vector2 cellSize, float spacing, int padding = 0,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cellSize;
            g.spacing = new Vector2(spacing, spacing);
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = align;
            g.startCorner = GridLayoutGroup.Corner.UpperLeft;
            g.startAxis = GridLayoutGroup.Axis.Horizontal;
            g.constraint = GridLayoutGroup.Constraint.Flexible;
            return g;
        }

        /// 레이아웃 그룹 자식의 크기 지정.
        public static void SetPreferredSize(RectTransform rt, float width = -1, float height = -1)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) le.preferredWidth = width;
            if (height >= 0) le.preferredHeight = height;
        }

        static void SetupLayout(HorizontalOrVerticalLayoutGroup g, float spacing, int padding, TextAnchor align)
        {
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
        }

        // ---------- 에셋 ----------

        /// 경로가 틀리거나 비어 있으면 null (→ 호출 측에서 TEMP 처리됨).
        /// 스프라이트 시트면 spriteName으로 서브 스프라이트 지정.
        public static Sprite LoadSprite(string path, string spriteName = null)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (spriteName == null)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null) Warn($"스프라이트 없음: {path}");
                return s;
            }
            var sub = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(x => x.name == spriteName);
            if (sub == null) Warn($"서브 스프라이트 없음: {path} / {spriteName}");
            return sub;
        }

        static void ApplySprite(Image img, Sprite sprite, Color tempColor)
        {
            if (sprite == null)
            {
                img.sprite = null;
                img.color = tempColor;
                return;
            }
            img.sprite = sprite;
            img.color = Color.white;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            if (img.type == Image.Type.Simple) img.preserveAspect = true;
        }

        // ---------- 저장 ----------

        /// Assets/UI_Generated/Prefabs/<fileName>.prefab 로 저장. 이 폴더 안에서만 덮어쓴다.
        public static void SavePrefab(GameObject root, string fileName)
        {
            EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{fileName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
            Object.DestroyImmediate(root);
            if (ok) Report.AppendLine("저장: " + path);
            else Error("저장 실패: " + path);
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // ---------- 앵커 ----------

        public static void SetAnchor(RectTransform rt, Anchor a)
        {
            Vector2 min, max, pivot;
            switch (a)
            {
                case Anchor.TopLeft:      min = max = pivot = new Vector2(0, 1); break;
                case Anchor.TopCenter:    min = max = pivot = new Vector2(0.5f, 1); break;
                case Anchor.TopRight:     min = max = pivot = new Vector2(1, 1); break;
                case Anchor.MiddleLeft:   min = max = pivot = new Vector2(0, 0.5f); break;
                case Anchor.Center:       min = max = pivot = new Vector2(0.5f, 0.5f); break;
                case Anchor.MiddleRight:  min = max = pivot = new Vector2(1, 0.5f); break;
                case Anchor.BottomLeft:   min = max = pivot = new Vector2(0, 0); break;
                case Anchor.BottomCenter: min = max = pivot = new Vector2(0.5f, 0); break;
                case Anchor.BottomRight:  min = max = pivot = new Vector2(1, 0); break;
                case Anchor.StretchAll:        min = Vector2.zero;          max = Vector2.one;           pivot = new Vector2(0.5f, 0.5f); break;
                case Anchor.StretchTop:        min = new Vector2(0, 1);     max = new Vector2(1, 1);     pivot = new Vector2(0.5f, 1); break;
                case Anchor.StretchBottom:     min = new Vector2(0, 0);     max = new Vector2(1, 0);     pivot = new Vector2(0.5f, 0); break;
                case Anchor.StretchLeft:       min = new Vector2(0, 0);     max = new Vector2(0, 1);     pivot = new Vector2(0, 0.5f); break;
                case Anchor.StretchRight:      min = new Vector2(1, 0);     max = new Vector2(1, 1);     pivot = new Vector2(1, 0.5f); break;
                case Anchor.StretchHorizontal: min = new Vector2(0, 0.5f);  max = new Vector2(1, 0.5f);  pivot = new Vector2(0.5f, 0.5f); break;
                case Anchor.StretchVertical:   min = new Vector2(0.5f, 0);  max = new Vector2(0.5f, 1);  pivot = new Vector2(0.5f, 0.5f); break;
                default:                  min = max = pivot = new Vector2(0.5f, 0.5f); break;
            }
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        static System.Type FindType(string fullName)
        {
            return TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.FullName == fullName);
        }
    }
}
