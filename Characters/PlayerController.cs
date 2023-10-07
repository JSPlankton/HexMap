using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    private const string ANIM_PARAM_SPEED = "Speed";
    
    [SerializeField]
    private Animator animator;

    private float _forwardSpeed = 0.5f;
    private float _backwardSpeed = 0.5f;

    private float _targetSpeed;
    private float _currentSpeed;
    private Vector3 _movement;
    private Vector3 _tmpMovement;

    private bool _locoBegin;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_locoBegin)
        {
            _forwardSpeed = Mathf.Lerp(_forwardSpeed, 2.0f, Time.deltaTime);
            _targetSpeed = _tmpMovement.y > 0 ? _forwardSpeed * _tmpMovement.y : _backwardSpeed * _tmpMovement.y;
        }
        Move();
    }

    private void Move()
    {
        _currentSpeed = Mathf.Lerp(_targetSpeed, _currentSpeed, 0.9f);
        _movement = new Vector3(0, 0, _currentSpeed * Time.deltaTime);
        transform.position += _movement;
        animator.SetFloat(ANIM_PARAM_SPEED, _currentSpeed);
        // Debug.Log(_currentSpeed);
    }

    public void PlayerMove(InputAction.CallbackContext context)
    {
        _tmpMovement = context.ReadValue<Vector2>();
        if (_tmpMovement.y > 0) if (!_locoBegin) _locoBegin = true;
        _targetSpeed = _tmpMovement.y > 0 ? _forwardSpeed * _tmpMovement.y : _backwardSpeed * _tmpMovement.y;
    }
}
