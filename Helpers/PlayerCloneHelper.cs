using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PandorasBox
{
    class PlayerCloneHelper
    {
        public static Dictionary<String, PlayerSpriteMode> modes = new Dictionary<string, PlayerSpriteMode>()
        {
            {"Backpack", PlayerSpriteMode.Madeline},
            {"NoBackpack", PlayerSpriteMode.MadelineNoBackpack},
            {"MadelineAsBadeline", PlayerSpriteMode.MadelineAsBadeline}
        };

        public static Player CreatePlayer(Level level, EntityData data, Vector2 offset)
        {
            string mode = data.Attr("mode");
            Vector2 position = data.Position + offset;

            return CreatePlayer(level, position, mode);
        }

        public static Player CreatePlayer(Level level, Vector2 position, string mode="Inventory")
        {
            if (modes.TryGetValue(mode, out var spriteMode))
            {
                return new Player(position, spriteMode);
            }

            bool hasBackpack = level.Session.Inventory.Backpack;

            return new Player(position, hasBackpack ? PlayerSpriteMode.Madeline : PlayerSpriteMode.MadelineNoBackpack);
        }
    }
}
