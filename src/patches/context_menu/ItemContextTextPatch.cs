using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ProbablyLua
{
    // The game's labels are left-aligned, with only a slight color change on
    // hover. Center them and give the hovered row its own background.
    [HarmonyPatch(typeof(ItemContextHandler), "UpdateSelectButtonColor")]
    internal static class ItemContextTextPatch
    {
        private static readonly FieldInfo? BackgroundField = AccessTools.Field(typeof(RichTextElement), "background");

        [HarmonyPostfix]
        private static void Postfix(RichTextElement selectedButton, PixelWindow ___contextWindow)
        {
            if (___contextWindow == null || !___contextWindow.IsVisible() ||
                RenderHandler.current == null)
                return;

            foreach (RichTextElement button in ItemContextButtons.Visible(___contextWindow))
            {
                TreeNodeRender node = button.GetText();
                if (node == null || node.text == null)
                    continue;

                bool hovered = object.ReferenceEquals(button, selectedButton);
                // Vanilla sets these exact colors immediately before this postfix.
                bool disabled = node.text.color == RenderHandler.GetColorFromPalette(
                    hovered
                        ? RenderHandler.ColorPalette.DarkRed
                        : RenderHandler.ColorPalette.DarkGray);

                node.text.alignment = TextAlignmentOptions.Center;
                node.text.fontStyle = hovered ? FontStyles.Bold : FontStyles.Normal;
                node.text.color = RenderHandler.GetColorFromPalette(
                    disabled
                        ? RenderHandler.ColorPalette.LightRed
                        : RenderHandler.ColorPalette.White);

                // RichTextElement gives the text box the entire button width
                // and offsets it by its left padding; subtract both paddings
                // so centering is symmetric inside the button.
                node.rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(1, button.widthPixels - 5),
                    node.rectTransform.sizeDelta.y);

                // RichTextElement.Validate() applies a 3px top offset to the
                // text node while giving it the full button height. Undo that
                // offset so TMP's Center alignment is vertically centered.
                node.rectTransform.anchoredPosition = new Vector2(node.rectTransform.anchoredPosition.x, 0f);

                TreeNodeRender? background = BackgroundField?.GetValue(button) as TreeNodeRender;

                if (background != null && background.image != null)
                {
                    RenderHandler.current.SetHighlightColor(
                        background.image,
                        !hovered
                            ? RenderHandler.BackgroundColor.Tan
                            : (disabled
                                ? RenderHandler.BackgroundColor.Black
                                : RenderHandler.BackgroundColor.Blue),

                        RenderHandler.BackgroundType.Smooth);
                }
            }
        }
    }
}
