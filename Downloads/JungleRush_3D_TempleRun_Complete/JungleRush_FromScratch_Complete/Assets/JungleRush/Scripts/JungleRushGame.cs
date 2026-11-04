using System.Collections.Generic;
using UnityEngine;

public class JungleRushGame : MonoBehaviour
{
    public static JungleRushGame Instance;
    public float startSpeed = 6f;
    public float maxSpeed = 15f;
    public float speedIncrease = 0.18f;

    private Camera cam;
    private Transform player;
    private Rigidbody2D playerBody;
    private SpriteRenderer playerRenderer;
    private BoxCollider2D playerCollider;
    private readonly List<MovingThing> movingThings = new List<MovingThing>();
    private readonly List<GameObject> backgroundObjects = new List<GameObject>();
    private Sprite squareSprite, circleSprite;
    private float speed, elapsed, spawnTimer, nextSpawn = 1.25f;
    private int coins, selectedSkin;
    private bool gameOver, started;
    private bool crouching;
    private Vector3 playerStartScale;
    private GUIStyle titleStyle, hudStyle, buttonStyle, smallStyle;
    private readonly Color[] skins = {
        new Color(0.12f, 0.72f, 1f),
        new Color(1f, 0.42f, 0.16f),
        new Color(0.55f, 0.95f, 0.25f)
    };

    private class MovingThing
    {
        public GameObject root;
        public bool isCoin;
        public float radius;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        speed = startSpeed;
        cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.52f, 0.78f, 0.92f);

