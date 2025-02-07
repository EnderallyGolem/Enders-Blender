using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;

namespace Celeste.Mod.EndHelper.Entities.Misc;
[Tracked]
[CustomEntity("EndHelper/TileEntity")]

// This is largely PLAGIARIZED from VivHelper because i really needed a good tile entity =[
public class TileEntity : Solid
{
    private TileGrid tiles;

    private char tileType;
    private char tiletypeOffscreen;
    private bool tileTypeMix = false;
    private bool bg;
    private bool allowMergeDifferentType;
    private bool allowMerge;
    private bool extendOffscreen;
    private bool noEdges;

    private bool locationSeeded;

    private List<bool> offDirecBoolList;

    private TileEntity master;

    public List<TileEntity> Group;

    public Point GroupBoundsMin;
    public Point GroupBoundsMax;

    public bool HasGroup
    {
        get;
        private set;
    }

    public bool MasterOfGroup
    {
        get;
        private set;
    }

    public TileEntity(Vector2 position, float width, float height, char tileType, char tiletypeOffscreen, int depth, bool bg, bool blockLights = true, bool allowMergeDifferentType = false, bool allowMerge = true, 
        bool extendOffscreen = false, bool noEdges = false, List<bool> offDirecBoolList = null, bool locationSeeded = false)
    : base(position, width, height, safe: true)
    {
        
        this.tileType = tileType;
        this.tiletypeOffscreen = tiletypeOffscreen;
        Depth = Calc.Clamp(depth, -300000, 20000);
        this.bg = bg;
        this.allowMergeDifferentType = allowMergeDifferentType;
        this.allowMerge = allowMerge;
        this.extendOffscreen = extendOffscreen;
        this.noEdges = noEdges;
        this.offDirecBoolList = offDirecBoolList ?? new List<bool>([true, true, true, true, true, true, true, true]); //Start at top, go CW
        this.locationSeeded = locationSeeded;

        if (bg)
        {
            Collidable = false;
        }
        if (blockLights)
            Add(new LightOcclude());
        if (!SurfaceIndex.TileToIndex.TryGetValue(tileType, out SurfaceSoundIndex))
            SurfaceSoundIndex = SurfaceIndex.Brick;
    }


    private Vector2 relativePos;

