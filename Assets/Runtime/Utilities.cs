using UnityEngine;

public static class Utilities {
    public enum RotationAxis { X, Y, Z }

    public class Pair<T1, T2> {
        T1 _first;
        T2 _second;

        public Pair(T1 first, T2 second) {
            _first = first;
            _second = second;
        }

        public T1 first {
            set {
                _first = value;
            }
            get {
                return _first;
            }
        }
        public T2 second {
            set {
                _second = value;
            }
            get {
                return _second;
            }
        }

        public static explicit operator Pair<T1, T2>((T1, T2) tuple) {
            return new Pair<T1, T2>(tuple.Item1, tuple.Item2);
        }
    }

    public static bool Contains(this LayerMask mask, int layer) {
        return mask == (mask | (1 << layer));
    }

    public static Quaternion ReplaceEulerAngle(Quaternion quaternion, float angle, RotationAxis axis) {
        switch (axis) {
            case RotationAxis.X:
                quaternion = Quaternion.Euler(angle, quaternion.eulerAngles.y, quaternion.eulerAngles.z);
                break;
            case RotationAxis.Y:
                quaternion = Quaternion.Euler(quaternion.eulerAngles.x, angle, quaternion.eulerAngles.z);
                break;
            case RotationAxis.Z:
                quaternion = Quaternion.Euler(quaternion.eulerAngles.x, quaternion.eulerAngles.y, angle);
                break;
        }
        return quaternion;
    }
}