        squareSprite = MakeSprite(false);
        circleSprite = MakeSprite(true);
        BuildBackground();
        BuildGround();
        BuildPlayer();
        started = true;
    }

    Sprite MakeSprite(bool circle)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + .5f) / size * 2 - 1;
            float dy = (y + .5f) / size * 2 - 1;
            bool inside = !circle || dx * dx + dy * dy <= 1f;
            tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64);
    }

    GameObject Shape(string name, Vector2 pos, Vector2 size, Color color, bool circle = false, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle ? circleSprite : squareSprite;
        sr.color = color;
        return go;
    }

    void BuildBackground()
    {
        // Distant layered hills and a lush jungle canopy; all art is generated locally.
        Shape("Far Jungle Hills", new Vector2(0, -1.0f), new Vector2(40, 4.4f), new Color(.30f, .65f, .43f));
        Shape("Mid Jungle", new Vector2(0, -1.8f), new Vector2(40, 3.0f), new Color(.12f, .43f, .27f));
        for (int i = 0; i < 18; i++)
        {
            float x = -12 + i * 1.5f;
            float h = 1.5f + (i % 4) * .35f;
            Shape("Jungle Tree Trunk", new Vector2(x, -1.25f), new Vector2(.28f, h + 1f), new Color(.30f, .18f, .10f));
            Shape("Jungle Canopy", new Vector2(x, -.45f + (i % 3) * .18f), new Vector2(1.7f, h), new Color(.08f + (i % 3)*.025f, .34f + (i % 2)*.08f, .16f), true);
        }
        Shape("Ground Back Edge", new Vector2(0, -3.65f), new Vector2(40, .55f), new Color(.35f, .22f, .11f));
        Shape("Grass Surface", new Vector2(0, -3.38f), new Vector2(40, .22f), new Color(.22f, .68f, .20f));
    }

    void BuildGround()
    {
        GameObject ground = new GameObject("Ground Collider");
        ground.transform.position = new Vector2(0, -4.1f);
        BoxCollider2D bc = ground.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(80, 1);
    }

    void BuildPlayer()
    {
        GameObject root = new GameObject("Runner Player");
        root.transform.position = new Vector2(-5.1f, -2.65f);
        player = root.transform;
        playerBody = root.AddComponent<Rigidbody2D>();
        playerBody.freezeRotation = true;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerBody.gravityScale = 3.4f;
        playerCollider = root.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(.62f, 1.35f);
        playerCollider.offset = new Vector2(0, -.27f);
        playerStartScale = root.transform.localScale;

        GameObject body = Shape("Character Body", Vector2.zero, new Vector2(.58f, .78f), skins[selectedSkin], false, root.transform);
        body.transform.localPosition = new Vector3(0, -.13f, 0);
        GameObject head = Shape("Character Head", Vector2.zero, new Vector2(.48f, .48f), new Color(1f, .73f, .49f), true, root.transform);
        head.transform.localPosition = new Vector3(0, .42f, 0);
        GameObject hair = Shape("Hair", Vector2.zero, new Vector2(.49f, .20f), new Color(.16f, .09f, .04f), true, root.transform);
        hair.transform.localPosition = new Vector3(0, .59f, 0);
        GameObject leg1 = Shape("Left Leg", Vector2.zero, new Vector2(.18f, .43f), new Color(.12f, .18f, .35f), false, root.transform);
        leg1.transform.localPosition = new Vector3(-.16f, -.68f, 0);
        GameObject leg2 = Shape("Right Leg", Vector2.zero, new Vector2(.18f, .43f), new Color(.12f, .18f, .35f), false, root.transform);
        leg2.transform.localPosition = new Vector3(.16f, -.68f, 0);
        playerRenderer = body.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!started || gameOver) return;
        elapsed += Time.deltaTime;
        speed = Mathf.Min(maxSpeed, startSpeed + elapsed * speedIncrease);
        HandleInput();
        SpawnThings();
        MoveThings();
        UpdateCharacter();
    }

    void HandleInput()
    {
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            && Mathf.Abs(player.position.y - -2.65f) < .22f)
        {
            playerBody.velocity = new Vector2(0, 10.8f);
        }
        crouching = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        player.localScale = new Vector3(1, crouching ? .68f : 1f, 1);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSkin(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSkin(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSkin(2);
    }

    void UpdateCharacter()
    {
        if (player.position.y < -4.8f) EndGame();
        // Small running bob when grounded.
        if (Mathf.Abs(playerBody.velocity.y) < .08f && !crouching)
            player.localPosition = new Vector3(-5.1f, -2.65f + Mathf.Sin(Time.time * 14f) * .035f, 0);
    }

    void SpawnThings()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < nextSpawn) return;
        spawnTimer = 0;
        nextSpawn = Random.Range(.85f, 1.45f) * Mathf.Clamp(7f / speed, .55f, 1f);
        float roll = Random.value;
        if (roll < .68f)
        {
            CreateObstacle();
            if (Random.value < .62f) CreateCoinTrail();
        }
        else CreateCoinTrail();
    }

    void CreateObstacle()
    {
        float x = 12f;
        float y = -2.98f;
        GameObject root = new GameObject("Jungle Obstacle");
        root.transform.position = new Vector2(x, y);
        Color bark = new Color(.37f, .20f, .09f);
        Shape("Log", new Vector2(x, y), new Vector2(.95f, .70f), bark, false, root.transform);
        Shape("Log End", new Vector2(x + .36f, y), new Vector2(.16f, .60f), new Color(.72f, .46f, .22f), true, root.transform);
        movingThings.Add(new MovingThing { root = root, isCoin = false, radius = .48f });
    }

    void CreateCoinTrail()
    {
        int count = Random.Range(4, 8);
        float startX = 12f;
        float y = Random.value < .5f ? -1.6f : -2.0f;
        for (int i = 0; i < count; i++)
        {
            float x = startX + i * .72f;
            GameObject coin = Shape("Gold Coin", new Vector2(x, y), new Vector2(.36f, .36f), new Color(1f, .76f, .05f), true);
            Shape("Coin Shine", new Vector2(x - .06f, y + .06f), new Vector2(.10f, .19f), new Color(1f, .96f, .54f), true, coin.transform);
            movingThings.Add(new MovingThing { root = coin, isCoin = true, radius = .27f });
        }
    }

    void MoveThings()
    {
        for (int i = movingThings.Count - 1; i >= 0; i--)
        {
            MovingThing thing = movingThings[i];
            if (thing.root == null) { movingThings.RemoveAt(i); continue; }
            thing.root.transform.position += Vector3.left * speed * Time.deltaTime;
            float dx = Mathf.Abs(thing.root.transform.position.x - player.position.x);
            float dy = Mathf.Abs(thing.root.transform.position.y - player.position.y);
            if (thing.isCoin && dx < .58f && dy < 1.0f)
            {
                coins++;
                Destroy(thing.root);
                movingThings.RemoveAt(i);
                continue;
            }
            if (!thing.isCoin && dx < .67f && dy < .75f && !crouching)
            {
                // Jumping clears the log if the player is high enough.
                if (player.position.y < -2.0f) { EndGame(); }
            }
            if (thing.root.transform.position.x < -12f)
            {
                Destroy(thing.root);
                movingThings.RemoveAt(i);
            }
        }
    }

    void SelectSkin(int index)
    {
        selectedSkin = Mathf.Clamp(index, 0, skins.Length - 1);
        if (player != null)
        {
            Transform body = player.Find("Character Body");
            if (body != null) body.GetComponent<SpriteRenderer>().color = skins[selectedSkin];
        }
    }

    void EndGame() { gameOver = true; }

    void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = Color.white;
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.white;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            smallStyle.normal.textColor = Color.white;
        }
        GUI.color = new Color(0, 0, 0, .35f);
        GUI.Box(new Rect(12, 12, 265, 102), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(24, 18, 250, 30), "JUNGLE RUSH", hudStyle);
        GUI.Label(new Rect(24, 48, 250, 25), "Coins: " + coins + "    Time: " + Mathf.FloorToInt(elapsed) + "s", hudStyle);
        GUI.Label(new Rect(24, 76, 250, 25), "Speed: " + speed.ToString("0.0"), hudStyle);

        GUI.color = new Color(0, 0, 0, .35f);
        GUI.Box(new Rect(Screen.width - 210, 12, 198, 118), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width - 200, 17, 180, 25), "CHOOSE RUNNER", smallStyle);
        string[] names = { "1  Explorer", "2  Adventurer", "3  Ranger" };
        for (int i = 0; i < 3; i++)
        {
            GUI.backgroundColor = selectedSkin == i ? skins[i] : Color.white;
            if (GUI.Button(new Rect(Screen.width - 195, 44 + i * 25, 168, 23), names[i], buttonStyle)) SelectSkin(i);
        }
        GUI.backgroundColor = Color.white;
        GUI.Label(new Rect(Screen.width * .5f - 200, Screen.height - 42, 400, 28), "SPACE / W / ↑  JUMP      S / ↓  DUCK      1–3  CHANGE CHARACTER", smallStyle);

        if (gameOver)
        {
            GUI.color = new Color(0, 0, 0, .72f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 100, 440, 60), "GAME OVER", titleStyle);
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 42, 440, 35), "Coins: " + coins + "     Survival: " + Mathf.FloorToInt(elapsed) + " seconds", hudStyle);
            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 15, 200, 55), "RUN AGAIN", buttonStyle))
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
