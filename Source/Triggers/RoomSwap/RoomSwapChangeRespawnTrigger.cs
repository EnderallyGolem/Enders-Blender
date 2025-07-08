using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Monocle;
using static MonoMod.InlineRT.MonoModRule;
using System.Threading.Tasks;
using Celeste.Mod.EndHelper.Utils;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/RoomSwapChangeRespawnTrigger")]
public class RoomSwapChangeRespawnTrigger : Trigger
{
    private Vector2 Target;
    private Vector2 dataOffset;
    private EntityData entityData;

    private bool checkSolid = true;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RoomSwapChangeRespawnTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        checkSolid = data.Bool("checkSolid", true);
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
        Level level = SceneAs<Level>();

        //Move target to closest spawn point
        if (entityData.Nodes != null && entityData.Nodes.Length != 0)
        {
            Target = entityData.Nodes[0] + dataOffset;
        }
        else
        {
            Target = Center;
        }

        Utils_DeathHandler.UpdateRespawnPos(Target, level, checkSolid);
    }
}