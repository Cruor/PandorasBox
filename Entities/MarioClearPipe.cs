using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

// TODO - Support red boosters and feather for player?
// TODO - Better player visuals?
// TODO - Grabing stuff going in and out of pipes might result in teleporting grabables
// TODO - Add clear pipe interacter to new entities if posibile
// TODO - Disable collidable status?
// TODO - Attributes
// - Can player enter
// - Blocking off exits
// - Transport speed?
// - Launch power?

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/clearPipe")]
    public class MarioClearPipe : Entity
    {
        // Transport speed Per second
        // TODO - More publics?
        public float TransportSpeed = 175f;
        private float transportSpeedEnterMultiplier = 0.75f;

        private int pipeWidth = 32;
        private int pipeColliderWidth = 28;
        private int pipeColliderDepth = 4;

        private string texturePath;
        private int surfaceSound;

        private bool hasPipeSolids;

        private Vector2[] nodes;

        private MarioClearPipeHelper.Direction startDirection;
        private MarioClearPipeHelper.Direction endDirection;
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

            startDirection = MarioClearPipeHelper.GetPipeExitDirection(nodes[0], nodes[1]);
            endDirection = MarioClearPipeHelper.GetPipeExitDirection(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

            startDirectionVector = MarioClearPipeHelper.GetPipeExitDirectionVector(nodes[0], nodes[1]);
            endDirectionVector = MarioClearPipeHelper.GetPipeExitDirectionVector(nodes[nodes.Length - 1], nodes[nodes.Length - 2]);

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

        private Hitbox getPipeCollider(Vector2 position, MarioClearPipeHelper.Direction exitDireciton, int pipeWidth, int pipeColliderWidth, int colliderDepth)
        {
            // Weird offset on Right/Down facing pipes with non multiples of 16 pipe width (8, 24, etc)
            switch (exitDireciton)
            {
                case MarioClearPipeHelper.Direction.Up:
                    return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - colliderDepth);

                case MarioClearPipeHelper.Direction.Right:
                    if (pipeWidth / 8 % 2 == 1)
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X - 4, position.Y - pipeColliderWidth / 2);
                    }
                    else
                    {
                        return new Hitbox(colliderDepth, pipeColliderWidth, position.X, position.Y - pipeColliderWidth / 2);
                    }

                case MarioClearPipeHelper.Direction.Down:
                    if (pipeWidth / 8 % 2 == 1)
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y - 4);
                    }
                    else
                    {
                        return new Hitbox(pipeColliderWidth, colliderDepth, position.X - pipeColliderWidth / 2, position.Y);
                    }

                case MarioClearPipeHelper.Direction.Left:
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

        private static bool canPlayerDashIntoPipe(Player player, MarioClearPipeHelper.Direction pipeDirection)
        {
            if ((Input.Dash.Pressed && player.CanDash && player.Holding == null) || player.DashAttacking)
            {
                Vector2 dashDir = Input.Dash.Pressed ? Input.GetAimVector() : player.DashDir;

                switch (pipeDirection)
                {
                    case MarioClearPipeHelper.Direction.Up:
                        return dashDir.Y > 0;

                    case MarioClearPipeHelper.Direction.Right:
                        return dashDir.X < 0;

                    case MarioClearPipeHelper.Direction.Down:
                        return dashDir.Y < 0;

                    case MarioClearPipeHelper.Direction.Left:
                        return dashDir.X > 0;

                    default:
                        return false;
                }
            }

            return false;
        }

        // TODO - Cleanup and make more generic 
        private IEnumerator pipeMovement(Entity entity, bool fromStart, bool canBounceBack=true, Vector2? forcedStartPosition=null)
        {
            MarioClearPipeInteraction interaction = MarioClearPipeHelper.GetClearPipeInteraction(entity);

            int startIndex = fromStart ? 0 : nodes.Length - 1;
            int lastIndex = fromStart ? nodes.Length - 1 : 0;
            int direction = fromStart ? 1 : -1;

            MarioClearPipeHelper.Direction transportStartDirection = fromStart ? startDirection : endDirection;
            MarioClearPipeHelper.Direction transportEndDirection = fromStart ? endDirection : startDirection;

            if (forcedStartPosition != null || MarioClearPipeHelper.CanTransportEntity(entity, transportStartDirection))
            {
                MarioClearPipeHelper.CurrentlyTransportedEntities.Add(entity);
                interaction.CurrentClearPipe = this;
                interaction?.OnPipeEnter?.Invoke(entity, transportStartDirection);

                if (forcedStartPosition != null)
                {
                    entity.Position = forcedStartPosition.Value;
                }

                Vector2 fromNode = entity.Position;
                Vector2 toNode = nodes[startIndex];

                float distance = (fromNode - toNode).Length();
                float moved = 0f;

                Vector2 movementDirection = Vector2.Zero;

                // Gracefully attempt to move to the first node
                while (entity != null && moved <= distance && distance != 0f)
                {
                    if (!interaction.CanStayInPipe(entity))
                    {
                        interaction.CurrentClearPipe = null;
                        MarioClearPipeHelper.CurrentlyTransportedEntities.Remove(entity);

                        yield break;
                    }

                    entity.Position = Vector2.Lerp(fromNode, toNode, moved / distance) + Vector2.Lerp(Vector2.Zero, interaction.PipeRenderOffset, moved / distance);

                    moved += TransportSpeed * Engine.DeltaTime * transportSpeedEnterMultiplier;

                    yield return null;
                }
    
                moved -= distance;

                // Follow the nodes
                for (int i = startIndex; i != lastIndex; i += direction)
                {
                    fromNode = nodes[i];
                    toNode = nodes[i + direction];

                    distance = (fromNode - toNode).Length();

                    while (entity != null && moved <= distance && distance != 0f)
                    {
                        if (!interaction.CanStayInPipe(entity))
                        {
                            interaction.CurrentClearPipe = null;
                            MarioClearPipeHelper.CurrentlyTransportedEntities.Remove(entity);

                            yield break;
                        }

                        entity.Position = Vector2.Lerp(fromNode, toNode, moved / distance) + interaction.PipeRenderOffset;

                        moved += TransportSpeed * Engine.DeltaTime;

                        yield return null;
                    }

                    moved -= distance;
                    movementDirection = (toNode - fromNode).SafeNormalize();
                }

                // Check if we can exit the pipe
                // If we can exit, visually update these steps
                // Otherwise bounce back early
                if (MarioClearPipeHelper.CanExitPipe(entity, movementDirection, TransportSpeed))
                {
                    Vector2 previousPosition = entity.Position;
                    Vector2 currentPosition = entity.Position;

                    bool colliding = entity?.CollideFirst<MarioClearPipeSolid>() != null;

                    while (entity != null && entity.Scene != null && colliding)
                    {
                        entity.Position += movementDirection * TransportSpeed * Engine.DeltaTime;

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

                // Send back if it gets stuck in a solid
                if (entity != null && entity.Scene != null && entity.CollideCheck<Solid>())
                {
                    if (canBounceBack)
                    {
                        entity.Position = toNode;

                        yield return pipeMovement(entity, !fromStart, false, toNode);
                    }
                    else
                    {
                        Logger.Log("PB", $"Pipe blocked for {entity}");

                        interaction?.OnPipeBlocked?.Invoke(entity, MarioClearPipeHelper.Direction.None);
                        interaction.CurrentClearPipe = null;
                        MarioClearPipeHelper.CurrentlyTransportedEntities.Remove(entity);

                        // Fix float positions, causes weird collision bugs for entities
                        entity.Position = new Vector2((int)Math.Round(entity.Position.X), (int)Math.Round(entity.Position.Y));
                    }
                }
                else
                {
                    interaction.OnPipeExit?.Invoke(entity, transportEndDirection);
                    interaction.CurrentClearPipe = null;
                    MarioClearPipeHelper.CurrentlyTransportedEntities.Remove(entity);

                    // Fix float positions, causes weird collision bugs for entities
                    entity.Position = new Vector2((int)Math.Round(entity.Position.X), (int)Math.Round(entity.Position.Y));
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

                if (entity.Collider.Collide(startCollider) && MarioClearPipeHelper.CanTransportEntity(entity, startDirection))
                {
                    Add(new Coroutine(pipeMovement(entity, true)));
                }
                else if (entity.Collider.Collide(endCollider) && MarioClearPipeHelper.CanTransportEntity(entity, endDirection))
                {
                    Add(new Coroutine(pipeMovement(entity, false)));
                }
            }

            base.Update();
        }

        public override void Awake(Scene scene)
        {
            MarioClearPipeHelper.AddClearPipeInteractionToHoldables(scene);

            if (hasPipeSolids)
            {
                addPipeSolids(pipeWidth);
            }

            base.Awake(scene);
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            if (!MarioClearPipeHelper.HasClearPipeInteraction(self))
            {
                MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(new Vector2(0f, 10f));

                interaction.OnPipeBlocked = (entity, direction) =>
                {
                    Player player = entity as Player;

                    if (player != null && !player.Dead)
                    {
                        player.Die(Vector2.Zero);
                    }
                };

                interaction.OnPipeEnter = (entity, direction) =>
                {
                    Player player = entity as Player;

                    if (player != null)
                    {
                        player.StateMachine.State = Player.StDummy;
                        player.StateMachine.Locked = true;
                        player.DummyGravity = false;
                        player.DummyAutoAnimate = false;
                        player.ForceCameraUpdate = true;
                        player.Speed = Vector2.Zero;

                        player.Sprite.Play("spin");
                    }
                };

                interaction.OnPipeExit = (entity, direction) =>
                {
                    Player player = entity as Player;
                    MarioClearPipeInteraction playerInteraction = entity?.Get<MarioClearPipeInteraction>();

                    if (player != null && playerInteraction != null)
                    {
                        float transportSpeed = playerInteraction.CurrentClearPipe.TransportSpeed;

                        player.StateMachine.Locked = false;
                        player.StateMachine.State = Player.StNormal;
                        player.DummyGravity = true;
                        player.DummyAutoAnimate = true;
                        player.ForceCameraUpdate = false;

                        switch (direction)
                        {
                            case MarioClearPipeHelper.Direction.Up:
                                player.Speed = new Vector2(0f, -transportSpeed);
                                break;

                            case MarioClearPipeHelper.Direction.Right:
                                if (Input.MoveX.Value >= 0)
                                {
                                    player.Speed = new Vector2(transportSpeed, 0f);
                                }
                                
                                break;

                            case MarioClearPipeHelper.Direction.Down:
                                player.Speed = new Vector2(0f, transportSpeed);
                                break;

                            case MarioClearPipeHelper.Direction.Left:
                                if (Input.MoveX.Value <= 0)
                                {
                                    player.Speed = new Vector2(-transportSpeed, 0f);
                                }
                                
                                break;

                            default:
                                player.Speed = Vector2.Zero;
                                break;
                        }
                    }
                };

                interaction.CanEnterPipe = (entity, direction) => {
                    Player player = entity as Player;

                    if (player.OnGround())
                    {
                        // If the player is visually ducking or pushing up against a solid
                        bool canDuckInto = player.Sprite.CurrentAnimationID == "duck" && direction == MarioClearPipeHelper.Direction.Up;
                        bool canPushInto = player.Sprite.CurrentAnimationID == "push" && (direction == MarioClearPipeHelper.Direction.Left || direction == MarioClearPipeHelper.Direction.Right);

                        if (canDuckInto || canPushInto)
                        {
                            return true;
                        }
                    }

                    return canPlayerDashIntoPipe(player, direction);
                };

                interaction.CanStayInPipe = (entity) =>
                {
                    Player player = entity as Player;

                    return player != null && !player.Dead;
                };

                self.Add(interaction);
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
