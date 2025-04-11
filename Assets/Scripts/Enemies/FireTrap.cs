using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrap : MonoBehaviour
{
    [SerializeField] private float damage;

    [Header("Firetrap Timer")]
    [SerializeField] private float activationDelay;
    [SerializeField] private float activeTime;
    private Animator anim;
    private SpriteRenderer spriteRend;

    private Collider2D trapCollider;
    private CapsuleCollider2D triggerCollider;
    private bool active;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();

        trapCollider = GetComponent<Collider2D>();
        triggerCollider = GetComponentInChildren<CapsuleCollider2D>();
        trapCollider.enabled = true;
        triggerCollider.enabled = false;
        StartCoroutine(ActivateFirtetrap());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.GetComponent<Health>().TakeDamage(damage);
        }
    }


    private IEnumerator ActivateFirtetrap()
    {

        while (true)
        {
            yield return new WaitForSeconds(activationDelay);
            spriteRend.color = new Color(1f, 0.5f, 0f);
            active = true;
            triggerCollider.enabled = true;
            anim.SetBool("activated", true);

            yield return new WaitForSeconds(activeTime);
            spriteRend.color = Color.white;
            active = false;
            triggerCollider.enabled = false;
            anim.SetBool("activated", false);
        }
    }
}