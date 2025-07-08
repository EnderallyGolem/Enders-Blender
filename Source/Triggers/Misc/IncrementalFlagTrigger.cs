using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.EndHelper.Utils;
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
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/IncrementalFlagTrigger")]
public class IncrementalFlagTrigger : Trigger
{
    private readonly String flag;
    private readonly int setValue;
    private readonly bool setOnlyIfOneBelow;
    private readonly String requireFlag;
    private readonly bool singleUse;
    private readonly bool temporary;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IncrementalFlagTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        flag = data.Attr("flag", "");
        setValue = data.Int("setValue", 1);
        setOnlyIfOneBelow = data.Bool("setOnlyIfOneBelow", true);
        requireFlag = data.Attr("requireFlag", "");
        singleUse = data.Bool("singleUse", true);
        temporary = data.Bool("temporary", true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (temporary)
        {
            SetFlagCounter(0);
        }
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnEnter(Player player)
    {
        if (!Utils_General.AreFlagsEnabled(player.level.Session, requireFlag, true)) 
            return;
        if (setOnlyIfOneBelow && setValue != 0 && !(GetFlagCounter() == setValue - 1))
            return;

        SetFlagCounter(setValue);

        if (singleUse) RemoveSelf();
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
    }

    internal void SetFlagCounter(int value)
    {
        Level level = SceneAs<Level>();

        int oldValue = GetFlagCounter();

        level.Session.SetFlag($"{flag}{oldValue}", false);
        level.Session.SetCounter(flag, value);
        level.Session.SetFlag($"{flag}{value}", true);
    }

    internal int GetFlagCounter()
    {
        Level level = SceneAs<Level>();
        return level.Session.GetCounter(flag);
    }
}