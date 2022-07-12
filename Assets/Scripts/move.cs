using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    [Header("Layer mask")]
    [SerializeField] private LayerMask _groundLayer;
    [Header("Components")]
    private Rigidbody2D _rb;
    [Header("Movement Variables")]
    [SerializeField] private float _movementAcceleration = 75f;
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _linearDrag = 10f;
    private float _horizontalDirection;
    private float _verticalDirection;
    private bool _onCrouch;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);

    [Header("Jump Variables")]
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 7f;
    [SerializeField] private float _lowJumpFallMultiplier = 5f;
    [SerializeField] private int _extraJumps = 1;
    private int _extraJumpValue;
    private bool _canJump => Input.GetButtonDown("Jump") && ((_onGround || _extraJumpValue > 0) && !_onCrouch);
    [Header("Dash Variables")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashLength = .3f;
    [SerializeField] private float _dashBufferlength = .2f;
    private float _dashbufferCounter;
    private bool _isDashing;
    private bool _hasDashed;
    private bool _canDash =>_dashbufferCounter > 0 && !_hasDashed ;
    [Header("Ground Collision Variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private Vector3 _groundRaycastOffset;
    private bool _onGround;


    // Start is called before the first frame update
    void Start()
    {
       _rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update()
    {
        _horizontalDirection = GetInput().x;
        _verticalDirection = GetInput().y;
        if (Input.GetKeyUp("left shift") && Input.GetButton("Horizontal") && !_onCrouch) _dashbufferCounter = _dashBufferlength;
        else _dashbufferCounter -= Time.deltaTime;
        if (_canJump) Jump();
        
        Debug.Log("can dash is:"+_canDash);
        Debug.Log("onGround is:" + _onGround);
        Debug.Log("crouch is:" + _onCrouch);
        Debug.Log("vel is :" + _rb.velocity.y);



    }
    private void FixedUpdate()
    {
        CheckCollisions();
        if (_canDash) StartCoroutine(Dash(_horizontalDirection, _verticalDirection));
        MoveCharacter();
        if (_onGround)
        {
            ApplyLinearDrag();
            _extraJumpValue = _extraJumps;
            _rb.gravityScale = 1f;
        }
        else
        {
            ApplyAirLinearDrag();
            FallMultiplier();
        }
        
    }
    private Vector2 GetInput() 
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); 
    }
    private void MoveCharacter()
    {
        var scale = transform.localScale;
        var position = transform.position;
        if (!_onCrouch && !_isDashing)
        {
            _rb.AddForce(new Vector2(_horizontalDirection, 0f) * _movementAcceleration);
            if (Mathf.Abs(_rb.velocity.x) > _maxMoveSpeed)
            {
                _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * _maxMoveSpeed, _rb.velocity.y);
            }
        }
        if (_verticalDirection < 0f && _dashbufferCounter < 0 && !_isDashing)
        {
            _onCrouch = true;
           
            scale.y = .5f;
            position.y -= scale.y / 2;
            transform.position = position;
            transform.localScale = scale;
            _horizontalDirection = 0f;
        }
        else if(_dashbufferCounter >0 && !_onCrouch)
        {
            scale.x = 1f;
            scale.y = .5f;
            position.y -= scale.y / 2;
            transform.position = position;
            transform.localScale = scale;
        }
        else
        {
            _onCrouch = false;
            scale.x = .5f;
            scale.y = 1f;
            transform.localScale = scale;

            
        }
    
    }
    private void ApplyLinearDrag()
    {
        if(Mathf.Abs(_horizontalDirection) < 0.4f || _changingDirection)
        {
            _rb.drag = _linearDrag;
        }
        else
        {
            _rb.drag = 0f;
        }
        
    }
    private void ApplyAirLinearDrag()
    {
       
            _rb.drag = _airLinearDrag;
        
    }
    private void Jump()
    {
        if (!_onGround)
        {
            --_extraJumpValue;
        }
        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
    }
    private IEnumerator Dash(float x, float y)
    {
        
            float dashStartTime = Time.time;
            _hasDashed = true;
            _isDashing = true;
            _onGround = false;
            Vector2 dir = new Vector2(0f, 0f);
            if (x != 0f || y != 0f) dir = new Vector2(x, y);

            while (Time.time < dashStartTime + _dashLength)
            {
                _rb.velocity = dir.normalized * _dashSpeed;
                yield return null;
            }
        
        Jump();
        _isDashing = false;
        _hasDashed = false;

    }
    
    public void FallMultiplier()
    {
        if (_rb.velocity.y < 0)
        {
            _rb.gravityScale = _fallMultiplier;
        }
        else if (_rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            _rb.gravityScale = _lowJumpFallMultiplier;
            
        }
        
        
    }
    private void CheckCollisions()
    {
        _onGround = Physics2D.Raycast(transform.position + _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer) ||
                                Physics2D.Raycast(transform.position - _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer);
    }

    private void OnDrawGizmos()

    {
        Gizmos.color = Color.green;

        Gizmos.DrawLine(transform.position + _groundRaycastOffset, transform.position + _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        Gizmos.DrawLine(transform.position - _groundRaycastOffset, transform.position - _groundRaycastOffset + Vector3.down * _groundRaycastLength);
    }
}
