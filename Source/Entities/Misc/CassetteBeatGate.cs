using Celeste.Mod.Entities;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.EndHelper.Integration;
using Celeste.Mod.QuantumMechanics.Entities;
using MonoMod.Utils;
using Celeste.Mod.EndHelper.Entities.RoomSwap;
using Celeste.Mod.EndHelper.Utils;

namespace Celeste.Mod.EndHelper.Entities.Misc
{
    [CustomEntity("EndHelper/CassetteBeatGate")]
    [Tracked(false)]
    public class CassetteBeatGate : Solid
    {
        private static Dictionary<string, Ease.Easer> easeTypes = new Dictionary<string, Ease.Easer> {
            { "Linear", Ease.Linear },
            { "SineIn", Ease.SineIn },
            { "SineOut", Ease.SineOut },
            { "SineInOut", Ease.SineInOut },
            { "QuadIn", Ease.QuadIn },
            { "QuadOut", Ease.QuadOut },
            { "QuadInOut", Ease.QuadInOut },
            { "CubeIn", Ease.CubeIn },
            { "CubeOut", Ease.CubeOut },
            { "CubeInOut", Ease.CubeInOut },
            { "QuintIn", Ease.QuintIn },
            { "QuintOut", Ease.QuintOut },
            { "QuintInOut", Ease.QuintInOut },
            { "BackIn", Ease.BackIn },
            { "BackOut", Ease.BackOut },
            { "BackInOut", Ease.BackInOut },
            { "ExpoIn", Ease.ExpoIn },
            { "ExpoOut", Ease.ExpoOut },
            { "ExpoInOut", Ease.ExpoInOut },
            { "BigBackIn", Ease.BigBackIn },
            { "BigBackOut", Ease.BigBackOut },
            { "BigBackInOut", Ease.BigBackInOut },
            { "ElasticIn", Ease.ElasticIn },
            { "ElasticOut", Ease.ElasticOut },
            { "ElasticInOut", Ease.ElasticInOut },
            { "BounceIn", Ease.BounceIn },
            { "BounceOut", Ease.BounceOut },
            { "BounceInOut", Ease.BounceInOut }
        };

        private readonly Vector2 startPos;
        private readonly Vector2[] nodes;
        private readonly float moveTime;
        private readonly Ease.Easer easer;
        private readonly string moveSound;

        private MTexture texture;
        private SoundSource openSfx;

        private bool moving;
        private bool cancelMoving;
        private int nodeIndex;
        private int newNodeIndex;
        private int firstNode;

        private bool changeInsteadOfSet = false;
        private bool loopNodes = true;

        private Color particleColour1;
        private Color particleColour2;
        private String requireFlag = "";

        private string moveLoopBeatString;
        private string moveCycleBeatString;
        private bool entityMover;
        private bool entityMoverPlatformOnly;
        private readonly List<Entity> moveEntities = [];

        public CassetteBeatGate(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, safe: false)
        {
            // parse all options
            nodes = data.NodesOffset(offset);
            moveTime = data.Float("moveTime", 0.3f);
            easer = easeTypes[data.Attr("easing", "SineInOut")];
            moveSound = data.Attr("moveSound", "");

            moveLoopBeatString = data.Attr("moveLoopBeat", "");
            moveCycleBeatString = data.Attr("moveCycleBeat", "0|1,8|2,16|3,24|4");
            firstNode = data.Int("firstNode", 0);
            changeInsteadOfSet = data.Bool("changeInsteadOfSet", false);
            loopNodes = data.Bool("loopNodes", true);

            entityMover = data.Bool("entityMover", false);
            entityMoverPlatformOnly = data.Bool("entityMoverPlatformOnly", false);

            particleColour1 = data.HexColor("particleColour1", Calc.HexToColor("ffeb6b"));
            particleColour2 = data.HexColor("particleColour2", Calc.HexToColor("d39332"));
            requireFlag = data.Attr("requireFlag", "");

            int surfaceSoundIndexSet = data.Int("surfaceSoundIndex", -1);
            if (surfaceSoundIndexSet >= 0) { SurfaceSoundIndex = surfaceSoundIndexSet; }
            
            startPos = Position;

            // initialize the gate texture
            string blockTexturePath = data.Attr("texturePath", "");
            blockTexturePath = Utils_General.TrimPath(blockTexturePath, "objects/EndHelper/CassetteBeatBlock/YellowBeatBlock");
            texture = GFX.Game[blockTexturePath];

            if (entityMover)
            {
                Visible = false;
                AllowStaticMovers = false;
                BlockWaterfalls = false;
                DisableLightsInside = true;
            }
            else
            {
                Add(openSfx = new SoundSource());
                Add(new LightOcclude(0.5f));
            }
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            if (entityMover)
            {
                foreach (Entity entity in scene.Entities)
                {
                    if (Collider.Collide(entity.Position))
                    {
                        // Add all entities that collide with this on awake into moveEntities
                        if (entity is not CassetteBeatGate)
                        {
                            moveEntities.Add(entity);
                        }
                    }
                }
                Collidable = false; // Disable collision now since it is just a zone which does stuff
            }

            if (loopNodes)
            {
                while (firstNode < 0) { firstNode += nodes.Length + 1; }
                firstNode = firstNode % (nodes.Length + 1);
            }
            else
            {
                if (firstNode < 0) { firstNode = 0; }
                if (firstNode > nodes.Length) { firstNode = nodes.Length; }
            }

            // Instantly move to firstNode, unless it is 0
            firstNode = RestrictNodeNum(firstNode);
            if (firstNode > 0)
            {
                nodeIndex = firstNode;
                newNodeIndex = firstNode;
                Add(new Coroutine(MoveSequence(nodes[firstNode - 1], 0)));
            }

        }

