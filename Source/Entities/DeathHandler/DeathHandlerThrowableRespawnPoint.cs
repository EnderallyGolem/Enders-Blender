using AsmResolver.PE.DotNet.ReadyToRun;
using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[Tracked(false)]
[TrackedAs(typeof(DeathHandlerRespawnPoint))]
[CustomEntity("EndHelper/DeathHandlerThrowableRespawnPoint")]
public class DeathHandlerThrowableRespawnPoint : Actor
{
    internal bool faceLeft = false;
    internal readonly bool fullReset = false;
    private readonly string requireFlag = "";
    public EntityID entityID;

    public Vector2 entityPosSpawnPointPrevious;
    public Vector2 entityPosSpawnPoint;
    internal bool disabled = false;
    private bool currentlySpawnpoint = false;

    const int width = 20;
    const int height = 22;
    private bool blockDirectionUpdate = false;

    private readonly ParticleType P_Impact;

    public Vector2 Speed;

    public bool OnPedestal;

    public Holdable Hold;

    public Sprite sprite;
    public bool dead;
    public Level Level;
    public Collision onCollideH;
    public Collision onCollideV;
    public float noGravityTimer;
    public Vector2 prevLiftSpeed;
    public Vector2 previousPosition;
    public HoldableCollider hitSeeker;
    public float swatTimer;
    public bool shattering;
    public float hardVerticalHitSoundCooldown;

