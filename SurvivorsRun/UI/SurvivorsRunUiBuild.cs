using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SHIN
{
    /// <summary>SurvivorsRun UI 런타임 빌드 헬퍼. 프리팹이 비어 있어도 동작하게 한다.</summary>
    internal static class SurvivorsRunUiBuild
    {
        public static RectTransform StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
                rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        public static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image AddImage(GameObject go, Color color, bool raycast = false)
        {
            var img = go.GetComponent<Image>();
            if (img == null)
                img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI AddText(
            GameObject go,
            string text,
            float fontSize,
            TextAlignmentOptions align,
            Color? color = null)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();

            ApplyKoreanFont(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        /// <summary>
        /// TMP 기본 폰트(Pretendard-SemiBold)를 강제 적용. LiberationSans면 한글이 깨진다.
        /// </summary>
        public static void ApplyKoreanFont(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                return;

            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        public static void ApplyKoreanFontRecursive(Transform root)
        {
            if (root == null)
                return;

            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var i = 0; i < texts.Length; i++)
                ApplyKoreanFont(texts[i]);
        }

        public static Button AddButton(GameObject go, Color bg)
        {
            AddImage(go, bg, raycast: true);
            var button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        public static void SetAnchored(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }
    }
}
