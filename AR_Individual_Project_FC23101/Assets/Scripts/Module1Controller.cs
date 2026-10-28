using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Module1Controller : BaseModule
{
    [Header("Scene objects")]
    public Transform client;
    public Transform loadBalancer;
    public Transform database;
    public ServerNode[] servers;           // [0] always on, [1] and [2] added later
    public GameObject packetPrefab;

    [Header("UI")]
    public TMP_Text statusText;
    public Button addServersButton;
    public Button enableLbButton;

    [Header("Colours")]
    public Color requestColor = Color.cyan;
    public Color dataColor = new Color(1f, 0.6f, 0.1f);
    public Color responseColor = Color.green;
    public Color failColor = Color.red;

    [Header("Timing")]
    public float processingTime = 2f;
    const float FlyHeight = 0.06f;

    bool lbOn;
    int rrIndex;
    readonly List<GameObject> livePackets = new();

    // ---------- Lifecycle ----------
    public override void ResetModule()
    {
        StopAllCoroutines();
        foreach (var p in livePackets) if (p) Destroy(p);
        livePackets.Clear();

        servers[0].gameObject.SetActive(true);
        servers[1].gameObject.SetActive(false);
        servers[2].gameObject.SetActive(false);
        foreach (var s in servers) s.ResetNode();

        loadBalancer.gameObject.SetActive(false);
        lbOn = false; rrIndex = 0;

        addServersButton.interactable = true;
        enableLbButton.interactable = false;   // needs extra servers first
        SetStatus("Tap the client (or press Send Request).");
    }

    // ---------- Touch on the client ----------
    void Update()
    {
        if (Input.touchCount == 0) return;
        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;

        Ray ray = Camera.main.ScreenPointToRay(t.position);
        if (Physics.Raycast(ray, out var hit) && hit.transform == client)
            OnSendRequest();
    }

    // ---------- Button handlers ----------
    public void OnSendRequest() => StartCoroutine(RequestFlow(false));
    public void OnFetchFromDb() => StartCoroutine(RequestFlow(true));
    public void OnIncreaseTraffic() => StartCoroutine(TrafficBurst(8));

    public void OnAddServers()
    {
        servers[1].gameObject.SetActive(true);
        servers[2].gameObject.SetActive(true);
        addServersButton.interactable = false;
        enableLbButton.interactable = true;
        SetStatus("2 servers added. But requests still go to Server 1 only!");
    }

    public void OnEnableLoadBalancer()
    {
        loadBalancer.gameObject.SetActive(true);
        lbOn = true;
        enableLbButton.interactable = false;
        SetStatus("Load balancer ON: requests are now shared across 3 servers.");
    }

    // ---------- Core flow ----------
    IEnumerator RequestFlow(bool useDb)
    {
        GameObject packet = SpawnPacket(requestColor);
        var mover = packet.GetComponent<Mover>();

        ServerNode node = lbOn ? NextActiveServer() : servers[0];

        if (lbOn) yield return mover.MoveTo(Fly(loadBalancer), 0.5f);
        yield return mover.MoveTo(Fly(node.transform), 0.6f);

        // Server accepts or rejects
        if (!node.TryAccept())
        {
            Tint(packet, failColor);
            SetStatus("Server overloaded! Request failed (503).");
            yield return mover.MoveTo(Fly(client), 0.8f);
            KillPacket(packet);
            yield break;
        }

        SetStatus(useDb ? "Server is processing and querying the database..."
                        : "Server is processing the request...");
        yield return new WaitForSeconds(processingTime * 0.5f);

        if (useDb)
        {
            yield return mover.MoveTo(Fly(database), 0.7f);   // server -> DB
            Tint(packet, dataColor);
            yield return new WaitForSeconds(0.4f);
            yield return mover.MoveTo(Fly(node.transform), 0.7f); // DB -> server
        }

        yield return new WaitForSeconds(processingTime * 0.5f);
        node.Release();

        Tint(packet, responseColor);
        if (lbOn) yield return mover.MoveTo(Fly(loadBalancer), 0.5f);
        yield return mover.MoveTo(Fly(client), 0.8f);          // response
        SetStatus("Response received by the client.");
        KillPacket(packet);
    }

    IEnumerator TrafficBurst(int count)
    {
        SetStatus($"Sending {count} requests quickly...");
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(RequestFlow(false));
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ---------- Helpers ----------
    ServerNode NextActiveServer()
    {
        for (int i = 0; i < servers.Length; i++)
        {
            rrIndex = (rrIndex + 1) % servers.Length;
            if (servers[rrIndex].gameObject.activeSelf) return servers[rrIndex];
        }
        return servers[0];
    }

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

    void SetStatus(string msg) { if (statusText) statusText.text = msg; }
}