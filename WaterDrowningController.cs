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
    [Tracked(true)]
    [CustomEntity("pandorasBox/waterDrowningController")]
    class WaterDrowningController : Entity
    {
        private static MethodInfo playerSwimCheck = typeof(Player).GetMethod("SwimCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo playerSwimUnderwaterCheck = typeof(Player).GetMethod("SwimUnderwaterCheck", BindingFlags.NonPublic | BindingFlags.Instance);

        public float WaterDuration;
        public float WaterDrownDuration;
        public string Mode;
        public bool Flashing;

        public WaterDrowningController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            WaterDuration = 0;
            WaterDrownDuration = data.Float("maxDuration", 10f);
            Mode = data.Attr("mode", "Swimming");
        }

        private bool playerInWater(Player player)
        {
            if (player != null)
            {
                if (Mode == "Swimming")
                {
                    return (bool)playerSwimCheck.Invoke(player, new object[] { });
                }
                else if (Mode == "Diving")
                {
                    return (bool)playerSwimUnderwaterCheck.Invoke(player, new object[] { });
                }
            }

            return false;
        }

        public override void Update()
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();

            if (player != null)
            {
                bool inWater = playerInWater(player);

                if (inWater)
                {
                    WaterDuration += Engine.DeltaTime;
                }
                else
                {
                    WaterDuration = 0f;
                }

                if (inWater && WaterDuration >= WaterDrownDuration && !player.Dead)
                {
                    player.Die(Vector2.Zero);
                }
            }

            float interval = 0f;

            if (WaterDuration > WaterDrownDuration * 0.7)
            {
                interval = 0.6f;
            }
            else if (WaterDuration > WaterDrownDuration * 0.5)
            {
                interval = 1.0f;
            }
            else if (WaterDuration > WaterDrownDuration * 0.3)
            {
                interval = 1.4f;
            }

            if (interval > 0 && base.Scene.OnInterval(interval) && player != null && !player.Dead)
            {
                Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
                Flashing = !Flashing;
            }

            base.Update();
        }

        public static void Load()
        {
            On.Celeste.Player.Render += Player_OnRender;
        }

        public static void Unload()
        {
            On.Celeste.Player.Render -= Player_OnRender;
        }

        private static void Player_OnRender(On.Celeste.Player.orig_Render orig, Player self)
        {
            WaterDrowningController controller = self.Scene.Tracker.GetEntity<WaterDrowningController>();

            if (controller != null && controller.WaterDuration > 0)
            {
                float stamina = self.Stamina;
                self.Stamina = controller.WaterDuration + 2 > controller.WaterDrownDuration ? 0 : Player.ClimbMaxStamina;

                orig(self);

                self.Stamina = stamina;
            }

            else
            {
                orig(self);
            }
        }
    }
}
