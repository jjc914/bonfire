using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * FIXME: wall running animation a little finicky
 * TODO: playtest wall running
 */

public class FirstPersonMovement : MonoBehaviour {
    public enum MovementState { RUNNING, SLIDING }

    [Header("Developer Settings")]
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private AnimationCurve _smoothInCurve;
    [SerializeField] private float _smoothInTimeMultiplier = 0.1f;
    [SerializeField] private AnimationCurve _smoothOutCurve;
    [SerializeField] private float _smoothOutTimeMultiplier = 0.2f;
    [Space]
    [SerializeField] private AnimationCurve _jumpVelocityCurve;
    [SerializeField] private float _jumpVelocityMultiplier = 10.0f;
    [SerializeField] private float _jumpVelocityTimeMultiplier = 1.0f;
    [Space]
    [SerializeField] private AnimationCurve _fallVelocityCurve;
    [SerializeField] private float _fallVelocityMultiplier = 10.0f;
    [SerializeField] private float _fallVelocityTimeMultiplier = 1.0f;

    private MovementState _movementState = MovementState.RUNNING;
    private Dictionary<KeyCode, Utilities.Pair<float, Coroutine>> _inputSmoothed = new Dictionary<KeyCode, Utilities.Pair<float, Coroutine>> () {
        { KeyCode.W, new Utilities.Pair<float, Coroutine>(0.0f, null) },
        { KeyCode.A, new Utilities.Pair<float, Coroutine>(0.0f, null) },
        { KeyCode.S, new Utilities.Pair<float, Coroutine>(0.0f, null) },
        { KeyCode.D, new Utilities.Pair<float, Coroutine>(0.0f, null) },
        { KeyCode.Space, new Utilities.Pair<float, Coroutine>(0.0f, null) }
    };
    private Dictionary<KeyCode, float> _inputRaw = new Dictionary<KeyCode, float>() {
        { KeyCode.W, 0.0f },
        { KeyCode.A, 0.0f },
        { KeyCode.S, 0.0f },
        { KeyCode.D, 0.0f },
        { KeyCode.Space, 0.0f }
    };
    /*
     * TODO: when deccelerating, don't first jump to maximum speed and then start deccelerating
     * TODO: bug test wall running
     * 
     * if jumped with wall colliding on left or right, wait until jump key is lifted, then allow wall running
     */
    
    private bool _isJumping = false;
    private bool _isFalling = false;

    private PlayerController _playerController;

    public MovementState movementState {
        private set {
            _movementState = value;
        }
        get {
            return _movementState;
        }
    }

