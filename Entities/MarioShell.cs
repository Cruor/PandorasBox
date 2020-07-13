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

        public static MethodInfo springMethodInfo = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);

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
                return Scene.Tracker.GetEntities<MarioShell>().Cast<MarioShell>().Any(other => (other.id == id && Hold.IsHeld != other.Hold.IsHeld));
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

            bool lights = data.Bool("lights", false);

            texture = data.Attr("texture", "koopa");

            // Check if this is a "valid" texture
            texture = (dangerous.ContainsKey(texture)? texture : "koopa");

            String rawColor = data.Attr("color", "Green");
            colorSpeed = data.Float("colorSpeed", 0.8f);

            foreach (String s in rawColor.Split(','))
            {
                colors.Add(ColorHelper.GetColor(s));
            }

            int direction = Math.Sign(data.Int("direction", 0));

            Speed = new Vector2(baseSpeed * direction, 0f);
            grace = 0f;

            prevLiftspeed = Vector2.Zero;

            pickupIdleCollider = new Hitbox(18f, 18f, -9, -9);
            pickupMovingCollider = new Hitbox(0f, 0f, 0f, 0f);

            shellHeldCollider = new Hitbox(8f, 14f, -4f, -7f);
            shellNotHeldCollider = new Hitbox(14f, 14f, -7f, -7f);

            Add(new PlayerCollider(new Action<Player>(OnPlayer)));
            Collider = shellNotHeldCollider;

            onCollideH = new Collision(OnCollideH);
            onCollideV = new Collision(OnCollideV);

            Add((Component)(decorationIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_idle")));
            decorationIdle.AddLoop("deco_idle", "", 0.1f);
            decorationIdle.Play("deco_idle", true, false);
            decorationIdle.CenterOrigin();

            Add((Component)(shellIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_idle")));
            shellIdle.AddLoop("shell_idle", "", 0.1f);
            shellIdle.Play("shell_idle", true, false);
            shellIdle.CenterOrigin();
            shellIdle.Color = colors[0];

            Add((Component)(decorationMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_moving")));
            decorationMoving.AddLoop("deco_moving", "", 0.1f);
            decorationMoving.Play("deco_moving", true, false);
            decorationMoving.CenterOrigin();

            Add((Component)(shellMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_moving")));
            shellMoving.AddLoop("shell_moving", "", 0.1f);
            shellMoving.Play("shell_moving", true, false);
            shellMoving.CenterOrigin();
            shellMoving.Color = colors[0];

            Add((Component)(Hold = new Holdable()));
            Hold.PickupCollider = new Hitbox(18f, 18f, -9, -9);
            Hold.OnPickup = new Action(OnPickup);
            Hold.OnRelease = new Action<Vector2>(OnRelease);
            Hold.SpeedGetter = () => Speed;

            decorationMoving.Visible = shellMoving.Visible = false;

            id = data.ID;

            if (lights)
            {
                Add((Component)(bloom = new BloomPoint(1f, 16f)));
                Add((Component)(light = new VertexLight(Collider.Center, Color.White, 1f, 8, 24)));

                bloom.Visible = light.Visible = true;
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

                shellMoving.Color = shellIdle.Color = newColor;
            }

            timeAcc += Engine.DeltaTime;

            bool useMovingVisuals = Speed.X != 0 || Get<MarioClearPipeInteraction>()?.CurrentClearPipe != null;

            decorationMoving.Visible = shellMoving.Visible = useMovingVisuals;
            decorationIdle.Visible = shellIdle.Visible = !useMovingVisuals;

            if (!Hold.IsHeld)
            {
                foreach (Spring spring in scene.Entities.Where(e => e is Spring))
                {
                    if (CollideCheck(spring))
                    {
                        Audio.Play("event:/game/general/spring", BottomCenter);
                        HitSpring(spring);
                    }
                }

                Speed.Y = OnGround() && Speed.Y > 0 ? 0f : Calc.Approach(Speed.Y, 200f, 400f * Engine.DeltaTime);

                MoveH(Speed.X * Engine.DeltaTime, onCollideH, null);
                MoveV(Speed.Y * Engine.DeltaTime, onCollideV, null);

                if (OnGround() && LiftSpeed == Vector2.Zero && prevLiftspeed != Vector2.Zero)
                {
                    Speed = prevLiftspeed;
                    Speed.X = Math.Sign(Speed.X) * baseSpeed;

                    prevLiftspeed = Vector2.Zero;
                }

                prevLiftspeed = LiftSpeed;
            }
            else
            {
                prevLiftspeed = Vector2.Zero;
            }

            if (!Hold.IsHeld && Collider == shellHeldCollider && grace == 0)
            {
                Collider = shellNotHeldCollider;

                if (CollideCheck<Solid>())
                {
                    Collider = shellHeldCollider;
                }
            }

            Hold.PickupCollider = Math.Abs(Speed.X) >= 10e-6 ? pickupMovingCollider : pickupIdleCollider;

            base.Update();
        }

        public void HitSpring(Spring spring)
        {
            if (!Hold.IsHeld && springGrace == 0)
            {
                Speed.Y -= spring.Orientation.ToString().Equals("Floor") ? 240f : 140f;

                springGrace = graceSpring;
                springMethodInfo.Invoke(spring, null);
            }
        }

        private void OnPlayer(Player player)
        {
            if (!Hold.IsHeld && grace == 0)
            {
                if (isDangerous)
                {
                    player.Die((player.Center - Position).SafeNormalize());

                    return;
                }

                if (player.BottomCenter.Y < Center.Y)
                {
                    grace = graceBounce;
                    player.Bounce(Top);

                    Speed.X = Math.Abs(Speed.X) >= 10e-6 ? 0 : ((Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed;

                    Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.5f);
                }
                else
                {
                    if (Speed.X == 0)
                    {
                        // Kick
                        Speed = new Vector2(((Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed, gravity);
                        grace = gracePush;

                        Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.5f);
                    }
                    else
                    {
                        player.Die((player.Center - Position).SafeNormalize(), false, true);
                    }                   
                }
            }
        }

        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }

            Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.3f);
            Speed.X *= -1;
        }

        private void OnCollideV(CollisionData data)
        {
            Speed.Y = 0f;
        }

        public override bool IsRiding(Solid solid)
        {
            return base.IsRiding(solid);
        }

        private void OnPickup()
        {
            Collider = shellHeldCollider;
            AddTag(Tags.Persistent);
            Speed = new Vector2(0f, 0f);
        }

        private void OnRelease(Vector2 force)
        {
            Speed = new Vector2(Math.Sign(force.X) * baseSpeed, force.Y * baseThrowHeight);
            grace = graceThrow;

            RemoveTag(Tags.Persistent);
        }

        protected override void OnSquish(CollisionData data)
        {
            if (base.TrySquishWiggle(data))
            {
                RemoveSelf();
            }
        }

        public override void Render()
        {
            base.Render();
        }
    }
}
