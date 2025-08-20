using Fusion;
using Fusion.Addons.Learning.UI;
using Fusion.Addons.StructureCohesion;
using UnityEngine;

namespace Fusion.Addons.Learning.ActivityTemplates.UI
{
    /// <summary>
    /// This class is in charge of resetting the naming activity.
    /// It restores the flag visuals and places the flags back on the tray (restoring the preconfiguredAttachedPoint)
    /// </summary>
    public class ResetNamingActivity : ResetActivity
    {
        [SerializeField] GameObject tray;


        protected override void Reset(NetworkTransform networkTransform)
        {
            base.Reset(networkTransform);
            RestoreFlagStatus(networkTransform);
            RestoreAttachmentPoints(networkTransform);
        }

        private void RestoreFlagStatus(NetworkTransform networkTransform)
        {
            var namingFlag = networkTransform.GetComponent<NamingFlag>();
            if (namingFlag != null)
            {
                namingFlag.RestoreFlagStatus();
            }
        }

        protected override void WillReset()
        {
            base.WillReset();
            // We first reset the tray in order to have the flag properly attached with good position
            var trayNT = tray.GetComponent<NetworkTransform>();
            foreach (var nt in pendingResetNetworkTransforms)
            {
                foreach (var magnetAttachmentPoint in nt.GetComponentsInChildren<MagnetStructureAttachmentPoint>())
                {
                    if (magnetAttachmentPoint.AttachedPoint != null)
                    {
                        magnetAttachmentPoint.RequestAttachmentDeletion(magnetAttachmentPoint.AttachedPoint);
                    }
                }
            }

            if (trayNT != null)
            {
                Reset(trayNT);
                pendingResetNetworkTransforms.Remove(trayNT);
            }

        }

        private void RestoreAttachmentPoints(NetworkTransform networkTransform)
        {
            foreach (var magnetAttachmentPoint in networkTransform.GetComponentsInChildren<MagnetStructureAttachmentPoint>())
            {
                if (magnetAttachmentPoint.preconfiguredAttachedPoint != null)
                {
                    magnetAttachmentPoint.RequestAttachmentStorage(magnetAttachmentPoint.preconfiguredAttachedPoint);
                }
            }
        }


    }
}
