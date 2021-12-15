-- TODO - Dropdowns

local waterDrownSprite = {}

waterDrownSprite.name = "pandorasBox/waterDrowningController"
waterDrownSprite.depth = 0
waterDrownSprite.placements = {
    name = "controller",
    data = {
        mode = "Swimming",
        maxDuration = 10.0
    }
}

waterDrownSprite.texture = "objects/pandorasBox/controllerIcons/waterDrowningController"

return waterDrownSprite