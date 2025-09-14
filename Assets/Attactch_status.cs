using Fusion;
using UnityEngine;

public class Attactch_status : NetworkBehaviour
{
    [Networked] public bool Attach { get; set; }

    [Networked] public bool Attach2 { get; set; }

    public Transform target;
    private Vector3 startPos;
    private Quaternion startRot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = transform;
        if (target != null)
        {
            startPos = target.position;
            startRot = target.rotation;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_On_Attache()
    {
      Attach = true;
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_off_Attache()
    {
        Attach = false;
    }



    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC__Attache()
    {
        if (!Attach2)
        {
            Attach = false;
            Attach2 = true;
        }
       
    }



    public void ResetObject()
    {
       // if (Object.HasStateAuthority) // sirf host/server trigger kare
        {
            RPC_ResetObject();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetObject()
    {
        if (target != null)
        {
            target.position = startPos;
            target.rotation = startRot;
        }

        Debug.Log("Object reset to its initial position & rotation.");
    }
}
