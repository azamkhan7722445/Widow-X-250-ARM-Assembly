using UnityEngine;
using System.Collections.Generic;
using Fusion.Addons.Reconnection;
using Fusion.Addons.SubscriberRegistry;
using Fusion.XR.Shared;
using Fusion.Addons.HapticAndAudioFeedback;


namespace Fusion.Addons.Learning
{
    /// <summary>
    /// This class manages the activities.
    /// It handles the registrations process with the LearningManagers thanks to the inherited classes LearnerComponent/Subscriber.
    /// It defines the common parameters such as :
    ///     - the user associated to the activity,
    ///     - the activity status (Disabled, ReadyToStart, Started, Succeded, etc.),
    ///     - the activity progress,
    /// Both ActivityStatusOfLearner and Progress are networked so all clients are notified when an other participant makes progress in the learning path.
    /// The ActivityDescription allows to defines which object should enabled/disabled for each status or when the activty changed (transition).
    /// 
    /// </summary>
    [RequireComponent(typeof(Recoverable))]
    public class LearnerActivityTracker : LearnerComponent
    {
        [Networked]
        public LearningManager LearningManager { get; set; }

        [Networked, OnChangedRender(nameof(OnActivityChange))]
        public NetworkBehaviourId LearnerId { get; set; }

        [Networked, OnChangedRender(nameof(OnActivityStatusOfLearnerChange))]
        public Status ActivityStatusOfLearner { get; set; }

        [Networked, OnChangedRender(nameof(OnActivityChange))]
        public float Progress { get; set; }

        public int activityId;

        public LearningActivityDescription learningActivityDescription;

        [System.Serializable]
        public struct LearningActivityDescription
        {
            public string activityName;
            public List<StatusDescription> statusDescriptions;
            [Tooltip("Describe object to activate on transition. Useful for effect to enable only when present when entering a state, not triggered for late joiners when the state is active")]
            public List<StateTransition> transitions;
        }

        [System.Serializable]
        public struct StatusDescription
        {
            public Status status;
            [Header("In status changes")]
            public List<GameObject> gameObjectsToActivateInStatus;
            public List<GameObject> gameObjectsToDesactivateInStatus;
            [Header("Out of status changes")]
            public List<GameObject> gameObjectsToActivateOutOfStatus;
            public List<GameObject> gameObjectsToDesactivateOutOfStatus;
        }

        [System.Serializable]
        public struct ObjectsStatusModification
        {
            public StatusObjectCriteria criteria;
            public List<GameObject> gameObjects;
        }

        public enum StatusObjectCriteria
        {
            ActivateInStatus,
            DesactivateInStatus,
            ActivateOutOfStatus,
            DesactivateOutOfStatus,
        }

        Recoverable recoverable;

        [System.Serializable]
        public struct StateTransition
        {
            public bool fromAnyStatus;
            [DrawIf(nameof(fromAnyStatus), false, Hide = true)]
            public Status from;
            public bool toAnyStatus;
            [DrawIf(nameof(toAnyStatus), false, Hide = true)]
            public Status to;
            public List<GameObject> objectToActiveOnTransition;
            public List<GameObject> objectToDesactiveOnTransition;
        }

        public enum Status
        {
            Disabled,
            ReadyToStart,
            Started,
            Paused,
            Stopped,
            Failed,
            Succeeded,
            Finished,
            PendingSuccessValidation,
            CustomStatus1,
            CustomStatus2,
            CustomStatus3,
            CustomStatus4,
            CustomStatus5,
        }

        [SerializeField] string activityOpenSound;
        [SerializeField] string activityClosedSound;
        [SerializeField] private SoundManager soundManager;

        bool registrationDone = false;

        #region LearnerComponent
        public override NetworkString<_64> UserId => (Object != null && recoverable) ? recoverable.UserId : "";


        protected override void OnRegisterOnRegistry(Registry<LearnerComponent> registry)
        {
            base.OnRegisterOnRegistry(registry);
            if (registry is LearningManager learningManager)
            {
                OnActivityChange();
                UpdateLocalActivityWithLearningManagerGlobalActivityStatus(learningManager);
                registrationDone = true;

            }
        }

        protected override void OnAvailableRegistryFound(Registry<LearnerComponent> registry)
        {
            base.OnAvailableRegistryFound(registry);
            if (registry is LearningManager learningManager)
            {
                if (learningManager.ActivitiesAvailability.ContainsKey(activityId))
                {
                    OnLearningManagerActivityUpdate(learningManager.ActivitiesAvailability[activityId]);
                }
            }
        }
        #endregion

        protected virtual void Awake()
        {
            recoverable = GetComponent<Recoverable>();
        }


        public override void Spawned()
        {
            base.Spawned();
            if (soundManager == null) soundManager = SoundManager.FindInstance();

            OnActivityChange();
            UpdateActivatedObjectsBasedOnStatus();
        }

        public override void Render()
        {
            base.Render();
            Object.AffectStateAuthorityIfNone();
        }

