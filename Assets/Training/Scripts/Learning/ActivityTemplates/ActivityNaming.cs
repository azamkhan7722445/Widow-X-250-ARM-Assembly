using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fusion.Addons.Learning.ActivityTemplates
{
    /// <summary>
    /// This class handles "What is what" kind activities.
    /// It listens to the NamingFlags in order to get notified when a flag has a new status.
    /// Then it calculates a new value for the Progress.
    /// </summary>
    public class ActivityNaming : LearnerActivityTracker
    {
        [SerializeField] private int numberOfFlags;
        [SerializeField] private int flagsInGoodPosition = 0;

        [SerializeField] List<NamingFlag> flags = new List<NamingFlag>();

        protected virtual void Start()
        {
            if (flags == null || flags.Count == 0)
            {
                flags = GetComponentsInChildren<NamingFlag>().ToList();
            }
            numberOfFlags = flags.Count;
            if (numberOfFlags > 0)
            {
                Debug.Log($"{numberOfFlags} NamingFlag found ");
            }
            else
                Debug.LogError("No NamingFlag found ");

            foreach (var flag in flags)
            {
                flag.onFlagStatusChanged.AddListener(FlagStatusChanged);
            }
        }

        protected virtual void FlagStatusChanged()
        {
            if (Object.HasStateAuthority)
            {
                flagsInGoodPosition = 0;
                foreach (var flag in flags)
                {
                    if (flag.flagStatus == NamingFlag.FlagStatus.goodPosition)
                    {
                        flagsInGoodPosition++;
                    }
                }
                Progress = (float)flagsInGoodPosition / numberOfFlags;
            }
        }

        public override void CheckActivityProgress()
        {
            base.CheckActivityProgress();
            if (ActivityStatusOfLearner == Status.ReadyToStart && flagsInGoodPosition != 0 && Progress != 1)
            {
                ActivityStatusOfLearner = Status.Started;
            }
        }
    }

}

