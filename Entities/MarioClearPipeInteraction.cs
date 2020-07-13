using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;


namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    public class MarioClearPipeInteraction : Component
    {
        public delegate void OnPipeInteractionDelegate(Entity entity, MarioClearPipeHelper.Direction direction);
        public delegate bool CanEnterPipeDelegate(Entity entity, MarioClearPipeHelper.Direction direction);
        public delegate bool CanStayInPipeDelegate(Entity entity);

        public OnPipeInteractionDelegate OnPipeEnter;
        public OnPipeInteractionDelegate OnPipeExit;
        public OnPipeInteractionDelegate OnPipeBlocked;

        public CanEnterPipeDelegate CanEnterPipe = (entity, direction) => true;
        public CanStayInPipeDelegate CanStayInPipe = (entity) => true;

        public Vector2 PipeRenderOffset;

        public MarioClearPipe CurrentClearPipe;

        public MarioClearPipeInteraction(Vector2 renderOffset) : base(true, true)
        {
            PipeRenderOffset = renderOffset;
        }
    }
}
