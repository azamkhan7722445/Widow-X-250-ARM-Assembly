using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System;

[RequireComponent(typeof(Slider))]
public class SynchedSlider : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnProgressChange))]
    public float Progress { get; set; } = 0;

    void OnProgressChange()
    {
        if (slider.value != Progress)
        {
            slider.value = Progress;
        }
    }

    Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public override void Spawned()
    {
        base.Spawned();
        slider.value = Progress;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        Progress = slider.value;

    }
}
