using UnityEngine;
using UnityEngine.EventSystems;

// Makes UI buttons pop a little on hover / press.
public class ButtonPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Vector3 baseScale = Vector3.one;
    float target = 1f;

    void Awake() { baseScale = transform.localScale; }
    void Update() { transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, 15f * Time.unscaledDeltaTime); }
    public void OnPointerEnter(PointerEventData e) { target = 1.08f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
    public void OnPointerDown(PointerEventData e) { target = 0.94f; }
    public void OnPointerUp(PointerEventData e) { target = 1.08f; }
}
