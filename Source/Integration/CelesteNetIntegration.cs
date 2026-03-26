using Celeste.Mod.CelesteNet.Client.Entities;
using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.EndHelper.Utils;
using FrostHelper.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using static Celeste.Mod.EndHelper.EndHelperModule;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class CelesteNetIntegration
    {
        internal static void Load()
        {
            EverestModuleMetadata CelesteNetMetaData = new()
            {
                Name = "CelesteNet.Client",
                Version = new Version(2, 4, 2)
            };
            if (Everest.Loader.DependencyLoaded(CelesteNetMetaData))
            {
                integratingWithCNet = true;
            }
        }

        internal static void Unload()
        {

        }


        // Lock camera to ghost
        internal static String lockedGhostName;
        internal static Vector2? lockedGhostPos = null;

        internal static void LockOnClosestGhost(Level level, Vector2 pos)
        {
            Ghost lockedGhost = GetClosestGhost(level, pos, true);

            if (lockedGhost != null)
            {
                lockedGhostName = lockedGhost.NameTag.Name;
                String message = Dialog.Get("EndHelper_Dialog_MultiroomWatchtower_SpectatingPlayer");
                RoomStatisticsDisplayer.ShowTooltip($"{message} {lockedGhostName}", 2);
            }
            else
            {
                String message = Dialog.Get("EndHelper_Dialog_MultiroomWatchtower_SpectatingUnable");
                RoomStatisticsDisplayer.ShowTooltip(message, 2);
            }
        }
        internal static Ghost GetClosestGhost(Level level, Vector2 pos, bool prioritiseCurrentRoom)
        {
            List<Entity> ghostList = level.Tracker.GetEntities<Ghost>();
            if (prioritiseCurrentRoom)
            {
                Ghost closestGhostInRoom = null;
                float closestGhostDistanceSq = float.MaxValue;

                // Get closest ghost in room
                foreach (Ghost ghost in ghostList.Cast<Ghost>()) {
                    // Ignore ghost if outside screen bounds
                    Rectangle levelBounds = level.Session.LevelData.Bounds;
                    if (!levelBounds.Contains(ghost.Position.ToPoint())) continue;

                    float ghostDistanceSq = Vector2.DistanceSquared(ghost.Position, pos);
                    if (ghostDistanceSq < closestGhostDistanceSq)
                    {
                        closestGhostInRoom = ghost;
                        closestGhostDistanceSq = ghostDistanceSq;
                    }
                }

                // Return if not null
                if (closestGhostInRoom != null) return closestGhostInRoom;

                // Otherwise just get closest entity like normal
            }
            return level.Tracker.GetNearestEntity<Ghost>(pos);
        }

        // Update ghost pos if it exists, or does nothing if it doesn't exist
        internal static Vector2? UpdateGhostPos(Level level)
        {
            Ghost lockedGhost = GetGhostFromName(level, lockedGhostName);

            if (lockedGhost != null)
            {
                lockedGhostPos = lockedGhost.Position;
            }
            return lockedGhostPos;
        }

        internal static Ghost GetGhostFromName(Level level, String name)
        {
            List<Monocle.Entity> ghostList = level.Tracker.GetEntities<Ghost>();
            foreach (Monocle.Entity ghost in ghostList)
            {
                Ghost ghostGhost = (Ghost)ghost;
                if (ghostGhost.NameTag.Name == name)
                {
                    return ghostGhost;
                }
            }
            return null;
        }

        internal static void ClearLockedGhost()
        {
            lockedGhostName = null;
            lockedGhostPos = null;
        }
    }
}
