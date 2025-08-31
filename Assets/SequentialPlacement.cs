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
        // Enable first part grab, disable rest
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].enabled = (i == currentIndex);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if trigger is table trigger
        if (other == tableTrigger)
        {
            // Current part ka collider (child me bhi ho sakta hai)
            Collider partCollider = parts[currentIndex].GetComponentInChildren<Collider>();

            if (partCollider != null && partCollider.bounds.Intersects(other.bounds))
            {
                // Current part ko grab disable kar do
                parts[currentIndex].enabled = false;

                // Update sequence index
                if (!reverse)
                {
                    currentIndex++;
                    if (currentIndex >= parts.Count)
                    {
                        reverse = true;
                        currentIndex = parts.Count - 1; // last part se reverse
                    }
                }
                else
                {
                    currentIndex--;
                    if (currentIndex < 0)
                    {
                        reverse = false;
                        currentIndex = 0;
                    }
                }

                // Next part grab enable karo
                if (currentIndex >= 0 && currentIndex < parts.Count)
                {
                    parts[currentIndex].enabled = true;
                }
            }
        }
    }
}
