using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

// Todo particle trail (configable)
// Uses shells current color

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/shell")]
    class MarioShell : Actor
    {
        public float grace;
        public float springGrace;
        public Vector2 Speed;
        public List<Color> colors;
        public float colorSpeed;
        public float timeAcc;
        public String texture;
        public int id;

        public MethodInfo springMethod;

        public Holdable Hold;
        public Scene scene;

        public Vector2 prevLiftspeed;

        private Sprite decorationIdle;
        private Sprite decorationMoving;
        private Sprite shellIdle;
        private Sprite shellMoving;

        private BloomPoint bloom;
        private VertexLight light;

        private Hitbox pickupMovingCollider;
        private Hitbox pickupIdleCollider;

        private Hitbox shellNotHeldCollider;
        private Hitbox shellHeldCollider;

        private Collision onCollideH;
        private Collision onCollideV;

        public static float gravity = -80f;
        public static float baseSpeed = 200f;
        public static float baseThrowHeight = 0f;
        public static float gracePush = 0.1f;
        public static float graceThrow = 0.15f;
        public static float graceBounce = 0.15f;
        public static float graceSpring = 0.15f;

        public static Hashtable dangerous = new Hashtable{
            {"koopa", false},
            {"spiny", true},
            {"beetle", false},
            {"bowserjr", true}
        };

        private bool isDangerous
        {
            get
            {
                return dangerous.Contains(texture) && (bool)dangerous[texture];
            }
        }

        private bool alreadyAdded
        {
            get
            {
                return Scene.Tracker.GetEntities<MarioShell>().Cast<MarioShell>().Any(other => (other.id == this.id && this.Hold.IsHeld != other.Hold.IsHeld));
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            this.scene = scene;

            if (alreadyAdded)
            {
                base.RemoveSelf();
            }
        }

        public MarioShell(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            colors = new List<Color>();
            timeAcc = 0;

            bool lights = Boolean.Parse(data.Attr("lights", "false"));

            texture = data.Attr("texture", "koopa");

            // Check if this is a "valid" texture
            texture = (dangerous.ContainsKey(texture)? texture : "koopa");

            String rawColor = data.Attr("color", "Green");
            colorSpeed = float.Parse(data.Attr("colorSpeed", "0.8"));

            foreach (String s in rawColor.Split(','))
            {
                colors.Add(ColorHelper.GetColor(s));
            }

            int direction = Math.Sign(Int32.Parse(data.Attr("direction", "0")));

            Speed = new Vector2(baseSpeed * direction, 0f);
            grace = 0f;

            prevLiftspeed = Vector2.Zero;

            this.pickupIdleCollider = new Hitbox(18f, 18f, -9, -9);
            this.pickupMovingCollider = new Hitbox(0f, 0f, 0f, 0f);

            this.shellHeldCollider = new Hitbox(8f, 14f, -4f, -7f);
            this.shellNotHeldCollider = new Hitbox(14f, 14f, -7f, -7f);

            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            base.Collider = shellNotHeldCollider;

            this.onCollideH = new Collision(this.OnCollideH);
            this.onCollideV = new Collision(this.OnCollideV);

            this.Add((Component)(this.decorationIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_idle")));
            this.decorationIdle.AddLoop("deco_idle", "", 0.1f);
            this.decorationIdle.Play("deco_idle", true, false);
            this.decorationIdle.CenterOrigin();

            this.Add((Component)(this.shellIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_idle")));
            this.shellIdle.AddLoop("shell_idle", "", 0.1f);
            this.shellIdle.Play("shell_idle", true, false);
            this.shellIdle.CenterOrigin();
            this.shellIdle.Color = colors[0];

            this.Add((Component)(this.decorationMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_moving")));
            this.decorationMoving.AddLoop("deco_moving", "", 0.1f);
            this.decorationMoving.Play("deco_moving", true, false);
            this.decorationMoving.CenterOrigin();

            this.Add((Component)(this.shellMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_moving")));
            this.shellMoving.AddLoop("shell_moving", "", 0.1f);
            this.shellMoving.Play("shell_moving", true, false);
            this.shellMoving.CenterOrigin();
            this.shellMoving.Color = colors[0];

            this.Add((Component)(this.Hold = new Holdable()));
            this.Hold.PickupCollider = new Hitbox(18f, 18f, -9, -9);
            this.Hold.OnPickup = new Action(this.OnPickup);
            this.Hold.OnRelease = new Action<Vector2>(this.OnRelease);

            this.decorationMoving.Visible = this.shellMoving.Visible = false;

            this.id = data.ID;

            if (lights)
            {
                this.Add((Component)(this.bloom = new BloomPoint(1f, 16f)));
                this.Add((Component)(this.light = new VertexLight(base.Collider.Center, Color.White, 1f, 8, 24)));

                this.bloom.Visible = this.light.Visible = true;
            }
        }

        public override void Update()
        {
            grace = Math.Max(0, grace - Engine.DeltaTime);
            springGrace = Math.Max(0, springGrace - Engine.DeltaTime);

            if (colors != null && colors.Count > 1 && colorSpeed > 0)
            {
                int index = (int)Math.Floor(timeAcc / colorSpeed % colors.Count);
                float lerp = timeAcc / colorSpeed % 1;

                Color newColor = Color.Lerp(colors[index], colors[(index + 1) % colors.Count], lerp);

                this.shellMoving.Color = this.shellIdle.Color = newColor;
            }

            timeAcc += Engine.DeltaTime;

            decorationMoving.Visible = shellMoving.Visible = Speed.X != 0;
            decorationIdle.Visible = shellIdle.Visible = Speed.X == 0;

            if (!this.Hold.IsHeld)
            {
                foreach (Spring spring in scene.Entities.Where(e => e is Spring))
                {
                    if (this.CollideCheck(spring))
                    {
                        Audio.Play("event:/game/general/spring", this.BottomCenter);
                        HitSpring(spring);
                    }
                }

                this.Speed.Y = Calc.Approach(this.Speed.Y, 200f, 400f * Engine.DeltaTime);
                this.Speed.Y = Calc.Approach(this.Speed.Y, 200f, 400f * Engine.DeltaTime);

                this.MoveH(this.Speed.X * Engine.DeltaTime, this.onCollideH, null);
                this.MoveV(this.Speed.Y * Engine.DeltaTime, this.onCollideV, null);

                if (LiftSpeed == Vector2.Zero && prevLiftspeed != Vector2.Zero)
                {
                    this.Speed = prevLiftspeed;
                    this.Speed.X = Math.Sign(this.Speed.X) * baseSpeed;
                    prevLiftspeed = Vector2.Zero;
                }

                prevLiftspeed = LiftSpeed;
            }
            else
            {
                prevLiftspeed = Vector2.Zero;
            }

            if (!this.Hold.IsHeld && this.Collider == this.shellHeldCollider && grace == 0)
            {
                this.Collider = this.shellNotHeldCollider;

                if (CollideCheck<Solid>())
                {
                    this.Collider = shellHeldCollider;
                }
            }

            this.Hold.PickupCollider = Math.Abs(Speed.X) >= 10e-6 ? this.pickupMovingCollider : this.pickupIdleCollider;

            base.Update();
        }

        public void HitSpring(Spring spring)
        {
            if (!this.Hold.IsHeld && springGrace == 0)
            {
                if (springMethod == null)
                {
                    springMethod = spring.GetType().GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                Speed.Y -= spring.Orientation.ToString().Equals("Floor") ? 240f : 140f;

                springGrace = graceSpring;
                springMethod.Invoke(spring, null);
            }
        }

        private void OnPlayer(Player player)
        {
            if (!Hold.IsHeld && grace == 0)
            {
                if (isDangerous)
                {
                    player.Die((player.Center - this.Position).SafeNormalize());

                    return;
                }

                if (player.BottomCenter.Y < this.Center.Y)
                {
                    grace = graceBounce;
                    player.Bounce(this.Top);

                    Speed.X = Math.Abs(Speed.X) >= 10e-6 ? 0 : ((this.Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed;

                    Audio.Play("event:/game/general/thing_booped", this.Position).setVolume(0.5f);
                }
                else
                {
                    if (Speed.X == 0)
                    {
                        // Kick
                        Speed = new Vector2(((this.Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed, gravity);
                        grace = gracePush;

                        Audio.Play("event:/game/general/thing_booped", this.Position).setVolume(0.5f);
                    }
                    else
                    {
                        player.Die((player.Center - this.Position).SafeNormalize(), false, true);
                    }                   
                }
            }
        }

        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(this.Speed.X));
            }

            Audio.Play("event:/game/general/thing_booped", this.Position).setVolume(0.3f);
            this.Speed.X *= -1;
        }

        private void OnCollideV(CollisionData data)
        {
            this.Speed.Y = 0f;
        }

        public override bool IsRiding(Solid solid)
        {
            return base.IsRiding(solid);
        }

        private void OnPickup()
        {
            this.Collider = shellHeldCollider;
            this.AddTag(Tags.Persistent);
            Speed = new Vector2(0f, 0f);
        }

        private void OnRelease(Vector2 force)
        {
            Speed = new Vector2(Math.Sign(force.X) * baseSpeed, baseThrowHeight);
            grace = graceThrow;

            this.RemoveTag(Tags.Persistent);
        }

        protected override void OnSquish(CollisionData data)
        {
            if (base.TrySquishWiggle(data))
            {
                this.RemoveSelf();
            }
        }

        public override void Render()
        {
            base.Render();
        }
    }
}
