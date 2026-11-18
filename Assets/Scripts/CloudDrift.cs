using UnityEngine;

public class CloudDrift : MonoBehaviour
{
    public float speed = 0.4f;
    void Update()
    {
        Vector3 p = transform.position;
        p.x += speed * Time.deltaTime;
        if (p.x > 13f) p.x = -13f;
        transform.position = p;
    }
}