        public override void Render()
        {
            if (entityMover) { return; }

            Vector2 renderPos = new Vector2(Position.X + Shake.X, Position.Y + Shake.Y);
            texture.Render9Slice((int)Collider.Width, (int)Collider.Height, renderPos);

            base.Render();
        }

        public override void Update()
        {
            // Compare newNodeIndex with previous nodeIndex. Move if different
            // Only do it if flag is found though
            if (newNodeIndex != nodeIndex && Utils_General.AreFlagsEnabled(SceneAs<Level>().Session, requireFlag))
            {
                // Go to the new node.
                nodeIndex = newNodeIndex;
                Add(new Coroutine(MoveSequence(nodeIndex > 0 ? nodes[nodeIndex - 1] : startPos, moveTime)));
            }

            base.Update();
        }

        public void IncrementBeatCheckMove(int currentBeat, int totalCycleBeats)
        {
            int cycleBeat = currentBeat % totalCycleBeats;
            //Logger.Log(LogLevel.Info, "EndHelper/Misc/CassetteBeatGate", $"Beat Incremented! currentBeat: {currentBeat} cycleBeat: {cycleBeat}/{totalCycleBeats}");

            // Check if matching
            CheckMatchBeatMove(cycleBeat, moveCycleBeatString);
            CheckMatchBeatMove(currentBeat, moveLoopBeatString);
        }

