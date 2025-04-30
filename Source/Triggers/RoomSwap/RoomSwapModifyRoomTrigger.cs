using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/RoomSwapModifyRoomTrigger")]
public class RoomSwapModifyRoomTrigger : Trigger
{

    private string gridID;
    private string modifyType = "None";
    private bool modifySilently = false;

    private string requireFlag = "";
    private string toggleFlag = "";
    private bool flashEffect = false;

    // OLD BEHAVIOUR
    private string flagCheck = "";
    private bool flagRequire = true;
    private bool flagToggle = false;

    public RoomSwapModifyRoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        gridID = data.Attr("gridId", "1");
        modifyType = data.Attr("modificationType", "None");
        modifySilently = data.Bool("modifySilently", false);
        flashEffect = data.Bool("flashEffect", false);

        requireFlag = data.Attr("requireFlag", "");
        toggleFlag = data.Attr("toggleFlag", "");

        // OLD BEHAVIOUR
        flagCheck = data.Attr("flagCheck", "");
        flagRequire = data.Bool("flagRequire", true);
        flagToggle = data.Bool("flagToggle", false);
    }

    public override void OnEnter(Player player)
    {
        Level level = SceneAs<Level>();
        if (flagCheck == "")
        {
            if (Utils_General.AreFlagsEnabled(level.Session, requireFlag))
            {
                bool succeedSwap = Utils_RoomSwap.ModifyRooms(modifyType, modifySilently, player, SceneAs<Level>(), gridID, teleportDisableMilisecond: 300, flashEffect: flashEffect);
                Utils_General.ToggleFlags(level.Session, toggleFlag, succeedSwap);
            }
        }
        // OLD BEHAVIOUR
        else if (level.Session.GetFlag(flagCheck) == flagRequire)
        {
            if (flagToggle)
            {
                level.Session.SetFlag(flagCheck, !level.Session.GetFlag(flagCheck));
            }
            Utils_RoomSwap.ModifyRooms(modifyType, modifySilently, player, SceneAs<Level>(), gridID, teleportDisableMilisecond: 300, flashEffect: flashEffect);
        }
    }

    public override void OnStay(Player player)
    {

    }

    public override void OnLeave(Player player)
    {

    }
}