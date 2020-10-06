using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PandorasBox
{
    public enum GrappleModes
    {
        Launch,
        Grapple,
        Swing
    }

    [Tracked(true)]
    [CustomEntity("pandorasBox/grapplePoint")]
    class GrapplePoint : Actor
    {
        public float ActivationRadius = 128f;
        public float ActivationRadiusSquared;
        public GrappleModes GrappleMode = GrappleModes.Launch;
        public bool CanGrapple = true;

        public GrapplePoint(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Collider = new Hitbox(8, 8, -4, -4);

            ActivationRadiusSquared = ActivationRadius * ActivationRadius;
        }

        public override void Render()
        {
            Draw.Rect(Collider, CanGrapple ? Color.Green : Color.Red);
            Draw.Circle(Position, ActivationRadius, Color.Purple, 100);

            base.Render();
        }

        private void grappleLaunch(Player player)
        {
            Vector2 launchVector = Position - player.Center;

            player.Speed = launchVector * 5;

            Input.Grab.ConsumePress();

            CanGrapple = false;
        }

        public void HandlePlayerGrapple(Player player)
        {
            switch (GrappleMode)
            {
                case (GrappleModes.Launch):
                    grappleLaunch(player);
                    break;

                default:
                    Logger.Log(PandorasBoxMod.LoggerTag, $"Unsupported grapple mode {GrappleMode}");
                    break;
            }
        }

        public static GrapplePoint GetClosestActiveGrapple(Player player)
        {
            List<Entity> grapples = player.Scene.Tracker.GetEntities<GrapplePoint>();

            GrapplePoint closestGrapple = null;
            float closestDistance = float.PositiveInfinity;

            foreach (Entity entity in grapples)
            {
                GrapplePoint grapple = entity as GrapplePoint;

                if (grapple.CanGrapple)
                {
                    float distance = (grapple.Position - player.Center).LengthSquared();

                    if (distance <= closestDistance && distance <= grapple.ActivationRadiusSquared)
                    {
                        closestGrapple = grapple;
                        closestDistance = distance;
                    }
                }
            }

            return closestGrapple;
        }

        public static void Load()
        {
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.Render += Player_Render;
        }

        public static void Unload()
        {
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.Render -= Player_Render;
        }

        // TODO - Cache closest point
        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            orig(self);

            if (Input.Grab.Pressed)
            {
                GrapplePoint grappleTarget = GetClosestActiveGrapple(self);

                if (grappleTarget != null)
                {
                    grappleTarget.HandlePlayerGrapple(self);
                }
            }
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            GrapplePoint grappleTarget = GetClosestActiveGrapple(self);

            if (grappleTarget != null)
            {
                Draw.Line(grappleTarget.Position, self.Center, Color.Yellow);
            }

            orig(self);
        }
    }
}
