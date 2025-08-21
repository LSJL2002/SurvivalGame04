using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerCondition : MonoBehaviour, IDamageable
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

        if (hunger.curValue == 0 || thirst.curValue == 0)
        {
            health.Subtract(5 * Time.deltaTime);
        }

        if (health.curValue <= 0f)
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

    public void Die()
    {
        Debug.Log("플레이어가 죽었다.");
        FindObjectOfType<GameOverManager>().ShowGameOver();
    }

    public void TakeDamage(int damage, Vector3 dir)
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