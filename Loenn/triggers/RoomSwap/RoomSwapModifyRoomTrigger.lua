local roomSwapModifyRoomTrigger = {
    name = "EndHelper/RoomSwapModifyRoomTrigger",
    placements = {
        {
            name = "normal",
            data = {
                gridId = "1",
                modificationType = "None",
                modifySilently = false,
                --OLD BEHAVIOUR
                --flagCheck = "",
                --flagRequire = true,
                --flagToggle = false,
                requireFlag = "",
                toggleFlag = "",
                flashEffect = false
            },
        },
    },
    fieldInformation = {
        modificationType = { fieldType = "string", 
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

return roomSwapModifyRoomTrigger