    private void Awake() {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            GetComponent<FirstPersonView>().Shake(1.0f, 0.2f);
        }
        CheckState();
        GetRawInput();
        GetSmoothInput();
        Vector3 movement = CalculateMovement();
        _playerController.Move(movement);
    }

    private void GetRawInput() {
        _inputRaw[KeyCode.W] = Input.GetKey(KeyCode.W) ? 1.0f : 0.0f;
        _inputRaw[KeyCode.A] = Input.GetKey(KeyCode.A) ? 1.0f : 0.0f;
        _inputRaw[KeyCode.S] = Input.GetKey(KeyCode.S) ? 1.0f : 0.0f;
        _inputRaw[KeyCode.D] = Input.GetKey(KeyCode.D) ? 1.0f : 0.0f;
        _inputRaw[KeyCode.Space] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
    }

    private void GetSmoothInput() {
        if (Input.GetKeyDown(KeyCode.W)) {
            if (_inputSmoothed[KeyCode.W].second != null) StopCoroutine(_inputSmoothed[KeyCode.W].second);
            _inputSmoothed[KeyCode.W].second = StartCoroutine(Smooth(_smoothInCurve, _smoothInTimeMultiplier, KeyCode.W));
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            if (_inputSmoothed[KeyCode.A].second != null) StopCoroutine(_inputSmoothed[KeyCode.A].second);
            _inputSmoothed[KeyCode.A].second = StartCoroutine(Smooth(_smoothInCurve, _smoothInTimeMultiplier, KeyCode.A));
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            if (_inputSmoothed[KeyCode.S].second != null) StopCoroutine(_inputSmoothed[KeyCode.S].second);
            _inputSmoothed[KeyCode.S].second = StartCoroutine(Smooth(_smoothInCurve, _smoothInTimeMultiplier, KeyCode.S));
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            if (_inputSmoothed[KeyCode.D].second != null) StopCoroutine(_inputSmoothed[KeyCode.D].second);
            _inputSmoothed[KeyCode.D].second = StartCoroutine(Smooth(_smoothInCurve, _smoothInTimeMultiplier, KeyCode.D));
        }

        if (Input.GetKeyUp(KeyCode.W)) {
            if (_inputSmoothed[KeyCode.W].second != null) StopCoroutine(_inputSmoothed[KeyCode.W].second);
            _inputSmoothed[KeyCode.W].second = StartCoroutine(Smooth(_smoothOutCurve, _smoothOutTimeMultiplier, KeyCode.W));
        }
        if (Input.GetKeyUp(KeyCode.A)) {
            if (_inputSmoothed[KeyCode.A].second != null) StopCoroutine(_inputSmoothed[KeyCode.A].second);
            _inputSmoothed[KeyCode.A].second = StartCoroutine(Smooth(_smoothOutCurve, _smoothOutTimeMultiplier, KeyCode.A));
        }
        if (Input.GetKeyUp(KeyCode.S)) {
            if (_inputSmoothed[KeyCode.S].second != null) StopCoroutine(_inputSmoothed[KeyCode.S].second);
            _inputSmoothed[KeyCode.S].second = StartCoroutine(Smooth(_smoothOutCurve, _smoothOutTimeMultiplier, KeyCode.S));
        }
        if (Input.GetKeyUp(KeyCode.D)) {
            if (_inputSmoothed[KeyCode.D].second != null) StopCoroutine(_inputSmoothed[KeyCode.D].second);
            _inputSmoothed[KeyCode.D].second = StartCoroutine(Smooth(_smoothOutCurve, _smoothOutTimeMultiplier, KeyCode.D));
        }

        if (_playerController.isGrounded) {
            if (_inputSmoothed[KeyCode.Space].second != null) StopCoroutine(_inputSmoothed[KeyCode.Space].second);
            _isFalling = false;
            if (Input.GetKeyDown(KeyCode.Space)) {
                Jump();
            }
        }
        else {
            if (!_isJumping && !_isFalling) {
                _isFalling = true;
                if(_inputSmoothed[KeyCode.Space].second != null) StopCoroutine(_inputSmoothed[KeyCode.Space].second);
                _inputSmoothed[KeyCode.Space].second = StartCoroutine(Smooth(_fallVelocityCurve, _fallVelocityTimeMultiplier, KeyCode.Space));
            }
        }
    }

    private void Jump() {
        _inputSmoothed[KeyCode.Space].second = StartCoroutine(Smooth(_jumpVelocityCurve, _jumpVelocityTimeMultiplier, KeyCode.Space));
        _isJumping = true;
        StartCoroutine(WaitForLanding());
    }

    private void CheckState() {
        // running
        _movementState = MovementState.RUNNING;
    }

    private Vector3 CalculateMovement() {
        Vector3 input = new Vector3(_inputSmoothed[KeyCode.D].first - _inputSmoothed[KeyCode.A].first, 0.0f, _inputSmoothed[KeyCode.W].first - _inputSmoothed[KeyCode.S].first);
        if (input.magnitude > 1) input.Normalize();

        Vector3 movement;
        // walking movement
        if (_movementState == MovementState.RUNNING) {
            movement = (transform.forward * input.z + transform.right * input.x) * _moveSpeed;

            float multiplier;
            if (_isJumping) multiplier = _jumpVelocityMultiplier;
            else if (_isFalling) multiplier = _fallVelocityMultiplier;
            else multiplier = 1.0f;
            movement.y = _inputSmoothed[KeyCode.Space].first * multiplier;

        // other movement
        } else {
            throw new System.Exception();
        }

        return movement;
    }

    private IEnumerator Smooth(AnimationCurve curve, float timeMultiplier, KeyCode key) {
        float start = curve[0].time;
        float end = curve[curve.length - 1].time;
        for (float t = start; t < end; t += Time.deltaTime * (1.0f / timeMultiplier)) {
            _inputSmoothed[key].first = curve.Evaluate(t);
            yield return null;
        }
        _inputSmoothed[key].first = curve.Evaluate(curve.length - 1);
    }

    private IEnumerator WaitForLanding() {
        yield return new WaitUntil(() => !_playerController.isGrounded);
        yield return new WaitUntil(() => _playerController.isGrounded);
        _isJumping = false;
    }
}
