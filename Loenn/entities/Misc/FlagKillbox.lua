local utils = require("utils")

local FlagKillbox = {
    name = "EndHelper/FlagKillbox",
    color = {0.8, 0.4, 0.4, 0.8},
    depth = -9999999,
    canResize = {true, false},
    placements = {
        name = "normal",
        alternativeName = {"altname"},
        data = {
            width = 8,
            triggerDistance = 4,
            requireFlag = "",
            permamentActivate = true,
        }
    }
}

function FlagKillbox.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, 32)
end

return FlagKillbox