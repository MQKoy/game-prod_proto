using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuildATower
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public event System.Action OnChanged;
        public event System.Action OnGameOver;
        public event System.Action<TowerSection> OnSelectionChanged;

        public int Blocks, Exp, Level = 1, ExpInLevel, Kills;
        public float Hp, MaxHp, RunTime;
        public bool Running = true;

        public readonly List<TowerSection> Sections = new List<TowerSection>();
        public readonly int[] UpLevel = new int[11];
        public TowerSection Selected;

        public float RageTime, SlowTime, StunTime, HitFlash;

        public bool LaserOn;

        public Transform World;
        Transform orbT, skyT, crownT;
        Camera cam;
        float camMin, camTarget;

        public float SpawnX { get { return Mathf.Max(Cfg.SpawnDistance, (cam != null ? cam.orthographicSize * cam.aspect : 20f) + 4f); } }

        TowerSection dragSec;
        Vector2 downPos;
        bool dragging;

        public int Difficulty { get { return Sections.Count; } }
        public float TowerTop { get { return Cfg.FoundationHeight + Sections.Count * Cfg.SectionHeight; } }
        float CamMax { get { return Mathf.Max(camMin, TowerTop + 3.5f - cam.orthographicSize); } }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Enemy.All.Clear();
            LaserOn = Cfg.LaserStartsOn;
            for (int i = 0; i < UpLevel.Length; i++) UpLevel[i] = 1;
            Blocks = Cfg.StartBlocks;
            Hp = MaxHp = Cfg.BaseTowerHp;
            World = new GameObject("World").transform;
            World.SetParent(transform, false);
        }

        void Start()
        {
            BuildWorld();
            gameObject.AddComponent<GameUI>();
            Changed();
        }

        void BuildWorld()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = Cfg.CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.65f, 0.9f);
            camMin = cam.orthographicSize - 1.6f;
            camTarget = camMin;
            cam.transform.position = new Vector3(0f, camMin, -10f);


            var sky = Art.NewSprite("Sky", Art.Sky, new Vector3(0f, camMin, 10f), -100, World);
            sky.transform.localScale = new Vector3(600f, cam.orthographicSize * 2.4f / 64f, 1f);
            skyT = sky.transform;


            for (int i = 0; i < 9; i++)
            {
                float cx = -64f + i * 16f + Random.Range(-4f, 4f);
                float cy = Random.Range(6f, 17f);
                var c = AddLayer(Art.Cloud(i), cx, cy, -95, Random.Range(1.6f, 2.6f), false, 0.92f, Random.Range(0.25f, 0.7f));
                c.color = new Color(1f, 1f, 1f, 0.9f);
            }
            for (int i = -2; i <= 2; i++) 
            {
                AddLayer(Art.Mountains, i * 46.08f, -0.5f, -90, 1.8f, (i & 1) != 0, 0.1f, 0f);
                AddLayer(Art.Hills, i * 38.4f, -0.4f, -80, 1.5f, (i & 1) != 0, 0.05f, 0f);
            }


            var dirt = Art.NewSprite("Dirt", Art.White, new Vector3(0f, -18f, 0f), -50, World);
            dirt.transform.localScale = new Vector3(600f, 30f, 1f);
            dirt.color = new Color(0.4f, 0.28f, 0.18f);
            var grass = Art.NewSprite("Grass", Art.White, new Vector3(0f, -1.5f, 0f), -49, World);
            grass.transform.localScale = new Vector3(600f, 3f, 1f);
            grass.color = new Color(0.33f, 0.6f, 0.25f);
            var lip = Art.NewSprite("GrassTop", Art.White, new Vector3(0f, -0.1f, 0f), -48, World);
            lip.transform.localScale = new Vector3(600f, 0.2f, 1f);
            lip.color = new Color(0.45f, 0.75f, 0.32f);


            var found = Art.NewSprite("Foundation", Art.FoundationFrames[0], new Vector3(0f, Cfg.FoundationHeight * 0.5f, 0f), 0, World);
            var fa = found.gameObject.AddComponent<SpriteAnim>();
            fa.frames = Art.FoundationFrames; fa.fps = 3f;

            var crown = Art.NewSprite("Crown", Art.CrownFrames[0], new Vector3(0f, Cfg.FoundationHeight, 0f), 2, World);
            var ca = crown.gameObject.AddComponent<SpriteAnim>();
            ca.frames = Art.CrownFrames; ca.fps = 2.5f;
            crownT = crown.transform;

            var orbGo = new GameObject("Orb");
            orbGo.transform.SetParent(World, false);
            orbGo.AddComponent<OrbLaser>();
            orbT = orbGo.transform;

            new GameObject("Spawner").transform.SetParent(World, false);
            World.Find("Spawner").gameObject.AddComponent<Spawner>();

            LayoutTower();
        }

        SpriteRenderer AddLayer(Sprite s, float x, float y, int order, float scale, bool flip, float factor, float drift)
        {
            var sr = Art.NewSprite("Backdrop", s, new Vector3(x, y, 0f), order, World);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.flipX = flip;
            var p = sr.gameObject.AddComponent<Parallax>();
            p.cam = cam;
            p.factor = factor;
            p.baseY = y - camMin * factor;
            p.drift = drift;
            p.wrapX = 70f;
            return sr;
        }

        public Vector3 MouseWorld()
        {
            Vector2 p = Inp.MousePos;
            return cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
        }


        void Update()
        {
            float dt = Time.deltaTime;
            HandleInput();
            if (!Running) return;

            RunTime += dt;
            RageTime = Mathf.Max(0f, RageTime - dt);
            SlowTime = Mathf.Max(0f, SlowTime - dt);
            StunTime = Mathf.Max(0f, StunTime - dt);
            HitFlash = Mathf.Max(0f, HitFlash - dt);

            float regen = 0f;
            for (int i = 0; i < Sections.Count; i++)
                if (Sections[i].def.type == SectionType.Regeneration)
                    regen += 0.001f * MaxHp + 0.1f * (UpLevel[(int)UpgradeId.Regeneration] - 1);
            if (regen > 0f) Hp = Mathf.Min(MaxHp, Hp + regen * dt);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            camTarget = Mathf.Clamp(camTarget + Inp.ScrollY * 1.5f, camMin, CamMax);
            Vector3 p = cam.transform.position;
            p.y = Mathf.Lerp(p.y, camTarget, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            cam.transform.position = p;
            if (skyT != null) skyT.position = new Vector3(0f, p.y, 10f);
        }

        bool PointerOverUI { get { return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(); } }

        TowerSection SectionAt(Vector3 w, TowerSection ignore)
        {
            for (int i = 0; i < Sections.Count; i++)
            {
                var s = Sections[i];
                if (s == ignore) continue;
                if (Mathf.Abs(w.x - s.Home.x) <= Cfg.TowerWidth * 0.5f && Mathf.Abs(w.y - s.Home.y) <= Cfg.SectionHeight * 0.5f) return s;
            }
            return null;
        }

        void HandleInput()
        {
            if (!Running) return;
            Vector2 mp = Inp.MousePos;

            if (Cfg.LaserHoldToFire) LaserOn = Inp.RightHeld;
            else if (Inp.RightDown) LaserOn = !LaserOn;

            if (Inp.LeftDown)
            {
                if (!PointerOverUI)
                {
                    dragSec = SectionAt(MouseWorld(), null);
                    downPos = mp;
                    dragging = false;
                    if (dragSec == null) Select(null);
                }
                else dragSec = null;
            }

            if (dragSec != null && Inp.LeftHeld)
            {
                if (!dragging && (mp - downPos).sqrMagnitude > 100f)
                {
                    dragging = true;
                    dragSec.SetDragging(true);
                }
                if (dragging)
                {
                    Vector3 w = MouseWorld();
                    dragSec.transform.position = new Vector3(w.x, w.y, 0f);
                }
            }

            if (dragSec != null && Inp.LeftUp)
            {
                if (dragging)
                {
                    dragSec.SetDragging(false);
                    var other = SectionAt(MouseWorld(), dragSec);
                    if (other != null) Swap(dragSec, other);
                    LayoutTower();
                }
                else Select(dragSec);
                dragSec = null;
                dragging = false;
            }
        }

        public void Select(TowerSection s)
        {
            if (Selected == s) return;
            if (Selected != null) Selected.SetSelected(false);
            Selected = s;
            if (s != null) s.SetSelected(true);
            if (OnSelectionChanged != null) OnSelectionChanged(s);
        }

        void Swap(TowerSection a, TowerSection b)
        {
            int ia = Sections.IndexOf(a), ib = Sections.IndexOf(b);
            if (ia < 0 || ib < 0) return;
            Sections[ia] = b;
            Sections[ib] = a;
            Changed();
        }

        public int Built(SectionType t)
        {
            int n = 0;
            for (int i = 0; i < Sections.Count; i++) if (Sections[i].def.type == t) n++;
            return n;
        }

        public int Charges(SectionType t)
        {
            var d = Defs.Get(t);
            return d.baseCharges + (d.scalesWithLevel ? UpLevel[(int)t] / 5 : 0);
        }

        public int BuildCost(SectionType t)
        {
            var d = Defs.Get(t);
            return d.baseCost * (1 + (d.scalesWithLevel ? UpLevel[(int)t] / 5 : 0));
        }

        public bool CanBuild(SectionType t, int replaceIndex = -1)
        {
            if (!Running) return false;
            var d = Defs.Get(t);
            if (Level < d.unlockLevel) return false;
            if (replaceIndex >= 0)
            {
                if (replaceIndex >= Sections.Count) return false;
                if (Sections[replaceIndex].def.type == t) return false;
            }
            if (Built(t) >= Charges(t)) return false;
            return Blocks >= BuildCost(t);
        }

        public bool Build(SectionType t, int replaceIndex = -1)
        {
            if (!CanBuild(t, replaceIndex)) return false;
            Blocks -= BuildCost(t);

            if (replaceIndex >= 0)
            {
                var old = Sections[replaceIndex];
                if (Selected == old) Select(null);
                Sections.RemoveAt(replaceIndex);
                Destroy(old.gameObject);
            }

            var go = new GameObject(Defs.Get(t).name + " Section");
            go.transform.SetParent(World, false);
            var s = go.AddComponent<TowerSection>();
            s.Init(Defs.Get(t));
            if (replaceIndex >= 0) Sections.Insert(replaceIndex, s); else Sections.Add(s);

            float oldMax = MaxHp;
            MaxHp = Cfg.BaseTowerHp + Cfg.HpPerSection * Sections.Count;
            Hp = Mathf.Min(MaxHp, Hp + Mathf.Max(0f, MaxHp - oldMax));

            LayoutTower();
            if (replaceIndex < 0) camTarget = CamMax;
            Changed();
            return true;
        }

        void LayoutTower()
        {
            for (int i = 0; i < Sections.Count; i++)
            {
                var s = Sections[i];
                s.Index = i;
                s.Home = new Vector3(0f, Cfg.FoundationHeight + (i + 0.5f) * Cfg.SectionHeight, 0f);
                s.transform.position = s.Home;
            }
            if (crownT != null) crownT.position = new Vector3(0f, TowerTop, 0f);
            if (orbT != null) orbT.position = new Vector3(0f, TowerTop + Cfg.CrownHeight, 0f);
        }

        public bool Unlocked(UpgradeId id)
        {
            if (id >= UpgradeId.OrbLaser) return true;
            return Level >= Defs.Get((SectionType)id).unlockLevel;
        }

        public int UpgradeCost(UpgradeId id)
        {
            return Mathf.RoundToInt(Defs.BaseUpgradeExp(id) * Mathf.Pow(Cfg.UpgradeCostGrowth, UpLevel[(int)id] - 1));
        }

        public bool CanUpgrade(UpgradeId id) { return Running && Unlocked(id) && Exp >= UpgradeCost(id); }

        public void Upgrade(UpgradeId id)
        {
            if (!CanUpgrade(id)) return;
            Exp -= UpgradeCost(id);
            UpLevel[(int)id]++;
            Changed();
        }

        public int ExpToNext(int level) { return 8 + 6 * (level - 1); }

        public void OnEnemyKilled(EnemyDef d)
        {
            Kills++;
            int res = UpLevel[(int)UpgradeId.Resources];
            Blocks += Mathf.Max(1, Mathf.FloorToInt((d.blocks + (res - 1)) * Cfg.RewardScale + 0.5f));
            int e = Mathf.Max(1, Mathf.FloorToInt((d.exp + res / 5) * Cfg.RewardScale + 0.5f));
            Exp += e;
            ExpInLevel += e;
            while (ExpInLevel >= ExpToNext(Level)) { ExpInLevel -= ExpToNext(Level); Level++; }
        }

        public float ArmorReduction
        {
            get
            {
                float a = 0f;
                for (int i = 0; i < Sections.Count; i++)
                    if (Sections[i].def.type == SectionType.Armor)
                        a += 0.01f + 0.001f * (UpLevel[(int)UpgradeId.Armor] - 1);
                return Mathf.Min(a, Cfg.MaxArmor);
            }
        }

        public void DamageTower(float dmg)
        {
            if (!Running) return;
            Hp -= dmg * (1f - ArmorReduction);
            HitFlash = 0.12f;
            if (Hp <= 0f)
            {
                Hp = 0f;
                Running = false;
                Select(null);
                if (OnGameOver != null) OnGameOver();
            }
        }

        void Changed() { if (OnChanged != null) OnChanged(); }

        public void Restart()
        {
            Time.timeScale = 1f;
            I = null;
            Destroy(gameObject);
            Bootstrap.Launch();
        }
    }
}
