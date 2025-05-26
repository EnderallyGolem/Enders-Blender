using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.EndHelper.Integration;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;

namespace Celeste.Mod.EndHelper.Utils
{
    internal class Utils_JournalStatistics
    {
        internal static bool journalOpen = false;
        internal static bool journalStatisticsGuiOpen = false;
        private static bool journalStatisticsRoomNameEditMenuOpen = false;

        // Yeah I could definitely have made this a class or something... oh well
        private static List<string> journalStatisticsMapNameSideInternalList = [];
        private static List<string> journalStatisticsMapNameSideDisplayList = [];
        private static List<int> journalStatisticsMapTotalStrawberryList = [];
        private static List<Color> journalStatisticsMapNameColorList = [];
        private static List<bool> journalStatisticsMapClearList = [];
        private static int journalStatisticsMapNameSideIndex = 0;

        private static string journalStatisticsClipboardText = "";
        private static string journalStatisticsEditingRoomName = "";
        private static int journalStatisticsFirstRowShown = 0;
        private static int journalStatisticsEditingRoomIndex = 0;

        internal enum journalRoomStatMenuTypeEnum { FirstClear, LastSession }
        private static journalRoomStatMenuTypeEnum journalRoomStatMenuType = journalRoomStatMenuTypeEnum.FirstClear;

        internal static float journalStatisticsBackgroundAlpha;

        private static void JournalStatisticsMenuCloseNameEditor()
        {
            //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"MenuCloseNameEditor: close text menu");
            TextInput.OnInput -= JournalStatisticsOnTextInput;

            Utils_General.ConsumeInput(Input.ESC, 3);
            Utils_General.ConsumeInput(Input.Pause, 3);
            Utils_General.ConsumeInput(Input.MenuCancel, 3);
            Utils_General.ConsumeInput(Input.MenuConfirm, 3);
            journalStatisticsRoomNameEditMenuOpen = false;

            EndHelperModule.afkDurationFrames = 0;
            Audio.Play("event:/ui/main/rename_entry_accept");
        }

        private static void JournalStatisticsOnTextInput(char c)
        {
            Dictionary<string, string> roomStatCustomNameDict;
            String mapNameSide_Internal = journalStatisticsMapNameSideInternalList[journalStatisticsMapNameSideIndex];
            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
            {
                roomStatCustomNameDict = EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<string, string>;
            }
            else
            {
                roomStatCustomNameDict = Utils_General.ConvertToStringDictionary((Dictionary<object, object>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal]);
            }
            roomStatCustomNameDict.TryGetValue(journalStatisticsEditingRoomName, out string roomCustomName);

            //Logger.Log(LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"i have typed {c}. mapNameSide_Internal: {mapNameSide_Internal}, journalStatisticsEditingRoomName: {journalStatisticsEditingRoomName}, roomCustomName: {roomCustomName}");

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

            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<object, object>)
            {
                (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<object, object>)[journalStatisticsEditingRoomName] = roomCustomName;
            }
            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
            {
                (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<string, string>)[journalStatisticsEditingRoomName] = roomCustomName;
            }
        }

