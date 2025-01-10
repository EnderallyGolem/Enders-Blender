using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.EndHelper.Triggers.RoomSwap;

[CustomEntity("EndHelper/RoomSwapRespawnForceSameRoomTrigger")]
public class RoomSwapRespawnForceSameRoomTrigger : Trigger
{

    private bool onAwake = true;
    private Vector2? respawnOffset = null;
    private Vector2 roomOffset;
    private EntityData entityData = null;

    public RoomSwapRespawnForceSameRoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        onAwake = data.Bool("onAwake", true);
        entityData = data;
        roomOffset = offset;
    }

    public override void OnEnter(Player player)
    {
        if (!onAwake)
        {
            doTheRespawnPositionChangeThingy();
        }
    }

    public override void OnStay(Player player)
    {

    }

    public override void OnLeave(Player player)
    {

    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (onAwake)
        {
            doTheRespawnPositionChangeThingy();
        }
    }

    private void doTheRespawnPositionChangeThingy()
    {
        Level level = SceneAs<Level>();

        if (respawnOffset == null)
        {
            //Get random spawnpoint
            Vector2 randomSpawnPos = level.Session.LevelData.Spawns[0];

            //Check all rooms one by one
            foreach (LevelData levelData in level.Session.MapData.Levels)
            {
                //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionChangeRespawnTrigger", $"Checking if spawnpoint at {randomSpawnPos.X} {randomSpawnPos.Y} are from {levelData.Name}");
                //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionChangeRespawnTrigger", $"level pos: {levelData.Position.X} {levelData.Position.Y}. Size: {levelData.Bounds.Width} {levelData.Bounds.Height}");
                if (levelData.Position.X < randomSpawnPos.X && levelData.Position.Y < randomSpawnPos.Y &&
                    levelData.Position.X + levelData.Bounds.Width > randomSpawnPos.X &&
                    levelData.Position.Y + levelData.Bounds.Height > randomSpawnPos.Y)
                {
                    respawnOffset = roomOffset - levelData.Position;
                    //Logger.Log(LogLevel.Info, "EndHelper/RoomSwap/TransitionChangeRespawnTrigger", $"Yes! Updating respawnOffset to {respawnOffset.Value.X} {respawnOffset.Value.Y}");
                    break;
                }
            }
        }

        for (int i = 0; i < level.Session.LevelData.Spawns.Count; i++)
        {
            Vector2 respawnOffsetV = respawnOffset.Value;
            level.Session.LevelData.Spawns[i] += respawnOffsetV;
        }

        Vector2? defaultSpawnPos = level.Session.LevelData.DefaultSpawn;
        if (defaultSpawnPos.HasValue)
        {
            Vector2 defaultSpawnPosV = defaultSpawnPos.Value;
            Vector2 respawnOffsetV = respawnOffset.Value;
            level.Session.LevelData.DefaultSpawn += respawnOffsetV;
        }
        //do thing with here if not null
    }
}