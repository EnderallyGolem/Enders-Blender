using Celeste.Mod.EndHelper.SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using Celeste.Mod.EndHelper.Utils;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[Tracked(false)]
[CustomEntity("EndHelper/DeathHandlerRespawnPoint")]
public class DeathHandlerRespawnPoint : Entity
{
    internal readonly bool faceLeft = false; // Handled in EndHelperModule OnPlayerSpawnFunc everest event.
    private readonly bool visible = true;
    private readonly bool attachable = true;
    internal readonly bool fullReset = false;
    private readonly string requireFlag = "";
    private readonly bool checkInvalid = true;
    private readonly string flagWhenSpawnpoint = "";

    private readonly MTexture currentSpawnpointTexture;
    private readonly MTexture inactiveTexture;
    private Image displayImage;

    const int width = 16;
    const int height = 18;

    private bool currentlySpawnpoint = false;
    internal bool disabled = false;
    public Vector2 entityPosCenter;
    public Vector2 entityPosSpawnPoint;
    public Vector2 entityPosSpawnPointPrevious;

    private Vector2 imageOffset = Vector2.Zero;
    public EntityID entityID;

    public DeathHandlerRespawnPoint(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
    {
        // This entity, if found, has its position checked whenever GetSpawnPoint is ran
        // It is not in LevelData.Spawns, because dealing with a game-loaded list together with room-loaded positions sounds like a disaster waiting to happen
        faceLeft = data.Bool("faceLeft", false);
        visible = data.Bool("visible", true);
        attachable = data.Bool("attachable", true);
        fullReset = data.Bool("fullReset", false);
        requireFlag = data.Attr("requireFlag", "");
        checkInvalid = data.Bool("checkSolid", true);
        flagWhenSpawnpoint = data.Attr("flagWhenSpawnpoint", "");

        entityID = id;

        if (fullReset)
        {
            Utils_DeathHandler.EnableDeathHandlerEntityChecks();
            currentSpawnpointTexture = GFX.Game["objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_fullreset_active"];
            inactiveTexture = GFX.Game["objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_fullreset_inactive"];
        }
        else
        {
            currentSpawnpointTexture = GFX.Game["objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_normal_active"];
            inactiveTexture = GFX.Game["objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_normal_inactive"];
        }
        UpdatePositionVectors(firstUpdate: true);

        base.Collider = new Hitbox(x: -2 - width/2, y: -2 - height/2, width:width + 4, height:height + 4);

        if (attachable)
        {
            Add(new StaticMover
            {
                OnShake = OnShake,
                SolidChecker = IsRidingSolid,
                JumpThruChecker = IsRidingJumpthrough,
            });
        }

        Depth = 2;
    }

    private void OnShake(Vector2 amount)
    {
        imageOffset += amount;
        UpdateImage();
    }
    private bool IsRidingSolid(Solid solid)
    {
        return CollideCheck(solid, Position + Vector2.UnitY);
    }
    private bool IsRidingJumpthrough(JumpThru jumpThru)
    {
        return CollideCheck(jumpThru, Position + Vector2.UnitY);
    }

    public override void Added(Scene scene)
    {
        (scene as Level).Session.SetFlag(flagWhenSpawnpoint, false, true);
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        // Level level = SceneAs<Level>();
        UpdateImage();
        base.Awake(scene);
    }

    public override void Update()
    {
        UpdatePositionVectors();
        base.Update();

        Level level = SceneAs<Level>();

        // Disable if flag says no
        if (Utils_General.AreFlagsEnabled(SceneAs<Level>().Session, requireFlag))
        {
            disabled = false;
        }
        else
        {
            disabled = true;
        }


        // Set to active if spawnpoint is there, else inactive
        bool currentPointIsSpawnpoint = false;
        if (!disabled)
        {
            if (entityPosSpawnPoint == level.Session.RespawnPoint || entityPosSpawnPointPrevious == level.Session.RespawnPoint)
            {
                ChangeActiveness(true);

                // i changed my mind i want them to show as active even with player deathbypass
                //if (level.Tracker.GetEntity<Player>() is Player player && player.Components.Get<DeathBypass>() is DeathBypass deathBypass && deathBypass.bypass
                //    && !fullReset)
                //{
                //    // If player has deathbypass and this isn't full reset, respawn point is at the player. Don't show as active.
                //    ChangeActiveness(false);
                //}
                //else
                //{
                //    ChangeActiveness(true);
                //}
                level.Session.RespawnPoint = entityPosSpawnPoint;
                if (fullReset)
                {
                    Utils_DeathHandler.SetFullResetPos(level.Session.RespawnPoint);
                }

                UpdateMarkerDirections(level);
                currentPointIsSpawnpoint = true;
            }

            else if (fullReset && entityPosSpawnPoint == EndHelperModule.Session.lastFullResetPos || entityPosSpawnPointPrevious == EndHelperModule.Session.lastFullResetPos)
            {
                // Special case for full Reset: Lets the lastFullResetPos update even if currently not the spawnpoint
                Utils_DeathHandler.SetFullResetPos(entityPosSpawnPoint);
            }
        }
        if (!currentPointIsSpawnpoint)
        {
            ChangeActiveness(false);
        }
    }

    private void UpdateMarkerDirections(Level level)
    {
        // If using a DeathHandlerRespawnMarker, set its direction 
        foreach (DeathHandlerRespawnMarker respawnMarker in level.Tracker.GetEntities<DeathHandlerRespawnMarker>())
        {
            if (respawnMarker.showRedEffects && !fullReset) return;

            if (fullReset) respawnMarker.fullResetFaceLeft = faceLeft;
            respawnMarker.faceLeft = faceLeft;
            respawnMarker.UpdateSprite();

            // If moving and Marker is near, lock position
            if (entityPosSpawnPoint != entityPosSpawnPointPrevious && respawnMarker.previousDistanceBetweenPosAndTarget <= 8 && respawnMarker.previousDistanceBetweenPosAndTarget != 0)
            {
                respawnMarker.Position = DeathHandlerRespawnMarker.ConvertSpawnPointPosToActualPos(entityPosSpawnPoint);
            }
        }
    }

    private void UpdatePositionVectors(bool firstUpdate = false)
    {
        Level level = SceneAs<Level>();

        Rectangle respawnPointCheckRect = this.HitRect(width, height);

        entityPosCenter = new Vector2(Position.X + width / 2, Position.Y);
        entityPosSpawnPointPrevious = entityPosSpawnPoint;

        // Do not update entityPosSpawnPoint if it is in an invalid respawn spot
        if (firstUpdate == false && !Utils_DeathHandler.NoInvalidCheck(level, respawnPointCheckRect, checkInvalid, inflate: -4)) return;

        entityPosSpawnPoint = new Vector2(Position.X, Position.Y + height / 2 - 1);
        if (firstUpdate)
        {
            entityPosSpawnPointPrevious = entityPosSpawnPoint;
        }
    }

    public override void Render()
    {
        base.Render();
    }


    public void ChangeActiveness(bool? newActiveness = null)
    {
        if (newActiveness == null)
        {
            currentlySpawnpoint = !currentlySpawnpoint;
        }
        else if (newActiveness.Value)
        {
            currentlySpawnpoint = true;
        }
        else if (newActiveness.Value == false)
        {
            currentlySpawnpoint = false;
        }

        Session session = SceneAs<Level>().Session;
        if (currentlySpawnpoint) session.SetFlag(flagWhenSpawnpoint, true, true);
        else session.SetFlag(flagWhenSpawnpoint, false, true);

        UpdateImage();
    }
    public void UpdateImage()
    {
        if (visible)
        {
            Components.Remove(displayImage);
            if (currentlySpawnpoint)
            {
                displayImage = new Image(currentSpawnpointTexture);
            }
            else
            {
                displayImage = new Image(inactiveTexture);
            }
            displayImage.Position -= new Vector2(width / 2, (height / 2 + 1));
            if (faceLeft)
            {
                displayImage.FlipX = true;
            }

            displayImage.Position += imageOffset;

            if (disabled)
            {
                displayImage.Color.A = (byte)(displayImage.Color.R * 0.7);
                displayImage.Color.R = (byte)(displayImage.Color.R * 0.4);
                displayImage.Color.G = (byte)(displayImage.Color.G * 0.4);
                displayImage.Color.B = (byte)(displayImage.Color.B * 0.4);
            }
            Add(displayImage);
        }
    }

    public override void Removed(Scene scene)
    {
        (scene as Level).Session.SetFlag(flagWhenSpawnpoint, false, true);
        base.Removed(scene);
    }
}
