using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.SpeedrunTool.Message;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using On.Celeste;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using static MonoMod.InlineRT.MonoModRule;
using static On.Celeste.Level;
using static On.Celeste.Strawberry;
using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;
using IL.Celeste;
using Celeste;
using System.Reflection;
using NETCoreifier;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings;
using Celeste.Mod.EndHelper.Entities.RoomSwap;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.EndHelper;

public class EndHelperModule : EverestModule {

    #region Everest Stuff
    public static EndHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(EndHelperModuleSettings);
    public static EndHelperModuleSettings Settings => (EndHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(EndHelperModuleSession);
    public static EndHelperModuleSession Session => (EndHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(EndHelperModuleSaveData);
    public static EndHelperModuleSaveData SaveData => (EndHelperModuleSaveData) Instance._SaveData;

    public EndHelperModule()
    {
        Instance = this;
    }

    //Custom spritebank, for contest xml location stuff
    public static SpriteBank SpriteBank => Instance._CustomEntitySpriteBank;
    private SpriteBank _CustomEntitySpriteBank;

    #endregion

    #region Initialisation

    // Debug mode is annoying
    public enum SessionResetCause { None, LoadState, Debug }
    public static int timeSinceSessionReset = 2;                                    // If == 1, correct for resets if needed. Starts from 2 so it does not cause a reset when loading!
    public static SessionResetCause lastSessionResetCause = SessionResetCause.None; // Stores the previous cause of reset. Sometimes useful.

    // Store information for room stats externally for them to persist through save states
    public static Dictionary<string, string> externalRoomStatDict_customName = new Dictionary<string, string> { };
    public static OrderedDictionary externalRoomStatDict_death = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_timer = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_strawberries = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_colorIndex = new OrderedDictionary { };

    // Decreases till -ve, enables input if 0 and disables if +
    // Lets me disable, but ensure it gets re-enabled when I don't need it anymore
    public static int mInputDisableDuration = 0;

    // This is modified by SSMQolIntegration to change multiroom bino speed multiplier
    // -1 means not integrated
    public static bool integratingWithSSMQoL = false;

    // Event Listener for when room modification occurs
    public static event EventHandler<RoomModificationEventArgs> RoomModificationEvent;
    public static bool enableRoomSwapHooks = false;
    public class RoomModificationEventArgs : EventArgs
    {
        public string gridID { get; set; }
        public RoomModificationEventArgs(string gridID)
        {
            this.gridID = gridID;
        }
    }

    public static void RoomModificationEventTrigger(string gridID)
    {
        RoomModificationEvent?.Invoke(null, new RoomModificationEventArgs(gridID));
    }

    public override void Load() {
        //On.Celeste.Level.TransitionRoutine += Hook_TransitionRoutine;
        Everest.Events.AssetReload.OnReloadLevel += ReupdateAllRooms;
        Everest.Events.AssetReload.OnBeforeReload += ReloadBeginFunc;
        Everest.Events.AssetReload.OnAfterReload += ReloadCompleteFunc;
        Everest.Events.Level.OnEnter += EnterMapFunc;

        On.Monocle.Engine.Update += Hook_EngineUpdate;
        On.Celeste.Level.Update += Hook_LevelUpdate;
        On.Celeste.Level.UpdateTime += Hook_LevelUpdateTime;
        On.Celeste.Player.Die += Hook_OnPlayerDeath;
        On.Celeste.Player.IntroRespawnBegin += Hook_OnPlayerRespawn;
        On.Celeste.Level.TransitionRoutine += Hook_TransitionRoutine;
        On.Celeste.LevelLoader.StartLevel += Hook_StartMap;
        On.Celeste.Level.Pause += Hook_Pause;

        On.Celeste.Editor.MapEditor.Update += Hook_UsingMapEditor;
        On.Celeste.Strawberry.Added += Hook_StrawberryAddedToLevel;
        On.Celeste.Strawberry.OnCollect += Hook_CollectStrawberry;

        SpeedrunToolIntegration.Load();
        SSMQoLIntegration.Load();
    }

    // Optional, initialize anything after Celeste has initialized itself properly.
    public override void Initialize(){

    }

    // Optional, do anything requiring either the Celeste or mod content here.
    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
        _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/EndHelper/Sprites.xml");
    }

    // Unload the entirety of your mod's content. Free up any native resources.
    public override void Unload() {
        //On.Celeste.Level.TransitionRoutine -= Hook_TransitionRoutine;
        Everest.Events.AssetReload.OnReloadLevel -= ReupdateAllRooms;
        Everest.Events.AssetReload.OnBeforeReload -= ReloadBeginFunc;
        Everest.Events.AssetReload.OnAfterReload -= ReloadCompleteFunc;
        Everest.Events.Level.OnEnter -= EnterMapFunc;

        On.Monocle.Engine.Update -= Hook_EngineUpdate;
        On.Celeste.Level.Update -= Hook_LevelUpdate;
        On.Celeste.Level.UpdateTime -= Hook_LevelUpdateTime;
        On.Celeste.Player.Die -= Hook_OnPlayerDeath;
        On.Celeste.Player.IntroRespawnBegin -= Hook_OnPlayerRespawn;
        On.Celeste.Level.TransitionRoutine -= Hook_TransitionRoutine;
        On.Celeste.LevelLoader.StartLevel -= Hook_StartMap;
        On.Celeste.Level.Pause -= Hook_Pause;

        On.Celeste.Editor.MapEditor.Update -= Hook_UsingMapEditor;
        On.Celeste.Strawberry.Added -= Hook_StrawberryAddedToLevel;
        On.Celeste.Strawberry.OnCollect -= Hook_CollectStrawberry;

        SpeedrunToolIntegration.Unload();
        SSMQoLIntegration.Unload();
    }
    #endregion

    #region Hooks

    public static bool reloadComplete;

    private static void ReloadCompleteFunc(bool silent)
    {
        reloadComplete = true;
    }
    private static void ReloadBeginFunc(bool silent)
    {
        reloadComplete = false;
    }
    private static void EnterMapFunc(global::Celeste.Session session, bool fromSaveData)
    {
        // If first time (not fromSaveData), check Hook_StartMap since it has access to level
        if (fromSaveData)
        {
        String roomName = session.Level;
            // +1 death for save and quit. The reason why this is done here instead of everest onexit event is because
            // as far as I can tell saving and returning to lobby with collabutil saves the session before onexit runs.
            EndHelperModule.Session.roomStatDict_death[roomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]) + 1;

            // Handle the custom name savedata dict. This requires fromSaveData as that is AFTER the session is made.
            SetupCustomNameSaveDataDict(session);
        }
    }

