module PandorasBoxGrapplePoint

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/grapplePoint" GrapplePoint(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Grapple Point (Pandora's Box)" => Ahorn.EntityPlacement(
        GrapplePoint
    )
)

sprite = "objects/refill/idle00"

function Ahorn.selection(entity::GrapplePoint)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrapplePoint)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end