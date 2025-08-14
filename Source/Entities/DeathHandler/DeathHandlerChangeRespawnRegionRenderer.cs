#region Assembly Celeste, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\user\AppData\Local\Temp\Celeste-publicized.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using Celeste.Mod;
using Celeste.Mod.EndHelper.Entities.DeathHandler;
using Celeste.Mod.EndHelper.Utils;
using CelesteMod.Publicizer;
using FrostHelper.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Celeste;

[Tracked(false)]
public class DeathHandlerChangeRespawnRegionRenderer : Entity
{
    public class Edge
    {
        public DeathHandlerChangeRespawnRegion Parent;

        public bool Visible;
        public Vector2 A;
        public Vector2 B;
        public Vector2 Min;
        public Vector2 Max;
        public Vector2 Normal;
        public Vector2 Perpendicular;
        public float[] Wave;
        public float Length;

        public bool fullReset;
        public bool killOnEnter;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Edge(DeathHandlerChangeRespawnRegion parent, Vector2 a, Vector2 b, bool fullReset, bool killOnEnter)
        {
            this.fullReset = fullReset;
            this.killOnEnter = killOnEnter;

            Parent = parent;
            Visible = true;
            A = a;
            B = b;
            Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            Normal = (b - a).SafeNormalize();
            Perpendicular = -Normal.Perpendicular();
            Length = (a - b).Length();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdateWave(float time)
        {
            if (Wave == null || (float)Wave.Length <= Length)
            {
                Wave = new float[(int)Length + 2];
            }

            for (int i = 0; (float)i <= Length; i++)
            {
                Wave[i] = GetWaveAt(time, i, Length);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public float GetWaveAt(float offset, float along, float length)
        {
            if (along <= 1f || along >= length - 1f)
            {
                return 0f;
            }

            float num = offset + along * 0.25f;
            float num2 = (float)(Math.Sin(num) * 2.0 + Math.Sin(num * 0.25f));
            return (1f + num2 * Ease.SineInOut(Calc.YoYo(along / length)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool InView(ref Rectangle view)
        {
            if ((float)view.Left < Parent.X + Max.X && (float)view.Right > Parent.X + Min.X && (float)view.Top < Parent.Y + Max.Y)
            {
                return (float)view.Bottom > Parent.Y + Min.Y;
            }

            return false;
        }
    }

    public List<DeathHandlerChangeRespawnRegion> list = new List<DeathHandlerChangeRespawnRegion>();
    public List<Edge> edges = new List<Edge>();
    public VirtualMap<bool> tiles;
    public Rectangle levelTileBounds;
    public bool dirty;


    private bool render_fullResetKill = false;
    private bool render_fullResetNormal = false;
    private bool render_resetKill = false;
    private bool render_resetNormal = false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerChangeRespawnRegionRenderer()
    {
        base.Tag = (int)Tags.TransitionUpdate | (int)Tags.Persistent;
        base.Depth = 0;
        Add(new CustomBloom(OnRenderBloom));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Track(DeathHandlerChangeRespawnRegion block)
    {
        list.Add(block);
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"added block - {block} | Location: {block.Position} | FullReset: {block.fullReset} | killOnEnter: {block.killOnEnter}");
        if (tiles == null)
        {
            Rectangle extendedLevelBounds = SceneAs<Level>().Bounds;
            levelTileBounds = new Rectangle(extendedLevelBounds.X, extendedLevelBounds.Y, extendedLevelBounds.Width, extendedLevelBounds.Height);
            tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: false);

            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"create new tile list");
        }

        for (int i = (int)block.X; (float)i < block.Right; i++)
        {
            for (int j = (int)block.Y; (float)j < block.Bottom; j++)
            {
                int mapX = i - levelTileBounds.X;
                int mapY = j - levelTileBounds.Y;
                bool pointFitsInMap = mapX >= 0 && mapY >= 0 && mapX < tiles.Columns && mapY < tiles.Rows;
                if (pointFitsInMap)
                {
                    tiles[mapX, mapY] = true;
                }
                else
                {
                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"pointfitsinmap failed: {mapX} {mapY} || {tiles.Columns} {tiles.Rows}");

                    // Extend the tile VirtualMap to fit the new point + 128 tiles worth of buffer
                    int extX = mapX < 0 ? -mapX + 128 : 0;
                    int extWidth = (mapX >= levelTileBounds.Width) ? (mapX - levelTileBounds.Width + 128) : 0;
                    int extY = mapY < 0 ? -mapY + 128 : 0;
                    int extHeight = (mapY >= levelTileBounds.Height) ? (mapY - levelTileBounds.Height + 128) : 0;
                    extWidth += extX; extHeight += extY;

                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"account for {i} {j}: level bounds: {levelTileBounds} >>");
                    levelTileBounds = new Rectangle(levelTileBounds.X - extX, levelTileBounds.Y - extY, levelTileBounds.Width + extWidth, levelTileBounds.Height + extHeight);
                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $">> now {levelTileBounds}");
                    VirtualMap<bool> newTiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: false);

                    // Copy over, now shifted
                    for (int ix = 0; ix < tiles.Columns; ix++)
                    {
                        for (int iy = 0; iy < tiles.Rows; iy++)
                        {
                            int wx = extX + ix;
                            int wy = extY + iy;
                            newTiles[wx, wy] = tiles[ix, iy];
                        }
                    }
                    newTiles[i - levelTileBounds.X, j - levelTileBounds.Y] = true;

                    //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"to include {i - levelTileBounds.X} {j - levelTileBounds.Y}: expanding tile size: {tiles.Columns}x{tiles.Rows} --> {newTiles.Columns}x{newTiles.Rows}");
                    tiles = newTiles;
                }
            }
        }

