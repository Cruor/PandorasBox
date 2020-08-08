using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

// TODO - Less reflection usage

namespace Celeste.Mod.PandorasBox
{
    [TrackedAs(typeof(Water))]
    [CustomEntity("pandorasBox/coloredWater")]
    class ColoredWater : Water
    {
        private Color baseColor;
        private Color surfaceColor;
        private Color fillColor;
        private Color rayTopColor;
        private bool fixedSurfaces;

        public static FieldInfo fillColorField = typeof(Water).GetField("FillColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo surfaceColorField = typeof(Water).GetField("SurfaceColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo rayTopColorField = typeof(Water).GetField("RayTopColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo fillField = typeof(Water).GetField("fill", BindingFlags.Instance | BindingFlags.NonPublic);

        public ColoredWater(EntityData data, Vector2 offset) : base(data, offset)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;
            rayTopColor = baseColor * 0.6f;

            fixedSurfaces = false;
        }

        private void fixSurfaces()
        {
            if (!fixedSurfaces)
            {
                bool hasTop = Surfaces.Contains(TopSurface);
                bool hasBottom = Surfaces.Contains(BottomSurface);

                Surfaces.Clear();

                if (hasTop)
                {
                    TopSurface = new Water.Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0.0f, -1f), Width, Height);
                    Surfaces.Add(TopSurface);
                }

                if (hasBottom)
                {
                    BottomSurface = new Water.Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0.0f, 1f), Width, Height);
                    Surfaces.Add(BottomSurface);
                }

                fixedSurfaces = true;
            }
        }

        public override void Render()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;
            Color origRayTop = Water.RayTopColor;

            fillColorField.SetValue(null, fillColor);
            surfaceColorField.SetValue(null, surfaceColor);
            //rayTopColorField.SetValue(null, rayTopColor);

            fixSurfaces();

            base.Render();

            fillColorField.SetValue(null, origFill);
            surfaceColorField.SetValue(null, origSurface);
            //rayTopColorField.SetValue(null, origRayTop);
        }

        public override void Update()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;
            Color origRayTop = Water.RayTopColor;

            //fillColorField.SetValue(null, fillColor);
            //surfaceColorField.SetValue(null, surfaceColor);
            rayTopColorField.SetValue(null, rayTopColor);

            base.Update();

            //fillColorField.SetValue(null, origFill);
            //surfaceColorField.SetValue(null, origSurface);
            rayTopColorField.SetValue(null, origRayTop);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
        }
    }
}
