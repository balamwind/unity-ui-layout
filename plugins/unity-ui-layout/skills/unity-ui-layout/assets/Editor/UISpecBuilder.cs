// UI Layout Gen - 스펙 JSON(Assets/UI_Generated/Specs/*.json)을 읽어 UI 계층을 만든다.
// 배치도 렌더러(render_layout.py)와 같은 스펙·같은 배치 규칙을 사용한다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UILayoutGen
{
    [Serializable]
    public class SafeInsets
    {
        public float left, right, top, bottom;
    }

    [Serializable]
    public class UIElementSpec
    {
        public string name = "";
        public string parent = "SafeArea";   // "Root" | "SafeArea" | 앞에 나온 요소 이름
        public string type = "image";        // group | image | text | button | dim
        public string anchor = "Center";     // UIBuilder.Anchor 이름
        public float x, y, w, h;
        public string role = "Panel";        // Background | Panel | Button | Icon | Bar (임시 색)
        public string sprite = "";           // Assets/... 스프라이트 경로, 비우면 TEMP
        public string spriteName = "";       // 스프라이트 시트의 서브 스프라이트 이름
        public string text = "";
        public float fontSize = 40;
        public string align = "Center";      // UIBuilder.TextAlign 이름
        public string layout = "";           // "" | vertical | horizontal | grid
        public string childAlign = "";       // TextAnchor 이름, 비우면 기본값
        public float spacing;
        public int padding;
        public float cellW = 100, cellH = 100;
    }

    [Serializable]
    public class UIScreenSpec
    {
        public string screen = "";
        public float refWidth = 1080, refHeight = 1920, match;
        public int sortingOrder;
        public SafeInsets previewSafeArea = new SafeInsets();
        public string tmpFont = "";
        public UIElementSpec[] elements = new UIElementSpec[0];
    }

    public static class UISpecBuilder
    {
        /// forceTemp = true면 스프라이트를 무시하고 전부 임시 색으로 만든다 (초안 캡처용).
        public static GameObject Build(UIScreenSpec spec, bool forceTemp)
        {
            UIBuilder.BeginSession(spec.screen);
            UIBuilder.TmpFontPath = spec.tmpFont ?? "";

            var root = UIBuilder.CreateScreenRoot(spec.screen, new Vector2(spec.refWidth, spec.refHeight),
                spec.match, spec.sortingOrder, out var safe);

            var map = new Dictionary<string, RectTransform>
            {
                { "Root", (RectTransform)root.transform },
                { "SafeArea", safe },
            };
            var layoutParents = new HashSet<string>();

            foreach (var e in spec.elements)
            {
                if (string.IsNullOrEmpty(e.name)) { UIBuilder.Error($"{spec.screen}: 이름 없는 요소"); continue; }
                if (map.ContainsKey(e.name)) { UIBuilder.Error($"{spec.screen}: 이름 중복 {e.name}"); continue; }
                if (!map.TryGetValue(e.parent ?? "SafeArea", out var parent))
                {
                    UIBuilder.Error($"{spec.screen}: {e.name}의 부모 '{e.parent}'가 없거나 자식보다 뒤에 있음");
                    continue;
                }

                if (!Enum.TryParse(e.anchor, out UIBuilder.Anchor anchor))
                {
                    if (e.type != "dim" && !layoutParents.Contains(e.parent))
                        UIBuilder.Warn($"{spec.screen}: {e.name} 앵커 '{e.anchor}' 인식 불가 → Center");
                    anchor = UIBuilder.Anchor.Center;
                }
                Enum.TryParse(e.align, out UIBuilder.TextAlign align);

                var pos = new Vector2(e.x, e.y);
                var size = new Vector2(e.w, e.h);
                Sprite sprite = forceTemp ? null : UIBuilder.LoadSprite(e.sprite, string.IsNullOrEmpty(e.spriteName) ? null : e.spriteName);
                Color color = RoleColor(e.role);

                RectTransform rt;
                switch (e.type)
                {
                    case "group":
                        rt = UIBuilder.CreateRect(e.name, parent, anchor, pos, size);
                        break;
                    case "text":
                        rt = UIBuilder.CreateText(e.name, parent, anchor, pos, size, e.text, e.fontSize, align).rectTransform;
                        break;
                    case "button":
                        rt = (RectTransform)UIBuilder.CreateButton(e.name, parent, anchor, pos, size, sprite, color,
                            string.IsNullOrEmpty(e.text) ? null : e.text, e.fontSize).transform;
                        break;
                    case "dim":
                        rt = UIBuilder.CreateDim(parent).rectTransform;
                        break;
                    case "image":
                        rt = UIBuilder.CreateImage(e.name, parent, anchor, pos, size, sprite, color).rectTransform;
                        break;
                    default:
                        UIBuilder.Warn($"{spec.screen}: {e.name} 타입 '{e.type}' 인식 불가 → image");
                        rt = UIBuilder.CreateImage(e.name, parent, anchor, pos, size, sprite, color).rectTransform;
                        break;
                }

                if (layoutParents.Contains(e.parent))
                    UIBuilder.SetPreferredSize(rt, e.w, e.h);

                if (!string.IsNullOrEmpty(e.layout))
                {
                    AddLayout(rt, e, spec.screen);
                    layoutParents.Add(e.name);
                }

                map[e.name] = rt;
            }

            UIBuilder.EndSession();
            return root;
        }

        static void AddLayout(RectTransform rt, UIElementSpec e, string screen)
        {
            switch (e.layout)
            {
                case "vertical":
                    UIBuilder.AddVerticalLayout(rt, e.spacing, e.padding, ParseAlign(e.childAlign, TextAnchor.UpperCenter));
                    break;
                case "horizontal":
                    UIBuilder.AddHorizontalLayout(rt, e.spacing, e.padding, ParseAlign(e.childAlign, TextAnchor.MiddleLeft));
                    break;
                case "grid":
                    UIBuilder.AddGridLayout(rt, new Vector2(e.cellW, e.cellH), e.spacing, e.padding,
                        ParseAlign(e.childAlign, TextAnchor.UpperCenter));
                    break;
                default:
                    UIBuilder.Warn($"{screen}: {e.name} 레이아웃 '{e.layout}' 인식 불가 → 무시");
                    break;
            }
        }

        static TextAnchor ParseAlign(string s, TextAnchor fallback)
        {
            return !string.IsNullOrEmpty(s) && Enum.TryParse(s, out TextAnchor a) ? a : fallback;
        }

        static Color RoleColor(string role)
        {
            switch (role)
            {
                case "Background": return UIBuilder.TempColor.Background;
                case "Button": return UIBuilder.TempColor.Button;
                case "Icon": return UIBuilder.TempColor.Icon;
                case "Bar": return UIBuilder.TempColor.Bar;
                default: return UIBuilder.TempColor.Panel;
            }
        }
    }
}
