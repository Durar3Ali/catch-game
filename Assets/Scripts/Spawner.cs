using UnityEngine;

// Lab 3/4/11: spawns prefabs from the top of the screen.
public class Spawner : MonoBehaviour
{
    public GameObject[] prefabs;      // Heart, Star, Cupcake, Storm
    public float[] weights;           // how often each one appears
    public float startInterval = 1.0f;
    public float minInterval = 0.55f;
    public float rampPerSecond = 0.008f;
    public float spawnY = 6.4f;
    public float rangeX = 8.2f;

    float timer = 0.6f;
    float interval;

    void Start() { interval = startInterval; }

    void Update()
    {
        timer -= Time.deltaTime;
        interval = Mathf.Max(minInterval, interval - rampPerSecond * Time.deltaTime);
        if (timer > 0f) return;
        timer = interval;

        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        float r = Random.Range(0f, total);
        int pick = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (r < weights[i]) { pick = i; break; }
            r -= weights[i];
        }
        Vector3 pos = new Vector3(Random.Range(-rangeX, rangeX), spawnY, 0f);
        Instantiate(prefabs[pick], pos, Quaternion.identity);
    }
}
