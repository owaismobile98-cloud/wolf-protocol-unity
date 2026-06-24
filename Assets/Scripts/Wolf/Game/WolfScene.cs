using System.Collections.Generic;
using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Runtime scene builder: ground, player prefab, enemy prefabs, camera rig, Y-sort.
    /// </summary>
    public static class WolfScene
    {
        public struct Config
        {
            public Vector2 ArenaCenter;
            public Vector2 ArenaSize;
            public GameObject PlayerPrefab;
            public GameObject EnemyPrefab;
            public int EnemyCount;
            public CameraRig.Mode CameraMode;
            public float CameraOrthographicSize;
            public Transform Parent;
        }

        public struct Result
        {
            public GameObject Root;
            public GameObject Floor;
            public PlayerController Player;
            public CameraRig CameraRig;
            public IReadOnlyList<EnemyController> Enemies;
        }

        public static Result Build(Config config)
        {
            var root = new GameObject("WolfScene");
            if (config.Parent != null)
                root.transform.SetParent(config.Parent);

            var floor = BuildFloor(root.transform, config.ArenaCenter, config.ArenaSize);
            var player = SpawnPlayer(root.transform, config);
            var rig = SetupCamera(root.transform, player, config);
            var enemies = SpawnEnemies(root.transform, player, config);

            return new Result
            {
                Root = root,
                Floor = floor,
                Player = player,
                CameraRig = rig,
                Enemies = enemies,
            };
        }

        static GameObject BuildFloor(Transform parent, Vector2 center, Vector2 size)
        {
            var floor = new GameObject("Ground");
            floor.transform.SetParent(parent);
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.White;
            sr.color = new Color(0.12f, 0.13f, 0.16f);
            sr.sortingOrder = YSorter.OrderForPosition(new Vector3(center.x, center.y, 10f), -1000);
            floor.transform.localScale = new Vector3(size.x, size.y, 1f);
            floor.transform.position = new Vector3(center.x, center.y, 10f);
            var sorter = floor.AddComponent<YSorter>();
            sorter.IsStatic = true;
            return floor;
        }

        static PlayerController SpawnPlayer(Transform parent, Config config)
        {
            GameObject go;
            if (config.PlayerPrefab != null)
            {
                go = Object.Instantiate(config.PlayerPrefab, parent);
                go.name = "Player";
            }
            else
            {
                go = new GameObject("Player");
                go.transform.SetParent(parent);
            }

            go.transform.position = config.ArenaCenter;

            var player = go.GetComponent<PlayerController>();
            if (player == null) player = go.AddComponent<PlayerController>();

            EnsureYSort(go);
            return player;
        }

        static CameraRig SetupCamera(Transform parent, PlayerController player, Config config)
        {
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(parent);
            camGo.AddComponent<Camera>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.CurrentMode = config.CameraMode;
            rig.FixedOrthographicSize = config.CameraOrthographicSize > 0f
                ? config.CameraOrthographicSize
                : 200f;
            rig.SideScrollLockX = config.ArenaCenter.x;
            rig.BaseOffset = new Vector3(0f, 0f, -10f);
            rig.BindTarget(player.transform);
            camGo.transform.position = new Vector3(config.ArenaCenter.x, config.ArenaCenter.y, rig.BaseOffset.z);
            player.BindCameraRig(rig);
            return rig;
        }

        static List<EnemyController> SpawnEnemies(Transform parent, PlayerController player, Config config)
        {
            var list = new List<EnemyController>();
            if (config.EnemyCount <= 0) return list;

            for (int i = 0; i < config.EnemyCount; i++)
            {
                GameObject go;
                if (config.EnemyPrefab != null)
                {
                    go = Object.Instantiate(config.EnemyPrefab, parent);
                    go.name = $"Enemy_{i}";
                }
                else
                {
                    go = new GameObject($"Enemy_{i}");
                    go.transform.SetParent(parent);
                }

                float ang = Mathf.PI * 2f * i / config.EnemyCount;
                float dist = Random.Range(240f, 340f);
                go.transform.position = (Vector2)player.transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

                var enemy = go.GetComponent<EnemyController>();
                if (enemy == null) enemy = go.AddComponent<EnemyController>();
                enemy.Configure(EnemyController.Kind.Runner, 0);
                enemy.Target = player.transform;
                EnsureYSort(go);
                list.Add(enemy);
            }

            return list;
        }

        static void EnsureYSort(GameObject go)
        {
            if (go.GetComponent<SpriteRenderer>() != null && go.GetComponent<YSorter>() == null)
                go.AddComponent<YSorter>();
        }
    }
}
