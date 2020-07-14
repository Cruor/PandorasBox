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
    public class MarioClearPipeHelper
    {
        public enum Direction
        {
            Up,
            Right,
            Down,
            Left,
            None
        }

        public static HashSet<Entity> CurrentlyTransportedEntities = new HashSet<Entity>();

        public static void HoldableOnPipeBlocked(Entity entity, Direction direction)
        {
            Actor actor = entity as Actor;

            if (actor != null)
            {
                // Attempt to force a squish by moving the colliding solid by 0 pixels
                Solid solid = actor.CollideFirst<Solid>(actor.Position);
                if (solid != null)
                {
                    solid.MoveHExact(0);
                }
            }
            else
            {
                entity?.Scene?.Remove(entity);
            }
        }

        public static void HoldableOnPipeEnter(Entity entity, Direction direction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null)
            {
                // Reset speeds and remove holder
                holdable.Release(Vector2.Zero);
            }
        }

        public static void HoldableOnPipeExit(Entity entity, Direction direction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null && entity.Scene != null)
            {
                Vector2 speed = Vector2.Zero;

                switch (direction)
                {
                    case Direction.Left:
                        speed = new Vector2(-1.0f, 0.1f);
                        break;

                    case Direction.Right:
                        speed = new Vector2(1.0f, -0.1f);
                        break;

                    case Direction.Up:
                        speed = new Vector2(0, -1.0f);
                        break;

                    case Direction.Down:
                        speed = new Vector2(0, 1.0f);
                        break;

                    default:
                        break;
                }

                holdable.Release(speed);
            }
        }

        public static bool HoldableCanEnterPipe(Entity entity, Direction direction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null && !holdable.IsHeld)
            {
                Vector2 speed = holdable?.SpeedGetter() ?? Vector2.Zero;

                switch (direction)
                {
                    case Direction.Left:
                        return speed.X > 0;

                    case Direction.Right:
                        return speed.X < 0;

                    case Direction.Up:
                        return speed.Y > 0;

                    case Direction.Down:
                        return speed.Y < 0;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static bool HoldableCanStayInPipe(Entity entity)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null)
            {
                return !holdable.IsHeld;
            }

            return true;
        }

        public static Vector2 GetPipeExitDirectionVector(Vector2 exit, Vector2 previous)
        {
            return (previous - exit).SafeNormalize();
        }

        public static Direction GetPipeExitDirection(Vector2 exit, Vector2 previous)
        {
            if (exit.X < previous.X)
            {
                return Direction.Left;
            }
            else if (exit.X > previous.X)
            {
                return Direction.Right;
            }
            else if (exit.Y < previous.Y)
            {
                return Direction.Up;
            }
            else if (exit.Y > previous.Y)
            {
                return Direction.Down;
            }

            return Direction.None;
        }

        public static bool CanExitPipe(Entity entity, Vector2 movementDirection, float transportSpeed, int checks=16)
        {
            if (entity != null)
            {
                Vector2 originalPosition = entity.Position;

                for (int i = 0; i < checks; i++)
                {
                    entity.Position += movementDirection * transportSpeed * (1f / 60f);

                    if (entity != null && entity.Scene != null && entity.CollideFirst<MarioClearPipeSolid>() == null)
                    {
                        bool result = !entity.CollideCheck<Solid>();

                        entity.Position = originalPosition;

                        return result;
                    }
                }

                entity.Position = originalPosition;

                return false;
            }

            return false;
        }

        public static bool CanTransportEntity(Entity entity, Direction direction)
        {
            if (entity != null && !CurrentlyTransportedEntities.Contains(entity))
            {
                MarioClearPipeInteraction interaction = GetClearPipeInteraction(entity);

                return interaction?.CanEnterPipe?.Invoke(entity, direction) == true;
            }

            return false;
        }

        public static MarioClearPipeInteraction GetClearPipeInteraction(Entity entity)
        {
            return entity?.Get<MarioClearPipeInteraction>();
        }

        public static bool HasClearPipeInteraction(Entity entity)
        {
            return GetClearPipeInteraction(entity) != null;
        }

        public static void AddClearPipeInteractionToHoldables(Scene scene)
        {
            foreach (Entity entity in scene.Entities)
            {
                Holdable holdable = entity.Get<Holdable>();

                // Depends on SpeedGetter and OnRelease being defined
                if (holdable != null && holdable.SpeedGetter != null && holdable.OnRelease != null && !HasClearPipeInteraction(entity))
                {
                    Vector2 pipeOffset = Vector2.Zero;

                    if (entity.Collider != null)
                    {
                        pipeOffset = -entity.Collider.Center;
                    }

                    MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(pipeOffset);

                    interaction.OnPipeBlocked = MarioClearPipeHelper.HoldableOnPipeBlocked;
                    interaction.OnPipeEnter = MarioClearPipeHelper.HoldableOnPipeEnter;
                    interaction.OnPipeExit = MarioClearPipeHelper.HoldableOnPipeExit;

                    interaction.CanEnterPipe = MarioClearPipeHelper.HoldableCanEnterPipe;
                    interaction.CanStayInPipe = MarioClearPipeHelper.HoldableCanStayInPipe;

                    entity.Add(interaction);
                }
            }
        }
    }
}
