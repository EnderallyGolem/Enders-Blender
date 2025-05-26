using Celeste.Mod.EndHelper.Entities.DeathHandler;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.EndHelperModule;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;

namespace Celeste.Mod.EndHelper.Utils
{
    internal class Utils_DeathHandler
    {
        internal static bool deathWipe = true;              // If false, skips wipe when dying.
        private static bool previousDeathWipe = true;
        internal static bool nextFastReload = false;        // If true, next reload will be forced to be fast
        internal static SeemlessRespawnEnum seemlessRespawn = SeemlessRespawnEnum.Disabled;
        private static SeemlessRespawnEnum? overrideSeemlessRespawn = null;

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

        // Prevent death spam
        internal static float deathCooldownFrames = 0;

        // This is null whenever screen transition occurs. When entering full reset zone, set spawn here.
        public static Vector2? lastFullResetPos = null;

        #region Hook Functions
        public static void Update()
        {
            spinnerAltInView = false;
            if (deathCooldownFrames > 0)
            {
                deathCooldownFrames -= 1;
            }
        }

        public static void UpdateSeemlessRespawn()
        {
            seemlessRespawn = EndHelperModule.Settings.GameplayTweaksMenu.SeemlessRespawn;
            if (overrideSeemlessRespawn != null)
            { seemlessRespawn = overrideSeemlessRespawn.Value; }
            else if (seemlessRespawn != SeemlessRespawnEnum.Disabled) { EndHelperModule.Session.usedGameplayTweaks = true; }
        }

        internal static void UpdateSeemlessRespawnOverride(String overrideSeemlessRespawnString, int overrideSeemlessRespawnDelay)
        {
            switch (overrideSeemlessRespawnString)
            {
                case "Default":
                    Utils_DeathHandler.overrideSeemlessRespawn = null;
                    break;
                case "Disabled":
                    Utils_DeathHandler.overrideSeemlessRespawn = SeemlessRespawnEnum.Disabled;
                    break;
                case "EnabledNormal":
                    Utils_DeathHandler.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledNormal;
                    break;
                case "EnabledNear":
                    Utils_DeathHandler.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledNear;
                    break;
                case "EnabledInstant":
                    Utils_DeathHandler.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledInstant;
                    break;
                case "EnabledKeepState":
                    Utils_DeathHandler.overrideSeemlessRespawn = SeemlessRespawnEnum.EnabledKeepState;
                    break;
                default:
                    Utils_DeathHandler.overrideSeemlessRespawn = null;
                    break;
            }
        }

        public static void BeforePlayerDeath(Player player)
        {
            // Set seemless respawn details
            UpdateSeemlessRespawn();
            Level level = player.SceneAs<Level>();

            previousDeathWipe = deathWipe;
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
                    if (level.Session.GrabbedGolden)
                    {
                        deathWipe = true;
                    }
                    break;
                    // TO-DO: For all of these other than EnabledKeepState, for X-death golden i still need to keep the golden somehow
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
            }

            // Global-ise DeathBypass entities temporarily. Prevent it from being loaded.
            foreach (Entity entity in level.Entities)
            {
                if (entity.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent)
                {
                    // Bypass component has to update, no matter if the entity itself is active
                    // Sometimes, eg in a move block, the entity might become inactive, but should still be affected by flag changes for example
                    deathBypassComponent.Update();
                    if (deathBypassComponent.bypass)
                    {
                        entity.AddTag(Tags.Global);
                        level.Session.DoNotLoad.Add(deathBypassComponent.entityID);
                    }
                }
            }
        }

        // Aka on respawn
        public static void AfterPlayerDeath(Player player)
        {
            Level level = player.SceneAs<Level>();

            // Deglobal-ise DeathBypass entities. Remove it from the DoNotLoad list.
            foreach (Entity entity in level.Entities)
            {
                if (entity.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass)
                {
                    // Bypass flag should hopefully not have changed in this time. If it does it might lead to issues. But another bypass update isn't necessary.
                    entity.RemoveTag(Tags.Global);
                    level.Session.DoNotLoad.Remove(deathBypassComponent.entityID);
                    deathBypassComponent.OnDeathBypass(entity);
                }
            }

            if (!deathWipe)
            {
                Utils_DeathHandler.deathCooldownFrames = 20; // Set death cooldown (in frames), only if no deathwipe
            }
        }

