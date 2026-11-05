using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controla moedas, mortes, checkpoints, fim de fase e o HUD.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("HUD")]
    [SerializeField] private Text coinText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text hintText;

    [Header("Regras")]
    [SerializeField] private float fallY = -10f;
    [SerializeField] private float respawnDelay = 0.7f;
    [SerializeField] private float hintSeconds = 7f;

    private int coins;
    private int totalCoins;
    private int deaths;
    private float elapsed;
    private Vector2 respawnPoint;
    private bool finished;
    private bool respawning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        totalCoins = FindObjectsOfType<Coin>().Length;
        respawnPoint = player.transform.position;
        messageText.text = "";
        UpdateHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (!finished) elapsed += Time.deltaTime;

        if (!finished && !respawning && player.transform.position.y < fallY)
            Kill();

        UpdateHud();

        if (hintText != null)
        {
            Color c = hintText.color;
            c.a = Mathf.Clamp01(hintSeconds + 1f - Time.timeSinceLevelLoad);
            hintText.color = c;
        }
    }

    private void UpdateHud()
    {
        coinText.text = "MOEDAS  " + coins + "/" + totalCoins;
        int minutes = (int)(elapsed / 60f);
        float seconds = elapsed - minutes * 60f;
        timeText.text = minutes.ToString("00") + ":" + seconds.ToString("00.0");
    }

    public void AddCoin()
    {
        coins++;
        Sfx.Coin();
    }

    public void SetCheckpoint(Vector2 position)
    {
        respawnPoint = position;
        Sfx.Checkpoint();
    }

    public void Kill()
    {
        if (respawning || finished) return;
        StartCoroutine(KillRoutine());
    }

    private IEnumerator KillRoutine()
    {
        respawning = true;
        deaths++;

        Sfx.Hit();
        Effects.Puff(player.transform.position, new Color(0.9f, 0.3f, 0.3f), 18, 6f, 0.18f, 0.6f, 12f);
        Effects.Puff(player.transform.position, new Color(0.25f, 0.45f, 0.95f), 12, 5f, 0.14f, 0.6f, 12f);
        cameraFollow.Shake(0.35f, 0.3f);
        player.SetAlive(false);

        yield return new WaitForSeconds(respawnDelay);

        player.Respawn(respawnPoint);
        cameraFollow.Snap();
        respawning = false;
    }

    public void Win()
    {
        if (finished) return;
        finished = true;

        Sfx.Win();
        player.SetControl(false);

        int minutes = (int)(elapsed / 60f);
        float seconds = elapsed - minutes * 60f;
        messageText.text = "FASE COMPLETA!\n\n"
                           + "Moedas: " + coins + "/" + totalCoins + "\n"
                           + "Tempo: " + minutes.ToString("00") + ":" + seconds.ToString("00.0") + "\n"
                           + "Mortes: " + deaths + "\n\n"
                           + "Aperte R para jogar de novo";
    }
}
