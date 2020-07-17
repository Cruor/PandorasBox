module PandorasBoxPropellerBox

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/propellerBox" PropellerBox(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Propeller Box (Pandora's Box)" => Ahorn.EntityPlacement(
        PropellerBox
    )
)

function getSprite(entity::PropellerBox)
    texture = get(entity, "texture", "default")

    return "objects/pandorasBox/propellerBox/$texture/charged00"
end


function Ahorn.selection(entity::PropellerBox)
    x, y = Ahorn.position(entity)
    sprite = getSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PropellerBox, room::Maple.Room) = Ahorn.drawSprite(ctx, getSprite(entity), 0, 0, jx=0.5, jy=1.0)

end