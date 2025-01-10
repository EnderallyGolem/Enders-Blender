using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.EndHelper;

public class EndHelperModuleSettings : EverestModuleSettings {

    [SettingName("modsettings_EndHelper_FreeMultiroomWatchtower")]
    public ButtonBinding FreeMultiroomWatchtower { get; set; }
}