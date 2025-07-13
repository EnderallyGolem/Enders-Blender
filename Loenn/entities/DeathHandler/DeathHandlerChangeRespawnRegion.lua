local DeathHandlerChangeRespawnRegion = {
    name = "EndHelper/DeathHandlerChangeRespawnRegion",
    depth = 0,
    nodeLimits = {0, 1},
    nodeLineRenderType = "line",
    nodeVisibility = "selected",
    placements = {
        {
            name = "normal",
            data = {
                width = 8,
                height = 8,
                checkSolid = true,
                attachable = true,
                fullReset = false,
                killOnEnter = false,

                visibleArea = true,
                visibleTarget = true,
            },
        },
    },
    fieldInformation = {
        speed = { fieldType = "number", minimumValue = 0 },
    }
}

function DeathHandlerChangeRespawnRegion.fillColor(room, entity)

    local transparency = 0.8;
    if entity.visibleArea == false then
        transparency = 0.4;
    end

    if entity.fullReset then
        return {0.4, 0, 0, transparency};
    else
        return {0, 0.4, 0, transparency};
    end
end

function DeathHandlerChangeRespawnRegion.borderColor(room, entity)

    local redMultiplier = 1
    if entity.killOnEnter then
        redMultiplier = 3
    end

    if entity.attachable then
       return {0.1 * redMultiplier, 0.1, 0.1, 0.9};
    else
        return {0.2 * redMultiplier, 0.2, 0.2, 0.3};
    end
end

return DeathHandlerChangeRespawnRegion