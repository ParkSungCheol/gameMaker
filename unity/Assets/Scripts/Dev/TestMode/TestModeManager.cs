using UnityEngine;
using System;

public class TestModeManager : MonoBehaviour
{
    public static TestModeManager Instance { get; private set; }

    [Header("Test Mode")]
    public bool IsTestMode = true;
    [Range(1f, 100f)]
    public float SpeedMultiplier = 10f; // 기본 10x, UI에서 10~30 권장

    public event Action<float> OnSpeedMultiplierChanged;
    public event Action<bool> OnTestModeToggled;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSpeedMultiplier(float m)
    {
        SpeedMultiplier = m;
        OnSpeedMultiplierChanged?.Invoke(m);
    }

    public void SetTestMode(bool on)
    {
        IsTestMode = on;
        OnTestModeToggled?.Invoke(on);
    }
}
