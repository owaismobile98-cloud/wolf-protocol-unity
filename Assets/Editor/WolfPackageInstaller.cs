// Resolves package versions via Package Manager (no hand-pinned legacy versions).
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[InitializeOnLoad]
public static class WolfPackageInstaller
{
    // DISABLED on Unity 6.5 (6000.5): inputsystem / cinemachine / ai.assistant(MCP) all fail to
    // compile here (obsolete-GetInstanceID hard error). Auto-adding them re-breaks the build every
    // editor load. Use legacy Input + a simple follow camera for now; re-enable these on Unity 6.0 LTS.
    static readonly string[] Required = { };

    static ListRequest _list;
    static AddRequest _add;
    static int _next;

    static WolfPackageInstaller()
    {
        EditorApplication.delayCall += Begin;
    }

    static void Begin()
    {
        _list = Client.List(offlineMode: false);
        EditorApplication.update += PollList;
    }

    static void PollList()
    {
        if (!_list.IsCompleted) return;
        EditorApplication.update -= PollList;
        if (_list.Status != StatusCode.Success)
        {
            UnityEngine.Debug.LogWarning("[Wolf] Package list failed — open Package Manager manually.");
            return;
        }

        var installed = new HashSet<string>();
        foreach (var p in _list.Result)
            installed.Add(p.name);

        _next = 0;
        TryAddNext(installed);
    }

    static void TryAddNext(HashSet<string> installed)
    {
        while (_next < Required.Length && installed.Contains(Required[_next]))
            _next++;

        if (_next >= Required.Length) return;

        var pkg = Required[_next++];
        UnityEngine.Debug.Log($"[Wolf] Adding package via PM: {pkg}");
        _add = Client.Add(pkg);
        EditorApplication.update += PollAdd;
    }

    static void PollAdd()
    {
        if (!_add.IsCompleted) return;
        EditorApplication.update -= PollAdd;
        if (_add.Status == StatusCode.Success)
            UnityEngine.Debug.Log($"[Wolf] Installed {_add.Result.name}@{_add.Result.version}");
        else
            UnityEngine.Debug.LogWarning($"[Wolf] Could not add package: {_add.Error?.message}");

        var installed = new HashSet<string>();
        if (_add.Status == StatusCode.Success)
            installed.Add(_add.Result.name);
        TryAddNext(installed);
    }
}
#endif
