using Fusion;
using Fusion.Addons.Learning;
using Fusion.Addons.Touch;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is used to release a slot when a user is disconnected.
    /// The button to release a slot is displayed only when the associated user is disconnected.
    /// To do so, SlotRelease listens to the LearningManager events.
    /// </summary>
    public class SlotRelease : NetworkBehaviour, LearningParticipant.ILearnerAssociatedComponent
    {
        [Networked]
        public NetworkString<_64> UserId { get; set; }

        [Networked, OnChangedRender(nameof(OnLearningManagerChange))]
        public LearningManager LearningManager { get; set; }

        [SerializeField]
        GameObject slotReleaseButtonVisual;

        Touchable touchable;

        void Awake()
        {
            if (touchable == null)
                touchable = GetComponentInChildren<Touchable>();
            else
                Debug.LogError("Touchable not found");

            if (touchable)
            {
                touchable.onTouch.AddListener(OnSlotRelease);
            }

            if (slotReleaseButtonVisual == null)
                Debug.LogError("slotReleaseButton not defined");

        }

        public override void Spawned()
        {
            base.Spawned();
            OnLearningManagerChange();
        }

        private void OnLearningManagerChange()
        {
            if (LearningManager)
            {
                LearningManager.onNewLearner.AddListener(OnNewLearner);
                LearningManager.onDeletedLearner.AddListener(OnDeletedLearner);
                UpdateSlotReleaseVisualDisplay();
            }
        }

        #region LearningManager event
        private void OnNewLearner(LearningManager learningManager, LearningParticipant learner)
        {

            if (UserId == learner.UserId)
            {
                ToogleButtonDisplay(false);
            }
        }

        private void OnDeletedLearner(LearningManager learningManager, NetworkString<_64> learnerUserId, NetworkBehaviourId learnerBehaviourId, bool isActiveLearnerInSlotInfo)
        {
            // we check if the disconnected learner is the active learner (in case of fast reconnection)
            if (UserId == learnerUserId && isActiveLearnerInSlotInfo)
            {
                // it is not a reconnection so we can display the button
                ToogleButtonDisplay(true);
            }
        }
        #endregion


        private void UpdateSlotReleaseVisualDisplay()
        {
            if (LearningManager && LearningManager.Object.IsValid)
            {
                // Find the slot info in the Learning Manager table
                foreach (LearnerSlotInfo slot in LearningManager.LearnerSlotInfos)
                {
                    if (slot.userId == UserId)
                    {
                        bool userDisconnected = slot.learnerBehaviourId == NetworkBehaviourId.None;
                        ToogleButtonDisplay(userDisconnected);
                    }
                }
            }
        }

        void ToogleButtonDisplay(bool shooldBeDisplayed)
        {
            slotReleaseButtonVisual.SetActive(shooldBeDisplayed);
        }

        private void OnSlotRelease()
        {
            LearningManager.FreeSlotAndDestroyRecoverablesWithUserId(UserId.ToString());
        }

        private void OnDestroy()
        {
            if (Object && LearningManager)
            {
                LearningManager.onNewLearner?.RemoveListener(OnNewLearner);
                LearningManager.onDeletedLearner?.RemoveListener(OnDeletedLearner);
            }
        }
    }

}
