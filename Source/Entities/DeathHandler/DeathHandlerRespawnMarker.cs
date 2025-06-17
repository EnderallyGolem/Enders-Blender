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
using Celeste.Mod.EndHelper.Entities.Misc;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[Tracked(false)]
[CustomEntity("EndHelper/DeathHandlerRespawnMarker")]
public class DeathHandlerRespawnMarker : Entity
{
    internal bool faceLeft = false; // Internally stored, can't be set by mapper
    private float speed = 1;
    private string requireFlag = "";
    private bool flagEnable = true;
    private bool offscreenPointer = true;

    public Sprite sprite;

    const int width = 16;
    const int height = 18;

    public EntityID entityID;
    private Vector2 previousTargetPos = Vector2.Zero;
    internal float previousDistanceBetweenPosAndTarget = 99999f;
    private int framesGoingFurtherFromTarget = 0;

    private SineWave sine;

    // Particles!
    readonly ParticleType particle = new ParticleType
    {
        Color = new Color(255, 232, 89, 128),
        Color2 = Calc.HexToColor("ffffff"),
        ColorMode = ParticleType.ColorModes.Fade,
        FadeMode = ParticleType.FadeModes.Linear,
        LifeMin = 0.7f,
        LifeMax = 3f,
        Size = 1f,
        SpeedMin = 5f,
        SpeedMax = 10f,
        Acceleration = new Vector2(0f, 5f),
        DirectionRange = MathF.PI * 2f
    };

