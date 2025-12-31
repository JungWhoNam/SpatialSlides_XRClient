using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace SlidesXR
{
    public class SelectedViewViewer : MonoBehaviour
    {
        [SerializeField] private Button viewBtn;
        [SerializeField] private Button linkBtn;
        [SerializeField] private Button deleteBtn;

        private View currView;

        public View CurrentView => currView;

        public void SetView(View view)
        {
            bool hasView = view != null;

            SetButtonInteractable(viewBtn, hasView);
            SetButtonInteractable(linkBtn, hasView);
            SetButtonInteractable(deleteBtn, hasView);

            SetButtonImage(viewBtn, view?.GetTexture());

            currView = view;
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void SetButtonImage(Button button, Texture texture)
        {
            if (button == null) return;

            RawImage rawImage = button.GetComponent<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = texture;

                if (texture != null)
                {
                    RectTransform rt = rawImage.rectTransform;
                    float aspectRatio = (float)texture.height / texture.width;
                    rt.localScale = new Vector3(rt.localScale.x, rt.localScale.x * aspectRatio, rt.localScale.z);
                }
            }
        }

    }
}
