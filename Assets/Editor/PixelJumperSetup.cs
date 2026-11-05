using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gera a arte e monta a cena Main (fase, fundo, jogador, câmera e HUD).
/// Menu: Pixel Jumper > Criar Cena Main. Também roda em batch mode.
/// </summary>
public static class PixelJumperSetup
{
    private const string GroundLayerName = "Ground";
    private const string MaterialPath = "Assets/Art/NoFriction.physicsMaterial2D";
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string ScenePath = "Assets/Scenes/Main.unity";

    private const float CamStartX = 11f;
    private const float CamStartY = 2.1f;
    private const float LevelMinX = 0f;
    private const float LevelMaxX = 98f;

    private static readonly Vector2 PlayerStart = new Vector2(2f, 0.6f);

    [MenuItem("Pixel Jumper/Criar Cena Main")]
    public static void CreateMainScene()
    {
        int groundLayer = EnsureLayer(GroundLayerName);
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
        ArtSet art = PixelArtFactory.Generate();

        // O quadrado branco da versão anterior não é mais usado
        if (File.Exists("Assets/Art/Square.png")) AssetDatabase.DeleteAsset("Assets/Art/Square.png");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject light = GameObject.Find("Directional Light");
        if (light != null) Object.DestroyImmediate(light);

        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
        cam.transform.position = new Vector3(CamStartX, CamStartY, -10f);

        BuildBackground(art, cam.transform);
        BuildLevel(art, groundLayer);
        GameObject player = BuildPlayer(art, noFriction, groundLayer);

        Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);

        CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
        var followSo = new SerializedObject(follow);
        followSo.FindProperty("target").objectReferenceValue = player.transform;
        followSo.FindProperty("minX").floatValue = LevelMinX;
        followSo.FindProperty("maxX").floatValue = LevelMaxX;
        followSo.ApplyModifiedPropertiesWithoutUndo();

