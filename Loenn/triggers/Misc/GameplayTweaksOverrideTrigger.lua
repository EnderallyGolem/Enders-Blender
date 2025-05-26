local GameplayTweaksOverrideTrigger = {
    name = "EndHelper/GameplayTweaksOverrideTrigger",
    nodeLimits = {0, 0},
    placements = {
        {
            name = "normal",
            data = {
                preventDownDashRedirects = "Default",
                seemlessRespawn = "Default",
                --seemlessRespawnDelay = -1,
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
                {"Enabled - Normal", "EnabledNormal"},
                {"Enabled - Diagonal", "EnabledDiagonal"},
            }
        },
        seemlessRespawn = { fieldType = "string", editable = false,
            options = {
                {"Default", "Default"},
                {"Disabled", "Disabled"},
                {"Enabled - Normal", "EnabledNormal"},
                {"Enabled - Near Only", "EnabledNear"},
                {"Enabled - Instant", "EnabledInstant"},
                {"Enabled - Keep State", "EnabledKeepState"},
            }
        },
        --seemlessRespawnDelay = { fieldType = "integer", minimumValue = -1, maximumValue = 30 },
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "requireFlag", "setToDefaultUponLeaving", "activateEnterRoom",
        "preventDownDashRedirects", "seemlessRespawn", "seemlessRespawnDelay",
    },
}

return GameplayTweaksOverrideTrigger