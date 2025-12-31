using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class GrabEventRelay : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;

    public UnityEngine.Events.UnityEvent OnGrab;
    public UnityEngine.Events.UnityEvent OnRelease;

    private void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();
    }
    private void OnEnable()
    {
        grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            Debug.Log($"[GrabEventRelay] Grabbed: {grabbable.Transform.gameObject.name}");
            OnGrab?.Invoke();
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            Debug.Log($"[GrabEventRelay] Released: {grabbable.Transform.gameObject.name}");
            OnRelease?.Invoke();
        }
    }
}
