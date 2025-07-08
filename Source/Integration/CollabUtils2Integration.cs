using Celeste.Mod.EndHelper;
using MonoMod.RuntimeDetour;

using Monocle;
using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.EndHelper.Integration
{
    [ModImportName("CollabUtils2.LobbyHelper")]
    public static class CollabUtils2Import
    {
        public static Func<string, string> GetLobbyLevelSet; // Input: map sid, output: lobby sid. this isn't even used lol
        public static Func<string, bool> IsHeartSide; // Input: map sid, output: bool
    }

    public static class CollabUtils2Integration
    {
        public static bool CollabUtils2Installed;
        internal static void Load()
        {
            typeof(CollabUtils2Import).ModInterop();
            CollabUtils2Installed = CollabUtils2Import.GetLobbyLevelSet is not null;
        }

        internal static void Unload()
        {

        }

#pragma warning disable IDE0051  // Private method is unused

    }
}