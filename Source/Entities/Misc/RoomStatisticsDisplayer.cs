using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using Celeste.Mod.Entities;
using Celeste.Mod;
using static On.Celeste.Level;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings;
using static Celeste.Mod.EndHelper.EndHelperModule;
using static Celeste.Mod.UI.CriticalErrorHandler;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.RoomStatDisplaySubMenu;
using NETCoreifier;
using System.Net.NetworkInformation;
using FMOD.Studio;
using static Celeste.Tentacles;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Celeste.Mod.EndHelper.Integration;
using MonoMod.Utils;
using System.Linq;
using Celeste.Mod.EndHelper.Utils;
using static Celeste.Mod.EndHelper.Utils.Utils_JournalStatistics;
using IL.MonoMod;
using Microsoft.Xna.Framework.Graphics;


namespace Celeste.Mod.EndHelper.Entities.Misc;

[Tracked(true)]
//[CustomEntity("EndHelper/RoomStatisticsDisplayer")]
public class RoomStatisticsDisplayer : Entity
{
    #region Initialisation

    private string clipboardText = "";
    public string currentEffectiveRoomName = "";
    public string currentRoomName = "";
    public bool statisticsGuiOpen = false;
    internal Utils_General.Countdown disableRoomChangeTimer = new Utils_General.Countdown();   // Eg: When viewing other rooms with multi-room bino

    public string mapNameSide_Internal = ""; // Internally checked to distinguish between map and sides & to determine which lang to pick for room names
    public string mapNameSide_Display = "";  // Text to display to the player

    public Color mapNameColor;    // for fun :D
    public bool dealWithFirstClear = false;
    public enum roomStatMenuFilter { None, Death0, Death10, Time60s, Renamed }
    public static roomStatMenuFilter filterSetting = roomStatMenuFilter.None;

    internal static bool hideIfGoldenStrawberryEnabled = false;
    internal static bool renameRoomsMoveRooms = false;

    private bool hasUpdatedOnce = false;

    public RoomStatisticsDisplayer(Level level)
    {
        Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
        Depth = -101;
    }
    public override void Added(Scene scene)
    {
        base.Added(scene);

        Level level = SceneAs<Level>();
        Session session = level.Session;

        mapNameSide_Display = GetMapNameSideDisplay(session.Area);
        mapNameSide_Internal = GetMapNameSideInternal(session.Area);
        mapNameColor = GetMapColour(session.Area);

        EndHelperModule.Session.roomStatDict_mapNameSide_Internal = mapNameSide_Internal; // Store mapNameSide of map into session - for error reference
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        EndHelperModule.Session.pauseTypeDict["Level_Invalid"] = !(scene as Level).Session.StartedFromBeginning;

        // Do not get affected by save states. This umm does not work. Commenting this out so it doesn't break if it works.
        //if (SpeedrunToolIntegration.SpeedrunToolInstalled)
        //{
        //    SpeedrunToolImport.IgnoreSaveState?.Invoke(this, true);
        //    Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"ignore save state !!!!!!!!!");
        //}
    }

    #endregion

    #region Update

