local drawableSprite = require("structs.drawable_sprite")

local DeathHandlerThrowableRespawnPoint = {}

DeathHandlerThrowableRespawnPoint.name = "EndHelper/DeathHandlerThrowableRespawnPoint"
DeathHandlerThrowableRespawnPoint.depth = 100
DeathHandlerThrowableRespawnPoint.placements = {
    name = "normal",
    data = {
        fullReset = false,
        requireFlag = "",
        initialFaceLeft = false;
    },
}

-- Offset is from sprites.xml, not justifications
local offsetY = -10
local normalTexture = "objects/EndHelper/DeathHandlerThrowableRespawnPoint/throwablerespawnpoint_normal_active00"
local fullResetTexture = "objects/EndHelper/DeathHandlerThrowableRespawnPoint/throwablerespawnpoint_fullreset_active00"

function DeathHandlerThrowableRespawnPoint.sprite(room, entity)
    local textureToUse = normalTexture
    if entity.fullReset then
        textureToUse = fullResetTexture
    end

    local sprite = drawableSprite.fromTexture(textureToUse, entity)
    sprite.y = sprite.y + offsetY

    if entity.initialFaceLeft then
        sprite.scaleX = -1;
    end
    return sprite
end

function DeathHandlerThrowableRespawnPoint.onFlip(room, entity, horizontal, vertical)
    if horizontal then
        entity.initialFaceLeft = not entity.initialFaceLeft
    end
end
function DeathHandlerThrowableRespawnPoint.onRotate(room, entity, direction)
    entity.initialFaceLeft = not entity.initialFaceLeft
end

return DeathHandlerThrowableRespawnPoint