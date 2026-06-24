// WOLF PROTOCOL — Unity Editor asset-generation window.
// Shells out to the VERIFIED Python engine
// (D:\Pro Jack\game-studio\unity-bridge\gen_sprites.py : WaveSpeed -> cloud ComfyUI -> Pollinations),
// then imports the PNG as a 2D Sprite and optionally grid-slices it into a sheet.
// Menu: Window > WOLF > Asset Generator.
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class WolfAssetGen : EditorWindow
{
    const string GenScript = @"D:\Pro Jack\game-studio\unity-bridge\gen_sprites.py";
    string prompt = "pixel art cyberpunk crate prop, flat solid grey background, centered, game asset";
    string assetName = "crate";
    int cols = 1, rows = 1;
    string comfyUrl = ""; // set to the Kaggle/Colab ComfyUI tunnel for limitless LoRA sheets

    [MenuItem("Window/WOLF/Asset Generator")]
    public static void Open() => GetWindow<WolfAssetGen>("WOLF Asset Generator");

    void OnGUI()
    {
        GUILayout.Label("AI Asset Generator (WaveSpeed / ComfyUI / Pollinations)", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextField("Prompt", prompt);
        assetName = EditorGUILayout.TextField("Asset name", assetName);
        cols = EditorGUILayout.IntField("Sheet cols", cols);
        rows = EditorGUILayout.IntField("Sheet rows", rows);
        comfyUrl = EditorGUILayout.TextField("ComfyUI URL (optional)", comfyUrl);
        if (GUILayout.Button("Generate + Import"))
            Generate();
    }

    void Generate()
    {
        string dir = "Assets/Sprites";
        Directory.CreateDirectory(dir);
        string outPath = Path.GetFullPath(Path.Combine(dir, assetName + ".png"));

        var args = $"\"{GenScript}\" --prompt \"{prompt}\" --out \"{outPath}\" --cols {cols} --rows {rows}";
        if (!string.IsNullOrEmpty(comfyUrl)) args += $" --comfy-url {comfyUrl}";

        var psi = new ProcessStartInfo("python", args)
        {
            UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true
        };
        UnityEngine.Debug.Log("[WolfAssetGen] python " + psi.Arguments);
        using (var p = Process.Start(psi))
        {
            string so = p.StandardOutput.ReadToEnd();
            string se = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (!string.IsNullOrEmpty(so)) UnityEngine.Debug.Log("[gen] " + so);
            if (p.ExitCode != 0) { UnityEngine.Debug.LogError("[gen] failed: " + se); return; }
        }

        string localPath = $"{dir}/{assetName}.png";
        AssetDatabase.ImportAsset(localPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(localPath) as TextureImporter;
        if (importer == null) { UnityEngine.Debug.LogError("import failed: " + localPath); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;                 // crisp pixel art
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteImportMode = (cols * rows > 1) ? SpriteImportMode.Multiple : SpriteImportMode.Single;

        if (cols * rows > 1)
            SliceGrid(importer, localPath, cols, rows);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        UnityEngine.Debug.Log($"[WolfAssetGen] imported {localPath} as {(cols*rows>1 ? $"{cols}x{rows} sheet" : "sprite")}.");
    }

    static void SliceGrid(TextureImporter importer, string localPath, int cols, int rows)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(localPath);
        if (tex == null) return;
        int sw = tex.width / cols, sh = tex.height / rows;
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                rects.Add(new SpriteRect {
                    name = $"{Path.GetFileNameWithoutExtension(localPath)}_{r}_{c}",
                    alignment = SpriteAlignment.Center,
                    rect = new Rect(c * sw, (rows - 1 - r) * sh, sw, sh)   // Unity origin = bottom-left
                });
        dp.SetSpriteRects(rects.ToArray());
        dp.Apply();
    }
}
