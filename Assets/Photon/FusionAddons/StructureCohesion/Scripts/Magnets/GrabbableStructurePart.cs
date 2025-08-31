using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;




namespace Fusion.Addons.StructureCohesion
{
    [DefaultExecutionOrder(GrabbableStructurePart.EXECUTION_ORDER)]
    public class GrabbableStructurePart : StructurePart
    {
        const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 10;
        [Header("Debug")]
        public bool fakeGrab = false;
        bool lastFakeGrab = false;

        NetworkGrabbable networkGrabbable;

        Pose? requestedPose;

        protected override void Awake()
        {
            base.Awake();
            networkGrabbable = GetComponentInParent<NetworkGrabbable>();
            networkGrabbable.onDidUngrab.AddListener(OnDidUngrab);

            networkGrabbable.onDidGrab.AddListener(OnDidGrabStart);
        }
        private void OnDidGrabStart(NetworkGrabber grabber)
        {
            // Try to detach if the attached point is non-grabbable
            var magnetPoint = GetComponentInParent<Fusion.Addons.StructureCohesion.MagnetStructureAttachmentPoint>();
            if (magnetPoint != null)
            {
               magnetPoint.DetachIfNonGrabbableSafe(); 
            }
        }
        private void OnDidUngrab()
        {
            AttachClosestPartInProximity();
        }

        protected override void UpdateIsMoving()
        {
            if (Object.HasStateAuthority)
            {
                IsMoving = networkGrabbable.IsGrabbed;
                IsMoving = IsMoving || fakeGrab;
            }
        }

        public override void Render()
        {
            base.Render();


            if (fakeGrab == false && lastFakeGrab)
            {
                AttachClosestPartInProximity();
            }
            lastFakeGrab = fakeGrab;
            if (networkGrabbable.isTakingAuthority)
            {
                isStill = false;
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (requestedPose is Pose pose)
            {
                MoveToPosition(pose);
                requestedPose = null;
            }
        }

        public void RequestMoveToPosition(Pose pose)
        {
            requestedPose = pose;
            if (Object.HasStateAuthority == false)
            {
                Object.RequestStateAuthority();
            }
        }

        #region Parenting
        // We want to move in FUN to be sure of aligment
        public bool CanChangeNetworkTransformImmediatly()
        {
            return (currentPhase == StructurePhase.FUN || currentPhase == StructurePhase.AfterTick);
        }
        #endregion

        public async Task WaitMoveToPosition(Pose pose)
        {
            if (CanChangeNetworkTransformImmediatly())
            {
                MoveToPosition(pose);
            } 
            else
            {
                RequestMoveToPosition(pose);
                while(requestedPose != null)
                {
                    await AsyncTask.Delay(10);
                }
            }
        }

        void MoveToPosition(Pose pose)
        {
            if(CanChangeNetworkTransformImmediatly() == false)
            {
                throw new System.Exception("Cannot move directly");
            }
            transform.position = pose.position;
            transform.rotation = pose.rotation;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            networkGrabbable?.onDidUngrab?.RemoveListener(OnDidUngrab);
        }
    }

}
