local checkpoint = {}

local themes = {"flag"}

checkpoint.name = "pandorasBox/checkpoint"
checkpoint.depth = -100
checkpoint.placements = {
    name = "checkpoint",
    data = {
        spawnConfetti = true,
        theme = "flag",
        activationSound = "event:/game/07_summit/checkpoint_confetti"
    }
}
checkpoint.fieldInformation = {
    theme = {
        options = themes,
        editable = true
    }
}

checkpoint.justification = {0.5, 1.0}

function checkpoint.texture(room, entity)
    local theme = entity.theme or "flag"

    return "objects/pandorasBox/checkpoint/" .. theme .. "/active_idle00"
end

return checkpoint