using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.ModInterop;

namespace Celeste.Mod.EndHelper.Utils
{
    public static class Utils_Interop
    {
        internal static void InitialiseInteropExports()
        {
            typeof(DeathHandler).ModInterop();
        }

        [ModExportName("EndersBlender.DeathHandler")]
        public static class DeathHandler
        {
            /// <summary>
            /// Returns AllowDeathHandlerEntityChecks.]
            /// This is enabled if the level has any entity that uses DeathHandler's modified respawns.
            /// i.e. this is false if the level is not using DeathHandler.
            /// </summary>
            /// <returns>Bool corresponding to whether if DeathHandler is used.</returns>
            public static bool GetEnableEntityChecks()
            {
                return EndHelperModule.Session.AllowDeathHandlerEntityChecks;
            }

            /// <summary>
            /// Checks whether if a full reset should occur upon player death
            /// This is made to be read during a On.Celeste.Player.Die hook.
            /// </summary>
            /// <returns>Bool corresponding to if the next death results in a full reset</returns>
            public static bool GetNextRespawnFullReset()
            {
                return EndHelperModule.Session.nextRespawnFullReset;
            }

            /// <summary>
            /// Checks whether if a player died due to retry.
            /// This is made to be read during a On.Celeste.Player.Die hook.
            /// </summary>
            /// <returns>Bool corresponding to if the death is due to manual retry</returns>
            public static bool GetManualReset()
            {
                return Utils_DeathHandler.manualReset;
            }
        }
    }
}
