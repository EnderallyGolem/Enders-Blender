﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Utils
{
    static internal class Utils_Shaders
    {
        public static bool loadedShaders = false;
        public static Effect FxGoldenRipple;
        public static Effect FxRespawnRipple;
        public static RenderTarget2D tempRender;

        internal static void LoadCustomShaders(bool forceReload = false)
        {
            if (!loadedShaders || forceReload)
            {
                tempRender = new RenderTarget2D(
                    Engine.Graphics.GraphicsDevice,
                    width: 320,
                    height: 180,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Color,
                    preferredDepthFormat: DepthFormat.Depth24Stencil8,
                    preferredMultiSampleCount: 0,
                    usage: RenderTargetUsage.DiscardContents
                );
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"Loading custom shaders.");
                FxGoldenRipple = LoadFxEndHelper("goldenRipple"); GoldenRipple.ResetRipples();
                FxRespawnRipple = LoadFxEndHelper("respawnRipple");
            }
            loadedShaders = true;
        }

        public static Effect LoadFxEndHelper(string shaderName)
        {
            ModAsset shaderAsset = Everest.Content.Get($"Effects/EndHelper/{shaderName}.cso");
            Effect effect = new Effect(Engine.Graphics.GraphicsDevice, shaderAsset.Data);
            return effect;
        }

        public static void ApplyShaders(Level level)
        {
            // Utils_Shaders kept having the tendancy to reset upon rebuild... so this is the nice simple safe solution!
            if (!loadedShaders) { LoadCustomShaders(); }

            // These apply each time Glitch is updated. Which is to say, every frame.
            if (GoldenRipple.enableShader)
            {
                GoldenRipple.Apply(GameplayBuffers.Level, level);
            }
        }
    }

    static internal class GoldenRipple
    {
        internal static bool enableShader = false; // Enabled by DeathBypass component

        // Note: This renders on top of the existing texture, so transparency doesn't work
        struct Ripple
        {
            public Vector2 OriginPosition;
            public float StartTime;
        }
        static List<Ripple> rippleList = new();
        static float timeSinceLastRipple = 0f;
        static readonly Random rnd = new Random();

        const float waveSpeed = 0.6f;
        const float waveStrength = 0.7f;
        const float fadeOutTime = 3f;
        const int maxRippleCount = 15; // For shader badly made garbage reasons: This should be MAX 20
        const int inverseFractionSpawnChance = 15; // Eg: 10 means 1/10 chance of spawning a ripple per frame

        internal static void ResetRipples()
        {
            rippleList.Clear();
            timeSinceLastRipple = 0;
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"GoldenRipple - Resetted Ripples!");
        }

        private static void UpdateRipples(Level level)
        {
            timeSinceLastRipple += Engine.DeltaTime;
            Camera camera = level.Camera;

            // Remove ripples that reach fadeOutTime
            for (int i = 0; i < rippleList.Count; i++)
            {
                if (rippleList[i].StartTime + fadeOutTime < Engine.Scene.TimeActive)
                {
                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"Removed ripple with start time {rippleList[i].StartTime}. Active time is {Engine.Scene.TimeActive}");
                    rippleList.RemoveAt(i);
                    i--;
                }
            }

            // Roll for a new ripple
            if (rippleList.Count < maxRippleCount)
            {
                // Roll for a chance to spawn another ripple
                if (rnd.Range(0, inverseFractionSpawnChance) >= inverseFractionSpawnChance - 1)
                {
                    // Spawn a new ripple
                    Vector2 rippleSpawnPos = new Vector2(rnd.Range(camera.Left - 200, camera.Right + 200), rnd.Range(camera.Top - 200, camera.Bottom + 200));
                    float rippleStartTime = Engine.Scene.TimeActive;
                    rippleList.Add(
                        new Ripple
                        {
                            OriginPosition = rippleSpawnPos,
                            StartTime = rippleStartTime
                        }
                    );
                    timeSinceLastRipple = 0;
                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"GoldenRipple - Spawned a ripple at {rippleSpawnPos} with StartTime {rippleStartTime}. {rippleList.Count} / {maxRippleCount}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Apply(VirtualRenderTarget sourceTarget, Level level)
        {
            UpdateRipples(level);
            Effect effect = Utils_Shaders.FxGoldenRipple;

            // Generic Parameters
            effect.Parameters["Time"]?.SetValue(Engine.Scene.TimeActive);
            effect.Parameters["Dimensions"]?.SetValue(new Vector2(GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height));
            effect.Parameters["CamPos"]?.SetValue(level.Camera.Position);
            Viewport vp = Engine.Graphics.GraphicsDevice.Viewport;
            effect.Parameters["TransformMatrix"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1));
            effect.Parameters["ViewMatrix"]?.SetValue(Matrix.Identity);

            // Special Parameters
            effect.Parameters["TimeTransitionRoom"]?.SetValue(Utils_General.timeSinceEnteredRoom / 60);
            effect.Parameters["WaveSpeed"]?.SetValue(waveSpeed);
            effect.Parameters["WaveStrength"]?.SetValue(waveStrength);
            effect.Parameters["FadeOutTime"]?.SetValue(fadeOutTime);
            Vector3[] rippleData = new Vector3[rippleList.Count];
            for (int i = 0; i < rippleList.Count; i++)
            {
                rippleData[i] = new Vector3(rippleList[i].OriginPosition.X, rippleList[i].OriginPosition.Y, rippleList[i].StartTime);
            }
            effect.Parameters["RippleData"]?.SetValue(rippleData);
            effect.Parameters["RippleNumber"]?.SetValue(rippleData.Length);

            // Temporarily render on tempRender
            Engine.Instance.GraphicsDevice.SetRenderTarget(Utils_Shaders.tempRender);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
            // Instead of drawing everything (Draw.SpriteBatch.Draw((RenderTarget2D)source, Vector2.Zero, Color.White);)
            foreach (Entity entity in level.Entities)
            {
                if (entity.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass && deathBypassComponent.showVisuals)
                {
                    if (entity.Visible)
                    {
                        entity.Render();
                    }
                }
            }
            Draw.SpriteBatch.End();

            // Blend that tempRender onto sourceTarget (GameplayBuffers.Level)
            Engine.Instance.GraphicsDevice.SetRenderTarget(sourceTarget);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, effect);
            Draw.SpriteBatch.Draw(Utils_Shaders.tempRender, Vector2.Zero, Color.White);
            Draw.SpriteBatch.End();
        }
    }

    static internal class RespawnRipple
    {
        internal static bool enableShader = false; // Enabled when BeginEntityRender is called

        const float waveSpeed = 0.16f;
        const float waveStrength = 1.5f;
        const float fadeOutTime = 2f;
        const float rippleCycleTime = 2.4f;
        const int distanceBetweenRipple = 25;

        static float rippleCycleCurrentTime = 0;
        static RenderTargetBinding[] renderTargets;
        static Color rippleColour;
        static Color outlineColour;

        // Run BeginEntityRender and EndEntityRender at the start and end of the entity render
        internal static void BeginEntityRender(Level level, Color setRippleColour, Color setOutlineColour)
        {
            enableShader = true;
            rippleColour = setRippleColour;
            outlineColour = setOutlineColour;

            Draw.SpriteBatch.End();
            renderTargets = Engine.Instance.GraphicsDevice.GetRenderTargets();

            // Temporarily render on tempRender
            Engine.Instance.GraphicsDevice.SetRenderTarget(Utils_Shaders.tempRender);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            Apply(level);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, level.Camera.matrix);
        }

        internal static void EndEntityRender(Level level)
        {
            Draw.SpriteBatch.End();

            Engine.Instance.GraphicsDevice.SetRenderTargets(renderTargets);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, Utils_Shaders.FxRespawnRipple);
            Draw.SpriteBatch.Draw(Utils_Shaders.tempRender, Vector2.Zero, Color.White);
            Draw.SpriteBatch.End();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, level.Camera.Matrix);
        }

        // Called by Hook_LevelUpdate
        internal static void UpdateRipples(Level level)
        {
            if (!level.FrozenOrPaused)
            {
                rippleCycleCurrentTime += Engine.DeltaTime;
                if (rippleCycleCurrentTime > rippleCycleTime)
                {
                    rippleCycleCurrentTime -= rippleCycleTime;
                }
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"RespawnRipple: Cycle time is {rippleCycleCurrentTime} / {rippleCycleTime}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Apply(Level level)
        {
            Effect effect = Utils_Shaders.FxRespawnRipple;

            // Generic Parameters
            effect.Parameters["Time"]?.SetValue(Engine.Scene.TimeActive);
            effect.Parameters["Dimensions"]?.SetValue(new Vector2(GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height));
            effect.Parameters["CamPos"]?.SetValue(level.Camera.Position);
            Viewport vp = Engine.Graphics.GraphicsDevice.Viewport;
            effect.Parameters["TransformMatrix"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1));
            effect.Parameters["ViewMatrix"]?.SetValue(Matrix.Identity);

            // Special Parameters
            Vector4 rippleColourFloat4Normalisation = new Vector4((float)rippleColour.R / 256, (float)rippleColour.G / 256, (float)rippleColour.B / 256, (float)(1 - rippleColour.A) / 256);
            Vector4 outlineColourFloat4Normalisation = new Vector4((float)outlineColour.R / 256, (float)outlineColour.G / 256, (float)outlineColour.B / 256, (float)(1 - outlineColour.A) / 256);
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_Shaders", $"RespawnRipple: Normalised Ripple float4: {rippleColourFloat4Normalisation}, Normalised outline float4: {outlineColourFloat4Normalisation}");

            effect.Parameters["TimeTransitionRoom"]?.SetValue(Utils_General.timeSinceEnteredRoom / 60);
            effect.Parameters["WaveSpeed"]?.SetValue(waveSpeed);
            effect.Parameters["WaveStrength"]?.SetValue(waveStrength);
            effect.Parameters["FadeOutTime"]?.SetValue(fadeOutTime);
            effect.Parameters["RippleTime"]?.SetValue(rippleCycleCurrentTime);
            effect.Parameters["RippleTimeMax"]?.SetValue(rippleCycleTime);
            effect.Parameters["DistanceBetweenRipple"]?.SetValue(distanceBetweenRipple);
            effect.Parameters["RippleColour"]?.SetValue(rippleColourFloat4Normalisation);
            effect.Parameters["OutlineColour"]?.SetValue(outlineColourFloat4Normalisation);
        }
    }
}
