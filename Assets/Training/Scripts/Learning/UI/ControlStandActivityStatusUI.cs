using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fusion.Addons.Learning.UI
{
    /// <summary>
    /// This class is used to provide an access to the UI elements of the activity prefab displayed on the console.
    /// It used in the ControlStandActivityListDisplay class to update the activities list/status.
    /// </summary>
    public class ControlStandActivityStatusUI : MonoBehaviour
    {
        public TextMeshProUGUI activityName;
        public TextMeshProUGUI activityStatus;
        public Image statusImage;
        public Button button;
        public TextMeshProUGUI buttonText;

        [ContextMenu("Simulate Button Click")]
        private void SimulateButtonClick()
        {
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }
    }
}
