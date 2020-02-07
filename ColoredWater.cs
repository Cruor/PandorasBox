using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/coloredWater")]
    class ColoredWater : Water
    {
        private Color baseColor;
        private Color surfaceColor;
        private Color fillColor;
        private Color rayTopColor;
        private Boolean fixedSurfaces;

        private Water fakeWater;

        public static FieldInfo fillColorField;
        public static FieldInfo surfaceColorField;
        public static FieldInfo rayTopColorField;
        public static FieldInfo fillField;

        public ColoredWater(EntityData data, Vector2 offset) : base(data, offset)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;
            rayTopColor = baseColor * 0.6f;

            fixedSurfaces = false;

            fakeWater = new Water(data, offset);
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
                    TopSurface = new Water.Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0.0f, 1f), Width, Height);
                    Surfaces.Add(TopSurface);
                }

                fixedSurfaces = true;
            }
        }

        private static void cacheFieldInfo()
        {
            if (fillColorField == null || surfaceColorField == null || rayTopColorField == null || fillField == null)
            {
                var type = typeof(Water);

                fillColorField = type.GetField("FillColor", BindingFlags.Static | BindingFlags.Public);
                surfaceColorField = type.GetField("SurfaceColor", BindingFlags.Static | BindingFlags.Public);
                rayTopColorField = type.GetField("RayTopColor", BindingFlags.Static | BindingFlags.Public);
                fillField = type.GetField("fill", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        public override void Render()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;
            Color origRayTop = Water.RayTopColor;

            cacheFieldInfo();

            fillColorField.SetValue(null, fillColor);
            surfaceColorField.SetValue(null, surfaceColor);
            rayTopColorField.SetValue(null, rayTopColor);

            fixSurfaces();

            base.Render();

            fillColorField.SetValue(null, origFill);
            surfaceColorField.SetValue(null, origSurface);
            rayTopColorField.SetValue(null, origRayTop);
        }

        public override void Update()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;
            Color origRayTop = Water.RayTopColor;

            cacheFieldInfo();

            fillColorField.SetValue(null, fillColor);
            surfaceColorField.SetValue(null, surfaceColor);
            rayTopColorField.SetValue(null, rayTopColor);

            base.Update();

            fillColorField.SetValue(null, origFill);
            surfaceColorField.SetValue(null, origSurface);
            rayTopColorField.SetValue(null, origRayTop);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            cacheFieldInfo();

            (scene as Level).Add(fakeWater);
            fillField.SetValue(fakeWater, new Rectangle(0, 0, 0, 0));
            fakeWater.Surfaces.Clear();
        }
    }
}
