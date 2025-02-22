local CassetteManagerTrigger = {
    name = "EndHelper/CassetteManagerTrigger",
    nodeLimits = {0, 0},
    placements = {
        {
            name = "normal",
            data = {
                wonkyCassettes = false,
                showDebugInfo = false,
                multiplyTempoAtBeat = "",
                multiplyTempoExisting = false,
                multiplyTempoOnEnter = false,
                setBeatOnEnter = 99999,
                setBeatOnLeave = 99999,
                setBeatInside = 99999,
                setBeatOnlyIfAbove = -99999,
                setBeatOnlyIfUnder = 99999,
                doNotSetIfWithinRange = 0,
                addInsteadOfSet = false,
            }
        },
    },
    fieldInformation = {
        setBeatOnEnter = { fieldType = "integer"},
        setBeatOnLeave = { fieldType = "integer"},
        setBeatInside = { fieldType = "integer"},
        setBeatOnlyIfAbove = { fieldType = "integer"},
        setBeatOnlyIfUnder = { fieldType = "integer"},
        doNotSetIfWithinRange = { fieldType = "integer"},
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "multiplyTempoAtBeat",
        "setBeatOnEnter", "setBeatOnLeave", "setBeatInside", "setBeatOnlyIfAbove", "setBeatOnlyIfUnder", "doNotSetIfWithinRange",
        "multiplyTempoExisting", "multiplyTempoOnEnter", "addInsteadOfSet",
        "wonkyCassettes", "showDebugInfo",
    },
}

return CassetteManagerTrigger