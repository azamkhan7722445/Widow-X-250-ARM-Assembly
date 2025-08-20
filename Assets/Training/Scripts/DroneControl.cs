using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Locomotion;
using Fusion.Addons.HapticAndAudioFeedback;
using UnityEngine.Events;
using Fusion.Addons.Learning;
using Fusion.Addons.Reconnection;

/// <summary>
/// This class in is in charge to control the drone with the user's input.
/// The drone can be controled when the activity is open and if the player switch on the "remote control" (menu button on the left controller).
/// The drone's position and inclination are synchronized by Network Transforms. 
/// </summary>
[DefaultExecutionOrder(DroneControl.EXECUTION_ORDER)]
public class DroneControl : NetworkBehaviour, IRecoverableListener
{
    const int EXECUTION_ORDER = 10;

    [System.Serializable]
    public enum DroneRemoteControlStatus
    {
        SwitchedOff,
        SwitchedOn
    }

    [System.Serializable]
    public enum UAVStatus
    {
        NotYetInitialized,
        ReadyToFly,
        LimitedFlyingMode,      // no pitch & roll
        Flying,
        AutoLanding,
        Landed                  // only for autoLanding
    }

    [Header("Global parameters")]
    [SerializeField] private ActivityDroneControl activityDroneControl;


    [Networked]
    [SerializeField] public UAVStatus DroneStatus { get; set; } = UAVStatus.NotYetInitialized;

    [Networked]
    [SerializeField] public DroneRemoteControlStatus RemoteControlStatus { get; set; } = DroneRemoteControlStatus.SwitchedOff;

    DroneRemoteControlStatus previousRemoteControlStatus;
    UAVStatus previousDroneStatus;

    RigLocomotion rigLocomotion;
    SoundManager soundManager;
    bool activityWasOpen = true;

    [Header("Inputs")]
    public InputActionProperty leftControllerAction;
    public InputActionProperty rightControllerAction;
    public InputActionProperty menuControllerAction;
    float leftStickX;
    float leftStickY;
    float rightStickX;
    float rightStickY;

    [Header("Drone objets")]
    public NetworkTransform drone;
    public Transform tiltVisual;
    [SerializeField] List<Transform> propellers = new List<Transform>();
    bool isLocalPlayerDrone = false;

    [Header("Throttle parameters")]
    [SerializeField] private float throttle = 0f;
    [SerializeField] private float throttleMin = 0f;
    [SerializeField] private float throttleMax = 20f;
    [SerializeField] private float throttleWhenSwitchON = 5f;
    [SerializeField] private float throttleThreshold = 10f;           // Power threshold before moving the drone
    [SerializeField] private float throttleNeutralZone = 0.15f;       // neutral zone
    [SerializeField] private float throttleChangeSpeed = 3f;
    [SerializeField] private float throttleForAutoLanding = 9.6f;
    [SerializeField] private float droneThrottleResetTime = 0.3f;
    [SerializeField] bool playerHasMovedTheThrottleLever = false;

    [Header("Pitch parameters")]
    [SerializeField] private float dronePitchNeutralZone = 0.02f;
    [SerializeField] private float dronePitchChangeSpeed = 4f;
    [SerializeField] private float dronePitch;
    [SerializeField] private float dronePitchMin = -5;
    [SerializeField] private float dronePitchMax = 5f;
    [SerializeField] private float dronePitchResetTime = 0.3f;

    [Header("Roll parameters")]
    [SerializeField] private float droneRollNeutralZone = 0.02f;
    [SerializeField] private float droneRollChangeSpeed = 4f;
    [SerializeField] private float droneRoll;
    [SerializeField] private float droneRollMin = -5;
    [SerializeField] private float droneRollMax = 5f;
    [SerializeField] private float droneRollResetTime = 0.3f;

    [Header("Yaw parameters")]
    [SerializeField] private float droneYawNeutralZone = 0.02f;
    [SerializeField] private float droneYawChangeSpeed = 150f;
    [SerializeField] private float droneYaw;
    [SerializeField] private float droneYawMin = -150;
    [SerializeField] private float droneYawMax = 150f;
    [SerializeField] private float droneYawResetTime = 0.3f;

