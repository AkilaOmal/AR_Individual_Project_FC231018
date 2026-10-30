using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Module4Controller : BaseModule
{
    private enum Mode { Monolith, Microservices }

    private const int Auth = 0;
    private const int Course = 1;
    private const int Payment = 2;
    private const int Notification = 3;

    [Header("Scene objects")]
    public Transform client;
    public Transform monoEntry;
    public Transform gateway;
    public GameObject monolithGroup;
    public GameObject microGroup;
    public Renderer monolithBuilding;
    public Transform[] monoRooms;     // 0 Auth, 1 Course, 2 Payment, 3 Notification
    public Transform[] services;      // 0 Auth, 1 Course, 2 Payment, 3 Notification
    public Transform extraPayment;    // second Payment instance (inactive at start)
    public GameObject packetPrefab;

    [Header("UI (UI TextMeshPro inside the Canvas)")]
    public TMP_Text statusText;
    public TMP_Text infoText;

    [Header("Colours")]
    public Color requestColor = Color.cyan;
    public Color highlightColor = Color.yellow;
    public Color successColor = Color.green;
    public Color failColor = Color.red;
    public Color warnColor = new Color(1f, 0.8f, 0.1f);

    private const float FlyHeight = 0.08f;

    private static readonly string[] Descriptions = new string[]
    {
        "Auth: verifies who the user is and whether they are logged in.",
        "Course: stores and returns the course details.",
        "Payment: handles the payment for the course.",
        "Notification: sends the confirmation message to the user."
    };

    private Mode mode = Mode.Monolith;
    private bool valid, busy, failed, scaled;
    private int buyCount;
    private Color[] monoBase, svcBase;
    private Color buildingBase, extraBase, gatewayBase;
    private Vector3 extraScale;
    private readonly List<GameObject> livePackets = new List<GameObject>();

    // ---------- Lifecycle ----------
    private void Awake()
    {
        valid = ValidateReferences();
        if (!valid) return;

        monoBase = new Color[4];
        svcBase = new Color[4];
        for (int i = 0; i < 4; i++)
        {
            monoBase[i] = monoRooms[i].GetComponent<Renderer>().material.color;
            svcBase[i] = services[i].GetComponent<Renderer>().material.color;
        }
        buildingBase = monolithBuilding.material.color;
        extraBase = extraPayment.GetComponent<Renderer>().material.color;
        gatewayBase = gateway.GetComponent<Renderer>().material.color;
        extraScale = extraPayment.localScale;

        AutoBindButtons();
    }

    public override void ResetModule()
    {
        if (!valid) return;

        StopFlows();
        failed = false;
        scaled = false;
        buyCount = 0;

        extraPayment.gameObject.SetActive(false);
        extraPayment.localScale = extraScale;

        RestoreColors();
        SetMode(Mode.Monolith);
    }

    private bool ValidateReferences()
    {
        var missing = new List<string>();

        if (client == null) missing.Add("client");
        if (monoEntry == null) missing.Add("monoEntry");
        if (gateway == null) missing.Add("gateway");
        if (monolithGroup == null) missing.Add("monolithGroup");
        if (microGroup == null) missing.Add("microGroup");
        if (monolithBuilding == null) missing.Add("monolithBuilding");
        if (extraPayment == null) missing.Add("extraPayment");
        if (packetPrefab == null) missing.Add("packetPrefab");
        if (statusText == null) missing.Add("statusText");
        if (infoText == null) missing.Add("infoText");

        CheckArray(monoRooms, "monoRooms", missing);
        CheckArray(services, "services", missing);

        if (gateway != null && gateway.GetComponent<Renderer>() == null) missing.Add("gateway has no Renderer");
        if (extraPayment != null && extraPayment.GetComponent<Renderer>() == null) missing.Add("extraPayment has no Renderer");

        if (missing.Count > 0)
        {
            Debug.LogError("Module4Controller: problems -> " + string.Join(", ", missing), this);
            return false;
        }
        return true;
    }

    private void CheckArray(Transform[] arr, string name, List<string> missing)
    {
        if (arr == null || arr.Length < 4)
        {
            missing.Add(name + " (needs 4 elements)");
            return;
        }
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) missing.Add(name + "[" + i + "]");
            else if (arr[i].GetComponent<Renderer>() == null) missing.Add(name + "[" + i + "] has no Renderer");
        }
    }

    private void AutoBindButtons()
    {
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;
            string t = label.text.ToLower();

            if (t.Contains("monolith")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnMonolithMode); }
            else if (t.Contains("microservices")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnMicroMode); }
            else if (t.Contains("buy")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnBuyCourse); }
            else if (t.Contains("fail")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnFailService); }
            else if (t.Contains("scale")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnScale); }
            else if (t.Contains("compare")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnCompare); }
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

        if (hit.transform == client)
        {
            OnBuyCourse();
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (hit.transform == monoRooms[i] || hit.transform == services[i])
            {
                SetText(statusText, Descriptions[i]);
                return;
            }
        }

        if (hit.transform == extraPayment)
            SetText(statusText, "Payment (instance 2): a second copy of only the Payment Service.");
    }

    // ---------- Button handlers ----------
    public void OnMonolithMode()
    {
        if (!valid) return;
        StopFlows();
        RestoreColors();
        SetMode(Mode.Monolith);
    }

    public void OnMicroMode()
    {
        if (!valid) return;
        StopFlows();
        RestoreColors();
        SetMode(Mode.Microservices);
    }

    public void OnBuyCourse()
    {
        if (!valid) return;
        if (busy)
        {
            SetText(statusText, "Please wait for the current purchase to finish.");
            return;
        }
        StartCoroutine(BuyRoutine());
    }

    public void OnFailService()
    {
        if (!valid) return;
        failed = !failed;
        ApplyFailureColors();
        SetText(statusText, failed
            ? "Notification is now failing. Press Buy Course to see the effect."
            : "Notification restored. Press Buy Course again.");
    }

    public void OnScale()
    {
        if (!valid) return;

        if (mode == Mode.Microservices)
        {
            if (scaled)
            {
                SetText(statusText, "Payment is already running as 2 instances.");
                return;
            }
            scaled = true;
            extraPayment.gameObject.SetActive(true);
            StartCoroutine(Reveal(extraPayment, extraScale));
            SetText(statusText, "Only the Payment Service was copied. The other services stay as they are.");
        }
        else
        {
            SetText(statusText, "A monolith is usually scaled by running more copies of the whole application, even if only Payment is busy.");
            Color c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, buildingBase.a);
            StartCoroutine(Flash(monolithBuilding, c, buildingBase, 0.8f));
        }
    }

    public void OnCompare()
    {
        if (!valid) return;
        SetText(infoText,
            "COMPARISON\n" +
            "Structure: one application | independent services\n" +
            "Deployment: all together | each separately\n" +
            "Scaling: whole app | per service\n" +
            "Complexity: simpler to start | distributed-system complexity\n" +
            "Communication: internal calls | network calls");
    }

    // ---------- Mode ----------
    private void SetMode(Mode m)
    {
        mode = m;
        monolithGroup.SetActive(m == Mode.Monolith);
        microGroup.SetActive(m == Mode.Microservices);

        if (m == Mode.Monolith)
        {
            SetText(infoText,
                "MONOLITH\n" +
                "One deployable application. Auth, Course, Payment and Notification run together, " +
                "and they usually call each other inside the same application.");
            SetText(statusText, "Monolith mode: press Buy Course (or tap the client). Tap a room to learn what it does.");
        }
        else
        {
            SetText(infoText,
                "MICROSERVICES\n" +
                "Separate services behind an API Gateway. Each can be deployed and scaled on its own, " +
                "but they talk over the network, which adds complexity.");
            SetText(statusText, "Microservices mode: press Buy Course. Tap a service to learn what it does.");
        }
    }

    // ---------- Purchase flows ----------
    private IEnumerator BuyRoutine()
    {
        busy = true;

        if (mode == Mode.Monolith) yield return StartCoroutine(MonolithBuy());
        else yield return StartCoroutine(MicroBuy());

        buyCount++;
        busy = false;
    }

    private IEnumerator MonolithBuy()
    {
        SetText(statusText, "1. Client sends the purchase request to the application");
        GameObject p = SpawnPacket(requestColor);
        Mover m = p.GetComponent<Mover>();

        yield return m.MoveTo(Fly(monoEntry), 0.9f);

        string[] steps = new string[]
        {
            "2. Auth module verifies the user",
            "3. Course module loads the course",
            "4. Payment module handles the payment",
            "5. Notification module sends the confirmation"
        };

        for (int i = 0; i < 4; i++)
        {
            SetText(statusText, steps[i]);
            yield return m.MoveTo(Fly(monoRooms[i]), 0.6f);

            if (i == Notification && failed)
            {
                Tint(p, failColor);
                SetText(statusText, "Notification module failed. All modules run in one application, so the whole application can be affected.");
                Color c = new Color(failColor.r, failColor.g, failColor.b, buildingBase.a);
                yield return Flash(monolithBuilding, c, buildingBase, 1.5f);
                yield return m.MoveTo(Fly(client), 0.9f);
                KillPacket(p);
                yield break;
            }

            yield return Flash(monoRooms[i].GetComponent<Renderer>(), highlightColor, monoBase[i], 0.4f);
        }

        SetText(statusText, "6. Response returns to the client");
        Tint(p, successColor);
        yield return m.MoveTo(Fly(client), 0.9f);
        KillPacket(p);

        SetText(statusText, "Purchase complete. Every function ran inside one application.");
    }

    private IEnumerator MicroBuy()
    {
        SetText(statusText, "1. Client sends the purchase request");
        GameObject p = SpawnPacket(requestColor);
        Mover m = p.GetComponent<Mover>();

        yield return m.MoveTo(Fly(gateway), 0.9f);
        SetText(statusText, "2. API Gateway routes the request to the right services");
        yield return Flash(gateway.GetComponent<Renderer>(), highlightColor, gatewayBase, 0.4f);

        string[] steps = new string[]
        {
            "3. Auth Service verifies the user",
            "4. Course Service loads the course",
            "5. Payment Service handles the payment",
            "6. Notification Service sends the confirmation"
        };

        bool notificationFailed = false;

        for (int i = 0; i < 4; i++)
        {
            Transform target = services[i];
            Color back = svcBase[i];

            if (i == Payment && scaled && buyCount % 2 == 1)
            {
                target = extraPayment;
                back = extraBase;
                steps[i] = "5. Payment Service (instance 2) handles the payment";
            }

            SetText(statusText, steps[i]);
            yield return m.MoveTo(Fly(target), 0.7f);

            if (i == Notification && failed)
            {
                notificationFailed = true;
                Tint(p, warnColor);
                SetText(statusText, "Notification Service is down. The other services keep working.");
                yield return new WaitForSeconds(1.2f);
            }
            else
            {
                yield return Flash(target.GetComponent<Renderer>(), highlightColor, back, 0.4f);
            }
        }

        SetText(statusText, "7. Response goes back through the gateway to the client");
        yield return m.MoveTo(Fly(gateway), 0.7f);
        Tint(p, notificationFailed ? warnColor : successColor);
        yield return m.MoveTo(Fly(client), 0.9f);
        KillPacket(p);

        if (notificationFailed)
            SetText(statusText, "Purchase completed, but the confirmation could not be sent. The failure stayed inside one service. (In real systems, failures can still spread if services depend on each other.)");
        else
            SetText(statusText, "Purchase complete. Each step was handled by a separate service.");
    }

    // ---------- Visual helpers ----------
    private void StopFlows()
    {
        StopAllCoroutines();
        foreach (var p in livePackets)
            if (p != null) Destroy(p);
        livePackets.Clear();
        busy = false;
    }

    private void RestoreColors()
    {
        for (int i = 0; i < 4; i++)
        {
            SetColor(monoRooms[i], monoBase[i]);
            SetColor(services[i], svcBase[i]);
        }
        SetColor(extraPayment, extraBase);
        SetColor(gateway, gatewayBase);
        monolithBuilding.material.color = buildingBase;
        ApplyFailureColors();
    }

    private void ApplyFailureColors()
    {
        SetColor(monoRooms[Notification], failed ? failColor : monoBase[Notification]);
        SetColor(services[Notification], failed ? failColor : svcBase[Notification]);
    }

    private void SetColor(Transform t, Color c)
    {
        if (t == null) return;
        Renderer r = t.GetComponent<Renderer>();
        if (r != null) r.material.color = c;
    }

    private IEnumerator Flash(Renderer r, Color c, Color back, float time)
    {
        if (r == null) yield break;
        r.material.color = c;
        yield return new WaitForSeconds(time);
        if (r != null) r.material.color = back;
    }

    private IEnumerator Reveal(Transform t, Vector3 target)
    {
        t.localScale = Vector3.zero;
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / 0.4f;
            t.localScale = Vector3.Lerp(Vector3.zero, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        t.localScale = target;
    }

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

    private void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }
}