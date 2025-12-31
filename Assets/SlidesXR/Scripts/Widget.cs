using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace SlidesXR
{
    public class Widget : MonoBehaviour
    {
        [Header("Modes")]
        [SerializeField] private bool viewMode = false;
        public bool ViewMode
        {
            get => viewMode;
            set
            {
                viewMode = value;

                if (currentView == null)
                {
                    UpdateEditModeVisuals(true);
                }
                else if (viewMode) // presentation mode
                {
                    UpdateEditModeVisuals(!modelInside);
                }
                else // edit mode
                {
                    UpdateEditModeVisuals(true);
                }
            }
        }

        [Header("References")]
        [SerializeField] private Camera snapCamera;
        [SerializeField] private GameObject model;
        [SerializeField] private GameObject nullText;
        [SerializeField] private GameObject[] visuals; // axes and frames
        [SerializeField] private GameObject grabbable; // contains Occulus.Interaction.Grabbable

        [Header("Model Sync Settings")]
        [SerializeField] private bool lockModelToWidget = true;
        public bool LockModelToWidget { get => lockModelToWidget; set => lockModelToWidget = value; }

        [Header("Transition Settings")]
        [SerializeField] private bool animateTransitions = true;
        [SerializeField] private float viewTolerance = 0.001f;
        [SerializeField] private float animationDuration = 0.5f;
        public bool AnimateTransitions { get => animateTransitions; set => animateTransitions = value; }

        private bool modelInside = false;
        private View currentView;
        private Coroutine currentAnimCoroutine;
        private Vector3? lastWidgetPosition;
        private Quaternion? lastWidgetRotation;
        private Vector3? lastWidgetScale;

        private Vector3 initialModelPosition;
        private Quaternion initialModelRotation;
        private Vector3 initialModelScale;

        private void Start()
        {
            if (model != null)
            {
                initialModelPosition = model.transform.localPosition;
                initialModelRotation = model.transform.localRotation;
                initialModelScale = model.transform.localScale;
            }

            if (!snapCamera) return;
            ConfigureSnapCamera();
        }

        private void Update()
        {
            if (!viewMode || model == null) return;
        }

        private void LateUpdate()
        {
            if (!viewMode || model == null || !lockModelToWidget || !modelInside) return;

            if (lastWidgetPosition.HasValue && lastWidgetRotation.HasValue && lastWidgetScale.HasValue)
            {
                Matrix4x4 previous = Matrix4x4.TRS(lastWidgetPosition.Value, lastWidgetRotation.Value, lastWidgetScale.Value);
                Matrix4x4 current = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                Matrix4x4 delta = current * previous.inverse;

                model.transform.SetPositionAndRotation(
                    delta.MultiplyPoint3x4(model.transform.position),
                    delta.rotation * model.transform.rotation
                );

                model.transform.localScale = Vector3.Scale(
                    model.transform.localScale,
                    transform.localScale.Divide(lastWidgetScale.Value)
                );
            }

            StoreLastWidgetTransform();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Model"))
            {
                modelInside = true;
                Debug.Log("[Widget] Model entered widget bounds.");

                if (viewMode)
                    UpdateEditModeVisuals(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Model"))
            {
                modelInside = false;
                Debug.Log("[Widget] Model exited widget bounds.");

                if (viewMode)
                {
                    UpdateEditModeVisuals(true);
                    ClearLastWidgetTransform();
                }
            }
        }

        private void ConfigureSnapCamera()
        {
            int sourceWidth = Camera.main ? Camera.main.pixelWidth : Screen.width;
            int sourceHeight = Camera.main ? Camera.main.pixelHeight : Screen.height;
            float scale = Mathf.Min(512f / sourceWidth, 512f / sourceHeight, 1f);
            int texWidth = Mathf.RoundToInt(sourceWidth * scale);
            int texHeight = Mathf.RoundToInt(sourceHeight * scale);

            snapCamera.targetTexture = new RenderTexture(texWidth, texHeight, 24, RenderTextureFormat.ARGB32);
            snapCamera.enabled = false;

            UpdateSquareViewport();
        }

        private void UpdateSquareViewport()
        {
            float aspect = (float)Screen.width / Screen.height;

            if (aspect > 1f)
            {
                float inset = (1f - (1f / aspect)) / 2f;
                snapCamera.rect = new Rect(inset, 0f, 1f / aspect, 1f);
            }
            else
            {
                float inset = (1f - aspect) / 2f;
                snapCamera.rect = new Rect(0f, inset, 1f, aspect);
            }
        }

        private void UpdateEditModeVisuals(bool show)
        {
            if (grabbable != null)
                grabbable.SetActive(show);

            foreach (var visual in visuals)
                if (visual != null)
                    visual.SetActive(show);
        }

        public Texture2D GetImage()
        {
            if (snapCamera == null || snapCamera.targetTexture == null)
                return null;

            snapCamera.Render();

            Texture2D fullImage = Utils.UtilsTexture.ReadRenderTexture(snapCamera.targetTexture);
            Rect rect = snapCamera.rect;
            int x = Mathf.RoundToInt(fullImage.width * rect.x);
            int y = Mathf.RoundToInt(fullImage.height * rect.y);
            int w = Mathf.RoundToInt(fullImage.width * rect.width);
            int h = Mathf.RoundToInt(fullImage.height * rect.height);

            return Utils.UtilsTexture.IsD3D()
                ? Utils.UtilsTexture.Crop(fullImage, x, y, w, h)
                : Utils.UtilsTexture.Resize(fullImage, w, h);
        }

        public (Vector3 pos, Quaternion rot, Vector3 scale) GetModelTransform()
        {
            if (model == null)
                return (Vector3.zero, Quaternion.identity, Vector3.one);

            Vector3 localPos = transform.InverseTransformPoint(model.transform.position);
            Quaternion localRot = Quaternion.Inverse(transform.rotation) * model.transform.rotation;
            Vector3 localScale = new Vector3(
                model.transform.localScale.x / transform.localScale.x,
                model.transform.localScale.y / transform.localScale.y,
                model.transform.localScale.z / transform.localScale.z
            );

            return (localPos, localRot, localScale);
        }

        public void ShowDefaultState()
        {
            if (model == null) return;

            model.SetActive(true);
            model.transform.localPosition = initialModelPosition;
            model.transform.localRotation = initialModelRotation;
            model.transform.localScale = initialModelScale;

            // Also reset the current view and ensure authoring visuals are on
            currentView = null;
            if (nullText != null) nullText.SetActive(true);
            UpdateEditModeVisuals(true);
        }

        public void SetView(View view, bool forceAnimate = true)
        {
            bool hasView = view != null;

            if (!hasView)
            {
                if (viewMode)
                {
                    if (model != null) model.SetActive(false);
                    if (nullText != null) nullText.SetActive(true);
                }
                else
                {
                    ShowDefaultState();
                }
                currentView = null;
                return;
            }

            // A view exists, so ensure the null text is always off.
            if (nullText != null) nullText.SetActive(false);
            if (model != null) model.SetActive(true);
            if (model == null) return;

            bool shouldAnimate = forceAnimate || !viewMode || !modelInside ||
                         !View.ApproximatelyEqual(currentView, view, viewTolerance);

            if (shouldAnimate)
            {
                StopAnimation();

                if (animateTransitions)
                    currentAnimCoroutine = StartCoroutine(AnimateToView(view, animationDuration));
                else
                    ApplyViewInstant(view);
            }

            currentView = view;

            if (viewMode) // presentation mode
                UpdateEditModeVisuals(!modelInside);
            else // edit mode
                UpdateEditModeVisuals(true);
        }

        private void ApplyViewInstant(View view)
        {
            model.transform.position = transform.TransformPoint(view.pos);
            model.transform.rotation = transform.rotation * view.rot;
            model.transform.localScale = Vector3.Scale(transform.localScale, view.scale);
        }

        private IEnumerator AnimateToView(View targetView, float duration)
        {
            Vector3 startPos = model.transform.position;
            Quaternion startRot = model.transform.rotation;
            Vector3 startScale = model.transform.localScale;

            Vector3 endPos = transform.TransformPoint(targetView.pos);
            Quaternion endRot = transform.rotation * targetView.rot;
            Vector3 endScale = Vector3.Scale(transform.localScale, targetView.scale);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                model.transform.position = Vector3.Lerp(startPos, endPos, t);
                model.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                model.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            model.transform.SetPositionAndRotation(endPos, endRot);
            model.transform.localScale = endScale;
            currentAnimCoroutine = null;
        }

        public void SnapToWidget()
        {
            if (viewMode && currentView != null && modelInside)
                SetView(currentView);
        }

        public bool IsAnimating() => currentAnimCoroutine != null;


        private void StopAnimation()
        {
            if (currentAnimCoroutine != null)
            {
                StopCoroutine(currentAnimCoroutine);
                currentAnimCoroutine = null;
            }
        }

        private void StoreLastWidgetTransform()
        {
            lastWidgetPosition = transform.position;
            lastWidgetRotation = transform.rotation;
            lastWidgetScale = transform.localScale;
        }

        private void ClearLastWidgetTransform()
        {
            lastWidgetPosition = null;
            lastWidgetRotation = null;
            lastWidgetScale = null;
        }

    }
}
