using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Sprites usados pela fase, gerados por PixelArtFactory.</summary>
public class ArtSet
{
    public Sprite[] playerIdle, playerRun, grass, dirt, coin, checkpointFlag, goalFlag, decor;
    public Sprite playerJump, playerFall, spikes, tree, sky, sun, clouds, hillsFar, hillsNear;
}

/// <summary>Desenha toda a arte pixel do jogo por código e salva como PNG em Assets/Art.</summary>
public static class PixelArtFactory
{
    private const string Root = "Assets/Art";
    private const float Ppu = 16f;

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

    // ------------------------------------------------------------------ canvas

    private class Px
    {
        public readonly int W, H;
        private readonly Color32[] d;

        public Px(int w, int h) { W = w; H = h; d = new Color32[w * h]; }

        // y = 0 e a linha de cima
        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            d[(H - 1 - y) * W + x] = c;
        }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    Set(x + i, y + j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 fill, Color32? outline = null)
        {
            var mask = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    mask[y * W + x] = dx * dx + dy * dy <= 1f;
                }

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!mask[y * W + x]) continue;
                    bool edge = !Inside(mask, x - 1, y) || !Inside(mask, x + 1, y)
                                || !Inside(mask, x, y - 1) || !Inside(mask, x, y + 1);
                    Set(x, y, edge && outline.HasValue ? outline.Value : fill);
                }
        }

        private bool Inside(bool[] mask, int x, int y)
        {
            return x >= 0 && y >= 0 && x < W && y < H && mask[y * W + x];
        }

        /// <summary>Formas sobrepostas com um unico contorno externo.</summary>
        public void Blob(float[][] shapes, Color32 fill, Color32 outline)
        {
            foreach (var s in shapes) Ellipse(s[0], s[1], s[2] + 1f, s[3] + 1f, outline);
            foreach (var s in shapes) Ellipse(s[0], s[1], s[2], s[3], fill);
        }

        public static Px FromMap(string[] rows, Dictionary<char, Color32> palette)
        {
            var px = new Px(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (palette.TryGetValue(rows[y][x], out var c)) px.Set(x, y, c);
            return px;
        }

        public void Write(string path)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(d);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }

    private static Color32 C(int r, int g, int b, int a = 255)
    {
        return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
    }

    // ------------------------------------------------------------------ entrada

    public static ArtSet Generate()
    {
        foreach (var dir in new[] { "Player", "Tiles", "Items", "Decor", "Background" })
            Directory.CreateDirectory(Root + "/" + dir);

        var art = new ArtSet();

        // Jogador
        var pal = new Dictionary<char, Color32>
        {
            { 'o', C(27, 27, 47) }, { 'b', C(59, 111, 224) }, { 'B', C(39, 74, 168) },
            { 's', C(255, 213, 168) }, { 'w', C(255, 255, 255) }, { 'e', C(30, 30, 50) },
            { 'r', C(224, 64, 64) },
        };

        string[] stand = { ".oBooBo.", ".oBooBo.", ".oo..oo." };
        string[] strideA = { ".oBooBo.", "oBo.oBo.", "oo..oo.." };
        string[] strideB = Mirror(strideA);
        string[] together = { "..oBBo..", "..oBBo..", "..oooo.." };
        string[] airUp = { "oBo..oBo", "oo....oo", "........" };
        string[] airDown = { "oBo..oBo", "oBo..oBo", "oo....oo" };

        art.playerIdle = new[]
        {
            SaveSprite(Px.FromMap(Compose(stand, 0, 0), pal), "Player/idle_0", Center),
            SaveSprite(Px.FromMap(Compose(stand, 1, 0), pal), "Player/idle_1", Center),
        };
        art.playerRun = new[]
        {
            SaveSprite(Px.FromMap(Compose(strideA, 1, 0), pal), "Player/run_0", Center),
            SaveSprite(Px.FromMap(Compose(together, 0, 0), pal), "Player/run_1", Center),
            SaveSprite(Px.FromMap(Compose(strideB, 1, 0), pal), "Player/run_2", Center),
            SaveSprite(Px.FromMap(Compose(together, 0, 0), pal), "Player/run_3", Center),
        };
        art.playerJump = SaveSprite(Px.FromMap(Compose(airUp, -1, -1), pal), "Player/jump", Center);
        art.playerFall = SaveSprite(Px.FromMap(Compose(airDown, 0, 0), pal), "Player/fall", Center);

        // Terreno
        art.grass = new[]
        {
            SaveSprite(GrassTile(11), "Tiles/grass_0", Center),
            SaveSprite(GrassTile(23), "Tiles/grass_1", Center),
        };
        art.dirt = new[]
        {
            SaveSprite(DirtTile(5), "Tiles/dirt_0", Center),
            SaveSprite(DirtTile(17), "Tiles/dirt_1", Center),
            SaveSprite(DirtTile(29), "Tiles/dirt_2", Center),
        };

        // Itens
        float[] coinWidths = { 5f, 3.5f, 1.5f, 3.5f };
        art.coin = new Sprite[4];
        for (int i = 0; i < 4; i++)
            art.coin[i] = SaveSprite(CoinFrame(coinWidths[i]), "Items/coin_" + i, Center);

        art.spikes = SaveSprite(Spikes(), "Items/spikes", Bottom);
        art.checkpointFlag = new[]
        {
            SaveSprite(Flag(0, C(70, 200, 110), C(40, 140, 80)), "Items/checkpoint_0", Bottom),
            SaveSprite(Flag(1, C(70, 200, 110), C(40, 140, 80)), "Items/checkpoint_1", Bottom),
        };
        art.goalFlag = new[]
        {
            SaveSprite(Flag(0, C(240, 190, 40), C(200, 120, 20)), "Items/goal_0", Bottom),
            SaveSprite(Flag(1, C(240, 190, 40), C(200, 120, 20)), "Items/goal_1", Bottom),
        };

        // Decoracao
        art.decor = new[]
        {
            SaveSprite(Bush(), "Decor/bush", Bottom),
            SaveSprite(Flower(C(235, 70, 80)), "Decor/flower_red", Bottom),
            SaveSprite(Flower(C(250, 215, 60)), "Decor/flower_yellow", Bottom),
        };
        art.tree = SaveSprite(Tree(), "Decor/tree", Bottom);

        // Fundo
        art.sky = SaveSprite(Sky(), "Background/sky", Center, 2f);
        art.sun = SaveSprite(Sun(), "Background/sun", Center);
        art.clouds = SaveSprite(Clouds(), "Background/clouds", Center, Ppu, true);
        art.hillsFar = SaveSprite(Hills(118, 16, 9, 2, 5, 1.3f, C(122, 178, 205), C(150, 200, 222)),
                                  "Background/hills_far", Bottom, Ppu, true);
        art.hillsNear = SaveSprite(Hills(100, 12, 7, 3, 8, 2.2f, C(86, 166, 112), C(112, 192, 128)),
                                   "Background/hills_near", Bottom, Ppu, true);

        AssetDatabase.SaveAssets();
        return art;
    }

    // ------------------------------------------------------------------ importacao

    private static Sprite SaveSprite(Px px, string name, Vector2 pivot, float ppu = Ppu, bool tiled = false)
    {
        string path = Root + "/" + name + ".png";
        px.Write(path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        imp.SetTextureSettings(settings);

        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ------------------------------------------------------------------ jogador

    private static readonly string[] HeadBody =
    {
        ".oooooo.",
        "obbbbbbo",
        "obBBBBbo",
        "osssssso",
        "osssweso",
        "osssssso",
        ".orrrro.",
        "obbbbbbo",
        "obbbbbbo",
        "obBBBBbo",
        ".oooooo.",
    };

    private static string[] Compose(string[] legs, int bodyDy, int legsDy)
    {
        var rows = new string[16];
        for (int i = 0; i < 16; i++) rows[i] = "................";
        for (int i = 0; i < HeadBody.Length; i++) rows[2 + bodyDy + i] = "...." + HeadBody[i] + "....";
        for (int i = 0; i < legs.Length; i++) rows[13 + legsDy + i] = "...." + legs[i] + "....";
        return rows;
    }

    private static string[] Mirror(string[] rows)
    {
        var result = new string[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var chars = rows[i].ToCharArray();
            System.Array.Reverse(chars);
            result[i] = new string(chars);
        }
        return result;
    }

    // ------------------------------------------------------------------ terreno

    private static Px DirtTile(int seed)
    {
        var rng = new System.Random(seed);
        var px = new Px(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int r = rng.Next(100);
                px.Set(x, y, r < 10 ? C(112, 70, 40) : r < 16 ? C(152, 104, 64) : C(134, 88, 52));
            }

        // pedrinhas
        for (int i = 0; i < 2; i++)
        {
            int sx = rng.Next(1, 14), sy = rng.Next(1, 14);
            px.Rect(sx, sy, 2, 1, C(150, 144, 138));
            px.Rect(sx, sy + 1, 2, 1, C(110, 104, 100));
        }
        return px;
    }

    private static Px GrassTile(int seed)
    {
        var px = DirtTile(seed);
        var rng = new System.Random(seed + 1);
        for (int x = 0; x < 16; x++)
        {
            int lip = 4 + rng.Next(3);
            for (int y = 0; y < lip; y++)
                px.Set(x, y, y == 0 ? C(124, 204, 92) : y == lip - 1 ? C(56, 130, 60) : C(84, 170, 70));
        }
        return px;
    }

    // ------------------------------------------------------------------ itens

    private static Px CoinFrame(float halfWidth)
    {
        var px = new Px(16, 16);
        px.Ellipse(8f, 8f, halfWidth, 6f, C(255, 200, 40), C(176, 112, 20));
        if (halfWidth > 3f)
        {
            px.Rect(6, 5, 1, 3, C(255, 240, 150));
            px.Rect(7, 4, 1, 1, C(255, 240, 150));
        }
        return px;
    }

    private static Px Spikes()
    {
        var px = new Px(16, 16);
        int[] centers = { 3, 8, 13 };
        foreach (int c in centers)
        {
            for (int r = 0; r < 10; r++)
            {
                float half = r * 0.5f;
                int y = 6 + r;
                for (int x = Mathf.RoundToInt(c - half); x <= Mathf.RoundToInt(c + half); x++)
                {
                    bool edge = x == Mathf.RoundToInt(c - half) || x == Mathf.RoundToInt(c + half) || r == 9;
                    bool light = x < c && !edge;
                    px.Set(x, y, edge ? C(86, 92, 110) : light ? C(214, 220, 232) : C(170, 176, 192));
                }
            }
        }
        return px;
    }

    private static Px Flag(int frame, Color32 cloth, Color32 shade)
    {
        var px = new Px(16, 32);
        px.Rect(2, 2, 2, 30, C(120, 80, 40));          // mastro
        px.Rect(1, 0, 4, 3, C(255, 220, 90));          // bola no topo

        for (int x = 4; x < 15; x++)
        {
            int wave = Mathf.RoundToInt(Mathf.Sin((x + frame * 3) * 0.9f));
            int height = Mathf.Max(2, 9 - (x - 4) * 6 / 10);
            int top = 4 + wave;
            for (int y = 0; y < height; y++)
                px.Set(x, top + y, y == height - 1 ? shade : cloth);
        }
        return px;
    }

    // ------------------------------------------------------------------ decoracao

    private static Px Bush()
    {
        var px = new Px(16, 16);
        px.Blob(new[]
        {
            new[] { 4f, 11.5f, 4f, 4f },
            new[] { 8f, 10f, 5f, 5f },
            new[] { 12f, 11.5f, 4f, 4f },
        }, C(64, 160, 76), C(38, 100, 52));
        px.Rect(6, 7, 3, 1, C(110, 200, 110));
        return px;
    }

    private static Px Flower(Color32 petal)
    {
        var px = new Px(16, 16);
        px.Rect(8, 9, 1, 7, C(60, 140, 60));
        px.Rect(6, 11, 2, 1, C(60, 140, 60));
        px.Rect(7, 6, 3, 3, petal);
        px.Set(8, 7, C(255, 240, 150));
        return px;
    }

    private static Px Tree()
    {
        var px = new Px(32, 48);
        px.Rect(14, 28, 4, 20, C(110, 70, 40));
        px.Rect(16, 28, 2, 20, C(88, 54, 30));
        px.Blob(new[]
        {
            new[] { 16f, 13f, 12f, 12f },
            new[] { 9f, 21f, 8f, 8f },
            new[] { 23f, 21f, 8f, 8f },
        }, C(52, 134, 62), C(30, 90, 45));
        px.Ellipse(12f, 8f, 4f, 3f, C(96, 176, 84));
        px.Ellipse(20f, 17f, 3f, 2f, C(96, 176, 84));
        return px;
    }

    // ------------------------------------------------------------------ fundo

    private static Px Sky()
    {
        var px = new Px(2, 64);
        var top = new Color(70f / 255f, 140f / 255f, 225f / 255f);
        var bottom = new Color(185f / 255f, 228f / 255f, 250f / 255f);
        for (int y = 0; y < 64; y++)
        {
            Color c = Color.Lerp(top, bottom, y / 63f);
            px.Rect(0, y, 2, 1, (Color32)c);
        }
        return px;
    }

    private static Px Sun()
    {
        var px = new Px(32, 32);
        px.Ellipse(16f, 16f, 15f, 15f, C(255, 240, 150, 70));
        px.Ellipse(16f, 16f, 10f, 10f, C(255, 225, 90), C(255, 190, 60));
        return px;
    }

    private static Px Clouds()
    {
        var px = new Px(256, 48);
        float[][] centers = { new[] { 40f, 24f }, new[] { 110f, 16f }, new[] { 180f, 28f }, new[] { 228f, 18f } };
        foreach (var c in centers)
        {
            px.Blob(new[]
            {
                new[] { c[0] - 10f, c[1], 8f, 6f },
                new[] { c[0], c[1] - 4f, 10f, 8f },
                new[] { c[0] + 10f, c[1], 8f, 6f },
                new[] { c[0], c[1] + 2f, 18f, 6f },
            }, C(255, 255, 255), C(206, 226, 246));
        }
        return px;
    }

    /// <summary>Colinas que se repetem sem emenda (frequencias inteiras em 256 px).</summary>
    private static Px Hills(int baseHeight, float amp1, float amp2, int freq1, int freq2, float phase,
                            Color32 fill, Color32 edge)
    {
        const int w = 256, h = 160;
        var px = new Px(w, h);
        for (int x = 0; x < w; x++)
        {
            float a = x / (float)w * Mathf.PI * 2f;
            int height = Mathf.RoundToInt(baseHeight + amp1 * Mathf.Sin(a * freq1 + phase) + amp2 * Mathf.Sin(a * freq2));
            height = Mathf.Clamp(height, 1, h);
            for (int y = 0; y < height; y++)
                px.Set(x, y == height - 1 ? h - height : h - 1 - y, fill);
            px.Set(x, h - height, edge);
        }
        return px;
    }
}
