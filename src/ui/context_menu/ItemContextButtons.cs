using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ProbablyLua
{
    // Use the actual grid contents rather than assuming there are seven buttons.
    // This also makes the presentation code independent of action names.
    internal static class ItemContextButtons
    {
        internal static IEnumerable<RichTextElement> Visible(PixelWindow window)
        {
            if (window == null || window.childElement is not GridPixelElement grid)
                yield break;

            for (int row = 0; row < grid.gridHeight; row++)
            {
                for (int col = 0; col < grid.gridWidth; col++)
                {
                    if (grid.GetElement(col, row) is RichTextElement button &&
                        button.handler != null &&
                        button.handler.gameObject.activeSelf)
                    {
                        yield return button;
                    }
                }
            }
        }
    }
}
