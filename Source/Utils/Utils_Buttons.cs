using AsmResolver.PE.DotNet.Cil;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Celeste.Mod.EndHelper.Utils
{
    internal class Utils_Buttons
    {
        public static VirtualButton Backboost;
        public static VirtualButton NeutralDrop;

        public static void Initialise()
        {
            Backboost = new VirtualButton(EndHelperModule.Settings.Backboost.Binding, Input.Gamepad, 0.08f, 0.2f)
            { canRepeat = true };
            NeutralDrop = new VirtualButton(EndHelperModule.Settings.NeutralDrop.Binding, Input.Gamepad, 0.08f, 0.2f)
            { canRepeat = true };
        }
    }
}
