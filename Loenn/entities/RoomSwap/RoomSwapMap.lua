local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")
local defaultTexture = "objects/EndHelper/roomSwapMap/lonnicon"

local roomSwapMap = {
    name = "EndHelper/RoomSwapMap",
    depth = -10550,
    -- offset = {0, 0},
    --texture = "objects/EndHelper/roomSwapMap/lonnicon",
    placements = {
        {
            name = "normal",
            data = {
                gridId = "1",
                
                folderPath = "",
                scale = 1.0,
                mapBackgroundFileName = "background",
                mapCurrentPosFileName = "current",
                mapIconFilePrefix = "icon_",
                floatAmplitude = 0.1,
                animationSpeedMultiplier = 0.1
            },
        },
    },
    fieldOrder = {
        "x", "y", "editorLayer",
        "gridId"
    },
    fieldInformation = {
        folderPath = {fieldType = "path", allowFolders = true, allowFiles = false}
    }
}

function roomSwapMap.scale(room, entity)
    local scale = entity.scale

    return {scale, scale}
end

function roomSwapMap.texture(room, entity)

    if entity.folderPath == "" 
    then 
        return defaultTexture 
    end

    local iconPath = entity.folderPath .. "/" .. entity.mapBackgroundFileName

    return miscFuncs.trimPath(iconPath, defaultTexture)
end

return roomSwapMap