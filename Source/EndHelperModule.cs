using AsmResolver.DotNet.Code.Cil;
using Celeste.Mod.EndHelper.Entities.DeathHandler;
using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using FMOD.Studio;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using NETCoreifier;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Celeste.DashSwitch;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using static Celeste.TrackSpinner;
using static On.Celeste.HeartGem;
using static On.Celeste.Level;

namespace Celeste.Mod.EndHelper;

public class EndHelperModule : EverestModule {

    #region Everest Stuff
    public static EndHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(EndHelperModuleSettings);
    public static EndHelperModuleSettings Settings => (EndHelperModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(EndHelperModuleSession);
    public static EndHelperModuleSession Session => (EndHelperModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(EndHelperModuleSaveData);
    public static EndHelperModuleSaveData SaveData => (EndHelperModuleSaveData)Instance._SaveData;

    public EndHelperModule()
    {
        Instance = this;
    }

    //Custom spritebank
    public static SpriteBank SpriteBank => Instance._CustomEntitySpriteBank;
    private SpriteBank _CustomEntitySpriteBank;

    #endregion

    #region Initialisation

    // Debug mode is annoying
    public enum SessionResetCause { None, LoadState, Debug, ReenterMap }
    public static SessionResetCause lastSessionResetCause = SessionResetCause.None; // Stores the previous cause of reset. Sometimes useful.
    public static int timeSinceSessionReset = 2;                                    // If == 1, correct for resets if needed. Starts from 2 so it does not cause a reset when loading!

    // Store information for room stats externally for them to persist through save states
    public static Dictionary<string, string> externalRoomStatDict_customName = new Dictionary<string, string> { };
    public static OrderedDictionary externalRoomStatDict_death = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_timer = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_rtatimer = new OrderedDictionary { };
    public static OrderedDictionary externalRoomStatDict_strawberries = new OrderedDictionary { };

    public static OrderedDictionary externalRoomStatDict_colorIndex = new OrderedDictionary { };
    public static Dictionary<string, bool> externalDict_pauseTypeDict = new Dictionary<string, bool> { };
    public static Dictionary<string, string> externalDict_fuseRoomRedirect = new Dictionary<string, string> { };

    public static List<string> externalRoomStatDict_firstClear_roomOrder = [];
    public static Dictionary<string, int> externalRoomStatDict_firstClear_death = [];
    public static Dictionary<string, long> externalRoomStatDict_firstClear_timer = [];
    public static Dictionary<string, long> externalRoomStatDict_firstClear_rtatimer = [];
    public static Dictionary<string, int> externalRoomStatDict_firstClear_strawberries = [];


    // Decreases till -ve, enables input if 0 and disables if +
    // Lets me disable, but ensure it gets re-enabled when I don't need it anymore
    internal static Utils_General.Countdown mInputDisableTimer = new Utils_General.Countdown(); 
    internal static Utils_General.Countdown DisableScreenTransitionMovementTimer = new Utils.Utils_General.Countdown();

    // Autosave timer
    public static TimeSpan autoSaveTimer = TimeSpan.Zero;
    public static void TryAutosave(Level level)
    {
        if (EndHelperModule.Settings.QOLTweaksMenu.AutosaveTime > 0 && autoSaveTimer.TotalMinutes >= EndHelperModule.Settings.QOLTweaksMenu.AutosaveTime)
        {
            level.AutoSave();
            autoSaveTimer = TimeSpan.Zero;
        }
    }

    public static bool integratingWithSSMQoL = false; // Cchange multiroom bino speed multiplier if used by this
    public static bool integratingWithFrostHelper = false; // Only to check for frost helper spinners
    public static bool integratingWithCommunualHelper = false; // Make some entities work better with death handler

    // Event Listener for when room modification occurs
    public static event EventHandler<RoomModificationEventArgs> RoomModificationEvent;
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


    private static ILHook Loadhook_Level_OrigTransitionRoutine;
    private static ILHook Loadhook_Refill_RefillRoutine;
    private static ILHook Loadhook_Input_GrabCheckGet;

    private static ILHook Loadhook_Player_DashCoroutine;
    private static ILHook Loadhook_Player_RedDashCoroutine;
    private static ILHook Loadhook_Player_OrigDie;
    private static ILHook Loadhook_PlayerDeadBody_DeathRoutine;

    public override void Load() {
        Everest.Events.AssetReload.OnReloadLevel += AssetReloadLevelFunc;
        Everest.Events.AssetReload.OnBeforeReload += ReloadBeginFunc;
        Everest.Events.AssetReload.OnAfterReload += ReloadCompleteFunc;
        Everest.Events.Level.OnEnter += EnterMapFunc;
        Everest.Events.Level.OnCreatePauseMenuButtons += CreatePauseMenuButtonsFunc;
        Everest.Events.Level.OnLoadEntity += OnLoadEntityFunc;
        Everest.Events.Player.OnSpawn += OnPlayerSpawnFunc;

        On.Monocle.Engine.Update += Hook_EngineUpdate;
        On.Celeste.Level.Update += Hook_LevelUpdate;
        On.Celeste.Level.UpdateTime += Hook_LevelUpdateTime;
        On.Celeste.LevelLoader.StartLevel += Hook_StartMapFromBeginning;
        On.Celeste.Level.Pause += Hook_Pause;
        On.Celeste.Session.GetSpawnPoint += Hook_SessionGetSpawnPoint;
        On.Celeste.Level.TransitionRoutine += Hook_TransitionRoutine;
        On.Monocle.Entity.Removed += Hook_EntityRemoved;
        On.Celeste.Glitch.Apply += Hook_GlitchEffectApply;
        On.Celeste.Level.CompleteArea_bool_bool_bool += Hook_CompleteArea;

        On.Celeste.Player.Update += Hook_OnPlayerUpdate;
        On.Celeste.Player.Die += Hook_OnPlayerDeath;
        On.Celeste.Player.IntroRespawnBegin += Hook_OnPlayerRespawn;
        IL.Celeste.PlayerDeadBody.Update += ILHook_PlayerDeadBodyUpdate;
        IL.Celeste.PlayerDeadBody.End += ILHook_PlayerDeadBodyEnd;
        MethodInfo ILOrigDie = typeof(Player).GetMethod("orig_Die", BindingFlags.Public | BindingFlags.Instance);
        Loadhook_Player_OrigDie = new ILHook(ILOrigDie, Hook_ILOrigDie);
        MethodInfo ILDeadBodyDeathRoutine = typeof(PlayerDeadBody).GetMethod("DeathRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        Loadhook_PlayerDeadBody_DeathRoutine = new ILHook(ILDeadBodyDeathRoutine, Hook_ILDeadBodyDeathRoutine);

        On.Celeste.AreaComplete.VersionNumberAndVariants += Hook_AreaCompleteVerNumVars;
        On.Celeste.OuiJournal.Update += Hook_JournalUpdate;
        On.Celeste.OuiJournal.Render += Hook_JournalRender;
        On.Celeste.OuiJournal.Close += Hook_JournalClose;

        On.Celeste.Editor.MapEditor.Update += Hook_UsingMapEditor;
        On.Celeste.Strawberry.Added += Hook_StrawberryAddedToLevel;
        On.Celeste.Strawberry.OnCollect += Hook_CollectStrawberry;
        On.Celeste.SpeedrunTimerDisplay.Render += Hook_SpeedrunTimerRender;
        IL.Celeste.GrabbyIcon.Update += ILHook_GrabbyIconUpdate;

        On.Celeste.Killbox.OnPlayer += Hook_KillboxKill;
        On.Celeste.CrystalStaticSpinner.InView += Hook_SpinnerInView;
        On.Celeste.Booster.PlayerDied += Hook_BoosterPlayerDied;
        On.Celeste.Solid.MoveHExact += Hook_SolidMoveHExact;
        On.Celeste.Solid.MoveVExact += Hook_SolidMoveVExact;

        On.Celeste.CassetteBlock.Awake += Hook_CassetteBlockAwake;
        On.Celeste.CassetteBlockManager.Awake += Hook_CassetteBlockManagerAwake;
        IL.Celeste.CassetteBlockManager.AdvanceMusic += ILHook_CassetteBlockManagerAdvMusic;

        MethodInfo ILRefillCoroutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        Loadhook_Refill_RefillRoutine = new ILHook(ILRefillCoroutine, Hook_IL_RefillRefillCoroutine);

        MethodInfo ILTransitionCoroutine = typeof(Level).GetMethod("orig_TransitionRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        Loadhook_Level_OrigTransitionRoutine = new ILHook(ILTransitionCoroutine, Hook_IL_OrigTransitionRoutine);

        On.Celeste.Player.DashBegin += Hook_DashBegin;
        IL.Celeste.Player.SuperBounce += ILHook_SuperBounce;
        IL.Celeste.Player.SideBounce += ILHook_SideBounce;
        MethodInfo ILDashCoroutine = typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        Loadhook_Player_DashCoroutine = new ILHook(ILDashCoroutine, Hook_IL_DashCoroutine);
        MethodInfo ILRedDashCoroutine = typeof(Player).GetMethod("RedDashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        Loadhook_Player_RedDashCoroutine = new ILHook(ILRedDashCoroutine, Hook_IL_RedDashCoroutine);

        MethodInfo ILInputGrabCheckGet = typeof(Input).GetProperty("GrabCheck").GetGetMethod();
        Loadhook_Input_GrabCheckGet = new ILHook(ILInputGrabCheckGet, Hook_IL_GrabCheckGet);

        SpeedrunToolIntegration.Load();
        SSMQoLIntegration.Load();
        FrostHelperIntegration.Load();
        ImGuiHelperIntegration.Load();
        QuantumMechanicsIntegration.Load();
        CollabUtils2Integration.Load();
    }

    // Unload the entirety of your mod's content. Free up any native resources.
    public override void Unload() {
        Everest.Events.AssetReload.OnReloadLevel -= AssetReloadLevelFunc;
        Everest.Events.AssetReload.OnBeforeReload -= ReloadBeginFunc;
        Everest.Events.AssetReload.OnAfterReload -= ReloadCompleteFunc;
        Everest.Events.Level.OnEnter -= EnterMapFunc;
        Everest.Events.Level.OnCreatePauseMenuButtons -= CreatePauseMenuButtonsFunc;
        Everest.Events.Level.OnLoadEntity -= OnLoadEntityFunc;
        Everest.Events.Player.OnSpawn -= OnPlayerSpawnFunc;

        On.Monocle.Engine.Update -= Hook_EngineUpdate;
        On.Celeste.Level.Update -= Hook_LevelUpdate;
        On.Celeste.Level.UpdateTime -= Hook_LevelUpdateTime;
        On.Celeste.LevelLoader.StartLevel -= Hook_StartMapFromBeginning;
        On.Celeste.Level.Pause -= Hook_Pause;
        On.Celeste.Session.GetSpawnPoint -= Hook_SessionGetSpawnPoint;
        On.Celeste.Level.TransitionRoutine -= Hook_TransitionRoutine;
        On.Celeste.Glitch.Apply -= Hook_GlitchEffectApply;
        On.Celeste.Level.CompleteArea_bool_bool_bool -= Hook_CompleteArea;

        On.Celeste.Player.Update -= Hook_OnPlayerUpdate;
        On.Celeste.Player.Die -= Hook_OnPlayerDeath;
        On.Celeste.Player.IntroRespawnBegin -= Hook_OnPlayerRespawn;
        IL.Celeste.PlayerDeadBody.Update -= ILHook_PlayerDeadBodyUpdate;
        IL.Celeste.PlayerDeadBody.End -= ILHook_PlayerDeadBodyEnd;
        Loadhook_Player_OrigDie?.Dispose(); Loadhook_Player_OrigDie = null;
        Loadhook_PlayerDeadBody_DeathRoutine?.Dispose(); Loadhook_PlayerDeadBody_DeathRoutine = null;

        On.Celeste.AreaComplete.VersionNumberAndVariants -= Hook_AreaCompleteVerNumVars;
        On.Celeste.OuiJournal.Update -= Hook_JournalUpdate;
        On.Celeste.OuiJournal.Render -= Hook_JournalRender;
        On.Celeste.OuiJournal.Close -= Hook_JournalClose;

        On.Celeste.Killbox.OnPlayer -= Hook_KillboxKill;
        On.Celeste.CrystalStaticSpinner.InView -= Hook_SpinnerInView;
        On.Celeste.Booster.PlayerDied -= Hook_BoosterPlayerDied;
        On.Celeste.Solid.MoveHExact -= Hook_SolidMoveHExact;
        On.Celeste.Solid.MoveVExact -= Hook_SolidMoveVExact;

        On.Celeste.Editor.MapEditor.Update -= Hook_UsingMapEditor;
        On.Celeste.Strawberry.Added -= Hook_StrawberryAddedToLevel;
        On.Celeste.Strawberry.OnCollect -= Hook_CollectStrawberry;
        On.Celeste.SpeedrunTimerDisplay.Render -= Hook_SpeedrunTimerRender;
        IL.Celeste.GrabbyIcon.Update -= ILHook_GrabbyIconUpdate;

        On.Celeste.CassetteBlock.Awake -= Hook_CassetteBlockAwake;
        On.Celeste.CassetteBlockManager.Awake -= Hook_CassetteBlockManagerAwake;
        IL.Celeste.CassetteBlockManager.AdvanceMusic -= ILHook_CassetteBlockManagerAdvMusic;

        Loadhook_Refill_RefillRoutine?.Dispose(); Loadhook_Refill_RefillRoutine = null;
        Loadhook_Level_OrigTransitionRoutine?.Dispose(); Loadhook_Level_OrigTransitionRoutine = null;

        On.Celeste.Player.DashBegin -= Hook_DashBegin;
        IL.Celeste.Player.SuperBounce -= ILHook_SuperBounce;
        IL.Celeste.Player.SideBounce -= ILHook_SideBounce;
        Loadhook_Player_DashCoroutine?.Dispose(); Loadhook_Player_DashCoroutine = null;
        Loadhook_Player_RedDashCoroutine?.Dispose(); Loadhook_Player_RedDashCoroutine = null;
        Loadhook_Input_GrabCheckGet?.Dispose(); Loadhook_Input_GrabCheckGet = null;

        SpeedrunToolIntegration.Unload();
        SSMQoLIntegration.Unload();
        FrostHelperIntegration.Unload();
        ImGuiHelperIntegration.Unload();
        QuantumMechanicsIntegration.Unload();
        CollabUtils2Integration.Unload();
    }

    // Optional, initialize anything after Celeste has initialized itself properly.
    public override void Initialize()
    { }

    // Optional, do anything requiring either the Celeste or mod content here.
    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
        _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/EndHelper/Sprites.xml");
    }
    #endregion

    #region Hooks

    public static bool reloadComplete;

    public static void AssetReloadLevelFunc(global::Celeste.Level level)
    {
        // Yeah this exists solely so reloading a map midway through it doesn't break.
        // Solely this or solely EnterMapFunc doesn't work.
        // Also these are both in timeSinceSessionReset > 2 checks so they don't infinite loop off each other
        // Can you tell that the code is made with glue and duct tape
        if (timeSinceSessionReset > 2)
        {
            timeSinceSessionReset = 0;
            lastSessionResetCause = SessionResetCause.ReenterMap;
        }
    }
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
        RoomStatisticsDisplayer.hideIfGoldenStrawberryEnabled = false;
        autoSaveTimer = TimeSpan.Zero;

        // If first time (not fromSaveData), check Hook_StartMap since it has access to level
        if (fromSaveData)
        {
            timeSinceSessionReset = 0;
            lastSessionResetCause = SessionResetCause.ReenterMap;

            // Death-Handler: If there's a full reset respawn point, set it to there when reentering the map. (otherwise, shenanigans ensue!!!)
            if (EndHelperModule.Session.firstFullResetPos != null)
            {
                session.RespawnPoint = EndHelperModule.Session.firstFullResetPos.Value;
            }

            // Clear old session data if the map is not the right map for some reason
            if (EndHelperModule.Session.roomStatDict_mapNameSide_Internal != GetMapNameSideInternal(session.Area))
            {
                Logger.Log(LogLevel.Warn, "EndHelper/main", $"EnterMapFunc: Session data mismatch: Current data is for {EndHelperModule.Session.roomStatDict_mapNameSide_Internal}, trying to load session data for {GetMapNameSideInternal(session.Area)}. Removing room stat data from the session!");
                Utils_JournalStatistics.ResetSessionDicts();
            }

            // Handle savedata dicts. This requires fromSaveData as that is AFTER the session is made.
            SetupRoomTrackerSaveDataDicts(session);

            String roomName = session.Level;
            roomName = RoomStatisticsDisplayer.GetEffectiveRoomName(roomName);
            // +1 death for save and quit. The reason why this is done here instead of everest onexit event is because
            // as far as I can tell saving and returning to lobby with collabutil saves the session before onexit runs.

            try
            {
                bool completedMapBefore = global::Celeste.SaveData.Instance.Areas_Safe[session.Area.ID].Modes[(int)session.Area.Mode].Completed;

                // This is done manually here to avoid touching RoomStatisticsDisplayer. Because this runs before the entity is loaded.
                EndHelperModule.Session.roomStatDict_death[roomName] = Convert.ToInt32(EndHelperModule.Session.roomStatDict_death[roomName]) + 1;
                String mapNameSide_Internal = GetMapNameSideInternal(session.Area);

                bool dealWithFirstCycle = EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != 0 && !completedMapBefore && EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal);
                if (dealWithFirstCycle && EndHelperModule.SaveData.mapDict_roomStat_firstClear_death.ContainsKey(mapNameSide_Internal) && EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal].ContainsKey(roomName))
                {
                    EndHelperModule.SaveData.mapDict_roomStat_firstClear_death[mapNameSide_Internal][roomName]++;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warn, "EndHelper/main", $"EnterMapFunc threw an error:\n{e}\nMost likely this is because the map can't be found, because the mod for it isn't loaded. If it isn't then there is an issue I have to fix :(");
            }
        }
    }

    private static int menuGiveUpLevelTimer = 0;
    private static int menuGiveUpLevelTimerMax = 1000;

    private static void CreatePauseMenuButtonsFunc(Level level, TextMenu menu, bool minimal)
    {
        // --- Prevent Accidental Quit ---
        switch (EndHelperModule.Settings.QOLTweaksMenu.PreventAccidentalQuit)
        {
            case QOLTweaks.PreventAccidentalQuitEnum.Disabled:
                menuGiveUpLevelTimerMax = 0; break;
            case QOLTweaks.PreventAccidentalQuitEnum.TimeSmall:
                menuGiveUpLevelTimerMax = (int)(0.2 * 10); break;
            case QOLTweaks.PreventAccidentalQuitEnum.TimeHalf:
                menuGiveUpLevelTimerMax = (int)(0.5 * 10); break;
            case QOLTweaks.PreventAccidentalQuitEnum.Time1:
                menuGiveUpLevelTimerMax = (int)(1 * 10); break;
            case QOLTweaks.PreventAccidentalQuitEnum.Time1Half:
                menuGiveUpLevelTimerMax = (int)(1.5 * 10); break;
            case QOLTweaks.PreventAccidentalQuitEnum.Time2:
                menuGiveUpLevelTimerMax = (int)(2 * 10); break;
            case QOLTweaks.PreventAccidentalQuitEnum.Time3:
                menuGiveUpLevelTimerMax = (int)(3 * 10); break;
            default:
                menuGiveUpLevelTimerMax = 0; break;
        }

        // Only do the pausing thing if enabled (menuGiveUpLevelTimerMax != 0) and just paused (menuGiveUpLevelTimer == menuGiveUpLevelTimerMax)
        if (menuGiveUpLevelTimer == menuGiveUpLevelTimerMax && menuGiveUpLevelTimerMax != 0)
        {
            PauseMenuButtonsFuncCooldown(level, menu, minimal);
        }

        // DeathHandler: Modify Retry to run SetManualReset
        Utils_DeathHandler.RetryButtonsManualCheck(level, menu);
    }

    private static EntityID? prevLoadedEntityID = null;
    private static bool OnLoadEntityFunc(global::Celeste.Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Loaded entity with id {level.Session.LevelData.Name} {entityData.ID} -- {entityData.Name}");
        prevLoadedEntityID = new EntityID(level.Session.LevelData.Name, entityData.ID);
        return false; // True makes it not load
    }

    private static void OnPlayerSpawnFunc(Player player)
    {
        // Spawn facing same direction as DeathHandlerRespawnPoint or DeathHandlerRespawnMarker.
        // Marker is last in priority, doesn't work that well when full resetting
        DeathHandlerRespawnPoint respawnPoint = player.CollideFirst<DeathHandlerRespawnPoint>();
        DeathHandlerThrowableRespawnPoint throwableRespawnPoint = player.CollideFirst<DeathHandlerThrowableRespawnPoint>();
        DeathHandlerRespawnMarker respawnMarker = player.CollideFirst<DeathHandlerRespawnMarker>();

        Facings? updateFacings = null;
        if (respawnPoint != null) updateFacings = respawnPoint.faceLeft ? Facings.Left : Facings.Right;
        else if (throwableRespawnPoint != null) updateFacings = throwableRespawnPoint.faceLeft ? Facings.Left : Facings.Right;
        else if (respawnMarker != null) updateFacings = respawnMarker.faceLeft ? Facings.Left : Facings.Right;

        if (updateFacings != null) player.Facing = updateFacings.Value;
    }

    async static void PauseMenuButtonsFuncCooldown(Level level, TextMenu menu, bool minimal)
    {
        // Temporarily Disable

        // Restart Chapter
        bool restartChapterInitialDisable = false;
        int restartChapterIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_restartarea"));
        if (restartChapterIndex != -1)
        {
            restartChapterInitialDisable = menu.Items[restartChapterIndex].Disabled;
            menu.Items[restartChapterIndex].Disabled = true;
        }

        // Return To Map
        bool returnToMapInitialDisable = false;
        int returnToMapIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_return"));
        if (returnToMapIndex != -1)
        {
            returnToMapInitialDisable = menu.Items[returnToMapIndex].Disabled;
            menu.Items[returnToMapIndex].Disabled = true;
        }

        // I have no idea what this is
        bool restartDemoInitialDisable = false;
        int restartDemoIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_restartdemo"));
        if (restartDemoIndex != -1)
        {
            restartDemoInitialDisable = menu.Items[restartDemoIndex].Disabled;
            menu.Items[restartDemoIndex].Disabled = true;
        }

        // Return To Lobby (but only if no save & quit)
        bool returnToLobbyDisable = false;
        int returnToLobbyIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("collabutils2_returntolobby"));
        if (returnToLobbyIndex != -1)
        {
            returnToLobbyDisable = menu.Items[returnToLobbyIndex].Disabled;
            menu.Items[returnToLobbyIndex].Disabled = true;
        }



        menuGiveUpLevelTimer--;
        while (menuGiveUpLevelTimer > 0 && menuGiveUpLevelTimer < menuGiveUpLevelTimerMax && level.Paused)
        {
            menuGiveUpLevelTimer--;
            await Task.Delay(100);
        }
        if (menuGiveUpLevelTimer == 0)
        {
            // Update Reenable stuff
            if (returnToMapIndex != -1)
            {
                menu.Items[returnToMapIndex].Disabled = returnToMapInitialDisable;
            }
            if (restartChapterIndex != -1)
            {
                menu.Items[restartChapterIndex].Disabled = restartChapterInitialDisable;
            }
            if (restartDemoIndex != -1)
            {
                menu.Items[restartDemoIndex].Disabled = restartDemoInitialDisable;
            }
            if (returnToLobbyIndex != -1)
            {
                menu.Items[returnToLobbyIndex].Disabled = returnToLobbyDisable;
            }

            // Update Menu
            menu.Update();
        }
    }