    public void ImportRoomStatInfo()
    {
        EndHelperModule.Session.roomStatDict_customName = EndHelperModule.externalRoomStatDict_customName; // Import
        EndHelperModule.Session.roomStatDict_death = EndHelperModule.externalRoomStatDict_death;
        EndHelperModule.Session.roomStatDict_timer = EndHelperModule.externalRoomStatDict_timer;
        EndHelperModule.Session.roomStatDict_rtatimer = EndHelperModule.externalRoomStatDict_rtatimer;
        EndHelperModule.Session.roomStatDict_colorIndex = EndHelperModule.externalRoomStatDict_colorIndex;
        EndHelperModule.Session.pauseTypeDict = EndHelperModule.externalDict_pauseTypeDict;
        EndHelperModule.Session.roomStatDict_fuseRoomRedirect = EndHelperModule.externalDict_fuseRoomRedirect;

        if (dealWithFirstClear)
        {
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal] = EndHelperModule.externalRoomStatDict_firstClear_roomOrder;
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal] = EndHelperModule.externalRoomStatDict_firstClear_death;
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal] = EndHelperModule.externalRoomStatDict_firstClear_timer;
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal] = EndHelperModule.externalRoomStatDict_firstClear_rtatimer;
        }

        // Only keep for debug. Load state also un-collects the berry so let the berry count be loaded.
        if (EndHelperModule.lastSessionResetCause == SessionResetCause.Debug)
        {
            EndHelperModule.Session.roomStatDict_strawberries = EndHelperModule.externalRoomStatDict_strawberries;

            if (dealWithFirstClear)
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal] = EndHelperModule.externalRoomStatDict_firstClear_strawberries;
            }
        }
    }

    public void ExportRoomStatInfo(Level level)
    {
        EndHelperModule.externalRoomStatDict_customName = EndHelperModule.Session.roomStatDict_customName; // Export
        EndHelperModule.externalRoomStatDict_death = EndHelperModule.Session.roomStatDict_death;
        EndHelperModule.externalRoomStatDict_timer = EndHelperModule.Session.roomStatDict_timer;
        EndHelperModule.externalRoomStatDict_rtatimer = EndHelperModule.Session.roomStatDict_rtatimer;
        EndHelperModule.externalRoomStatDict_strawberries = EndHelperModule.Session.roomStatDict_strawberries;
        EndHelperModule.externalRoomStatDict_colorIndex = EndHelperModule.Session.roomStatDict_colorIndex;
        EndHelperModule.externalDict_pauseTypeDict = EndHelperModule.Session.pauseTypeDict;
        EndHelperModule.externalDict_fuseRoomRedirect = EndHelperModule.Session.roomStatDict_fuseRoomRedirect;

        if (dealWithFirstClear && EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer.ContainsKey(mapNameSide_Internal))
        {
            EndHelperModule.externalRoomStatDict_firstClear_roomOrder = EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal];
            EndHelperModule.externalRoomStatDict_firstClear_death = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal];
            EndHelperModule.externalRoomStatDict_firstClear_timer = EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal];
            EndHelperModule.externalRoomStatDict_firstClear_rtatimer = EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal];
            EndHelperModule.externalRoomStatDict_firstClear_strawberries = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal];
        }

        // Export to SaveData
        if (EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != 0)
        {
            // All SaveData Data
            UpdateSaveDataCustomName();
            UpdateSaveDataColorIndex();

            // First Clear Data
            // If level not completed, any existing enabled pauseType gets added to the firstclear pauseType dict
            if (dealWithFirstClear)
            {
                foreach (KeyValuePair<string, bool> entry in EndHelperModule.Session.pauseTypeDict)
                {
                    String key = entry.Key;
                    bool value = entry.Value;

                    if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal].ContainsKey(key)) {
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal][key] = false; 
                    }

                    if (value)
                    {
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal][key] = true;
                    }
                }
            }

            // Latest Session Data. These only gets exported at the end of the map, if you started from the beginning (valid clear).
            if (level.Completed && CheckSaveSessionData(level))
            {
                SaveSessionData();
            }
        }
    }

    private void SaveSessionData()
    {
        savedSessionData = true;
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"Saving session's room stat data!");

        // Room Order: Add every room in order.
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal] = [];
        foreach (DictionaryEntry sessionDeathDict in EndHelperModule.Session.roomStatDict_death)
        {
            string sessionDeathDictRoomName = (String)sessionDeathDict.Key;
            EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal].Add(sessionDeathDictRoomName);
        }

        // Copy paste all the session data into the latestSession dicts
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_death[mapNameSide_Internal] = Utils_General.ConvertFromOrderedDictionary<string, int>(EndHelperModule.Session.roomStatDict_death);
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_timer[mapNameSide_Internal] = Utils_General.ConvertFromOrderedDictionary<string, long>(EndHelperModule.Session.roomStatDict_timer);
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_rtatimer[mapNameSide_Internal] = Utils_General.ConvertFromOrderedDictionary<string, long>(EndHelperModule.Session.roomStatDict_rtatimer);
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_strawberries[mapNameSide_Internal] = Utils_General.ConvertFromOrderedDictionary<string, int>(EndHelperModule.Session.roomStatDict_strawberries);
        EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal] = EndHelperModule.Session.pauseTypeDict;
    }

    private bool haveCheckedSaveSessionData = false;
    private bool savedSessionData = false;
    private bool CheckSaveSessionData(Level level)
    {
        if (haveCheckedSaveSessionData) return false;
        haveCheckedSaveSessionData = true;

        if (mapNameSide_Internal == "Celeste/7-Summit" && level.Session.LevelData.Name == "credits-summit")
        {
            return false; // Hardcode it not to store for vanilla credits, because the menu can crash the game :(
        }

        if (!EndHelperModule.SaveData.mapDict_roomStat_latestSession_death.ContainsKey(mapNameSide_Internal))
        {
            return true; // If no stored map, return true (save it!)
        }
        else
        {
            // Otherwise, check settings to determine if it should be saved
            bool validClear = level.Session.StartedFromBeginning;

            int current_totalDeaths = 0; long current_totalTimer = 0; long current_totalRtaTimer = 0; int current_totalBerries = 0;
            int saved_totalDeaths = 0; long saved_totalTimer = 0; long saved_totalRtaTimer = 0; int saved_totalBerries = 0;
            bool saved_invalidClear = EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal].ContainsKey("Level_Invalid") 
                && EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["Level_Invalid"];
            
            foreach (string roomName in EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal])
            {
                try
                {
                    int saved_roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_latestSession_death[mapNameSide_Internal][roomName];
                    long saved_roomTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_latestSession_timer[mapNameSide_Internal][roomName];
                    long saved_roomRtaTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_latestSession_rtatimer[mapNameSide_Internal][roomName];
                    int saved_roomBerries = EndHelperModule.SaveData.mapDict_roomStat_latestSession_strawberries[mapNameSide_Internal][roomName];
                    saved_totalDeaths += saved_roomDeaths;
                    saved_totalTimer += saved_roomTimeTicks;
                    saved_totalRtaTimer += saved_roomRtaTimeTicks;
                    saved_totalBerries += saved_roomBerries;
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "EndHelper/RoomStatisticsDisplayer", $"Error when checking last saved room stats data for room ${roomName}: ${e}");
                }
            }
            foreach (string roomName in EndHelperModule.Session.roomStatDict_death.Keys)
            {
                try
                {
                    int current_roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                    long current_roomTimeTicks = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]);
                    long current_roomRtaTimeTicks = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[roomName]);
                    int current_roomBerries = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);
                    current_totalDeaths += current_roomDeaths;
                    current_totalTimer += current_roomTimeTicks;
                    current_totalRtaTimer += current_roomRtaTimeTicks;
                    current_totalBerries += current_roomBerries;
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "EndHelper/RoomStatisticsDisplayer", $"Error when checking current run's room stats data for room ${roomName}: ${e}");
                }
            }

            if (current_totalTimer <= 0)
            {
                // If total timer is 0, it's probably not something that should be saved.
                return false;
            }

            switch (EndHelperModule.Settings.RoomStatMenu.StoredClears)
            {
                case RoomStatMenuSubMenu.StoredClearsEnum.Always:
                    return true;
                case RoomStatMenuSubMenu.StoredClearsEnum.Ask:
                    ShowSaveSessionMenu(current_totalDeaths, current_totalTimer, current_totalRtaTimer, current_totalBerries, !validClear, saved_totalDeaths, saved_totalTimer, saved_totalRtaTimer, saved_totalBerries, saved_invalidClear);
                    return false;
                case RoomStatMenuSubMenu.StoredClearsEnum.AskIfValidClear:
                    if (validClear) ShowSaveSessionMenu(current_totalDeaths, current_totalTimer, current_totalRtaTimer, current_totalBerries, !validClear, saved_totalDeaths, saved_totalTimer, saved_totalRtaTimer, saved_totalBerries, saved_invalidClear);
                    return false;
                case RoomStatMenuSubMenu.StoredClearsEnum.ValidClear:
                    if (validClear) return true;
                    else return false;
                case RoomStatMenuSubMenu.StoredClearsEnum.ValidClearFaster:
                    if (validClear && current_totalTimer <= saved_totalTimer) return true;
                    else return false;
                case RoomStatMenuSubMenu.StoredClearsEnum.ValidClearLessDeaths:
                    if (validClear && current_totalDeaths <= saved_totalDeaths) return true;
                    else return false;
                case RoomStatMenuSubMenu.StoredClearsEnum.Never: return false;
                default: return false;
            }
        }
    }

    private void ShowSaveSessionMenu(int current_deaths, long current_timer, long current_rtatimer, int current_berries, bool current_invalidClear,
                                     int saved_deaths, long saved_timer, long saved_rtatimer, int saved_berries, bool saved_invalidClear)
    {
        TextMenu menu = new TextMenu();
        Level level = SceneAs<Level>();
        level.Paused = true;

        menu.OnCancel = (() =>
        {
            Audio.Play("event:/ui/main/button_back");
            menu.Close();
            level.Unpause();
        });

        menu.Add(new TextMenu.Header(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_StoredClears_Header")));
        menu.Add(new TextMenu.SubHeader(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_StoredClears_SubHeader")));

        // Create old and new stat comparison menu
        bool showRtaTimer = EndHelperModule.Settings.RoomStatMenu.MenuShowTime == EndHelperModuleSettings.RoomStatMenuSubMenu.MenuShowTimeEnum.RTA;
        String current_timer_string; String currentStatString; String saved_timer_string; String savedStatString;

        if (showRtaTimer)
        {
            current_timer_string = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(current_rtatimer));
            currentStatString = $":EndHelper/uioutline_skull: {current_deaths}   :EndHelper/uioutline_rtaclock: {current_timer_string}   :EndHelper/uioutline_strawberry: {current_berries}";

            saved_timer_string = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(saved_rtatimer));
            savedStatString = $":EndHelper/uioutline_skull: {saved_deaths}   :EndHelper/uioutline_rtaclock: {saved_timer_string}   :EndHelper/uioutline_strawberry: {saved_berries}";
        }
        else
        {
            current_timer_string = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(current_timer));
            currentStatString = $":EndHelper/uioutline_skull: {current_deaths}   :EndHelper/uioutline_clock: {current_timer_string}   :EndHelper/uioutline_strawberry: {current_berries}";

            saved_timer_string = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(saved_timer));
            savedStatString = $":EndHelper/uioutline_skull: {saved_deaths}   :EndHelper/uioutline_clock: {saved_timer_string}   :EndHelper/uioutline_strawberry: {saved_berries}";
        }
        if (current_invalidClear) currentStatString += $" [Not Valid]";
        if (saved_invalidClear) savedStatString += $" [Not Valid]";

        menu.Add(new TextMenu.SubHeader($"{currentStatString}     >>>     {savedStatString}"));
        menu.Add(new TextMenu.Button(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_StoredClears_Override")).Pressed(() =>
        {
            SaveSessionData();
            menu.OnCancel();
        }
        ));
        menu.Add(new TextMenu.Button(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_StoredClears_Cancel")).Pressed(() =>
        {
            menu.OnCancel();
        }
        ));

        Scene.Add(menu);
    }

    private void UpdateSaveDataCustomName()
    {
        if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Contains(mapNameSide_Internal))
        {
            foreach (KeyValuePair<string, string> entry in EndHelperModule.Session.roomStatDict_customName)
            {
                String key = entry.Key;
                string value = entry.Value;

                // I am never using an ordereddict again.
                if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<object, object>)
                {
                    (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<object, object>)[key] = value;
                }
                if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
                {
                    (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<string, string>)[key] = value;
                }
            }
        }
    }

    private void UpdateSaveDataColorIndex()
    {
        if (EndHelperModule.SaveData.mapDict_roomStat_colorIndex.ContainsKey(mapNameSide_Internal))
        {
            foreach (DictionaryEntry entry in EndHelperModule.Session.roomStatDict_colorIndex)
            {
                String key = (String)entry.Key;
                int value = Convert.ToInt32(entry.Value);

                EndHelperModule.SaveData.mapDict_roomStat_colorIndex[mapNameSide_Internal][key] = value;
            }
        }
    }

    public override void Update()
    {

        // Keep these updated!
        Level level = SceneAs<Level>();
        EnsureDictsHaveKey(level);

        // Tick down disable change room timer
        if (!level.FrozenOrPaused && !level.Transitioning) disableRoomChangeTimer.Update();

        // Don't update this when map is completed, otherwise the stats may change upon collecting heart
        if (!level.Completed)
        {
            dealWithFirstClear = EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != 0 && !SaveData.Instance.Areas_Safe[level.Session.Area.ID].Modes[(int)level.Session.Area.Mode].Completed && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal);
        }

        String roomDialogName = $"{mapNameSide_Internal}_{level.Session.LevelData.Name}".Replace(".", "__point__").DialogCleanOrNull(Dialog.Languages["english"]) ?? "";
        if (!disableRoomChangeTimer.IsTicking && roomDialogName != "%skip")
        {
            currentRoomName = level.Session.LevelData.Name;
            currentEffectiveRoomName = GetEffectiveRoomName(currentRoomName);
        }

        // If something wacky happens (aka if debug mode is used), grab data from there. Otherwise export data there.
        if (EndHelperModule.timeSinceSessionReset == 1)
        {
            ImportRoomStatInfo();
        }
        else if (EndHelperModule.timeSinceSessionReset > 1)
        {
            ExportRoomStatInfo(level);
        }

        // removed feature because it is stupid

        // Counters for people to use I guess
        // OrderedDict do not handle types well, save & quit converts them into strings for some reason, hence the really dumb Convert.ToInts
        //int timeSpentInSeconds = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentEffectiveRoomName])).Seconds;

        //String counterFriendlyRoomName = currentEffectiveRoomName;
        //counterFriendlyRoomName = counterFriendlyRoomName.Replace("%", "_"); // %s in %segment causes issues lol

        //level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_death", Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentEffectiveRoomName]));
        //level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_timer", timeSpentInSeconds);
        //level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_strawberries", Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentEffectiveRoomName]));

        // Show/Hide GUI
        if (EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed && !statisticsGuiOpen && !Utils_JournalStatistics.journalOpen && !level.Paused && !level.Transitioning)
        {
            statisticsGuiOpen = true;
            Depth = -9000;
            level.Paused = true;
            Audio.Play("event:/ui/game/pause");
        }
        else if (statisticsGuiOpen && !roomNameEditMenuOpen && (!level.Paused || Utils_JournalStatistics.journalOpen || Input.ESC.Pressed || Input.MenuCancel.Pressed || Input.Pause
            || level.Transitioning || EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed))
        {
            statisticsGuiOpen = false;
            level.Paused = false;
            EndHelperModule.afkDurationFrames = 0;

            // Directly consuming doesn't do it for long enough
            Utils_General.ConsumeInput(Input.Jump, 2);
            Utils_General.ConsumeInput(Input.Dash, 2);
            Utils_General.ConsumeInput(Input.CrouchDash, 2);
            Utils_General.ConsumeInput(Input.Grab, 2);
            Utils_General.ConsumeInput(Input.ESC, 3);
            Utils_General.ConsumeInput(Input.Pause, 3);
            Audio.Play("event:/ui/game/unpause");

            if (Input.MenuCancel.Pressed)
            {
                string clipboardToolTipMsg = Dialog.Get("EndHelper_Dialog_RoomStatisticsDisplayer_CopiedToClipboard");
                RoomStatisticsDisplayer.ShowTooltip(clipboardToolTipMsg, 2f);
                TextInput.SetClipboardText(clipboardText);
            }
        }
        // MInput.Disabled = false;
        // Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"schedule {scheduleMInputDisable} >> {MInput.Disabled}");

        if ((Input.ESC.Pressed || Input.Pause.Pressed || !statisticsGuiOpen || !level.Paused) && roomNameEditMenuOpen)
        {
            MenuCloseNameEditor();
        }

        if (statisticsGuiOpen)
        {
            EndHelperModule.mInputDisableTimer.Set(5);
        }

        // Hide if golden strawberry
        if (level.Session.GrabbedGolden)
        {
            hideIfGoldenStrawberryEnabled = true;
        }

        base.Update();
        // MInput.Disabled = false;

        hasUpdatedOnce = true;
    }

    void MenuCloseNameEditor()
    {
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"MenuCloseNameEditor: close text menu");
        TextInput.OnInput -= OnTextInput;

        Utils_General.ConsumeInput(Input.ESC, 3);
        Utils_General.ConsumeInput(Input.Pause, 3);
        Utils_General.ConsumeInput(Input.MenuCancel, 3);
        Utils_General.ConsumeInput(Input.MenuConfirm, 3);
        roomNameEditMenuOpen = false;

        EndHelperModule.afkDurationFrames = 0;
        Audio.Play("event:/ui/main/rename_entry_accept");
    }

    public override void Render()
    {
        if (!hasUpdatedOnce) return;

        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"Render - currentRoomName {currentRoomName} effroomname {currentEffectiveRoomName}");
        Level level = SceneAs<Level>();
        EnsureDictsHaveKey(level);

        if (statisticsGuiOpen) StatisticGUI();

        // Text Display
        int displayXPos = 15 + EndHelperModule.Settings.RoomStatDisplayMenu.OffsetX * 8;
        int displayYPos = 15 + EndHelperModule.Settings.RoomStatDisplayMenu.OffsetY * 8;
        float displayScale = (float)EndHelperModule.Settings.RoomStatDisplayMenu.Size / 20;

        int deathNum;
        long timerNum;
        long rtatimerNum;
        int strawberriesNum;

        if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
        {
            // Show first cycle
            deathNum = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][currentEffectiveRoomName];
            timerNum = EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][currentEffectiveRoomName];
            rtatimerNum = EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][currentEffectiveRoomName];
            strawberriesNum = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][currentEffectiveRoomName];
        } 
        else
        {
            // Show current stats
            deathNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentEffectiveRoomName]);
            timerNum = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentEffectiveRoomName]);
            rtatimerNum = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[currentEffectiveRoomName]);
            strawberriesNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentEffectiveRoomName]);
        }

        Color timerColor = allowIncrementRoomTimer ? Color.White : Color.Gray;

        float xJustification = 0;
        if (EndHelperModule.Settings.RoomStatDisplayMenu.xJustification == RoomStatDisplaySubMenu.Justification.Center)
        {
            xJustification = 0.5f;
        } 
        else if (EndHelperModule.Settings.RoomStatDisplayMenu.xJustification == RoomStatDisplaySubMenu.Justification.Right)
        {
            xJustification = 1f;
        }

        if (!statisticsGuiOpen && (!EndHelperModule.Settings.RoomStatDisplayMenu.HideIfGolden || !hideIfGoldenStrawberryEnabled))
        {
            IconType iconType = IconType.White;
            if (level.Completed) iconType = savedSessionData ? IconType.Green : IconType.Gray;
            bool showAliveTimer = EndHelperModule.Settings.RoomStatDisplayMenu.ShowAliveTime;

            ShowGUIStats(currentEffectiveRoomName, displayXPos, displayYPos, displayScale, timerColor, false, xJustification, false, false, showAliveTimer, false, level.Session.MapData.DetectedStrawberries, $"", $"", deathNum, timerNum, rtatimerNum, strawberriesNum, iconType);
        }

        RenderOtherStuffCompletelyUnrelatedToRoomStatsButAddedHereDueToConvenience(level);

        base.Render();
    }

    #endregion

    #region Display Stats

    public class DisplayInfo
    {
        public string displayMsg;
        public int textWidth;
        public string id;
        public DisplayInfo(string id, string displayMsg, int textWidth)
        {
            this.displayMsg = displayMsg;
            this.textWidth = textWidth;
            this.id = id;
        }
    }

    // currentEffectiveRoomName only necessary if showRoomName or showAll enabled. Otherwise just set to empty string
    internal enum IconType { White, Gray, Green, Yellow }
    internal static void ShowGUIStats(string currentEffectiveRoomName, int displayXPos, int displayYPos, float displayScale, Color timerColor, bool yCentered, float xJustification, bool showMenuStats, bool hideRoomName, bool showAliveTimer, bool showTotalMapBerryCount, int totalMapBerryCount, string prefix, string suffix, int deathNum, long timerNum, long rtatimerNum, int strawberriesNum, IconType iconType)
    {
        var roomDisplaySettings = EndHelperModule.Settings.RoomStatDisplayMenu;
        Vector2 justification = new Vector2(0, yCentered ? 0.5f : 0f);
        List<DisplayInfo> displayInfoList = [];

        if (prefix != "")
        {
            displayInfoList.Add(new DisplayInfo("prefix", prefix, (int)(ActiveFont.WidthToNextLine($"{prefix}", 0) * displayScale)));
            //ActiveFont.DrawOutline(prefix, new Vector2(sectionXPos + xOffset, displayYPos), justification, Vector2.One * displayScale, timerColor, 2f, Color.Black);
        }

        if (!hideRoomName && (roomDisplaySettings.ShowRoomName || showMenuStats) && currentEffectiveRoomName != "")
        {
            string displayMsg = "";

            // Crash prevention. Just in case. The currentEffectiveRoomName != "" check should already handle the main source of crash though.
            if (!EndHelperModule.Session.roomStatDict_customName.ContainsKey(currentEffectiveRoomName))
            {
                Logger.Log(LogLevel.Warn, "EndHelper/RoomStatisticsDisplayer", $"Failed to get custom name for room ${currentEffectiveRoomName}. Manually setting custom name as this.");
                EndHelperModule.Session.roomStatDict_customName[currentEffectiveRoomName] = currentEffectiveRoomName;
            }

            string customRoomName = Convert.ToString(EndHelperModule.Session.roomStatDict_customName[currentEffectiveRoomName]);

            if (customRoomName.Length > 35)
            {
                displayMsg += $"{customRoomName.Substring(0, 33)}...";
            }
            else
            {
                displayMsg += customRoomName;
            }

            if (roomDisplaySettings.ShowDeaths || roomDisplaySettings.ShowTimeSpent 
                || (roomDisplaySettings.ShowStrawberries && strawberriesNum > 0) || showAliveTimer)
            {
                displayMsg += ":";
            }

            displayInfoList.Add(new DisplayInfo("roomname", displayMsg, (int)(ActiveFont.WidthToNextLine($"{displayMsg} ", 0) * displayScale)));
        }

        if (showMenuStats || roomDisplaySettings.ShowDeaths)
        {
            string displayMsg = $":EndHelper/uioutline_skull: {deathNum}";
            if (iconType == IconType.Gray) displayMsg = $":EndHelper/uioutline_skull_gray: {deathNum}";
            if (iconType == IconType.Green) displayMsg = $":EndHelper/uioutline_skull_green: {deathNum}";
            else if (iconType == IconType.Yellow) displayMsg = $":EndHelper/uioutline_skull_yellow: {deathNum}";

            displayInfoList.Add(new DisplayInfo("deaths", displayMsg, (int)(ActiveFont.WidthToNextLine($"{deathNum}XXX|", 0) * displayScale)));
        }
        if ( (showMenuStats && EndHelperModule.Settings.RoomStatMenu.MenuShowTime != RoomStatMenuSubMenu.MenuShowTimeEnum.RTA) || (!showMenuStats && roomDisplaySettings.ShowTimeSpent))
        {
            TimeSpan timeSpent = TimeSpan.FromTicks(timerNum);
            string timeString = Utils_General.MinimalGameplayFormat(timeSpent);
            string displayMsg = $":EndHelper/uioutline_clock: {timeString}";

            if (timerColor == Color.Gray || iconType == IconType.Gray) displayMsg = $":EndHelper/uioutline_clock_gray: {timeString}";
            if (iconType == IconType.Green) displayMsg = $":EndHelper/uioutline_clock_green: {timeString}";
            if (iconType == IconType.Yellow) displayMsg = $":EndHelper/uioutline_clock_yellow: {timeString}";

            int textWidth = 0;
            if (timeSpent.TotalHours < 1)
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * timeString.Length + ActiveFont.WidthToNextLine($"XX|", 0)) * displayScale);
            }
            else
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * timeString.Length + ActiveFont.WidthToNextLine($"X:|", 0)) * displayScale);
            }
            displayInfoList.Add(new DisplayInfo("timer", displayMsg, textWidth));
        }
        if ( (showMenuStats && EndHelperModule.Settings.RoomStatMenu.MenuShowTime != RoomStatMenuSubMenu.MenuShowTimeEnum.Normal) || (!showMenuStats && roomDisplaySettings.ShowRTATimeSpent))
        {
            TimeSpan rtatimeSpent = TimeSpan.FromTicks(rtatimerNum);
            string rtatimeString = Utils_General.MinimalGameplayFormat(rtatimeSpent);
            string displayMsg = $":EndHelper/uioutline_rtaclock: {rtatimeString}";

            if (timerColor == Color.Gray || iconType == IconType.Gray) displayMsg = $":EndHelper/uioutline_rtaclock_gray: {rtatimeString}";
            if (iconType == IconType.Green) displayMsg = $":EndHelper/uioutline_rtaclock_green: {rtatimeString}";
            if (iconType == IconType.Yellow) displayMsg = $":EndHelper/uioutline_rtaclock_yellow: {rtatimeString}";

            int textWidth = 0;
            if (rtatimeSpent.TotalHours < 1)
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * rtatimeString.Length + ActiveFont.WidthToNextLine($"XX|", 0)) * displayScale);
            }
            else
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * rtatimeString.Length + ActiveFont.WidthToNextLine($"X:|", 0)) * displayScale);
            }
            displayInfoList.Add(new DisplayInfo("timer", displayMsg, textWidth));
        }
        if (showAliveTimer)
        {
            TimeSpan timespanSinceRespawn = TimeSpan.FromTicks(EndHelperModule.Session.framesSinceRespawn * TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime).Ticks);
            string timeString = Utils_General.MinimalGameplayFormat(timespanSinceRespawn);
            string displayMsg = $"({timeString})";

            int textWidth = 0;
            if (timespanSinceRespawn.TotalHours < 1)
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * timeString.Length + ActiveFont.WidthToNextLine($"X|", 0)) * displayScale);
            }
            else
            {
                textWidth = (int)((ActiveFont.WidthToNextLine($"X", 0) * timeString.Length + ActiveFont.WidthToNextLine($":|", 0)) * displayScale);
            }
            displayInfoList.Add(new DisplayInfo("timeralive", displayMsg, textWidth));
        }
        if (showMenuStats || roomDisplaySettings.ShowStrawberries)
        {
            int mapBerryCount = totalMapBerryCount;
            if (strawberriesNum > 0 || (showTotalMapBerryCount && mapBerryCount > 0)) // If player (incl gold/moon) or map (excl gold/moon) has strawberries
            {
                String displayMsgNoEmote = " ";
                if (strawberriesNum >= 2 || showTotalMapBerryCount) // Show num if >=2 or menu
                {
                    displayMsgNoEmote += $"{strawberriesNum}";
                }
                if (showTotalMapBerryCount && (mapBerryCount > 0 || strawberriesNum > 0)) // Show total if menu && map/player has strawberries
                {
                    displayMsgNoEmote += $"/{mapBerryCount}";
                }
                String displayMsg = $":EndHelper/uioutline_strawberry:{displayMsgNoEmote}";
                if (iconType == IconType.Gray) displayMsg = $":EndHelper/uioutline_strawberry_gray:{displayMsgNoEmote}";
                if (iconType == IconType.Green) displayMsg = $":EndHelper/uioutline_strawberry_green:{displayMsgNoEmote}";
                if (iconType == IconType.Yellow) displayMsg = $":EndHelper/uioutline_strawberry_yellow:{displayMsgNoEmote}";

                displayInfoList.Add(new DisplayInfo("strawberries", displayMsg, (int)(ActiveFont.WidthToNextLine($"{displayMsgNoEmote}XXX", 0) * displayScale)));
            }
        }
        if (suffix != "")
        {
            displayInfoList.Add(new DisplayInfo("suffix", suffix, (int)(ActiveFont.WidthToNextLine($"{prefix}", 0) * displayScale)));
        }

        // First get the totalTextWidth to find how much to offset for the justification
        int xOffset = 0;
        if (xJustification > 0)
        {
            int totalTextWidth = 0;
            foreach (DisplayInfo displayInfo in displayInfoList)
            {
                totalTextWidth += displayInfo.textWidth;
            }
            xOffset = -(int)(totalTextWidth * xJustification);
        }

        // Secondly display all that text
        int sectionXPos = displayXPos;
        foreach (DisplayInfo displayInfo in displayInfoList)
        {
            Color color = displayInfo.id == "timer" ? timerColor : Color.White;

            if (iconType == IconType.Gray) color = Color.Gray;
            if (iconType == IconType.Green) color = Calc.HexToColor("6ded87");
            if (iconType == IconType.Yellow) color = Calc.HexToColor("fad768");

            ActiveFont.DrawOutline(displayInfo.displayMsg, new Vector2(sectionXPos + xOffset, displayYPos), justification, Vector2.One * displayScale, color, 2f, Color.Black);
            sectionXPos += displayInfo.textWidth;
        }
    }

    #endregion

    #region Display Stats GUI

    private int firstRowShown = 0;
    int editingRoomIndex = -1;
    string editingRoomName = null;
    void StatisticGUI()
    {
        Level level = SceneAs<Level>();
        Session session = level.Session;

        MTexture backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background"];
        MTexture backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short"];
        MTexture backgroundTextureMed = GFX.Gui["misc/EndHelper/statGUI_background_med"];
        MTexture pageArrow = GFX.Gui["dotarrow_outline"];
        if (!EndHelperModule.Settings.RoomStatMenu.MenuMulticolor)
        {
            backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background_purple"];
            backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short_purple"];
            backgroundTextureMed = GFX.Gui["misc/EndHelper/statGUI_background_med_purple"];
        }
        MTexture backgroundTextureEdit = GFX.Gui["misc/EndHelper/statGUI_background_edit"];

        MTexture upKey = GFX.Gui["controls/keyboard/up"];
        MTexture downKey = GFX.Gui["controls/keyboard/down"];
        MTexture leftKey = GFX.Gui["controls/keyboard/left"];
        MTexture rightKey = GFX.Gui["controls/keyboard/right"];
        //MTexture deleteKey = GFX.Gui["controls/keyboard/delete"];
        MTexture moveRoomArrows = GFX.Gui["misc/EndHelper/statGUI_moveroomarrows"];

        // Create a list of all room names to show
        List<string> roomNamesToShowList = new List<string>();

        ICollection allRoomsList;
        if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
        {
            allRoomsList = EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal];
        } 
        else
        {
            allRoomsList = EndHelperModule.Session.roomStatDict_death.Keys;
        }

        foreach (string roomName in new ArrayList(allRoomsList))
        {
            if (roomName == "")
            {
                RemoveRoomData("", true, true);
                continue;
            }
            int roomDeaths;
            TimeSpan roomTimeSpan;
            TimeSpan roomRtaTimeSpan;
            int roomStrawberriesCollected;

            if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
            {
                // Show First Cycle
                EnsureDictsHaveKey(level, roomName, DictsHaveKeyType.FirstClear); // Strawberry might not have key when load state
                roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName];
                roomTimeSpan = TimeSpan.FromTicks(EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName]);
                roomRtaTimeSpan = TimeSpan.FromTicks(EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][roomName]);
                roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName];
            }
            else
            {
                // Show current stats
                roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                roomTimeSpan = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]));
                roomRtaTimeSpan = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[roomName]));
                roomStrawberriesCollected = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);
            }
            string roomTimeString = Utils_General.MinimalGameplayFormat(roomTimeSpan);

            // Filtering
            bool checkRTAFilter = EndHelperModule.Settings.RoomStatMenu.MenuShowTime == EndHelperModuleSettings.RoomStatMenuSubMenu.MenuShowTimeEnum.RTA;
            switch (filterSetting)
            {
                case roomStatMenuFilter.Death0:
                    if (roomDeaths <= 0) { continue; }
                    break;

                case roomStatMenuFilter.Death10:
                    if (roomDeaths <= 10) { continue; }
                    break;

                case roomStatMenuFilter.Time60s:
                    if ((checkRTAFilter && roomRtaTimeSpan.TotalSeconds <= 60) || (!checkRTAFilter && roomTimeSpan.TotalSeconds <= 60)) { continue; }
                    break;

                case roomStatMenuFilter.Renamed:
                    String defaultName = roomName;
                    defaultName = $"{mapNameSide_Internal}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;

                    if (defaultName == Convert.ToString(EndHelperModule.Session.roomStatDict_customName[roomName])) { continue; }
                    break;

                default:
                    // Nothing!!
                    break;
            }
            roomNamesToShowList.Add(roomName);
        }
        String filterString;
        switch (filterSetting)
        {
            case roomStatMenuFilter.Death0:
                filterString = "≥1 Death";
                break;

            case roomStatMenuFilter.Death10:
                filterString = "≥10 Deaths";
                break;

            case roomStatMenuFilter.Time60s:
                filterString = "≥60s";
                break;

            case roomStatMenuFilter.Renamed:
                filterString = "Renamed";
                break;

            default:
                filterString = "All";
                break;
        }


        // Menu spacings
        const int roomsPerColumn = 16;
        int lastRowShown = firstRowShown + 2 * roomsPerColumn;
        int currentItemIndex = 0;


        int startX = 550;
        int startX_first = 100;

        const int width_twoTimerExt = 90;
        int twoTimerOffset = EndHelperModule.Settings.RoomStatMenu.MenuShowTime == RoomStatMenuSubMenu.MenuShowTimeEnum.Both ? width_twoTimerExt : 0;

        int col2Buffer = 900;
        if (EndHelperModule.Settings.RoomStatMenu.MenuShowTime == RoomStatMenuSubMenu.MenuShowTimeEnum.Both) { col2Buffer = col2Buffer + twoTimerOffset - 35; }

        int dictSize = roomNamesToShowList.Count;

        if (dictSize - firstRowShown > roomsPerColumn)
        {
            startX = startX_first;
        }
        startX -= twoTimerOffset;

        const int startY = 100;
        const int heightBetweenRows = 55;

        const int width_death = 140;
        int startX_death = startX + 532;

        int width_timer = 140 + twoTimerOffset;
        int startX_timer = startX_death + width_death + 10;
        int startX_strawberry = startX_timer + width_timer + 20;

        const int bufferX = 10;


        // Map Name Header
        ActiveFont.DrawOutline($"{mapNameSide_Display}", new Vector2(960, 5), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), mapNameColor, 2f, Color.Black);
        if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
        {
            String firstClearMsg = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_FirstRun");
            ActiveFont.DrawOutline($"({firstClearMsg})", new Vector2(960, 40), new Vector2(0.5f, 0f), new Vector2(0.45f, 0.45f), Color.DarkGray, 2f, Color.Black);
        }
        else
        {
            String currentSessionMsg = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_Current");
            ActiveFont.DrawOutline($"({currentSessionMsg})", new Vector2(960, 40), new Vector2(0.5f, 0f), new Vector2(0.45f, 0.45f), Color.DarkGray, 2f, Color.Black);
        }

        // The table headers (aka just death and timer icons)
        const int iconHeightOffset = -50;
        if (!inCreateSegmentRoomMenu)
        {
            bool useCol2BufferOffset = dictSize - firstRowShown > roomsPerColumn;
            String clockIcon = ":EndHelper/uioutline_clock:";
            if (EndHelperModule.Settings.RoomStatMenu.MenuShowTime == RoomStatMenuSubMenu.MenuShowTimeEnum.RTA)
            { clockIcon = ":EndHelper/uioutline_rtaclock:"; }
            else if (EndHelperModule.Settings.RoomStatMenu.MenuShowTime == RoomStatMenuSubMenu.MenuShowTimeEnum.Both)
            { clockIcon = ":EndHelper/uioutline_clock: / :EndHelper/uioutline_rtaclock:"; }

            // First Column
            ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            ActiveFont.DrawOutline(clockIcon, new Vector2(startX_timer + width_timer / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

            // Second Column. If needed.
            if (useCol2BufferOffset)
            {
                ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + col2Buffer + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                ActiveFont.DrawOutline(clockIcon, new Vector2(startX_timer + col2Buffer + width_timer / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            }
        }

        // The table
        int totalDeaths = 0;
        long totalTimer = 0;
        long totalRtaTimer = 0;
        int totalStrawberries = 0;

        bool checkRTA = EndHelperModule.Settings.RoomStatMenu.MenuShowTime == EndHelperModuleSettings.RoomStatMenuSubMenu.MenuShowTimeEnum.RTA;
        
        switch (EndHelperModule.Settings.RoomStatMenu.MenuShowTime)
        {
            case RoomStatMenuSubMenu.MenuShowTimeEnum.Normal:
                clipboardText = $"Room\tDeaths\tTime\tBerries";
                break;
            case RoomStatMenuSubMenu.MenuShowTimeEnum.RTA:
                clipboardText = $"Room\tDeaths\tRTA Time\tBerries";
                break;
            case RoomStatMenuSubMenu.MenuShowTimeEnum.Both:
                clipboardText = $"Room\tDeaths\tTime\tRTA Time\tBerries";
                break;

        }

        if (!inCreateSegmentRoomMenu)
        {
            foreach (string roomName in roomNamesToShowList)
            {
                // Just in case. This sometimes errors out when rebuilding, but as a safeguard in case it occurs elsewhere im gonna ensure the key exist.
                EnsureDictsHaveKey(level, roomName, (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear) ? DictsHaveKeyType.FirstClear : DictsHaveKeyType.Current);

                int roomDeaths;
                long roomTimeTicks;
                TimeSpan roomTimeSpan;
                long roomRtaTimeTicks;
                TimeSpan roomRtaTimeSpan;
                int roomStrawberriesCollected;

                if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
                {
                    // Show First Cycle
                    roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName];
                    roomTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName];
                    roomRtaTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][roomName];
                    roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName];
                }
                else
                {
                    // Show current stats
                    roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                    roomTimeTicks = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]);
                    roomRtaTimeTicks = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[roomName]);
                    roomStrawberriesCollected = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);
                }
                roomTimeSpan = TimeSpan.FromTicks(roomTimeTicks);
                string roomTimeString = Utils_General.MinimalGameplayFormat(roomTimeSpan);
                roomRtaTimeSpan = TimeSpan.FromTicks(roomRtaTimeTicks);
                string roomRtaTimeString = Utils_General.MinimalGameplayFormat(roomRtaTimeSpan);


                if (!roomNameEditMenuOpen && currentEffectiveRoomName == roomName) // if closed, set editingRoomIndex to current room
                {
                    editingRoomIndex = currentItemIndex;
                    editingRoomName = roomName;
                }

                totalDeaths += roomDeaths;
                totalTimer += roomTimeTicks;
                totalRtaTimer += roomRtaTimeTicks;
                totalStrawberries += roomStrawberriesCollected;

                string customRoomName = Convert.ToString(EndHelperModule.Session.roomStatDict_customName[roomName]);

                if ((EndHelperModule.Session.roomStatDict_customName[roomName] is null || (customRoomName.Trim().Length == 0)) && !roomNameEditMenuOpen)
                {
                    EndHelperModule.Session.roomStatDict_customName[roomName] = $"{mapNameSide_Internal}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;
                } // No empty names!

                string shortenedRoomName = customRoomName;

                Vector2 textScale = new Vector2(0.7f, 0.7f);
                if (ActiveFont.WidthToNextLine(customRoomName, 0) * 0.4 > 510)
                {
                    int cutOffIndex = customRoomName.Length > 38 ? 38 : customRoomName.Length - 5;
                    shortenedRoomName = $"{customRoomName.Substring(0, cutOffIndex)}...";
                }
                if (ActiveFont.WidthToNextLine(customRoomName, 0) * 0.5 > 510)
                {
                    textScale = new Vector2(0.4f, 0.4f);
                }
                else if
                (ActiveFont.WidthToNextLine(customRoomName, 0) * 0.7 > 510)
                {
                    textScale = new Vector2(0.5f, 0.5f);
                }

                if (currentItemIndex >= firstRowShown && currentItemIndex < lastRowShown)
                {
                    int displayRow = currentItemIndex - firstRowShown;
                    int col2BufferCurrent = 0;
                    if (currentItemIndex - firstRowShown >= roomsPerColumn)
                    {
                        displayRow += -roomsPerColumn;
                        col2BufferCurrent = col2Buffer;
                    }

                    Color bgColor;
                    if (EndHelperModule.Settings.RoomStatMenu.MenuMulticolor)
                    {
                        int colorIndex;

                        if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear && EndHelperModule.SaveData.mapDict_roomStat_colorIndex[mapNameSide_Internal].ContainsKey(roomName))
                        {
                            colorIndex = EndHelperModule.SaveData.mapDict_roomStat_colorIndex[mapNameSide_Internal][roomName];
                        }
                        else
                        {
                            colorIndex = Convert.ToInt32(EndHelperModule.Session.roomStatDict_colorIndex[roomName]);
                        }

                        switch (colorIndex)
                        {
                            case 0: bgColor = Color.Gray; break;
                            case 1: bgColor = Color.DarkOrange; break;
                            case 2: bgColor = Color.LimeGreen; break;
                            case 3: bgColor = Color.Cyan; break;
                            case 4: bgColor = Color.Blue; break;
                            case 5: bgColor = Color.Magenta; break;
                            case 6: bgColor = Color.DarkRed; break;
                            default: bgColor = Color.White; break;
                        }
                    }
                    else
                    {
                        bgColor = Color.White;
                    }

                    if (roomNameEditMenuOpen && editingRoomIndex == currentItemIndex)
                    {
                        editingRoomName = roomName; // Update editingRoomName while updating the bg
                        backgroundTextureEdit.Draw(new Vector2(startX + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);

                        if (renameRoomsMoveRooms) // Checking journal input here directly might be buggy idk?
                        {
                            moveRoomArrows.DrawOutline(new Vector2(startX + col2BufferCurrent - 40, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0.5f), Color.White, 1.3f);
                        }
                    }
                    else
                    {
                        backgroundTexture.Draw(new Vector2(startX + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                    }
                    ActiveFont.DrawOutline(shortenedRoomName, new Vector2(startX + bufferX + col2BufferCurrent, startY + heightBetweenRows * displayRow + (heightBetweenRows - 5) / 2), new Vector2(0f, 0.5f), textScale, Color.White, 2f, Color.Black);

                    backgroundTextureShort.Draw(new Vector2(startX_death + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                    ActiveFont.DrawOutline($"{roomDeaths}", new Vector2(startX_death + col2BufferCurrent + width_death / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                    switch (EndHelperModule.Settings.RoomStatMenu.MenuShowTime)
                    {
                        case RoomStatMenuSubMenu.MenuShowTimeEnum.Normal:
                            backgroundTextureShort.Draw(new Vector2(startX_timer + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                            ActiveFont.DrawOutline(roomTimeString, new Vector2(startX_timer + col2BufferCurrent + width_timer / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                            break;
                        case RoomStatMenuSubMenu.MenuShowTimeEnum.RTA:
                            backgroundTextureShort.Draw(new Vector2(startX_timer + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                            ActiveFont.DrawOutline(roomRtaTimeString, new Vector2(startX_timer + col2BufferCurrent + width_timer / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                            break;
                        case RoomStatMenuSubMenu.MenuShowTimeEnum.Both:
                            backgroundTextureMed.Draw(new Vector2(startX_timer + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                            if (roomRtaTimeString == "0:00")
                            {
                                ActiveFont.DrawOutline(roomTimeString, new Vector2(startX_timer + col2BufferCurrent + width_timer / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                            }
                            else
                            {
                                ActiveFont.DrawOutline($"{roomTimeString} / {roomRtaTimeString}", new Vector2(startX_timer + col2BufferCurrent + width_timer / 2, startY + heightBetweenRows * displayRow + 3), new Vector2(0.5f, 0f), new Vector2(0.6f, 0.6f), Color.White, 2f, Color.Black);
                            }
                            break;
                    }

                    if (roomStrawberriesCollected > 0)
                    {
                        ActiveFont.DrawOutline($":EndHelper/uioutline_strawberry:", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows / 2 - 3), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                    }
                    if (roomStrawberriesCollected > 1)
                    {
                        ActiveFont.DrawOutline($"{roomStrawberriesCollected}", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows / 2), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.White, 2f, Color.Black);
                    }
                }


                clipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t";
                if (EndHelperModule.Settings.RoomStatMenu.MenuShowTime != RoomStatMenuSubMenu.MenuShowTimeEnum.RTA) { clipboardText += $"{roomTimeString}\t"; }
                if (EndHelperModule.Settings.RoomStatMenu.MenuShowTime != RoomStatMenuSubMenu.MenuShowTimeEnum.Normal) { clipboardText += $"{roomRtaTimeString}\t"; }
                if (roomStrawberriesCollected > 0) { clipboardText += $"{roomStrawberriesCollected}\t"; }
                currentItemIndex++;
            }

            // Total Stats
            bool showTotalMapBerryCount = true;
            if (!EndHelperModule.Settings.RoomStatMenu.MenuSpoilBerries && !SaveData.Instance.Areas_Safe[level.Session.Area.ID].Modes[(int)level.Session.Area.Mode].Completed)
            {
                showTotalMapBerryCount = false; // No berry count spoilery
            }

            String totalText = "Total";
            if (filterSetting != roomStatMenuFilter.None) { totalText += $" [{filterString}]"; }
            IconType iconType = EndHelperModule.Session.pauseTypeDict["Level_Invalid"] == true ? IconType.Gray : IconType.White;

            ShowGUIStats(currentEffectiveRoomName, 100, 1010, 0.7f, Color.White, true, 0, true, true, false, showTotalMapBerryCount, level.Session.MapData.DetectedStrawberries, $"{totalText}: ", "", totalDeaths, totalTimer, totalRtaTimer, totalStrawberries, iconType);
        }

        // Instructions
        int instructionXPos = 100;
        const int instructionYPos = 1060;
        const float instructionScale = 0.4f;
        Color instructionColor = Color.LightGray;

        if (!roomNameEditMenuOpen && !inCreateSegmentRoomMenu)
        {
            // Normal room stats instructions
            ActiveFont.DrawOutline("Edit Room Name: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Edit Room Name: XI", 0) * instructionScale);
            Input.GuiButton(Input.QuickRestart, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            ActiveFont.DrawOutline("Copy to Clipboard: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Copy to Clipboard: XI", 0) * instructionScale);
            Input.GuiButton(Input.MenuCancel, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            ActiveFont.DrawOutline($"Filter [{filterString}]:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Filter [{filterString}]: XI", 0) * instructionScale);
            Input.GuiButton(Input.MenuConfirm, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            ActiveFont.DrawOutline($"Segment/Fuse Room:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Segment/Fuse Room: XI", 0) * instructionScale);
            Input.GuiButton(Input.MenuJournal, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            if (dictSize > roomsPerColumn * 2)
            {
                ActiveFont.DrawOutline("Change Page: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Page: XI", 0) * instructionScale);
                Input.GuiButton(Input.MenuLeft, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"XX", 0) * instructionScale);
                Input.GuiButton(Input.MenuRight, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
            }
        } 
        else if (!inCreateSegmentRoomMenu)
        {
            // Rename menu instructions
            ActiveFont.DrawOutline("Stop Editing: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Stop Editing: XI", 0) * instructionScale);
            Input.GuiButton(Input.ESC, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            ActiveFont.DrawOutline("Change Selection: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Selection: XI", 0) * instructionScale);
            upKey.DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XX", 0) * instructionScale);
            downKey.DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XX", 0) * instructionScale);
            leftKey.DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XX", 0) * instructionScale);
            rightKey.DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);

            ActiveFont.DrawOutline("Move Room: Hold", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Move Room: Hold  XI", 0) * instructionScale);
            Input.GuiButton(Input.MenuJournal, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
        }

        if (!inCreateSegmentRoomMenu)
        {
            // Timer and Death Count Freeze Icons
            String pauseIconMsg = "";

            if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("Pause")) { EndHelperModule.Session.pauseTypeDict["Pause"] = false; }
            if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("Inactive")) { EndHelperModule.Session.pauseTypeDict["Inactive"] = false; }
            if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("AFK")) { EndHelperModule.Session.pauseTypeDict["AFK"] = false; }
            if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("LoadNoDeath")) { EndHelperModule.Session.pauseTypeDict["LoadNoDeath"] = false; }

            if (EndHelperModule.Session.pauseTypeDict["LoadNoDeath"])
            {
                pauseIconMsg += ":EndHelper/ui_deathfreeze_loadstaterespawn:";
            }
            if (EndHelperModule.Session.pauseTypeDict["Pause"])
            {
                pauseIconMsg += ":EndHelper/ui_timerfreeze_pause:";
            }
            if (EndHelperModule.Session.pauseTypeDict["Inactive"])
            {
                pauseIconMsg += ":EndHelper/ui_timerfreeze_inactive:";
            }
            if (EndHelperModule.Session.pauseTypeDict["AFK"])
            {
                pauseIconMsg += ":EndHelper/ui_timerfreeze_afk:";
            }
            ActiveFont.DrawOutline(pauseIconMsg, new Vector2(1820, instructionYPos + 80), new Vector2(1f, 0.5f), Vector2.One * 3f, Color.DarkGray, 1f, Color.Black);

            string totalTimeString = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(totalTimer));
            string totalRtaTimeString = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(totalRtaTimer));

            clipboardText += $"\r\n{"Total"}\t{totalDeaths}\t";
            switch (EndHelperModule.Settings.RoomStatMenu.MenuShowTime)
            {
                case RoomStatMenuSubMenu.MenuShowTimeEnum.Normal:
                    clipboardText += $"{totalTimeString}\t";
                    break;
                case RoomStatMenuSubMenu.MenuShowTimeEnum.RTA:
                    clipboardText += $"{totalRtaTimeString}\t";
                    break;
                case RoomStatMenuSubMenu.MenuShowTimeEnum.Both:
                    clipboardText += $"{totalTimeString}\t{totalRtaTimeString}\t";
                    break;

            }
            clipboardText += $"{totalStrawberries}";


            // Page Number
            int currentPage = (int)Math.Ceiling((float)(firstRowShown + 1) / (roomsPerColumn * 2));
            if (dictSize > roomsPerColumn * 2)
            {
                int totalPage = (int)Math.Ceiling((float)dictSize / (roomsPerColumn * 2));

                const int pageXpos = 1740;

                ActiveFont.DrawOutline($"{currentPage}/{totalPage}", new Vector2(pageXpos, 1010), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                // Left
                if (currentPage != 1)
                {
                    pageArrow.DrawCentered(new Vector2(pageXpos - 60, 1010), new Color(1f, 1f, 1f, 1f), 1, MathF.PI);
                }
                else
                {
                    pageArrow.DrawCentered(new Vector2(pageXpos - 60, 1010), new Color(0.3f, 0.3f, 0.3f, 0.3f), 1, MathF.PI);
                }
                // Right
                if (currentPage < totalPage)
                {
                    pageArrow.DrawCentered(new Vector2(pageXpos + 60, 1010), new Color(1f, 1f, 1f, 1f), 1, 0);
                }
                else
                {
                    pageArrow.DrawCentered(new Vector2(pageXpos + 60, 1010), new Color(0.3f, 0.3f, 0.3f, 0.3f), 1, 0);
                }
            }

            // Scroll Sets of rooms
            MInput.Disabled = false;
            if (!roomNameEditMenuOpen)
            {
                firstRowShown = Utils_General.ScrollInput(valueToChange: firstRowShown, increaseInput: Input.MenuDown, increaseValue: roomsPerColumn * 2,
                    decreaseInput: Input.MenuUp, decreaseValue: roomsPerColumn * 2, minValue: 0, maxValue: (dictSize - 1) - (dictSize - 1) % (roomsPerColumn * 2), loopValues: false, doNotChangeIfPastCap: true,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
                firstRowShown = Utils_General.ScrollInput(valueToChange: firstRowShown, increaseInput: Input.MenuRight, increaseValue: roomsPerColumn * 2,
                    decreaseInput: Input.MenuLeft, decreaseValue: roomsPerColumn * 2, minValue: 0, maxValue: (dictSize - 1) - (dictSize - 1) % (roomsPerColumn * 2), loopValues: false, doNotChangeIfPastCap: true,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
            }

            // Ensure room that is being renamed is currently viewble
            if (roomNameEditMenuOpen)
            {
                int oldEditingRoomIndex = editingRoomIndex;

                // Allow changing rooms
                editingRoomIndex = Utils_General.ScrollInput(valueToChange: editingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.PageDown), increaseValue: 1,
                    decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.PageUp), decreaseValue: 1, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
                editingRoomIndex = Utils_General.ScrollInput(valueToChange: editingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Down), increaseValue: 1,
                    decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Up), decreaseValue: 1, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);

                if (!MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Up) && !MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    editingRoomIndex = Utils_General.ScrollInput(valueToChange: editingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Right), increaseValue: roomsPerColumn,
                        decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Left), decreaseValue: roomsPerColumn, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                        framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
                }

                int editRoomPage = (int)Math.Ceiling((editingRoomIndex + 1f) / (roomsPerColumn * 2));
                if (currentPage > editRoomPage)
                {
                    firstRowShown -= roomsPerColumn * 2;
                }
                else if (currentPage < editRoomPage)
                {
                    firstRowShown += roomsPerColumn * 2;
                }

                // Allow moving rooms
                if (Input.MenuJournal.Check)
                {
                    journalRoomStatMenuTypeEnum? menuType = EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear ? journalRoomStatMenuTypeEnum.FirstClear : null;
                    Utils_JournalStatistics.EditingRoomMovePosition(oldEditingRoomIndex, editingRoomIndex, mapNameSide_Internal, menuType);
                    renameRoomsMoveRooms = true;
                } 
                else
                {
                    renameRoomsMoveRooms = false;
                }
            } else
            {
                renameRoomsMoveRooms = false;
            }
        }

        // Room Stat Menu Buttons Functionality
        if (!roomNameEditMenuOpen && !inCreateSegmentRoomMenu)
        {
            if (Input.QuickRestart.Pressed)
            {
                // Renaming current room
                Utils_General.ConsumeInput(Input.QuickRestart, 3);

                //LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"open text menu");
                roomNameEditMenuOpen = true;
                Audio.Play("event:/ui/main/message_confirm");
                TextInput.OnInput += OnTextInput;

            } 
            else if (Input.MenuConfirm.Pressed)
            {
                // Change Filter
                filterSetting = (roomStatMenuFilter)(((int)filterSetting + 1) % Enum.GetValues(typeof(roomStatMenuFilter)).Length);
            }
            else if (Input.MenuJournal.Pressed)
            {
                // Segment current room (bring up menu)
                CreateSegmentRoomMenu(level, currentEffectiveRoomName, currentRoomName);
            }
        }
        MInput.Disabled = true;
    }

    #endregion

    #region Room Rename

    public bool roomNameEditMenuOpen = false;

    private void OnTextInput(char c)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"i have typed {c}");
        String roomCustomName = Convert.ToString(EndHelperModule.Session.roomStatDict_customName[editingRoomName]);

        if (c == (char)13 || c == (char)9)
        {
            // Do not allow: Enter, Tab
        }
        else if (c == (char)8 || c == (char)24)
        {
            // Getting the length can crash. No, I have absolutely no idea how.
            try { int roomCustomNameLength = roomCustomName.Length; }
            catch (Exception) { roomCustomName = ""; }

            // Trim: Backspace, Cancel. Whatever Cancel is.
            if (roomCustomName.Length > 0)
            {
                Audio.Play("event:/ui/main/rename_entry_backspace");
                roomCustomName = roomCustomName.Remove(roomCustomName.Length - 1);

                // If over the limit, make it not over the limit.
                while (ActiveFont.WidthToNextLine($"{roomCustomName}", 0) * 0.4 > 510)
                {
                    roomCustomName = roomCustomName.Remove(roomCustomName.Length - 1);
                }
            }
        }
        else if (c == (char)127)
        {
            // Clear name: Delete
            roomCustomName = "";
            Audio.Play("event:/ui/main/rename_entry_rollover");
        }
        else if (ActiveFont.WidthToNextLine($"{roomCustomName}{c}", 0) * 0.4 < 510)
        {
            // Append
            if (c == (char)32) // Space
            {
                Audio.Play("event:/ui/main/rename_entry_space");
            } 
            else
            {
                Audio.Play("event:/ui/main/rename_entry_char");
            }
            roomCustomName = $"{roomCustomName}{c}";
        }
        EndHelperModule.Session.roomStatDict_customName[editingRoomName] = roomCustomName;

        // if (!roomNameEditMenuOpen){ TextInput.OnInput -= OnTextInput; } // this attempted failsafe does not work lol
    }

    #endregion

    #region Helper Functions

    public static Color GetMapColour(AreaKey areaKey)
    {
        String SID = areaKey.SID;
        Color mapNameColor = Color.Lime;
        Dictionary<string, Color> difficultyColourMap = new Dictionary<string, Color>
        {
            { "easy", Color.LimeGreen },
            { "medium", Color.PaleVioletRed },
            { "hard", Color.MediumPurple },
            { "beginner", Color.Aqua },
            { "intermediate", Color.IndianRed },
            { "advanced", Color.Yellow },
            { "expert", Color.Orange },
            { "grandmaster", Color.Magenta },
            { "astral", Color.LightSlateGray },
            { "celestial", Color.Beige },
            { "cosmic", Color.GhostWhite }
        };

        foreach (KeyValuePair<string, Color> difficultyColour in difficultyColourMap)
        {
            string difficulty = difficultyColour.Key;
            Color colour = difficultyColour.Value;

            if (SID.ToLower().EndsWith(difficulty) || SID.ToLower().Contains($"{difficulty}/"))
            {
                mapNameColor = colour;
                break;
            }
        }
        return mapNameColor;
    }

    public static string GetMapNameSideDisplay(AreaKey areaKey)
    {
        // Get map name header text
        String mapNameSide_Display = areaKey.SID;
        if (mapNameSide_Display.StartsWith("Celeste/"))
        {
            int mapID = areaKey.ID;
            mapNameSide_Display = $"AREA_{mapID}";
        }
        mapNameSide_Display = mapNameSide_Display.DialogCleanOrNull(Dialog.Languages["english"]) ?? mapNameSide_Display;

        // Ensure a reasonably sized name
        int displayNameSize = (int)(ActiveFont.WidthToNextLine(mapNameSide_Display, 0) * 0.9f);
        int maxDisplayNameSize = 1800;
        if (displayNameSize > maxDisplayNameSize)
        {
            while (displayNameSize > maxDisplayNameSize - 50)
            {
                mapNameSide_Display = mapNameSide_Display[..^1];
                displayNameSize = (int)(ActiveFont.WidthToNextLine(mapNameSide_Display, 0) * 0.9f);
            }
            mapNameSide_Display = $"{mapNameSide_Display}...";
        }


        AreaMode side = areaKey.Mode;
        if (side == AreaMode.BSide)
        {
            mapNameSide_Display += " B";
        }
        else if (side == AreaMode.CSide)
        {
            mapNameSide_Display += " C";
        }
        return mapNameSide_Display;
    }

    public static string GetMapNameSideInternal(AreaKey areaKey)
    {
        String mapNameSide_Internal = areaKey.SID;
        if (areaKey.Mode == AreaMode.BSide) { mapNameSide_Internal += "_B"; }
        else if (areaKey.Mode == AreaMode.CSide) { mapNameSide_Internal += "_C"; }
        return mapNameSide_Internal;
    }
    public void AddDeath()
    {
        // NOTE: Mnaual AddDeath at EndHelperModule > EnterMapFunc
        Level level = SceneAs<Level>();
        EnsureDictsHaveKey(level, currentEffectiveRoomName, DictsHaveKeyType.All); // Check if dict has current room, just in case.
        EndHelperModule.Session.roomStatDict_death[currentEffectiveRoomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentEffectiveRoomName]) + 1;

        if (dealWithFirstClear)
        {
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][currentEffectiveRoomName]++;
        }
    }

    public void AddTimer()
    {
        EndHelperModule.Session.roomStatDict_timer[currentEffectiveRoomName] = TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime).Ticks + Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentEffectiveRoomName]);

        if (dealWithFirstClear)
        {
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][currentEffectiveRoomName] += TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime).Ticks;
        }
    }

    public void AddRTATimer(long addTicks)
    {
        EndHelperModule.Session.roomStatDict_rtatimer[currentEffectiveRoomName] = addTicks + Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[currentEffectiveRoomName]);

        if (dealWithFirstClear)
        {
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][currentEffectiveRoomName] += addTicks;
        }
    }

    public void AddStrawberry(Strawberry strawberry)
    {
        DynamicData strawberryData = DynamicData.For(strawberry);
        bool isGhostBerry = strawberryData.Get<bool>("isGhostBerry"); //Must be false to add to first clear list
        string homeroom = strawberry.Get<HomeRoom>().roomName;

        if (homeroom == "")
        {
            homeroom = currentEffectiveRoomName;
            //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"can't get homeroom, using current room {homeroom}");
        }
        else
        {
            homeroom = GetEffectiveRoomName(homeroom);
            //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"strawberry homeroom = {homeroom}");
        }

        EndHelperModule.Session.roomStatDict_strawberries[homeroom] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[homeroom]) + 1;

        if (dealWithFirstClear && !isGhostBerry)
        {
            EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][homeroom] += 1;
        }
    }

    public enum DictsHaveKeyType { All, FirstClear, Current }
    // This does not include latest session
    public void EnsureDictsHaveKey(Level level, String roomName = null, DictsHaveKeyType dictsHaveKeyType = DictsHaveKeyType.All)
    {
        roomName ??= currentEffectiveRoomName;
        if (roomName == "") return;

        // Custom Names are seperated as additional extra keys can be stored
        // Strawberries are seperated as they are undone during load state unlike the rest.

        if (!EndHelperModule.Session.roomStatDict_customName.ContainsKey(roomName))
        {
            EndHelperModule.Session.roomStatDict_customName[roomName] = $"{mapNameSide_Internal}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;
        }
        if (!EndHelperModule.Session.roomStatDict_colorIndex.Contains(roomName) && roomName == currentEffectiveRoomName)
        {
            EndHelperModule.Session.roomStatDict_colorIndex[roomName] = (int)level.Session.LevelData.EditorColorIndex;
            UpdateSaveDataColorIndex();
        }


        if (dictsHaveKeyType == DictsHaveKeyType.All || dictsHaveKeyType == DictsHaveKeyType.Current)
        {
            if (!EndHelperModule.Session.roomStatDict_death.Contains(roomName) || !EndHelperModule.Session.roomStatDict_timer.Contains(roomName))
            {
                EndHelperModule.Session.roomStatDict_death[roomName] = (int)0;
                EndHelperModule.Session.roomStatDict_timer[roomName] = (long)0;
            }
            if (!EndHelperModule.Session.roomStatDict_rtatimer.Contains(roomName))
            {
                EndHelperModule.Session.roomStatDict_rtatimer[roomName] = (int)0;
            }
            if (!EndHelperModule.Session.roomStatDict_strawberries.Contains(roomName))
            {
                EndHelperModule.Session.roomStatDict_strawberries[roomName] = (int)0;
            }
        }


        // All the first clear stuff
        if (dictsHaveKeyType == DictsHaveKeyType.All)
        {
            // For version updating - Empty rtatimer
            if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer.ContainsKey(mapNameSide_Internal) || EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal].Count == 0)
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal] = [];
                foreach (String timerRooms in EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal].Keys)
                {
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][timerRooms] = 0;
                }
            }

            if (dealWithFirstClear && (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Contains(roomName) || !EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal].ContainsKey(roomName)))
            {
                // Take existing data if it exists. This is pretty much just so if updating from prev ver the first clear doesn't reset if it doesn't need to
                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Count == 0)
                {
                    // This is for upgrading from prev ver. Add every room in order.
                    foreach (DictionaryEntry sessionDeathDict in EndHelperModule.Session.roomStatDict_death)
                    {
                        string sessionDeathDictRoomName = (String)sessionDeathDict.Key;

                        if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Contains(sessionDeathDictRoomName))
                        {
                            EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Add(sessionDeathDictRoomName);
                        }
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][sessionDeathDictRoomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[sessionDeathDictRoomName]);
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][sessionDeathDictRoomName] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[sessionDeathDictRoomName]);
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][sessionDeathDictRoomName] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[sessionDeathDictRoomName]);

                        if (EndHelperModule.Session.roomStatDict_strawberries.Contains(sessionDeathDictRoomName))
                        {
                            EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][sessionDeathDictRoomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[sessionDeathDictRoomName]);
                        }
                        else
                        {
                            EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][sessionDeathDictRoomName] = 0;
                        }
                    }
                }
                else {
                    // Else just add the new room
                    if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Contains(roomName))
                    {
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Add(roomName);
                    }
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]);
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][roomName] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[roomName]);

                    if (EndHelperModule.Session.roomStatDict_strawberries.Contains(roomName))
                    {
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);
                    }
                    else
                    {
                        EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName] = 0;
                    }
                }
            }
        }

        else if (dictsHaveKeyType == DictsHaveKeyType.FirstClear)
        {
            // Updating just the first clear isn't used, but this is here just in case.
            if (dealWithFirstClear && !EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Contains(roomName))
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Add(roomName);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName] = 0;
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName] = 0;
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][roomName] = 0;
            }
            if (dealWithFirstClear && !EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal].ContainsKey(roomName))
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName] = 0;
            }
        }
    }

    public void CombineRoomStats(Level level, String headRoom, String combinedRoom, DictsHaveKeyType combineDicts = DictsHaveKeyType.All)
    {
        EnsureDictsHaveKey(level, headRoom, DictsHaveKeyType.All);

        if (combineDicts == DictsHaveKeyType.All || combineDicts == DictsHaveKeyType.Current)
        {
            if (EndHelperModule.Session.roomStatDict_death.Contains(combinedRoom))
            {
                EndHelperModule.Session.roomStatDict_death[headRoom] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[headRoom]) + Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[combinedRoom]);
                EndHelperModule.Session.roomStatDict_timer[headRoom] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[headRoom]) + Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[combinedRoom]);
                EndHelperModule.Session.roomStatDict_rtatimer[headRoom] = Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[headRoom]) + Convert.ToInt64(EndHelperModule.Session.roomStatDict_rtatimer[combinedRoom]);
                EndHelperModule.Session.roomStatDict_death.Remove(combinedRoom);
                EndHelperModule.Session.roomStatDict_rtatimer.Remove(combinedRoom);
            }
            if (EndHelperModule.Session.roomStatDict_strawberries.Contains(combinedRoom))
            {
                EndHelperModule.Session.roomStatDict_strawberries[headRoom] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[headRoom]) + Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[combinedRoom]);
                EndHelperModule.Session.roomStatDict_strawberries.Remove(combinedRoom);
            }
            // The regret of making the ordereddictionaries...
            if (EndHelperModule.Session.roomStatDict_colorIndex.Contains(combinedRoom))
            {
                EndHelperModule.Session.roomStatDict_colorIndex.Remove(combinedRoom);
            }
        }

        if ((combineDicts == DictsHaveKeyType.All || combineDicts == DictsHaveKeyType.FirstClear) && dealWithFirstClear)
        {
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal].ContainsKey(combinedRoom))
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][headRoom] += EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][combinedRoom];
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][headRoom] += EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][combinedRoom];
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][headRoom] += EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal][combinedRoom];
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal].Remove(combinedRoom);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal].Remove(combinedRoom);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer[mapNameSide_Internal].Remove(combinedRoom);
            }

            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal].ContainsKey(combinedRoom))
            {
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][headRoom] += EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][combinedRoom];
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal].Remove(combinedRoom);
            }

            EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Remove(combinedRoom);
        }

        currentRoomName = headRoom;
        currentEffectiveRoomName = GetEffectiveRoomName(currentRoomName);
    }

    static private bool CheckIfSessionDictHaveRoomName(String roomName)
    {
        if (EndHelperModule.Session.roomStatDict_death.Contains(roomName)) { return true; } 
        else { return false; }
    }

    private void CreateRoomSeg(Level level, String roomName)
    {
        roomName = GetRoomNameNoSeg(roomName);
        int nextRoomSeg = GetRoomSegCount(roomName) + 1;

        // The existence of a segmented room will be enough, as code checks for latest existing segment.
        EnsureDictsHaveKey(level, $"{roomName}%seg{nextRoomSeg}");
    }

    // This should only run if there's multiple segments! Otherwise previousseg will fail. And also this just won't make sense.
    private void RemoveLatestRoomSeg(Level level, String roomName)
    {
        // Simply remove the latest segment
        roomName = GetRoomNameNoSeg(roomName);
        string roomNameLatestSeg = GetRoomNameLatestSeg(roomName);
        string roomNamePrevSeg = GetRoomNameLatestSeg(roomName, -1);

        // Absorb latest seg into previous seg
        CombineRoomStats(level, roomNamePrevSeg, roomNameLatestSeg, DictsHaveKeyType.All);
    }

    private void CreateRoomFuse(Level level, String currentEffectiveRoomName, String redirectRoomName)
    {
        currentEffectiveRoomName = GetRoomNameNoSeg(currentEffectiveRoomName);
        redirectRoomName = GetRoomNameNoSeg(redirectRoomName);

        // Add room fusion by creating a new key in roomStatDict_fuseRoomRedirect.
        if (EndHelperModule.Session.roomStatDict_fuseRoomRedirect.ContainsKey(currentEffectiveRoomName))
        {
            // It shouldn't already be in roomStatDict_fuseRoomRedirect, since you can't modify a fused room directly (only by the head room)
            throw new Exception("Tried fusing an already fused room. This should never happen since fused rooms aren't accessible directly.");
        }
        else
        {
            EndHelperModule.Session.roomStatDict_fuseRoomRedirect.Add(currentEffectiveRoomName, redirectRoomName);

            // Ensure redirectRoomName exists. There's a possibly it, eg, exists in first clear list but not current session list.
            EnsureDictsHaveKey(level, redirectRoomName);

            // Now combine stats!
            CombineRoomStats(level, redirectRoomName, currentEffectiveRoomName, DictsHaveKeyType.All);
        }
    }

    private static void RemoveRoomFuse(String fuseRoomName)
    {
        fuseRoomName = GetRoomNameNoSeg(fuseRoomName);
        EndHelperModule.Session.roomStatDict_fuseRoomRedirect.Remove(fuseRoomName);
    }

    // Finds room fused to a head room. Currently unused.
    //private string FindRoomFuse(String headRoomName)
    //{
    //    headRoomName = GetRoomNameNoSeg(headRoomName);

    //    // Remove room fuse. this will have to check all values and return the matching key.
    //    foreach (KeyValuePair<string, string> roomEntry in EndHelperModule.Session.roomStatDict_fuseRoomRedirect)
    //    {
    //        // Search for redirect. If found, remove the entry with that key
    //        string fuseRoomName = roomEntry.Key;
    //        string redirectRoomName = roomEntry.Value;

    //        if (redirectRoomName == headRoomName)
    //        {
    //            return fuseRoomName;
    //        }
    //    }
    //    return null;
    //}

    private string getPreviousRoomToFuseWith(string roomName) // Return null if no previous room
    {
        if (EndHelperModule.Settings.RoomStatMenu.MenuShowFirstClear && dealWithFirstClear)
        {
            // Check if first item
            int index = EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].IndexOf(roomName);
            if (index <= 0)
            {
                // Either not found or first item. Either way, there is no previous room, so return null.
                return null;
            }
            else
            {
                return EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal][index - 1];
            }
        }
        else
        {
            // Check if first item
            IList ilist = (IList)EndHelperModule.Session.roomStatDict_death.Keys;
            int index = ilist.IndexOf(roomName);
            if (index <= 0)
            {
                // Either not found or first item. Either way, there is no previous room, so return null.
                return null;
            }
            else
            {
                return Convert.ToString(ilist[index - 1]);
            }
        }
    }

    static private int GetRoomSegCount(String roomName)
    {
        roomName = GetRoomNameNoSeg(roomName);

        for (int segNum = 2; true; segNum++)
        {
            // Find next available segment number
            if (!CheckIfSessionDictHaveRoomName($"{roomName}%seg{segNum}"))
            {
                return segNum - 1; // Subtract to get final used number (min 1 for no segment)
            }
        }
    }

    // Given a raw room name, what internal room name should be used for room stats.
    // This means accounting for fuse redirects and room segments.
    static public string GetEffectiveRoomName(String roomName, int segOffset = 0)
    {
        roomName = GetRoomFuseRedirect(roomName, true); // Fuse redirects FIRST
        roomName = GetRoomNameLatestSeg(roomName, segOffset); // Then get latest segment
        return roomName;
    }

    static private string GetRoomNameLatestSeg(String roomName, int segOffset = 0)
    {
        roomName = GetRoomNameNoSeg(roomName);
        int latestSeg = GetRoomSegCount(roomName);

        latestSeg += segOffset;

        if (latestSeg <= 1)
        {
            return roomName;
        } 
        else
        {
            return $"{roomName}%seg{latestSeg}";
        }
    }

    // allRedirects means this will constantly iterate until it finds the head room, otherwise it will only redirect once
    static private string GetRoomFuseRedirect(String roomName, bool allRedirects = true)
    {
        roomName = GetRoomNameNoSeg(roomName); // Make sure we are dealing with the raw, non-segmented room names.

        // Key is the current room, value is the redirected room. Set value as roomName to get redirect.
        if (EndHelperModule.Session.roomStatDict_fuseRoomRedirect.ContainsKey(roomName))
        {
            roomName = EndHelperModule.Session.roomStatDict_fuseRoomRedirect[roomName];
        }

        // If allRedirects == true, repeat this until the room isn't found to get the head room.
        while (allRedirects && EndHelperModule.Session.roomStatDict_fuseRoomRedirect.ContainsKey(roomName))
        {
            roomName = EndHelperModule.Session.roomStatDict_fuseRoomRedirect[roomName];
        }

        return roomName;
    }

    // Note: A room name cannot have segments and a fuse redirect at the same time
    // Trying to fuse a segmented room will remove a segment.
    // Trying to segment a fused room will segment the head room instead.
    static private string GetRoomNameNoSeg(String roomName)
    {
        if (roomName.Contains("%seg"))
        {
            int segIndex = roomName.LastIndexOf("%seg");
            if (segIndex > -1)
            {
                roomName = roomName.Substring(0, segIndex); // Remove everything from %seg onwards
            }
        }
        return roomName;
    }

    private void RemoveRoomData(String roomName, bool resetCustomName = true, bool removeFromFirstClear = false)
    {
        if (resetCustomName)
        {
            EndHelperModule.Session.roomStatDict_customName.Remove(roomName);
        }
        EndHelperModule.Session.roomStatDict_death.Remove(roomName);
        EndHelperModule.Session.roomStatDict_timer.Remove(roomName);
        EndHelperModule.Session.roomStatDict_rtatimer.Remove(roomName);
        EndHelperModule.Session.roomStatDict_strawberries.Remove(roomName);
        EndHelperModule.Session.roomStatDict_colorIndex.Remove(roomName);

        if (removeFromFirstClear)
        {
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.TryGetValue(mapNameSide_Internal, out List<string> roomOrderDict))
            {
                roomOrderDict.Remove(roomName);
            }
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_death.TryGetValue(mapNameSide_Internal, out Dictionary<string, int> deathDict))
            {
                deathDict.Remove(roomName);
            }
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer.TryGetValue(mapNameSide_Internal, out Dictionary<string, long> timerDict))
            {
                timerDict.Remove(roomName);
            }
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer.TryGetValue(mapNameSide_Internal, out Dictionary<string, long> timerRtaDict))
            {
                timerRtaDict.Remove(roomName);
            }
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries.TryGetValue(mapNameSide_Internal, out Dictionary<string, int> strawDict))
            {
                strawDict.Remove(roomName);
            }
        }
    }

    private string GetShortenedDisplayRoomName(string roomName, bool getEffectiveRoomName = true)
    {
        if (getEffectiveRoomName)
        {
            roomName = GetEffectiveRoomName(roomName);
        }
        if (EndHelperModule.Session.roomStatDict_customName.TryGetValue(roomName, out string value))
        {
            roomName = value;
        }
        
        if (roomName.Length > 30)
        {
            roomName = $"{roomName[..30]}...";
        }
        return roomName;
    }

    private bool inCreateSegmentRoomMenu = false;
    private void CreateSegmentRoomMenu(Level level, String currentEffectiveRoomName, String currentRoomName)
    {
        String displayCurrentEffectiveRoomName = GetShortenedDisplayRoomName(currentEffectiveRoomName);

        inCreateSegmentRoomMenu = true;
        TextMenu menu = new TextMenu();
        menu.AutoScroll = false;
        menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
        menu.Add(new TextMenu.Header(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentFuseRoomHeader")));
        menu.Add(new TextMenu.SubHeader(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentFuseRoomSubHeader")));

        String newSegName = GetEffectiveRoomName(currentEffectiveRoomName, +1);

        String headerSegment = "";
        String headerFuse = "";

        // SEGMENT HEADER
        bool currentRoomFused = GetRoomFuseRedirect(currentRoomName, false) != currentRoomName;
        if (currentRoomFused)
        {
            // If currentRoomName is a fused room - unfuse instead
            String fuseAttachTo = GetRoomFuseRedirect(currentRoomName, true);
            String displayFuseAttachTo = GetShortenedDisplayRoomName(fuseAttachTo);
            headerSegment = $"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_UnfuseRoomSubHeader")} {displayFuseAttachTo}.";
        }
        else
        {
            // If segment room is new (normal segmenting)
            headerSegment = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomSubHeader");
        }

        // FUSE HEADER
        String roomFuseWith = getPreviousRoomToFuseWith(currentEffectiveRoomName); // null = CANNOT FUSE
        String displayRoomFuseWith = roomFuseWith == null ? null : GetShortenedDisplayRoomName(roomFuseWith);

        if (GetRoomSegCount(currentEffectiveRoomName) > 1)
        {
            // If currentEffectiveRoomName is segmented
            headerFuse = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_FuseSegmentsSubHeader");
        }
        else if (roomFuseWith == null)
        {
            // Cannot fuse
            headerFuse = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_CannotFuseRoomSubHeader");
        }
        else
        {
            // Normal fuse message
            headerFuse = $"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_FuseRoomSubHeader")} {displayRoomFuseWith}.";
        }

        menu.Add(new TextMenu.SubHeader($"{headerSegment}\n{headerFuse}"));


        // BUTTONS
        menu.Add(new TextMenu.SubHeader("")); // Empty

        // SEGMENT
        if (currentRoomFused)
        {
            // Current room is fused. Unfuse instead.
            String fuseAttachToDisplay = GetShortenedDisplayRoomName(currentRoomName, false);
            menu.Add(new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmUnfuseRoom")} {fuseAttachToDisplay} {Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmUnfuseRoom_from")} {displayCurrentEffectiveRoomName}").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
                // Unfuse rooms
                Audio.Play("event:/ui/main/button_select");
                RemoveRoomFuse(currentRoomName);
                exitMenuCommands();
            }));
        }
        else
        {
            // Normal segmenting
            String displayNewSegName = GetShortenedDisplayRoomName(newSegName, false);

            menu.Add(new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmSegmentRoom")} {displayNewSegName}").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
                // Segment current room
                Audio.Play("event:/ui/main/button_select");
                CreateRoomSeg(level, currentEffectiveRoomName);
                exitMenuCommands();
            }));
        }

        // FUSE
        if (GetRoomSegCount(currentEffectiveRoomName) > 1)
        {
            // Segmented room - Remove segment fuse
            String displayPreviousSegmentRoomName = GetRoomNameLatestSeg(currentEffectiveRoomName, -1);
            displayPreviousSegmentRoomName = GetShortenedDisplayRoomName(displayPreviousSegmentRoomName, false);
            menu.Add(new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmFuseRoom")} {displayPreviousSegmentRoomName}").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
                // Segment current room
                Audio.Play("event:/ui/main/button_select");
                RemoveLatestRoomSeg(level, currentEffectiveRoomName);
                exitMenuCommands();
            }));
        }
        else if (roomFuseWith == null)
        {
            // (non-segmented) First room: Cannot Fuse
            TextMenu.Item button = new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmFuseRoom")} -------").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
            });
            button.Disabled = true;
            menu.Add(button);
        }
        else
        {
            // Normal Fuse
            menu.Add(new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_ConfirmFuseRoom")} {displayRoomFuseWith}").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
                // Segment current room
                Audio.Play("event:/ui/main/button_select");
                CreateRoomFuse(level, currentEffectiveRoomName, roomFuseWith);
                exitMenuCommands();
            }));
        }

        menu.Add(new TextMenu.Button(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomCancel")).Pressed(delegate
        {
            Audio.Play("event:/ui/main/button_back");
            exitMenuCommands();
        }));

        menu.OnPause = (menu.OnESC = [MethodImpl(MethodImplOptions.NoInlining)] () =>
        {
            Audio.Play("event:/ui/main/button_back");
            exitMenuCommands();
        });
        menu.OnCancel = [MethodImpl(MethodImplOptions.NoInlining)] () =>
        {
            Audio.Play("event:/ui/main/button_back");
            exitMenuCommands();
        };
        level.Add(menu);

        void exitMenuCommands()
        {
            inCreateSegmentRoomMenu = false;
            menu.RemoveSelf();

            Utils_General.ConsumeInput(Input.ESC, 3);
            Utils_General.ConsumeInput(Input.Pause, 3);
            Utils_General.ConsumeInput(Input.MenuConfirm, 3);
            Utils_General.ConsumeInput(Input.MenuCancel, 3);
        }
    }

    private void RenderOtherStuffCompletelyUnrelatedToRoomStatsButAddedHereDueToConvenience(Level level)
    {
        // Toggle-ify
        if (EndHelperModule.Session.toggleifyEnabled && !(EndHelperModule.Settings.ToggleGrabMenu.HideWhenPause && level.Paused))
        {
            MTexture toggleifyIcon = GFX.Gui["misc/EndHelper/ToggleGrabkeyIcon"];
            int displayXPos = 15 + EndHelperModule.Settings.ToggleGrabMenu.GrabOffsetX * 8;
            int displayYPos = 15 + EndHelperModule.Settings.ToggleGrabMenu.GrabOffsetY * 8;
            float displayScale = (float)EndHelperModule.Settings.ToggleGrabMenu.GrabSize / 10;
            toggleifyIcon.Draw(new Vector2(displayXPos, displayYPos), Vector2.Zero, Color.White, displayScale);
        }

        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"tooltipDuration {tooltipDuration} alpha {alpha} tooltiptext {tooltipText}");
        if (tooltipDuration > -60)
        {
            ActiveFont.DrawOutline(tooltipText, new Vector2(100, 950), Vector2.Zero, Vector2.One, Color.White * alpha, 2, Color.Black * alpha);
            tooltipDuration--;
        }
        if (tooltipDuration > 10 && alpha < 1) { alpha += 0.1f; }
        if (tooltipDuration < 0 && alpha > 0) { alpha -= 0.03f; }
    }

    public static string tooltipText = "";
    public static int tooltipDuration = 0;
    public static float alpha = 0;

    public static void ShowTooltip(String message, float durationSeconds)
    {
        tooltipText = message;
        tooltipDuration = (int)durationSeconds * 60;
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"Show tooltip {message} for {durationSeconds} seconds.");
    }

    #endregion
}