        BuildHudAndManager(player.GetComponent<PlayerController>(), follow);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PixelJumperSetup] Cena Main criada com sucesso.");
    }

    // ------------------------------------------------------------------ fundo

    private static void BuildBackground(ArtSet art, Transform cam)
    {
        var root = new GameObject("Background").transform;

        // Céu e sol acompanham a câmera (fator 1)
        var sky = ParallaxSprite("Ceu", root, art.sky, new Vector3(CamStartX, CamStartY, 0f), -100, cam, 1f, 1f);
        sky.transform.localScale = new Vector3(80f, 0.5f, 1f);

        ParallaxSprite("Sol", root, art.sun, new Vector3(CamStartX + 6f, CamStartY + 3.2f, 0f), -99, cam, 1f, 1f);

        var clouds = ParallaxSprite("Nuvens", root, art.clouds, new Vector3(CamStartX, CamStartY + 2.6f, 0f), -90, cam, 0.9f, 0.9f);
        Tile(clouds, 200f, art.clouds.rect.height / 16f);
        var cloudSo = new SerializedObject(clouds.GetComponent<ParallaxLayer>());
        cloudSo.FindProperty("autoScroll").floatValue = 0.4f;
        cloudSo.FindProperty("wrapWidth").floatValue = art.clouds.rect.width / 16f;
        cloudSo.ApplyModifiedPropertiesWithoutUndo();

        var far = ParallaxSprite("Colinas Longe", root, art.hillsFar, new Vector3(CamStartX, -6.5f, 0f), -80, cam, 0.85f, 0.92f);
        Tile(far, 200f, art.hillsFar.rect.height / 16f);

        var near = ParallaxSprite("Colinas Perto", root, art.hillsNear, new Vector3(CamStartX, -6.5f, 0f), -70, cam, 0.65f, 0.95f);
        Tile(near, 200f, art.hillsNear.rect.height / 16f);
    }

    private static GameObject ParallaxSprite(string name, Transform parent, Sprite sprite, Vector3 position,
                                             int order, Transform cam, float factorX, float factorY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;

        var layer = go.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("cam").objectReferenceValue = cam;
        so.FindProperty("factorX").floatValue = factorX;
        so.FindProperty("factorY").floatValue = factorY;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void Tile(GameObject go, float width, float height)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, height);
    }

    // ------------------------------------------------------------------ fase

    private struct Segment
    {
        public float xl, xr, top;
        public int depth;
        public Segment(float xl, float xr, float top, int depth) { this.xl = xl; this.xr = xr; this.top = top; this.depth = depth; }
    }

    private static void BuildLevel(ArtSet art, int groundLayer)
    {
        var level = new GameObject("Level").transform;
        var items = new GameObject("Itens").transform;
        var rng = new System.Random(7);

        // Chão (x esquerdo, x direito, altura do topo, profundidade)
        var ground = new[]
        {
            new Segment(0, 16, 0, 5),
            new Segment(19, 31, 0, 5),
            new Segment(35, 47, 0, 5),
            new Segment(50, 62, 1, 6),
            new Segment(66, 80, 0, 5),
            new Segment(84, 98, 2, 7),
        };

        // Plataformas flutuantes
        var floating = new[]
        {
            new Segment(22, 25, 2, 1),
            new Segment(38, 41, 2, 1),
            new Segment(42, 45, 4, 1),
            new Segment(53, 56, 3, 1),
            new Segment(58, 61, 4, 1),
            new Segment(69, 72, 2, 1),
            new Segment(74, 77, 3, 1),
            new Segment(81, 83, 1, 1),
            new Segment(88, 91, 4, 1),
            new Segment(92, 95, 6, 1),
        };

        // Células (x inteiro) com espinhos, por altura do chão
        var spikeCells = new HashSet<int> { 27, 28, 44, 45, 57, 58, 77, 78 };

        foreach (var s in ground) BuildSolid("Chao", level, art, groundLayer, s, rng, spikeCells);
        foreach (var s in floating) BuildSolid("Plataforma", level, art, groundLayer, s, rng, null);

        // Paredes invisíveis nas pontas
        BuildWall("Parede Esquerda", level, new Vector2(-0.5f, 5f));
        BuildWall("Parede Direita", level, new Vector2(98.5f, 5f));

        // Espinhos
        foreach (int cell in spikeCells)
        {
            float top = GroundTopAt(ground, cell + 0.5f);
            var go = Item("Espinhos", items, art.spikes, new Vector3(cell + 0.5f, top, 0f), 2);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.45f);
            col.offset = new Vector2(0f, 0.25f);
            go.AddComponent<Hazard>();
        }

        // Checkpoints e meta
        BuildFlag("Checkpoint", items, art.checkpointFlag, new Vector3(37f, 0f, 0f), false);
        BuildFlag("Checkpoint", items, art.checkpointFlag, new Vector3(68f, 0f, 0f), false);
        BuildFlag("Meta", items, art.goalFlag, new Vector3(93.5f, 6f, 0f), true);

        // Moedas
        CoinArc(art, items, 14f, 20f, 0.9f, 2.2f, 7);     // sobre o 1o buraco
        CoinLine(art, items, 22.5f, 24.5f, 3.0f, 3);      // plataforma 1
        CoinArc(art, items, 29f, 36f, 0.9f, 2.2f, 7);     // 2o buraco
        CoinLine(art, items, 38.5f, 40.5f, 3.0f, 3);
        CoinLine(art, items, 42.5f, 44.5f, 5.0f, 3);
        CoinArc(art, items, 46f, 51f, 1.9f, 2.0f, 5);     // 3o buraco
        CoinLine(art, items, 53.5f, 55.5f, 4.0f, 3);
        CoinLine(art, items, 58.5f, 60.5f, 5.0f, 3);
        CoinArc(art, items, 61f, 67f, 1.9f, 2.0f, 6);     // 4o buraco
        CoinLine(art, items, 69.5f, 71.5f, 3.0f, 3);
        CoinLine(art, items, 74.5f, 76.5f, 4.0f, 3);
        CoinArc(art, items, 79.5f, 84.5f, 2.2f, 2.0f, 5); // 5o buraco
        CoinLine(art, items, 88.5f, 90.5f, 5.0f, 3);
        BuildCoin(art, items, new Vector3(92.4f, 7.2f, 0f));
        BuildCoin(art, items, new Vector3(94.6f, 7.2f, 0f));
    }

    private static float GroundTopAt(Segment[] ground, float x)
    {
        foreach (var s in ground)
            if (x >= s.xl && x <= s.xr) return s.top;
        return 0f;
    }

    private static void BuildSolid(string name, Transform parent, ArtSet art, int groundLayer, Segment s,
                                   System.Random rng, HashSet<int> blockedCells)
    {
        int width = Mathf.RoundToInt(s.xr - s.xl);

        var go = new GameObject(name);
        go.layer = groundLayer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((s.xl + s.xr) * 0.5f, s.top - s.depth * 0.5f, 0f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, s.depth);

        for (int cy = 0; cy < s.depth; cy++)
            for (int cx = 0; cx < width; cx++)
            {
                Sprite tile = cy == 0 ? art.grass[rng.Next(art.grass.Length)] : art.dirt[rng.Next(art.dirt.Length)];
                Item("Tile", go.transform, tile, new Vector3(s.xl + cx + 0.5f, s.top - cy - 0.5f, 0f), 0);
            }

        // Decoração (arbustos, flores, árvores) só no chão principal
        if (s.depth > 1)
        {
            for (int cx = 1; cx < width - 1; cx++)
            {
                int cell = Mathf.FloorToInt(s.xl) + cx;
                if (blockedCells != null && (blockedCells.Contains(cell) || blockedCells.Contains(cell - 1) || blockedCells.Contains(cell + 1)))
                    continue;

                double roll = rng.NextDouble();
                Vector3 pos = new Vector3(cell + 0.5f, s.top, 0f);
                if (roll < 0.07) Item("Arvore", go.transform, art.tree, pos, -5);
                else if (roll < 0.17) Item("Arbusto", go.transform, art.decor[0], pos, -4);
                else if (roll < 0.30) Item("Flor", go.transform, art.decor[1 + rng.Next(2)], pos, -4);
            }
        }
    }

    private static void BuildWall(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 40f);
    }

    private static GameObject Item(string name, Transform parent, Sprite sprite, Vector3 position, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    private static void BuildFlag(string name, Transform parent, Sprite[] frames, Vector3 position, bool goal)
    {
        var go = Item(name, parent, frames[0], position, 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = goal ? new Vector2(1.5f, 2.5f) : new Vector2(1f, 2f);
        col.offset = new Vector2(0f, goal ? 1.25f : 1f);

        var flipbook = go.AddComponent<SpriteFlipbook>();
        SetSprites(new SerializedObject(flipbook), "frames", frames, 4f);

        if (goal) go.AddComponent<Goal>();
        else go.AddComponent<Checkpoint>();
    }

    private static void CoinLine(ArtSet art, Transform parent, float x0, float x1, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (float)(count - 1);
            BuildCoin(art, parent, new Vector3(Mathf.Lerp(x0, x1, t), y, 0f));
        }
    }

    private static void CoinArc(ArtSet art, Transform parent, float x0, float x1, float baseY, float height, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float y = baseY + Mathf.Sin(t * Mathf.PI) * height;
            BuildCoin(art, parent, new Vector3(Mathf.Lerp(x0, x1, t), y, 0f));
        }
    }

    private static void BuildCoin(ArtSet art, Transform parent, Vector3 position)
    {
        var go = Item("Moeda", parent, art.coin[0], position, 3);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;
        go.AddComponent<Coin>();
        var flipbook = go.AddComponent<SpriteFlipbook>();
        SetSprites(new SerializedObject(flipbook), "frames", art.coin, 8f);
    }

    // ------------------------------------------------------------------ jogador

    private static GameObject BuildPlayer(ArtSet art, PhysicsMaterial2D material, int groundLayer)
    {
        var player = new GameObject("Player");
        player.transform.position = PlayerStart;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.85f);
        col.offset = new Vector2(0f, -0.075f);
        col.sharedMaterial = material;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var check = new GameObject("GroundCheck");
        check.transform.SetParent(player.transform, false);
        check.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = art.playerIdle[0];
        sr.sortingOrder = 5;

        var controller = player.AddComponent<PlayerController>();
        var cso = new SerializedObject(controller);
        cso.FindProperty("groundCheck").objectReferenceValue = check.transform;
        cso.FindProperty("groundLayer").intValue = 1 << groundLayer;
        cso.ApplyModifiedPropertiesWithoutUndo();

        var animator = visual.AddComponent<PlayerAnimator>();
        var aso = new SerializedObject(animator);
        aso.FindProperty("controller").objectReferenceValue = controller;
        aso.FindProperty("spriteRenderer").objectReferenceValue = sr;
        SetSprites(aso, "idle", art.playerIdle);
        SetSprites(aso, "run", art.playerRun);
        aso.FindProperty("jump").objectReferenceValue = art.playerJump;
        aso.FindProperty("fall").objectReferenceValue = art.playerFall;
        aso.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    // ------------------------------------------------------------------ HUD

    private static void BuildHudAndManager(PlayerController player, CameraFollow follow)
    {
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Text coins = HudText("Moedas", canvasGo.transform, new Vector2(0f, 1f), new Vector2(24f, -20f), TextAnchor.UpperLeft, 34);
        Text time = HudText("Tempo", canvasGo.transform, new Vector2(1f, 1f), new Vector2(-24f, -20f), TextAnchor.UpperRight, 34);
        Text message = HudText("Mensagem", canvasGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 48);
        Text hint = HudText("Dica", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), TextAnchor.LowerCenter, 26);
        hint.text = "A / D ou setas: mover     Espaço: pular     R: reiniciar";

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("cameraFollow").objectReferenceValue = follow;
        so.FindProperty("coinText").objectReferenceValue = coins;
        so.FindProperty("timeText").objectReferenceValue = time;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text HudText(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition,
                                TextAnchor alignment, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(900f, 400f);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.05f, 0.05f, 0.15f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    // ------------------------------------------------------------------ utilidades

    private static void SetSprites(SerializedObject so, string property, Sprite[] sprites, float fps = -1f)
    {
        SerializedProperty array = so.FindProperty(property);
        array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

        if (fps > 0f) so.FindProperty("fps").floatValue = fps;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Camadas de usuário começam no índice 8
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                return i;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        throw new System.Exception("Sem camada livre para " + layerName);
    }

    private static PhysicsMaterial2D EnsureNoFrictionMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
        if (existing != null) return existing;

        var mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }
}
