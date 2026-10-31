using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Module5Controller : BaseModule
{
    private enum Mode { Http, WebSocket }

    [Header("Scene objects")]
    public Transform clientA;
    public Transform clientB;
    public Transform server;
    public LineRenderer httpLink;
    public LineRenderer wsLinkA;
    public LineRenderer wsLinkB;
    public GameObject packetPrefab;

    [Header("UI (UI TextMeshPro inside the Canvas)")]
    public TMP_Text statusText;
    public TMP_Text counterText;
    public TMP_Text logText;

    [Header("Colours")]
    public Color requestColor = Color.cyan;
    public Color emptyColor = Color.gray;
    public Color dataColor = Color.green;
    public Color msgAColor = new Color(1f, 0.6f, 0.1f);
    public Color msgBColor = new Color(0.9f, 0.3f, 0.9f);
    public Color upgradeColor = new Color(1f, 0.85f, 0.2f);
    public Color pushColor = Color.yellow;
    public Color errorColor = Color.red;

    private const float FlyHeight = 0.08f;
    private const int MaxLogLines = 8;

    private Mode mode = Mode.Http;
    private bool valid, busy, connected;
    private int httpRequests, usefulResponses, wsMessages;
    private Renderer serverRend;
    private Color serverBase;
    private readonly List<string> logLines = new List<string>();
    private readonly List<GameObject> livePackets = new List<GameObject>();

    // ---------- Lifecycle ----------
    private void Awake()
    {
        valid = ValidateReferences();
        if (!valid) return;

        serverRend = server.GetComponent<Renderer>();
        serverBase = serverRend.material.color;

        LineRenderer[] links = new LineRenderer[] { httpLink, wsLinkA, wsLinkB };
        foreach (LineRenderer l in links)
        {
            l.useWorldSpace = true;
            l.positionCount = 2;
            l.enabled = false;
        }
        httpLink.startWidth = 0.005f; httpLink.endWidth = 0.005f;
        wsLinkA.startWidth = 0.008f; wsLinkA.endWidth = 0.008f;
        wsLinkB.startWidth = 0.008f; wsLinkB.endWidth = 0.008f;

        AutoBindButtons();
    }

    public override void ResetModule()
    {
        if (!valid) return;
        SetMode(Mode.Http);
    }

    private bool ValidateReferences()
    {
        var missing = new List<string>();

        if (clientA == null) missing.Add("clientA");
        if (clientB == null) missing.Add("clientB");
        if (server == null) missing.Add("server");
        if (httpLink == null) missing.Add("httpLink (needs a Line Renderer)");
        if (wsLinkA == null) missing.Add("wsLinkA (needs a Line Renderer)");
        if (wsLinkB == null) missing.Add("wsLinkB (needs a Line Renderer)");
        if (packetPrefab == null) missing.Add("packetPrefab");
        if (statusText == null) missing.Add("statusText");
        if (counterText == null) missing.Add("counterText");
        if (logText == null) missing.Add("logText");

        if (server != null && server.GetComponent<Renderer>() == null)
            missing.Add("server has no Renderer");

        if (missing.Count > 0)
        {
            Debug.LogError("Module5Controller: problems -> " + string.Join(", ", missing), this);
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

            if (t.Contains("websocket")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnWebSocketMode); }
            else if (t.Contains("http")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnHttpMode); }
            else if (t.Contains("message")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnSendMessage); }
            else if (t.Contains("reply")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnSendReply); }
            else if (t.Contains("polling")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnStartPolling); }
            else if (t.Contains("push")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnServerPush); }
        }
    }

    // ---------- Tap / click ----------
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
        if (!Physics.Raycast(ray, out hit)) return;

        if (hit.transform == clientA) OnSendMessage();
        else if (hit.transform == clientB) OnSendReply();
        else if (hit.transform == server) OnServerPush();
    }

    // ---------- Button handlers ----------
    public void OnHttpMode()
    {
        if (!valid) return;
        SetMode(Mode.Http);
    }

    public void OnWebSocketMode()
    {
        if (!valid) return;
        SetMode(Mode.WebSocket);
    }

    public void OnSendMessage()
    {
        if (!Ready()) return;
        if (mode == Mode.Http) Run(HttpDeliver(clientA, clientB, "Hello B!", msgAColor));
        else Run(WsDeliver(clientA, clientB, "Hello B!", msgAColor));
    }

    public void OnSendReply()
    {
        if (!Ready()) return;
        if (mode == Mode.Http) Run(HttpDeliver(clientB, clientA, "Hi A!", msgBColor));
        else Run(WsDeliver(clientB, clientA, "Hi A!", msgBColor));
    }

    public void OnStartPolling()
    {
        if (!valid) return;
        if (mode == Mode.WebSocket)
        {
            SetText(statusText, "Polling is not needed here. The server can push messages over the open connection.");
            return;
        }
        if (!Ready()) return;
        Run(PollingFlow());
    }

    public void OnServerPush()
    {
        if (!Ready()) return;
        if (mode == Mode.Http) Run(HttpPushAttempt());
        else Run(WsPush());
    }

    private bool Ready()
    {
        if (!valid) return false;
        if (busy)
        {
            SetText(statusText, "Please wait for the current animation to finish.");
            return false;
        }
        if (mode == Mode.WebSocket && !connected)
        {
            SetText(statusText, "The WebSocket connection is still opening.");
            return false;
        }
        return true;
    }

    private void Run(IEnumerator routine)
    {
        StartCoroutine(Locked(routine));
    }

    private IEnumerator Locked(IEnumerator inner)
    {
        busy = true;
        yield return StartCoroutine(inner);
        busy = false;
    }

    // ---------- Mode ----------
    private void SetMode(Mode m)
    {
        StopFlows();
        httpRequests = 0; usefulResponses = 0; wsMessages = 0;
        logLines.Clear();
        mode = m;

        if (m == Mode.Http)
        {
            SetText(statusText, "HTTP mode: the client asks, the server answers. Try Send Message, then Start Polling.");
            Log("HTTP: each exchange is a request followed by a response.");
            UpdateCounter();
        }
        else
        {
            SetText(statusText, "WebSocket mode: opening connections...");
            UpdateCounter();
            StartCoroutine(HandshakeFlow());
        }
    }

    private void StopFlows()
    {
        StopAllCoroutines();
        foreach (var p in livePackets)
            if (p != null) Destroy(p);
        livePackets.Clear();

        busy = false;
        connected = false;

        httpLink.enabled = false;
        wsLinkA.enabled = false;
        wsLinkB.enabled = false;

        if (serverRend != null) serverRend.material.color = serverBase;
    }

    // ---------- HTTP flows ----------
    private IEnumerator Exchange(Transform from, string reqLine, string resLine,
                                 Color reqColor, Color resColor, bool useful)
    {
        httpRequests++;
        UpdateCounter();
        Log(NameOf(from) + " -> server: " + reqLine);

        SetupLink(httpLink, from);
        httpLink.enabled = true;

        GameObject p = SpawnPacket(from, reqColor);
        Mover m = p.GetComponent<Mover>();

        yield return m.MoveTo(Fly(server), 0.8f);
        yield return new WaitForSeconds(0.4f);

        Log("server -> " + NameOf(from) + ": " + resLine);
        Tint(p, resColor);
        yield return m.MoveTo(Fly(from), 0.8f);

        if (useful) usefulResponses++;
        UpdateCounter();

        KillPacket(p);
        httpLink.enabled = false;
    }

    private IEnumerator HttpDeliver(Transform from, Transform to, string text, Color msgColor)
    {
        SetText(statusText, NameOf(from) + " sends the message with an HTTP request");
        yield return Exchange(from, "POST /messages \"" + text + "\"", "201 Created", msgColor, dataColor, false);

        SetText(statusText, "The server stored it, but " + NameOf(to) + " does not know yet. It has to ask.");
        yield return new WaitForSeconds(1.2f);

        SetText(statusText, NameOf(to) + " asks the server for new messages");
        yield return Exchange(to, "GET /messages", "200 OK \"" + text + "\"", requestColor, dataColor, true);

        Log(NameOf(to) + " received: \"" + text + "\"");
        SetText(statusText, NameOf(to) + " got the message only after asking. It took 2 request-response exchanges.");
    }

    private IEnumerator PollingFlow()
    {
        SetText(statusText, "Polling: Client A keeps asking whether there is something new");
        Log("Client A starts polling");

        yield return Exchange(clientA, "GET /messages (poll 1)", "200 OK (no new messages)",
                              requestColor, emptyColor, false);
        yield return new WaitForSeconds(0.8f);

        SetText(statusText, "Meanwhile, Client B posts a message");
        yield return Exchange(clientB, "POST /messages \"Hi A!\"", "201 Created",
                              msgBColor, dataColor, false);

        SetText(statusText, "Client A cannot know yet. It only finds out on its next poll.");
        yield return new WaitForSeconds(1.5f);

        yield return Exchange(clientA, "GET /messages (poll 2)", "200 OK \"Hi A!\"",
                              requestColor, dataColor, true);
        Log("Client A received: \"Hi A!\"");
        yield return new WaitForSeconds(1.0f);

        yield return Exchange(clientA, "GET /messages (poll 3)", "200 OK (no new messages)",
                              requestColor, emptyColor, false);

        SetText(statusText, "Polling used " + httpRequests + " requests, but only " + usefulResponses +
                            " carried new data. Delivery waited for the next poll.");
    }

    private IEnumerator HttpPushAttempt()
    {
        SetText(statusText, "The server has news for the clients...");
        Log("server: has news, but no client request is open");
        yield return Flash(serverRend, errorColor, serverBase, 1.2f);
        SetText(statusText, "With plain HTTP the server answers requests but does not start the conversation. It needs polling or another mechanism to reach clients.");
    }

    // ---------- WebSocket flows ----------
    private IEnumerator HandshakeFlow()
    {
        busy = true;
        Log("Client A -> server: GET /chat  Upgrade: websocket");
        Log("server -> Client A: 101 Switching Protocols");

        Coroutine a = StartCoroutine(OpenConnection(clientA, wsLinkA));
        Coroutine b = StartCoroutine(OpenConnection(clientB, wsLinkB));
        yield return a;
        yield return b;

        connected = true;
        UpdateCounter();
        SetText(statusText, "Connected. The connections stay open, so messages flow both ways without new requests.");
        busy = false;
    }

    private IEnumerator OpenConnection(Transform from, LineRenderer link)
    {
        GameObject p = SpawnPacket(from, requestColor);
        Mover m = p.GetComponent<Mover>();

        yield return m.MoveTo(Fly(server), 0.8f);
        yield return new WaitForSeconds(0.3f);

        Tint(p, upgradeColor);
        yield return m.MoveTo(Fly(from), 0.8f);
        KillPacket(p);

        SetupLink(link, from);
        link.enabled = true;     // stays visible: the connection is persistent
    }

    private IEnumerator WsDeliver(Transform from, Transform to, string text, Color msgColor)
    {
        SetText(statusText, NameOf(from) + " sends a message over its open connection");
        Log(NameOf(from) + " -> server: \"" + text + "\"");

        GameObject p = SpawnPacket(from, msgColor);
        Mover m = p.GetComponent<Mover>();

        yield return m.MoveTo(Fly(server), 0.7f);
        yield return Flash(serverRend, pushColor, serverBase, 0.25f);

        Log("server -> " + NameOf(to) + ": \"" + text + "\"");
        yield return m.MoveTo(Fly(to), 0.7f);
        KillPacket(p);

        wsMessages++;
        UpdateCounter();
        Log(NameOf(to) + " received: \"" + text + "\"");
        SetText(statusText, "Delivered through the server with no new connection and no polling.");
    }

    private IEnumerator WsPush()
    {
        SetText(statusText, "The server pushes a notice to both clients without any request");
        Log("server -> all clients: \"Server notice\"");

        Coroutine a = StartCoroutine(PushOne(clientA));
        yield return PushOne(clientB);
        yield return a;

        wsMessages += 2;
        UpdateCounter();
        SetText(statusText, "Over an open WebSocket connection the server can send messages whenever it needs to.");
    }

    private IEnumerator PushOne(Transform to)
    {
        GameObject p = SpawnPacket(server, pushColor);
        Mover m = p.GetComponent<Mover>();
        yield return m.MoveTo(Fly(to), 0.9f);
        KillPacket(p);
    }

    // ---------- Helpers ----------
    private void SetupLink(LineRenderer l, Transform from)
    {
        l.SetPosition(0, Fly(from));
        l.SetPosition(1, Fly(server));
    }

    private IEnumerator Flash(Renderer r, Color c, Color back, float time)
    {
        if (r == null) yield break;
        r.material.color = c;
        yield return new WaitForSeconds(time);
        if (r != null) r.material.color = back;
    }

    private string NameOf(Transform t)
    {
        return t == clientA ? "Client A" : "Client B";
    }

    private void Log(string line)
    {
        logLines.Add(line);
        while (logLines.Count > MaxLogLines) logLines.RemoveAt(0);
        SetText(logText, string.Join("\n", logLines.ToArray()));
    }

    private void UpdateCounter()
    {
        if (mode == Mode.Http)
            SetText(counterText, "HTTP requests: " + httpRequests + "  |  Responses with new data: " + usefulResponses);
        else
            SetText(counterText, "Open connections: " + (connected ? 2 : 0) + "  |  Messages delivered: " + wsMessages);
    }

    private Vector3 Fly(Transform t)
    {
        return t.position + Vector3.up * FlyHeight;
    }

    private GameObject SpawnPacket(Transform origin, Color c)
    {
        GameObject p = Instantiate(packetPrefab, Fly(origin), Quaternion.identity, transform);
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

    private void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }
}