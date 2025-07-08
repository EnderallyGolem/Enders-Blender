local IncrementalFlagTrigger = {
    name = "EndHelper/IncrementalFlagTrigger",
    nodeLimits = {0, 0},
    placements = {
        {
            name = "normal",
            data = {
                flag = "",
                setValue = 1,
                setOnlyIfOneBelow = true,
                requireFlag = "",
                singleUse = true,
                temporary = true,
            }
        },
    },
    fieldInformation = {
        setValue = { fieldType = "integer", minimumValue = 0 },
    },
    fieldOrder = {
        "x", "y", "height", "width", "editorLayer",
        "flag", "singleUse", "temporary",
        "requireFlag",
        "incrementAmount", "incrementOnlyIfOneBelow",
    },
}

return IncrementalFlagTrigger