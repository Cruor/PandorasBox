local drawableSpriteStruct = require("structs.drawable_sprite")

local introCar = {}

local bodyTexture = "scenery/car/body"
local wheelsTexture = "scenery/car/wheels"

introCar.name = "pandorasBox/introCar"
introCar.placements = {
    name = "intro_car",
    data = {
        acceleration = 256.0,
        deceleration = 384.0,
        maxSpeed = 384.0,
        brokenDoor = false,
        keepCarSpeedOnExit = true,
        nitroAcceleration = 448.0,
        nitroMaxDuration = 3.0,
        nitroRegenMultiplier = 0.2,
        facing = 1
    }
}
introCar.fieldInformation = {
    facing = {
        fieldType = "integer"
    }
}

function introCar.sprite(room, entity)
    local sprites = {}

    local bodySprite = drawableSpriteStruct.fromTexture(bodyTexture, entity)
    bodySprite:setJustification(0.5, 1.0)
    bodySprite.depth = 1

    local wheelSprite = drawableSpriteStruct.fromTexture(wheelsTexture, entity)
    wheelSprite:setJustification(0.5, 1.0)
    wheelSprite.depth = 3

    table.insert(sprites, bodySprite)
    table.insert(sprites, wheelSprite)

    return sprites
end

return introCar