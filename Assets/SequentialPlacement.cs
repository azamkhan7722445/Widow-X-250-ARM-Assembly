using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;

public class SequentialPlacement : NetworkBehaviour {
    [Header("Sequence Parts")]
    public List<NetworkGrabbable> parts;      // Sequence order me parts
    [Header("Target Placements")]
    public List<Transform> targetPoints;      // Jahan parts snap honge (same order)
    [Header("Table Trigger")]
    public Collider tableTrigger;             // Table ke trigger collider
    public NetworkGrabbable network;

    public int currentIndex = 0;             // Track current part
    public bool check1,check2,check;
    public string Tag,Tag1;
   
    private void OnTriggerEnter(Collider other) {
       // if(!Object.HasStateAuthority) return; // sirf server/host trigger handle kare

       // Collider partCollider = parts[currentIndex].GetComponentInChildren<Collider>();

        if(other.tag==Tag) {
            Debug.Log(other.tag+" heloo");
            RPC_HandlePlacement();
            transform.GetComponent<BoxCollider>().enabled = false;
        }
        else if (other.tag == Tag1 )
        {
            Debug.Log(other.tag + " heloo");
            RPC_HandlePlacement();
            transform.GetComponent<BoxCollider>().enabled = false;
        }
    }

    //  ye sab clients par chalega
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandlePlacement() {
        // part disable
        //transform.GetComponent<BoxCollider>().enabled = false;
       // parts[currentIndex].enabled = false;

        // snap to target position (agar available hai)
       // if(currentIndex < targetPoints.Count) {
           // Transform partTransform = parts[currentIndex].transform;
         //   Transform target = targetPoints[currentIndex];

       // parts[currentIndex].transform.position = targetPoints[currentIndex].transform.position;
      //  parts[currentIndex].transform.rotation = targetPoints[currentIndex].transform.rotation;
       // }

        // sequence logic
        if(parts.Count == 2) {
            currentIndex++;
            if(currentIndex == 1) {
                //check1 = true;
                parts[currentIndex].enabled = true;
            }
            else if(currentIndex == 2) {
                network.enabled = true;
               // currentIndex = 0;
            }
        }
        else if(parts.Count == 1) {
            currentIndex++;
            if(currentIndex == 1 && !check) {
                network.enabled = true;
               // currentIndex = 0;
            }
        }
        Invoke("enable_Disable", 2f);
    }



    public void enable_Disable()
    {
        transform.GetComponent<BoxCollider>().enabled = true;
    }
}
