-- TODO - Dropdowns

local entityActivator = {}

entityActivator.name = "pandorasBox/entityActivator"
entityActivator.fillColor = {0.7, 1.0, 0.7, 0.4}
entityActivator.borderColor = {0.7, 1.0, 0.7, 1.0}
entityActivator.placements = {
    name = "entity_activator",
    data = {
        width = 8,
        height = 8,
        mode = "ActivateInsideDeactivateOutside",
        activationMode = "OnEnter",
        targets = "",
        useTracked = true,
        flag = "",
        updateInterval = -1.0
    }
}

return entityActivator