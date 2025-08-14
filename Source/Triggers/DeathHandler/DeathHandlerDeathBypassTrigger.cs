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
using static Celeste.Mod.EndHelper.EndHelperModule;

namespace Celeste.Mod.EndHelper.Triggers.DeathHandler;

[CustomEntity("EndHelper/DeathHandlerDeathBypassTrigger")]
public class DeathHandlerDeathBypassTrigger : Trigger
{
    private readonly String requireFlag = "";
    private readonly bool showVisuals = true;
    private readonly Vector2[] nodes;

    private readonly Rectangle triggerRange;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerDeathBypassTrigger(EntityData data, Vector2 offset, EntityID id)
        : base(data, offset)
    {
        nodes = data.NodesOffset(offset);
        requireFlag = data.Attr("requireFlag", "");
        showVisuals = data.Bool("showVisuals", true);

        triggerRange = new Rectangle((int)(data.Position.X + offset.X), (int)(data.Position.Y + offset.Y), data.Width, data.Height);
        GoldenRipple.enableShader = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (Active)
        {
            Level level = SceneAs<Level>();

            // Give all entities in its range + closest entity at its nodes the DeathBypass component
            foreach (Entity entity in level.Entities)
            {
                // Not collider in case no collision. And also i haven't figured out how to use collider yet :p
                Rectangle entityRect = new Rectangle((int)entity.Position.X, (int)entity.Position.Y, (int)entity.Width, (int)entity.Height);
                //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerDeathBypassTrigger", $"Compare entity {entity}: {entityRect.Left} {entityRect.Top} with trigger {triggerRange.Left} {triggerRange.Top}");
                if ((triggerRange.Intersects(entityRect) || CollideCheck(entity)) && entity.Components.Get<DeathBypass>() is null && FilterEntity(entity))
                {
                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerDeathBypassTrigger", $"Trigger Range: Add {entity} to DeathBypass");
                    DeathBypass deathBypassComponent = new DeathBypass(requireFlag, showVisuals);
                    entity.Add(deathBypassComponent);
                }
            }

            // Nodes
            foreach (Vector2 nodePos in nodes)
            {
                foreach (Entity entity in level.Entities)
                {
                    // Not collider in case no collision. And also i haven't figured out how to use collider yet :p
                    Rectangle entityRect = new Rectangle((int)entity.Position.X, (int)entity.Position.Y, (int)entity.Width, (int)entity.Height);
                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerDeathBypassTrigger", $"Compare entity {entity}: {entityRect.Left} {entityRect.Top} with trigger {triggerRange.Left} {triggerRange.Top}");
                    if ((entityRect.Contains((int)nodePos.X, (int)nodePos.Y) || Collide.CheckPoint(entity, nodePos)) && entity.Components.Get<DeathBypass>() is null && FilterEntity(entity))
                    {
                        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerDeathBypassTrigger", $"Trigger Range: Add {entity} to DeathBypass");
                        DeathBypass deathBypassComponent = new DeathBypass(requireFlag, showVisuals);
                        entity.Add(deathBypassComponent);
                    }
                }
            }
        }
    }

    internal static bool FilterEntity(Entity entity, bool allowPlayer = false, bool allowPreventChange = true)
    {
        if (entity is Trigger || entity is SolidTiles || entity is BackgroundTiles || (entity is Player && !allowPlayer) || entity is PlayerDeadBody 
            || entity is DeathHandlerDeathBypassTrigger || entity is Killbox)
        {
            return false;
        }
        if (integratingWithFrostHelper)
        {
            if (FrostHelperIntegration.CheckIfCustomSpinner(entity)) return false; // I am not figuring out how frost helper spinners work and potentially break stuff for this
        }
        if (entity is not Player && (entity.TagCheck(Tags.HUD) || entity.TagCheck(Tags.Global)))
        {
            return false;
        }
        if (!allowPreventChange && entity.Components.Get<DeathBypass>() is DeathBypass deathBypass && deathBypass.preventChange)
        {
            return false;
        }

        return true;
    }
}