    public TileEntity(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'), data.Char("tiletypeOffscreen", '◯'), data.Int("Depth", -9000), data.Bool("BackgroundTile", false), data.Bool("BlockLights", true), data.Bool("allowMergeDifferentType", false), data.Bool("allowMerge", true), data.Bool("extendOffscreen", true), data.Bool("noEdges", false),
              [data.Bool("offU", true), data.Bool("offUR", true), data.Bool("offR", true), data.Bool("offDR", true), data.Bool("offD", true), data.Bool("offDL", true), data.Bool("offL", true), data.Bool("offUL", true)], data.Bool("locationSeeded", false))
    {
        relativePos = data.Position;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (!HasGroup)
        {
            MasterOfGroup = true;
            Group = new List<TileEntity>();
            GroupBoundsMin = new Point((int)X, (int)Y);
            GroupBoundsMax = new Point((int)Right, (int)Bottom);
            AddToGroupAndFindChildren(this);
            _ = Scene;

            Rectangle rectangle = new Rectangle(GroupBoundsMin.X / 8 - 1, GroupBoundsMin.Y / 8 - 1, (GroupBoundsMax.X - GroupBoundsMin.X) / 8 + 3, (GroupBoundsMax.Y - GroupBoundsMin.Y) / 8 + 3);
            VirtualMap<char> virtualMap = new VirtualMap<char>(rectangle.Width, rectangle.Height, '0');


            Level level = SceneAs<Level>();
            Rectangle roomRect = level.Bounds;

            bool noEdgesAny = noEdges;

            foreach (TileEntity item in Group)
            {
                if (item.noEdges)
                {
                    noEdgesAny = true;
                }

                int num = (int)(item.X / 8f - rectangle.X);
                int num2 = (int)(item.Y / 8f - rectangle.Y);
                int num3 = (int)(item.Width / 8f);
                int num4 = (int)(item.Height / 8f);

                //If group size reaches the screen edge and extendOffscreen is enabled, increase width/height by 1 or decrease starting x/y by 1
                if (item.extendOffscreen)
                {
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", "Identifying if edge of room:");
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"LEFT > {(num + rectangle.X)} == {(int) roomRect.Left/8}");
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"RIGHT > {(num + num3 + rectangle.X)} == {(int) roomRect.Right/8}");
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"TOP > {(num2 + rectangle.Y)} == {(int) roomRect.Top/8}");
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"BOTTOM > {(num2 + num4 + rectangle.Y)} == {(int) roomRect.Bottom/8}");
                    if (num + rectangle.X == roomRect.Left / 8)
                    {
                        num--;
                        num3++;
                    }
                    if (num + num3 + rectangle.X == roomRect.Right / 8)
                    {
                        num3++;
                    }
                    if (num2 + rectangle.Y == roomRect.Top / 8)
                    {
                        num2--;
                        num4++;
                    }
                    if (num2 + num4 + rectangle.Y == roomRect.Bottom / 8)
                    {
                        num4++;
                    }
                }

                for (int i = num; i < num + num3; i++)
                {
                    for (int j = num2; j < num2 + num4; j++)
                    {
                        virtualMap[i, j] = item.tileType;
                        Vector2 tilePos = new Vector2(i + rectangle.X, j + rectangle.Y) * 8;
                        //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"{tilePos.X}/{roomRect.Left}/{roomRect.Right} {tilePos.Y}/{roomRect.Top}/{roomRect.Bottom}");

                        Vector2 offDirection = new Vector2(0, 0);
                        if (tilePos.X < roomRect.Left) { offDirection.X = -1; }
                        if (tilePos.X >= roomRect.Right) { offDirection.X = 1; }
                        if (tilePos.Y < roomRect.Top) { offDirection.Y = -1; }
                        if (tilePos.Y >= roomRect.Bottom) { offDirection.Y = 1; }

                        //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"Exceed screen in direction {offDirection.X} {offDirection.Y}");

                        if (offDirection != Vector2.Zero && (
                             item.offDirecBoolList[0] && offDirection == new Vector2(0, -1) ||  //U
                             item.offDirecBoolList[1] && offDirection == new Vector2(1, -1) ||  //UR
                             item.offDirecBoolList[2] && offDirection == new Vector2(1, 0) ||  //R
                             item.offDirecBoolList[3] && offDirection == new Vector2(1, 1) ||  //DR
                             item.offDirecBoolList[4] && offDirection == new Vector2(0, 1) ||  //D
                             item.offDirecBoolList[5] && offDirection == new Vector2(-1, 1) ||  //DL
                             item.offDirecBoolList[6] && offDirection == new Vector2(-1, 0) ||  //L
                             item.offDirecBoolList[7] && offDirection == new Vector2(-1, -1)     //UL;
                           ))
                        {
                            //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"offscreen stuff");
                            virtualMap[i, j] = item.tiletypeOffscreen;
                        }
                    }
                }
            }
            //Logger.Log(LogLevel.Info, "EndHelper/Misc/TileEntity", $"{virtualMap}");
            if (locationSeeded) { Calc.PushRandom((int)(relativePos.X * relativePos.Y + Width + Height)); }
            Autotiler tiler = bg ? GFX.BGAutotiler : GFX.FGAutotiler;
            tiles = tiler.GenerateMap(virtualMap, new Autotiler.Behaviour
            {
                EdgesExtend = false,
                EdgesIgnoreOutOfLevel = noEdgesAny,
                PaddingIgnoreOutOfLevel = false
            }).TileGrid;
            tiles.Position = new Vector2(GroupBoundsMin.X - X - 8, GroupBoundsMin.Y - Y - 8);
            tiles.VisualExtend = 32;
            Add(tiles);
            if (locationSeeded) { Calc.PopRandom(); }
        }
    }

    private void AddToGroupAndFindChildren(TileEntity from, List<Entity> entities = null)
    {
        if (from.X < GroupBoundsMin.X)
        {
            GroupBoundsMin.X = (int)from.X;
        }
        if (from.Y < GroupBoundsMin.Y)
        {
            GroupBoundsMin.Y = (int)from.Y;
        }
        if (from.Right > GroupBoundsMax.X)
        {
            GroupBoundsMax.X = (int)from.Right;
        }
        if (from.Bottom > GroupBoundsMax.Y)
        {
            GroupBoundsMax.Y = (int)from.Bottom;
        }
        from.HasGroup = true;
        Group.Add(from);
        if (from != this)
        {
            from.master = this;
        }
        // Implement variable entities so that it doesn't pull from hash per tileentity in the chain
        if (entities == null && !Scene.Tracker.TryGetEntities<TileEntity>(out entities))
            return;
        foreach (TileEntity entity in entities)
        {
            if (allowMerge && entity.allowMerge && !entity.HasGroup && entity.bg == bg && (Scene.CollideCheck(new Rectangle((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height), entity) || Scene.CollideCheck(new Rectangle((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2), entity)))
            {
                if (allowMergeDifferentType && entity.allowMergeDifferentType)
                {
                    tileTypeMix = true;
                    AddToGroupAndFindChildren(entity, entities);
                }
                else if (entity.tileType == tileType && !tileTypeMix)
                {
                    AddToGroupAndFindChildren(entity, entities);
                }

            }
        }
    }
}

public static class Extensions
{
    public static bool TryGetEntities<T>(this Tracker self, out List<Entity> entities)
    {
        return self.TryGetEntities(typeof(T), out entities);
    }
    public static bool TryGetEntities(this Tracker self, Type type, out List<Entity> entities)
    {
        entities = null;
        if (self.Entities.TryGetValue(type, out entities))
            return true;
        return false;
    }
}