using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour {
    private void Update() {
        transform.Rotate(0, Time.deltaTime * 10, 0);
    }
}
