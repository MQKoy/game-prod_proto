using System.Collections.Generic;
using UnityEngine;

namespace BuildATower
{
    public class Pix
    {
        public readonly int w, h;
        readonly Color32[] c;

        public Pix(int w, int h) { this.w = w; this.h = h; c = new Color32[w * h]; }

        public void Set(int x, int y, Color32 col) { if (x < 0 || y < 0 || x >= w || y >= h) return; c[y * w + x] = col; }
        public void Fill(Color32 col) { for (int i = 0; i < c.Length; i++) c[i] = col; }

        public void Rect(int x, int y, int rw, int rh, Color32 col)
        {
            for (int j = 0; j < rh; j++)
                for (int i = 0; i < rw; i++)
                    Set(x + i, y + j, col);
        }

        public void Disc(float cx, float cy, float r, Color32 col)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) c[y * w + x] = col;
                }
        }

        public void Border(Color32 col)
        {
            Rect(0, 0, w, 1, col); Rect(0, h - 1, w, 1, col);
            Rect(0, 0, 1, h, col); Rect(w - 1, 0, 1, h, col);
        }

        public Sprite ToSprite(float ppu, Vector2 pivot, bool point = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels32(c);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), pivot, ppu);
        }
    }

    public class EnemyAnim
    {
        public Sprite[] walk, atk, flashWalk, flashAtk;
        public float height;
    }

    public static class Art
    {
        static Material mat;
        static Sprite white, whiteL, circle, sky, orb, arrow, ball, outline;
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<int, EnemyAnim> enemies = new Dictionary<int, EnemyAnim>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mat = null; white = whiteL = circle = sky = orb = arrow = ball = outline = null;
            texCache.Clear(); frameCache.Clear(); enemies.Clear();
        }

        public static Material Mat
        {
            get
            {
                if (mat == null)
                {
                    var sh = Shader.Find("Sprites/Default");
                    if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    mat = new Material(sh);
                }
                return mat;
            }
        }

        public static SpriteRenderer NewSprite(string name, Sprite s, Vector3 localPos, int order, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = Mat;
            sr.sortingOrder = order;
            return sr;
        }

        static Texture2D Tex(string key)
        {
            Texture2D t;
            if (texCache.TryGetValue(key, out t) && t != null) return t;
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(System.Convert.FromBase64String(ArtData.Png[key]));
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            texCache[key] = t;
            return t;
        }

        static Texture2D WhiteSilhouette(Texture2D src)
        {
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
                if (px[i].a > 0) px[i] = new Color32(255, 255, 255, px[i].a);
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Sprite[] Slice(Texture2D t, int fw, int fh, Vector2 pivot)
        {
            int n = t.width / fw;
            var res = new Sprite[n];
            for (int i = 0; i < n; i++) res[i] = Sprite.Create(t, new Rect(i * fw, 0, fw, fh), pivot, Cfg.Ppu);
            return res;
        }

        static Sprite[] Frames(string key, int fw, int fh, Vector2 pivot)
        {
            Sprite[] f;
            if (frameCache.TryGetValue(key, out f) && f != null && f.Length > 0 && f[0] != null) return f;
            f = Slice(Tex(key), fw, fh, pivot);
            frameCache[key] = f;
            return f;
        }

        public static Sprite SingleSprite(string key, float ppu, Vector2 pivot)
        {
            var t = Tex(key);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu);
        }


        static Sprite Solid(Vector2 pivot)
        {
            var p = new Pix(1, 1);
            p.Fill(new Color32(255, 255, 255, 255));
            return p.ToSprite(1f, pivot);
        }

        public static Sprite White { get { if (white == null) white = Solid(new Vector2(0.5f, 0.5f)); return white; } }
        public static Sprite WhiteL { get { if (whiteL == null) whiteL = Solid(new Vector2(0f, 0.5f)); return whiteL; } }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    var p = new Pix(32, 32);
                    p.Disc(16, 16, 16, new Color32(255, 255, 255, 255));
                    circle = p.ToSprite(32f, new Vector2(0.5f, 0.5f), false);
                }
                return circle;
            }
        }

        public static Sprite Sky
        {
            get
            {
                if (sky == null)
                {
                    var t = new Texture2D(1, 64, TextureFormat.RGBA32, false);
                    t.wrapMode = TextureWrapMode.Clamp;
                    t.filterMode = FilterMode.Bilinear;
                    var cols = new Color32[64];
                    for (int i = 0; i < 64; i++)
                        cols[i] = Color.Lerp(new Color(0.72f, 0.88f, 0.98f), new Color(0.25f, 0.45f, 0.82f), i / 63f);
                    t.SetPixels32(cols);
                    t.Apply();
                    sky = Sprite.Create(t, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 1f);
                }
                return sky;
            }
        }

        public static Sprite[] FoundationFrames { get { return Frames("foundation", 80, 20, new Vector2(0.5f, 0.5f)); } }
        public static Sprite[] CrownFrames { get { return Frames("crown", 64, 30, new Vector2(0.5f, 0f)); } }

        public static Sprite[] SectionFrames(SectionDef d)
        {
            string key = "section_" + d.type.ToString().ToLowerInvariant();
            if (d.type == SectionType.Regeneration) key = "section_regen";
            return Frames(key, 56, 36, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Outline
        {
            get
            {
                if (outline == null)
                {
                    var p = new Pix(56, 36);
                    Color32 y = new Color32(255, 225, 70, 255);
                    p.Rect(0, 0, 56, 2, y); p.Rect(0, 34, 56, 2, y); p.Rect(0, 0, 2, 36, y); p.Rect(54, 0, 2, 36, y);
                    outline = p.ToSprite(Cfg.Ppu, new Vector2(0.5f, 0.5f));
                }
                return outline;
            }
        }

        public static Sprite Orb
        {
            get
            {
                if (orb == null)
                {
                    var p = new Pix(16, 16);
                    p.Disc(8, 8, 8, new Color32(60, 160, 230, 255));
                    p.Disc(8, 8, 6, new Color32(110, 210, 255, 255));
                    p.Disc(8, 8, 3.5f, new Color32(200, 245, 255, 255));
                    p.Rect(5, 10, 2, 2, new Color32(255, 255, 255, 255));
                    orb = p.ToSprite(Cfg.Ppu, new Vector2(0.5f, 0.5f));
                }
                return orb;
            }
        }

        public static Sprite Mountains { get { return SingleSprite("mountains", Cfg.Ppu, new Vector2(0.5f, 0f)); } }
        public static Sprite Hills { get { return SingleSprite("hills", Cfg.Ppu, new Vector2(0.5f, 0f)); } }
        public static Sprite Cloud(int k) { return SingleSprite("cloud_" + (k % 3), Cfg.Ppu, new Vector2(0.5f, 0.5f)); }

        public static Sprite ArrowSprite
        {
            get
            {
                if (arrow == null)
                {
                    var p = new Pix(12, 3);
                    p.Rect(0, 1, 10, 1, new Color32(140, 95, 50, 255));
                    p.Rect(9, 0, 3, 3, new Color32(210, 210, 220, 255));
                    p.Rect(0, 0, 2, 3, new Color32(240, 240, 240, 255));
                    arrow = p.ToSprite(Cfg.Ppu, new Vector2(0.5f, 0.5f));
                }
                return arrow;
            }
        }

        public static Sprite BallSprite
        {
            get
            {
                if (ball == null)
                {
                    var p = new Pix(8, 8);
                    p.Disc(4, 4, 4, new Color32(30, 30, 38, 255));
                    p.Rect(2, 5, 2, 2, new Color32(120, 120, 135, 255));
                    ball = p.ToSprite(Cfg.Ppu, new Vector2(0.5f, 0.5f));
                }
                return ball;
            }
        }

        public static int EnemyFrame(EnemySize s) { return s == EnemySize.Small ? 24 : s == EnemySize.Medium ? 32 : 48; }

        public static EnemyAnim Enemy(EnemyDef d)
        {
            int idx = (int)d.size * 3 + (d.tier - 1);
            EnemyAnim a;
            if (enemies.TryGetValue(idx, out a) && a != null && a.walk[0] != null) return a;

            int fr = EnemyFrame(d.size);
            var tex = Tex("enemy_" + idx);
            var white = WhiteSilhouette(tex);
            var all = Slice(tex, fr, fr, new Vector2(0.5f, 0.5f));
            var allW = Slice(white, fr, fr, new Vector2(0.5f, 0.5f));
            a = new EnemyAnim
            {
                walk = new[] { all[0], all[1], all[2], all[3] },
                atk = new[] { all[4], all[5] },
                flashWalk = new[] { allW[0], allW[1], allW[2], allW[3] },
                flashAtk = new[] { allW[4], allW[5] },
                height = fr / Cfg.Ppu
            };
            enemies[idx] = a;
            return a;
        }
    }
}
