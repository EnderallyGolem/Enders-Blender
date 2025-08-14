using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Triggers.DeathHandler;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static MonoMod.InlineRT.MonoModRule;

namespace Celeste.Mod.EndHelper.Entities.DeathHandler;

[CustomEntity("EndHelper/DeathHandlerBypassZone")]
public class DeathHandlerBypassZone : Entity
{
    internal readonly MTexture borderTexture;
    internal readonly EntityID entityID;

    internal enum BypassEffect { Activate, Deactivate, Toggle, None }
    private readonly BypassEffect mainEffect;
    private readonly BypassEffect altEffect;
    internal readonly string altFlag;
    internal readonly bool attachable;
    internal readonly string bypassFlag;
    internal readonly bool affectPlayer;

    internal readonly float width;
    internal readonly float height;

    internal BypassEffect currentEffect = BypassEffect.None;
    internal List<Vector2> particles = new List<Vector2>();
    public float[] particleSpeeds = new float[3] { 6f, 12f, 20f };

    List<Entity> entitiesInsideZone = new List<Entity>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerBypassZone(EntityData data, Vector2 offset, EntityID id)
        : base(data.Position + offset)
    {
        Depth = 9500;
        entityID = id;

        mainEffect = ConvertToBypassEffect(data.Attr("effect"));
        altEffect = ConvertToBypassEffect(data.Attr("altEffect"));
        altFlag = data.Attr("altFlag", "");

        attachable = data.Bool("attachable", true);
        bypassFlag = data.Attr("bypassFlag", "");
        affectPlayer = data.Bool("affectPlayer", true);

        String mainTexturePath = Utils_General.TrimPath(
            data.Attr("mainTexturePath"), "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/empty"
        );
        Image mainTextureImage = new Image(GFX.Game[mainTexturePath]);
        String borderTexturePath = Utils_General.TrimPath(
            data.Attr("borderTexturePath"), "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/mural_border_none"
        );
        borderTexture = GFX.Game[borderTexturePath];

        if (mainTexturePath == "objects/EndHelper/DeathHandlerBypassZone/empty" 
            && borderTexturePath == "objects/EndHelper/DeathHandlerBypassZone/mural_border_none")
        {
            Visible = false;
        }

        Add(mainTextureImage);
        width = data.Width;
        height = data.Height;
        base.Collider = new Hitbox(x: 1, y: 1, width: width - 2, height: height - 2);

        // Cool effects!!!
        for (int i = 0; (float)i < width * height / 12f; i++)
        {
            particles.Add(new Vector2(Calc.Random.NextFloat(width - 1f), Calc.Random.NextFloat(height - 1f)));
        }

        // Static Mover stuff
        if (attachable)
        {
            Add(new StaticMover
            {
                OnShake = OnShake,
                SolidChecker = IsRidingSolid
            });
        }

        GoldenRipple.enableShader = true;
    }

    private void OnShake(Vector2 shakePos)
    {
        Position += shakePos;
    }

    private bool IsRidingSolid(Solid solid)
    {
        Collider origCollider = base.Collider;
        base.Collider = new Hitbox(Width + 4, Height + 4, -1, -1);
        bool collideCheck = CollideCheck(solid);
        base.Collider = origCollider;
        return collideCheck;
    }

