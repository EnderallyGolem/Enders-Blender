using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using Celeste.Mod.Entities;
using static On.Celeste.Level;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings;
using static Celeste.Mod.EndHelper.EndHelperModule;
using Celeste.Mod.SpeedrunTool.Message;
using static Celeste.Mod.UI.CriticalErrorHandler;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.StatDisplaySubMenu;
using NETCoreifier;


namespace Celeste.Mod.EndHelper.Entities.Misc;

[Tracked(true)]
[CustomEntity("EndHelper/RoomStatisticsDisplayer")]
public class RoomStatisticsDisplayer : Entity
{
    private string clipboardText = "";
    public string currentRoomName = "";
    private int afkDurationFrames = 0;
    private bool allowIncrementTimer = true;
    private bool statisticsGuiOpen = false;
    public bool disableRoomChange = false;

    public RoomStatisticsDisplayer(Level level)
    {
        Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;

        Depth = -101;
    }
    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    public void ImportRoomStatInfo()
    {
        EndHelperModule.Session.roomStatDict_death = EndHelperModule.externalRoomStatDict_death; // Import
        EndHelperModule.Session.roomStatDict_timer = EndHelperModule.externalRoomStatDict_timer;
        EndHelperModule.Session.roomStatDict_colorIndex = EndHelperModule.externalRoomStatDict_colorIndex;

        // Only keep for debug. Load state also un-collects the berry so let the berry count be loaded.
        if (EndHelperModule.lastSessionResetCause == SessionResetCause.Debug)
        {
            EndHelperModule.Session.roomStatDict_strawberries = EndHelperModule.externalRoomStatDict_strawberries;
        }
    }

