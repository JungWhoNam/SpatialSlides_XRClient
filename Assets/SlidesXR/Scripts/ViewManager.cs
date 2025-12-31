using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SlidesXR
{
    public class ViewManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetMQClient netMQClient;
        [SerializeField] private Widget widget;
        [SerializeField] private GameObject content;
        [SerializeField] private View viewBase;
        [SerializeField] private SelectedViewViewer viewer;
        [SerializeField] private SlideSyncController controller;

        [Header("State")]
        private readonly List<View> views = new();

        private void Start()
        {
            if (netMQClient == null)
            {
                Debug.LogWarning("[ViewManager] NetMQClient not assigned.");
                return;
            }

            // Register for NetMQ events
            netMQClient.OnConnected.AddListener(RequestGetAllViews);
            netMQClient.OnAllViewsReceived.AddListener(HandleAllViewsReceived);

            // In some cases, NetMQClient may already be connected before this script runs,
            // so OnConnected might not fire. We call GetAllViews() here just in case.
            // However, if the NetMQ thread takes time to initialize, OnConnected will still be needed.
            // Therefore, we use both direct check and event listener for robustness.
            if (netMQClient.IsRunning)
                RequestGetAllViews();
        }

        private void OnEnable()
        {
            RequestGetAllViews();
        }

        /// <summary>Requests all views from the server.</summary>
        public void RequestGetAllViews()
        {
            byte[] req = Encoding.UTF8.GetBytes("GetAllViews");
            netMQClient.SendMessageToServer(new List<byte[]> { req });
        }

        public View FindViewByMetadata(Data.SlideViewMetadata metadata)
        {
            return views.Find(view =>
                Data.SlideViewMetadata.ApproximatelyEqual(
                    metadata,
                    Data.SlideViewMetadata.FromView(view)
                )
            );
        }

        /// <summary>Adds a new view based on the current widget state.</summary>
        public void AddView()
        {
            if (!IsReady()) return;

            Texture2D img = widget.GetImage();
            (Vector3 pos, Quaternion rot, Vector3 scale) = widget.GetModelTransform();
            AddView(img, pos, rot, scale);
        }

        /// <summary>Adds a view from server-provided note data.</summary>
        public void AddView(Data.SlideImageNote note)
        {
            if (!IsReady()) return;

            AddView(note.image, note.metadata.position, note.metadata.rotation, note.metadata.scale, false);
        }

        /// <summary>Instantiates a new view and sets it as selected and shown.</summary>
        private void AddView(Texture2D img, Vector3 pos, Quaternion rot, Vector3 scale, bool show = true)
        {
            View newView = Instantiate(viewBase, content.transform);
            newView.transform.SetSiblingIndex(0);
            newView.gameObject.SetActive(true);

            newView.SetImage(img);
            newView.SetTransform(pos, rot, scale);

            views.Insert(0, newView);

            if (show)
            {
                viewer.SetView(newView);
                widget.SetView(newView);
            }
        }

        /// <summary>Removes the specified view.</summary>
        public void Remove(View view)
        {
            if (view == null || !IsReady()) return;

            views.Remove(view);
            Destroy(view.gameObject);
            viewer.SetView(null);
        }

        /// <summary>Removes the currently selected view.</summary>
        public void RemoveSelectedView()
        {
            if (!IsReady()) return;

            View currView = viewer.CurrentView;
            if (currView != null)
            {
                Remove(currView);
            }
        }

        /// <summary>Clears all views from the list and UI.</summary>
        public void Clear()
        {
            if (!IsReady()) return;

            foreach (View view in views)
            {
                Destroy(view.gameObject);
            }

            views.Clear();
            viewer.SetView(null);
        }

        /// <summary>Displays the given view in both widget and viewer.</summary>
        public void Show(View view)
        {
            if (view == null || !IsReady()) return;

            widget.SetView(view);
            viewer.SetView(view);
        }

        /// <summary>Shows the currently selected view.</summary>
        public void ShowSelectedView()
        {
            if (!IsReady()) return;

            View currView = viewer.CurrentView;
            if (currView != null)
            {
                Show(currView);
            }
        }

        /// <summary>Sends the currently selected view to the server.</summary>
        public void SendSelectedView()
        {
            View currView = viewer.CurrentView;
            if (currView != null)
            {
                SendView(currView);
            }
        }

        /// <summary>Sends a view to the server as a CreateView request.</summary>
        public void SendView(View view)
        {
            if (view == null) return;

            var reqdata = Encoding.UTF8.GetBytes("CreateView");
            var metadata = Data.SlideViewMetadata.FromView(view);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(metadata));
            byte[] imageBytes = (view.GetTexture() as Texture2D).EncodeToPNG();

            netMQClient.SendMessageToServer(new List<byte[]>
            {
                reqdata,
                jsonBytes,
                imageBytes
            });
        }

        /// <summary>Handles a response from the server with all available views.</summary>
        private void HandleAllViewsReceived(List<Data.SlideImageNote> notes)
        {
            Clear();

            foreach (var note in notes)
            {
                Debug.Log($"[ViewManager] Metadata - Pos: {note.metadata.position}, Rot: {note.metadata.rotation}");

                // Check against existing views (if any remain)
                bool alreadyExists = views.Exists(view =>
                    Data.SlideViewMetadata.ApproximatelyEqual(
                        note.metadata,
                        Data.SlideViewMetadata.FromView(view),
                        0.001f
                    )
                );

                if (!alreadyExists)
                {
                    AddView(note);
                }
                else
                {
                    Debug.Log("[ViewManager] Skipped adding duplicate view.");
                }
            }

            // update the slide sync controller
            controller.RequestGetCurrentViews();

            // after processing all views, if the list is still empty, ensure the default model is shown for authoring.
            if (views.Count == 0)
            {
                widget.ShowDefaultState();
            }
        }

        private bool IsReady()
        {
            return viewBase != null && content != null && widget != null && viewer != null;
        }

    }
}
