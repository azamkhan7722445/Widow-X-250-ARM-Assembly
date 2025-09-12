using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.XR.Shared.Grabbing;
using UnityEngine.UI;

public class ObjectReset : NetworkBehaviour {
    [Header("Target Object")]
    public Transform target;   // jis object ko reset karna hai
    public string anees;
    private Vector3 startPos;
    private Quaternion startRot;
    public List<NetworkGrabbable> parts;
    public Button Next, Previous;
    [Networked] public int count { get; set; }
    public List<SequentialPlacement> Acctive_count = new List<SequentialPlacement>();
    List<string> names = new List<string> { "First_MAIN-SCREW", "Second_MAIN-SCREW", "GRAPPER", "First_S_SHAPE-SCREW", "Second_S_SHAPE-SCREW", "S_SHAPE", "SARWO_HORN_SCREW", "Sarvo_Horn_Defected", "Sarvo_Horn_Non_Defected" };

    //List<Transform> postion = new List<string>
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

        Invoke("enable_next_button", 2f);
   }



    public void enable_next_button() {
        Next.interactable = true;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Reset_previous()
    {
        if ( count>=0)
        {
            
            parts[count].enabled = false;
            count--;
            if (count == 7)
            {

            }
            else
            {
                parts[count].enabled = true;
            }
            

        }
        Invoke("enable_previous_button", 2f);
    }
    public void enable_previous_button() {
        Previous.interactable = true;
    }
}
