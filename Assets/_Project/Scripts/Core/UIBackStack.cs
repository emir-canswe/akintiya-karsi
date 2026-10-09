using System;
using System.Collections.Generic;

namespace Upstream.Core
{
    /// <summary>
    /// Open panels (shop, settings) register a close action here so the Android back button closes the
    /// top-most panel instead of quitting the app or pausing the game underneath.
    /// </summary>
    public static class UIBackStack
    {
        private static readonly List<Action> Stack = new List<Action>();

        /// <summary>A full-screen overlay (the test ad) is up: the back button does nothing until it closes.</summary>
        public static bool Blocked { get; set; }

        public static void Push(Action close)
        {
            Stack.Remove(close);
            Stack.Add(close);
        }

        public static void Remove(Action close) => Stack.Remove(close);

        /// <summary>Closes the top-most panel. Returns false when nothing was open.</summary>
        public static bool TryPop()
        {
            if (Stack.Count == 0) return false;
            Action close = Stack[Stack.Count - 1];
            Stack.RemoveAt(Stack.Count - 1);
            close?.Invoke();
            return true;
        }

        public static void Clear() => Stack.Clear();
    }
}
