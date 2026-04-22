using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow3D : MonoBehaviour
{
    public Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 17f, -14f);
    [SerializeField] float smooth = 10f;
    [SerializeField] float lookAtHeight = 0.6f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 want = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, want, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookAtHeight);
    }
}
