using Fusion;
using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.Addons.StructureCohesion;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.Learning.ActivityTemplates
{
    /// <summary>
    /// This class manages the naming activity flag.
    /// It listens to the flag attachmentPoint events and updates the flag status according to the attached point tag.
    /// Because flagStatus is a networked variable, all players are notified when the value changedand and can update the flag renderer.
    /// </summary>

    public class NamingFlag : NetworkBehaviour
    {
        [SerializeField] string namingTag;
        [SerializeField] AttachmentPoint attachmentPoint;
        [SerializeField] TMP_Text textMeshProUGUI;

        [Header("Visual Feedback")]
        [SerializeField] Material notDefinedMaterial;
        [SerializeField] Material goodAnswerMaterial;
        [SerializeField] Material badAnswerMaterial;
        [SerializeField] MeshRenderer resultMeshRenderer;

        [Header("Audio Feedback")]
        [SerializeField] bool enableAudioFeedback = true;
        [SerializeField] SoundManager soundManager;
        [SerializeField] string goodAnswerAudioType = "GoodAnswer";
        [SerializeField] string badAnswerAudioType = "BadAnswer";
        [SerializeField] string unsnapAudioType = "MagnetUnsnap";
        [SerializeField] string defaultSnapAudioType = "MagnetSnap";

        public UnityEvent onFlagStatusChanged;

        public enum FlagStatus
        {
            goodPosition,
            badPosition,
            notDefined
        }

        [SerializeField]
        [Networked, OnChangedRender(nameof(OnFlagStatusChanged))]
        public FlagStatus flagStatus { get; set; } = FlagStatus.notDefined;


        protected virtual void Awake()
        {
            // Find the SoundManager, if not defined
            if (soundManager == null) soundManager = SoundManager.FindInstance();

            if (attachmentPoint == null)
                attachmentPoint = GetComponent<AttachmentPoint>();
            if (attachmentPoint == null)
                Debug.LogError("attachmentPoint not defined");

            if (goodAnswerMaterial == null)
                Debug.LogError("goodAnswerMaterial not defined");

            if (badAnswerMaterial == null)
                Debug.LogError("badAnswerMaterial not defined");

            if (resultMeshRenderer == null)
                Debug.LogError("resultMeshRenderer not defined");

            if (textMeshProUGUI == null)
                textMeshProUGUI = GetComponentInChildren<TMP_Text>();
            if (textMeshProUGUI)
                textMeshProUGUI.text = namingTag;
        }

        public override void Spawned()
        {
            base.Spawned();
            UpdateFlagRenderer();
        }

        protected virtual void OnFlagStatusChanged()
        {
            if (onFlagStatusChanged != null) onFlagStatusChanged.Invoke();
            UpdateFlagRenderer();
        }

        public virtual void RestoreFlagStatus()
        {
            if (flagStatus != FlagStatus.notDefined)
            {
                flagStatus = FlagStatus.notDefined;
                UpdateFlagRenderer();
            }
        }

        protected virtual void UpdateFlagRenderer()
        {
            if (resultMeshRenderer)
            {
                if (flagStatus == FlagStatus.goodPosition)
                {
                    resultMeshRenderer.material = goodAnswerMaterial;
                }
                else if (flagStatus == FlagStatus.badPosition)
                {
                    resultMeshRenderer.material = badAnswerMaterial;
                }
                else
                {
                    resultMeshRenderer.material = notDefinedMaterial;
                }
            }
        }

        protected virtual void Start()
        {
            if (attachmentPoint)
            {
                attachmentPoint.onRegisterAttachment.AddListener(OnSnap);
                attachmentPoint.onUnregisterAttachment.AddListener(OnUnSnap);
            }
        }

        #region AttachmentPoint callbacks
        protected virtual void OnUnSnap()
        {
            if (flagStatus == FlagStatus.badPosition || flagStatus == FlagStatus.goodPosition)
            {
                flagStatus = FlagStatus.notDefined;
                PlayAudioFeedback(unsnapAudioType);
            }
        }

        protected virtual void OnSnap()
        {
            if (attachmentPoint.AttachedPoint != null)
            {
                var magnetNamingTag = attachmentPoint.AttachedPoint.GetComponent<NamingTag>();
                if (magnetNamingTag != null)
                {
                    if (magnetNamingTag.namingTag == namingTag)
                    {
                        flagStatus = FlagStatus.goodPosition;
                        PlayAudioFeedback(goodAnswerAudioType);
                    }
                    else
                    {
                        flagStatus = FlagStatus.badPosition;
                        PlayAudioFeedback(badAnswerAudioType);
                    }
                }
                else
                {
                    PlayAudioFeedback(defaultSnapAudioType);
                }
            }
        }
        #endregion

        protected virtual void PlayAudioFeedback(string audioType)
        {
            if (enableAudioFeedback && soundManager)
            {
                soundManager.PlayOneShot(audioType, transform.position);
            }
        }

        protected virtual void OnDestroy()
        {
            attachmentPoint?.onRegisterAttachment?.RemoveListener(OnSnap);
            attachmentPoint?.onUnregisterAttachment?.RemoveListener(OnUnSnap);
        }
    }
}
