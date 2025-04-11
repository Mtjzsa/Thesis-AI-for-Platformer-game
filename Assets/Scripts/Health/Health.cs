using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header ("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private bool dead;

    [Header("Components")]
    [SerializeField] private Behaviour[] components;

    private void Awake()
    {
        currentHealth = startingHealth;
        anim = GetComponent<Animator>();
    }

    public void Respawn()
    {
        dead = false;
        currentHealth = startingHealth;
        anim.ResetTrigger("die");
        anim.Play("Idle");
        //foreach (Behaviour component in components)
        //{
        //    component.enabled = true;
        //}
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (!dead && currentHealth <= 0)
        {
            //foreach (Behaviour component in components)
            //{
            //    component.enabled = false;
            //}
            Die();
        }
    }

    public void Die()
    {
        anim.SetTrigger("die");
        dead = true;
    }

    public bool IsDead()
    {
        return dead;
    }
}
