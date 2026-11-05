using UnityEngine;

/// <summary>Efeitos visuais simples (poeira, faíscas) sem depender de assets externos.</summary>
public static class Effects
{
    private static Sprite pixel;

    private static Sprite Pixel
    {
        get
        {
            if (pixel == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return pixel;
        }
    }

    public static void Puff(Vector2 position, Color color, int count, float speed = 2f,
                            float size = 0.12f, float life = 0.4f, float gravity = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Puff");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Pixel;
            sr.color = color;
            sr.sortingOrder = 10;

            Vector2 dir = Random.insideUnitCircle;
            dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
            go.AddComponent<PuffParticle>().Init(dir.normalized * speed * Random.Range(0.4f, 1f),
                                                 life * Random.Range(0.7f, 1.2f), gravity);
        }
    }
}

public class PuffParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float life;
    private float age;
    private float gravity;
    private float startScale;
    private SpriteRenderer sr;
    private Color startColor;

    public void Init(Vector2 initialVelocity, float lifetime, float gravityValue)
    {
        velocity = initialVelocity;
        life = lifetime;
        gravity = gravityValue;
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
        startScale = transform.localScale.x;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * Time.deltaTime;
        velocity *= 1f - Time.deltaTime * 3f;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        float t = age / life;
        transform.localScale = Vector3.one * startScale * (1f - t * 0.6f);
        Color c = startColor;
        c.a *= 1f - t;
        sr.color = c;
    }
}
