local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")
local defaultMainTexture = "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/empty_loenn"
local defaultBorderTexture = "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/mural_border_none"

local activateOverlay = "objects/EndHelper/DeathHandlerBypassZone/activeOverlay"
local deactivateOverlay = "objects/EndHelper/DeathHandlerBypassZone/deactiveOverlay"
local toggleOverlay = "objects/EndHelper/DeathHandlerBypassZone/toggleOverlay"

local DeathHandlerBypassZone = {}

DeathHandlerBypassZone.name = "EndHelper/DeathHandlerBypassZone"
DeathHandlerBypassZone.depth = 9500
DeathHandlerBypassZone.canResize = {false, false}
DeathHandlerBypassZone.justification = {0, 0}
DeathHandlerBypassZone.placements = {
    name = "normal",
    data = {
        mainTexturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/mural_placeholder",
        borderTexturePath = "Graphics/Atlases/Gameplay/objects/EndHelper/DeathHandlerBypassZone/mural_border",

        effect = "Activate",
        altEffect = "Dectivate",
        altFlag = "",

        attachable = true,

        bypassFlag = "",
        affectPlayer = true,
    }
}

DeathHandlerBypassZone.fieldInformation = {
    mainTexturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
    borderTexturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
    effect = { fieldType = "string", editable = false,
        options = {
            {"Activate", "Activate"},
            {"Deactivate", "Deactivate"},
            {"Toggle", "Toggle"},
            {"None", "None"},
        }
    },
    altEffect = { fieldType = "string", editable = false,
        options = {
            {"Activate", "Activate"},
            {"Deactivate", "Deactivate"},
            {"Toggle", "Toggle"},
            {"None", "None"},
        }
    }
}

DeathHandlerBypassZone.fieldOrder = {
    "x", "y", "editorLayer",
    "mainTexturePath", "borderTexturePath",
    "effect", "altEffect", "altFlag",
    "attachable", "bypassFlag", "affectPlayer"
}
DeathHandlerBypassZone.ignoredFields = {"_name", "_id", "originX", "originY", "height", "width"}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

function DeathHandlerBypassZone.sprite(room, entity)
    local sprites = {};
    local mainTexturePath = miscFuncs.trimPath(entity.mainTexturePath, defaultMainTexture);
    local mainSprite = drawableSprite.fromTexture(mainTexturePath, entity);
    mainSprite:setJustification(0, 0);

    local mainRect = mainSprite:getRectangle();
    entity.width = mainRect.width;
    entity.height = mainRect.height;

    local x, y = entity.x or 0, entity.y or 0;
    local width, height = entity.width or 24, entity.height or 24;

    local frame = miscFuncs.trimPath(entity.borderTexturePath, defaultBorderTexture);
    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height);

    table.insert(sprites, mainSprite)
    table.insert(sprites, ninePatch)

    if (entity.effect == "Activate") then
        local activateNinePatch = drawableNinePatch.fromTexture(activateOverlay, ninePatchOptions, x, y, width, height);
        table.insert(sprites, activateNinePatch)
    elseif (entity.effect == "Deactivate") then
        local deactivateNinePatch = drawableNinePatch.fromTexture(deactivateOverlay, ninePatchOptions, x, y, width, height);
        table.insert(sprites, deactivateNinePatch)
    elseif (entity.effect == "Toggle") then
        local toggleNinePatch = drawableNinePatch.fromTexture(toggleOverlay, ninePatchOptions, x, y, width, height);
        table.insert(sprites, toggleNinePatch)
    end

    return sprites
end


return DeathHandlerBypassZone