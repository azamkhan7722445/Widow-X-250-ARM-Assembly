using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is used in the LearnerScoreUI prefab to provide access to the player's name UI element
    /// </summary>
    public class LearnerScoreUI : MonoBehaviour
    {
        public TMPro.TMP_Text learnerName;
        public Transform activitiesScoreContainer;
        protected LearningParticipant learner;

        public virtual void InitializeForLearner(LearningParticipant learner)
        {
            this.learner = learner;
            learnerName.text = learner.Object.StateAuthority.ToString();
        }
    }
}
