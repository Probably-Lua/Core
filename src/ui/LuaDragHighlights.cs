using System.Collections.Generic;

namespace ProbablyLua
{
    internal static class LuaDragHighlights
    {
        internal static void Clear(List<TreeNodeRender> nodes)
        {
            foreach (TreeNodeRender node in nodes)
                if (node != null && node.image != null)
                    node.image.enabled = false;
                    
            // Native EndDrag/AbortDrag hide these nodes but retain the list.
            // Each later drag otherwise revisits every previous drag's nodes.
            nodes.Clear();
        }
    }
}
