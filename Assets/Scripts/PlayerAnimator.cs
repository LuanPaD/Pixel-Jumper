using UnityEngine;

/// <summary>Anima o sprite do jogador (idle, corrida, pulo, queda) com squash and stretch.</summary>
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Quadros")]
    [SerializeField] private Sprite[] idle;
    [SerializeField] private Sprite[] run;
    [SerializeField] private Sprite jump;
    [SerializeField] private Sprite fall;

    [Header("Ajustes")]
    [SerializeField] private float idleFps = 2.5f;
    [SerializeField] private float runFps = 12f;
    [SerializeField] private Color dustColor = new Color(0.86f, 0.80f, 0.70f, 0.9f);

    private float animTimer;
    private float dustTimer;
    private Vector3 squash = Vector3.one;

    private void OnEnable()
    {
        controller.Jumped += OnJumped;
        controller.Landed += OnLanded;
    }

    private void OnDisable()
    {
        controller.Jumped -= OnJumped;
        controller.Landed -= OnLanded;
    }

    private Vector2 Feet => (Vector2)controller.transform.position + Vector2.down * 0.5f;

    private void OnJumped()
    {
        squash = new Vector3(0.7f, 1.35f, 1f);
        Sfx.Jump();
        Effects.Puff(Feet, dustColor, 5, 1.6f, 0.1f, 0.3f);
    }

    private void OnLanded(float impact)
    {
        float k = Mathf.Clamp01(impact / 18f);
        squash = new Vector3(1f + 0.4f * k, 1f - 0.35f * k, 1f);
        Effects.Puff(Feet, dustColor, Mathf.RoundToInt(3 + 6 * k), 2f, 0.1f, 0.35f);
    }

    private void Update()
    {
        spriteRenderer.enabled = controller.IsAlive;
        spriteRenderer.flipX = controller.Facing < 0f;

        float vx = Mathf.Abs(controller.VelocityX);
        animTimer += Time.deltaTime;

        if (!controller.IsGrounded)
        {
            spriteRenderer.sprite = controller.VelocityY > 0.1f ? jump : fall;
        }
        else if (vx > 0.5f)
        {
            spriteRenderer.sprite = Frame(run, runFps);

            if (vx > 4f)
            {
                dustTimer -= Time.deltaTime;
                if (dustTimer <= 0f)
                {
                    dustTimer = 0.14f;
                    Vector2 back = Feet + Vector2.left * Mathf.Sign(controller.VelocityX) * 0.15f;
                    Effects.Puff(back, dustColor, 1, 1f, 0.08f, 0.3f);
                }
            }
        }
        else
        {
            spriteRenderer.sprite = Frame(idle, idleFps);
        }

        // Squash and stretch com os pés fixos no chão
        squash = Vector3.Lerp(squash, Vector3.one, Time.deltaTime * 12f);
        transform.localScale = squash;
        transform.localPosition = new Vector3(0f, 0.5f * (squash.y - 1f), 0f);
    }

    private Sprite Frame(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return spriteRenderer.sprite;
        return frames[(int)(animTimer * fps) % frames.Length];
    }
}