        private void CheckMatchBeatMove(int currentBeat, string stringListOfBeats)
        {
            // stringListOfBeats is in format 0|1,16|2,40|1.5. Both are integers.
            List<string> tempoPairList = stringListOfBeats.Split(',')
                .Select(s => s.Trim())                  // Remove extra spaces
                .Where(s => !string.IsNullOrEmpty(s))   // Remove empty strings
                .ToList();

            try
            {
                foreach (String beatNodePairStr in tempoPairList)
                {
                    List<String> beatTempoPair = beatNodePairStr.Split('|')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    int beatNum = int.Parse(beatTempoPair[0]);
                    int nodeNum = int.Parse(beatTempoPair[1]);

                    // If changeInsteadOfSet, change the "change" value into the "set" value
                    if (changeInsteadOfSet)
                    {
                        nodeNum += nodeIndex;
                    }

                    // Check if currentBeat matches beatNum. If matches, do the movement!
                    if (beatNum == currentBeat)
                    {
                        newNodeIndex = RestrictNodeNum(nodeNum);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warn, "EndHelper/CassetteBeatGate", $"Cassette Beat Gate at {Position} threw an error. Likely invalid Beat format!\n{e}");
            }
        }

        private int RestrictNodeNum(int nodeNum)
        {
            if (loopNodes)
            {
                while (nodeNum < 0) { nodeNum += nodes.Length + 1; }
                nodeNum = nodeNum % (nodes.Length + 1);
            }
            else
            {
                if (nodeNum < 0) { nodeNum = 0; }
                if (nodeNum > nodes.Length) { nodeNum = nodes.Length; }
            }
            return nodeNum;
        }

        private IEnumerator MoveSequence(Vector2 node, float moveSequenceTime)
        {
            while (moving)
            {
                // cancel the current move, and wait for the move to be effectively cancelled
                cancelMoving = true;
                yield return null;
            }
            cancelMoving = false;
            moving = true;

            Vector2 start = Position;

            if (node != start)
            {
                //yield return 0.1f;

                if (cancelMoving)
                {
                    moving = false;
                    yield break;
                }

                if (!entityMover) 
                {
                    openSfx.Play(moveSound);
                }

                //yield return 0.1f;

                if (cancelMoving)
                {
                    moving = false;
                    yield break;
                }

                // move and emit particles
                int particleAt = 0;
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easer, moveSequenceTime, start: true);
                bool waiting = true;

                ParticleType particle = new ParticleType
                {
                    Color = particleColour1,
                    Color2 = particleColour2,
                    ColorMode = ParticleType.ColorModes.Blink,
                    FadeMode = ParticleType.FadeModes.Late,
                    LifeMin = 1f,
                    LifeMax = 1.5f,
                    Size = 1f,
                    SpeedMin = 5f,
                    SpeedMax = 10f,
                    Acceleration = new Vector2(0f, 6f),
                    DirectionRange = MathF.PI * 2f
                };

                void MoveEntity(Entity entity, Vector2 to)
                {

                    // Round to's coords to integers
                    to = new Vector2((int)Math.Round(to.X), (int)Math.Round(to.Y));

                    if (entity.Get<StaticMover>() is StaticMover staticMoverComponent && staticMoverComponent.Platform != null)
                    {
                        return; // Do not move StaticMovers (that has a platform). Their movement is already handled by moving the actual platform.
                    }
                    //Logger.Log(LogLevel.Info, "EndHelper/Misc/CassetteBeatGate", $"Moving {entity} from {entity.Position} >>> {to}");

                    if (entity is Platform platform)
                    {
                        // Platform
                        try
                        {
                            platform.MoveTo(to);
                        }
                        catch { }
                    }
                    else if (!entityMoverPlatformOnly)
                    {
                        if (entity is Actor actor)
                        {
                            // Actor
                            try
                            {
                                actor.MoveH((to.X - entity.Position.X));
                                actor.MoveV((to.Y - entity.Position.Y));
                            }
                            catch { }
                        }
                        else
                        {
                            // Any Entity
                            entity.Position = to;
                        }
                    }
                }

                tween.OnUpdate = delegate (Tween t) {


                    if (entityMover)
                    {
                        for (int i = 0; i < moveEntities.Count; i++)
                        {
                            Vector2 offset = (Position - moveEntities[i].Position);
                            MoveEntity(moveEntities[i], Vector2.Lerp(start - offset, node - offset, t.Eased));
                        }
                    }
                    MoveTo(Vector2.Lerp(start, node, t.Eased));

                    if (Scene.OnInterval(0.1f) && !entityMover)
                    {
                        particleAt++;
                        particleAt %= 2;
                        for (int x = 0; (float)x < Width / 8f; x++)
                        {
                            for (int y = 0; (float)y < Height / 8f; y++)
                            {
                                if ((x + y) % 2 == particleAt)
                                {
                                    SceneAs<Level>().ParticlesBG.Emit(particle, Position + new Vector2(x * 8, y * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                                }
                            }
                        }
                    }
                };
                tween.OnComplete = (t) => { waiting = false; };
                Add(tween);

                // wait for the move to be done.
                while (waiting)
                {
                    if (cancelMoving)
                    {
                        tween.Stop();
                        Remove(tween);
                        moving = false;
                        yield break;
                    }
                    yield return null;
                }
                Remove(tween);

                if (!entityMover)
                {
                    bool wasCollidable = Collidable;
                    // collide dust particles on the left
                    if (node.X <= start.X)
                    {
                        Vector2 add = new Vector2(0f, 2f);
                        for (int tileY = 0; tileY < Height / 8f; tileY++)
                        {
                            Vector2 collideAt = new Vector2(Left - 1f, Top + 4f + (tileY * 8));
                            Vector2 noCollideAt = collideAt + Vector2.UnitX;
                            if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                            {
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float)Math.PI);
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float)Math.PI);
                            }
                        }
                    }

                    // collide dust particles on the right
                    if (node.X >= start.X)
                    {
                        Vector2 add = new Vector2(0f, 2f);
                        for (int tileY = 0; tileY < Height / 8f; tileY++)
                        {
                            Vector2 collideAt = new Vector2(Right + 1f, Top + 4f + (tileY * 8));
                            Vector2 noCollideAt = collideAt - Vector2.UnitX * 2f;
                            if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                            {
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, 0f);
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, 0f);
                            }
                        }
                    }

                    // collide dust particles on the top
                    if (node.Y <= start.Y)
                    {
                        Vector2 add = new Vector2(2f, 0f);
                        for (int tileX = 0; tileX < Width / 8f; tileX++)
                        {
                            Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Top - 1f);
                            Vector2 noCollideAt = collideAt + Vector2.UnitY;
                            if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                            {
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, -(float)Math.PI / 2f);
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, -(float)Math.PI / 2f);
                            }
                        }
                    }

                    // collide dust particles on the bottom
                    if (node.Y >= start.Y)
                    {
                        Vector2 add = new Vector2(2f, 0f);
                        for (int tileX = 0; tileX < Width / 8f; tileX++)
                        {
                            Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Bottom + 1f);
                            Vector2 noCollideAt = collideAt - Vector2.UnitY * 2f;
                            if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                            {
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float)Math.PI / 2f);
                                SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float)Math.PI / 2f);
                            }
                        }
                    }
                    Collidable = wasCollidable;
                }

                if (cancelMoving)
                {
                    moving = false;
                    yield break;
                }
            }

            moving = false;
        }
    }
}