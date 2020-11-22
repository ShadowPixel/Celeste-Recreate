using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    private Collision coll;

    public Rigidbody2D PlayerRB;
    private Animations Anim;

    [Space]
    [Header("Stats")]
    public float speed = 10;
    public float jumpForce = 50;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrabbed;
    public bool wallJump;
    public bool wallSlide;
    public bool isDashing;

    [Space]
    private bool onGround;
    public bool hasDashed;

    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem wallJumpParticle;
    public ParticleSystem slideParticle;

    void Start()
    {
        coll = GetComponent<Collision>();
        PlayerRB = GetComponent<Rigidbody2D>();
        Anim = GetComponentInChildren<Animations>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        Walk(dir);
        Anim.SetHorizontalMovement(x, y, PlayerRB.velocity.y);

        if (coll.onWall && Input.GetButton("Fire3") && canMove)
        {
            if (side != coll.wallSide)
            {
                Anim.Flip(side * -1);
            }
            wallGrabbed = true;
            wallSlide = false;
        }

        if (Input.GetButtonUp("Fire3") || !coll.onWall || !canMove)
        {
            wallGrabbed = false;
            wallSlide = false;
        }

        if (coll.onGround && !isDashing)
        {
            wallJump = false;
            GetComponent<BetterJumping>().enabled = true;
        }

        if (wallGrabbed && !isDashing)
        {
            PlayerRB.gravityScale = 0;
            if (x > .2f || x < -.2f)
            {
                PlayerRB.velocity = new Vector2(PlayerRB.velocity.x, 0);
            }
            float speedModifier = y > 0 ? .5f : 1;
            PlayerRB.velocity = new Vector2(PlayerRB.velocity.x, y * (speed * speedModifier));
        }
        else
        {
            PlayerRB.gravityScale = 3;
        }

        if (coll.onWall && !coll.onGround)
        {
            if (x != 0 && !wallGrabbed)
            {
                wallSlide = true;
                WallSlide();
            }
        }

        if (!coll.onWall || coll.onGround)
        {
            wallSlide = false;
        }

        if (Input.GetButtonDown("Jump"))
        {
            Anim.SetTrigger("jump");

            if (coll.onGround)
            {
                Jump(Vector2.up, false);
            }
            if (coll.onWall && !coll.onGround)
            {
                WallJump();
            }
        }

        if (Input.GetButtonDown("Fire1") && !hasDashed)
        {
            if (xRaw != 0 || yRaw != 0)
            {
                Dash(xRaw, yRaw);
            }
        }

        if (coll.onGround && !onGround)
        {
            OnGround();
            onGround = true;
        }

        if (!coll.onGround && onGround)
        {
            onGround = false;
        }

        WallParticle(y);

        if (wallGrabbed || wallSlide || !canMove)
        {
            return;
        }

        if (x > 0)
        {
            side = 1;
            Anim.Flip(side);
        }
        if (x < 0)
        {
            side = -1;
            Anim.Flip(side);
        }
    }

    void OnGround()
    {
        hasDashed = false;
        isDashing = false;

        side = Anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Dash(float x, float y)
    {
        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);

        hasDashed = true;

        Anim.SetTrigger("dash");

        PlayerRB.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        PlayerRB.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashPause());
    }

    IEnumerator DashPause()
    {
        FindObjectOfType<GhostEffect>().ShowGhost();
        StartCoroutine(GroundDash());
        DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        dashParticle.Play();
        PlayerRB.gravityScale = 0;
        GetComponent<BetterJumping>().enabled = false;
        wallJump = true;
        isDashing = false;

        yield return new WaitForSeconds(.3f);

        dashParticle.Stop();
        PlayerRB.gravityScale = 3;
        GetComponent<BetterJumping>().enabled = true;
        wallJump = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
        {
            hasDashed = false;
        }
    }

    private void WallJump()
    {
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            Anim.Flip(side);
        }

        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJump = true;
    }

    private void WallSlide()
    {
        if(coll.wallSide != side)
        {
            Anim.Flip(side * -1);
        }

        if (!canMove)
        {
            return;
        }

        bool pushingWall = false;
        if((PlayerRB.velocity.x > 0 && coll.onRightWall) || (PlayerRB.velocity.x < 0 && coll. onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : PlayerRB.velocity.x;

        PlayerRB.velocity = new Vector2(push, -slideSpeed);
    }

    private void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrabbed)
            return;

        if (!wallJump)
        {
            PlayerRB.velocity = new Vector2(dir.x * speed, PlayerRB.velocity.y);
        }
        else
        {
            PlayerRB.velocity = Vector2.Lerp(PlayerRB.velocity, (new Vector2(dir.x * speed, PlayerRB.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

        PlayerRB.velocity = new Vector2(PlayerRB.velocity.x, 0);
        PlayerRB.velocity += dir * jumpForce;

        particle.Play();
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        PlayerRB.drag = x;
    }

    void WallParticle(float vertical)
    {
        var main = slideParticle.main;

        if (wallSlide || (wallGrabbed && vertical < 0))
        {
            slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    int ParticleSide()
    {
        int particleSide = coll.onRightWall ? 1 : -1;
        return particleSide;
    }
}