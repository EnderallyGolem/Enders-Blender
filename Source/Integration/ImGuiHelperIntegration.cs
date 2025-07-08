using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using static Celeste.Mod.EndHelper.EndHelperModule;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class ImGuiHelperIntegration
    {
        private static Hook Hook_ImGuiEngineUpdate;
        internal static void Load()
        {
            EverestModuleMetadata ImGuiHelperMetaData = new()
            {
                Name = "ImGuiHelper",
                Version = new Version(0, 1, 3)
            };
            if (Everest.Loader.DependencyLoaded(ImGuiHelperMetaData))
            {
                // Do the important stuff here
                Type typeOfThatDumbFunc = Type.GetType("Celeste.Mod.ImGuiHelper.ImGuiHelperModule,ImGuiHelper");

                MethodInfo targetMethod = typeOfThatDumbFunc.GetMethod("Engine_Update", BindingFlags.Static | BindingFlags.NonPublic);
                Hook_ImGuiEngineUpdate = new Hook(targetMethod, typeof(ImGuiHelperIntegration).GetMethod("Engine_Update_Hook", BindingFlags.Static | BindingFlags.NonPublic));
            }
        }

        internal static void Unload()
        {
            Hook_ImGuiEngineUpdate?.Dispose(); Hook_ImGuiEngineUpdate = null;
        }

#pragma warning disable  // Private method is unused

        private static void Engine_Update_Hook(Action<On.Monocle.Engine.orig_Update, Monocle.Engine, GameTime> orig, On.Monocle.Engine.orig_Update ogorig, Monocle.Engine self, GameTime gametime)
        {
            // This is here to get ImGuiHelper to stop forcing MInput Disable to be false when I need it to be true ):<
            if (mInputDisableDuration >= 1)
            {
                Monocle.MInput.Disabled = true;
                ogorig(self, gametime);
            } 
            else
            {
                orig(ogorig, self, gametime);
            }
        }
    }
}
