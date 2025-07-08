using Celeste.Mod.EndHelper.Entities.Misc;
using FMOD.Studio;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EndHelper.Utils
{
    static internal class Utils_CassetteManager
    {
        internal static int ManagerLeadBeatShenanigans(int leadBeatReturn)
        {
            if (Engine.Scene is Level level && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
            {
                // Using manager! Return 0 at the end as it ensures the functions will be skipped. Before that though...
                // Replace vanilla's lead beat logic with our lead beat logic.
                // Vanilla's lead beat logic does not allow us to change the lead beat between 0 and not 0.

                DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);

                int c_leadBeats = cassetteManagerData.Get<int>("leadBeats");
                int c_beatIndex = cassetteManagerData.Get<int>("beatIndex");
                bool c_cassetteStarted = cassetteManagerData.Get<bool>("EndHelper_CassetteStartedSFX");
                bool c_isLevelMusic = cassetteManagerData.Get<bool>("isLevelMusic");

                // If c_leadBeats is 1 (which is to say it increases normally rather than set), set beatIndex to 0.
                // If it is 1 that means it will be set to 0 in the next step.
                // Running this because manual set will set it to 0 directly and thus avoiding setting beatIndex to 0, which is what I want
                if (c_leadBeats == 1)
                {
                    c_beatIndex = 0;
                    cassetteManagerData.Set("beatIndex", c_beatIndex);
                }

                // Next, reduce leadBeats if it is larger than 0.
                // This is the opposite order of what vanilla cassetteblockmanager does
                if (c_leadBeats > 0)
                {
                    c_leadBeats--;
                    cassetteManagerData.Set("leadBeats", c_leadBeats);
                }

                // Lastly, start the SFX if leadBeats == 0 and c_cassetteStarted == false (set to true).
                // This is taken out of any c_leadBeat > 0 check so setting directly to 0 works.
                // However to prevent it from being spammed, use a bool to check if this has already been done.
                if (c_leadBeats <= 0 && !c_cassetteStarted && !c_isLevelMusic)
                {
                    c_cassetteStarted = true;
                    cassetteManagerData.Set("EndHelper_CassetteStartedSFX", c_cassetteStarted);

                    // idk throw this in here in case dynamicdataing this specifically is laggy. it probably won't do much.
                    EventInstance sfx = cassetteManagerData.Get<EventInstance>("sfx");
                    sfx?.start(); // Start the musik!
                }

                return int.MaxValue;
            }
            else
            {
                // Not using manager. Just return existing value.
                return leadBeatReturn;
            }
        }
        internal static float ManagerMultiplyCassetteSpeed(float originalTime)
        {
            if (Engine.Scene is Level level && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
            {
                DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);

                try
                {
                    List<List<object>> multiplierList = cassetteManagerData.Get<List<List<object>>>("EndHelper_CassetteManagerTriggerTempoMultiplierList");
                    bool multiplyOnTop = cassetteManagerData.Get<bool>("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop");
                    int effectiveBeatIndex = cassetteManagerData.Get<int>("EndHelper_CassetteManagerTriggerEffectiveBeatIndex");
                    int beatIndexMax = cassetteManagerData.Get<int>("beatIndexMax");

                    int cassetteHaveCheckedBeatGet = cassetteManagerData.Get<int>("EndHelper_CassetteHaveCheckedBeat");
                    float cassettePreviousTempoNumGet = cassetteManagerData.Get<float>("EndHelper_CassettePreviousTempoNum");

                    //Logger.Log(LogLevel.Info, "EndHelper/main", $"checkedbeatget == effectivebeatindex {cassetteHaveCheckedBeatGet} == {effectiveBeatIndex}");
                    if (cassetteHaveCheckedBeatGet != effectiveBeatIndex) // Check if this beat has already been checked
                    {
                        // Check if the current beat matches. This should never skip since the cassette block manager can only increase by 1 beat at a time
                        cassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", effectiveBeatIndex);

                        // For CassetteBeatGates: Run IncrementBeatCheckMove
                        int c_beatIndex = cassetteManagerData.Get<int>("beatIndex");
                        int c_beatsPerTick = cassetteManagerData.Get<int>("beatsPerTick");
                        int c_ticksPerSwap = cassetteManagerData.Get<int>("ticksPerSwap");
                        int c_maxBeat = cassetteManagerData.Get<int>("maxBeat");
                        foreach (CassetteBeatGate cassetteBeatGate in level.Tracker.GetEntities<CassetteBeatGate>())
                        {
                            cassetteBeatGate.IncrementBeatCheckMove(currentBeat: c_beatIndex, totalCycleBeats: c_beatsPerTick * c_ticksPerSwap * c_maxBeat);
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
                                if (beatNum == effectiveBeatIndex)
                                {
                                    // Reset. Since this is only triggered once I can change cassettePreviousTempoNumGet too
                                    if (tempoNum < 0)
                                    {
                                        tempoNum = 1;
                                        cassettePreviousTempoNumGet = 1;
                                    }
                                    cassettePreviousTempoNumGet = tempoNum * cassettePreviousTempoNumGet;
                                    cassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", cassettePreviousTempoNumGet);
                                    break; // There should only be one matching beatNum. If there are multiple the mapper is stupid. and also later ones are ignored
                                }
                            }
                            else
                            // If normal, check how far behind the effectiveBeatIndex is behind the current beat.
                            // If effectiveBeatIndex is positive, ensure this loops across to beatIndexMax
                            {
                                // First, filter the beatNum. +ve beatNum only works for +ve effectiveBeatIndex, and vice versa
                                if (effectiveBeatIndex >= 0 && beatNum < 0 || effectiveBeatIndex < 0 && beatNum >= 0)
                                {
                                    continue;
                                }

                                // Count how many beats this beatNum is behind the current (effective) beat
                                int beatsFromCurrent = effectiveBeatIndex - beatNum;
                                if (beatsFromCurrent < 0 && effectiveBeatIndex >= 0) // Loop around (only for +ve)
                                {
                                    beatsFromCurrent += beatIndexMax;
                                }
                                else
                                {
                                    // Firstly, if the beatNum is larger than effectiveBeatIndex, it is not being called. Go away.
                                    if (beatNum > effectiveBeatIndex) { continue; }
                                    // Now, pick the LARGER beatNum. I am going to just cheat this by flipping the sign.
                                    beatNum *= -1;
                                }

                                // Logger.Log(LogLevel.Info, "EndHelper/main", $"{beatNum}: is {beatsFromCurrent} from current beat ({effectiveBeatIndex} - {beatNum}). Compare with min {minBeatsFromCurrent}");

                                // If this is smaller than minBeatsFromCurrent, set this as the new min beats and set tempo
                                if (beatsFromCurrent < minBeatsFromCurrent)
                                {
                                    minBeatsFromCurrent = beatsFromCurrent;

                                    // Reset. In this case it is just = 1 lol
                                    if (tempoNum < 0) { tempoNum = 1; }
                                    cassettePreviousTempoNumGet = tempoNum;
                                    cassetteManagerData.Set("EndHelper_CassettePreviousTempoNum", cassettePreviousTempoNumGet);
                                }
                            }
                        }
                    }
                    //Logger.Log(LogLevel.Info, "EndHelper/main", $"return [no change] {originalTime * cassettePreviousTempoNumGet} -- {cassettePreviousTempoNumGet}");
                    return originalTime * cassettePreviousTempoNumGet;
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warn, "EndHelper/main", $"Cassette ManagerMultiplyCassetteSpeed error: {e}");
                }
            }

            // Not using the cassette block manager trigger lol
            return originalTime;
        }
    }
}
