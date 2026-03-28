using UnityEngine;

public class PlayerMoveController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float climbSpeed = 3f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Mobile UI")]
    [Tooltip("Ссылка на корневой объект мобильного UI (Canvas/Panel). Будет включаться/отключаться.")]
    [SerializeField] private GameObject mobileUIRoot;
    [Tooltip("Включать мобильный режим автоматически на мобильных платформах.")]
    [SerializeField] private bool autoEnableOnMobilePlatform = true;
    [SerializeField] private bool mobileMode = false;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    // это тут быть не должно, но времени уже нет :))0)
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpAudioClip;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isClimbing;
    private bool moveLocks = false;
    public bool MoveLocks => moveLocks;

    private Rigidbody2D rb;

    private float baseGravity;
    private Vector3 baseScale;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private float kbHorizontal;
    private float kbVertical;

    private bool mLeftHeld, mRightHeld, mUpHeld, mDownHeld;
    private bool mJumpPressedFrame;

    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
    private static readonly int IsClimbingHash = Animator.StringToHash("isClimbing");
    private static readonly int JumpHash = Animator.StringToHash("jump");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        baseGravity = rb.gravityScale;

        if (autoEnableOnMobilePlatform && Application.isMobilePlatform)
            SetMobileMode(true);
        else
            ApplyMobileUIVisibility();
    }

    private void Start()
    {
        moveLocks = true;
    }

    private void Update()
    {
        if (!mobileMode)
        {
            kbHorizontal = Input.GetAxisRaw("Horizontal");
            kbVertical = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.Space))
                jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            int h = 0;
            if (mLeftHeld) h -= 1;
            if (mRightHeld) h += 1;

            int v = 0;
            if (mDownHeld) v -= 1;
            if (mUpHeld) v += 1;

            kbHorizontal = h;
            kbVertical = v;

            if (mJumpPressedFrame)
            {
                jumpBufferCounter = jumpBufferTime;
                mJumpPressedFrame = false;
            }
        }

        UpdateGroundedState();
        UpdateJumpTimers();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (!moveLocks)
        {
            if (isClimbing)
            {
                HandleClimbing(kbHorizontal, kbVertical);
            }
            else
            {
                HandleMovement(kbHorizontal);
                HandleJump();
            }
        }

        Flip(kbHorizontal);
    }

    private void UpdateGroundedState()
    {
        if (!groundCheck) return;

        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateJumpTimers()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void HandleMovement(float horizontalInput)
    {
        Vector2 movement = new Vector2(horizontalInput * speed, rb.linearVelocity.y);
        rb.linearVelocity = movement;

        if (anim != null)
            anim.SetFloat(SpeedHash, Mathf.Abs(movement.x));
    }

    private void HandleClimbing(float horizontalInput, float verticalInput)
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(horizontalInput * speed, verticalInput * climbSpeed);
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            audioSource.PlayOneShot(jumpAudioClip);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (anim != null)
                anim.SetTrigger(JumpHash);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        anim.SetBool(IsGroundedHash, isGrounded);
        anim.SetFloat(VerticalSpeedHash, rb.linearVelocity.y);
        anim.SetBool(IsClimbingHash, isClimbing);

        if (moveLocks)
            anim.SetFloat(SpeedHash, 0f);
    }

    private void Flip(float horizontalInput)
    {
        if (horizontalInput != 0 && !moveLocks)
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput) * baseScale.x, baseScale.y, baseScale.z);
    }

    public void SetClimbing(bool value)
    {
        isClimbing = value;
        rb.gravityScale = value ? 0f : baseGravity;
    }

    public void LockMove()
    {
        moveLocks = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anim != null)
            anim.SetFloat(SpeedHash, 0f);
    }

    public void UnlockMove()
    {
        moveLocks = false;
    }

    public void ToggleMobileMode()
    {
        SetMobileMode(!mobileMode);
    }

    public void SetMobileMode(bool enabled)
    {
        mobileMode = enabled;
        ResetMobileHeld();
        ApplyMobileUIVisibility();
    }

    private void ApplyMobileUIVisibility()
    {
        if (mobileUIRoot)
            mobileUIRoot.SetActive(mobileMode);
    }

    private void ResetMobileHeld()
    {
        mLeftHeld = mRightHeld = mUpHeld = mDownHeld = false;
        mJumpPressedFrame = false;
        kbHorizontal = kbVertical = 0f;
        jumpBufferCounter = 0f;
    }

    public void MobileLeftDown() { mLeftHeld = true; }
    public void MobileLeftUp() { mLeftHeld = false; }
    public void MobileRightDown() { mRightHeld = true; }
    public void MobileRightUp() { mRightHeld = false; }

    public void MobileUpDown() { mUpHeld = true; }
    public void MobileUpUp() { mUpHeld = false; }
    public void MobileDownDown() { mDownHeld = true; }
    public void MobileDownUp() { mDownHeld = false; }
    public void MobileJumpPress() { mJumpPressedFrame = true; }

    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
