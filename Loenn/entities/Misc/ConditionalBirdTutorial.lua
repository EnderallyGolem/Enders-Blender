local enums = require("consts.celeste_enums")
local ConditionalBirdTutorial = {}

ConditionalBirdTutorial.name = "EndHelper/ConditionalBirdTutorial"
ConditionalBirdTutorial.depth = -1000000
ConditionalBirdTutorial.justification = {0.5, 1.0}
ConditionalBirdTutorial.nodeLineRenderType = "line"
ConditionalBirdTutorial.nodeVisibility = "always"
ConditionalBirdTutorial.texture = "characters/bird/crow00"
-- ConditionalBirdTutorial.nodeTexture = "objects/EndHelper/multiroomWatchtower/node"
ConditionalBirdTutorial.nodeLimits = {2, 2}

ConditionalBirdTutorial.placements = {
    name = "normal",
    data = {
        faceLeft = true,
        birdId = "",
        onlyOnce = false,
        caw = true,
        info = "TUTORIAL_DREAMJUMP",
        controls = "DownRight,+,Dash,tinyarrow,Jump",

        flyInSpeedMultiplier = 1,
        showSprite = true,
        onlyOnceFlyIn = true,
        onlyFulfillConditionOnce = true,

        secInZoneTotal = 0,
        secInZoneAtOnce = 0,
        secInRoom = 0,
        deathsInZone = 0,
        deathsInRoom = 0,
        requireOnScreen = true,
        requireFlagForIncrement = "",
        requireFlag = ""
    }
}

ConditionalBirdTutorial.fieldOrder = {
    "x", "y", "editorLayer",
    "birdId", "controls", "info", "caw", "faceLeft", "onlyOnce", "onlyOnceFlyIn", "onlyFulfillConditionOnce", "showSprite",
    "flyInSpeedMultiplier",
    "secInZoneTotal", "secInZoneAtOnce", "secInRoom", "deathsInZone", "deathsInRoom",
    "requireFlag", "requireFlagForIncrement", "requireOnScreen"
}

ConditionalBirdTutorial.fieldInformation = {
    info = {
        options = enums.everest_bird_tutorial_tutorials
    },
    flyInSpeedMultiplier = { fieldType = "number", minimumValue = 0 },
    secInZoneTotal = { fieldType = "number", minimumValue = 0 },
    secInZoneAtOnce = { fieldType = "number", minimumValue = 0 },
    secInRoom = { fieldType = "number", minimumValue = 0 },
    deathsInZone = { fieldType = "integer", minimumValue = 0 },
    deathsInRoom = { fieldType = "integer", minimumValue = 0 },
}

function ConditionalBirdTutorial.scale(room, entity)
    return entity.faceLeft and -1 or 1, 1
end

function ConditionalBirdTutorial.nodeTexture(room, entity, node, nodeIndex, viewport)
    if (nodeIndex == 1) then
        return "objects/EndHelper/ConditionalBirdTutorial/node_topleft"
    else
        return "objects/EndHelper/ConditionalBirdTutorial/node_bottomright"
    end
end

function ConditionalBirdTutorial.onFlip(room, entity, horizontal, vertical)
    if horizontal then
        entity.faceLeft = not entity.faceLeft
    end
end
function ConditionalBirdTutorial.onRotate(room, entity, direction)
    entity.faceLeft = not entity.faceLeft
end

return ConditionalBirdTutorial