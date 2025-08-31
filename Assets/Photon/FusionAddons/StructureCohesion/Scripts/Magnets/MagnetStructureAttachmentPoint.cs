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

        public void DetachIfNonGrabbableSafe()
        {
            if (AttachedPoint != null && AttachedPoint is MagnetStructureAttachmentPoint magnetPoint)
            {
                print("anees");
                if (magnetPoint.IsNonGrabbable)
                {
                    Debug.Log($"Detaching non-grabbable safely: {magnetPoint.gameObject.name}");

                    // Proper Fusion-safe detach
                    magnetPoint.UnregisterAttachment(magnetPoint.AttachedPoint, true); // true for change detection
                    this.UnregisterAttachment(this.AttachedPoint, true);
                    print("anees1");
                    // Optional: reset local reference after unregister
                    magnetPoint.AttachedPoint = null;
                    this.AttachedPoint = null;
                    print("anees3");
                }
            }
        }


        public void Update()
        {
            if (dis)
            {
                DetachIfNonGrabbableSafe();
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
