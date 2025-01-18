using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using Celeste.Mod.Entities;
using static Celeste.TempleGate;
using static On.Celeste.Level;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;
using System.Linq;


using Celeste.Mod.Core;
using static Celeste.GaussianBlur;
using static Celeste.WaveDashPage;
using Celeste.Mod.SpeedrunTool.Message;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.SpeedrunTool;
using System.Diagnostics.CodeAnalysis;

// Because I keep forgetting: the vanilla entity is Lookout.
namespace Celeste.Mod.EndHelper.Entities.Misc;

[Tracked(true)]
[CustomEntity("EndHelper/MultiroomWatchtower")]
public class MultiroomWatchtower : Entity
{

    public bool leftLookoutRoomScroll = false;
    public bool rightLookoutRoomScroll = false;
    public bool upLookoutRoomScroll = false;
    public bool downLookoutRoomScroll = false;
    public bool Q1LookoutRoomScroll = false; //start top right, anticlockwise
    public bool Q2LookoutRoomScroll = false;
    public bool Q3LookoutRoomScroll = false;
    public bool Q4LookoutRoomScroll = false;
    public LevelData lookoutRoom = null;

    Vector2 watchtowerPosition;

    public class Hud : Entity
    {
        public float Easer;

        public float timerUp;
        public float timerDown;
        public float timerLeft;
        public float timerRight;

        public float timerQ1;
        public float timerQ2;
        public float timerQ3;
        public float timerQ4;


        public float multUp;
        public float multDown;
        public float multLeft;
        public float multRight;

        public float multQ1;
        public float multQ2;
        public float multQ3;
        public float multQ4;


        public float leftScrollHUD;
        public float rightScrollHUD;
        public float upScrollHUD;
        public float downScrollHUD;

        public float Q1ScrollHUD;
        public float Q2ScrollHUD;
        public float Q3ScrollHUD;
        public float Q4ScrollHUD;

        Vector2 Q1FadeOffset;
        Vector2 Q2FadeOffset;
        Vector2 Q3FadeOffset;
        Vector2 Q4FadeOffset;

        public Vector2 aim;


        public MTexture halfDot = GFX.Gui["dot"].GetSubtexture(0, 0, 64, 32);

