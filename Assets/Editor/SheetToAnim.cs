// WOLF PROTOCOL — "drop a sheet → game-ready animated character" importer.
// Put sprite sheets in  Assets/Incoming/  named:   <char>__<anim>__<cols>x<rows>__<fps>.png
//   e.g.  razor__run__8x1__14.png   razor__idle__6x1__6.png   trooper__attack__6x1__16.png
// Then:  Window > WOLF > Build Characters From Incoming   (or batchmode -executeMethod
// SheetToAnim.BuildIncoming  — driven by the desktop shortcut / an AI agent, fully turnkey).
// For each char it slices every sheet, builds an AnimationClip per anim, an AnimatorController with
// those states, and a prefab Assets/Prefabs/<char>.prefab (SpriteRenderer + Animator). NO rigging.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class SheetToAnim
{
    const string Incoming = "Assets/Incoming";
    const string ClipsDir = "Assets/Generated/Clips";
    const string CtrlDir  = "Assets/Generated/Controllers";
    const string PrefabDir = "Assets/Prefabs";
    // loop these; one-shots (attack/hurt/death/shoot) play once.
    static readonly HashSet<string> Looping = new() { "idle", "run", "walk" };
    // normalize pack anim names to our state vocabulary.
    static readonly Dictionary<string, string> Alias = new() {
        {"punch","attack"}, {"kick","attack"}, {"gethit","hurt"}, {"get_hit","hurt"},
        {"knockdown","death"}, {"fall","hurt"}, {"defend","block"}
    };

    [MenuItem("Window/WOLF/Build Characters From Incoming")]
    public static void BuildIncoming()
    {
        if (!Directory.Exists(Incoming)) { Directory.CreateDirectory(Incoming); AssetDatabase.Refresh();
            Debug.LogWarning("[SheetToAnim] Created " + Incoming + " — drop sheets there and rerun."); return; }
        foreach (var d in new[] { ClipsDir, CtrlDir, PrefabDir }) Directory.CreateDirectory(d);

        var sheets = Directory.GetFiles(Incoming, "*.png").Where(f => f.Contains("__")).ToArray();
        var byChar = new Dictionary<string, List<string>>();
        foreach (var f in sheets) {
            var name = Path.GetFileNameWithoutExtension(f);
            var ch = name.Split(new[] { "__" }, System.StringSplitOptions.None)[0];
            if (!byChar.ContainsKey(ch)) byChar[ch] = new List<string>();
            byChar[ch].Add(f);
        }
        if (byChar.Count == 0) { Debug.LogWarning("[SheetToAnim] No '<char>__<anim>__CxR__fps.png' sheets in " + Incoming); return; }

        foreach (var kv in byChar) BuildCharacter(kv.Key, kv.Value);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log($"[SheetToAnim] Built {byChar.Count} character(s).");
    }

    static void BuildCharacter(string ch, List<string> files)
    {
        var ctrlPath = $"{CtrlDir}/{ch}.controller";
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm = ctrl.layers[0].stateMachine;
        AnimationClip idleClip = null;

        foreach (var file in files) {
            var parts = Path.GetFileNameWithoutExtension(file).Split(new[] { "__" }, System.StringSplitOptions.None);
            if (parts.Length < 3) { Debug.LogWarning("skip (bad name): " + file); continue; }
            string anim = parts[1].ToLower();
            if (Alias.TryGetValue(anim, out var a)) anim = a;
            var grid = parts[2].ToLower().Split('x');
            int cols = int.Parse(grid[0]), rows = grid.Length > 1 ? int.Parse(grid[1]) : 1;
            float fps = parts.Length > 3 && float.TryParse(parts[3], out var f) ? f : 12f;

            var sprites = SliceSheet(file, cols, rows);
            if (sprites.Count == 0) { Debug.LogWarning("no sprites sliced: " + file); continue; }
            var clip = BuildClip(ch, anim, sprites, fps, Looping.Contains(anim));
            var st = sm.AddState(anim);
            st.motion = clip;
            if (anim == "idle") { idleClip = clip; sm.defaultState = st; }
        }
        if (idleClip == null && sm.states.Length > 0) sm.defaultState = sm.states[0].state;

        // Prefab: SpriteRenderer + Animator(controller).
        var go = new GameObject(ch);
        go.AddComponent<SpriteRenderer>();
        var an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        var prefabPath = $"{PrefabDir}/{ch}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[SheetToAnim] {ch}: {files.Count} anim(s) -> {prefabPath}");
    }

    static List<Sprite> SliceSheet(string file, int cols, int rows)
    {
        var importer = AssetImporter.GetAtPath(file) as TextureImporter;
        if (importer == null) return new List<Sprite>();
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = (cols * rows > 1) ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        if (cols * rows > 1) {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
            int sw = tex.width / cols, sh = tex.height / rows;
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer); dp.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    rects.Add(new SpriteRect {
                        name = $"{Path.GetFileNameWithoutExtension(file)}_{r * cols + c}",
                        alignment = SpriteAlignment.BottomCenter,
                        rect = new Rect(c * sw, (rows - 1 - r) * sh, sw, sh)
                    });
            dp.SetSpriteRects(rects.ToArray()); dp.Apply();
        }
        EditorUtility.SetDirty(importer); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>()
            .OrderBy(s => { var p = s.name.LastIndexOf('_'); return p >= 0 && int.TryParse(s.name[(p+1)..], out var n) ? n : 0; })
            .ToList();
    }

    static AnimationClip BuildClip(string ch, string anim, List<Sprite> sprites, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = new EditorCurveBinding {
            type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        if (loop) { var s = AnimationUtility.GetAnimationClipSettings(clip); s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s); }
        var path = $"{ClipsDir}/{ch}_{anim}.anim";
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
