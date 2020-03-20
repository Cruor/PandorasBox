using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/dreamDashController")]
    class DreamDashController : Entity
    {
        private bool allowSameDirectionDash;
        private float sameDirectionSpeedMultiplier;

        public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            allowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
            sameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1.0f);
        }

        public override void Update()
        {
            if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
            {
                foreach (Player entity in Scene.Tracker.GetEntities<Player>())
                {
                    if (entity.StateMachine.State == Player.StDreamDash && entity.CanDash)
                    {
                        bool sameDirection = Input.GetAimVector() == entity.DashDir;

                        if (!sameDirection || allowSameDirectionDash)
                        {
                            entity.DashDir = Input.GetAimVector();
                            entity.Speed = entity.DashDir * entity.Speed.Length();
                            entity.Dashes = Math.Max(0, entity.Dashes - 1);

                            Audio.Play("event:/char/madeline/dreamblock_enter");

                            if (sameDirection)
                            {
                                entity.Speed *= sameDirectionSpeedMultiplier;
                                entity.DashDir *= Math.Sign(sameDirectionSpeedMultiplier);
                            }

                            Input.Dash.ConsumeBuffer();
                        }
                    }
                }
            }
        }
    }
}