    static void SetupRoomTrackerSaveDataDicts(global::Celeste.Session session)
    {
        String mapNameSide_Internal = session.Area.GetSID();
        if (session.Area.Mode == AreaMode.BSide) { mapNameSide_Internal += "_B"; }
        else if (session.Area.Mode == AreaMode.CSide) { mapNameSide_Internal += "_C"; }

        // Move the current map to the front of the list, and trim size if exceeds max
        // The custom name dict will be the reference dict for size. (Just because it was added first.)

        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Being the stuff:");
        if (EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != 0)
        {
            // Not disabled.

            // Handles adding/ordering of roomStatsCustomNameDict. This won't be necessary for the other dicts.
            if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Contains(mapNameSide_Internal))
            {
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"Already contains {mapNameSide} => {EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Count} => {EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide]}. Setting, Removing then Readding:");
                if (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] is Dictionary<string, string>)
                {
                    EndHelperModule.Session.roomStatDict_customName = (Dictionary<string, string>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal];
                } 
                else
                {
                    EndHelperModule.Session.roomStatDict_customName = Utils_General.ConvertToStringDictionary((Dictionary<object, object>)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal]);
                }
                EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Remove(mapNameSide_Internal);
            }

            //Logger.Log(LogLevel.Info, "EndHelper/main", $"Adding {mapNameSide_Internal}.");
            EndHelperModule.SaveData.mapDict_roomStatCustomNameDict[mapNameSide_Internal] = EndHelperModule.Session.roomStatDict_customName;


            // Handle colorIndex dict
            if (!EndHelperModule.SaveData.mapDict_roomStat_colorIndex.ContainsKey(mapNameSide_Internal))
            {
                EndHelperModule.SaveData.mapDict_roomStat_colorIndex.Add(mapNameSide_Internal, []);
            }

            // Handles firstClear dicts. Just create them if they don't already exist.
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"Check if {mapNameSide_Internal} has all the first clear stuff:");
            if (EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.ContainsKey(mapNameSide_Internal))
            {
                // Do nothing. Already exist.
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"Yes. Already has it.");
            }
            else
            {

                //Logger.Log(LogLevel.Info, "EndHelper/main", $"No. Add {mapNameSide_Internal} to the relevant first clear lists!");
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.Add(mapNameSide_Internal, []);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_death.Add(mapNameSide_Internal, []);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer.Add(mapNameSide_Internal, []);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer.Add(mapNameSide_Internal, []);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries.Add(mapNameSide_Internal, []);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType.Add(mapNameSide_Internal, []);
            }
        }

        while (EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Count > EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount && EndHelperModule.Settings.RoomStatMenu.MenuTrackerStorageCount != -1)
        {
            String earliestMapNameSide = (String)EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.Cast<DictionaryEntry>().ElementAt(0).Key;
            //Logger.Log(LogLevel.Info, "EndHelper/main", $"Too many mapDicts: Removing the earliest: {earliestMapNameSide}");
            EndHelperModule.SaveData.mapDict_roomStatCustomNameDict.RemoveAt(0);

            try
            {
                // Try block In case these don't exist (which is possible if updating from older ver)
                EndHelperModule.SaveData.mapDict_roomStat_colorIndex.Remove(earliestMapNameSide);

                EndHelperModule.SaveData.mapDict_roomStat_firstClear_roomOrder.Remove(earliestMapNameSide);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_death.Remove(earliestMapNameSide);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_timer.Remove(earliestMapNameSide);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_rtatimer.Remove(earliestMapNameSide);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_strawberries.Remove(earliestMapNameSide);
                EndHelperModule.SaveData.mapDict_roomStat_firstClear_pauseType.Remove(earliestMapNameSide);
            } catch { }
        }
    }


    // This has to be here so you don't get softlocked if MInput is disabled in UI or something
    // ...somehow that can still happen. well.
    private static void Hook_EngineUpdate(On.Monocle.Engine.orig_Update orig, global::Monocle.Engine self, GameTime gameTime)
    {
        bool levelPause = false;
        if (Engine.Scene is Level level)
        {
            levelPause = level.FrozenOrPaused;
        }

        // MInput Timer. Note: Set above 3
        if (!levelPause) mInputDisableTimer.Update();
        if (mInputDisableTimer.TimeLeft >= 3) MInput.Disabled = true;
        if (mInputDisableTimer.TimeLeft == 1 || mInputDisableTimer.TimeLeft == 2) MInput.Disabled = false;
        orig(self, gameTime);

        // Scroll Input Timer. Set above 1
        if (Utils_General.scrollResetInputFrames.TimeLeft == 1) Utils_General.scrollInputFrames = 0;
        Utils_General.scrollResetInputFrames.Update();

        // Autosave Timer
        if (EndHelperModule.Settings.QOLTweaksMenu.AutosaveTime > 0)
        {
            autoSaveTimer += TimeSpanShims.FromSeconds((double)Engine.RawDeltaTime);
        }
    }

    private static void SessionResetFuncs(Level level)
    {
        //Utils_Shaders.LoadCustomShaders(forceReload: true);
        TryAutosave(level);

        if (EndHelperModule.Session.enableRoomSwapFuncs)
        {
            // This only exists so it updates when you respawn from debug. It umm still requires a transition/respawn to work lol
            // Also runs if SessionResetCause is ReenterMap
            Utils_RoomSwap.ReupdateAllRooms(level);

            if (lastSessionResetCause == SessionResetCause.Debug || lastSessionResetCause == SessionResetCause.ReenterMap)
            {
                // Check if require double reload - if room the player is in is in a grid
                String currentRoom = level.Session.LevelData.Name;
                foreach (String gridID in EndHelperModule.Session.roomSwapOrderList.Keys)
                {
                    String roomSwapPrefix = EndHelperModule.Session.roomSwapPrefix[gridID];
                    if (currentRoom.Contains(roomSwapPrefix))
                    {
                        // Is in one! Reload level again and break out of the loop.
                        // First load rearranges the room, but doesn't reload them, so it's just empty...
                        Utils_DeathHandler.ReloadRoomSeemlessly(level, Utils_DeathHandler.ReloadRoomSeemlesslyEffect.None);

                        break;
                    }
                }
            }
        }
        if (lastSessionResetCause != SessionResetCause.ReenterMap)
        {
            if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                roomStatDisplayer.ImportRoomStatInfo();
            }
        }

        if (EndHelperModule.Session.AllowDeathHandlerEntityChecks && lastSessionResetCause == SessionResetCause.Debug)
        {
            Utils_DeathHandler.ResetFullResetAndBypassBetweenRooms(level);
        }

        if (timeSinceSessionReset <= 1)
        {
            timeSinceSessionReset = 2;
        }
    }

    // Using player update instead of level update so nothing happens when the level is loading
    // Level update screws with the timeSinceSessionReset.
    internal static List<Action> actionWhenUnpaused = [];

    private static void Hook_LevelUpdate(On.Celeste.Level.orig_Update orig, global::Celeste.Level self)
    {
        Level level = self;

        if (!level.Paused)
        {
            foreach (Action action in actionWhenUnpaused)
            {
                if (action is not null)
                {
                    action.Invoke();
                }
            }
            actionWhenUnpaused.Clear();
        }

        // No Player Death Countdown (used by portable multiroom bino)
        if (!self.FrozenOrPaused) Utils_General.disablePlayerDeathCountdown.Update();

        // Disable Screen Transition Movement Timer
        if (!self.FrozenOrPaused && !self.Transitioning) DisableScreenTransitionMovementTimer.Update();

        // Session Reset Checker
        EndHelperModule.timeSinceSessionReset++;
        if (EndHelperModule.timeSinceSessionReset == 1)
        {
            SessionResetFuncs(self);
        }

        {
            // Increment timeSinceRespawn if player is alive. and also not paused
            if (level.Tracker.GetEntity<Player>() is Player player && !player.Dead && !player.JustRespawned && !level.FrozenOrPaused)
            {
                EndHelperModule.Session.framesSinceRespawn++;
            }
        }

        if (EndHelperModule.Settings.FreeMultiroomWatchtower.Button.Pressed && !level.FrozenOrPaused && !level.Transitioning)
        {
            Utils_MultiroomWatchtower.SpawnMultiroomWatchtower();
        }

        // Quick Restart Keybind
        bool forceCloseMenuAfter = false;
        {
            if (EndHelperModule.Settings.QuickRetry.Button.Pressed && level.Tracker.GetEntity<Player>() is Player player && !level.Paused && level.CanPause && level.CanRetry && !player.Dead && !level.InCutscene)
            {
                if (level.Session.GrabbedGolden)
                {
                    // Don't die if you have a golden. Just play a funny sfx instead.
                    player.Add(new SoundSource("event:/game/general/strawberry_laugh"));
                    return;
                } 
                else if (!player.Dead)
                {
                    Utils_DeathHandler.SetManualReset(level); // Set spawnpoint to full reset if it's used
                    level.Paused = false;
                    level.PauseMainMenuOpen = false;
                    Distort.GameRate = 1f;
                    Distort.Anxiety = 0f;
                    level.InCutscene = (level.SkippingCutscene = false);
                    foreach (LevelEndingHook component in level.Tracker.GetComponents<LevelEndingHook>().Cast<LevelEndingHook>())
                    {
                        component.OnEnd?.Invoke();
                    }
                    Utils_DeathHandler.nextFastReload = true;
                    PlayerDeadBody deadPlayer = player.Die(Vector2.Zero, evenIfInvincible: true);

                    // This sometimes fails if you spam retry and pauses the game instead with no way to unpause. No clue why.
                    try
                    { deadPlayer.ActionDelay = 0; }
                    catch (Exception)
                    { forceCloseMenuAfter = true; }
                }
            }
        }

        // Grab Recast Keybind
        if (level.Paused == false)
        {
            if (EndHelperModule.Settings.ToggleGrab.Button.Pressed)
            {
                EndHelperModule.Session.usedGameplayTweaks["grabrecast"] = true;

                Session.toggleifyEnabled = !Session.toggleifyEnabled;

                // Set to false first
                Session.GrabFakeTogglePressPressed = false;

                if (EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour == ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToTogglePress)
                { Session.GrabFakeTogglePressPressed = !Input.Grab; }

                if (EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour == ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToToggle)
                { Session.GrabFakeTogglePressPressed = Input.Grab; }
            }

            if (Input.Grab.Pressed && EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour != ToggleGrabSubMenu.ToggleGrabBehaviourEnum.NothingIfGrab)
            {
                // Convert this too, unless NothingIfGrab
                Session.GrabFakeTogglePressPressed = !Session.GrabFakeTogglePressPressed;
            }
            if (Session.toggleifyEnabled && Input.Grab.Pressed && EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour == ToggleGrabSubMenu.ToggleGrabBehaviourEnum.NothingIfGrab
                && global::Celeste.Settings.Instance.GrabMode == GrabModes.Toggle)
            {
                Input.UpdateGrab(); // If NothingIfGrab and Toggle Grab, LOCK THIS during toggleify. (Locking being just update twice)
            }
        }

        // Tick up Respawn Ripple Shader
        if (RespawnRipple.enableShader)
        {
            RespawnRipple.UpdateRipples(self);
        }

        orig(self);
        Utils_DeathHandler.Update(level);

        if (forceCloseMenuAfter)
        {
            level.Unpause();
        }

        if (!level.Transitioning && !level.FrozenOrPaused)
        {
            Utils_General.framesSinceEnteredRoom++;
        }
    }

    // This is here to ensure that (as much as possible) the times are synced
    // If added to the entity, it'll lag behind during the pause animation, and if in level update, it'll be ahead during state change
    public static int afkDurationFrames = 0;
    public static int inactiveDurationFrames = 0;
    public static bool allowIncrementRoomTimer = true;
    public static long previousSessionTime;
    public static long previousSaveDataTime;
    public static bool allowIncrementLevelTimer = true;

    // Store timers for RTA Timer
    private static long roomStatRtaTimeChecker_currTime;
    private static long roomStatRtaTimeChecker_timeChange;

    private static void Hook_LevelUpdateTime(On.Celeste.Level.orig_UpdateTime orig, global::Celeste.Level self)
    {
        // Update RTA Timer
        roomStatRtaTimeChecker_timeChange = System.DateTime.Now.Ticks - roomStatRtaTimeChecker_currTime;
        roomStatRtaTimeChecker_currTime = System.DateTime.Now.Ticks;

        if (roomStatRtaTimeChecker_timeChange < -3e7 || roomStatRtaTimeChecker_timeChange > 6e9) 
        {
            // Don't change if its too large (> 10mins) or negative (under -3s)
            Logger.Log(LogLevel.Warn, "EndHelper/main", $"Identified overly huge real-time change of {roomStatRtaTimeChecker_timeChange / 1e7}s. Ignoring change!");
            roomStatRtaTimeChecker_timeChange = 0;
        }
        ;

        Level level = self;
        previousSessionTime = level.Session.Time;
        AreaKey area = level.Session.Area;
        previousSaveDataTime = global::Celeste.SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].TimePlayed;

        {
            if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                // Timer will be increased here instead of the entity's update as otherwise pause menu appearing will freeze the timer temporarily
                String incrementRoomName = roomStatDisplayer.currentEffectiveRoomName;

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
                allowIncrementRoomTimer = true;

                if (!level.TimerStarted || level.TimerStopped || level.Completed)
                { allowIncrementRoomTimer = false; }

                if (allowIncrementRoomTimer && level.FrozenOrPaused && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.Pause ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseAFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseInactive ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseInactiveAFK
                ))
                {
                    allowIncrementRoomTimer = false;
                    EndHelperModule.Session.pauseTypeDict["Pause"] = true;
                }


                if (allowIncrementRoomTimer && inactiveDurationFrames >= 60 && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseInactive ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseInactiveAFK
                 ))
                {
                    allowIncrementRoomTimer = false;
                    if (level.TimerStarted && !level.TimerStopped && !level.Completed)
                    { EndHelperModule.Session.pauseTypeDict["Inactive"] = true; }
                }

                if (allowIncrementRoomTimer && afkDurationFrames >= 1800 && (
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.AFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseAFK ||
                    EndHelperModule.Settings.RoomStatMenu.PauseOption == RoomStatMenuSubMenu.RoomPauseScenarioEnum.PauseInactiveAFK
                ))
                {
                    allowIncrementRoomTimer = false;
                    EndHelperModule.Session.pauseTypeDict["AFK"] = true;
                }

                roomStatDisplayer.EnsureDictsHaveKey(level);

                if (allowIncrementRoomTimer)
                {
                    roomStatDisplayer.AddTimer();
                    roomStatDisplayer.AddRTATimer(roomStatRtaTimeChecker_timeChange);
                }
            }
        }
        orig(self);

        // Prevent level timer from increasing if pause/afk
        // Check if can increment time spent in room
        allowIncrementLevelTimer = true;
        if (allowIncrementLevelTimer && level.Paused && (
            EndHelperModule.Settings.PauseOptionLevel == LevelPauseScenarioEnum.Pause ||
            EndHelperModule.Settings.PauseOptionLevel == LevelPauseScenarioEnum.PauseAFK
        ))
        { 
            allowIncrementLevelTimer = false;
            EndHelperModule.Session.pauseTypeDict["LevelTimer_Pause"] = true;
        }

        if (allowIncrementLevelTimer && afkDurationFrames >= 1800 && (
            EndHelperModule.Settings.PauseOptionLevel == LevelPauseScenarioEnum.AFK ||
            EndHelperModule.Settings.PauseOptionLevel == LevelPauseScenarioEnum.PauseAFK
        ))
        { 
            allowIncrementLevelTimer = false;
            EndHelperModule.Session.pauseTypeDict["LevelTimer_AFK"] = true;
        }

        if (!allowIncrementLevelTimer)
        {
            level.Session.Time = previousSessionTime;
            global::Celeste.SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].TimePlayed = previousSaveDataTime;
        }
    }

    public static void Hook_OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
    {
        if (EndHelperModule.Settings.NeutralDrop.Button.Pressed && self.Holding != null)
        {
            EndHelperModule.Session.usedGameplayTweaks["neutraldrop"] = true;
            Input.MoveY.Value = 1;
            self.Throw();
        }
        if (EndHelperModule.Settings.Backboost.Button.Pressed && self.Holding != null)
        {
            EndHelperModule.Session.usedGameplayTweaks["backboost"] = true;
            if (self.Facing == Facings.Left)
            {
                self.Facing = Facings.Right;
            }
            else if (self.Facing == Facings.Right)
            {
                self.Facing = Facings.Left;
            }
            self.Throw();
        }

        if (EndHelperModule.Session.AllowDeathHandlerEntityChecks)
        {
            Utils_DeathHandler.PlayerUpdate(self);
        }

        orig(self);
    }

    public static PlayerDeadBody Hook_OnPlayerDeath(On.Celeste.Player.orig_Die orig, global::Celeste.Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
    {
        if (Utils_General.disablePlayerDeathCountdown.IsTicking)
        {
            return null; // Prevent death when countdown is not 0
        }

        // Check if actual death
        Level level = self.SceneAs<Level>();
        Session session = level.Session;
        bool invincibilityFlag = !evenIfInvincible && global::Celeste.SaveData.Instance.Assists.Invincible;
        if (!self.Dead && !invincibilityFlag && self.StateMachine.State != 18)
        {
            // Untoggle-ify if set to do so on death
            if (EndHelperModule.Settings.ToggleGrabMenu.UntoggleUponDeath) { Session.toggleifyEnabled = false; }
            EndHelperModule.Session.framesSinceRespawn = 0;

            if (!level.IsInBounds(self)) Utils_DeathHandler.ForceShortDeathCooldown(); // Reduce cooldown if player is out of bounds. Will definitely need to die soon!

            // Prevent death spam (if using seemless respawn).
            if (Utils_DeathHandler.deathCooldownFrames > 0) return null;
            Utils_DeathHandler.BeforePlayerDeath(self);
        }

        PlayerDeadBody origMethod = orig(self, direction, evenIfInvincible, registerDeathInStats);

        if (origMethod is not null)
        {
            DeathCountGate.OnPlayerDeathStatic(level);
        }

        return origMethod;
    }

    public static void ILHook_PlayerDeadBodyUpdate(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Replace the Input.MenuConfirm.Pressed check, so we can force it to true if using the quick retry keybind
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt(typeof(VirtualButton), "get_Pressed")
        ))
        {
            cursor.EmitDelegate<Func<bool, bool>>(ILHook_PlayerDeadBodyUpdate_ReplacementMenuConfirmPressed);
        }
    }
    private static bool ILHook_PlayerDeadBodyUpdate_ReplacementMenuConfirmPressed(bool origPressed)
    {
        bool forceFast = Utils_DeathHandler.CheckPlayerNextFastReload();
        return forceFast || origPressed; // Same as original, unless forceFast
    }

    public static void ILHook_PlayerDeadBodyEnd(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Skip Death Wipe. And by skip I mean delete self (and return to be safe).
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdloc0(),
            instr => instr.MatchLdcI4(0),
            instr => instr.MatchLdarg0(),
            instr => instr.MatchLdfld(out _),
            instr => instr.MatchLdcI4(0),
            instr => instr.MatchCallvirt<Level>("DoScreenWipe")
        ))
        {
            ILLabel resume = cursor.DefineLabel();
            cursor.EmitDelegate<Func<bool>>(Utils_DeathHandler.CheckPlayerDeathSkipRemovePlayer);
            cursor.Emit(OpCodes.Brfalse_S, resume);

            // End!
            cursor.Emit(OpCodes.Ldarg_0);  // push PlayerDeadBody (this) onto the stack
            cursor.EmitDelegate<Action<PlayerDeadBody>>(Utils_DeathHandler.OnPlayerDeathSkipRemovePlayer);
            cursor.Emit(OpCodes.Ret); // Return, for safety

            cursor.MarkLabel(resume); // This continues to DoScreenWipe
        }
    }

    public static void Hook_ILOrigDie(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Match session.Deaths++, where I run stat menu add death
        if (cursor.TryGotoNext(MoveType.After,
            //instr => instr.MatchDup(),
            //instr => instr.MatchLdfld<Session>("Deaths"),
            //instr => instr.MatchLdcI4(out _),
            //instr => instr.MatchAdd(),
            instr => instr.MatchStfld(typeof(global::Celeste.Session),"Deaths")
        ))
        {
            cursor.EmitDelegate(ILRunOnPlayerDeath);
        }

        // Skip losing all followers
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Player>("Leader"),
            instr => instr.MatchCallvirt<Leader>("LoseFollowers")
        ))
        {
            ILLabel end = cursor.DefineLabel();

            cursor.EmitDelegate<Func<bool>>(Utils_DeathHandler.CheckPlayerDeathSkipLoseFollowers);
            cursor.Emit(OpCodes.Brtrue_S, end); // Skip lose followers if true

            cursor.GotoNext(MoveType.After,
                instr => instr.MatchCallvirt<Leader>("LoseFollowers")
            );
            cursor.MarkLabel(end);
        }

        // Disable first half of Death Screen Shake: Celeste.Level::Shake(float32)
        if (cursor.TryGotoNext(MoveType.Before,
            //instr => instr.MatchLdarg0(),
            //instr => instr.MatchLdfld<Player>("level"),
            //instr => instr.MatchLdcR4(0.3f), // After loading the shake intensity (0.3f)
            instr => instr.MatchCallvirt<Level>("Shake")
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }

        // Skip the player remove - base.Scene.Remove(this)
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCall(typeof(Entity), "get_Scene"),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt(typeof(Scene), "Remove")
        ))
        {
            // Define labels for skipping
            ILLabel skipLabel = cursor.DefineLabel();
            ILLabel endLabel = cursor.DefineLabel();

            cursor.EmitDelegate<Func<bool>>(Utils_DeathHandler.CheckPlayerDeathSkipRemovePlayer); // Skip Check: true = skip, false = run original
            
            cursor.Emit(OpCodes.Brtrue_S, skipLabel); // If true (skip), jump to skipLabel (BEFORE going through above instructions)
            cursor.GotoNext(MoveType.After,
                instr => instr.MatchCallvirt(typeof(Scene), "Remove") // Advance past above instructions
            );
            cursor.Emit(OpCodes.Br_S, endLabel); // If not true (no skip), jump to endLabel (AFTER going through above instructions)

            cursor.MarkLabel(skipLabel); // Mark skipLabel (Land here if skip)

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(Utils_DeathHandler.OnPlayerDeathSkip); // Run this if skip

            cursor.MarkLabel(endLabel); // Mark endLabel (Land here if no skip. This skips the run-if-skip function.)
        }
    }

    public static void ILRunOnPlayerDeath()
    {
        if (Engine.Scene is Level level)
        {
            level.Tracker.GetEntity<RoomStatisticsDisplayer>()?.AddDeath();
            foreach (ConditionalBirdTutorial conditionalBirdTutorial in level.Tracker.GetEntities<ConditionalBirdTutorial>())
            {
                conditionalBirdTutorial.UpdateConditionTracking_Death();
            }
        }
    }

    public static void Hook_ILDeadBodyDeathRoutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Disable 2nd half of Death Screen Shake: Celeste.Level::Shake(float32)
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchCallvirt<Level>("Shake")
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    public static void Hook_OnPlayerRespawn(On.Celeste.Player.orig_IntroRespawnBegin orig, global::Celeste.Player self)
    {
        Level level = self.SceneAs<Level>();

        //Update the room-swap rooms. This is kind of here as a failsafe, and also otherwise warping with debug mode permamently empty the swap rooms.
        Utils_RoomSwap.ReupdateAllRooms();

        orig(self);
        if (Utils_DeathHandler.seemlessRespawn == SeemlessRespawnEnum.EnabledInstant)
        {
            self.StateMachine.State = 0;
            self.Sprite.Scale = new Vector2(1.5f, 0.5f);
        }
        if (Utils_DeathHandler.seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || Utils_DeathHandler.playerHasDeathBypass)
        {
            self.StateMachine.State = 0;
            self.Sprite.Scale = new Vector2(1f, 1f);
        }

        Utils_DeathHandler.AfterPlayerDeath(self);

        // Autosave
        TryAutosave(level);
    }

    private static Vector2 Hook_SessionGetSpawnPoint(On.Celeste.Session.orig_GetSpawnPoint orig, global::Celeste.Session self, Vector2 from)
    {
        if (Engine.Scene is Level level && 
            (level.Tracker.GetEntity<DeathHandlerRespawnPoint>() is not null || level.Tracker.GetEntity<DeathHandlerThrowableRespawnPoint>() is not null))
        {
            // In case other mods use this function, don't just replace orig directly
            // So do this roundabout thing where we add extra points, check, then remove the extra points
            List<Vector2> deathHandlerSpawnPoints = [];
            foreach (DeathHandlerRespawnPoint deathHandlerSpawnPointEntity in level.Tracker.GetEntities<DeathHandlerRespawnPoint>())
            {
                if (deathHandlerSpawnPointEntity.disabled == false)
                {
                    Vector2 deathHandlerSpawnPointPos = deathHandlerSpawnPointEntity.entityPosSpawnPoint;
                    deathHandlerSpawnPoints.Add(deathHandlerSpawnPointPos);
                }
            }
            foreach (DeathHandlerThrowableRespawnPoint deathHandlerThrowableSpawnPointEntity in level.Tracker.GetEntities<DeathHandlerThrowableRespawnPoint>())
            {
                if (deathHandlerThrowableSpawnPointEntity.disabled == false)
                {
                    Vector2 deathHandlerThrowableSpawnPointPos = deathHandlerThrowableSpawnPointEntity.entityPosSpawnPoint;
                    deathHandlerSpawnPoints.Add(deathHandlerThrowableSpawnPointPos);
                }
            }

            self.LevelData.Spawns.AddRange(deathHandlerSpawnPoints);                    // Add everything in deathHandlerSpawnPoints to LevelData.Spawns
            Vector2 closestSpawnPos = orig(self, from);                                 // Do the usual checks
            foreach (Vector2 ourSpawnPointPos in deathHandlerSpawnPoints)
            {
                self.LevelData.Spawns.Remove(ourSpawnPointPos);                         // Now remove the stuff we added
            }
            return closestSpawnPos;
        }
        else
        {
            return orig(self, from);
        }
    }

    private static IEnumerator Hook_TransitionRoutine(
        On.Celeste.Level.orig_TransitionRoutine orig, global::Celeste.Level self, global::Celeste.LevelData next, Vector2 direction
    )
    {
        Utils_General.framesSinceEnteredRoom = 0;
        yield return new SwapImmediately(orig(self, next, direction));
        DeathCountGate.OnTransitionStatic(self);

        if (EndHelperModule.Session.AllowDeathHandlerEntityChecks) Utils_DeathHandler.ResetFullResetAndBypassBetweenRooms(self); // AFTER room change
    }

    private static void Hook_StartMapFromBeginning(On.Celeste.LevelLoader.orig_StartLevel orig, global::Celeste.LevelLoader self)
    {
        Level level = self.Level;
        level.Add(new RoomStatisticsDisplayer(level));
        inactiveDurationFrames = 60; // For maps starting with cutscene. If without, would be set to 0 immediately. Not even sure if this works lol


        // Set up the save data custom name dictionaries if starting a map from the beginning
        SetupRoomTrackerSaveDataDicts(level.Session);

        //Utils_Shaders.LoadCustomShaders(forceReload: true);

        orig(self);
    }

    private static void Hook_Pause(On.Celeste.Level.orig_Pause orig, global::Celeste.Level self, int startIndex, bool minimal, bool quickReset)
    {
        Level level = self;

        if (quickReset)
        {
            { 
                if (EndHelperModule.Settings.QOLTweaksMenu.DisableQuickRestart ||
                (EndHelperModule.Settings.QuickRetry.Button.Pressed && level.Tracker.GetEntity<Player>() is Player player && !level.Paused && level.CanPause && level.CanRetry))
                {
                    // Do not quick reset if you are quick dying (or if disabled)
                    return;
                }
            }
        } 
        else
        {
            if (!level.Paused)
            {
                // This is here so going backwards in a menu doesn't cause the timer to reset
                menuGiveUpLevelTimer = menuGiveUpLevelTimerMax;
            }
        }
        orig(self, startIndex, minimal, quickReset);
    }

    public static void Hook_EntityRemoved(On.Monocle.Entity.orig_Removed orig, global::Monocle.Entity self, global::Monocle.Scene scene)
    {
        if (scene is Level && EndHelperModule.Session is not null && 
            EndHelperModule.Session.AllowDeathHandlerEntityChecks && self.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass
            && self is not Player)
        {
            DeathBypass.entityIDDisappearUntilFullReset.Add(deathBypassComponent.entityID);
        }
        orig(self, scene);
    }

    public static void Hook_GlitchEffectApply(On.Celeste.Glitch.orig_Apply orig, VirtualRenderTarget source, float timer, float seed, float amplitude)
    {
        // Does not work if applied at the start/end of level render
        orig(source, timer, seed, amplitude);
        if (Engine.Scene is Level level)
        {
            //Utils_Shaders.ApplyShaders(level);
        }
    }

    private static ScreenWipe Hook_CompleteArea(On.Celeste.Level.orig_CompleteArea_bool_bool_bool orig, global::Celeste.Level level, bool spotlightWipe, bool skipScreenWipe, bool skipCompleteScreen)
    {
        if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
        {
            level.RegisterAreaComplete();
            void action() { orig(level, spotlightWipe, skipScreenWipe, skipCompleteScreen); }
            actionWhenUnpaused.Add(action);
            return null;
        }
        else
        {
            return orig(level, spotlightWipe, skipScreenWipe, skipCompleteScreen);
        }
    }

    private static void Hook_AreaCompleteVerNumVars(On.Celeste.AreaComplete.orig_VersionNumberAndVariants orig, string version, float ease, float alpha)
    {
        try
        {
            bool showFullBlender = false;
            List<string> iconList = [];

            // Check each gameplay tweak, and add relevant GFX / set showFullBlender to true if required
            Dictionary<string, bool> tweakList = EndHelperModule.Session.usedGameplayTweaks;

            // dashredirect backboost neutraldrop | seemlessrespawn_keepstate seemlessrespawn_minor grabrecast

            if (tweakList["dashredirect"]) { iconList.Add(":EndHelper/endscreen_dashredirect:"); showFullBlender = true; }
            if (tweakList["backboost"]) { iconList.Add(":EndHelper/endscreen_backboost:"); showFullBlender = true; }
            if (tweakList["neutraldrop"]) { iconList.Add(":EndHelper/endscreen_neutraldrop:"); showFullBlender = true; }

            if (tweakList["seemlessrespawn_minor"])
            {
                if (tweakList["seemlessrespawn_keepstate"]) { iconList.Add(":EndHelper/endscreen_seemlessrespawn_keepstate:"); showFullBlender = true; }
                else iconList.Add(":EndHelper/endscreen_seemlessrespawn_minor:");
            }
            if (tweakList["grabrecast"]) iconList.Add(":EndHelper/endscreen_grabrecast:");

            // Show blender if at least 1 thing to show!
            if (iconList.Count >= 1)
            {
                // Get and draw the correct blender (half or full)
                MTexture blenderTexture = showFullBlender ? GFX.Gui["misc/EndHelper/endscreenblender_full"] : GFX.Gui["misc/EndHelper/endscreenblender_half"];
                Vector2 blenderPos = new Vector2(1820f - 1f + 300f * (1f - Ease.CubeOut(ease)), 1020f - 48f);

                if (global::Celeste.SaveData.Instance.AssistMode || global::Celeste.SaveData.Instance.VariantMode)
                { blenderPos.Y -= 48; }

                FieldInfo versionOffsetField = typeof(AreaComplete).GetField("versionOffset", BindingFlags.Static | BindingFlags.NonPublic);
                float versionOffset = (float)versionOffsetField.GetValue(null);
                if (Engine.Scene is Level) versionOffset += -32f; // lazy as heck collabui check
                blenderPos.Y += versionOffset;
                //Logger.Log(LogLevel.Info, "EndHelper/main", $"why is the versionoffset not publically gettable: {versionOffset}");

                blenderTexture.DrawJustified(blenderPos + new Vector2(0f, -8f), new Vector2(0.5f, 1f), Color.White, 0.6f);

                // Draw the icons
                Vector2 iconPos = blenderPos + new Vector2(0, -120);
                string fullIconString = "";
                int nextLineCounter = 1;
                foreach (string iconString in iconList)
                {
                    if (nextLineCounter > 2)
                    {
                        nextLineCounter = 1;
                        ActiveFont.DrawOutline(fullIconString, iconPos, new Vector2(0.5f, 0f), Vector2.One * 5f, Color.White, 1f, Color.Black);
                        fullIconString = "";
                        iconPos += new Vector2(0, -50);
                    }
                    fullIconString += iconString;
                    nextLineCounter++;
                }
                if (fullIconString != "")
                {
                    ActiveFont.DrawOutline(fullIconString, iconPos, new Vector2(0.5f, 0f), Vector2.One * 5f, Color.White, 1f, Color.Black);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, "EndHelper/main", $"Error when showing endscreen gameplay tweaks: {e}");
        }

        orig(version, ease, alpha);
    }


    private static void Hook_JournalUpdate(On.Celeste.OuiJournal.orig_Update orig, global::Celeste.OuiJournal self)
    {
        Utils_JournalStatistics.Update(self);
        if (!Utils_JournalStatistics.journalStatisticsGuiOpen)
        {
            orig(self);
        }
    }

    private static void Hook_JournalRender(On.Celeste.OuiJournal.orig_Render orig, global::Celeste.OuiJournal self)
    {
        orig(self);

        // Specifically for in overworld
        if (Engine.Scene is Overworld)
        {
            if (RoomStatisticsDisplayer.tooltipDuration > -60)
            {
                ActiveFont.DrawOutline(RoomStatisticsDisplayer.tooltipText, new Vector2(100, 950), Vector2.Zero, Vector2.One, Color.White * alpha, 2, Color.Black * alpha);
                RoomStatisticsDisplayer.tooltipDuration += -4;
            }
            if (RoomStatisticsDisplayer.tooltipDuration > 10 && RoomStatisticsDisplayer.alpha < 1) { RoomStatisticsDisplayer.alpha += 0.1f; }
            if (RoomStatisticsDisplayer.tooltipDuration < 0 && RoomStatisticsDisplayer.alpha > 0) { RoomStatisticsDisplayer.alpha -= 0.03f; }
        }

        Utils_JournalStatistics.Render();
    }

    private static void Hook_JournalClose(On.Celeste.OuiJournal.orig_Close orig, global::Celeste.OuiJournal self)
    {
        orig(self);
        Utils_JournalStatistics.journalStatisticsGuiOpen = false;
        Utils_JournalStatistics.journalOpen = false;
    }

    private static void Hook_IL_RefillRefillCoroutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Disable Refill Screen Shake: Celeste.Level::Shake(float32)
        if (cursor.TryGotoNext(MoveType.Before,
            //instr => instr.MatchLdloc1(),
            //instr => instr.MatchLdfld<Refill>("level"),
            //instr => instr.MatchLdcR4(0.3f) // After loading the shake intensity (0.3f)
            instr => instr.MatchCallvirt<Level>("Shake")
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    private static void Hook_IL_OrigTransitionRoutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // --- Prevent movement during transition ---

        // Bottom of screen Y-Movement
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt(typeof(Rectangle), "get_Bottom")
        ))
        {
            cursor.EmitDelegate<Func<int, int>>(ShouldRunLoopInt);
        }

        // Transitionary Movement
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt(typeof(Level), "IsInBounds")
        ))
        {
            cursor.EmitDelegate<Func<bool, bool>>(ShouldRunLoopBool);
        }

        // Camera (and possibly other stuff) Movement
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt(typeof(Player), "TransitionTo")
        ))
        {
            cursor.EmitDelegate<Func<bool, bool>>(ShouldRunLoopBool); // Push 'true' to pretend the transition was successful
        }


        // --- Replace Session.LevelData.Spawns.ClosestTo(to) with Session.GetSpawnPoint(from) ---
        // by replace i mean just add a line immediately after it
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchStfld<Session>("RespawnPoint")
        ))
        {
            cursor.EmitDelegate(Utils_DeathHandler.ReplaceTransitionRoutineGetSpawnpointWithTheActualFunction);
        }
    }

    static int ShouldRunLoopInt(int bottom)
    {
        if (!DisableScreenTransitionMovementTimer.IsTicking)
        {
            return bottom; // run first loop
        }
        else
        {
            return int.MaxValue; // make Y < bottom so bge fails → skip
        }
    }
    static bool ShouldRunLoopBool(bool originalBool)
    {
        if (!DisableScreenTransitionMovementTimer.IsTicking)
        {
            return originalBool; // run first loop
        }
        else
        {
            return true; // make Y < bottom so bge fails → skip
        }
    }

    private static Vector2 preRedirectDashDir = Vector2.Zero;
    private static void Hook_DashBegin(On.Celeste.Player.orig_DashBegin orig, global::Celeste.Player self)
    {
        preRedirectDashDir = Input.LastAim;
        orig(self);
    }

    private static void ILHook_SuperBounce(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Remove Vertical Spring (and possibly other things?) screen shake.
        // Replaces the default shake intensity (0.2f) with 0 if disabled!
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchCallvirt<Level>("DirectionalShake")
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    private static void ILHook_SideBounce(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Remove Sideway Spring (and possibly other things?) screen shake.
        // Replaces the default shake intensity (0.2f) with 0 if disabled!
        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchCallvirt<Level>("DirectionalShake")
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    private static void Hook_IL_DashCoroutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Prevent Demos: Override redirects
        // Move the cursor to right after lastAim is obtained, but before lastAim is set.
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("lastAim")))
        {
            cursor.EmitDelegate(PreventDownRedirect); // Redirects the aim if necessary. My first IL hook :lilysass:
        }

        // Remove Dash Screen Shake
        // Replaces the default shake intensity (0.2f) with 0 if disabled!
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdloc1(),
            instr => instr.MatchLdfld<Player>("DashDir"),
            instr => instr.MatchLdcR4(0.2f) // After loading the shake intensity (0.2f)
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    private static void Hook_IL_RedDashCoroutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Remove Red Booster Dash Screen Shake
        // Replaces the default shake intensity (0.2f) with 0 if disabled!
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdloc1(),
            instr => instr.MatchLdfld<Player>("DashDir"),
            instr => instr.MatchLdcR4(0.2f) // After loading the shake intensity (0.2f)
        ))
        {
            // Replace the current shake intensity with 0.0f
            cursor.EmitDelegate<Func<float, float>>(GetScreenShakeReplacementIntensity);
        }
    }

    private static float GetScreenShakeReplacementIntensity(float initialIntensity)
    {
        // Replace the incoming intensity value with 0.0f
        float outIntensity = EndHelperModule.Settings.QOLTweaksMenu.DisableFrequentScreenShake ? 0.0f : initialIntensity;
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"Something related to dash intensity happened. {initialIntensity} >> {outIntensity}");
        return outIntensity;
    }

    public static Vector2 PreventDownRedirect(Vector2 redirectedVector)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"pre-redirected vector: {preRedirectDashDir}");
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"redirected vector: {redirectedVector}");
        //Logger.Log(LogLevel.Info, "EndHelper/main", $"current aim: {Input.Aim.PreviousValue}");

        Vector2 currentAim = Input.Aim.PreviousValue;
        ConvertDemoEnum usedConvertDemoSetting = EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo != null ? EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo.Value : EndHelperModule.Settings.GameplayTweaksMenu.ConvertDemo;

        // If down direction was redirected to neutral, add it back during redirection //global::Celeste.SaveData.Instance.Assists.Invincible
        if (usedConvertDemoSetting != GameplayTweaks.ConvertDemoEnum.Disabled && !global::Celeste.SaveData.Instance.Assists.ThreeSixtyDashing
            && preRedirectDashDir.Y > 0.01 && redirectedVector.Y == 0)
        {
            if (usedConvertDemoSetting != ConvertDemoEnum.Disabled && EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo == null)
            {
                EndHelperModule.Session.usedGameplayTweaks["dashredirect"] = true;
            }

            redirectedVector.Y = preRedirectDashDir.Y;

            // If this happens, last aim will probably be left/right. Check current aim to see if it should be downwards or diagonal, unless forced diagonal
            if (usedConvertDemoSetting == GameplayTweaks.ConvertDemoEnum.EnabledNormal && currentAim.X == 0)
            {
                redirectedVector.X = 0;
            }

            redirectedVector = redirectedVector.EightWayNormal(); // Normalise lol
        }

        //Logger.Log(LogLevel.Info, "EndHelper/main", $"final vector: {redirectedVector}");
        return redirectedVector;
    }

    private static void Hook_IL_GrabCheckGet(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Move to where the switch starts
        if (cursor.TryGotoNext(instr => instr.MatchSwitch(out ILLabel[] switchLabel)))
        {
            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchRet()))
            { 
                cursor.EmitDelegate<Func<bool, bool>>(ToggleifyModifyGrab); // Loop across all 3 grab type checks
                cursor.Index++;
            }
        }
    }

    public static bool ToggleifyModifyGrab(bool grabbing)
    {
        GrabModes grabMode = global::Celeste.Settings.Instance.GrabMode;
        bool pressedGrab = Input.Grab.Pressed;

        if (Session.toggleifyEnabled)
        {
            switch (EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour)
            {
                case ToggleGrabSubMenu.ToggleGrabBehaviourEnum.UntoggleOnGrab:
                    if (pressedGrab)  { Session.toggleifyEnabled = false; } 
                    else { grabbing = !grabbing; }
                    Session.GrabFakeTogglePressPressed = false;
                    Session.ToggleGrabRanNothing = false;
                    break;

                case ToggleGrabSubMenu.ToggleGrabBehaviourEnum.InvertDuringGrab:
                    grabbing = !grabbing; // Simplest behaviour lol
                    Session.GrabFakeTogglePressPressed = false;
                    Session.ToggleGrabRanNothing = false;
                    break;

                case ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToToggle:
                    if (!Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Hold) { grabbing = false; } // Set grabbing dependent on GrabFakeTogglePressPressed
                    else if (Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Hold) { grabbing = true; }
                    else if (!Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Invert) { grabbing = true; }
                    else if (Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Invert) { grabbing = false; }
                    Session.ToggleGrabRanNothing = false;
                    // Nothing happens if you're using toggle grab, because like, yeah sure i change the toggle grab into a toggle grab
                    break;

                case ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToTogglePress: // Same as above (Press handled in level update)
                    if (!Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Hold) { grabbing = false; }  // Set grabbing dependent on GrabFakeTogglePressPressed
                    else if (Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Hold) { grabbing = true; }
                    else if (!Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Invert) { grabbing = true; }
                    else if (Session.GrabFakeTogglePressPressed && grabMode == GrabModes.Invert) { grabbing = false; }
                    Session.ToggleGrabRanNothing = false;
                    break;

                case ToggleGrabSubMenu.ToggleGrabBehaviourEnum.NothingIfGrab:
                    if (grabMode == GrabModes.Hold) { grabbing = true; }
                    else if (grabMode == GrabModes.Invert) { grabbing = false; }
                    else if (grabMode == GrabModes.Toggle) { 
                        if (Session.ToggleGrabRanNothing == false)
                        {
                            Session.GrabFakeTogglePressPressed = !grabbing; // Store OPPOSITE grab mode before toggle-ifier in here
                            Session.ToggleGrabRanNothing = true;
                        } 
                        else
                        {
                            grabbing = Session.GrabFakeTogglePressPressed;
                        }
                    }
                    break;
                default:
                    Session.GrabFakeTogglePressPressed = false;
                    Session.ToggleGrabRanNothing = false;
                    break;
            }
        } else
        {
            Session.ToggleGrabRanNothing = false;
            Session.GrabFakeTogglePressPressed = false;
        }

        return grabbing;
    }

    public static void Hook_KillboxKill(On.Celeste.Killbox.orig_OnPlayer orig, global::Celeste.Killbox self, global::Celeste.Player player)
    {
        if (EndHelperModule.Session.AllowDeathHandlerEntityChecks && !global::Celeste.SaveData.Instance.Assists.Invincible) Utils_DeathHandler.SetNextRespawnFullReset(player.level, true);
        orig(self, player);
    }

    public static bool Hook_SpinnerInView(On.Celeste.CrystalStaticSpinner.orig_InView orig, global::Celeste.CrystalStaticSpinner self)
    {
        bool inView = orig(self);

        if (!inView && Utils_DeathHandler.spinnerAltInView)
        {
            // Check if within screen bounds
            if (Utils_DeathHandler.oldCameraRectInflate.Contains((int)(self.Position.X + self.Width / 2), (int)(self.Position.Y + self.Height / 2)))
            {
                inView = true;
            }
        }

        return inView;
    }
    public static void Hook_BoosterPlayerDied(On.Celeste.Booster.orig_PlayerDied orig, global::Celeste.Booster self)
    {
        orig(self);
        // Booster forces tag to be 1 (idk what 1 is but its not global) upon death. I don't want that to happen with deathbypass
        // Otherwise the booster kind of just disappears forever
        if (self.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass)
        {
            self.AddTag(Tags.Global);
        }
    }

    public static void Hook_SolidMoveHExact(On.Celeste.Solid.orig_MoveHExact orig, global::Celeste.Solid self, int move)
    {
        // This fixes a very specific crash - with deathhandler, if a solid has a staticmover that has a subentity, the subentity may have a null scene on reset.
        self.Scene ??= Engine.Scene as Level;
        orig(self, move);
    }
    public static void Hook_SolidMoveVExact(On.Celeste.Solid.orig_MoveVExact orig, global::Celeste.Solid self, int move)
    {
        // Same as above
        self.Scene ??= Engine.Scene as Level;
        orig(self, move);
    }


    public static void Hook_UsingMapEditor(On.Celeste.Editor.MapEditor.orig_Update orig, global::Celeste.Editor.MapEditor self)
    {
        timeSinceSessionReset = 0;
        lastSessionResetCause = SessionResetCause.Debug;
        orig(self);
    }

    // Component for strawberries to store its home room. This was probably not necessary...
    public class HomeRoom(String roomName) : Component(true, false)
    {
        public string roomName = roomName;
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
        if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
        {
            roomStatDisplayer.AddStrawberry(self);
        }
        orig(self);
    }

    private static void Hook_SpeedrunTimerRender(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, global::Celeste.SpeedrunTimerDisplay self)
    {
        bool renderTimer = true;
        if (Engine.Scene is Level level && level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer && roomStatDisplayer.statisticsGuiOpen)
        { renderTimer = false; }

        if (!self.Active || self.DrawLerp <= 0) { renderTimer = false; }

        if (renderTimer)
        {
            if (Engine.Scene is Level level2 && level2.Paused && self.Visible)
            {
                // Display pause/afk icons if necessary
                if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("LevelTimer_Pause")) { EndHelperModule.Session.pauseTypeDict["LevelTimer_Pause"] = false; }
                if (!EndHelperModule.Session.pauseTypeDict.ContainsKey("LevelTimer_AFK")) { EndHelperModule.Session.pauseTypeDict["LevelTimer_AFK"] = false; }
                bool freezedByPause = EndHelperModule.Session.pauseTypeDict["LevelTimer_Pause"];
                bool freezedByAfk = EndHelperModule.Session.pauseTypeDict["LevelTimer_AFK"];
                String pauseIconMsg = ":EndHelper/ui_timerfreeze_pause:";
                String afkIconMsg = ":EndHelper/ui_timerfreeze_afk:";

                if (global::Celeste.Settings.Instance.SpeedrunClock == SpeedrunType.Chapter)
                {
                    const int xPos = 13; const int xPosDiff = 22; const int yPos = 162;
                    if (freezedByPause && freezedByAfk)
                    {
                        ActiveFont.DrawOutline(pauseIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                        ActiveFont.DrawOutline(afkIconMsg, new Vector2(xPos + xPosDiff, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                    else if (freezedByPause)
                    {
                        ActiveFont.DrawOutline(pauseIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                    else if (freezedByAfk)
                    {
                        ActiveFont.DrawOutline(afkIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                }
                else if (global::Celeste.Settings.Instance.SpeedrunClock == SpeedrunType.File)
                {
                    const int xPos = 13; const int xPosDiff = 22; const int yPos = 185;
                    if (freezedByPause && freezedByAfk)
                    {
                        ActiveFont.DrawOutline(pauseIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                        ActiveFont.DrawOutline(afkIconMsg, new Vector2(xPos + xPosDiff, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                    else if (freezedByPause)
                    {
                        ActiveFont.DrawOutline(pauseIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                    else if (freezedByAfk)
                    {
                        ActiveFont.DrawOutline(afkIconMsg, new Vector2(xPos, yPos), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.DarkGray, 1f, Color.Black);
                    }
                }
            }
            orig(self);
        }
    }

    #region GrabbyIcon
    private static void ILHook_GrabbyIconUpdate(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Recast is using grab toggle, set grab mode to grab toggle
        // I shouldn't need to IL hook the Input.GrabCheck == true since grab check itself is already hooked
        if (!cursor.TryGotoNext(MoveType.After,
            // Find Settings.Instance.GrabMode
            instr => instr.MatchLdsfld<Settings>("Instance"),
            instr => instr.MatchLdfld<Settings>("GrabMode")
        ))
        {
            // This means cannot find both in a row
            return;
        }

        // Replace Settings.Instance.GrabMode with this check
        cursor.EmitDelegate<Func<GrabModes, GrabModes>>(ReplaceGrabModeGrabbyIconCheck);
    }
    private static GrabModes ReplaceGrabModeGrabbyIconCheck(GrabModes grabMode)
    {
        // If Recast is enabled and set to TurnGrabToToggle(Press), this is as good as grab mode being on.
        // So for purposes of the icon, return grabMode as toggle.
        if (Session.toggleifyEnabled && (
            EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour == ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToToggle
            || EndHelperModule.Settings.ToggleGrabMenu.toggleGrabBehaviour == ToggleGrabSubMenu.ToggleGrabBehaviourEnum.TurnGrabToTogglePress
        ))
        {
            grabMode = GrabModes.Toggle;
        }
        return grabMode;
    }

    #endregion

    #region CassetteBlockManager

    private static void Hook_CassetteBlockAwake(On.Celeste.CassetteBlock.orig_Awake orig, global::Celeste.CassetteBlock self, Scene scene)
    {
        // Set initial dynamic data stuff
        DynamicData cassetteBlockData = DynamicData.For(self);
        cassetteBlockData.Set("EndHelper_CassetteInitialPos", self.Position);

        orig(self, scene);

        // Do it for spikes and springs attached too
        List<StaticMover> c_staticMovers = cassetteBlockData.Get<List<StaticMover>>("staticMovers");
        foreach (StaticMover staticMover in c_staticMovers)
        {
            if (staticMover.Entity is Spikes spikes)
            {
                DynamicData spikeData = DynamicData.For(spikes);
                spikeData.Set("EndHelper_CassetteInitialPos", spikes.Position);
            }
            if (staticMover.Entity is Spring spring)
            {
                DynamicData springData = DynamicData.For(spring);
                springData.Set("EndHelper_CassetteInitialPos", spring.Position);
            }
        }
    }

    private static void Hook_CassetteBlockManagerAwake(On.Celeste.CassetteBlockManager.orig_Awake orig, global::Celeste.CassetteBlockManager self, Scene scene)
    {
        // Set initial dynamic data stuff
        DynamicData cassetteManagerData = DynamicData.For(self);
        cassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", int.MinValue);
        cassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", 1f);
        cassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop", false);
        List<List<object>> tempoChangeTimeDefault = [];
        cassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierList", tempoChangeTimeDefault);
        cassetteManagerData.Set("EndHelper_CassetteStartedSFX", false);

        // effectivebeatindex lol
        int c_beatIndex = cassetteManagerData.Get<int>("beatIndex");
        int effectiveBeatIndex = c_beatIndex;
        int c_leadBeats = cassetteManagerData.Get<int>("leadBeats");
        if (c_leadBeats > 0)
        {
            effectiveBeatIndex = -c_leadBeats;
        }
        cassetteManagerData.Set("EndHelper_CassetteManagerTriggerEffectiveBeatIndex", effectiveBeatIndex);

        orig(self, scene);
    }

    private static void ILHook_CassetteBlockManagerAdvMusic(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Hooks the very start in order to multiply tempo
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1)))
        {
            // Multiply `time` by CassetteManagerTrigger multiplier
            cursor.EmitDelegate<Func<float, float>>(Utils_CassetteManager.ManagerMultiplyCassetteSpeed);
        }

        // Find "if (leadBeats > 0)" condition check. Replace the whole thing with new logic if using the manager
        if (cursor.TryGotoNext(MoveType.After, 
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<CassetteBlockManager>("leadBeats"),
            instr => instr.MatchLdcI4(0)
        ))
        {
            // Condition in IL code is 0 <= leadBeats mean skip instr. This here force-changes the 0 to int limit (true, skip) if using the manager.
            cursor.EmitDelegate<Func<int, int>>(Utils_CassetteManager.ManagerLeadBeatShenanigans);
        }
    }
    #endregion

    #endregion
}