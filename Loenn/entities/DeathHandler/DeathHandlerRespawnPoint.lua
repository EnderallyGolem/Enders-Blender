local DeathHandlerRespawnPoint = {
    name = "EndHelper/DeathHandlerRespawnPoint",
    depth = -8500,
    justification = {0.5, 0.5},
    offset = {0, 1},
    placements = {
        {
            name = "normal",
            data = {
                faceLeft = false,
                visible = true,
                attachable = true,
                requireFlag = "",
                fullReset = false,
            },
        },
    },
    fieldInformation = {}
}
function DeathHandlerRespawnPoint.scale(room, entity)
    return entity.faceLeft and -1 or 1, 1
end
function DeathHandlerRespawnPoint.texture(room, entity)
    if (entity.fullReset == false and entity.visible == true) then
        return "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_normal_active"
    elseif (entity.fullReset == false and entity.visible == false) then
        return "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_normal_inactive"
    elseif (entity.fullReset == true and entity.visible == true) then
        return "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_fullreset_active"
    elseif (entity.fullReset == true and entity.visible == false) then
        return "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_fullreset_inactive"
    end
    return "objects/EndHelper/DeathHandlerRespawnPoint/respawnpoint_normal_inactive"
end

return DeathHandlerRespawnPoint