using Fusion.Addons.Reconnection;
using Fusion.Addons.SubscriberRegistry;
using Fusion.XR.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.Learning
{
    /// <summary>
    ///  LearningManager is a central element in the operation of the sample.
    ///  It manages :
    ///     - learners registration,
    ///     - learners slot allocation,
    ///     - learning activities registration,
    ///     - learning activity global status,
    ///     - events required to update the scoring board and console,
    ///  It inherits from the Registry class (of SubscriberRegistry addon) to benefit from the registration mechanism.
    /// </summary>


    [System.Serializable]
    public struct LearnerSlotInfo : INetworkStruct
    {
        public NetworkBehaviourId learnerBehaviourId;
        public NetworkString<_64> userId;
    }

    public class LearningManager : DutyRegistry<LearnerComponent>, IStateAuthorityChanged
    {
        const int MaxLearner = 6;
        const int MaxActivities = 10;

        // Positions to spawn player's activity prefab
        public List<Transform> activityTransformPositionList = new List<Transform>();

        // List to save the slot occupation
        [Networked, OnChangedRender(nameof(OnLearnerSlotChange))]
        [Capacity(MaxLearner)]
        [UnitySerializeField]
        public NetworkArray<LearnerSlotInfo> LearnerSlotInfos { get; }

        // Collection of Activities Status
        [Networked, OnChangedRender(nameof(OnActivitiesAvailability))]
        [Capacity(MaxActivities)]
        [UnitySerializeField]
        public NetworkDictionary<int, ActivityStatus> ActivitiesAvailability { get; }

        public enum ActivityStatus
        {
            Open,
            Closed
        }

        // List to save the learners
        List<LearningParticipant> registeredLearners = new List<LearningParticipant>();

        // List to save the activities
        public List<LearnerActivityTracker> registeredLearnerActivities = new List<LearnerActivityTracker>();

        // Player's prefab that contains the activities
        public GameObject learnerActivitiesPrefab = null;

        [SerializeField] bool debug = false;

        // Event
        public UnityEvent<LearningManager, LearningParticipant> onNewLearner;
        // the 4th parameter isActiveLearnerInSlotInfo is used to determine if the deleted learner has been replaced by a new leaner in the slot info (in case the previous avatar is not yet remove before a reconnection)
        public UnityEvent<LearningManager, NetworkString<_64>, NetworkBehaviourId, bool> onDeletedLearner;


        // This event is called when a LearnerActivityTracker registers or updates its data
        public UnityEvent<LearningManager, LearnerActivityTracker> onLearnerActivityTrackerUpdate;

        // This event is called when ActivitiesAvailability dictionary has been updated
        public UnityEvent<LearningManager> onActivityListUpdate;

        #region Registry<LearnerComponent> overrides
        protected override void OnSubscriberRegistration(LearnerComponent subscriber)
        {
            base.OnSubscriberRegistration(subscriber);
            if (subscriber is LearningParticipant learner)
            {
                OnLearnerRegistration(learner);
            }
            if (subscriber is LearnerActivityTracker learnerActivityTracker)
            {
                OnLearnerActivityTrackerRegistration(learnerActivityTracker);
            }
        }

        protected override void OnSubscriberUnregistration(LearnerComponent subscriber)
        {
            base.OnSubscriberUnregistration(subscriber);
            if (subscriber is LearningParticipant learner)
            {
                OnLearnerUnregistration(learner);
            }
            if (subscriber is LearnerActivityTracker activityInfo)
            {
                OnLearnerActivityTrackerUnregistration(activityInfo);
            }
        }
        #endregion

        #region Learner components subtypes registration
        void OnLearnerRegistration(LearningParticipant learner)
        {
            StoreLearner(learner);
            if (onNewLearner != null)
                onNewLearner.Invoke(this, learner);
        }

        void OnLearnerActivityTrackerRegistration(LearnerActivityTracker learnerActivityTracker)
        {
            registeredLearnerActivities.Add(learnerActivityTracker);
            OnLearnerActivityTrackerChange(learnerActivityTracker);

            if (Object.HasStateAuthority)
            {
                // If this activity is not known, we want sure it is stored in the ActivitiesAvailability (can be set in the scene directly)
                if (ActivitiesAvailability.ContainsKey(learnerActivityTracker.activityId) == false)
                {
                    ActivitiesAvailability.Add(learnerActivityTracker.activityId, ActivityStatus.Open);
                    OnLearnerActivityTrackerChange(learnerActivityTracker);
                }
            }
        }

        bool IsActiveLearnerInSlotInfo(NetworkString<_64> userId, NetworkBehaviourId learnerBehaviourId)
        {
            bool isActiveLearnerInSlotInfo = true;
            for (int i = 0; i < MaxLearner; i++)
            {
                if (LearnerSlotInfos[i].userId == userId)
                {
                    if (LearnerSlotInfos[i].learnerBehaviourId != learnerBehaviourId)
                    {
                        Debug.Log("Another Learner is using this userId: cleanups not required");
                        isActiveLearnerInSlotInfo = false;
                    }
                }
            }
            return isActiveLearnerInSlotInfo;
        }

        void OnLearnerUnregistration(LearningParticipant learner)
        {

            bool preserverUserSlot = learner.IsLearner;
            bool isActiveLearnerInSlotInfo = IsActiveLearnerInSlotInfo(learner.UserId, learner.Id);

            if (Object && Object.HasStateAuthority)
            {
                // Check that this learner is the one currently registered for its userId
                for (int i = 0; i < MaxLearner; i++)
                {
                    if (LearnerSlotInfos[i].userId == learner.UserId)
                    {
                        if (LearnerSlotInfos[i].learnerBehaviourId == learner.Id)
                        {
                            CleanLearnerSlotPosition(learner.UserId, preserveUserId: preserverUserSlot);
                        }
                    }
                }
            }

            if (onDeletedLearner != null)
                onDeletedLearner.Invoke(this, learner.UserId, learner.Id, isActiveLearnerInSlotInfo);
        }

        void OnLearnerActivityTrackerUnregistration(LearnerActivityTracker activityInfo)
        {
            registeredLearnerActivities.Remove(activityInfo);
        }
        #endregion



        public override void Spawned()
        {
            base.Spawned();

            InitializeSlots();

            if (ActivitiesAvailability.Count > 0)
                ActivityListUpdateEvent();

            foreach (var activityEntry in ActivitiesAvailability)
            {
                OnLearningManagerActivityUpdate(activityEntry.Key);
            }
        }

        #region SlotManagment
        void InitializeSlots()
        {
            if (Object.HasStateAuthority)
            {
                // Init the LearnerSlotInfos array with the max number of learner
                for (int i = 0; i < MaxLearner; i++)
                {
                    LearnerSlotInfo defaultLearnerSlotInfo = new LearnerSlotInfo();
                    LearnerSlotInfos.Set(i, defaultLearnerSlotInfo);
                }
                if (debug) DebugLearnerSlotInfo();
            }
        }

        void StoreLearnerSlot(NetworkString<_64> userId, NetworkBehaviourId learnerBehaviourId)
        {
            int availableSlotIndex = GetAvailablePosition(userId);
            if (debug) Debug.LogError($"LearningManager : availableSlotIndex={availableSlotIndex}");

            if (availableSlotIndex != -1)
            {
                // Update the networked Array
                LearnerSlotInfo newLearnerSlotInfo = new LearnerSlotInfo();
                newLearnerSlotInfo.learnerBehaviourId = learnerBehaviourId;
                newLearnerSlotInfo.userId = userId;
                LearnerSlotInfos.Set(availableSlotIndex, newLearnerSlotInfo);
                if (debug) DebugLearnerSlotInfo();
            }
        }

        void StoreLearner(LearningParticipant learner)
        {
            // Only the State Authority of the LearningManager manages the SlotInfo networked array
            if (Object && Object.HasStateAuthority)
            {
                if (debug) Debug.LogError($"LearningManager : Store {learner} in StoreLearner");
                StoreLearnerSlot(learner.UserId, learner.Id);
            }
        }

        private void CleanLearnerSlotPosition(NetworkString<_64> userID, bool preserveUserId)
        {
            bool found = false;
            for (int i = 0; i < MaxLearner; i++)
            {
                if (LearnerSlotInfos[i].userId == userID)
                {
                    LearnerSlotInfo newLearnerSlotInfo = new LearnerSlotInfo();
                    if (preserveUserId)
                    {
                        newLearnerSlotInfo.userId = LearnerSlotInfos[i].userId;
                    }
                    LearnerSlotInfos.Set(i, newLearnerSlotInfo);
                    if (debug) Debug.LogError($"LearningManager => CleanLearnerSlotPosition : the player {userID} is removed from the LearnerSlotInfos array");
                    found = true;
                    break;
                }
            }
            if (found == false)
            {
                Debug.LogError($"LearningManager => CleanLearnerSlotPosition : the player {userID} was not found in the learner slot list");
            }
        }

        private void OnLearnerSlotChange(NetworkBehaviourBuffer previousBuffer)
        {
            var reader = GetArrayReader<LearnerSlotInfo>(nameof(LearnerSlotInfos));
            NetworkArrayReadOnly<LearnerSlotInfo> previous = reader.Read(previousBuffer);

            // Inform new learners that a spot has been booked, so they can spawn their prefabs
            for (int i = 0; i < MaxLearner; i++)
            {
                bool newLearner = previous[i].learnerBehaviourId == NetworkBehaviourId.None && LearnerSlotInfos[i].learnerBehaviourId != NetworkBehaviourId.None;
                if (newLearner && Runner.TryFindBehaviour<LearningParticipant>(LearnerSlotInfos[i].learnerBehaviourId, out var learner))
                {
                    bool alreadyUsedSlot = previous[i].userId != "";
                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity; 
                    if (activityTransformPositionList.Count <= i)
                    {
                        position = Vector3.forward * i;
                        Debug.LogError($"[Error] Not enough activityTransformPositionList. {activityTransformPositionList.Count} positions, and slot n°{i} added. Using {position} as fallback");
                    } 
                    else
                    {
                        if (debug) Debug.LogError($"LearningManager => OnLearnerSlotChange : New learner detected for index:{i} Id:{LearnerSlotInfos[i].learnerBehaviourId}. Inform it that registration is completed");
                        position = activityTransformPositionList[i].position;
                        rotation = activityTransformPositionList[i].rotation;
                    }
                    learner.LearnerSlotBookedOnLearningManager(this, learnerActivitiesPrefab, position, rotation, alreadyUsedSlot);
                }
            }

            // Inform leaving learners that their spot has been released, so they can despawn their prefabs
            for (int i = 0; i < MaxLearner; i++)
            {
                bool oldLearner = previous[i].learnerBehaviourId != NetworkBehaviourId.None && LearnerSlotInfos[i].learnerBehaviourId == NetworkBehaviourId.None;
                if (oldLearner && Runner.TryFindBehaviour<LearningParticipant>(previous[i].learnerBehaviourId, out var learner))
                {
                    learner.LearnerSlotReleasedOnLearningManager(this);
                }
            }
        }

        public int GetAvailablePosition(NetworkString<_64> preExistingUserId)
        {
            int availableSlot = -1;
            for (int i = 0; i < MaxLearner; i++)
            {
                // Check if it matches requested userId
                if (LearnerSlotInfos[i].userId == preExistingUserId)
                {
                    availableSlot = i;
                    break;
                }
            }

            if (availableSlot == -1)
            {
                for (int i = 0; i < MaxLearner; i++)
                {
                    if (LearnerSlotInfos[i].learnerBehaviourId == default && LearnerSlotInfos[i].userId == "")
                    {
                        // Default slot, if the userId is not found
                        availableSlot = i;
                        break;
                    }
                }
            }

            if (debug) Debug.LogError($"[Debug] Available slot {availableSlot} (isPreExistingUserId: {LearnerSlotInfos[availableSlot].userId == preExistingUserId}, {LearnerSlotInfos[availableSlot].userId} => {preExistingUserId})");
            return availableSlot;
        }

        public int LearnerIndex(NetworkBehaviourId learnerBehaviourId)
        {
            int index = -1;

            for (int i = 0; i < MaxLearner; i++)
            {
                if (LearnerSlotInfos[i].learnerBehaviourId == learnerBehaviourId)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        void DebugLearnerSlotInfo()
        {
            String debugInfo = "";
            for (int i = 0; i < MaxLearner; i++)
            {
                debugInfo += i + ":" + LearnerSlotInfos[i].learnerBehaviourId + "     ";
            }
        }
        #endregion

        #region Recoverables cleanup
        public async void FreeSlotAndDestroyRecoverablesWithUserId(string userId)
        {
            foreach (var slot in LearnerSlotInfos)
            {
                if (slot.userId == userId)
                {
                    // check if player is disconnected 
                    if (slot.learnerBehaviourId.IsValid)
                    {
                        if (debug) Debug.LogError($"Can not destroy object because player is connected {slot.learnerBehaviourId.IsValid} is still connected");
                    }
                    else
                    {
                        if (debug) Debug.LogError($"Slot {slot.learnerBehaviourId}/{slot.userId} can be destroyed (player is not connected naymore)");

                        foreach (var recoverable in FindObjectsByType<Recoverable>(FindObjectsSortMode.None))
                        {
                            recoverable.DespawnForUserId(userId);
                        }

                        // Get State Auth to clean the Slot Position
                        if (Object.StateAuthority != Runner.LocalPlayer)
                        {
                            if (!await RequestAuthority())
                            {
                                Debug.LogError($"Fail to get State Auth");
                                return;
                            }
                        }
                        CleanLearnerSlotPosition(slot.userId, preserveUserId: false);
                    }
                }
            }
        }
        #endregion

        public LearnerActivityTracker GetLearnerActivityById(int activityId)
        {
            return registeredLearnerActivities.Find(activity => activity.activityId == activityId);
        }

        public async void ToggleActivityStatus(int index)
        {
            if (Object.StateAuthority != Runner.LocalPlayer)
            {
                // Get State Auth
                if (!await RequestAuthority())
                {
                    Debug.LogError($"Fail to get State Auth");
                    return;
                }
            }

            ActivitiesAvailability.TryGet(index, out ActivityStatus activityStatus);
            if (activityStatus == ActivityStatus.Open)
            {
                ActivitiesAvailability.Set(index, ActivityStatus.Closed);
            }
            else
            {
                ActivitiesAvailability.Set(index, ActivityStatus.Open);
            }
        }


        private async Task<bool> RequestAuthority()
        {
            if (!await Object.WaitForStateAuthority()) return false;
            return true;
        }

        [EditorButton("OpenAllActivities")]
        public void OpenAllActivities()
        {
            if (Object.HasStateAuthority)
            {
                foreach (var activityStatus in ActivitiesAvailability)
                {
                    var index = activityStatus.Key;
                    var status = activityStatus.Value;
                    if (status == ActivityStatus.Closed)
                    {
                        ActivitiesAvailability.Set(index, ActivityStatus.Open);
                    }
                }
            }
        }

        [EditorButton("CloseAllActivities")]
        public void CloseAllActivities()
        {
            if (Object.HasStateAuthority)
            {
                foreach (var activityStatus in ActivitiesAvailability)
                {
                    var index = activityStatus.Key;
                    var status = activityStatus.Value;
                    if (status == ActivityStatus.Open)
                    {
                        ActivitiesAvailability.Set(index, ActivityStatus.Closed);
                    }
                }
            }
        }

        public void OnActivitiesAvailability(NetworkBehaviourBuffer previousBuffer)
        {
            ActivityListUpdateEvent();
            InformLearnerTrackerInfo(previousBuffer);
        }

        private void ActivityListUpdateEvent()
        {
            if (onActivityListUpdate != null)
            {
                onActivityListUpdate.Invoke(this);
            }
        }

        private void InformLearnerTrackerInfo(NetworkBehaviourBuffer previousBuffer)
        {
            // Compare with previous state
            var reader = GetDictionaryReader<int, ActivityStatus>(nameof(ActivitiesAvailability));
            NetworkDictionaryReadOnly<int, ActivityStatus> previous = reader.Read(previousBuffer);

            foreach (var entry in ActivitiesAvailability)
            {
                int activityId = entry.Key;
                ActivityStatus status = entry.Value;

                // Detect if a modification occured
                if (previous.TryGet(activityId, out ActivityStatus previousStatus))
                {
                    if (previousStatus != status)
                    {
                        // ChangeDetected for this activityId
                        OnLearningManagerActivityUpdate(activityId);
                    }
                } 
                else
                {
                    // The key was not yet present before
                    OnLearningManagerActivityUpdate(activityId);
                }
            }
        }

        // Global status of the activity (opened/closed)
        private void OnLearningManagerActivityUpdate(int activityId)
        {
            // inform the learnerActivityTracker
            foreach (var learnerActivity in registeredLearnerActivities)
            {
                // find all LearnerActivities with the activityId
                if (learnerActivity.activityId == activityId)
                {
                    // We want to play a sound (if any) anouncing the opening/closing of the activity
                    learnerActivity.OnLearningManagerActivityUpdate(ActivitiesAvailability[activityId], provideAudioFeedback: true);
                    OnLearnerActivityTrackerChange(learnerActivity);
                }
            }
        }

        // Called on learnerActivityTracker change 
        public void OnLearnerActivityTrackerChange(LearnerActivityTracker learnerActivityTracker)
        {
            if (onLearnerActivityTrackerUpdate != null)
            {
                onLearnerActivityTrackerUpdate.Invoke(this, learnerActivityTracker);
            }
        }

        #region State authority change handling
        public void StateAuthorityChanged()
        {

            // need to check and clean the slotinfos when getting the State Authority on it in case the State Auth was disconnected
            if (Object.HasStateAuthority)
            {
                for (int i = 0; i < LearnerSlotInfos.Length; i++)
                {
                    var slotInfo = LearnerSlotInfos[i];

                    var learnerFound = Runner.TryFindBehaviour<LearningParticipant>(slotInfo.learnerBehaviourId, out var learner);

                    if (learnerFound == false)
                    {

                        slotInfo.learnerBehaviourId = NetworkBehaviourId.None;
                        LearnerSlotInfos.Set(i, slotInfo);
                    }
                }
            }
        }
        #endregion
    }

}
