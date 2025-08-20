using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is in charge of displaying the list of activity available in the room and their current global status (open/close).
    /// To do so, it listens to the LearningManager event to get notified when a new activity is registered/unregistered and when the status is updated.
    /// </summary>
    public class ControlStandActivityListDisplay : MonoBehaviour
    {
        [SerializeField]
        LearningManager learningManager;

        [SerializeField]
        GameObject ControlStandActivityStatusUIPrefab;

        [SerializeField]
        RectTransform ControlStandActivityStatusUIContainer;

        List<GameObject> ActivityStatusPanels = new List<GameObject>();

        [SerializeField] Color openColor = Color.green;
        [SerializeField] Color closeColor = Color.red;

        private void Start()
        {
            if (learningManager == null)
                Debug.LogError("Learning Manager not set");

            if (ControlStandActivityStatusUIPrefab == null)
                Debug.LogError("ControlStandActivityStatusUIPrefab is not defined");

            learningManager.onActivityListUpdate.AddListener(UpdateUIForActivities);
            learningManager.onLearnerActivityTrackerUpdate.AddListener(UpdateUIForActivityUpdate);
        }

        private void UpdateUIForActivityUpdate(LearningManager learningManager, LearnerActivityTracker learnerActivityTracker)
        {

            UpdateUIForActivities(learningManager);
        }

        private void UpdateUIForActivities(LearningManager learningManager)
        {

            int numberOfActivities = 0;
            numberOfActivities = learningManager.ActivitiesAvailability.Count;

            if (ActivityStatusPanels.Count != numberOfActivities)
            {
                var difference = numberOfActivities - ActivityStatusPanels.Count;
                if (difference > 0)
                {
                    for (int i = 0; i < difference; i++)
                    {
                        var activityStatusUIObj = Instantiate(ControlStandActivityStatusUIPrefab, ControlStandActivityStatusUIContainer.transform.position, Quaternion.identity);

                        if (activityStatusUIObj != null)
                        {
                            activityStatusUIObj.transform.SetParent(ControlStandActivityStatusUIContainer.transform);
                            activityStatusUIObj.transform.localScale = Vector3.one;
                            activityStatusUIObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                        }
                        ActivityStatusPanels.Add(activityStatusUIObj);
                    }
                }

                if (difference < 0)
                {
                    for (int i = 0; i < difference; i++)
                    {
                        int lastIndex = ActivityStatusPanels.Count - 1;
                        Destroy(ActivityStatusPanels[lastIndex]);
                        ActivityStatusPanels.RemoveAt(lastIndex);
                    }
                }
            }


            // Update the content with Activities details (name, status, etc...)
            for (int i = 0; i < ActivityStatusPanels.Count; i++)
            {
                ControlStandActivityStatusUI activityStatusUI = ActivityStatusPanels[i].GetComponentInChildren<ControlStandActivityStatusUI>();
                if (activityStatusUI != null)
                {
                    LearnerActivityTracker activity = learningManager.GetLearnerActivityById(i);
                    if (activity != null)
                    {
                        activityStatusUI.activityName.text = activity.learningActivityDescription.activityName + " is";
                        if (learningManager.ActivitiesAvailability.TryGet(i, out LearningManager.ActivityStatus activityStatus))
                        {
                            activityStatusUI.activityStatus.text = activityStatus.ToString();
                            if (activityStatusUI.activityStatus.text == LearningManager.ActivityStatus.Open.ToString())
                            {
                                activityStatusUI.buttonText.text = "Close it";
                                activityStatusUI.activityStatus.color = openColor;
                            }
                            else
                            {
                                activityStatusUI.buttonText.text = "Open it";
                                activityStatusUI.activityStatus.color = closeColor;
                            }

                            int currentIndex = i;
                            // Set the button action 
                            activityStatusUI.button.onClick.RemoveAllListeners();
                            activityStatusUI.button.onClick.AddListener(() => learningManager.ToggleActivityStatus(currentIndex));
                        }
                    }
                }
            }
        }
    }
}
