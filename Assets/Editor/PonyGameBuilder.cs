using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// One-click builder: creates sprites, prefabs, animations, UI and both scenes.
// Runs automatically the first time the project loads, or via  Pony Game > Build Everything.
public static class PonyGameBuilder
{
    static Font font;
    static readonly Color Pink = new Color(1f, 0.30f, 0.64f);
    static readonly Color Plum = new Color(0.70f, 0.27f, 0.49f);

    [InitializeOnLoadMethod]
    static void AutoRun()
    {
        if (File.Exists("Assets/Scenes/Game.unity")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists("Assets/Art/pony.png")) return;
            Build();
        };
    }

    [MenuItem("Pony Game/Build Everything")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Animations");
        SetupTextures();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Poppins-Bold.ttf");
        AddSortingLayers(new[] { "Background", "Clouds", "Pony", "Items", "Effects" });

        Material sparkleMat = MakeSparkleMaterial();
        GameObject sparkle = MakeSparkleBurst(sparkleMat);
        GameObject confetti = MakeConfetti(sparkleMat);
        AnimatorController pony = BuildPonyController();

        GameObject heart = MakeItem("Heart", "heart", FallingItem.Kind.Heart, 1, 0.36f, 1.0f);
        GameObject star = MakeItem("Star", "star", FallingItem.Kind.Star, 2, 0.34f, 1.0f);
        GameObject cupcake = MakeItem("Cupcake", "cupcake", FallingItem.Kind.Cupcake, 3, 0.34f, 1.0f);
        GameObject storm = MakeItem("StormCloud", "storm", FallingItem.Kind.Storm, 0, 0.42f, 1.0f);

        BuildGameScene(pony, sparkle, confetti, sparkleMat, heart, star, cupcake, storm);
        BuildMenuScene(pony, sparkleMat);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        Debug.Log("Sparkle Pony built! Press Play to start.");
    }

    // ---------- assets ----------
    static void SetupTextures()
    {
        AssetDatabase.Refresh();
        foreach (string f in Directory.GetFiles("Assets/Art", "*.png"))
        {
            string path = f.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
    }

    static Sprite S(string n) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/" + n + ".png"); }

    static void AddSortingLayers(string[] names)
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (all == null || all.Length == 0) return;
        SerializedObject tm = new SerializedObject(all[0]);
        SerializedProperty layers = tm.FindProperty("m_SortingLayers");
        foreach (string n in names)
        {
            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) exists = true;
            if (exists) continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);
            SerializedProperty el = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            el.FindPropertyRelative("name").stringValue = n;
            el.FindPropertyRelative("uniqueID").intValue = Mathf.Abs(n.GetHashCode()) % 100000 + 1000;
            el.FindPropertyRelative("locked").intValue = 0;
        }
        tm.ApplyModifiedProperties();
    }

    static bool HasLayer(string n)
    {
        return System.Array.Exists(SortingLayer.layers, l => l.name == n);
    }

    static void Layer(SpriteRenderer sr, string layer, int order)
    {
        if (HasLayer(layer)) sr.sortingLayerName = layer;
        sr.sortingOrder = order;
    }

    static Material MakeSparkleMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material m = new Material(sh);
        m.mainTexture = S("sparkle").texture;
        AssetDatabase.CreateAsset(m, "Assets/Materials/Sparkle.mat");
        return m;
    }

    static void StyleRenderer(ParticleSystem ps, Material mat, int order)
    {
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        if (HasLayer("Effects")) r.sortingLayerName = "Effects";
        r.sortingOrder = order;
    }

    static GameObject MakeSparkleBurst(Material mat)
    {
        GameObject go = new GameObject("CatchSparkle");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.8f), new Color(1f, 0.9f, 0.45f));
        main.gravityModifier = 0.25f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.2f;
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        StyleRenderer(ps, mat, 20);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/CatchSparkle.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject MakeConfetti(Material mat)
    {
        GameObject go = new GameObject("Confetti");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.gravityModifier = 0.35f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        Gradient rainbow = new Gradient();
        rainbow.SetKeys(new[]
        {
            new GradientColorKey(new Color(1f, 0.43f, 0.62f), 0f), new GradientColorKey(new Color(1f, 0.89f, 0.48f), 0.25f),
            new GradientColorKey(new Color(0.56f, 0.89f, 0.66f), 0.5f), new GradientColorKey(new Color(0.5f, 0.82f, 1f), 0.75f),
            new GradientColorKey(new Color(0.72f, 0.55f, 1f), 1f)
        }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        ParticleSystem.MinMaxGradient mg = new ParticleSystem.MinMaxGradient(rainbow);
        mg.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = mg;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 40f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(18f, 0.5f, 1f);
        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
        StyleRenderer(ps, mat, 30);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Confetti.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject MakeAmbient(Material mat)
    {
        GameObject go = new GameObject("AmbientSparkles");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f), new Color(1f, 0.85f, 0.45f));
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 5f;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(19f, 10f, 1f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        StyleRenderer(ps, mat, -5);
        return go;
    }

    static AnimationCurve Curve(params float[] tv)
    {
        Keyframe[] keys = new Keyframe[tv.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(tv[i * 2], tv[i * 2 + 1]);
        AnimationCurve c = new AnimationCurve(keys);
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    static AnimationClip MakeClip(string name, bool loop, params object[] curves)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 60f;
        for (int i = 0; i < curves.Length; i += 2)
            clip.SetCurve("", typeof(Transform), (string)curves[i], (AnimationCurve)curves[i + 1]);
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/" + name + ".anim");
        return clip;
    }

    static AnimatorController BuildPonyController()
    {
        AnimationClip idle = MakeClip("PonyIdle", true,
            "localPosition.y", Curve(0f, 0f, 0.6f, 0.07f, 1.2f, 0f),
            "localScale.y", Curve(0f, 1f, 0.6f, 1.025f, 1.2f, 1f),
            "localScale.x", Curve(0f, 1f, 0.6f, 0.99f, 1.2f, 1f));
        AnimationClip happy = MakeClip("PonyHappy", false,
            "localPosition.y", Curve(0f, 0f, 0.22f, 0.9f, 0.5f, 0f),
            "localScale.y", Curve(0f, 1f, 0.08f, 0.9f, 0.22f, 1.12f, 0.5f, 1f),
            "localScale.x", Curve(0f, 1f, 0.08f, 1.08f, 0.22f, 0.92f, 0.5f, 1f));
        AnimationClip ouch = MakeClip("PonyOuch", false,
            "localEulerAnglesRaw.z", Curve(0f, 0f, 0.08f, 10f, 0.16f, -10f, 0.24f, 8f, 0.32f, -6f, 0.4f, 0f));

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/PonyController.controller");
        ctrl.AddParameter("Happy", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Ouch", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        AnimatorState sHappy = sm.AddState("Happy"); sHappy.motion = happy;
        AnimatorState sOuch = sm.AddState("Ouch"); sOuch.motion = ouch;
        sm.defaultState = sIdle;

        AnimatorStateTransition a = sm.AddAnyStateTransition(sHappy);
        a.AddCondition(AnimatorConditionMode.If, 0f, "Happy");
        a.hasExitTime = false; a.duration = 0.02f; a.canTransitionToSelf = true;
        AnimatorStateTransition b = sm.AddAnyStateTransition(sOuch);
        b.AddCondition(AnimatorConditionMode.If, 0f, "Ouch");
        b.hasExitTime = false; b.duration = 0.02f; b.canTransitionToSelf = true;

        AnimatorStateTransition r1 = sHappy.AddTransition(sIdle);
        r1.hasExitTime = true; r1.exitTime = 1f; r1.duration = 0.05f;
        AnimatorStateTransition r2 = sOuch.AddTransition(sIdle);
        r2.hasExitTime = true; r2.exitTime = 1f; r2.duration = 0.05f;
        return ctrl;
    }

    static GameObject MakeItem(string name, string sprite, FallingItem.Kind kind, int pts, float scale, float radius)
    {
        GameObject go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(sprite);
        Layer(sr, "Items", 5);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0.3f;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = kind == FallingItem.Kind.Storm ? 0.85f : radius;
        FallingItem fi = go.AddComponent<FallingItem>();
        fi.kind = kind;
        fi.points = pts;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- scene helpers ----------
    static Camera MakeCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1f, 0.78f, 0.89f);
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    static SpriteRenderer MakeSprite(string name, string sprite, Vector3 pos, float scale, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(sprite);
        Layer(sr, layer, order);
        return sr;
    }

    static void MakeClouds()
    {
        float[] xs = { -7f, 0.5f, 7f, -2.5f };
        float[] ys = { 4.0f, 3.0f, 4.4f, 1.6f };
        float[] sc = { 0.9f, 0.7f, 1.0f, 0.5f };
        float[] sp = { 0.35f, 0.25f, 0.45f, 0.2f };
        for (int i = 0; i < xs.Length; i++)
        {
            SpriteRenderer sr = MakeSprite("Cloud" + i, "cloud", new Vector3(xs[i], ys[i], 0f), sc[i], "Clouds", i);
            sr.color = new Color(1f, 1f, 1f, 0.85f);
            sr.gameObject.AddComponent<CloudDrift>().speed = sp[i];
        }
    }

    static GameObject MakePony(AnimatorController ctrl, float scale)
    {
        GameObject root = new GameObject("Pony");
        root.transform.localScale = Vector3.one * scale;
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = S("pony");
        Layer(sr, "Pony", 0);
        Animator anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        return root;
    }

    // ---------- UI helpers ----------
    static Canvas MakeCanvas()
    {
        GameObject go = new GameObject("Canvas");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static void MakeEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule m = go.AddComponent<InputSystemUIInputModule>();
        m.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image MakeImage(string name, Transform parent, string sprite, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, anchor, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        if (sprite != null) img.sprite = S(sprite);
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(string name, Transform parent, string s, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        RectTransform rt = Rect(name, parent, anchor, pos, box);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Plum;
        o.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, UnityAction onClick)
    {
        RectTransform rt = Rect(name, parent, anchor, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = S("button");
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.96f, 1f);
        cb.pressedColor = new Color(0.9f, 0.82f, 0.92f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        rt.gameObject.AddComponent<ButtonPop>();
        MakeText("Label", rt, label, fontSize, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), size);
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    static GameObject MakePanel(string name, Transform parent, string title, string subtitle, int titleSize)
    {
        RectTransform root = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(root);
        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0.45f, 0.1f, 0.3f, 0.45f);
        MakeImage("Card", root, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 640f));
        MakeText("Title", root, title, titleSize, Pink, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(820f, 160f)).GetComponent<Outline>().effectColor = Color.white;
        MakeText("Subtitle", root, subtitle, 40, Plum, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(760f, 200f)).GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0f);
        return root.gameObject;
    }

    // ---------- scenes ----------
    static void BuildGameScene(AnimatorController ctrl, GameObject sparkle, GameObject confetti, Material mat,
        GameObject heart, GameObject star, GameObject cupcake, GameObject storm)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        MakeSprite("Background", "bg", Vector3.zero, 1f, "Background", -10);
        MakeClouds();
        MakeAmbient(mat);

        GameObject pony = MakePony(ctrl, 0.4f);
        pony.transform.position = new Vector3(0f, -2.95f, 0f);
        Rigidbody2D rb = pony.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D box = pony.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.6f, 3.8f);
        box.offset = new Vector2(0.1f, 0.7f);
        PonyController pc = pony.AddComponent<PonyController>();
        pc.animator = pony.GetComponentInChildren<Animator>();
        pc.sprite = pony.GetComponentInChildren<SpriteRenderer>();
        pc.sparklePrefab = sparkle;

        GameObject spObj = new GameObject("Spawner");
        Spawner sp = spObj.AddComponent<Spawner>();
        sp.prefabs = new[] { heart, star, cupcake, storm };
        sp.weights = new[] { 50f, 16f, 10f, 22f };

        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        AudioSource audio = gmObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        gm.audioSource = audio;
        gm.spawner = sp;
        gm.confettiPrefab = confetti;
        gm.winClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Win.wav");
        gm.loseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Lose.wav");
        pc.audioSource = audio;
        pc.catchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Catch.wav");
        pc.ouchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ouch.wav");

        // UI
        Canvas canvas = MakeCanvas();
        MakeEventSystem();
        Transform cv = canvas.transform;
        Vector2 tl = new Vector2(0f, 1f), tr = new Vector2(1f, 1f), bc = new Vector2(0.5f, 0f);

        Image pill = MakeImage("ScorePill", cv, "button", tl, new Vector2(250f, -75f), new Vector2(400f, 112f));
        MakeImage("StarIcon", pill.transform, "star", new Vector2(0.5f, 0.5f), new Vector2(-135f, 4f), new Vector2(76f, 76f));
        gm.scoreText = MakeText("ScoreText", pill.transform, "0 / 20", 54, Color.white, new Vector2(0.5f, 0.5f), new Vector2(30f, 6f), new Vector2(260f, 90f));

        Image barBg = MakeImage("ProgressBg", cv, "button", tl, new Vector2(250f, -160f), new Vector2(400f, 36f));
        barBg.color = new Color(1f, 1f, 1f, 0.55f);
        Image fill = MakeImage("ProgressFill", barBg.transform, "button", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform frt = fill.rectTransform;
        Stretch(frt);
        frt.offsetMin = new Vector2(5f, 5f); frt.offsetMax = new Vector2(-5f, -5f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        gm.progressFill = fill;

        gm.heartIcons = new Image[3];
        for (int i = 0; i < 3; i++)
            gm.heartIcons[i] = MakeImage("Life" + i, cv, "heart", tr, new Vector2(-250f + 100f * i, -75f), new Vector2(86f, 86f));

        MakeText("Hint", cv, "Move with  <  >  or  A  D  (or drag)", 34, Color.white, bc, new Vector2(0f, 45f), new Vector2(1000f, 60f));

        GameObject win = MakePanel("WinPanel", cv, "You did it!", "The whole rainbow is full of hearts!", 110);
        GameObject lose = MakePanel("LosePanel", cv, "Oh no!", "The storm clouds won this time.\nTry again, you can do it!", 120);
        MakeButton("PlayAgain", win.transform, "Play Again", new Vector2(0.5f, 0.5f), new Vector2(-300f, -180f), new Vector2(280f, 96f), 36, gm.Restart);
        MakeButton("Menu", win.transform, "Menu", new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(280f, 96f), 36, gm.GoToMenu);
        MakeButton("Quit", win.transform, "Quit", new Vector2(0.5f, 0.5f), new Vector2(300f, -180f), new Vector2(280f, 96f), 36, gm.QuitGame);
        MakeButton("TryAgain", lose.transform, "Try Again", new Vector2(0.5f, 0.5f), new Vector2(-300f, -180f), new Vector2(280f, 96f), 36, gm.Restart);
        MakeButton("Menu", lose.transform, "Menu", new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(280f, 96f), 36, gm.GoToMenu);
        MakeButton("Quit", lose.transform, "Quit", new Vector2(0.5f, 0.5f), new Vector2(300f, -180f), new Vector2(280f, 96f), 36, gm.QuitGame);
        win.SetActive(false);
        lose.SetActive(false);
        gm.winPanel = win;
        gm.losePanel = lose;

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/Game.unity");
    }

    static void BuildMenuScene(AnimatorController ctrl, Material mat)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        MakeSprite("Background", "bg", Vector3.zero, 1f, "Background", -10);
        MakeClouds();
        MakeAmbient(mat);

        GameObject pony = MakePony(ctrl, 0.75f);
        pony.transform.position = new Vector3(-5.2f, -1.9f, 0f);

        string[] deco = { "heart", "star", "cupcake", "heart", "star" };
        float[] dx = { -7.6f, -2.6f, 7.4f, 2.2f, 8.0f };
        float[] dy = { 1.2f, 0.2f, 2.4f, -3.4f, -2.2f };
        for (int i = 0; i < deco.Length; i++)
        {
            SpriteRenderer sr = MakeSprite("Deco" + i, deco[i], new Vector3(dx[i], dy[i], 0f), 0.3f, "Items", 1);
            Floater f = sr.gameObject.AddComponent<Floater>();
            f.amplitude = 0.2f; f.speed = 1.2f + i * 0.2f; f.pulse = 0.05f;
        }

        Canvas canvas = MakeCanvas();
        MakeEventSystem();
        Transform cv = canvas.transform;
        GameObject mcObj = new GameObject("MenuController");
        MenuController mc = mcObj.AddComponent<MenuController>();

        Image logo = MakeImage("Logo", cv, "logo", new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1100f, 330f));
        Floater lf = logo.gameObject.AddComponent<Floater>();
        lf.amplitude = 8f; lf.speed = 1.4f;

        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 bsz = new Vector2(440f, 126f);
        MakeButton("PlayButton", cv, "Play", mid, new Vector2(420f, -20f), bsz, 56, mc.Play);
        MakeButton("HowToButton", cv, "How to Play", mid, new Vector2(420f, -170f), bsz, 46, mc.ShowHowToOn);
        MakeButton("QuitButton", cv, "Quit", mid, new Vector2(420f, -320f), bsz, 56, mc.QuitGame);

        GameObject how = MakePanel("HowToPanel", cv, "How to Play", "Move the pony with the arrow keys or A / D (or drag with the mouse).\nCatch hearts, stars and cupcakes!\nDodge the grumpy storm clouds.\nCollect 20 points to win!", 100);
        Text body = how.transform.Find("Subtitle").GetComponent<Text>();
        body.fontSize = 36;
        body.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 260f);
        body.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
        MakeButton("GotIt", how.transform, "Got it!", mid, new Vector2(0f, -200f), new Vector2(320f, 100f), 40, mc.ShowHowToOff);
        how.SetActive(false);
        mc.howToPanel = how;

        RectTransform overlay = Rect("LoadingOverlay", cv, mid, Vector2.zero, Vector2.zero);
        Stretch(overlay);
        Image oimg = overlay.gameObject.AddComponent<Image>();
        oimg.color = new Color(1f, 0.74f, 0.87f, 1f);
        Image spin = MakeImage("Spinner", overlay, "heart", mid, new Vector2(0f, 70f), new Vector2(190f, 190f));
        spin.gameObject.AddComponent<Spinner>();
        Text lt = MakeText("LoadingText", overlay, "Loading", 84, Color.white, mid, new Vector2(0f, -120f), new Vector2(700f, 130f));
        overlay.gameObject.SetActive(false);
        mc.loadingOverlay = overlay.gameObject;
        mc.loadingText = lt;

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/Menu.unity");
    }
}
