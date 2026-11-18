using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = -180f;
    void Update() { transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime); }
}
