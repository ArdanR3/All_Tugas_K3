using UnityEngine;

public class GazeInfoTarget : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;

    public GameObject InfoPanel
    {
        get => infoPanel;
        set => infoPanel = value;
    }

    public void ShowInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }
    }

    public void HideInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
}

