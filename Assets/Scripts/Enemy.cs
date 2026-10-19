using System.Collections.Generic;
using UnityEngine;

namespace BuildATower
{
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public EnemyDef def;
        public float hp, maxHp, dmg, radius;
        public bool IsDead { get { return dead; } }

        public float LogicY { get; private set; }
        public float HalfH { get; private set; }

        EnemyAnim anim;
        SpriteRenderer sr, hpBg, hpFg;
        float atk, baseY, phase, punch, barW, animT, flashT, atkAnimT;
        int side, lane, order;
        bool dead;
        Sprite curFlash;

        public static Enemy Spawn(EnemyDef d, int difficulty)
        {
            var go = new GameObject("Enemy " + d.size + " T" + d.tier);
            go.transform.SetParent(GameManager.I.World, false);
            var e = go.AddComponent<Enemy>();
            e.Init(d, difficulty);
            return e;
        }

        void Init(EnemyDef d, int difficulty)
        {
            def = d;
            anim = Art.Enemy(d);
            float scale = 1f + difficulty / 2f * 0.1f;
            maxHp = hp = d.hp * Cfg.EnemyHpScale * scale;
            dmg = d.dmg * scale;
            radius = d.radius;
            HalfH = anim.height * 0.5f;

            side = Random.value < 0.5f ? -1 : 1;
            float y;
            if (d.flying)
            {
                lane = 0;
                float low = Mathf.Max(2.6f, anim.height);
                float high = Mathf.Max(low + 1.5f, Mathf.Clamp(GameManager.I.TowerTop, 5f, 16f));
                y = Random.Range(low, high);
                LogicY = y;
                order = 22;
            }
            else
            {
                lane = Random.Range(0, Cfg.GroundLanes);   
                LogicY = HalfH;
                y = HalfH - lane * 0.4f;
                order = 10 + lane * 3;
            }
            baseY = y;
            float sx = GameManager.I.SpawnX + Random.Range(0f, 3f);
            transform.position = new Vector3(side * sx, y, 0f);

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = anim.walk[0];
            sr.sharedMaterial = Art.Mat;
            sr.sortingOrder = order;
            sr.flipX = side > 0;                      

            barW = Mathf.Max(0.8f, anim.height * 0.7f);
            float by = HalfH + 0.25f;
            hpBg = Art.NewSprite("HpBg", Art.White, new Vector3(0f, by, 0f), order + 1, transform);
            hpBg.transform.localScale = new Vector3(barW + 0.08f, 0.14f, 1f);
            hpBg.color = new Color(0f, 0f, 0f, 0.75f);
            hpFg = Art.NewSprite("HpFg", Art.WhiteL, new Vector3(-barW * 0.5f, by, 0f), order + 2, transform);
            hpFg.transform.localScale = new Vector3(barW, 0.09f, 1f);
            hpFg.color = new Color(0.4f, 1f, 0.4f);
            hpBg.enabled = hpFg.enabled = false;

            phase = Random.value * 6.28f;
            animT = Random.value * 4f;
            All.Add(this);
        }

        void Update()
        {
            var g = GameManager.I;
            if (g == null || !g.Running || dead) return;
            float dt = Time.deltaTime;

            bool stunned = g.StunTime > 0f;
            bool slowed = g.SlowTime > 0f;

            Vector3 p = transform.position;
            float stopDist = Cfg.TowerWidth * 0.5f + def.rangeUnits + radius * 0.5f;
            float x = Mathf.Abs(p.x);
            bool inRange = x <= stopDist + 0.05f;

            if (!stunned)
            {
                if (!inRange)
                {
                    float spd = def.speedPts;
                    if (slowed) spd = Mathf.Max(spd * 0.2f, spd - 10f);
                    float step = spd * Cfg.EnemySpeedUnit * dt;

                    float limit = stopDist;
                    for (int i = 0; i < All.Count; i++)
                    {
                        var o = All[i];
                        if (o == this || o.dead || o.side != side || o.def.flying != def.flying) continue;
                        float ox = Mathf.Abs(o.transform.position.x);
                        if (ox >= x - 0.001f) continue;
                        if (def.flying)
                        {
                            if (Mathf.Abs(o.baseY - baseY) > (radius + o.radius) * 0.8f) continue;
                        }
                        else if (o.lane != lane) continue;
                        float need = ox + (radius + o.radius) * 0.85f;
                        if (need > limit) limit = need;
                    }
                    p.x = side * Mathf.Max(x - step, Mathf.Min(x, limit));
                    animT += dt * (slowed ? 4f : 8f);
                }
                else
                {
                    float rate = def.rate;
                    if (slowed) rate = Mathf.Max(0.2f, rate - 1f);
                    atk += dt;
                    if (atk >= 1f / rate) { atk = 0f; Attack(); }
                    animT += dt * 3f;}
            }

            p.y = def.flying ? baseY + Mathf.Sin(Time.time * 3f + phase) * 0.18f : baseY;
            transform.position = p;
            flashT = Mathf.Max(0f, flashT - dt);
            atkAnimT = Mathf.Max(0f, atkAnimT - dt);
            int wi = (int)animT % 4;
            Sprite normal, flash;
            if (atkAnimT > 0f)
            {
                int ai = atkAnimT > 0.17f ? 0 : 1;
                normal = anim.atk[ai]; flash = anim.flashAtk[ai];
            }
            else { normal = anim.walk[wi]; flash = anim.flashWalk[wi]; }
            curFlash = flash;
            sr.sprite = flashT > 0f ? flash : normal;
            sr.color = flashT > 0f ? Color.white : stunned ? new Color(1f, 1f, 0.55f) : slowed ? new Color(0.6f, 0.9f, 1f) : Color.white;

            punch = Mathf.Max(0f, punch - dt * 4f);
            transform.localScale = Vector3.one * (1f + punch * 0.12f);
        }

