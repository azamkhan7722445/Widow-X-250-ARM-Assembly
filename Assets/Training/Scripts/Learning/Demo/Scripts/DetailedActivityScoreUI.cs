using Fusion.Addons.Learning;
using Fusion.Addons.Learning.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetailedActivityScoreUI : ActivityScoreUI
{
    public TMPro.TMP_Text activityStatus;

    public override void UpdateUIForAnActivity(LearningManager learningManager, LearnerActivityTracker info)
    {
        base.UpdateUIForAnActivity(learningManager, info);
        activityStatus.text = info.ActivityStatusOfLearner.ToString();
    }
}
