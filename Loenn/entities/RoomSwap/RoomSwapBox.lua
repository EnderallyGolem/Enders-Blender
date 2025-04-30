local miscFuncs = require('mods').requireFromPlugin("libraries.miscFuncs")

local defaultBoxTexture = "objects/EndHelper/RoomSwapBox/loenn"

local roomSwapBox = {
    name = "EndHelper/RoomSwapBox",
    depth = -10550,
    --offset = {-8, -8},
    justification = {0, 0},
    texture = "objects/EndHelper/RoomSwapBox/loenn",
    --texture = "objects/breakerBox/Idle00"
    placements = {
        {
            name = "normal",
            data = {
                gridId = "1",
                preset = "Custom",

                particleColour1 = "ff3399",
                particleColour2 = "ff00ff",
                
                modificationTypeLeft = "None",
                modificationTypeRight = "None",
                modificationTypeUp = "None",
                modificationTypeDown = "None",
                modifySilently = false,
                texturePath = "",
                
                --OLD BEHAVIOUR
                --flagCheck = "",
                --flagRequire = true,
                --flagToggle = false,
                requireFlag = "",
                toggleFlag = "",
                flashEffect = false,
            },
        },
    },
    fieldOrder = {
        "x", "y", "editorLayer",
        "gridId", "preset", "modificationTypeLeft", "modificationTypeRight", "modificationTypeUp", "modificationTypeDown",
        "particleColour1", "particleColour2", "texturePath", "flagCheck"
    },
    fieldInformation = {
        texturePath = {fieldType = "path", allowFolders = false, allowFiles = true},
        particleColour1 = {fieldType = "color"},
        particleColour2 = {fieldType = "color"},
        preset = { fieldType = "string", editable = false,
            options = {
                {"Custom", "Custom"},
                {"Reset", "Reset"},
                {"Slider (No warp)", "Slider (No warp)"},
                {"Slider (Warp)", "Slider (Warp)"},
                {"Swapper ", "Swapper"},
            }
        },
        modificationTypeLeft = { fieldType = "string", 
            options = {
                {"None", "None"},
                {"Reset", "Reset"},
                {"Current Row Left", "CurrentRowLeft"},
                {"Current Row Left (Prevent warp)", "CurrentRowLeft_PreventWarp"},
                {"Current Row Right", "CurrentRowRight"},
                {"Current Row Right (Prevent warp)", "CurrentRowRight_PreventWarp"},
                {"Current Column Up", "CurrentColumnUp"},
                {"Current Column Up (Prevent warp)", "CurrentColumnUp_PreventWarp"},
                {"Current Column Down", "CurrentColumnDown"},
                {"Current Column Down (Prevent warp)", "CurrentColumnDown_PreventWarp"},
                {"Swap Left<->Right", "SwapLeftRight"},
                {"Swap Up<->Down", "SwapUpDown"},
                {"Set To Order", "Set_11_12_21_22"}
            }
        },
        modificationTypeRight = { fieldType = "string", 
            options = {
                {"None", "None"},
                {"Reset", "Reset"},
                {"Current Row Left", "CurrentRowLeft"},
                {"Current Row Left (Prevent warp)", "CurrentRowLeft_PreventWarp"},
                {"Current Row Right", "CurrentRowRight"},
                {"Current Row Right (Prevent warp)", "CurrentRowRight_PreventWarp"},
                {"Current Column Up", "CurrentColumnUp"},
                {"Current Column Up (Prevent warp)", "CurrentColumnUp_PreventWarp"},
                {"Current Column Down", "CurrentColumnDown"},
                {"Current Column Down (Prevent warp)", "CurrentColumnDown_PreventWarp"},
                {"Swap Left<->Right", "SwapLeftRight"},
                {"Swap Up<->Down", "SwapUpDown"},
                {"Set To Order", "Set_11_12_21_22"}
            }
        },
        modificationTypeUp = { fieldType = "string", 
            options = {
                {"None", "None"},
                {"Reset", "Reset"},
                {"Current Row Left", "CurrentRowLeft"},
                {"Current Row Left (Prevent warp)", "CurrentRowLeft_PreventWarp"},
                {"Current Row Right", "CurrentRowRight"},
                {"Current Row Right (Prevent warp)", "CurrentRowRight_PreventWarp"},
                {"Current Column Up", "CurrentColumnUp"},
                {"Current Column Up (Prevent warp)", "CurrentColumnUp_PreventWarp"},
                {"Current Column Down", "CurrentColumnDown"},
                {"Current Column Down (Prevent warp)", "CurrentColumnDown_PreventWarp"},
                {"Swap Left<->Right", "SwapLeftRight"},
                {"Swap Up<->Down", "SwapUpDown"},
                {"Set To Order", "Set_11_12_21_22"}
            }
        },
        modificationTypeDown = { fieldType = "string", 
            options = {
                {"None", "None"},
                {"Reset", "Reset"},
                {"Current Row Left", "CurrentRowLeft"},
                {"Current Row Left (Prevent warp)", "CurrentRowLeft_PreventWarp"},
                {"Current Row Right", "CurrentRowRight"},
                {"Current Row Right (Prevent warp)", "CurrentRowRight_PreventWarp"},
                {"Current Column Up", "CurrentColumnUp"},
                {"Current Column Up (Prevent warp)", "CurrentColumnUp_PreventWarp"},
                {"Current Column Down", "CurrentColumnDown"},
                {"Current Column Down (Prevent warp)", "CurrentColumnDown_PreventWarp"},
                {"Swap Left<->Right", "SwapLeftRight"},
                {"Swap Up<->Down", "SwapUpDown"},
                {"Set To Order", "Set_11_12_21_22"}
            }
        },
        modifySilently = { fieldType = "boolean"}
    }
}

