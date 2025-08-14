using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.EndHelper.Entities.Misc;
[Tracked(true)]
[TrackedAs(typeof(CustomBirdTutorial))]
[CustomEntity("EndHelper/ConditionalBirdTutorial")]

public class ConditionalBirdTutorial : CustomBirdTutorial
{
    private readonly EntityData entityData;
    #pragma warning disable CS0108
    private readonly Vector2[] nodes;
    #pragma warning restore CS0108d

    private readonly bool showSprite = true;
    private float flyInSpeedMultiplier = 1;
    private readonly bool onlyOnceFlyIn;
    private readonly bool onlyFulfillConditionOnce;
    private bool flownIn = false;

    private readonly int requireFrameInZoneTotal = 0;
    private readonly int requireFrameInZoneAtOnce = 0;
    private readonly int requireFrameInRoom = 0;
    private readonly int requireDeathsInZone = 0;
    private readonly int requireDeathsInRoom = 0;
    private readonly bool requireOnScreen = true;
    private readonly string requireFlag = "";
    private readonly string requireFlagForIncrement = "";

    private Vector2 restPosition;
    private Rectangle nodeBounds;
    private bool triggered;

    public ConditionalBirdTutorial(EntityData data, Vector2 offset)
            : base(data, offset)
    {
        entityData = data;
        nodes = data.NodesOffset(offset);

        showSprite = data.Bool("showSprite", true);
        flyInSpeedMultiplier = data.Float("flyInSpeedMultiplier", 1);
        onlyOnceFlyIn = data.Bool("onlyOnceFlyIn", true);
        onlyFulfillConditionOnce = data.Bool("onlyFulfillConditionOnce", true);

        requireFrameInZoneTotal = (int)(data.Float("secInZoneTotal", 0) * 60f);
        requireFrameInZoneAtOnce = (int)(data.Float("secInZoneAtOnce", 0) * 60f);
        requireFrameInRoom = (int)(data.Float("secInRoom", 0) * 60f);
        requireDeathsInZone = data.Int("deathsInZone", 0);
        requireDeathsInRoom = data.Int("deathsInRoom", 0);
        requireOnScreen = data.Bool("requireOnScreen", true);
        requireFlag = data.Attr("requireFlag", "");
        requireFlagForIncrement = data.Attr("requireFlagForIncrement", "");

        restPosition = data.Position + offset;
        //Logger.Log(LogLevel.Info, "EndHelper/Misc/ConditionalBirdTutorial", $"fly in time {flyInSpeedMultiplier}  id {BirdId}  startPos {Position} --- required requireFrameInZoneAtOnce: {requireFrameInZoneAtOnce}");
    }

