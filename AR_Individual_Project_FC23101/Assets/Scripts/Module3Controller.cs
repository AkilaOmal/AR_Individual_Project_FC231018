using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Module3Controller : BaseModule
{
    private enum Mode { None, Rest, GraphQL, Compare }

    [Header("Scene objects")]
    public Transform client;
    public Transform restServer;
    public Transform graphQLServer;
    public GameObject restLane;
    public GameObject graphQLLane;
    public GameObject[] restResults;    // [0] profile, [1] courses
    public GameObject[] gqlResults;     // [0] profile, [1] courses
    public GameObject packetPrefab;

    [Header("UI (UI TextMeshPro inside the Canvas)")]
    public TMP_Text statusText;
    public TMP_Text counterText;
    public TMP_Text requestText;
    public TMP_Text responseText;

    [Header("Colours")]
    public Color requestColor = Color.cyan;
    public Color restColor = new Color(0.2f, 0.9f, 0.4f);
    public Color graphQLColor = new Color(0.9f, 0.3f, 0.8f);

    private const float FlyHeight = 0.08f;

    private Mode mode = Mode.None;
    private bool valid;
    private int restCount, gqlCount;
    private string restReq = "", restRes = "", gqlReq = "", gqlRes = "";
    private Vector3[] restScale, gqlScale;
    private readonly List<GameObject> livePackets = new List<GameObject>();

    // ---------- Lifecycle ----------
    private void Awake()
    {
        valid = ValidateReferences();
        if (!valid) return;

        restScale = new Vector3[restResults.Length];
        for (int i = 0; i < restResults.Length; i++) restScale[i] = restResults[i].transform.localScale;

        gqlScale = new Vector3[gqlResults.Length];
        for (int i = 0; i < gqlResults.Length; i++) gqlScale[i] = gqlResults[i].transform.localScale;

        AutoBindButtons();
    }

    public override void ResetModule()
    {
        if (!valid) return;

        StopAllCoroutines();
        foreach (var p in livePackets)
            if (p != null) Destroy(p);
        livePackets.Clear();

        mode = Mode.None;
        restCount = 0; gqlCount = 0;
        restReq = ""; restRes = ""; gqlReq = ""; gqlRes = "";

        restLane.SetActive(true);
        graphQLLane.SetActive(true);

        for (int i = 0; i < restResults.Length; i++)
        {
            restResults[i].SetActive(false);
            restResults[i].transform.localScale = restScale[i];
        }
        for (int i = 0; i < gqlResults.Length; i++)
        {
            gqlResults[i].SetActive(false);
            gqlResults[i].transform.localScale = gqlScale[i];
        }

        SetText(statusText, "Choose a mode: REST, GraphQL or Compare.");
        SetText(counterText, "");
        SetText(requestText, "Request: (none yet)");
        SetText(responseText, "Response: (none yet)");
    }

    private bool ValidateReferences()
    {
        var missing = new List<string>();

        if (client == null) missing.Add("client");
        if (restServer == null) missing.Add("restServer");
        if (graphQLServer == null) missing.Add("graphQLServer");
        if (restLane == null) missing.Add("restLane");
        if (graphQLLane == null) missing.Add("graphQLLane");
        if (packetPrefab == null) missing.Add("packetPrefab");
        if (statusText == null) missing.Add("statusText");
        if (counterText == null) missing.Add("counterText");
        if (requestText == null) missing.Add("requestText");
        if (responseText == null) missing.Add("responseText");

        if (restResults == null || restResults.Length < 2) missing.Add("restResults (needs 2)");
        else for (int i = 0; i < restResults.Length; i++)
            if (restResults[i] == null) missing.Add("restResults[" + i + "]");

        if (gqlResults == null || gqlResults.Length < 2) missing.Add("gqlResults (needs 2)");
        else for (int i = 0; i < gqlResults.Length; i++)
            if (gqlResults[i] == null) missing.Add("gqlResults[" + i + "]");

        if (missing.Count > 0)
        {
            Debug.LogError("Module3Controller: unassigned fields -> " + string.Join(", ", missing), this);
            return false;
        }
        return true;
    }

    private void AutoBindButtons()
    {
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;
            string t = label.text.ToLower();

            if (t.Contains("graphql")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnGraphQLMode); }
            else if (t.Contains("compare")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnCompare); }
            else if (t.Contains("rest")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnRestMode); }
            else if (t.Contains("replay")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnReplay); }
        }
    }

    // ---------- Tap / click the client to replay ----------
    private void Update()
    {
        if (!valid) return;

        Vector2 pos = Vector2.zero;
        bool pressed = false;
        int fingerId = -1;

#if UNITY_EDITOR
        pressed = Input.GetMouseButtonDown(0);
        pos = Input.mousePosition;
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            pressed = t.phase == TouchPhase.Began;
            pos = t.position;
            fingerId = t.fingerId;
        }
