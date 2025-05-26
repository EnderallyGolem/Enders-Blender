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
using static Celeste.Mod.EndHelper.EndHelperModule;
using System.Data.Common;
using AsmResolver.PE.DotNet.ReadyToRun;
using Celeste.Mod.EndHelper.SharedCode;
using Celeste.Mod.EndHelper.Utils;



namespace Celeste.Mod.EndHelper.Entities.RoomSwap;
[Tracked(true)]
[CustomEntity("EndHelper/RoomSwapMap")]
public class RoomSwapMap : Entity
{
    private string gridID;
    private float mapWidth;
    private float mapHeight;
    private Vector2 scale;
    private string folderPath;

    private SineWave sine;

    private EntityData entityData;
    private Vector2 offset;
    private Vector2 hudRenderPos;

    private Image backgroundTexture;
    private string currentRoomPosFileName;
    private List<int> currentRoomPos;
    private Image currentRoomImage;
    private float animationSpeedMultiplier;

    bool refreshNextRenderCycle = true;

    private string iconFilePrefix;
    private string iconFilePrefixLevel;
    private float iconWidthDivide;
    private float iconHeightDivide;
    private List<List<string>> roomSwapOrderList;

    private List<List<int>> iconAnimList;
    private List<List<float>> iconAnimListCurrent;
    private List<List<string>> roomPosSuffixList;

    private bool hudLayer;

    public RoomSwapMap(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        entityData = data;
        this.offset = offset;
        gridID = data.Attr("gridId", "1");
        iconFilePrefix = entityData.Attr("mapIconFilePrefix");
        iconFilePrefixLevel = iconFilePrefix;
        animationSpeedMultiplier = entityData.Float("animationSpeedMultiplier", 0.1f);
        hudLayer = data.Bool("hudLayer", false);

        //Add(RoomSwapModule.SpriteBank.Create("transitionController"));
        Collider = new Hitbox(16, 16, -8, -8);

        Depth = 20;


        folderPath = data.Attr("folderPath", "");
        scale = new Vector2(data.Float("scale", 1f), data.Float("scale", 1f));

        sine = new SineWave(0.5f, MathF.PI / 2);
        Add(sine);

        folderPath = Utils_General.TrimPath(folderPath, "objects/EndHelper/RoomSwapMap");
    }

    public override void Update()
    {
        roomSwapOrderList = EndHelperModule.Session.roomSwapOrderList[gridID];
        sine.Rate = MathHelper.Lerp(0.7f, 0.3f, 0.3f);

        Position.Y += sine.Value * entityData.Float("floatAmplitude", 0.1f);
        base.Update();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        //Add listener
        RoomModificationEvent += RoomModificationEventListener;

        //Background Image
        string backgroundFileName = entityData.Attr("mapBackgroundFileName");
        backgroundTexture = new Image(GFX.Game[$"{folderPath}/{backgroundFileName}"]);
        backgroundTexture.Scale = scale;
        mapWidth = backgroundTexture.Width * scale.X;
        mapHeight = backgroundTexture.Height * scale.Y;

        if (hudLayer)
        {
            AddTag(Tags.HUD);
            AddTag(Tags.TransitionUpdate);
            backgroundTexture.Scale *= 6;
            backgroundTexture.RenderPosition = hudRenderPos;
        }
        backgroundTexture.Position -= new Vector2(mapWidth / 2, mapHeight / 2);

        //Current Location Indicator
        Level level = SceneAs<Level>();
        string currentRoomName = level.Session.LevelData.Name;
        currentRoomPosFileName = entityData.Attr("mapCurrentPosFileName", "");
        if (currentRoomName.StartsWith(EndHelperModule.Session.roomSwapPrefix[gridID]) && currentRoomPosFileName != "")
        {
            currentRoomPos = Utils_RoomSwap.GetPosFromRoomName(currentRoomName);
            string currentRoomPosSuffix = $"{currentRoomPos[0].ToString()}{currentRoomPos[1].ToString()}";
        }
    }

    public void RoomModificationEventListener(object sender, RoomModificationEventArgs e)
    {
        //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionMap", $"If this runs the event listener is probably working! grid id {e.gridID}");
        if (e.gridID == gridID)
        {
            refreshNextRenderCycle = true;
        }
    }

