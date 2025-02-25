using Microsoft.Xna.Framework.Input;
using System.ComponentModel;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.RoomStatMenuSubMenu;

namespace Celeste.Mod.EndHelper;

[SettingName("EndHelper_Settings")]
public class EndHelperModuleSettings : EverestModuleSettings {
    public ButtonBinding OpenStatDisplayMenu { get; set; }
    public ButtonBinding QuickRetry { get; set; }
    public ButtonBinding FreeMultiroomWatchtower { get; set; }
    public ButtonBinding ToggleGrab { get; set; }


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


        [SettingSubText("modoptions_EndHelperModule_MenuMulticolor_Desc")]
        public bool MenuMulticolor { get; set; } = true;

        [SettingSubText("modoptions_EndHelperModule_MenuSpoilBerries_Desc")]
        public bool MenuSpoilBerries { get; set; } = false;

        [SettingSubText("modoptions_EndHelperModule_MenuCustomNameStorageCount_Desc")]
        [SettingRange(min: 0, max: 100, largeRange: true)]
        public int MenuCustomNameStorageCount { get; set; } = 10;
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


    // The settings
    public enum LevelPauseScenarioEnum { None, Pause, AFK, PauseAFK }
    [DefaultValue(LevelPauseScenarioEnum.None)]
    [SettingSubText("modoptions_EndHelperModule_PauseOptionLevel_Desc")]
    public LevelPauseScenarioEnum PauseOptionLevel { get; set; }

    [SettingSubText("modoptions_EndHelperModule_DisableQuickRestart_Desc")]
    public bool DisableQuickRestart { get; set; } = false;

    public enum ConvertDemoEnum { Disabled, EnabledNormal, EnabledDiagonal }
    [DefaultValue(ConvertDemoEnum.Disabled)]
    [SettingSubText("modoptions_EndHelperModule_ConvertDemo_Desc")]
    public ConvertDemoEnum convertDemo { get; set; }

    public RoomStatMenuSubMenu RoomStatMenu { get; set; } = new();
    public RoomStatDisplaySubMenu RoomStatDisplayMenu { get; set; } = new();
    public ToggleGrabSubMenu ToggleGrabMenu { get; set; } = new();
}