using Celeste.Mod.EndHelper.Integration;
using IL.Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.EndHelperModule;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;

namespace Celeste.Mod.EndHelper.Utils
{
    static public class Utils_DeathHandler
    {
        internal static bool deathWipe = true;              // If false, skips wipe when dying.
        private static bool previousDeathWipe = true;
        internal static bool nextFastReload = false;        // If true, next reload will be forced to be fast (screen transition animation after dying)
        internal static SeemlessRespawnEnum seemlessRespawn = SeemlessRespawnEnum.Disabled;

        internal static bool playerHasDeathBypass = false; // Constantly false, unless when dying with bypass
                                                           // Then will be true until Hook_OnPlayerRespawn sets it to false.
        internal static bool manualReset = false; // Set true by SetNextRespawnFullReset.
                                                  // This bool itself doesn't do much other than a special-case with death bypass.

        internal static bool spinnerAltInView = false;
        internal static Rectangle oldCameraRectInflate;

        // For all reloads
        internal static int oldDashes;
        internal static float oldStamina;
        internal static Facings oldFacing;

        // For death reloads, if keep state
        internal static float oldDashAttackTimer;
        internal static float oldDashCooldownTimer;
        internal static Vector2 oldDashDir;
        internal static Vector2 oldSpeed;

        internal static int oldForceMoveX;
        internal static float oldForceMoveXTimer;

        internal static float oldVarJumpSpeed;
        internal static float oldVarJumpTimer;
        internal static bool oldAutoJump;
        internal static float oldAutoJumpTimer;
        internal static float oldWallSlideTimer;
        internal static int oldWallSlideDir;
        internal static bool oldLaunched;
        internal static float oldLaunchedTimer;

        // For player death bypass reloads
        internal static string oldbypass_requireFlag;
        internal static bool oldbypass_showVisuals;
        internal static int oldStateMachine;

        // Prevent death spam
        public static float deathCooldownFrames { private set; get; } = 0;

        #region Hook Functions
        

        public static void Update(Level level)
        {
            spinnerAltInView = false;
            if (deathCooldownFrames > 0 && !level.FrozenOrPaused)
            {
                deathCooldownFrames -= 1;
            }
        }

        private static int nextRespawnFullResetTrailTimer = 0;
        public static void PlayerUpdate(Player player)
        {
            // Afterglow for full reset
            if (EndHelperModule.Session.nextRespawnFullReset && EndHelperModule.Session.framesSinceRespawn > 3)
            {
                if (nextRespawnFullResetTrailTimer == 5)
                {
                    Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
                    TrailManager.Add(player, scale * 1.2f, Color.Red * 0.5f, 0.8f);
                    nextRespawnFullResetTrailTimer = 0;
                }
                else
                {
                    nextRespawnFullResetTrailTimer++;
                }
            }
        }

        public static bool getNextRespawnFullReset()
        {
            return EndHelperModule.Session.nextRespawnFullReset;
        }
        public static Vector2? getLastFullResetPos()
        {
            return EndHelperModule.Session.lastFullResetPos;
        }

        public static void EnableDeathHandlerEntityChecks()
        {
            if (!EndHelperModule.Session.AllowDeathHandlerEntityChecks)
            {
                // Init stuff
                DeathBypass.entityIDDisappearUntilFullReset = [];
            }
            EndHelperModule.Session.AllowDeathHandlerEntityChecks = true;
        }

        public static void ForceShortDeathCooldown()
        {
            // 5 frames is the minimum cooldown which prevents pauses
            if (deathCooldownFrames > 5) deathCooldownFrames = 5;
        }
        public static void ForceNoDeathCooldown() { deathCooldownFrames = 0; }

        public static void UpdateSeemlessRespawn()
        {
            seemlessRespawn = EndHelperModule.Settings.GameplayTweaksMenu.SeemlessRespawn;
            if (EndHelperModule.Session.overrideSeemlessRespawn != null)
            { seemlessRespawn = EndHelperModule.Session.overrideSeemlessRespawn.Value; }
            else if (seemlessRespawn != SeemlessRespawnEnum.Disabled) {
                EndHelperModule.Session.usedGameplayTweaks["seemlessrespawn_minor"] = true;
                if (seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState)
                {
                    EndHelperModule.Session.usedGameplayTweaks["seemlessrespawn_keepstate"] = true;
                }
            }
        }

        internal static void UpdateSeemlessRespawnOverride(String overrideSeemlessRespawnString)
        {
            EndHelperModule.Session.seemlessRespawnExceptFullReset = false;
            switch (overrideSeemlessRespawnString)
            {
                case "Default":
                    EndHelperModule.Session.overrideSeemlessRespawn = null;
                    break;
                case "Disabled":
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.Disabled;
                    break;
                case "EnabledNormal":
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledNormal;
                    break;
                case "EnabledNear":
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledNear;
                    break;
                case "EnabledInstant":
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledInstant;
                    break;
                case "EnabledKeepState":
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledKeepState;
                    break;
                case "EnabledExceptFullReset":
                    EndHelperModule.Session.seemlessRespawnExceptFullReset = true;
                    EndHelperModule.Session.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledNormal;
                    break;
                default:
                    EndHelperModule.Session.overrideSeemlessRespawn = null;
                    break;
            }
        }

