using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.XR.Shared.Touch
{
    /***
     * 
     * TouchableButton can be used as a press, toggle or radio button.
     * It provides visual & audio feedback when the button is touched
     *  
     * The class also handles material changes for the button mesh renderer when the button is pressed or released. 
     *
     * If primaryIcon is set, it displays primary and secondary icons based on button state. 
     * It updates the icons based on the button type (press button, toggle button, or radio button) and the current button status. 
     *
     * 
     ***/
    public class TouchableButton : MonoBehaviour, ITouchable
    {
        [Header("Unity Event")]
        public UnityEvent onTouchStart;
        public UnityEvent onTouchEnd;

        [Header("Current state")]
        public bool isButtonPressed = false;
        protected MeshRenderer meshRenderer;

        public enum ButtonType
        {
            PressButton,
            RadioButton,
            ToggleButton
        }

        [Header("Button")]
        public ButtonType buttonType = ButtonType.PressButton;
        public bool toggleStatus = false;

        [SerializeField]
        List<TouchableButton> radioGroupButtons = new List<TouchableButton>();

        public bool isRadioGroupDefaultButton = false;

        [Header("Anti-bounce")]
        public float timeBetweenTouchTrigger = 0.3f;

        [Header("Feedback")]
        [SerializeField] string audioType;
        [SerializeField] protected Material touchMaterial;
        [SerializeField] private bool playSoundWhenTouched = true;
        [SerializeField] private bool playHapticFeedbackOnToucher = true;
        [SerializeField] float toucherHapticAmplitude = 0.2f;
        [SerializeField] float toucherHapticDuration = 0.05f;
        protected Material materialAtStart;
        [SerializeField] IFeedbackHandler feedback;

        [Header("Sibling button")]
        [SerializeField]
        bool doNotallowTouchIfSiblingTouched = true;
        [SerializeField]
        bool doNotallowTouchIfSiblingWasRecentlyTouched = true;
        [SerializeField]
        bool automaticallyDetectSiblings = true;
        [SerializeField]
        List<TouchableButton> siblingButtons = new List<TouchableButton>();

        [Header("Icons")]
        [SerializeField] GameObject primaryIcon;
        [SerializeField] GameObject secondaryIcon;


        float lastTouchEnd = -1;

        public bool WasRecentlyTouched => lastTouchEnd != -1 && (Time.time - lastTouchEnd) < timeBetweenTouchTrigger;
        public bool IsPressButton => buttonType == ButtonType.PressButton;
        public bool IsToggleButton => buttonType == ButtonType.ToggleButton;
        public bool IsRadioButton => buttonType == ButtonType.RadioButton;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer) materialAtStart = meshRenderer.material;

            if (feedback == null)
                feedback = GetComponentInParent<IFeedbackHandler>();
        }

        private void OnEnable()
        {
            // We need to clear if component was disabled 
            isButtonPressed = false;
            UpdateButton();
        }

        private void Start()
        {
            if (automaticallyDetectSiblings && transform.parent)
            {
                foreach (Transform child in transform.parent)
                {
                    if (child == transform) continue;
                    if (child.TryGetComponent<TouchableButton>(out var sibling))
                    {
                        siblingButtons.Add(sibling);
                    }
                }
            }


            if (IsRadioButton && radioGroupButtons.Count == 0)
            {
                foreach (var siblingButton in siblingButtons)
                {
                    if (siblingButton.IsRadioButton)
                    {
                        radioGroupButtons.Add(siblingButton);
                    }
                }
            }

            if (isRadioGroupDefaultButton)
            {
                ChangeRadioButtonsStatus();
            }
        }

        bool CheckIfTouchIsAllowed()
        {
            if (WasRecentlyTouched)
            {
                // Local anti-bounce 
                return false;
            }
            if (doNotallowTouchIfSiblingTouched)
            {
                foreach (var sibling in siblingButtons)
                {
                    if (sibling.isButtonPressed)
                    {
                        //  Debug.LogError("Preventing due to active " + sibling);
                        return false;
                    }
                    else if (doNotallowTouchIfSiblingWasRecentlyTouched && sibling.WasRecentlyTouched)
                    {
                        // Sibling anti-bounce 
                        //   Debug.LogError("Preventing due to recently active" + sibling);
                        return false;

                    }
                }
            }
            return true;
        }

        public void ChangeButtonStatus(bool status)
        {
            if (IsToggleButton || IsRadioButton)
            {
                toggleStatus = status;
                if (toggleStatus)
                {
                    TriggerOnTouchStartCallback();
                }
            }
            else
            {
                if (isButtonPressed)
                {
                    TriggerOnTouchStartCallback();
                }
            }
            UpdateButton();
        }

        void ChangeRadioButtonsStatus()
        {
            ChangeButtonStatus(true);
            foreach (var button in radioGroupButtons)
            {
                button.ChangeButtonStatus(false);
            }
        }

        private void TouchStartAnalysis(Toucher toucher)
        {
            if (CheckIfTouchIsAllowed() == false) return;

            isButtonPressed = true;

            if (IsToggleButton)
            {
                ChangeButtonStatus(!toggleStatus);
            }
            else if (IsRadioButton)
            {
                ChangeRadioButtonsStatus();
            }
            else if(IsPressButton)
            {
                ChangeButtonStatus(true);
            }

            TouchFeedback(toucher);
        }

        private void TouchEndAnalysis(Toucher toucher)
        {
            var buttonWasActive = isButtonPressed;
            isButtonPressed = false;

            if (buttonWasActive)
            {
                TriggerOnTouchEndCallback();
                lastTouchEnd = Time.time;
                if (IsPressButton)
                {
                    ChangeButtonStatus(false);
                }
            }

            UpdateButton();
        }

        #region Visual effect & feedback
        public virtual void UpdateButton()
        {
            UpdateMeshRenderer();
            UpdateIcons();
        }

        void UpdateMeshRenderer()
        {
            if (!meshRenderer) return;

            bool boutonActivated = isButtonPressed || toggleStatus;

            if (touchMaterial && boutonActivated)
            {
                meshRenderer.material = touchMaterial;
            }
            else if (materialAtStart && boutonActivated == false)
            {
                RestoreMaterial();
            }
        }

        void UpdateIcons()
        {
            if (IsPressButton)
            {
                if (primaryIcon != null)
                {
                    primaryIcon.SetActive(true);

                    if (secondaryIcon != null)
                    {
                        primaryIcon.SetActive(!isButtonPressed);
                        secondaryIcon.SetActive(isButtonPressed);
                    }
                }
            }

            if (IsToggleButton || IsRadioButton)
            {
                if (primaryIcon != null)
                {
                    primaryIcon.SetActive(toggleStatus);

                    if (secondaryIcon != null)
                        secondaryIcon.SetActive(!toggleStatus);
                }
            }
        }

        // Restore initial material
        protected async void RestoreMaterial()
        {
            await System.Threading.Tasks.Task.Delay(100);
            if (meshRenderer) meshRenderer.material = materialAtStart;
        }

        void TouchFeedback(Toucher toucher)
        {
            if (playSoundWhenTouched && feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
                feedback.PlayAudioFeeback(audioType);

            if (playHapticFeedbackOnToucher && toucher != null)
            {
                var feedbackHandler = toucher.gameObject.GetComponentInParent<IFeedbackHandler>();
                var hardwarehand = toucher.gameObject.GetComponentInParent<HardwareHand>();
                if (feedbackHandler != null && hardwarehand != null)
                {

                    feedbackHandler.PlayHapticFeedback(hapticAmplitude: toucherHapticAmplitude, hardwareHand: hardwarehand, hapticDuration: toucherHapticDuration);
                }
            }
        }
        #endregion 

        #region ITouchable
        public virtual void OnToucherContactStart(Toucher toucher)
        {
            TouchStartAnalysis(toucher);
        }

        public virtual void OnToucherStay(Toucher toucher) { }

        public virtual void OnToucherContactEnd(Toucher toucher)
        {
            TouchEndAnalysis(toucher);
        }
        #endregion

        #region Callbacks
        public virtual void TriggerOnTouchEndCallback()
        {
            if (onTouchEnd != null) onTouchEnd.Invoke();
        }

        public virtual void TriggerOnTouchStartCallback()
        {
            if (onTouchStart != null) onTouchStart.Invoke();
        }
        #endregion

        #region Inspector

        [ContextMenu("SimulateTouchStart")]
        public void SimulateTouchStart()
        {
            TouchStartAnalysis(null);
        }

        [ContextMenu("SimulateTouchEnd")]
        public void SimulateTouchEnd()
        {
            TouchEndAnalysis(null);
        }
        #endregion
    }
}
