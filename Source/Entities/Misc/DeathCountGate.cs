using Celeste.Mod.EndHelper.Entities.DeathHandler;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EndHelper.Entities.Misc
{
    [CustomEntity("EndHelper/DeathCountGate")]
    [Tracked(false)]
    [TrackedAs(typeof(TempleGate))]
    public class DeathCountGate : Solid
    {
        public bool theoGate;
        public int closedHeight;
        public Sprite sprite;
        public Shaker shaker;
        public float drawHeight;
        public float drawHeightMoveSpeed;
        public bool lockState;
        public bool Opened { get; private set; }

        public int ReferenceDeathCount { get; private set; } = 0;
        private readonly int deathLimit;
        private readonly String setFlag = "";
        private readonly bool silent = false;

        private enum ComparisonTypes { Below, BelowOrEqual, Equal, AboveOrEqual, Above }
        private readonly ComparisonTypes comparison;
        private String comparisionStr = "=";
        private enum DeathCountType { Map, RoomPermanent, RoomFullreset, RoomTransition, RoomTransitionRetry }
        private readonly DeathCountType deathCountType;

        internal readonly EntityID entityID;
        private MarkerHUD hud;


        public DeathCountGate(EntityData data, Vector2 offset, EntityID entityID)
            : base(data.Position + offset, 9f, data.Height, safe: true)
        {
            String spriteName = data.Attr("sprite", "objects/door/TempleDoor00");
            deathLimit = data.Int("deathLimit", 10);
            setFlag = data.Attr("setFlag", "");
            silent = data.Bool("silent", false);

            comparison = GetComparisonType(data.Attr("comparison"));
            deathCountType = GetDeathCountType(data.Attr("deathCountType"));

            closedHeight = data.Height;
            Add(sprite = GFX.SpriteBank.Create("templegate_" + spriteName));
            sprite.X = base.Collider.Width / 2f;
            sprite.Play("idle");
            Add(shaker = new Shaker(on: false));
            base.Depth = -9000;
            theoGate = spriteName.Equals("theo", StringComparison.InvariantCultureIgnoreCase);
            this.entityID = entityID;

            //Logger.Log(LogLevel.Info, "EndHelper/DeathCountGate", $"anything. freaking. happening. ?. err sprite is {sprite.Path} .... {spriteName} .... {base.Collider.Width} {base.Collider.Height} {closedHeight}");
        }

        private ComparisonTypes GetComparisonType(String comparisonStr)
        {
            switch (comparisonStr)
            {
                case "below":
                    comparisionStr = "<";
                    return ComparisonTypes.Below;
                case "below_or_equal":
                    comparisionStr = "≤";
                    return ComparisonTypes.BelowOrEqual;
                case "equal":
                    comparisionStr = "=";
                    return ComparisonTypes.Equal;
                case "above_or_equal":
                    comparisionStr = "≥";
                    return ComparisonTypes.AboveOrEqual;
                case "above":
                    comparisionStr = ">";
                    return ComparisonTypes.Above;
                default:
                    comparisionStr = "<";
                    return ComparisonTypes.BelowOrEqual;
            }
        }
        private static DeathCountType GetDeathCountType(String deathCountTypeStr)
        {
            switch (deathCountTypeStr)
            {
                case "map":
                    return DeathCountType.Map;
                case "room_permanent":
                    return DeathCountType.RoomPermanent;
                case "room_fullreset":
                    return DeathCountType.RoomFullreset;
                case "room_transition":
                    return DeathCountType.RoomTransition;
                case "room_transition_retry":
                    return DeathCountType.RoomTransitionRetry;
                default:
                    return DeathCountType.RoomPermanent;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Awake(Scene scene)
        {
            Level level = scene as Level;
            if (level.Tracker.GetNearestEntityExcluding<DeathCountGate>(Position, this) is DeathCountGate deathCountGate && deathCountGate.entityID.Equals(this.entityID))
            {
                RemoveSelf();
                return;
            }

            AddTag(Tags.Global);
            level.Add(hud = new MarkerHUD(this));

            if (deathCountType == DeathCountType.Map)
            {
                ReferenceDeathCount = level.Session.Deaths; // Otherwise uses default of 0
            }

            base.Awake(scene);
            if (DoorOpenCheck()) StartOpen();
            drawHeight = Math.Max(4f, base.Height);
        }

        public bool DoorOpenCheck()
        {
            switch (comparison)
            {
                case ComparisonTypes.Below:
                    return ReferenceDeathCount < deathLimit;
                case ComparisonTypes.BelowOrEqual:
                    return ReferenceDeathCount <= deathLimit;
                case ComparisonTypes.Equal:
                    return ReferenceDeathCount == deathLimit;
                case ComparisonTypes.AboveOrEqual:
                    return ReferenceDeathCount >= deathLimit;
                case ComparisonTypes.Above:
                    return ReferenceDeathCount > deathLimit;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CloseBehindPlayerCheck()
        {
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (entity != null)
            {
                return entity.X < base.X;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SwitchOpen()
        {
            sprite.Play("open");
            Alarm.Set(this, 0.2f, [MethodImpl(MethodImplOptions.NoInlining)] () =>
            {
                shaker.ShakeFor(0.2f, removeOnFinish: false);
                Alarm.Set(this, 0.2f, Open);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Open()
        {
            if (Opened) return;

            if (!silent) Audio.Play(theoGate ? "event:/game/05_mirror_temple/gate_theo_open" : "event:/game/05_mirror_temple/gate_main_open");

            drawHeightMoveSpeed = 200f;
            drawHeight = base.Height;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(0);
            sprite.Play("open");
            Opened = true;

            if (setFlag != "") SceneAs<Level>().Session.SetFlag(setFlag, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StartOpen()
        {
            SetHeight(0);
            drawHeight = 4f;
            Opened = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Close()
        {
            if (!Opened) return;
            if (!silent) Audio.Play(theoGate ? "event:/game/05_mirror_temple/gate_theo_close" : "event:/game/05_mirror_temple/gate_main_close");
            
            drawHeightMoveSpeed = 300f;
            drawHeight = Math.Max(4f, base.Height);
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(closedHeight);
            sprite.Play("hit");
            Opened = false;

            if (setFlag != "") SceneAs<Level>().Session.SetFlag(setFlag, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetHeight(int height)
        {
            if ((float)height < base.Collider.Height)
            {
                base.Collider.Height = height;
                return;
            }

            float y = base.Y;
            int num = (int)base.Collider.Height;
            if (base.Collider.Height < 64f)
            {
                base.Y -= 64f - base.Collider.Height;
                base.Collider.Height = 64f;
            }

            MoveVExact(height - num);
            base.Y = y;
            base.Collider.Height = height;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            Level level = SceneAs<Level>();

            if (!lockState)
            {
                if (DoorOpenCheck())
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }

            float num = Math.Max(4f, base.Height);
            if (drawHeight != num)
            {
                lockState = true;
                drawHeight = Calc.Approach(drawHeight, num, drawHeightMoveSpeed * Engine.DeltaTime);
            }
            else
            {
                lockState = false;
            }

            base.Update();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Render()
        {
            Vector2 vector = new Vector2(Math.Sign(shaker.Value.X), 0f);
            Draw.Rect(base.X - 2f, base.Y - 8f, 14f, 10f, Color.Black);
            sprite.DrawSubrect(Vector2.Zero + vector, new Rectangle(0, (int)(sprite.Height - drawHeight), (int)sprite.Width, (int)drawHeight));
        }

        private class MarkerHUD : Entity
        {
            private DeathCountGate p;
            private Vector2 renderPos;
            private float opacity;
            
            internal MarkerHUD(DeathCountGate parent)
            {
                Depth = -1;
                p = parent;
                AddTag(TagsExt.SubHUD);
                AddTag(Tags.Global);
            }

            public override void Update()
            {
                base.Update();
            }

            public override void Render()
            {
                Level level = SceneAs<Level>();
                renderPos = level.WorldToScreen(p.TopCenter);
                if (level.Transitioning)
                {
                    if (opacity > 0)
                    {
                        opacity -= 0.1f;
                    }
                }
                else if (opacity < 1)
                {
                    opacity += 0.1f;
                }
                if (opacity > 0)
                {
                    Color colour = p.Opened ? Color.White : Color.Red;
                    String emote = colour == Color.White ? ":EndHelper/uioutline_skull:" : ":EndHelper/uioutline_skull_red:";
                    ActiveFont.DrawOutline($"{emote}{p.ReferenceDeathCount} {p.comparisionStr} {p.deathLimit}", renderPos, new Vector2(0.5f, 0.5f), Vector2.One * 0.7f, colour * opacity, 2f, Color.Black);
                }
                base.Render();
            }
        }

        static internal void OnPlayerDeathStatic(Level level)
        {
            foreach (DeathCountGate deathCountGate in level.Tracker.GetEntities<DeathCountGate>())
            {
                deathCountGate.OnPlayerDeath(level);
            }
        }

        internal void OnPlayerDeath(Level level)
        {
            switch (deathCountType)
            {
                case DeathCountType.Map:
                    Logger.Log(LogLevel.Info, "EndHelper/DeathCountGate", $"DEATHS {level.Session.Deaths}");
                    ReferenceDeathCount = level.Session.Deaths;
                    break;
                case DeathCountType.RoomPermanent:
                    if (CheckSameRoomAsPlayer()) ReferenceDeathCount++;                    
                    break;
                case DeathCountType.RoomFullreset:
                    if (CheckSameRoomAsPlayer()) ReferenceDeathCount++;
                    if (EndHelperModule.Session.AllowDeathHandlerEntityChecks && EndHelperModule.Session.nextRespawnFullReset)
                    {
                        ReferenceDeathCount = 0;
                    }
                    break;
                case DeathCountType.RoomTransition:
                    if (CheckSameRoomAsPlayer()) ReferenceDeathCount++;
                    break;
                case DeathCountType.RoomTransitionRetry:
                    if (CheckSameRoomAsPlayer()) ReferenceDeathCount++;
                    if (Utils_DeathHandler.manualReset) ReferenceDeathCount = 0;
                    break;
                default:
                    break;
            }
        }

        static internal void OnTransitionStatic(Level level)
        {
            // Transition Listeners didn't work =/
            foreach (DeathCountGate deathCountGate in level.Tracker.GetEntities<DeathCountGate>())
            {
                deathCountGate.OnTransition(level);
            }
        }

        internal void OnTransition(Level level)
        {
            if (CheckSameRoomAsPlayer())
            {
                Active = true; Visible = true; Collidable = true; hud.Active = true; hud.Visible = true;
            }
            else
            {
                Active = false; Visible = true; Collidable = false; hud.Active = false; hud.Visible = false;
            }

            if (base.Scene.Tracker.GetEntity<Player>() is Player player && Vector2.Distance(player.Position, Position) <= level.Camera.GetRect().Width * 3)
            {
                Visible = true;
            }

            if (deathCountType == DeathCountType.RoomFullreset || deathCountType == DeathCountType.RoomTransition || deathCountType == DeathCountType.RoomTransitionRetry)
            {
                ReferenceDeathCount = 0;
            }
        }

        private bool CheckSameRoomAsPlayer()
        {
            Level level = SceneAs<Level>();
            return this.SourceData.Level.Name == level.Session.LevelData.Name;
        }
    }
}