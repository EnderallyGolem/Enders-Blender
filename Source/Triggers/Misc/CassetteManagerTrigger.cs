using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.Entities;
using Celeste.Mod.QuantumMechanics.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/CassetteManagerTrigger")]
public class CassetteManagerTrigger : Trigger
{
    private Vector2 dataOffset;
    private EntityData entityData;

    private bool wonkyCassettes = false;
    private bool showDebugInfo = false;
    private String debugInfo = "";
    private String debugInfo2 = "";

    private String multiplyTempoAtBeat = "";
    private bool multiplyTempoExisting = false;
    private bool multiplyTempoOnEnter = false;

    private int setBeatOnEnter = 1;
    private int setBeatOnLeave = 1;
    private int setBeatInside = 1;
    private int setBeatOnlyIfAbove = 0;
    private int setBeatOnlyIfUnder = -1;
    private bool addInsteadOfSet = false;
    private int doNotSetIfWithinRange = 0;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public CassetteManagerTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        wonkyCassettes = data.Bool("wonkyCassettes", false);
        showDebugInfo = data.Bool("showDebugInfo", false);

        multiplyTempoAtBeat = data.Attr("multiplyTempoAtBeat", "");
        multiplyTempoExisting = data.Bool("multiplyTempoExisting", false);
        multiplyTempoOnEnter = data.Bool("multiplyTempoOnEnter", false);

        setBeatOnEnter = data.Int("setBeatOnEnter", 1);
        setBeatOnLeave = data.Int("setBeatOnLeave", 1);
        setBeatInside = data.Int("setBeatInside", 1);
        addInsteadOfSet = data.Bool("addInsteadOfSet", false);

        setBeatOnlyIfAbove = data.Int("setBeatOnlyIfAbove", 0);
        setBeatOnlyIfUnder = data.Int("setBeatOnlyIfUnder", -1);
        doNotSetIfWithinRange = data.Int("doNotSetIfWithinRange", 0);

