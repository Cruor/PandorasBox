using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/timefield")]
    class TimeField : Entity
    {
        private float start;
        private float stop;

        private float startTime;
        private float stopTime;

        private float lerp;

        private float baseTimeRate;

        private float animTimer;
        private float animRate;
        private Boolean render;

        private Boolean lingering;
        private Boolean playerAffected;

        private MTexture[] particleTextures;
        private TimeParticle[] particles;

        private Color tint;

        private static Player targetPlayer;
        private static PropertyInfo engineDeltaTimeProp;
        private static TimeField lingeringTarget;
        private static Boolean addedHook;

        public TimeField(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            lerp = 0f;

            start = float.Parse(data.Attr("start", "0.2"));
            stop = float.Parse(data.Attr("stop", "1"));

            startTime = float.Parse(data.Attr("stopTime", "1.0"));
            stopTime = float.Parse(data.Attr("startTime", "3.0"));

            animRate = float.Parse(data.Attr("animRate", "6.0"));

            render = Boolean.Parse(data.Attr("render", "true"));
            playerAffected = Boolean.Parse(data.Attr("playerAffected", "true"));
            lingering = Boolean.Parse(data.Attr("lingering", "false"));

            String rawColor = data.Attr("color", "teal");
            tint = ColorHelper.GetColor(rawColor);

            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            base.Collider = (Collider)new Hitbox(data.Width, data.Height, 0, 0);

            this.particleTextures = new MTexture[4]
            {
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(24, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(0, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(8, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(16, 0, 8, 8, null)
            };
        }

        public void OnPlayer(Player player)
        {
            targetPlayer = player;
            lingeringTarget = this;

            CacheTimeProp();

            if (!addedHook)
            {
                On.Celeste.Player.Update += (orig, p) => {
                    float dt = Engine.DeltaTime;
                    engineDeltaTimeProp.SetValue(null, Engine.RawDeltaTime * Engine.TimeRateB, null);

                    orig(p);

                    engineDeltaTimeProp.SetValue(null, dt, null);
                };

                addedHook = true;
            }
        }

        private Boolean PlayerInside()
        {
            if (targetPlayer == null)
            {
                return false;
            }

            if (lingering)
            {
                return this == lingeringTarget || this.CollideCheck(targetPlayer);
            }
            else
            {
                return this.CollideCheck(targetPlayer);
            }
        }

        private Boolean PlayerMoving()
        {
            if (targetPlayer == null)
            {
                return false;
            }

            return targetPlayer.Speed != Vector2.Zero;
        }

        public void CacheTimeProp()
        {
            if (engineDeltaTimeProp == null)
            {
                engineDeltaTimeProp = typeof(Engine).GetProperty("DeltaTime");
            }
        }

        public override void Update()
        {
            animTimer += animRate * Engine.DeltaTime;

            if (PlayerInside() && lingeringTarget.playerAffected)
            {
                CacheTimeProp();

                // float dt = Engine.DeltaTime;
                // engineDeltaTimeProp.SetValue(null, Engine.RawDeltaTime * Engine.TimeRateB - dt, null);
                // targetPlayer.Update();
                // engineDeltaTimeProp.SetValue(null, dt, null);
            }

            if (this == lingeringTarget && !PlayerInside())
            {
                Engine.TimeRate = baseTimeRate;
            }

            UpdateTimeRate();

            if (targetPlayer != null && targetPlayer.Dead)
            {
                Engine.TimeRate = 1.0f;
            }

            // OnPlayer will set this to true again, we just assume that the player is no longer colliding on every update
            base.Update();
        }

        private void UpdateTimeRate()
        {
            if (lingeringTarget == this && PlayerInside() && targetPlayer != null && !targetPlayer.Dead)
            {
                // The lerp updates should be in realtime, not ingame time
                float dt = Engine.RawDeltaTime * baseTimeRate;
                float delta = PlayerMoving() ? dt / startTime : -dt / stopTime;

                lerp = Calc.Clamp(lerp + delta, 0, 1);
                Engine.TimeRate = (stop + (start - stop) * lerp) * baseTimeRate;
            }
        }

        private void Setup()
        {
            particles = new TimeParticle[(int)(this.Width / 8.0 * (this.Height / 8.0) * 0.7)];
            for (int index = 0; index < particles.Length; index++)
            {
                particles[index].Position = new Vector2(Calc.Random.NextFloat(this.Width), Calc.Random.NextFloat(this.Height));
                particles[index].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[index].TimeOffset = Calc.Random.NextFloat();
                particles[index].Scale = Calc.Random.NextFloat();
                particles[index].Color = tint * particles[index].Scale;
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            baseTimeRate = Engine.TimeRate;
            Setup();
        }

        public override void Render()
        {
            if (!render)
            {
                // Excuse me, but I spent some time copy paste modifying space jam...
                // And you decide to not render it?!
                // Well, suit yourself...

                return;
            }

            Camera camera = this.SceneAs<Level>().Camera;
            Vector2 position = camera.Position;

            // Entity is offscreen, no need to render
            if (this.Right < camera.Left || this.Left > camera.Right || this.Bottom < camera.Top || this.Top > camera.Bottom)
            {
                return;
            }

            MTexture particleTexture;
            for (int index = 0; index < particles.Length; index++)
            {
                TimeParticle particle = particles[index];

                int layer = particle.Layer;
                Vector2 targetPosition = PutInside(particle.Position + position * (float)(0.3 + 0.25 * layer));

                switch (layer)
                {
                    case 0:
                        particleTexture = particleTextures[3 - (int)((particle.TimeOffset * 4.0 + animTimer) % 4.0)];
                        break;

                    case 1:
                        particleTexture = particleTextures[1 + (int)((particle.TimeOffset * 2.0 + animTimer) % 2.0)];
                        break;

                    default:
                        particleTexture = particleTextures[2];
                        break;
                }

                if (targetPosition.X >= this.X + 3.0 && targetPosition.Y >= this.Y + 3.0 && targetPosition.X < this.Right - 3.0 && targetPosition.Y < this.Bottom - 3.0)
                {
                    particleTexture.DrawCentered(targetPosition, particle.Color);
                }
            }

            base.Render();
        }

        private struct TimeParticle
        {
            public Vector2 Position;
            public int Layer;
            public Color Color;
            public float Scale;
            public float TimeOffset;
        }

        private Vector2 PutInside(Vector2 pos)
        {
            while (pos.X < this.X)
                pos.X += this.Width;

            while (pos.X > this.X + this.Width)
                pos.X -= this.Width;

            while (pos.Y < this.Y)
                pos.Y += this.Height;

            while (pos.Y > this.Y + this.Height)
                pos.Y -= this.Height;

            return pos;
        }
    }
}