// UI Layout Gen - TextMeshPro 모드. TMP를 쓰기로 한 경우에만 Assets/UI_Generated/Editor/에 복사한다.
// 레거시 Text 모드로 가려면 이 파일을 넣지 않는다 (있으면 삭제).
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UILayoutGen
{
    [InitializeOnLoad]
    static class UIBuilderTMP
    {
        static UIBuilderTMP()
        {
            UIBuilder.TmpFactory = Create;
        }

        static Graphic Create(GameObject go, string text, float fontSize, UIBuilder.TextAlign align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            if (!string.IsNullOrEmpty(UIBuilder.TmpFontPath))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIBuilder.TmpFontPath);
                if (font != null) t.font = font;
                else UIBuilder.Warn("TMP 폰트 에셋 없음: " + UIBuilder.TmpFontPath);
            }
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = Map(align);
            t.color = Color.white;
            return t;
        }

        static TextAlignmentOptions Map(UIBuilder.TextAlign a)
        {
            switch (a)
            {
                case UIBuilder.TextAlign.TopLeft: return TextAlignmentOptions.TopLeft;
                case UIBuilder.TextAlign.Top: return TextAlignmentOptions.Top;
                case UIBuilder.TextAlign.TopRight: return TextAlignmentOptions.TopRight;
                case UIBuilder.TextAlign.Left: return TextAlignmentOptions.Left;
                case UIBuilder.TextAlign.Right: return TextAlignmentOptions.Right;
                case UIBuilder.TextAlign.BottomLeft: return TextAlignmentOptions.BottomLeft;
                case UIBuilder.TextAlign.Bottom: return TextAlignmentOptions.Bottom;
                case UIBuilder.TextAlign.BottomRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}
