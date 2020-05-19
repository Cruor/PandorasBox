using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/coloredBigWaterfall")]
    class ColoredBigWaterfall : BigWaterfall
    {
        private Color baseColor;

        public static FieldInfo fillColorField;
        public static FieldInfo surfaceColorField;

        private bool fixedColors;

        public ColoredBigWaterfall(EntityData data, Vector2 offset) : base(data, offset)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));

            fixedColors = false;
        }

        public override void Update()
        {
            if (!fixedColors)
            {
                cacheFieldInfo();

                Color surfaceColor = baseColor * 0.8f;
                Color fillColor = baseColor * 0.3f;

                surfaceColorField.SetValue(this, surfaceColor);
                fillColorField.SetValue(this, fillColor);

                fixedColors = true;
            }

            base.Update();
        }

        private static void cacheFieldInfo()
        {
            if (fillColorField == null || surfaceColorField == null)
            {
                var type = typeof(BigWaterfall);

                fillColorField = type.GetField("fillColor", BindingFlags.Instance | BindingFlags.NonPublic);
                surfaceColorField = type.GetField("surfaceColor", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }
    }
}
