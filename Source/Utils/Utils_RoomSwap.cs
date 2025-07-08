using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.EndHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EndHelper.SharedCode
{
    static internal class Utils_RoomSwap
    {
        internal static void ReupdateAllRooms()
        {
            if (Engine.Scene is not Level level)
            {
                return;
            }
            else
            {
                ReupdateAllRooms(level);
            }
        }

        internal static void ReupdateAllRooms(global::Celeste.Level level)
        {
            foreach (String gridID in EndHelperModule.Session.roomSwapOrderList.Keys)
            {
                int roomSwapTotalRow = EndHelperModule.Session.roomSwapRow[gridID];
                int roomSwapTotalColumn = EndHelperModule.Session.roomSwapColumn[gridID];
                String roomSwapPrefix = EndHelperModule.Session.roomSwapPrefix[gridID];
                String roomTemplatePrefix = EndHelperModule.Session.roomTemplatePrefix[gridID];

                for (int row = 1; row <= roomSwapTotalRow; row++)
                {
                    for (int column = 1; column <= roomSwapTotalColumn; column++)
                    {
                        ReplaceRoomAfterReloadEnd(gridID, roomSwapPrefix, row, column, level);
                    }
                }
                EndHelperModule.RoomModificationEventTrigger(gridID);
            }
        }

        internal static async void ReplaceRoomAfterReloadEnd(string gridID, String roomSwapPrefix, int row, int column, global::Celeste.Level level)
        {
            while (EndHelperModule.reloadComplete != true)
            {
                await Task.Delay(20);
            }

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"Replace {EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1]} >> {roomSwapPrefix}{row}{column}");
            ReplaceRoom($"{roomSwapPrefix}{row}{column}", EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1], level);
        }

        static LevelData getRoomDataFromName(string roomName, Level level)
        {
            foreach (LevelData levelData in level.Session.MapData.Levels)
            {
                if (levelData.Name == roomName) { return levelData; }
            }
            Logger.Log(LogLevel.Warn, "EndHelper/main/getRoomDataFromName", $"Unable to find room {roomName} - returning current room leveldata instead.");
            return level.Session.LevelData; //returns current room if can't find (this should not happen)
        }

        static void ReplaceRoom(String replaceSwapRoomName, String replaceTemplateRoomName, global::Celeste.Level level)
        {
            LevelData replaceSwapRoomData = getRoomDataFromName(replaceSwapRoomName, level);
            LevelData replaceTemplateRoomData = getRoomDataFromName(replaceTemplateRoomName, level);

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"Replacing room {replaceSwapRoomName} with the template {replaceTemplateRoomName}");

            // Avoid changing name, position
            // FG and BG tiles don't even work smhmh
            replaceSwapRoomData.Entities = replaceTemplateRoomData.Entities;
            replaceSwapRoomData.Dummy = replaceTemplateRoomData.Dummy;
            //replaceSwapRoomData.Space = replaceTemplateRoomData.Space;
            replaceSwapRoomData.Bg = replaceTemplateRoomData.Bg;
            replaceSwapRoomData.BgDecals = replaceTemplateRoomData.BgDecals;

            // Spawns don't have their position set properly, but that's what TransitionRespawnForceSameRoomTrigger is for
            replaceSwapRoomData.Spawns = replaceTemplateRoomData.Spawns;
            replaceSwapRoomData.DefaultSpawn = replaceTemplateRoomData.DefaultSpawn;

            //Tiles only SOMETIMES work, so i'll remove here so they consistently don't work
            //replaceSwapRoomData.BgTiles = replaceTemplateRoomData.BgTiles;
            //replaceSwapRoomData.FgTiles = replaceTemplateRoomData.FgTiles;
            replaceSwapRoomData.ObjTiles = replaceTemplateRoomData.ObjTiles;

            replaceSwapRoomData.Solids = replaceTemplateRoomData.Solids;

            replaceSwapRoomData.FgDecals = replaceTemplateRoomData.FgDecals;
            replaceSwapRoomData.Music = replaceTemplateRoomData.Music;
            replaceSwapRoomData.Strawberries = replaceTemplateRoomData.Strawberries;
            replaceSwapRoomData.Triggers = replaceTemplateRoomData.Triggers;
            replaceSwapRoomData.MusicLayers = replaceTemplateRoomData.MusicLayers;
            replaceSwapRoomData.Music = replaceTemplateRoomData.Music;
            replaceSwapRoomData.MusicProgress = replaceTemplateRoomData.MusicProgress;
            replaceSwapRoomData.MusicWhispers = replaceTemplateRoomData.MusicWhispers;
            replaceSwapRoomData.DelayAltMusic = replaceTemplateRoomData.DelayAltMusic;
            replaceSwapRoomData.AltMusic = replaceTemplateRoomData.AltMusic;
            replaceSwapRoomData.Ambience = replaceTemplateRoomData.Ambience;
            replaceSwapRoomData.AmbienceProgress = replaceTemplateRoomData.AmbienceProgress;
            replaceSwapRoomData.Dark = replaceTemplateRoomData.Dark;
            replaceSwapRoomData.EnforceDashNumber = replaceTemplateRoomData.EnforceDashNumber;
            replaceSwapRoomData.Underwater = replaceTemplateRoomData.Underwater;
            replaceSwapRoomData.WindPattern = replaceTemplateRoomData.WindPattern;
            replaceSwapRoomData.HasGem = replaceTemplateRoomData.HasGem;
            replaceSwapRoomData.HasHeartGem = replaceTemplateRoomData.HasHeartGem;
            replaceSwapRoomData.HasCheckpoint = replaceTemplateRoomData.HasCheckpoint;
        }

        internal static async void TemporarilyDisableTrigger(int millisecondDelay, string gridID)
        {
            EndHelperModule.Session.allowTriggerEffect[gridID] = false;
            await Task.Delay(millisecondDelay);
            EndHelperModule.Session.allowTriggerEffect[gridID] = true;
        }

        internal static bool ModifyRooms(String modifyType, bool isSilent, Player player, Level level, String gridID, int teleportDelayMilisecond = 0, int teleportDisableMilisecond = 200, bool flashEffect = false)
        {
            bool succeedModify = false;

            //player is NULLable! player should only be checked inside the not-silent box
            LevelData currentRoomData = level.Session.LevelData;
            String currentRoomName = currentRoomData.Name;

            int roomSwapTotalRow = EndHelperModule.Session.roomSwapRow[gridID];
            int roomSwapTotalColumn = EndHelperModule.Session.roomSwapColumn[gridID];
            String roomSwapPrefix = EndHelperModule.Session.roomSwapPrefix[gridID];
            String roomTemplatePrefix = EndHelperModule.Session.roomTemplatePrefix[gridID];

            String currentTemplateRoomName = GetTemplateRoomFromSwapRoom(currentRoomName);

            if (EndHelperModule.Session.allowTriggerEffect[gridID])
            {
                Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"Modifying Room! Type: {modifyType}. Triggered from {currentRoomName}. ({roomSwapTotalRow}x{roomSwapTotalColumn})");
                TemporarilyDisableTrigger(teleportDisableMilisecond + (int)(EndHelperModule.Session.roomTransitionTime[gridID] * 1000 + teleportDelayMilisecond), gridID);

                if (EndHelperModule.Session.roomSwapOrderList.ContainsKey(gridID)) //Don't run this if first load
                {
                    level.Session.SetFlag(GetTransitionFlagName(), false); //Remove flag
                }


                switch (modifyType)
                {
                    case "Reset":
                        {
                            List<List<string>> initial = null;
                            if (EndHelperModule.Session.roomSwapOrderList.TryGetValue(gridID, out List<List<string>> value))
                            {
                                initial = new List<List<string>>(Utils_General.DeepCopyJSON(value));
                            }


                            EndHelperModule.Session.roomSwapOrderList[gridID] = [];
                            for (int row = 1; row <= roomSwapTotalRow; row++)
                            {
                                List<string> roomRow = [];
                                for (int column = 1; column <= roomSwapTotalColumn; column++)
                                {
                                    roomRow.Add($"{roomTemplatePrefix}{row}{column}");
                                }
                                EndHelperModule.Session.roomSwapOrderList[gridID].Add(roomRow);
                            }

                            if (!Utils_General.Are2LayerListsEqual(initial, EndHelperModule.Session.roomSwapOrderList[gridID]))
                            {
                                UpdateRooms();
                            }

                            //If reset is triggered while in the swap zone, do le warp
                            if (currentRoomName.StartsWith(roomSwapPrefix))
                            {
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "CurrentRowLeft":
                        {
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentRowLeft ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].Add(EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][0]);
                            EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].RemoveAt(0);
                            UpdateRooms();
                            TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                        }
                        break;

                    case "CurrentRowLeft_PreventWarp":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentRowLeft_PreventWarp ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if roomCol is not leftmost room
                            if (roomCol != 1)
                            {
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].Add(EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][0]);
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].RemoveAt(0);
                                UpdateRooms();
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "CurrentRowRight":
                        {
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentRowRight ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].Insert(0, EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomSwapTotalColumn - 1]);
                            EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].RemoveAt(roomSwapTotalColumn);
                            UpdateRooms();
                            TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                        }
                        break;

                    case "CurrentRowRight_PreventWarp":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentRowRight_PreventWarp ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if roomCol is not rightmost room
                            if (roomCol != roomSwapTotalColumn)
                            {
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].Insert(0, EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomSwapTotalColumn - 1]);
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1].RemoveAt(roomSwapTotalColumn);
                                UpdateRooms();
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "CurrentColumnUp":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentColumnUp ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            String topRoomName = EndHelperModule.Session.roomSwapOrderList[gridID][0][roomCol - 1];
                            for (int row = 1; row <= roomSwapTotalRow - 1; row++)
                            {
                                //Move each room up
                                EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][row][roomCol - 1];
                            }
                            //Copy over top room to the bottom
                            EndHelperModule.Session.roomSwapOrderList[gridID][roomSwapTotalRow - 1][roomCol - 1] = topRoomName;
                            UpdateRooms();
                            TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                        }
                        break;

                    case "CurrentColumnUp_PreventWarp":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentColumnUp_PreventWarp ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if roomRow is not topmost room
                            if (roomRow != 1)
                            {
                                String topRoomName = EndHelperModule.Session.roomSwapOrderList[gridID][0][roomCol - 1];
                                for (int row = 1; row <= roomSwapTotalRow - 1; row++)
                                {
                                    //Move each room up
                                    EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][row][roomCol - 1];
                                }
                                //Copy over top room to the bottom
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomSwapTotalRow - 1][roomCol - 1] = topRoomName;
                                UpdateRooms();
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "CurrentColumnDown":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentColumnDown ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            String bottomRoomName = EndHelperModule.Session.roomSwapOrderList[gridID][roomSwapTotalRow - 1][roomCol - 1];
                            for (int row = roomSwapTotalRow; row > 1; row--)
                            {
                                //Move each room down
                                EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][row - 2][roomCol - 1];
                            }
                            //Copy over bottom room to the top
                            EndHelperModule.Session.roomSwapOrderList[gridID][0][roomCol - 1] = bottomRoomName;
                            UpdateRooms();
                            TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                        }
                        break;

                    case "CurrentColumnDown_PreventWarp":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of CurrentColumnDown_PreventWarp ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if roomRow is not bottommost room
                            if (roomRow != roomSwapTotalRow)
                            {
                                String bottomRoomName = EndHelperModule.Session.roomSwapOrderList[gridID][roomSwapTotalRow - 1][roomCol - 1];
                                for (int row = roomSwapTotalRow; row > 1; row--)
                                {
                                    //Move each room down
                                    EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][row - 2][roomCol - 1];
                                }
                                //Copy over bottom room to the top
                                EndHelperModule.Session.roomSwapOrderList[gridID][0][roomCol - 1] = bottomRoomName;
                                UpdateRooms();
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "SwapLeftRight":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0) 
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of SwapLeftRight ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if not leftmost or rightmost
                            if (roomCol != roomSwapTotalColumn && roomCol != 1)
                            {
                                String leftRoom = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 2];
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 2] = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol];
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol] = leftRoom;
                                UpdateRooms();
                                //teleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    case "SwapUpDown":
                        {
                            int roomCol = GetPosFromRoomName(currentRoomName)[1];
                            int roomRow = GetPosFromRoomName(currentRoomName)[0];

                            if (roomCol < 0 || roomRow < 0)
                            {
                                // Room is not in grid. Why is the box here...
                                Logger.Log(LogLevel.Warn, "EndHelper/Utils_RoomSwap", $"Modification of SwapUpDown ran, but current room {currentRoomName} is not in grid!");
                                break;
                            }

                            //Only continue if not topmost or bottommost
                            if (roomRow != roomSwapTotalRow && roomRow != 1)
                            {
                                String topRoom = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 2][roomCol - 1];
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 2][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow][roomCol - 1];
                                EndHelperModule.Session.roomSwapOrderList[gridID][roomRow][roomCol - 1] = topRoom;
                                UpdateRooms();
                                //teleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                            }
                        }
                        break;

                    //set_11_12_21_22
                    case string s when s.StartsWith("Set_"):
                        {
                            string oldTemplateRoomAtThisPos = "";
                            int roomCol = 0; int roomRow = 0;
                            if (currentTemplateRoomName != null)
                            {
                                roomCol = GetPosFromRoomName(currentRoomName)[1];
                                roomRow = GetPosFromRoomName(currentRoomName)[0];
                                // Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"------------- room name {currentRoomName}. col row {roomCol} {roomRow} and gridID {gridID}");
                                oldTemplateRoomAtThisPos = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 1];
                            }

                            string[] splittedArr = s.Split("_");
                            int row = 1;
                            int column = 1;

                            List<List<string>> initial = null;
                            if (EndHelperModule.Session.roomSwapOrderList.TryGetValue(gridID, out List<List<string>> value))
                            {
                                initial = new List<List<string>>(Utils_General.DeepCopyJSON(value));
                            }

                            for (int i = 1; i < splittedArr.Length; i++)
                            {

                                List<int> roomPos = GetPosFromRoomName(splittedArr[i]);

                                // Set i-th item in splittedArr to row/col
                                if (roomPos[0] == -1 || roomPos[1] == -1)
                                {
                                    // Same as the previous one
                                    EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1] = initial[row - 1][column - 1];
                                }
                                else
                                {
                                    // Dependent on setted roomPos
                                    EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1] = $"{roomTemplatePrefix}{roomPos[0]}{roomPos[1]}";
                                }

                                // Change row/column index.
                                // If overshot/undershot, it still shouldn't break
                                column++;
                                if (column > roomSwapTotalColumn)
                                {
                                    column = 1;
                                    row++;
                                }
                                if (row > roomSwapTotalRow) { break; }
                            }
                            if (!Utils_General.Are2LayerListsEqual(initial, EndHelperModule.Session.roomSwapOrderList[gridID]))
                            {

                                UpdateRooms();

                                // Teleport the player if it is triggered in a room that gets changed from setting
                                // First, check if the player is even in the grid
                                if (oldTemplateRoomAtThisPos == "")
                                {
                                    // Player is not in a room in the grid. Stop checking.
                                }
                                else
                                {
                                    // There can be multiple of the same template room. First check if the current room is the same
                                    string newTemplateRoomAtThisPos = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 1];

                                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"current {oldTemplateRoomAtThisPos} new {newTemplateRoomAtThisPos}");
                                    if (oldTemplateRoomAtThisPos != newTemplateRoomAtThisPos)
                                    {
                                        // Only teleport if the template room at the same position is different
                                        TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                                    }
                                }
                            }
                        }
                        break;

                    case "None":
                        UpdateRooms();
                        break;

                    default:
                        // nothing!!!!
                        break;
                }
                level.Session.SetFlag(GetTransitionFlagName(), true); //Set flag
            }

            void UpdateRooms()
            {
                for (int row = 1; row <= roomSwapTotalRow; row++)
                {
                    for (int column = 1; column <= roomSwapTotalColumn; column++)
                    {
                        ReplaceRoom($"{roomSwapPrefix}{row}{column}", EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1], level);
                    }
                }
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", "Updating rooms...");
                swapEffects();
            }

            async void TeleportToRoom(String teleportToRoomName, Player player, Level level)
            {
                LevelData currentRoomData = level.Session.LevelData;
                Vector2 currentRoomPos = currentRoomData.Position;

                if (currentRoomData.Name == teleportToRoomName)
                {
                    //If same room, do nothing
                    EndHelperModule.Session.allowTriggerEffect[gridID] = true; //Well actually we can set this to true immediately
                }
                else
                {
                    LevelData toRoomData = getRoomDataFromName(teleportToRoomName, level);
                    Vector2 toRoomPos = toRoomData.Position;

                    await Task.Delay(teleportDelayMilisecond);

                    Vector2 playerOriginalPos = new(player.Position.X, player.Position.Y);

                    level.NextTransitionDuration = EndHelperModule.Session.roomTransitionTime[gridID];
                    Vector2 transitionOffset = toRoomPos - currentRoomPos;
                    Vector2 transitionDirection = transitionOffset.SafeNormalize();

                    player.Position += transitionOffset;
                    level.TransitionTo(toRoomData, transitionDirection);

                    //Occasionally the transition is jank and undoes the position change.
                    //This is here to unjank the jank
                    if (Math.Abs(playerOriginalPos.X - player.Position.X) <= 5 && Math.Abs(playerOriginalPos.Y - player.Position.Y) <= 5)
                    {
                        //If player coordinates barely change, teleport again...
                        player.Position += transitionOffset;
                    }

                    player.ResetSpriteNextFrame(default); // Hopefully fixes a bug where the player sometimes turns invisible after warp

                    // Move followers along with player
                    for (int index = 0; index < player.Leader.PastPoints.Count; index++)
                    {
                        player.Leader.PastPoints[index] += transitionOffset;
                    }
                    foreach (var follower in player.Leader.Followers)
                    {
                        if (follower != null)
                        {
                            follower.Entity.Position += transitionOffset;
                        }
                    }
                    Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"Teleporting from {currentRoomData.Name} >> {teleportToRoomName}. Pos change: ({playerOriginalPos.X} {playerOriginalPos.Y} => {player.Position.X} {player.Position.Y}) - change by ({(toRoomPos - currentRoomPos).X} {(toRoomPos - currentRoomPos).Y}), Transition direction: ({transitionDirection.X} {transitionDirection.Y})");
                }
            }

            String getSwapRoomFromTemplateRoom(String templateRoomName)
            {
                List<List<String>> roomList = EndHelperModule.Session.roomSwapOrderList[gridID];
                for (int row = 1; row <= roomSwapTotalRow; row++)
                {
                    for (int column = 1; column <= roomSwapTotalColumn; column++)
                    {
                        if (templateRoomName == roomList[row - 1][column - 1])
                        {
                            //Found match at {row}{colu} - the swap room is {prefix}{row}{col}
                            return $"{roomSwapPrefix}{row}{column}";
                        }
                    }
                }
                Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"getSwapRoomFromTemplateRoom - Unable to find {templateRoomName} - returning current room leveldata instead.");
                return currentRoomName; //This shouldn't happen...
            }

            String GetTemplateRoomFromSwapRoom(String swapRoomName)
            {
                if (swapRoomName.StartsWith(roomSwapPrefix))
                {
                    List<List<String>> roomList = EndHelperModule.Session.roomSwapOrderList[gridID];
                    int len = swapRoomName.Length;
                    int rowIndex = swapRoomName[len - 2] - '0';
                    rowIndex += -1;
                    int colIndex = swapRoomName[len - 1] - '0';
                    colIndex += -1;
                    String templateRoomName = roomList[rowIndex][colIndex];
                    return templateRoomName;
                }
                //This means the current room is not a swap room
                //If the template room can't be found it means modifyRoom was triggered from outside the swap grid
                //If the swap effect doesn't depend on the current room this will run with no issues! (Null check for Set)
                return null;
            }

            void swapEffects()
            {
                EndHelperModule.RoomModificationEventTrigger(gridID);

                if (flashEffect)
                {
                    level.Flash(Color.White, drawPlayerOver: true);
                }
                if (!isSilent && player is not null)
                {
                    level.Shake();

                    if (EndHelperModule.Session.activateSoundEvent1[gridID] != "")
                    {
                        Audio.Play(EndHelperModule.Session.activateSoundEvent1[gridID], player.Position);
                    }
                    if (EndHelperModule.Session.activateSoundEvent2[gridID] != "")
                    {
                        Audio.Play(EndHelperModule.Session.activateSoundEvent2[gridID], player.Position);
                    }
                }
                succeedModify = true;
            }

            String GetTransitionFlagName()
            {
                String flagName = roomSwapPrefix;
                for (int row = 1; row <= roomSwapTotalRow; row++)
                {
                    for (int column = 1; column <= roomSwapTotalColumn; column++)
                    {
                        String roomNameAtPos = EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1];
                        List<int> roomPos = GetPosFromRoomName(roomNameAtPos);
                        flagName += $"_{roomPos[0]}{roomPos[1]}";
                    }
                }
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_RoomSwap", $"getTransitionFlagName - Obtained flag name for {roomSwapPrefix} to be {flagName}");
                return flagName;
            }
            return succeedModify;
        }

        /// <summary>
        /// Returns the last 2 digits of the room name... or any string lol. Returns [-1, -1] if last 2 characters aren't digits.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        internal static List<int> GetPosFromRoomName(String roomName)
        {
            int len = roomName.Length;
            int row; int col;

            if (char.IsDigit(roomName[len - 1]) && char.IsDigit(roomName[len - 2]))
            {
                row = roomName[len - 2] - '0';
                col = roomName[len - 1] - '0';
                return [row, col];
            }
            else
            {
                return [-1, -1];
            }
        }
    }
}