        public static void OnRoomTransition(Level level)
        {
            lastFullResetPos = null;
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
            return seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState;
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
            Level level = player.SceneAs<Level>();
            ReloadRoomSeemlessly(level, ReloadRoomSeemlesslyEffect.Death);
        }

        internal static void ReplaceTransitionRoutineGetSpawnpointWithTheActualFunction()
        {
            // Returning the actual function should always be better
            // but since this is a little jank, just in case, we'll add a DeathHanderRespawnPoint check
            if (Engine.Scene is Level level && level.Tracker.GetEntity<DeathHandlerRespawnPoint>() is not null)
            {
                Player player = level.Tracker.GetEntity<Player>();
                Vector2 to = player.CollideFirst<RespawnTargetTrigger>()?.Target ?? player.Position;
                level.Session.RespawnPoint = level.Session.GetSpawnPoint(to);
            }
        }
        #endregion

        #region Reload Logic

        public enum ReloadRoomSeemlesslyEffect { None, Wipe, Warp, Death }
        public static void ReloadRoomSeemlessly(Level level, ReloadRoomSeemlesslyEffect effect = ReloadRoomSeemlesslyEffect.None)
        {
            if (level.Tracker.GetEntity<Player>() is Player player && !level.InCutscene && !level.Paused)
            {
                level.OnEndOfFrame += delegate
                {
                    LevelData leveldata = level.Session.LevelData;
                    oldCameraRectInflate = level.Camera.GetRect(16, 16);

                    // Get pre-reload info
                    Vector2 oldPlayerPos = player.Position;
                    Vector2 cameraOffset = level.CameraOffset;
                    Vector2 oldCameraPos = level.Camera.Position;
                    level.Session.Level = leveldata.Name;



                    if (effect != ReloadRoomSeemlesslyEffect.Death)
                    {
                        oldDashes = player.Dashes;
                        oldStamina = player.Stamina;
                        oldFacing = player.Facing;
                    }

                    if (effect != ReloadRoomSeemlesslyEffect.Death || seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState)
                    {
                        oldDashes = player.Dashes;
                        oldStamina = player.Stamina;
                        oldFacing = player.Facing;

                        // Global-ise followers temporarily
                        Leader leader = player.Get<Leader>();
                        foreach (Follower follower in leader.Followers)
                        {
                            if (follower.Entity != null)
                            {
                                try
                                {
                                    EntityID parentID = follower.ParentEntityID;
                                    level.Session.DoNotLoad.Add(parentID);
                                    follower.Entity.AddTag(Tags.Global);
                                }
                                catch (Exception)
                                {
                                    EntityID parentID = follower.ParentEntityID;
                                    Logger.Log(LogLevel.Warn, "EndHelper/Utils_DeathHandler", $"{follower} (parentID: {parentID}) couldn't be properly added to Session DoNotLoad");
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
                        level.LoadLevel(Player.IntroTypes.Respawn);
                        level.Wipe = null;

                        if (seemlessRespawn == SeemlessRespawnEnum.EnabledInstant || seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState)
                        {
                            // Only do smoothing (and very fast) if near
                            Vector2 respawnPoint = level.Session.RespawnPoint.Value;
                            bool doCameraShift = Vector2.Distance(respawnPoint, oldPlayerPos) < 300;
                            Player respawnPlayer = level.Tracker.GetEntity<Player>();
                            if (doCameraShift)
                            {
                                Vector2 respawnCameraPos = level.Camera.Position;
                                level.Camera.Position = oldCameraPos;
                                SeemlessRespawnCamera(level, respawnPlayer, oldCameraPos, respawnCameraPos, 0.3f);
                            }

                            if (seemlessRespawn == SeemlessRespawnEnum.EnabledKeepState)
                            {
                                respawnPlayer.Dashes = oldDashes;
                                respawnPlayer.Stamina = oldStamina;
                                respawnPlayer.Facing = oldFacing;
                                level.CameraOffset = cameraOffset;

                                respawnPlayer.dashAttackTimer = oldDashAttackTimer;
                                respawnPlayer.dashCooldownTimer = oldDashCooldownTimer;
                                respawnPlayer.DashDir = oldDashDir;
                                respawnPlayer.Speed = oldSpeed;

                                Leader.StoreStrawberries(player.Get<Leader>());
                                Leader.RestoreStrawberries(respawnPlayer.Get<Leader>());
                            }
                        } 
                        else
                        {
                            // Normal camera smoothing
                            Vector2 respawnCameraPos = level.Camera.Position;
                            level.Camera.Position = oldCameraPos;
                            Player respawnPlayer = level.Tracker.GetEntity<Player>();
                            SeemlessRespawnCamera(level, respawnPlayer, oldCameraPos, respawnCameraPos, 0.5f);
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
                        RestorePlayerFollowersOnRespawn(level, player, oldPlayerPos);
                    }

                    // Make spinners appear properly
                    foreach (CrystalStaticSpinner spinner in level.Tracker.GetEntities<CrystalStaticSpinner>())
                    {
                        if (oldCameraRectInflate.Contains((int)(spinner.Position.X + spinner.Width/2), (int)(spinner.Position.Y + spinner.Height/2)))
                        {
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
            Vector2 currentCameraPos = initialPos;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineInOut, timeSeconds, start: true);
            tween.OnUpdate = delegate (Tween t) 
            {
                currentCameraPos = Vector2.Lerp(initialPos, finalPos, t.Eased);
                level.Camera.Position = currentCameraPos;
            };
            player.Add(tween);
        }

        static void RestorePlayerFollowersOnRespawn(Level level, Player player, Vector2 oldPlayerPos)
        {
            Leader leader = player.Get<Leader>();

            // Unglobalise followers
            foreach (Follower follower2 in leader.Followers)
            {
                if (follower2.Entity != null)
                {
                    try
                    {
                        level.Session.DoNotLoad.Remove(follower2.ParentEntityID);
                        follower2.Entity.Position += player.Position - oldPlayerPos;
                        follower2.Entity.RemoveTag(Tags.Global);
                    }
                    catch { }
                }
            }
            for (int i = 0; i < leader.PastPoints.Count; i++)
            {
                leader.PastPoints[i] += player.Position - oldPlayerPos;
            }
            leader.TransferFollowers();
        }
    }

    #endregion

    #region Components

    // Component for entities to have Death Bypass
    public class DeathBypass(String requireFlag = "", bool showVisuals = true, EntityID? id = null) : Component(true, true)
    {
        internal bool bypass;
        internal bool showVisuals = showVisuals;
        internal EntityID entityID;

        private readonly bool allowBypass = true;
        private readonly string requireFlag = requireFlag;
        
        public override void Added(Entity entity)
        {
            Level level = entity.SceneAs<Level>();

            // Add id
            if (id != null)
            {
                entityID = id.Value;
            }
            else if (entity.Components.Get<AccessibleID>() is AccessibleID accessibleID)
            {
                entityID = accessibleID.entityID;
            }
            else
            {
                // No ID found. End.
                Active = false;
                //RemoveSelf(); // why doesn't this work lol
                entity.Remove(this);
                return;
            }

            // Special entity considerations
            if (entity is ZipMover zipmover)
            {
                zipmover.pathRenderer.Add(new DeathBypass(requireFlag, showVisuals, entityID));
            }

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"deathbypass: added entity {entity}");

            // Static mowers
            if (entity is Solid solid)
            {
                foreach (StaticMover component in level.Tracker.GetComponents<StaticMover>())
                {
                    if (component.Platform != null && component.IsRiding(solid))
                    {
                        Entity staticMowerEntity = component.Entity;
                        if (staticMowerEntity.Components.Get<DeathBypass>() is null)
                        {
                            if (staticMowerEntity is DeathHandlerRespawnPoint respawnPoint)
                            {
                                respawnPoint.Add(new DeathBypass(requireFlag, showVisuals, respawnPoint.entityID));
                            }
                            else
                            {
                                staticMowerEntity.Add(new DeathBypass(requireFlag, showVisuals));
                            }
                        }
                    }
                }
            }
            base.Added(entity);
        }

        internal void OnDeathBypass(Entity entity)
        {
            Level level = entity.SceneAs<Level>();
            if (entity is Booster booster)
            {
                level.Add(booster.outline);
            }
        }

        public override void Update()
        {
            Level level = SceneAs<Level>();

            bypass = Utils_General.AreFlagsEnabled(level.Session, requireFlag, true) && allowBypass;
            base.Update();
        }

        public override void Render()
        {
            base.Render();
        }
    }

    #endregion
}
