using Celeste.Mod.EndHelper.Entities.RoomSwap;
using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.QuantumMechanics.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Entities.Misc
{
    [CustomEntity("EndHelper/FlagKillbox")]
    [Tracked(false)]
    [TrackedAs(typeof(Killbox))]
    public class FlagKillbox : Killbox
    {
        private readonly float triggerDistance;
        private readonly string requireFlag;
        private readonly bool permamentActivate;

        private bool flagAllow = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public FlagKillbox(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            triggerDistance = data.Float("triggerDistance", 4f);
            requireFlag = data.Attr("requireFlag", "");
            permamentActivate = data.Bool("permamentActivate", true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            base.Update();

            Level level = SceneAs<Level>();
            float triggerPixels = triggerDistance * 8f;
            if (permamentActivate && flagAllow) { } // Stay true if permament activate
            else
            {
                flagAllow = Utils_General.AreFlagsEnabled(level.Session, requireFlag, true);
            }

            if (!Collidable)
            {
                Player player = base.Scene.Tracker.GetEntity<Player>();
                if (player != null && player.Bottom < base.Top - triggerPixels && flagAllow)
                {
                    Collidable = true;
                }
            }
            else
            {
                Player entity2 = base.Scene.Tracker.GetEntity<Player>();
                if ((entity2 != null && entity2.Top > base.Bottom + 32f) || !flagAllow)
                {
                    Collidable = false;
                }
            }
        }
    }
}