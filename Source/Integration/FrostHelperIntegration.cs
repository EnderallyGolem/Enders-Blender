using Celeste.Mod.EndHelper.Utils;
using FrostHelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using static Celeste.Mod.EndHelper.EndHelperModule;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class FrostHelperIntegration
    {
        internal static void Load()
        {
            EverestModuleMetadata FrostHelperMetaData = new()
            {
                Name = "FrostHelper",
                Version = new Version(1, 70, 1)
            };
            if (Everest.Loader.DependencyLoaded(FrostHelperMetaData))
            {
                integratingWithFrostHelper = true;
            }
        }

        internal static void Unload()
        {

        }

        internal static bool CheckCollisionWithCustomSpinners(Level level, Rectangle targetRect)
        {
            return level.CollideCheck<CustomSpinner>(targetRect);
        }
        internal static bool CheckCollisionWithCustomSpinners(Level level, Vector2 point)
        {
            return level.CollideCheck<CustomSpinner>(point);
        }
        internal static bool CheckIfCustomSpinner(Entity entity)
        {
            return entity is CustomSpinner;
        }
    }
}
