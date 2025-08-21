using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public UIInventory inventory;
    public Transform dropPosition;

    public void Craft(CraftingRecipe recipe)
    {
        foreach (var ingridient in recipe.inputs)
        {
            if (!inventory.HasItem(ingridient.item, ingridient.amount))
            {
                Debug.Log("Missing Ingridient: " + ingridient.item.displayName);
                return;
            }
        }

        foreach (var ingridient in recipe.inputs)
        {
            inventory.RemoveItem(ingridient.item, ingridient.amount);
        }

        if (recipe.outputItem.dropPrefab != null)
        {
            inventory.ThrowItem(recipe.outputItem, recipe.outputAmount);
        }
    }
}
