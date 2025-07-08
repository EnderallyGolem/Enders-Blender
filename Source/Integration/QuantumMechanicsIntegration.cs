using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.EndHelper.Entities.Misc;
using Celeste.Mod.QuantumMechanics;
using MonoMod.Utils;
using MonoMod.Cil;
using Celeste.Mod.QuantumMechanics.Entities;

namespace Celeste.Mod.EndHelper.Integration
{
    public static class QuantumMechanicsIntegration
    {
        public static bool allowQuantumMechanicsIntegration = false;

        private static ILHook LoadHook_WonkyCassetteBlockManagerAdvMusic;
        private static Hook LoadHook_WonkyCassetteBlockManagerAwake;
        private static Hook LoadHook_WonkyCassetteBlockAwake;

        private static Type Type_WonkyCassetteBlock;
        private static Type Type_WonkyCassetteController;

        internal static void Load()
        {
            EverestModuleMetadata QuantumMechanicsMetaData = new()
            {
                Name = "QuantumMechanics",
                Version = new Version(1, 3, 0)
            };
            if (Everest.Loader.DependencyLoaded(QuantumMechanicsMetaData))
            {
                // Do the important stuff here
                allowQuantumMechanicsIntegration = true; // Check if loaded

                Type_WonkyCassetteBlock = Type.GetType("Celeste.Mod.QuantumMechanics.Entities.WonkyCassetteBlock,QuantumMechanics");
                Type_WonkyCassetteController = Type.GetType("Celeste.Mod.QuantumMechanics.Entities.WonkyCassetteBlockController,QuantumMechanics");

                // Wonky Cassettes: On Hook awake
                MethodInfo targetMethodCassetteBlock = Type_WonkyCassetteBlock.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);
                LoadHook_WonkyCassetteBlockAwake = new Hook(targetMethodCassetteBlock, typeof(QuantumMechanicsIntegration).GetMethod("WonkyCassetteBlockAwakeHook", BindingFlags.Static | BindingFlags.NonPublic));

                // Wonky Controller: On Hook awake
                MethodInfo targetMethod = Type_WonkyCassetteController.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);
                LoadHook_WonkyCassetteBlockManagerAwake = new Hook(targetMethod, typeof(QuantumMechanicsIntegration).GetMethod("WonkyCassetteControllerAwakeHook", BindingFlags.Static | BindingFlags.NonPublic));

                // Wonky Controller: IL Hook AdvanceMusic
                MethodInfo ILWonkyCassetteManagerAdv = Type_WonkyCassetteController.GetMethod("AdvanceMusic", BindingFlags.NonPublic | BindingFlags.Instance);
                LoadHook_WonkyCassetteBlockManagerAdvMusic = new ILHook(ILWonkyCassetteManagerAdv, ILHook_WonkyCassetteBlockManagerAdvMusic); // Pass ILContext to Hook_IL_DashCoroutine
            }
        }

        internal static void Unload()
        {
            LoadHook_WonkyCassetteBlockManagerAdvMusic?.Dispose(); LoadHook_WonkyCassetteBlockManagerAdvMusic = null;
            LoadHook_WonkyCassetteBlockManagerAwake?.Dispose(); LoadHook_WonkyCassetteBlockManagerAwake = null;
            LoadHook_WonkyCassetteBlockAwake?.Dispose(); LoadHook_WonkyCassetteBlockAwake = null;
        }

        public static int QMInte_MusicBeatIndex(int? setVal = null)
        {
            if (setVal == null)
            {
                return QuantumMechanicsModule.Session.MusicWonkyBeatIndex;
            }
            else
            {
                QuantumMechanicsModule.Session.MusicWonkyBeatIndex = setVal.Value;
                return setVal.Value;
            }
        }
        public static int QMInte_CassetteBeatIndex(int? setVal = null)
        {
            if (setVal == null)
            {
                return QuantumMechanicsModule.Session.CassetteWonkyBeatIndex;
            }
            else
            {
                QuantumMechanicsModule.Session.CassetteWonkyBeatIndex = setVal.Value;
                return setVal.Value;
            }
        }

        public static float QMInte_MusicBeatTimer(float? setVal = null)
        {
            if (setVal == null)
            {
                return QuantumMechanicsModule.Session.MusicBeatTimer;
            }
            else
            {
                QuantumMechanicsModule.Session.MusicBeatTimer = setVal.Value;
                return setVal.Value;
            }
        }

        public static float QMInte_CassetteBeatTimer(float? setVal = null)
        {
            if (setVal == null)
            {
                return QuantumMechanicsModule.Session.CassetteBeatTimer;
            }
            else
            {
                QuantumMechanicsModule.Session.CassetteBeatTimer = setVal.Value;
                return setVal.Value;
            }
        }

        public static void QMInte_setMusicLoopStarted(bool setVal)
        {
            QuantumMechanicsModule.Session.MusicLoopStarted = setVal;
        }

