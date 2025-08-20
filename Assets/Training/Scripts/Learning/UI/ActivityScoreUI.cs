using UnityEngine;
using UnityEngine.UI;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is used in the LearnerScoreUI prefab to provide access to the activity UI elements (name & progress bar)
    /// </summary>
    public class ActivityScoreUI : MonoBehaviour
    {
        public TMPro.TMP_Text activityName;
        public Slider activitySlider;
        protected LearnerActivityTracker learnerActivityTracker;

        public virtual void UpdateUIForAnActivity(LearningManager learningManager, LearnerActivityTracker tracker)
        {
            learnerActivityTracker = tracker;
            activityName.text = learnerActivityTracker.learningActivityDescription.activityName;
            activitySlider.value = learnerActivityTracker.Progress;
        }
    }

}
