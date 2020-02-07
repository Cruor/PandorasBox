using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/lamp")]
    class Lamp : Actor
    {
        private Sprite startupSprite;
        private Sprite idleSprite;
        private Sprite baseSprite;

        bool inStartupAnimation;
        bool inIdleAnimation;

        private BloomPoint bloom;
        private VertexLight light;

        private string flag;
        private int id;
        private Color baseColor;
        private Color lightColor;

        public Lamp(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            flag = data.Attr("flag", "");
            id = data.ID;
            baseColor = ColorHelper.GetColor(data.Attr("baseColor", "White"));
            lightColor = ColorHelper.GetColor(data.Attr("lightColor", "White"));

            Add((Component)(startupSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/start")));
            startupSprite.AddLoop("start", "", 0.1f);
            startupSprite.JustifyOrigin(0.5f, 0.5f);
            startupSprite.Play("start");
            startupSprite.OnLastFrame = onLastFrame;
            startupSprite.Stop();

            Add((Component)(idleSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/idle")));
            idleSprite.AddLoop("idle", "", 0.125f);
            idleSprite.JustifyOrigin(0.5f, 0.5f);
            idleSprite.Play("idle");
            idleSprite.Visible = false;

            Add((Component)(baseSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/base")));
            baseSprite.AddLoop("base", "", 0.1f);
            baseSprite.JustifyOrigin(0.5f, 0.5f);
            baseSprite.Play("base");
            baseSprite.SetColor(baseColor);

            inStartupAnimation = false;

            Add((Component)(bloom = new BloomPoint(0.5f, 8f)));
            Add((Component)(light = new VertexLight(lightColor, 1f, 48, 64)));

            bloom.Visible = light.Visible = false;
        }

        private void onLastFrame(string s)
        {
            if (isActive())
            {
                idleSprite.Play("idle", true);
                idleSprite.Rate = 1;
                idleSprite.Visible = true;
                startupSprite.Visible = false;

                bloom.Visible = light.Visible = true;

                inIdleAnimation = true;

                bloom.Visible = light.Visible = true;
            }
            else
            {
                inIdleAnimation = false;
            }

            inStartupAnimation = false;
            startupSprite.Stop();
        }

        private bool isActive()
        {
            Level level = Scene as Level;

            return level.Session.GetFlag(flag);
        }

        public override void Update()
        {

            if (isActive())
            {
                if (!inStartupAnimation && !inIdleAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = 1;
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;
                    inIdleAnimation = false;

                    bloom.Visible = light.Visible = false;
                }
            }
            else
            {
                if (!inStartupAnimation && inIdleAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = -1;
                    startupSprite.SetAnimationFrame(startupSprite.CurrentAnimationTotalFrames - 1);
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;
                    inIdleAnimation = false;

                    bloom.Visible = light.Visible = false;
                }
            }

            base.Update();
        }
    }
}
