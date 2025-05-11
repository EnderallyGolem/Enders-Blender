using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.EndHelper.EndHelperModuleSettings.GameplayTweaks;

namespace Celeste.Mod.EndHelper.Utils
{
    internal class Utils_DeathHandler
    {
        internal static bool deathWipe = true;              // If false, skips wipe when dying.
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
        internal static float oldDashCooldownTimer ;
        internal static Vector2 oldDashDir;
        internal static Vector2 oldSpeed;

        public static void Update()
        {
            spinnerAltInView = false;
        }

        public static void UpdateSeemlessRespawn()
        {
            seemlessRespawn = EndHelperModule.Settings.GameplayTweaksMenu.SeemlessRespawn;
            if (overrideSeemlessRespawn != null)
            { seemlessRespawn = overrideSeemlessRespawn.Value; }
            else if (seemlessRespawn != SeemlessRespawnEnum.Disabled) { EndHelperModule.Session.usedGameplayTweaks = true; }
        }

        internal static void UpdateSeemlessRespawnOverride(String overrideSeemlessRespawnString)
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

            if (deathWipe)
            {
                // Remove all existing dead bodies - otherwise THESE will lead to level reload
                foreach (PlayerDeadBody playerDeadBody in level.Tracker.GetEntities<PlayerDeadBody>())
                {
                    playerDeadBody.RemoveSelf();
                }
            }

            oldDashes = player.Dashes;
            oldStamina = player.Stamina;
            oldFacing = player.Facing;

            oldDashAttackTimer = player.dashAttackTimer;
            oldDashCooldownTimer = player.dashCooldownTimer;
            oldDashDir = player.DashDir;
            oldSpeed = player.Speed;

            // Global-ise DeathBypass entities temporarily. Update deathHandlerEntityIDList
            EndHelperModule.Session.deathHandlerEntityIDList.Clear();
            foreach (Entity entity in level.Entities)
            {
                if (entity.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass)
                {
                    deathBypassComponent.originalGlobalTag = entity.TagCheck(Tags.Global);
                    entity.AddTag(Tags.Global);

                    EndHelperModule.Session.deathHandlerEntityIDList.Add(deathBypassComponent.deathBypassID);
                    level.Session.DoNotLoad.Add(new EntityID("test", 4));
                }
            }
            Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"aft globalised - list length {EndHelperModule.Session.deathHandlerEntityIDList.Count}");
        }

        // Aka on respawn
        public static void AfterPlayerDeath(Player player)
        {
            Level level = player.SceneAs<Level>();

            // Deglobal-ise DeathBypass entities
            foreach (Entity entity in level.Entities)
            {
                if (entity.Components.Get<DeathBypass>() is DeathBypass deathBypassComponent && deathBypassComponent.bypass)
                {
                    if (deathBypassComponent.originalGlobalTag)
                    {
                        entity.RemoveTag(Tags.Global);
                    }
                }
            }
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
            // This is 2 seconds after it would normally call for wipe.
            RemoveEntityAfterDelay(playerDeadBody, 2);
        }

        internal static async void RemoveEntityAfterDelay(Entity entity, float timeSeconds)
        {
            await Task.Delay( (int)(timeSeconds * 1000) );
            entity.RemoveSelf();
        }

        internal static void OnPlayerDeathSkip(Player player)
        {
            Level level = player.SceneAs<Level>();
            ReloadRoomSeemlessly(level, ReloadRoomSeemlesslyEffect.Death);
        }

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
                    // Globalise PlayerDeadBody
                    foreach (PlayerDeadBody playerDeadBody in level.Tracker.GetEntities<PlayerDeadBody>())
                    {
                        playerDeadBody.AddTag(Tags.Global);
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

    #region Components

    // Component for entities to have Death Bypass
    public class DeathBypass(String requireFlag = "", bool showVisuals = true) : Component(true, true)
    {
        internal bool bypass;
        public bool originalGlobalTag = false;
        internal string deathBypassID;

        private bool allowBypass = true;
        private string requireFlag = requireFlag;
        public override void Added(Entity entity)
        {
            originalGlobalTag = entity.TagCheck(Tags.Global);

            //// It'll be REALLY nice if i can get the entity id easily... TO-DO in the future!
            //deathBypassID = $"{entity}_{entity.X}_{entity.Y}_{entity.Width}_{entity.Height}_{entity.Depth}";


            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"list length {EndHelperModule.Session.deathHandlerEntityIDList.Count}. Compare with {deathBypassID}");
            //if (EndHelperModule.Session.deathHandlerEntityIDList.Contains(deathBypassID))
            //{
            //    allowBypass = false;
            //    entity.Active = false;
            //    entity.Visible = false;
            //    entity.Collidable = false;
            //    Active = false;
            //    Visible = false;
            //    Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"prevented: {entity} from death bypass please gtfo");
            //    entity.RemoveSelf();
            //}
            base.Added(entity);

            DynamicData entityData = DynamicData.For(entity);
            entityData.Set("ID", new EntityID("test", 4));

            //Logger.Log(LogLevel.Info, "EndHelper/Utils_DeathHandler", $"Added entity to death bypass: {entity} with id {deathBypassID}");
        }

        public override void Update()
        {
            Level level = SceneAs<Level>();

            bypass = Utils_General.AreFlagsEnabled(level.Session, requireFlag, true) && allowBypass;
            base.Update();
        }
        
        // TO-DO: golden glint visuals if showVisuals is true. Shader learning time!
    }

    #endregion
}
