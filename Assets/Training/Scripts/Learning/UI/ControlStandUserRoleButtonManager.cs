using Fusion;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is in charge to display the correct button on the console
    /// </summary>
    public class ControlStandUserRoleButtonManager : MonoBehaviour
    {

        [SerializeField] GameObject requestLearnerRole;
        [SerializeField] GameObject requestTrainerRole;
        public NetworkRunner runner;
        LearningParticipant learnerOfLocalPlayer;
        bool previusLearnerRole = false;

        private void Awake()
        {
            if (requestLearnerRole == null)
                Debug.LogError("requestLearnerRole is not set");

            if (requestTrainerRole == null)
                Debug.LogError("requestTeacherRole is not set");

            // Check if a runner exist in the scene
            if (runner == null) runner = FindAnyObjectByType<NetworkRunner>();
        }

        void Update()
        {
            GetLocalPlayerLearner();
            if (learnerOfLocalPlayer)
            {
                if (learnerOfLocalPlayer.Object && previusLearnerRole != learnerOfLocalPlayer.IsLearner)
                {
                    if (learnerOfLocalPlayer.IsLearner)
                    {
                        requestLearnerRole.SetActive(false);
                        requestTrainerRole.SetActive(true);
                    }
                    else
                    {
                        requestLearnerRole.SetActive(true);
                        requestTrainerRole.SetActive(false);
                    }
                    previusLearnerRole = learnerOfLocalPlayer.IsLearner;
                }
            }
        }

        void GetLocalPlayerLearner()
        {
            if (learnerOfLocalPlayer == null)
            {
                if (runner && runner.LocalPlayer != PlayerRef.None && runner.TryGetPlayerObject(runner.LocalPlayer, out var learnerNO))
                {
                    learnerOfLocalPlayer = learnerNO.GetComponent<LearningParticipant>();
                    if (learnerOfLocalPlayer == null)
                        Debug.LogError($"Learner not found on {learnerNO.name}");
                }
            }
        }
    }
}
