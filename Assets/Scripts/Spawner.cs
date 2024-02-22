using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public float minTime = 0.01f;//2f
    public float maxTime = 0.02f;//4f
    private void Start() {
        Spawn();
    }

    private void Spawn(){
        Instantiate(prefab, transform.position, Quaternion.identity);
        Invoke(nameof(Spawn), Random.Range(minTime, maxTime));
    }

}
