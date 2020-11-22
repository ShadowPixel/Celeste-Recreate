using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animations : MonoBehaviour
{
    private Animator Anim;
    private PlayerMovement move;
    private Collision coll;
    
    public SpriteRenderer sr;

    void Start()
    {
        Anim = GetComponent<Animator>();
        coll = GetComponentInParent<Collision>();
        move = GetComponentInParent<PlayerMovement>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Anim.SetBool("onGround", coll.onGround);
        Anim.SetBool("onWall", coll.onWall);
        Anim.SetBool("onRightWall", coll.onRightWall);
        Anim.SetBool("wallGrab", move.wallGrabbed);
        Anim.SetBool("wallSlide", move.wallSlide);
        Anim.SetBool("canMove", move.canMove);
        Anim.SetBool("isDashing", move.isDashing);

    }

    public void SetHorizontalMovement(float x, float y, float yVel)
    {
        Anim.SetFloat("HorizontalAxis", x);
        Anim.SetFloat("VerticalAxis", y);
        Anim.SetFloat("VerticalVelocity", yVel);
    }

    public void SetTrigger(string trigger)
    {
        Anim.SetTrigger(trigger);
    }

    public void Flip(int side)
    {
        if (move.wallGrabbed || move.wallSlide)
        {
            if (side == -1 && sr.flipX)
            {
                return;
            }
            if (side == 1 && !sr.flipX)
            {
                return;
            }
        }

        bool state = (side == 1) ? false : true;
        sr.flipX = state;
    }
}
