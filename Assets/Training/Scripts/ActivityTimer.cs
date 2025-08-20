using Fusion;
using Fusion.Addons.Learning;
using Fusion.Addons.Touch;
using TMPro;
using UnityEngine;

/// <summary>
/// This class handles the Timer activity.
/// The user configures a target time with a margin.
/// One button starts the timer and another stops it.
/// A text on the display informs the user whether the elapsed time corresponds to the set target time.
/// Also, the background color of the screen changes according to the elapsed time :
///     - it's green when the elapsed time corresponds to the target time (with the margin),
///     - then turns red when the target time is exceeded).
/// </summary>
public class ActivityTimer : LearnerActivityTracker
{
    [Header("Timer")]

    [Tooltip("To determine whether an end of activity below the margin is considered successful")]
    [SerializeField] bool marginApplyOnlyToTimerOverruns = false;

    [SerializeField] MeshRenderer displayMeshRenderer;

    [SerializeField] TextMeshProUGUI goalTimeTMP;
    [SerializeField] TextMeshProUGUI marginTimeTMP;
    [SerializeField] TextMeshProUGUI elapsedTimeTMP;
    [SerializeField] TextMeshProUGUI displayStatusTMP;
    [SerializeField] TextMeshProUGUI playPauseButtonTMP;

    [SerializeField] Touchable goalTimeIncrease;
    [SerializeField] Touchable goalTimeDecrease;
    [SerializeField] Touchable marginTimeIncrease;
    [SerializeField] Touchable marginDecrease;
    [SerializeField] Touchable elapsedTimeStart;
    [SerializeField] Touchable elapsedTimeStop;
    [SerializeField] Touchable elapsedTimeReset;

    [SerializeField] float defaultGoalTime = 60;
    [SerializeField] float goalTimeStep = 10;

    [SerializeField] float defaultMarginTime = 10;
    [SerializeField] float marginTimeStep = 1;

    [Networked, OnChangedRender(nameof(OnGoalTimeChange))]
    float GoalTime { get; set; }

    [Networked, OnChangedRender(nameof(OnMarginTimeChange))]
    float MarginTime { get; set; }

    [Networked, OnChangedRender(nameof(OnElapsedTimeChange))]
    [UnitySerializeField]
    float ElapsedTime { get; set; }

    const string Disabled = "Activity Disabled";
    const string ReadyToStart = "Ready to start";
    const string Started = "Let's go !";
    const string Paused = "Paused";
    const string Succeeded = "Well done !";
    const string FailedTooEarly = "Too early... Try again !";
    const string FailedTooLate = "Too Late... Try again !";
    const string PendingSuccessValidation = "Valid time range";
    const string TimeExceeded = "Time exceeded";

    float _elapsedTime = 0f;
    float previousTimeSync = 0f;
    Color defaultDisplayColor;
    Color displayColor;

