using UnityEngine;

namespace BuildATower
{
    public class OrbLaser : MonoBehaviour
    {
        const float MaxLength = 24f;

        SpriteRenderer orbSr, beam;
        float tick, flash;

        GameManager G { get { return GameManager.I; } }
        int Lv { get { return G.UpLevel[(int)UpgradeId.OrbLaser]; } }

        public float Damage { get { return (1f + (Lv - 1)) * Cfg.TowerDamageScale; } }
        public float Rate { get { return 2f + 0.1f * (Lv - 1); } }
        public float Radius { get { return (1f + 2f * (Lv / 5)) * Cfg.AoeUnit; } }

        void Awake()
        {
            orbSr = gameObject.AddComponent<SpriteRenderer>();
            orbSr.sprite = Art.Orb;
            orbSr.sharedMaterial = Art.Mat;
            orbSr.sortingOrder = 3;

            beam = Art.NewSprite("Beam", Art.WhiteL, Vector3.zero, 28, transform.parent);
            beam.color = new Color(0.5f, 0.9f, 1f, 0.8f);
        }

        void Update()
        {
            var g = G;
            if (g == null || !g.Running) { if (beam != null) beam.enabled = false; return; }
            float dt = Time.deltaTime;

            float pulse = g.LaserOn ? 1f + 0.12f * Mathf.Sin(Time.time * 14f) : 1f;
            orbSr.color = g.LaserOn ? Color.white : new Color(0.55f, 0.6f, 0.7f, 1f);
            transform.localScale = Vector3.one * 1.5f * pulse;
            if (!g.LaserOn) { beam.enabled = false; return; }

            Vector3 from = transform.position;
            Vector3 m = g.MouseWorld();
            m.z = 0f;
            Vector3 d = m - from;
            float dist = Mathf.Min(d.magnitude, MaxLength);
            if (dist < 0.3f) { beam.enabled = false; return; }

            Vector3 end = from + d.normalized * dist;
            flash = Mathf.Max(0f, flash - dt);
            float thick = 0.16f + 0.3f * (flash / 0.1f);

            beam.enabled = true;
            beam.transform.position = from;
            beam.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beam.transform.localScale = new Vector3(dist, thick, 1f);

            tick += dt;
            float iv = 1f / Rate;
            if (tick >= iv)
            {
                tick = 0f;
                flash = 0.1f;
                float r = Radius;
                for (int i = Enemy.All.Count - 1; i >= 0; i--)
                {
                    if (i >= Enemy.All.Count) continue;
                    var e = Enemy.All[i];
                    if (e.IsDead) continue;
                    if (Util.SegDist(e.transform.position, from, end) <= r + e.radius) e.Hurt(Damage);
                }
                FxItem.Burst(end, r, new Color(0.5f, 0.9f, 1f), 0.15f);
            }
        }
    }
}
