local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")
local defaultTexture = "objects/EndHelper/CassetteBeatBlock/YellowBeatBlock"
local moverTexture = "objects/EndHelper/CassetteBeatBlock/MoverBeatBlock"
local cassetteBeatGate = {}

cassetteBeatGate.name = "EndHelper/CassetteBeatGate"
cassetteBeatGate.depth = 0
cassetteBeatGate.nodeLimits = {0, -1}
cassetteBeatGate.nodeLineRenderType = "line"
cassetteBeatGate.minimumSize = {8, 8}
cassetteBeatGate.warnBelowSize = {16, 16}
cassetteBeatGate.placements = {}
cassetteBeatGate.placements[1] = {
    name = "blue",
    data = {
        width = 16,
        height = 16,
        moveTime = 0.3,
        easing = "SineInOut",
        texturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/CassetteBeatBlock/BlueBeatBlock",
        moveSound = "event:/classic/sfx15",

        moveLoopBeat = "",
        moveCycleBeat = "0|0,8|1,16|2,24|3",
        firstNode = 0,
        surfaceSoundIndex = 32,
        changeInsteadOfSet = false,
        loopNodes = true,
        entityMover = false,
        entityMoverPlatformOnly = false,

        particleColour1 = "68a1ee",
        particleColour2 = "2d47f7",
        requireFlag = "",
    }
}
cassetteBeatGate.placements[2] = {
    name = "red",
    data = {
        width = 16,
        height = 16,
        moveTime = 0.3,
        easing = "SineInOut",
        texturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/CassetteBeatBlock/RedBeatBlock",
        moveSound = "event:/classic/sfx15",

        moveLoopBeat = "",
        moveCycleBeat = "0|0,8|1,16|2,24|3",
        firstNode = 0,
        surfaceSoundIndex = 32,
        changeInsteadOfSet = false,
        loopNodes = true,
        entityMover = false,
        entityMoverPlatformOnly = false,

        particleColour1 = "fb859b",
        particleColour2 = "f42121",
        requireFlag = "",
    }
}
cassetteBeatGate.placements[3] = {
    name = "yellow",
    data = {
        width = 16,
        height = 16,
        moveTime = 0.3,
        easing = "SineInOut",
        texturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/CassetteBeatBlock/YellowBeatBlock",
        moveSound = "event:/classic/sfx15",

        moveLoopBeat = "",
        moveCycleBeat = "0|0,8|1,16|2,24|3",
        firstNode = 0,
        surfaceSoundIndex = 32,
        changeInsteadOfSet = false,
        loopNodes = true,
        entityMover = false,
        entityMoverPlatformOnly = false,

        particleColour1 = "ffeb6b",
        particleColour2 = "d39332",
        requireFlag = "",
    }
}
cassetteBeatGate.placements[4] = {
    name = "green",
    data = {
        width = 16,
        height = 16,
        moveTime = 0.3,
        easing = "SineInOut",
        texturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/CassetteBeatBlock/GreenBeatBlock",
        moveSound = "event:/classic/sfx15",

        moveLoopBeat = "",
        moveCycleBeat = "0|0,8|1,16|2,24|3",
        firstNode = 0,
        surfaceSoundIndex = 32,
        changeInsteadOfSet = false,
        loopNodes = true,
        entityMover = false,
        entityMoverPlatformOnly = false,

        particleColour1 = "a9f5b4",
        particleColour2 = "1fcc2a",
        requireFlag = "",
    }
}

local easeTypes = {
    "Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "BackIn", "BackOut", "BackInOut", "ExpoIn", "ExpoOut", "ExpoInOut", "BigBackIn", "BigBackOut", "BigBackInOut", "ElasticIn", "ElasticOut", "ElasticInOut", "BounceIn", "BounceOut", "BounceInOut"
}

cassetteBeatGate.fieldInformation = {
    texturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
    easing = {
        options = easeTypes,
        editable = false
    },
    firstNode = { fieldType = "integer", minimumValue = 0 },
    surfaceSoundIndex = { fieldType = "integer", minimumValue = -1 },
    blockColour = {fieldType = "color"},
    particleColour1 = {fieldType = "color"},
    particleColour2 = {fieldType = "color"},
}

cassetteBeatGate.fieldOrder = {
    "x", "y", "height", "width", "editorLayer",
    "moveCycleBeat", "moveLoopBeat", "firstNode", "moveTime", "easing",
    "texturePath", "particleColour1", "particleColour2",
    "surfaceSoundIndex", "moveSound", "requireFlag",
    "changeInsteadOfSet", "loopNodes", "entityMover", "entityMoverPlatformOnly"
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

function cassetteBeatGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local color = entity.blockColour or "FFFFFF"

    local frame = miscFuncs.trimPath(entity.texturePath, defaultTexture)
    if entity.entityMover then
        frame = moverTexture
    end

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    local sprites = ninePatch:getDrawableSprite()

    return sprites
end


function cassetteBeatGate.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local nodes = entity.nodes or {}
    local nodeRectangles = {}

    for i, node in ipairs(nodes) do
        local rectangle = utils.rectangle(node.x or x, node.y or y, width, height)
        table.insert(nodeRectangles, rectangle)
    end

    return utils.rectangle(x, y, width, height), nodeRectangles
end


return cassetteBeatGate