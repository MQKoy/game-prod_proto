using System.Collections.Generic;
using UnityEngine;

namespace BuildATower
{
    public enum SectionType { Arrow, Spear, Flame, Cannon, Armor, Regeneration, Rage, Time, Electric }
    public enum SectionCategory { Offensive, Defensive, Utility }
    public enum UpgradeId { Arrow, Spear, Flame, Cannon, Armor, Regeneration, Rage, Time, Electric, OrbLaser, Resources }
    public enum EnemySize { Small, Medium, Large }

    public static class Cfg
    {
        public const float Ppu = 20f;
        public const float AoeUnit = 0.5f;
        public const float TowerWidth = 2.8f;
        public const float SectionHeight = 1.8f;
        public const float FoundationHeight = 1.0f;
        public const float CrownHeight = 1.5f;
        public const float CameraSize = 10f;
        public const float SpawnDistance = 32f;
        public const float TowerRange = 24f;
        public const float EnemySpeedUnit = 0.12f;
        public const int GroundLanes = 3;

        public static readonly bool LaserHoldToFire = false;
        public static readonly bool LaserStartsOn = false;

        public const int StartBlocks = 30;
        public const float StartGraceSeconds = 6f;
        public const float BaseTowerHp = 150f;
        public const float HpPerSection = 25f;
        public const float SpawnRateScale = 0.25f;
        public const float EnemyHpScale = 0.7f;
        public const float TowerDamageScale = 1.5f;
        public const float RewardScale = 1.5f;
        public const float UpgradeCostGrowth = 1.2f;
        public const float MaxArmor = 0.8f;
        public const int MaxEnemies = 250;
    }

    public class SectionDef
    {
        public SectionType type;
        public SectionCategory category;
        public string name, blurb, upgradeInfo;
        public int baseCharges, baseCost, unlockLevel, baseUpgradeExp;
        public bool scalesWithLevel;
        public float baseDamage, baseRate, dmgPerLevel, ratePerLevel;
        public float baseDuration, baseCooldown;
        public Color color;
    }

    public class EnemyDef
    {
        public EnemySize size;
        public int tier;
        public bool flying;
        public float hp, dmg, rangeUnits, speedPts, rate, radius;
        public string rangeName;
        public int exp, blocks;
        public Color color;
    }

    public static class Defs
    {
        public static readonly SectionDef[] Sections =
        {
            new SectionDef { type = SectionType.Arrow, category = SectionCategory.Offensive, name = "Arrow",
                blurb = "Shoots arrows at the nearest enemies", upgradeInfo = "+0.1 atk/s, +2 dmg. Every 5: +1 arrow, +1 charge",
                baseCharges = 3, baseCost = 15, unlockLevel = 1, baseUpgradeExp = 20, scalesWithLevel = true,
                baseDamage = 5f, baseRate = 1f, dmgPerLevel = 2f, ratePerLevel = 0.1f, color = new Color(0.85f, 0.7f, 0.35f) },

            new SectionDef { type = SectionType.Spear, category = SectionCategory.Offensive, name = "Spear",
                blurb = "Piercing melee stab, hits enemies at its height", upgradeInfo = "+0.1 atk/s, +2 dmg. Every 5: +2 reach, +1 charge",
                baseCharges = 3, baseCost = 20, unlockLevel = 5, baseUpgradeExp = 25, scalesWithLevel = true,
                baseDamage = 3f, baseRate = 0.33f, dmgPerLevel = 2f, ratePerLevel = 0.1f, color = new Color(0.8f, 0.8f, 0.9f) },

            new SectionDef { type = SectionType.Flame, category = SectionCategory.Offensive, name = "Flame",
                blurb = "3s burst of piercing fire, then cooldown", upgradeInfo = "+0.1 atk/s, +1 dmg, -0.1s cooldown. Every 5: +2 area, +1 charge",
                baseCharges = 2, baseCost = 30, unlockLevel = 15, baseUpgradeExp = 30, scalesWithLevel = true,
                baseDamage = 2f, baseRate = 2f, dmgPerLevel = 1f, ratePerLevel = 0.1f, baseDuration = 3f, baseCooldown = 5f,
                color = new Color(1f, 0.5f, 0.1f) },

            new SectionDef { type = SectionType.Cannon, category = SectionCategory.Offensive, name = "Cannon",
                blurb = "Slow shot that explodes on impact", upgradeInfo = "+0.1 atk/s, +1 dmg. Every 5: +2 area, +1 charge",
                baseCharges = 1, baseCost = 50, unlockLevel = 20, baseUpgradeExp = 50, scalesWithLevel = true,
                baseDamage = 15f, baseRate = 0.2f, dmgPerLevel = 1f, ratePerLevel = 0.1f, color = new Color(0.35f, 0.35f, 0.4f) },

            new SectionDef { type = SectionType.Armor, category = SectionCategory.Defensive, name = "Armor",
                blurb = "Reduces incoming damage", upgradeInfo = "+0.1% damage reduction. Every 5: +1 charge",
                baseCharges = 1, baseCost = 35, unlockLevel = 10, baseUpgradeExp = 50, scalesWithLevel = true,
                color = new Color(0.45f, 0.6f, 0.85f) },

            new SectionDef { type = SectionType.Regeneration, category = SectionCategory.Defensive, name = "Regeneration",
                blurb = "Heals the tower over time", upgradeInfo = "+0.1 HP/s. Every 5: +1 charge",
                baseCharges = 1, baseCost = 65, unlockLevel = 15, baseUpgradeExp = 70, scalesWithLevel = true,
                color = new Color(0.4f, 0.85f, 0.45f) },

            new SectionDef { type = SectionType.Rage, category = SectionCategory.Utility, name = "Rage",
                blurb = "Offense x1.5 speed & damage for a while", upgradeInfo = "+0.5s duration. Every 5: -10s cooldown",
                baseCharges = 1, baseCost = 100, unlockLevel = 10, baseUpgradeExp = 150, baseDuration = 5f, baseCooldown = 180f,
                color = new Color(0.9f, 0.25f, 0.25f) },

            new SectionDef { type = SectionType.Time, category = SectionCategory.Utility, name = "Time",
                blurb = "Slows enemy movement and attack speed", upgradeInfo = "+0.5s duration. Every 5: -10s cooldown",
                baseCharges = 1, baseCost = 150, unlockLevel = 15, baseUpgradeExp = 200, baseDuration = 3f, baseCooldown = 240f,
                color = new Color(0.4f, 0.85f, 0.95f) },

            new SectionDef { type = SectionType.Electric, category = SectionCategory.Utility, name = "Electric",
                blurb = "Stuns every enemy", upgradeInfo = "+0.5s duration. Every 5: -10s cooldown",
                baseCharges = 1, baseCost = 200, unlockLevel = 25, baseUpgradeExp = 250, baseDuration = 2f, baseCooldown = 300f,
                color = new Color(1f, 0.9f, 0.2f) },
        };

