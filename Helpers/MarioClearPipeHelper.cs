using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions;
using Celeste.Mod.PandorasBox.Entities;

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

        public static Vector2 GetPipeExitDirectionVector(Vector2 exit, Vector2 previous)
        {
            return (exit - previous).SafeNormalize();
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

        public static bool AddClearPipeInteraction(Entity entity)
        {
            if (!HasClearPipeInteraction(entity))
            {
                foreach (BaseInteraction baseInteraction in InteractionRegistry.BaseInteractions)
                {
                    if (baseInteraction.AddInteraction(entity))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
