using UnityEngine;

// Lab 5: Rigidbody2D + trigger collider. Things that fall from the sky.
public class FallingItem : MonoBehaviour
{
    public enum Kind { Heart, Star, Cupcake, Storm }
    public Kind kind = Kind.Heart;
    public int points = 1;

    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = Random.Range(0.22f, 0.40f);
        rb.linearVelocity = new Vector2(Random.Range(-0.5f, 0.5f), 0f);
        if (kind == Kind.Storm) rb.freezeRotation = true;
        else rb.angularVelocity = Random.Range(-60f, 60f);
    }

    void Update()
    {
        if (transform.position.y < -6.5f) Destroy(gameObject);
    }
}
