local CassetteManagerTrigger = {
    name = "EndHelper/CassetteManagerTrigger",
    nodeLimits = {0, 0},
    placements = {
        {
            name = "normal",
            data = {
                wonkyCassettes = false,
                showDebugInfo = false,
                multiplyTempoEnterRoom = "",
                multiplyTempoOnEnter = "",
                multiplyTempoInside = "",
                multiplyTempoOnLeave = "",
                multiplyTempoExisting = false,
                setBeatEnterRoom = 99999,
                setBeatOnEnter = 99999,
                setBeatOnLeave = 99999,
                setBeatInside = 99999,
                setBeatOnlyIfAbove = -99999,
                setBeatOnlyIfUnder = 99999,
                doNotSetIfWithinRange = 0,
                addInsteadOfSet = false,
                removeImmediately = false,
                setBeatResetCassettePos = true,
                requireFlag = "",
            }
        },
    },
    fieldInformation = {
        setBeatEnterRoom = { fieldType = "integer"},
        setBeatOnEnter = { fieldType = "integer"},
        setBeatOnLeave = { fieldType = "integer"},
        setBeatInside = { fieldType = "integer"},
        setBeatOnlyIfAbove = { fieldType = "integer"},
        setBeatOnlyIfUnder = { fieldType = "integer"},
        doNotSetIfWithinRange = { fieldType = "integer"},
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "requireFlag",
        "multiplyTempoEnterRoom", "multiplyTempoOnEnter", "multiplyTempoInside", "multiplyTempoOnLeave", "multiplyTempoExisting", "",
        "setBeatEnterRoom", "setBeatOnEnter", "setBeatOnLeave", "setBeatInside", "setBeatOnlyIfAbove", "setBeatOnlyIfUnder", "doNotSetIfWithinRange",
        "addInsteadOfSet",
        "wonkyCassettes", "showDebugInfo",
    },
}

return CassetteManagerTrigger