using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.XR.Shared.Grabbing;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.StructureCohesion;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class ObjectReset : NetworkBehaviour {
    [Header("Target Object")]
    public Transform target;   // jis object ko reset karna hai
    public string anees;
    private Vector3 startPos;
    private Quaternion startRot;
    public List<NetworkGrabbable> parts;
    public Button Nextbtn, Previousbtn;
    [Networked]public int count { get; set; }
    public List<SequentialPlacement> Acctive_count = new List<SequentialPlacement>();
    public List<string> names = new List<string> { "First_MAIN-SCREW", "Second_MAIN-SCREW", "GRAPPER", "First_S_SHAPE-SCREW", "Second_S_SHAPE-SCREW", "S_SHAPE", "SARWO_HORN_SCREW", "Sarvo_Horn_Defected", "Sarvo_Horn_Non_Defected" };

    public List<Transform> postion = new List<Transform>();
    public List<Attactch_status> magnet = new List<Attactch_status>();
    public MagnetStructureAttachmentPoint extra_mag;
    public TextMeshProUGUI nxt, previous, Active,Alert;

    public NetworkGrabbable defectet, undefected;

    [Networked] public bool assembling { get; set; }
    [Networked] public bool deassembling { get; set; }
    void Start() {
        if (target != null) {
            startPos = target.position;
            startRot = target.rotation;
        }

        nxt.text = "Second_MAIN-SCREW";
        Active.text = "Grab First_MAIN-SCREW";
        previous.text = "";
    }

    // ye function call karo jab reset karna ho
    public void ResetObject() {
       // if (Object.HasStateAuthority) // sirf host/server trigger kare
        {
            RPC_ResetObject();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetObject() {
        if (target != null) {
            target.position = startPos;
            target.rotation = startRot;
        }

        Debug.Log("Object reset to its initial position & rotation.");
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Reset_Next()
    {
        if (!magnet[count].Attach)
        {
            if (parts.Count - 1 > count)
            {
                if (!Previousbtn.gameObject.activeSelf)
                {

                    Previousbtn.gameObject.SetActive(true);


                }
                parts[count].enabled = false;

                //ResetObject1();
                if (Object.HasStateAuthority) // sirf host/server trigger kare
                {
                    count++;
                    RPC_sett_count(count);
                }
               // count++;
               
            }
        }
        else
        {
            Alert.gameObject.SetActive(true);
            Invoke("off_alert", 2f);
        }

        Invoke("enable_next_button", 2f);
    }



    public void enable_next_button() {
        Nextbtn.interactable = true;
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Reset_previous()
    {

        if (magnet[count].Attach)
        {
            if (!Nextbtn.gameObject.activeSelf)
            {

                Nextbtn.gameObject.SetActive(true);


            }

            if (count >= 0)
            {


                parts[count].enabled = false;


                if (Object.HasStateAuthority) // sirf host/server trigger kare
                {
                    count--;
                    RPC_sett_count1(count);
                }

                

            }
        }
        else
        {
            Alert.gameObject.SetActive(true);
            Invoke("off_alert", 2f);
        }
        Invoke("enable_previous_button", 2f);
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_sett_count1(int count1)
    {

        count = count1;
        if (count == 7)
        {
            parts[count].enabled = false;
            if (Object.HasStateAuthority) // sirf host/server trigger kare
            {
                count--;
                RPC_sett_count2(count);
               
            }
            return;
        }
        else
        {
            parts[count].enabled = true;
        }
        if (count == 0)
        {
            previous.text = names[count];
            Previousbtn.gameObject.SetActive(false);
        }
        else
        {
            previous.text = names[count - 1];
        }

        Active.text = "Grab" + names[count];
        nxt.text = names[count + 1];

    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_sett_count2(int count1)
    {

        count = count1;
        parts[count].enabled = true;

        if (count == 0)
        {
            previous.text = names[count];
            Previousbtn.gameObject.SetActive(false);
        }
        else
        {
            previous.text = names[count - 1];
        }

        Active.text = "Grab" + names[count];
        nxt.text = names[count + 1];

    }

        [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_sett_count(int count1)
    {
        count = count1;
        parts[count].enabled = true;

        if (assembling && count == 7)
        {
            parts[count].enabled = false;
            parts[count - 1].enabled = true;
        }
        if (count < 7)
        {
            nxt.text = names[count + 1];
        }

        else
        {
            nxt.text = "";
        }
        if (count == 8)
        {
            if (!assembling)
            {
                assembling = true;
            }

            Nextbtn.gameObject.SetActive(false);
        }

        Active.text = "Grab" + names[count];
        previous.text = names[count - 1];



    }

        [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Reset()
    {
        foreach (var item in magnet)
        {
            if (item != null && item.gameObject != null)
            {
                item.ResetObject();
            }
        }

        nxt.text = "Second_MAIN-SCREW";
        Active.text = "Grab First_MAIN-SCREW";
        previous.text = "";
        count = 0;
        Previousbtn.gameObject.SetActive(false);
        for(int i = 0; i <= 8; i++)
        {

            if (i == 0)
            {
                parts[0].enabled = true;
            }
            else {
                parts[i].enabled = false;

            }
        }


        //if (Runner.IsServer) // Host only
        //{
        //    Get current scene name
        //    string sceneName = SceneManager.GetActiveScene().name;

        //    Reload the scene(all clients will follow automatically)
        //    Runner.SetActiveScene(sceneName);
        //}
    }

    public void off_alert()
    {
      Alert.gameObject.SetActive(false);
    }
    public void enable_previous_button() {
        Previousbtn.interactable = true;
    }

    public void ResetObject1()
    {
        if (Object.HasStateAuthority) // sirf host/server trigger kare
        {
            RPC_ResetObject1();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetObject1()
    {
        parts[count].gameObject.transform.position = postion[count].position;
        parts[count].gameObject.transform.rotation = quaternion.Euler(Vector3.zero);
    }

}
