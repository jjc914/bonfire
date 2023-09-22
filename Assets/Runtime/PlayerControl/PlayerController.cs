using UnityEngine;

public class PlayerController : MonoBehaviour {
    public struct CollisionData {
        private Collider _collider;
        private Vector3 _depenetration;

        public Collider collider {
            set {
                _collider = value;
            }
            get {
                return _collider;
            }
        }
        public Vector3 depenetration {
            set {
                _depenetration = value;
            }
            get {
                return _depenetration;
            }
        }

        public CollisionData(Collider collider, Vector3 depenetration) {
            _collider = collider;
            _depenetration = depenetration;
        }
    }

    public const float GROUND_COLLISION_EPSILON = 0.00001f;
    public const float SIDE_COLLISION_EPSILON = 0.0000001f;

    public bool isGrounded { private set; get; }
    public bool isCollidingLeft { private set; get; }
    public bool isCollidingRight { private set; get; }

    public CollisionData collideGroundData { private set; get; }
    public CollisionData collideLeftData { private set; get; }
    public CollisionData collideRightData { private set; get; }

    [Header("Developer Settings")]
    [Tooltip("The drag acceleration applied to the X and Z directions (units squared per second).")]
    [Min(0)]
    [SerializeField] private float _horizontalDrag;
    [Tooltip("The drag acceleration applied to the Y direction (units squared per second).")]
    [Min(0)]
    [SerializeField] private float _verticalDrag;

    private Vector3 _movementVelocity;
    private Vector3 _physicsVelocity;

    // raw movement, unaffected by drag
    public void Move(Vector3 movement) {
        _movementVelocity = movement;
    }

    // physics movement, affected by drag
    public void AddForce(Vector3 direction, float magnitude) {
        _physicsVelocity = direction.normalized * magnitude;
    }

    private void Update() {
        Vector3 movement = (_movementVelocity + _physicsVelocity) * Time.deltaTime;

        Vector3 depenetration;
        CheckPenetration(movement, out depenetration);

        transform.position += movement + depenetration;

        float horizontalDragVelocity = _horizontalDrag * Time.deltaTime;
        float verticalDragVelocity = _verticalDrag * Time.deltaTime;
        _physicsVelocity = new Vector3(_physicsVelocity.x > 0.0f ? Mathf.Max(_physicsVelocity.x - horizontalDragVelocity, 0.0f) : Mathf.Min(_physicsVelocity.x + horizontalDragVelocity, 0.0f),
            _physicsVelocity.y > 0.0f ? Mathf.Max(_physicsVelocity.y - verticalDragVelocity, 0.0f) : Mathf.Min(_physicsVelocity.y + verticalDragVelocity, 0.0f),
            _physicsVelocity.z > 0.0f ? Mathf.Max(_physicsVelocity.z - horizontalDragVelocity, 0.0f) : Mathf.Min(_physicsVelocity.z + horizontalDragVelocity, 0.0f));
    }

    private bool CheckPenetration(Vector3 movement, out Vector3 depenetration) {
        Collider collider = GetComponent<Collider>();
        float radius = Mathf.Max(
            collider.bounds.max.x - collider.bounds.min.x, 
            collider.bounds.max.y - collider.bounds.min.y, 
            collider.bounds.max.z - collider.bounds.min.z) / 2.0f;

        bool didCollide = false;
        depenetration = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position + movement, radius);

        isGrounded = false;
        collideGroundData = new CollisionData();
        isCollidingLeft = false;
        collideLeftData = new CollisionData();
        isCollidingRight = false;
        collideRightData = new CollisionData();
        foreach (Collider hit in hits) {
            if (hit.Equals(collider)) continue;
            didCollide = true;

            Vector3 pDirection;
            float pDistance;
            Physics.ComputePenetration(
                collider, transform.position + movement, transform.rotation,
                hit, hit.transform.position, hit.transform.rotation,
                out pDirection, out pDistance);

            if (!isGrounded) {
                isGrounded = pDirection.y > GROUND_COLLISION_EPSILON;
                if (isGrounded) collideGroundData = new CollisionData(hit, pDirection * pDistance);
            }
            float dot = Vector3.Dot(transform.right, pDirection * pDistance);
            if (!isCollidingLeft) {
                isCollidingLeft = dot > SIDE_COLLISION_EPSILON;
                if (isCollidingLeft) collideLeftData = new CollisionData(hit, pDirection * pDistance);
            }
            if (!isCollidingRight) {
                isCollidingRight = dot < -SIDE_COLLISION_EPSILON;
                if (isCollidingRight) collideRightData = new CollisionData(hit, pDirection * pDistance);
            }

            depenetration += pDirection * pDistance;
        }
        return didCollide;
    }
}
