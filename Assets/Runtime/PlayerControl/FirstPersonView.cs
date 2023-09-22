using System.Collections;
using UnityEngine;

public class FirstPersonView : MonoBehaviour {
    [Header("Player Settings")]
    [SerializeField] private float _sensitivity = 500.0f;
    [SerializeField] private bool _invertY = false;
    [Header("Developer Settings")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private float _maxPitch = 280.0f;
    [SerializeField] private float _minPitch = 80.0f;

    private Coroutine _shakeCoroutine = null;

    private FirstPersonMovement _movement;

    public void Shake(float duration, float intensity) {
        if (_shakeCoroutine != null) {

            StopCoroutine(_shakeCoroutine);
        }
        _shakeCoroutine = StartCoroutine(ShakeCamera(duration, intensity));
    }

    private void Awake() {
        _movement = GetComponent<FirstPersonMovement>();
    }

    private void Start() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        transform.localRotation = Quaternion.Euler(0.0f, transform.localRotation.eulerAngles.y + input.x * _sensitivity * Time.deltaTime, 0.0f);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraRoot.transform.localRotation.eulerAngles.x + input.y * (_invertY ? 1.0f : -1.0f) * _sensitivity * Time.deltaTime, 0.0f, 0.0f);
    }

    private void LateUpdate() {
        if (_cameraRoot.localRotation.eulerAngles.x > _minPitch && _cameraRoot.localRotation.eulerAngles.x < _maxPitch) {
            if (Mathf.Abs(_cameraRoot.localRotation.eulerAngles.x - _minPitch) < Mathf.Abs(_cameraRoot.localRotation.eulerAngles.x - _maxPitch)) {
                _cameraRoot.localRotation = Quaternion.Euler(
                    _minPitch,
                    _cameraRoot.localRotation.eulerAngles.y,
                    _cameraRoot.localRotation.eulerAngles.z);
            }
            else {
                _cameraRoot.localRotation = Quaternion.Euler(
                    _maxPitch,
                    _cameraRoot.localRotation.eulerAngles.y,
                    _cameraRoot.localRotation.eulerAngles.z);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_cameraRoot.position, 0.1f);
    }

    private IEnumerator ShakeCamera(float duration, float intensity) {
        Vector3 lastOffset = Vector3.zero;
        for (float t = 0; t < duration; t += Time.deltaTime) {
            Vector3 offset = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) * intensity;
            _cameraRoot.localPosition -= lastOffset;
            _cameraRoot.localPosition += offset;
            lastOffset = offset;
            yield return null;
        }
        _cameraRoot.localPosition -= lastOffset;
    }
}
