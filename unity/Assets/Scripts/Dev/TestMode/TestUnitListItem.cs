using UnityEngine;
using UnityEngine.UI;

public class TestUnitListItem : MonoBehaviour
{
    public Text nameText;
    public Button selectButton;
    public Image background;

    public void Setup(string name, System.Action onClick)
    {
        if (nameText != null) nameText.text = name;
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClick());
        }
    }

    public void SetSelected(bool sel)
    {
        if (background != null) background.color = sel ? Color.cyan : Color.white;
    }
}