        private MultiroomWatchtower p;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Hud(MultiroomWatchtower parent)
        {
            p = parent;
            AddTag(Tags.HUD);
            AddTag(Tags.Persistent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            Level level = SceneAs<Level>();
            Vector2 position = level.Camera.Position;
            Rectangle bounds = level.Bounds;

            int num = 320;
            int num2 = 180;

            bool flagLeftEdge, flagRightEdge, flagUpEdge, flagDownEdge;

            if (p.ignoreBlocker)
            {
                flagLeftEdge = false;
                flagRightEdge = false;
                flagUpEdge = p.trackMode && p.trackPercent >= 1f;
                flagDownEdge = p.trackMode && p.trackPercent <= 0f;
            } 
            else
            {
                flagLeftEdge = level.CollideCheck<LookoutBlocker>(new Rectangle((int)(position.X - 8f), (int)position.Y, num, num2));
                flagRightEdge = level.CollideCheck<LookoutBlocker>(new Rectangle((int)(position.X + 8f), (int)position.Y, num, num2));
                flagUpEdge = p.trackMode && p.trackPercent >= 1f || level.CollideCheck<LookoutBlocker>(new Rectangle((int)position.X, (int)(position.Y - 8f), num, num2));
                flagDownEdge = p.trackMode && p.trackPercent <= 0f || level.CollideCheck<LookoutBlocker>(new Rectangle((int)position.X, (int)(position.Y + 8f), num, num2));
            }

            leftScrollHUD = Calc.Approach(leftScrollHUD, p.leftLookoutRoomScroll || !flagLeftEdge && position.X > bounds.Left + 2 ? 1 : 0, Engine.DeltaTime * 8f);
            rightScrollHUD = Calc.Approach(rightScrollHUD, p.rightLookoutRoomScroll || !flagRightEdge && position.X + num < bounds.Right - 2 ? 1 : 0, Engine.DeltaTime * 8f);
            upScrollHUD = Calc.Approach(upScrollHUD, p.upLookoutRoomScroll || p.trackMode && p.trackPercent < 1f || !flagUpEdge && position.Y > bounds.Top + 2 ? 1 : 0, Engine.DeltaTime * 8f);
            downScrollHUD = Calc.Approach(downScrollHUD, p.downLookoutRoomScroll || p.trackMode && p.trackPercent > 0f || !flagDownEdge && position.Y + num2 < bounds.Bottom - 2 ? 1 : 0, Engine.DeltaTime * 8f);

            Q1ScrollHUD = Calc.Approach(Q1ScrollHUD, p.Q1LookoutRoomScroll ? 1 : 0, Engine.DeltaTime * 8f);
            Q2ScrollHUD = Calc.Approach(Q2ScrollHUD, p.Q2LookoutRoomScroll ? 1 : 0, Engine.DeltaTime * 8f);
            Q3ScrollHUD = Calc.Approach(Q3ScrollHUD, p.Q3LookoutRoomScroll ? 1 : 0, Engine.DeltaTime * 8f);
            Q4ScrollHUD = Calc.Approach(Q4ScrollHUD, p.Q4LookoutRoomScroll ? 1 : 0, Engine.DeltaTime * 8f);

            aim = Input.Aim.Value;
            if (aim.X < 0f)
            {
                multLeft = Calc.Approach(multLeft, 0f, Engine.DeltaTime * 2f);
                timerLeft += Engine.DeltaTime * 12f;
            }
            else
            {
                multLeft = Calc.Approach(multLeft, 1f, Engine.DeltaTime * 2f);
                timerLeft += Engine.DeltaTime * 6f;
            }

            if (aim.X > 0f)
            {
                multRight = Calc.Approach(multRight, 0f, Engine.DeltaTime * 2f);
                timerRight += Engine.DeltaTime * 12f;
            }
            else
            {
                multRight = Calc.Approach(multRight, 1f, Engine.DeltaTime * 2f);
                timerRight += Engine.DeltaTime * 6f;
            }

            if (aim.Y < 0f)
            {
                multUp = Calc.Approach(multUp, 0f, Engine.DeltaTime * 2f);
                timerUp += Engine.DeltaTime * 12f;
            }
            else
            {
                multUp = Calc.Approach(multUp, 1f, Engine.DeltaTime * 2f);
                timerUp += Engine.DeltaTime * 6f;
            }

            if (aim.Y > 0f)
            {
                multDown = Calc.Approach(multDown, 0f, Engine.DeltaTime * 2f);
                timerDown += Engine.DeltaTime * 12f;
            }
            else
            {
                multDown = Calc.Approach(multDown, 1f, Engine.DeltaTime * 2f);
                timerDown += Engine.DeltaTime * 6f;
            }

            if (aim == new Vector2(1, -1)) { timerQ1 += Engine.DeltaTime * 12f; }
            else { timerQ1 += Engine.DeltaTime * 6f; }
            if (aim == new Vector2(-1, -1)) { timerQ2 += Engine.DeltaTime * 12f; }
            else { timerQ2 += Engine.DeltaTime * 6f; }
            if (aim == new Vector2(-1, 1)) { timerQ3 += Engine.DeltaTime * 12f; }
            else { timerQ3 += Engine.DeltaTime * 6f; }
            if (aim == new Vector2(1, 1)) { timerQ4 += Engine.DeltaTime * 12f; }
            else { timerQ4 += Engine.DeltaTime * 6f; }


            Q1FadeOffset = new Vector2(
                +50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ1),
                -50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ1)
            );
            Q2FadeOffset = new Vector2(
                    -50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ2),
                    -50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ2)
                );
            Q3FadeOffset = new Vector2(
                    -50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ3),
                    +50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ3)
                );
            Q4FadeOffset = new Vector2(
                    +50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ4),
                    +50 * (float)(double)MathHelper.Lerp(0f, 1f, 1 - multQ4)
                );

            if (p.Q1LookoutRoomScroll) { multQ1 = Calc.Approach(multQ1, 1f, Engine.DeltaTime * 2.83f); }
            else { multQ1 = Calc.Approach(multQ1, 0f, Engine.DeltaTime * 2.83f); }
            if (p.Q2LookoutRoomScroll) { multQ2 = Calc.Approach(multQ2, 1f, Engine.DeltaTime * 2.83f); }
            else { multQ2 = Calc.Approach(multQ2, 0f, Engine.DeltaTime * 2.83f); }
            if (p.Q3LookoutRoomScroll) { multQ3 = Calc.Approach(multQ3, 1f, Engine.DeltaTime * 2.83f); }
            else { multQ3 = Calc.Approach(multQ3, 0f, Engine.DeltaTime * 2.83f); }
            if (p.Q4LookoutRoomScroll) { multQ4 = Calc.Approach(multQ4, 1f, Engine.DeltaTime * 2.83f); }
            else { multQ4 = Calc.Approach(multQ4, 0f, Engine.DeltaTime * 2.83f); }

            base.Update();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Render()
        {
            Level level = Scene as Level;
            float num_ease = Ease.CubeInOut(Easer);
            Color colorWhite = Color.White * num_ease;
            Color colorGold = Color.Gold * num_ease;
            int num_edgewidth = (int)(80f * num_ease);
            int num_edgeheight = (int)(80f * num_ease * 0.5625f);
            int thickness = 8;
            if (level.FrozenOrPaused || level.RetryPlayerCorpse != null)
            {
                colorWhite *= 0.25f;
                colorGold *= 0.25f;
            }

            //White edges: 80 - 960 - 1920 | 80 - 540 - 1080 
            Draw.Rect(num_edgewidth, num_edgeheight, 1920 - num_edgewidth * 2 - thickness, thickness, colorWhite);
            Draw.Rect(num_edgewidth, num_edgeheight + thickness, thickness + 2, 1080 - num_edgeheight * 2 - thickness, colorWhite);
            Draw.Rect(1920 - num_edgewidth - thickness - 2, num_edgeheight, thickness + 2, 1080 - num_edgeheight * 2 - thickness, colorWhite);
            Draw.Rect(num_edgewidth + thickness, 1080 - num_edgeheight - thickness, 1920 - num_edgewidth * 2 - thickness, thickness, colorWhite);
            if (level.FrozenOrPaused || level.RetryPlayerCorpse != null)
            {
                return;
            }

            Color upColor, downColor, leftColor, rightColor;
            if (!p.trackMode)
            {
                upColor = p.upLookoutRoomScroll ? colorGold : colorWhite;
                downColor = p.downLookoutRoomScroll ? colorGold : colorWhite;
                leftColor = p.leftLookoutRoomScroll ? colorGold : colorWhite;
                rightColor = p.rightLookoutRoomScroll ? colorGold : colorWhite;
            }
            else
            {
                upColor = downColor = leftColor = rightColor = colorGold;
            }

            MTexture mTexture_towerArrow = GFX.Gui["towerarrow"];
            if (!p.onlyX)
            {
                float yUp = num_edgeheight * upScrollHUD - (float)(Math.Sin(timerUp) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, multUp)) - (1f - multUp) * 12f;
                mTexture_towerArrow.DrawCentered(new Vector2(960f, yUp), upColor * upScrollHUD, 1f, MathF.PI / 2f);
                float yDown = 1080f - num_edgeheight * downScrollHUD + (float)(Math.Sin(timerDown) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, multDown)) + (1f - multDown) * 12f;
                mTexture_towerArrow.DrawCentered(new Vector2(960f, yDown), downColor * downScrollHUD, 1f, 4.712389f);
            }
            if (!p.trackMode && !p.onlyY)
            {
                float num_left = leftScrollHUD;
                float num_multLeft = multLeft;
                float num_timerLeft = timerLeft;
                float num_right = rightScrollHUD;
                float num_multRight = multRight;
                float num_timerRight = timerRight;
                if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                {
                    num_left = rightScrollHUD;
                    num_multLeft = multRight;
                    num_timerLeft = timerRight;
                    num_right = leftScrollHUD;
                    num_multRight = multLeft;
                    num_timerRight = timerLeft;
                }

                float xLeft = num_edgewidth * num_left - (float)(Math.Sin(num_timerLeft) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, num_multLeft)) - (1f - num_multLeft) * 12f;
                mTexture_towerArrow.DrawCentered(new Vector2(xLeft, 540f), leftColor * num_left);
                float xRight = 1920f - num_edgewidth * num_right + (float)(Math.Sin(num_timerRight) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, num_multRight)) + (1f - num_multRight) * 12f;
                mTexture_towerArrow.DrawCentered(new Vector2(xRight, 540f), rightColor * num_right, 1f, MathF.PI);
            }
            else if (p.trackMode)
            {
                int yTrackBottom = 1080 - num_edgeheight * 2 - 128 - 64;
                int xTrack = 1920 - num_edgewidth - 64;
                float yTrackTop = (1080 - yTrackBottom) / 2f + 32f;
                Draw.Rect(xTrack - 7, yTrackTop + 7f, 14f, yTrackBottom - 14, Color.Black * num_ease);
                halfDot.DrawJustified(new Vector2(xTrack, yTrackTop + 7f), new Vector2(0.5f, 1f), Color.Black * num_ease);
                halfDot.DrawJustified(new Vector2(xTrack, yTrackTop + yTrackBottom - 7f), new Vector2(0.5f, 1f), Color.Black * num_ease, new Vector2(1f, -1f));
                GFX.Gui["lookout/cursor"].DrawCentered(new Vector2(xTrack, yTrackTop + (1f - p.trackPercent) * yTrackBottom), Color.Gold * num_ease, 1f);
                GFX.Gui["lookout/summit"].DrawCentered(new Vector2(xTrack, yTrackTop - 64f), Color.Gold * num_ease, 0.65f);
            }

            Vector2 inputVector = Input.Aim.Value;
            if (p.onlyY)
            {
                inputVector.X = 0f;
            }
            if (p.onlyX)
            {
                inputVector.Y = 0f;
            }

            Vector2 confirmButtonPos = new Vector2(9999f, 9999f);

            //White edges: 80 - 960 - 1920 | 80 - 540 - 1080 
            Vector2 topLeft = new Vector2(num_ease * 160f, num_ease * 120f);
            Vector2 middle = new Vector2(960f, 540f);
            Vector2 bottomRight = new Vector2(1920f - num_ease * 160, 1080f - num_ease * 120);

            // Confirm button for changing rooms
            if (p.lookoutRoom != null && !p.trackMode)
            {
                switch ((inputVector.X, inputVector.Y))
                {
                    case (-1, 0):
                        confirmButtonPos = new Vector2(topLeft.X, middle.Y);
                        break;
                    case (-1, -1):
                        confirmButtonPos = new Vector2(topLeft.X, topLeft.Y);
                        p.Q2LookoutRoomScroll = true;
                        break;
                    case (0, -1):
                        confirmButtonPos = new Vector2(middle.X, topLeft.Y);
                        break;
                    case (1, -1):
                        confirmButtonPos = new Vector2(bottomRight.X, topLeft.Y);
                        p.Q1LookoutRoomScroll = true;
                        break;
                    case (1, 0):
                        confirmButtonPos = new Vector2(bottomRight.X, middle.Y);
                        break;
                    case (1, 1):
                        confirmButtonPos = new Vector2(bottomRight.X, bottomRight.Y);
                        p.Q4LookoutRoomScroll = true;
                        break;
                    case (0, 1):
                        confirmButtonPos = new Vector2(middle.X, bottomRight.Y);
                        break;
                    case (-1, 1):
                        confirmButtonPos = new Vector2(topLeft.X, bottomRight.Y);
                        p.Q3LookoutRoomScroll = true;
                        break;
                }
                Input.GuiButton(Input.MenuConfirm, mode: Input.PrefixMode.Latest).DrawCentered(confirmButtonPos, colorGold, 1f, 0 * MathF.PI);
            }

            // Diagonal arrows
            // These show either if that direction leads to a new room or if the direction is held and leads to any room (speed up)

            if (!p.trackMode)
            {
                float Q1sinOffset = (float)Math.Sin(timerQ1) * 9f;
                float Q2sinOffset = (float)Math.Sin(timerQ2) * 9f;
                float Q3sinOffset = (float)Math.Sin(timerQ3) * 9f;
                float Q4sinOffset = (float)Math.Sin(timerQ4) * 9f;

                mTexture_towerArrow.DrawCentered(Q1FadeOffset + new Vector2(bottomRight.X + 80f + Q1sinOffset, topLeft.Y - 80f - Q1sinOffset), colorGold * Q1ScrollHUD, 1f, MathF.PI * 3 / 4);        //Q1
                mTexture_towerArrow.DrawCentered(Q2FadeOffset + new Vector2(topLeft.X - 80f - Q2sinOffset, topLeft.Y - 80f - Q2sinOffset), colorGold * Q2ScrollHUD, 1f, MathF.PI * 1 / 4);            //Q2
                mTexture_towerArrow.DrawCentered(Q3FadeOffset + new Vector2(topLeft.X - 80f - Q3sinOffset, bottomRight.Y + 80f + Q3sinOffset), colorGold * Q3ScrollHUD, 1f, MathF.PI * 7 / 4);        //Q3
                mTexture_towerArrow.DrawCentered(Q4FadeOffset + new Vector2(bottomRight.X + 80f + Q4sinOffset, bottomRight.Y + 80f + Q4sinOffset), colorGold * Q4ScrollHUD, 1f, MathF.PI * 5 / 4);    //Q4
            }
        }
    }


    public TalkComponent talk;


    public Hud hud;


    public Sprite sprite;


    public Tween lightTween;


    public bool interacting;
    public bool previouslyInteracted = false;


    public bool onlyY;
    public bool onlyX;
    public float maxSpeedSet;
    public bool modifiedInterpolation;
    public bool trackMode;
    public float trackPercent;


    public List<Vector2> nodes;
    public int currentNodeNum;
    public float nodePercent;
    public bool summit;
    public string animPrefix = "";

    public bool allowAnywhere = false;
    public bool destroyUponFinishView = false;
    public bool ignoreBlocker = false;
    public bool canToggleBlocker = false;

    public EntityData data;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public MultiroomWatchtower(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        watchtowerPosition = data.Position + offset;
        this.data = data;
        Depth = -8500;
        Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(-0.5f, -20f), Interact));
        talk.PlayerMustBeFacing = false;
        summit = data.Bool("summit");
        onlyY = data.Bool("onlyY");
        onlyX = data.Bool("onlyX");
        maxSpeedSet = data.Float("speed", 240);
        modifiedInterpolation = data.Bool("modifiedInterpolation", true);
        ignoreBlocker = data.Bool("ignoreLookoutBlocker", false);
        Collider = new Hitbox(4f, 4f, -2f, -4f);
        VertexLight vertexLight = new VertexLight(new Vector2(-1f, -11f), Color.White, 0.8f, 16, 24);
        Add(vertexLight);
        lightTween = vertexLight.CreatePulseTween();
        Add(lightTween);
        sprite = EndHelperModule.SpriteBank.Create("multiroomWatchtower");
        Add(sprite);
        sprite.OnFrameChange = [MethodImpl(MethodImplOptions.NoInlining)] (s) =>
        {
            switch (s)
            {
                case "idle":
                case "badeline_idle":
                case "nobackpack_idle":
                    if (sprite.CurrentAnimationFrame == sprite.CurrentAnimationTotalFrames - 1)
                    {
                        lightTween.Start();
                    }

                    break;
            }
        };
        Vector2[] array = data.NodesOffset(offset);
        if (array != null && array.Length != 0)
        {
            nodes = new List<Vector2>(array);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (interacting)
        {
            Player entity = scene.Tracker.GetEntity<Player>();
            if (entity != null)
            {
                entity.StateMachine.State = 0;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public void Interact(Player player)
    {
        if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineAsBadeline || SaveData.Instance.Assists.PlayAsBadeline)
        {
            animPrefix = "badeline_";
        }
        else if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineNoBackpack)
        {
            animPrefix = "nobackpack_";
        }
        else
        {
            animPrefix = "";
        }

        Coroutine coroutine = new Coroutine(LookRoutine(player));
        coroutine.RemoveOnComplete = true;
        Add(coroutine);
        interacting = true;
        previouslyInteracted = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StopInteracting()
    {
        interacting = false;
        sprite.Play(animPrefix + "idle");
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        // Prevent overlapping of multiple watchtowers
        //Collider = new Hitbox(16, 16, -8, -8);


        MultiroomWatchtower collideWatchtower = CollideFirst<MultiroomWatchtower>();
        if (collideWatchtower != null && interacting == false && previouslyInteracted == false && !TagCheck(Tags.Persistent))
        {
            RemoveSelf();
        }
        // I HAVE LITERALLY NO IDEA WHY THIS WORKS
        // ok maybe it's due to the TalkComponent hitbox?
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        if (talk.UI != null)
        {
            talk.UI.Visible = !CollideCheck<Solid>();
        }

        base.Update();
        Player entity = Scene.Tracker.GetEntity<Player>();
        if (entity != null)
        {
            sprite.Active = interacting || entity.StateMachine.State != 11;
            if (!sprite.Active)
            {
                sprite.SetAnimationFrame(0);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]


    public IEnumerator LookRoutine(Player player)
    {
        trackPercent = 0f;
        Level level = SceneAs<Level>();

        SandwichLava sandwichLava = level.Entities.FindFirst<SandwichLava>();
        if (sandwichLava != null)
        {
            sandwichLava.Waiting = true;
        }

        if (player.Holding != null)
        {
            player.Drop();
        }
        player.StateMachine.State = 11;

        if (allowAnywhere)
        {
            // For keybind: No walking, just immediately use the bino. Also set to inactive.
            player.X = X;
            player.Y = Y;
            player.Active = false;
        }
        else
        {
            yield return player.DummyWalkToExact((int)X, walkBackwards: false, 1f, cancelOnFall: true);
        }

        if (Math.Abs(X - player.X) > 4f || player.Dead || (!player.OnGround() && !allowAnywhere))
        {
            if (!player.Dead)
            {
                player.StateMachine.State = 0;
            }

            yield break;
        }

        // From here: player is definitely using it
        AddTag(Tags.Persistent);
        {if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
        {
            roomStatDisplayer.disableRoomChange = true;
        }}

        previouslyInteracted = true;

        bool canRetryInitial = level.CanRetry;
        bool canSaveQuitInitial = level.SaveQuitDisabled;
        bool pauseLock = level.PauseLock;
        level.CanRetry = false; //Disable retry because otherwise you can just respawn at the screen you are looking at
        level.SaveQuitDisabled = true; //Disable save and quit because respawning also
        level.PauseLock = true; //ok i give up just lock pause. im gonna keep the other two just in case someone makes a retry shortcut or something

        Audio.Play("event:/game/general/lookout_use", Position);
        if (player.Facing == Facings.Right)
        {
            sprite.Play(animPrefix + "lookRight");
        }
        else
        {
            sprite.Play(animPrefix + "lookLeft");
        }

        player.Sprite.Visible = false;
        player.Hair.Visible = false;
        Collider originalPlayerCollider = player.Collider;
        yield return 0.2f;
        level.Add(hud = new Hud(this));
        trackMode = nodes != null;
        nodePercent = 0f;
        currentNodeNum = 0;
        Audio.Play("event:/ui/game/lookout_on");
        while ((hud.Easer = Calc.Approach(hud.Easer, 1f, Engine.DeltaTime * 3f)) < 1f)
        {
            level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
            yield return null;
        }


        float accel = 800f * maxSpeedSet / 240f;
        float maxspd = maxSpeedSet;
        Vector2 camCorner = level.Camera.Position;
        Vector2 speed = Vector2.Zero;
        Vector2 lastDir = Vector2.Zero;

        LevelData camStartLevelData = level.Session.LevelData;
        string camStartRoomName = camStartLevelData.Name;

        Vector2 camStart = level.Camera.Position;
        Vector2 camStartCenter = camStart + new Vector2(160f, 90f);

        LevelData currentRoomLevelData = level.Session.LevelData;
        Rectangle currentRoomBounds = currentRoomLevelData.Bounds;

        List<LevelData> edgeRoomDataList = getEdgeRoomDataList(level);

        int changeRoomCooldown = 0;

        //
        // Stay within this while loop as long as viewing binoculars
        //
        while (!Input.MenuCancel.Pressed && !Input.Pause.Pressed && !Input.ESC.Pressed && interacting
            /*&& !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed*/)
        {
            // Force these to be the correct values. In case other stuff changes these.
            player.Sprite.Visible = false;
            player.Hair.Visible = false;

            // Solely for the keybind multiroom bino
            if (canToggleBlocker && EndHelperModule.Settings.FreeMultiroomWatchtower.Button.Pressed)
            {
                ignoreBlocker = !ignoreBlocker;
                string message;
                if (ignoreBlocker)
                {
                    message = Dialog.Get("EndHelper_Dialog_MultiroomWatchtower_FreeCameraON");
                } else
                {
                    message = Dialog.Get("EndHelper_Dialog_MultiroomWatchtower_FreeCameraOFF");
                }
                Tooltip.Show(message, 1.5f);
            }


            ResetCameraTrackSettings(level, player, false);
            if (changeRoomCooldown > 0) { changeRoomCooldown--; }

            camCorner = level.Camera.Position;

            Vector2 inputVector = Input.Aim.Value;
            if (onlyY)
            {
                inputVector.X = 0f;
            }
            if (onlyX)
            {
                inputVector.Y = 0f;
            }

            if (Math.Sign(inputVector.X) != Math.Sign(lastDir.X) || Math.Sign(inputVector.Y) != Math.Sign(lastDir.Y))
            {
                Audio.Play("event:/game/general/lookout_move", camCorner);
            }

            // Check whether if L/R/U/D points towards a room, if yes, colour them gold

            if (!trackMode)
            {
                LevelData leftLookoutRoom = findLookoutRoom(new Vector2(-1, 0), edgeRoomDataList, out _);
                LevelData rightLookoutRoom = findLookoutRoom(new Vector2(1, 0), edgeRoomDataList, out _);
                LevelData upLookoutRoom = findLookoutRoom(new Vector2(0, -1), edgeRoomDataList, out _);
                LevelData downLookoutRoom = findLookoutRoom(new Vector2(0, 1), edgeRoomDataList, out _);

                //Start top right, Anti CW
                LevelData Q1LookoutRoom = findLookoutRoom(new Vector2(1, -1), edgeRoomDataList, out _);
                LevelData Q2LookoutRoom = findLookoutRoom(new Vector2(-1, -1), edgeRoomDataList, out _);
                LevelData Q3LookoutRoom = findLookoutRoom(new Vector2(-1, 1), edgeRoomDataList, out _);
                LevelData Q4LookoutRoom = findLookoutRoom(new Vector2(1, 1), edgeRoomDataList, out _);

                // Allow L/R/U/D if not null
                leftLookoutRoomScroll = leftLookoutRoom != null;
                rightLookoutRoomScroll = rightLookoutRoom != null;
                upLookoutRoomScroll = upLookoutRoom != null;
                downLookoutRoomScroll = downLookoutRoom != null;

                // Allow quadrants if it's different from cardinal directions
                Q1LookoutRoomScroll = Q1LookoutRoom != null && Q1LookoutRoom != upLookoutRoom && Q1LookoutRoom != rightLookoutRoom && !onlyX && !onlyY;
                Q2LookoutRoomScroll = Q2LookoutRoom != null && Q2LookoutRoom != upLookoutRoom && Q2LookoutRoom != leftLookoutRoom && !onlyX && !onlyY;
                Q3LookoutRoomScroll = Q3LookoutRoom != null && Q3LookoutRoom != downLookoutRoom && Q3LookoutRoom != leftLookoutRoom && !onlyX && !onlyY;
                Q4LookoutRoomScroll = Q4LookoutRoom != null && Q4LookoutRoom != downLookoutRoom && Q4LookoutRoom != rightLookoutRoom && !onlyX && !onlyY;


                // Then check whether if the currently held direction (might be none, might be L/R/U/D, might be diagonal) points. If yes, warp prompt
                Vector2 inputVectorDiagonalCorrect = new Vector2(inputVector.X, inputVector.Y);

                lookoutRoom = findLookoutRoom(inputVector, edgeRoomDataList, out Vector2 roomAimPos);

                //
                // The view other rooms part
                //
                if (lookoutRoom != null && Input.MenuConfirm && interacting && changeRoomCooldown == 0)
                {
                    // Transition camera to target pos (slightly farther that roomAimPos)
                    Vector2 targetTransitionPos = roomAimPos + inputVector * 16;
                    transitionToTarget(targetTransitionPos, lookoutRoom, out Vector2 newTargetPositionPos);

                    lookoutRoom = null; //This is set to null (almost) immediately so the (c) prompt can't show up during the transition

                    // Update the current level (and camera) after a frame
                    // Do it twice, cause sometimes the first one fails
                    updateLevelAfterFrame(1, newTargetPositionPos - new Vector2(160f, 90f));
                    updateLevelAfterFrame(2, newTargetPositionPos - new Vector2(160f, 90f));

                    pleaseStopFlickeringThankYou(); // padding please freaking stay on
                }
            }

            void transitionToTarget(Vector2 targetTransitionPos, LevelData newRoomLevelData, out Vector2 newTargetPositionPos)
            {
                Vector2 camCenter = camCorner + new Vector2(160f, 90f);

                // Modify targetTransitionPos so that it's not at the edge of the room, but rather somewhere the camera center can reach
                Rectangle newRoomBounds = newRoomLevelData.Bounds;
                targetTransitionPos.X = Calc.Clamp(targetTransitionPos.X, newRoomBounds.Left + 160, newRoomBounds.Right - 160);
                targetTransitionPos.Y = Calc.Clamp(targetTransitionPos.Y, newRoomBounds.Top + 90, newRoomBounds.Bottom - 90);

                Vector2 transitionDirection = (targetTransitionPos - camCenter).SafeNormalize();

                //ResetCameraTrackSettings(level, player, false);
                player.Position = new Vector2(newRoomBounds.Left + 16, newRoomBounds.Bottom - 16);
                player.CameraAnchor = targetTransitionPos - new Vector2(160f, 90f);

                level.TransitionTo(newRoomLevelData, transitionDirection);

                // If watchtower room, set player position to watchtower, and let the player be active and have collision
                // Otherwise, inactive and no collision
                if (newRoomLevelData.Name == camStartRoomName)
                {
                    player.Position = watchtowerPosition;
                    player.Visible = true;
                    player.Active = allowAnywhere ? false : true;
                    player.Collidable = true;
                    player.Collider = originalPlayerCollider;
                }
                else
                {
                    player.Visible = false;
                    player.Active = false;
                    player.Collidable = false;
                    player.Collider = new Hitbox(-9999f, -9999f, 0f, 0f);
                }

                changeRoomCooldown = 10; // Rooms can't be changed for this number of frames
                newTargetPositionPos = targetTransitionPos;
            }

            async void pleaseStopFlickeringThankYou()
            {
                int repeat = 100;
                while (repeat > 0 && interacting)
                {
                    repeat--;
                    level.ScreenPadding = 16;
                    level.CameraLockMode = Level.CameraLockModes.None;
                    player.CleanUpTriggers();
                    await Task.Delay(1);
                }
            }

            async void updateLevelAfterFrame(int frames, Vector2? cameraTargetPos = null)
            {
                await Task.Delay((int)(Engine.DeltaTime * 1000 * frames + 1));

                // Update variables with new level information
                level = SceneAs<Level>();

                level.CanRetry = false;
                level.SaveQuitDisabled = true;
                level.PauseLock = true; //Prevent cheese
                level.ScreenPadding = 16;
                edgeRoomDataList = getEdgeRoomDataList(level);
                currentRoomLevelData = level.Session.LevelData;
                currentRoomBounds = currentRoomLevelData.Bounds;

                // Look at target
                if (cameraTargetPos != null)
                {
                    //ResetCameraTrackSettings(level, player, false);
                    player.CameraAnchor = cameraTargetPos.Value;
                }
            }


            bool checkBlockerAtPos(Vector2 position, Scene level)
            {
                if (ignoreBlocker)
                {
                    return false;
                }

                List<Entity> lookoutBlockerEntityList = level.Tracker.GetEntities<LookoutBlocker>();

                foreach (Entity blockerEntity in lookoutBlockerEntityList)
                {
                    if (position.X > blockerEntity.Left && position.X < blockerEntity.Right && position.Y > blockerEntity.Top && position.Y < blockerEntity.Bottom)
                    {
                        return true;
                    }
                }
                return false;
            }

            LevelData findLookoutRoom(Vector2 lookDirection, List<LevelData> edgeRoomDataList, out Vector2 roomAimPos)
            {
                roomAimPos = level.Camera.Position + new Vector2(160f, 90f);
                // Get room that the lookDirection is pointing towards
                if (lookDirection != Vector2.Zero)
                {
                    // Extend vector until it exits the current room
                    while (roomAimPos.X >= currentRoomBounds.Left && roomAimPos.X <= currentRoomBounds.Right
                        && roomAimPos.Y >= currentRoomBounds.Top && roomAimPos.Y <= currentRoomBounds.Bottom)
                    {
                        roomAimPos += lookDirection * 8;
                        if (checkBlockerAtPos(roomAimPos, level))
                        {
                            return null; //Blocker found! Stop everything.
                        }
                    }

                    // This is forced to be up/down/left/right. If it's diagonal (exceed both x and y), push the y pos back
                    if (
                        (roomAimPos.X < currentRoomBounds.Left + 7 || roomAimPos.X > currentRoomBounds.Right - 7)
                        && (roomAimPos.Y < currentRoomBounds.Top + 7 || roomAimPos.Y > currentRoomBounds.Bottom - 7)
                       )
                    {
                        roomAimPos.Y = Calc.Clamp(roomAimPos.Y, currentRoomBounds.Top + 7, currentRoomBounds.Bottom - 7);
                    }

                    // Extend the checkRect a bit. 80 for L/R, 45 for U/D
                    Rectangle checkRect = new Rectangle(-1, -1, 2, 2);
                    if (lookDirection.X != 0)
                    {
                        checkRect.Y = -40;
                        checkRect.Height = 80;
                    }
                    if (lookDirection.Y != 0)
                    {
                        checkRect.X = -50;
                        checkRect.Width = 100;
                    }
                    LevelData aimRoomData = findOverlapRoom(roomAimPos, edgeRoomDataList, checkRect);
                    return aimRoomData;
                }
                else
                {
                    return null;
                }
            }

            LevelData findOverlapRoom(Vector2 roomAimPos, List<LevelData> edgeRoomDataList, Rectangle? checkRect = null)
            {

                if (checkRect == null)
                {
                    checkRect = new Rectangle(-1, -1, 2, 2);
                }

                Rectangle checkRectHalfAbsolute = new Rectangle((int)roomAimPos.X + checkRect.Value.Left/2, (int)roomAimPos.Y + checkRect.Value.Top/2, checkRect.Value.Width/2, checkRect.Value.Height/2);
                Rectangle checkRectAbsolute = new Rectangle((int)roomAimPos.X + checkRect.Value.Left, (int)roomAimPos.Y + checkRect.Value.Top, checkRect.Value.Width, checkRect.Value.Height);

                // Check the exact point first - so the selected room is the one that makes more sense if there's multiple available
                foreach (LevelData levelData in edgeRoomDataList)
                {
                    Rectangle levelDataBounds = levelData.Bounds;

                    if (roomAimPos.X < levelDataBounds.Right + 7 && roomAimPos.X > levelDataBounds.Left - 7
                        && roomAimPos.Y > levelDataBounds.Top - 7 && roomAimPos.Y < levelDataBounds.Bottom + 7)
                    {
                        return levelData; //Found room that this position aims at
                    }
                }

                // If no room found, check half the size of the rect instead. This is so rooms closer to the rect are "prioritised" if there are multiple.
                foreach (LevelData levelData in edgeRoomDataList)
                {
                    Rectangle levelDataBounds = levelData.Bounds;
                    if (checkRectHalfAbsolute.Intersects(levelDataBounds))
                    {
                        return levelData; //Found room that this position aims at
                    }
                }

                // If STILL no room found, check in the rect instead
                foreach (LevelData levelData in edgeRoomDataList)
                {
                    Rectangle levelDataBounds = levelData.Bounds;
                    if (checkRectAbsolute.Intersects(levelDataBounds))
                    {
                        return levelData; //Found room that this position aims at
                    }
                }

                // if **STILL** no room found, return null
                return null;
            }

            lastDir = inputVector;
            if (sprite.CurrentAnimationID != "lookLeft" && sprite.CurrentAnimationID != "lookRight")
            {
                if (inputVector.X == 0f)
                {
                    if (inputVector.Y == 0f)
                    {
                        sprite.Play(animPrefix + "looking");
                    }
                    else if (inputVector.Y > 0f)
                    {
                        sprite.Play(animPrefix + "lookingDown");
                    }
                    else
                    {
                        sprite.Play(animPrefix + "lookingUp");
                    }
                }
                else if (inputVector.X > 0f)
                {
                    if (inputVector.Y == 0f)
                    {
                        sprite.Play(animPrefix + "lookingRight");
                    }
                    else if (inputVector.Y > 0f)
                    {
                        sprite.Play(animPrefix + "lookingDownRight");
                    }
                    else
                    {
                        sprite.Play(animPrefix + "lookingUpRight");
                    }
                }
                else if (inputVector.X < 0f)
                {
                    if (inputVector.Y == 0f)
                    {
                        sprite.Play(animPrefix + "lookingLeft");
                    }
                    else if (inputVector.Y > 0f)
                    {
                        sprite.Play(animPrefix + "lookingDownLeft");
                    }
                    else
                    {
                        sprite.Play(animPrefix + "lookingUpLeft");
                    }
                }
            }

            if (nodes == null)
            {
                speed += accel * inputVector * Engine.DeltaTime;
                if (inputVector.X == 0f)
                {
                    speed.X = Calc.Approach(speed.X, 0f, accel * 2f * Engine.DeltaTime);
                }

                if (inputVector.Y == 0f)
                {
                    speed.Y = Calc.Approach(speed.Y, 0f, accel * 2f * Engine.DeltaTime);
                }

                if (speed.Length() > maxspd)
                {
                    speed = speed.SafeNormalize(maxspd);
                }

                Vector2 vector = camCorner;
                List<Entity> lookoutBlockerEntityList = level.Tracker.GetEntities<LookoutBlocker>();

                camCorner.X += speed.X * Engine.DeltaTime;
                camCorner.Y += speed.Y * Engine.DeltaTime;

                if (camCorner.X < level.Bounds.Left || camCorner.X + 320f > level.Bounds.Right)
                {
                    speed.X = 0f;
                }
                if (camCorner.Y < level.Bounds.Top || camCorner.Y + 180f > level.Bounds.Bottom)
                {
                    speed.Y = 0f;
                }

                if (!ignoreBlocker)
                {
                    foreach (Entity blockerEntity in lookoutBlockerEntityList)
                    {
                        if (camCorner.X + 320f > blockerEntity.Left && camCorner.Y + 180f > blockerEntity.Top && camCorner.X < blockerEntity.Right && camCorner.Y < blockerEntity.Bottom)
                        {
                            camCorner.X = vector.X;
                            speed.X = 0f;
                        }
                        if (camCorner.X + 320f > blockerEntity.Left && camCorner.Y + 180f > blockerEntity.Top && camCorner.X < blockerEntity.Right && camCorner.Y < blockerEntity.Bottom)
                        {
                            camCorner.Y = vector.Y;
                            speed.Y = 0f;
                        }
                    }
                }

                camCorner.X = Calc.Clamp(camCorner.X, level.Bounds.Left, level.Bounds.Right - 320);
                camCorner.Y = Calc.Clamp(camCorner.Y, level.Bounds.Top, level.Bounds.Bottom - 180);

                level.Camera.Position = camCorner;
            }
            else
            {
                // FOR NODES. Nodepercent is percent BETWEEN NODES, not total track percent!
                int movementLimit = 500;
                camCorner = level.Camera.Position;
                moveBino();

                void moveBino()
                {
                    movementLimit--;
                    // Get Node Details
                    Vector2 originalCam = camCorner;
                    float nodeBetweenLength;

                    //(nextNodePosition - previousNodePosition).SafeNormalize();

                    // Set Camera
                    setCamera();
                    void setCamera()
                    {
                        Vector2 previousNodePosition = currentNodeNum <= 0 ? camStartCenter : nodes[currentNodeNum - 1];
                        Vector2 nextNodePosition = nodes[currentNodeNum];
                        nodeBetweenLength = (previousNodePosition - nextNodePosition).Length();

                        if (modifiedInterpolation)
                        {
                            Vector2 previous2NodePosition = currentNodeNum <= 1 ? camStartCenter : nodes[currentNodeNum - 2];
                            Vector2 next2NodePosition = currentNodeNum >= nodes.Count - 1 ? nodes[currentNodeNum] : nodes[currentNodeNum + 1];

                            camCorner = CatmullRomInterpolation(previous2NodePosition, previousNodePosition, nextNodePosition, next2NodePosition, nodePercent);
                        }
                        else
                        {
                            if (nodePercent < 0.25f && currentNodeNum > 0)
                            {
                                Vector2 previous2NodePosition = currentNodeNum <= 1 ? camStartCenter : nodes[currentNodeNum - 2];
                                Vector2 begin = Vector2.Lerp(previous2NodePosition, previousNodePosition, 0.75f); //Prev-2 node to prev-1 node
                                Vector2 end = Vector2.Lerp(previousNodePosition, nextNodePosition, 0.25f); //Prev-1 to Next+1
                                SimpleCurve simpleCurve = new SimpleCurve(begin, end, previousNodePosition);

                                camCorner = simpleCurve.GetPoint(0.5f + nodePercent / 0.25f * 0.5f);

                            }
                            else if (nodePercent > 0.75f && currentNodeNum < nodes.Count - 1)
                            {
                                Vector2 next2NodePosition = nodes[currentNodeNum + 1];
                                Vector2 begin2 = Vector2.Lerp(previousNodePosition, nextNodePosition, 0.75f); //Prev-1 to Next+1
                                Vector2 end2 = Vector2.Lerp(nextNodePosition, next2NodePosition, 0.25f); //Next+1 to Next+2 (same as above but the other end)
                                SimpleCurve simpleCurve2 = new SimpleCurve(begin2, end2, nextNodePosition);
                                camCorner = simpleCurve2.GetPoint((nodePercent - 0.75f) / 0.25f * 0.5f);

                            }
                            else
                            {
                                camCorner = Vector2.Lerp(previousNodePosition, nextNodePosition, nodePercent);
                            }
                        }

                        camCorner += new Vector2(-160f, -90f); //TopLeftCorner-ify
                    }

                    Vector2 CatmullRomInterpolation(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
                    {
                        float t2 = t * t;
                        float t3 = t2 * t;

                        return 0.5f * (
                            2 * p1 +
                            (-p0 + p2) * t +
                            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
                            (-p0 + 3 * p1 - 3 * p2 + p3) * t3
                        );
                    }

                    // The part where you actually move the bino
                    float moveNodeAmount = inputVector.Y * (maxspd / nodeBetweenLength) * Engine.DeltaTime;

                    if (inputVector.Y != 0 && changeRoomCooldown == 0)
                    {
                        nodePercent -= moveNodeAmount;

                        if (nodePercent < 0f)
                        {
                            if (currentNodeNum > 0)
                            {
                                currentNodeNum--;
                                nodePercent = 1f;
                            }
                            else
                            {
                                nodePercent = 0f;
                            }
                        }
                        else if (nodePercent > 1f)
                        {
                            if (currentNodeNum < nodes.Count - 1)
                            {
                                currentNodeNum++;
                                nodePercent = 0f;
                            }
                            else
                            {
                                nodePercent = 1f;
                                // if (summit) { break; }
                            }
                        }

                        // Obtain progress
                        float trackProgress = 0f;
                        float trackTotalLength = 0f;
                        for (int nodeNum2 = 0; nodeNum2 < nodes.Count; nodeNum2++)
                        {
                            float betweenNodeLength2 = ((nodeNum2 == 0 ? camStartCenter : nodes[nodeNum2 - 1]) - nodes[nodeNum2]).Length();
                            trackTotalLength += betweenNodeLength2;
                            if (nodeNum2 < currentNodeNum)
                            {
                                trackProgress += betweenNodeLength2;
                            }
                            else if (nodeNum2 == currentNodeNum)
                            {
                                trackProgress += betweenNodeLength2 * nodePercent;
                            }
                        }

                        trackPercent = trackProgress / trackTotalLength;


                        // Transition Handler
                        // If part of screen is offscreen AND it is not at the ends, continue moving!

                        // Slightly smaller than the camera - camCorner must partially exceed to transition (the actual camera is clamped)
                        // Inner box only requires y or x coordinate to be within the room - this is here to try making node placements not as strict
                        int buffer = 2;
                        int bufferInner = 8;
                        Vector2 camCenter = camCorner + new Vector2(160f, 90f);
                        Rectangle checkRequireTransitionBox = new Rectangle((int)camCorner.X + 8 * buffer, (int)camCorner.Y + 8 * buffer, 320 - 16 * buffer, 180 - 16 * buffer);
                        Rectangle checkRequireTransitionBoxInner = new Rectangle((int)camCorner.X + 8 * bufferInner, (int)camCorner.Y + 8 * bufferInner, 320 - 16 * bufferInner, 180 - 16 * bufferInner);

                        if (trackPercent >= 0f && trackPercent <= 1f
                            && !(
                                    currentRoomBounds.Contains(checkRequireTransitionBoxInner) &&
                                    (checkRequireTransitionBox.Left > currentRoomBounds.Left && checkRequireTransitionBox.Right < currentRoomBounds.Right
                                    || checkRequireTransitionBox.Top > currentRoomBounds.Top && checkRequireTransitionBox.Bottom < currentRoomBounds.Bottom)
                                    ) //If offscreen
                                )
                        {
                            // Check if it's within range for *ANY* room (along the edge)
                            foreach (LevelData room in edgeRoomDataList)
                            {
                                Rectangle roomBounds = room.Bounds;

                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"%: {trackPercent} | Checking room {room.Name} --- {checkRequireTransitionBox.Right - checkRequireTransitionBox.Left} x {checkRequireTransitionBox.Bottom - checkRequireTransitionBox.Top} vs {room.Bounds.Right - room.Bounds.Left} x {room.Bounds.Bottom - room.Bounds.Top}:");
                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"FALL WITHIN: {roomBounds.Contains(checkRequireTransitionBoxInner)} (located at {camCenter})");
                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"LEFT: {checkRequireTransitionBox.Left} > {roomBounds.Left} -- {checkRequireTransitionBox.Left > roomBounds.Left}");
                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"RIGHT: {checkRequireTransitionBox.Right} < {roomBounds.Right} -- {checkRequireTransitionBox.Right < roomBounds.Right}");
                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"UP: {checkRequireTransitionBox.Top} > {roomBounds.Top} -- {checkRequireTransitionBox.Top > roomBounds.Top}");
                                //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"DOWN: {checkRequireTransitionBox.Bottom} < {roomBounds.Bottom} -- {checkRequireTransitionBox.Bottom < roomBounds.Bottom}");

                                //if (checkRequireTransitionBox.Left > roomBounds.Left && checkRequireTransitionBox.Right < roomBounds.Right
                                //    && checkRequireTransitionBox.Top > roomBounds.Top && checkRequireTransitionBox.Bottom < roomBounds.Bottom) //If onscreen
                                if (
                                    roomBounds.Contains(checkRequireTransitionBoxInner) &&
                                    (checkRequireTransitionBox.Left > roomBounds.Left && checkRequireTransitionBox.Right < roomBounds.Right
                                    || checkRequireTransitionBox.Top > roomBounds.Top && checkRequireTransitionBox.Bottom < roomBounds.Bottom)
                                    ) //If onscreen
                                {
                                    // Within range: Transition the room here

                                    setCamera(); //Fixes some smoothness jank

                                    // Transition camera to target pos (slightly farther that roomAimPos)
                                    transitionToTarget(camCorner + new Vector2(160, 90), room, out Vector2 transitionPosTarget);

                                    // Update the current level (and camera) after a frame
                                    // Do it twice, cause sometimes the first one fails

                                    camCorner = transitionPosTarget - new Vector2(160, 90);

                                    updateLevelAfterFrame(1, camCorner);
                                    updateLevelAfterFrame(2, camCorner);

                                    pleaseStopFlickeringThankYou(); // padding please freaking stay on

                                    break; //Note: moveBino triggers one more time to update camera pos
                                }
                            }
                            if (movementLimit <= 0)
                            {
                                Logger.Log(LogLevel.Warn, "EndHelper/Misc/MultiroomWatchtower", $"Movement limit exceeded. After {currentRoomLevelData.Name}, cannot find next room! Node Number: {currentNodeNum},  NodePercent: {nodePercent}, trackPercent: {trackPercent}");
                            }
                            else if (trackPercent > 0f && trackPercent < 1f)
                            {
                                moveBino();
                            }
                        }
                    }
                }
                if (changeRoomCooldown == 0)
                {
                    camCorner.X = Calc.Clamp(camCorner.X, level.Bounds.Left, level.Bounds.Right - 320);
                    camCorner.Y = Calc.Clamp(camCorner.Y, level.Bounds.Top, level.Bounds.Bottom - 180);
                    level.Camera.Position = camCorner;
                }
            }
            yield return null;
        }
        //
        // End interaction
        //

        Audio.Play("event:/ui/game/lookout_off");
        while ((hud.Easer = Calc.Approach(hud.Easer, 0f, Engine.DeltaTime * 3f)) > 0f)
        {
            level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
            yield return null;
        }

        bool atSummitTop = summit && currentNodeNum >= nodes.Count - 1 && nodePercent >= 0.95f;
        if (atSummitTop)
        {
            yield return 0.5f;
            float duration2 = 3f;
            float approach2 = 0f;
            Coroutine component = new Coroutine(level.ZoomTo(new Vector2(160f, 90f), 2f, duration2));
            Add(component);
            while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && !Input.Pause.Pressed && !Input.ESC.Pressed && interacting)
            {
                approach2 = Calc.Approach(approach2, 1f, Engine.DeltaTime / duration2);
                Audio.SetMusicParam("escape", approach2);
                yield return null;
            }
        }

        if ((camStart - level.Camera.Position).Length() > 600f)
        {
            Vector2 was = level.Camera.Position;
            Vector2 direction = (was - camStart).SafeNormalize();
            float approach2 = atSummitTop ? 1f : 0.5f;
            new FadeWipe(level, wipeIn: false).Duration = approach2;
            for (float duration2 = 0f; duration2 < 1f; duration2 += Engine.DeltaTime / approach2)
            {
                level.Camera.Position = was - direction * MathHelper.Lerp(0f, 64f, Ease.CubeIn(duration2));
                yield return null;
            }

            level.Camera.Position = camStart + direction * 32f;
            new FadeWipe(level, wipeIn: true);
        }

        Audio.SetMusicParam("escape", 0f); //i have no idea what this does it was in the regular bino code tho
        level.ScreenPadding = 0f;
        level.ZoomSnap(Vector2.Zero, 1f);
        level.Remove(hud);
        interacting = false;
        previouslyInteracted = true;
        player.StateMachine.State = 0;

        // Move camera back
        if (level.Session.LevelData.Name != camStartRoomName)
        {
            level.TransitionTo(camStartLevelData, (camStartCenter - camCorner).SafeNormalize());
        }
        ResetCameraTrackSettings(level, player, true);
        player.Position = watchtowerPosition;

        sprite.Play(animPrefix + "idle");

        level.CanRetry = canRetryInitial;
        level.SaveQuitDisabled = canSaveQuitInitial;
        level.PauseLock = pauseLock;

        removeTagAfterFrame(3);

        async void removeTagAfterFrame(int frames)
        {
            await Task.Delay((int)(Engine.DeltaTime * 1000 * frames + 1));

            RemoveTag(Tags.Persistent);
            {if (level.Tracker.GetEntity<RoomStatisticsDisplayer>() is RoomStatisticsDisplayer roomStatDisplayer)
            {
                roomStatDisplayer.disableRoomChange = false;
            }}
            player.Active = true;
            player.Visible = true;
            player.Collidable = true;
            player.Collider = originalPlayerCollider;
            player.Sprite.Visible = true;
            player.Hair.Visible = true;

            previouslyInteracted = true;

            if (destroyUponFinishView)
            {
                RemoveSelf();
            }
        }

        // Prevent previous view's arrows for appearing for a split second when reviewing from the beginning
        leftLookoutRoomScroll = rightLookoutRoomScroll = upLookoutRoomScroll = downLookoutRoomScroll = 
            Q1LookoutRoomScroll = Q2LookoutRoomScroll = Q3LookoutRoomScroll = Q4LookoutRoomScroll = false;

    yield return null;
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
    }

    public List<LevelData> getEdgeRoomDataList(Level level)
    {
        LevelData currentRoomLevelData = level.Session.LevelData;
        Rectangle currentRoomBounds = currentRoomLevelData.Bounds;
        Rectangle currentRoomBoundsExt = currentRoomLevelData.Bounds;
        currentRoomBoundsExt.Inflate(7, 7);

        List<LevelData> edgeRoomDataList = [];

        foreach (LevelData levelData in level.Session.MapData.Levels)
        {
            if (levelData != currentRoomLevelData && levelData.Spawns.Count > 0)
            {
                Rectangle roomBounds = levelData.Bounds;
                if (roomBounds.Intersects(currentRoomBoundsExt))
                {
                    edgeRoomDataList.Add(levelData);
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/MultiroomWatchtower", $"Adjacent room: {levelData.Name}");
                }
            }
        }
        return edgeRoomDataList;
    }

    void ResetCameraTrackSettings(Level level, Player player, bool endWatchtowerView)
    {
        if (endWatchtowerView)
        {
            player.CameraAnchorLerp = Vector2.Zero;
            player.ForceCameraUpdate = true;
        }
        else
        {
            player.CameraAnchorLerp = Vector2.One;
            player.ForceCameraUpdate = false;
        }
        level.CameraOffset = Vector2.Zero;
        player.CameraAnchorIgnoreX = false;
        player.CameraAnchorIgnoreY = false;
        level.CameraLockMode = Level.CameraLockModes.None;
        player.CleanUpTriggers();
        level.CancelCutscene();
        level.EndCutscene();
    }
}