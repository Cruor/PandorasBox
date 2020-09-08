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
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.PandorasBox
{
    public class ValueHolder<T>
    {
        public T value;

        public ValueHolder(T value)
        {
            this.value = value;
        }
    }

    [Tracked]
    [CustomEntity("pandorasBox/playerClone")]
    class CloneSpawner : Actor
    {
        private Player clone;
        private string flag;
        private int id;
        private bool active;
        private string visualMode;

        private static FieldInfo spinnerOffset = typeof(CrystalStaticSpinner).GetField("offset", BindingFlags.Instance | BindingFlags.NonPublic);
        private static ConditionalWeakTable<CrystalStaticSpinner, ValueHolder<float>> spinnerOffsets = new ConditionalWeakTable<CrystalStaticSpinner, ValueHolder<float>>();

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

        private static float getSpinnerOffset(CrystalStaticSpinner spinner)
        {
            if (spinnerOffsets.TryGetValue(spinner, out var holder)) {
                return holder.value;
            }
            else
            {
                float offset = (float)spinnerOffset.GetValue(spinner);

                spinnerOffsets.Add(spinner, new ValueHolder<float>(offset));

                return offset;
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
            On.Celeste.Mod.Entities.DialogCutscene.OnBegin += DialogCutscene_OnBegin;
            On.Celeste.Mod.Entities.DialogCutscene.OnEnd += DialogCutscene_OnEnd;

            On.Celeste.CrystalStaticSpinner.Update += CrystalStaticSpinner_Update;

            On.Celeste.Player.Die += Player_OnDie;

            On.Celeste.Lookout.Interact += Lookout_Interact;
            On.Celeste.Lookout.LookRoutine += Lookout_LookRoutine;
            On.Celeste.Lookout.StopInteracting += Lookout_StopInteracting;
        }

        public static void Unload()
        {
            On.Celeste.Mod.Entities.DialogCutscene.OnBegin -= DialogCutscene_OnBegin;
            On.Celeste.Mod.Entities.DialogCutscene.OnEnd -= DialogCutscene_OnEnd;

            On.Celeste.CrystalStaticSpinner.Update -= CrystalStaticSpinner_Update;

            On.Celeste.Player.Die -= Player_OnDie;

            On.Celeste.Lookout.Interact -= Lookout_Interact;
            On.Celeste.Lookout.LookRoutine -= Lookout_LookRoutine;
            On.Celeste.Lookout.StopInteracting -= Lookout_StopInteracting;
        }

        private static void CrystalStaticSpinner_Update(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self)
        {
            bool visibleBeforeOrig = self.Visible;

            orig(self);

            var players = self.Scene.Tracker.GetEntities<Player>();

            if (players.Count > 1 && visibleBeforeOrig && !self.Collidable && self.Scene.OnInterval(0.05f, getSpinnerOffset(self)))
            {
                foreach (Player entity in players)
                {
                    self.Collidable = self.Collidable || (Math.Abs(entity.X - self.X) < 128f && Math.Abs(entity.Y - self.Y) < 128f);
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
                if (!SaveData.Instance.Assists.Invincible || evenIfInvincible)
                {
                    foreach (Player player in players)
                    {
                        if (player != self)
                        {
                            foreach (Follower follower in player.Leader.Followers)
                            {
                                self.Leader.Followers.Add(follower);
                            }

                            player.Scene.Add(new CustomPlayerDeadBody(player, Vector2.Zero, false, true));
                            player.Scene.Remove(player);
                        }
                    }

                    return orig(self, direction, evenIfInvincible, registerDeathsInStats);
                }
            }

            return null;
        }

        private static IEnumerator Lookout_LookRoutine(On.Celeste.Lookout.orig_LookRoutine orig, Lookout self, Player player)
        {
            yield return orig(self, player);

            foreach (Player player2 in self.SceneAs<Level>().Tracker.GetEntities<Player>())
            {
                player2.StateMachine.State = Player.StNormal;
            }

            yield break;
        }

        private static void Lookout_StopInteracting(On.Celeste.Lookout.orig_StopInteracting orig, Lookout self)
        {
            orig(self);

            foreach (Player player2 in self.SceneAs<Level>().Tracker.GetEntities<Player>())
            {
                player2.StateMachine.State = Player.StNormal;
            }
        }

        private static void Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player)
        {
            orig(self, player);

            foreach (Player player2 in self.SceneAs<Level>().Tracker.GetEntities<Player>())
            {
                player2.StateMachine.State = Player.StDummy;
            }
        }
    }
}
