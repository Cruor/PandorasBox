using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

using static Celeste.Mod.PandorasBox.MarioClearPipeHelper;

namespace Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions
{
    class PlayerInteraction : BaseInteraction
    {
        public override void Load()
        {
            base.Load();

            On.Celeste.Player.Added += Player_Added;
        }

        public override void Unload()
        {
            base.Unload();

            On.Celeste.Player.Added -= Player_Added;
        }

        private static bool canPlayerDashIntoPipe(Player player, Direction pipeDirection)
        {
            if ((Input.Dash.Pressed && player.CanDash) || player.DashAttacking)
            {
                Vector2 dashDir = Input.Dash.Pressed ? Input.GetAimVector() : player.DashDir;

                switch (pipeDirection)
                {
                    case Direction.Up:
                        return dashDir.Y > 0;

                    case Direction.Right:
                        return dashDir.X < 0;

                    case Direction.Down:
                        return dashDir.Y < 0;

                    case Direction.Left:
                        return dashDir.X > 0;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static void PlayerOnPipeBlocked(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead)
            {
                player.Die(Vector2.Zero);
            }
        }

        public static void PlayerOnPipeEnter(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead)
            {
                if (player.StateMachine.State != Player.StRedDash)
                {
                    player.StateMachine.State = Player.StDummy;
                }

                player.StateMachine.Locked = true;
                player.DummyGravity = false;
                player.DummyAutoAnimate = false;
                player.ForceCameraUpdate = true;
                player.Speed = Vector2.Zero;

                player.Sprite.Play("spin");
            }
        }

        public static void PlayerOnPipeExit(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead && interaction != null)
            {
                player.StateMachine.Locked = false;
                player.DummyGravity = true;
                player.DummyAutoAnimate = true;
                player.ForceCameraUpdate = false;

                if (player.StateMachine.State != Player.StRedDash)
                {
                    player.StateMachine.State = Player.StNormal;
                }

                player.Speed = interaction.DirectionVector * interaction.CurrentClearPipe.TransportSpeed;

                if (Math.Abs(player.Speed.X) > 0.707)
                {
                    if (interaction.CurrentClearPipe.HasPipeSolids && (player.Speed.X < 0 && Input.MoveX > 0 || player.Speed.X > 0 && Input.MoveX < 0) && Input.Grab.Check && player.StateMachine.State != Player.StRedDash)
                    {
                        player.Speed = Vector2.Zero;
                    }
                }

                if (player.StateMachine.State == Player.StRedDash)
                {
                    player.DashDir = player.Speed.SafeNormalize();
                }
            }
        }

        public static bool PlayerCanEnterPipe(Entity entity, Direction direction)
        {
            Player player = entity as Player;

            if (player.Holding != null)
            {
                return false;
            }

            if (player.OnGround())
            {
                // If the player is visually ducking or pushing up against a solid
                bool canDuckInto = player.Sprite.CurrentAnimationID == "duck" && direction == Direction.Up;
                bool canPushInto = player.Sprite.CurrentAnimationID == "push" && (direction == Direction.Left || direction == Direction.Right);

                if (canDuckInto || canPushInto)
                {
                    return true;
                }
            }
            else
            {
                // Player holds up near a downwards facing pipe
                if (Input.MoveY < 0 && direction == Direction.Down && player.Speed.Y < 0)
                {
                    return true;
                }
            }

            return canPlayerDashIntoPipe(player, direction);
        }

        public static void PlayerOnPipeUpdate(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && player.Dead)
            {
                interaction.ExitEarly = true;
            }
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            if (!HasClearPipeInteraction(self))
            {
                MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(new Vector2(0f, 10f));

                interaction.OnPipeBlocked = PlayerOnPipeBlocked;
                interaction.OnPipeEnter = PlayerOnPipeEnter;
                interaction.OnPipeExit = PlayerOnPipeExit;
                interaction.CanEnterPipe = PlayerCanEnterPipe;
                interaction.OnPipeUpdate = PlayerOnPipeUpdate;

                self.Add(interaction);
            }

            orig(self, scene);
        }
    }
}