        public void OnLearningManagerActivityUpdate(LearningManager.ActivityStatus globalActivityStatus, bool provideAudioFeedback = false)
        {
            if (Object.HasStateAuthority)
            {
                if (globalActivityStatus == LearningManager.ActivityStatus.Open)
                {
                    if (ActivityStatusOfLearner == Status.Disabled)
                    {
                        ActivityStatusOfLearner = Status.ReadyToStart;
                    }
                    if (registrationDone && provideAudioFeedback)
                    {
                        PlayActivityStatusVoice(true);
                    }
                }
                else
                {
                    ActivityStatusOfLearner = Status.Disabled;
                    if (registrationDone && provideAudioFeedback)
                    {
                        PlayActivityStatusVoice(false);
                    }
                }
            }
        }

        private void PlayActivityStatusVoice(bool isOpen)
        {
            string activityVoice = default;

            if (isOpen)
                activityVoice = activityOpenSound;
            else
                activityVoice = activityClosedSound;


            if (soundManager)
            {
                soundManager.PlayOneShot(activityVoice);
            }

        }

        void UpdateLocalActivityWithLearningManagerGlobalActivityStatus(LearningManager learningManager)
        {
            // Find the global status for this activity
            foreach (var entry in learningManager.ActivitiesAvailability)
            {
                int globalActivityId = entry.Key;
                LearningManager.ActivityStatus status = entry.Value;

                // update the learnerActivityTracker with the global status (open/closed)
                if (globalActivityId == activityId)
                {
                    OnLearningManagerActivityUpdate(status);
                    break;
                }
            }
        }

        [EditorButton("OnActivityChange")]
        public virtual void OnActivityChange()
        {
            if (Object.HasStateAuthority)
            {
                CheckActivityProgress();
            }
            LearningManager?.OnLearnerActivityTrackerChange(this);
        }

        // Updates status based on progress. Only called on the state auth. 
        public virtual void CheckActivityProgress()
        {
            //Check if we ActivityStatusOfLearner should be updated

            if (Progress > 0 && ActivityStatusOfLearner == Status.ReadyToStart)
            {
                ActivityStatusOfLearner = Status.Started;
            }

            if (Progress == 1f && ActivityStatusOfLearner != Status.Succeeded)
            {
                ActivityStatusOfLearner = Status.Succeeded;
            }
            if (Progress != 1f && ActivityStatusOfLearner == Status.Succeeded)
            {
                ActivityStatusOfLearner = Status.Started;
            }
        }

        #region Handle objects display
        private void ActivateObjects(List<GameObject> gameObjects)
        {
            if (gameObjects == null) return;

            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(true);
                }
            }
        }
        private void DeactivateObjects(List<GameObject> gameObjects)
        {
            if (gameObjects == null) return;

            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public virtual void OnActivityStatusOfLearnerChange(NetworkBehaviourBuffer previousBuffer)
        {
            if (learningActivityDescription.transitions != null && learningActivityDescription.transitions.Count != 0)
            {
                var reader = GetPropertyReader<Status>(nameof(ActivityStatusOfLearner));
                Status previousStatus = reader.Read(previousBuffer);
                Debug.Log($"[{name}] Transition from {previousStatus} to {ActivityStatusOfLearner}");

                foreach (var transition in learningActivityDescription.transitions)
                {
                    bool compatibleFrom = transition.from == previousStatus || transition.fromAnyStatus == true;
                    bool compatibleTo = transition.to == ActivityStatusOfLearner || transition.toAnyStatus == true;

                    if (compatibleFrom && compatibleTo)
                    {
                        ActivateObjects(transition.objectToActiveOnTransition);
                        DeactivateObjects(transition.objectToDesactiveOnTransition);
                    }
                }
            }
            UpdateActivatedObjectsBasedOnStatus();
        }

        public void UpdateActivatedObjectsBasedOnStatus()
        {
            AdaptDisplayedObjects(ActivityStatusOfLearner);
        }

        public void AdaptDisplayedObjects(Status status)
        {
            StatusDescription? potentiallySelectedStatusDescription = null;
            List<GameObject> gameObjectsToDisable = new List<GameObject>();
            List<GameObject> gameObjectsToEnable = new List<GameObject>();
            foreach (var description in learningActivityDescription.statusDescriptions)
            {
                if (status == description.status)
                {
                    potentiallySelectedStatusDescription = description;
                }
                else
                {
                    if (description.gameObjectsToDesactivateOutOfStatus != null) gameObjectsToDisable.AddRange(description.gameObjectsToDesactivateOutOfStatus);
                    if (description.gameObjectsToActivateOutOfStatus != null) gameObjectsToEnable.AddRange(description.gameObjectsToActivateOutOfStatus);
                }
            }
            if (potentiallySelectedStatusDescription is StatusDescription selectedStatusDescription)
            {
                gameObjectsToDisable.RemoveAll((o) => selectedStatusDescription.gameObjectsToActivateInStatus?.Contains(o) ?? false);
                gameObjectsToEnable.RemoveAll((o) => selectedStatusDescription.gameObjectsToDesactivateInStatus?.Contains(o) ?? false);
                if (selectedStatusDescription.gameObjectsToDesactivateOutOfStatus != null) gameObjectsToDisable.AddRange(selectedStatusDescription.gameObjectsToDesactivateInStatus);
                if (selectedStatusDescription.gameObjectsToActivateOutOfStatus != null) gameObjectsToEnable.AddRange(selectedStatusDescription.gameObjectsToActivateInStatus);
            }
            ActivateObjects(gameObjectsToEnable);
            DeactivateObjects(gameObjectsToDisable);
        }

        #endregion
    }
}