    public DeathHandlerThrowableRespawnPoint(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
    {
        // This entity, if found, has its position checked whenever GetSpawnPoint is ran
        // It is not in LevelData.Spawns, because dealing with a game-loaded list together with room-loaded positions sounds like a disaster waiting to happen
        fullReset = data.Bool("fullReset", false);
        requireFlag = data.Attr("requireFlag", "");
        faceLeft = data.Bool("initialFaceLeft", false);
        entityID = id;

        Add(sprite = EndHelperModule.SpriteBank.Create("DeathHandlerThrowableRespawnPoint"));
        if (faceLeft) sprite.FlipX = true;
        orig_ctor(Position);

        Hold.SpeedSetter = delegate (Vector2 speed)
        {
            Speed = speed;
        };

        UpdatePositionVectors(firstUpdate: true);

        if (fullReset)
        {
            Utils_DeathHandler.EnableDeathHandlerEntityChecks();
            P_Impact = new ParticleType
            {
                Color = Calc.HexToColor("BF5764"),
                Size = 1f,
                FadeMode = ParticleType.FadeModes.Late,
                DirectionRange = 1.74532926f,
                SpeedMin = 10f,
                SpeedMax = 20f,
                SpeedMultiplier = 0.1f,
                LifeMin = 0.3f,
                LifeMax = 0.8f
            };
        }
        else
        {
            P_Impact = new ParticleType
            {
                Color = Calc.HexToColor("7FC18F"),
                Size = 1f,
                FadeMode = ParticleType.FadeModes.Late,
                DirectionRange = 1.74532926f,
                SpeedMin = 10f,
                SpeedMax = 20f,
                SpeedMultiplier = 0.1f,
                LifeMin = 0.3f,
                LifeMax = 0.8f
            };
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (fullReset) sprite.Play("fullreset_inactive");
        else sprite.Play("normal_inactive");

        Level = SceneAs<Level>();
        foreach (DeathHandlerThrowableRespawnPoint entity in Level.Tracker.GetEntities<DeathHandlerThrowableRespawnPoint>())
        {
            if (entity != this && entity.SourceId.Match(this.SourceId) && entity.Hold.IsHeld)
            {
                RemoveSelf();
            }
        }
    }

    public override void Update()
    {
        TheoUpdate(); 
        blockDirectionUpdate = false;
        UpdatePositionVectors();

        if (Speed.X > 0) faceLeft = false;
        if (Speed.X < 0) faceLeft = true;
        if (faceLeft) sprite.FlipX = true;
        else sprite.FlipX = false;

        if (Utils_General.AreFlagsEnabled(SceneAs<Level>().Session, requireFlag)) disabled = false;
        else disabled = true;

        // Set to active if spawnpoint is there, else inactive
        Level level = SceneAs<Level>();
        bool currentPointIsSpawnpoint = false;
        if (!disabled)
        {
            if (entityPosSpawnPoint == level.Session.RespawnPoint || entityPosSpawnPointPrevious == level.Session.RespawnPoint)
            {
                if (level.Tracker.GetEntity<Player>() is Player player && player.Components.Get<DeathBypass>() is DeathBypass deathBypass && deathBypass.bypass
                    && !fullReset)
                {
                    // If player has deathbypass and this isn't full reset, respawn point is at the player. Don't show as active.
                    ChangeActiveness(false);
                }
                else
                {
                    ChangeActiveness(true);
                }
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

        base.Update();
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

        if (currentlySpawnpoint)
        {
            if (fullReset) sprite.Play("fullreset_active");
            else sprite.Play("normal_active");
        }
        else
        {
            if (fullReset) sprite.Play("fullreset_inactive");
            else sprite.Play("normal_inactive");
        }
    }

    private void UpdatePositionVectors(bool firstUpdate = false, bool allowInvalid = false)
    {
        Level level = SceneAs<Level>();

        Rectangle respawnPointCheckRect = this.HitRect(useCollider: true);
        respawnPointCheckRect.Y -= 4;
        respawnPointCheckRect.Height += 4;

        entityPosSpawnPointPrevious = entityPosSpawnPoint;

        // Do not update entityPosSpawnPoint if it is in an invalid respawn spot
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerThrowableRespawnPoint", $"y comparising: {respawnPointCheckRect.Y} {level.Bounds.Bottom}");
        if (firstUpdate == false && (respawnPointCheckRect.Y + 12 > level.Bounds.Bottom || !Utils_DeathHandler.NoSolidCheck(level, respawnPointCheckRect, !allowInvalid, inflate: 0))) 
        {
            blockDirectionUpdate = true;
            return;
        }

        if (faceLeft) entityPosSpawnPoint = new Vector2(Position.X - 1, Position.Y - 1);
        else entityPosSpawnPoint = new Vector2(Position.X + 1, Position.Y - 1);
        if (firstUpdate)
        {
            entityPosSpawnPointPrevious = entityPosSpawnPoint;
        }
    }

    private void UpdateMarkerDirections(Level level)
    {
        // If using a DeathHandlerRespawnMarker, set its direction 
        foreach (DeathHandlerRespawnMarker respawnMarker in level.Tracker.GetEntities<DeathHandlerRespawnMarker>())
        {
            if (!blockDirectionUpdate)
            {
                respawnMarker.faceLeft = faceLeft;
                respawnMarker.UpdateSprite();
            }

            // If moving and Marker is near, lock position
            if (entityPosSpawnPoint != entityPosSpawnPointPrevious && respawnMarker.previousDistanceBetweenPosAndTarget <= 8 && respawnMarker.previousDistanceBetweenPosAndTarget != 0 && !DeathHandlerRespawnMarker.attachedToPlayer)
            {
                respawnMarker.Position = respawnMarker.ConvertSpawnPointPosToActualPos(entityPosSpawnPoint);
            }
        }
    }

    internal void Die()
    {
        if (!dead)
        {
            dead = true;
            sprite.Visible = false;
            base.Depth = -1000000;
            AllowPushing = false;
            RemoveSelf();
        }
    }

    #region Theo Stuff

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TheoUpdate()
    {
        if (shattering || dead)
        {
            return;
        }

        if (swatTimer > 0f)
        {
            swatTimer -= Engine.DeltaTime;
        }

        hardVerticalHitSoundCooldown -= Engine.DeltaTime;
        if (OnPedestal)
        {
            base.Depth = 8999;
            return;
        }

        base.Depth = 100;
        if (Hold.IsHeld)
        {
            prevLiftSpeed = Vector2.Zero;
        }
        else
        {
            if (OnGround())
            {
                float target = ((!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f)));
                Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                Vector2 liftSpeed = base.LiftSpeed;
                if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                {
                    Speed = prevLiftSpeed;
                    prevLiftSpeed = Vector2.Zero;
                    Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                    if (Speed.X != 0f && Speed.Y == 0f)
                    {
                        Speed.Y = -60f;
                    }

                    if (Speed.Y < 0f)
                    {
                        noGravityTimer = 0.15f;
                    }
                }
                else
                {
                    prevLiftSpeed = liftSpeed;
                    if (liftSpeed.Y < 0f && Speed.Y < 0f)
                    {
                        Speed.Y = 0f;
                    }
                }
            }
            else if (Hold.ShouldHaveGravity)
            {
                float num = 800f;
                if (Math.Abs(Speed.Y) <= 30f)
                {
                    num *= 0.5f;
                }

                float num2 = 350f;
                if (Speed.Y < 0f)
                {
                    num2 *= 0.5f;
                }

                Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
                if (noGravityTimer > 0f)
                {
                    noGravityTimer -= Engine.DeltaTime;
                }
                else
                {
                    Speed.Y = Calc.Approach(Speed.Y, 200f, num * Engine.DeltaTime);
                }
            }

            previousPosition = base.ExactPosition;
            MoveH(Speed.X * Engine.DeltaTime, onCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
            if (base.Center.X > (float)Level.Bounds.Right)
            {
                MoveH(32f * Engine.DeltaTime);
                if (base.Left - 8f > (float)Level.Bounds.Right)
                {
                    RemoveSelf();
                }
            }
            else if (base.Left < (float)Level.Bounds.Left)
            {
                base.Left = Level.Bounds.Left;
                Speed.X *= -0.4f;
            }
            else if (base.Top < (float)(Level.Bounds.Top - 4))
            {
                base.Top = Level.Bounds.Top + 4;
                Speed.Y = 0f;
            }
            else if (base.Bottom > (float)Level.Bounds.Bottom && SaveData.Instance.Assists.Invincible)
            {
                base.Bottom = Level.Bounds.Bottom;
                Speed.Y = -300f;
                Audio.Play("event:/game/general/assist_screenbottom", Position);
            }
            else if (base.Top > (float)Level.Bounds.Bottom + 8)
            {
                Die();
            }

            if (base.X < (float)(Level.Bounds.Left + 10))
            {
                MoveH(32f * Engine.DeltaTime);
            }

            Player entity = base.Scene.Tracker.GetEntity<Player>();
            TempleGate templeGate = CollideFirst<TempleGate>();
            if (templeGate != null && entity != null)
            {
                templeGate.Collidable = false;
                MoveH((float)(Math.Sign(entity.X - base.X) * 32) * Engine.DeltaTime);
                templeGate.Collidable = true;
            }
        }

        if (!dead)
        {
            Hold.CheckAgainstColliders();
        }

        if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold))
        {
            hitSeeker = null;
        }
    }

    public IEnumerator Shatter()
    {
        shattering = true;
        BloomPoint bloom = new BloomPoint(0f, 32f);
        VertexLight light = new VertexLight(Color.AliceBlue, 0f, 64, 200);
        Add(bloom);
        Add(light);
        for (float p = 0f; p < 1f; p += Engine.DeltaTime)
        {
            Position += Speed * (1f - p) * Engine.DeltaTime;
            Level.ZoomFocusPoint = TopCenter - Level.Camera.Position;
            light.Alpha = p;
            bloom.Alpha = p;
            yield return null;
        }

        yield return 0.5f;
        Level.Shake();
        sprite.Play("shatter");
        yield return 1f;
        Level.Shake();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ExplodeLaunch(Vector2 from)
    {
        if (!Hold.IsHeld)
        {
            Speed = (base.Center - from).SafeNormalize(120f);
            SlashFx.Burst(base.Center, Speed.Angle());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Swat(HoldableCollider hc, int dir)
    {
        if (Hold.IsHeld && hitSeeker == null)
        {
            swatTimer = 0.1f;
            hitSeeker = hc;
            Hold.Holder.Swat(dir);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Dangerous(HoldableCollider holdableCollider)
    {
        if (!Hold.IsHeld && Speed != Vector2.Zero)
        {
            return hitSeeker != holdableCollider;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void HitSeeker(Seeker seeker)
    {
        if (!Hold.IsHeld)
        {
            Speed = (base.Center - seeker.Center).SafeNormalize(120f);
        }

        Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void HitSpinner(Entity spinner)
    {
        if (!Hold.IsHeld && Speed.Length() < 0.01f && base.LiftSpeed.Length() < 0.01f && (previousPosition - base.ExactPosition).Length() < 0.01f && OnGround())
        {
            int num = Math.Sign(base.X - spinner.X);
            if (num == 0)
            {
                num = 1;
            }

            Speed.X = (float)num * 120f;
            Speed.Y = -30f;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool HitSpring(Spring spring)
    {
        if (!Hold.IsHeld)
        {
            if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
            {
                Speed.X *= 0.5f;
                Speed.Y = -160f;
                noGravityTimer = 0.15f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
            {
                MoveTowardsY(spring.CenterY + 5f, 4f);
                Speed.X = 220f;
                Speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }

            if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
            {
                MoveTowardsY(spring.CenterY + 5f, 4f);
                Speed.X = -220f;
                Speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnCollideH(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
        }

        Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
        if (Math.Abs(Speed.X) > 100f)
        {
            ImpactParticles(data.Direction);
        }

        Speed.X *= -0.4f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnCollideV(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
        }

        if (Speed.Y > 0f)
        {
            if (hardVerticalHitSoundCooldown <= 0f)
            {
                Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f));
                hardVerticalHitSoundCooldown = 0.5f;
            }
            else
            {
                Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
            }
        }

        if (Speed.Y > 160f)
        {
            ImpactParticles(data.Direction);
        }

        if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch))
        {
            Speed.Y *= -0.6f;
        }
        else
        {
            Speed.Y = 0f;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ImpactParticles(Vector2 dir)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        if (dir.X > 0f)
        {
            direction = MathF.PI;
            position = new Vector2(base.Right, base.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.X < 0f)
        {
            direction = 0f;
            position = new Vector2(base.Left, base.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.Y > 0f)
        {
            direction = -MathF.PI / 2f;
            position = new Vector2(base.X, base.Bottom);
            positionRange = Vector2.UnitX * 6f;
        }
        else
        {
            direction = MathF.PI / 2f;
            position = new Vector2(base.X, base.Top);
            positionRange = Vector2.UnitX * 6f;
        }

        Level.Particles.Emit(P_Impact, 12, position, positionRange, direction);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override bool IsRiding(Solid solid)
    {
        if (Speed.Y == 0f)
        {
            return base.IsRiding(solid);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnSquish(CollisionData data)
    {
        if (!TrySquishWiggle(data, 3, 3) && !SaveData.Instance.Assists.Invincible)
        {
            Die();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnPickup()
    {
        Speed = Vector2.Zero;
        AddTag(Tags.Persistent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnRelease(Vector2 force)
    {
        RemoveTag(Tags.Persistent);
        if (force.X != 0f && force.Y == 0f)
        {
            force.Y = -0.4f;
        }

        Speed = force * 200f;
        if (Speed != Vector2.Zero)
        {
            noGravityTimer = 0.1f;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void orig_ctor(Vector2 position)
    {
        previousPosition = position;
        base.Depth = 100;
        base.Collider = new Hitbox(9f, 12f, -5f, -11f);
        Add(Hold = new Holdable(0.1f));
        Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
        Hold.SlowFall = false;
        Hold.SlowRun = true;
        Hold.OnPickup = OnPickup;
        Hold.OnRelease = OnRelease;
        Hold.DangerousCheck = Dangerous;
        Hold.OnHitSeeker = HitSeeker;
        Hold.OnSwat = Swat;
        Hold.OnHitSpring = HitSpring;
        Hold.OnHitSpinner = HitSpinner;
        Hold.SpeedGetter = () => Speed;
        onCollideH = OnCollideH;
        onCollideV = OnCollideV;
        LiftSpeedGraceTime = 0.1f;
        Add(new VertexLight(base.Collider.Center, Color.White, 1f, 32, 64));
        //base.Tag = Tags.TransitionUpdate;
        Add(new MirrorReflection());
    }

    #endregion
}
