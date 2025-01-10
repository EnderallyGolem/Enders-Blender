local roomSwapRespawnForceSameRoomTrigger = {
    name = "EndHelper/RoomSwapRespawnForceSameRoomTrigger",
    placements = {
        {
            name = "normal",
            data = {
                onAwake = true
            },
        },
    },
    fieldInformation = {
        onAwake = { fieldType = "boolean"}
    }
}

return roomSwapRespawnForceSameRoomTrigger