#endif

        if (!pressed) return;

        if (EventSystem.current != null)
        {
#if UNITY_EDITOR
            if (EventSystem.current.IsPointerOverGameObject()) return;
#else
            if (EventSystem.current.IsPointerOverGameObject(fingerId)) return;
#endif
        }

        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(pos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.transform == client)
            StartMode(mode == Mode.None ? Mode.Rest : mode);
    }

    // ---------- Button handlers ----------
    public void OnRestMode() { StartMode(Mode.Rest); }
    public void OnGraphQLMode() { StartMode(Mode.GraphQL); }
    public void OnCompare() { StartMode(Mode.Compare); }
    public void OnReplay() { if (mode != Mode.None) StartMode(mode); }

    private void StartMode(Mode m)
    {
        if (!valid) return;

        ResetModule();
        mode = m;

        restLane.SetActive(m == Mode.Rest || m == Mode.Compare);
        graphQLLane.SetActive(m == Mode.GraphQL || m == Mode.Compare);

        Refresh();
        StartCoroutine(RunMode());
    }

    private IEnumerator RunMode()
    {
        if (mode == Mode.Rest)
        {
            yield return StartCoroutine(RestFlow());
            SetText(statusText, "REST needed 2 requests here (profile, then courses). A REST API can also be designed to return both in one request.");
        }
        else if (mode == Mode.GraphQL)
        {
            yield return StartCoroutine(GraphQLFlow());
            SetText(statusText, "GraphQL needed 1 request and returned exactly the fields the query asked for.");
        }
        else
        {
            Coroutine a = StartCoroutine(RestFlow());
            Coroutine b = StartCoroutine(GraphQLFlow());
            yield return a;
            yield return b;
            SetText(statusText, "Same data: REST used 2 requests, GraphQL used 1.");
        }
    }

    // ---------- REST flow ----------
    private IEnumerator RestFlow()
    {
        SetStatusSolo("REST 1/2: request the student profile");
        restReq = "GET /api/students/1";
        restCount = 1;
        Refresh();
        yield return RoundTrip(restServer, restColor);

        restRes = "{\"name\":\"Nimal\",\"email\":\"nimal@example.com\"}";
        Refresh();
        yield return Reveal(restResults, restScale, 0);
        yield return new WaitForSeconds(0.5f);

        SetStatusSolo("REST 2/2: second request for the enrolled courses");
        restReq = restReq + "\nGET /api/students/1/courses";
        restCount = 2;
        Refresh();
        yield return RoundTrip(restServer, restColor);

        restRes = restRes + "\n{\"courses\":[\"Web Development\",\"Machine Learning\"]}";
        Refresh();
        yield return Reveal(restResults, restScale, 1);
    }

    // ---------- GraphQL flow ----------
    private IEnumerator GraphQLFlow()
    {
        SetStatusSolo("GraphQL 1/2: the client writes a query with only the fields it needs");

        string[] lines = new string[]
        {
            "query {",
            "  student(id: 1) {",
            "    name",
            "    email",
            "    courses {",
            "      name",
            "    }",
            "  }",
            "}"
        };

        for (int i = 0; i < lines.Length; i++)
        {
            gqlReq = string.Join("\n", lines, 0, i + 1);
            Refresh();
            yield return new WaitForSeconds(0.25f);
        }

        SetStatusSolo("GraphQL 2/2: one request goes to the single GraphQL endpoint");
        gqlCount = 1;
        Refresh();
        yield return RoundTrip(graphQLServer, graphQLColor);

        gqlRes = "{\"data\":{\"student\":{\"name\":\"Nimal\",\"email\":\"nimal@example.com\",\"courses\":[{\"name\":\"Web Development\"},{\"name\":\"Machine Learning\"}]}}}";
        Refresh();

        StartCoroutine(Reveal(gqlResults, gqlScale, 0));
        yield return Reveal(gqlResults, gqlScale, 1);
    }

    // ---------- Shared animation ----------
    private IEnumerator RoundTrip(Transform server, Color responseColor)
    {
        GameObject packet = SpawnPacket(requestColor);
        Mover mover = packet.GetComponent<Mover>();

        yield return mover.MoveTo(Fly(server), 0.9f);
        yield return new WaitForSeconds(0.5f);

        Tint(packet, responseColor);
        yield return mover.MoveTo(Fly(client), 0.9f);

        KillPacket(packet);
    }

    private IEnumerator Reveal(GameObject[] arr, Vector3[] scales, int index)
    {
        GameObject g = arr[index];
        g.SetActive(true);

        Transform t = g.transform;
        Vector3 target = scales[index];
        float k = 0f;

        while (k < 1f)
        {
            k += Time.deltaTime / 0.4f;
            t.localScale = Vector3.Lerp(Vector3.zero, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        t.localScale = target;
    }

    // ---------- Text ----------
    private void Refresh()
    {
        switch (mode)
        {
            case Mode.Rest:
                SetText(requestText, restReq);
                SetText(responseText, restRes);
                SetText(counterText, "REST requests: " + restCount);
                break;

            case Mode.GraphQL:
                SetText(requestText, gqlReq);
                SetText(responseText, gqlRes);
                SetText(counterText, "GraphQL requests: " + gqlCount);
                break;

            case Mode.Compare:
                SetText(requestText, "REST\n" + restReq + "\n\nGraphQL\n" + gqlReq);
                SetText(responseText, "REST\n" + restRes + "\n\nGraphQL\n" + gqlRes);
                SetText(counterText, "REST requests: " + restCount + "   |   GraphQL requests: " + gqlCount);
                break;
        }
    }

    private void SetStatusSolo(string msg)
    {
        if (mode != Mode.Compare) SetText(statusText, msg);
    }

    private void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }

    // ---------- Helpers ----------
    private Vector3 Fly(Transform t)
    {
        return t.position + Vector3.up * FlyHeight;
    }

    private GameObject SpawnPacket(Color c)
    {
        GameObject p = Instantiate(packetPrefab, Fly(client), Quaternion.identity, transform);
        Tint(p, c);
        livePackets.Add(p);
        return p;
    }

    private void Tint(GameObject p, Color c)
    {
        if (p == null) return;
        Renderer r = p.GetComponent<Renderer>();
        if (r != null) r.material.color = c;
    }

    private void KillPacket(GameObject p)
    {
        livePackets.Remove(p);
        if (p != null) Destroy(p);
    }
}