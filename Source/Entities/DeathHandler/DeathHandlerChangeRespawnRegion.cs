using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Celeste.ClutterBlock;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[Tracked(false)]
[CustomEntity("EndHelper/DeathHandlerChangeRespawnRegion")]
public class DeathHandlerChangeRespawnRegion : Solid
{
    internal EntityID entityID;

    internal bool checkSolid = true;
    internal bool attachable = true;
    internal bool fullReset = false;
    internal bool killOnEnter = false;

    internal bool visibleArea = true;
    internal bool visibleTarget = true;

    internal Vector2 targetSpawnpoint;
    private readonly Vector2 targetSpawnpointOffset = Vector2.Zero;

    internal SimpleCurve visualLine1;
    internal SimpleCurve visualLine2;
    internal SimpleCurve visualLine3;
    Color visualLineColour = new Color();
    internal float visualLineThickness = 1f;

    private Vector2? lineEndPos = null;

    internal static EntityID? triggerChangeRespawnDeathEntityID; // When changing respawn in death zones, set this. This is checked upon add.
                                                                 // Purpose of this is to allow effect to occur if the player dies immediately

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerChangeRespawnRegion(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        Collidable = false;
        this.BlockWaterfalls = false;
        this.AllowStaticMovers = false;
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

        if (data.Nodes.Length > 0) targetSpawnpointOffset = data.Nodes[0] + offset - Position;

        entityID = id;
        if (attachable)
        {
            Add(new StaticMover
            {
                OnShake = OnShake,
                SolidChecker = IsRidingSolid,
                OnEnable = OnEnable,
                OnDisable = OnDisable,
                OnDestroy = OnDestroy
            });
        }

        if (!visibleArea) Visible = false;
    }

    private void OnEnable()
    {
        Level level = SceneAs<Level>();
        Active = true;
        Visible = visibleArea;
        level.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Track(this);
    }

    private void OnDisable()
    {
        Level level = SceneAs<Level>();
        Active = false;
        Visible = false;
        level.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Untrack(this);
    }

    private void OnDestroy()
    {
        Level level = SceneAs<Level>();
        level.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Untrack(this);
        RemoveSelf();
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegion", $"it destroyed");
    }

    private bool IsRidingSolid(Solid solid)
    {
        Collidable = true;
        Collider origCollider = base.Collider;
        base.Collider = new Hitbox(Width+2, Height+2, -1, -1);
        bool collideCheck = CollideCheck(solid);
        Collidable = false;
        base.Collider = origCollider;
        return collideCheck;
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

        // If triggerChangeRespawnDeathEntityID == entityID, this means effect was triggered right before death. Show effect.
        if (triggerChangeRespawnDeathEntityID is not null && triggerChangeRespawnDeathEntityID.Value.ID == entityID.ID)
        {
            triggerChangeRespawnDeathEntityID = null;
            DisplayVisualLineEffect();
        }
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

    public IEnumerator OnDeathBypass()
    {
        Level level = SceneAs<Level>();
        int nullReturnCount = 0;
        while (level.Tracker.CountEntities<DeathHandlerChangeRespawnRegionRenderer>() == 0)
        {
            yield return null;
            nullReturnCount++;
            // If repeatedly null, add the renderer
            if (nullReturnCount >= 3) level.Add(new DeathHandlerChangeRespawnRegionRenderer());
        }
        level.Tracker.GetEntity<DeathHandlerChangeRespawnRegionRenderer>().Track(this);
        yield break;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        Player player = base.Scene.Tracker.GetEntity<Player>();
        Rectangle regionRect = this.HitRect();

        if (fullReset && EndHelperModule.Session.lastFullResetPos is not null)
        {
            targetSpawnpoint = EndHelperModule.Session.lastFullResetPos.Value;
        }
        else
        {
            targetSpawnpoint = SceneAs<Level>().GetSpawnPoint(regionRect.Center.ToVector2() + targetSpawnpointOffset);
        }
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegion", $"{entityID} >> targetSpawnpoint: {targetSpawnpoint}");
        UpdateVisualLine(player);

        if (player is not null && !player.Dead)
        {
            Rectangle playerRect = new Rectangle((int)(player.Position.X - player.Width / 2), (int)(player.Position.Y - player.Height), (int)player.Width, (int)player.Height);
            if (regionRect.Intersects(playerRect))
            {
                PlayerCollide(player);
            }
        }
        base.Update();
    }

    public void PlayerCollide(Player player)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegion", $"it continues to collide {pos}");
        Vector2 playerPosMiddle = new Vector2((player.Position.X - player.Width / 2), (player.Position.Y - player.Height));

        Level level = SceneAs<Level>();
        bool nextIsFullReset = EndHelperModule.Session.nextRespawnFullReset;
        bool changeRespawnSuccess = Utils_DeathHandler.UpdateRespawnPos(targetSpawnpoint, level, checkSolid, fullReset);
        if (EndHelperModule.Session.nextRespawnFullReset != nextIsFullReset) changeRespawnSuccess = true;

        if (killOnEnter && (changeRespawnSuccess || DeathHandlerRespawnMarker.attachedToPlayer && fullReset))
        {
            triggerChangeRespawnDeathEntityID = entityID; // Schedule a VisualLineEffect to occur after death. If player is bypass + full reset, don't need to be a success.
        }
        else if (changeRespawnSuccess)
        {
            DisplayVisualLineEffect();
        }

        if (killOnEnter)
        {
            Vector2 dieDir = (player.Position - new Vector2(Position.X + Width / 2, Position.Y + Height / 2)).SafeNormalize() / 5;
            Rectangle regionRect = new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
            if (player.Position.X < regionRect.Left) dieDir += new Vector2(-1, 0);
            if (player.Position.X > regionRect.Right) dieDir += new Vector2(1, 0);
            if (player.Position.Y < regionRect.Top) dieDir += new Vector2(0, -1);
            if (player.Position.Y > regionRect.Bottom) dieDir += new Vector2(0, 1);
            if (!visibleArea) dieDir = Vector2.Zero;

            dieDir = dieDir.SafeNormalize();
            if (fullReset) Utils_DeathHandler.ForceShortDeathCooldown();
            player.Die(dieDir);
        }
    }

