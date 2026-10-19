using UnityEngine;

namespace BuildATower
{
    public class Projectile : MonoBehaviour
    {
        Enemy target;
        Vector3 dest;
        float dmg, splash, speed;
        bool arrow;

        public static void Arrow(Vector3 from, Enemy t, float damage)
        {
            Make(from, t, damage, 0f, 16f, Art.ArrowSprite, true);
        }

        public static void Cannon(Vector3 from, Enemy t, float damage, float splashRadius)
        {
            Make(from, t, damage, splashRadius, 9f, Art.BallSprite, false);
        }

        static void Make(Vector3 from, Enemy t, float damage, float splashRadius, float spd, Sprite spr, bool isArrow)
        {
            var sr = Art.NewSprite(isArrow ? "Arrow" : "Cannonball", spr, from, 30, GameManager.I.World);
            var p = sr.gameObject.AddComponent<Projectile>();
            p.target = t;
            p.dest = t.transform.position;
            p.dmg = damage;
            p.splash = splashRadius;
            p.speed = spd;
            p.arrow = isArrow;
        }

        void Update()
        {
            var g = GameManager.I;
            if (g == null || !g.Running) return;

            if (target != null && !target.IsDead) dest = target.transform.position;
            else target = null;

            Vector3 pos = transform.position;
            Vector3 to = dest - pos;
            float step = speed * Time.deltaTime;
            if (to.magnitude <= step + 0.15f) { Hit(); return; }

            transform.position = pos + to.normalized * step;
            if (arrow) transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
        }

        void Hit()
        {
            if (splash > 0f)
            {
                for (int i = Enemy.All.Count - 1; i >= 0; i--)
                {
                    if (i >= Enemy.All.Count) continue;
                    var e = Enemy.All[i];
                    if (e.IsDead) continue;
                    if (Vector2.Distance(e.transform.position, dest) <= splash + e.radius) e.Hurt(dmg);
                }
                FxItem.Burst(dest, splash, new Color(1f, 0.6f, 0.2f), 0.3f);
            }
            else if (target != null && !target.IsDead)
            {
                target.Hurt(dmg);
            }
            Destroy(gameObject);
        }
    }

    public class FxItem : MonoBehaviour
    {
        SpriteRenderer sr;
        float t, life;
        Vector3 s0, s1;
        Color c0;

        static FxItem Make(Sprite s, Vector3 pos, int order, Color col, float lifetime)
        {
            var r = Art.NewSprite("Fx", s, pos, order, GameManager.I.World);
            r.color = col;
            var f = r.gameObject.AddComponent<FxItem>();
            f.sr = r; f.life = lifetime; f.c0 = col;
            return f;
        }

        public static void Burst(Vector3 pos, float radius, Color col, float life = 0.3f)
        {
            col.a = 0.75f;
            var f = Make(Art.Circle, pos, 31, col, life);
            f.s0 = Vector3.one * radius * 0.6f;
            f.s1 = Vector3.one * radius * 2f;
            f.transform.localScale = f.s0;
        }

        public static void Beam(Vector3 a, Vector3 b, Color col, float width, float life)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            var f = Make(Art.WhiteL, a, 28, col, life);
            f.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            f.s0 = new Vector3(len, width, 1f);
            f.s1 = new Vector3(len, width * 0.2f, 1f);
            f.transform.localScale = f.s0;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.Lerp(s0, s1, k);
            Color c = c0;
            c.a = c0.a * (1f - k);
            sr.color = c;
        }
    }
}
