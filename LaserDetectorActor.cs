using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(true)]
    class LaserDetectorActor : Actor
    {
        private int detectionDelay = 0;
        private HashSet<Laserbeam> handledBeams;

        public int DetectionDelay {
            get {
                return detectionDelay;
            }
            set
            {
                detectionDelay = value;
                laserbeamPaq = new PeekAQueue<List<Laserbeam>>(detectionDelay + 1);
            }
        }

        public Collider LaserBlockingCollider = null;
        public Collider LaserDetectionCollider = null;

        protected PeekAQueue<List<Laserbeam>> laserbeamPaq;

        public LaserDetectorActor(Vector2 position) : base(position)
        {
            DetectionDelay = 0;
            handledBeams = new HashSet<Laserbeam>();
        }

        public virtual void OnNewLaserbeam(Laserbeam beam)
        {

        }

        public virtual void OnDeadLaserbeam(Laserbeam beam)
        {
            
        }

        public virtual void OnLaserbeams(List<Laserbeam> laserbeams)
        {

        }

        public virtual Collider GetLaserBlockingCollider(Laserbeam beam)
        {
            return LaserBlockingCollider;
        }

        public virtual bool LaserBlockingCheck(Laserbeam beam)
        {
            Collider origCollider = Collider;
            Collider = GetLaserBlockingCollider(beam);

            bool colliding = Collide.Check(beam, this);

            Collider = origCollider;

            return colliding;
        }

        public override void Update()
        {
            laserbeamPaq.Enqueue(LaserHelper.ConnectedLasers(Scene, this));

            if (laserbeamPaq.Count > DetectionDelay)
            {
                List<Laserbeam> laserbeams = laserbeamPaq.Peek(DetectionDelay);
                OnLaserbeams(laserbeams);
                HashSet<Laserbeam> handledThisUpdate = new HashSet<Laserbeam>();

                foreach (Laserbeam beam in laserbeams)
                {
                    if (!handledBeams.Contains(beam))
                    {
                        handledBeams.Add(beam);
                        OnNewLaserbeam(beam);
                    }

                    handledThisUpdate.Add(beam);
                }

                foreach (Laserbeam beam in handledBeams.Except(handledThisUpdate).ToArray())
                {
                    handledBeams.Remove(beam);
                    OnDeadLaserbeam(beam);
                }
            }

            base.Update();
        }
    }
}
