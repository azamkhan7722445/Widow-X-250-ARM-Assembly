using System.Collections.Generic;
using UnityEngine;
using Fusion.XR.Shared.Grabbing;

public class SequentialPlacement : MonoBehaviour
{
    [Header("Sequence Parts")]
    public List<NetworkGrabbable> parts;  // Sequence order me parts
    [Header("Table Trigger")]
    public Collider tableTrigger;         // Table ke trigger collider

    private int currentIndex = 0;         // Track current part
    private bool reverse = false;         // Sequence reverse flag

    void Start()
    {
        Debug.Log("SequentialPlacement Start: Initializing sequence.");
        // Enable first part grab, disable rest
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].enabled = (i == currentIndex);
            Debug.Log($"Part {i} enabled: {parts[i].enabled}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter called with collider: {other.name}");
        // Check if trigger is table trigger
        //if (other == tableTrigger)
        { 
            Debug.Log("Table trigger detected.");
            // Current part ka collider (child me bhi ho sakta hai)
            Collider partCollider = parts[currentIndex].GetComponentInChildren<Collider>();
            Debug.Log($"Current part index: {currentIndex}, Collider found: {partCollider != null}");

            if (partCollider != null && partCollider.bounds.Intersects(other.bounds))
            {
                Debug.Log($"Part {currentIndex} placed on table. Disabling grab.");
                // Current part ko grab disable kar do
                parts[currentIndex].enabled = false;

                // Update sequence index
                if (!reverse)
                {
                    currentIndex++;
                    Debug.Log($"Sequence forward. New index: {currentIndex}");
                    if (currentIndex >= parts.Count)
                    {
                        reverse = true;
                        currentIndex = parts.Count - 1; // last part se reverse
                        Debug.Log("Reached end of sequence. Reversing.");
                    }
                }
                else
                {
                    currentIndex--;
                    Debug.Log($"Sequence reverse. New index: {currentIndex}");
                    if (currentIndex < 0)
                    {
                        reverse = false;
                        currentIndex = 0;
                        Debug.Log("Reached start of sequence. Forward again.");
                    }
                }

                // Next part grab enable karo
                if (currentIndex >= 0 && currentIndex < parts.Count)
                {
                    parts[currentIndex].enabled = true;
                    Debug.Log($"Part {currentIndex} grab enabled.");
                }
            }
         else
            {
                Debug.LogWarning("Part collider not found or not intersecting table trigger.");
            }
        }
        //else
        //{
        //    Debug.Log("Trigger entered is not the table trigger.");
        //}
    }
}