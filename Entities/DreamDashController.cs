using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Collections;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/dreamDashController")]
    [Tracked]
    class DreamDashController : Entity
    {
        public bool AllowSameDirectionDash;
        public bool AllowDreamDashRedirection;
        public bool OverrideDreamDashSpeed;
        public bool OverrideColors;
        public bool NeverSlowDown;
        public bool BounceOnCollision;

        private float sameDirectionSpeedMultiplier;
        private float dreamDashSpeed;

        private Color activeBackColor;
        private Color disabledBackColor;
        private Color activeLineColor;
        private Color disabledLineColor;

        private Color activeBackColorDefault;
        private Color disabledBackColorDefault;
        private Color activeLineColorDefault;
        private Color disabledLineColorDefault;

        private List<List<Color>> particleLayerColors;

        private bool addedColors;

        private static FieldInfo playerDashCooldownTimerMethod = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo dreamBlockActiveBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockActiveLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticles = typeof(DreamBlock).GetField("particles", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockPlayerHasDreamDash = typeof(DreamBlock).GetField("playerHasDreamDash", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Type dreamBlockParticleType = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticleLayer = dreamBlockParticleType.GetField("Layer", BindingFlags.Instance | BindingFlags.Public);
        private static FieldInfo dreamBlockParticleColor = dreamBlockParticleType.GetField("Color", BindingFlags.Instance | BindingFlags.Public);

        private static MethodInfo playerDreamDashedIntoSolid = typeof(Player).GetMethod("DreamDashedIntoSolid", BindingFlags.Instance | BindingFlags.NonPublic);

        public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            AllowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
            AllowDreamDashRedirection = data.Bool("allowDreamDashRedirect", true);
            OverrideDreamDashSpeed = data.Bool("overrideDreamDashSpeed", false);
            OverrideColors = data.Bool("overrideColors", false);
            NeverSlowDown = data.Bool("neverSlowDown", false);
            BounceOnCollision = data.Bool("bounceOnCollision", false);

            sameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1.0f);
            dreamDashSpeed = data.Float("dreamDashSpeed", 240f);

            activeBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
            disabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
            activeLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
            disabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));

            particleLayerColors = new List<List<Color>> {
                ColorHelper.GetColors(data.Attr("particleLayer0Colors", "ffef11,ff00d0,08a310")),
                ColorHelper.GetColors(data.Attr("particleLayer1Colors", "5fcde4,7fb25e,e0564c")),
                ColorHelper.GetColors(data.Attr("particleLayer2Colors", "5b6ee1,CC3B3B,7daa64"))
            };

            addedColors = false;
        }

        private bool dreamDashRedirect(Player player)
        {
            if (player.StateMachine.State == Player.StDreamDash)
            {
                playerDashCooldownTimerMethod.SetValue(player, 0f);

                if (player.CanDash)
                {
                    bool sameDirection = Input.GetAimVector() == player.DashDir;

                    if (AllowDreamDashRedirection && !sameDirection || AllowSameDirectionDash && sameDirection)
                    {
                        player.DashDir = Input.GetAimVector();
                        player.Speed = player.DashDir * player.Speed.Length();
                        player.Dashes = Math.Max(0, player.Dashes - 1);

                        Audio.Play("event:/char/madeline/dreamblock_enter");

                        // Freeze game when redirecting dash
                        // Consistent with dashing in the base game
                        if (Engine.TimeRate > 0.25f)
                        {
                            Celeste.Freeze(0.05f);
                        }

                        if (sameDirection)
                        {
                            player.Speed *= sameDirectionSpeedMultiplier;
                            player.DashDir *= Math.Sign(sameDirectionSpeedMultiplier);
                        }

                        Input.Dash.ConsumeBuffer();

                        return true;
                    }
                }
            }

            return false;
        }

        // Do a bounce check and bounce if possible
        public bool AttemptBounce(Player player)
        {
            if (BounceOnCollision)
            {
                Vector2 moveCheckVector = player.Speed * Engine.DeltaTime;

                player.NaiveMove(moveCheckVector);

                DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();

                if (dreamBlock == null)
                {
                    bool inSolid = (bool)playerDreamDashedIntoSolid.Invoke(player, new Object[] { });
                    if (inSolid)
                    {
                        // Move the player out of the wall properly, then bounce
                        player.NaiveMove(-moveCheckVector);
                        BouncePlayer(player);

                        return true;
                    }

                }

                // Make sure we undo the check movement
                player.NaiveMove(-moveCheckVector);
            }

            return false;
        }

        public void BouncePlayer(Player player)
        {
            float speedX = Math.Abs(player.Speed.X);
            float speedY = Math.Abs(player.Speed.Y);

            bool horizontal = speedX > speedY;
            bool vertical = speedY > speedX;

            if (speedX == speedY)
            {
                // Rough check to see if this was a vertical or horizontal collision
                Vector2 horizontalMoveCheckVector = new Vector2(player.Speed.X * Engine.DeltaTime, 0f);
                Vector2 verticalMoveCheckVector = new Vector2(0f, player.Speed.Y * Engine.DeltaTime);

                player.NaiveMove(horizontalMoveCheckVector);
                horizontal = player.CollideFirst<DreamBlock>() == null;
                player.NaiveMove(-horizontalMoveCheckVector);

                player.NaiveMove(verticalMoveCheckVector);
                vertical = player.CollideFirst<DreamBlock>() == null;
                player.NaiveMove(-verticalMoveCheckVector);
            }

            if (horizontal)
            {
                player.Speed.X *= -1;
            }
            
            if (vertical)
            {
                player.Speed.Y *= -1;
            }
        }

        public void DreamDashStart(Player player, Vector2 preEnterSpeed)
        {
            if (OverrideDreamDashSpeed)
            {
                player.Speed = player.DashDir * dreamDashSpeed;
            }

            if (NeverSlowDown)
            {
                if (player.Speed.LengthSquared() < preEnterSpeed.LengthSquared())
                {
                    player.Speed = player.DashDir * preEnterSpeed.Length();
                }
            }
        }

        public void AddColors()
        {
            if (OverrideColors && !addedColors)
            {
                activeBackColorDefault = (Color)dreamBlockActiveBackColor.GetValue(null);
                disabledBackColorDefault = (Color)dreamBlockDisabledBackColor.GetValue(null);
                activeLineColorDefault = (Color)dreamBlockActiveLineColor.GetValue(null);
                disabledLineColorDefault = (Color)dreamBlockDisabledLineColor.GetValue(null);

                dreamBlockActiveBackColor.SetValue(null, activeBackColor);
                dreamBlockDisabledBackColor.SetValue(null, disabledBackColor);
                dreamBlockActiveLineColor.SetValue(null, activeLineColor);
                dreamBlockDisabledLineColor.SetValue(null, disabledLineColor);

                addedColors = true;
            }
        }

        public void RemoveColors()
        {
            if (OverrideColors)
            {
                dreamBlockActiveBackColor.SetValue(null, activeBackColorDefault);
                dreamBlockDisabledBackColor.SetValue(null, disabledBackColorDefault);
                dreamBlockActiveLineColor.SetValue(null, activeLineColorDefault);
                dreamBlockDisabledLineColor.SetValue(null, disabledLineColorDefault);
            }
        }

        private void changeDreamBlockParticleColors(DreamBlock dreamBlock)
        {
            Array particles = dreamBlockParticles.GetValue(dreamBlock) as Array;

            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    var particle = particles.GetValue(i);

                    int layer = (int)dreamBlockParticleLayer.GetValue(particle);
                    Color color = Calc.Random.Choose(particleLayerColors[layer]);

                    dreamBlockParticleColor.SetValue(particle, color);

                    particles.SetValue(particle, i);
                }
            }
        }

        public override void Added(Scene scene)
        {
            AddColors();

            base.Added(scene);
        }

        public override void Awake(Scene scene)
        {
            if (OverrideColors)
            {
                foreach (DreamBlock dreamBlock in scene.Tracker.GetEntities<DreamBlock>())
                {
                    changeDreamBlockParticleColors(dreamBlock);
                }
            }
            base.Awake(scene);
        }

        public override void Removed(Scene scene)
        {
            RemoveColors();

            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene)
        {
            RemoveColors();

            base.SceneEnd(scene);
        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();

            controller?.AttemptBounce(self);

            return orig(self);
        }

        private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();
            Vector2 preEnterSpeed = self.Speed;

            orig(self);

            controller?.DreamDashStart(self, preEnterSpeed);
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();
            bool playerHasDreamdash = (bool)dreamBlockPlayerHasDreamDash.GetValue(self);

            orig(self);

            if (playerHasDreamdash)
            {
                controller?.changeDreamBlockParticleColors(self);
            }
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();

            if (controller != null)
            {
                if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
                {
                    controller.dreamDashRedirect(self);
                }
            }

            orig(self);
        }

        public static void Load()
        {
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
        }

        public static void Unload()
        {
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
        }
    }
}
