using Microsoft.Xna.Framework.Input;
using System.ComponentModel;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.RoomStatMenuSubMenu;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.ToggleGrabSubMenu;

namespace Celeste.Mod.EndHelper;

[SettingName("EndHelper_Settings")]
public class EndHelperModuleSettings : EverestModuleSettings {

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_RoomStat")]
    public ButtonBinding OpenStatDisplayMenu { get; set; }

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_GameplayTweaks")]
    public ButtonBinding ToggleGrab { get; set; }

    public ButtonBinding NeutralDrop { get; set; }
    public ButtonBinding Backboost { get; set; }

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_QOLTweaks")]
    public ButtonBinding QuickRetry { get; set; }[SettingSubText("modoptions_EndHelperModule_DisableFrequentScreenShake_Desc")]
    public ButtonBinding FreeMultiroomWatchtower { get; set; }


    // Room Statistics Menu
    [SettingSubMenu]
    public class RoomStatMenuSubMenu
    {
        public enum RoomPauseScenarioEnum { None, Pause, AFK, PauseAFK, PauseInactive, PauseInactiveAFK }

        [DefaultValue(RoomPauseScenarioEnum.Pause)]
        [SettingSubText("modoptions_EndHelperModule_PauseOption_Desc")]
        public RoomPauseScenarioEnum PauseOption { get; set; }

        [SettingSubText("modoptions_EndHelperModule_DeathIgnoreLoadAfterDeath_Desc")]
        public bool DeathIgnoreLoadAfterDeath { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_MenuShowFirstClear_Desc")]
        public bool MenuShowFirstClear { get; set; } = false;

        public enum StoredClearsEnum { Always, Ask, AskIfValidClear, ValidClear, ValidClearFaster, ValidClearLessDeaths, Never }
        [SettingSubText("modoptions_EndHelperModule_StoredClears_Desc")]
        [DefaultValue(StoredClearsEnum.Ask)]
        public StoredClearsEnum StoredClears { get; set; }

        [SettingSubText("modoptions_EndHelperModule_MenuMulticolor_Desc")]
        public bool MenuMulticolor { get; set; } = true;

        [SettingSubText("modoptions_EndHelperModule_MenuSpoilBerries_Desc")]
        public bool MenuSpoilBerries { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_MenuTrackerStorageCount_Desc")]
        [SettingRange(min: -1, max: 10000, largeRange: true)]
        public int MenuTrackerStorageCount { get; set; } = -1;
    }

    // Room Statistics Display
    [SettingSubMenu]
    public class RoomStatDisplaySubMenu
    {
        public bool ShowRoomName { get; set; } = false;
        public bool ShowDeaths { get; set; } = false;
        public bool ShowTimeSpent { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_ShowStrawberries_Desc")]
        public bool ShowStrawberries { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_ShowAliveTime_Desc")]
        public bool ShowAliveTime { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_HideIfGolden_Desc")]
        public bool HideIfGolden { get; set; } = false;

        // offset xy and scale
        public enum Justification { Left, Center, Right }
        [DefaultValue(Justification.Left)]
        public Justification xJustification { get; set; }

        [SettingRange(min: -30, max: 240, largeRange: true)]
        // [DefaultValue(0)]
        public int OffsetX { get; set; } = 0;

        [SettingRange(min: -30, max: 140, largeRange: true)]
        public int OffsetY { get; set; } = 0;

        [SettingRange(min: 1, max: 20, largeRange: false)]
        public int Size { get; set; } = 10;
    }


    // Toggle Grab Key
    [SettingSubMenu]
    [SettingName("modoptions_EndHelperModule_ToggleGrab")]
    public class ToggleGrabSubMenu
    {
        [SettingRange(min: -30, max: 240, largeRange: true)]
        public int GrabOffsetX { get; set; } = 225;

        [SettingRange(min: -30, max: 140, largeRange: true)]
        public int GrabOffsetY { get; set; } = 120;

        [SettingRange(min: 1, max: 30, largeRange: false)]
        public int GrabSize { get; set; } = 15;

        public bool HideWhenPause { get; set; } = true;
        public bool UntoggleUponDeath { get; set; } = false;

        // offset xy and scale
        public enum ToggleGrabBehaviourEnum { InvertDuringGrab, UntoggleOnGrab, TurnGrabToToggle, TurnGrabToTogglePress, NothingIfGrab }
        [DefaultValue(ToggleGrabBehaviourEnum.TurnGrabToToggle)]
        [SettingSubText("modoptions_EndHelperModule_toggleGrabBehaviour_Desc")]
        public ToggleGrabBehaviourEnum toggleGrabBehaviour { get; set; }
    }

    [SettingSubMenu]
    public class GameplayTweaks
    {
        public enum ConvertDemoEnum { Disabled, EnabledNormal, EnabledDiagonal }
        [DefaultValue(ConvertDemoEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_ConvertDemo_Desc")]
        public ConvertDemoEnum ConvertDemo { get; set; }

        public enum SeemlessRespawnEnum { Disabled, EnabledNormal, EnabledNear, EnabledInstant, EnabledKeepState }
        [DefaultValue(SeemlessRespawnEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_SeemlessRespawn_Desc")]
        public SeemlessRespawnEnum SeemlessRespawn { get; set; }

        // These were removed due to too buggy
        //[SettingRange(min: 0, max: 30, largeRange: false)]
        //[SettingSubText("modoptions_EndHelperModule_SeemlessRespawnDelay_Desc")]
        //public int SeemlessRespawnDelay { get; set; } = 0;
    }

    [SettingSubMenu]
    public class QOLTweaks
    {
        [SettingSubText("modoptions_EndHelperModule_AutosaveTime_Desc")]
        [SettingRange(min: 0, max: 30, largeRange: false)]
        public int AutosaveTime { get; set; } = 0;

        [SettingSubText("modoptions_EndHelperModule_DisableQuickRestart_Desc")]
        public bool DisableQuickRestart { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_DisableFrequentScreenShake_Desc")]
        public bool DisableFrequentScreenShake { get; set; } = false;

        public enum PreventAccidentalQuitEnum { Disabled, TimeSmall, TimeHalf, Time1, Time1Half, Time2, Time3 }
        [DefaultValue(PreventAccidentalQuitEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_PreventAccidentalQuit_Desc")]
        public PreventAccidentalQuitEnum PreventAccidentalQuit { get; set; }
    }


    // The settings
    public enum LevelPauseScenarioEnum { None, Pause, AFK, PauseAFK }
    [DefaultValue(LevelPauseScenarioEnum.None)]
    //[SettingSubHeader("modoptions_EndHelperModule_SubHeader_Misc")]
    [SettingSubText("modoptions_EndHelperModule_PauseOptionLevel_Desc")]
    public LevelPauseScenarioEnum PauseOptionLevel { get; set; }


    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_RoomStat")]
    public RoomStatMenuSubMenu RoomStatMenu { get; set; } = new();
    public RoomStatDisplaySubMenu RoomStatDisplayMenu { get; set; } = new();

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_GameplayTweaks")]
    public ToggleGrabSubMenu ToggleGrabMenu { get; set; } = new();
    public GameplayTweaks GameplayTweaksMenu { get; set; } = new();

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_QOLTweaks")]
    public QOLTweaks QOLTweaksMenu { get; set; } = new();
}