        public static SectionDef Get(SectionType t) { return Sections[(int)t]; }

        public static int BaseUpgradeExp(UpgradeId id)
        {
            if (id == UpgradeId.OrbLaser) return 50;
            if (id == UpgradeId.Resources) return 100;
            return Sections[(int)id].baseUpgradeExp;
        }

        public static string UpgradeName(UpgradeId id)
        {
            if (id == UpgradeId.OrbLaser) return "Orb Laser";
            if (id == UpgradeId.Resources) return "Resources";
            return Sections[(int)id].name;
        }

        public static string UpgradeInfo(UpgradeId id)
        {
            if (id == UpgradeId.OrbLaser) return "+0.1 atk/s, +1 dmg. Every 5: +2 area";
            if (id == UpgradeId.Resources) return "+1 Blocks per kill. Every 5: +1 EXP per kill";
            return Sections[(int)id].upgradeInfo;
        }
        public static readonly EnemyDef[] Enemies = BuildEnemies();

        public static EnemyDef Enemy(EnemySize size, int tier) { return Enemies[(int)size * 3 + (tier - 1)]; }

        static EnemyDef[] BuildEnemies()
        {
            var l = new List<EnemyDef>();
            Add(l, EnemySize.Small,  1, false,  25,   1, "direct",  1,  1);
            Add(l, EnemySize.Small,  2, true,   35,   1, "far",     3,  1);
            Add(l, EnemySize.Small,  3, true,   40,   5, "direct",  9,  4);
            Add(l, EnemySize.Medium, 1, false,  35,  10, "medium",  4,  2);
            Add(l, EnemySize.Medium, 2, false,  50,  12, "close",   6,  3);
            Add(l, EnemySize.Medium, 3, true,   60,  15, "far",    16,  8);
            Add(l, EnemySize.Large,  1, false,  80,  25, "direct", 10,  5);
            Add(l, EnemySize.Large,  2, true,   90,  30, "direct", 20, 10);
            Add(l, EnemySize.Large,  3, true,  100,  35, "close",  40, 20);
            return l.ToArray();
        }

        static void Add(List<EnemyDef> l, EnemySize size, int tier, bool flying, float hp, float dmg, string range, int exp, int blocks)
        {
            var d = new EnemyDef { size = size, tier = tier, flying = flying, hp = hp, dmg = dmg, rangeName = range, exp = exp, blocks = blocks };
            switch (range)
            {
                case "direct": d.rangeUnits = 0.15f; break;
                case "close": d.rangeUnits = 2.5f; break;
                case "medium": d.rangeUnits = 5f; break;
                default: d.rangeUnits = 8f; break; // far
            }
            switch (size)
            {
                case EnemySize.Small: d.speedPts = 25f; d.rate = 1f; d.radius = 0.5f; break;
                case EnemySize.Medium: d.speedPts = 20f; d.rate = 0.8f; d.radius = 0.7f; break;
                default: d.speedPts = 15f; d.rate = 0.5f; d.radius = 1.1f; break;
            }
            d.color = tier == 1 ? new Color(0.45f, 0.8f, 0.35f) : tier == 2 ? new Color(0.35f, 0.6f, 0.95f) : new Color(0.9f, 0.3f, 0.3f);
            l.Add(d);
        }
    }

    public static class Util
    {
        public static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 1e-6f) return (p - a).magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            return (p - (a + ab * t)).magnitude;
        }
    }
}