    private static BypassEffect ConvertToBypassEffect(String effectString)
    {
        switch (effectString)
        {
            case "Activate": return BypassEffect.Activate;
            case "Deactivate": return BypassEffect.Deactivate;
            case "Toggle": return BypassEffect.Toggle;
            case "None": return BypassEffect.None;
            default: return BypassEffect.None;
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
    }
    public override void Awake(Scene scene)
    {
        UpdateCurrentEffect();
        base.Awake(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        UpdateCurrentEffect();
        UpdateDeathBypass();
        base.Update();

        // Update particles
        int i = 0;
        for (int count = particles.Count; i < count; i++)
        {
            Vector2 direction = Vector2.Zero;

            switch (currentEffect)
            {
                case BypassEffect.Activate:
                    direction = new Vector2(0f, -1f);
                    break;
                case BypassEffect.Deactivate:
                    direction = new Vector2(0f, 1f);
                    break;
                case BypassEffect.Toggle:
                    direction = i % 2 == 1 ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
                    break;
                default:
                    break;
            }

            if (direction != Vector2.Zero)
            {
                direction.Normalize();

                Vector2 value = particles[i] + direction * particleSpeeds[i % particleSpeeds.Length] * Engine.DeltaTime;

                if (value.X < 0) value.X += width - 1;
                if (value.Y < 0) value.Y += height - 1;
                value.Y %= height - 1;
                value.X %= width - 1;
                particles[i] = value;
            }
        }
    }

    private void UpdateCurrentEffect()
    {
        Level level = SceneAs<Level>();
        bool useAlt = Utils_General.AreFlagsEnabled(level.Session, altFlag, false);
        currentEffect = useAlt ? altEffect : mainEffect;
    }

    private void UpdateDeathBypass()
    {
        Level level = SceneAs<Level>();
        foreach (Entity entity in level.Entities)
        {
            if (DeathHandlerDeathBypassTrigger.FilterEntity(entity, affectPlayer, false) && entity is not DeathHandlerBypassZone)
            {
                bool isCurrentlyInside = CollideCheck(entity);
                if (entity is Booster booster && booster.BoostingPlayer)
                {
                    Player player = base.Scene.Tracker.GetEntity<Player>();
                    isCurrentlyInside = CollideCheck(player);
                }

                bool wasInside = entitiesInsideZone.Contains(entity);

                if (isCurrentlyInside && !wasInside) UpdateZoneOnEnter(entity);
                if (!isCurrentlyInside && wasInside) UpdateZoneOnExit(entity);
            }
        }
    }

    private void UpdateDeathBypassEntity(Entity entity)
    {
        // Check for existing deathbypass
        if (entity.Components.Get<DeathBypass>() is null)
        {
            // No deathbypass. Add one.
            entity.Add(new DeathBypass(bypassFlag, true, initialAllowBypass: false));
        }
    }

    private void UpdateZoneOnEnter(Entity entity)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerBypassZone", $"Entity entered zone: {entity} {entity.SourceId}");
        UpdateDeathBypassEntity(entity); // Set death bypass if not already set

        if (entity.Components.Get<DeathBypass>() is DeathBypass deathbypass)
        {
            entitiesInsideZone.Add(entity);
            switch (currentEffect)
            {
                case BypassEffect.Activate:
                    deathbypass.ToggleAllowBypass(entity, true, newRequireFlag: bypassFlag);
                    break;
                case BypassEffect.Deactivate:
                    deathbypass.ToggleAllowBypass(entity, false, newRequireFlag: bypassFlag);
                    break;
                case BypassEffect.Toggle:
                    deathbypass.ToggleAllowBypass(entity, null, newRequireFlag: bypassFlag);
                    break;
                default:
                    break;
            }
        }
    }
    private void UpdateZoneOnExit(Entity entity)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerBypassZone", $"Entity exited zone: {entity} {entity.SourceID}");
        entitiesInsideZone.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        // Main
        base.Render();

        Level level = SceneAs<Level>();
        if (level.Camera.GetRect().Intersects(this.HitRect()))
        {
            // Glow
            float sineMultiplier = (float)Math.Sin(Engine.Scene.TimeActive * 4);
            float sineMultiplierFast = (float)Math.Sin(Engine.Scene.TimeActive * 17);
            Color color;
            switch (currentEffect)
            {
                case BypassEffect.Activate:
                    color = new Color(256, 249, 139); // Gold
                    Draw.Rect(base.Collider, color * (0.3f + sineMultiplier * 0.05f));
                    RenderParticles(color, 0.08f, 0.04f);
                    break;
                case BypassEffect.Deactivate:
                    color = new Color(0, 51, 102); // Dark Blue
                    Draw.Rect(base.Collider, color * (0.3f + sineMultiplier * 0.05f));
                    RenderParticles(color, 0.08f, 0.04f);
                    break;
                case BypassEffect.Toggle:
                    color = Color.Red;
                    Draw.Rect(base.Collider, color * (0.2f + sineMultiplier * 0.03f));
                    RenderParticles(color, 0.08f, 0.04f);
                    break;
                case BypassEffect.None:
                    break;
                default:
                    break;
            }

            // Border
            borderTexture.Render9Slice(width, height, Position);
        }
    }

    private void RenderParticles(Color color, float baseOpacity, float deltaOpacity)
    {
        // Make particles easier to see if deathbypass
        if (Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass && deathBypassComponent.showVisuals)
        {
            baseOpacity *= 2;
            deltaOpacity *= 2;
        }

        // Particle rendering
        int i = 0;
        foreach (Vector2 particle in particles)
        {

            float distanceOpacityFactor = 0;

            if (currentEffect == BypassEffect.Activate)
            {
                float distanceFromCenter = (particle - new Vector2(width / 2, height / 2)).Length();
                distanceOpacityFactor = (float)Math.Sin(distanceFromCenter / 7 - Engine.Scene.TimeActive * 4);
                distanceOpacityFactor = ((distanceOpacityFactor / 2) + 1) * 0.2f - 0.1f;
            }
            else if (currentEffect == BypassEffect.Deactivate)
            {
                float distanceFromCenter = (particle - new Vector2(width / 2, height / 2)).Length();
                distanceOpacityFactor = (float)Math.Sin(distanceFromCenter / 7 + Engine.Scene.TimeActive * 4);
                distanceOpacityFactor = ((distanceOpacityFactor / 2) + 1) * 0.2f - 0.1f;
            }

            i++;
            float sineMultiplierFast = (float)Math.Sin(Engine.Scene.TimeActive * 7 + i);
            Draw.Pixel.Draw(Position + particle, Vector2.Zero, color * (baseOpacity + sineMultiplierFast * deltaOpacity + distanceOpacityFactor));
        }
    }
}