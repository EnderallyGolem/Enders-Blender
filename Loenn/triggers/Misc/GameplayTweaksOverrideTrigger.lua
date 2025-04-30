local GameplayTweaksOverrideTrigger = {
    name = "EndHelper/GameplayTweaksOverrideTrigger",
    nodeLimits = {0, 0},
    placements = {
        {
            name = "normal",
            data = {
                preventDownDashRedirects = "Default",
                setToDefaultUponLeaving = false,
                activateEnterRoom = false,
                requireFlag = "",
            }
        },
    },
    fieldInformation = {
        preventDownDashRedirects = { fieldType = "string", editable = false,
            options = {
                {"Default", "Default"},
                {"Disabled", "Disabled"},
                {"Enabled (Normal)", "EnabledNormal"},
                {"Enabled (Diagonal)", "EnabledDiagonal"},
            }
        },
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "preventDownDashRedirects",
        "requireFlag", "setToDefaultUponLeaving", "activateEnterRoom",
    },
}

return GameplayTweaksOverrideTrigger