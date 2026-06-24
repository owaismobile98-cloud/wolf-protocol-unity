using UnityEngine;

namespace Wolf.Protocol
{
    [System.Serializable]
    public struct WeaponDef
    {
        public string Name;
        public float Damage;
        public float Rate;
        public bool Auto;
        public int Mag;
        public int Reserve;
        public float Spread;
        public int Pellets;
        public float Speed;
        public float Reload;
        public Feel.PauseTier PauseTier;
        public Feel.StunLevel StunLevel;
    }

    public static class WeaponTable
    {
        public static readonly WeaponDef[] All =
        {
            new() { Name = "PISTOL", Damage = 20f, Rate = 0.26f, Auto = false, Mag = 12, Reserve = 96, Spread = 0.02f, Pellets = 1, Speed = 780f, Reload = 0.9f, PauseTier = Feel.PauseTier.Light, StunLevel = Feel.StunLevel.Normal },
            new() { Name = "RIFLE", Damage = 14f, Rate = 0.10f, Auto = true, Mag = 30, Reserve = 210, Spread = 0.045f, Pellets = 1, Speed = 860f, Reload = 1.3f, PauseTier = Feel.PauseTier.Light, StunLevel = Feel.StunLevel.Normal },
            new() { Name = "SMG", Damage = 9f, Rate = 0.06f, Auto = true, Mag = 40, Reserve = 280, Spread = 0.08f, Pellets = 1, Speed = 760f, Reload = 1.1f, PauseTier = Feel.PauseTier.Light, StunLevel = Feel.StunLevel.Normal },
            new() { Name = "SHOTGUN", Damage = 7f, Rate = 0.70f, Auto = false, Mag = 6, Reserve = 42, Spread = 0.22f, Pellets = 8, Speed = 680f, Reload = 1.5f, PauseTier = Feel.PauseTier.Heavy, StunLevel = Feel.StunLevel.High },
        };
    }
}