    public override void Removed(Scene scene)
    {
        //Remove Listener
        RoomModificationEvent -= RoomModificationEventListener;
        base.Removed(scene);
    }

    public override void Render()
    {
        if (hudLayer)
        {
            Camera camera = SceneAs<Level>().Camera;
            hudRenderPos = new Vector2((entityData.Position.X - (camera.X - offset.X)) * 6f, (entityData.Position.Y - (camera.Y - offset.Y)) * 6f);
            hudRenderPos += new Vector2(0, sine.Value * entityData.Float("floatAmplitude", 0.1f) * 180f);

            backgroundTexture.RenderPosition = hudRenderPos;
            backgroundTexture.RenderPosition -= new Vector2(mapWidth / 2, mapHeight / 2) * 6;

            if (currentRoomImage != null)
            {
                currentRoomImage.RenderPosition = hudRenderPos;
                currentRoomImage.RenderPosition -= new Vector2(mapWidth / 2, mapHeight / 2) * 6;
                currentRoomImage.RenderPosition += new Vector2(iconWidthDivide * currentRoomPos[1], iconHeightDivide * currentRoomPos[0]) * 6;
            }
        }

        if (EndHelperModule.Session.roomSwapOrderList.ContainsKey(gridID) && EndHelperModule.Session.roomTransitionTime.ContainsKey(gridID))
        {
            //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionMap", $"refreshNextRenderCycle {refreshNextRenderCycle}");
            if (refreshNextRenderCycle)
            {

                Components.RemoveAll<Image>();

                //Background Image
                Add(backgroundTexture);

                roomSwapOrderList = EndHelperModule.Session.roomSwapOrderList[gridID];
                iconWidthDivide = mapWidth / (EndHelperModule.Session.roomSwapColumn[gridID] + 1);
                iconHeightDivide = mapHeight / (EndHelperModule.Session.roomSwapRow[gridID] + 1);

                if (EndHelperModule.Session.roomMapLevel[gridID] > 0)
                {
                    iconFilePrefixLevel = $"{EndHelperModule.Session.roomMapLevel[gridID].ToString()}{iconFilePrefix}";
                }
                else
                {
                    iconFilePrefixLevel = iconFilePrefix;
                }

                //Icons
                iconAnimList = []; iconAnimListCurrent = []; roomPosSuffixList = [];
                for (int row = 1; row <= EndHelperModule.Session.roomSwapRow[gridID]; row++)
                {
                    List<int> iconAnimListRow = [];
                    List<float> iconAnimListCurrentRow = [];
                    List<string> roomPosSuffixListRow = [];
                    for (int col = 1; col <= EndHelperModule.Session.roomSwapColumn[gridID]; col++)
                    {
                        List<int> roomPos = Utils_RoomSwap.GetPosFromRoomName(roomSwapOrderList[row - 1][col - 1]);
                        string roomPosSuffix = $"{roomPos[0].ToString()}{roomPos[1].ToString()}";

                        //Try get folderpath/iconprefix_XY_# then see how many # are there

                        //if can't, get folderpath/iconprefix_XY

                        int frameIndex = -1;
                        MTexture iconTexture;

                        // Check if animated, and if so, how many frames (idk if there's a better way of doing this that doesn't spam errors)
                        while (true)
                        {
                            iconTexture = GFX.Game[$"{folderPath}/{iconFilePrefixLevel}{roomPosSuffix}_{(frameIndex + 1)}"];

                            if (iconTexture.AtlasPath == "__fallback")
                            {
                                break;
                            }
                            frameIndex++;
                        }

                        // Set iconTexture depending on if its animated or not
                        if (frameIndex == -1)
                        { iconTexture = GFX.Game[$"{folderPath}/{iconFilePrefixLevel}{roomPosSuffix}"]; }
                        else
                        { iconTexture = GFX.Game[$"{folderPath}/{iconFilePrefixLevel}{roomPosSuffix}_0"]; }

                        Image iconImage = new Image(iconTexture);
                        iconImage.Scale = scale;

                        //Center to top left
                        iconImage.CenterOrigin();
                        iconImage.Position -= new Vector2(mapWidth / 2, mapHeight / 2);

                        //Move them so they're evenly spaced
                        iconImage.Position += new Vector2(iconWidthDivide * col, iconHeightDivide * row);
                        Add(iconImage);

                        if (hudLayer)
                        {
                            iconImage.Scale *= 6f;
                            iconImage.RenderPosition = hudRenderPos;
                            iconImage.RenderPosition -= new Vector2(mapWidth / 2, mapHeight / 2) * 6;
                            iconImage.RenderPosition += new Vector2(iconWidthDivide * col, iconHeightDivide * row) * 6;
                        }

                        iconAnimListRow.Add(frameIndex);
                        iconAnimListCurrentRow.Add(0);
                        roomPosSuffixListRow.Add(roomPosSuffix);
                    }
                    iconAnimList.Add(iconAnimListRow);
                    iconAnimListCurrent.Add(iconAnimListCurrentRow);
                    roomPosSuffixList.Add(roomPosSuffixListRow);
                }

                //Get current room's position
                if (currentRoomPos != null)
                {
                    currentRoomImage = new Image(GFX.Game[$"{folderPath}/{currentRoomPosFileName}"]);
                    currentRoomImage.Scale = scale;

                    //Center to top left
                    currentRoomImage.CenterOrigin();
                    currentRoomImage.Position -= new Vector2(mapWidth / 2, mapHeight / 2);
                    currentRoomImage.Position += new Vector2(iconWidthDivide * currentRoomPos[1], iconHeightDivide * currentRoomPos[0]);

                    if (hudLayer)
                    {
                        currentRoomImage.Scale *= 6f;
                    }

                    Add(currentRoomImage);
                }
                refreshNextRenderCycle = false;
            }
            else
            {
                // A slightly less resource intensive version of the code above that takes animation into account

                Components.RemoveAll<Image>();

                //Background Image
                Add(backgroundTexture);

                //Icons
                for (int row = 1; row <= EndHelperModule.Session.roomSwapRow[gridID]; row++)
                {
                    List<int> iconAnimListRow = [];
                    for (int col = 1; col <= EndHelperModule.Session.roomSwapColumn[gridID]; col++)
                    {
                        string roomPosSuffix = roomPosSuffixList[row - 1][col - 1];
                        float currentAnimIndex = iconAnimListCurrent[row - 1][col - 1];
                        int maxAnimIndex = iconAnimList[row - 1][col - 1];

                        MTexture iconTexture;

                        // No anim: Just set iconTexture to the thing
                        if (maxAnimIndex == -1)
                        {
                            iconTexture = GFX.Game[$"{folderPath}/{iconFilePrefixLevel}{roomPosSuffix}"];
                        }
                        else
                        {
                            //Yes Anim: Increase currentAnimIndex, loop if necessary, then set texture
                            currentAnimIndex += animationSpeedMultiplier;
                            if (currentAnimIndex >= maxAnimIndex + 1)
                            {
                                currentAnimIndex = 0;
                            }
                            else if (currentAnimIndex < 0)
                            {
                                currentAnimIndex = maxAnimIndex + 0.999f;
                            }
                            iconTexture = GFX.Game[$"{folderPath}/{iconFilePrefixLevel}{roomPosSuffix}_{(int)currentAnimIndex}"];
                            iconAnimListCurrent[row - 1][col - 1] = currentAnimIndex;
                        }

                        Image iconImage = new Image(iconTexture);
                        iconImage.Scale = scale;

                        //Center to top left
                        iconImage.CenterOrigin();
                        iconImage.Position -= new Vector2(mapWidth / 2, mapHeight / 2);

                        //Move them so they're evenly spaced
                        iconImage.Position += new Vector2(iconWidthDivide * col, iconHeightDivide * row);

                        Add(iconImage);
                        if (hudLayer)
                        {
                            iconImage.Scale *= 6f;
                            iconImage.RenderPosition = hudRenderPos;
                            iconImage.RenderPosition -= new Vector2(mapWidth / 2, mapHeight / 2) * 6;
                            iconImage.RenderPosition += new Vector2(iconWidthDivide * col, iconHeightDivide * row) * 6;
                        }
                    }
                }

                //Current room highlight image
                if (currentRoomPos != null)
                {
                    Add(currentRoomImage);
                }
            }
        }
        base.Render();
    }
}