        internal static void RetryButtonsManualCheck(Level level, TextMenu menu)
        {
            // Modify retry button
            int retryIndex = menu.Items.FindIndex(item =>
                item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_retry"));
            if (retryIndex != -1)
            {
                if (EndHelperModule.Session.AllowDeathHandlerEntityChecks && EndHelperModule.Session.framesSinceRespawn <= 2)
                {
                    menu.Items[retryIndex].Disabled = false; // Ensure that infinite death loops from Death Handler shenanigans are escapable from
                    menu.Items[retryIndex].Selectable = true;
                }
                menu.Items[retryIndex].OnPressed = OnRetryFunction + menu.Items[retryIndex].OnPressed; // Make sure OnRetryFunction comes FIRST
            }

            void OnRetryFunction()
            {
                SetManualReset(level);
            }
        }

        public static void BeforePlayerDeath(Player player)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"BeforePlayerDeath ran!");

            // Set seemless respawn details
            UpdateSeemlessRespawn();
            Level level = player.SceneAs<Level>();

            if (EndHelperModule.Session.AllowDeathHandlerEntityChecks)
            {
                // If level was paused when this happens and lastFullResetPos is not null, player retried from menu: count as manual reset

                if (player.Components.Get<DeathBypass>() is { } deathBypass && deathBypass.bypass)
                {
                    oldbypass_requireFlag = deathBypass.RequireFlag;
                    oldbypass_showVisuals = deathBypass.showVisuals;
                    oldStateMachine = player.StateMachine;
                    //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"Before player death state machine: {oldStateMachine}");
                    playerHasDeathBypass = true;
                }
                else playerHasDeathBypass = false;

                // If player fell out of the map, this counts as a full reset
                if (player.Top > (float)level.Bounds.Bottom) SetNextRespawnFullReset(level, true);

                // Global-ise DeathBypass entities temporarily. Prevent it from being loaded.
                if (!EndHelperModule.Session.nextRespawnFullReset)
                {
                    foreach (Entity entity in level.Entities)
                    {
                        if (entity.Components.Get<DeathBypass>() is { } deathBypassComponent && entity is not Player)
                        {
                            // Bypass component has to update, no matter if the entity itself is active
                            // Sometimes, eg in a move block, the entity might become inactive, but should still be affected by flag changes for example
                            deathBypassComponent.Update();
                            if (deathBypassComponent.bypass)
                            {
                                deathBypassComponent.BeforeDeathBypass(entity);
                            }
                        }
                    }
                    DeathBypass.BeforeDeathBypassID(level);
                }
            }

            // Update DeathWipe
            previousDeathWipe = deathWipe;
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"has deathbypass {playerHasDeathBypass} => no deathwipe. also nextrespawnfullreset {EndHelperModule.Session.nextRespawnFullReset}");
            if (playerHasDeathBypass && !level.Session.GrabbedGolden && !manualReset)
            {
                deathWipe = false;
                // TO-DO: If carrying X-death golden, don't deathwipe true.
            }
            else
            {
                switch (seemlessRespawn)
                {
                    case SeemlessRespawnEnum.Disabled:
                        deathWipe = true;
                        break;
                    case SeemlessRespawnEnum.EnabledNear:
                        Rectangle cameraRectVer = level.Camera.GetRect(128, 128);
                        Vector2 respawnPoint = level.Session.RespawnPoint.Value;
                        deathWipe = cameraRectVer.Contains((int)respawnPoint.X, (int)respawnPoint.Y) ? false : true;
                        break;
                    case SeemlessRespawnEnum.EnabledKeepState:
                        // TO-DO: If carrying X-death golden, don't deathwipe true.
                        deathWipe = false;
                        if (level.Session.GrabbedGolden)
                        {
                            deathWipe = true;
                        }
                        break;
                    default:
                        deathWipe = false;
                        if (EndHelperModule.Session.seemlessRespawnExceptFullReset && EndHelperModule.Session.nextRespawnFullReset)
                        {
                            deathWipe = true;
                        }
                        if (level.Session.GrabbedGolden)
                        {
                            deathWipe = true;
                        }
                        break;
                }
            }

