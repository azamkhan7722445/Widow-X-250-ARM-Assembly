using Fusion.Addons.Containment;
using Fusion.XRShared.GrabbableMagnet;
using UnityEngine;

namespace Fusion.Addons.StructureCohesion
{
    public class MagnetStructureAttachmentPoint : MagnetAttachmentPoint, IStructurePartPoint, IMagnetConfigurator
    {
        [Header("Grab Settings")]
        public bool IsNonGrabbable = false; // Agar tick hai  grab ke sath move nahi karega
        public bool dis = false;
        #region Structure
        public Structure CurrentStructure => StructurePart == null ? null : StructurePart.CurrentStructure;
        public StructurePart StructurePart { get; set; } = null;
        #endregion

        #region IMagnetGroupIdentificator


        // MagnetStructureAttachmentPoint ke andar add karo

        public void DetachIfNonGrabbable()
        {
            // Agar attached object hai aur wo MagnetStructureAttachmentPoint type ka hai
            if (AttachedPoint != null && AttachedPoint is MagnetStructureAttachmentPoint magnetPoint)
            {
                print("anees");
                // Agar attached object non-grabbable hai
                if (magnetPoint.IsNonGrabbable)
                {
                    print("anees");
                    Debug.Log($"Detaching non-grabbable: {magnetPoint.gameObject.name}");

                    // Detach logic
                    magnetPoint.AttachedPoint = null;
                    this.AttachedPoint = null;

                    // Reset internal references
                    magnetPoint.attachedToPoint = null;
                    magnetPoint.requestedNewAttachedPoint = null;

                    // Optional: detach from structure
                    magnetPoint.StructurePart = null;
                }
            }
        }

        public void Update()
        {
            if (dis)
            {
                DetachIfNonGrabbable();
                dis = false;
            }
        }

        public bool IsInSameGroup(IMagnetConfigurator otherIdentificator)
        {
            if (otherIdentificator is MagnetStructureAttachmentPoint otherAttachmentPoint)
            {
                if (CurrentStructure == null && otherAttachmentPoint == this)
                {
                    return true;
                }
                if (CurrentStructure != null && otherAttachmentPoint.CurrentStructure == CurrentStructure)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMagnetActive()
        {
            if (AttachedPoint != null || attachedToPoint != null || requestedNewAttachedPoint != null)
            {
                return false;
            }
            return true;
        }
        #endregion

        protected override bool ApplyAutomaticMagnetAttachment => false;

        protected override void Awake()
        {
            base.Awake();
            StructurePart = GetComponentInParent<StructurePart>();
            if (attractableMagnet != null)
            {
                attractableMagnet.CheckOnUngrab = false;
                attractableMagnet.MagnetConfigurator = this;
            }

            if (attractorMagnet != null)
            {
                attractorMagnet.MagnetConfigurator = this;
            }
        }

        //  Modified logic
        public override bool IsValidAttractingMagnet(IAttractorMagnet otherMagnet, out AttachmentPoint attachmentPoint)
        {
            var structurePoint = otherMagnet.MagnetConfigurator as MagnetStructureAttachmentPoint;
            attachmentPoint = structurePoint;

            if (structurePoint == null)
                return false;

            // Agar ye NonGrabbable hai aur dusra Grabbable hai  sirf logical connection
            if (IsNonGrabbable && !structurePoint.IsNonGrabbable)
            {
                return true; // connection hoga but move nahi karega
            }

            return true;
        }

        //  Grabbed hone par check
        public void OnGrabbed()
        {
            if (IsNonGrabbable)
            {
                Debug.Log($"{gameObject.name} is Non-Grabbable. It will not move with grab.");
                return; // Movement block
            }

            Debug.Log($"{gameObject.name} grabbed normally.");
        }
    }
}
