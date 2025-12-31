using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace SlidesXR
{
    /// <summary>
    /// Handles both sending view metadata to the server and reacting to incoming slide and animation changes.
    /// </summary>
    public class SlideSyncController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetMQClient netMQClient;
        [SerializeField] private ViewManager viewManager;
        [SerializeField] private Widget previewWidget;
        [SerializeField] private GameObject viewContainer;
        [SerializeField] private View viewPrefab;
        [SerializeField] private GameObject viewNull;

        [Header("Options")]
        [SerializeField] private bool autoLinkWidget = true;
        public bool AutoLinkWidget
        {
            get => autoLinkWidget;
            set => autoLinkWidget = value;
        }

        [Header("State")]
        private readonly List<View> activeViews = new();
        private int currentViewIndex = -1;

        private void Start()
        {
            if (netMQClient == null || viewManager == null)
            {
                Debug.LogWarning("[Sync] NetMQClient or ViewManager not assigned.");
                return;
            }

            // Register for NetMQ events
            //netMQClient.OnConnected.AddListener(RequestGetCurrentViews);
            //netMQClient.OnSlideChanged.AddListener(HandleSlideChanged);
            netMQClient.OnAnimationStep.AddListener(HandleAnimationStep);
            netMQClient.OnViewRefsReceived.AddListener(HandleViewRefsReceived);
        }

        /// <summary>
        /// Sends a request to the server asking for the current slide's views.
        /// </summary>
        public void RequestGetCurrentViews()
        {
            var reqdata = Encoding.UTF8.GetBytes("GetCurrentViews");
            netMQClient.SendMessageToServer(new List<byte[]>
            {
                reqdata
            });
        }

        /*/// <summary>
        /// Handles slide change events received from the Python server.
        /// </summary>
        private void HandleSlideChanged(int slideNumber, List<Data.SlideImageNote> notes)
        {
            Debug.Log($"[Sync] Slide #{slideNumber} received with {notes.Count} notes.");
            ClearAllViews();

            foreach (var note in notes)
            {
                Debug.Log($"[Sync] Metadata - Pos: {note.metadata.position}, Rot: {note.metadata.rotation}");
                AddView(note);
            }

            if (viewNull != null)
                viewNull.SetActive(notes.Count == 0);

            if (activeViews.Count > 0)
            {
                currentViewIndex = 0;
                if (autoLinkWidget)
                {
                    ShowViewInWidget(activeViews[currentViewIndex]);
                }
            }
            else
            {
                currentViewIndex = -1;
                if (autoLinkWidget)
                {
                    ShowNullViewInWidget();
                }
            }
        }*/

        /// <summary>
        /// Handles animation step changes, using the step as a direct view index.
        /// </summary>
        private void HandleAnimationStep(int slideIndex, int animationStep)
        {
            Debug.Log($"[Sync] Animation event for slide {slideIndex}: Show view at index {animationStep}.");

            if (autoLinkWidget)
            {
                // Use animationStep as a direct index, after checking that it's in a valid range.
                if (animationStep >= 0 && animationStep < activeViews.Count)
                {
                    ShowViewInWidget(activeViews[animationStep]);
                }
                else
                {
                    Debug.LogWarning($"[Sync] Received animation step index {animationStep}, which is out of bounds for the {activeViews.Count} active views.");
                }
            }
        }

        private void HandleViewRefsReceived(int slideNumber, List<Data.SlideViewMetadata> metadataList)
        {
            Debug.Log($"[Sync] Slide #{slideNumber} refs received with {metadataList.Count} notes.");
            ClearAllViews();

            foreach (var metadata in metadataList)
            {
                View foundView = viewManager.FindViewByMetadata(metadata);
                if (foundView != null)
                {
                    // Add a UI representation of the found view
                    AddViewFromSource(foundView);
                }
                else
                {
                    Debug.LogWarning("Could not find a matching view in ViewManager for received metadata.");
                }
            }

            if (viewNull != null)
                viewNull.SetActive(activeViews.Count == 0);

            if (autoLinkWidget)
            {
                if (activeViews.Count > 0)
                {
                    ShowViewInWidget(activeViews[0]);
                }
                else
                {
                    ShowNullViewInWidget();
                }
            }
        }

        public void ShowViewInWidget(View view)
        {
            if (!IsReady()) return;

            previewWidget.SetView(view, true);
        }

        public void ShowNullViewInWidget()
        {
            if (!IsReady()) return;

            previewWidget.SetView(null);
        }

        /*private void AddView(Data.SlideImageNote note)
        {
            if (!IsReady()) return;

            var newView = Instantiate(viewPrefab, viewContainer.transform);
            newView.gameObject.SetActive(true);
            newView.SetImage(note.image);
            newView.SetTransform(note.metadata.position, note.metadata.rotation, note.metadata.scale);

            activeViews.Add(newView);
        }*/

        private void AddViewFromSource(View sourceView)
        {
            if (!IsReady() || sourceView == null) return;

            var newViewUI = Instantiate(viewPrefab, viewContainer.transform);
            newViewUI.gameObject.SetActive(true);
            newViewUI.SetImage(sourceView.GetTexture() as Texture2D);
            newViewUI.SetTransform(sourceView.pos, sourceView.rot, sourceView.scale);

            activeViews.Add(newViewUI);
        }

        private void ClearAllViews()
        {
            if (!IsReady()) return;

            foreach (var view in activeViews)
            {
                if (view != viewPrefab)
                    Destroy(view.gameObject);
            }

            activeViews.Clear();
        }

        private bool IsReady()
        {
            return previewWidget != null && viewContainer != null && viewPrefab != null;
        }
    }

}