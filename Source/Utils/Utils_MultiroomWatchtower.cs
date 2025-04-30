using Celeste.Mod.EndHelper.Entities.Misc;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Utils
{
    internal class Utils_MultiroomWatchtower
    {
        internal static void SpawnMultiroomWatchtower()
        {
            if (Engine.Scene is not Level level)
            {
                return;
            }
            if (level.Tracker.GetEntity<Player>() is not Player player || !player.InControl)
            {
                return;
            }
            if (level.Tracker.GetEntity<PortableMultiroomWatchtower>() is PortableMultiroomWatchtower)
            {
                return;
            }

            PortableMultiroomWatchtower portableWatchtower = new(new EntityData
            {
                Position = player.Position,
                Level = level.Session.LevelData
            }, Vector2.Zero);

            level.Add(portableWatchtower);
            portableWatchtower.Interact(player);
        }

        [Tracked(true)]
        private class PortableMultiroomWatchtower : MultiroomWatchtower
        {
            internal PortableMultiroomWatchtower(EntityData data, Vector2 offset) : base(data, offset)
            {
                allowAnywhere = true;
                destroyUponFinishView = true;
                maxSpeedSet *= 2;
                canToggleBlocker = true;
                doOverlapCheck = false;
            }
            internal static bool Exists => Engine.Scene.Tracker.GetEntity<PortableMultiroomWatchtower>() != null;
        }
    }
}