    static void SetupCustomNameSaveDataDict(global::Celeste.Session session)
    {
        String mapNameSide = session.Area.GetSID();
        if (session.Area.Mode == AreaMode.BSide) { mapNameSide += "_BSide"; }
        else if (session.Area.Mode == AreaMode.CSide) { mapNameSide += "_CSide"; }

        // Handle the dict storing room stat custom name dicts.
        // Move the current map to the front of the list, and trim size if
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Being the stuff:");
        if (EndHelperModule.Settings.RoomStatMenu.MenuCustomNameStorageCount > 0)
        {
            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Contains(mapNameSide))
            {
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"Already contains {mapNameSide} => {EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Count} => {EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide]}. Setting, Removing then Readding:");
                if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide] is Dictionary<string, string>)
                {
                    EndHelperModule.Session.roomStatDict_customName = (Dictionary<string, string>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide];
                } else
                {
                    EndHelperModule.Session.roomStatDict_customName = ConvertToStringDictionary((Dictionary<object, object>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide]);
                }
                EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Remove(mapNameSide);
            }
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"Adding {mapNameSide}.");
            EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide] = EndHelperModule.Session.roomStatDict_customName;
        }
        if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Count > EndHelperModule.Settings.RoomStatMenu.MenuCustomNameStorageCount)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"Too many mapDicts: Removing the earliest.");
            EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.RemoveAt(0);
        }
    }

    public static Dictionary<string, string> ConvertToStringDictionary(Dictionary<object, object> source)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        foreach (var kvp in source)
        {
            string key = kvp.Key?.ToString() ?? "";  // Convert key to string, default to ""
            string value = kvp.Value?.ToString() ?? ""; // Convert value to string, default to ""
            if (key == "" || value == ""){ continue; }
            result[key] = value;
        }
        return result;
    }

    // This has to be here so you don't get softlocked if MInput is disabled in UI or something
    private static void Hook_EngineUpdate(On.Monocle.Engine.orig_Update orig, global::Monocle.Engine self, GameTime gameTime)
    {
        bool levelPause = false;
        if (Engine.Scene is Level level)
        {
            levelPause = level.FrozenOrPaused;
        }

        if (mInputDisableDuration > -3 && !levelPause)
        {
            mInputDisableDuration--;
        }
        if (mInputDisableDuration >= 1)
        {
            MInput.Disabled = true;
        }
        if (mInputDisableDuration >= -2 && mInputDisableDuration <= 0)
        {
            MInput.Disabled = false;
        }
        orig(self, gameTime);
    }

    // Using player update instead of level update so nothing happens when the level is loading
    // Level update screws with the timeSinceSessionReset.
    private static void Hook_LevelUpdate(On.Celeste.Level.orig_Update orig, global::Celeste.Level self)
    {
        Level level = self;

        if (EndHelperModule.Settings.FreeMultiroomWatchtower.Button.Pressed && !level.FrozenOrPaused)
        {
            spawnMultiroomWatchtower();
        }

        EndHelperModule.timeSinceSessionReset++;

        if (EndHelperModule.timeSinceSessionReset == 1)
        {
            // This occurs when reset happens
            if (enableRoomSwapHooks)
            {
                ReupdateAllRooms(level); //This only exists so it updates when you respawn from debug. It umm still requires a transition/respawn to work lol
            }
            {
                if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
                {
                    roomStatDisplayer.ImportRoomStatInfo();
                }
            }
        }
        else if (EndHelperModule.timeSinceSessionReset > 1)
        {
            // Store data to use during reset
        }

        // Quick Restart Keybind
        if (EndHelperModule.Settings.QuickRetry.Button.Pressed && level.Tracker.GetEntity<Player>() is Player player && !level.Paused && level.CanPause && level.CanRetry)
        {
            if (level.Session.GrabbedGolden)
            {
                // Don't die if you have a golden. Just play a funny sfx instead.
                player.Add(new SoundSource("event:/game/general/strawberry_laugh"));
                return;
            } else if (!player.Dead)
            {
                level.Paused = false;
                level.PauseMainMenuOpen = false;
                Engine.TimeRate = 1f;
                Distort.GameRate = 1f;
                Distort.Anxiety = 0f;
                level.InCutscene = (level.SkippingCutscene = false);
                foreach (LevelEndingHook component in level.Tracker.GetComponents<LevelEndingHook>())
                {
                    if (component.OnEnd != null)
                    {
                        component.OnEnd();
                    }
                }
                PlayerDeadBody deadPlayer = player.Die(Vector2.Zero, evenIfInvincible: true);
                DynamicData deadPlayerData = DynamicData.For(deadPlayer);
                level.DoScreenWipe(wipeIn: false, level.Reload);
                deadPlayerData.Set("finished", true); // Stop trying to end again if holding confirm
            }
        }
        orig(self);
    }

    // This is here to ensure that (as much as possible) the times are synced
    // If added to the entity, it'll lag behind during the pause animation, and if in level update, it'll be ahead during state change
    private static int afkDurationFrames = 0;
    private static int inactiveDurationFrames = 0;
    public static bool allowIncrementTimer = true;
    private static void Hook_LevelUpdateTime(On.Celeste.Level.orig_UpdateTime orig, global::Celeste.Level self)
    {
        Level level = self;

        {
            if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                // Timer will be increased here instead of the entity's update as otherwise pause menu appearing will freeze the timer temporarily
                String incrementRoomName = roomStatDisplayer.currentRoomName;

                //AFK Checker
                if (Input.Aim == Vector2.Zero && Input.Dash.Pressed == false && Input.Grab.Pressed == false && Input.CrouchDash.Pressed == false && Input.Talk.Pressed == false
                    && Input.MenuCancel.Pressed == false && Input.MenuConfirm.Pressed == false && Input.ESC.Pressed == false && EndHelperModule.Settings.OpenStatDisplayMenu.Button.Pressed == false)
                {
                    afkDurationFrames++;
                }
                else
                {
                    afkDurationFrames = 0;
                }

                //Inactive Checker
                {
                    if (level.Tracker.GetEntity<Player>() is Player player && (player.InControl == false || level.InCutscene))
                    {
                        inactiveDurationFrames++;
                    }
                    else
                    {
                        inactiveDurationFrames = 0;
                    }
                }

                // Check if can increment time spent in room
                allowIncrementTimer = true;
                if (level.FrozenOrPaused && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.Pause ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseAFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseInactive ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseInactiveAFK
                ))
                {
                    allowIncrementTimer = false;
                    EndHelperModule.Session.pauseTypeDict["Pause"] = true;
                }


                if (inactiveDurationFrames >= 60 && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseInactive ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseInactiveAFK
                 ))
                {
                    allowIncrementTimer = false;
                    if (level.TimerStarted && !level.TimerStopped && !level.Completed)
                    { EndHelperModule.Session.pauseTypeDict["Inactive"] = true; }
                }

                if (afkDurationFrames >= 1800 && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.AFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseAFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.PauseScenarioEnum.PauseInactiveAFK
                ))
                {
                    allowIncrementTimer = false;
                    EndHelperModule.Session.pauseTypeDict["AFK"] = true;
                }

                if (!level.TimerStarted || level.TimerStopped || level.Completed)
                { allowIncrementTimer = false; }

                roomStatDisplayer.ensureDictsHaveKey(level);

                if (allowIncrementTimer)
                {
                    EndHelperModule.Session.roomStatDict_timer[incrementRoomName] = TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime).Ticks + Convert.ToInt64(EndHelperModule.Session.roomStatDict_timer[incrementRoomName]);
                }
            }
        }
        orig(self);
    }

    public static PlayerDeadBody Hook_OnPlayerDeath(On.Celeste.Player.orig_Die orig, global::Celeste.Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
    {
        Level level = self.SceneAs<Level>();
        //Increment room death count.
        if (global::Celeste.SaveData.Instance.Assists.Invincible && !evenIfInvincible)
        {
            // Assist mode death is not incremented if evenIfInvincible is false
        } 
        else
        {
            level.Tracker.GetEntity<RoomStatisticsDisplayer>()?.OnDeath();
        }

        return orig(self, direction, evenIfInvincible, registerDeathInStats);
    }

    public static void Hook_OnPlayerRespawn(On.Celeste.Player.orig_IntroRespawnBegin orig, global::Celeste.Player self)
    {
        Level level = self.SceneAs<Level>();

        //Update the room-swap rooms. This is kind of here as a failsafe, and also otherwise warping with debug mode permamently empty the swap rooms.
        reupdateAllRooms();

        orig(self);
    }

    private static IEnumerator Hook_TransitionRoutine(
        On.Celeste.Level.orig_TransitionRoutine orig, global::Celeste.Level self, global::Celeste.LevelData next, Vector2 direction
    )
    {
        // To potentially use in the future
        yield return new SwapImmediately(orig(self, next, direction));
    }

    private static void Hook_StartMap(On.Celeste.LevelLoader.orig_StartLevel orig, global::Celeste.LevelLoader self)
    {
        Level level = self.Level;
        level.Add(new RoomStatisticsDisplayer(level));
        inactiveDurationFrames = 60; // For maps starting with cutscene. If without, would be set to 0 immediately. Not even sure if this works lol

        // Set up the save data custom name dictionaries if starting a map from the beginning
        SetupCustomNameSaveDataDict(level.Session);

        orig(self);
    }
    private static void Hook_Pause(On.Celeste.Level.orig_Pause orig, global::Celeste.Level self, int startIndex, bool minimal, bool quickReset)
    {
        Level level = self;

        if (quickReset)
        {
            if (EndHelperModule.Settings.DisableQuickRestart ||
                (EndHelperModule.Settings.QuickRetry.Button.Pressed && level.Tracker.GetEntity<Player>() is Player player && !level.Paused && level.CanPause && level.CanRetry)
               )
            {
                // Do not quick reset if you are quick dying (or if disabled)
                return;
            }
        }
        orig(self, startIndex, minimal, quickReset);
    }

    public static void Hook_UsingMapEditor(On.Celeste.Editor.MapEditor.orig_Update orig, global::Celeste.Editor.MapEditor self)
    {
        timeSinceSessionReset = 0;
        lastSessionResetCause = SessionResetCause.Debug;
        orig(self);
    }

    // Component for strawberries to store its home room
    public class HomeRoom : Component
    {
        public string roomName;
        public HomeRoom(String roomName) : base(true, false)
        {
            this.roomName = roomName;
        }
    }

    private static void Hook_StrawberryAddedToLevel(On.Celeste.Strawberry.orig_Added orig, global::Celeste.Strawberry self, Scene scene)
    {
        Level level = scene as Level;
        String roomName = level.Session.LevelData.Name;
        self.Add(new HomeRoom(roomName));
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"added component to berry in {roomName}");

        orig(self, scene);
    }

    private static void Hook_CollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, global::Celeste.Strawberry self)
    {
        Level level = self.SceneAs<Level>();
        string roomName;

        string homeroom = self.Get<HomeRoom>().roomName;

        if (homeroom == "")
        {
            roomName = level.Session.LevelData.Name;
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"can't get homeroom, using current room {roomName}");
        } else
        {
            roomName = homeroom;
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"strawberry homeroom = {roomName}");
        }

        EndHelperModule.Session.roomStatDict_strawberries[roomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_strawberries[roomName]) + 1;

        orig(self);
    }

    #endregion

    #region Spawn Multiroom Watchtower

    private static void spawnMultiroomWatchtower()
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
        internal PortableMultiroomWatchtower(EntityData data, Vector2 offset) : base(data, offset) {
            allowAnywhere = true;
            destroyUponFinishView = true;
            maxSpeedSet *= 2;
            canToggleBlocker = true;
            doOverlapCheck = false;
        }
        internal static bool Exists => Engine.Scene.Tracker.GetEntity<PortableMultiroomWatchtower>() != null;
    }

    #endregion

    #region Room-Swap

    public static void reupdateAllRooms()
    {
        if (Engine.Scene is not Level level)
        {
            return;
        } else
        {
            ReupdateAllRooms(level);
        }
    }

    public static void ReupdateAllRooms(global::Celeste.Level level)
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
            RoomModificationEventTrigger(gridID);
        }
    }

    public static async void ReplaceRoomAfterReloadEnd(string gridID, String roomSwapPrefix, int row, int column, global::Celeste.Level level)
    {
        while (reloadComplete != true)
        {
            await Task.Delay(20);
        }

        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Replace {EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1]} >> {roomSwapPrefix}{row}{column}");
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

        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Replacing room {replaceSwapRoomName} with the template {replaceTemplateRoomName}");

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

    public static async void TemporarilyDisableTrigger(int millisecondDelay, string gridID)
    {
        EndHelperModule.Session.allowTriggerEffect[gridID] = false;
        await Task.Delay(millisecondDelay);
        EndHelperModule.Session.allowTriggerEffect[gridID] = true;
    }

    public static bool ModifyRooms(String modifyType, bool isSilent, Player player, Level level, String gridID, int teleportDelayMilisecond = 0, int teleportDisableMilisecond = 200, bool flashEffect = false)
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
            Logger.Log(LogLevel.Info, "EndHelper/Main", $"Modifying Room! Type: {modifyType}. Triggered from {currentRoomName}. ({roomSwapTotalRow}x{roomSwapTotalColumn})");
            EndHelperModule.TemporarilyDisableTrigger(teleportDisableMilisecond + (int)(EndHelperModule.Session.roomTransitionTime[gridID] * 1000 + teleportDelayMilisecond), gridID);

            if (EndHelperModule.Session.roomSwapOrderList.ContainsKey(gridID)) //Don't run this if first load
            {
                level.Session.SetFlag(GetTransitionFlagName(), false); //Remove flag
            }
            

            switch (modifyType)
            {

                case "Test":
                    EndHelperModule.Session.roomSwapOrderList[gridID] = [];
                    for (int row = 1; row <= roomSwapTotalRow; row++)
                    {
                        List<string> roomRow = [];
                        for (int column = 1; column <= roomSwapTotalColumn; column++)
                        {
                            GetPosFromRoomName($"{roomSwapPrefix}");
                            roomRow.Add($"{roomTemplatePrefix}11");

                        }
                        EndHelperModule.Session.roomSwapOrderList[gridID].Add(roomRow);
                    }
                    UpdateRooms();
                    //teleportToRoom("swap11", player, level);
                    if (currentRoomName.StartsWith(roomSwapPrefix))
                    {
                        TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                    }

                    //level.Session.LevelData = getRoomDataFromName($"{roomTemplatePrefix}11", level);
                    break;

                case "Reset":
                    {
                        List<List<string>> initial = null;
                        if (EndHelperModule.Session.roomSwapOrderList.TryGetValue(gridID, out List<List<string>> value))
                        {
                            initial = new List<List<string>>(DeepCopyJSON(value));
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

                        if(!Are2LayerListsEqual(initial, EndHelperModule.Session.roomSwapOrderList[gridID]))
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

                        EndHelperModule.Session.roomSwapOrderList[gridID][roomRow-1].Insert(0, EndHelperModule.Session.roomSwapOrderList[gridID][roomRow-1][roomSwapTotalColumn - 1]);
                        EndHelperModule.Session.roomSwapOrderList[gridID][roomRow-1].RemoveAt(roomSwapTotalColumn);
                        UpdateRooms();
                        TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                    }
                    break;

                case "CurrentRowRight_PreventWarp":
                    {
                        int roomCol = GetPosFromRoomName(currentRoomName)[1];
                        int roomRow = GetPosFromRoomName(currentRoomName)[0];

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

                        String topRoomName = EndHelperModule.Session.roomSwapOrderList[gridID][0][roomCol - 1];
                        for (int row = 1; row <= roomSwapTotalRow-1; row++)
                        {
                            //Move each room up
                            EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][roomCol - 1] = EndHelperModule.Session.roomSwapOrderList[gridID][row][roomCol - 1];
                        }
                        //Copy over top room to the bottom
                        EndHelperModule.Session.roomSwapOrderList[gridID][roomSwapTotalRow-1][roomCol - 1] = topRoomName;
                        UpdateRooms();
                        TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
                    }
                    break;

                case "CurrentColumnUp_PreventWarp":
                    {
                        int roomCol = GetPosFromRoomName(currentRoomName)[1];
                        int roomRow = GetPosFromRoomName(currentRoomName)[0];

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

                        //Only continue if not leftmost or rightmost
                        if (roomCol != roomSwapTotalColumn && roomCol != 1)
                        {
                            String leftRoom = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 2];
                            EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol-2] = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol];
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
                        int roomCol = GetPosFromRoomName(currentRoomName)[1];
                        int roomRow = GetPosFromRoomName(currentRoomName)[0];
                        string oldTemplateRoomAtThisPos = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 1];

                        string[] splittedArr = s.Split("_");
                        int row = 1;
                        int column = 1;

                        List<List<string>> initial = null;
                        if (EndHelperModule.Session.roomSwapOrderList.TryGetValue(gridID, out List<List<string>> value))
                        {
                            initial = new List<List<string>>(DeepCopyJSON(value));
                        }

                        for (int i = 1; i < splittedArr.Length; i++)
                        {

                            List<int> roomPos = GetPosFromRoomName(splittedArr[i]);

                            // Set i-th item in splittedArr to row/col
                            EndHelperModule.Session.roomSwapOrderList[gridID][row - 1][column - 1] = $"{roomTemplatePrefix}{roomPos[0]}{roomPos[1]}";
                            
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
                        if (!Are2LayerListsEqual(initial, EndHelperModule.Session.roomSwapOrderList[gridID]))
                        {

                            UpdateRooms();
                            // There can be multiple of the same template room. First check if the current room is the same
                            string newTemplateRoomAtThisPos = EndHelperModule.Session.roomSwapOrderList[gridID][roomRow - 1][roomCol - 1];

                            //Logger.Log(LogLevel.Info, "EndHelper/Main", $"current {oldTemplateRoomAtThisPos} new {newTemplateRoomAtThisPos}");
                            if (oldTemplateRoomAtThisPos != newTemplateRoomAtThisPos)
                            {
                                // Only teleport if the template room at the same position is different
                                TeleportToRoom(getSwapRoomFromTemplateRoom(currentTemplateRoomName), player, level);
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
            //Logger.Log(LogLevel.Info, "EndHelper/Main", "Updating rooms...");
            swapEffects();
        }

        async void TeleportToRoom(String teleportToRoomName, Player player, Level level)
        {
            LevelData currentRoomData = level.Session.LevelData;
            Vector2 currentRoomPos = currentRoomData.Position;

            if (currentRoomData.Name == teleportToRoomName){
                //If same room, do nothing
                EndHelperModule.Session.allowTriggerEffect[gridID] = true; //Well actually we can set this to true immediately
            } else {
                LevelData toRoomData = getRoomDataFromName(teleportToRoomName, level);
                Vector2 toRoomPos = toRoomData.Position;

                await Task.Delay(teleportDelayMilisecond);

                Vector2 playerOriginalPos = new (player.Position.X, player.Position.Y);

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
                Logger.Log(LogLevel.Info, "EndHelper/Main", $"Teleporting from {currentRoomData.Name} >> {teleportToRoomName}. Pos change: ({playerOriginalPos.X} {playerOriginalPos.Y} => {player.Position.X} {player.Position.Y}) - change by ({(toRoomPos - currentRoomPos).X} {(toRoomPos - currentRoomPos).Y}), Transition direction: ({transitionDirection.X} {transitionDirection.Y})");
            }
        }

        String getSwapRoomFromTemplateRoom(String templateRoomName)
        {
            List<List<String>> roomList = EndHelperModule.Session.roomSwapOrderList[gridID];
            for (int row = 1; row <= roomSwapTotalRow; row++)
            {
                for (int column = 1; column <= roomSwapTotalColumn; column++)
                {
                    if (templateRoomName == roomList[row-1][column-1])
                    {
                        //Found match at {row}{colu} - the swap room is {prefix}{row}{col}
                        return $"{roomSwapPrefix}{row}{column}";
                    }
                }
            }
            Logger.Log(LogLevel.Info, "EndHelper/main", $"getSwapRoomFromTemplateRoom - Unable to find {templateRoomName} - returning current room leveldata instead.");
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
            //This means the current room is not a swap room, so just return something invalid
            //If the template room can't be found it means modifyRoom was triggered from outside the swap grid
            //If the swap effect doesn't depend on the current room this will run with no issues!
            return "no such template room name...";
        }

        void swapEffects()
        {
            RoomModificationEventTrigger(gridID);

            if (flashEffect)
            {
                level.Flash(Color.White, drawPlayerOver: true);
            }
            if (!isSilent && player is not null)
            {
                level.Shake();

                if(EndHelperModule.Session.activateSoundEvent1[gridID] != "")
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
                    String roomNameAtPos = EndHelperModule.Session.roomSwapOrderList[gridID][row-1][column-1];
                    List<int> roomPos = GetPosFromRoomName(roomNameAtPos);
                    flagName += $"_{roomPos[0]}{roomPos[1]}";
                }
            }
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"getTransitionFlagName - Obtained flag name for {roomSwapPrefix} to be {flagName}");
            return flagName;
        }
        return succeedModify;
    }

    /// <summary>
    /// Returns the last 2 digits of the room name... or any string lol
    /// </summary>
    /// <param name="roomName"></param>
    /// <returns></returns>
    public static List<int> GetPosFromRoomName(String roomName)
    {
        int len = roomName.Length;
        int row = roomName[len - 2] - '0';
        int col = roomName[len - 1] - '0';

        return [row, col];
    }

    #endregion

    #region Misc Functions

    /// <summary>
    /// Compare if 2 2d lists are equal
    /// </summary>
    /// <param name="list1"></param>
    /// <param name="list2"></param>
    /// <returns></returns>
    static bool Are2LayerListsEqual<T>(List<List<T>> list1, List<List<T>> list2)
    {
        if(list1 == null || list2 == null)
        {
            return false;
        }

        return list1.Count == list2.Count &&
               list1.Zip(list2, (inner1, inner2) => inner1.SequenceEqual(inner2)).All(equal => equal);
    }

    // When will I finally learn a language that doesn't make deep cloning an absolute pain

    /// <summary>
    /// Lazy af deep cloning
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public static T DeepCopyJSON<T>(T input)
    {
        var jsonString = JsonSerializer.Serialize(input);

        return JsonSerializer.Deserialize<T>(jsonString);
    }

    /// <summary>
    /// Converts a Timespan into h:mm:ss string
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static string MinimalGameplayFormat(TimeSpan time)
    {
        if (time.TotalHours >= 1.0)
        {
            return (int)time.TotalHours + ":" + time.ToString("mm\\:ss");
        }
        return time.ToString("m\\:ss");
    }

    /// <summary>
    /// Buffers a VirtualButton for a few frames
    /// </summary>
    /// <param name="input"></param>
    /// <param name="frames"></param>
    public async static void consumeInput(VirtualButton input, int frames)
    {
        while (frames > 0)
        {
            input.ConsumePress();
            input.ConsumeBuffer();
            frames--;
            await Task.Delay((int)(Engine.DeltaTime * 1000));
        }
    }

    #endregion

}