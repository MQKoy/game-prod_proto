using UnityEngine;

namespace BuildATower
{
    public class SpriteAnim : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 2f;
        SpriteRenderer sr;
        float offset;

        void Awake() { sr = GetComponent<SpriteRenderer>(); offset = Random.value * 3f; }

        void Update()
        {
            if (frames == null || frames.Length == 0 || sr == null) return;
            sr.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
        }
    }

    public class Parallax : MonoBehaviour
    {
        public Camera cam;
        public float factor, baseY, drift, wrapX = 60f;

        void LateUpdate()
        {
            Vector3 p = transform.position;
            if (drift != 0f)
            {
                p.x += drift * Time.deltaTime;
                if (p.x > wrapX) p.x = -wrapX;
                if (p.x < -wrapX) p.x = wrapX;
            }
            p.y = baseY + (cam != null ? cam.transform.position.y * factor : 0f);
            transform.position = p;
        }
    }

    public class Corpse : MonoBehaviour
    {
        SpriteRenderer sr;
        Color tint;
        float t;
        const float Life = 0.4f;
        Vector3 start;

        public static void Spawn(Sprite whiteSprite, Vector3 pos, bool flipX, Color tint, int order)
        {
            var r = Art.NewSprite("Corpse", whiteSprite, pos, order, GameManager.I.World);
            r.flipX = flipX;
            var c = r.gameObject.AddComponent<Corpse>();
            c.sr = r; c.tint = tint; c.start = pos;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            float sx = 1f + k * 0.5f, sy = 1f - k * 0.75f;
            transform.localScale = new Vector3(sx, sy, 1f);
            transform.position = start + new Vector3(0f, k * 0.6f - (1f - sy) * 0.4f, 0f);
            Color c = Color.Lerp(Color.white, tint, Mathf.Clamp01(k * 3f));
            c.a = 1f - k * k;
            sr.color = c;
        }
    }
}
