using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TestModeUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;           // 전체 패널(활성/비활성)
    public RectTransform listContent;      // ScrollRect content
    public GameObject listItemPrefab;      // 단일 항목 프리팹 (버튼 포함)
    public Button walkButton;
    public Button attackButton;
    public Button dieButton;
    public Toggle applyToAllToggle;
    public Slider speedSlider;             // 범위: 1~100 (UI에서 10~30 권장)
    public Text speedValueText;

    List<ITestableUnit> units = new List<ITestableUnit>();
    ITestableUnit selected;

    void Start()
    {
        if (TestModeManager.Instance == null)
        {
            var go = new GameObject("TestModeManager");
            go.AddComponent<TestModeManager>();
        }

        RefreshUnitList();
        speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        walkButton.onClick.AddListener(() => DoActionOnSelection(u => u.PlayWalk()));
        attackButton.onClick.AddListener(() => DoActionOnSelection(u => u.PlayAttack()));
        dieButton.onClick.AddListener(() => DoActionOnSelection(u => u.PlayDie()));

        // 초기 값
        speedSlider.value = TestModeManager.Instance.SpeedMultiplier;
        UpdateSpeedText(speedSlider.value);
        panelRoot.SetActive(TestModeManager.Instance.IsTestMode);
    }

    void Update()
    {
        // 단축키로 토글(옵션)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        bool on = !panelRoot.activeSelf;
        panelRoot.SetActive(on);
        if (TestModeManager.Instance != null) TestModeManager.Instance.SetTestMode(on);
    }

    void RefreshUnitList()
    {
        // 비우기
        foreach (Transform c in listContent) Destroy(c.gameObject);
        units.Clear();

        // 씬의 ITestableUnit 찾기 (예: TestUnitController를 붙여둔 유닛)
        var found = FindObjectsOfType<MonoBehaviour>().OfType<ITestableUnit>();
        foreach (var u in found)
        {
            units.Add(u);
            var itemGO = Instantiate(listItemPrefab, listContent);
            var label = itemGO.GetComponentInChildren<Text>();
            label.text = u.GetUnitName();

            var btn = itemGO.GetComponentInChildren<Button>();
            var captured = u;
            btn.onClick.AddListener(() => SelectUnit(captured));
        }
    }

    void SelectUnit(ITestableUnit u)
    {
        selected = u;
        // 포커싱 등 추가 가능(예: 카메라 이동)
        Debug.Log("Selected for test: " + u.GetUnitName());
    }

    void DoActionOnSelection(System.Action<ITestableUnit> act)
    {
        if (applyToAllToggle.isOn)
        {
            foreach (var u in units) act(u);
        }
        else
        {
            if (selected != null) act(selected);
        }
    }

    void OnSpeedSliderChanged(float v)
    {
        if (TestModeManager.Instance != null)
            TestModeManager.Instance.SetSpeedMultiplier(v);
        UpdateSpeedText(v);

        // 각 유닛에도 알림(선택적)
        foreach (var u in units) u.SetTestSpeedMultiplier(v);
    }

    void UpdateSpeedText(float v)
    {
        if (speedValueText != null)
            speedValueText.text = $"{v:0.##}x";
    }
}
