using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[Tracked(false)]
[CustomEntity("EndHelper/DeathHandlerChangeRespawnRegion")]
public class DeathHandlerChangeRespawnRegion : Solid
{
    public float Flash;
    public bool Flashing;
    internal EntityID entityID;

    internal bool checkSolid = true;
    internal bool attachable = true;
    internal bool fullReset = false;
    internal bool killOnEnter = false;

    internal bool visibleArea = true;
    internal bool visibleTarget = true;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerChangeRespawnRegion(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        Collidable = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerChangeRespawnRegion(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, data.Width, data.Height)
    {
        this.checkSolid = data.Bool("checkSolid", true);
        this.attachable = data.Bool("attachable", true);
        this.fullReset = data.Bool("fullReset", false);
        this.killOnEnter = data.Bool("killOnEnter", false);

        this.visibleArea = data.Bool("visibleArea", true);
        this.visibleTarget = data.Bool("visibleTarget", true);

        entityID = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);

        // Add render if the level didn't already have one
        Level level = scene as Level;
        if (level.Tracker.CountEntities<DeathHandlerChangeRespawnRegionRenderer>() == 0)
        { level.Add(new DeathHandlerChangeRespawnRegionRenderer()); }
    }

    public override void Awake(Scene scene)
    {
        scene.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Track(this);
        base.Awake(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (scene.Tracker.CountEntities<DeathHandlerChangeRespawnRegionRenderer>() > 0)
        {
            scene.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Untrack(this);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        if (Flashing)
        {
            Flash = Calc.Approach(Flash, 0f, Engine.DeltaTime * 4f);
            if (Flash <= 0f)
            {
                Flashing = false;
            }
        }
        base.Update();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        if (!IsVisible())
        {
            return;
        }

        Color color = Color.White * 0.5f;

        if (Flashing)
        {
            Draw.Rect(base.Collider, Color.White * Flash * 0.5f);
        }
    }

    private bool IsVisible()
    {
        return CullHelper.IsRectangleVisible(Position.X, Position.Y, base.Width, base.Height);
    }
}
