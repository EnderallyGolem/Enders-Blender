local drawableSprite = require("structs.drawable_sprite")
local celesteEnums = require("consts.celeste_enums")
local utils = require("utils")

local DeathCountGate = {}

local textures = {
    default = "objects/door/TempleDoor00",
    mirror = "objects/door/TempleDoorB00",
    theo = "objects/door/TempleDoorC00"
}

local textureOptions = {}

for texture, _ in pairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

DeathCountGate.name = "EndHelper/DeathCountGate"
DeathCountGate.depth = -9000
DeathCountGate.canResize = {false, false}

DeathCountGate.placements = {
    name = "normal",
    alternativeName = {"altname1"},
    placementType = "point",
    data = {
        height = 48,
        sprite = "default",
        deathLimit = 10,
        comparison = "below_or_equal",
        deathCountType = "room_permanent",
        setFlag = "",
        silent = false,
    }
}

DeathCountGate.fieldInformation = {
    sprite = {
        options = textureOptions,
        editable = false
    },
    deathLimit = { fieldType = "integer", minimumValue = 0 },
    comparison = { fieldType = "string", editable = false,
        options = {
            {"<", "below"},
            {"≤", "below_or_equal"},
            {"=", "equal"},
            {"≥", "above_or_equal"},
            {">", "above"},
        }
    },
    deathCountType = { fieldType = "string", editable = false,
        options = {
            {"Map-Wide", "map"},
            {"Room - Permanent", "room_permanent"},
            {"Room - Reset on Full Reset", "room_fullreset"},
            {"Room - Reset on Transition", "room_transition"},
            {"Room - Reset on Transition / Retry", "room_transition_retry"},
        }
    },
}

function DeathCountGate.sprite(room, entity)
    local variant = entity.sprite or "default"
    local texture = textures[variant] or textures["default"]
    local sprite = drawableSprite.fromTexture(texture, entity)
    local height = entity.height or 48

    -- Weird offset from the code, justifications are from sprites.xml
    sprite:setJustification(0.5, 0.0)
    sprite:addPosition(4, height - 48)

    return sprite
end

return DeathCountGate