using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patchouib.Scrpits.Main
{
    public static class IgniteIntegration
    {
        private static readonly List<Func<IgnitePower, Task>> _handlers = new List<Func<IgnitePower, Task>>();

        public static void Register(Func<IgnitePower, Task> handler)
        {
            if (handler == null)
            {
                return;
            }
            _handlers.Add(handler);
        }

        public static async Task Run(IgnitePower power)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                await _handlers[i](power);
            }
        }
    }
}

