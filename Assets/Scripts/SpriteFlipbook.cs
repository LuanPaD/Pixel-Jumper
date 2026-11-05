using UnityEngine;

/// <summary>Troca os quadros de um SpriteRenderer em loop (moedas, bandeiras).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 8f;

    private SpriteRenderer sr;
    private float offset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        offset = Random.value * 10f;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        sr.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
    }
}
