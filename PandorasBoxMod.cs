using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using NLua;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions;

namespace Celeste.Mod.PandorasBox
{
    public class PandorasBoxMod : EverestModule
    {
        public static PandorasBoxMod Instance;

        public override Type SettingsType => null;

        public PandorasBoxMod()
        {
            Instance = this;
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            FlagToggleSwitch.LoadContent();
        }

        public override void Load()
        {
            CloneSpawner.Load();
            WaterDrowningController.Load();
            TimeField.Load();
            MarioClearPipe.Load();

            InteractionRegistry.Add(new HoldableInteraction());
            InteractionRegistry.Add(new PlayerInteraction());

            InteractionRegistry.Load();
        }

        public override void Unload()
        {
            CloneSpawner.Unload();
            WaterDrowningController.Unload();
            TimeField.Unload();
            MarioClearPipe.Unload();
            InteractionRegistry.Unload();
        }
    }
}
