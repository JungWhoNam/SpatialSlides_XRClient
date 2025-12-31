using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlidesXR
{
    public class WidgetManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetMQClient netMQClient;
        [SerializeField] private ViewManager viewManager;
        [SerializeField] private Widget[] widgets;
        [SerializeField] private TMP_Text modeText;

        private bool viewMode = false;
        public bool ViewMode { get => viewMode; }


        private void Start()
        {
            if (netMQClient == null)
            {
                Debug.LogWarning("[WidgetManager] NetMQClient not assigned.");
                return;
            }

            // Register for NetMQ events
            netMQClient.OnConnected.AddListener(RequestGetCurrentMode);
            netMQClient.OnModeChanged.AddListener(HandleModeChanged);

            // In some cases, NetMQClient may already be connected before this script runs,
            // so OnConnected might not fire. We call RequestGetCurrentMode() here just in case.
            // However, if the NetMQ thread takes time to initialize, OnConnected will still be needed.
            // Therefore, we use both direct check and event listener for robustness.
            if (netMQClient.IsRunning)
                RequestGetCurrentMode();
        }

        /// <summary>
        /// Sends a request to the server asking for the current mode (e.g., present or edit).
        /// </summary>
        public void RequestGetCurrentMode()
        {
            var reqdata = Encoding.UTF8.GetBytes("GetCurrentMode");
            netMQClient.SendMessageToServer(new List<byte[]>
            {
                reqdata
            });
        }

        private void HandleModeChanged(string mode)
        {
            if (mode != "present" && mode != "edit") return;

            viewMode = mode == "present";

            foreach (var widget in widgets)
            {
                widget.ViewMode = viewMode;
            }

            modeText.text = "Mode: " + (viewMode ? "Present" : "Edit");
            viewManager.gameObject.SetActive(!viewMode);
        }

    }
}