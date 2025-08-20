using Fusion;
using Fusion.Addons.Touch;
using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is in charge of resetting the activity objets position and change the activity status to "ReadyToStart" when the player push the reset button.
    /// </summary>

    public class ResetActivity : NetworkBehaviour
    {
        Touchable touchable;

        [SerializeField]
        GameObject rootObjectToSearchForNetworkObject;
        [SerializeField]
        protected List<NetworkTransform> networkTransformsToReset = new List<NetworkTransform>();
        protected List<NetworkTransform> pendingResetNetworkTransforms = new List<NetworkTransform>();
        protected List<NetworkTransform> pendingStateAuthorityNetworkTransforms = new List<NetworkTransform>();
        [SerializeField]
        Dictionary<NetworkTransform, TransformData> networkTransformsData = new Dictionary<NetworkTransform, TransformData>();

        protected bool resetRequested = false;

        [SerializeField] LearnerActivityTracker learnerActivityTracker;

        public struct TransformData
        {
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }

            public TransformData(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
        protected virtual void Awake()
        {
            if (touchable == null)
                touchable = GetComponent<Touchable>();
            if (touchable == null)
                Debug.LogError($"Touchable not found on {gameObject.name}");

            // Find all NetWorkTransform
            if (networkTransformsToReset.Count == 0 && rootObjectToSearchForNetworkObject != null)
            {
                networkTransformsToReset.AddRange(rootObjectToSearchForNetworkObject.GetComponentsInChildren<NetworkTransform>(true));
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            // Save position and rotation in a dictionnary
            foreach (NetworkTransform networkTransform in networkTransformsToReset)
            {
                TransformData transformdata = new TransformData(networkTransform.transform.position, networkTransform.transform.rotation);
                networkTransformsData.Add(networkTransform, transformdata);
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (touchable)
            {
                touchable.onTouch.AddListener(OnReset);
            }
        }

        protected async virtual void OnReset()
        {
            resetRequested = true;

            // It is required to get the state authority to run the FUN and reset the position
            if (Object.HasStateAuthority == false)
            {
                await Object.WaitForStateAuthority();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (resetRequested)
            {
                resetRequested = false;
                InitialReset();
                ResetActivityStatus();
            }
            else if (pendingResetNetworkTransforms.Count != 0)
            {
                HandlePendingAuthorityTransferReset();
            }
        }

        private void ResetActivityStatus()
        {
            if (learnerActivityTracker && learnerActivityTracker.LearningManager)
            {
                // check the global activity status
                foreach (var activityStatus in learnerActivityTracker.LearningManager.ActivitiesAvailability)
                {
                    var index = activityStatus.Key;
                    var status = activityStatus.Value;
                    if (index == learnerActivityTracker.activityId)
                    {
                        if (status == LearningManager.ActivityStatus.Open)
                        {
                            learnerActivityTracker.ActivityStatusOfLearner = LearnerActivityTracker.Status.ReadyToStart;
                        }
                        else
                        {
                            learnerActivityTracker.ActivityStatusOfLearner = LearnerActivityTracker.Status.Disabled;
                        }
                    }
                }
            }
        }

        protected virtual void InitialReset()
        {
            pendingResetNetworkTransforms.Clear();
            pendingResetNetworkTransforms.AddRange(networkTransformsToReset);
            pendingStateAuthorityNetworkTransforms = new List<NetworkTransform>();
            foreach (var networkTransform in networkTransformsToReset)
            {
                if (networkTransform.HasStateAuthority == false)
                {
                    pendingStateAuthorityNetworkTransforms.Add(networkTransform);
                }
            }
            ResetTargets(requestAuthority: true);
        }

        protected virtual void HandlePendingAuthorityTransferReset()
        {
            // pendingResetNetworkTransforms will only contain object where the authority request has already been done once. Not requesting it again
            ResetTargets(requestAuthority: false);
        }

        protected void ResetTargets(bool requestAuthority)
        {
            List<NetworkTransform> stateAuthorityReceivedTransforms = new List<NetworkTransform>();
            foreach (NetworkTransform networkTransform in pendingStateAuthorityNetworkTransforms)
            {
                // Get State Authority
                if (networkTransform.HasStateAuthority == false)
                {
                    if (requestAuthority)
                    {
                        networkTransform.Object.RequestStateAuthority();
                    }
                }
                else
                {
                    stateAuthorityReceivedTransforms.Add(networkTransform);
                }
            }

            foreach (var networkTransform in stateAuthorityReceivedTransforms)
            {
                pendingStateAuthorityNetworkTransforms.Remove(networkTransform);
            }

            if (pendingStateAuthorityNetworkTransforms.Count == 0)
            {
                WillReset();

                foreach (NetworkTransform networkTransform in pendingResetNetworkTransforms)
                {
                    Reset(networkTransform);
                }
                DidReset();
            }
        }

        protected virtual void WillReset()
        {

        }

        protected virtual void DidReset()
        {
            pendingResetNetworkTransforms.Clear();
        }

        protected virtual void Reset(NetworkTransform networkTransform)
        {
            // Restore the initial position & rotation
            if (networkTransformsData.TryGetValue(networkTransform, out TransformData transformData))
            {
                networkTransform.transform.position = transformData.Position;
                networkTransform.transform.rotation = transformData.Rotation;
            }
        }

        private void OnDestroy()
        {
            touchable?.onTouch?.RemoveListener(OnReset);
        }

    }
}
