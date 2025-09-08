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

    private int currentIndex = 0;             // Track current part
    public bool check;

    private void OnTriggerEnter(Collider other) {
        if(!Object.HasStateAuthority) return; // sirf server/host trigger handle kare

        Collider partCollider = parts[currentIndex].GetComponentInChildren<Collider>();

        if(partCollider != null && partCollider.bounds.Intersects(other.bounds)) {
            Debug.Log($"[Server] Part {currentIndex} placed on table.");
            RPC_HandlePlacement();
        }
    }

    //  ye sab clients par chalega
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandlePlacement() {
        // part disable
        parts[currentIndex].enabled = false;

        // snap to target position (agar available hai)
        if(currentIndex < targetPoints.Count) {
            Transform partTransform = parts[currentIndex].transform;
            Transform target = targetPoints[currentIndex];

            partTransform.position = target.position;
            partTransform.rotation = target.rotation;
        }

        // sequence logic
        if(parts.Count == 2) {
            currentIndex++;
            if(currentIndex == 1) {
                parts[currentIndex].enabled = true;
            }
            else if(currentIndex == 2) {
                network.enabled = true;
            }
        }
        else if(parts.Count == 1) {
            currentIndex++;
            if(currentIndex == 1 && !check) {
                network.enabled = true;
            }
        }
    }
}
