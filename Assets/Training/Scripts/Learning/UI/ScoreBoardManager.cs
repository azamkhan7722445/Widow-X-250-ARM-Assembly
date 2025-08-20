using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// The ScoreBoardManager class is in charge to update the score board when it is required :
    ///     - a learner join or left the room,
    ///     - a learner's progress on an activity has changed,
    /// To do that, the ScoreBoardManager listen to the LearningManager events.
    /// Depending on events and modifications made, ScoreBoardManager will :
    ///     - add / delete lines on the score board,
    ///     - update the activity progress bars,
    /// </summary>
    public class ScoreBoardManager : MonoBehaviour
    {
        [SerializeField] LearningManager learningManager;
        [Tooltip("UI prefab containing the LearnerScoreUI for the score board")]
        [SerializeField] GameObject learnerScoreUIprefab;
        [Tooltip("Transform under which the instanciated learnerScoreUIprefab will be stored")]
        [SerializeField] RectTransform learnerScoreUIContainer;
        [Tooltip("UI prefab containing the ActivityScoreUI for an activity")]
        [SerializeField] GameObject activityScoreUIprefab;

        // Dictionnary to save the activities UI for each of each learner
        Dictionary<NetworkBehaviourId, Dictionary<int, ActivityScoreUI>> activityPanels = new Dictionary<NetworkBehaviourId, Dictionary<int, ActivityScoreUI>>();

        // Dictionnary to save the learner panel (learnerScoreUIprefab) of each learner
        Dictionary<NetworkBehaviourId, GameObject> learnerPanels = new Dictionary<NetworkBehaviourId, GameObject>();

        private void Start()
        {
            if (learningManager == null)
            {
                Debug.LogError("Learning Manager not set. finding the first one by default");
                learningManager = FindAnyObjectByType<LearningManager>();
            }

            if (learnerScoreUIprefab == null)
                Debug.LogError("playerScoreUIprefab is not defined");

            learningManager.onNewLearner.AddListener(OnNewLearner);
            learningManager.onDeletedLearner.AddListener(OnDeletedLearner);
            learningManager.onLearnerActivityTrackerUpdate.AddListener(UpdateUIForAnActivity);
        }

        void OnNewLearner(LearningManager learningManager, LearningParticipant learner)
        {
            CreateUILineForALearner(learner);
            UpdateUIForAllRegisteredActivities(learningManager);
        }


        private void CreateUILineForALearner(LearningParticipant learner)
        {
            Transform activitiesScoreUIContainer = default;

            // Create the UI line
            var learnerScoreUIObj = Instantiate(learnerScoreUIprefab, learnerScoreUIContainer.transform);
            learnerPanels[learner.Id] = learnerScoreUIObj;

            if (learnerScoreUIObj != null)
            {
                learnerScoreUIObj.transform.localScale = Vector3.one;
                learnerScoreUIObj.transform.localRotation = Quaternion.identity;

                activitiesScoreUIContainer = learnerScoreUIObj.GetComponentInChildren<LearnerScoreUI>().activitiesScoreContainer;
            }

            // Update the learner name
            LearnerScoreUI learnerScoreUI = learnerScoreUIObj.GetComponentInChildren<LearnerScoreUI>();
            if (learnerScoreUI != null)
            {
                learnerScoreUI.InitializeForLearner(learner);
            }

            // Create activities UI
            CreateAllActivitiesUIForANewLearner(learner, activitiesScoreUIContainer);

        }
        private void CreateAllActivitiesUIForANewLearner(LearningParticipant learner, Transform activitiesListContainer)
        {
            // Create the activities progress UI
            List<ActivityScoreUI> activityScoresUI = new List<ActivityScoreUI>();

            for (int i = 1; i <= learningManager.ActivitiesAvailability.Count; i++)
            {
                var activityScoreUIObj = Instantiate(activityScoreUIprefab, activitiesListContainer);
                if (activityScoreUIObj != null)
                {
                    activityScoreUIObj.transform.localScale = Vector3.one;
                    activityScoreUIObj.transform.localRotation = Quaternion.identity;
                }

                ActivityScoreUI activityScoreUI = activityScoreUIObj.GetComponentInChildren<ActivityScoreUI>();

                if (activityScoreUI)
                {
                    activityScoresUI.Add(activityScoreUI);
                }
            }

            // Update the activityPanels dictionnary with the new activityScoresUI created for this player
            activityPanels[learner.Id] = new Dictionary<int, ActivityScoreUI>();

            for (int activityId = 0; activityId < activityScoresUI.Count; activityId++)
            {
                var activityScoreUI = activityScoresUI[activityId];
                activityPanels[learner.Id][activityId] = activityScoreUI;
            }
        }

        private void CreateAnActivityUIForAllLearners(LearnerActivityTracker learnerActivityTracker)
        {
            foreach (var learnerBoardEntry in learnerPanels)
            {
                var learnerBehaviourId = learnerActivityTracker.LearnerId;
                Transform container = learnerBoardEntry.Value.GetComponent<LearnerScoreUI>().activitiesScoreContainer;

                var activityScoreUIObj = Instantiate(activityScoreUIprefab, container);
                if (activityScoreUIObj != null)
                {
                    activityScoreUIObj.transform.localScale = Vector3.one;
                    activityScoreUIObj.transform.localRotation = Quaternion.identity;
                }

                ActivityScoreUI activityScoreUI = activityScoreUIObj.GetComponentInChildren<ActivityScoreUI>();
                // Add an entry in the activityPanels dictionnary with the new activityScoresUI created for this player
                activityPanels[learnerBehaviourId][learnerActivityTracker.activityId] = activityScoreUI;
            }
        }


        void OnDeletedLearner(LearningManager learningManager, NetworkString<_64> learnerUserId, NetworkBehaviourId learnerBehaviourId, bool isActiveLearnerInSlotInfo)
        {
            // we don't care about isActiveLearnerInSlotInfo because we want to remove the line for the disconnected client, as panel are indexed by the NetworkBehaviorId

            activityPanels.Remove(learnerBehaviourId);
            if (learnerPanels.ContainsKey(learnerBehaviourId))
            {
                Destroy(learnerPanels[learnerBehaviourId]);
                learnerPanels.Remove(learnerBehaviourId);
            }
        }

        void UpdateUIForAllRegisteredActivities(LearningManager learningManager)
        {
            foreach (var learnerActivityTracker in learningManager.registeredLearnerActivities)
            {
                UpdateUIForAnActivity(learningManager, learnerActivityTracker);
            }
            UpdateLearnersOrder();
        }

        void UpdateUIForAnActivity(LearningManager learningManager, LearnerActivityTracker learnerActivityTracker)
        {
            var learnerBehaviourId = learnerActivityTracker.LearnerId;

            // check if this activity has already been added in the dictionnary
            if (activityPanels.ContainsKey(learnerBehaviourId) && activityPanels[learnerBehaviourId].ContainsKey(learnerActivityTracker.activityId) == false)
            {
                // This activity was not existing when the player registered, probably because this activity is not existing by default (in the scene) on the LearningManager ActivitiesAvailability
                CreateAnActivityUIForAllLearners(learnerActivityTracker);
            }

            // Here we have all the activity UI ready
            if (activityPanels.ContainsKey(learnerBehaviourId) && activityPanels[learnerBehaviourId].ContainsKey(learnerActivityTracker.activityId))
            {
                var panel = activityPanels[learnerBehaviourId][learnerActivityTracker.activityId];
                panel.UpdateUIForAnActivity(learningManager, learnerActivityTracker);
            }
        }

        void UpdateLearnersOrder()
        {
            foreach (var learnerBoardEntry in learnerPanels)
            {
                var learnerBehaviourId = learnerBoardEntry.Key;
                var learnerBoard = learnerBoardEntry.Value;

                var index = learningManager.LearnerIndex(learnerBehaviourId);
                if (index != -1)
                {
                    if (index >= learnerBoard.transform.parent.childCount)
                        index = learnerBoard.transform.parent.childCount - 1;

                    learnerBoard.transform.SetSiblingIndex(index);
                }
            }
        }

        private void OnDestroy()
        {
            learningManager?.onNewLearner?.RemoveListener(OnNewLearner);
            learningManager?.onDeletedLearner?.RemoveListener(OnDeletedLearner);
            learningManager?.onLearnerActivityTrackerUpdate?.RemoveListener(UpdateUIForAnActivity);
        }
    }
}
