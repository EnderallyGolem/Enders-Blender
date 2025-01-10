using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Monocle;
using static MonoMod.InlineRT.MonoModRule;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/RoomSwapChangeRespawnTrigger")]
public class RoomSwapChangeRespawnTrigger : Trigger
{
    private Vector2 Target;
    private Vector2 dataOffset;
    private EntityData entityData;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RoomSwapChangeRespawnTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Collider = new Hitbox(data.Width, data.Height);
        entityData = data;
        dataOffset = offset;
        Visible = Active = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnEnter(Player player)
    {
        //Move target to closest spawn point
        if (entityData.Nodes != null && entityData.Nodes.Length != 0)
        {
            Target = entityData.Nodes[0] + dataOffset;
        }
        else
        {
            Target = Center;
        }
        Target = SceneAs<Level>().GetSpawnPoint(Target);

        base.OnEnter(player);
        Session session = (Scene as Level).Session;
        if (SolidCheck() && (!session.RespawnPoint.HasValue || session.RespawnPoint.Value != Target))
        {
            session.HitCheckpoint = true;
            session.RespawnPoint = Target;
            session.UpdateLevelStartDashes();
            //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionChangeRespawnTrigger", $"Updated respawn point to {Target.X} {Target.Y}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool SolidCheck()
    {
        Vector2 point = Target + Vector2.UnitY * -4f;
        if (Scene.CollideCheck<Solid>(point))
        {
            return Scene.CollideCheck<FloatySpaceBlock>(point);
        }

        return true;
    }
}