
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
public static class Entrada
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    public static float Horizontal()
    {
        var k = Keyboard.current;
        if (k == null) return 0;

        float h = 0;
        if (k.upArrowKey.isPressed) h -= 1;
        if (k.aKey.isPressed) h += 1;
        return h;
    }
    public static bool Saltar()
    {
        return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
    }
    public static bool Reiniciar()
    {
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    }
#else
    public static float Horizontal()
    {
        float h = 0;
        if (Input.GetKey(KeyCode.UpArrow)) h -= 1;
        if (Input.GetKey(KeyCode.A)) h += 1;
        return h;
    }

    public static bool Saltar()
    {
        return Input.GetKeyDown(KeyCode.V);
    }

    public static bool Reiniciar()
    {
        return Input.GetKeyDown(KeyCode.R);
    }
#endif
}
public class Jugador : MonoBehaviour
{
    public System.Action AlGolpearTecho;
    Rigidbody2D rb;
    SpriteRenderer sr;
    const float Velocidad = 6f;
    const float FuerzaSalto = 9.5f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }
    void SetVel(float x, float y)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(x, y);
#else
        rb.velocity = new Vector2(x, y);
#endif
    }
    float VelY()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity.y;
#else
        return rb.velocity.y;
#endif
    }
    bool EnSuelo()
    {
        Vector2 centro = (Vector2)transform.position + Vector2.down * 0.5f;
        var hits = Physics2D.OverlapBoxAll(centro, new Vector2(1f, 0.15f), 0f);
        foreach (var h in hits)
            if (h.name == "Piso") return true;
        return false;
    }
    void Update()
    {
        float h = Entrada.Horizontal();
        float vy = VelY();
        if (Entrada.Saltar() && EnSuelo())
            vy = FuerzaSalto;
        SetVel(h * Velocidad, vy);
        if (h != 0) sr.flipX = h < 0;
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.name == "Techo")
            AlGolpearTecho?.Invoke();
    }
    public void Reiniciar(Vector2 pos)
    {
        transform.position = pos;
        SetVel(0, 0);
    }
}
public class Minijuego : MonoBehaviour
{
    static readonly Vector2 Inicio = new Vector2(-6f, -1.8f);
    Jugador jugador;
    AudioSource audio_;
    AudioClip sonido;
    Sprite cuadrado;
    bool perdio;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        if (FindFirstObjectByType<Minijuego>() != null) return;
        new GameObject("Minijuego").AddComponent<Minijuego>();
    }
    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cuadrado = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );
        Plataforma("Piso", new Vector2(-5.75f, -3f), new Vector2(8.5f, 1f),
            new Color(0.35f, 0.25f, 0.15f));
        Plataforma("Piso", new Vector2(5.5f, -3f), new Vector2(9f, 1f),
            new Color(0.35f, 0.25f, 0.15f));
        Plataforma("Techo", new Vector2(-5f, -0.3f), new Vector2(3.5f, 0.5f),
            new Color(0.6f, 0.3f, 0.7f));
        Plataforma("Techo", new Vector2(4f, 0f), new Vector2(3.5f, 0.5f),
            new Color(0.6f, 0.3f, 0.7f));
        Pared(-9.3f);
        Pared(9.3f);
        CrearJugador();
        audio_ = gameObject.AddComponent<AudioSource>();
        sonido = CrearSonido();
        jugador.AlGolpearTecho = () =>
        {
            audio_.PlayOneShot(sonido);
        };
    }
    void Plataforma(string nombre, Vector2 pos, Vector2 tam, Color color)
    {
        var o = new GameObject(nombre);
        o.transform.position = pos;
        o.transform.localScale = new Vector3(tam.x, tam.y, 1);
        var sr = o.AddComponent<SpriteRenderer>();
        sr.sprite = cuadrado;
        sr.color = color;
        o.AddComponent<BoxCollider2D>();
    }
    void Pared(float x)
    {
        var o = new GameObject("Pared");
        o.transform.position = new Vector2(x, 0);
        o.AddComponent<BoxCollider2D>().size = new Vector2(1, 40);
    }
    void CrearJugador()
    {
        var o = new GameObject("Charizard");
        o.transform.position = Inicio;
        var sr = o.AddComponent<SpriteRenderer>();
        var tex = Resources.Load<Texture2D>("Charizard");
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width / 2.2f
            );
        }
        else
        {
            sr.sprite = cuadrado;
            sr.color = new Color(1f, 0.5f, 0.1f);
        }
        sr.sortingOrder = 5;
        var rb = o.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var bc = o.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1.3f, 1f);
        bc.sharedMaterial = new PhysicsMaterial2D
        {
            friction = 0f,
            bounciness = 0f
        };
        jugador = o.AddComponent<Jugador>();
    }
    AudioClip CrearSonido()
    {
        int rate = 44100;
        int n = (int)(rate * 0.18f);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float freq = 500f + 600f * (1f - i / (float)n);

            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t)
                * (1f - i / (float)n) * 0.5f;
        }
        var clip = AudioClip.Create("golpe", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
    void Update()
    {
        if (!perdio && jugador.transform.position.y < -7f)
        {
            perdio = true;
            jugador.enabled = false;
        }
        if (perdio && Entrada.Reiniciar())
        {
            perdio = false;
            jugador.Reiniciar(Inicio);
            jugador.enabled = true;
        }
    }
    void OnGUI()
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = new Color(0.1f, 0.1f, 0.4f);
        var old = GUI.matrix;
        var r = new Rect(
            Screen.width - 190,
            Screen.height - 55,
            175,
            40
        );
        GUIUtility.RotateAroundPivot(-12f, r.center);
        GUI.Box(r, "");
        GUI.Label(r, "Said_Lazarte", st);
        GUI.matrix = old;
        if (perdio)
        {
            var p = new GUIStyle(GUI.skin.label)
            {
                fontSize = 70,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            p.normal.textColor = Color.red;
            GUI.Label(
                new Rect(0, Screen.height / 2f - 70, Screen.width, 100),
                "¡PERDISTE!", p
            );
            p.fontSize = 24;
            p.normal.textColor = Color.white;
            GUI.Label(
                new Rect(0, Screen.height / 2f + 20, Screen.width, 40),
                "Presiona R para reiniciar", p
            );
        }
    }
}

