﻿using System;
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
using Celeste.Mod.SpeedrunTool.Message;
using static Celeste.Mod.UI.CriticalErrorHandler;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.RoomStatDisplaySubMenu;
using NETCoreifier;
using System.Net.NetworkInformation;
using FMOD.Studio;
using static Celeste.Tentacles;


namespace Celeste.Mod.EndHelper.Entities.Misc;

[Tracked(true)]
[CustomEntity("EndHelper/RoomStatisticsDisplayer")]
public class RoomStatisticsDisplayer : Entity
{
    private string clipboardText = "";
    public string currentRoomName = "";
    public bool statisticsGuiOpen = false;
    public bool disableRoomChange = false;
    public string mapNameSide = "";
    public Color mapNameColor;
    private enum roomStatMenuFilter { None, Death0, Death10, Time60s, Renamed }
    private roomStatMenuFilter filterSetting = roomStatMenuFilter.None;

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

        // Get map name header text
        mapNameSide = session.Area.GetSID();
        if (mapNameSide.StartsWith("Celeste/"))
        {
            int mapID = session.Area.ID;
            mapNameSide = $"AREA_{mapID}";
        }

        mapNameColor = Color.Lime; // No else so if multiple appears for some reason it picks the hardest
        if (mapNameSide.ToLower().Contains("beginner")) { mapNameColor = Color.Aqua; }
        if (mapNameSide.ToLower().Contains("intermediate")) { mapNameColor = Color.PaleVioletRed; }
        if (mapNameSide.ToLower().Contains("advanced")) { mapNameColor = Color.Yellow; }
        if (mapNameSide.ToLower().Contains("expert")) { mapNameColor = Color.Orange; }
        if (mapNameSide.ToLower().Contains("grandmaster")) { mapNameColor = Color.Magenta; }

        mapNameSide = mapNameSide.DialogCleanOrNull(Dialog.Languages["english"]) ?? mapNameSide;

