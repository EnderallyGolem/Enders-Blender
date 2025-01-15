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
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.EndHelper.Entities.Misc;
using IL.Monocle;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using System.Reflection.PortableExecutable;
using System.Collections;
using System.Collections.Specialized;
using static Celeste.Mod.EndHelper.EndHelperModule;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class SpeedrunToolIntegration
    {

        private static Type StateManager;
        private static Type TeleportRoomUtils;

        private static MethodInfo StateManager_SaveState;
        private static MethodInfo StateManager_LoadState;
        private static MethodInfo TeleportRoomUtils_TeleportTo;

        private static Hook Hook_StateManager_SaveState;
        private static Hook Hook_StateManager_LoadState;
        private static Hook Hook_TeleportRoomUtils_TeleportTo;

        internal static void Load()
        {
            try
            {
                // Get type info and functions
                StateManager = Type.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager,SpeedrunTool");
                if (StateManager == null)
                {
                    return;
                }
                StateManager_SaveState = StateManager.GetMethod(
                    "SaveState", BindingFlags.NonPublic | BindingFlags.Instance,
                    Type.DefaultBinder, new Type[] { typeof(bool) }, null);
                StateManager_LoadState = StateManager.GetMethod(
                    "LoadState", BindingFlags.NonPublic | BindingFlags.Instance,
                    Type.DefaultBinder, new Type[] { typeof(bool) }, null);

                TeleportRoomUtils = Type.GetType("Celeste.Mod.SpeedrunTool.TeleportRoom.TeleportRoomUtils,SpeedrunTool");
                TeleportRoomUtils_TeleportTo = TeleportRoomUtils.GetMethod("TeleportTo", BindingFlags.NonPublic | BindingFlags.Static);


                // Set up hooks
                Hook_StateManager_SaveState = new Hook(StateManager_SaveState,
                    typeof(SpeedrunToolIntegration).GetMethod("OnSaveState", BindingFlags.NonPublic | BindingFlags.Static));
                Hook_StateManager_LoadState = new Hook(StateManager_LoadState,
                    typeof(SpeedrunToolIntegration).GetMethod("OnLoadState", BindingFlags.NonPublic | BindingFlags.Static));
                Hook_TeleportRoomUtils_TeleportTo = new Hook(TeleportRoomUtils_TeleportTo,
                    typeof(SpeedrunToolIntegration).GetMethod("OnTeleportTo", BindingFlags.NonPublic | BindingFlags.Static));

                Hook_TeleportRoomUtils_TeleportTo = new Hook(TeleportRoomUtils_TeleportTo,
                    typeof(SpeedrunToolIntegration).GetMethod("OnTeleportTo", BindingFlags.NonPublic | BindingFlags.Static));

            }
            catch (Exception) { }
        }

        internal static void Unload()
        {
            Hook_StateManager_SaveState?.Dispose();
            Hook_StateManager_SaveState = null;
            Hook_StateManager_LoadState?.Dispose();
            Hook_StateManager_LoadState = null;
            Hook_TeleportRoomUtils_TeleportTo?.Dispose();
            Hook_TeleportRoomUtils_TeleportTo = null;
        }

#pragma warning disable IDE0051  // Private method is unused

        private static bool OnSaveState(Func<object, bool, bool> orig, object stateManager, bool tas)
        {
            bool result = orig(stateManager, tas);

            return result;
        }

        private static bool OnLoadState(Func<object, bool, bool> orig, object stateManager, bool tas)
        {
            // +1 to death count =)
            Level preloadLevel = Monocle.Engine.Scene as Level;
            //String reloadRoomName = preloadLevel.Session.LevelData.Name;

            if (preloadLevel.Tracker.GetEntity<Player>() is Player player && preloadLevel.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                if (!player.Dead) {
                    // Add +1 death when loading state, unless the player is already dead. Use the currentRoomName from roomStatDisplayer so it doesn't count multi-room bino
                    EndHelperModule.externalRoomStatDict_death[roomStatDisplayer.currentRoomName] = Convert.ToInt32(EndHelperModule.externalRoomStatDict_death[roomStatDisplayer.currentRoomName]) + 1;
                }
            }

            bool result = orig(stateManager, tas);

            EndHelperModule.timeSinceSessionReset = 0; // Call for reset
            lastSessionResetCause = SessionResetCause.LoadState;

            return result;
        }

        private static void OnTeleportTo(Action<Session, bool> orig, Session session, bool fromHistory)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/SpeedrunToolIntegration", $"onteleport beofre");
            orig(session, fromHistory);
        }

#pragma warning restore IDE0051  // Private method is unused

        }
    }