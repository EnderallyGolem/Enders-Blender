local multiroomwatchtower = {}

multiroomwatchtower.name = "EndHelper/MultiroomWatchtower"
multiroomwatchtower.depth = -8500
multiroomwatchtower.justification = {0.5, 1.0}
multiroomwatchtower.nodeLineRenderType = "line"
multiroomwatchtower.nodeVisibility = "always"
multiroomwatchtower.texture = "objects/EndHelper/multiroomWatchtower/lookout05"
multiroomwatchtower.nodeTexture = "objects/EndHelper/multiroomWatchtower/node"
multiroomwatchtower.nodeLimits = {0, -1}
multiroomwatchtower.placements = {
    name = "normal",
    alternativeName = {"Multiroom Tower Viewer", "Multiroom lookout", "Multiroom binoculars", "Multi-room Tower Viewer", "Multi-room lookout", "Multi-room binoculars", "Multiroom Watchtower"},
    data = {
        onlyX = false,
        onlyY = false,
        speed = 240.0,
        modifiedInterpolation = false,
        ignoreLookoutBlocker = false,
    }
}

return multiroomwatchtower