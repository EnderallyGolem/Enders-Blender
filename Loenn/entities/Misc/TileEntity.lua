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
                Depth = -10000,
                width = 8,
                height = 8,
                extendOffscreen = true,
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
                locationSeeded = false,
                --BackgroundTile = false,
                dashBlock = false;
                dashBlockPermament = true;
                dashBlockBreakSound = "event:/game/general/wall_break_stone";
            }
        }
    }
}

tileEntity.depth = function(room,entity) return entity.Depth or -10000 end

local function getSearchPredicate(entity)
    return function(target)
        -- Both Tile Entities
        return entity._name == target._name and target._name == "EndHelper/TileEntity" 
        -- Both set to allow merge, and match dashBlock
        and entity.dashBlock == target.dashBlock and entity.allowMerge and target.allowMerge
        and entity.tiletype == target.tiletype
    end
end

function tileEntity.sprite(room, entity)
    local relevantBlocks = utils.filter(getSearchPredicate(entity), room.entities)
    local firstEntity = relevantBlocks[1] == entity

    if firstEntity then
        -- Can use simple render, nothing to merge together
        if #relevantBlocks == 1 then
            return fakeTilesHelper.getEntitySpriteFunction("tiletype", false)(room, entity)
        end

        return fakeTilesHelper.getCombinedEntitySpriteFunction(relevantBlocks, "tiletype")(room)
    end

    local entityInRoom = utils.contains(entity, relevantBlocks)

    -- Entity is from a placement preview
    if not entityInRoom then
        return fakeTilesHelper.getEntitySpriteFunction("tiletype", false)(room, entity)
    end
end

tileEntity.fieldInformation = function(entity)
    local orig = fakeTilesHelper.getFieldInformation("tiletype")(entity)
    orig["tiletypeOffscreen"] = fakeTilesHelper.getFieldInformation("tiletypeOffscreen")(entity)["tiletypeOffscreen"]
    orig["tiletypeOffscreenDiagonal"] = fakeTilesHelper.getFieldInformation("tiletypeOffscreenDiagonal")(entity)["tiletypeOffscreenDiagonal"]
    orig["Depth"] = {fieldType = "integer"}
    orig["surfaceSoundIndex"] = { fieldType = "integer", minimumValue = -1 }

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

return tileEntity