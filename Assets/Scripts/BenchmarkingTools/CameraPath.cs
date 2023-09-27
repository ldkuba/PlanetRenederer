using System.Collections.Generic;
using UnityEngine;

public class CameraPath : MonoBehaviour {

    [SerializeField]
    public List<Transform> path;

    [SerializeField]
    public float speed = 5.0f;

    private int current_target;

    void Start() {
        if(path.Count > 0) {
            transform.SetPositionAndRotation(path[0].position, path[0].rotation);
            current_target = 1;
        }

        // List all gameobjects in scene
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects) {
            Debug.Log(go.name);
        }
    }

    void Update() {
        if (current_target < path.Count) {
            float distance_to_previous = Vector3.Distance(transform.position, path[current_target - 1].position);
            float path_distance = Vector3.Distance(path[current_target - 1].position, path[current_target].position);
            transform.position = Vector3.MoveTowards(transform.position, path[current_target].position, speed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(path[current_target - 1].rotation, path[current_target].rotation, distance_to_previous / path_distance);

            if(transform.position == path[current_target].position) {
                current_target++;
            }
        }
    }
}
