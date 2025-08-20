using Fusion;
using Fusion.Addons.Reconnection;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fusion.Addons.Learning
{
    /// <summary>
    /// The LearningParticipant class is located on the networked rig of the prefab spawned when the user connects to the room.
    /// It defines :
    ///     - if the user is a learner (by default) or a teacher (IsLearner parameter)
    ///     - a UserID identifier to detect disconnections,
    /// It manages :
    ///     - the registration process with the LearningManager (thanks the inherited classes LearnerComponent/Subscriber),
    ///     - the activity prefab spawn once registered with the LearningManager ,
    ///     - the authority recovery over its objects in case of reconnection (thanks to the IRecoverRequester interface).
    /// </summary>
    [RequireComponent(typeof(ReconnectionHandler))]
    public class LearningParticipant : LearnerComponent, IRecoverRequester
    {
        public List<NetworkObject> learnerNetworkObjects = new List<NetworkObject>();

        // to store if this learner is registered to a learning manager
        List<LearningManager> registeredForLearningManagers = new List<LearningManager>();
        public List<LearningManager> activitiesPrefabSpawnedForLearningManagers = new List<LearningManager>();
        List<LearningManager> learningManagerToRegisterOn = new List<LearningManager>();

        ReconnectionHandler reconnectionHandler;

        [Networked, OnChangedRender(nameof(OnIsLearnerChange))]
        public NetworkBool IsLearner { get; set; }

        [SerializeField] MeshRenderer playerCap;
        [SerializeField] Material learnerMaterial;
        [SerializeField] Material teacherMaterial;

        public interface ILearnerAssociatedComponent
        {
            public NetworkString<_64> UserId { get; set; }
            public LearningManager LearningManager { get; set; }
        }

        void OnIsLearnerChange()
        {
            UpdateLearnerCap();
        }

        #region LearnerComponent

        public override NetworkString<_64> UserId => reconnectionHandler.UserId;

        public override bool IsAvailable => base.IsAvailable && IsLearner;
        #endregion

        [SerializeField] bool debug = false;

        private void Awake()
        {
            reconnectionHandler = GetComponent<ReconnectionHandler>();
            reconnectionHandler.onRecovery.AddListener(OnRecovered);
        }


        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasStateAuthority)
            {
                // Default as learner
                IsLearner = true;
            }

            // Set the networkRig as the PlayerObject to find it easily
            Runner.SetPlayerObject(Object.StateAuthority, Object);
            UpdateLearnerCap();
        }


        private void UpdateLearnerCap()
        {
            if (playerCap != null)
            {
                if (IsLearner)
                {
                    playerCap.material = learnerMaterial;
                }
                else
                {
                    playerCap.material = teacherMaterial;
                }
            }
        }

        public void LearnerSlotBookedOnLearningManager(LearningManager learningManager, GameObject activityPrefab, Vector3 spawnPosition, Quaternion spawnRotation, bool alreadyUsedSlot)
        {
            if (Object.HasStateAuthority)
            {
                if (activitiesPrefabSpawnedForLearningManagers.Contains(learningManager) == false)
                {
                    activitiesPrefabSpawnedForLearningManagers.Add(learningManager);

                    bool shouldSpawn = alreadyUsedSlot == false;
                    if (shouldSpawn)
                    {
                        if (debug) Debug.LogError($"Learner => OnRegisteredOnLearningManager : spawn activity prefabs {activityPrefab} at {spawnPosition}");
                        var activityTableObj = Runner.Spawn(activityPrefab, spawnPosition, spawnRotation);

                        // Set the LearningManager on all activities
                        foreach (var activity in activityTableObj.GetComponentsInChildren<LearnerActivityTracker>())
                        {
                            activity.LearningManager = learningManager;
                            activity.LearnerId = Id;
                        }
                        foreach (var recoverable in activityTableObj.GetComponentsInChildren<Recoverable>())
                        {
                            recoverable.UserId = UserId;
                        }

                        if (activityTableObj != null)
                        {
                            learnerNetworkObjects = activityTableObj.GetComponentsInChildren<NetworkObject>().ToList();

                            foreach(var associatedComponent in activityTableObj.GetComponentsInChildren<ILearnerAssociatedComponent>())
                            {
                                associatedComponent.UserId = UserId;
                                associatedComponent.LearningManager = learningManager;
                            }
                        }
                    }
                }
            }
        }

        public void LearnerSlotReleasedOnLearningManager(LearningManager learningManager)
        {
            // destroy all networkObject of the player
            DestroyPlayerNetworkObjects();
            activitiesPrefabSpawnedForLearningManagers.Remove(learningManager);
        }

        [SerializeField] bool deletionWorkaround = true;

        public void DestroyPlayerNetworkObjects()
        {
            if (debug) Debug.LogError($"Number of NO to Destroy " + learnerNetworkObjects.Count());

            if (deletionWorkaround && learnerNetworkObjects.Count > 0)
            {
                learnerNetworkObjects.RemoveAll(obj =>
                {
                    if (obj == null)
                    {
                        return true;
                    }
                    var no = obj.GetComponentsInChildren<NetworkObject>();
                    if (no.Length == 1)
                    {
                        Runner.Despawn(obj);
                        if (debug) Debug.LogError($"Destroy {obj}");
                        return true;
                    }
                    if (debug) Debug.LogError($"can not destroy {obj}/{no.Length}");

                    return false;
                });
            }

            foreach (NetworkObject networkObjectToDestroy in learnerNetworkObjects)
            {
                if (networkObjectToDestroy.IsValid)
                {
                    if (debug) Debug.LogError($"Final destroy {networkObjectToDestroy}");
                    Runner.Despawn(networkObjectToDestroy);
                }
            }
        }

        #region IRecoverRequester
        public void OnRecovered(Recoverable recovered)
        {
            learnerNetworkObjects.Add(recovered.Object);

            foreach (var learnerActivityTracker in recovered.GetComponentsInChildren<LearnerActivityTracker>())
            {
                // The Learner field was set to null when we initially disconnected: recovering it
                learnerActivityTracker.LearnerId = Id;
            }
        }
        #endregion
    }

}
