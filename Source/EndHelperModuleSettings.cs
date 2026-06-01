using System.ComponentModel;
using Celeste.Mod.EndHelper.Utils;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Celeste.Mod.EndHelper;

[SettingName("EndHelper_Settings")]
public class EndHelperModuleSettings : EverestModuleSettings {

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_RoomStat")]
    public ButtonBinding OpenStatDisplayMenu { get; set; }

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_QOLTweaks")]
    public ButtonBinding QuickRetry { get; set; }
    [SettingSubText("modoptions_EndHelperModule_DisableFrequentScreenShake_Desc")]
    public ButtonBinding FreeMultiroomWatchtower { get; set; }

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_GameplayTweaks")]
    public ButtonBinding ToggleGrab { get; set; }

    public ButtonBinding NeutralDrop { get; set; }
    public ButtonBinding Backboost { get; set; }



    // Room Statistics Menu
    [SettingSubMenu]
    public class RoomStatMenuSubMenu
    {
        [DefaultValue(Utils_General_Public.TimerPauseScenarioEnum.Pause)]
        [SettingSubText("modoptions_EndHelperModule_PauseOption_Desc")]
        public Utils_General_Public.TimerPauseScenarioEnum PauseOption { get; set; }

        [SettingSubText("modoptions_EndHelperModule_DeathIgnoreLoadAfterDeath_Desc")]
        public bool DeathIgnoreLoadAfterDeath { get; set; } = false;

        // [SettingSubText("modoptions_EndHelperModule_MenuShowFirstClear_Desc")]
        // public bool MenuShowFirstClear { get; set; } = false;
        public enum MenuShowTimeEnum { Normal, RTA, Both }
        [SettingSubText("modoptions_EndHelperModule_MenuShowTime_Desc")]
        [DefaultValue(MenuShowTimeEnum.Normal)]
        public MenuShowTimeEnum MenuShowTime { get; set; }

        public enum StoredClearsEnum { Always, Ask, AskIfValidClear, ValidClear, ValidClearFaster, ValidClearLessDeaths, Never }
        [SettingSubText("modoptions_EndHelperModule_StoredClears_Desc")]
        [DefaultValue(StoredClearsEnum.ValidClear)]
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
        [SettingSubText("modoptions_EndHelperModule_ShowRTATimeSpent_Desc")]
        public bool ShowRTATimeSpent { get; set; } = false;

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
    public class QOLTweaks
    {
        [SettingSubText("modoptions_EndHelperModule_AutosaveTime_Desc")]
        [SettingRange(min: 0, max: 30, largeRange: false)]
        public int AutosaveTime { get; set; } = 0;

        [SettingSubText("modoptions_EndHelperModule_DisableFrequentScreenShake_Desc")]
        public bool DisableFrequentScreenShake { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_DisableQuickRestart_Desc")]
        public bool DisableQuickRestart { get; set; } = false;

        public enum PreventAccidentalQuitEnum { Disabled, TimeSmall, TimeHalf, Time1, Time1Half, Time2, Time3 }
        [DefaultValue(PreventAccidentalQuitEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_PreventAccidentalQuit_Desc")]
        public PreventAccidentalQuitEnum PreventAccidentalQuit { get; set; }


        [SettingSubHeader("modoptions_EndHelperModule_SubSubHeader_Respawns")]
        [SettingSubText("modoptions_EndHelperModule_AlwaysQuickRespawn_Desc")]
        public bool AlwaysQuickRespawn { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_NoRespawnTransition_Desc")]
        public bool NoRespawnTransition { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_NoRespawnAnimation_Desc")]
        public bool NoRespawnAnimation { get; set; } = false;
    }

    [SettingSubMenu]
    public class GameplayTweaks
    {
        public enum ConvertDemoEnum { Disabled, EnabledNormal, EnabledDiagonal }
        [DefaultValue(ConvertDemoEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_ConvertDemo_Desc")]
        public ConvertDemoEnum ConvertDemo { get; set; }

        public enum SeamlessRespawnEnum { Disabled, EnabledNormal, EnabledNear, EnabledInstant, EnabledKeepState }
        [DefaultValue(SeamlessRespawnEnum.Disabled)]
        [SettingSubText("modoptions_EndHelperModule_SeamlessRespawn_Desc")]
        public SeamlessRespawnEnum seamlessRespawn { get; set; }

    }


    // The settings
    [DefaultValue(Utils_General_Public.TimerPauseScenarioEnum.None)]
    //[SettingSubHeader("modoptions_EndHelperModule_SubHeader_Misc")]
    [SettingSubText("modoptions_EndHelperModule_PauseOptionLevel_Desc")]
    public Utils_General_Public.TimerPauseScenarioEnum PauseOptionLevel { get; set; }

    [DefaultValue(Utils_General_Public.TimerPauseScenarioEnum.None)]
    [SettingSubText("modoptions_EndHelperModule_PauseOptionFile_Desc")]
    public Utils_General_Public.TimerPauseScenarioEnum PauseOptionFile { get; set; }


    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_RoomStat")]
    public RoomStatMenuSubMenu RoomStatMenu { get; set; } = new();
    public RoomStatDisplaySubMenu RoomStatDisplayMenu { get; set; } = new();

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_QOLTweaks")]
    public QOLTweaks QOLTweaksMenu { get; set; } = new();

    [SettingSubHeader("modoptions_EndHelperModule_SubHeader_GameplayTweaks")]
    public ToggleGrabSubMenu ToggleGrabMenu { get; set; } = new();
    public GameplayTweaks GameplayTweaksMenu { get; set; } = new();
}