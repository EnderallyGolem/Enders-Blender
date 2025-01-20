using Celeste.Mod.EndHelper;
using MonoMod.RuntimeDetour;

using Monocle;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.SSMQoLMod;
using IL.Monocle;
using static Celeste.Mod.EndHelper.Entities.Misc.RoomStatisticsDisplayer;
using System.Reflection.PortableExecutable;
using System.Collections;
using System.Collections.Specialized;
using static Celeste.Mod.EndHelper.EndHelperModule;
using static Celeste.TrackSpinner;
using Celeste.Mod.SSMQoLMod.Modifications;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class SSMQoLIntegration
    {
        internal static void Load()
        {
            try
            {
                integratingWithSSMQoL = true;
            }
            catch (Exception) { }
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