    [Header("Tilt parameters")]
    [SerializeField] private float tiltAngleMax = 20f;
    private Quaternion tiltInitialRotation;
    private float currentTiltX = 0f;
    private float currentTiltY = 0f;

    [Header("Fly parameters")]
    [SerializeField] private float minimumHeightBeforePitchAndRoll = 0.15f;
    private Vector3 accumulatedMove;
    private float accumulatedRotationY = 0f;

    [Header("Propellers parameters")]
    [SerializeField] float propellerAccelerationTime = 2f;
    [SerializeField] float propellerDecelerationTime = 4f;
    [SerializeField] float propellerMinRotationSpeed = 500f;
    [SerializeField] float propellerotationSpeedMultiplier = 150f;
    private float currentPropellerSpeed = 0f;                           // store current propellers' speed
    private float targetPropellerSpeed = 0f;                            // store target propellers' speed
    private float previousPropellerSpeed = 0f;
    private float propellerSpeedReference = 0f;
    private float elapsedTimeSinceDroneSwitchOn = 0f;
    private float elapsedTimeSinceDroneSwitchOff = 0f;


    [Header("World limits")]
    [SerializeField] private float Xmin = -14.3f;
    [SerializeField] private float Xmax = 14f;
    [SerializeField] private float Ymin = 0f;
    [SerializeField] private float Ymax = 13.6f;
    [SerializeField] private float Zmin = -24f;
    [SerializeField] private float Zmax = -7.5f;


    [Header("Visual Circle Feedback")]
    [SerializeField] private Transform droneCircle;
    private bool remoteControlWasSwitchedOn = false;
    private float animatedScale = 1;
    private float animatedScaleTarget = 1;
    private float animationDuration = 1f;
    private bool shouldBeVisible = false;
    private bool stopCircleAnimation = false;


    [Header("Audio Feedback")]
    [SerializeField] private bool enableAudioFeedback = true;
    [SerializeField] private AudioSource audiosource = null;
    [SerializeField] private string droneStartingAudioType = "DroneStarting";
    [SerializeField] private string droneLandingAudioType = "DroneLanding";
    [SerializeField] private string droneFlyingAudioType = "DroneFlying";
    [SerializeField] private string droneReadyToFlyAudioType = "DroneReadyToFly";
    [SerializeField] private string droneAutoLandingAudioType = "DroneAutoLanding";
    [SerializeField] private string droneLimitedFlyingModeAudioType = "DroneLimitedFlyingMode";


    public UnityEvent onRemoteControlSwitchedOn;
    public UnityEvent onRemoteControlSwitchedOff;

    public bool droneControlIsAuthorized = true;


    private void Awake()
    {
        // Controller binding
        var bindings = new List<string> { "joystick" };
        leftControllerAction.EnableWithDefaultXRBindings(leftBindings: bindings);
        rightControllerAction.EnableWithDefaultXRBindings(rightBindings: bindings);
        menuControllerAction.EnableWithDefaultXRBindings(leftBindings: new List<string> { "menu" });
        if (drone == null)
        {
            drone = GetComponent<NetworkTransform>();
        }
        if (tiltVisual)
        {
            tiltInitialRotation = tiltVisual.transform.localRotation;
        }

        // Find the ActivityDroneControl
        if (activityDroneControl == null) activityDroneControl = GetComponentInParent<ActivityDroneControl>();

        // Find the SoundManager, if not defined
        if (soundManager == null) soundManager = SoundManager.FindInstance();

        // Find the audiosource, if not defined
        if (audiosource == null) audiosource = GetComponent<AudioSource>();

    }

    public override void Spawned()
    {
        base.Spawned();
        UpdateDroneCirclePosition();
        if(Object.HasStateAuthority)
        {
            isLocalPlayerDrone = true;
        }
    }

