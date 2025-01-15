using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace Celeste.Mod.EndHelper;

[SettingName("EndHelper_Settings")]
public class EndHelperModuleSettings : EverestModuleSettings {
    public ButtonBinding FreeMultiroomWatchtower { get; set; }
    public ButtonBinding OpenStatDisplayMenu { get; set; }

    // Room Statistics Displayer

    [SettingSubMenu]
    public class StatDisplaySubMenu
    {
        [DefaultValue(false)]
        public bool ShowRoomName { get; set; }
        [DefaultValue(false)]
        public bool ShowDeaths { get; set; }
        [DefaultValue(false)]
        public bool ShowTimeSpent { get; set; }
        [DefaultValue(false)]
        public bool ShowStrawberries { get; set; }

        // offset xy and scale
        public enum Justification { Left, Center, Right }
        [DefaultValue(Justification.Left)]
        public Justification xJustification { get; set; }

        [SettingRange(min: -30, max: 240, largeRange: true)]
        [DefaultValue(0)]
        public int OffsetX { get; set; }

        [SettingRange(min: -30, max: 140, largeRange: true)]
        [DefaultValue(0)]
        public int OffsetY { get; set; }

        [SettingRange(min: 1, max: 20, largeRange: false)]
        [DefaultValue(10)]
        public int Size { get; set; }



        public enum PauseScenarioEnum { None, Pause, AFK, Both }

        [DefaultValue(PauseScenarioEnum.Pause)]
        public PauseScenarioEnum PauseOption { get; set; }
    }
    public StatDisplaySubMenu StatDisplay { get; set; } = new();
}