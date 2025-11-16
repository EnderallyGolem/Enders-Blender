using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EndHelper.Entities.Misc
{
    [CustomEntity("EndHelper/ConnectableOutline")]
    [Tracked(false)]
    public class ConnectableOutline : Entity
    {
        public List<ConnectableOutline> group;
        public List<Image> imageList = [];
        public bool groupLeader = false;
        public Vector2 groupOrigin;
        public Wiggler wiggler;
        public Vector2 wigglerScaler;

        private readonly string visibleFlag;
        private readonly Color colour;
        private readonly int connectLayer;
        private readonly bool attachable;
        private readonly string folderPath;
        private readonly Vector2 nodeOffset = Vector2.Zero;

        private bool flagAllow = true;
        private bool previousFlagAllow = true;
        private float opacity = 1f;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ConnectableOutline(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            this.colour = Calc.HexToColorWithAlpha(data.Attr("colour", "ffffffff"));
            visibleFlag = data.Attr("visibleFlag", "");
            connectLayer = data.Int("connectLayer", 0);
            attachable = data.Bool("attachable", true);
            folderPath = Utils_General.TrimPath(data.Attr("texturePath"), "objects/EndHelper/Misc/outline_filled");

            Depth = data.Int("depth", 10000);
            Collider = new Hitbox(data.Width, data.Height);

            if (data.Nodes.Length > 0) nodeOffset = data.Nodes[0] + offset - Position;

            if (attachable)
            {
                Add(new StaticMover
                {
                    OnShake = OffsetImage,
                    SolidChecker = IsRidingSolid,
                    OnEnable = Nothing,
                    OnDisable = Nothing,
                    Visible = true,
                });
            }
        }

        private void Nothing()
        {

        }

        private bool IsRidingSolid(Solid solid)
        {
            Collider origCollider = base.Collider;
            base.Collider = new Hitbox(Width + 2, Height + 2, -1 + nodeOffset.X, -1 + nodeOffset.Y);
            bool collideCheck = CollideCheck(solid);
            base.Collider = origCollider;
            return collideCheck;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            if (group == null)
            {
                groupLeader = true;
                group = new List<ConnectableOutline>();
                group.Add(this);
                FindInGroup(this);
                float num = float.MaxValue;
                float num2 = float.MinValue;
                float num3 = float.MaxValue;
                float num4 = float.MinValue;
                foreach (ConnectableOutline item in group)
                {
                    if (item.Left < num) num = item.Left;
                    if (item.Right > num2) num2 = item.Right;
                    if (item.Bottom > num4) num4 = item.Bottom;
                    if (item.Top < num3) num3 = item.Top;
                }

                groupOrigin = new Vector2((int)(num + (num2 - num) / 2f), (int)num4);
                wigglerScaler = new Vector2(Calc.ClampedMap(num2 - num, 32f, 96f, 1f, 0.2f), Calc.ClampedMap(num4 - num3, 32f, 96f, 1f, 0.2f));
                Add(wiggler = Wiggler.Create(0.3f, 3f));
                foreach (ConnectableOutline item2 in group)
                {
                    item2.wiggler = wiggler;
                    item2.wigglerScaler = wigglerScaler;
                    item2.groupOrigin = groupOrigin;
                }
            }

            for (float num5 = base.Left; num5 < base.Right; num5 += 8f)
            {
                for (float num6 = base.Top; num6 < base.Bottom; num6 += 8f)
                {
                    bool flag = CheckForSame(num5 - 8f, num6);
                    bool flag2 = CheckForSame(num5 + 8f, num6);
                    bool flag3 = CheckForSame(num5, num6 - 8f);
                    bool flag4 = CheckForSame(num5, num6 + 8f);
                    if (flag && flag2 && flag3 && flag4)
                    {
                        if (!CheckForSame(num5 + 8f, num6 - 8f))
                        {
                            SetImage(num5, num6, 3, 0);
                        }
                        else if (!CheckForSame(num5 - 8f, num6 - 8f))
                        {
                            SetImage(num5, num6, 3, 1);
                        }
                        else if (!CheckForSame(num5 + 8f, num6 + 8f))
                        {
                            SetImage(num5, num6, 3, 2);
                        }
                        else if (!CheckForSame(num5 - 8f, num6 + 8f))
                        {
                            SetImage(num5, num6, 3, 3);
                        }
                        else
                        {
                            SetImage(num5, num6, 1, 1);
                        }
                    }
                    else if (flag && flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 1, 0);
                    }
                    else if (flag && flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 1, 2);
                    }
                    else if (flag && !flag2 && flag3 && flag4)
                    {
                        SetImage(num5, num6, 2, 1);
                    }
                    else if (!flag && flag2 && flag3 && flag4)
                    {
                        SetImage(num5, num6, 0, 1);
                    }
                    else if (flag && !flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 2, 0);
                    }
                    else if (!flag && flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 0, 0);
                    }
                    else if (flag && !flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 2, 2);
                    }
                    else if (!flag && flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 0, 2);
                    }
                }
            }
        }

        public void FindInGroup(ConnectableOutline block)
        {
            foreach (ConnectableOutline entity in base.Scene.Tracker.GetEntities<ConnectableOutline>())
            {
                if (entity != this && entity != block && entity.connectLayer == connectLayer && entity.connectLayer != -1
                    && (entity.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) || entity.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) && !group.Contains(entity))
                {
                    group.Add(entity);
                    FindInGroup(entity);
                    entity.group = group;
                }
            }
        }

        public bool CheckForSame(float x, float y)
        {
            foreach (ConnectableOutline entity in base.Scene.Tracker.GetEntities<ConnectableOutline>())
            {
                if (entity.connectLayer == connectLayer && entity.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8)))
                {
                    return true;
                }
            }

            return false;
        }
        public void SetImage(float x, float y, int tx, int ty)
        {
            MTexture mtexture = GFX.Game[folderPath];
            imageList.Add(CreateImage(x, y, tx, ty, mtexture));
        }

        public Image CreateImage(float x, float y, int tx, int ty, MTexture tex)
        {
            Vector2 vector = new Vector2(x - base.X, y - base.Y);
            Image image = new Image(tex.GetSubtexture(tx * 8, ty * 8, 8, 8));
            Vector2 vector2 = groupOrigin - Position;
            image.Origin = vector2 - vector;
            image.Position = vector2;
            image.Color = new Color(colour.R, colour.G, colour.B);
            image.Color *= (float)colour.A / 255;
            Add(image);
            return image;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            Level level = SceneAs<Level>();
            previousFlagAllow = flagAllow;
            flagAllow = Utils_General.AreFlagsEnabled(level.Session, visibleFlag, true);
            //Logger.Log(LogLevel.Info, "EndHelper/ConnectableOutline", $"flag allow: {flagAllow} -- {visibleFlag}");

            if ( (flagAllow != previousFlagAllow) || (opacity != 0 && opacity != 1) )
            {
                if (flagAllow && opacity < 1)
                {
                    opacity += 0.1f;
                }
                if (!flagAllow && opacity > 0)
                {
                    opacity -= 0.1f;
                }

                foreach (Image image in Components.GetAll<Image>())
                {
                    image.Color = colour * opacity;
                }
            }
            base.Update();
        }

        internal void OffsetImage(Vector2 offset)
        {
            foreach (Image image in Components.GetAll<Image>())
            {
                image.Position += offset;
            }
        }
    }
}