        AreaMode side = session.Area.Mode;
        if (side == AreaMode.BSide)
        {
            mapNameSide += " B";
        }
        else if (side == AreaMode.CSide)
        {
            mapNameSide += " C";
        }
    }

    public void ImportRoomStatInfo()
    {
        EndHelperModule.Session.roomStatDict_customName = EndHelperModule.externalRoomStatDict_customName; // Import
        EndHelperModule.Session.roomStatDict_death = EndHelperModule.externalRoomStatDict_death;
        EndHelperModule.Session.roomStatDict_timer = EndHelperModule.externalRoomStatDict_timer;
        EndHelperModule.Session.roomStatDict_colorIndex = EndHelperModule.externalRoomStatDict_colorIndex;
        EndHelperModule.Session.pauseTypeDict = EndHelperModule.externalDict_pauseTypeDict;

        // Only keep for debug. Load state also un-collects the berry so let the berry count be loaded.
        if (EndHelperModule.lastSessionResetCause == SessionResetCause.Debug)
        {
            EndHelperModule.Session.roomStatDict_strawberries = EndHelperModule.externalRoomStatDict_strawberries;
        }
    }

    public override void Update()
    {// Keep these updated!
        Level level = SceneAs<Level>();
        if (!disableRoomChange)
        {
            currentRoomName = getRoomNameLatestSeg(level.Session.LevelData.Name);
        }

        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"straw count {level.Session.Strawberries.Count} {level.Session.MapData.DetectedStrawberries}");
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"session reset time {EndHelperModule.timeSinceSessionReset}");

        // If something wacky happens (aka if debug mode is used), grab data from there. Otherwise export data there.
        if (EndHelperModule.timeSinceSessionReset == 1)
        {
            ImportRoomStatInfo();
        }
        else if (EndHelperModule.timeSinceSessionReset > 1)
        {
            EndHelperModule.externalRoomStatDict_customName = EndHelperModule.Session.roomStatDict_customName; // Export
            EndHelperModule.externalRoomStatDict_death = EndHelperModule.Session.roomStatDict_death;
            EndHelperModule.externalRoomStatDict_timer = EndHelperModule.Session.roomStatDict_timer;
            EndHelperModule.externalRoomStatDict_strawberries = EndHelperModule.Session.roomStatDict_strawberries;
            EndHelperModule.externalRoomStatDict_colorIndex = EndHelperModule.Session.roomStatDict_colorIndex;
            EndHelperModule.externalDict_pauseTypeDict = EndHelperModule.Session.pauseTypeDict;

            // Export custom names to SaveData
            if (EndHelperModule.Settings.RoomStatMenu.MenuCustomNameStorageCount > 0)
            {
                String roomName = level.Session.Level;
                String mapNameSide = level.Session.Area.GetSID();
                if (level.Session.Area.Mode == AreaMode.BSide) { mapNameSide += "_BSide"; }
                else if (level.Session.Area.Mode == AreaMode.CSide) { mapNameSide += "_CSide"; }
                EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide] = EndHelperModule.Session.roomStatDict_customName;
            }
        }

        // Counters for people to use I guess
        // OrderedDict do not handle types well, save & quit converts them into strings for some reason, hence the really dumb Convert.ToInts
        int timeSpentInSeconds = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentRoomName])).Seconds;

        String counterFriendlyRoomName = currentRoomName;
        counterFriendlyRoomName = counterFriendlyRoomName.Replace("%", "_"); // %s in %segment causes issues lol

        level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_death", Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]));
        level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_timer", timeSpentInSeconds);
        level.Session.SetCounter($"EndHelper_RoomStatistics_{counterFriendlyRoomName}_strawberries", Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentRoomName]));

        // Show/Hide GUI
        if (EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed && !statisticsGuiOpen && !level.Paused && !level.Transitioning)
        {
            statisticsGuiOpen = true;
            Depth = -9000;
            level.Paused = true;
            Audio.Play("event:/ui/game/pause");
        }
        else if (statisticsGuiOpen && !roomNameEditMenuOpen && (!level.Paused || Input.ESC.Pressed || Input.MenuCancel.Pressed || Input.Pause
            || level.Transitioning || EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed))
        {
            statisticsGuiOpen = false;
            level.Paused = false;
            EndHelperModule.afkDurationFrames = 0;

            // Directly consuming doesn't do it for long enough
            consumeInput(Input.Jump, 2);
            consumeInput(Input.Dash, 2);
            consumeInput(Input.CrouchDash, 2);
            consumeInput(Input.Grab, 2);
            consumeInput(Input.ESC, 3);
            consumeInput(Input.Pause, 3);
            Audio.Play("event:/ui/game/unpause");

            if (Input.MenuCancel.Pressed)
            {
                string clipboardToolTipMsg = Dialog.Get("EndHelper_Dialog_RoomStatisticsDisplayer_CopiedToClipboard");
                Tooltip.Show(clipboardToolTipMsg, 2f);
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
            EndHelperModule.mInputDisableDuration = 3;
        }

        base.Update();
        // MInput.Disabled = false;
    }

    void MenuCloseNameEditor()
    {
        //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"MenuCloseNameEditor: close text menu");
        TextInput.OnInput -= OnTextInput;

        consumeInput(Input.ESC, 3);
        consumeInput(Input.Pause, 3);
        consumeInput(Input.MenuCancel, 3);
        consumeInput(Input.MenuConfirm, 3);
        roomNameEditMenuOpen = false;

        EndHelperModule.afkDurationFrames = 0;
        Audio.Play("event:/ui/main/rename_entry_accept");
    }

    public override void Render()
    {
        Level level = SceneAs<Level>();
        ensureDictsHaveKey(level);

        if (statisticsGuiOpen)
        {
            statisticGUI();
        }

        // Text Display
        int displayXPos = 15 + EndHelperModule.Settings.RoomStatDisplayMenu.OffsetX * 8;
        int displayYPos = 15 + EndHelperModule.Settings.RoomStatDisplayMenu.OffsetY * 8;
        float displayScale = (float)EndHelperModule.Settings.RoomStatDisplayMenu.Size / 20;

        int deathNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]);
        long timerNum = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentRoomName]);
        int strawberriesNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentRoomName]);

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

        if (!statisticsGuiOpen)
        {
            showStats(displayXPos, displayYPos, displayScale, timerColor, false, xJustification, false, false, false, $"", $"", deathNum, timerNum, strawberriesNum);
        }

        RenderOtherStuffCompletelyUnrelatedToRoomStatsButAddedHereDueToConvenience(level);

        base.Render();
    }

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

    void showStats(int displayXPos, int displayYPos, float displayScale, Color timerColor, bool yCentered, float xJustification, bool showAll, bool hideRoomName, bool showTotalMapBerryCount, string prefix, string suffix, int deathNum, long timerNum, int strawberriesNum)
    {
        var roomDisplaySettings = EndHelperModule.Settings.RoomStatDisplayMenu;

        Level level = SceneAs<Level>();
        Vector2 justification = new Vector2(0, yCentered ? 0.5f : 0f);
        List<DisplayInfo> displayInfoList = [];

        if (prefix != "")
        {
            displayInfoList.Add(new DisplayInfo("prefix", prefix, (int)(ActiveFont.WidthToNextLine($"{prefix}", 0) * displayScale)));
            //ActiveFont.DrawOutline(prefix, new Vector2(sectionXPos + xOffset, displayYPos), justification, Vector2.One * displayScale, timerColor, 2f, Color.Black);
        }

        if (!hideRoomName && (roomDisplaySettings.ShowRoomName || showAll))
        {
            string displayMsg = "";
            string customRoomName = Convert.ToString(EndHelperModule.Session.roomStatDict_customName[currentRoomName]);

            if (customRoomName.Length > 35)
            {
                displayMsg += $"{customRoomName.Substring(0, 33)}...";
            }
            else
            {
                displayMsg += customRoomName;
            }

            if (roomDisplaySettings.ShowDeaths || roomDisplaySettings.ShowTimeSpent 
                || (roomDisplaySettings.ShowStrawberries && strawberriesNum > 0))
            {
                displayMsg += ":";
            }

            displayInfoList.Add(new DisplayInfo("roomname", displayMsg, (int)(ActiveFont.WidthToNextLine($"{displayMsg} ", 0) * displayScale)));
        }

        if (showAll || roomDisplaySettings.ShowDeaths)
        {
            string displayMsg = $":EndHelper/uioutline_skull: {deathNum}";
            displayInfoList.Add(new DisplayInfo("deaths", displayMsg, (int)(ActiveFont.WidthToNextLine($"{deathNum}XXX|", 0) * displayScale)));
        }
        if (showAll || roomDisplaySettings.ShowTimeSpent)
        {
            TimeSpan timeSpent = TimeSpan.FromTicks(timerNum);
            string timeString = EndHelperModule.MinimalGameplayFormat(timeSpent);
            string displayMsg = $":EndHelper/uioutline_clock: {timeString}";
            if (timerColor == Color.Gray)
            {
                displayMsg = $":EndHelper/uioutline_clock_gray: {timeString}";
            }

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
        if (showAll || roomDisplaySettings.ShowStrawberries)
        {
            int mapBerryCount = level.Session.MapData.DetectedStrawberries;
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
            ActiveFont.DrawOutline(displayInfo.displayMsg, new Vector2(sectionXPos + xOffset, displayYPos), justification, Vector2.One * displayScale, color, 2f, Color.Black);
            sectionXPos += displayInfo.textWidth;
        }
    }

    public void OnDeath()
    {
        Level level = SceneAs<Level>();
        ensureDictsHaveKey(level); // Check if dict has current room, just in case.
        EndHelperModule.Session.roomStatDict_death[currentRoomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]) + 1;
    }

    private int firstRowShown = 0;
    int editingRoomIndex = -1;
    string editingRoomName = null;
    void statisticGUI()
    {
        Level level = SceneAs<Level>();
        Session session = level.Session;

        MTexture backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background"];
        MTexture backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short"];
        MTexture pageArrow = GFX.Gui["dotarrow_outline"];
        if (!EndHelperModule.Settings.RoomStatMenu.MenuMulticolor)
        {
            backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background_purple"];
            backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short_purple"];
        }
        MTexture backgroundTextureEdit = GFX.Gui["misc/EndHelper/statGUI_background_edit"];

        MTexture upKey = GFX.Gui["controls/keyboard/up"];
        MTexture downKey = GFX.Gui["controls/keyboard/down"];
        MTexture leftKey = GFX.Gui["controls/keyboard/left"];
        MTexture rightKey = GFX.Gui["controls/keyboard/right"];
        //MTexture deleteKey = GFX.Gui["controls/keyboard/delete"];

        const int roomsPerColumn = 16;
        int lastRowShown = firstRowShown + 2*roomsPerColumn;

        int currentItemIndex = 0;

        int startX = 550;
        int startX_first = 100;
        const int col2Buffer = 900;

        // Create a list of all room names to show
        List<string> roomNamesToShowList = new List<string>();
        foreach (string roomName in new ArrayList(EndHelperModule.Session.roomStatDict_death.Keys))
        {
            if (roomName == "")
            {
                removeRoomData("");
                continue;
            }

            int roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
            TimeSpan roomTimeSpan = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]));
            string roomTimeString = EndHelperModule.MinimalGameplayFormat(roomTimeSpan);
            int roomStrawberriesCollected = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);

            // Filtering
            switch (filterSetting)
            {
                case roomStatMenuFilter.Death0:
                    if (roomDeaths <= 0) { continue; }
                    break;

                case roomStatMenuFilter.Death10:
                    if (roomDeaths <= 10) { continue; }
                    break;

                case roomStatMenuFilter.Time60s:
                    if (roomTimeSpan.TotalSeconds <= 60) { continue; }
                    break;

                case roomStatMenuFilter.Renamed:
                    String defaultName = roomName;
                    String mapNameSideDialog = session.Area.GetSID();
                    if (session.Area.Mode == AreaMode.BSide) { mapNameSideDialog += "_B"; }
                    else if (session.Area.Mode == AreaMode.CSide) { mapNameSideDialog += "_C"; }
                    defaultName = $"{mapNameSideDialog}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;

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

        int dictSize = roomNamesToShowList.Count;

        if (dictSize - firstRowShown > roomsPerColumn)
        {
            startX = startX_first;
        }

        const int startY = 100;
        const int heightBetweenRows = 55;

        const int width_death = 140;
        int startX_death = startX + 532;

        const int width_timer = 140;
        int startX_timer = startX_death + width_timer + 10;

        int startX_strawberry = startX_timer + width_timer + 20;

        const int bufferX = 10;

        // Map Name Header
        ActiveFont.DrawOutline($"{mapNameSide}", new Vector2(980, 5), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), mapNameColor, 2f, Color.Black);


        // The table headers (aka just death and timer icons)
        if (!inCreateSegmentRoomMenu)
        {
            ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            if (dictSize - firstRowShown > roomsPerColumn)
            {
                ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + col2Buffer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + col2Buffer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            }
        }

        // The table
        int totalDeaths = 0;
        long totalTimer = 0;
        int totalStrawberries = 0;
        clipboardText = $"Room\tDeaths\tTime\tBerries";

        if (!inCreateSegmentRoomMenu)
        {
            foreach (string roomName in roomNamesToShowList)
            {
                // Just in case. This sometimes errors out when rebuilding, but as a safeguard in case it occurs elsewhere im gonna ensure the key exist.
                ensureDictsHaveKey(level, roomName);

                int roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                TimeSpan roomTimeSpan = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]));
                string roomTimeString = EndHelperModule.MinimalGameplayFormat(roomTimeSpan);
                int roomStrawberriesCollected = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);

                if (!roomNameEditMenuOpen && currentRoomName == roomName) // if closed, set editingRoomIndex to current room
                {
                    editingRoomIndex = currentItemIndex;
                    editingRoomName = roomName;
                }

                totalDeaths += Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
                totalTimer += Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]);
                totalStrawberries += Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);

                string customRoomName = Convert.ToString(EndHelperModule.Session.roomStatDict_customName[roomName]);

                if ((customRoomName.Trim().Length == 0 || EndHelperModule.Session.roomStatDict_customName[roomName] is null) && !roomNameEditMenuOpen)
                {
                    String mapNameSideDialog = session.Area.GetSID();
                    if (session.Area.Mode == AreaMode.BSide) { mapNameSideDialog += "_B"; }
                    else if (session.Area.Mode == AreaMode.CSide) { mapNameSideDialog += "_C"; }
                    EndHelperModule.Session.roomStatDict_customName[roomName] = $"{mapNameSideDialog}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;
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
                        int colorIndex = Convert.ToInt32(EndHelperModule.Session.roomStatDict_colorIndex[roomName]);
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
                    }
                    else
                    {
                        backgroundTexture.Draw(new Vector2(startX + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                    }
                    ActiveFont.DrawOutline(shortenedRoomName, new Vector2(startX + bufferX + col2BufferCurrent, startY + heightBetweenRows * displayRow + (heightBetweenRows - 5) / 2), new Vector2(0f, 0.5f), textScale, Color.White, 2f, Color.Black);

                    backgroundTextureShort.Draw(new Vector2(startX_death + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                    ActiveFont.DrawOutline($"{roomDeaths}", new Vector2(startX_death + col2BufferCurrent + width_death / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                    backgroundTextureShort.Draw(new Vector2(startX_timer + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);

                    ActiveFont.DrawOutline(roomTimeString, new Vector2(startX_timer + col2BufferCurrent + width_timer / 2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                    if (roomStrawberriesCollected > 0)
                    {
                        ActiveFont.DrawOutline($":EndHelper/uioutline_strawberry:", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows / 2 - 3), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                    }
                    if (roomStrawberriesCollected > 1)
                    {
                        ActiveFont.DrawOutline($"{roomStrawberriesCollected}", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows / 2), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.White, 2f, Color.Black);
                    }
                }
                if (roomStrawberriesCollected == 0)
                {
                    clipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t";
                }
                else
                {
                    clipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t{roomStrawberriesCollected}";
                }
                currentItemIndex++;
            }

            // Total Stats
            bool showTotalMapBerryCount = true;
            if (!EndHelperModule.Settings.RoomStatMenu.MenuSpoilBerries && !level.Completed)
            {
                showTotalMapBerryCount = false; // No berry count spoilery
            }

            String totalText = "Total";
            if (filterSetting != roomStatMenuFilter.None) { totalText += $" [{filterString}]"; }
            showStats(100, 1010, 0.7f, Color.White, true, 0, true, true, showTotalMapBerryCount, $"{totalText}: ", "", totalDeaths, totalTimer, totalStrawberries);
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

            ActiveFont.DrawOutline($"Segment Room:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
            instructionXPos += (int)(ActiveFont.WidthToNextLine($"Segment Room: XI", 0) * instructionScale);
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

            string totalTimeString = EndHelperModule.MinimalGameplayFormat(TimeSpan.FromTicks(totalTimer));
            clipboardText += $"\r\n{"Total"}\t{totalDeaths}\t{totalTimeString}\t{totalStrawberries}";


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

            // Scroll Down
            MInput.Disabled = false;
            if (firstRowShown < dictSize - roomsPerColumn * 2 && (Input.MenuDown.Pressed || Input.MenuRight.Pressed) && !roomNameEditMenuOpen)
            {
                firstRowShown += roomsPerColumn * 2;
            }
            if (firstRowShown < 0) { firstRowShown = 0; }
            // Scroll Up
            if (firstRowShown > 0 && (Input.MenuUp.Pressed || Input.MenuLeft.Pressed) && !roomNameEditMenuOpen)
            {
                firstRowShown -= roomsPerColumn * 2;
            }
            while (firstRowShown >= dictSize && dictSize > 0) { firstRowShown -= roomsPerColumn * 2; }

            // Ensure room that is being renamed is currently viewble
            if (roomNameEditMenuOpen)
            {
                // Allow changing rooms
                if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Up) || MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.PageUp)) { editingRoomIndex--; Audio.Play("event:/ui/main/savefile_rollover_up"); }
                if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Down) || MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.PageDown)) { editingRoomIndex++; Audio.Play("event:/ui/main/savefile_rollover_down"); }
                if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Right)) { editingRoomIndex += roomsPerColumn; Audio.Play("event:/ui/main/savefile_rollover_down"); }
                if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Left)) { editingRoomIndex -= roomsPerColumn; Audio.Play("event:/ui/main/savefile_rollover_up"); }
                if (editingRoomIndex < 0) { editingRoomIndex = 0; }
                if (editingRoomIndex > dictSize - 1) { editingRoomIndex = dictSize - 1; }

                int editRoomPage = (int)Math.Ceiling((editingRoomIndex + 1f) / (roomsPerColumn * 2));
                if (currentPage > editRoomPage)
                {
                    firstRowShown -= roomsPerColumn * 2;
                }
                else if (currentPage < editRoomPage)
                {
                    firstRowShown += roomsPerColumn * 2;
                }
            }
        }

        // Room Stat Menu Buttons Functionality
        if (!roomNameEditMenuOpen && !inCreateSegmentRoomMenu)
        {
            if (Input.QuickRestart.Pressed)
            {
                // Renaming current room
                consumeInput(Input.QuickRestart, 3);

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
                createSegmentRoomMenu(level, currentRoomName);
            }
        }
        MInput.Disabled = true;
    }

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

    public void ensureDictsHaveKey(Level level, String roomName = null)
    {
        if (roomName == null) { roomName = currentRoomName; }

        // Custom Names are seperated as additional extra keys can be stored
        // Strawberries are seperated as they are undone during load state unlike the rest.
        if (!EndHelperModule.Session.roomStatDict_customName.ContainsKey(roomName))
        {
            String mapNameSideDialog = level.Session.Area.GetSID();
            if (level.Session.Area.Mode == AreaMode.BSide) { mapNameSideDialog += "_B"; }
            else if (level.Session.Area.Mode == AreaMode.CSide) { mapNameSideDialog += "_C"; }
            EndHelperModule.Session.roomStatDict_customName[roomName] = $"{mapNameSideDialog}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;
        }
        if (!EndHelperModule.Session.roomStatDict_death.Contains(roomName) || !EndHelperModule.Session.roomStatDict_timer.Contains(roomName) || !EndHelperModule.Session.roomStatDict_colorIndex.Contains(roomName))
        {
            EndHelperModule.Session.roomStatDict_death[roomName] = (int)0;
            EndHelperModule.Session.roomStatDict_timer[roomName] = (long)0;
            EndHelperModule.Session.roomStatDict_colorIndex[roomName] = (int)level.Session.LevelData.EditorColorIndex;
        }
        if (!EndHelperModule.Session.roomStatDict_strawberries.Contains(roomName))
        {
            EndHelperModule.Session.roomStatDict_strawberries[roomName] = (int)0;
        }
    }

    private bool checkIfDictHaveRoomName(String roomName)
    {
        if (EndHelperModule.Session.roomStatDict_death.Contains(roomName)) { return true; } 
        else { return false; }
    }

    // Unused
    //private void cloneRoomData(Level level, String fromRoomName, String toRoomName, String toRoomNameCustomSuffix = "", bool resetFromRoom = true, bool doNotResetCustomName = true)
    //{

    //    ensureDictsHaveKey(level, toRoomName);

    //    EndHelperModule.Session.roomStatDict_customName[toRoomName] = $"{EndHelperModule.Session.roomStatDict_customName[fromRoomName]}{toRoomNameCustomSuffix}";
    //    EndHelperModule.Session.roomStatDict_death[toRoomName] = EndHelperModule.Session.roomStatDict_death[fromRoomName];
    //    EndHelperModule.Session.roomStatDict_timer[toRoomName] = EndHelperModule.Session.roomStatDict_timer[fromRoomName];
    //    EndHelperModule.Session.roomStatDict_strawberries[toRoomName] = EndHelperModule.Session.roomStatDict_strawberries[fromRoomName];
    //    EndHelperModule.Session.roomStatDict_colorIndex[toRoomName] = EndHelperModule.Session.roomStatDict_colorIndex[fromRoomName];

    //    if (resetFromRoom) { removeRoomData(fromRoomName, false); }
    //}

    private void createRoomSeg(Level level, String roomName)
    {
        roomName = getRoomNameNoSeg(roomName);
        int nextRoomSeg = getRoomSegCount(roomName) + 1;
        ensureDictsHaveKey(level, $"{roomName}%seg{nextRoomSeg}");
    }

    private int getRoomSegCount(String roomName)
    {
        roomName = getRoomNameNoSeg(roomName);

        for (int segNum = 2; true; segNum++)
        {
            // Find next available segment number
            if (!checkIfDictHaveRoomName($"{roomName}%seg{segNum}"))
            {
                return segNum - 1; // Subtract to get final used number
            }
        }
    }

    public string getRoomNameLatestSeg(String roomName)
    {
        roomName = getRoomNameNoSeg(roomName);
        int latestSeg = getRoomSegCount(roomName);

        if (latestSeg <= 1)
        {
            return roomName;
        } 
        else
        {
            return $"{roomName}%seg{latestSeg}";
        }
    }

    private string getRoomNameNoSeg(String roomName)
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

    private void removeRoomData(String roomName, bool resetCustomName = true)
    {
        if (resetCustomName)
        {
            EndHelperModule.Session.roomStatDict_customName.Remove(roomName);
        }
        EndHelperModule.Session.roomStatDict_death.Remove(roomName);
        EndHelperModule.Session.roomStatDict_timer.Remove(roomName);
        EndHelperModule.Session.roomStatDict_strawberries.Remove(roomName);
        EndHelperModule.Session.roomStatDict_colorIndex.Remove(roomName);
    }

    private bool inCreateSegmentRoomMenu = false;
    private void createSegmentRoomMenu(Level level, String currentRoomName)
    {
        String displayCurrentRoomName = EndHelperModule.Session.roomStatDict_customName[currentRoomName];
        if (displayCurrentRoomName.Length > 30)
        {
            displayCurrentRoomName = $"{displayCurrentRoomName.Substring(0, 30)}...";
        }

        inCreateSegmentRoomMenu = true;
        TextMenu menu = new TextMenu();
        menu.AutoScroll = false;
        menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
        menu.Add(new TextMenu.Header(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomHeader")));
        menu.Add(new TextMenu.SubHeader(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomSubHeader")));
        menu.Add(new TextMenu.SubHeader(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomSubHeader2")));
        menu.Add(new TextMenu.Button($"{Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_SegmentRoomConfirm")} {displayCurrentRoomName}").Pressed([MethodImpl(MethodImplOptions.NoInlining)] () =>
        {
            // Segment current room
            Audio.Play("event:/ui/main/button_select");
            createRoomSeg(level, currentRoomName);
            exitMenuCommands();
        }));
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

            consumeInput(Input.ESC, 3);
            consumeInput(Input.Pause, 3);
            consumeInput(Input.MenuConfirm, 3);
            consumeInput(Input.MenuCancel, 3);
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
    }

}