using Celeste.Mod.EndHelper;
using MonoMod.RuntimeDetour;

using Monocle;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.EndHelper.Entities.Misc;
using IL.Monocle;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using System.Reflection.PortableExecutable;
using System.Collections;
using System.Collections.Specialized;
using static Celeste.Mod.EndHelper.EndHelperModule;
using MonoMod.ModInterop;
using On.Monocle;
using MonoMod.Utils;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.EndHelper.Integration
{
    [ModImportName("SpeedrunTool.SaveLoad")]
    public static class SpeedrunToolImport
    {
        public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;
        public static Action<Monocle.Entity, bool> IgnoreSaveState;
        public static Action<object> Unregister;
    }

    public static class SpeedrunToolIntegration
    {
        public static bool SpeedrunToolInstalled;
        private static object action;
        internal static void Load()
        {
            typeof(SpeedrunToolImport).ModInterop();
            SpeedrunToolInstalled = SpeedrunToolImport.IgnoreSaveState is not null;
            AddSaveLoadAction();
            Logger.Log(LogLevel.Info, "EndHelper/SpeedrunToolIntegration", $"initialise stuff perhaps. {SpeedrunToolInstalled}");
        }

        internal static void Unload()
        {
            RemoveSaveLoadAction();
        }

        private static void AddSaveLoadAction()
        {
            if (!SpeedrunToolInstalled)
            {
                return;
            }
            Logger.Log(LogLevel.Info, "EndHelper/SpeedrunToolIntegration", $"hmmm");

            action = SpeedrunToolImport.RegisterSaveLoadAction(
                // Save State - Action<Dictionary<Type, Dictionary<string, object>>
                (_, level) => {

                },

                // Load State - Action<Dictionary<Type, Dictionary<string, object>>, Level>
                (_, level) => {

                },

                // Clear State - Action
                null,

                // Level before Save State - Action<Level>
                null,

                // Level before Load State - Action<Level>
                (level) =>
                {
                    OnLoadState(level);
                },

                // preCloneEntities - Action
                null
            );
        }

        private static void RemoveSaveLoadAction()
        {
            if (SpeedrunToolInstalled)
            {
                SpeedrunToolImport.Unregister(action);
            }
        }

#pragma warning disable IDE0051  // Private method is unused


        private static void OnLoadState(Level preloadLevel)
        {
            // +1 to death count =) unless prevented

            if (preloadLevel.Tracker.GetEntity<Player>() is Player player && preloadLevel.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                if (!player.Dead)
                {
                    if (EndHelperModule.Settings.RoomStatMenu.DeathIgnoreLoadAfterDeath && timeSinceRespawn <= 30)
                    {
                        // Do not increment death count. Instead make the ignore death from load state after respawn icon appear instead
                        EndHelperModule.externalDict_pauseTypeDict["LoadNoDeath"] = true;
                    } 
                    else
                    {
                        // Add +1 death when loading state, unless the player is already dead. Use the currentRoomName from roomStatDisplayer so it doesn't count multi-room bino
                        EndHelperModule.externalRoomStatDict_death[roomStatDisplayer.currentRoomName] = Convert.ToInt32(EndHelperModule.externalRoomStatDict_death[roomStatDisplayer.currentRoomName]) + 1;
                    }
                }
            }

            EndHelperModule.timeSinceSessionReset = 0; // Call for reset
            lastSessionResetCause = SessionResetCause.LoadState;
        }
    }
}