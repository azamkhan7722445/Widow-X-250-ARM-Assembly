using UnityEngine;
using Fusion;

public class ObjectReset : NetworkBehaviour {
    [Header("Target Object")]
    public Transform target;   // jis object ko reset karna hai
    public string anees;
    private Vector3 startPos;
    private Quaternion startRot;

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
}
