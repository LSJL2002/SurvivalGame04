using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you use TextMeshPro for the text

public class CraftingUIButton : MonoBehaviour
{
    public CraftingRecipe recipe;
    public CraftingManager craftingManager;

    [Header("UI References")]
    public Button button;
    public Image inputImage;
    public Image outputImage;
    public TextMeshProUGUI recipeText; // Or UnityEngine.UI.Text if using default Text

    void Start()
    {
        // Update the UI elements
        UpdateButtonUI();

        // Add listener for crafting
        button.onClick.AddListener(() => craftingManager.Craft(recipe));
    }

    public void UpdateButtonUI()
    {
        if (recipe.inputs != null && recipe.inputs.Length > 0)
        {
            // Take the first input for display (if multiple inputs, you could extend this)
            inputImage.sprite = recipe.inputs[0].item.icon;
            inputImage.enabled = true;
        }
        else
        {
            inputImage.enabled = false;
        }

        if (recipe.outputItem != null)
        {
            outputImage.sprite = recipe.outputItem.icon;
            outputImage.enabled = true;

            recipeText.text = recipe.outputItem.displayName;
        }
        else
        {
            outputImage.enabled = false;
            recipeText.text = "Unknown Item";
        }
    }
}
