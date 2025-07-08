using AsmResolver.PE.DotNet.Cil;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    static internal class Utils_General
    {
        public static float framesSinceEnteredRoom = 0;


        /// <summary>
        /// Compare if 2 2d lists are equal
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool Are2LayerListsEqual<T>(List<List<T>> list1, List<List<T>> list2)
        {
            if (list1 == null || list2 == null)
            {
                return false;
            }

            return list1.Count == list2.Count &&
                   list1.Zip(list2, (inner1, inner2) => inner1.SequenceEqual(inner2)).All(equal => equal);
        }

        // When will I finally learn a language that doesn't make deep cloning an absolute pain

        /// <summary>
        /// Lazy af deep cloning
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T DeepCopyJSON<T>(T input)
        {
            var jsonString = JsonSerializer.Serialize(input);

            return JsonSerializer.Deserialize<T>(jsonString);
        }

        /// <summary>
        /// Converts a Timespan into h:mm:ss string
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string MinimalGameplayFormat(TimeSpan time)
        {
            if (time.TotalHours >= 1.0)
            {
                return (int)time.TotalHours + ":" + time.ToString("mm\\:ss");
            }
            return time.ToString("m\\:ss");
        }

        /// <summary>
        /// Convert a Dictionary<object, objcet> to a dictionary<string, string>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ConvertToStringDictionary(Dictionary<object, object> source)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var kvp in source)
            {
                string key = kvp.Key?.ToString() ?? "";  // Convert key to string, default to ""
                string value = kvp.Value?.ToString() ?? ""; // Convert value to string, default to ""
                if (key == "" || value == "") { continue; }
                result[key] = value;
            }
            return result;
        }

        /// <summary>
        /// Convert an OrderedDictionary to a Dictionary<TKey, TValue>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> ConvertFromOrderedDictionary<TKey, TValue>(OrderedDictionary source)
        {
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            foreach (DictionaryEntry entry in source)
            {
                TKey key = (TKey)Convert.ChangeType(entry.Key, typeof(TKey));
                TValue value = (TValue)Convert.ChangeType(entry.Value, typeof(TValue));
                result[key] = value;
            }
            return result;
        }

        /// <summary>
        /// Buffers a VirtualButton for a few frames
        /// </summary>
        /// <param name="input"></param>
        /// <param name="frames"></param>
        public async static void ConsumeInput(VirtualButton input, int frames)
        {
            while (frames > 0)
            {
                input.ConsumePress();
                input.ConsumeBuffer();
                frames--;
                await Task.Delay((int)(Engine.DeltaTime * 1000));
            }
        }

        public static int scrollInputFrames = 0;
        public static int scrollResetInputFrames = 0;

        /// <summary>
        /// Held menu buttons. Eg: Holding right increases a value once, then waits a few frames, then rapidly increases.
        /// This should run every frame.
        /// </summary>
        /// <param name="valueToChange"></param>
        /// <param name="increaseInput"></param>
        /// <param name="increaseValue"></param>
        /// <param name="decreaseInput"></param>
        /// <param name="decreaseValue"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="framesFirstHeldChange"></param>
        /// <param name="framesBetweenHeldChange"></param>
        /// <returns></returns>
        public static int ScrollInput(int valueToChange, bool increaseInput, int increaseValue, bool decreaseInput, int decreaseValue, int minValue, int maxValue, bool loopValues, bool doNotChangeIfPastCap, int framesFirstHeldChange, int framesBetweenHeldChange)
        {
            // Hook_EngineUpdate: Decrease scrollResetInputFrames every frame, if 1, set scrollInputFrames to 0

            // If maxValue < minValue, there is no scroll to do. Just exit!
            // maxValue == minValue might still have to scroll if it is outside the range (eg: due to range being reduced by filter)
            // This prevents crash from clamp min > clamp max
            if (maxValue < minValue)
            {
                return valueToChange;
            }

            int initialValue = valueToChange;

            if (increaseInput)
            {
                if (scrollInputFrames < 0)
                {
                    scrollInputFrames = 0;
                }
                scrollInputFrames++;
                scrollResetInputFrames = 5;
            }
            else if (decreaseInput)
            {
                if (scrollInputFrames > 0)
                {
                    scrollInputFrames = 0;
                }
                scrollInputFrames--;
                scrollResetInputFrames = 5;
            }

            // Increase first time
            if (scrollInputFrames == 1)
            {
                scrollInputFrames++;
                if (!doNotChangeIfPastCap || valueToChange + increaseValue <= maxValue)
                {
                    valueToChange += increaseValue;
                }
            }
            // Increase more
            if (scrollInputFrames > framesFirstHeldChange + framesBetweenHeldChange + 2)
            {
                scrollInputFrames = framesFirstHeldChange;
            }
            if (scrollInputFrames == framesFirstHeldChange)
            {
                scrollInputFrames++;
                if (!doNotChangeIfPastCap || valueToChange + increaseValue <= maxValue)
                {
                    valueToChange += increaseValue;
                }
            }

            // Decrease first time
            if (scrollInputFrames == -1)
            {
                scrollInputFrames--;
                if (!doNotChangeIfPastCap || valueToChange - decreaseValue >= minValue)
                {
                    valueToChange -= decreaseValue;
                }
            }
            // Decrease more
            if (scrollInputFrames < -framesFirstHeldChange - framesBetweenHeldChange - 2)
            {
                scrollInputFrames = -framesFirstHeldChange;
            }
            if (scrollInputFrames == -framesFirstHeldChange)
            {
                scrollInputFrames--;
                if (!doNotChangeIfPastCap || valueToChange - decreaseValue >= minValue)
                {
                    valueToChange -= decreaseValue;
                }
            }

            // Ensure within range
            if (loopValues)
            {
                if (valueToChange > maxValue)
                {
                    valueToChange = minValue;
                }
                else if (valueToChange < minValue)
                {
                    valueToChange = maxValue;
                }
            }
            valueToChange = Math.Clamp(value: valueToChange, min: minValue, max: maxValue);

            if (valueToChange > initialValue)
            {
                Audio.Play("event:/ui/main/savefile_rollover_up");
            }
            else if (valueToChange < initialValue)
            {
                Audio.Play("event:/ui/main/savefile_rollover_up");
            }

            return valueToChange;
        }

        /// <summary>
        /// Takes in a path set in loenn, outputs a path that can be used as an Image.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="defaultPath"></param>
        /// <returns></returns>
        public static string TrimPath(string path, string defaultPath)
        {
            if (path == "") { path = defaultPath; }
            while (path.StartsWith("objects") == false)
            {
                path = path.Substring(path.IndexOf('/') + 1);
            }
            if (path.IndexOf(".") > -1)
            {
                path = path.Substring(0, path.IndexOf("."));
            }
            return path;
        }

        /// <summary>
        /// Renders a 9-slice texture. A good renderPos is: new Vector2(Position.X + Shake.X, Position.Y + Shake.Y)
        /// </summary>
        /// <param name="spriteWidth"></param>
        /// <param name="spriteHeight"></param>
        /// <param name="renderPos"></param>
        /// <param name="texture"></param>
        /// 
        public static void Render9Slice(this MTexture texture, int spriteWidth, int spriteHeight, Vector2 renderPos)
        {
            Vector2 origRenderPos = renderPos;

            int widthInTiles = spriteWidth / 8 - 1;
            int heightInTiles = spriteHeight / 8 - 1;

            float xSize = texture.Width / 3;
            float ySize = texture.Height / 3;

            Texture2D baseTexture = texture.Texture.Texture;
            int clipBaseX = texture.ClipRect.X;
            int clipBaseY = texture.ClipRect.Y;

            Rectangle clipRect = new Rectangle(clipBaseX, clipBaseY, 8, 8);

            for (int i = 0; i <= widthInTiles; i++)
            {
                clipRect.X = clipBaseX + ((i < widthInTiles) ? i == 0 ? 0 : 8 : 16);
                for (int j = 0; j <= heightInTiles; j++)
                {
                    int tilePartY = (j < heightInTiles) ? j == 0 ? 0 : 8 : 16;
                    clipRect.Y = tilePartY + clipBaseY;
                    Draw.SpriteBatch.Draw(baseTexture, renderPos, clipRect, Color.White);
                    renderPos.Y += ySize;
                }
                renderPos.X += xSize;
                renderPos.Y = origRenderPos.Y;
            }
        }
        public static void Render9Slice(this MTexture texture, float spriteWidth, float spriteHeight, Vector2 renderPos)
        { texture.Render9Slice((int)spriteWidth, (int)spriteHeight, renderPos); }

        /// <summary>
        /// Checks if the specified flag is enabled, negation if ! is in front. Returns boolIfEmpty (default true) if empty.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="flagStringList"></param>
        /// <param name="boolIfEmpty"></param>
        /// <returns></returns>
        public static bool AreFlagsEnabled(Session session, string flagStringList, bool boolIfEmpty = true)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"IsFlagEnabled - Flag {flag}, boolIfEmpty {boolIfEmpty}");
            if (flagStringList == "")
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"Flag is empty! Returning {boolIfEmpty}");
                return boolIfEmpty;
            }
            else
            {
                // a, b, !c | d, e
                // Split the |
                // If any returns true, whole thing is true.

                String[] flagListAnds = flagStringList.Split('|');

                foreach (String flagStringAnds in flagListAnds)
                {
                    bool orTrue = AreFlagsEnabled_ANDs(session, flagStringAnds);

                    if (orTrue)
                    {
                        return true;
                    }
                }
                return false; // None of the ORs return true. Thus false.
            }
        }

        private static bool AreFlagsEnabled_ANDs(Session session, string flagStringAnds)
        {
            // a, b, !c
            // Split the ,
            // If any returns false, whole thing is false.

            String[] flagList = flagStringAnds.Split(',');

            foreach (String flagString in flagList)
            {
                bool orTrue = IsFlagEnabled(session, flagString);

                if (!orTrue)
                {
                    return false;
                }
            }
            return true; // None of the ANDs return false. Thus true.
        }

        // The boolIfEmpty is just here IN CASE this is to be used individually outside of here.
        // It is currently private because it's not being used outside of here lol
        private static bool IsFlagEnabled(Session session, string flagString, bool boolIfEmpty = true)
        {
            flagString = flagString.Trim();
            // flagString is either b or !c (or empty).
            if (flagString == "")
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"Flag is empty! Returning {boolIfEmpty}");
                return boolIfEmpty;
            }
            else
            {
                // Check if first character is !
                if (flagString.StartsWith('!'))
                {
                    flagString = flagString.Substring(1);
                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"Flag starts with !, checking if {flag} exists: {session.GetFlag(flag)} (returning opposite)");
                    return !session.GetFlag(flagString);
                }
                else
                {
                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"Flag starts with !, checking if {flag} exists: {session.GetFlag(flag)}");
                    return session.GetFlag(flagString);
                }
            }
        }

        /// <summary>
        /// Takes in a string of flags (Eg: "flagA" or "flagB, flagC") and inverts all of them if shouldToggle is true.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="flagString"></param>
        /// <param name="shouldToggle"></param>
        public static void ToggleFlags(Session session, string flagString, bool shouldToggle = true)
        {
            if (shouldToggle && flagString.Trim() != "")
            {
                String[] flagList = flagString.Split(",");
                foreach (String flag in flagList)
                {
                    String flagTrim = flag.Trim();
                    session.SetFlag(flagTrim, !session.GetFlag(flagTrim));
                }
            }
        }
        
        /// <summary>
        /// Converts a Camera into a Rectangle
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="inflateX"></param>
        /// <param name="inflateY"></param>
        /// <returns></returns>
        public static Rectangle GetRect(this Camera camera, int inflateX = 0, int inflateY = 0)
        {
            Rectangle cameraRect = new Rectangle((int)camera.X, (int)camera.Y, (int)(camera.Right - camera.Left), (int)(camera.Bottom - camera.Top));
            cameraRect.Inflate(inflateX, inflateY);
            return cameraRect;
        }

        /// <summary>
        /// Returns the distance from the pos to the rectangle. 0 if inside.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static float GetDistanceTo(this Rectangle rectangle, Vector2 pos, out Vector2 closestPos)
        {
            // Return 0 if inside
            if (rectangle.Contains((int)pos.X, (int)pos.Y))
            {
                closestPos = pos;
                return 0;
            }

            // Find closer edge


            float closerXEdge; float closerYEdge;
            if (pos.X < rectangle.Left) closerXEdge = rectangle.Left;
            else if (pos.X > rectangle.Right) closerXEdge = rectangle.Right; 
            else closerXEdge = pos.X;

            if (pos.Y < rectangle.Top) closerYEdge = rectangle.Top;
            else if (pos.Y > rectangle.Bottom) closerYEdge = rectangle.Bottom;
            else closerYEdge = pos.Y;

            // Distance
            closestPos = new Vector2(closerXEdge, closerYEdge);
            return Vector2.Distance(closestPos, pos);
        }

        /// <summary>
        /// Returns the intersection point between the line between (or extending from) a point and the center of a rectangle, and the boundary of that rectangle
        /// and the boundary of rect.
        /// </summary>
        public static Vector2 PointToCenterIntersect(this Rectangle rectangle, Vector2 point)
        {
            Vector2 center = Extensions.ToVector2(rectangle.Center);
            Vector2 centerToPointVector = point - center;

            if (centerToPointVector == Vector2.Zero)
            {
                // what nonsense is this
                return point;
            }

            centerToPointVector.Normalize();

            Vector2 intersectPos = center;

            rectangle.Inflate(-3, -3);
            while (rectangle.Contains((int)intersectPos.X, (int)intersectPos.Y))
            {
                intersectPos += centerToPointVector * 3;
                // Logger.Log(LogLevel.Info, "EndHelper/Utils_General", $"PointToCenterIntersect: intersectPos {intersectPos}");
            }
            rectangle.Inflate(2, 2);
            while (rectangle.Contains((int)intersectPos.X, (int)intersectPos.Y))
            {
                intersectPos += centerToPointVector * 0.3f;
            }
            rectangle.Inflate(1, 1);
            while (rectangle.Contains((int)intersectPos.X, (int)intersectPos.Y))
            {
                intersectPos += centerToPointVector * 0.03f;
            }

            return intersectPos;
        }

        /// <summary>
        /// Converts a Point to a Vector2
        /// </summary>
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Entity GetNearestGenericEntity(this Level level, Vector2 nearestTo)
        {
            EntityList entityList = level.Entities;
            Entity closestEntity = null;
            float num = 0f;
            foreach (Entity entity in entityList)
            {
                float num2 = Vector2.DistanceSquared(nearestTo, entity.Position);
                if (closestEntity == null || num2 < num)
                {
                    closestEntity = entity;
                    num = num2;
                }
            }

            return closestEntity;
        }

        /// <summary>
        /// Returns a Rectangle which is the size of the entity (same x, y, width, height)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="overrideWidth"></param>
        /// <param name="overrideHeight"></param>
        /// <returns></returns>
        public static Rectangle HitRect(this Entity entity, int? overrideWidth = null, int? overrideHeight = null, bool useCollider = false)
        {
            if (!useCollider)
            {
                if (overrideWidth == null) { overrideWidth = (int)entity.Width; }
                if (overrideHeight == null) { overrideHeight = (int)entity.Height; }
                return new Rectangle((int)entity.X, (int)entity.Y, overrideWidth.Value, overrideHeight.Value);
            }
            else
            {
                if (overrideWidth == null) { overrideWidth = (int)entity.Collider.Width; }
                if (overrideHeight == null) { overrideHeight = (int)entity.Collider.Height; }
                return new Rectangle((int)entity.Collider.AbsoluteLeft, (int)entity.Collider.AbsoluteTop, overrideWidth.Value, overrideHeight.Value);
            }
        }

        public static bool IsAttachedTo(this StaticMover staticMowerMain, Platform entity)
        {
            if (entity == staticMowerMain.Platform) return true;
            return false;
        }

        public static bool Match(this EntityID left, EntityID right)
        {
            return (left.Key == right.Key) && (left.ID == right.ID);
        }
    }
}
