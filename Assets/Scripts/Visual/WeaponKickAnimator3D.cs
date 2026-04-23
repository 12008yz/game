using UnityEngine;

public class WeaponKickAnimator3D : MonoBehaviour
{
    [SerializeField] Vector3 kickOffset = new Vector3(-0.08f, 0f, -0.06f);
    [SerializeField] Vector3 kickEuler = new Vector3(-14f, 0f, 0f);
    [SerializeField] float returnSpeed = 16f;

    Vector3 _basePos;
    Quaternion _baseRot;
    float _kickBlend;

    void Awake()
    {
        _basePos = transform.localPosition;
        _baseRot = transform.localRotation;
    }

    void LateUpdate()
    {
        _kickBlend = Mathf.MoveTowards(_kickBlend, 0f, returnSpeed * Time.deltaTime);
        transform.localPosition = _basePos + kickOffset * _kickBlend;
        transform.localRotation = _baseRot * Quaternion.Euler(kickEuler * _kickBlend);
    }

    public void TriggerKick()
    {
        _kickBlend = 1f;
    }
}
