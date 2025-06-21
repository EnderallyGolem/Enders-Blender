#region Assembly Celeste, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\user\AppData\Local\Temp\Celeste-publicized.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using Celeste.Mod.EndHelper.Entities.DeathHandler;
using Celeste.Mod.EndHelper.Utils;
using CelesteMod.Publicizer;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public DeathHandlerChangeRespawnRegionRenderer()
    {
        base.Tag = (int)Tags.TransitionUpdate;
        base.Depth = 0;
        Add(new CustomBloom(OnRenderBloom));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Track(DeathHandlerChangeRespawnRegion block)
    {
        list.Add(block);
        if (tiles == null)
        {
            levelTileBounds = (base.Scene as Level).TileBounds;
            tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: false);
        }

        for (int i = (int)block.X / 8; (float)i < block.Right / 8f; i++)
        {
            for (int j = (int)block.Y / 8; (float)j < block.Bottom / 8f; j++)
            {
                tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = true;
            }
        }

        dirty = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Untrack(DeathHandlerChangeRespawnRegion block)
    {
        list.Remove(block);
        if (list.Count <= 0)
        {
            tiles = null;
        }
        else
        {
            for (int i = (int)block.X / 8; (float)i < block.Right / 8f; i++)
            {
                for (int j = (int)block.Y / 8; (float)j < block.Bottom / 8f; j++)
                {
                    tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = false;
                }
            }
        }

        dirty = true;
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
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0)
        };
        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            for (int i = (int)item.X / 8; (float)i < item.Right / 8f; i++)
            {
                for (int j = (int)item.Y / 8; (float)j < item.Bottom / 8f; j++)
                {
                    Point[] array2 = array;
                    for (int k = 0; k < array2.Length; k++)
                    {
                        Point point = array2[k];
                        Point point2 = new Point(-point.Y, point.X);
                        if (!Inside(i + point.X, j + point.Y) && (!Inside(i - point2.X, j - point2.Y) || Inside(i + point.X - point2.X, j + point.Y - point2.Y)))
                        {
                            Point point3 = new Point(i, j);
                            Point point4 = new Point(i + point2.X, j + point2.Y);
                            Vector2 vector = new Vector2(4f) + new Vector2(point.X - point2.X, point.Y - point2.Y) * 4f;
                            while (Inside(point4.X, point4.Y) && !Inside(point4.X + point.X, point4.Y + point.Y))
                            {
                                point4.X += point2.X;
                                point4.Y += point2.Y;
                            }

                            Vector2 a = new Vector2(point3.X, point3.Y) * 8f + vector - item.Position;
                            Vector2 b = new Vector2(point4.X, point4.Y) * 8f + vector - item.Position;
                            edges.Add(new Edge(item, a, b, item.fullReset, item.killOnEnter));
                        }
                    }
                }
            }
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
        Camera camera = (base.Scene as Level).Camera;
        new Rectangle((int)camera.Left, (int)camera.Top, (int)(camera.Right - camera.Left), (int)(camera.Bottom - camera.Top));
        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            if (item.Visible)
            {
                Draw.Rect(item.X, item.Y, item.Width, item.Height, Color.White);
            }
        }

        foreach (Edge edge in edges)
        {
            if (edge.Visible)
            {
                Vector2 vector = edge.Parent.Position + edge.A;
                _ = edge.Parent.Position + edge.B;
                for (int i = 0; (float)i <= edge.Length; i++)
                {
                    Vector2 vector2 = vector + edge.Normal * i;
                    Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[i], Color.White);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        Level level = SceneAs<Level>();

        Color color = Color.White * 0.15f;
        Color value = Color.White * 0.25f;

        if (list.Count == 0 && edges.Count == 0)
        {
            return;
        }


        // Check if rendering is necessary
        bool fullResetFalse = false;
        bool fullResetTrue = false;
        bool killOnEnterFalse = false;
        bool killOnEnterTrue = false;

        foreach (DeathHandlerChangeRespawnRegion item in list)
        {
            if (item.fullReset == false) fullResetFalse = true;
            if (item.fullReset == true) fullResetTrue = true;
            if (item.killOnEnter == false) killOnEnterFalse = true;
            if (item.killOnEnter == true) killOnEnterTrue = true;
        }

        if (fullResetTrue && killOnEnterTrue) RenderSet(true, true);
        if (fullResetTrue && killOnEnterFalse) RenderSet(true, false);
        if (fullResetFalse && killOnEnterTrue) RenderSet(false, true);
        if (fullResetFalse && killOnEnterFalse) RenderSet(false, false);

        void RenderSet(bool fullReset, bool killOnEnter)
        {
            Color insideColour = fullReset ? Color.Red : Color.Green;
            Color outlineColour = killOnEnter ? Color.Red : Color.Green;

            RespawnRipple.BeginEntityRender(level, insideColour, outlineColour); // Begin applying respawn ripple effect

            foreach (DeathHandlerChangeRespawnRegion item in list)
            {
                if (item.Visible && item.fullReset == fullReset && item.killOnEnter == killOnEnter)
                {
                    Draw.Rect(item.Collider, color);
                }
            }

            foreach (Edge edge in edges)
            {
                if (edge.Visible && edge.fullReset == fullReset && edge.killOnEnter == killOnEnter)
                {
                    Vector2 vector = edge.Parent.Position + edge.A;
                    _ = edge.Parent.Position + edge.B;
                    Color.Lerp(value, Color.White, edge.Parent.Flash);
                    for (int i = 0; (float)i <= edge.Length; i++)
                    {
                        Vector2 vector2 = vector + edge.Normal * i;
                        Draw.Line(vector2, vector2 + edge.Perpendicular * edge.Wave[i], color);
                    }
                }
            }
            RespawnRipple.EndEntityRender(level); // End applying respawn ripple effect
        }
    }
}