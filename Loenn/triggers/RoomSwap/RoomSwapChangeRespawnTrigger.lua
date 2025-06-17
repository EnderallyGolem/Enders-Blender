local roomSwapChangeRespawnTrigger = {
    name = "EndHelper/RoomSwapChangeRespawnTrigger",
    nodeLimits = {0, 1},
    placements = {
        {
            name = "normal",
            alternativeName = {"altname", "altname2"},
            data = {
                checkSolid = true
            },
        },
    }
}

return roomSwapChangeRespawnTrigger