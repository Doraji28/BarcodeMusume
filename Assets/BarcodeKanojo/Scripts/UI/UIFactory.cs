using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BarcodeKanojo
{
    /// <summary>
    /// 씬 파일 없이 코드로 uGUI를 만드는 도우미. (에디터에서 손으로 배치해도 되지만,
    /// 이렇게 하면 스크립트만 복사해도 어느 씬에서나 앱이 뜬다.)
    /// </summary>
    public static class UIFactory
    {
        static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font != null) return font;
                try
                {
                    // 한글 글리프가 있는 OS 폰트를 우선 찾고, 없으면 Unity 기본 폰트(OS 폴백 사용)로.
                    font = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Noto Sans KR", "NotoSansCJK",
                        "Apple SD Gothic Neo", "Droid Sans Fallback",
                    }, 32);
                }
                catch { font = null; }
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>앵커(0~1)와 오프셋(픽셀)으로 위치를 정한다.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0f)
        {
            return rt.Place(Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Color color, UnityAction onClick, out Text text, int fontSize = 40)
        {
            var img = Panel(name, parent, color);
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            text = Label("Label", img.transform, label, fontSize, Color.white);
            text.rectTransform.Stretch();
            return btn;
        }

        public static InputField Input(string name, Transform parent, string placeholder, int fontSize = 38)
        {
            var bg = Panel(name, parent, Color.white);
            var field = bg.gameObject.AddComponent<InputField>();

            var text = Label("Text", bg.transform, "", fontSize, new Color(0.15f, 0.15f, 0.2f), TextAnchor.MiddleLeft);
            text.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(24, 6), new Vector2(-24, -6));
            text.supportRichText = false;

            var ph = Label("Placeholder", bg.transform, placeholder, fontSize, new Color(0.6f, 0.6f, 0.65f), TextAnchor.MiddleLeft);
            ph.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(24, 6), new Vector2(-24, -6));
            ph.fontStyle = FontStyle.Italic;

            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = bg;
            return field;
        }
    }
}
