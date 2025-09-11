using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.XR.Shared.Grabbing;

public class ObjectReset : NetworkBehaviour {
    [Header("Target Object")]
    public Transform target;   // jis object ko reset karna hai
    public string anees;
    private Vector3 startPos;
    private Quaternion startRot;
    public List<NetworkGrabbable> parts;
    [Networked] public int count { get; set; }
    public List<SequentialPlacement> Acctive_count = new List<SequentialPlacement>();
    void Start() {
        if(target != null) {
            startPos = target.position;
            startRot = target.rotation;
        }
    }

    // ye function call karo jab reset karna ho
    public void ResetObject() {
        if(Object.HasStateAuthority) // sirf host/server trigger kare
        {
            RPC_ResetObject();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetObject() {
        if(target != null) {
            target.position = startPos;
            target.rotation = startRot;
        }

        Debug.Log("Object reset to its initial position & rotation.");
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
   public void RPC_Reset_Next()
   {
        if(parts.Count-1> count)
        {
            parts[count].enabled = false;
            count++;
            parts[count].enabled = true;

        }
   }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Reset_previous()
    {
        if ( count>=0)
        {
            parts[count].enabled = false;
            count--;
            parts[count].enabled = true;

        }
    }
}
