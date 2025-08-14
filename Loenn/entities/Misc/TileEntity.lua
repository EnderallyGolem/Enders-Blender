local fakeTilesHelper = require("helpers.fake_tiles")
local utils = require("utils")

local tileEntity = {
    name = "EndHelper/TileEntity",
    placements = {
        {
            name = "normal",
            data = {
                tiletype = "3",
                tiletypeOffscreen = "◯",
                width = 8,
                height = 8,
                backgroundTiles = false,
                Depth = -10000,
                extendOffscreen = true,
                collidable = true,
                colour = "ffffffff",
                allowMerge = true,
                allowMergeDifferentType = true,
                noEdges = false,
                offU = true,
                offUR = true,
                offR = true,
                offDR = true,
                offD = true,
                offDL = true,
                offL = true,
                offUL = true,
                surfaceSoundIndex = -1,
                locationSeeded = true,
                dashBlock = false;
                dashBlockPermament = true;
                dashBlockBreakSound = "event:/game/general/wall_break_stone";
            }
        }
    }
}

tileEntity.fieldOrder = {
    "x", "y", "width", "height",
    "tiletype", "tiletypeOffscreen",
    "backgroundTiles", "collidable", "allowMerge", "allowMergeDifferentType",
    "Depth", "locationSeeded", "colour", "surfaceSoundIndex",
    "dashBlock", "dashBlockPermament", "dashBlockBreakSound",
    "offUL", "offU", "offUR", "offR", "offDR", "offD", "offDL", "offL",
    "noEdges", "extendOffscreen"

}

tileEntity.depth = function(room,entity) return entity.Depth or -10000 end

-- Returns true if a and b could potentially merge, ignoring position.
local function canMergeGlobally(a, b)
    if b._name ~= "EndHelper/TileEntity" then return false end
    if not (a.allowMerge and b.allowMerge) then return false end
    if a.dashBlock ~= b.dashBlock then return false end
    if a.colour ~= b.colour then return false end
    if not (a.tiletype == b.tiletype or (a.allowMergeDifferentType and b.allowMergeDifferentType)) then
        return false
    end
    return true
end

-- Returns true if a and b are adjacent or overlapping (but not just corner-touching).
local function isAdjacentOrOverlapping(a, b)
    local dx = math.min(a.x + a.width, b.x + b.width) - math.max(a.x, b.x)
    local dy = math.min(a.y + a.height, b.y + b.height) - math.max(a.y, b.y)

    return (dx > 0 and dy >= 0) or (dx >= 0 and dy > 0)
end

-- First pass: filter by non-positional merge requirements.
local function prefilterMergables(seed, room)
    local out = {}
    for _, e in ipairs(room.entities) do
        if canMergeGlobally(seed, e) then
            table.insert(out, e)
        end
    end
    return out
end

-- Second pass: flood-fill to collect adjacent mergeables starting from `seed`.
local function collectConnected(seed, room)
    local candidates = prefilterMergables(seed, room)
    local seen, queue, connected = { [seed] = true }, { seed }, {}

    while #queue > 0 do
        local e = table.remove(queue, 1)
        table.insert(connected, e)

        for _, cand in ipairs(candidates) do
            if not seen[cand] and isAdjacentOrOverlapping(e, cand) then
                seen[cand] = true
                table.insert(queue, cand)
            end
        end
    end

    return connected
end

-- The tile entity sprite function
function tileEntity.sprite(room, entity)
    local relevantBlocks = collectConnected(entity, room)
    local firstEntity = relevantBlocks[1] == entity

    local returnSprite

    local tileStr = "tilesFg"
    if entity.backgroundTiles then tileStr = "tilesBg" end

    if firstEntity then
        -- Can use simple render, nothing to merge together
        if #relevantBlocks == 1 then
            returnSprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false, tileStr)(room, entity)
        else
            returnSprite = fakeTilesHelper.getCombinedEntitySpriteFunction(relevantBlocks, "tiletype", false, tileStr)(room)
        end
    end

    if not utils.contains(entity, relevantBlocks) then
        returnSprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false, tileStr)(room, entity)
    end

    if returnSprite ~= nil then
        local color = utils.getColor(entity.colour or {1, 1, 1, 1})
        for i = 1, #returnSprite do
            returnSprite[i]:setColor(color)
        end
        return returnSprite
    end
end


tileEntity.fieldInformation = function(entity)

    local tileStr = "tilesFg"
    if entity.backgroundTiles then
        tileStr = "tilesBg"
    end

    local orig = fakeTilesHelper.getFieldInformation("tiletype", tileStr)(entity)
    orig["tiletypeOffscreen"] = fakeTilesHelper.getFieldInformation("tiletypeOffscreen", tileStr)(entity)["tiletypeOffscreen"]
    orig["Depth"] = {fieldType = "integer"}
    orig["surfaceSoundIndex"] = { fieldType = "integer", minimumValue = -1 }
    orig["colour"] = { fieldType = "color", allowEmpty = true, useAlpha = true }

    orig["dashBlockBreakSound"] = { fieldType = "string", 
        options = {
            {"None", ""},
            {"Dirt", "event:/game/general/wall_break_dirt"},
            {"Ice", "event:/game/general/wall_break_ice"},
            {"Wood", "event:/game/general/wall_break_wood"},
            {"Stone", "event:/game/general/wall_break_stone"},
        }
    }
    return orig
end

function tileEntity.onRotate(room, entity, direction)
    local oldWidth = entity.width
    entity.width = entity.height
    entity.height = oldWidth
end

return tileEntity