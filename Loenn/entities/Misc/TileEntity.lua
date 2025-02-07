local fakeTilesHelper = require('helpers.fake_tiles')

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
                locationSeeded = false,
                --BackgroundTile = false,
            }
        }
    }
}

tileEntity.depth = function(room,entity) return entity.Depth or -10000 end

tileEntity.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

tileEntity.fieldInformation = function(entity)
    local orig = fakeTilesHelper.getFieldInformation("tiletype")(entity)
    orig["tiletypeOffscreen"] = fakeTilesHelper.getFieldInformation("tiletypeOffscreen")(entity)["tiletypeOffscreen"]
    orig["tiletypeOffscreenDiagonal"] = fakeTilesHelper.getFieldInformation("tiletypeOffscreenDiagonal")(entity)["tiletypeOffscreenDiagonal"]
    orig["Depth"] = {fieldType = "integer"}
    return orig
end

return tileEntity