    public override void Render()
    {
        base.Render();

        // Only the state authoruity controls the drone
        if (Object.HasStateAuthority)
        {
            // Configure the local player rig
            ConfigureRigLocomotion();

            // Read the player's inputs
            if (droneControlIsAuthorized)
                ReadPlayerInputs();

            // Play an animation when the remote control is switched on/off
            ManageDroneCircleDisplay();

            // Calculate the drone parameters (throttle/yaw/pitch/roll) based on the player's inputs
            ComputeDroneParametersWithInputs();

            // Update the drone status (flying, auto-landing, etc.) based on the current drone status and the remote control
            UpdateDroneStatus();

            // Calculate the pitch & roll tilt
            ComputeDroneVisualTilt();
        }

        // The visual & audio feedback must seen/heard on all clients 
        PropellersAnimation();
        PlayDroneAudioFeedback();

        // Manage circle visibility if a player disconnect/reconnect
        CheckDroneCircleVisibility();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (drone && (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn || DroneStatus == UAVStatus.AutoLanding))
        {
            // Update the drone position based on the players's input processed during the render frames
            UpdateDronePositionAndRotation();

            // check that the drone remains within the defined limits
            CheckDronePosition();

            // Reset accumulated value
            accumulatedRotationY = 0f;
            accumulatedMove = Vector3.zero;
        }

        // Update the drone status if the drone has landed
        CheckIfDroneHasLanded();
    }

    // check that the drone remains within the defined limits
    private void CheckDronePosition()
    {

        Vector3 newDronePosition = drone.transform.position;
        bool dronePositionCorrectionIsRequired = false;

        if (drone.transform.position.x < Xmin)
        {
            newDronePosition.x = Xmin;

            dronePositionCorrectionIsRequired = true;
        }
        if (drone.transform.position.x > Xmax)
        {
            newDronePosition.x = Xmax;
            dronePositionCorrectionIsRequired = true;
        }

        if (drone.transform.position.y < Ymin)
        {
            newDronePosition.y = Ymin;
            dronePositionCorrectionIsRequired = true;
        }
        if (drone.transform.position.y > Ymax)
        {
            newDronePosition.y = Ymax;
            dronePositionCorrectionIsRequired = true;
        }

        if (drone.transform.position.z < Zmin)
        {
            newDronePosition.z = Zmin;
            dronePositionCorrectionIsRequired = true;
        }
        if (drone.transform.position.z > Zmax)
        {
            newDronePosition.z = Zmax;
            dronePositionCorrectionIsRequired = true;
        }

        if (dronePositionCorrectionIsRequired)
        {
            drone.transform.position = newDronePosition;
            dronePitch = 0f;
            droneRoll = 0f;
            droneYaw = 0f;
            throttle = throttleThreshold;
        }
    }

    private void OnDestroy()
    {
        // restore locomotion control if the drone was active
        if (rigLocomotion)
        {
            if (rigLocomotion.disableLeftHandRotation)
                rigLocomotion.disableLeftHandRotation = false;
            if (rigLocomotion.disableRightHandRotation)
                rigLocomotion.disableRightHandRotation = false;
        }
    }
    // Read the player's inputs
    private void ReadPlayerInputs()
    {
        bool activityIsOpen = activityDroneControl.ActivityStatusOfLearner != LearnerActivityTracker.Status.Disabled;

        if (menuControllerAction.action.WasPressedThisFrame())
        {
            if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOff && activityIsOpen)
            {
                RemoteControlStatus = DroneRemoteControlStatus.SwitchedOn;
                if (onRemoteControlSwitchedOn != null)
                    onRemoteControlSwitchedOn.Invoke();
            }
            else
            {
                RemoteControlStatus = DroneRemoteControlStatus.SwitchedOff;
                if (onRemoteControlSwitchedOff != null)
                    onRemoteControlSwitchedOff.Invoke();
            }

        }

        // Force the remote control to off if the activity is closed
        if (activityWasOpen && activityIsOpen == false)
        {
            RemoteControlStatus = DroneRemoteControlStatus.SwitchedOff;
        }

        // Drone control mode 2 :
        // Left Stick X = Yaw
        // Left Stick Y = Throttle
        // Right Stick X = Roll
        // Right Stick Y = Pitch

        leftStickX = leftControllerAction.action.ReadValue<Vector2>().x;
        leftStickY = leftControllerAction.action.ReadValue<Vector2>().y;
        rightStickX = rightControllerAction.action.ReadValue<Vector2>().x;
        rightStickY = rightControllerAction.action.ReadValue<Vector2>().y;

