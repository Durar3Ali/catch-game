using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Lab 4 + 5 + 7: input, scripting, triggers, animator.
public class PonyController : MonoBehaviour
{
    public float speed = 9f;
    public float limitX = 8.3f;
    public Animator animator;
    public SpriteRenderer sprite;
    public GameObject sparklePrefab;
    public AudioSource audioSource;
    public AudioClip catchClip;
    public AudioClip ouchClip;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsOver) return;

        float h = ReadHorizontal();
        Vector3 p = transform.position;

        if (Mathf.Abs(h) > 0.01f)
        {
            p.x += h * speed * Time.deltaTime;
            if (sprite != null) sprite.flipX = h < 0f;
        }
        else if (PointerWorldX(out float wx))
        {
            float old = p.x;
            p.x = Mathf.MoveTowards(p.x, wx, speed * 1.5f * Time.deltaTime);
            if (sprite != null && Mathf.Abs(p.x - old) > 0.001f) sprite.flipX = p.x < old;
        }

        p.x = Mathf.Clamp(p.x, -limitX, limitX);
        transform.position = p;
    }

    float ReadHorizontal()
    {
        float h = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) h -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h += 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        h += Input.GetAxisRaw("Horizontal");
#endif
        return Mathf.Clamp(h, -1f, 1f);
    }

    bool PointerWorldX(out float worldX)
    {
        worldX = 0f;
        bool down = false;
        Vector3 sp = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            sp = Mouse.current.position.ReadValue();
            down = true;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            sp = Touchscreen.current.primaryTouch.position.ReadValue();
            down = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!down && Input.GetMouseButton(0))
        {
            sp = Input.mousePosition;
            down = true;
        }
#endif
        if (down && Camera.main != null)
        {
            sp.z = 10f;
            worldX = Camera.main.ScreenToWorldPoint(sp).x;
        }
        return down;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        FallingItem item = other.GetComponent<FallingItem>();
        if (item == null || GameManager.Instance == null || GameManager.Instance.IsOver) return;

        if (item.kind == FallingItem.Kind.Storm)
        {
            if (animator != null) animator.SetTrigger("Ouch");
            if (audioSource != null && ouchClip != null) audioSource.PlayOneShot(ouchClip);
            GameManager.Instance.LoseLife();
        }
        else
        {
            if (animator != null) animator.SetTrigger("Happy");
            if (audioSource != null && catchClip != null) audioSource.PlayOneShot(catchClip);
            if (sparklePrefab != null) Instantiate(sparklePrefab, other.transform.position, Quaternion.identity);
            GameManager.Instance.AddScore(item.points);
        }
        Destroy(other.gameObject);
    }
}
