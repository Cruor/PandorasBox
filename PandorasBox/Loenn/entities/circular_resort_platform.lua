-- TODO - Dropdowns

local resortPlatformHelper = require("helpers.resort_platforms")
local utils = require("utils")

local circularResortPlatform = {}

circularResortPlatform.name = "pandorasBox/circularResortPlatform"
circularResortPlatform.depth = 1
circularResortPlatform.fieldInformation = {
    lineFillColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    lineEdgeColor = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
circularResortPlatform.nodeLimits = {1, 1}
circularResortPlatform.nodeLineRenderType = "circle"
circularResortPlatform.placements = {
    name = "normal",
    data = {
        width = 16,
        texture = "default",
        clockwise = true,
        speed = 1500.0,
        particles = true,
        attachToSolid = true,
        renderRail = true,
        lineFillColor = "160b12",
        lineEdgeColor = "2a1923",
        rotationFix = true
    }
}

function circularResortPlatform.sprite(room, entity)
    return resortPlatformHelper.addPlatformSprites({}, entity, entity, texture, width)
end

circularResortPlatform.selection = resortPlatformHelper.getSelection

return circularResortPlatform