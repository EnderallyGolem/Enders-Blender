local DeathHandlerDeathBypassTrigger = {
    name = "EndHelper/DeathHandlerDeathBypassTrigger",
    nodeLimits = {0, -1},
    nodeLineRenderType = "fan",
    placements = {
        {
            name = "normal",
            data = {
                requireFlag = "",
                showVisuals = true,
            }
        },
    },
    fieldInformation = {
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "requireFlag", "showVisuals"
    },
}

return DeathHandlerDeathBypassTrigger