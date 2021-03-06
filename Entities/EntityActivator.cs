﻿using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/entityActivator")]
    class EntityActivator : Trigger
    {
        public enum EffectModes
        {
            ActivateInsideDeactivateOutside,
            ActivateInside,
            ActivateOutside,
            DeactivateInside,
            DeactivateOutside,
            ActivateOnScreenDeactivateOffScreen
        }

        public enum ActivationModes
        {
            OnEnter,
            OnStay,
            OnLeave,
            OnFlagActive,
            OnFlagInactive,
            OnFlagActivated,
            OnFlagDeactivated,
            OnUpdate,
            OnCameraMoved,
            OnAwake
        }

        public EffectModes Mode;
        public ActivationModes ActivationMode;

        public HashSet<Type> Targets;

        public bool UseTracked;

        public string Flag;

        public float UpdateInterval;

        private bool previousFlagValue = false;
        private bool updateFlagValues = false;

        private Vector2 previousCameraPosition;
        private float previousCameraZoom;

        public EntityActivator(EntityData data, Vector2 offset) : base(data, offset)
        {
            Mode = data.Enum<EffectModes>("mode", EffectModes.ActivateInsideDeactivateOutside);
            ActivationMode = data.Enum<ActivationModes>("activationMode", ActivationModes.OnEnter);

            Targets = new HashSet<Type>(TypeHelper.GetTypesFromString(data.Attr("targets", "")).Distinct());

            UseTracked = data.Bool("useTracked", true);

            Flag = data.Attr("flag", "");

            UpdateInterval = data.Float("updateInterval", -1f);
        }

        public override void OnEnter(Player player)
        {
            if (ActivationMode == ActivationModes.OnEnter)
            {
                UpdateEntities();
            }
        }

        public override void OnStay(Player player)
        {
            if (ActivationMode == ActivationModes.OnStay)
            {
                UpdateEntities();
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player); if (ActivationMode == ActivationModes.OnLeave)
            {
                UpdateEntities();
            }
        }

        public override void Update()
        {
            if (ActivationMode == ActivationModes.OnUpdate && OnInterval())
            {
                UpdateEntities();
            }
            else if (ActivationMode == ActivationModes.OnCameraMoved)
            {
                Camera camera = SceneAs<Level>().Camera;

                if (Math.Abs(camera.X - previousCameraPosition.X) > 8 || Math.Abs(camera.Y - previousCameraPosition.Y) > 8 || camera.Zoom != previousCameraZoom)
                {
                    UpdateEntities();

                    previousCameraPosition = camera.Position;
                    previousCameraZoom = camera.Zoom;
                }
            }
            else if (updateFlagValues)
            {
                bool currentFlagValue = SceneAs<Level>().Session.GetFlag(Flag);

                if (ActivationMode == ActivationModes.OnFlagActive && currentFlagValue && OnInterval())
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagInactive && !currentFlagValue && OnInterval())
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagActivated && currentFlagValue && !previousFlagValue)
                {
                    UpdateEntities();
                }
                else if (ActivationMode == ActivationModes.OnFlagDeactivated && !currentFlagValue && previousFlagValue)
                {
                    UpdateEntities();
                }

                previousFlagValue = currentFlagValue;
            }

            base.Update();
        }

        public override void Awake(Scene scene)
        {
            if (!string.IsNullOrEmpty(Flag))
            {
                previousFlagValue = SceneAs<Level>()?.Session?.GetFlag(Flag) ?? false;
                updateFlagValues = true;
            }

            Camera camera = SceneAs<Level>().Camera;

            previousCameraPosition = camera.Position;
            previousCameraZoom = camera.Zoom;

            if (ActivationMode == ActivationModes.OnAwake)
            {
                UpdateEntities();
            }
        }

        public void UpdateEntities()
        {
            switch (Mode)
            {
                case EffectModes.ActivateInsideDeactivateOutside:
                    ActivateInsideDeactivateOutside();
                    break;

                case EffectModes.ActivateInside:
                    ActivateInside();
                    break;

                case EffectModes.DeactivateInside:
                    DeactivateInside();
                    break;

                case EffectModes.ActivateOutside:
                    ActivateOutside();
                    break;

                case EffectModes.DeactivateOutside:
                    DeactivateOutside();
                    break;

                case EffectModes.ActivateOnScreenDeactivateOffScreen:
                    ActivateOnScreenDeactivateOffScreen();
                    break;

                default:
                    Logger.Log(PandorasBoxMod.LoggerTag, $"Unsupported entity activator mode: {Mode}");
                    break;
            }
        }

        public List<Entity> FindTargetEntities()
        {
            return UseTracked ? FindTargetEntitiesTracked() : FindTargetEntitiesUntracked();
        }

        public List<Entity> FindTargetEntitiesUntracked()
        {
            List<Entity> entities = Scene.Entities.Where(entity => Targets.Contains(entity.GetType())).ToList();

            return entities;
        }

        public List<Entity> FindTargetEntitiesTracked()
        {
            List<Entity> entities = new List<Entity>();

            foreach (Type type in Targets)
            {
                if (type != null && Scene.Tracker.Entities.ContainsKey(type))
                {
                    entities.AddRange(Scene.Tracker.Entities[type]);
                }
            }

            return entities;
        }

        public bool OnInterval()
        {
            return UpdateInterval <= 0f || Scene.OnInterval(UpdateInterval);
        }

        public void UpdateTarget(Entity target, bool visible, bool active, bool collidable)
        {
            target.Visible = visible;
            target.Active = active;
            target.Collidable = collidable;
        }

        public bool EntityInside(Entity target)
        {
            if (target.Collider == null)
            {
                return target.X >= Collider.AbsoluteLeft && target.X <= Collider.AbsoluteRight && target.Y >= Collider.AbsoluteTop && target.Y <= Collider.AbsoluteBottom;
            }

            return target.Collider.Collide(Collider);
        }

        public void ActivateInsideDeactivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
                else
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        public void ActivateInside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
            }
        }

        public void DeactivateInside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        public void ActivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (!EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
            }
        }

        public void DeactivateOutside()
        {
            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (!EntityInside(entity))
                {
                    UpdateTarget(entity, false, false, false);
                }
            }
        }

        private void ActivateOnScreenDeactivateOffScreen()
        {
            Collider triggerCollider = Collider;
            Camera camera = SceneAs<Level>().Camera;
            float cameraWidth = camera.Right - camera.Left;
            float cameraHeight = camera.Bottom - camera.Top;

            Collider = new Hitbox(cameraWidth * 3, cameraHeight * 3, camera.Position.X - cameraWidth - Position.X, camera.Position.Y - cameraHeight - Position.Y);

            List<Entity> targets = FindTargetEntities();

            foreach (Entity entity in targets)
            {
                if (EntityInside(entity))
                {
                    UpdateTarget(entity, true, true, true);
                }
                else
                {
                    UpdateTarget(entity, false, false, false);
                }
            }

            Collider = triggerCollider;
        }
    }
}
