using Celeste.Mod.Entities;
using Celeste.Mod.EndHelper.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;
using static Celeste.TempleGate;
using static On.Celeste.Level;
using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using static MonoMod.InlineRT.MonoModRule;
using System.Threading.Tasks;
using System.Collections;
using static Celeste.Mod.EndHelper.EndHelperModule;
using System.Linq;
using System.Threading;

namespace Celeste.Mod.EndHelper.Entities.RoomSwap;

[CustomEntity("EndHelper/RoomSwapMapUpgrade")]
public class RoomSwapMapUpgrade : Entity
{
    private class BgFlash : Entity
    {
        private float alpha = 1f;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public BgFlash()
        {
            Depth = 10100;
            Tag = Tags.Persistent;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            base.Update();
            alpha = Calc.Approach(alpha, 0f, Engine.DeltaTime * 0.5f);
            if (alpha <= 0f)
            {
                RemoveSelf();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Render()
        {
            Vector2 position = (Scene as Level).Camera.Position;
            Draw.Rect(position.X - 10f, position.Y - 10f, 340f, 200f, Color.Black * alpha);
        }
    }

    public static ParticleType P_Shatter = new ParticleType
    {
        Source = GFX.Game["particles/triangle"],
        ColorMode = ParticleType.ColorModes.Static,
        FadeMode = ParticleType.FadeModes.Late,
        LifeMin = 0.25f,
        LifeMax = 0.4f,
        Size = 1f,
        Direction = 4.712389f,
        DirectionRange = 0.87266463f,
        SpeedMin = 140f,
        SpeedMax = 210f,
        SpeedMultiplier = 0.005f,
        RotationMode = ParticleType.RotationModes.Random,
        SpinMin = MathF.PI / 2f,
        SpinMax = 4.712389f,
        SpinFlippedChance = true
    };

    public static Color[] GemColors = new Color[6]
    {
        Calc.HexToColor("9ee9ff"),
        Calc.HexToColor("54baff"),
        Calc.HexToColor("90ff2d"),
        Calc.HexToColor("ffd300"),
        Calc.HexToColor("ff609d"),
        Calc.HexToColor("c5e1ba")
    };


    private string gridID;
    private SineWave sine;

    private EntityID entityID;

    private EntityData entityData;
    MTexture texture;


    public RoomSwapMapUpgrade(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
    {
        entityID = id;
        entityData = data;
        gridID = data.Attr("gridId", "1");
        string texturePath = data.Attr("texturePath");
        //String obtainSoundEvent = data.Attr("obtainSoundEvent", "");
        float floatAmplitude = data.Float("floatAmplitude", 0.1f);
        //int changeLevel = data.Int("changeLevel", 1);
        //bool setLevel = data.Bool("setLevel", false);
        bool oneTime = data.Bool("oneTime", true);

        Depth = 20;

        sine = new SineWave(0.5f, MathF.PI / 2);
        Add(sine);


        texturePath = trimPath(texturePath, "objects/EndHelper/RoomSwapMap/upgradeicon");
        texture = GFX.Game[texturePath];

        data.Width = texture.Width;
        data.Height = texture.Height;

        Collider = new Hitbox(data.Width, data.Height, -data.Width / 2, -data.Height / 2);
        Add(new PlayerCollider(ObtainMap));
        Add(new Image(texture).CenterOrigin());
        Add(new VertexLight(Color.White, 1f, 32, 64));
        Add(new BloomPoint(0.5f, 12f));
    }

    private void ObtainMap(Player player)
    {
        int changeLevel = entityData.Int("changeLevel", 1);
        bool setLevel = entityData.Bool("setLevel", false);

        if (setLevel)
        {
            EndHelperModule.Session.roomMapLevel[gridID] = changeLevel;
        }
        else
        {
            EndHelperModule.Session.roomMapLevel[gridID] += changeLevel;
        }

        Collidable = false;
        Visible = false;

        string obtainSoundEvent = entityData.Attr("obtainSoundEvent", "");
        if (obtainSoundEvent != "")
        {
            Audio.Play(obtainSoundEvent, Position);
        }

        Level level = Scene as Level;
        Add(new Coroutine(SmashRoutine(player, level)));
    }

    private IEnumerator SmashRoutine(Player player, Level level)
    {
        bool oneTime = entityData.Bool("oneTime", true);
        if (oneTime)
        {
            level.Session.DoNotLoad.Add(entityID);
        }

        Visible = false;
        Collidable = false;

        Vector2 effectPos = Position;

        player.Stamina = 110f;
        Session session = (Scene as Level).Session;
        level.Shake();
        Celeste.Freeze(0.1f);
        float num = player.Speed.Angle();
        level.ParticlesFG.Emit(P_Shatter, 3, effectPos, Vector2.One * 4f, num - MathF.PI / 2f);
        level.ParticlesFG.Emit(P_Shatter, 3, effectPos, Vector2.One * 4f, num + MathF.PI / 2f);
        SlashFx.Burst(effectPos, num);
        level.Flash(new Color(30, 30, 30, 10), drawPlayerOver: true);


        RoomSwapMap transitionMap = level.Tracker.GetNearestEntity<RoomSwapMap>(Position);
        if (transitionMap != null)
        {
            for (int i = 0; i < 7; i++)
            {
                Scene.Add(new AbsorbOrb(effectPos, transitionMap, transitionMap.Position));
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Scene.Add(new AbsorbOrb(effectPos, player));
            }
        }

        //level.Flash(Color.White, drawPlayerOver: true);
        //Scene.Add(new BgFlash());
        Engine.TimeRate = 0.5f;
        while (Engine.TimeRate < 1f)
        {
            Engine.TimeRate += Engine.RawDeltaTime * 0.5f;
            yield return null;
        }

        int waitFrames = 20;
        while (waitFrames > 0)
        {
            Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionMapUpgrade", $"ummm wait {waitFrames}");
            waitFrames--;
            yield return null;
        }
        level.Flash(new Color(200, 200, 200, 200), drawPlayerOver: true);
        Scene.Add(new BgFlash());
        RoomModificationEventTrigger(gridID);

        RemoveSelf();
    }

    public override void Update()
    {
        sine.Rate = MathHelper.Lerp(0.7f, 0.3f, 0.3f);

        Position.Y += sine.Value * entityData.Float("floatAmplitude", 0.1f);
        base.Update();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
    }


    private string trimPath(string path, string defaultPath)
    {
        if (path == "") { path = defaultPath; }
        while (path.StartsWith("objects") == false)
        {
            path = path.Substring(path.IndexOf('/') + 1);
        }
        if (path.IndexOf(".") > -1)
        {
            path = path.Substring(0, path.IndexOf("."));
        }
        return path;
    }
}