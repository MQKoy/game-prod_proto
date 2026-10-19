using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BuildATower
{
    public class GameUI : MonoBehaviour
    {
        class Row { public Button btn; public Text txt; public SectionType type; public UpgradeId up; }

        static readonly float[] Speeds = { 1f, 2f, 3f };
        static readonly Color PanelCol = new Color(0.07f, 0.08f, 0.12f, 0.88f);
        static readonly Color Muted = new Color(0.65f, 0.65f, 0.7f);

        GameManager g;
        Font font;
        Transform canvasT;
        readonly List<Row> buildRows = new List<Row>();
        readonly List<Row> upRows = new List<Row>();

        Text lvTxt, blocksTxt, expTxt, diffTxt, hpTxt, hintTxt, laserTxt, selTitle, selInfo, replaceTxt, speedTxt, overStats;
        RectTransform hpFill;
        GameObject selPanel, overPanel;
        Button activateBtn, replaceBtn;

        int replaceIndex = -1;
        int speedIdx;
        float refreshT;

        void Start()
        {
            g = GameManager.I;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
            g.OnChanged += Refresh;
            g.OnSelectionChanged += OnSelection;
            g.OnGameOver += OnGameOver;
            Refresh();
        }

        void Update()
        {
            refreshT -= Time.unscaledDeltaTime;
            if (refreshT <= 0f) { refreshT = 0.1f; Refresh(); }
        }

        void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var old = es.GetComponent<StandaloneInputModule>();
            if (old != null) Destroy(old);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                var m = es.gameObject.AddComponent<InputSystemUIInputModule>();
                m.AssignDefaultActions();
            }
#else
            if (es.GetComponent<StandaloneInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvasT = cgo.transform;
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildTopBar();
            BuildHpBar();
            BuildBuildPanel();
            BuildUpgradePanel();
            BuildSelectionPanel();
            BuildTexts();
            BuildGameOver();
        }

        void BuildTopBar()
        {
            var bar = MakePanel("TopBar", canvasT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f), PanelCol);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.sizeDelta = new Vector2(0f, 64f);
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 24, 8, 8);
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;

            lvTxt = Label(bar, "", 26, TextAnchor.MiddleLeft);
            blocksTxt = Label(bar, "", 26, TextAnchor.MiddleLeft);
            expTxt = Label(bar, "", 26, TextAnchor.MiddleLeft);
            diffTxt = Label(bar, "", 26, TextAnchor.MiddleLeft);

            var speedBtn = MakeButton(bar, 48f, out speedTxt);
            speedTxt.alignment = TextAnchor.MiddleCenter;
            speedTxt.text = "SPEED x1";
            var le = speedBtn.GetComponent<LayoutElement>();
            le.preferredWidth = 170f; le.flexibleWidth = 0f;
            speedBtn.onClick.AddListener(() =>
            {
                speedIdx = (speedIdx + 1) % Speeds.Length;
                Time.timeScale = Speeds[speedIdx];
                speedTxt.text = "SPEED x" + Speeds[speedIdx].ToString("0");
            });
        }

        void BuildHpBar()
        {
            var bg = MakePanel("HpBar", canvasT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(640f, 34f), new Color(0.15f, 0.04f, 0.04f, 0.9f));
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bg, false);
            hpFill = (RectTransform)fillGo.transform;
            hpFill.anchorMin = Vector2.zero; hpFill.anchorMax = Vector2.one;
            hpFill.offsetMin = Vector2.zero; hpFill.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.25f, 0.8f, 0.3f);
            hpTxt = Label(bg, "", 22, TextAnchor.MiddleCenter);
            Stretch(hpTxt.rectTransform, 0, 0, 0, 0);
        }

        void BuildBuildPanel()
        {
            var p = MakePanel("BuildPanel", canvasT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -30f), new Vector2(390f, 0f), PanelCol);
            Vlg(p, 12, 6, true);
            Label(p, "<b>BUILD</b>  (spend Blocks)", 24, TextAnchor.MiddleLeft);
            foreach (var d in Defs.Sections)
            {
                Text t;
                var b = MakeButton(p, 66f, out t);
                var type = d.type;
                b.onClick.AddListener(() => OnBuildClicked(type));
                buildRows.Add(new Row { btn = b, txt = t, type = type });
            }
        }

        void BuildUpgradePanel()
        {
            var p = MakePanel("UpgradePanel", canvasT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, -30f), new Vector2(430f, 0f), PanelCol);
            Vlg(p, 12, 6, true);
            Label(p, "<b>UPGRADES</b>  (spend EXP)", 24, TextAnchor.MiddleLeft);
            foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId)))
            {
                Text t;
                var b = MakeButton(p, 60f, out t);
                var uid = id;
                b.onClick.AddListener(() => g.Upgrade(uid));
                upRows.Add(new Row { btn = b, txt = t, up = uid });
            }
        }

        void BuildSelectionPanel()
        {
            var p = MakePanel("SelectionPanel", canvasT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(560f, 0f), PanelCol);
            Vlg(p, 14, 8, true);
            selPanel = p.gameObject;
            selTitle = Label(p, "", 26, TextAnchor.MiddleLeft);
            selInfo = Label(p, "", 19, TextAnchor.UpperLeft);

            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(p, false);
            row.GetComponent<LayoutElement>().preferredHeight = 48f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;

            Text at;
            activateBtn = MakeButton(row.transform, 48f, out at);
            at.text = "Activate now"; at.alignment = TextAnchor.MiddleCenter;
            activateBtn.onClick.AddListener(() => { if (g.Selected != null) g.Selected.Activate(); });

            replaceBtn = MakeButton(row.transform, 48f, out replaceTxt);
            replaceTxt.alignment = TextAnchor.MiddleCenter;
            replaceBtn.onClick.AddListener(() =>
            {
                if (g.Selected == null) return;
                replaceIndex = replaceIndex >= 0 ? -1 : g.Selected.Index;
                Refresh();
            });

            Text ct;
            var close = MakeButton(row.transform, 48f, out ct);
            ct.text = "Close"; ct.alignment = TextAnchor.MiddleCenter;
            close.onClick.AddListener(() => g.Select(null));

            selPanel.SetActive(false);
        }

        void BuildTexts()
        {
            hintTxt = Label(canvasT, "", 28, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.5f));
            var rt = hintTxt.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -156f);
            rt.sizeDelta = new Vector2(900f, 80f);

            laserTxt = Label(canvasT, "", 22, TextAnchor.MiddleCenter);
            var lr = laserTxt.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.anchoredPosition = new Vector2(0f, -118f);
            lr.sizeDelta = new Vector2(700f, 32f);

            string laserHelp = Cfg.LaserHoldToFire ? "Hold right-click to fire the orb laser" : "Right-click toggles the orb laser";
            var help = Label(canvasT, "Move the mouse to aim the orb   |   " + laserHelp + "   |   Click a section to inspect it   |   Drag a section onto another to swap   |   Scroll to climb the tower",
                17, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
            var hr = help.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0f, 14f);
            hr.sizeDelta = new Vector2(1200f, 30f);
        }

        void BuildGameOver()
        {
            var dim = MakePanel("GameOver", canvasT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero; dim.offsetMax = Vector2.zero;
            overPanel = dim.gameObject;

            var box = MakePanel("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 0f), new Color(0.1f, 0.1f, 0.15f, 0.97f));
            Vlg(box, 28, 16, true);
            Label(box, "<b>THE TOWER HAS FALLEN</b>", 44, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.45f));
            overStats = Label(box, "", 26, TextAnchor.MiddleCenter);
            Text rt;
            var b = MakeButton(box, 64f, out rt);
            rt.text = "<b>Build again</b>"; rt.alignment = TextAnchor.MiddleCenter; rt.fontSize = 28;
            b.onClick.AddListener(() => g.Restart());
            overPanel.SetActive(false);
        }


        RectTransform MakePanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color col)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = col;
            return rt;
        }

        static void Vlg(RectTransform rt, int pad, float spacing, bool fit)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.spacing = spacing;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            if (fit)
            {
                var f = rt.gameObject.AddComponent<ContentSizeFitter>();
                f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        Text Label(Transform parent, string s, int size, TextAnchor anchor, Color? col = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = col ?? Color.white;
            t.text = s;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Text Label(RectTransform parent, string s, int size, TextAnchor anchor, Color? col = null)
        {
            return Label((Transform)parent, s, size, anchor, col);
        }

        Button MakeButton(Transform parent, float height, out Text label)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = new Color(0.2f, 0.24f, 0.34f);
            cb.highlightedColor = new Color(0.3f, 0.36f, 0.5f);
            cb.pressedColor = new Color(0.14f, 0.17f, 0.25f);
            cb.selectedColor = cb.normalColor;
            cb.disabledColor = new Color(0.13f, 0.14f, 0.18f, 0.85f);
            btn.colors = cb;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            label = Label(go.transform, "", 20, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 12, 4, 12, 4);
            return btn;
        }

        void OnBuildClicked(SectionType t)
        {
            if (g.Build(t, replaceIndex) && replaceIndex >= 0)
            {
                replaceIndex = -1;
                g.Select(null);
            }
        }

        void OnSelection(TowerSection s)
        {
            replaceIndex = -1;
            Refresh();
        }

        void OnGameOver()
        {
            int secs = (int)g.RunTime;
            overStats.text = "Survived " + (secs / 60) + ":" + (secs % 60).ToString("00") +
                             "\nPlayer level " + g.Level + "   |   Sections built " + g.Sections.Count +
                             "   |   Enemies defeated " + g.Kills;
            overPanel.SetActive(true);
            Refresh();
        }

        void Refresh()
        {
            if (g == null || lvTxt == null) return;

            lvTxt.text = "LEVEL " + g.Level + "  <size=18>(" + g.ExpInLevel + "/" + g.ExpToNext(g.Level) + ")</size>";
            blocksTxt.text = "BLOCKS " + g.Blocks;
            expTxt.text = "EXP " + g.Exp;
            diffTxt.text = "DIFFICULTY " + g.Difficulty;
            hpTxt.text = "TOWER HP  " + Mathf.CeilToInt(g.Hp) + " / " + Mathf.CeilToInt(g.MaxHp);
            hpFill.anchorMax = new Vector2(g.MaxHp > 0f ? Mathf.Clamp01(g.Hp / g.MaxHp) : 0f, 1f);
            laserTxt.text = g.LaserOn
                ? "ORB LASER  <color=#7CFF9A><b>ON</b></color>"
                : "ORB LASER  <color=#FF8A80><b>OFF</b></color>  <size=17>(" + (Cfg.LaserHoldToFire ? "hold" : "press") + " right-click)</size>";

            if (replaceIndex >= 0)
            {
                hintTxt.gameObject.SetActive(true);
                hintTxt.text = "Pick a section in the BUILD list to replace the selected one (no refund)";
            }
            else if (g.Sections.Count == 0 && g.Running)
            {
                hintTxt.gameObject.SetActive(true);
                hintTxt.text = "Spend your Blocks on an Arrow section to start defending!";
            }
            else hintTxt.gameObject.SetActive(false);

            foreach (var r in buildRows)
            {
                var d = Defs.Get(r.type);
                bool unlocked = g.Level >= d.unlockLevel;
                bool can = g.CanBuild(r.type, replaceIndex);
                r.btn.interactable = can;
                if (!unlocked)
                    r.txt.text = "<b>" + d.name + "</b>\n<size=16>Unlocks at level " + d.unlockLevel + "</size>";
                else
                    r.txt.text = "<b>" + d.name + "</b>  -  " + g.BuildCost(r.type) + " blocks   [" + g.Built(r.type) + "/" + g.Charges(r.type) + " built]\n<size=16>" + d.blurb + "</size>";
                r.txt.color = can ? Color.white : Muted;
            }

            foreach (var r in upRows)
            {
                bool can = g.CanUpgrade(r.up);
                r.btn.interactable = can;
                int lv = g.UpLevel[(int)r.up];
                string name = Defs.UpgradeName(r.up);
                if (!g.Unlocked(r.up))
                    r.txt.text = "<b>" + name + "</b>\n<size=16>Unlocks at level " + Defs.Get((SectionType)r.up).unlockLevel + "</size>";
                else
                    r.txt.text = "<b>" + name + "</b>  Lv " + lv + " -> " + (lv + 1) + "   -  " + g.UpgradeCost(r.up) + " EXP\n<size=15>" + Defs.UpgradeInfo(r.up) + "</size>";
                r.txt.color = can ? Color.white : Muted;
            }

            var sel = g.Selected;
            if (sel != null && g.Running)
            {
                selPanel.SetActive(true);
                string[] lines = sel.Describe().Split('\n');
                selTitle.text = "<b>" + lines[0] + "</b>";
                selInfo.text = lines.Length > 1 ? lines[1] : "";
                activateBtn.gameObject.SetActive(sel.def.category == SectionCategory.Utility);
                activateBtn.interactable = sel.Cooldown <= 0f;
                replaceTxt.text = replaceIndex >= 0 ? "Cancel replace" : "Replace";
                replaceTxt.alignment = TextAnchor.MiddleCenter;
            }
            else selPanel.SetActive(false);
        }
    }
}