    protected override void Awake()
    {
        base.Awake();
        if (goalTimeIncrease) goalTimeIncrease.onTouch.AddListener(OnGoalTimeIncrease);
        if (goalTimeDecrease) goalTimeDecrease.onTouch.AddListener(OnGoalTimeDecrease);
        if (marginTimeIncrease) marginTimeIncrease.onTouch.AddListener(OnMarginTimeIncrease);
        if (marginDecrease) marginDecrease.onTouch.AddListener(OnMarginDecrease);
        if (elapsedTimeStart) elapsedTimeStart.onTouch.AddListener(OnElapsedTimeStart);
        if (elapsedTimeStop) elapsedTimeStop.onTouch.AddListener(OnElapsedTimeStop);
        if (elapsedTimeReset) elapsedTimeReset.onTouch.AddListener(OnElapsedTimeReset);
        if (displayMeshRenderer)
        {
            defaultDisplayColor = displayMeshRenderer.material.color;
            displayColor = displayMeshRenderer.material.color;
        }

        if (playPauseButtonTMP)
        {
            playPauseButtonTMP.text = "Play";
        }

    }
    public override void Spawned()
    {
        base.Spawned();

        if (Object && Object.HasStateAuthority)
        {
            ActivityStatusOfLearner = Status.ReadyToStart;
            GoalTime = defaultGoalTime;
            MarginTime = defaultMarginTime;
        }

        RefreshTimerScreen(elapsedTimeTMP, ElapsedTime);
        RefreshTimerScreen(goalTimeTMP, GoalTime);
        RefreshTimerScreen(marginTimeTMP, MarginTime);
        RefreshPlayPauseButton();
    }
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (ActivityStatusOfLearner == Status.Started || ActivityStatusOfLearner == Status.PendingSuccessValidation || ActivityStatusOfLearner == Status.CustomStatus1)
        {
            UpdateLocalTimer();
            SyncElapsedTime();
        }
    }
    private void UpdateProgress()
    {
        if (Object && Object.HasStateAuthority)
        {
            if (ElapsedTime < GoalTime)
            {
                Progress = ElapsedTime / GoalTime;
            }
            else
            {
                var extraTime = ElapsedTime - GoalTime;
                if (extraTime < GoalTime)
                    Progress = 2 - (ElapsedTime / GoalTime);
                else
                    Progress = 0;
            }
        }
    }
    private void ResetProgress()
    {
        if (Object && Object.HasStateAuthority)
        {
            Progress = 0;
        }
    }

    #region OnChangedRender callbacks
    public override void OnActivityStatusOfLearnerChange(NetworkBehaviourBuffer previousBuffer)
    {
        base.OnActivityStatusOfLearnerChange(previousBuffer);

        // Activity has been reset
        if (ActivityStatusOfLearner == Status.ReadyToStart)
        {
            ResetProgress();
            ResetLocalTimer();
            RestoreDisplayColor();
        }

        // Activity has been stopped
        if (ActivityStatusOfLearner == Status.Stopped)
        {
            if (Object && Object.HasStateAuthority)
            {
                CheckActivityProgress();
            }
        }

        // Activity has been disabled
        if (ActivityStatusOfLearner == Status.Disabled)
        {
            if (Object && Object.HasStateAuthority)
            {
                OnElapsedTimeStop();
            }
        }

        // Activity failed
        if (ActivityStatusOfLearner == Status.Failed)
        {
            ShowFailedDisplayColor();
        }
        else
        {
            UpdateDisplayColor();
        }

        UpdateDisplayText();
        RefreshPlayPauseButton();
    }

    // CheckActivityProgress() is called when the Progress is updated
    // Status.CustomStatus1 is used when the timer exceed the time limit but the player has not yet push the stop button (for UI purpose)
    public override void CheckActivityProgress()
    {

        if (marginApplyOnlyToTimerOverruns)
        {
            if ((ElapsedTime - GoalTime) <= MarginTime)
            {
                if (ActivityStatusOfLearner == Status.Stopped) ActivityStatusOfLearner = Status.Succeeded;
                if (ActivityStatusOfLearner == Status.Started) ActivityStatusOfLearner = Status.PendingSuccessValidation;

            }
            else
            {
                if (ActivityStatusOfLearner == Status.Stopped) ActivityStatusOfLearner = Status.Failed;
                if (ActivityStatusOfLearner == Status.PendingSuccessValidation ) ActivityStatusOfLearner = Status.CustomStatus1;
            }
        }
        else
        {
            if (Mathf.Abs(ElapsedTime - GoalTime) <= MarginTime)
            {
                if (ActivityStatusOfLearner == Status.Stopped) ActivityStatusOfLearner = Status.Succeeded;
                if (ActivityStatusOfLearner == Status.Started) ActivityStatusOfLearner = Status.PendingSuccessValidation;
            }
            else
            {
                if (ActivityStatusOfLearner == Status.Stopped) ActivityStatusOfLearner = Status.Failed;
                if (ActivityStatusOfLearner == Status.PendingSuccessValidation) ActivityStatusOfLearner = Status.CustomStatus1;
            }
        }


    }
    private void OnGoalTimeChange()
    {
        RefreshTimerScreen(goalTimeTMP, GoalTime);
    }
    private void OnMarginTimeChange()
    {
        RefreshTimerScreen(marginTimeTMP, MarginTime);
    }
    private void OnElapsedTimeChange()
    {
        RefreshTimerScreen(elapsedTimeTMP, ElapsedTime);
        UpdateProgress();
        UpdateDisplayColor();
    }
    #endregion

    #region Timer
    private void UpdateLocalTimer()
    {
        _elapsedTime += Runner.DeltaTime;
    }
    private void ResetLocalTimer()
    {
        _elapsedTime = 0;
        previousTimeSync = 0;
        RefreshTimerScreen(elapsedTimeTMP, _elapsedTime);
    }
    private void SyncElapsedTime()
    {
        // Sync the networked ElapsedTime only 1 time per second
        if (Mathf.FloorToInt(_elapsedTime) > Mathf.FloorToInt(previousTimeSync))
        {
            ElapsedTime = _elapsedTime;
            previousTimeSync = _elapsedTime;
        }
    }
    private void RefreshTimerScreen(TextMeshProUGUI textMeshPro, float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        textMeshPro.text = $"{minutes:D2}:{seconds:D2}";
    }
    #endregion

    #region Buttons interactions
    private void OnElapsedTimeReset()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            if (ActivityStatusOfLearner != Status.ReadyToStart)
            {
                ActivityStatusOfLearner = Status.ReadyToStart;
            }
        }
    }
    private void OnElapsedTimeStop()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            if (ActivityStatusOfLearner == Status.Started || ActivityStatusOfLearner == Status.Paused || ActivityStatusOfLearner == Status.PendingSuccessValidation || ActivityStatusOfLearner == Status.CustomStatus1)
            {
                ActivityStatusOfLearner = Status.Stopped;
            }
        }
    }
    private void OnElapsedTimeStart()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            if (ActivityStatusOfLearner == Status.Started || ActivityStatusOfLearner == Status.PendingSuccessValidation || ActivityStatusOfLearner == Status.CustomStatus1)
            {
                ActivityStatusOfLearner = Status.Paused;
            }
            else
            {
                ActivityStatusOfLearner = Status.Started;
            }
        }
    }
    private void OnMarginDecrease()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            if (MarginTime - marginTimeStep < 0)
                MarginTime = 0;
            else
                MarginTime -= marginTimeStep;
        }
    }
    private void OnMarginTimeIncrease()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            MarginTime += marginTimeStep;
        }
    }
    private void OnGoalTimeDecrease()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            if (GoalTime - goalTimeStep < 0)
                GoalTime = 0;
            else
                GoalTime -= goalTimeStep;
        }
    }
    private void OnGoalTimeIncrease()
    {
        if (ActivityStatusOfLearner == Status.Disabled) return;

        if (Object && Object.HasStateAuthority)
        {
            GoalTime += goalTimeStep;
        }
    }
    #endregion

    #region UI and Display
    private void RefreshPlayPauseButton()
    {
        if (ActivityStatusOfLearner == Status.Started || ActivityStatusOfLearner == Status.PendingSuccessValidation || ActivityStatusOfLearner == Status.CustomStatus1)
        {
            playPauseButtonTMP.text = "Pause";
        }
        else
        {
            playPauseButtonTMP.text = "Play";
        }
    }
    void UpdateDisplayText()
    {
        switch (ActivityStatusOfLearner)
        {
            case Status.ReadyToStart:
                displayStatusTMP.text = ReadyToStart;
                break;

            case Status.Started:
                displayStatusTMP.text = Started;
                break;

            case Status.Paused:
                displayStatusTMP.text = Paused;
                break;

            case Status.PendingSuccessValidation:
                displayStatusTMP.text = PendingSuccessValidation;
                break;

            case Status.Succeeded:
                    displayStatusTMP.text = Succeeded;
                break;

            case Status.Failed:
                if (ElapsedTime < GoalTime)
                {
                    displayStatusTMP.text = FailedTooEarly;
                }
                else
                {
                    displayStatusTMP.text = FailedTooLate;
                }
                break;

            case Status.Disabled:
                displayStatusTMP.text = Disabled;
                break;

            case Status.CustomStatus1:
                displayStatusTMP.text = TimeExceeded;
                break;

        }
    }
    private void UpdateDisplayColor()
    {
        displayColor = defaultDisplayColor;
        if (Progress > displayColor.g)
        {
            if (marginApplyOnlyToTimerOverruns)
            {
                if ((ElapsedTime - GoalTime) <= MarginTime)
                {
                    displayColor.g = Progress;
                }
            }
            else
            {
                if (Mathf.Abs(ElapsedTime - GoalTime) <= MarginTime)
                {
                    displayColor.g = Progress;
                }
            }
        }
        displayMeshRenderer.material.color = displayColor;
    }
    private void RestoreDisplayColor()
    {
        displayColor = defaultDisplayColor;
        displayMeshRenderer.material.color = displayColor;
    }
    private void ShowFailedDisplayColor()
    {
        displayColor = defaultDisplayColor;
        displayColor.r = 1;
        displayMeshRenderer.material.color = displayColor;
    }
    #endregion

}


