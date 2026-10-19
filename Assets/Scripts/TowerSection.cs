using UnityEngine;

namespace BuildATower
{
    public class TowerSection : MonoBehaviour
    {
        public SectionDef def;
        public int Index;
        public Vector3 Home;
        public float Cooldown;

        SpriteRenderer sr, outline, flameSr;
        Sprite[] frames;
        float atk, flameActive, flameCd, flameTick;
        int flameSide = 1;

        GameManager G { get { return GameManager.I; } }
        int Lv { get { return G.UpLevel[(int)def.type]; } }
        float RageMul { get { return def.category == SectionCategory.Offensive && G.RageTime > 0f ? 1.5f : 1f; } }

        public float Damage { get { return (def.baseDamage + def.dmgPerLevel * (Lv - 1)) * RageMul * (def.category == SectionCategory.Offensive ? Cfg.TowerDamageScale : 1f); } }
        public float Rate { get { return (def.baseRate + def.ratePerLevel * (Lv - 1)) * RageMul; } }
        public float Duration { get { return def.baseDuration + 0.5f * (Lv - 1); } }
        public float CooldownTotal { get { return Mathf.Max(30f, def.baseCooldown - 10f * (Lv / 5)); } }
        float FlameCooldownTotal { get { return Mathf.Max(1f, def.baseCooldown - 0.1f * (Lv - 1)); } }
        float Aoe(float basePts) { return (basePts + 2f * (Lv / 5)) * Cfg.AoeUnit; }

        public void Init(SectionDef d)
        {
            def = d;
            sr = gameObject.AddComponent<SpriteRenderer>();
            frames = Art.SectionFrames(d);
            sr.sprite = frames[0];
            sr.sharedMaterial = Art.Mat;
            sr.sortingOrder = 1;

            outline = Art.NewSprite("Selection", Art.Outline, Vector3.zero, 2, transform);
            outline.transform.localScale = Vector3.one * 1.04f;
            outline.enabled = false;

            if (d.type == SectionType.Flame)
            {
                flameSr = Art.NewSprite("FlameStream", Art.WhiteL, Vector3.zero, 27, transform);
                flameSr.color = new Color(1f, 0.55f, 0.1f, 0.85f);
                flameSr.enabled = false;
            }
        }

        public void SetSelected(bool on) { if (outline != null) outline.enabled = on; }
        public void SetDragging(bool on) { sr.sortingOrder = on ? 20 : 1; outline.sortingOrder = on ? 21 : 2; }

        void Update()
        {
            if (G == null || !G.Running) return;
            float dt = Time.deltaTime;

            switch (def.type)
            {
                case SectionType.Arrow: UpdateArrow(dt); break;
                case SectionType.Spear: UpdateSpear(dt); break;
                case SectionType.Flame: UpdateFlame(dt); break;
                case SectionType.Cannon: UpdateCannon(dt); break;
                case SectionType.Rage:
                case SectionType.Time:
                case SectionType.Electric: UpdateUtility(dt); break;
            }

            sr.sprite = frames[(int)(Time.time * 2.2f + Index * 0.5f) % frames.Length];

            Color c = RageMul > 1f ? new Color(1f, 0.82f, 0.7f) : Color.white;
            if (G.HitFlash > 0f) c = Color.Lerp(c, new Color(1f, 0.4f, 0.4f), G.HitFlash / 0.12f);
            sr.color = c;
        }


        void UpdateArrow(float dt)
        {
            atk += dt;
            float iv = 1f / Rate;
            if (atk < iv) return;
            int arrows = 1 + Lv / 5;
            var targets = Enemy.Nearest(Home, Cfg.TowerRange, arrows);
            if (targets.Count == 0) { atk = iv; return; }
            atk = 0f;
            for (int i = 0; i < arrows; i++)
            {
                var t = targets[i % targets.Count];
                Vector3 from = Home + new Vector3(0f, (i - (arrows - 1) * 0.5f) * 0.15f, 0f);
                Projectile.Arrow(from, t, Damage);
            }
        }

        void UpdateCannon(float dt)
        {
            atk += dt;
            float iv = 1f / Rate;
            if (atk < iv) return;
            var targets = Enemy.Nearest(Home, Cfg.TowerRange + 2f, 1);
            if (targets.Count == 0) { atk = iv; return; }
            atk = 0f;
            Projectile.Cannon(Home, targets[0], Damage, Aoe(3f));
        }

        bool InBand(Enemy e, int side, float len, float halfBand)
        {
            float ex = e.transform.position.x * side;
            if (ex <= 0f) return false;
            if (ex - Cfg.TowerWidth * 0.5f - e.radius > len) return false;
            return e.LogicY + e.HalfH >= Home.y - halfBand && e.LogicY - e.HalfH <= Home.y + halfBand;
        }

