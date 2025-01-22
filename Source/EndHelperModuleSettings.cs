using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace Celeste.Mod.EndHelper;

[SettingName("EndHelper_Settings")]
public class EndHelperModuleSettings : EverestModuleSettings {
    public ButtonBinding FreeMultiroomWatchtower { get; set; }
    public ButtonBinding OpenStatDisplayMenu { get; set; }
    public ButtonBinding QuickRetry { get; set; }

    // Room Statistics Menu
    [SettingSubMenu]
    public class RoomStatMenuSubMenu
    {
        public enum PauseScenarioEnum { None, Pause, AFK, PauseAFK, PauseInactive, PauseInactiveAFK }

        [DefaultValue(PauseScenarioEnum.Pause)]
        [SettingSubText("modoptions_EndHelperModule_PauseOption_Desc")]
        public PauseScenarioEnum PauseOption { get; set; }


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


    public RoomStatMenuSubMenu RoomStatMenu { get; set; } = new();
    public RoomStatDisplaySubMenu RoomStatDisplay { get; set; } = new();

    [SettingSubText("modoptions_EndHelperModule_DisableQuickRestart_Desc")]
    public bool DisableQuickRestart { get; set; } = false;
}