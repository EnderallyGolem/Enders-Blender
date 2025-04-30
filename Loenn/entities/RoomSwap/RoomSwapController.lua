local roomSwapController = {
    name = "EndHelper/RoomSwapController",
    depth = -8500,
    texture = "objects/EndHelper/roomSwapController/idle00",
    placements = {
        {
            name = "normal",
            data = {
                gridId = "1",

                totalRows = 0,
                totalColumns = 0,
                swapRoomNamePrefix = "swap",
                templateRoomNamePrefix = "template",
                roomTransitionTime = 0.3,
                activateSoundEvent1 = "event:/game/04_cliffside/snowball_impact",
                activateSoundEvent2 = "event:/game/05_mirror_temple/seeker_death"
            },
        },
    },
    fieldInformation = {
        totalRows = { fieldType = "integer", minimumValue = 0 },
        totalColumns = { fieldType = "integer", minimumValue = 0 }
    }
}

return roomSwapController