    public override void Added(Scene scene)
    {
        if (!showSprite)
        {
            flyInSpeedMultiplier = 0;
        }
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        string trackerPrefix = $"EndHelper_ConditionalBirdTutorial_{entityData.ID}";
        Level level = SceneAs<Level>();
        bool flewInBefore = level.Session.GetFlag($"{trackerPrefix}_flewInBefore");

        if (!showSprite) { Visible = false; }

        Vector2 node1Pos = nodes[0];
        Vector2 node2Pos = nodes[1];

        float top = (node1Pos.Y < node2Pos.Y ? node1Pos.Y : node2Pos.Y) - 12f;
        float bottom = (node1Pos.Y > node2Pos.Y ? node1Pos.Y : node2Pos.Y) + 12f;
        float left = (node1Pos.X < node2Pos.X ? node1Pos.X : node2Pos.X) - 12f;
        float right = (node1Pos.X > node2Pos.X ? node1Pos.X : node2Pos.X) + 12f;
        nodeBounds = new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));

        //base.Awake(scene);
        //I don't want the regular awake to trigger

        if (flewInBefore && onlyOnceFlyIn && 
            ( onlyFulfillConditionOnce ) || (!onlyFulfillConditionOnce && CheckMetFlyInCondition(level, true))
           )
        {
            // Stay flown in (but not triggered). If no onlyFulfillConditionOnce, flewInBefore is temporarily saved for this check. If condition met, start already flown in.
            StartFlyIn(showTutorial: false, skipFly: true);
        }
        else
        {
            // Prepare Fly in
            SetStartingPosition();
        }

        if (!onlyFulfillConditionOnce)
        {
            level.Session.SetFlag($"{trackerPrefix}_flewInBefore", false); // Reset flewInBefore if no onlyFulfillConditionOnce
        }
    }

    public override void Update()
    {
        base.Update();

        UpdateConditionTracking_Time();
        FlyInIfMetCondition();

    }

    private void UpdateConditionTracking_Time(bool avoidIncrement = false)
    {
        Level level = SceneAs<Level>();
        if (!Utils_General.AreFlagsEnabled(level.Session, requireFlagForIncrement, true))
        {
            avoidIncrement = true; // Avoid incrementing if require flag for condition fails
        }

        if (level.Tracker.GetEntity<Player>() is Player player)
        {
            string trackerPrefix = $"EndHelper_ConditionalBirdTutorial_{entityData.ID}";
            if (nodeBounds.Contains((int)player.X, (int)player.Y))
            {
                if (!avoidIncrement)
                {
                    // --- IN BOUNDS ---
                    // secInZoneTotal
                    int frameInZoneTotal = level.Session.GetCounter($"{trackerPrefix}_frameInZoneTotal");
                    level.Session.SetCounter($"{trackerPrefix}_frameInZoneTotal", frameInZoneTotal + 1);

                    // secInZoneAtOnce
                    int frameInZoneAtOnce = level.Session.GetCounter($"{trackerPrefix}_frameInZoneAtOnce");
                    level.Session.SetCounter($"{trackerPrefix}_frameInZoneAtOnce", frameInZoneAtOnce + 1);
                }
            }
            else
            {
                // --- OUT OF BOUNDS ---
                // secInZoneAtOnce
                level.Session.SetCounter($"{trackerPrefix}_frameInZoneAtOnce", 0);
            }
            // --- ANY BOUNDS ---
            // secInRoom
            if (!avoidIncrement)
            {
                int frameInRoom = level.Session.GetCounter($"{trackerPrefix}_frameInRoom");
                level.Session.SetCounter($"{trackerPrefix}_frameInRoom", frameInRoom + 1);
            }
        }
    }

    internal void UpdateConditionTracking_Death(bool avoidIncrement = false) // From EndHelperModule - ILRunOnPlayerDeath
    {
        Level level = SceneAs<Level>();
        if (!Utils_General.AreFlagsEnabled(level.Session, requireFlagForIncrement, true))
        {
            avoidIncrement = true; // Avoid incrementing if require flag for condition fails
        }

        if (level.Tracker.GetEntity<Player>() is Player player)
        {
            string trackerPrefix = $"EndHelper_ConditionalBirdTutorial_{entityData.ID}";
            if (nodeBounds.Contains((int)player.X, (int)player.Y))
            {
                if (!avoidIncrement)
                {
                    // --- IN BOUNDS ---
                    // deathsInZone
                    int deathsInZone = level.Session.GetCounter($"{trackerPrefix}_deathsInZone");
                    level.Session.SetCounter($"{trackerPrefix}_deathsInZone", deathsInZone + 1);
                }

            }
            else
            {
                // --- OUT OF BOUNDS ---
                // nothing!
            }
            // --- ANY BOUNDS ---
            // deathsInRoom
            if (!avoidIncrement)
            {
                int deathsInRoom = level.Session.GetCounter($"{trackerPrefix}_deathsInRoom");
                level.Session.SetCounter($"{trackerPrefix}_deathsInRoom", deathsInRoom + 1);
            }

        }
    }

    private void FlyInIfMetCondition()
    {
        // Do logic to check if condition met
        Level level = SceneAs<Level>();
        if (CheckMetFlyInCondition(level))
        {
            // All passed! Get the bird to fly in.
            bool skipFly = false;
            if (flownIn)
            {
                skipFly = true;
            }
            StartFlyIn(showTutorial: true, skipFly: skipFly);
        }
    }
    
    private bool CheckMetFlyInCondition(Level level, bool recheckIfOutsideZone = false)
    {
        if (recheckIfOutsideZone) UpdateConditionTracking_Time(true);

        if (level.Tracker.GetEntity<Player>() is Player player)
        {
            // Look through each condition. If any fails, exit.
            // Ensure hasn't triggered yet
            if (triggered || player.Dead || level.Transitioning)
            {
                return false;
            }

            string trackerPrefix = $"EndHelper_ConditionalBirdTutorial_{entityData.ID}";
            bool flewInBefore = level.Session.GetFlag($"{trackerPrefix}_flewInBefore");

            if (!flewInBefore || !onlyFulfillConditionOnce)
            {
                // secInRoom Checks
                if (level.Session.GetCounter($"{trackerPrefix}_frameInZoneTotal") < requireFrameInZoneTotal) return false;
                if (level.Session.GetCounter($"{trackerPrefix}_frameInZoneAtOnce") < requireFrameInZoneAtOnce) return false;
                if (level.Session.GetCounter($"{trackerPrefix}_frameInRoom") < requireFrameInRoom) return false;

                // death checks
                if (level.Session.GetCounter($"{trackerPrefix}_deathsInZone") < requireDeathsInZone) return false;
                if (level.Session.GetCounter($"{trackerPrefix}_deathsInRoom") < requireDeathsInRoom) return false;

                // flag check
                bool passCheck = Utils_General.AreFlagsEnabled(level.Session, requireFlag, true);
                if (!passCheck) return false;
            }

            // within screen boundary check
            if (requireOnScreen)
            {
                Rectangle entityEndRectangle = new Rectangle((int)restPosition.X - 8, (int)restPosition.Y - 16, entityData.Width + 16, entityData.Height + 16);
                //if (!level.Bounds.Contains(entityEndRectangle)) return;
                Rectangle cameraRectangle = new Rectangle((int)level.Camera.X, (int)level.Camera.Y, (int)(level.Camera.Right - level.Camera.Left), (int)(level.Camera.Bottom - level.Camera.Top));
                if (!cameraRectangle.Contains(entityEndRectangle)) return false;
            }

            return true; // All checks passed!
        }
        return false;
    }

    private void SetStartingPosition()
    {
        if (flyInSpeedMultiplier == 0)
        {
            return;
        }
        Level level = SceneAs<Level>();
        Rectangle levelBounds = level.Bounds;
        Vector2 flyawaySpeed = new Vector2((int)Facing * -5, -10f);
        Vector2 startingPos = restPosition;

        int maxIterationFailsafe = 500;

        while (startingPos.Y > levelBounds.Top - 16 && maxIterationFailsafe > 0)
        {
            startingPos += flyawaySpeed; // Keep moving in this direction until offscreen
            maxIterationFailsafe--;
        }

        Position = startingPos;
        Visible = false;
        Sprite.Active = false;
    }


    private void StartFlyIn(bool showTutorial, bool skipFly)
    {
        if (!triggered)
        {
            Add(new Coroutine(FlyIn(showTutorial, skipFly)));
        }
    }
    private IEnumerator FlyIn(bool showTutorial, bool skipFly)
    {
        if (showTutorial)
        {
            triggered = true;
        }
        Level level = SceneAs<Level>();

        string trackerPrefix = $"EndHelper_ConditionalBirdTutorial_{entityData.ID}";

        level.Session.SetFlag($"{trackerPrefix}_flewInBefore", true);
        flownIn = true;

        if (flyInSpeedMultiplier > 0 && !skipFly)
        {
            if (showSprite) { 
                Visible = true;
                Sprite.Active = true;
            }
            Sprite.Play("fly");

            EventInstance instance = Audio.Play("event:/game/general/bird_in", Position);
            Sprite.Play("fall");

            // Animate fly in
            float percent = 0f;
            Vector2 from = Position;
            Vector2 to = restPosition;
            while (percent < 1f)
            {
                Position = from + (to - from) * Ease.QuadOut(percent);
                Audio.Position(instance, Position);
                if (percent > 0.5f)
                {
                    Sprite.Play("fly");
                }

                percent += Engine.RawDeltaTime * flyInSpeedMultiplier;
                yield return null;
            }

            // Landed
            Audio.Play("event:/game/general/bird_land_dirt", Position);
            Dust.Burst(Position, -MathF.PI / 2f, 12, null);
        }
        Position = restPosition;
        Sprite.Play("idle");

        if (showTutorial)
        {
            TriggerShowTutorial();
        }
        yield break;
    }
}