        Collider = new Hitbox(data.Width, data.Height);
        entityData = data;
        dataOffset = offset;
        Visible = Active = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (showDebugInfo)
        {
            AddTag(Tags.HUD);
        }
    }

    public override void Awake(Scene scene)
    {
        if (wonkyCassettes && !QuantumMechanicsIntegration.allowQuantumMechanicsIntegration)
        {
            throw new ArgumentException($"A Cassette Manager Trigger is set to use Wonky Cassettes, but the Quantum Mechanics mod required for it cannot be found!");
        }


        if (!multiplyTempoOnEnter)
        {
            setTempoMultiplier(multiplyTempoAtBeat, multiplyTempoExisting);
        }
        base.Awake(scene);
    }

    public override void Update()
    {
        Level level = SceneAs<Level>();

        if (!wonkyCassettes && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
        {
            DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);

            // Get all the juicy data yum
            int c_currentIndex = cassetteManagerData.Get<int>("currentIndex");
            int c_maxBeat = cassetteManagerData.Get<int>("maxBeat");

            int c_beatIndex = cassetteManagerData.Get<int>("beatIndex");
            int c_beatIndexMax = cassetteManagerData.Get<int>("beatIndexMax");

            float c_tempoMult = cassetteManagerData.Get<float>("tempoMult");
            float c_beatTimer = cassetteManagerData.Get<float>("beatTimer");

            int c_leadBeats = cassetteManagerData.Get<int>("leadBeats");
            int c_beatIndexOffset = cassetteManagerData.Get<int>("beatIndexOffset");
            int c_beatsPerTick = cassetteManagerData.Get<int>("beatsPerTick");
            int c_ticksPerSwap = cassetteManagerData.Get<int>("ticksPerSwap");


            int effectiveBeatIndex = c_beatIndex;
            if (c_leadBeats > 0)
            {
                effectiveBeatIndex = -c_leadBeats;
            }

            cassetteManagerData.Set("EndHelper_CassetteManagerTriggerEffectiveBeatIndex", effectiveBeatIndex);

            String additionalMultiplierText = "";
            float cassettePreviousTempoNum = cassetteManagerData.Get<float>("EndHelper_CassettePreviousTempoNum");
            if (cassettePreviousTempoNum != 1) { additionalMultiplierText = $" x {cassettePreviousTempoNum}"; }

            String beatTimerText = "";
            if (c_beatTimer >= 1 / 6f)
            {
                beatTimerText = $" [Speed Overflow!]";
            }
            if (c_beatTimer > 1 / 6f)
            {
                cassetteManagerData.Set("beatTimer", 1 / 6f); // Prevent beatTimer overflow
            }

            if (showDebugInfo)
            {
                debugInfo = $"Index: {c_currentIndex}/{c_maxBeat} | BeatIndex: {effectiveBeatIndex}/{c_beatIndexMax} | Swap every {c_beatsPerTick}*{c_ticksPerSwap}={c_beatsPerTick * c_ticksPerSwap} beats | TempoMult: {c_tempoMult}{additionalMultiplierText}{beatTimerText}";
                debugInfo2 = "Tempo Change Times:    ";

                bool multiplyOnTop = cassetteManagerData.Get<bool>("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop");
                if (multiplyOnTop)
                { debugInfo2 = "Tempo Change Times [x Existing]:    "; }

                List<List<object>> multiplierList = cassetteManagerData.Get<List<List<object>>>("EndHelper_CassetteManagerTriggerTempoMultiplierList");
                foreach (List<object> tempoPairList in multiplierList)
                {
                    int beatNum = (int)tempoPairList[0];
                    float tempoNum = (float)tempoPairList[1];

                    if (tempoNum < 0)
                    {
                        debugInfo2 += $"{beatNum}: Reset   ";
                    }
                    else
                    {
                        debugInfo2 += $"{beatNum}: x{tempoNum}   ";
                    }

                }
            }
        }
        else if (wonkyCassettes && level.Tracker.GetEntity<WonkyCassetteBlockController>() is WonkyCassetteBlockController wonkyCassetteBlockManager)
        {
            DynamicData wonkyCassetteManagerData = DynamicData.For(wonkyCassetteBlockManager);

            // Get all the juicy data yum
            int c_introBeats = wonkyCassetteManagerData.Get<int>("introBeats");
            int s_musicBeatIndex = QuantumMechanicsIntegration.QMInte_MusicBeatIndex();
            int s_cassetteBeatIndex = QuantumMechanicsIntegration.QMInte_CassetteBeatIndex();
            int c_maxBeats = wonkyCassetteManagerData.Get<int>("maxBeats");

            int c_bpm = wonkyCassetteManagerData.Get<int>("bpm");
            float c_beatIncrement = wonkyCassetteManagerData.Get<float>("beatIncrement");
            float s_cassetteBeatTimer = QuantumMechanicsIntegration.QMInte_CassetteBeatTimer();
            float s_musicBeatTimer = QuantumMechanicsIntegration.QMInte_MusicBeatTimer();

            String additionalMultiplierText = "";
            float cassettePreviousTempoNum = wonkyCassetteManagerData.Get<float>("EndHelper_CassettePreviousTempoNum");
            if (cassettePreviousTempoNum != 1) { additionalMultiplierText = $" x {cassettePreviousTempoNum}"; }

            String beatTimerText = "";
            if (s_cassetteBeatTimer >= c_beatIncrement * 2)
            {
                beatTimerText = $" [Speed Overflow!]";
            }
            if (s_cassetteBeatTimer > c_beatIncrement * 2)
            {
                QuantumMechanicsIntegration.QMInte_CassetteBeatTimer(c_beatIncrement); // Prevent beatTimer overflow for cassette timer
            }
            if (s_musicBeatTimer > c_beatIncrement * 2)
            {
                QuantumMechanicsIntegration.QMInte_MusicBeatTimer(c_beatIncrement); // Prevent beatTimer overflow for music timer
            }

            if (showDebugInfo)
            {
                debugInfo = $"MusicBeatIndex: {s_musicBeatIndex}/{c_maxBeats} [Loop: {c_introBeats}] | CassetteBeatIndex: {s_cassetteBeatIndex} | BPM: {c_bpm}{additionalMultiplierText}{beatTimerText}";
                debugInfo2 = "Tempo Change Times:    ";

                bool multiplyOnTop = wonkyCassetteManagerData.Get<bool>("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop");
                if (multiplyOnTop)
                { debugInfo2 = "Tempo Change Times [x Existing]:    "; }

                List<List<object>> multiplierList = wonkyCassetteManagerData.Get<List<List<object>>>("EndHelper_CassetteManagerTriggerTempoMultiplierList");
                foreach (List<object> tempoPairList in multiplierList)
                {
                    int beatNum = (int)tempoPairList[0];
                    float tempoNum = (float)tempoPairList[1];

                    if (tempoNum < 0)
                    {
                        debugInfo2 += $"{beatNum}: Reset   ";
                    }
                    else
                    {
                        debugInfo2 += $"{beatNum}: x{tempoNum}   ";
                    }

                }
            }
        }

        base.Update();
    }

    public override void Render()
    {
        if (showDebugInfo)
        {
            ActiveFont.DrawOutline(debugInfo, new Vector2(100, 900), new Vector2(0f, 0f), Vector2.One * 0.6f, Color.Pink, 1f, Color.Black);
            ActiveFont.DrawOutline(debugInfo2, new Vector2(100, 950), new Vector2(0f, 0f), Vector2.One * 0.6f, Color.Pink, 1f, Color.Black);
        }
        base.Render();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnEnter(Player player)
    {
        if (multiplyTempoOnEnter)
        { setTempoMultiplier(multiplyTempoAtBeat, multiplyTempoExisting); }

        SetBeatToIfAllow(setBeatOnEnter);
        base.OnEnter(player);
    }

    public override void OnStay(Player player)
    {
        SetBeatToIfAllow(setBeatInside);
        base.OnStay(player);
    }

    public override void OnLeave(Player player)
    {
        SetBeatToIfAllow(setBeatOnLeave);
        base.OnLeave(player);
    }

    public void SetBeatToIfAllow(int setBeat)
    {
        Level level = SceneAs<Level>();
        if (!wonkyCassettes && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
        {
            DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);
            int c_beatIndexMax = cassetteManagerData.Get<int>("beatIndexMax");
            int effectiveBeatIndex = cassetteManagerData.Get<int>("EndHelper_CassetteManagerTriggerEffectiveBeatIndex");

            // Exit if larger than beatIndexMax
            if (setBeat > c_beatIndexMax)
            { return; }

            // Check if outside setBeatOnlyIfAbove/Under range
            if (effectiveBeatIndex < setBeatOnlyIfAbove || effectiveBeatIndex > setBeatOnlyIfUnder)
            { return; }

            // Check doNotSetIfWithinRange. +ve: Cannot be within that range. -ve: MUST be within that range.
            if (doNotSetIfWithinRange != 0)
            {
                // Difference ignoring loop
                int diff = Math.Abs(effectiveBeatIndex - setBeat);

                // Difference: effectiveBeatIndex is behind by 1 loop
                int diffLoopBehind = Math.Abs(effectiveBeatIndex - setBeat - c_beatIndexMax);

                // Difference: effectiveBeatIndex is ahead by 1 loop
                int diffLoopAhead = Math.Abs(effectiveBeatIndex - setBeat + c_beatIndexMax);

                //Logger.Log(LogLevel.Info, "EndHelper/CassetteManagerTrigger", $"current: {effectiveBeatIndex}, set to {setBeat}. beat difference: {diff}");

                // Get smallest
                if (diff > diffLoopBehind) { diff = diffLoopBehind; }
                if (diff > diffLoopAhead) { diff = diffLoopAhead; }

                if (doNotSetIfWithinRange > 0)
                {
                    // Cannot be within doNotSetIfWithinRange
                    if (diff < doNotSetIfWithinRange){ return; }
                }
                else
                {
                    // Must be within doNotSetIfWithinRange
                    int checkRange = -doNotSetIfWithinRange;
                    if (diff > checkRange) { return; }
                }
            }


            // Change setBeat to the actual set value if addInsteadOfSet
            if (addInsteadOfSet && setBeat != 0)
            {
                bool lockedToPositive = effectiveBeatIndex >= 0;

                // Replace setBeat with an addition to current index
                setBeat = effectiveBeatIndex + setBeat;

                // If effectiveBeatIndex >= 0 and deduct, loop instead
                while (lockedToPositive && setBeat < 0)
                {
                    setBeat += c_beatIndexMax;
                }
            }

            SetBeatTo(setBeat);
        }
        else if (wonkyCassettes && level.Tracker.GetEntity<WonkyCassetteBlockController>() is WonkyCassetteBlockController wonkyCassetteBlockManager)
        {
            DynamicData wonkyCassetteManagerData = DynamicData.For(wonkyCassetteBlockManager);
            int s_musicBeatIndex = QuantumMechanicsIntegration.QMInte_MusicBeatIndex();
            int c_maxBeats = wonkyCassetteManagerData.Get<int>("maxBeats");
            int c_introBeats = wonkyCassetteManagerData.Get<int>("introBeats");

            // Exit if larger than maxBeats
            if (setBeat > c_maxBeats)
            { return; }

            // Check if outside setBeatOnlyIfAbove/Under range
            if (s_musicBeatIndex < setBeatOnlyIfAbove || s_musicBeatIndex > setBeatOnlyIfUnder)
            { return; }

            // Check doNotSetIfWithinRange. +ve: Cannot be within that range. -ve: MUST be within that range.
            if (doNotSetIfWithinRange != 0)
            {
                // Difference ignoring loop
                int diff = Math.Abs(s_musicBeatIndex - setBeat);

                // Difference: effectiveBeatIndex is behind by 1 loop
                int diffLoopBehind = Math.Abs(s_musicBeatIndex - setBeat - c_maxBeats);

                // Difference: effectiveBeatIndex is ahead by 1 loop
                int diffLoopAhead = Math.Abs(s_musicBeatIndex - setBeat + c_maxBeats);

                //Logger.Log(LogLevel.Info, "EndHelper/CassetteManagerTrigger", $"current: {s_musicBeatIndex}, set to {setBeat}. beat difference: {diff}");

                // Get smallest
                if (diff > diffLoopBehind) { diff = diffLoopBehind; }
                if (diff > diffLoopAhead) { diff = diffLoopAhead; }

                if (doNotSetIfWithinRange > 0)
                {
                    // Cannot be within doNotSetIfWithinRange
                    if (diff < doNotSetIfWithinRange) { return; }
                }
                else
                {
                    // Must be within doNotSetIfWithinRange
                    int checkRange = -doNotSetIfWithinRange;
                    if (diff > checkRange) { return; }
                }
            }


            // Change setBeat to the actual set value if addInsteadOfSet
            if (addInsteadOfSet && setBeat != 0)
            {
                bool lockedToAboveLoop = s_musicBeatIndex >= c_introBeats;

                // Replace setBeat with an addition to current index
                setBeat = s_musicBeatIndex + setBeat;

                // If effectiveBeatIndex >= 0 and deduct, loop instead
                while (lockedToAboveLoop && setBeat < c_introBeats)
                {
                    setBeat += c_maxBeats - c_introBeats;
                }
            }

            SetBeatTo(setBeat);
        }
    }

    public void SetBeatTo(int setBeat)
    {
        Level level = SceneAs<Level>();
        if (!wonkyCassettes && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
        {
            DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);
            int c_currentIndex = cassetteManagerData.Get<int>("currentIndex");
            int c_maxBeat = cassetteManagerData.Get<int>("maxBeat");

            int oldbeatIndex = cassetteManagerData.Get<int>("beatIndex"); // Old one, BEFORE the set beat
            int c_beatsPerTick = cassetteManagerData.Get<int>("beatsPerTick");
            int c_ticksPerSwap = cassetteManagerData.Get<int>("ticksPerSwap");

            // Set beat. Different option for negative and positive beats
            if (setBeat < 0)
            {
                cassetteManagerData.Set("leadBeats", -setBeat);
                cassetteManagerData.Set("beatIndex", 0);
            }
            else
            {
                cassetteManagerData.Set("leadBeats", 0);
                cassetteManagerData.Set("beatIndex", setBeat);
            }

            // Set currentIndex (depending on beatsPerTick * ticksPerSwap) to ensure cassette blocks gets synced
            int beatsPerSwap = c_beatsPerTick * c_ticksPerSwap;
            int newCurrentIndex = (int)(Math.Floor(setBeat/beatsPerSwap * 1f) % c_beatsPerTick);
            int newCurrentIndexNext = (newCurrentIndex + 1) % c_beatsPerTick;
            cassetteManagerData.Set("currentIndex", newCurrentIndex);


            // Correct for dumb cassette height stuff
            bool swapToChanging = (setBeat + 1) % beatsPerSwap == 0;


            // If not swapping next beat and also newCurrentIndex is the same as currentIndex then we don't have to do anything.
            // Not doing anything lets there be no transition when there doesn't need to be one so stuff is seemless
            if (swapToChanging == false && c_currentIndex == newCurrentIndex)
            {
                return;
            }

            // Step 1: Reset and Disable everything!
            foreach (CassetteBlock cassetteBlock in base.Scene.Tracker.GetEntities<CassetteBlock>())
            {
                DynamicData cassetteBlockData = DynamicData.For(cassetteBlock);
                Vector2 initialPos = cassetteBlockData.Get<Vector2>("EndHelper_CassetteInitialPos") + new Vector2(0, 2);
                cassetteBlockData.Set("Position", initialPos);
                cassetteBlockData.Set("blockHeight", 0);

                cassetteBlock.Activated = false; // Stop activating.
                cassetteBlock.Collidable = false;
            }
            foreach (CassetteListener component in base.Scene.Tracker.GetComponents<CassetteListener>())
            {
                component.Activated = false; // Just no.
            }


            // Step 2: Appear properly

            // If the swapping happens to a beat before willactive, activate it.
            //Logger.Log(LogLevel.Info, "EndHelper/CassetteManagerTrigger", $"am i swapping TO change? {swapToChanging}");

            if (swapToChanging)
            {
                // Do not set will activate, because that already happens here.
                // Instead, set the will activate to the NEXT one. Umm my brain is too mush to figure out why but it works so shut up
                cassetteBlockManager.SetWillActivate(newCurrentIndexNext);
            }
            else
            {
                cassetteBlockManager.SetWillActivate(newCurrentIndex);
            }
            cassetteBlockManager.SetActiveIndex(newCurrentIndex);
        }
        else if (wonkyCassettes && level.Tracker.GetEntity<WonkyCassetteBlockController>() is WonkyCassetteBlockController wonkyCassetteBlockManager)
        {
            // Set Beats
            DynamicData wonkyCassetteManagerData = DynamicData.For(wonkyCassetteBlockManager);
            int c_introBeats = wonkyCassetteManagerData.Get<int>("introBeats");
            QuantumMechanicsIntegration.QMInte_MusicBeatIndex(setBeat);
            QuantumMechanicsIntegration.QMInte_CassetteBeatIndex(setBeat);

            if (setBeat < c_introBeats)
            { QuantumMechanicsIntegration.QMInte_setMusicLoopStarted(false); /* If set to before loop, set MusicLoopStarted to false */ }
            else
            { QuantumMechanicsIntegration.QMInte_setMusicLoopStarted(true); /* Else true I guess */ }


            // Good news: Wonky cassettes auto-sync when I change the beats. All I have to do is ensure the cassettes reset back to their original positions!
            // Unfortunately that is the hard part!

            // TO-DO:::: toggle nicely pls

            // Step 1: Reset and Disable everything!
            foreach (WonkyCassetteBlock cassetteBlock in base.Scene.Tracker.GetEntities<WonkyCassetteBlock>())
            {
                DynamicData cassetteBlockData = DynamicData.For(cassetteBlock);
                Vector2 initialPos = cassetteBlockData.Get<Vector2>("EndHelper_CassetteInitialPos") + new Vector2(0, 2);
                cassetteBlockData.Set("Position", initialPos);
                cassetteBlockData.Set("blockHeight", 0);

                cassetteBlock.Activated = false; // Stop activating.
                cassetteBlock.Collidable = false;
            }
            foreach (WonkyCassetteListener component in base.Scene.Tracker.GetComponents<WonkyCassetteListener>())
            {
                component.Activated = false; // Just no.
            }


            // Step 2: Appear Properly
            // Since each cassette block has its own swap beats this is going to be tougher :w


        }
    }

    public void setTempoMultiplier(String tempoBeatString, bool multiplyOnTop)
    {
        if (tempoBeatString == "" || tempoBeatString == null)
        { return; }

        Level level = SceneAs<Level>();
        try
        {
            // tempoBeatString is in format 0|1,16|2,40|1.5
            List<string> tempoPairList = tempoBeatString.Split(',')
                .Select(s => s.Trim())                  // Remove extra spaces
                .Where(s => !string.IsNullOrEmpty(s))   // Remove empty strings
                .ToList();

            List<List<object>> tempoChangeTime = [];

            foreach (String beatTempoPairStr in tempoPairList)
            {
                List<String> beatTempoPair = beatTempoPairStr.Split('|')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                int beatNum = int.Parse(beatTempoPair[0]);
                float tempoNum = float.Parse(beatTempoPair[1]);

                tempoChangeTime.Add([beatNum, tempoNum]);
            }

            if (!wonkyCassettes && level.Tracker.GetEntity<CassetteBlockManager>() is CassetteBlockManager cassetteBlockManager)
            {
                DynamicData cassetteManagerData = DynamicData.For(cassetteBlockManager);
                cassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierList", tempoChangeTime);
                cassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop", multiplyOnTop);
                cassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", int.MinValue); // This normally prevents rechecking same beat. If changing multiplier, unset this.
            }

            else if (wonkyCassettes && level.Tracker.GetEntity<WonkyCassetteBlockController>() is WonkyCassetteBlockController wonkyCassetteBlockManager)
            {
                foreach (WonkyCassetteBlockController wonkyCasseteController in level.Tracker.GetEntities<WonkyCassetteBlockController>())
                {
                    Logger.Log(LogLevel.Info, "EndHelper/CassetteManagerTrigger", $"id {wonkyCasseteController.ID}: HEY ARE YOU SETTING THE {tempoBeatString}");

                    DynamicData wonkyCassetteManagerData = DynamicData.For(wonkyCasseteController);
                    wonkyCassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierList", tempoChangeTime);
                    wonkyCassetteManagerData.Set("EndHelper_CassetteManagerTriggerTempoMultiplierMultiplyOnTop", multiplyOnTop);
                    wonkyCassetteManagerData.Set("EndHelper_CassetteHaveCheckedBeat", int.MinValue); // This normally prevents rechecking same beat. If changing multiplier, unset this.
                }
            }
        }
        catch (Exception)
        {
            Logger.Log(LogLevel.Warn, "EndHelper/CassetteManagerTrigger", $"Warning: Invalid string added to multiplyTempoAtBeat: {tempoBeatString}");
        }
    }

    // Currently UNUSED.
    private void RevertWillActivate(int index)
    {
        foreach (CassetteBlock entity in base.Scene.Tracker.GetEntities<CassetteBlock>())
        {
            if (entity.Index == index)
            {
                entity.Collidable = !entity.Collidable;
                entity.WillToggle();
                entity.Collidable = !entity.Collidable;
            }
        }

        foreach (CassetteListener component in base.Scene.Tracker.GetComponents<CassetteListener>())
        {
            if (component.Index == index)
            {
                if (component.Mode == CassetteListener.Modes.WillDisable)
                { component.Mode = CassetteListener.Modes.WillEnable; }
                else if (component.Mode == CassetteListener.Modes.WillEnable)
                { component.Mode = CassetteListener.Modes.WillDisable; }
                component.WillToggle();
                if (component.Mode == CassetteListener.Modes.WillDisable)
                { component.Mode = CassetteListener.Modes.WillEnable; }
                else if (component.Mode == CassetteListener.Modes.WillEnable)
                { component.Mode = CassetteListener.Modes.WillDisable; }
            }
        }
    }
}