            if (deathWipe && previousDeathWipe == false)
            {
                // If switch from no deathWipe to yes deathWipe, remove previous dead bodies if any
                foreach (Entity entity in level.Entities)
                {
                    if (entity is PlayerDeadBody playerDeadBody)
                    {
                        playerDeadBody.RemoveSelf();
                    }
                }
            } 
            if (!deathWipe)
            {
                // Set these if no death wipe
                oldDashes = player.Dashes;
                oldStamina = player.Stamina;
                oldFacing = player.Facing;

                oldDashAttackTimer = player.dashAttackTimer;
                oldDashCooldownTimer = player.dashCooldownTimer;
                oldDashDir = player.DashDir;
                oldSpeed = player.Speed;
                oldForceMoveX = player.forceMoveX;
                oldForceMoveXTimer = player.forceMoveXTimer;

                oldVarJumpSpeed = player.varJumpSpeed;
                oldVarJumpTimer = player.varJumpTimer;
                oldAutoJump = player.AutoJump;
                oldAutoJumpTimer = player.AutoJumpTimer;
                oldWallSlideTimer = player.wallSlideTimer;
                oldWallSlideDir = player.wallSlideDir;
                oldLaunched = player.launched;
                oldLaunchedTimer = player.launchedTimer;
            }
        }

        internal static void SetManualReset(Level level)
        {
            // Ran when dying from retry (or retry keybind).
            // This forces complete full reset, even with player bypass
            if (EndHelperModule.Session.AllowDeathHandlerEntityChecks)
            {
                SetNextRespawnFullReset(level, true, manualReset: true);
                ForceNoDeathCooldown(); // Only needed with AllowDeathHandlerEntityChecks to prevent softlock with full resets
            }
            manualReset = true;
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"manual reset. {EndHelperModule.Session.firstFullResetPos is not null}");
        }

        /// <summary>
        /// Moves the respawn position to the point closest to targetPos. Outputs true if successful change.
        /// </summary>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static bool UpdateRespawnPos(Vector2 targetPos, Level level, bool checkSolid, bool fullResetOnly = false)
        {
            bool changedRespawn = false;

            if (fullResetOnly && EndHelperModule.Session.lastFullResetPos is not null)
            {
                // For full reset, only target the lastFullRestPos, regardless of targetPos
                targetPos = EndHelperModule.Session.lastFullResetPos.Value;
                SetNextRespawnFullReset(level, true);
            }
            else
            {
                targetPos = level.GetSpawnPoint(targetPos);
                SetNextRespawnFullReset(level, false);
            }

            Session session = level.Session;
            if (NoInvalidCheck(level, targetPos, checkSolid) && (!session.RespawnPoint.HasValue || session.RespawnPoint.Value != targetPos))
            {
                session.HitCheckpoint = true;
                if (session.RespawnPoint != targetPos)
                {
                    changedRespawn = true;
                }
                session.RespawnPoint = targetPos;
                session.UpdateLevelStartDashes();
            }

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"Tried updating respawn point to {targetPos}. Success: {changedRespawn}");
            return changedRespawn;
        }

        /// Returns false for locations invalid for spawn points.
        public static bool NoInvalidCheck(Level level, Vector2 targetPos, bool checkInvalid = true)
        {
            if (!checkInvalid)
            {
                return true; // Avoid any checks for solid. Always return true (no solids), since they are already invalid by default.
            }

            Vector2 point = targetPos + Vector2.UnitY * -4f;

            if (level.CollideCheck<CrystalStaticSpinner>(point) || level.CollideCheck<DustStaticSpinner>(point) || level.CollideCheck<Spikes>(point) ||
                (integratingWithFrostHelper && FrostHelperIntegration.CheckCollisionWithCustomSpinners(level, point)))
            {
                return false;
            }
            if (level.CollideCheck<Solid>(point))
            {
                return level.CollideCheck<FloatySpaceBlock>(point);
            }
            foreach (Entity entity in level.Entities)
            {
                if (entity.CollidePoint(point) && entity.Components.Get<LedgeBlocker>() != null) return false;
            }

            return true;
        }
        public static bool NoInvalidCheck(Level level, Rectangle targetRect, bool checkInvalid = true, int inflate = 0)
        {
            if (!checkInvalid)
            {
                return true; // Avoid any checks for solid. Always return true (no solids), since they are already invalid by default.
            }

            targetRect.X += inflate;
            targetRect.Y += inflate;
            targetRect.Width += inflate * 2;
            targetRect.Height += inflate * 2;

            if (level.CollideCheck<CrystalStaticSpinner>(targetRect) || level.CollideCheck<DustStaticSpinner>(targetRect) || level.CollideCheck<Spikes>(targetRect) ||
                (integratingWithFrostHelper && FrostHelperIntegration.CheckCollisionWithCustomSpinners(level, targetRect)))
            {
                return false;
            }
            if (level.CollideCheck<Solid>(targetRect))
            {
                return level.CollideCheck<FloatySpaceBlock>(targetRect);
            }


            targetRect.Height += 2;
            foreach (Entity entity in level.Entities)
            {
                if (entity.CollideRect(targetRect) && entity.Components.Get<LedgeBlocker>() != null) return false;
            }

            return true;
        }

        public static void SetFullResetPos(Vector2? setPos, bool overrideFirstPos = false)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"try set {setPos} as full reset. first full reset pos is {EndHelperModule.Session.firstFullResetPos}");
            if (setPos is null) return;

            EndHelperModule.Session.lastFullResetPos = setPos.Value;
            if (EndHelperModule.Session.firstFullResetPos is null || overrideFirstPos)
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"{setPos} is the first full reset pos");
                EndHelperModule.Session.firstFullResetPos = setPos.Value;
            }
        }

        public static void SetNextRespawnFullReset(Level level, bool setTo, bool manualReset = false)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"Set Next Respawn Full Reset: setTo {setTo} | manualReset {manualReset}");
            if (setTo)
            {
                if (manualReset && EndHelperModule.Session.firstFullResetPos is not null)
                {
                    level.Session.RespawnPoint = EndHelperModule.Session.firstFullResetPos.Value;
                }
                else if (EndHelperModule.Session.lastFullResetPos is not null)
                {
                    level.Session.RespawnPoint = EndHelperModule.Session.lastFullResetPos.Value;
                }
                EndHelperModule.Session.nextRespawnFullReset = true;
            }
            else
            {
                EndHelperModule.Session.nextRespawnFullReset = false;
            }
        }

        // Aka on respawn
        public static void AfterPlayerDeath(Player player)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"AfterPlayerDeath ran!");
            Level level = player.SceneAs<Level>();

            if (!deathWipe)
            {
                // Set death cooldown (in frames), only if no deathwipe.
                // Longer cooldown if death bypass, though important stuff should ignore the cooldown.
                deathCooldownFrames = 20;
            }

            if (EndHelperModule.Session.AllowDeathHandlerEntityChecks)
            {
                // Deglobal-ise DeathBypass entities. Remove it from the DoNotLoad list.
                if (!EndHelperModule.Session.nextRespawnFullReset)
                {
                    foreach (Entity entity in level.Entities)
                    {
                        if (entity.Components.Get<DeathBypass>() is { } deathBypassComponent && deathBypassComponent.bypass && entity is not Player)
                        {
                            // Bypass flag should hopefully not have changed in this time. If it does it might lead to issues. But another bypass update isn't necessary.
                            deathBypassComponent.OnDeathBypass(entity);
                        }
                    }
                    DeathBypass.OnDeathBypassID(level);
                }
                else
                {
                    DeathBypass.ClearDeathBypassID(level);
                }

                playerHasDeathBypass = false;

                // Delay removing manual reset for a frame if using death-handler, in order to shift lastFullResetPos -> firstFullResetPos
                player.Add(new Coroutine(AfterPlayerDeathDelayed()));           
            }
            else
            {
                manualReset = false;
            }
        }

        public static IEnumerator AfterPlayerDeathDelayed()
        {
            yield return null;
            if (manualReset)
            {
                EndHelperModule.Session.lastFullResetPos = EndHelperModule.Session.firstFullResetPos;
                manualReset = false;
            }
            EndHelperModule.Session.nextRespawnFullReset = false;

            yield break;
        }


        internal static bool CheckPlayerNextFastReload()
        {
            bool fastReload = nextFastReload;
            nextFastReload = false; // Reset to false
            return fastReload;
        }
        internal static bool CheckPlayerDeathSkipRemovePlayer()
        {
            return !deathWipe;
        }
        internal static bool CheckPlayerDeathSkipLoseFollowers()
        {
            return seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || (playerHasDeathBypass && !manualReset && !EndHelperModule.Session.nextRespawnFullReset);
        }
        internal static void OnPlayerDeathSkipRemovePlayer(PlayerDeadBody playerDeadBody)
        {
            // I don't think there's any mechanism to remove dead bodies without the reload. So remove it manually!
            // This is 5 seconds after it would normally call for wipe.
            RemoveEntityAfterDelay(playerDeadBody, 5);
        }

        internal static async void RemoveEntityAfterDelay(Entity entity, float timeSeconds)
        {
            Level level = entity.SceneAs<Level>();

            int delayedTime = 0;
            while (delayedTime < timeSeconds)
            {
                await Task.Delay(1000);
                if (!level.FrozenOrPaused)
                {
                    delayedTime++;
                }
            }
            entity.RemoveSelf();
        }

        internal static void OnPlayerDeathSkip(Player player)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"OnPlayerDeathSkip ran: About to ReloadRoomSeemlesly.");
            Level level = player.SceneAs<Level>();
            ReloadRoomSeemlessly(level, ReloadRoomSeemlesslyEffect.Death);
        }

        #endregion

        #region Reload Logic

        public enum ReloadRoomSeemlesslyEffect { None, Wipe, Warp, Death }
        public static void ReloadRoomSeemlessly(Level level, ReloadRoomSeemlesslyEffect effect = ReloadRoomSeemlesslyEffect.None)
        {
            // Allow reload when paused if it's by player death - since death occurs when (un)pause(ing) when clicking retry
            if (level.Tracker.GetEntity<Player>() is { } player && (!level.InCutscene && !level.Paused || effect == ReloadRoomSeemlesslyEffect.Death))
            {
                bool playerDyingWithDeathBypass = false;
                if (effect == ReloadRoomSeemlesslyEffect.Death && playerHasDeathBypass && !manualReset) playerDyingWithDeathBypass = true;

                level.OnEndOfFrame += delegate
                {
                    LevelData leveldata = level.Session.LevelData;
                    oldCameraRectInflate = level.Camera.GetRect(16, 16);

                    // Get pre-reload info
                    Vector2 oldPlayerPos = player.Position;
                    Vector2 cameraOffset = level.CameraOffset;
                    Vector2 oldCameraPos = level.Camera.Position;
                    float oldCameraAngle = level.Camera.angle;
                    Vector2 oldCameraAnchor = player.CameraAnchor;
                    Vector2 oldCameraAnchorLerp = player.CameraAnchorLerp;
                    bool oldCameraAnchorIgnoreX = player.CameraAnchorIgnoreX;
                    bool oldCameraAnchorIgnoreY = player.CameraAnchorIgnoreY;
                    level.Session.Level = leveldata.Name;

                    bool transferFollowersBackLater = false;

                    if (effect != ReloadRoomSeemlesslyEffect.Death || seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || playerDyingWithDeathBypass)
                    {
                        oldDashes = player.Dashes;
                        oldStamina = player.Stamina;
                        oldFacing = player.Facing;

                        // Global-ise followers temporarily
                        if ( !(playerDyingWithDeathBypass && EndHelperModule.Session.nextRespawnFullReset) )
                        {
                            Leader leader = player.Get<Leader>();
                            foreach (Follower follower in leader.Followers)
                            {
                                if (follower.Entity != null)
                                {
                                    try
                                    {
                                        EntityID parentID = follower.ParentEntityID;
                                        if (follower.Entity is Strawberry)
                                        {
                                            level.Session.DoNotLoad.Add(parentID);
                                            follower.Entity.AddTag(Tags.Global); // Strawberries need global while keys (and hopefully everything else) can't have it. No idea why.
                                        }
                                        transferFollowersBackLater = true;
                                    }
                                    catch (Exception)
                                    {
                                        EntityID parentID = follower.ParentEntityID;
                                    }
                                }
                            }
                        }
                    }

                    // Globalise PlayerDeadBody
                    foreach (Entity entity in level.Entities)
                    {
                        if (entity is PlayerDeadBody playerDeadBody)
                        {
                            playerDeadBody.AddTag(Tags.Global);
                        }
                    }

                    // Reload
                    level.Remove(player);
                    level.UnloadLevel();
                    spinnerAltInView = true;

                    if (effect == ReloadRoomSeemlesslyEffect.Death)
                    {
                        // Specifically from dying

                        // First, if deathbypass respawning at same location, set spawnpoint at current player location.
                        // This prevents the player from starting at a different location when level is loaded (before tping back instantly)
                        //if (playerDyingWithDeathBypass && !EndHelperModule.Session.nextRespawnFullReset) level.Session.RespawnPoint = oldPlayerPos;

                        // edit: i added that code above ages ago and it causes issues when dying with deathbypass then reverting to not-bypass,
                        // the respawn location will be where you just died. idk why i did this, i don't understand that issue, wtf????

                        level.LoadLevel(Player.IntroTypes.Respawn);
                        level.Wipe = null;

                        if (level.Tracker.GetEntity<Player>() is { } respawnPlayer)
                        {
                            if (seemlessRespawn == SeemlessRespawnEnum.EnabledInstant || seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || playerDyingWithDeathBypass)
                            {
                                //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"the current nextrespawnfullreset is {EndHelperModule.Session.nextRespawnFullReset}");

                                // Only do smoothing (and very fast) if near
                                if (playerDyingWithDeathBypass && !EndHelperModule.Session.nextRespawnFullReset)
                                {
                                    level.Camera.Position = oldCameraPos;
                                    respawnPlayer.Position = oldPlayerPos;
                                }
                                else
                                {
                                    //Vector2 respawnPoint = level.Session.RespawnPoint.Value;
                                    //bool doCameraShift = Vector2.Distance(respawnPoint, oldPlayerPos) < 300;
                                    //if (doCameraShift)
                                    //{
                                    //    Vector2 respawnCameraPos = level.Camera.Position;
                                    //    level.Camera.Position = oldCameraPos;
                                    //    SeemlessRespawnCamera(level, respawnPlayer, oldCameraPos, respawnCameraPos, 0.3f);
                                    //}
                                    Vector2 respawnCameraPos = level.Camera.Position;
                                    level.Camera.Position = oldCameraPos;
                                    SeemlessRespawnCamera(level, respawnPlayer, oldCameraPos, respawnCameraPos, 0.3f);
                                }

                                if (seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || playerDyingWithDeathBypass)
                                {
                                    respawnPlayer.Dashes = oldDashes;
                                    respawnPlayer.Stamina = oldStamina;
                                    respawnPlayer.Facing = oldFacing;
                                    level.CameraOffset = cameraOffset;

                                    if (playerDyingWithDeathBypass && !EndHelperModule.Session.nextRespawnFullReset)
                                    {
                                        respawnPlayer.CameraAnchor = oldCameraAnchor;
                                        respawnPlayer.CameraAnchorLerp = oldCameraAnchorLerp;
                                        respawnPlayer.CameraAnchorIgnoreX = oldCameraAnchorIgnoreX;
                                        respawnPlayer.CameraAnchorIgnoreY = oldCameraAnchorIgnoreY;
                                        level.Camera.angle = oldCameraAngle;
                                    }

                                    respawnPlayer.dashAttackTimer = oldDashAttackTimer;
                                    respawnPlayer.dashCooldownTimer = oldDashCooldownTimer;
                                    respawnPlayer.DashDir = oldDashDir;
                                    respawnPlayer.Speed = oldSpeed;
                                    respawnPlayer.forceMoveX = oldForceMoveX;
                                    respawnPlayer.forceMoveXTimer = oldForceMoveXTimer;

                                    respawnPlayer.varJumpSpeed = oldVarJumpSpeed;
                                    respawnPlayer.varJumpTimer = oldVarJumpTimer;
                                    respawnPlayer.AutoJump = oldAutoJump;
                                    respawnPlayer.AutoJumpTimer = oldAutoJumpTimer;
                                    respawnPlayer.wallSlideTimer = oldWallSlideTimer;
                                    respawnPlayer.wallSlideDir = oldWallSlideDir;
                                    respawnPlayer.launched = oldLaunched;
                                    respawnPlayer.launchedTimer = oldLaunchedTimer;

                                    if (playerDyingWithDeathBypass)
                                    {
                                        //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"after player ded: {oldStateMachine}");
                                        // Allow for "dream jumps" if dying from: Dash + Green Booster / Red booster / Dream block / Bird fling (+ tutorial bird fling) / Feather
                                        if (oldStateMachine == 2 | oldStateMachine == 5 || oldStateMachine == 9 || oldStateMachine == 24 || oldStateMachine == 16 | oldStateMachine == 19)
                                        {
                                            respawnPlayer.jumpGraceTimer = 0.1f;
                                            respawnPlayer.dreamJump = true;
                                            respawnPlayer.dashCooldownTimer = 0;

                                            // Add momentum boost for feather
                                            if (oldStateMachine == 19)
                                            {
                                                int addMomentum = 50;
                                                if (respawnPlayer.Facing == Facings.Left) addMomentum *= -1;
                                                if (Input.Feather.Value.X != 0)
                                                {
                                                    respawnPlayer.Speed.X += addMomentum;
                                                }
                                            }
                                        }
                                        DeathBypass deathBypass;
                                        respawnPlayer.Add(deathBypass = new DeathBypass(oldbypass_requireFlag, oldbypass_showVisuals));
                                        deathBypass.Update();
                                    }

                                    if (seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState || (playerDyingWithDeathBypass && !EndHelperModule.Session.nextRespawnFullReset))
                                    {
                                        Leader.StoreStrawberries(player.Get<Leader>());
                                        Leader.RestoreStrawberries(respawnPlayer.Get<Leader>());
                                    }
                                }
                            }
                            else
                            {
                                // Normal camera smoothing
                                Vector2 respawnCameraPos = level.Camera.Position;
                                level.Camera.Position = oldCameraPos;
                                SeemlessRespawnCamera(level, respawnPlayer, oldCameraPos, respawnCameraPos, 0.5f);
                            }
                        }
                    }
                    else
                    {
                        // Restore pre-reload info
                        level.Add(player);
                        level.LoadLevel(Player.IntroTypes.Transition);
                    }

                    if (effect != ReloadRoomSeemlesslyEffect.Death)
                    {
                        player.Dashes = oldDashes;
                        player.Stamina = oldStamina;
                        player.Facing = oldFacing;
                        level.CameraOffset = cameraOffset;
                    }

                    if (transferFollowersBackLater)
                    {
                        RestorePlayerFollowersOnRespawn(level, player, oldPlayerPos);
                    }

                    // Make spinners appear properly
                    foreach (CrystalStaticSpinner spinner in level.Tracker.GetEntities<CrystalStaticSpinner>())
                    {
                        if (oldCameraRectInflate.Contains((int)(spinner.Position.X + spinner.Width/2), (int)(spinner.Position.Y + spinner.Height/2)))
                        {
                            if (spinner.color == CrystalColor.Rainbow)
                            {
                                spinner.UpdateHue();
                            }
                            spinner.ForceInstantiate();
                        }
                    }
                    foreach (DustStaticSpinner spinner in level.Tracker.GetEntities<DustStaticSpinner>())
                    {
                        if (oldCameraRectInflate.Contains((int)(spinner.Position.X + spinner.Width / 2), (int)(spinner.Position.Y + spinner.Height / 2)))
                        {
                            spinner.ForceInstantiate();
                        }
                    }

                    // Effects
                    switch (effect)
                    {
                        case ReloadRoomSeemlesslyEffect.None:
                            break;
                        case ReloadRoomSeemlesslyEffect.Wipe:
                            level.DoScreenWipe(wipeIn: true);
                            break;
                        case ReloadRoomSeemlesslyEffect.Warp:
                            level.Flash(Color.Gray * 0.4f);
                            level.Displacement.AddBurst(oldPlayerPos, 0.1f, 12f, 60f);
                            level.Displacement.AddBurst(oldPlayerPos, 0.3f, 6f, 30f);
                            level.Displacement.AddBurst(player.Position, 0.1f, 60f, 12f);
                            level.Displacement.AddBurst(player.Position, 0.3f, 30f, 6f);
                            break;
                        case ReloadRoomSeemlesslyEffect.Death:
                            break;
                    }
                };
            }
        }

        static void SeemlessRespawnCamera(Level level, Player player, Vector2 initialPos, Vector2 finalPos, float timeSeconds)
        {
            if (player is null)
            {
                return;
            }

            Vector2 currentCameraPos = initialPos;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineInOut, timeSeconds, start: true);
            tween.OnUpdate = delegate (Tween t) 
            {
                currentCameraPos = Vector2.Lerp(initialPos, finalPos, t.Eased);
                level.Camera.Position = currentCameraPos;
            };
            player.Add(tween);
        }

        static void RestorePlayerFollowersOnRespawn(Level level, Player oldPlayer, Vector2 oldPlayerPos)
        {
            Leader oldLeader = oldPlayer.Get<Leader>();

            Player respawnPlayer = level.Tracker.GetEntity<Player>();
            Leader respawnLeader = respawnPlayer.Get<Leader>();

            // Transfer old player followers to the new player
            oldLeader.TransferFollowers();

            // Shift positions
            respawnLeader.PastPoints = oldLeader.PastPoints;
            for (int i = 0; i < respawnLeader.PastPoints.Count; i++)
            {
                respawnLeader.PastPoints[i] += respawnPlayer.Position - oldPlayerPos;
            }

            // Unglobalise followers
            foreach (Follower follower2 in respawnLeader.Followers)
            {
                if (follower2.Entity != null)
                {
                    try
                    {
                        follower2.Entity.Position += oldPlayer.Position - oldPlayerPos;
                        if (follower2.Entity is Strawberry)
                        {
                            level.Session.DoNotLoad.Remove(follower2.ParentEntityID);
                            follower2.Entity.RemoveTag(Tags.Global);
                        }
                    }
                    catch { }
                }
            }
        }
        #endregion
    }


    #region Components

    // Component for entities to have Death Bypass
    public class DeathBypass(String requireFlag = "", bool showVisuals = true, EntityID? id = null, bool isAttached = false, bool initialAllowBypass = true, bool preventChange = false) : Component(true, true)
    {
        public static List<EntityID> entityIDDisappearUntilFullReset = new List<EntityID>();

        public bool bypass { get; internal set; }
        public bool showVisuals { get; internal set; } = showVisuals;
        internal EntityID entityID;

        public bool allowBypass { get; internal set; } = initialAllowBypass; // Modified by Zone

        internal string RequireFlag { get; private set; } = requireFlag;
        public readonly bool preventChange = preventChange;
        private readonly bool isAttached = isAttached;

        public List<Entity> subEntitiesList = [];
        
        public override void Added(Entity entity)
        {
            Utils_DeathHandler.EnableDeathHandlerEntityChecks();

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"Attempt to apply Death Bypass to entity: {entity} {entity.Position} {entity.SourceId}");
            Level level = entity.SceneAs<Level>();

            // Add id
            if (id != null)
            {
                entityID = id.Value;
            }
            else if (entity.SourceData is not null || entity is Player)
            {
                entityID = entity.SourceId;
            }
            else
            {
                // No ID found. End.
                Active = false;
                //RemoveSelf(); // why doesn't this work lol
                entity.Remove(this);
                return;
            }

            // Special entity considerations - Subentities (entities that create more entities)
            if (entity.Get<DeathBypassModifier>() is { } modifier &&
                modifier.subEntityList != null && modifier.subEntityList.Count > 0)
            {
                foreach (Entity subEntity in modifier.subEntityList) AddSubentity(subEntity);
            }
            else if (entity is ZipMover zipmover) AddSubentity(zipmover.pathRenderer);
            else if (entity is SwapBlock swapBlock) AddSubentity(swapBlock.path);
            else if (entity is MoveBlock moveBlock) AddSubentity(moveBlock.border);
            else if (entity is CrystalStaticSpinner crystalStaticSpinner) AddSubentity(crystalStaticSpinner.border);
            else if (entity is Booster booster) AddSubentity(booster.outline);
            //else if (entity is DustStaticSpinner dustStaticSpinner) AddSubentity(dustStaticSpinner.Sprite.eyes); // This umm doesn't work. I am not adding hooks just for this.

            // Static mowers
            if (entity is Solid solid)
            {
                foreach (StaticMover component in level.Tracker.GetComponents<StaticMover>())
                {
                    if (component.Platform != null && component.IsAttachedTo(solid))
                    {
                        Entity staticMowerEntity = component.Entity;
                        if (staticMowerEntity.Components.Get<DeathBypass>() is null)
                        {
                            staticMowerEntity.Add(new DeathBypass(RequireFlag, showVisuals, id: staticMowerEntity.SourceId, isAttached: true));
                        }
                    }
                }
            }
            base.Added(entity);
        }

        // Ran by DeathBypass entities before player dies.
        private bool deathBypassPreviousGlobal = false;
        private bool previouslyInDoNotLoadList = false;
        internal void BeforeDeathBypass(Entity entity)
        {
            if (entity.Get<DeathBypassModifier>() is { } modifier && modifier.beforeDeathBypassAction != null)
            {
                modifier.beforeDeathBypassAction.Invoke();
            }

            Level level = entity.SceneAs<Level>();
            try
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"{entity}: retainBypass. do not add. {entityID}");
                previouslyInDoNotLoadList = level.Session.DoNotLoad.Contains(entityID);
                if (!previouslyInDoNotLoadList) level.Session.DoNotLoad.Add(entityID);

                deathBypassPreviousGlobal = entity.TagCheck(Tags.Global);
                if (!deathBypassPreviousGlobal) entity.AddTag(Tags.Global);
            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Warn, "EndHelper/Utils_DeathHandler", $"{entity}: Failed to death-bypass. Removing entity!");
                RemoveSelf();
            }
        }

        static internal void BeforeDeathBypassID(Level level)
        {
            foreach (EntityID entityID in entityIDDisappearUntilFullReset)
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"BeforeDeathBypassID >> Id: {entityID} {entityID.Key == null} {entityID.Key == ""}");
                if (entityID.Key != null)
                {
                    level.Session.DoNotLoad.Add(entityID);
                }
                else
                {
                    entityIDDisappearUntilFullReset.Remove(entityID);
                }
            }
        }

        // Ran by DeathBypass entities after player dies. Also does special things to specific entities.
        internal void OnDeathBypass(Entity entity)
        {
            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"OnDeathBypass: {entity} {entity.Position}. add back! id: {entityID}");
            Level level = entity.SceneAs<Level>();

            // Remove global tag / doNotLoad list only if it didn't already have it previously
            if (!deathBypassPreviousGlobal) entity.RemoveTag(Tags.Global);
            if (!previouslyInDoNotLoadList) level.Session.DoNotLoad.Remove(entityID);

            entity.Scene = Engine.Scene as Level;

            // Entity itself
            if (entity.Get<DeathBypassModifier>() is { } modifier && modifier.onDeathBypassAction != null)
            {
                modifier.onDeathBypassAction.Invoke();
            }

            if (entity is Solid solid)
            {
                foreach (StaticMover component in level.Tracker.GetComponents<StaticMover>())
                {
                    if (component.Platform != null && component.IsAttachedTo(solid))
                    {
                        Entity staticMowerEntity = component.Entity;
                        staticMowerEntity.Scene = Engine.Scene as Level;
                    }
                }
            }
        }

        static internal void OnDeathBypassID(Level level)
        {
            foreach (EntityID entityID in entityIDDisappearUntilFullReset)
            {
                //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"OnDeathBypassID >> Id: {entityID}");
                if (entityID.Key != null)
                {
                    level.Session.DoNotLoad.Remove(entityID);
                }
                else
                {
                    entityIDDisappearUntilFullReset.Remove(entityID);
                }
            }
        }

        static public void ClearDeathBypassID(Level level)
        {
            OnDeathBypassID(level);
            entityIDDisappearUntilFullReset = [];
        }

        public override void Update()
        {
            Level level = SceneAs<Level>();

            bypass = Utils_General.AreFlagsEnabled(level.Session, RequireFlag, true) && allowBypass;
            base.Update();
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Removed(Entity entity)
        {
            // Remove from staticmowers too
            Level level = entity.SceneAs<Level>();

            RemoveSubentities();

            // Static Movers
            if (entity is Solid solid)
            {
                foreach (StaticMover component in level.Tracker.GetComponents<StaticMover>())
                {
                    if (component.Platform != null && component.IsAttachedTo(solid))
                    {
                        Entity staticMowerEntity = component.Entity;
                        if (staticMowerEntity.Components.Get<DeathBypass>() is { } staticDeathBypass)
                        {
                            entity.Remove(staticDeathBypass);
                        }
                    }
                }
            }
            base.Removed(entity);
        }

        public void ToggleAllowBypass(Entity entity, bool? setValue, String newRequireFlag = null)
        {
            if (isAttached) return; // Do not toggle if this entity is a staticmover attacked to something, unless set otherwise

            Level level = entity.SceneAs<Level>();

            // null means toggle. Otherwise will be set to that value.
            if (setValue is null)
            {
                setValue = !allowBypass;
            }
            allowBypass = setValue.Value;
            if (newRequireFlag != null) RequireFlag = newRequireFlag;

            ToggleBypassSubentities(setValue.Value); // Special entity considerations

            // Static mowers
            if (entity is Solid solid)
            {
                foreach (StaticMover component in level.Tracker.GetComponents<StaticMover>())
                {
                    if (component.Platform != null && component.IsAttachedTo(solid))
                    {
                        Entity staticMowerEntity = component.Entity;
                        if (staticMowerEntity.Components.Get<DeathBypass>() is { } staticDeathBypass)
                        {
                            staticDeathBypass.ToggleAllowBypassAttached(staticMowerEntity, setValue);
                        }
                    }
                }
            }
        }

        public void ToggleAllowBypassAttached(Entity entity, bool? setValue, String newRequireFlag = null)
        {
            Level level = entity.SceneAs<Level>();

            // null means toggle. Otherwise will be set to that value.
            if (setValue is null)
            {
                setValue = !allowBypass;
            }
            allowBypass = setValue.Value;

            if (newRequireFlag != null) RequireFlag = newRequireFlag;
        }

        // Deal with subentities
        public void AddSubentity(Entity subEntity)
        {
            subEntitiesList.Add(subEntity);
            if (subEntity is not null)
            {
                subEntity.Add(new DeathBypass(RequireFlag, showVisuals, entityID, isAttached: true));
            }
        }
        public void RemoveSubentities()
        {
            while (subEntitiesList.Count > 0)
            {
                if (subEntitiesList[0] is not null && subEntitiesList[0].Components.Get<DeathBypass>() is { } deathBypass) 
                {
                    subEntitiesList[0].Remove(deathBypass);
                }
                subEntitiesList.RemoveAt(0);
            }
        }
        public void ToggleBypassSubentities(bool setValue, String newRequireFlag = null)
        {
            foreach (Entity subentity in subEntitiesList)
            {
                if (subentity is not null && subentity.Components.Get<DeathBypass>() is { } deathBypass)
                {
                    deathBypass.ToggleAllowBypassAttached(subentity, setValue, newRequireFlag);
                }
            }
        }
    }

    /// <summary>
    /// Modifies the effect of DeathBypass
    /// </summary>
    public class DeathBypassModifier(List<Entity>? subEntityList = null, Action? beforeDeathBypassAction = null, Action? onDeathBypassAction = null) : Component(true, true)
    {
        public List<Entity>? subEntityList { get; } = subEntityList;
        public Action? beforeDeathBypassAction { get; } = beforeDeathBypassAction;
        public Action? onDeathBypassAction { get; } = onDeathBypassAction;
    }

    #endregion
}