        dirty = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Untrack(DeathHandlerChangeRespawnRegion block)
    {
        list.Remove(block);

        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"removed block");
        if (list.Count <= 0)
        {
            tiles = null;
            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"tiles = null");
        }
        else
        {
            for (int i = (int)block.X; (float)i < block.Right; i++)
            {
                for (int j = (int)block.Y; (float)j < block.Bottom; j++)
                {
                    tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = false;
                }
            }
        }

        dirty = true;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        Update();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        if (dirty)
        {
            RebuildEdges();
        }

        UpdateEdges();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateEdges()
    {
        Camera camera = (base.Scene as Level).Camera;
        Rectangle view = new Rectangle((int)camera.Left - 4, (int)camera.Top - 4, (int)(camera.Right - camera.Left) + 8, (int)(camera.Bottom - camera.Top) + 8);
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].Visible)
            {
                if (base.Scene.OnInterval(0.25f, (float)i * 0.01f) && !edges[i].InView(ref view))
                {
                    edges[i].Visible = false;
                }
            }
            else if (base.Scene.OnInterval(0.05f, (float)i * 0.01f) && edges[i].InView(ref view))
            {
                edges[i].Visible = true;
            }

            if (edges[i].Visible && (base.Scene.OnInterval(0.05f, (float)i * 0.01f) || edges[i].Wave == null))
            {
                edges[i].UpdateWave(base.Scene.TimeActive * 3f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RebuildEdges()
    {
        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"rebuild edges");
        dirty = false;
        edges.Clear();
        if (list.Count <= 0)
        {
            return;
        }

        Level obj = base.Scene as Level;
        _ = obj.TileBounds.Left;
        _ = obj.TileBounds.Top;
        _ = obj.TileBounds.Right;
        _ = obj.TileBounds.Bottom;
        Point[] array = new Point[4]
        {
            new Point(0, -8),
            new Point(0, 8),
            new Point(-8, 0),
            new Point(8, 0)
        };
        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            if (!item.Visible) continue;
            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"Adding edges for {item} at {item.Position}. Full Reset {item.fullReset}, killOnEnter {item.killOnEnter}");

            for (int i = (int)item.X; (float)i < item.Right; i += 8)
            {
                for (int j = (int)item.Y; (float)j < item.Bottom; j += 8)
                {
                    Point[] array2 = array;
                    for (int k = 0; k < array2.Length; k++)
                    {
                        Point dir = array2[k];
                        Point rotDir = new Point(-dir.Y, dir.X);
                        if (!Inside(i + dir.X, j + dir.Y) && (!Inside(i - rotDir.X, j - rotDir.Y) || Inside(i + dir.X - rotDir.X, j + dir.Y - rotDir.Y)))
                        {
                            Point topLeftPoint = new Point(i, j);
                            Point bottomRightPoint = new Point(i + rotDir.X, j + rotDir.Y);
                            Vector2 vector = new Vector2(4f) + new Vector2(dir.X - rotDir.X, dir.Y - rotDir.Y) * 0.5f;
                            while (Inside(bottomRightPoint.X, bottomRightPoint.Y) && !Inside(bottomRightPoint.X + dir.X, bottomRightPoint.Y + dir.Y))
                            {
                                bottomRightPoint.X += rotDir.X;
                                bottomRightPoint.Y += rotDir.Y;
                            }
                            Vector2 a = new Vector2(topLeftPoint.X, topLeftPoint.Y) + vector - item.Position;
                            Vector2 b = new Vector2(bottomRightPoint.X, bottomRightPoint.Y) + vector - item.Position;

                            Edge addEdge = new Edge(item, a, b, item.fullReset, item.killOnEnter);
                            edges.Add(addEdge);
                            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"add edge {addEdge}: {addEdge.A} {addEdge.B}");
                        }
                    }
                }
            }
        }

        // Check if rendering is necessary
        render_fullResetKill = false;
        render_fullResetNormal = false;
        render_resetKill = false;
        render_resetNormal = false;

        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            if (!item.visibleArea) continue;
            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $">> for {item}: fullreset {item.fullReset} killOnEnter {item.killOnEnter}");
            if (item.fullReset && item.killOnEnter) render_fullResetKill = true;
            if (item.fullReset && !item.killOnEnter) render_fullResetNormal = true;
            if (!item.fullReset && item.killOnEnter) render_resetKill = true;
            if (!item.fullReset && !item.killOnEnter) render_resetNormal = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Inside(int tx, int ty)
    {
        return tiles[tx - levelTileBounds.X, ty - levelTileBounds.Y];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnRenderBloom()
    {
        Color color = Color.White * 0.15f;
        Rectangle cameraRect = SceneAs<Level>().Camera.GetRect();
        Rectangle cameraRectInflated = cameraRect; cameraRectInflated.Inflate(16, 16);
        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            if (item.Visible && cameraRect.Intersects(item.HitRect()))
            {
                Draw.Rect(item.X, item.Y, item.Width, item.Height, color);
            }
        }

        //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"--------------------------");
        foreach (Edge edge in edges)
        {
            //Rectangle edgeRect = new Rectangle((int)(edge.Parent.Position.X + edge.Min.X - 8), (int)(edge.Parent.Position.Y + edge.Min.Y - 8), (int)(edge.Max.X - edge.Min.X + 16), (int)(edge.Max.Y - edge.Min.Y + 16));
            if (edge.Visible)
            {
                Vector2 edgeFirstPos = edge.Parent.Position + edge.A;
                for (int i = 0; (float)i <= edge.Length; i++)
                {
                    Vector2 vector2 = edgeFirstPos + edge.Normal * i;
                    if (cameraRectInflated.Contains((int)vector2.X, (int)vector2.Y))
                    {
                        Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[i], color);
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        Level level = SceneAs<Level>();
        Color color = Color.White * 0.15f;

        if (list.Count == 0 && edges.Count == 0)
        {
            return;
        }

        Rectangle cameraRect = level.Camera.GetRect();
        Rectangle cameraRectInflated = cameraRect; cameraRectInflated.Inflate(16, 16);

        if (render_fullResetKill) RenderSet(true, true);
        if (render_fullResetNormal) RenderSet(true, false);
        if (render_resetKill) RenderSet(false, true);
        if (render_resetNormal) RenderSet(false, false);

        void RenderSet(bool fullReset, bool killOnEnter)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"render for fullreset {fullReset} killOnEnter {killOnEnter}");

            Color insideColour = fullReset ? Color.Red : Color.Green;
            Color outlineColour = killOnEnter ? Color.Red : Color.Green;

            RespawnRipple.BeginEntityRender(level, insideColour, outlineColour); // Begin applying respawn ripple effect

            foreach (DeathHandlerChangeRespawnRegion item in list)
            {
                if (item.Visible && item.fullReset == fullReset && item.killOnEnter == killOnEnter && cameraRect.Intersects(item.HitRect()))
                {
                    Draw.Rect(item.X, item.Y, item.Width, item.Height, color);
                }
            }

            //Logger.Log(LogLevel.Info, "EndHelper/DeathHandlerChangeRespawnRegionRenderer", $"--------------------------");
            foreach (Edge edge in edges)
            {
                //Rectangle edgeRect = new Rectangle((int)(edge.Parent.Position.X + edge.Min.X - 8), (int)(edge.Parent.Position.Y + edge.Min.Y - 8), (int)(edge.Max.X - edge.Min.X + 16), (int)(edge.Max.Y - edge.Min.Y + 16));
                if (edge.Visible && edge.fullReset == fullReset && edge.killOnEnter == killOnEnter)
                {
                    Vector2 edgeFirstPos = edge.Parent.Position + edge.A;
                    for (int i = 0; (float)i <= edge.Length; i++)
                    {
                        Vector2 vector2 = edgeFirstPos + edge.Normal * i;
                        if (cameraRectInflated.Contains((int)vector2.X, (int)vector2.Y))
                        {
                            Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[i], color);
                        }
                    }
                }
            }
            RespawnRipple.EndEntityRender(level); // End applying respawn ripple effect
        }
    }
}