    private int visualLineEffectDuration = 0;
    public void DisplayVisualLineEffect()
    {
        if (!visibleTarget) return;

        visualLineEffectDuration = 128;
        Add(new SoundSource("event:/game/03_resort/forcefield_bump"));
    }

    public void UpdateVisualLine(Player player)
    {
        if (!visibleTarget) return;
        if (visualLineEffectDuration > 0) visualLineEffectDuration--;

        Vector2 startPos = this.HitRect().Center.ToVector2();
        Vector2 endAimPos = targetSpawnpoint + new Vector2(0, -8);

        if (lineEndPos is null) lineEndPos = endAimPos;
        lineEndPos = Vector2.Lerp(lineEndPos.Value, endAimPos, 0.2f);
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegion", $"{entityID} >> lineEndPos {lineEndPos} | endAimPos {endAimPos}");

        float lengthDifference = (startPos - lineEndPos.Value).Length();
        Vector2 perpendicularDirection = (startPos - lineEndPos.Value).SafeNormalize().Rotate((float)Math.PI/2);

        Vector2 middlePos = startPos / 2 + lineEndPos.Value / 2;
        Vector2 line1controlPos = middlePos + perpendicularDirection * (float)Math.Sin(Engine.Scene.TimeActive * 0.7 + entityID.ID) * lengthDifference / 10;
        Vector2 line2controlPos = middlePos + perpendicularDirection * (float)Math.Sin(Engine.Scene.TimeActive * 1.3 + entityID.ID + 0.5) * lengthDifference / 15;
        Vector2 line3controlPos = middlePos + perpendicularDirection * (float)Math.Sin(Engine.Scene.TimeActive * 1.7 + entityID.ID + 1) * lengthDifference / 20;

        if (fullReset)
        {
            visualLineColour = new Color(255, 51, 0, 1);
        }
        else
        {
            visualLineColour = new Color(102, 255, 102, 1);
        }
        float transparencyMultiplier = 0.15f;
        if (player is not null)
        {
            transparencyMultiplier = 0.15f + (1 - (Math.Clamp((player.Center - startPos).Length(), 20, 100) - 20) / 80) * 0.3f;
        }

        // Fade in when transitioning into room
        if (Utils_General.framesSinceEnteredRoom < 30)
        {
            transparencyMultiplier *= Utils_General.framesSinceEnteredRoom / 30;
        }

        // Activation visual effects
        if (visualLineEffectDuration > 0)
        {
            Color activeColour = new Color(255, 255, 128, 1);
            visualLineColour = Color.Lerp(visualLineColour, activeColour,  (float)visualLineEffectDuration / 100);
            transparencyMultiplier = float.Lerp(transparencyMultiplier, 1, (float)visualLineEffectDuration / 100);

            if (visualLineEffectDuration >= 118)
            {
                line1controlPos = Vector2.Lerp(middlePos, line1controlPos, (float)(visualLineEffectDuration - 118) / 10);
                line2controlPos = Vector2.Lerp(middlePos, line2controlPos, (float)(visualLineEffectDuration - 118) / 10);
                line3controlPos = Vector2.Lerp(middlePos, line3controlPos, (float)(visualLineEffectDuration - 118) / 10);
                visualLineThickness = float.Lerp(3, 1, (float)(visualLineEffectDuration - 118) / 10);
            }
            else
            {
                line1controlPos = Vector2.Lerp(line1controlPos, middlePos, (float)visualLineEffectDuration / 100);
                line2controlPos = Vector2.Lerp(line2controlPos, middlePos, (float)visualLineEffectDuration / 100);
                line3controlPos = Vector2.Lerp(line3controlPos, middlePos, (float)visualLineEffectDuration / 100);
                visualLineThickness = float.Lerp(1, 3, (float)visualLineEffectDuration / 100);
            }

            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegion", $"visualLineEffectDuration: {visualLineEffectDuration} | transparencyMultiplier: {transparencyMultiplier}");
        }

        visualLine1 = new SimpleCurve(startPos, lineEndPos.Value, line1controlPos);
        visualLine2 = new SimpleCurve(startPos, lineEndPos.Value, line2controlPos);
        visualLine3 = new SimpleCurve(startPos, lineEndPos.Value, line3controlPos);
        visualLineColour *= transparencyMultiplier;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        if (visibleTarget)
        {
            visualLine1.Render(Vector2.Zero, visualLineColour, 30, visualLineThickness);
            visualLine2.Render(Vector2.Zero, visualLineColour * 0.8f, 30, visualLineThickness);
            visualLine3.Render(Vector2.Zero, visualLineColour * 0.6f, 30, visualLineThickness);
        }
        base.Render();
    }
}
