using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CraftingIngredient
{
    public ItemData item;
    public int amount;
}
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Inputs")]
    public CraftingIngredient[] inputs;

    [Header("Output")]
    public ItemData outputItem;
    public int outputAmount = 1;
    
}
