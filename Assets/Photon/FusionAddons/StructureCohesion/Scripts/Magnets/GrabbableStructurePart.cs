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
        private AttachmentPoint currentAttachmentPoint;
        Pose? requestedPose;

        protected override void Awake()
        {
            base.Awake();
            networkGrabbable = GetComponentInParent<NetworkGrabbable>();

            // Subscribe to grab/ungrab events
            networkGrabbable.onDidUngrab.AddListener(OnDidUngrab);
            networkGrabbable.onDidGrab.AddListener(OnGrabStart);
        }

        private void OnGrabStart(NetworkGrabber grabber)
        {
            Debug.Log("Grab started");

            if (currentAttachmentPoint != null && currentAttachmentPoint.AttachedPoint != null)
            {
                var otherMagnet = currentAttachmentPoint.AttachedPoint as MagnetStructureAttachmentPoint;
                if (otherMagnet != null && otherMagnet.IsNonGrabbable)
                {
                    Debug.Log("Auto-detaching non-grabbable on grab");

                    // Proper detach
                    currentAttachmentPoint.RequestAttachmentDeletion();

                    // Clear both sides reference
                    otherMagnet.Detach();
                    currentAttachmentPoint = null;
                }
            }
        }

        private void OnDidUngrab()
        {
            AttachClosestPartInProximity();
        }

        /// <summary>
        /// Called by AttachmentPoint when this object gets attached.
        /// </summary>
        public void SetCurrentAttachment(AttachmentPoint newPoint)
        {
            currentAttachmentPoint = newPoint;
        }

        /// <summary>
        /// Called by AttachmentPoint when detached.
        /// </summary>
        public void ClearCurrentAttachment()
        {
            currentAttachmentPoint = null;
        }

        protected override void UpdateIsMoving()
        {
            if (Object.HasStateAuthority)
            {
                IsMoving = networkGrabbable.IsGrabbed || fakeGrab;
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
                while (requestedPose != null)
                {
                    await AsyncTask.Delay(10);
                }
            }
        }

        void MoveToPosition(Pose pose)
        {
            if (!CanChangeNetworkTransformImmediatly())
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
            networkGrabbable?.onDidGrab?.RemoveListener(OnGrabStart);
        }
    }
}
