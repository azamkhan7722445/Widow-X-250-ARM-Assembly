using Fusion;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is used to change the user's role with the console buttons
    /// </summary>
    public class PlayerRoleSelection : NetworkBehaviour
    {
        LearningParticipant learner;

        [EditorButton("RequestTrainerRole")]
        public void RequestTrainerRole()
        {
            Debug.Log("RequestTrainerRole");

            // The touch is called locally only
            // we need to find the networkRig of the local player
            if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out var learnerNO))
            {
                learner = learnerNO.GetComponent<LearningParticipant>();
                if (learner != null)
                {
                    learner.IsLearner = false;
                }
                else
                    Debug.LogError($"Learner not found on {learnerNO.name}");
            }
        }

        [EditorButton("RequestLearnerRole")]
        public void RequestLearnerRole()
        {
            Debug.Log("RequestLearnerRole");

            // The touch is called locally only
            // we need to find the networkRig of the local player
            if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out var learnerNO))
            {
                learner = learnerNO.GetComponent<LearningParticipant>();
                if (learner != null)
                {
                    learner.IsLearner = true;
                }
                else
                    Debug.LogError($"Learner not found on {learnerNO.name}");
            }
        }
    }
}
