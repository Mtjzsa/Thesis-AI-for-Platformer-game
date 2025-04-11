using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]public float speed;
    public float horizontalInput;
    public Rigidbody2D body;
    private Animator anim;
    private bool grounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        Move(horizontalInput);

        transform.localScale = new Vector3(Mathf.Sign(horizontalInput) * 5, 5, 5);

        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            Jump();
        }

        anim.SetBool("run", horizontalInput != 0);
        anim.SetBool("grounded", grounded);

    }

    public void Move(float direction)
    {
        body.velocity = new Vector2(direction * speed, body.velocity.y);
    }

    public void Jump()
    {
        body.velocity = new Vector2(body.velocity.x, (speed*1.5f));
        anim.SetTrigger("jump");
        grounded = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            grounded = true;
        }
    }
}
