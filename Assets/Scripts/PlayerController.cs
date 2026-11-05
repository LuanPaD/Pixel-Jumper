using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 100f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.75f;

    [Header("Pulo")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float fallGravityMultiplier = 1.4f;
    [SerializeField] private float maxFallSpeed = 22f;

    [Header("Detecção de chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float baseGravity;
    private float moveInput;
    private float coyoteCounter;
    private float bufferCounter;
    private bool cutJump;
    private bool wasGrounded;
    private bool controlEnabled = true;
    private float lastYVelocity;

    public bool IsGrounded { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public float VelocityX => rb.velocity.x;
    public float VelocityY => rb.velocity.y;
    public float Facing { get; private set; } = 1f;

    /// <summary>Disparado quando o pulo começa.</summary>
    public event Action Jumped;

    /// <summary>Disparado ao tocar o chão. O parâmetro é a velocidade de impacto.</summary>
    public event Action<float> Landed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        baseGravity = rb.gravityScale;
    }

    private void Update()
    {
        if (controlEnabled)
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            bool jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            bool jumpUp = Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);

            if (jumpDown) bufferCounter = jumpBufferTime;
            if (jumpUp && rb.velocity.y > 0f) cutJump = true;
        }
        else
        {
            moveInput = 0f;
        }

        bufferCounter -= Time.deltaTime;

        if (moveInput > 0.01f) Facing = 1f;
        else if (moveInput < -0.01f) Facing = -1f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        IsGrounded = groundCheck != null
                     && rb.velocity.y <= 0.1f
                     && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (IsGrounded && !wasGrounded && lastYVelocity < -2f)
            Landed?.Invoke(Mathf.Abs(lastYVelocity));

        coyoteCounter = IsGrounded ? coyoteTime : coyoteCounter - dt;

        Vector2 v = rb.velocity;

        // Aceleração horizontal
        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        if (!IsGrounded) rate *= airControl;
        v.x = Mathf.MoveTowards(v.x, target, rate * dt);

        // Pulo (com coyote time e buffer de input)
        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            v.y = jumpForce;
            bufferCounter = 0f;
            coyoteCounter = 0f;
            cutJump = false;
            Jumped?.Invoke();
        }

        // Pulo variável: soltar o botão corta a subida
        if (cutJump)
        {
            if (v.y > 0f) v.y *= jumpCutMultiplier;
            cutJump = false;
        }

        // Queda mais pesada e velocidade máxima de queda
        rb.gravityScale = v.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
        v.y = Mathf.Max(v.y, -maxFallSpeed);

        rb.velocity = v;

        wasGrounded = IsGrounded;
        lastYVelocity = v.y;
    }

    /// <summary>Liga ou desliga a leitura de input, sem desligar a física.</summary>
    public void SetControl(bool enabled)
    {
        controlEnabled = enabled;
        if (!enabled) moveInput = 0f;
    }

    /// <summary>Remove o jogador de cena (morte) ou o devolve.</summary>
    public void SetAlive(bool alive)
    {
        IsAlive = alive;
        controlEnabled = alive;
        rb.simulated = alive;
        if (!alive)
        {
            rb.velocity = Vector2.zero;
            moveInput = 0f;
        }
    }

    public void Respawn(Vector2 position)
    {
        transform.position = position;
        rb.position = position;
        rb.velocity = Vector2.zero;
        bufferCounter = 0f;
        coyoteCounter = 0f;
        lastYVelocity = 0f;
        wasGrounded = true;
        SetAlive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
