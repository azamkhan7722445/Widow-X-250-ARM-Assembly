using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.XR.Shared.Grabbing;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.StructureCohesion;
public class ObjectReset : NetworkBehaviour {
    [Header("Target Object")]
    public Transform target;   // jis object ko reset karna hai
    public string anees;
    private Vector3 startPos;
    private Quaternion startRot;
    public List<NetworkGrabbable> parts;
    public Button Nextbtn, Previousbtn;
    [Networked] public int count { get; set; }
    public List<SequentialPlacement> Acctive_count = new List<SequentialPlacement>();
    public List<string> names = new List<string> { "First_MAIN-SCREW", "Second_MAIN-SCREW", "GRAPPER", "First_S_SHAPE-SCREW", "Second_S_SHAPE-SCREW", "S_SHAPE", "SARWO_HORN_SCREW", "Sarvo_Horn_Defected", "Sarvo_Horn_Non_Defected" };

    public List<Transform> postion = new List<Transform>();
    public List<MagnetStructureAttachmentPoint> magnet = new List<MagnetStructureAttachmentPoint>();
    public MagnetStructureAttachmentPoint extra_mag;
    public TextMeshProUGUI nxt, previous, Active,Alert;
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
        if (Object.HasStateAuthority) // sirf host/server trigger kare
        {
            RPC_ResetObject();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetObject() {
        if (target != null) {
            target.position = startPos;
            target.rotation = startRot;
        }

        Debug.Log("Object reset to its initial position & rotation.");
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Reset_Next()
    {
        //if (magnet[count].check_attched || magnet[2].check_attched)
        {
            if (parts.Count - 1 > count)
            {
                if (!Previousbtn.gameObject.activeSelf)
                {

                    Previousbtn.gameObject.SetActive(true);


                }

                parts[count].gameObject.transform.position = postion[count].position;
                parts[count].gameObject.transform.rotation = postion[count].rotation;
                parts[count].enabled = false;
                count++;
                parts[count].enabled = true;
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
                    Nextbtn.gameObject.SetActive(false);
                }

                Active.text = "Grab" + names[count];
                previous.text = names[count - 1];
            }
        }
        /*else
        {
            Alert.gameObject.SetActive(true);
            Invoke("off_alert", 2f);
        }*/

        Invoke("enable_next_button", 2f);
    }



    public void enable_next_button() {
        Nextbtn.interactable = true;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Reset_previous()
    {

        //if (!magnet[count].check_attched || !magnet[2].check_attched)
        {
            if (!Nextbtn.gameObject.activeSelf)
            {

                Nextbtn.gameObject.SetActive(true);


            }

            if (count >= 0)
            {


                parts[count].enabled = false;
                count--;

                if (count == 7)
                {
                    count--;
                    parts[count].enabled = true;
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
        }
       /* else
        {
            Alert.gameObject.SetActive(true);
            Invoke("off_alert", 2f);
        }*/
        Invoke("enable_previous_button", 2f);
    }


    public void off_alert()
    {
      Alert.gameObject.SetActive(false);
    }
    public void enable_previous_button() {
        Previousbtn.interactable = true;
    }
}