        private static void JournalOpenActive(OuiJournal self)
        {
            journalStatisticsMapNameSideIndex = 0;
            journalStatisticsFirstRowShown = 0;
            journalStatisticsEditingRoomIndex = 0;

            journalOpen = true;

            Oui currentOui = self.Overworld.Current;

            List<AreaStats> mapAreaStatsList = [];

            if (Engine.Scene is Level level && CollabUtils2Integration.CollabUtils2Installed)
            {
                AreaData journalArea = new DynData<Overworld>(self.Overworld).Get<AreaData>("collabInGameForcedArea");
                String areaLevelSetName = journalArea.LevelSet;
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"whats this forcearea {areaLevelSetName}");

                LevelSetStats areaLevelSet = null;
                if (areaLevelSetName != null)
                {
                    areaLevelSet = global::Celeste.SaveData.Instance.GetLevelSetStatsFor(areaLevelSetName);
                }

                List<AreaStats> sortedMaps = areaLevelSet?.Areas;

                // Same sorting as used by CollabUI2
                Regex startsWithNumber = new Regex(".*/[0-9]+-.*");
                if (sortedMaps is not null && sortedMaps.Select(map => AreaData.Get(map).Icon ?? "").All(icon => startsWithNumber.IsMatch(icon)))
                {
                    sortedMaps.Sort((a, b) => {
                        AreaData adata = AreaData.Get(a);
                        AreaData bdata = AreaData.Get(b);

                        bool aHeartSide = CollabUtils2Import.IsHeartSide(a.SID);
                        bool bHeartSide = CollabUtils2Import.IsHeartSide(b.SID);

                        // heart sides should appear last.
                        if (aHeartSide && !bHeartSide)
                            return 1;
                        if (!aHeartSide && bHeartSide)
                            return -1;

                        // sort by icon name, then by map bin name.
                        return adata.Icon == bdata.Icon ? adata.Name.CompareTo(bdata.Name) : adata.Icon.CompareTo(bdata.Icon);
                    });
                }

                foreach (AreaStats lobbyMap in areaLevelSet?.Areas ?? new List<AreaStats>())
                {
                    AreaData areaData = AreaData.Get(lobbyMap.ID_Safe);
                    mapAreaStatsList.Add(lobbyMap);
                }
            }
            else
            {
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"the journal is in overworld (or lobby no collabutil????)");
                foreach (AreaStats overworldMap in global::Celeste.SaveData.Instance.Areas_Safe)
                {
                    AreaData areaData = AreaData.Get(overworldMap.ID_Safe);
                    if (areaData.Interlude_Safe)
                    {
                        continue;
                    }
                    mapAreaStatsList.Add(overworldMap);
                }
            }

            // From mapNameSideList, Create a list of all SIDES with data. mapNameSideList is just all maps, need to check sides too!
            // (This code is horrid by the way)

            journalStatisticsMapNameSideInternalList = [];
            journalStatisticsMapNameSideDisplayList = [];
            journalStatisticsMapTotalStrawberryList = [];
            journalStatisticsMapNameColorList = [];
            journalStatisticsMapClearList = [];
            foreach (AreaStats areaStats in mapAreaStatsList)
            {
                AreaData areaData = AreaData.Get(areaStats.ID_Safe);

                bool mapClearA = areaStats.Modes.Length >= 1 && areaStats.Modes[0] is not null && areaStats.Modes[0].Completed;
                bool mapClearB = areaStats.Modes.Length >= 2 && areaStats.Modes[1] is not null && areaStats.Modes[1].Completed;
                bool mapClearC = areaStats.Modes.Length >= 3 && areaStats.Modes[2] is not null && areaStats.Modes[2].Completed;

                int mapStrawCountA = areaData.Mode[0] is null ? 0 : areaData.Mode[0].TotalStrawberries;
                int mapStrawCountB = areaData.Mode.Length <= 1 || areaData.Mode[1] is null ? 0 : areaData.Mode[1].TotalStrawberries;
                int mapStrawCountC = areaData.Mode.Length <= 2 || areaData.Mode[2] is null ? 0 : areaData.Mode[2].TotalStrawberries;

                AreaKey areaKey = areaData.ToKey();
                AreaKey mapKeyA = areaKey; mapKeyA.Mode = AreaMode.Normal; // I think it is always normal but just in case!
                AreaKey mapKeyB = areaKey; mapKeyB.Mode = AreaMode.BSide;
                AreaKey mapKeyC = areaKey; mapKeyC.Mode = AreaMode.CSide;


                String mapNameSide_Display_A = RoomStatisticsDisplayer.GetMapNameSideDisplay(mapKeyA);
                String mapNameSide_Internal_A = RoomStatisticsDisplayer.GetMapNameSideInternal(mapKeyA);
                Color mapNameColor = RoomStatisticsDisplayer.GetMapColour(mapKeyA);
                String mapNameSide_Display_B = RoomStatisticsDisplayer.GetMapNameSideDisplay(mapKeyB);
                String mapNameSide_Internal_B = RoomStatisticsDisplayer.GetMapNameSideInternal(mapKeyB);
                String mapNameSide_Display_C = RoomStatisticsDisplayer.GetMapNameSideDisplay(mapKeyC);
                String mapNameSide_Internal_C = RoomStatisticsDisplayer.GetMapNameSideInternal(mapKeyC);

                if ((EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal_A) && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal_A].Count > 0)
                    || EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder.ContainsKey(mapNameSide_Internal_A))
                {
                    journalStatisticsMapNameSideInternalList.Add(mapNameSide_Internal_A);
                    journalStatisticsMapNameSideDisplayList.Add(mapNameSide_Display_A);
                    journalStatisticsMapNameColorList.Add(mapNameColor);
                    journalStatisticsMapTotalStrawberryList.Add(mapStrawCountA);
                    journalStatisticsMapClearList.Add(mapClearA);
                }

                if ((EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal_B) && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal_B].Count > 0)
                    || EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder.ContainsKey(mapNameSide_Internal_B))
                {
                    journalStatisticsMapNameSideInternalList.Add(mapNameSide_Internal_B);
                    journalStatisticsMapNameSideDisplayList.Add(mapNameSide_Display_B);
                    journalStatisticsMapNameColorList.Add(mapNameColor);
                    journalStatisticsMapTotalStrawberryList.Add(mapStrawCountB);
                    journalStatisticsMapClearList.Add(mapClearB);
                    //Logger.Log(LogLevel.Info, "EndHelper/main", $"MAPS [B]: {mapNameSide_Display_B}");
                }

                if ((EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal_C) && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal_C].Count > 0)
                    || EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder.ContainsKey(mapNameSide_Internal_C))
                {
                    journalStatisticsMapNameSideInternalList.Add(mapNameSide_Internal_C);
                    journalStatisticsMapNameSideDisplayList.Add(mapNameSide_Display_C);
                    journalStatisticsMapNameColorList.Add(mapNameColor);
                    journalStatisticsMapTotalStrawberryList.Add(mapStrawCountC);
                    journalStatisticsMapClearList.Add(mapClearC);
                    //Logger.Log(LogLevel.Info, "EndHelper/main", $"MAPS [C]: {mapNameSide_Display_C}");
                }
            }
        }

        internal static void Update(OuiJournal journal)
        {
            if (journal.Active && journal.Focused)
            {
                if (!journalOpen)
                {
                    JournalOpenActive(journal);
                }

                // Show/Hide GUI
                if (EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed && !journalStatisticsGuiOpen)
                {
                    journalStatisticsGuiOpen = true;
                    Audio.Play("event:/ui/game/pause");
                }
                else if (journalStatisticsGuiOpen && !journalStatisticsRoomNameEditMenuOpen && (Input.ESC.Pressed || Input.MenuCancel.Pressed
                    || Input.Pause || EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed))
                {
                    journalStatisticsGuiOpen = false;
                    journalOpen = false; // May not be true but if it's not true it'll be immediately set to true
                                         // This is just in case of edge cases, eg ESC skips JournalClose
                    EndHelperModule.afkDurationFrames = 0;
                    Audio.Play("event:/ui/game/unpause");


                    if (Input.MenuCancel.Pressed)
                    {
                        string clipboardToolTipMsg = Dialog.Get("EndHelper_Dialog_RoomStatisticsDisplayer_CopiedToClipboard");
                        RoomStatisticsDisplayer.ShowTooltip(clipboardToolTipMsg, 2f);
                        TextInput.SetClipboardText(journalStatisticsClipboardText);
                    }
                    Utils_General.ConsumeInput(Input.ESC, 3);
                    Utils_General.ConsumeInput(Input.Pause, 3);
                }

                if ((Input.ESC.Pressed || Input.Pause.Pressed || !journalStatisticsGuiOpen) && journalStatisticsRoomNameEditMenuOpen)
                {
                    JournalStatisticsMenuCloseNameEditor();
                }
            }
            else
            {
                journalStatisticsGuiOpen = false;
                journalOpen = false;
                journalStatisticsBackgroundAlpha = 0;
            }

            if (journalStatisticsGuiOpen && journalStatisticsBackgroundAlpha < 0.7)
            {
                journalStatisticsBackgroundAlpha += 0.1f;
            }
            if (!journalStatisticsGuiOpen && journalStatisticsBackgroundAlpha > 0)
            {
                journalStatisticsBackgroundAlpha += -0.1f;
            }

            if (journalStatisticsGuiOpen)
            {
                if (Engine.Scene is Level)
                {
                    EndHelperModule.mInputDisableDuration = 3;
                }

                // Buttons!
                if (!journalStatisticsRoomNameEditMenuOpen)
                {
                    // Tab changing stat type
                    if (Input.MenuJournal.Pressed)
                    {
                        journalRoomStatMenuType = (journalRoomStatMenuTypeEnum)(((int)journalRoomStatMenuType + 1) % Enum.GetValues(typeof(journalRoomStatMenuTypeEnum)).Length);
                    }

                    // Scroll through maps
                    journalStatisticsMapNameSideIndex = Utils_General.ScrollInput(valueToChange: journalStatisticsMapNameSideIndex, increaseInput: Input.MenuDown, increaseValue: 1,
                        decreaseInput: Input.MenuUp, decreaseValue: 1, minValue: 0, maxValue: journalStatisticsMapNameSideInternalList.Count - 1, loopValues: true, doNotChangeIfPastCap: false,
                        framesFirstHeldChange: 30, framesBetweenHeldChange: 5);

                    if (Input.MenuDown.Pressed || Input.MenuUp.Pressed)
                    {
                        journalStatisticsFirstRowShown = 0;
                    }
                }
            }
        }

        internal static void Render()
        {
            // Do stuff with journalStatisticsMapNameSideInternalList
            Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * journalStatisticsBackgroundAlpha);

            if (journalStatisticsGuiOpen)
            {
                if (journalStatisticsMapNameSideInternalList.Count <= 0)
                {
                    if (EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount < 10 && EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != -1)
                    {
                        ActiveFont.DrawOutline(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_Journal_NoMaps_LowStorageSetting"), new Vector2(960, 300), new Vector2(0.5f, 0f), new Vector2(1f, 1f), Color.LightGray, 2f, Color.Black);
                    }
                    else
                    {
                        ActiveFont.DrawOutline(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_Journal_NoMaps"), new Vector2(960, 300), new Vector2(0.5f, 0f), new Vector2(1f, 1f), Color.LightGray, 2f, Color.Black);
                    }
                }
                else
                {
                    JournalStatisticsGUI();
                }
            }
            else if (journalOpen)
            {
                int instructionXPos = 1681;
                const int instructionYPos = 980;
                const float instructionScale = 0.5f;
                Color instructionColor = Color.White;

                ActiveFont.DrawOutline("Room Stats ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"Room Stats XI", 0) * instructionScale);

                Input.GuiButton(EndHelperModule.Settings.OpenStatDisplayMenu.Button, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
            }
        }

        private static void JournalStatisticsGUI()
        {
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
            MTexture moveRoomArrows = GFX.Gui["misc/EndHelper/statGUI_moveroomarrows"];

            String mapNameSide_Internal = journalStatisticsMapNameSideInternalList[journalStatisticsMapNameSideIndex];
            String mapNameSide_Display = journalStatisticsMapNameSideDisplayList[journalStatisticsMapNameSideIndex];
            Color mapNameColor = journalStatisticsMapNameColorList[journalStatisticsMapNameSideIndex];
            int mapTotalStrawberries = journalStatisticsMapTotalStrawberryList[journalStatisticsMapNameSideIndex];
            bool mapCleared = journalStatisticsMapClearList[journalStatisticsMapNameSideIndex];

            // OrderedDictionary is so unwieldy and was a mistake...
            Dictionary<string, string> roomStatCustomNameDict;
            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
            {
                roomStatCustomNameDict = EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<string, string>;
            }
            else
            {
                roomStatCustomNameDict = Utils_General.ConvertToStringDictionary((Dictionary<object, object>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal]);
            }


            // Map Number
            int totalMapNum = journalStatisticsMapNameSideInternalList.Count;

            int mapNumXpos = 960;
            const int mapNumXPosOffsetMin = 300; // Minimum offset distance (Has to be at least this far to the right)
            const int mapNumYpos = 30;

            // Shift num to the right to prevent overlap if too close
            int mapNumXposOffset = (int)(ActiveFont.WidthToNextLine(mapNameSide_Display, 0) * 0.7f) / 2 + 110;
            if (mapNumXposOffset < mapNumXPosOffsetMin)
            {
                mapNumXposOffset = mapNumXPosOffsetMin;
            }
            mapNumXpos += mapNumXposOffset;

            ActiveFont.DrawOutline($"{journalStatisticsMapNameSideIndex + 1}/{totalMapNum}", new Vector2(mapNumXpos, mapNumYpos), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.7f), mapNameColor, 2f, Color.Black);

            if (totalMapNum > 1)
            {
                pageArrow.DrawCentered(new Vector2(mapNumXpos - 60, mapNumYpos), mapNameColor, 1, MathF.PI * 0.5f); // Up
                pageArrow.DrawCentered(new Vector2(mapNumXpos + 60, mapNumYpos), mapNameColor, 1, MathF.PI * 1.5f); // Down
            }
            else
            {
                pageArrow.DrawCentered(new Vector2(mapNumXpos - 60, mapNumYpos), mapNameColor * 0.3f, 1, MathF.PI * 0.5f); // Up
                pageArrow.DrawCentered(new Vector2(mapNumXpos + 60, mapNumYpos), mapNameColor * 0.3f, 1, MathF.PI * 1.5f); // Down
            }

            // Map not cleared msg
            if (!mapCleared)
            {
                String notClearedYetMessage = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_MapNotClearedYet");
                int mapNotClearedMsgXpos = 960 - mapNumXposOffset + 50;
                ActiveFont.DrawOutline($"({notClearedYetMessage})", new Vector2(mapNotClearedMsgXpos, mapNumYpos), new Vector2(1f, 0.5f), new Vector2(0.7f, 0.7f), mapNameColor * 0.5f, 2f, Color.Black);
            }


            const int roomsPerColumn = 16;
            int lastRowShown = journalStatisticsFirstRowShown + 2 * roomsPerColumn;

            int currentItemIndex = 0;

            int startX = 550;
            int startX_first = 100;
            const int col2Buffer = 900;

            // Create a list of all room names to show
            List<string> roomNamesToShowList = new List<string>();

            // Map Name Header
            ActiveFont.DrawOutline($"{mapNameSide_Display}", new Vector2(960, 5), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), mapNameColor, 2f, Color.Black);
            if (journalRoomStatMenuType == journalRoomStatMenuTypeEnum.FirstClear)
            {
                String firstClearMsg;

                if (mapCleared)
                {
                    firstClearMsg = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_FirstClear");
                }
                else
                {
                    firstClearMsg = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_FirstRun");
                }
                ActiveFont.DrawOutline($"({firstClearMsg})", new Vector2(960, 40), new Vector2(0.5f, 0f), new Vector2(0.45f, 0.45f), Color.DarkGray, 2f, Color.Black);
            }
            else
            {
                String currentSessionMsg = Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_LastClear");
                ActiveFont.DrawOutline($"({currentSessionMsg})", new Vector2(960, 40), new Vector2(0.5f, 0f), new Vector2(0.45f, 0.45f), Color.DarkGray, 2f, Color.Black);
            }

            ICollection allRoomsList;

            int instructionXPos = 100;
            const int instructionYPos = 1060;
            const float instructionScale = 0.4f;
            Color instructionColor = Color.LightGray;

            if (journalRoomStatMenuType == journalRoomStatMenuTypeEnum.FirstClear)
            {
                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.TryGetValue(mapNameSide_Internal, out List<string> value)
                    && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Count > 0)
                {
                    allRoomsList = value;
                }
                else
                {
                    ActiveFont.DrawOutline(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_Journal_NoFirstClearData"), new Vector2(960, 300), new Vector2(0.5f, 0f), new Vector2(0.9f, 0.9f), Color.LightGray, 2f, Color.Black);
                    ActiveFont.DrawOutline($"Change Stats:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Stats: XI", 0) * instructionScale);
                    Input.GuiButton(Input.MenuJournal, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
                    return;
                }
            }
            else
            {
                if (EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder.ContainsKey(mapNameSide_Internal))
                {
                    allRoomsList = EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal];
                }
                else
                {
                    ActiveFont.DrawOutline(Dialog.Clean("EndHelper_Dialog_RoomStatisticsDisplayer_Journal_NoLastClearData"), new Vector2(960, 300), new Vector2(0.5f, 0f), new Vector2(0.9f, 0.9f), Color.LightGray, 2f, Color.Black);
                    ActiveFont.DrawOutline($"Change Stats:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Stats: XI", 0) * instructionScale);
                    Input.GuiButton(Input.MenuJournal, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
                    return;
                }
            }

            foreach (string roomName in new ArrayList(allRoomsList))
            {
                if (roomName == "") { continue; }
                int roomDeaths;
                TimeSpan roomTimeSpan;
                int roomStrawberriesCollected;

                if (journalRoomStatMenuType == journalRoomStatMenuTypeEnum.FirstClear)
                {
                    // Show First Cycle
                    roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName];
                    roomTimeSpan = TimeSpan.FromTicks(EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName]);
                    roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName];
                }
                else
                {
                    // Show last session
                    roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_latestSession_death[mapNameSide_Internal][roomName];
                    roomTimeSpan = TimeSpan.FromTicks(EndHelperModule.SaveData.mapDict_roomStat_latestSession_timer[mapNameSide_Internal][roomName]);
                    roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_latestSession_strawberries[mapNameSide_Internal][roomName];
                }
                string roomTimeString = Utils_General.MinimalGameplayFormat(roomTimeSpan);

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
                        defaultName = $"{mapNameSide_Internal}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;

                        if (defaultName == roomStatCustomNameDict[roomName]) { continue; }
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

            if (dictSize - journalStatisticsFirstRowShown > roomsPerColumn)
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

            // The table headers (aka just death and timer icons)
            const int iconHeightOffset = -50;
            ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            if (dictSize - journalStatisticsFirstRowShown > roomsPerColumn)
            {
                ActiveFont.DrawOutline(":EndHelper/uioutline_skull:", new Vector2(startX_death + col2Buffer + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
                ActiveFont.DrawOutline(":EndHelper/uioutline_clock:", new Vector2(startX_timer + col2Buffer + width_death / 2, startY + iconHeightOffset), new Vector2(0.5f, 0f), new Vector2(0.7f, 0.7f), Color.White, 2f, Color.Black);
            }

            // The table
            int totalDeaths = 0;
            long totalTimer = 0;
            int totalStrawberries = 0;
            journalStatisticsClipboardText = $"Room\tDeaths\tTime\tBerries";

            foreach (string roomName in roomNamesToShowList)
            {
                int roomDeaths;
                long roomTimeTicks;
                TimeSpan roomTimeSpan;
                int roomStrawberriesCollected;

                if (journalRoomStatMenuType == journalRoomStatMenuTypeEnum.FirstClear)
                {
                    // Show First Cycle
                    roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName];
                    roomTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer[mapNameSide_Internal][roomName];
                    roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries[mapNameSide_Internal][roomName];
                }
                else
                {
                    // Show current stats
                    roomDeaths = EndHelperModule.SaveData.mapDict_roomStat_latestSession_death[mapNameSide_Internal][roomName];
                    roomTimeTicks = EndHelperModule.SaveData.mapDict_roomStat_latestSession_timer[mapNameSide_Internal][roomName];
                    roomStrawberriesCollected = EndHelperModule.SaveData.mapDict_roomStat_latestSession_strawberries[mapNameSide_Internal][roomName];
                }
                roomTimeSpan = TimeSpan.FromTicks(roomTimeTicks);
                string roomTimeString = Utils_General.MinimalGameplayFormat(roomTimeSpan);

                totalDeaths += roomDeaths;
                totalTimer += roomTimeTicks;
                totalStrawberries += roomStrawberriesCollected;

                roomStatCustomNameDict.TryGetValue(roomName, out string customRoomName);
                if ((customRoomName is null || (customRoomName.Trim().Length == 0)) && !journalStatisticsRoomNameEditMenuOpen)
                {
                    String newCustomRoomName = $"{mapNameSide_Internal}_{roomName}".DialogCleanOrNull(Dialog.Languages["english"]) ?? roomName;
                    if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<object, object>)
                    {
                        (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<object, object>)[roomName] = newCustomRoomName;
                    }
                    if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
                    {
                        (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] as Dictionary<string, string>)[roomName] = newCustomRoomName;
                    }
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

                if (currentItemIndex >= journalStatisticsFirstRowShown && currentItemIndex < lastRowShown)
                {
                    int displayRow = currentItemIndex - journalStatisticsFirstRowShown;
                    int col2BufferCurrent = 0;
                    if (currentItemIndex - journalStatisticsFirstRowShown >= roomsPerColumn)
                    {
                        displayRow += -roomsPerColumn;
                        col2BufferCurrent = col2Buffer;
                    }

                    Color bgColor;
                    if (EndHelperModule.Settings.RoomStatMenu.MenuMulticolor)
                    {
                        int colorIndex = EndHelperModule.SaveData.mapDict_roomStat_colorIndex[mapNameSide_Internal].TryGetValue(roomName, out int value) ? value : 0;
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

                    if (journalStatisticsRoomNameEditMenuOpen && journalStatisticsEditingRoomIndex == currentItemIndex)
                    {
                        journalStatisticsEditingRoomName = roomName; // Update editingRoomName while updating the bg
                        backgroundTextureEdit.Draw(new Vector2(startX + col2BufferCurrent, startY + heightBetweenRows * displayRow), Vector2.Zero, bgColor);

                        if (RoomStatisticsDisplayer.renameRoomsMoveRooms)
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
                    journalStatisticsClipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t";
                }
                else
                {
                    journalStatisticsClipboardText += $"\r\n{shortenedRoomName}\t{roomDeaths}\t{roomTimeString}\t{roomStrawberriesCollected}";
                }
                currentItemIndex++;
            }

            // Total Stats
            bool showTotalMapBerryCount = true;
            if (!EndHelperModule.Settings.RoomStatMenu.MenuSpoilBerries && !mapCleared)
            {
                showTotalMapBerryCount = false; // No berry count spoilery.
            }

            String totalText = "Total";
            if (filterSetting != roomStatMenuFilter.None) { totalText += $" [{filterString}]"; }
            RoomStatisticsDisplayer.ShowGUIStats("", 100, 1010, 0.7f, Color.White, true, 0, true, true, false, showTotalMapBerryCount, mapTotalStrawberries, $"{totalText}: ", "", totalDeaths, totalTimer, totalStrawberries, false);

            // Instructions
            if (!journalStatisticsRoomNameEditMenuOpen)
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

                ActiveFont.DrawOutline($"Change Stats:", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Stats: XI", 0) * instructionScale);
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
                if (journalStatisticsMapNameSideInternalList.Count > 1)
                {
                    ActiveFont.DrawOutline("Change Map: ", new Vector2(instructionXPos, instructionYPos), new Vector2(0f, 0.5f), new Vector2(instructionScale, instructionScale), instructionColor, 2f, Color.Black);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"Change Map: XI", 0) * instructionScale);
                    Input.GuiButton(Input.MenuUp, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"XX", 0) * instructionScale);
                    Input.GuiButton(Input.MenuDown, mode: Input.PrefixMode.Latest).DrawCentered(new Vector2(instructionXPos, instructionYPos), instructionColor, instructionScale, 0);
                    instructionXPos += (int)(ActiveFont.WidthToNextLine($"XXXXX", 0) * instructionScale);
                }
            }
            else
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


            // Timer and Death Count Freeze Icons
            String pauseIconMsg = "";

            if (journalRoomStatMenuType == journalRoomStatMenuTypeEnum.FirstClear)
            {
                if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal].ContainsKey("Pause")) { EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["Pause"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal].ContainsKey("Inactive")) { EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["Inactive"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal].ContainsKey("AFK")) { EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["AFK"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal].ContainsKey("LoadNoDeath")) { EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["LoadNoDeath"] = false; }

                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["LoadNoDeath"])
                {
                    pauseIconMsg += ":EndHelper/ui_deathfreeze_loadstaterespawn:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["Pause"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_pause:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["Inactive"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_inactive:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType[mapNameSide_Internal]["AFK"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_afk:";
                }
            }
            else
            {
                if (!EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal].ContainsKey("Pause")) { EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["Pause"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal].ContainsKey("Inactive")) { EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["Inactive"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal].ContainsKey("AFK")) { EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["AFK"] = false; }
                if (!EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal].ContainsKey("LoadNoDeath")) { EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["LoadNoDeath"] = false; }

                if (EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["LoadNoDeath"])
                {
                    pauseIconMsg += ":EndHelper/ui_deathfreeze_loadstaterespawn:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["Pause"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_pause:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["Inactive"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_inactive:";
                }
                if (EndHelperModule.SaveData.mapDict_roomStat_latestSession_pauseType[mapNameSide_Internal]["AFK"])
                {
                    pauseIconMsg += ":EndHelper/ui_timerfreeze_afk:";
                }
            }
            ActiveFont.DrawOutline(pauseIconMsg, new Vector2(1820, instructionYPos + 80), new Vector2(1f, 0.5f), Vector2.One * 3f, Color.DarkGray, 1f, Color.Black);

            string totalTimeString = Utils_General.MinimalGameplayFormat(TimeSpan.FromTicks(totalTimer));
            journalStatisticsClipboardText += $"\r\n{"Total"}\t{totalDeaths}\t{totalTimeString}\t{totalStrawberries}";


            // Page Number
            int currentPage = (int)Math.Ceiling((float)(journalStatisticsFirstRowShown + 1) / (roomsPerColumn * 2));
            if (dictSize > roomsPerColumn * 2)
            {
                int totalPage = (int)Math.Ceiling((float)dictSize / (roomsPerColumn * 2));

                const int pageXpos = 1660;

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
            if (!journalStatisticsRoomNameEditMenuOpen)
            {
                journalStatisticsFirstRowShown = Utils_General.ScrollInput(valueToChange: journalStatisticsFirstRowShown, increaseInput: Input.MenuRight, increaseValue: roomsPerColumn * 2,
                    decreaseInput: Input.MenuLeft, decreaseValue: roomsPerColumn * 2, minValue: 0, maxValue: (dictSize - 1) - (dictSize - 1) % (roomsPerColumn * 2), loopValues: false, doNotChangeIfPastCap: true,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
            }

            // Ensure room that is being renamed is currently viewble
            if (journalStatisticsRoomNameEditMenuOpen)
            {
                int oldEditingRoomIndex = journalStatisticsEditingRoomIndex;

                // Allow changing rooms
                journalStatisticsEditingRoomIndex = Utils_General.ScrollInput(valueToChange: journalStatisticsEditingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.PageDown), increaseValue: 1,
                    decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.PageUp), decreaseValue: 1, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
                journalStatisticsEditingRoomIndex = Utils_General.ScrollInput(valueToChange: journalStatisticsEditingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Down), increaseValue: 1,
                    decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Up), decreaseValue: 1, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);
                journalStatisticsEditingRoomIndex = Utils_General.ScrollInput(valueToChange: journalStatisticsEditingRoomIndex, increaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Right), increaseValue: roomsPerColumn,
                    decreaseInput: MInput.Keyboard.orig_Check(Microsoft.Xna.Framework.Input.Keys.Left), decreaseValue: roomsPerColumn, minValue: 0, maxValue: dictSize - 1, loopValues: false, doNotChangeIfPastCap: false,
                    framesFirstHeldChange: 30, framesBetweenHeldChange: 5);

                int editRoomPage = (int)Math.Ceiling((journalStatisticsEditingRoomIndex + 1f) / (roomsPerColumn * 2));
                if (currentPage > editRoomPage)
                {
                    journalStatisticsFirstRowShown -= roomsPerColumn * 2;
                }
                else if (currentPage < editRoomPage)
                {
                    journalStatisticsFirstRowShown += roomsPerColumn * 2;
                }

                // Allow moving rooms
                if (Input.MenuJournal.Check)
                {
                    Utils_JournalStatistics.EditingRoomMovePosition(oldEditingRoomIndex, journalStatisticsEditingRoomIndex, mapNameSide_Internal, journalRoomStatMenuType);
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

            // Room Stat Menu Buttons Functionality
            if (!journalStatisticsRoomNameEditMenuOpen)
            {
                if (Input.QuickRestart.Pressed)
                {
                    // Renaming current room
                    // Consume Input shouldn't be here for journal otherwise it might eat up input when its not supposed to
                    //ConsumeInput(Input.QuickRestart, 3);

                    //LogLevel.Info, "EndHelper/RoomStatisticsDisplayer", $"open text menu");
                    journalStatisticsRoomNameEditMenuOpen = true;
                    Audio.Play("event:/ui/main/message_confirm");
                    TextInput.OnInput += JournalStatisticsOnTextInput;

                    // Set the cursor to start of page
                    journalStatisticsEditingRoomIndex = (currentPage - 1) * roomsPerColumn * 2;

                }
                else if (Input.MenuConfirm.Pressed)
                {
                    // Change Filter
                    filterSetting = (roomStatMenuFilter)(((int)filterSetting + 1) % Enum.GetValues(typeof(roomStatMenuFilter)).Length);
                }
            }
            if (Engine.Scene is Level)
            {
                MInput.Disabled = true;
            }
        }

        public static void EditingRoomMovePosition(int initialPosIndex, int finalPosIndex, string mapNameSide_Internal, journalRoomStatMenuTypeEnum? menuType)
        {
            if (initialPosIndex == finalPosIndex)
            {
                return;
            }
            switch (menuType)
            {
                case null: // Session
                    String roomNameKey = (string)((IList)EndHelperModule.Session.roomStatDict_death.Keys)[initialPosIndex]; //c# might be a little bit stupid

                    int value_death = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[initialPosIndex]);
                    EndHelperModule.Session.roomStatDict_death.RemoveAt(initialPosIndex);
                    EndHelperModule.Session.roomStatDict_death.Insert(finalPosIndex, roomNameKey, value_death);

                    long value_timer = Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[initialPosIndex]);
                    EndHelperModule.Session.roomStatDict_timer.RemoveAt(initialPosIndex);
                    EndHelperModule.Session.roomStatDict_timer.Insert(finalPosIndex, roomNameKey, value_timer);

                    int value_strawberries = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[initialPosIndex]);
                    EndHelperModule.Session.roomStatDict_strawberries.RemoveAt(initialPosIndex);
                    EndHelperModule.Session.roomStatDict_strawberries.Insert(finalPosIndex, roomNameKey, value_strawberries);

                    int value_colorIndex = Convert.ToInt32(EndHelperModule.Session.roomStatDict_colorIndex[initialPosIndex]);
                    EndHelperModule.Session.roomStatDict_colorIndex.RemoveAt(initialPosIndex);
                    EndHelperModule.Session.roomStatDict_colorIndex.Insert(finalPosIndex, roomNameKey, value_colorIndex);

                    break;

                case journalRoomStatMenuTypeEnum.FirstClear:
                    String roomNameFirstClear = EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal][initialPosIndex];
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].RemoveAt(initialPosIndex);
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder[mapNameSide_Internal].Insert(finalPosIndex, roomNameFirstClear);
                    break;

                case journalRoomStatMenuTypeEnum.LastSession:
                    String roomNameLastSession = EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal][initialPosIndex];
                    EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal].RemoveAt(initialPosIndex);
                    EndHelperModule.SaveData.mapDict_roomStat_latestSession_roomOrder[mapNameSide_Internal].Insert(finalPosIndex, roomNameLastSession);
                    break;

                default:
                    break;
            }
        }
    }
}
