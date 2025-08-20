using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.Addons.StructureCohesion;
using UnityEngine;

/// <summary>
/// This class manages the audio feedback when a structure part is snapped with another part.
/// To do so, it listens to the StructurePart events.
/// </summary>
public class MagnetAudioFeedback : MonoBehaviour
{

    SoundManager soundManager;
    StructurePart structurePart;

    public bool enableAudioFeedback = true;
    public string snapAudioType = "MagnetSnap";
    public string unsnapAudioType = "MagnetUnsnap";

    // Start is called before the first frame update
    void Start()
    {
        // Find the SoundManager, if not defined
        if (soundManager == null) soundManager = SoundManager.FindInstance();

        if (structurePart == null) structurePart = GetComponent<StructurePart>();
        if (structurePart != null)
        {
            structurePart.onRegisterAttachmentEvent.AddListener(OnSnap);
            structurePart.onUnregisterAttachmentEvent.AddListener(OnUnsnap);
        }
    }

    private void OnSnap(StructurePart part, AttachmentPoint attachmentPoint, Vector3 snapPosition, bool reverseAttachement)
    {
        if (enableAudioFeedback && soundManager && reverseAttachement == false && !string.IsNullOrEmpty(snapAudioType))
        {
            soundManager.PlayOneShot(snapAudioType, snapPosition);
        }
    }

    private void OnUnsnap(StructurePart part, AttachmentPoint attachmentPoint, Vector3 unsnapPosition, bool reverseAttachement)
    {
        if (enableAudioFeedback && soundManager && reverseAttachement == false && !string.IsNullOrEmpty(unsnapAudioType))
        {
            soundManager.PlayOneShot(unsnapAudioType, unsnapPosition);
        }
    }

    private void OnDestroy()
    {
        structurePart?.onRegisterAttachmentEvent?.RemoveListener(OnSnap);
        structurePart?.onUnregisterAttachmentEvent?.RemoveListener(OnUnsnap);
    }
}
