using Celeste.Mod.Entities;
using Celeste.Mod.EndHelper.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;
using static Celeste.TempleGate;
using static On.Celeste.Level;
using System.Security.Cryptography.X509Certificates;
using System;
using Celeste.Mod.EndHelper.SharedCode;



namespace Celeste.Mod.EndHelper.Entities.RoomSwap;

[CustomEntity("EndHelper/RoomSwapController")]
public class RoomSwapController : Entity
{

    private string gridID;

    public RoomSwapController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(16, 16, -8, -8);

        gridID = data.Attr("gridId", "1");

        // Swappy Setup
        EndHelperModule.Session.roomSwapRow[gridID] = data.Int("totalRows", 0);
        EndHelperModule.Session.roomSwapColumn[gridID] = data.Int("totalColumns", 0);
        EndHelperModule.Session.roomSwapPrefix[gridID] = data.Attr("swapRoomNamePrefix", "swap");
        EndHelperModule.Session.roomTemplatePrefix[gridID] = data.Attr("templateRoomNamePrefix", "template");

        EndHelperModule.Session.roomTransitionTime[gridID] = data.Float("roomTransitionTime", 0.3f);
        EndHelperModule.Session.activateSoundEvent1[gridID] = data.Attr("activateSoundEvent1", "");
        EndHelperModule.Session.activateSoundEvent2[gridID] = data.Attr("activateSoundEvent2", "");

        EndHelperModule.Session.enableRoomSwapFuncs = true;
    }
    public override void Added(Scene scene)
    {
        base.Added(scene);
        // Ensure reset only occurs ONCE. Reentering room with controller doesn't re-resets it.
        // I have no idea why this has to be here or the map dies
        // also do not check player properties, player is null when the map *just* loads
        if (!EndHelperModule.Session.roomSwapOrderList.ContainsKey(gridID))
        {
            EndHelperModule.Session.allowTriggerEffect[gridID] = true;
            EndHelperModule.Session.roomMapLevel[gridID] = 0;

            Level level = SceneAs<Level>();
            Player player = level.Tracker.GetEntity<Player>();

            Utils_RoomSwap.ModifyRooms("Reset", true, player, level, gridID); //This loads the stuff in 
            Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/RoomSwapController", $"Added a {EndHelperModule.Session.roomSwapRow[gridID]}x{EndHelperModule.Session.roomSwapColumn[gridID]} grid with id {gridID}");
        }
    }
}