using Fusion;
using Fusion.Addons.Learning.UI;
using Fusion.Addons.StructureCohesion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is in charge of resetting the drone assembly activity.
/// It restores the magnet visuals of drone parts and deletes the attached points of all StructurePart attachment points 
/// </summary>

public class ResetDroneAssemblyActivity : ResetActivity
{
    StructurePartsManager structurePartsManager;
    [SerializeField]
    List<MagnetPointVisual> magnetPointVisuals = new List<MagnetPointVisual>();

    [SerializeField]
    List<GrabbableStructurePart> grabbableStructureParts = new List<GrabbableStructurePart>();

    protected override void Awake()
    {
        base.Awake();
        foreach (NetworkTransform networkTransform in networkTransformsToReset)
        {
            magnetPointVisuals.AddRange(networkTransform.GetComponentsInChildren<MagnetPointVisual>(true));
            grabbableStructureParts.AddRange(networkTransform.GetComponentsInChildren<GrabbableStructurePart>(true));
        }
    }

    protected override void Start()
    {
        base.Start();
        if (structurePartsManager == null)
            structurePartsManager = FindAnyObjectByType<StructurePartsManager>(FindObjectsInactive.Include);
    }

    protected override void InitialReset()
    {
        base.InitialReset();
        RestoreMagnetVisual();
        RestoreStructureAttachment();
    }

    private void RestoreStructureAttachment()
    {
        // reset structurepart
        foreach (GrabbableStructurePart grabbableStructurePart in grabbableStructureParts)
        {
            foreach(var a in grabbableStructurePart.attachmentPoints)
            {
                if (a.IsAttachementSource)
                {
                    a.RequestAttachmentDeletion(a.AttachedPoint);
                }
            }
        }
    }

    private void RestoreMagnetVisual()
    {
        // reset MagnetPointVisual
        foreach (MagnetPointVisual magnetPointVisual in magnetPointVisuals)
        {
            magnetPointVisual.UpdateVisuals(true);
        }
    }
}
