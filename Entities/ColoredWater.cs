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
        private bool visibleOnCamera;

        private bool hasLeftSurface;
        private bool hasRightSurface;

        private Vector2 previousPosition;
        private bool trackedPosition = false;
        private bool hasUpdatedFill = false;

        private List<Water.Surface> emptySurfaces;
        private List<Water.Surface> actualSurfaces;

        private Water.Surface actualTopSurface;
        private Water.Surface dummyTopSurface;
        private Water.Surface actualBottomSurface;
        private Water.Surface dummyBottomSurface;
        public Water.Surface LeftSurface;
        public Water.Surface RightSurface;
        private Water.Surface actualLeftSurface;
        private Water.Surface dummyLeftSurface;
        private Water.Surface actualRightSurface;
        private Water.Surface dummyRightSurface;

        private static int horizontalVisiblityBuffer = 48;
        private static int verticalVisiblityBuffer = 48;

        private Rectangle waterFill;
        private HashSet<WaterInteraction> interactionContains;

        public static FieldInfo fillColorField = typeof(Water).GetField("FillColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo surfaceColorField = typeof(Water).GetField("SurfaceColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo rayTopColorField = typeof(Water).GetField("RayTopColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo fillField = typeof(Water).GetField("fill", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo containsField = typeof(Water).GetField("contains", BindingFlags.Instance | BindingFlags.NonPublic);

        public ColoredWater(EntityData data, Vector2 offset) : base(data.Position + offset, data.Bool("hasTop", true), data.Bool("hasBottom"), data.Width, data.Height)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;
            rayTopColor = baseColor * 0.6f;

            hasLeftSurface = data.Bool("hasLeft");
            hasRightSurface = data.Bool("hasRight");

            waterFill = (Rectangle) fillField.GetValue(this);
            interactionContains = (HashSet<WaterInteraction>) containsField.GetValue(this);
        }

        private void initializeSurfaces()
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

            if (hasLeftSurface)
            {
                LeftSurface = new Water.Surface(Position + new Vector2(8, Height / 2), new Vector2(-1f, 0f), Height, Width);
                Surfaces.Add(LeftSurface);

                actualLeftSurface = LeftSurface;
                dummyLeftSurface = new Water.Surface(Position + new Vector2(8, Height / 2), new Vector2(-1f, 0f), Height, Width);
            }

            if (hasRightSurface)
            {
                RightSurface = new Water.Surface(Position + new Vector2(Width - 8, Height / 2), new Vector2(1f, 0f), Height, Width);
                Surfaces.Add(RightSurface);

                actualRightSurface = RightSurface;
                dummyRightSurface = new Water.Surface(Position + new Vector2(Width - 8, Height / 2), new Vector2(1f, 0f), Height, Width);
            }

            // Update fill rectangle
            if (!hasUpdatedFill && (hasLeftSurface || hasRightSurface))
            {
                Rectangle fill = (Rectangle) fillField.GetValue(this);
                int newX = fill.X;
                int newWidth = fill.Width;

                if (hasLeftSurface)
                {
                    newX += 8;
                    newWidth -= 8;
                }

                if (hasRightSurface)
                {
                    newWidth -= 8;
                }

                Rectangle newFill = new Rectangle(newX, fill.Y, newWidth, fill.Height);

                fillField.SetValue(this, newFill);

                hasUpdatedFill = true;
            } 

            actualSurfaces = Surfaces;
            emptySurfaces = new List<Surface>();

            changeColor(fillColorField, fillColor, origFill);
            changeColor(surfaceColorField, surfaceColor, origSurface);
        }

        // Swap around surfaces to make sure the base Update method doesn't waste cycles on non visible water
        // Use dummy surfaces to not crash vanilla waterfall
        private void updateSurfaceVisibility()
        {
            Surfaces = visibleOnCamera ? actualSurfaces : emptySurfaces;
            TopSurface = visibleOnCamera ? actualTopSurface : dummyTopSurface;
            BottomSurface = visibleOnCamera ? actualBottomSurface : dummyBottomSurface;
            LeftSurface = visibleOnCamera ? actualLeftSurface : dummyLeftSurface;
            RightSurface = visibleOnCamera ? actualRightSurface : dummyRightSurface;

            if (!visibleOnCamera)
            {
                dummyTopSurface?.Ripples?.Clear();
                dummyBottomSurface?.Ripples?.Clear();
                dummyLeftSurface?.Ripples?.Clear();
                dummyRightSurface?.Ripples?.Clear();
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
            if (visibleOnCamera)
            {
                Color origFill = Water.FillColor;
                Color origSurface = Water.SurfaceColor;

                changeColor(fillColorField, origFill, fillColor);
                changeColor(surfaceColorField, origSurface, surfaceColor);

                base.Render();

                changeColor(fillColorField, fillColor, origFill);
                changeColor(surfaceColorField, surfaceColor, origSurface);
            }
        }

        private void rippleLeftRightSurfaces()
        {
            if (!visibleOnCamera)
            {
                return;
            }

            foreach (WaterInteraction component in Scene.Tracker.GetComponents<WaterInteraction>())
            {
                Rectangle bounds = component.Bounds;
                bool contains = interactionContains.Contains(component);
                bool colliding = CollideRect(bounds);
                if (contains != colliding)
                {
                    if (LeftSurface != null && bounds.Center.X <= base.Center.X)
                    {
                        LeftSurface.DoRipple(bounds.Center.ToVector2(), 1f);
                    }

                    if (RightSurface != null && bounds.Center.X > base.Center.X)
                    {
                        RightSurface.DoRipple(bounds.Center.ToVector2(), 1f);
                    }
                }

                // Do not add to the contains hashset, leave that to the base Update method
            }
        }

        private void updateSurfacePositionsAndSize()
        {
            if (!trackedPosition)
            {
                previousPosition = Position;
                trackedPosition = true;
            }

            if (previousPosition != Position)
            {
                Vector2 flooredPosition = new Vector2((float)Math.Floor(Position.X), (float)Math.Floor(Position.Y));

                if (actualTopSurface != null)
                {
                    actualTopSurface.Position = flooredPosition + new Vector2(Width / 2f, 8f);
                    dummyTopSurface.Position = flooredPosition + new Vector2(Width / 2f, 8f);
                }

                if (actualBottomSurface != null)
                {
                    actualBottomSurface.Position = flooredPosition + new Vector2(Width / 2f, Height - 8f);
                    dummyBottomSurface.Position = flooredPosition + new Vector2(Width / 2f, Height - 8f);
                }

                if (hasLeftSurface)
                {
                    actualLeftSurface.Position = flooredPosition + new Vector2(8, Height / 2);
                    dummyLeftSurface.Position = flooredPosition + new Vector2(8, Height / 2);
                }

                if (hasRightSurface)
                {
                    actualRightSurface.Position = flooredPosition + new Vector2(Width - 8, Height / 2);
                    dummyRightSurface.Position = flooredPosition + new Vector2(Width - 8, Height / 2);
                }

                previousPosition = Position;
            }
        }

        public override void Update()
        {
            Level level = Scene as Level;
            Color origRayTop = Water.RayTopColor;

            updateVisiblity(level);
            updateSurfaceVisibility();

            changeColor(rayTopColorField, origRayTop, rayTopColor);

            rippleLeftRightSurfaces();

            base.Update();
            updateSurfacePositionsAndSize();

            changeColor(rayTopColorField, rayTopColor, origRayTop);
        }

        public override void Added(Scene scene)
        {
            initializeSurfaces();

            base.Added(scene);
        }
    }
}
