using UnityEngine;

namespace ProbablyLua
{
    public sealed class LuaUpdateDriver : MonoBehaviour
    {
        internal LuaHost? Host { get; set; }

        private void Update() => Host?.Tick();
    }
}