        activityWasOpen = activityIsOpen;
    }

    void UpdateDroneCirclePosition()
    {
        droneCircle.transform.position = new Vector3(drone.transform.position.x, droneCircle.position.y, drone.transform.position.z);
    }

    // Play an animation when the remote control is switched on/off
    private void ManageDroneCircleDisplay()
    {
        bool remoteControlIsSwitchedOn = RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn;

        // Hide the circle if the RC is switched on
        if (remoteControlIsSwitchedOn && remoteControlWasSwitchedOn == false)
        {
            animatedScaleTarget = 0;
            shouldBeVisible = false;
            stopCircleAnimation = false;
        }

        // Show the circle if the RC is switched off
        if (remoteControlIsSwitchedOn == false && remoteControlWasSwitchedOn == true)
        {
            animatedScaleTarget = 1;
            shouldBeVisible = true;
            stopCircleAnimation = false;
            UpdateDroneCirclePosition();
        }
        remoteControlWasSwitchedOn = remoteControlIsSwitchedOn;

        if (stopCircleAnimation == false)
        {

            if (shouldBeVisible && animatedScale > 0.98f)
            {
                stopCircleAnimation = true;
                droneCircle.localScale = Vector3.one;
            }
            else if (shouldBeVisible == false && animatedScale < 0.02f)
            {
                stopCircleAnimation = true;
                droneCircle.localScale = Vector3.zero;
            }
            else
            {
                float t = Time.deltaTime / animationDuration;
                animatedScale = Mathf.Lerp(animatedScale, animatedScaleTarget, t);
                droneCircle.localScale = animatedScale * Vector3.one;
            }
        }
    }

    // Manage circle visibility if a player disconnect/reconnect
    private void CheckDroneCircleVisibility()
    {
        if (droneCircle)
        {
            if (Object.HasStateAuthority)
            {
                if (droneCircle.gameObject.activeInHierarchy == false)
                {
                    droneCircle.gameObject.SetActive(true);
                }
            }
            else
            {
                if (droneCircle.gameObject.activeInHierarchy == true)
                {
                    droneCircle.gameObject.SetActive(false);
                }
            }
        }
    }

    // Configure the local player rig
    private void ConfigureRigLocomotion()
    {
        // we do not change the rig locomotion in case the local player has the state authority on a remote player's drone (in case this player disconnects)
        if(isLocalPlayerDrone == false)
        {
            return;
        }

        // Set the rig locomotion pour the local player
        FindRigLocomotion();

        // The drone is switched on
        if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
        {
            // Disable the player rotation when the drone is switched on
            if (rigLocomotion.disableLeftHandRotation == false)
                rigLocomotion.disableLeftHandRotation = true;

            if (rigLocomotion.disableRightHandRotation == false)
                rigLocomotion.disableRightHandRotation = true;
        }

        // The drone is switched off
        else
        {
            if (rigLocomotion.disableLeftHandRotation)
                rigLocomotion.disableLeftHandRotation = false;
            if (rigLocomotion.disableRightHandRotation)
                rigLocomotion.disableRightHandRotation = false;
        }
    }

    // Calculate the drone parameters (throttle/yaw/pitch/roll) based on the player's inputs
    private void ComputeDroneParametersWithInputs()
    {
        // Compute drone commands only if the drone is switched on
        if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
        {
            // Throttle
            if (Mathf.Abs(leftStickY) > throttleNeutralZone)
            {
                throttle += leftStickY * throttleChangeSpeed * Time.deltaTime;
                if (playerHasMovedTheThrottleLever == false)
                    playerHasMovedTheThrottleLever = true;
            }
            else
            {
                float t = Time.deltaTime / droneThrottleResetTime;

                if (DroneStatus != UAVStatus.ReadyToFly)
                    throttle = Mathf.Lerp(throttle, throttleThreshold, t);
            }
            throttle = Mathf.Clamp(throttle, throttleMin, throttleMax);


            // Pitch
            if (Mathf.Abs(rightStickY) > dronePitchNeutralZone)
            {
                dronePitch += rightStickY * dronePitchChangeSpeed * Time.deltaTime;
            }
            else
            {
                float t = Time.deltaTime / dronePitchResetTime;
                dronePitch = Mathf.Lerp(dronePitch, 0f, t);
            }
            dronePitch = Mathf.Clamp(dronePitch, dronePitchMin, dronePitchMax);

            // Roll
            if (Mathf.Abs(rightStickX) > droneRollNeutralZone)
            {
                droneRoll += rightStickX * droneRollChangeSpeed * Time.deltaTime;
            }
            else
            {
                float t = Time.deltaTime / droneRollResetTime;
                droneRoll = Mathf.Lerp(droneRoll, 0f, t);
            }
            droneRoll = Mathf.Clamp(droneRoll, droneRollMin, droneRollMax);

            // Yaw
            if (Mathf.Abs(leftStickX) > droneYawNeutralZone)
            {
                droneYaw += leftStickX * droneYawChangeSpeed * Time.deltaTime;
            }
            else
            {
                float t = Time.deltaTime / droneYawResetTime;
                droneYaw = Mathf.Lerp(droneYaw, 0f, t);
            }
            droneYaw = Mathf.Clamp(droneYaw, droneYawMin, droneYawMax);
        }
    }

    // Update the drone status (flying, auto-landing, etc.) based on the current drone status and the remote control
    private void UpdateDroneStatus()
    {
        // drone is switched on
        if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
        {
            // drone init => set minimal power
            if (DroneStatus == UAVStatus.NotYetInitialized)
            {
                throttle = throttleWhenSwitchON;
                DroneStatus = UAVStatus.ReadyToFly;
            }

            else
            {
                // disable auto landing if the drone was already flying with auto landing
                if (DroneStatus == UAVStatus.AutoLanding)
                {
                    DroneStatus = UAVStatus.Flying;

                    // Set the throttle to throttleThreshold so the drone don't move up/down
                    throttle = throttleThreshold;
                }

                // if drone can fly or is flying
                else if (DroneStatus == UAVStatus.ReadyToFly || DroneStatus == UAVStatus.LimitedFlyingMode || DroneStatus == UAVStatus.Flying)
                {
                    // Limit drone movements when close to the ground
                    if (drone.transform.position.y < minimumHeightBeforePitchAndRoll)
                    {
                        if ((DroneStatus == UAVStatus.ReadyToFly && playerHasMovedTheThrottleLever) || DroneStatus == UAVStatus.Flying)
                        {
                            DroneStatus = UAVStatus.LimitedFlyingMode;
                        }
                    }
                    else
                    {
                        // Drone is in the air
                        if (DroneStatus != UAVStatus.Flying)
                        {
                            DroneStatus = UAVStatus.Flying;
                        }
                    }

                    if (DroneStatus == UAVStatus.LimitedFlyingMode)
                    {
                        // allow only the drone to go up/down
                        accumulatedMove += Time.deltaTime * (transform.up * (throttle - throttleThreshold));
                    }

                    if (DroneStatus == UAVStatus.Flying)
                    {
                        // allow all movements
                        accumulatedMove += Time.deltaTime * (
                               transform.forward * dronePitch +
                               transform.right * droneRoll +
                               transform.up * (throttle - throttleThreshold));
                    }

                    // Read the rotation
                    accumulatedRotationY += Time.deltaTime * droneYaw;
                }
            }
        }

        // Drone is switched off
        else
        {
            // Reset the drone status is not yet flying
            if (DroneStatus == UAVStatus.ReadyToFly)
            {
                DroneStatus = UAVStatus.NotYetInitialized;

                // edge case to manage propeller because propeller animation is called too late
                targetPropellerSpeed = 0f;
                elapsedTimeSinceDroneSwitchOff = 0f;
            }

            // Enable auto landing if drone is in the air
            if (DroneStatus == UAVStatus.Flying || DroneStatus == UAVStatus.LimitedFlyingMode)
            {
                DroneStatus = UAVStatus.AutoLanding;
                dronePitch = 0;
                droneRoll = 0;
            }

            // Stop the motor if the drone landed
            if (DroneStatus == UAVStatus.Landed)
            {
                DroneStatus = UAVStatus.NotYetInitialized;
                throttle = 0;
                playerHasMovedTheThrottleLever = false;
            }

            // move the drone on the ground when auto landing 
            if (DroneStatus == UAVStatus.AutoLanding)
            {
                throttle = throttleForAutoLanding;
                // Set the power based on the gaz
                accumulatedMove += Time.deltaTime * (transform.up * (throttle - throttleThreshold));
            }
        }
    }

    private void PlayDroneAudioFeedback()
    {
        if (DroneStatus != previousDroneStatus || RemoteControlStatus != previousRemoteControlStatus)
        {
            switch (DroneStatus)
            {
                case UAVStatus.NotYetInitialized:
                    if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOff && previousRemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
                    {
                        PlayAudioFeedback(droneLandingAudioType);
                    }
                    else if (previousDroneStatus == UAVStatus.AutoLanding && DroneStatus == UAVStatus.NotYetInitialized)
                    {
                        PlayAudioFeedback(droneLandingAudioType);
                    }
                    break;

                case UAVStatus.ReadyToFly:

                    if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn && previousRemoteControlStatus == DroneRemoteControlStatus.SwitchedOff)
                        PlayAudioFeedback(droneStartingAudioType);
                    else
                        PlayAudioFeedback(droneReadyToFlyAudioType, true);
                    break;

                case UAVStatus.LimitedFlyingMode:
                    PlayAudioFeedback(droneLimitedFlyingModeAudioType, false);
                    break;

                case UAVStatus.Flying:
                    PlayAudioFeedback(droneFlyingAudioType, false);
                    break;

                case UAVStatus.AutoLanding:
                    PlayAudioFeedback(droneAutoLandingAudioType);
                    break;

                case UAVStatus.Landed:
                    PlayAudioFeedback(droneLandingAudioType);
                    break;
            }
            previousRemoteControlStatus = RemoteControlStatus;
            previousDroneStatus = DroneStatus;
        }
        else
        {
            // we need to change the audio when the droneStartingAudioType is finished
            if (DroneStatus == UAVStatus.ReadyToFly)
            {
                if (audiosource.isPlaying == false)
                {
                    PlayAudioFeedback(droneReadyToFlyAudioType);
                }
            }
        }
    }

    // Apply rotation on the propellers
    private void PropellersAnimation()
    {
        CheckPropellersParameters();
        ComputePropellersSpeed();

        // animate the propeller
        for (int i = 0; i < propellers.Count; i++)
        {
            propellers[i].Rotate(Vector3.forward * currentPropellerSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void CheckPropellersParameters()
    {
        // throttle level for remote player to avoid sync it but keep propeller animation
        if (Object.HasStateAuthority == false)
        {
            if (DroneStatus != UAVStatus.NotYetInitialized && DroneStatus != UAVStatus.ReadyToFly)
            {
                throttle = throttleThreshold;
            }
            else
            {
                throttle = throttleWhenSwitchON;
            }
        }


        // drone is switched on
        if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
        {
            // Set the propellers' parameters if propellers were not yet configured
            if (DroneStatus == UAVStatus.NotYetInitialized)
            {
                // Set minimal rotation
                propellerMinRotationSpeed = throttleWhenSwitchON * propellerotationSpeedMultiplier;
                targetPropellerSpeed = propellerMinRotationSpeed;

                // Reset the counter only if the drone is not already flying
                elapsedTimeSinceDroneSwitchOn = 0f;
            }

            // avoid propeller acceleration if the drone is already autolanding (in case of reconnection)
            else if (DroneStatus == UAVStatus.Flying)
            {
                elapsedTimeSinceDroneSwitchOn = propellerAccelerationTime;
                targetPropellerSpeed = Mathf.Max(propellerMinRotationSpeed, throttle * propellerotationSpeedMultiplier);
            }

            // Drone was already switched on and propellers were already enabled
            else
            {
                // Reset the counter only if the drone is not already flying 
                elapsedTimeSinceDroneSwitchOn += Time.deltaTime;

                // Set the rotation based on the throttle
                targetPropellerSpeed = Mathf.Max(propellerMinRotationSpeed, throttle * propellerotationSpeedMultiplier);
            }
        }

        // drone is switched off
        else
        {
            // Reset the propellers' parameters if the drone is flying
            if (DroneStatus == UAVStatus.ReadyToFly || DroneStatus == UAVStatus.LimitedFlyingMode || DroneStatus == UAVStatus.Flying || DroneStatus == UAVStatus.AutoLanding)
            {
                // Reset the target speed
                targetPropellerSpeed = 0f;
                elapsedTimeSinceDroneSwitchOff = 0f;
            }

            // Drone was already switched off and propellers's parameters were already reset
            // Start the timer only if the drone is on the ground
            if (DroneStatus == UAVStatus.Landed || DroneStatus == UAVStatus.NotYetInitialized)
            {
                elapsedTimeSinceDroneSwitchOff += Time.deltaTime;
            }

            
        }
    }

    private void ComputePropellersSpeed()
    {

        // Drone is switched on
        if (RemoteControlStatus == DroneRemoteControlStatus.SwitchedOn)
        {
            // Check if the propeller acceleration time is over
            if (elapsedTimeSinceDroneSwitchOn < propellerAccelerationTime)
            {
                // Increase the speed progressively
                float t = Mathf.Clamp01(elapsedTimeSinceDroneSwitchOn / propellerAccelerationTime);
                currentPropellerSpeed = Mathf.Lerp(previousPropellerSpeed, targetPropellerSpeed, t);
            }
            else
            {   // Full speed
                currentPropellerSpeed = targetPropellerSpeed;
            }
            propellerSpeedReference = currentPropellerSpeed;
        }

        // Drone is switched off
        else
        {
            // Wait the drone to touch the ground before reducing the propeller
            if (DroneStatus == UAVStatus.Landed || DroneStatus == UAVStatus.NotYetInitialized)
            {
                // Check if the propeller deceleration time is over
                if (elapsedTimeSinceDroneSwitchOff < propellerDecelerationTime)
                {
                    // Reduce the speed progressively
                    float t = Mathf.Clamp01(elapsedTimeSinceDroneSwitchOff / propellerDecelerationTime);
                    currentPropellerSpeed = Mathf.Lerp(propellerSpeedReference, targetPropellerSpeed, t);
                }
                else
                {
                    // Stop the rotation
                    currentPropellerSpeed = targetPropellerSpeed;
                }

                // save previous speed
                previousPropellerSpeed = currentPropellerSpeed;
            }

            // In case of quick disconnect/reconnect, we have to set the propeller speed if the drone is still in AutoLanding
            if (DroneStatus == UAVStatus.AutoLanding)
            {
                currentPropellerSpeed = throttleThreshold * propellerotationSpeedMultiplier;
            }
        }
    }

    // Update the drone position based on the players's input processed during the render frames
    private void UpdateDronePositionAndRotation()
    {
        // Update the Position
        drone.transform.position += accumulatedMove;

        // Apply drone rotation in Y axis
        drone.transform.Rotate(Vector3.up * accumulatedRotationY, Space.World);

    }

    // Calculate the pitch & roll tilt
    private void ComputeDroneVisualTilt()
    {
        if (tiltVisual != null)
        {
            if (DroneStatus == UAVStatus.Flying)
            {
                // Compute tilt for forward/backward 
                float unclampedTiltX = dronePitch * dronePitchChangeSpeed;
                currentTiltX = Mathf.Clamp(unclampedTiltX, -tiltAngleMax, tiltAngleMax);

                // Compute tilt for Left/right
                float unclampedTiltY = droneRoll * droneRollChangeSpeed;
                currentTiltY = Mathf.Clamp(unclampedTiltY, -tiltAngleMax, tiltAngleMax);

                // Apply tilt
                tiltVisual.localRotation = tiltInitialRotation * Quaternion.Euler(currentTiltX, currentTiltY, 0);
            }
            else
            {
                // Reset tilt if drone is not flying
                tiltVisual.localRotation = tiltInitialRotation;
            }
        }
    }

    // Update the drone status if the drone has landed
    private void CheckIfDroneHasLanded()
    {
        // Check if the drone landed
        if (DroneStatus == UAVStatus.AutoLanding && drone.transform.position.y < 0.01f)
        {
            DroneStatus = UAVStatus.Landed;
        }
    }

    private void FindRigLocomotion()
    {
        if (rigLocomotion == null)
        {
            rigLocomotion = FindAnyObjectByType<RigLocomotion>();
        }

        if (rigLocomotion == null)
        {
            Debug.LogError("RigLocomotion not found");
        }
    }

    private void PlayAudioFeedback(string audioType, bool waitForTheEndOfPreviousClip = false)
    {
        if (enableAudioFeedback && soundManager)
        {
            soundManager.Play(audioType, audiosource, waitForTheEndOfPreviousClip);
        }
    }

    public void OnRecovered(Recoverable recovered, IRecoverRequester requester)
    {
        isLocalPlayerDrone = true;
    }
}
