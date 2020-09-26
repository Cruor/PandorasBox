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

        private static ConditionalWeakTable<Player, ValueHolder<int>> playerStates = new ConditionalWeakTable<Player, ValueHolder<int>>();
        private static ConditionalWeakTable<Player, ValueHolder<Vector2>> playerPreEnterSpeeds = new ConditionalWeakTable<Player, ValueHolder<Vector2>>();

        private static FieldInfo playerDashCooldownTimerMethod = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo dreamBlockActiveBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockActiveLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticles = typeof(DreamBlock).GetField("particles", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Type dreamBlockParticleType = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticleLayer = dreamBlockParticleType.GetField("Layer", BindingFlags.Instance | BindingFlags.Public);
        private static FieldInfo dreamBlockParticleColor = dreamBlockParticleType.GetField("Color", BindingFlags.Instance | BindingFlags.Public);

        public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            AllowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
            AllowDreamDashRedirection = data.Bool("allowDreamDashRedirect", true);
            OverrideDreamDashSpeed = data.Bool("overrideDreamDashSpeed", false);
            OverrideColors = data.Bool("overrideColors", false);
            NeverSlowDown = data.Bool("neverSlowDown", false);

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

        private void dreamDashStart(Player player)
        {
            Vector2 enterSpeed = getPlayerPreEnterSpeed(player);

            if (OverrideDreamDashSpeed)
            {
                player.Speed = player.DashDir * dreamDashSpeed;
            }

            if (NeverSlowDown)
            {
                if (player.Speed.LengthSquared() < enterSpeed.LengthSquared())
                {
                    player.Speed = player.DashDir * enterSpeed.Length();
                }
            }
        }

        private int getNewPlayerState(Player player)
        {
            int currentState = player.StateMachine.State;

            if (playerStates.TryGetValue(player, out var holder))
            {
                if (holder.value != currentState)
                {
                    holder.value = currentState;

                    return currentState;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                playerStates.Add(player, new ValueHolder<int>(currentState));
            }

            return -1;
        }

        private void setPlayerPreEnterSpeed(Player player)
        {
            if (playerPreEnterSpeeds.TryGetValue(player, out var holder))
            {
                holder.value = player.Speed;
            }
            else
            {
                playerPreEnterSpeeds.Add(player, new ValueHolder<Vector2>(player.Speed));
            }
        }

        private Vector2 getPlayerPreEnterSpeed(Player player)
        {
            if (playerPreEnterSpeeds.TryGetValue(player, out var holder))
            {
                return holder.value;
            }
            else
            {
                return Vector2.Zero;
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

        public override void Update()
        {
            foreach (Player player in Scene.Tracker.GetEntities<Player>())
            {
                if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
                {
                    dreamDashRedirect(player);
                }

                if (player.StateMachine.State != Player.StDreamDash)
                {
                    setPlayerPreEnterSpeed(player);
                }

                if (getNewPlayerState(player) == Player.StDreamDash)
                {
                    dreamDashStart(player);
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
    }
}
