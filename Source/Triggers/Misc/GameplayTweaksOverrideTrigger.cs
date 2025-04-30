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

[CustomEntity("EndHelper/GameplayTweaksOverrideTrigger")]
public class GameplayTweaksOverrideTrigger : Trigger
{
    private readonly bool setToDefaultUponLeaving = false;
    private readonly bool activateEnterRoom = false;
    private readonly String requireFlag = "";

    private readonly string preventDownDashRedirects = "Default";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GameplayTweaksOverrideTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        setToDefaultUponLeaving = data.Bool("setToDefaultUponLeaving", false);
        activateEnterRoom = data.Bool("activateEnterRoom", false);
        requireFlag = data.Attr("requireFlag", "");

        preventDownDashRedirects = data.Attr("preventDownDashRedirects", "Default");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        if (activateEnterRoom)
        {
            SetGameplayTweaks(setToDefault: false);
        }
        base.Awake(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnEnter(Player player)
    {
        SetGameplayTweaks(setToDefault: false);
        base.OnEnter(player);
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);
    }

    public override void OnLeave(Player player)
    {
        if (setToDefaultUponLeaving)
        {
            SetGameplayTweaks(setToDefault: true);
        }
        base.OnLeave(player);
    }


    private void SetGameplayTweaks(bool setToDefault = false)
    {
        // Flag Check
        Level level = SceneAs<Level>();
        bool allowFunctionality = Utils_General.AreFlagsEnabled(level.Session, requireFlag, true);
        if (!allowFunctionality) { return; }

        // Set stuff

        // Prevent down dash redirect
        if (setToDefault)
        {
            EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = null;
        }
        else
        {
            Logger.Log(LogLevel.Info, "EndHelper/GameplayTweaksOverrideTrigger", $"is this running... {preventDownDashRedirects}");
            switch (preventDownDashRedirects)
            {
                case "Default":
                    EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = null;
                    break;
                case "Disabled":
                    EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = ConvertDemoEnum.Disabled;
                    break;
                case "EnabledNormal":
                    EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = ConvertDemoEnum.EnabledNormal;
                    break;
                case "EnabledDiagonal":
                    EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = ConvertDemoEnum.EnabledDiagonal;
                    break;
                default:
                    EndHelperModule.Session.GameplayTweaksOverride_ConvertDemo = null;
                    break;
            }
        }
    }
}