using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Module2Controller : BaseModule
{
    class HttpCall
    {
        public string method, endpoint, body;
        public int status;
        public string reason, json, note;
    }

    [Header("Scene objects")]
    public Transform client;
    public Transform[] routers;          // in order: client -> server
    public Transform server;
    public Transform database;
    public GameObject packetPrefab;
    public GameObject[] courses;         // Course_1, Course_2, Course_3
    public GameObject missingMarker;
    public GameObject errorEffect;       // optional

    [Header("UI")]
    public TMP_Text requestText;
    public TMP_Text responseText;
    public TMP_Text statusText;

    [Header("Colours")]
    public Color requestColor = Color.cyan;
    public Color dataColor = new Color(1f, 0.6f, 0.1f);
    public Color okColor = Color.green;
    public Color warnColor = Color.yellow;
    public Color errorColor = Color.red;
    public Color updatedColor = new Color(0.7f, 0.3f, 1f);

    const float FlyHeight = 0.1f;
    const float HopTime = 0.6f;

    const string CoursesJson =
        "{\n  \"courses\": [\n    { \"id\": 1, \"name\": \"Web Development\" },\n" +
        "    { \"id\": 2, \"name\": \"Machine Learning\" }\n  ]\n}";

    Renderer serverRend, clientRend;
    Color serverBase, clientBase, courseBase;
    Vector3[] courseScale, routerScale;
    readonly List<GameObject> livePackets = new();
    HttpCall lastCall;
    bool busy, showJson, finished;

    // ---------- Lifecycle ----------
    void Awake()
    {
        serverRend = server.GetComponent<Renderer>();
        clientRend = client.GetComponent<Renderer>();
        serverBase = serverRend.material.color;
        clientBase = clientRend.material.color;
        courseBase = courses[0].GetComponent<Renderer>().material.color;

        courseScale = new Vector3[courses.Length];
        for (int i = 0; i < courses.Length; i++) courseScale[i] = courses[i].transform.localScale;
        routerScale = new Vector3[routers.Length];
        for (int i = 0; i < routers.Length; i++) routerScale[i] = routers[i].localScale;


    }

    public override void ResetModule()
    {
        StopAllCoroutines();
        foreach (var p in livePackets) if (p) Destroy(p);
        livePackets.Clear();
        busy = false; finished = false; showJson = false; lastCall = null;

        serverRend.material.color = serverBase;
        clientRend.material.color = clientBase;

        for (int i = 0; i < courses.Length; i++)
        {
            courses[i].transform.localScale = courseScale[i];
            courses[i].SetActive(i < 2);
        }
        courses[0].GetComponent<Renderer>().material.color = courseBase;
        for (int i = 0; i < routers.Length; i++) routers[i].localScale = routerScale[i];

        if (missingMarker) missingMarker.SetActive(false);
        if (errorEffect) errorEffect.SetActive(false);

        Set(requestText, "Press a button to send an HTTP request.");
        Set(responseText, "");
        Set(statusText, "");
    }

    // ---------- Button handlers ----------
    public void OnSendGet() => Run(new HttpCall
    {
        method = "GET",
        endpoint = "/api/courses",
        status = 200,
        reason = "OK",
        json = CoursesJson,
        note = "200 OK: the request was successful."
    });

    public void OnSendPost() => Run(new HttpCall
    {
        method = "POST",
        endpoint = "/api/courses",
        body = "{ \"name\": \"Data Science\" }",
        status = 201,
        reason = "Created",
        json = "{\n  \"id\": 3,\n  \"name\": \"Data Science\"\n}",
        note = "201 Created: a new resource was created."
    });

    public void OnSendPut() => Run(new HttpCall
    {
        method = "PUT",
        endpoint = "/api/courses/1",
        body = "{ \"name\": \"Advanced Web Development\" }",
        status = 200,
        reason = "OK",
        json = "{\n  \"id\": 1,\n  \"name\": \"Advanced Web Development\"\n}",
        note = "200 OK: the resource was updated."
    });

    public void OnSendDelete() => Run(new HttpCall
    {
        method = "DELETE",
        endpoint = "/api/courses/1",
        status = 200,
        reason = "OK",
        json = "{\n  \"message\": \"Course 1 deleted\"\n}",
        note = "200 OK: the resource was deleted."
    });

    public void OnSimulate400() => Run(new HttpCall
    {
        method = "POST",
        endpoint = "/api/courses",
        body = "{ \"name\": }",
        status = 400,
        reason = "Bad Request",
        json = "{\n  \"error\": \"Malformed JSON\"\n}",
        note = "400 Bad Request: the server could not understand the request."
    });

    public void OnSimulate404() => Run(new HttpCall
    {
        method = "GET",
        endpoint = "/api/courses/99",
        status = 404,
        reason = "Not Found",
        json = "{\n  \"error\": \"Course 99 not found\"\n}",
        note = "404 Not Found: the requested resource does not exist."
    });

    public void OnSimulate500() => Run(new HttpCall
    {
        method = "GET",
        endpoint = "/api/courses",
        status = 500,
        reason = "Internal Server Error",
        json = "{\n  \"error\": \"Unexpected server failure\"\n}",
        note = "500 Internal Server Error: the server failed while handling a valid request."
    });

    //public void OnToggleJson()
    //{
    //    showJson = !showJson;
    //    if (finished && lastCall != null) Set(responseText, BuildResponse(lastCall));
    //}
    public void OnToggleJson()
    {
        showJson = !showJson;

        if (lastCall == null)
        {
            Set(statusText, "Send a request first, then press Show JSON.");
            return;
        }

        if (finished)
            Set(responseText, BuildResponse(lastCall));
        else
            Set(statusText, "JSON will appear when the response arrives.");
    }

    public void OnReplay()
    {
        if (lastCall == null) return;
        var c = lastCall;
        ResetModule();      // restores data blocks, then runs the same call again
        Run(c);
    }

    void Run(HttpCall c)
    {
        if (busy) return;
        StartCoroutine(Flow(c));
    }

    // ---------- Main flow ----------
    IEnumerator Flow(HttpCall call)
    {
        busy = true; finished = false; lastCall = call;
        Set(requestText, BuildRequest(call));
        Set(responseText, "Waiting for response...");
        Set(statusText, "1. Client sends the request");

        var packet = SpawnPacket(requestColor);
        var mover = packet.GetComponent<Mover>();

        // Client -> routers -> server
        foreach (var r in routers)
        {
            yield return mover.MoveTo(Fly(r), HopTime);
            StartCoroutine(Pulse(r));
        }
        yield return mover.MoveTo(Fly(server), HopTime);

        Set(statusText, $"2. Server receives {call.method} {call.endpoint}");
        yield return new WaitForSeconds(0.8f);

        if (call.status == 400)
        {
            Set(statusText, "3. Server validates the request and rejects it");
            StartCoroutine(Flash(serverRend, warnColor, serverBase, 1.2f));
            yield return new WaitForSeconds(1.2f);
        }
        else if (call.status == 500)
        {
            Set(statusText, "3. Server fails while processing!");
            if (errorEffect) errorEffect.SetActive(true);
            StartCoroutine(Flash(serverRend, errorColor, serverBase, 1.5f));
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            Set(statusText, "3. Server asks the database");
            Tint(packet, dataColor);
            yield return mover.MoveTo(Fly(database), 0.7f);
            yield return ApplyDataEffect(call);
            Set(statusText, "4. Database returns the result to the server");
            yield return mover.MoveTo(Fly(server), 0.7f);
        }

        // Response travels back
        Color rc = call.status >= 500 ? errorColor : call.status >= 400 ? warnColor : okColor;
        Tint(packet, rc);
        Set(statusText, "5. Server sends the response back");

        for (int i = routers.Length - 1; i >= 0; i--)
        {
            yield return mover.MoveTo(Fly(routers[i]), HopTime);
            StartCoroutine(Pulse(routers[i]));
        }
        yield return mover.MoveTo(Fly(client), HopTime);

        StartCoroutine(Flash(clientRend, rc, clientBase, 1f));
        Set(responseText, BuildResponse(call));
        Set(statusText, call.note);
        if (errorEffect) errorEffect.SetActive(false);
        KillPacket(packet);
        finished = true; busy = false;
    }

    // What the database step does visually
    IEnumerator ApplyDataEffect(HttpCall call)
    {
        if (call.status == 201)                       // POST: new object appears
        {
            courses[2].SetActive(true);
            yield return PopIn(courses[2].transform, courseScale[2]);
        }
        else if (call.status == 404)                  // requested object missing
        {
            missingMarker.SetActive(true);
            yield return new WaitForSeconds(1f);
        }
        else if (call.method == "PUT")                // update: recolour + pulse
        {
            courses[0].GetComponent<Renderer>().material.color = updatedColor;
            yield return Pulse(courses[0].transform);
        }
        else if (call.method == "DELETE")             // object disappears
        {
            if (courses[0].activeSelf) yield return ShrinkOut(courses[0].transform, courseScale[0]);
            else yield return new WaitForSeconds(0.5f);
        }
        else                                          // GET: highlight all data blocks
        {
            foreach (var c in courses) if (c.activeSelf) StartCoroutine(Pulse(c.transform));
            yield return new WaitForSeconds(0.6f);
        }
    }

    // ---------- Animation helpers ----------
    IEnumerator Pulse(Transform t)
    {
        Vector3 s = t.localScale; float k = 0;
        while (k < 1f)
        {
            k += Time.deltaTime / 0.25f;
            t.localScale = s * (1f + 0.3f * Mathf.Sin(Mathf.Min(k, 1f) * Mathf.PI));
            yield return null;
        }
        t.localScale = s;
    }

    IEnumerator PopIn(Transform t, Vector3 target)
    {
        float k = 0;
        while (k < 1f)
        {
            k += Time.deltaTime / 0.35f;
            t.localScale = Vector3.Lerp(Vector3.zero, target, Mathf.SmoothStep(0, 1, k));
            yield return null;
        }
        t.localScale = target;
    }

    IEnumerator ShrinkOut(Transform t, Vector3 original)
    {
        float k = 0; Vector3 s = t.localScale;
        while (k < 1f)
        {
            k += Time.deltaTime / 0.3f;
            t.localScale = Vector3.Lerp(s, Vector3.zero, Mathf.SmoothStep(0, 1, k));
            yield return null;
        }
        t.gameObject.SetActive(false);
        t.localScale = original;
    }

    IEnumerator Flash(Renderer r, Color c, Color baseColor, float time)
    {
        r.material.color = c;
        yield return new WaitForSeconds(time);
        r.material.color = baseColor;
    }

    // ---------- Text builders ----------
    string BuildRequest(HttpCall c)
    {
        string s = $"{c.method} {c.endpoint} HTTP/1.1\nHost: example.com\nAccept: application/json";
        if (!string.IsNullOrEmpty(c.body))
            s += "\nContent-Type: application/json\n\n" + c.body;
        return s;
    }

    string BuildResponse(HttpCall c)
    {
        string s = $"HTTP/1.1 {c.status} {c.reason}\nContent-Type: application/json\n\n";
        s += showJson ? c.json : "(press Show JSON to view the body)";
        return s;
    }

    // ---------- Misc helpers ----------
    Vector3 Fly(Transform t) => t.position + Vector3.up * FlyHeight;

    GameObject SpawnPacket(Color c)
    {
        var p = Instantiate(packetPrefab, Fly(client), Quaternion.identity, transform);
        Tint(p, c);
        livePackets.Add(p);
        return p;
    }

    void Tint(GameObject p, Color c) => p.GetComponent<Renderer>().material.color = c;
    void KillPacket(GameObject p) { livePackets.Remove(p); Destroy(p); }
    void Set(TMP_Text t, string s) { if (t) t.text = s; }
}