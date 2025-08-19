using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;


public interface IDamagable
{
    void TakePhysicalDamage(int damage);
}
public class PlayerCondition : MonoBehaviour, IDamagable
{
    public UICondition uiCondition;

    Condition health { get { return uiCondition.health; } }
    Condition stamina { get { return uiCondition.stamina; } }
    Condition thirst { get { return uiCondition.thirst; } }
    Condition hunger { get { return uiCondition.hunger; } }

    public event Action onTakeDamage;
    private bool isInfiniteStamina = false;

    private void Update()
    {
        stamina.Add(stamina.passiveValue * Time.deltaTime);
        thirst.Subtract(thirst.passiveValue * Time.deltaTime);
        hunger.Subtract(hunger.passiveValue * Time.deltaTime);

        if (health.curValue < 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        health.Add(amount);
    }

    public void Recover(float amount)
    {
        stamina.Add(amount);
    }

    public void Eat(float amount)
    {
        hunger.Add(amount);
    }

    public void Drink(float amount)
    {
        thirst.Add(amount);
    }

    // public void Boost(Func<float> getter, Action<float> setter, float multiplier, float duration, BoostType boostType)
    // {
    //     StartCoroutine(BoostStats(getter, setter, multiplier, duration, boostType));
    // }

    // private IEnumerator BoostStats(Func<float> getter, Action<float> setter, float multiplier, float duration, BoostType boostType)
    // {
    //     if (boostType == BoostType.Stamina)
    //     {
    //         isInfiniteStamina = true;
    //     }
    //     else
    //     {
    //         float originalValue = getter();  // Save original value here
    //         setter(originalValue * multiplier);

    //         yield return new WaitForSeconds(duration);

    //         setter(originalValue);  // Restore original value here
    //     }

    //     if (boostType == BoostType.Stamina)
    //     {
    //         yield return new WaitForSeconds(duration); //Set the bool of isInfinite Stamina for a duration amount of time.
    //         isInfiniteStamina = false;
    //     }
    // }

    public void Die()
    {
        Debug.Log("플레이어가 죽었다.");
    }

    public void TakePhysicalDamage(int damage)
    {
        health.Subtract(damage);
        onTakeDamage?.Invoke();
    }

    public bool UseStamina(float amount)
    {
        if (isInfiniteStamina) //If the bool is true, then stamina will no longer be subtracted.
        {
            return true;
        }
        if (stamina.curValue - amount < 0)
            {
                return false;
            }
        stamina.Subtract(amount);
        return true;
    }

}