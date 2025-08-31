using System.Collections.Generic;
using UnityEngine;
using Fusion.XR.Shared.Grabbing;

public class SequentialPlacement : MonoBehaviour
{
    [Header("Sequence Parts")]
    public List<NetworkGrabbable> parts;  // Sequence order me parts
    [Header("Table Trigger")]
    public Collider tableTrigger;         // Table ke trigger collider
    public NetworkGrabbable network;
        
    private int currentIndex = 0;         // Track current part
    public bool check;
    //public string Tag;
    void Start()
    {
        Debug.Log("SequentialPlacement Start: Initializing sequence.");
        // Enable first part grab, disable rest
        //for (int i = 0; i < parts.Count; i++)
        //{
        //   // parts[i].enabled = (i == currentIndex);
        //   // Debug.Log($"Part {i} enabled: {parts[i].enabled}");
        //    print("hahahahaha" + parts.Count);
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
       
        
        
           
            // Current part ka collider (child me bhi ho sakta hai)
            Collider partCollider = parts[currentIndex].GetComponentInChildren<Collider>();
            

            if (partCollider != null && partCollider.bounds.Intersects(other.bounds))
            {
                Debug.Log($"Part {currentIndex} placed on table. Disabling grab.");
                // Current part ko grab disable kar do
                parts[currentIndex].enabled = false;
            if (parts.Count == 2)
            {
                currentIndex++;
                if (currentIndex == 1)
                {
                    parts[currentIndex].enabled = true;
                }
                else if(currentIndex == 2) 
                {
                    network.enabled = true;
                }
            }

            else if(parts.Count == 1)
            {
                currentIndex++;
                if (currentIndex == 1)
                {
                    if (!check)
                    {
                        network.enabled = true;
                    }
                   
                }
            }
               
            }
         
    }
        
    
}