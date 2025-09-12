using Fusion;
using UnityEngine;

public class Attactch_status : NetworkBehaviour
{
    [Networked] public bool Attach { get; set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_On_Attache()
    {
      Attach = true;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_off_Attache()
    {
        Attach = false;
    }
}
