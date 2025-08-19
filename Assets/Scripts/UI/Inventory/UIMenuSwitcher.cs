using UnityEngine;

public class UIMenuSwitcher : MonoBehaviour
{
    [Header("Menus")]
    public GameObject inventoryMenu;
    public GameObject craftingMenu;

    // Called by buttons
    public void ShowInventory()
    {
        inventoryMenu.SetActive(true);
        craftingMenu.SetActive(false);
    }

    public void ShowCrafting()
    {
        inventoryMenu.SetActive(false);
        craftingMenu.SetActive(true);
    }
}
