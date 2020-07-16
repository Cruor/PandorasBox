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

// TODO - Better player visuals?
// TODO - Move all player pipe interaction away
// TODO - Expose HasPipeSolids, players can "grab" air when exiting horizontal pipeless pipes now
// - Make it use a method lookup for each state, defaulting to current behavior, lets mods easily use the system
// TODO - Attributes
// - Can player enter
// - Blocking off exits
// - Transport speed?
// - Launch power?
// - Force dash entry?
// - Disable dash entry?

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/clearPipe")]
    public class MarioClearPipe : Entity
    {
        // Transport speed Per second
        // TODO - More publics?
        public float TransportSpeed { get; protected set; } = 175f;
        private float transportSpeedEnterMultiplier = 0.75f;

        private int pipeWidth = 32;
        private int pipeColliderWidth = 28;
        private int pipeColliderDepth = 4;

        private string texturePath;
        private int surfaceSound;

        private bool hasPipeSolids;

        private Vector2[] nodes;

        private Direction startDirection;
        private Direction endDirection;
        private Vector2 startDirectionVector;
        private Vector2 endDirectionVector;

        private Hitbox startCollider;
        private Hitbox endCollider;

        private List<MarioClearPipeSolid> pipeSolids;

        public MarioClearPipe(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            nodes = data.NodesWithPosition(offset);

            removeRedundantNodes();

            texturePath = data.Attr("texture", "glass");
            surfaceSound = data.Int("surfaceSound", -1);

            hasPipeSolids = data.Bool("hasPipeSolids", true);

            // Debug attributes
            TransportSpeed = data.Float("debugTransportSpeed", 175f);
            transportSpeedEnterMultiplier = data.Float("debugTransportSpeedEnterMultiplier", 0.75f);

            pipeWidth = data.Int("debugPipeWidth", 32);
            pipeColliderWidth = data.Int("debugPipeColliderWidth", 28);
            pipeColliderDepth = data.Int("debugPipeColliderDepth", 4);
            // ----

            startDirection = GetPipeExitDirection(nodes[0], nodes[1]);
            endDirection = GetPipeExitDirection(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

            startDirectionVector = GetPipeExitDirectionVector(nodes[0], nodes[1]);
            endDirectionVector = GetPipeExitDirectionVector(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

            startCollider = getPipeCollider(Vector2.Zero, startDirection, pipeWidth, pipeColliderWidth, pipeColliderDepth);
            endCollider = getPipeCollider(new Vector2(nodes.Last().X - nodes.First().X, nodes.Last().Y - nodes.First().Y), endDirection, pipeWidth, pipeColliderWidth, pipeColliderDepth);

            Collider = new ColliderList(startCollider, endCollider);

            pipeSolids = new List<MarioClearPipeSolid>();
        }

        // Node is considered redundant if it does not change the direction
        private void removeRedundantNodes()
        {
            List<Vector2> newNodes = new List<Vector2>();

            Vector2 previousNode = Vector2.Zero;
            Vector2 previousDirection = Vector2.Zero;
            bool hasPreviousNode = false;

            foreach (Vector2 node in nodes)
            {
                if (hasPreviousNode)
                {
                    Vector2 direction = (previousNode - node).SafeNormalize();

                    if (Math.Abs(direction.X - previousDirection.X) > 0.0005 || Math.Abs(direction.Y - previousDirection.Y) > 0.0005)
                    {
                        newNodes.Add(previousNode);
                    }

                    previousDirection = direction;
                }

                hasPreviousNode = true;
                previousNode = node;
            }

            newNodes.Add(nodes.Last());

            nodes = newNodes.ToArray();
        }

        private Hitbox getPipeCollider(Vector2 position, Direction exitDireciton, int pipeWidth, int pipeColliderWidth, int colliderDepth)
        {
            // Weird offset on Right/Down facing pipes with non multiples of 16 pipe width (8, 24, etc)
            switch (exitDireciton)
            {
                case Direction.Up:
                    return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - colliderDepth);

                case Direction.Right:
                    if (pipeWidth / 8 % 2 == 1 && nodes.Length > 2)
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X - 4, position.Y - pipeColliderWidth / 2);
                    }
                    else
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X, position.Y - pipeColliderWidth / 2);
                    }

                case Direction.Down:
                    if (pipeWidth / 8 % 2 == 1 && nodes.Length > 2)
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - 4);
                    }
                    else
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y);
                    }

                case Direction.Left:
                    return new Hitbox(colliderDepth, pipeColliderWidth, position.X - colliderDepth, position.Y - pipeColliderWidth / 2);

                default:
                    return new Hitbox(pipeWidth, pipeWidth, position.X - pipeWidth / 2, position.Y - pipeWidth / 2);
            }
        }

        private void addPipeSolids(int pipeWidth)
        {
            Vector2 previousNode = Vector2.Zero;
            bool hasPreviousNode = false;

            int i = 0;

            float halfPipeWidth = pipeWidth / 2f;

            foreach (Vector2 node in nodes)
            {
                if (hasPreviousNode)
                {
                    bool startNodeExit = i == 1;
                    bool endNodeExit = i == nodes.Count() - 1;

                    Vector2 nextNode = nodes.ElementAtOrDefault(i + 1);

                    MarioClearPipeSolid pipeSolid = MarioClearPipeSolid.FromNodes(previousNode, node, nextNode, startNodeExit, endNodeExit, pipeWidth, texturePath, surfaceSound);

                    pipeSolids.Add(pipeSolid);
                    Scene.Add(pipeSolid);
                }

                hasPreviousNode = true;
                previousNode = node;
                i++;
            }
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

        private IEnumerator moveBetweenNodes(Entity entity, MarioClearPipeInteraction interaction, Vector2 from, Vector2 to, float? travelSpeed=null, bool? lerpPipeOffset=null)
        {
            interaction.Distance = (from - to).Length();

            interaction.DirectionVector = GetPipeExitDirectionVector(to, from);
            interaction.Direction = GetPipeExitDirection(to, from);

            interaction.From = from;
            interaction.To = to;

            interaction.TravelSpeed = travelSpeed != null ? travelSpeed.Value : interaction.TravelSpeed;
            interaction.LerpPipeOffset = lerpPipeOffset != null ? lerpPipeOffset.Value : interaction.LerpPipeOffset;

            while (entity != null && interaction.Moved <= interaction.Distance && interaction.Distance != 0f && !interaction.ExitEarly)
            {
                interaction?.OnPipeUpdate(entity, interaction);

                float lerpValue = interaction.Moved / interaction.Distance;

                entity.Position = Vector2.Lerp(from, to, lerpValue) + (interaction.LerpPipeOffset ? Vector2.Lerp(Vector2.Zero, interaction.PipeRenderOffset, lerpValue) : interaction.PipeRenderOffset);
                interaction.Moved += interaction.TravelSpeed * Engine.DeltaTime;

                yield return null;
            }

            interaction.Moved -= interaction.Distance;
        }

        // Visually update exiting steps
        // Correct for exit overshooting
        private IEnumerator exitPipeMovement(Entity entity, MarioClearPipeInteraction interaction)
        {
            Vector2 previousPosition = entity.Position;
            Vector2 currentPosition = entity.Position;

            bool colliding = entity?.CollideFirst<MarioClearPipeSolid>() != null;

            while (entity != null && entity.Scene != null && colliding)
            {
                entity.Position += interaction.DirectionVector * TransportSpeed * Engine.DeltaTime;

                previousPosition = currentPosition;
                currentPosition = entity.Position;

                colliding = entity.CollideFirst<MarioClearPipeSolid>() != null;

                if (colliding)
                {
                    yield return null;
                }
            }

            // Correct for overshooting the exit, attempt to place entity as close as possible to the pipe

            Vector2 lowerValue = previousPosition;
            Vector2 upperValue = currentPosition;
            Vector2 lastUnblocked = entity.Position;

            while (entity != null && entity.Scene != null && (lowerValue - upperValue).LengthSquared() > 0.5f)
            {
                entity.Position = Vector2.Lerp(lowerValue, upperValue, 0.5f);

                if (entity.CollideFirst<MarioClearPipeSolid>() != null)
                {
                    lowerValue = entity.Position;
                }
                else
                {
                    upperValue = entity.Position;
                    lastUnblocked = entity.Position;
                }
            }

            entity.Position = lastUnblocked;
        }

        private void ejectFromPipe(Entity entity, MarioClearPipeInteraction interaction)
        {
            interaction.OnPipeExit?.Invoke(entity, interaction);
            interaction.CurrentClearPipe = null;

            CurrentlyTransportedEntities.Remove(entity);

            // Fix float positions, causes weird collision bugs for entities
            entity.Position = new Vector2((int)Math.Round(entity.Position.X), (int)Math.Round(entity.Position.Y));
        }

        // TODO - Cleanup and make more generic 
        private IEnumerator pipeMovement(Entity entity, bool fromStart, bool canBounceBack=true, Vector2? forcedStartPosition=null)
        {
            MarioClearPipeInteraction interaction = GetClearPipeInteraction(entity);

            int startIndex = fromStart ? 0 : nodes.Length - 1;
            int lastIndex = fromStart ? nodes.Length - 1 : 0;
            int direction = fromStart ? 1 : -1;

            Direction transportStartDirection = fromStart ? startDirection : endDirection;
            Direction transportEndDirection = fromStart ? endDirection : startDirection;

            if (forcedStartPosition != null || CanTransportEntity(entity, transportStartDirection))
            {
                CurrentlyTransportedEntities.Add(entity);
                interaction.CurrentClearPipe = this;
                interaction.ExitEarly = false;
                interaction?.OnPipeEnter?.Invoke(entity, interaction);

                // Check if we are entering the pipe or bouncing back from a blocked exit
                if (forcedStartPosition != null)
                {
                    entity.Position = forcedStartPosition.Value;
                }
                else
                {
                    // Gracefully attempt to move to the first node
                    yield return moveBetweenNodes(entity, interaction, entity.Position, nodes[startIndex], TransportSpeed * transportSpeedEnterMultiplier, true);
                }

                // Follow the nodes
                for (int i = startIndex; i != lastIndex && !interaction.ExitEarly; i += direction)
                {
                    yield return moveBetweenNodes(entity, interaction, nodes[i], nodes[i + direction], TransportSpeed, false);
                }

                if (interaction.ExitEarly)
                {
                    ejectFromPipe(entity, interaction);

                    yield break;
                }

                // Check if we can exit the pipe
                if (CanExitPipe(entity, interaction.DirectionVector, TransportSpeed))
                {
                    yield return exitPipeMovement(entity, interaction);
                }

                // Send back if it gets stuck in a solid
                if (entity != null && entity.Scene != null && entity.CollideCheck<Solid>())
                {
                    if (canBounceBack)
                    {
                        entity.Position = nodes[lastIndex] + interaction.PipeRenderOffset;

                        yield return pipeMovement(entity, !fromStart, false, entity.Position);
                    }
                    else
                    {
                        ejectFromPipe(entity, interaction);
                    }
                }
                else
                {
                    ejectFromPipe(entity, interaction);
                }
            }
        }

        public override void Update()
        {
            foreach (Entity entity in Scene.Entities)
            {
                if (entity.Collider == null)
                {
                    continue;
                }

                if (entity.Collider.Collide(startCollider))
                {
                    AddClearPipeInteraction(entity);

                    if (CanTransportEntity(entity, startDirection)) {
                        Add(new Coroutine(pipeMovement(entity, true)));
                    }
                }
                else if (entity.Collider.Collide(endCollider))
                {
                    AddClearPipeInteraction(entity);

                    if (CanTransportEntity(entity, endDirection))
                    {
                        Add(new Coroutine(pipeMovement(entity, false)));
                    }
                }
            }

            base.Update();
        }

        public override void Awake(Scene scene)
        {
            if (hasPipeSolids)
            {
                addPipeSolids(pipeWidth);
            }

            base.Awake(scene);
        }

        // TODO - Move
        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            if (!HasClearPipeInteraction(self))
            {
                MarioClearPipeInteraction pipeInteraction = new MarioClearPipeInteraction(new Vector2(0f, 10f));

                pipeInteraction.OnPipeBlocked = (entity, direction) =>
                {
                    Player player = entity as Player;

                    if (player != null && !player.Dead)
                    {
                        player.Die(Vector2.Zero);
                    }
                };

                pipeInteraction.OnPipeEnter = (entity, direction) =>
                {
                    Player player = entity as Player;

                    if (player != null)
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
                };

                pipeInteraction.OnPipeExit = (entity, interaction) =>
                {
                    Player player = entity as Player;

                    if (player != null && interaction != null)
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
                            if ((player.Speed.X < 0 && Input.MoveX > 0 || player.Speed.X > 0 && Input.MoveX < 0) && Input.Grab.Check)
                            {
                                player.Speed = Vector2.Zero;
                            }
                        }

                        if (player.StateMachine.State == Player.StRedDash)
                        {
                            player.DashDir = player.Speed.SafeNormalize();
                        }
                    }
                };

                pipeInteraction.CanEnterPipe = (entity, direction) => {
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
                };

                pipeInteraction.OnPipeUpdate = (entity, interaction) =>
                {
                    Player player = entity as Player;

                    if (player != null && player.Dead)
                    {
                        interaction.ExitEarly = true;
                    }
                };

                self.Add(pipeInteraction);
            }

            orig(self, scene);
        }

        public static void Load()
        {
            On.Celeste.Player.Added += Player_Added;
        }

        public static void Unload()
        {
            On.Celeste.Player.Added -= Player_Added;
        }
    }
}
