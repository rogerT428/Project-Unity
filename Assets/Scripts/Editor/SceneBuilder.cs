#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TrashSorting.EditorTools
{
    /// <summary>Menu: Tools > Trash Sorting > Build Scene. Membangun seluruh scene game secara otomatis.</summary>
    public static class SceneBuilder
    {
        const string MatFolder = "Assets/Generated/Materials";
        const string ScenePath = "Assets/Scenes/TrashSorting.unity";
        const string ModelFolder = "Assets/Models/PolyPizza";
        static Font _font;

        // Nama sampah -> file model .glb di ModelFolder (hasil download Tools/download_assets.ps1)
        static readonly Dictionary<string, string> ModelKeys = new Dictionary<string, string>
        {
            { "Botol Plastik", "BotolPlastik" }, { "Gelas Plastik", "GelasPlastik" }, { "Kantong Plastik", "KantongPlastik" },
            { "Kulit Pisang", "KulitPisang" }, { "Apel Busuk", "Apel" }, { "Daun Kering", "Daun" },
            { "Kertas Bekas", "Kertas" }, { "Kardus", "Kardus" }, { "Koran", "Koran" },
            { "Kaleng Minuman", "Kaleng" }, { "Kaleng Sarden", "KalengSarden" }, { "Tutup Botol", "TutupBotol" },
        };

        /// <summary>Instansiasi model .glb, diskalakan agar sisi terbesarnya = size, dan alasnya di y=0.</summary>
        static GameObject SpawnModel(string key, Transform parent, float size)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{key}.glb");
            if (asset == null) return null;
            var m = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            m.transform.SetParent(parent, false);
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var b = LocalBounds(m);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max > 0.0001f) m.transform.localScale *= size / max;
            b = LocalBounds(m);
            m.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            return m;
        }

        /// <summary>Bounds semua renderer, dalam ruang lokal root.</summary>
        static Bounds LocalBounds(GameObject root)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var t = root.transform.parent != null ? root.transform.parent : root.transform;
            Bounds b = default; bool init = false;
            foreach (var r in rs)
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var lp = t.InverseTransformPoint(corner);
                    if (!init) { b = new Bounds(lp, Vector3.zero); init = true; } else b.Encapsulate(lp);
                }
            }
            return b;
        }

        struct TrashDef
        {
            public string name; public TrashType type; public PrimitiveType shape;
            public Vector3 scale; public Color color; public string info;
            public TrashDef(string n, TrashType t, PrimitiveType s, Vector3 sc, Color c, string i)
            { name = n; type = t; shape = s; scale = sc; color = c; info = i; }
        }

        static readonly List<TrashDef> Trash = new List<TrashDef>
        {
            new TrashDef("Botol Plastik", TrashType.Plastik, PrimitiveType.Capsule, new Vector3(.07f,.12f,.07f), new Color(.6f,.85f,1f), "Botol plastik butuh ratusan tahun untuk terurai. Bisa didaur ulang jadi serat kain!"),
            new TrashDef("Gelas Plastik", TrashType.Plastik, PrimitiveType.Cylinder, new Vector3(.08f,.06f,.08f), new Color(.9f,.95f,1f), "Gelas plastik sekali pakai sebaiknya diganti tumbler."),
            new TrashDef("Kantong Plastik", TrashType.Plastik, PrimitiveType.Sphere, new Vector3(.14f,.1f,.14f), new Color(.95f,.95f,.95f), "Kantong plastik berbahaya bagi hewan laut."),
            new TrashDef("Kulit Pisang", TrashType.Organik, PrimitiveType.Capsule, new Vector3(.05f,.09f,.05f), new Color(1f,.85f,.1f), "Kulit pisang bisa dijadikan kompos dalam 2-5 minggu."),
            new TrashDef("Apel Busuk", TrashType.Organik, PrimitiveType.Sphere, new Vector3(.09f,.09f,.09f), new Color(.6f,.25f,.1f), "Sisa buah adalah sampah organik, bagus untuk pupuk."),
            new TrashDef("Daun Kering", TrashType.Organik, PrimitiveType.Cube, new Vector3(.15f,.01f,.1f), new Color(.55f,.4f,.15f), "Daun kering cepat terurai dan menyuburkan tanah."),
            new TrashDef("Kertas Bekas", TrashType.Kertas, PrimitiveType.Cube, new Vector3(.2f,.01f,.28f), Color.white, "Daur ulang 1 ton kertas menyelamatkan sekitar 17 pohon."),
            new TrashDef("Kardus", TrashType.Kertas, PrimitiveType.Cube, new Vector3(.2f,.15f,.2f), new Color(.7f,.5f,.3f), "Kardus bisa didaur ulang hingga 7 kali."),
            new TrashDef("Koran", TrashType.Kertas, PrimitiveType.Cube, new Vector3(.25f,.02f,.3f), new Color(.8f,.8f,.75f), "Koran bekas bisa diolah jadi kertas baru atau kerajinan."),
            new TrashDef("Kaleng Minuman", TrashType.Logam, PrimitiveType.Cylinder, new Vector3(.065f,.06f,.065f), new Color(.75f,.75f,.8f), "Kaleng aluminium bisa didaur ulang tanpa batas!"),
            new TrashDef("Kaleng Sarden", TrashType.Logam, PrimitiveType.Cylinder, new Vector3(.1f,.02f,.1f), new Color(.85f,.3f,.2f), "Bilas kaleng makanan sebelum dibuang ke tempat logam."),
            new TrashDef("Tutup Botol", TrashType.Logam, PrimitiveType.Cylinder, new Vector3(.04f,.008f,.04f), new Color(.9f,.75f,.2f), "Tutup botol logam termasuk sampah logam."),
        };

        static readonly Dictionary<TrashType, Color> BinColors = new Dictionary<TrashType, Color>
        {
            { TrashType.Organik, new Color(.15f,.6f,.25f) },
            { TrashType.Plastik, new Color(.95f,.75f,.1f) },
            { TrashType.Kertas,  new Color(.15f,.4f,.85f) },
            { TrashType.Logam,   new Color(.8f,.15f,.15f) },
        };

        [MenuItem("Tools/Trash Sorting/Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            System.IO.Directory.CreateDirectory(MatFolder);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(.6f, .65f, .7f);

            BuildEnvironment();
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            BuildBins();
            BuildTrash();
            BuildUI(gm);
            SetupXRRig();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("<color=lime>[Trash Sorting]</color> Scene selesai dibuat: " + ScenePath);
        }

        // ---------- Environment ----------
        static void BuildEnvironment()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor"; floor.transform.localScale = new Vector3(3, 1, 3);
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Floor", new Color(.45f, .65f, .3f));

            var path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Path"; path.transform.position = new Vector3(0, .005f, 1.5f);
            path.transform.localScale = new Vector3(3f, .01f, 6f);
            path.GetComponent<Renderer>().sharedMaterial = Mat("Path", new Color(.85f, .7f, .45f));

            // Pohon & semak di sekeliling (pakai model Poly Pizza kalau ada)
            var trunkMat = Mat("Trunk", new Color(.4f, .25f, .12f));
            var leafMat = Mat("Leaves", new Color(.2f, .5f, .2f));
            var rnd = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f, r = 8f + (float)rnd.NextDouble() * 4f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                var tree = new GameObject("Tree"); tree.transform.position = p;
                if (SpawnModel("Pohon", tree.transform, 3.5f + (float)rnd.NextDouble() * 1.5f) != null) continue;
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(tree.transform, false);
                trunk.transform.localPosition = new Vector3(0, 1, 0); trunk.transform.localScale = new Vector3(.3f, 1, .3f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(tree.transform, false);
                leaf.transform.localPosition = new Vector3(0, 2.6f, 0); leaf.transform.localScale = Vector3.one * 2f;
                leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
            }
            for (int i = 0; i < 10; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 6.5f + (float)rnd.NextDouble() * 1.5f;
                var bush = new GameObject("Bush");
                bush.transform.SetPositionAndRotation(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), Quaternion.Euler(0, a * 57f, 0));
                SpawnModel("Semak", bush.transform, .9f);
            }
            foreach (var bp in new[] { new Vector3(-3.5f, 0, -1f), new Vector3(3.5f, 0, -1f) })
            {
                var bench = new GameObject("Bench");
                bench.transform.SetPositionAndRotation(bp, Quaternion.Euler(0, bp.x < 0 ? 90 : -90, 0));
                var bm = SpawnModel("Bangku", bench.transform, 1.6f);
                if (bm) bench.AddComponent<BoxCollider>().size = new Vector3(1.6f, .9f, .6f);
            }

            // Papan petunjuk cara bermain untuk anak-anak
            var sign = WorldCanvas(null, "HowToPlay", new Vector3(-2.6f, 1.5f, 1.2f), new Vector2(700, 600), .002f);
            sign.transform.rotation = Quaternion.Euler(0, -60, 0);
            sign.gameObject.AddComponent<Image>().color = new Color(1f, .97f, .85f, .95f);
            Label(sign.transform, "AYO JAGA LINGKUNGAN!", 48, new Color(.1f, .5f, .2f), new Vector2(0, 240), new Vector2(680, 80));
            Label(sign.transform,
                "1. Cari sampah di sekitarmu\n2. Tekan tombol GRIP untuk mengambil\n3. Bawa ke tong sampah yang benar\n4. Lepas di atas tong\n\n" +
                "HIJAU = Organik (sisa makanan, daun)\nKUNING = Plastik (botol, kantong)\nBIRU = Kertas (koran, kardus)\nMERAH = Logam (kaleng)",
                32, new Color(.15f, .15f, .2f), new Vector2(0, -40), new Vector2(660, 460)).alignment = TextAnchor.UpperLeft;
        }

        // ---------- Bins ----------
        static void BuildBins()
        {
            var types = (TrashType[])System.Enum.GetValues(typeof(TrashType));
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var bin = new GameObject("Bin_" + type);
                bin.transform.position = new Vector3(-1.8f + i * 1.2f, 0, 3f);
                var mat = Mat("Bin_" + type, BinColors[type]);
                const float w = .6f, h = .8f, th = .04f;

                Part(bin, "Bottom", new Vector3(0, th / 2, 0), new Vector3(w, th, w), mat);
                Part(bin, "WallF", new Vector3(0, h / 2, -w / 2), new Vector3(w, h, th), mat);
                Part(bin, "WallB", new Vector3(0, h / 2, w / 2), new Vector3(w, h, th), mat);
                Part(bin, "WallL", new Vector3(-w / 2, h / 2, 0), new Vector3(th, h, w), mat);
                Part(bin, "WallR", new Vector3(w / 2, h / 2, 0), new Vector3(th, h, w), mat);
                var rim = Part(bin, "Rim", new Vector3(0, h + .01f, -w / 2), new Vector3(w + .04f, .03f, th + .02f), Mat("Rim_" + type, BinColors[type] * 1.3f));
                Object.DestroyImmediate(rim.GetComponent<Collider>());

                var trig = new GameObject("SortTrigger");
                trig.transform.SetParent(bin.transform, false);
                trig.transform.localPosition = new Vector3(0, h * .55f, 0);
                var box = trig.AddComponent<BoxCollider>();
                box.size = new Vector3(w - .1f, h * .8f, w - .1f); box.isTrigger = true;
                var tb = trig.AddComponent<TrashBin>();
                tb.acceptedType = type;
                tb.highlightRenderer = rim.GetComponent<Renderer>();

                // Label
                var canvas = WorldCanvas(bin.transform, "Label", new Vector3(0, h + .25f, -w / 2), new Vector2(400, 120), .0015f);
                var bg = canvas.gameObject.AddComponent<Image>(); bg.color = BinColors[type];
                Label(canvas.transform, type.ToString().ToUpper(), 70, Color.white, Vector2.zero, new Vector2(400, 120));
            }
        }

        static GameObject Part(GameObject parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        // ---------- Trash ----------
        static void BuildTrash()
        {
            var root = new GameObject("Trash");
            // Sampah berserakan di tanah sekitar pemain (setiap jenis muncul 2x)
            var rnd = new System.Random(42);
            for (int i = 0; i < Trash.Count * 2; i++)
            {
                var d = Trash[i % Trash.Count];
                ModelKeys.TryGetValue(d.name, out var key);
                float size = Mathf.Max(d.scale.x, d.scale.y, d.scale.z) * (d.shape == PrimitiveType.Cube ? 1f : 2f);
                size = Mathf.Max(size, .12f); // jangan terlalu kecil untuk tangan anak-anak

                GameObject g = new GameObject(d.name);
                var model = key != null ? SpawnModel(key, g.transform, size) : null;
                if (model != null)
                {
                    var b = LocalBounds(g);
                    var bc = g.AddComponent<BoxCollider>();
                    bc.center = b.center; bc.size = Vector3.Max(b.size, Vector3.one * .03f);
                }
                else
                {
                    Object.DestroyImmediate(g);
                    g = GameObject.CreatePrimitive(d.shape);
                    g.name = d.name;
                    g.transform.localScale = d.scale;
                    g.GetComponent<Renderer>().sharedMaterial = Mat("Trash_" + d.name.Replace(" ", ""), d.color);
                    if (g.GetComponent<MeshCollider>() is MeshCollider mc) mc.convex = true;
                }
                g.transform.SetParent(root.transform);

                Vector3 pos;
                do
                {
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 1.2f + (float)rnd.NextDouble() * 4.5f;
                    pos = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                } while (pos.z > 2.2f && pos.z < 4f && Mathf.Abs(pos.x) < 2.6f); // jangan di area tong
                pos.y = model != null ? .03f : d.scale.y + .05f;
                g.transform.position = pos;
                g.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, model == null && d.shape == PrimitiveType.Capsule ? 90 : 0);

                var rb = g.AddComponent<Rigidbody>();
                rb.mass = .2f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                var grab = g.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach = true;
                grab.useDynamicAttach = true;

                var ti = g.AddComponent<TrashItem>();
                ti.displayName = d.name; ti.type = d.type; ti.info = d.info;
            }
        }

        // ---------- UI ----------
        static void BuildUI(GameManager gm)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));

            var board = WorldCanvas(null, "ScoreBoard", new Vector3(0, 2.1f, 3.6f), new Vector2(1000, 500), .002f);
            board.gameObject.AddComponent<Image>().color = new Color(.05f, .1f, .15f, .85f);
            Label(board.transform, "PILAH SAMPAH!", 70, new Color(.5f, 1f, .6f), new Vector2(0, 190), new Vector2(1000, 100));
            gm.scoreText = Label(board.transform, "Skor: 0", 55, Color.white, new Vector2(-300, 90), new Vector2(380, 80));
            gm.timerText = Label(board.transform, "Waktu: 3:00", 55, Color.white, new Vector2(0, 90), new Vector2(380, 80));
            gm.remainingText = Label(board.transform, "Sisa: 12", 55, Color.white, new Vector2(320, 90), new Vector2(400, 80));
            gm.feedbackText = Label(board.transform, "", 48, Color.white, new Vector2(0, -20), new Vector2(960, 120));
            gm.infoText = Label(board.transform, "", 38, new Color(.85f, .9f, 1f), new Vector2(0, -160), new Vector2(940, 140));
            gm.infoText.fontStyle = FontStyle.Italic;

            // Panel game over
            var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(board.transform, false);
            var prt = (RectTransform)panel.transform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.02f, .05f, .1f, .97f);
            gm.gameOverPanel = panel;
            gm.gameOverText = Label(panel.transform, "", 60, Color.white, new Vector2(0, 70), new Vector2(950, 320));

            var btn = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(panel.transform, false);
            var brt = (RectTransform)btn.transform; brt.sizeDelta = new Vector2(360, 100); brt.anchoredPosition = new Vector2(0, -170);
            btn.GetComponent<Image>().color = new Color(.2f, .7f, .35f);
            Label(btn.transform, "MAIN LAGI", 50, Color.white, Vector2.zero, new Vector2(360, 100));
            UnityEventTools.AddPersistentListener(btn.GetComponent<Button>().onClick, gm.RestartGame);
        }

        static Canvas WorldCanvas(Transform parent, string name, Vector3 pos, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform; rt.sizeDelta = size;
            go.transform.localScale = Vector3.one * scale;
            return c;
        }

        static Text Label(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // ---------- XR Rig ----------
        static void SetupXRRig()
        {
            string prefabPath = null;
            foreach (var guid in AssetDatabase.FindAssets("XR Origin (XR Rig) t:Prefab"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("XR Origin (XR Rig).prefab")) { prefabPath = p; break; }
            }

            if (prefabPath == null)
            {
                EditorUtility.DisplayDialog("XR Rig belum ada",
                    "Prefab 'XR Origin (XR Rig)' tidak ditemukan.\n\n" +
                    "Buka Window > Package Manager > XR Interaction Toolkit > Samples > Import 'Starter Assets', " +
                    "lalu jalankan menu Tools > Trash Sorting > Build Scene lagi.", "OK");
                return;
            }

            var cam = Camera.main; if (cam) Object.DestroyImmediate(cam.gameObject);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            rig.transform.position = new Vector3(0, 0, -.3f);

            // Interaction Manager (kalau rig tidak membawanya)
            if (Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        }

        // ---------- Util ----------
        static Material Mat(string name, Color c)
        {
            string path = $"{MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = c;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
