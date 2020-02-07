using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/playerClone")]
    class CloneSpawner : Actor
    {
        private Player clone;
        private string flag;
        private int id;
        private bool active;
        private string visualMode;

        private bool getFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_clone_spawner_" + id : flag;

                return level.Session.GetFlag(targetFlag);
            }

            return false;
        }

        private void handleClone()
        {
            Level level = Scene as Level;

            if (active)
            {
                if (clone == null)
                {
                    clone = PlayerCloneHelper.CreatePlayer(level, Position, visualMode);
                    level.Add(clone);
                }
            }
            else
            {
                if (clone != null)
                {
                    clone.RemoveSelf();
                    clone = null;
                }
            }
        }

        public override void Added(Scene scene)
        {
            active = getFlag();

            base.Added(scene);
        }

        public override void Removed(Scene scene)
        {
            active = false;
            handleClone();

            base.Removed(scene);
        }

        public override void Update()
        {
            active = getFlag();
            handleClone();

            base.Update();
        }

        public CloneSpawner(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            flag = data.Attr("flag");
            id = data.ID;
            visualMode = data.Attr("mode", "Inventory");
        }

        public static void Load()
        {
            On.Celeste.CrystalStaticSpinner.Update += CrystalStaticSpinner_Update;
            On.Celeste.Mod.Entities.DialogCutscene.OnBegin += DialogCutscene_OnBegin;
            On.Celeste.Mod.Entities.DialogCutscene.OnEnd += DialogCutscene_OnEnd;
            On.Celeste.CrystalStaticSpinner.Update += CrystalStaticSpinner_Update;
            On.Celeste.Player.Die += Player_OnDie;
        }

        public static void Unload()
        {
            On.Celeste.CrystalStaticSpinner.Update -= CrystalStaticSpinner_Update;
            On.Celeste.Mod.Entities.DialogCutscene.OnBegin -= DialogCutscene_OnBegin;
            On.Celeste.Mod.Entities.DialogCutscene.OnEnd -= DialogCutscene_OnEnd;
            On.Celeste.CrystalStaticSpinner.Update -= CrystalStaticSpinner_Update;
            On.Celeste.Player.Die -= Player_OnDie;
        }

        private static void CrystalStaticSpinner_Update(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self)
        {
            orig(self);

            if (!self.Collidable)
            {
                foreach (Player entity in self.Scene.Tracker.GetEntities<Player>())
                {
                    if (Math.Abs(entity.X - self.X) < 128f && Math.Abs(entity.Y - self.Y) < 128f)
                    {
                        self.Collidable = true;

                        break;
                    }
                }
            }
        }

        private static void DialogCutscene_OnBegin(On.Celeste.Mod.Entities.DialogCutscene.orig_OnBegin orig, DialogCutscene self, Level level)
        {
            foreach (Player player in self.Scene.Tracker.GetEntities<Player>())
            {
                player.StateMachine.State = 11;
                player.StateMachine.Locked = true;
                player.ForceCameraUpdate = true;
            }

            orig(self, level);
        }

        private static void DialogCutscene_OnEnd(On.Celeste.Mod.Entities.DialogCutscene.orig_OnEnd orig, DialogCutscene self, Level level)
        {
            foreach (Player player in self.Scene.Tracker.GetEntities<Player>())
            {
                player.StateMachine.Locked = false;
                player.StateMachine.State = 0;
                player.ForceCameraUpdate = false;
            }

            orig(self, level);
        }

        private static PlayerDeadBody Player_OnDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible=false, bool registerDeathsInStats=true)
        {
            IEnumerable<Player> players = self.Scene.Tracker.GetEntities<Player>().Cast<Player>();
            int playerCount = players.Count();
            int deadPlayers = players.Where(p => p.Dead).Count();
            PlayerDeadBody body = self.Scene.Entities.FindFirst<PlayerDeadBody>();

            // If there is a single player there will never be a dead body in the screen when they die
            if (body == null)
            {
                return orig(self, direction, evenIfInvincible, registerDeathsInStats);
            }

            return null;
        }
    }
}
