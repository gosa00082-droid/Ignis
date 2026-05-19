using UnityEngine;
using UnityEngine.UI;

public class QuestShopTabController : MonoBehaviour
{
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private GameObject questCanvas;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button questButton;

    private void Start()
    {
        shopButton.onClick.AddListener(tabShop);
        questButton.onClick.AddListener(tabQuest);
    }

    private void tabShop()
    {
        shopCanvas.SetActive(true);
        questCanvas.SetActive(false);
    }

    private void tabQuest()
    {
        shopCanvas.SetActive(false);
        questCanvas.SetActive(true);
    }
}
