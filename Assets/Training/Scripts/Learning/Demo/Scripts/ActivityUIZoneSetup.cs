using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.Reconnection;

public class ActivityUIZoneSetup : NetworkBehaviour, IRecoverableListener
{
    public string parentZone = "LearnerActivitiesZone";
    public GameObject stateAuthorityFeedback;

    public override void Spawned()
    {
        base.Spawned();
        var landingZone = GameObject.Find(parentZone);
        if (landingZone)
        {
            transform.parent = landingZone.transform;
            if (landingZone.TryGetComponent<RectTransform>(out var rectTransform))
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }
        UpdateToStateAuthority();
    }
    public void OnRecovered(Recoverable recovered, IRecoverRequester requester)
    {
        UpdateToStateAuthority();
    }

    public void UpdateToStateAuthority()
    {
        var landingZone = GameObject.Find(parentZone);
        foreach (var g in GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {
            g.raycastTarget = Object.HasStateAuthority;
        }

        if (stateAuthorityFeedback)
        {
            stateAuthorityFeedback.SetActive(Object.HasStateAuthority);
        }
    }
}
