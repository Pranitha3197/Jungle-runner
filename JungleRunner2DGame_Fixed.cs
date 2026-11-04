using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main controller for the 2D Jungle Runner prototype.
/// Creates a simple playable loop: menu, run, coins, hazards, score, timer, pause and game over.
/// </summary>
public class JungleRunner2DGame : MonoBehaviour
{
    public static JungleRunner2DGame Instance { get; private set; }

    [Header("Run settings")]
    public float startSpeed = 7f;
    public float maxSpeed = 16f;
    public float speedIncreasePerSecond = 0.12f;
    public float spawnDistance = 13f;
    public float groundY = -2.2f;

    [Header("Runtime stats")]
    public int coins { get; private set; }
    public int score { get; private set; }
    public float distance { get; private set; }
    public float elapsed { get; private set; }
    public float CurrentSpeed { get; private set; }

    private enum State { Menu, Running, Paused, GameOver }
    private State state = State.Menu;
    private Transform world;
    private Transform player;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private Text titleText, hudText, hintText, buttonText, summaryText;
    private GameObject overlay, mainButton, secondButton;
    private bool grounded;
    private float spawnTimer;
    private float nextSpawn;
    private float jumpBuffer;
    private float groundCheckTimer;
    private float slideTimer;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        CurrentSpeed = startSpeed;
        BuildWorld();
        BuildPlayer();
        BuildUI();
        ShowMenu();
    }

    void Update()
    {
        if (state != State.Running) return;

        elapsed += Time.deltaTime;
        CurrentSpeed = Mathf.Min(maxSpeed, startSpeed + elapsed * speedIncreasePerSecond);
        distance += CurrentSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distance) + coins * 10;

        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            Jump();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            Slide();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawn)
        {
            SpawnPattern();
            spawnTimer = 0f;
            nextSpawn = Random.Range(1.05f, 1.75f) * Mathf.Clamp(9f / CurrentSpeed, 0.65f, 1.1f);
        }

        UpdateHUD();
        MoveWorld();
        if (slideTimer > 0f) slideTimer -= Time.deltaTime;
    }

    void BuildWorld()
    {
        world = new GameObject("Scrolling Jungle World").transform;

        Camera cam = Camera.main;
        if (!cam) {
            GameObject c = new GameObject("Main Camera");
            cam = c.AddComponent<Camera>();
            c.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.55f, 0.78f, 0.88f);

        // Distant hills and layered jungle silhouettes.
        MakeRect("Far Jungle Canopy", new Vector2(0, 1.4f), new Vector2(40, 2.4f), new Color(0.12f, 0.34f, 0.22f), -5, world);
        MakeRect("Near Jungle Undergrowth", new Vector2(0, -0.2f), new Vector2(40, 1.4f), new Color(0.08f, 0.25f, 0.16f), -4, world);
        MakeRect("Runner Trail", new Vector2(0, groundY - 0.25f), new Vector2(40, 5.2f), new Color(0.47f, 0.29f, 0.15f), -2, world);
        MakeRect("Trail Highlight", new Vector2(0, groundY + 2.25f), new Vector2(40, 0.12f), new Color(0.68f, 0.47f, 0.25f), -1, world);

        // Repeating decorative trees on both sides.
        for (int i = 0; i < 18; i++)
        {
            float x = -10f + i * 1.25f;
            MakeTree(new Vector2(x, Random.Range(-0.1f, 1.2f)), Random.Range(0.75f, 1.2f), world, -3);
        }
        // Bushes and fern-like decorations around the track.
        for (int i = 0; i < 26; i++)
        {
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-1.25f, 1.1f);
            if (Mathf.Abs(y - groundY) < 1.6f) continue;
            MakeCircle("Jungle Bush", new Vector2(x, y), Random.Range(0.35f, 0.7f),
                new Color(0.10f, Random.Range(0.28f, 0.48f), 0.13f), -3, world);
        }
    }

    void BuildPlayer()
    {
        GameObject p = new GameObject("Runner Player");
        p.transform.position = new Vector3(-3f, groundY + 0.78f, 0);
        player = p.transform;
        SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSprite(Color.white);
        sr.color = new Color(0.96f, 0.68f, 0.32f);
        sr.sortingOrder = 20;
        p.transform.localScale = new Vector3(0.58f, 1.2f, 1f);

        playerBody = p.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = 3.2f;
        playerBody.freezeRotation = true;
        playerCollider = p.AddComponent<CapsuleCollider2D>();
        playerCollider.isTrigger = false;

        GameObject ground = new GameObject("Ground Collider");
        ground.transform.position = new Vector3(0, groundY - 0.18f, 0);
        ground.transform.SetParent(world);
        BoxCollider2D box = ground.AddComponent<BoxCollider2D>();
        box.size = new Vector2(100f, 0.35f);
        ground.AddComponent<GroundMarker>();
        p.AddComponent<JungleRunner2DPlayer>();
    }

    void BuildUI()
    {
        GameObject canvasGo = new GameObject("Jungle Runner UI");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();

        hudText = MakeText(canvasGo.transform, "HUD", "DISTANCE 0 m     SCORE 0     🟡 COINS 0     TIME 0:00", 23,
            new Vector2(0.5f, 0.94f), new Vector2(0.95f, 0.09f), TextAnchor.MiddleCenter, new Color(1f, 0.97f, 0.78f));
        hudText.fontStyle = FontStyle.Bold;

        overlay = new GameObject("Menu Overlay");
        overlay.transform.SetParent(canvasGo.transform, false);
        Image shade = overlay.AddComponent<Image>();
        shade.color = new Color(0.015f, 0.08f, 0.055f, 0.82f);
        RectTransform or = overlay.GetComponent<RectTransform>();
        or.anchorMin = Vector2.zero; or.anchorMax = Vector2.one; or.offsetMin = Vector2.zero; or.offsetMax = Vector2.zero;

        titleText = MakeText(overlay.transform, "Title", "JUNGLE RUNNER", 58,
            new Vector2(0.5f, 0.73f), new Vector2(0.86f, 0.16f), TextAnchor.MiddleCenter, new Color(0.86f, 1f, 0.78f));
        titleText.fontStyle = FontStyle.Bold;
        hintText = MakeText(overlay.transform, "Subtitle", "RUN WILD  •  DODGE HAZARDS  •  COLLECT GOLD", 22,
            new Vector2(0.5f, 0.59f), new Vector2(0.9f, 0.08f), TextAnchor.MiddleCenter, Color.white);
        summaryText = MakeText(overlay.transform, "Summary", "", 22,
            new Vector2(0.5f, 0.43f), new Vector2(0.85f, 0.12f), TextAnchor.MiddleCenter, Color.white);

        mainButton = MakeButton(overlay.transform, "Main Action", "START RUN", new Vector2(0.5f, 0.31f), StartRun);
        secondButton = MakeButton(overlay.transform, "Secondary Action", "QUIT", new Vector2(0.5f, 0.19f), QuitGame);
        overlay.SetActive(true);
    }

    Text MakeText(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 dimensions, TextAnchor align, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = content; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dimensions.x * 1280f, dimensions.y * 720f);
        rt.anchoredPosition = Vector2.zero;
        return t;
    }

    GameObject MakeButton(Transform parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.24f, 0.47f, 0.22f, 1f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320, 70); rt.anchoredPosition = Vector2.zero;

        Text txt = MakeText(go.transform, "Label", label, 27, new Vector2(0.5f, 0.5f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, Color.white);
        txt.fontStyle = FontStyle.Bold;
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        return go;
    }

    void ShowMenu()
    {
        state = State.Menu;
        Time.timeScale = 1f;
        overlay.SetActive(true);
        titleText.text = "JUNGLE RUNNER";
        hintText.text = "RUN WILD  •  DODGE HAZARDS  •  COLLECT GOLD";
        summaryText.text = "A 2D ENDLESS RUNNER";
        SetButton(mainButton, "START RUN", StartRun);
        SetButton(secondButton, "QUIT", QuitGame);
        hudText.text = "🟡 COINS 0";
    }

    void StartRun()
    {
        Time.timeScale = 1f;
        // Reset run data without reloading the scene (reloading would show the menu again).
        coins = 0; score = 0; distance = 0f; elapsed = 0f;
        CurrentSpeed = startSpeed; spawnTimer = 0f; nextSpawn = 1.2f;
        if (player)
        {
            player.position = new Vector3(-3f, groundY + 0.78f, 0f);
            player.localScale = new Vector3(0.58f, 1.2f, 1f);
        }
        if (playerBody) playerBody.linearVelocity = Vector2.zero;
        foreach (var hazard in FindObjectsByType<JungleRunner2DHazard>(FindObjectsSortMode.None))
            Destroy(hazard.gameObject);
        foreach (var coin in FindObjectsByType<JungleRunner2DCoin>(FindObjectsSortMode.None))
            Destroy(coin.gameObject);
        state = State.Running;
        overlay.SetActive(false);
        UpdateHUD();
    }

    void TogglePause()
    {
        if (state == State.Running)
        {
            state = State.Paused; Time.timeScale = 0f;
            overlay.SetActive(true); titleText.text = "PAUSED"; hintText.text = "TAKE A BREATH, EXPLORER";
            summaryText.text = "Press RESUME to continue";
            SetButton(mainButton, "RESUME", Resume);
            SetButton(secondButton, "RESTART", StartRun);
        }
        else if (state == State.Paused) Resume();
    }

    void Resume()
    {
        overlay.SetActive(false); state = State.Running; Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (state != State.Running) return;
        state = State.GameOver; Time.timeScale = 0f;
        overlay.SetActive(true); titleText.text = "RUN OVER";
        hintText.text = "THE JUNGLE ALWAYS OFFERS ANOTHER CHANCE";
        summaryText.text = $"SCORE  {score}     •     COINS  {coins}     •     DISTANCE  {Mathf.FloorToInt(distance)} m";
        SetButton(mainButton, "RUN AGAIN", StartRun);
        SetButton(secondButton, "MAIN MENU", ReturnToMenu);
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SetButton(GameObject go, string label, UnityEngine.Events.UnityAction action)
    {
        Button b = go.GetComponent<Button>();
        b.onClick.RemoveAllListeners(); b.onClick.AddListener(action);
        go.GetComponentInChildren<Text>().text = label;
    }

    void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void Jump()
    {
        if (playerBody && Mathf.Abs(playerBody.linearVelocity.y) < 0.05f)
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 9.5f);
    }

    void Slide()
    {
        // Simple visual duck: squash the player briefly. A production sprite animation can replace this.
        if (player && slideTimer <= 0f)
        {
            player.localScale = new Vector3(0.7f, 0.65f, 1f);
            slideTimer = 0.55f;
            StartCoroutine(RestorePlayerHeight());
        }
    }

    IEnumerator RestorePlayerHeight()
    {
        yield return new WaitForSeconds(0.55f);
        if (player) player.localScale = new Vector3(0.58f, 1.2f, 1f);
    }

    void SpawnPattern()
    {
        float x = 12f;
        int roll = Random.Range(0, 5);
        if (roll <= 2) MakeObstacle(x, roll);
        else MakeCoinLine(x, Random.Range(3, 7));
    }

    void MakeObstacle(float x, int type)
    {
        string name = type == 0 ? "Fallen Log" : type == 1 ? "Jungle Rock" : "Wooden Crate";
        Color c = type == 0 ? new Color(0.34f, 0.18f, 0.08f) :
                  type == 1 ? new Color(0.35f, 0.38f, 0.34f) : new Color(0.54f, 0.29f, 0.10f);
        Vector2 size = type == 0 ? new Vector2(1.5f, 0.45f) : type == 1 ? new Vector2(0.85f, 0.75f) : new Vector2(0.8f, 0.9f);
        GameObject o = MakeRect(name, new Vector2(x, groundY + size.y * 0.5f), size, c, 5, world);
        BoxCollider2D col = o.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        o.AddComponent<JungleRunner2DHazard>();
    }

    void MakeCoinLine(float x, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject coin = MakeCircle("Gold Coin", new Vector2(x + i * 0.62f, groundY + Random.Range(0.8f, 1.8f)),
                0.22f, new Color(1f, 0.72f, 0.08f), 10, world);
            CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            coin.AddComponent<JungleRunner2DCoin>();
        }
    }

    void MoveWorld()
    {
        // Move all spawned objects and decorative world objects left, then recycle the distant scenery.
        foreach (Transform child in world)
        {
            // Keep the path and distant layers fixed; scroll only hazards, coins and foreground vegetation.
            bool movingGameplayObject = child.GetComponent<JungleRunner2DHazard>() != null ||
                                        child.GetComponent<JungleRunner2DCoin>() != null;
            bool movingDecoration = child.name == "Jungle Tree Trunk" ||
                                    child.name == "Canopy Cluster" ||
                                    child.name == "Jungle Bush";
            if (!movingGameplayObject && !movingDecoration) continue;

            child.position += Vector3.left * CurrentSpeed * Time.deltaTime;
            if (child.position.x < -16f)
            {
                if (movingDecoration)
                    child.position = new Vector3(Random.Range(13f, 20f), child.position.y, child.position.z);
                else
                    Destroy(child.gameObject);
            }
        }
    }

    void UpdateHUD()
    {
        int secs = Mathf.FloorToInt(elapsed);
        hudText.text = $"DISTANCE  {Mathf.FloorToInt(distance)} m     SCORE  {score}     🟡 COINS  {coins}     TIME  {secs / 60:00}:{secs % 60:00}     SPEED  {CurrentSpeed:0.0}";
    }

    public void AddCoin()
    {
        coins++;
        score += 10;
    }

    GameObject MakeRect(string name, Vector2 pos, Vector2 size, Color color, int order, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        if (parent) go.transform.SetParent(parent, true);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSprite(Color.white); sr.color = color; sr.sortingOrder = order;
        return go;
    }

    GameObject MakeCircle(string name, Vector2 pos, float radius, Color color, int order, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = Vector3.one * radius * 2f;
        if (parent) go.transform.SetParent(parent, true);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(); sr.color = color; sr.sortingOrder = order;
        return go;
    }

    void MakeTree(Vector2 pos, float scale, Transform parent, int order)
    {
        MakeRect("Jungle Tree Trunk", pos + Vector2.down * 0.55f * scale, new Vector2(0.18f, 1.4f) * scale,
            new Color(0.25f, 0.12f, 0.055f), order, parent);
        MakeCircle("Canopy Cluster", pos + Vector2.up * 0.25f * scale, 0.75f * scale,
            new Color(0.06f, Random.Range(0.25f, 0.42f), 0.13f), order + 1, parent);
        MakeCircle("Canopy Cluster", pos + new Vector2(-0.35f, 0.12f) * scale, 0.55f * scale,
            new Color(0.08f, Random.Range(0.3f, 0.47f), 0.15f), order + 1, parent);
    }

    Sprite MakeSprite(Color color)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { color, color, color, color }); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    Sprite MakeCircleSprite()
    {
        int n = 64; Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = (x + .5f - n / 2f) / (n / 2f);
            float dy = (y + .5f - n / 2f) / (n / 2f);
            tex.SetPixel(x, y, dx * dx + dy * dy <= 1 ? Color.white : Color.clear);
        }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n / 2f);
    }
}

public class JungleRunner2DPlayer : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D c) { if (c.collider.GetComponent<GroundMarker>()) return; }
    void OnTriggerEnter2D(Collider2D other)
    {
        JungleRunner2DCoin coin = other.GetComponent<JungleRunner2DCoin>();
        if (coin) { coin.Collect(); return; }
        if (other.GetComponent<JungleRunner2DHazard>()) JungleRunner2DGame.Instance.GameOver();
    }
}

public class JungleRunner2DCoin : MonoBehaviour
{
    public void Collect()
    {
        if (JungleRunner2DGame.Instance) JungleRunner2DGame.Instance.AddCoin();
        Destroy(gameObject);
    }
}

public class JungleRunner2DHazard : MonoBehaviour { }
public class GroundMarker : MonoBehaviour { }