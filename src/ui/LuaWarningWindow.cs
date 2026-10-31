using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyLua
{
    internal static class LuaWarningWindow
    {
        private static readonly Color Background = new(0.16f, 0.14f, 0.20f);
        private static readonly Color Foreground = new(0.95f, 0.94f, 0.91f);
        private static readonly Color Secondary = new(0.72f, 0.70f, 0.73f);
        private static readonly Color Accent = new(0.95f, 0.58f, 0.34f);

        public static GameObject Show(string id, string name)
        {
            var root = new GameObject(
                "Probably Lua Warning",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            try
            {
                Object.DontDestroyOnLoad(root);

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 32760;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 900);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var dim = CreateRect(root.transform, "Dim");
                dim.anchorMin = Vector2.zero;
                dim.anchorMax = Vector2.one;
                dim.offsetMin = Vector2.zero;
                dim.offsetMax = Vector2.zero;

                AddImage(dim, new Color(0f, 0f, 0f, 0.55f));

                var panel = CreateRect(root.transform, "Panel");
                panel.sizeDelta = new Vector2(780, 420);

                AddImage(panel, Background);

                var outline = panel.gameObject.AddComponent<Outline>();
                outline.effectColor = Accent;
                outline.effectDistance = new Vector2(2, -2);

                var line = CreateRect(panel, "Accent");
                line.anchorMin = new Vector2(0.5f, 1f);
                line.anchorMax = line.anchorMin;
                line.pivot = new Vector2(0.5f, 1f);
                line.anchoredPosition = Vector2.zero;
                line.sizeDelta = new Vector2(780, 6);

                AddImage(line, Accent);

                AddText(
                    panel,
                    "Lua initialization stopped",
                    32,
                    Foreground,
                    32, -30, 710, 55
                );

                AddText(
                    panel,
                    name,
                    28,
                    Accent,
                    32, -108, 710, 42
                );

                AddText(
                    panel,
                    $"Mod ID: {id}",
                    20,
                    Secondary,
                    32, -155, 710, 35
                );

                var separator = CreateRect(panel, "Separator");
                separator.anchorMin = new Vector2(0f, 1f);
                separator.anchorMax = separator.anchorMin;
                separator.pivot = new Vector2(0f, 1f);
                separator.anchoredPosition = new Vector2(32, -205);
                separator.sizeDelta = new Vector2(716, 2);

                AddImage(separator, new Color(0.4f, 0.38f, 0.43f));

                AddText(
                    panel,
                    "Initialization exceeded the 10-second execution limit.",
                    23,
                    Foreground,
                    32, -226, 710, 65
                );

                AddText(
                    panel,
                    "This mod has been stopped for the current session.",
                    21,
                    Secondary,
                    32, -300, 710, 40
                );

                var buttonRect = CreateRect(panel, "CloseButton");

                buttonRect.anchorMin = new Vector2(1f, 0f);
                buttonRect.anchorMax = buttonRect.anchorMin;
                buttonRect.pivot = new Vector2(1f, 0f);
                buttonRect.anchoredPosition = new Vector2(-28, 24);
                buttonRect.sizeDelta = new Vector2(200, 58);

                var buttonImage = AddImage(
                    buttonRect,
                    new Color(0.35f, 0.48f, 0.51f)
                );

                var button = buttonRect.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonImage;

                button.onClick.AddListener(() => Object.Destroy(root));

                var buttonText = AddText(
                    buttonRect,
                    "Close",
                    24,
                    Foreground,
                    0, 0, 200, 58
                );

                buttonText.alignment = TextAlignmentOptions.Center;

                var textRect = buttonText.rectTransform;
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = textRect.anchorMin;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;

                return root;
            }
            catch
            {
                Object.Destroy(root);
                throw;
            }
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));

            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            return image;
        }

        private static TextMeshProUGUI AddText(
            Transform parent,
            string value,
            float size,
            Color color,
            float x,
            float y,
            float width,
            float height
        )
        {
            var rect = CreateRect(parent, "Text");

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableAutoSizing = false;
            text.richText = false;
            text.raycastTarget = false;

            return text;
        }
    }
}