    public DeathHandlerRespawnMarker(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
    {
        speed = data.Float("speed", 1f);
        requireFlag = data.Attr("requireFlag", "");
        offscreenPointer = data.Bool("offscreenPointer", true);

        entityID = id;

        sine = new SineWave(0.6f, 0f);
        Add(sine);

        Add(new DeathBypass(requireFlag, false, id));

        Add(sprite = EndHelperModule.SpriteBank.Create("DeathHandlerRespawnPoint"));
        sprite.Position += new Vector2(0, -1);
        sprite.Play("idle");

        Depth = 1;
        base.Collider = new Hitbox(x: -width / 2, y: -height / 2, width: width, height: height);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        UpdateSprite();
        base.Awake(scene);

        Level level = scene as Level;

        // le HUD
        level.Add(new MarkerHUD(this));

        // Warp to spawnpoint location
        Vector2 currentPosSpawnpoint = new Vector2(Position.X, Position.Y + height / 2 - 1);
        Vector2 targetPos = level.Session.RespawnPoint.Value;
        previousTargetPos = targetPos;
        currentPosSpawnpoint = targetPos;
        Position = new Vector2(currentPosSpawnpoint.X, currentPosSpawnpoint.Y - height / 2 + 1);
    }

    private bool pastFirstFrame = false;
    private void UpdateFirstFrame()
    {
        if (!pastFirstFrame)
        {
            Level level = SceneAs<Level>();
            pastFirstFrame = true;

            // Warp to spawnpoint location. Again. lol
            Vector2 currentPosSpawnpoint = new Vector2(Position.X, Position.Y + height / 2 - 1);
            Vector2 targetPos = level.Session.RespawnPoint.Value;
            currentPosSpawnpoint = targetPos;
            Position = new Vector2(currentPosSpawnpoint.X, currentPosSpawnpoint.Y - height / 2 + 1);
        }
    }

    private int particleLimiter = 0;
    public override void Update()
    {
        // Flag enable
        flagEnable = Utils_General.AreFlagsEnabled(SceneAs<Level>().Session, requireFlag);

        UpdateFirstFrame();
        Level level = SceneAs<Level>();

        // Move to spawn point location
        Vector2 currentPosSpawnpoint = ConvertSpawnPointPosToActualPos(Position, true);
        Vector2 targetPos = level.Session.RespawnPoint.Value;

        float distanceBetweenPosAndTarget = Vector2.Distance(currentPosSpawnpoint, targetPos);

        // Count how many times previousDistanceTargetPrevTarget <= distanceTargetPrevTarget (going FURTHER/same distance from target)
        if (distanceBetweenPosAndTarget >= previousDistanceBetweenPosAndTarget && distanceBetweenPosAndTarget != 0)
        {
            framesGoingFurtherFromTarget++;
        }
        else
        {
            framesGoingFurtherFromTarget = 0;
        }

        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerRespawnMarker", $"Distance between pos and target: {distanceBetweenPosAndTarget}. Previous: {previousDistanceBetweenPosAndTarget}. Frames going further: {framesGoingFurtherFromTarget}");

        // If becoming larger, and distance >= 1 tile, play SFX
        if (distanceBetweenPosAndTarget >= 8 && framesGoingFurtherFromTarget == 1 && flagEnable)
        {
            Add(new SoundSource("event:/game/06_reflection/feather_bubble_bounce"));
        }

        if (currentPosSpawnpoint != targetPos)
        {
            if (speed == 0 || (framesGoingFurtherFromTarget >= 2 && distanceBetweenPosAndTarget <= 8))
            {
                // If set to instant teleport (speed == 0) or target is going away (while being close)
                currentPosSpawnpoint = targetPos;
            }
            else
            {
                float approachSpeed = Engine.DeltaTime * speed * (distanceBetweenPosAndTarget + 8) * 4.5f;
                if (approachSpeed < 1) { approachSpeed = 0.6f; }
                currentPosSpawnpoint = Calc.Approach(currentPosSpawnpoint, targetPos, approachSpeed);
            }
        }

        Position = ConvertSpawnPointPosToActualPos(currentPosSpawnpoint);

        base.Update();

        particleLimiter++;
        if (particleLimiter > 4)
        {
            if (flagEnable)
            {
                SceneAs<Level>().ParticlesBG.Emit(particle, Position + new Vector2(-width / 2 * 0.8f, -height / 2) + Calc.Random.Range(Vector2.Zero, Vector2.One * 16));
            }
            particleLimiter = 0;
        }

        previousTargetPos = targetPos;
        previousDistanceBetweenPosAndTarget = distanceBetweenPosAndTarget;
    }
  
    public override void Render()
    {
        UpdateSprite();
        base.Render();
    }

    public void UpdateSprite()
    {
        if (flagEnable)
        {
            sprite.Visible = true;
        } 
        else
        {
            sprite.Visible = false;
        }

        if (faceLeft)
        {
            sprite.FlipX = true;
        }
        else
        {
            sprite.FlipX = false;
        }

        sprite.Color.A = 128;
        sprite.Color.A += (byte)(sine.Value * 120f);
    }

    internal Vector2 ConvertSpawnPointPosToActualPos(Vector2 pos, bool reverse = false)
    {
        if (!reverse)
        {
            return new Vector2(pos.X, pos.Y - height / 2 + 1);
        }
        else
        {
            return new Vector2(pos.X, pos.Y + height / 2 - 1);
        }
    }


    public class MarkerHUD : Entity
    {
        private DeathHandlerRespawnMarker p;

        private float distanceToCamera = 0;
        private Vector2 closestPos;
        private float angleFromArrow = 0f;

        private Vector2 arrowDrawScreenPos;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public MarkerHUD(DeathHandlerRespawnMarker parent)
        {
            Depth = -1;
            p = parent;
            AddTag(Tags.HUD);

            Add(new DeathBypass(p.requireFlag, false, p.entityID));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            Level level = SceneAs<Level>();
            Rectangle camera = level.Camera.GetRect(-8, -8);

            distanceToCamera = camera.GetDistanceTo(p.Position, out closestPos);

            Vector2 arrowPos = camera.PointToCenterIntersect(p.Position);
            arrowDrawScreenPos = level.WorldToScreen(arrowPos);

            angleFromArrow = (float)(Math.Atan2(p.Position.Y - arrowPos.Y, p.Position.X - arrowPos.X) + Math.PI);

            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerRespawnMarker", $"distanceToCamera {distanceToCamera} | closestPos {closestPos} | angleFromCamera {angleFromCamera}");

            base.Update();
        }

        public override void Render()
        {
            if (distanceToCamera > 0 && p.offscreenPointer)
            {
                float scaleMultiple = (float)Math.Clamp(distanceToCamera / 32, 0, 1);

                Color iconColour = new Color(255, 232, 89, 64);

                // Red if full reset
                if (Utils_DeathHandler.lastFullResetPos == SceneAs<Level>().Session.RespawnPoint)
                {
                    iconColour = new Color(255, 80, 80, 64);
                }

                iconColour.A += (byte)(p.sine.Value * 60);
                iconColour *= scaleMultiple;

                float iconSize = 0.5f * scaleMultiple + 0.5f;
                iconSize += p.sine.Value * 0.1f;

                MTexture mTexture_towerArrow = GFX.Gui["controls/directions/-1x0"];

                mTexture_towerArrow.DrawCentered(arrowDrawScreenPos, iconColour, iconSize, angleFromArrow);
                //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerRespawnMarker", $"Drawing marker at: closestPosScreen {arrowDrawScreenPos} | iconSize {iconSize} | iconColour.A {iconColour.A}");
            }

            base.Render();
        }
    }
}
