local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")
local defaultTexture = "objects/EndHelper/roomSwapMap/upgradeicon"

local roomSwapMapUpgrade = {
    name = "EndHelper/RoomSwapMapUpgrade",
    depth = -10550,
    -- offset = {-8, -8},
    texture = "objects/EndHelper/roomSwapMap/upgradeicon",
    placements = {
        {
            name = "normal",
            data = {
                gridId = "1",
                texturePath = "",
                obtainSoundEvent = "event:/game/07_summit/gem_get",
                floatAmplitude = 0.1,
                changeLevel = 1,
                setLevel = false,
                oneTime = true,
            },
        },
    },
    fieldInformation = {
        texturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
        changeLevel = {fieldType = "integer"}
    }
}

function roomSwapMapUpgrade.texture(room, entity)
    return miscFuncs.trimPath(entity.texturePath, defaultTexture)
end

return roomSwapMapUpgrade