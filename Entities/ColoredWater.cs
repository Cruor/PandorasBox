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
        private bool visibleOnCamera;

        private List<Water.Surface> emptySurfaces;
        private List<Water.Surface> actualSurfaces;
        private Water.Surface actualTopSurface;
        private Water.Surface dummyTopSurface;
        private Water.Surface actualBottomSurface;
        private Water.Surface dummyBottomSurface;

        private static int horizontalVisiblityBuffer = 48;
        private static int verticalVisiblityBuffer = 48;

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
                Color origFill = Water.FillColor;
                Color origSurface = Water.SurfaceColor;

                changeColor(fillColorField, origFill, fillColor);
                changeColor(surfaceColorField, origSurface, surfaceColor);

                bool hasTop = Surfaces.Contains(TopSurface);
                bool hasBottom = Surfaces.Contains(BottomSurface);

                Surfaces.Clear();

                if (hasTop)
                {
                    TopSurface = new Water.Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0.0f, -1f), Width, Height);
                    Surfaces.Add(TopSurface);

                    actualTopSurface = TopSurface;
                    dummyTopSurface = new Water.Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0.0f, -1f), Width, Height);
                }

                if (hasBottom)
                {
                    BottomSurface = new Water.Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0.0f, 1f), Width, Height);
                    Surfaces.Add(BottomSurface);

                    actualBottomSurface = BottomSurface;
                    dummyBottomSurface = new Water.Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0.0f, 1f), Width, Height);
                }

                fixedSurfaces = true;
                actualSurfaces = Surfaces;
                emptySurfaces = new List<Surface>();

                changeColor(fillColorField, fillColor, origFill);
                changeColor(surfaceColorField, surfaceColor, origSurface);
            }
        }

        // Swap around surfaces to make sure the base Update method doesn't waste cycles on non visible water
        // Use dummy surfaces to not crash vanilla waterfall
        private void updateSurfaces()
        {
            Surfaces = visibleOnCamera ? actualSurfaces : emptySurfaces;
            TopSurface = visibleOnCamera ? actualTopSurface : dummyTopSurface;
            BottomSurface = visibleOnCamera ? actualBottomSurface : dummyBottomSurface;

            if (!visibleOnCamera)
            {
                dummyTopSurface?.Ripples?.Clear();
                dummyBottomSurface?.Ripples?.Clear();
            }
        }

        private void updateVisiblity(Level level)
        {
            Camera camera = level.Camera;

            bool horizontalCheck = X < camera.Right + horizontalVisiblityBuffer && X + Width > camera.Left - horizontalVisiblityBuffer;
            bool verticalCheck = Y < camera.Bottom + verticalVisiblityBuffer && Y + Height > camera.Top - verticalVisiblityBuffer;

            visibleOnCamera = horizontalCheck && verticalCheck;
        }

        private void changeColor(FieldInfo fieldInfo, Color from, Color to)
        {
            if (from != to)
            {
                fieldInfo.SetValue(null, to);
            }
        }

        public override void Render()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;

            changeColor(fillColorField, origFill, fillColor);
            changeColor(surfaceColorField, origSurface, surfaceColor);

            base.Render();

            changeColor(fillColorField, fillColor, origFill);
            changeColor(surfaceColorField, surfaceColor, origSurface);
        }

        public override void Update()
        {
            Level level = Scene as Level;
            Color origRayTop = Water.RayTopColor;

            updateVisiblity(level);
            updateSurfaces();

            changeColor(rayTopColorField, origRayTop, rayTopColor);

            base.Update();

            changeColor(rayTopColorField, rayTopColor, origRayTop);
        }

        public override void Added(Scene scene)
        {
            fixSurfaces();

            base.Added(scene);
        }
    }
}
