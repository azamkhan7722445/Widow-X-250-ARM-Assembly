using Fusion;
using Fusion.Addons.Learning;
using Fusion.XR.Shared;
using System;
using UnityEngine;

/// <summary>
/// This class handles the drone control activity.
/// It calculates the Progress networked variable based on the distance between the initial position and the target position
/// </summary>
public class ActivityDroneControl : LearnerActivityTracker
{
    [Tooltip("Distance from the target that is considered to be a success")]
    [SerializeField] float successfulDistance = 1f;
    [SerializeField] float changeThreshold = 0.05f;
    [SerializeField] float droneYThreshold = 0.10f;
    private float lastProgress = 0f;

    [SerializeField] GameObject drone;
    [SerializeField] Transform droneStartPosition;
    [SerializeField] GameObject targetdestination;
    DroneControl droneControl;

    float initialDistance = -1f;
    bool droneAtDestination = false;

    [SerializeField] LearningManager learningManager;

    private void Start()
    {
        if (learningManager == null)
            learningManager = FindAnyObjectByType<LearningManager>();

        if (learningManager == null)
            Debug.LogError("Learning Manager not set");

        if (drone == null || targetdestination == null)
        {
            Debug.LogError("drone or destination not set");
        }
        else if (droneControl == null)
        {
            droneControl = drone.GetComponent<DroneControl>();
        }

        if (droneStartPosition == null)
        {
            droneStartPosition = drone.transform;
        }
    }


    public override void Render()
    {
        base.Render();
        if (Object.IsStateAuthorityPresent() == false)
        {
            TransferStateAuthority();
        }
    }

    async void TransferStateAuthority()
    {
        // One player must get the state authority on the activity drone control to change the remote status
        await droneControl.Object.EnsureHasStateAuthority();

        if (droneControl.Object && droneControl.Object.HasStateAuthority)
        {
            Debug.Log("A player is disconnected. Switching off the remote control...");
            // switch off the remote control
            droneControl.RemoteControlStatus = DroneControl.DroneRemoteControlStatus.SwitchedOff;
            droneControl.droneControlIsAuthorized = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        UpdateActivityProgress();
    }

    
    private void UpdateActivityProgress()
    {
        if (Object.HasStateAuthority)
        {
            // compute the initial distance when getting the stateAuth the first time
            if (initialDistance == -1f)
            {
                Vector3 distance = droneStartPosition.position - targetdestination.transform.position;
                initialDistance = distance.magnitude;
            }

            if (ActivityStatusOfLearner == Status.ReadyToStart && droneControl.DroneStatus == DroneControl.UAVStatus.Flying)
            {
                ActivityStatusOfLearner = Status.Started;
            }

            if (droneControl.DroneStatus == DroneControl.UAVStatus.LimitedFlyingMode || droneControl.DroneStatus == DroneControl.UAVStatus.Flying || droneControl.DroneStatus == DroneControl.UAVStatus.AutoLanding)
            {
                Vector3 distance = drone.transform.position - targetdestination.transform.position;
                float currentDistance = distance.magnitude;

                float newProgress = Mathf.Clamp01(1f - (currentDistance / initialDistance));
                float change = Mathf.Abs(newProgress - lastProgress);

                bool droneClosedToTheTargetPosition = (currentDistance < successfulDistance) && (drone.transform.position.y < droneYThreshold);

                if (change > changeThreshold || currentDistance < successfulDistance)
                {
                    if (droneClosedToTheTargetPosition)
                    {
                        if (droneAtDestination == false)
                        {
                            droneAtDestination = true;
                        }
                        Progress = 1;
                    }
                    else
                    {
                        if (droneAtDestination)
                        {
                            droneAtDestination = false;
                        }
                        Progress = newProgress;
                    }
                    lastProgress = Progress;
                }
            }
        }
    }
}
