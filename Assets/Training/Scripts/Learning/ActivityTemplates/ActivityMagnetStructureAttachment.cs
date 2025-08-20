using Fusion.Addons.StructureCohesion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fusion.Addons.Learning.ActivityTemplates
{
    /// <summary>
    /// This class handle the progress of an activity whose completion is based on every attachmentPoints (specified, or in children otherwise) to be attached. 
    /// It implepents the IAttachmentListener interface to manage a list of attached AttachmentPoints (in order to compute the Progress variable)
    /// </summary>
    public class ActivityMagnetStructureAttachment : LearnerActivityTracker, IAttachmentListener
    {
        [Tooltip("To reach a full progress, all those attachment points need to be attached (or attaching) another point")]
        [SerializeField] List<MagnetStructureAttachmentPoint> attachmentPoints = new List<MagnetStructureAttachmentPoint>();
        [Tooltip("Detected points already attached")]
        [SerializeField] List<AttachmentPoint> attachedAttachmentPoints = new List<AttachmentPoint>();


        #region LearnerActivityTracker override
        protected override void Awake()
        {
            base.Awake();
            if (attachmentPoints == null || attachmentPoints.Count == 0)
            {
                attachmentPoints = GetComponentsInChildren<MagnetStructureAttachmentPoint>().ToList();
            }
            if (attachmentPoints.Count > 0)
            {
                Debug.Log($"{attachmentPoints.Count} MagnetStructureAttachmentPoint found ");
            }
            else
            {
                Debug.LogError($"{name} No MagnetStructureAttachmentPoint found ");
            }
        }

        public override void CheckActivityProgress()
        {
            base.CheckActivityProgress();
            if (ActivityStatusOfLearner == Status.ReadyToStart && attachedAttachmentPoints.Count != 0 && Progress != 1)
            {
                ActivityStatusOfLearner = Status.Started;
            }
        }
        #endregion

        #region Attachment listeners
        public void OnRegisterAttachment(AttachmentPoint attachedTo, AttachmentPoint attachedPoint, bool changeDetectionDuringRender)
        {
            if (attachedAttachmentPoints.Contains(attachedPoint) == false)
            {
                attachedAttachmentPoints.Add(attachedPoint);
            }
            if (attachedAttachmentPoints.Contains(attachedTo) == false)
            {
                attachedAttachmentPoints.Add(attachedTo);
            }

            if (Object.HasStateAuthority)
            {
                Progress = (float)attachedAttachmentPoints.Count / attachmentPoints.Count;
            }
        }

        public virtual void OnUnregisterAttachment(AttachmentPoint attachedTo, AttachmentPoint attachedPoint, bool changeDetectionDuringRender)
        {
            if (attachedAttachmentPoints.Contains(attachedPoint))
            {
                attachedAttachmentPoints.Remove(attachedPoint);
            }
            if (attachedAttachmentPoints.Contains(attachedTo))
            {
                attachedAttachmentPoints.Remove(attachedTo);
            }

            if (Object.HasStateAuthority)
            {
                Progress = (float)attachedAttachmentPoints.Count / attachmentPoints.Count;
            }
        }
        #endregion


        #region unused Attachment listeners
        public virtual void OnRegisterReverseAttachment(AttachmentPoint attachedTo, AttachmentPoint attachedPoint, bool changeDetectionDuringRender) { }
        public virtual void OnUnregisterReverseAttachment(AttachmentPoint attachedTo, AttachmentPoint attachedPoint, bool changeDetectionDuringRender) { }
        #endregion
    }
}
