local DeathHandlerRespawnMarker = {
    name = "EndHelper/DeathHandlerRespawnMarker",
    depth = -8500,
    justification = {0.5, 0.5},
    offset = {0, 1},
    texture = "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_marker",
    placements = {
        {
            name = "normal",
            data = {
                speed = 1.0,
                requireFlag = "",
                offscreenPointer = true,
            },
        },
    },
    fieldInformation = {
        speed = { fieldType = "number", minimumValue = 0 },
    }
}
return DeathHandlerRespawnMarker