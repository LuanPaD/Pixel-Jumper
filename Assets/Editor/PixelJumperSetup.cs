using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a cena Main com o jogador, o chao e as plataformas.
/// Menu: Pixel Jumper > Criar Cena Main. Tambem roda em batch mode.
/// </summary>
public static class PixelJumperSetup
{
    private const string GroundLayerName = "Ground";
    private const string SquarePath = "Assets/Art/Square.png";
    private const string MaterialPath = "Assets/Art/NoFriction.physicsMaterial2D";
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Pixel Jumper/Criar Cena Main")]
    public static void CreateMainScene()
    {
        int groundLayer = EnsureLayer(GroundLayerName);
        Sprite square = EnsureSquareSprite();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        ConfigureCamera();

        // Chao, plataformas e paredes
        CreateSolid("Chao", square, new Vector2(0f, -4f), new Vector2(26f, 1f), groundLayer, new Color(0.30f, 0.70f, 0.35f));
        CreateSolid("Plataforma 1", square, new Vector2(-4f, -1.5f), new Vector2(4f, 0.5f), groundLayer, new Color(0.30f, 0.70f, 0.35f));
        CreateSolid("Plataforma 2", square, new Vector2(2f, 0.5f), new Vector2(4f, 0.5f), groundLayer, new Color(0.30f, 0.70f, 0.35f));
        CreateSolid("Plataforma 3", square, new Vector2(8f, 2.5f), new Vector2(4f, 0.5f), groundLayer, new Color(0.30f, 0.70f, 0.35f));
        CreateSolid("Parede Esquerda", square, new Vector2(-13.5f, 1f), new Vector2(1f, 12f), groundLayer, new Color(0.25f, 0.25f, 0.30f));
        CreateSolid("Parede Direita", square, new Vector2(13.5f, 1f), new Vector2(1f, 12f), groundLayer, new Color(0.25f, 0.25f, 0.30f));

        GameObject player = CreatePlayer(square, noFriction, groundLayer);

        // Prefab do jogador
        Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Remove a cena de exemplo e deixa Main como cena de build
        if (File.Exists("Assets/Scenes/SampleScene.unity"))
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PixelJumperSetup] Cena Main criada com sucesso.");
    }

    private static void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.transform.position = new Vector3(0f, 1f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
    }

    private static GameObject CreateSolid(string name, Sprite sprite, Vector2 pos, Vector2 size, int layer, Color color)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static GameObject CreatePlayer(Sprite sprite, PhysicsMaterial2D material, int groundLayer)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(-8f, -2.5f, 0f);

        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.20f, 0.40f, 0.95f);
        sr.sortingOrder = 1;

        var col = player.AddComponent<BoxCollider2D>();
        col.sharedMaterial = material;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var check = new GameObject("GroundCheck");
        check.transform.SetParent(player.transform, false);
        check.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        var controller = player.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("groundCheck").objectReferenceValue = check.transform;
        so.FindProperty("groundLayer").intValue = 1 << groundLayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static int EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Camadas de usuario comecam no indice 8
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

    private static Sprite EnsureSquareSprite()
    {
        if (!File.Exists(SquarePath))
        {
            Directory.CreateDirectory("Assets/Art");
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SquarePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SquarePath);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(SquarePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4f; // 4x4 px = 1 unidade
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
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
