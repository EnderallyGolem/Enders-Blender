using System;
using Celeste.Mod.SSMQoLMod;
using static Celeste.Mod.EndHelper.EndHelperModule;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class SSMQoLIntegration
    {
        internal static void Load()
        {
            EverestModuleMetadata SSMQolMetaData = new()
            {
                Name = "SSMQoLMod",
                Version = new Version(1, 2, 1)
            };
            if (Everest.Loader.DependencyLoaded(SSMQolMetaData))
            {
                integratingWithSSMQoL = true;
            }
        }

        internal static void Unload()
        {

        }

        internal static float GetMultiplier()
        {
            float multiplier = 1f;
            if (SSMQoLModule.Settings.FastLookout && SSMQoLModule.Settings.FastLookoutButton.Check)
            {
                multiplier *= SSMQoLModule.Settings.FastLookoutMultiplier;
            }
            return multiplier;
        }
    }
}
