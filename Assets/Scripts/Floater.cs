using UnityEngine;

// Gentle bobbing for decorations / logo / pony.
public class Floater : MonoBehaviour
{
    public float amplitude = 0.15f;
    public float speed = 1.5f;
    public float pulse = 0f;
    Vector3 start;
    Vector3 startScale;
    float phase;

    void Start()
    {
        start = transform.localPosition;
        startScale = transform.localScale;
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        float s = Mathf.Sin(Time.time * speed + phase);
        transform.localPosition = start + new Vector3(0f, s * amplitude, 0f);
        if (pulse > 0f) transform.localScale = startScale * (1f + s * pulse);
    }
}
