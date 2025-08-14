local utils = require("utils")
local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")
local drawableSprite = require("structs.drawable_sprite")
local connectedEntities = require("helpers.connected_entities")

local defaultTexture = "objects/EndHelper/Misc/outline_filled"

local ConnectableOutline = {
    minimumSize = {16, 16},
    nodeLimits = {0, 1},
    name = "EndHelper/ConnectableOutline",
    texture = defaultTexture,
    nodeLineRenderType = "line",
    nodeVisibility = "selected",
    placements = {
        {
            name = "normal",
            data = {
                width = 16,
                height = 16,
                colour = "ffffffff",
                connectLayer = 0,
                depth = 10000,
                attachable = false,
                visibleFlag = "",
                texturePath = defaultTexture,
            }
        }
    },
    fieldInformation = {
        texturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
        connectLayer = { fieldType = "integer", minimumValue = -1 },
        colour = { fieldType = "color", useAlpha = true },
    }
}
ConnectableOutline.fieldOrder = {
    "x", "y", "width", "height",
}

ConnectableOutline.depth = function(room,entity) return entity.depth or 10000 end

-- Filter by cassette blocks sharing the same index
local function getSearchPredicate(entity)
    return function(target)
        return entity._name == target._name and entity.connectLayer == target.connectLayer and entity.connectLayer ~= -1
    end
end

local function getTileSprite(entity, x, y, frame, color, depth, rectangles)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = false, false

    if completelyClosed then
        if not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 0

        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 8

        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 16

        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 24

        else
            quadX, quadY = 8, 8
        end
    else
        if closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 8, 0

        elseif closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 8, 16

        elseif closedLeft and not closedRight and closedUp and closedDown then
            quadX, quadY = 16, 8

        elseif not closedLeft and closedRight and closedUp and closedDown then
            quadX, quadY = 0, 8

        elseif closedLeft and not closedRight and not closedUp and closedDown then
            quadX, quadY = 16, 0

        elseif not closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 0, 0

        elseif not closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 0, 16

        elseif closedLeft and not closedRight and closedUp and not closedDown then
            quadX, quadY = 16, 16
        end
    end

    if quadX and quadY then
        local sprite = drawableSprite.fromTexture(frame, entity)

        sprite:addPosition(drawX, drawY)
        sprite:useRelativeQuad(quadX, quadY, 8, 8)
        sprite:setColor(color)

        sprite.depth = depth

        return sprite
    end
end

function ConnectableOutline.sprite(room, entity)
    local relevantBlocks = utils.filter(getSearchPredicate(entity), room.entities)

    connectedEntities.appendIfMissing(relevantBlocks, entity)

    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local sprites = {}

    local width, height = entity.width or 32, entity.height or 32
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local frame = miscFuncs.trimPath(entity.texturePath, defaultTexture)

    for x = 1, tileWidth do
        for y = 1, tileHeight do
            local sprite = getTileSprite(entity, x, y, frame, entity.color, entity.depth, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end


    if sprites ~= nil then
        local color = utils.getColor(entity.colour or {1, 1, 1, 1})
        for i = 1, #sprites do
            sprites[i]:setColor(color)
        end
        return sprites
    end

    return sprites
end

function ConnectableOutline.onRotate(room, entity, direction)
    local oldWidth = entity.width
    entity.width = entity.height
    entity.height = oldWidth
end

return ConnectableOutline