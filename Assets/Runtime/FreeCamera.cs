using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCamera : MonoBehaviour {
    [SerializeField] private float _moveSpeed = 10;
    [SerializeField] private float _sensitivity = 300;

    private void Update() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(transform.forward * _moveSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.S)) {
            transform.Translate(-transform.forward * _moveSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.A)) {
            transform.Translate(-transform.right * _moveSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(transform.right * _moveSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.Space)) {
            transform.Translate(transform.up * _moveSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            transform.Translate(-transform.up * _moveSpeed * Time.deltaTime, Space.World);
        }

        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x - input.y * _sensitivity * Time.deltaTime, transform.localRotation.eulerAngles.y + input.x * _sensitivity * Time.deltaTime, 0.0f);
    }
}