#pragma warning disable  // Private method is unused
        private static void ILHook_WonkyCassetteBlockManagerAdvMusic(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"il hook :3");
            // Hooks the very start in order to multiply tempo
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1)))
            {
                // Multiply `time` by CassetteManagerTrigger multiplier
                cursor.EmitDelegate<Func<float, float>>(WonkyManagerMultiplyCassetteSpeed);
            }
        }

        private static float WonkyManagerMultiplyCassetteSpeed(float originalTime)
        {
            if (Monocle.Engine.Scene is Level level && level.Tracker.GetEntity<WonkyCassetteBlockController>() is WonkyCassetteBlockController wonkyCassetteBlockManager)
            {
                // Logger.Log(LogLevel.Info, "EndHelper/main", $"changing multiply speed. id: {wonkyCassetteBlockManager.ID}");
                DynamicData wonkyCassetteManagerData = DynamicData.For(wonkyCassetteBlockManager);

                try
                {
                    List<List<object>> multiplierList = wonkyCassetteManagerData.Get<List<List<object>>>("EndHelper_CassetteManagerTriggerTempoMultiplierList");
                    bool multiplyOnTop = wonkyCassetteManagerData.Get<bool>("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop");
                    int beatIndex = QuantumMechanicsIntegration.QMInte_MusicBeatIndex();
                    int beatIndexMax = wonkyCassetteManagerData.Get<int>("maxBeats");
                    int c_introBeats = wonkyCassetteManagerData.Get<int>("introBeats");

                    int cassetteHaveCheckedBeatGet = wonkyCassetteManagerData.Get<int>("EndHelper_CassetteHaveCheckedBeat");
                    float cassettePreviousTempoNumGet = wonkyCassetteManagerData.Get<float>("EndHelper_CassettePreviousTempoNum");

                    //Logger.Log(LogLevel.Info, "EndHelper/main", $"checkedbeatget == beatIndex {cassetteHaveCheckedBeatGet} == {beatIndex}");
                    if (cassetteHaveCheckedBeatGet != beatIndex) // Check if this beat has already been checked
                    {
                        // Check if the current beat matches. This should never skip since the cassette block manager can only increase by 1 beat at a time
                        wonkyCassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", beatIndex);

                        // For CassetteBeatGates: Run IncrementBeatCheckMove
                        int c_barLength = wonkyCassetteBlockManager.barLength;
                        int c_beatLength = wonkyCassetteBlockManager.beatLength;
                        foreach (CassetteBeatGate cassetteBeatGate in level.Tracker.GetEntities<CassetteBeatGate>())
                        {
                            cassetteBeatGate.IncrementBeatCheckMove(currentBeat: beatIndex, totalCycleBeats: c_barLength * c_beatLength);
                        }

                        int minBeatsFromCurrent = int.MaxValue;

                        foreach (List<object> tempoPairList in multiplierList)
                        {
                            int beatNum = (int)tempoPairList[0];
                            float tempoNum = (float)tempoPairList[1];
                            //Logger.Log(LogLevel.Info, "EndHelper/main", $"beat index comparision: {beatNum} == {effectiveBeatIndex}.");

                            // If multiplyOnTop, only set check if beatNum == effectiveBeatIndex
                            if (multiplyOnTop)
                            {
                                if (beatNum == beatIndex)
                                {
                                    // Reset. Since this is only triggered once I can change cassettePreviousTempoNumGet too
                                    if (tempoNum < 0)
                                    {
                                        tempoNum = 1;
                                        cassettePreviousTempoNumGet = 1;
                                    }
                                    cassettePreviousTempoNumGet = tempoNum * cassettePreviousTempoNumGet;
                                    wonkyCassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", cassettePreviousTempoNumGet);
                                    break; // There should only be one matching beatNum. If there are multiple the mapper is stupid. and also later ones are ignored
                                }
                            }
                            else
                            // If normal, check how far behind the effectiveBeatIndex is behind the current beat.
                            // If effectiveBeatIndex is positive, ensure this loops across to beatIndexMax
                            {
                                // First, filter the beatNum. +ve beatNum only works for +ve effectiveBeatIndex, and vice versa
                                // EDIT FROM NORMAL CASSETTE: +ve, with respect to c_introBeats
                                // so yeah as lazy code im just gonna
                                int shiftedBeatIndex = beatIndex - c_introBeats;
                                // so the beatIndex from this part of the code onwards is relative to the loop point
                                // the same has to go for beatNum
                                int shiftedBeatNum = beatNum - c_introBeats;

                                if (shiftedBeatIndex >= 0 && shiftedBeatNum < 0 || shiftedBeatIndex < 0 && shiftedBeatNum >= 0)
                                {
                                    continue;
                                }

                                // Count how many beats this beatNum is behind the current (effective) beat
                                int beatsFromCurrent = shiftedBeatIndex - shiftedBeatNum;

                                // Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"BeatIndex: {beatIndex}. For {beatNum}, beatsFromCurrent = {beatsFromCurrent} and shiftedBeatIndex is {shiftedBeatIndex}");
                                if (beatsFromCurrent < 0 && shiftedBeatIndex >= 0) // Loop around (only for +ve)
                                {
                                    beatsFromCurrent += beatIndexMax - c_introBeats;
                                    // Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"1 now {beatsFromCurrent}");
                                }
                                else
                                {
                                    // Firstly, if the beatNum is larger than effectiveBeatIndex, it is not being called. Go away.
                                    if (shiftedBeatNum > shiftedBeatIndex) { continue; }
                                    // Now, pick the LARGER beatNum. I am going to just cheat this by flipping the sign. wait does this even do anything
                                    shiftedBeatNum *= -1;
                                    // Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"2 now {beatsFromCurrent}");
                                }

                                // Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"BeatIndex: {beatIndex}. For {beatNum}, beatsFromCurrent = {beatsFromCurrent}, vs nearest {minBeatsFromCurrent}");

                                // If this is smaller than minBeatsFromCurrent, set this as the new min beats and set tempo
                                if (beatsFromCurrent < minBeatsFromCurrent)
                                {
                                    minBeatsFromCurrent = beatsFromCurrent;

                                    // Reset. In this case it is just = 1 lol
                                    if (tempoNum < 0) { tempoNum = 1; }
                                    cassettePreviousTempoNumGet = tempoNum;
                                    wonkyCassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", cassettePreviousTempoNumGet);
                                }
                            }
                        }
                    }
                    //Logger.Log(LogLevel.Info, "EndHelper/QuantumMechanicsIntegration", $"return [no change] {originalTime * cassettePreviousTempoNumGet} -- {cassettePreviousTempoNumGet}");
                    return originalTime * cassettePreviousTempoNumGet;
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warn, "EndHelper/QuantumMechanicsIntegration", $"Cassette ManagerMultiplyCassetteSpeed error: {e}");
                }
            }

            // Not using the cassette block manager trigger lol
            return originalTime;
        }


        private static void WonkyCassetteControllerAwakeHook(Action<WonkyCassetteBlockController, Monocle.Scene> orig, WonkyCassetteBlockController self, Monocle.Scene scene)
        {
            DynamicData wonkyCassetteManagerData = DynamicData.For(self);
            wonkyCassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", int.MinValue);
            wonkyCassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", 1f);
            wonkyCassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop", false);
            List<List<object>> tempoChangeTimeDefault = [];
            wonkyCassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierList", tempoChangeTimeDefault);
            // wonkyCassetteManagerData.Set("EndHelper_CassetteStartedSFX", false); // Not needed since this was for lead beats but QM handles lead beats differently
            orig(self, scene);
        }
        private static void WonkyCassetteBlockAwakeHook(Action<WonkyCassetteBlock, Monocle.Scene> orig, WonkyCassetteBlock self, Monocle.Scene scene)
        {
            DynamicData wonkyCassetteBlockData = DynamicData.For(self);
            wonkyCassetteBlockData.Set("EndHelper_CassetteInitialPos", self.Position);
            orig(self, scene);

            List<StaticMover> c_staticMovers = wonkyCassetteBlockData.Get<List<StaticMover>>("staticMovers");
            foreach (StaticMover staticMover in c_staticMovers)
            {
                if (staticMover.Entity is Spikes spikes)
                {
                    DynamicData spikeData = DynamicData.For(spikes);
                    spikeData.Set("EndHelper_CassetteInitialPos", spikes.Position);
                }
                if (staticMover.Entity is Spring spring)
                {
                    DynamicData springData = DynamicData.For(spring);
                    springData.Set("EndHelper_CassetteInitialPos", spring.Position);
                }
            }
        }

    }
}
