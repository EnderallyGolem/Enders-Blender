using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.EndHelper.Entities.Misc;
using MonoMod.ModInterop;

namespace Celeste.Mod.EndHelper.Utils
{
    public static class Utils_Interop
    {
        internal static void InitialiseInteropExports()
        {
            typeof(DeathHandler).ModInterop();
            typeof(RoomStatistics).ModInterop();
        }

        [ModExportName("EndersBlender.RoomStatistics")]
        public static class RoomStatistics
        {
            /// <summary>
            /// Room stats have an export/import system, for storing stats externally temporarily, and importing later.
            /// (Currently it's only used for save/load states to avoid the states being reset.)
            /// This grabs the stats from the blender as a List<![CDATA[<object?>]]>, to use on ImportStatistics later on.
            /// <br/>
            /// You can modify the stats from this. See the source code for the object list - look for RoomStatisticsDisplayer.cs > SetExportData. There might be additional data added to the list in the future but the existing ones shouldn't change.
            /// </summary>
            /// <param name="level">The current level.</param>
            /// <param name="addDeath">Number of deaths to add to the stats.</param>
            /// <returns></returns>
            public static List<object>? ExportStatistics(Level level, int addDeath = 0)
            {
                if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is { } roomStatDisplayer)
                {
                    if (addDeath > 0) roomStatDisplayer.AddDeath(addDeath);
                    return roomStatDisplayer.ExportRoomStatInfo(level);
                }
                Logger.Log(LogLevel.Warn, "EndHelper/Utils_Interop", $"RoomStatistics - ExportStatistics: Cannot find a RoomStatisticsDisplayer! This shouldn't happen...");
                return null;
            }

            /// <summary>
            /// Room stats have an export/import system, for storing stats externally temporarily, and importing later.
            /// (Currently it's only used for save/load states to avoid the states being reset.)
            /// This imports the stats back into the blender.
            /// </summary>
            /// <param name="level">The current level.</param>
            /// <param name="importStats">The list of stats obtained from ExportStatistics.</param>
            public static void ImportStatistics(Level level, List<object>? importStats = null)
            {
                if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is { } roomStatDisplayer)
                {
                    roomStatDisplayer.ImportRoomStatInfo(importStats);
                }
                else
                {
                    Logger.Log(LogLevel.Warn, "EndHelper/Utils_Interop", $"RoomStatistics - ImportStatistics: Cannot find a RoomStatisticsDisplayer! This shouldn't happen...");
                }
            }


            /// <summary>
            /// ExportStatistics specifically for loading states. This should either be ran right before the state is loaded, or right after the load state but using the level before the load state. This returns the stats as a List<![CDATA[<object?>]]>, to use on ImportStatistics later on.
            /// <br/>
            /// This includes a check for Ignore Load State Death After Respawn, which avoids the
            ///
            /// </summary>
            /// <param name="level">The level BEFORE the state is loaded.</param>
            /// <param name="addDeath">How many deaths should the load state add normally (usually 1, or 0 if player.Dead is true). If unspecified, it will automatically be set to 1, or 0 if the player is dead. This death increase will be ignored if the Ignore Load State Death After Respawn is enabled.</param>
            /// <returns></returns>
            public static List<object>? ExportStatisticsLoadState(Level level, int? addDeath = null)
            {
                // +1 to death count =) unless prevented
                if (level.Tracker.GetEntity<Player>() is { } player && level.Tracker.GetEntity<RoomStatisticsDisplayer>() is { } roomStatDisplayer)
                {
                    addDeath ??= player.Dead ? 0 : 1; // For addDeath = null: 0 if player is already dead, 1 if alive.

                    {
                        if (!player.Dead && EndHelperModule.Settings.RoomStatMenu.DeathIgnoreLoadAfterDeath && EndHelperModule.Session.framesSinceRespawn <= 30)
                        {
                            // Do not increment death count. Instead make the ignore death from load state after respawn icon appear instead
                            EndHelperModule.externalDict_pauseTypeDict["LoadNoDeath"] = true;
                        }
                        else
                        {
                            // Add death when loading state.
                            if (addDeath.Value > 0) roomStatDisplayer.AddDeath(addDeath.Value);
                        }
                    }

                    return roomStatDisplayer.ExportRoomStatInfo(level); //
                }
                Logger.Log(LogLevel.Warn, "EndHelper/Utils_Interop", $"RoomStatistics - ExportStatisticsLoadState: Cannot find a RoomStatisticsDisplayer or Player! This shouldn't happen...");
                return null; // can't find anything. Something is probably wrong!
            }

            /// <summary>
            /// ImportStatistics specifically for states. This should be ran right after the states have been loaded.
            /// Rather than immediately importing the stats, this also run some functions that should run on reload (noteably trying to autosave).
            /// <br/><br/>
            /// also if you are running this can you run the NotifySessionReset interop in Ender's Extras pretty please <![CDATA[<33]]>
            ///
            /// </summary>
            /// <param name="level">The level AFTER the state is loaded.</param>
            /// <param name="importStats">The list of stats obtained from ExportStatisticsLoadState.</param>
            public static void ImportStatisticsLoadState(Level level, List<object>? importStats = null)
            {
                RoomStatisticsDisplayer.SetExportData(importStats);

                EndHelperModule.timeSinceSessionReset = 0; // Call for reset
                EndHelperModule.lastSessionResetCause = EndHelperModule.SessionResetCause.LoadState;
            }
        }

        [ModExportName("EndersBlender.DeathHandler")]
        public static class DeathHandler
        {
            /// <summary>
            /// Returns AllowDeathHandlerEntityChecks.]
            /// This is enabled if the level has any entity that uses DeathHandler's modified respawns.
            /// i.e. this is false if the level is not using DeathHandler.
            /// </summary>
            /// <returns>Bool corresponding to whether if DeathHandler is used.</returns>
            public static bool GetEnableEntityChecks()
            {
                return EndHelperModule.Session.AllowDeathHandlerEntityChecks;
            }

            /// <summary>
            /// Checks whether if a full reset should occur upon player death
            /// This is made to be read during a On.Celeste.Player.Die hook.
            /// </summary>
            /// <returns>Bool corresponding to if the next death results in a full reset</returns>
            public static bool GetNextRespawnFullReset()
            {
                return EndHelperModule.Session.nextRespawnFullReset;
            }

            /// <summary>
            /// Checks whether if a player died due to retry.
            /// This is made to be read during a On.Celeste.Player.Die hook.
            /// </summary>
            /// <returns>Bool corresponding to if the death is due to manual retry</returns>
            public static bool GetManualReset()
            {
                return Utils_DeathHandler.manualReset;
            }
        }
    }
}