        int PickSide(float len, float halfBand)
        {
            float best = float.MaxValue;
            int side = 0;
            for (int i = 0; i < Enemy.All.Count; i++)
            {
                var e = Enemy.All[i];
                if (e.IsDead) continue;
                int s = e.transform.position.x > 0f ? 1 : -1;
                if (!InBand(e, s, len, halfBand)) continue;
                float d = Mathf.Abs(e.transform.position.x);
                if (d < best) { best = d; side = s; }
            }
            return side;
        }

        void HitBand(int side, float len, float halfBand, float damage)
        {
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var e = Enemy.All[i];
                if (e.IsDead) continue;
                if (InBand(e, side, len, halfBand)) e.Hurt(damage);
            }
        }

        void UpdateSpear(float dt)
        {
            atk += dt;
            float iv = 1f / Rate;
            if (atk < iv) return;
            float len = Aoe(5f), band = Cfg.SectionHeight * 0.5f;
            int side = PickSide(len, band);
            if (side == 0) { atk = iv; return; }
            atk = 0f;
            HitBand(side, len, band, Damage);
            Vector3 a = Home + new Vector3(side * Cfg.TowerWidth * 0.5f, 0f, 0f);
            FxItem.Beam(a, a + new Vector3(side * len, 0f, 0f), new Color(0.92f, 0.92f, 1f), 0.16f, 0.18f);
        }

        void UpdateFlame(float dt)
        {
            float len = Aoe(6f);
            float band = Cfg.SectionHeight * 0.5f;

            if (flameActive > 0f)
            {
                flameActive -= dt;
                flameTick += dt;
                float iv = 1f / Rate;
                while (flameTick >= iv) { flameTick -= iv; HitBand(flameSide, len, band, Damage); }

                flameSr.enabled = true;
                flameSr.transform.localPosition = new Vector3(flameSide * Cfg.TowerWidth * 0.5f, 0f, 0f);
                flameSr.transform.localScale = new Vector3(flameSide * len, 0.9f + 0.25f * Mathf.Sin(Time.time * 40f), 1f);

                if (flameActive <= 0f) { flameSr.enabled = false; flameCd = FlameCooldownTotal; }
                return;
            }

            if (flameCd > 0f) { flameCd -= dt; return; }

            int side = PickSide(len, band);
            if (side != 0)
            {
                flameSide = side;
                flameActive = def.baseDuration;
                flameTick = 1f / Rate;
            }
        }

        void UpdateUtility(float dt)
        {
            if (Cooldown > 0f) { Cooldown -= dt; return; }

            int n = Enemy.All.Count;
            bool go = false;
            switch (def.type)
            {
                case SectionType.Rage: go = n >= 3; break;
                case SectionType.Time: go = n >= 5; break;
                case SectionType.Electric: go = n >= 7 || (n >= 3 && G.Hp < G.MaxHp * 0.5f); break;
            }
            if (go) Activate();
        }

        public void Activate()
        {
            if (def.category != SectionCategory.Utility || Cooldown > 0f || !G.Running) return;
            Cooldown = CooldownTotal;
            float d = Duration;
            switch (def.type)
            {
                case SectionType.Rage: G.RageTime = Mathf.Max(G.RageTime, d); break;
                case SectionType.Time: G.SlowTime = Mathf.Max(G.SlowTime, d); break;
                case SectionType.Electric: G.StunTime = Mathf.Max(G.StunTime, d); break;
            }
            FxItem.Burst(Home, 3.5f, def.color, 0.5f);
        }

        public string Describe()
        {
            string s = def.name + " Section   Lv " + Lv + "\n";
            switch (def.type)
            {
                case SectionType.Arrow:
                    return s + "Damage " + Damage.ToString("0.#") + "   " + Rate.ToString("0.##") + " attacks/s   " + (1 + Lv / 5) + " arrow(s) per shot";
                case SectionType.Spear:
                    return s + "Damage " + Damage.ToString("0.#") + "   " + Rate.ToString("0.##") + " attacks/s   reach " + Aoe(5f).ToString("0.#") + " (hits enemies at this height)";
                case SectionType.Flame:
                    return s + "Damage " + Damage.ToString("0.#") + "/tick   " + Rate.ToString("0.##") + " ticks/s   " + def.baseDuration.ToString("0") + "s burst, " + FlameCooldownTotal.ToString("0.#") + "s cooldown";
                case SectionType.Cannon:
                    return s + "Damage " + Damage.ToString("0.#") + "   " + Rate.ToString("0.##") + " attacks/s   blast radius " + Aoe(3f).ToString("0.#");
                case SectionType.Armor:
                    return s + "Reduces incoming damage by " + ((0.01f + 0.001f * (Lv - 1)) * 100f).ToString("0.#") + "%";
                case SectionType.Regeneration:
                    return s + "Heals " + (0.001f * G.MaxHp + 0.1f * (Lv - 1)).ToString("0.##") + " HP per second";
                default:
                    string status = Cooldown > 0f ? "recharging " + Mathf.CeilToInt(Cooldown) + "s" : "READY";
                    return s + "Lasts " + Duration.ToString("0.#") + "s   cooldown " + CooldownTotal.ToString("0") + "s   (" + status + ")";
            }
        }
    }
}