function roomSwapBox.texture(room, entity)

    -- Setting Presets
    local preset = entity.preset

    if preset == "Reset" then
        entity.modificationTypeLeft = "Reset"
        entity.modificationTypeRight = "Reset"
        entity.modificationTypeUp = "Reset"
        entity.modificationTypeDown = "Reset"
    elseif preset == "Slider (Warp)" then
        entity.modificationTypeLeft = "CurrentRowRight"
        entity.modificationTypeRight = "CurrentRowLeft"
        entity.modificationTypeUp = "CurrentColumnDown"
        entity.modificationTypeDown = "CurrentColumnUp"
    elseif preset == "Slider (No warp)" then
        entity.modificationTypeLeft = "CurrentRowRight_PreventWarp"
        entity.modificationTypeRight = "CurrentRowLeft_PreventWarp"
        entity.modificationTypeUp = "CurrentColumnDown_PreventWarp"
        entity.modificationTypeDown = "CurrentColumnUp_PreventWarp"
    elseif preset == "Swapper" then
        entity.modificationTypeLeft = "SwapLeftRight"
        entity.modificationTypeRight = "SwapLeftRight"
        entity.modificationTypeUp = "SwapUpDown"
        entity.modificationTypeDown = "SwapUpDown"
    end


    -- The actual texture logic

    local left = entity.modificationTypeLeft

    if left=="CurrentRowRight_PreventWarp"
    then defaultBoxTexture = "objects/EndHelper/RoomSwapBox/transitionBoxShift"
    
    elseif left=="CurrentRowRight"
    then defaultBoxTexture = "objects/EndHelper/RoomSwapBox/transitionBoxShiftWarp"

    elseif left=="Reset"
    then defaultBoxTexture = "objects/EndHelper/RoomSwapBox/transitionBoxReset"

    elseif left=="SwapLeftRight"
    then defaultBoxTexture = "objects/EndHelper/RoomSwapBox/transitionBoxSwap"
    
    else defaultBoxTexture = "objects/EndHelper/RoomSwapBox/loenn" end

    if entity.texturePath == "heart" or entity.texturePath == "Heart"
    then
        defaultBoxTexture = "objects/EndHelper/RoomSwapBox/transitionBoxHeart"
        return "objects/EndHelper/RoomSwapBox/transitionBoxHeart"
    end

    local returnPath = miscFuncs.trimPath(entity.texturePath, defaultBoxTexture)

    return returnPath
end

return roomSwapBox