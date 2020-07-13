module PandorasBoxMarioClearPipe

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/clearPipe" ClearPipe(x::Integer, y::Integer, texture::String="glass", surfaceSound::Integer=-1, hasPipeSolids::Bool=true, debugTransportSpeed::Number=175.0, debugTransportSpeedEnterMultiplier::Number=0.75, debugPipeWidth::Int=32, debugPipeColliderWidth::Int=28, debugPipeColliderDepth::Int=4)

const placements = Ahorn.PlacementDict(
    "Clear Pipe (Pandora's Box)" => Ahorn.EntityPlacement(
        ClearPipe
    ),
)

Ahorn.nodeLimits(entity::ClearPipe) = 0, -1

sprite = "objects/badelineboost/idle00"

function Ahorn.selection(entity::ClearPipe)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

    for node in nodes
        nx, ny = Int.(node)

        push!(res, Ahorn.getSpriteRectangle(sprite, nx, ny))
    end

    return res
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ClearPipe, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, x, y)

    px, py = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px, py, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)

        px, py = nx, ny
    end
end

end