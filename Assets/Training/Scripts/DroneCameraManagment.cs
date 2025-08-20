using Fusion;
using Fusion.Addons.Reconnection;
using UnityEngine;

/// <summary>
/// This class is in charge to handle the drone camera.
/// It listens to the DroneControl remote control events to enable the display only when the remote control is switched on.
/// Then it captures a frame of the drone camera at the frame rate defined by targetFrameRate variable.
/// </summary>
public class DroneCameraManagment : NetworkBehaviour, IRecoverableListener
{
    [SerializeField] private DroneControl droneControl;
    [SerializeField] private Camera cameraOnTheDrone;
    [SerializeField] private GameObject droneVideoFeebackGlobalObject;
    [SerializeField] private GameObject droneVideoCanvas;
    [SerializeField] private GameObject droneVideoScreen;
    [SerializeField] private Recoverable droneControlActivityRecoverable;

    public float targetFrameRate = 24f;
    private float nextRenderTime = 0f;
    private bool isCameraReady = false;
    private bool remoteControlIsActive = false;

    private void Awake()
    {
        if(cameraOnTheDrone == null || droneVideoFeebackGlobalObject == null || droneVideoCanvas == null || droneVideoScreen == null)
        {
            Debug.LogError("Object(s) not set in DroneCameraManagment");
        }

        if(droneControl == null)
            droneControl = GetComponentInChildren<DroneControl>();
        if (droneControl == null)
            Debug.LogError("DroneControl not found");


        if(droneControlActivityRecoverable == null)
            droneControlActivityRecoverable = GetComponent<Recoverable>();


        cameraOnTheDrone.enabled = false;
    }


    private void PrepareCamera()
    {
        if(isCameraReady) return;

        cameraOnTheDrone.targetTexture = new RenderTexture(cameraOnTheDrone.targetTexture);
        Renderer display = droneVideoScreen.GetComponent<Renderer>();
        display.material.mainTexture = cameraOnTheDrone.targetTexture;
        isCameraReady = true;
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            PrepareDroneCameraManagment();
        }
        else
        {
            droneVideoFeebackGlobalObject.SetActive(false);
        }
    }

    private void PrepareDroneCameraManagment()
    {
        droneVideoFeebackGlobalObject.SetActive(true);

        // we have to check the drone status in case of reconnection
        if(droneControl.DroneStatus == DroneControl.UAVStatus.Flying)
        {
            remoteControlIsActive = true;
            droneVideoCanvas.SetActive(false);
            droneVideoScreen.SetActive(true);
        }
        else
        {
            droneVideoCanvas.SetActive(true);
            droneVideoScreen.SetActive(false);
        }
        

        droneControl.onRemoteControlSwitchedOn.AddListener(OnRemoteControlSwitchedOn);
        droneControl.onRemoteControlSwitchedOff.AddListener(OnRemoteControlSwitchedOff);
    }

    private void OnRemoteControlSwitchedOn()
    {
        remoteControlIsActive = true;
        droneVideoCanvas.SetActive(false);
        droneVideoScreen.SetActive(true);
    }

    private void OnRemoteControlSwitchedOff()
    {
        remoteControlIsActive = false;
        droneVideoCanvas.SetActive(true);
        droneVideoScreen.SetActive(false);
    }

    public override void Render()
    {
        base.Render();

        if (Object.HasStateAuthority && remoteControlIsActive)
        {
            if (Time.time >= nextRenderTime)
            {
                nextRenderTime = Time.time + (1f / targetFrameRate);
                CaptureFrame();
            }
        }
    }

    void CaptureFrame()
    {
        PrepareCamera();
        cameraOnTheDrone.Render();
    }

    private void OnDestroy()
    {
        if (isCameraReady && cameraOnTheDrone && cameraOnTheDrone.targetTexture)
        {
            Destroy(cameraOnTheDrone.targetTexture);
        }

        if (droneControl)
        {
            droneControl.onRemoteControlSwitchedOn.RemoveListener(OnRemoteControlSwitchedOn);
            droneControl.onRemoteControlSwitchedOff.RemoveListener(OnRemoteControlSwitchedOff);
        }
    }


    public void OnRecovered(Recoverable recovered, IRecoverRequester requester)
    {

        // in case of reconnection we have to wait the OnRecovered call back to listen to droneControl event
        if(recovered == droneControlActivityRecoverable)
        {
            PrepareDroneCameraManagment();
            droneControl.droneControlIsAuthorized = true;
        }
    }
}
