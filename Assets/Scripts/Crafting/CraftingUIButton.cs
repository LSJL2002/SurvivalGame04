using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUIButton : MonoBehaviour
{
    public CraftingRecipe recipe;
    public CraftingManager craftingManager;

    [Header("UI References")]
    public Button button;

    [System.Serializable]
    public struct InputSlot
    {
        public Image icon;
        public TextMeshProUGUI amountText;
    }

    public InputSlot[] inputSlots; // assign 3 in the inspector
    public Image outputImage;
    public TextMeshProUGUI outputAmount;
    public TextMeshProUGUI recipeText;

    void Start()
    {
        UpdateButtonUI();
        button.onClick.AddListener(() => craftingManager.Craft(recipe));
    }

    public void UpdateButtonUI()
    {
        // First hide all input slots
        foreach (var slot in inputSlots)
        {
            slot.icon.gameObject.SetActive(false);
            slot.amountText.gameObject.SetActive(false);
        }

        // Then show only the ones we need
        if (recipe.inputs != null)
        {
            for (int i = 0; i < recipe.inputs.Length && i < inputSlots.Length; i++)
            {
                var ingredient = recipe.inputs[i];
                inputSlots[i].icon.sprite = ingredient.item.icon;
                inputSlots[i].icon.gameObject.SetActive(true);

                inputSlots[i].amountText.text = ingredient.amount.ToString();
                inputSlots[i].amountText.gameObject.SetActive(true);
            }
        }

        // Output
        if (recipe.outputItem != null)
        {
            outputImage.sprite = recipe.outputItem.icon;
            outputImage.enabled = true;
            outputAmount.text = recipe.outputAmount.ToString();
            recipeText.text = recipe.outputItem.displayName;
        }
        else
        {
            outputImage.enabled = false;
            recipeText.text = "Unknown Item";
        }
    }
}
