using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SlidesXR.Data;

public class NetMQClient : MonoBehaviour
{
    private Thread clientThread;
    private bool running;
    public bool IsRunning => running;

    private SubscriberSocket subSocket;
    private PushSocket pushSocket;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    [System.Serializable] public class SlideChangedEvent : UnityEvent<int, List<SlideImageNote>> { }
    [System.Serializable] public class AllViewsReceivedEvent : UnityEvent<List<SlideImageNote>> { }
    [System.Serializable] public class ModeChangedEvent : UnityEvent<string> { }
    [System.Serializable] public class AnimationStepEvent : UnityEvent<int, int> { }
    [System.Serializable] public class ViewRefsReceivedEvent : UnityEvent<int, List<SlideViewMetadata>> { }

    public UnityEvent OnConnected;
    public SlideChangedEvent OnSlideChanged;
    public AllViewsReceivedEvent OnAllViewsReceived;
    public ModeChangedEvent OnModeChanged;
    public AnimationStepEvent OnAnimationStep;
    public ViewRefsReceivedEvent OnViewRefsReceived;

    void Start() => StartClient();

    private void StartClient()
    {
        running = true;
        clientThread = new Thread(ClientLoop) { IsBackground = true };
        clientThread.Start();
    }

    private void ClientLoop()
    {
        AsyncIO.ForceDotNet.Force();

        using (subSocket = new SubscriberSocket())
        using (pushSocket = new PushSocket())
        {
            subSocket.Connect("tcp://localhost:5557");
            subSocket.Subscribe("");

            pushSocket.Connect("tcp://localhost:5558");

            Debug.Log("[NetMQ] connected.");

            mainThreadActions.Enqueue(() => OnConnected?.Invoke());

            while (running)
            {
                try
                {
                    List<byte[]> parts = null;
                    if (subSocket.TryReceiveMultipartBytes(ref parts))
                    {
                        ProcessMultipartMessage(parts);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetMQ] error: {e.Message}");
                }
            }
        }

        NetMQConfig.Cleanup();
    }

    private void ProcessMultipartMessage(List<byte[]> parts)
    {
        if (parts == null || parts.Count < 1)
        {
            Debug.LogWarning("[NetMQ] Incomplete multipart message.");
            return;
        }

        string eventName = Encoding.UTF8.GetString(parts[0]);

        if (eventName == "CurrentMode" && parts.Count >= 2)
        {
            string json = Encoding.UTF8.GetString(parts[1]);
            string mode = JObject.Parse(json)?["mode"]?.ToString();

            if (!string.IsNullOrEmpty(mode))
            {
                mainThreadActions.Enqueue(() => OnModeChanged?.Invoke(mode));
            }
        }
        else if (eventName == "AnimationStep" && parts.Count >= 2)
        {
            string json = Encoding.UTF8.GetString(parts[1]);
            JObject data = JObject.Parse(json);
            int slideIndex = data["slide"]?.Value<int>() ?? -1;
            int animationStep = data["animation_step"]?.Value<int>() ?? -1;

            if (slideIndex >= 0)
            {
                mainThreadActions.Enqueue(() => OnAnimationStep?.Invoke(slideIndex, animationStep));
            }
        }
        else if (eventName == "CurrentViewRefs" && parts.Count >= 2)
        {
            JObject slideData = JObject.Parse(Encoding.UTF8.GetString(parts[1]));
            int slideIndex = slideData["slide"]?.Value<int>() ?? -1;

            List<SlideViewMetadata> metadataList = new List<SlideViewMetadata>();
            // Start from index 2 to skip header and slide data
            for (int i = 2; i < parts.Count; i++)
            {
                var metadataJson = Encoding.UTF8.GetString(parts[i]);
                var metadata = SlideViewMetadata.FromJObject(JObject.Parse(metadataJson));
                metadataList.Add(metadata);
            }

            mainThreadActions.Enqueue(() => OnViewRefsReceived?.Invoke(slideIndex, metadataList));
        }
        else if (eventName == "CurrentViews" || eventName == "AllViews")
        {
            ProcessViewMessage(eventName, parts);
        }
        else
        {
            Debug.LogWarning($"[NetMQ] Unrecognized event: {eventName}");
        }
    }

    private void ProcessViewMessage(string eventName, List<byte[]> parts)
    {
        int offset = 1;
        int slideNumber = -1;

        if (eventName == "CurrentViews")
        {
            if (parts.Count < 2) return;
            JObject slideData = JObject.Parse(Encoding.UTF8.GetString(parts[1]));
            slideNumber = slideData["slide"]?.Value<int>() ?? -1;
            if (slideNumber < 0) return;

            offset = 2; // Skip event and slide JSON
        }

        // Prepare raw note data (metadata + image bytes)
        List<(SlideViewMetadata metadata, byte[] imageBytes)> rawNotes = new();
        for (int i = offset; i < parts.Count - 1; i += 2)
        {
            var metadataJson = Encoding.UTF8.GetString(parts[i]);
            var metadata = SlideViewMetadata.FromJObject(JObject.Parse(metadataJson));
            var imageBytes = parts[i + 1];

            rawNotes.Add((metadata, imageBytes));
        }

        // Defer image creation and event dispatch to the main thread
        mainThreadActions.Enqueue(() =>
        {
            List<SlideImageNote> notes = new();

            foreach (var raw in rawNotes)
            {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(raw.imageBytes);

                notes.Add(new SlideImageNote
                { 
                    metadata = raw.metadata,
                    image = tex 
                });
            }

            if (eventName == "CurrentViews")
            {
                OnSlideChanged?.Invoke(slideNumber, notes);
            }
            else // AllViews
            {
                OnAllViewsReceived?.Invoke(notes);
            }
        });
    }


    public void SendMessageToServer(List<byte[]> messageParts)
    {
        if (pushSocket == null || !running) return;

        try
        {
            pushSocket.SendMultipartBytes(messageParts);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetMQ] Failed to send message: {e.Message}");
        }
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }

    void OnDestroy() => StopClient();

    private void StopClient()
    {
        if (!running) return;

        running = false;
        clientThread?.Join();
        subSocket?.Dispose();
        pushSocket?.Dispose();
        NetMQConfig.Cleanup();

        Debug.Log("[NetMQ] NetMQ Client stopped.");
    }
}