        void Attack()
        {
            var g = GameManager.I;
            g.DamageTower(dmg);
            punch = 1f;
            atkAnimT = 0.34f;
            if (def.rangeUnits > 1f)
            {
                Vector3 from = transform.position;
                Vector3 to = new Vector3(side * Cfg.TowerWidth * 0.5f, Mathf.Clamp(from.y, 0.5f, g.TowerTop), 0f);
                FxItem.Beam(from, to, Color.Lerp(def.color, Color.white, 0.4f), 0.09f, 0.14f);
            }
        }

        public void Hurt(float amount)
        {
            if (dead) return;
            hp -= amount;
            flashT = 0.07f;
            if (!hpFg.enabled) { hpBg.enabled = true; hpFg.enabled = true; }
            hpFg.transform.localScale = new Vector3(barW * Mathf.Clamp01(hp / maxHp), 0.09f, 1f);
            if (hp <= 0f) Die();
        }

        void Die()
        {
            dead = true;
            All.Remove(this);
            GameManager.I.OnEnemyKilled(def);
            if (curFlash == null) curFlash = anim.flashWalk[0];
            Corpse.Spawn(curFlash, transform.position, sr.flipX, def.color, order + 3);
            FxItem.Burst(transform.position, radius * 1.1f, def.color, 0.22f);
            Destroy(gameObject);
        }

        void OnDestroy() { All.Remove(this); }
        public static List<Enemy> Nearest(Vector3 p, float range, int n)
        {
            var res = new List<Enemy>();
            float r2 = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var e = All[i];
                if (e.dead) continue;
                if (((Vector2)(e.transform.position - p)).sqrMagnitude <= r2) res.Add(e);
            }
            res.Sort((a, b) =>
                ((Vector2)(a.transform.position - p)).sqrMagnitude.CompareTo(((Vector2)(b.transform.position - p)).sqrMagnitude));
            if (res.Count > n) res.RemoveRange(n, res.Count - n);
            return res;
        }
    }

    public class Spawner : MonoBehaviour
    {
        float acc;

        void Update()
        {
            var g = GameManager.I;
            if (g == null || !g.Running) return;
            if (g.RunTime < Cfg.StartGraceSeconds) return;
            int diff = g.Difficulty;
            acc += Time.deltaTime * (1f + diff / 2f) * Cfg.SpawnRateScale;
            while (acc >= 1f)
            {
                acc -= 1f;
                if (Enemy.All.Count < Cfg.MaxEnemies) SpawnOne(diff);
            }
        }

        void SpawnOne(int diff)
        {
            float w1 = 70f, w2 = diff >= 2 ? 25f : 0f, w3 = diff >= 5 ? 5f : 0f;
            float r = Random.value * (w1 + w2 + w3);
            int tier = r < w1 ? 1 : (r < w1 + w2 ? 2 : 3);
            float s1 = 60f, s2 = diff >= 3 ? 30f : 0f, s3 = diff >= 8 ? 10f : 0f;
            r = Random.value * (s1 + s2 + s3);
            EnemySize size = r < s1 ? EnemySize.Small : (r < s1 + s2 ? EnemySize.Medium : EnemySize.Large);

            Enemy.Spawn(Defs.Enemy(size, tier), diff);
        }
    }
}
