using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;
using System;
using static MonoMod.InlineRT.MonoModRule;

namespace Celeste.Mod.EndHelper.Entities.RoomSwap;

[CustomEntity("EndHelper/RoomSwapBox")]
public class RoomSwapBox : Solid
{

    private string gridId;

    private Sprite sprite;
    private Sprite sprite_activate;
    private SineWave sine;
    private Vector2 start;
    private float sink;
    private float shakeCounter;
    private Vector2 bounceDir;
    private Wiggler bounce;
    private Shaker shaker;
    private bool smashParticles;
    private Coroutine pulseRoutine;
    private SoundSource firstHitSfx;
    private bool spikesLeft;
    private bool spikesRight;
    private bool spikesUp;
    private bool spikesDown;

    private EntityData entityData;
    private MTexture texture;
    private string flagCheck = "";
    private bool flagRequire = true;
    private bool flagToggle = false;
    private bool flashEffect = false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RoomSwapBox(Vector2 position)
        : base(position, 32f, 32f, safe: true)
    {
        SurfaceSoundIndex = 9;
        start = Position;
        sprite = GFX.SpriteBank.Create("breakerBox");
        sprite_activate = GFX.SpriteBank.Create("breakerBox");

        Sprite obj = sprite;
        sine = new SineWave(0.5f, 0f);
        Add(sine);
        bounce = Wiggler.Create(1f, 0.5f);
        bounce.StartZero = false;
        Add(bounce);
        Add(shaker = new Shaker(on: false));
        OnDashCollide = Dashed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RoomSwapBox(EntityData e, Vector2 levelOffset)
        : this(e.Position + levelOffset)
    {
        entityData = e;
        gridId = e.Attr("gridId", "1");
        flagCheck = e.Attr("flagCheck", "");
        flagRequire = e.Bool("flagRequire", true);
        flagToggle = e.Bool("flagToggle", false);
        flashEffect = e.Bool("flashEffect", false);

        //Image Path
        string imagePath = e.Attr("texturePath", "objects/EndHelper/RoomSwapBox/loenn");
        if (imagePath == "") { 

            string left = entityData.Attr("modificationTypeLeft", "None");

            if (left== "CurrentRowRight_PreventWarp")
            {
                imagePath = "objects/EndHelper/RoomSwapBox/transitionBoxShift";
            }
            else if (left == "CurrentRowRight")
            {
                imagePath = "objects/EndHelper/RoomSwapBox/transitionBoxShiftWarp";
            }
            else if (left == "Reset")
            {
                imagePath = "objects/EndHelper/RoomSwapBox/transitionBoxReset";
            }
            else if (left == "SwapLeftRight")
            {
                imagePath = "objects/EndHelper/RoomSwapBox/transitionBoxSwap";
            }
            else
            {
                imagePath = "objects/EndHelper/RoomSwapBox/loenn";
            }

        } else if (imagePath.ToLower() == "heart")
        {
            imagePath = "objects/EndHelper/RoomSwapBox/transitionBoxHeart";
        }
        while (imagePath.StartsWith("objects") == false)
        {
            imagePath = imagePath.Substring(imagePath.IndexOf('/') + 1);
        }
        if (imagePath.IndexOf(".") > -1)
        {
            imagePath = imagePath.Substring(0, imagePath.IndexOf("."));
        }
        texture = GFX.Game[imagePath];
        Add(new Image(texture));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        spikesUp = CollideCheck<Spikes>(Position - Vector2.UnitY);
        spikesDown = CollideCheck<Spikes>(Position + Vector2.UnitY);
        spikesLeft = CollideCheck<Spikes>(Position - Vector2.UnitX);
        spikesRight = CollideCheck<Spikes>(Position + Vector2.UnitX);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DashCollisionResults Dashed(Player player, Vector2 dir)
    {
        if (!SaveData.Instance.Assists.Invincible)
        {
            if (dir == Vector2.UnitX && spikesLeft)
            {
                return DashCollisionResults.NormalCollision;
            }

            if (dir == -Vector2.UnitX && spikesRight)
            {
                return DashCollisionResults.NormalCollision;
            }

            if (dir == Vector2.UnitY && spikesUp)
            {
                return DashCollisionResults.NormalCollision;
            }

            if (dir == -Vector2.UnitY && spikesDown)
            {
                return DashCollisionResults.NormalCollision;
            }
        }
        string modifyTypeLeft = entityData.Attr("modificationTypeLeft", "None");
        string modifyTypeRight = entityData.Attr("modificationTypeRight", "None");
        string modifyTypeUp = entityData.Attr("modificationTypeUp", "None");
        string modifyTypeDown = entityData.Attr("modificationTypeDown", "None");
        bool modifySilently = entityData.Bool("modifySilently", false);

        Level level = SceneAs<Level>();
        if (flagCheck == "")
        {
            modifyRoomCommandsAndStuff();
        }
        else if (level.Session.GetFlag(flagCheck) == flagRequire)
        {
            if (flagToggle)
            {
                level.Session.SetFlag(flagCheck, !level.Session.GetFlag(flagCheck));
            }
            modifyRoomCommandsAndStuff();
        }

        void modifyRoomCommandsAndStuff()
        {
            if (dir == Vector2.UnitX)
            {
                bool checkSucceed = EndHelperModule.ModifyRooms(modifyTypeLeft, modifySilently, player, SceneAs<Level>(), gridId, teleportDelayMilisecond: 150, flashEffect: flashEffect);
                if (checkSucceed) { hitEffects(); }
            }

            if (dir == -Vector2.UnitX)
            {
                bool checkSucceed = EndHelperModule.ModifyRooms(modifyTypeRight, modifySilently, player, SceneAs<Level>(), gridId, teleportDelayMilisecond: 150, flashEffect: flashEffect);
                if (checkSucceed) { hitEffects(); }
            }

            if (dir == Vector2.UnitY)
            {
                bool checkSucceed = EndHelperModule.ModifyRooms(modifyTypeUp, modifySilently, player, SceneAs<Level>(), gridId, teleportDelayMilisecond: 250, flashEffect: flashEffect); //More delay to kb away
                if (checkSucceed) { hitEffects(); }
            }

            if (dir == -Vector2.UnitY)
            {
                bool checkSucceed = EndHelperModule.ModifyRooms(modifyTypeDown, modifySilently, player, SceneAs<Level>(), gridId, teleportDelayMilisecond: 0, flashEffect: flashEffect); //No delay otherwise clip inside
                if (checkSucceed) { hitEffects(); }
            }
        }

        (Scene as Level).DirectionalShake(dir);
        sprite.Scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);

        Add(firstHitSfx = new SoundSource("event:/new_content/game/10_farewell/fusebox_hit_1"));
        hitEffectsAlways(player);

        bounceDir = dir;
        bounce.Start();
        smashParticles = true;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

        //player.RefillDash();

        return DashCollisionResults.Rebound;
    }

    private void hitEffects()
    {
        Add(sprite_activate);
        sprite_activate.Position = new Vector2(Width, Height) / 2f;
        sprite_activate.Play("break", true);
        Celeste.Freeze(0.1f);
        shakeCounter = 0.2f;
        shaker.On = true;
        Pulse();
    }

    void hitEffectsAlways(Player player)
    {
        if (player.StateMachine.State == 2 || player.StateMachine.State == 5) //Get out of the booster
        {
            player.StateMachine.State = 0;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SmashParticles(Vector2 dir)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int num;
        if (dir == Vector2.UnitX)
        {
            direction = 0f;
            position = CenterRight - Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            num = (int)(Height / 8f) * 8;
        }
        else if (dir == -Vector2.UnitX)
        {
            direction = MathF.PI;
            position = CenterLeft + Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            num = (int)(Height / 8f) * 8;
        }
        else if (dir == Vector2.UnitY)
        {
            direction = MathF.PI / 2f;
            position = BottomCenter - Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            num = (int)(Width / 8f) * 8;
        }
        else
        {
            direction = -MathF.PI / 2f;
            position = TopCenter + Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            num = (int)(Width / 8f) * 8;
        }

        num += 2;

        Color particleColour1 = entityData.HexColor("particleColour1", Calc.HexToColor("ff3399"));
        Color particleColour2 = entityData.HexColor("particleColour2", Calc.HexToColor("ff00ff"));

        ParticleType P_Smash = new ParticleType
        {
            Source = GFX.Game["particles/rect"],
            Color = particleColour1,
            Color2 = particleColour2,
            ColorMode = ParticleType.ColorModes.Blink,
            RotationMode = ParticleType.RotationModes.SameAsDirection,
            Size = 0.5f,
            SizeRange = 0.3f,
            DirectionRange = MathF.PI,
            FadeMode = ParticleType.FadeModes.Late,
            LifeMin = 0.5f,
            LifeMax = 1.5f,
            SpeedMin = 50f,
            SpeedMax = 150f,
            SpeedMultiplier = 0.8f
        };

        SceneAs<Level>().Particles.Emit(P_Smash, num, position, positionRange, direction);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        base.Update();

        if (shakeCounter > 0f)
        {
            shakeCounter -= Engine.DeltaTime;
            if (shakeCounter <= 0f)
            {
                shaker.On = false;
                sprite.Scale = Vector2.One * 1.2f;
            }
        }

        if (Collidable)
        {
            bool flag = HasPlayerRider();
            sink = Calc.Approach(sink, flag ? 1 : 0, 2f * Engine.DeltaTime);
            sine.Rate = MathHelper.Lerp(0.7f, 0.3f, sink);
            Vector2 vector = start;
            vector.Y += sink * 3f + sine.Value * MathHelper.Lerp(2f, 1f, sink);
            vector += bounce.Value * bounceDir * 8f;
            MoveToX(vector.X);
            MoveToY(vector.Y);
            if (smashParticles)
            {
                smashParticles = false;
                SmashParticles(bounceDir);
            }
        }

        sprite.Scale.X = Calc.Approach(sprite.Scale.X, 1f, Engine.DeltaTime * 4f);
        sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, 1f, Engine.DeltaTime * 4f);
        LiftSpeed = Vector2.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        Vector2 position = sprite.Position;
        sprite.Position += shaker.Value;
        base.Render();
        sprite.Position = position;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Pulse()
    {
        pulseRoutine = new Coroutine(Lightning.PulseRoutine(SceneAs<Level>()));
        Add(pulseRoutine);
    }
}