    public override void Update()
    {
        // Keep these updated!
        Level level = SceneAs<Level>();
        if (!disableRoomChange)
        {
            currentRoomName = level.Session.LevelData.Name;
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
            EndHelperModule.externalRoomStatDict_death = EndHelperModule.Session.roomStatDict_death; // Export
            EndHelperModule.externalRoomStatDict_timer = EndHelperModule.Session.roomStatDict_timer;
            EndHelperModule.externalRoomStatDict_strawberries = EndHelperModule.Session.roomStatDict_strawberries;
            EndHelperModule.externalRoomStatDict_colorIndex = EndHelperModule.Session.roomStatDict_colorIndex;
        }

        //Increment time spent in room
        allowIncrementTimer = true;

        if (level.FrozenOrPaused && (
            EndHelperModule.Settings.StatDisplay.PauseOption == StatDisplaySubMenu.PauseScenarioEnum.Pause ||
            EndHelperModule.Settings.StatDisplay.PauseOption == StatDisplaySubMenu.PauseScenarioEnum.Both
        ))
        {
            allowIncrementTimer = false;
        }

        if (afkDurationFrames > 1800 && (
            EndHelperModule.Settings.StatDisplay.PauseOption == StatDisplaySubMenu.PauseScenarioEnum.AFK ||
            EndHelperModule.Settings.StatDisplay.PauseOption == StatDisplaySubMenu.PauseScenarioEnum.Both
        ))
        {
            allowIncrementTimer = false;
        }

        if (!level.TimerStarted || level.TimerStopped || level.Completed)
        {
            allowIncrementTimer = false;
        }

        // Ensure key. Strawberries are seperated as they are undone during load state unlike the rest.
        if (!EndHelperModule.Session.roomStatDict_death.Contains(currentRoomName) || !EndHelperModule.Session.roomStatDict_timer.Contains(currentRoomName) || !EndHelperModule.Session.roomStatDict_colorIndex.Contains(currentRoomName))
        {
            EndHelperModule.Session.roomStatDict_death[currentRoomName] = (int)0;
            EndHelperModule.Session.roomStatDict_timer[currentRoomName] = (long)0;
            EndHelperModule.Session.roomStatDict_colorIndex[currentRoomName] = (int)level.Session.LevelData.EditorColorIndex;
        }
        if (!EndHelperModule.Session.roomStatDict_strawberries.Contains(currentRoomName))
        {
            EndHelperModule.Session.roomStatDict_strawberries[currentRoomName] = (int)0;
        }

        if (allowIncrementTimer)
        {
            // OrderedDict do not handle types well, save & quit converts them into strings for some reason, hence the really dumb Convert.ToInts
            EndHelperModule.Session.roomStatDict_timer[currentRoomName] = TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime).Ticks + Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentRoomName]);
        }

        //AFK Checker
        if (Input.Aim == Vector2.Zero && Input.Dash.Pressed == false && Input.Grab.Pressed == false && Input.CrouchDash.Pressed == false && Input.Talk.Pressed == false 
            && Input.MenuCancel.Pressed == false && Input.MenuConfirm.Pressed == false && EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed == false)
        {
            afkDurationFrames++;
        } else
        {
            afkDurationFrames = 0;
        }

        // Counters for people to use I guess
        int timeSpentInSeconds = TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentRoomName])).Seconds;
        level.Session.SetCounter($"EndHelper_RoomStatistics_{currentRoomName}_death", Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]));
        level.Session.SetCounter($"EndHelper_RoomStatistics_{currentRoomName}_timer", timeSpentInSeconds);
        level.Session.SetCounter($"EndHelper_RoomStatistics_{currentRoomName}_strawberries", Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentRoomName]));


        // Show/Hide GUI
        if (EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed && !statisticsGuiOpen && !level.Paused)
        {
            statisticsGuiOpen = true;
            Depth = -9000;
            level.Paused = true;
        }
        else if (statisticsGuiOpen && (!level.Paused || Input.ESC.Pressed || Input.MenuCancel.Pressed || Input.MenuConfirm.Pressed
            || level.Transitioning || EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed))
        {
            statisticsGuiOpen = false;
            level.Paused = false;

            if (Input.MenuConfirm.Pressed)
            {
                string clipboardToolTipMsg = Dialog.Get("EndHelper_Dialog_RoomStatisticsDisplayer_CopiedToClipboard");
                Tooltip.Show(clipboardToolTipMsg, 2f);
                TextInput.SetClipboardText(clipboardText);
            }
        }

        base.Update();
    }

    public override void Render()
    {
        if (statisticsGuiOpen)
        {
            statisticGUI();
        }


        // Text Display

        int displayXPos = 15 + EndHelperModule.Settings.StatDisplay.OffsetX * 8;
        int displayYPos = 15 + EndHelperModule.Settings.StatDisplay.OffsetY * 8;
        float displayScale = (float)EndHelperModule.Settings.StatDisplay.Size / 20;

        int deathNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]);
        long timerNum = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[currentRoomName]);
        int strawberriesNum = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[currentRoomName]);

        Color timerColor = allowIncrementTimer ? Color.White : Color.Gray;
        float xJustification = 0;
        if (EndHelperModule.Settings.StatDisplay.xJustification == StatDisplaySubMenu.Justification.Center)
        {
            xJustification = 0.5f;
        } 
        else if (EndHelperModule.Settings.StatDisplay.xJustification == StatDisplaySubMenu.Justification.Right)
        {
            xJustification = 1f;
        }

        if (!statisticsGuiOpen)
        {
            showStats(displayXPos, displayYPos, displayScale, timerColor, 255, false, xJustification, false, false, false, "", "", deathNum, timerNum, strawberriesNum);
        }
        

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

    void showStats(int displayXPos, int displayYPos, float displayScale, Color timerColor, int alpha, bool yCentered, float xJustification, bool showAll, bool hideRoomName, bool showTotalMapBerryCount, string prefix, string suffix, int deathNum, long timerNum, int strawberriesNum)
    {
        Level level = SceneAs<Level>();
        Vector2 justification = new Vector2(0, yCentered ? 0.5f : 0f);
        List<DisplayInfo> displayInfoList = [];

        if (prefix != "")
        {
            displayInfoList.Add(new DisplayInfo("prefix", prefix, (int)(ActiveFont.WidthToNextLine($"{prefix}", 0) * displayScale)));
            //ActiveFont.DrawOutline(prefix, new Vector2(sectionXPos + xOffset, displayYPos), justification, Vector2.One * displayScale, timerColor, 2f, Color.Black);
        }

        if (!hideRoomName && (EndHelperModule.Settings.StatDisplay.ShowRoomName || showAll))
        {
            string displayMsg = "";
            string shortenedRoomName = currentRoomName;
            if (currentRoomName.Length > 35)
            {
                displayMsg += $"{currentRoomName.Substring(0, 33)}...";
            }
            else
            {
                displayMsg += currentRoomName;
            }

            if ( EndHelperModule.Settings.StatDisplay.ShowDeaths || EndHelperModule.Settings.StatDisplay.ShowTimeSpent 
                || (EndHelperModule.Settings.StatDisplay.ShowStrawberries && strawberriesNum > 0))
            {
                displayMsg += ":";
            }

            displayInfoList.Add(new DisplayInfo("roomname", displayMsg, (int)(ActiveFont.WidthToNextLine($"{displayMsg} ", 0) * displayScale)));
        }

        if (showAll || EndHelperModule.Settings.StatDisplay.ShowDeaths)
        {
            string displayMsg = $":EndHelper/uioutline_skull: {deathNum}";
            displayInfoList.Add(new DisplayInfo("deaths", displayMsg, (int)(ActiveFont.WidthToNextLine($"{deathNum}XXX|", 0) * displayScale)));
        }
        if (showAll || EndHelperModule.Settings.StatDisplay.ShowTimeSpent)
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
        if (showAll || EndHelperModule.Settings.StatDisplay.ShowStrawberries)
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
        // Check if dict has current room, just in case.
        if (!EndHelperModule.Session.roomStatDict_death.Contains(currentRoomName) || !EndHelperModule.Session.roomStatDict_timer.Contains(currentRoomName) || !EndHelperModule.Session.roomStatDict_colorIndex.Contains(currentRoomName))
        {
            EndHelperModule.Session.roomStatDict_death[currentRoomName] = (int)0;
            EndHelperModule.Session.roomStatDict_timer[currentRoomName] = (long)0;
            EndHelperModule.Session.roomStatDict_colorIndex[currentRoomName] = (int)level.Session.LevelData.EditorColorIndex;
        }
        if (!EndHelperModule.Session.roomStatDict_strawberries.Contains(currentRoomName))
        {
            EndHelperModule.Session.roomStatDict_strawberries[currentRoomName] = (int)0;
        }
        EndHelperModule.Session.roomStatDict_death[currentRoomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[currentRoomName]) + 1;
    }

    private int firstRowShown = 0;
    void statisticGUI()
    {
        MTexture backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background"];
        MTexture backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short"];
        MTexture pageArrow = GFX.Gui["dotarrow_outline"];
        if (!EndHelperModule.Settings.StatDisplay.MenuMulticolor)
        {
            backgroundTexture = GFX.Gui["misc/EndHelper/statGUI_background_purple"];
            backgroundTextureShort = GFX.Gui["misc/EndHelper/statGUI_background_short_purple"];
        }
            const int roomsPerColumn = 16;
        int lastRowShown = firstRowShown + 2*roomsPerColumn;

        int currentItem = 0;

        int startX = 550;
        int startX_first = 100;
        const int col2Buffer = 900;

        int dictSize = EndHelperModule.Session.roomStatDict_death.Count;

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

        Session session = SceneAs<Level>().Session;
        // Map Name Header
        String mapName = session.Area.GetSID();
        if (mapName.StartsWith("Celeste/"))
        {
            int mapID = session.Area.ID;
            mapName = $"AREA_{mapID}";
        }
        mapName = mapName.DialogCleanOrNull(Dialog.Languages["english"]) ?? mapName;

        AreaMode side = session.Area.Mode;
        if (side == AreaMode.BSide)
        {
            mapName += " B";
        } else if (side == AreaMode.CSide)
        {
            mapName += " C";
        }
        ActiveFont.DrawOutline($"{mapName}", new Vector2(980, 5), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.Orange, 2f, Color.Black);


        // The table headers (aka just death and timer icons)
        ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
        ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
        if (dictSize - firstRowShown > roomsPerColumn)
        {
            ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + col2Buffer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + col2Buffer + width_death / 2, startY - heightBetweenRows), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
        }

        // The table
        int totalDeaths = 0;
        long totalTimer = 0;
        int totalStrawberries = 0;
        clipboardText = $"Room\tDeaths\tTime\tBerries";

        foreach (string roomName in new ArrayList(EndHelperModule.Session.roomStatDict_death.Keys))
        {
            if(roomName == "")
            {
                EndHelperModule.Session.roomStatDict_death.Remove("");
                EndHelperModule.Session.roomStatDict_timer.Remove("");
                continue;
            }

            totalDeaths += Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
            totalTimer += Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName]);
            totalStrawberries += Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);

            int roomDeaths = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]);
            string roomTimeString = EndHelperModule.MinimalGameplayFormat(TimeSpan.FromTicks(Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[roomName])));
            int roomStrawberriesCollected = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]);

            string shortenedRoomName = roomName;
            Vector2 textScale = new Vector2(0.7f, 0.7f);
            if (roomName.Length > 45)
            {
                shortenedRoomName = $"{roomName.Substring(0, 43)}...";
            }
            if (roomName.Length > 35)
            {
                textScale = new Vector2(0.4f, 0.4f);
            }
            else if
            (roomName.Length > 25)
            {
                textScale = new Vector2(0.5f, 0.5f);
            }

            if (currentItem >= firstRowShown && currentItem < lastRowShown)
            {
                int displayRow = currentItem - firstRowShown;
                int col2BufferCurrent = 0;
                if (currentItem - firstRowShown >= roomsPerColumn) {
                    displayRow += -roomsPerColumn;
                    col2BufferCurrent = col2Buffer;
                }

                Color bgColor;
                if (EndHelperModule.Settings.StatDisplay.MenuMulticolor)
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
                } else
                {
                    bgColor = Color.White;
                }

                backgroundTexture.Draw(new Vector2(startX + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                ActiveFont.DrawOutline(shortenedRoomName, new Vector2(startX + bufferX + col2BufferCurrent, startY + heightBetweenRows * displayRow + (heightBetweenRows-5)/2), new Vector2(0f, 0.5f), textScale, Color.White, 2f, Color.Black);

                backgroundTextureShort.Draw(new Vector2(startX_death + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);
                ActiveFont.DrawOutline($"{roomDeaths}", new Vector2(startX_death + col2BufferCurrent + width_death/2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                backgroundTextureShort.Draw(new Vector2(startX_timer + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);

                ActiveFont.DrawOutline(roomTimeString, new Vector2(startX_timer + col2BufferCurrent + width_timer/2, startY + heightBetweenRows * displayRow), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

                if(roomStrawberriesCollected > 0)
                {
                    ActiveFont.DrawOutline($":EndHelper/uioutline_strawberry:", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows/2 - 3), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                }
                if (roomStrawberriesCollected > 1)
                {
                    ActiveFont.DrawOutline($"{roomStrawberriesCollected}", new Vector2(startX_strawberry + col2BufferCurrent, startY + heightBetweenRows * displayRow + heightBetweenRows / 2), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.White, 2f, Color.Black);
                }
            }
            currentItem++;
            if(roomStrawberriesCollected == 0)
            {
                clipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t";
            } 
            else
            {
                clipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t{roomStrawberriesCollected}";
            }
        }

        // Total Stats
        showStats(100, 1010, 0.7f, Color.White, 255, true, 0, true, true, true, "Total: ", "", totalDeaths, totalTimer, totalStrawberries);

        //ActiveFont.DrawOutline(displayTotalStatsString, new Vector2(totalXpos, 1010), new Vector2(0f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

        // Page Number
        if (dictSize > roomsPerColumn * 2)
        {
            int totalPage = (int)Math.Ceiling((float)dictSize / (roomsPerColumn * 2));
            int currentPage = (int)Math.Ceiling((float)(firstRowShown+1) / (roomsPerColumn * 2));

            const int pageXpos = 1740;

            ActiveFont.DrawOutline($"{currentPage}/{totalPage}", new Vector2(pageXpos, 1010), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);

            // Left
            if (currentPage != 1) {
                pageArrow.DrawCentered(new Vector2(pageXpos - 60, 1010), new Color(1f, 1f, 1f, 1f), 1, MathF.PI);
            } else {
                pageArrow.DrawCentered(new Vector2(pageXpos - 60, 1010), new Color(0.3f, 0.3f, 0.3f, 0.3f), 1, MathF.PI);
            }
            // Right
            if (currentPage < totalPage) {
                pageArrow.DrawCentered(new Vector2(pageXpos + 60, 1010), new Color(1f, 1f, 1f, 1f), 1, 0);
            } else {
                pageArrow.DrawCentered(new Vector2(pageXpos + 60, 1010), new Color(0.3f, 0.3f, 0.3f, 0.3f), 1, 0);
            }
        }

        // Scroll Down
        // Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"menu: currentItem {currentItem} dictSize {dictSize}");
        if (firstRowShown < dictSize - roomsPerColumn*2 && (Input.MenuDown.Pressed || Input.MenuRight.Pressed))
        {
            firstRowShown += roomsPerColumn * 2;
        }
        // Scroll Up
        if (firstRowShown > 0 && (Input.MenuUp.Pressed || Input.MenuLeft.Pressed))
        {
            firstRowShown -= roomsPerColumn * 2;
        }
    }
}