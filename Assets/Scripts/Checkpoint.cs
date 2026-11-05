using UnityEngine;

/// <summary>Bandeira de checkpoint: começa apagada e acende (animada) quando o jogador passa.</summary>
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer), typeof(SpriteFlipbook))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);

    private SpriteRenderer sr;
    private SpriteFlipbook flipbook;
    private bool active;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        flipbook = GetComponent<SpriteFlipbook>();
        sr.color = inactiveColor;
        flipbook.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (active || other.GetComponent<PlayerController>() == null) return;
        active = true;

        sr.color = Color.white;
        flipbook.enabled = true;
        Effects.Puff(transform.position + Vector3.up * 1.5f, new Color(0.5f, 1f, 0.6f), 10, 3f, 0.1f, 0.5f);

        if (GameManager.Instance != null)
            GameManager.Instance.SetCheckpoint((Vector2)transform.position + Vector2.up * 0.6f);
    }
}
