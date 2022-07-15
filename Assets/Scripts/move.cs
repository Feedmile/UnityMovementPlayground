using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class move : MonoBehaviour
{
    [Header("Layer mask")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;
    [Header("Components")]
    private Rigidbody2D _rb;
    private Animator _anim;
    private string currentState;
    private bool _isDelay = false;

    [Header("Movement Variables")]
    [SerializeField] private float _movementAcceleration = 75f;
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _linearDrag = 10f;
    private float _horizontalDirection;
    private float _verticalDirection;
    private bool _facingRight = true;
    private bool _isMoving => _horizontalDirection != 0;
    private bool _onCrouch=false;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);
    private bool _canMove => !_wallSlide && !_inAir && !_waitForStart && !_isDelay ;
    private bool _inAir = false;
    [Header("Jump Variables")]
    [SerializeField] private float _jumpForce = 15f;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 7f;
    [SerializeField] private float _lowJumpFallMultiplier = 5f;
    [SerializeField] private int _extraJumps = 1;
    private int _extraJumpValue;
    private bool _isFalling;
    private bool _isJumping;
    private bool _isSJumping;
    private bool _fastJump;
    [Header("Wall Movement Variables")]
    [SerializeField] private float _wallSlideModifier = .5f;

    private bool _wallSlide => _onWall && !_onGround && _rb.velocity.y <= 0f;   
    
    private bool _canJump => Input.GetButtonDown("Jump") &&_verticalDirection != -1 && !_isDelay && !_waitForStart && (((_onGround || _extraJumpValue > 0) && !_onCrouch) || _onWall);
    
    [Header("Dash Variables")]
    [SerializeField] private float _dashSpeed = 10f;
    [SerializeField] private float _dashLength = .3f;
    [SerializeField] private float _dashBufferlength = .3f;
    private float _dashbufferCounter;
    private bool _isDashing;
    private bool _hasDashed;
    private bool _canDash =>_dashbufferCounter > 0 &&!_isDelay  && !_hasDashed &&  !(_horizontalDirection == 0 && _verticalDirection == 0) ;
    [Header("Ground Collision Variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private Vector3 _groundRaycastOffset;
    private bool _onGround;
    [Header("Wall Collision Variables")]
    [SerializeField] private float _wallRaycastLength;
    private bool _onWall;
    private bool _onRightWall;
    //Animation States
    const string CROUCH = "Crouch";
    const string WALLJUMP = "WallJump";
    const string STAND = "Stand";
    const string JUMP = "Jump";
    const string FALLING = "Falling";
    const string WALLSLIDE = "WallSlide";
    const string DASH = "Dash";
    const string ENTRANCE = "Entrance";
    const string FASTJUMP = "FastJump";
    private bool _waitForStart;


    // Start is called before the first frame update
    void Start()
    {
       _rb = GetComponent<Rigidbody2D>();
       _anim = GetComponent<Animator>();
       _waitForStart = true;
    }
    // Update is called once per frame
    void Update()
    {
        
        _horizontalDirection = GetInput().x;
        _verticalDirection = GetInput().y;
        if (Input.GetKeyUp("left shift") && Input.GetButton("Horizontal") && !_onCrouch) _dashbufferCounter = _dashBufferlength;
        else _dashbufferCounter -= Time.deltaTime;
        if (_canJump)
        {
            if(_onWall && !_onGround)
            {
                StartCoroutine(WallJump());
                
            }
            else
            {
                Jump(Vector2.up);
            }
            
        }
       


    }
    private void FixedUpdate()
    {
        
        CheckCollisions();
       
        if (_canMove) MoveCharacter();
        if (_onGround)
        {
            _isJumping = false;
            _isFalling = false;
            _inAir = false;
            _fastJump = false;
            ApplyLinearDrag();
            _extraJumpValue = _extraJumps;
            _rb.gravityScale = 1f;
            if (_onWall) StickToWall();
            if (_isDelay || _onCrouch)
            {
                _rb.velocity = new Vector2(0f, 0f);
            }
        }
        else
        {
            ApplyAirLinearDrag();
            FallMultiplier();
            if (_rb.velocity.y < 0f)
            {
                _isJumping = false;
                
            }
            if (_rb.velocity.y > 0f)
            {
                _isJumping = true;

            }
            if (_rb.velocity.y < 0f && !_wallSlide) _isFalling = true;
        }
    
        if (!_isJumping)
        {
            if (_wallSlide) WallSlide();
            if (_onWall)
            {
                
                StickToWall();
            }
        }
        if (_canDash && !_onWall) StartCoroutine(Dash(_horizontalDirection));
        if (_onWall) _isDelay = false;

        Debug.Log("jump state" + _isJumping);
        Debug.Log(_facingRight);
        //Animation
        _anim.SetBool("isCrouching", _onCrouch);
        _anim.SetBool("isDashing", _isDashing);
        _anim.SetBool("onGround", _onGround);
        _anim.SetFloat("horizontalDirection", Mathf.Abs(_horizontalDirection));
        Animation();
        //---------//
        

    }
    ////////////////////////////////////////////////////////////////Animtions///////////////////////////////////////////////////////////////////////
    private void ChangeAnimationState(string newState)
    {
        if (currentState == newState) return;
        _anim.Play(newState);

    }
    private IEnumerator WaitForAnimation(string name)
    {
        
        while (!_anim.GetCurrentAnimatorStateInfo(0).IsName(name))
        {
            _isDelay = true;
            if (_onWall)
            {
                _isDelay = false;
                yield return null;
                break;
            }
            yield return null;

        }
        _isDelay = false;
        Debug.Log(_isDelay);
    }

    private void Animation()
    {
        if (_waitForStart)
        {
            ChangeAnimationState(ENTRANCE);
            Task.Delay(1500).ContinueWith(t => _waitForStart = false);
        }
        if ((_horizontalDirection < 0f && _facingRight || _horizontalDirection > 0f && !_facingRight) && !_wallSlide)
        {
            Flip();
        }
        if (_isJumping && !_isFalling && !_isDashing && _dashbufferCounter < 0f && !_isSJumping &&!_fastJump &&_verticalDirection != -1)
        {
            
            ChangeAnimationState(JUMP);
        }
        if (_isFalling && !_inAir && !_isDashing && !_onGround && !_isJumping && !_isSJumping && !_inAir && !_fastJump && !_isDelay)
        {
            
            ChangeAnimationState(FALLING);
        }
        if (_wallSlide)

        {
            ChangeAnimationState(WALLSLIDE);
        }
        if (_isDashing)
        {
            ChangeAnimationState(DASH);
        }
        if (_onCrouch)
        {
            ChangeAnimationState(CROUCH);
        }
        if (!_isDashing && _onWall &&!_wallSlide &&!_isMoving &&!_isJumping && !_onCrouch)
        {
            
            ChangeAnimationState(STAND);
        }
        if(_verticalDirection == -1 &&!_isDashing && (_isJumping || _isFalling) && !_onWall)
        {
            
            ChangeAnimationState(FASTJUMP);
            StartCoroutine(WaitForAnimation("Idle"));
        }
        //if (_inAir && !_onGround && !_onWall)
        //{
        //  IS PRETTY BUGGY
        //    ChangeAnimationState(WALdsLJUMP);
        //}
    }
    //////////////////////////////////////////////////////////////Animtions_END//////////////////////////////////////////////////////////////////////
    //--------------------------------------------------------------------------------------------------------------------------------------------//
    /////////////////////////////////////////////////////////////Player_Controls////////////////////////////////////////////////////////////////////
    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
    private void MoveCharacter()
    {
        if (!_onCrouch && !_isDashing)
        {
            _rb.AddForce(new Vector2(_horizontalDirection, 0f) * _movementAcceleration);
            if (Mathf.Abs(_rb.velocity.x) > _maxMoveSpeed)
            {
                _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * _maxMoveSpeed, _rb.velocity.y);
            }
        }
        if (_verticalDirection < 0f && _dashbufferCounter < 0 && !_isDashing && !_inAir && !_onWall && (_isJumping || _isFalling) )
        {
            _fastJump = true;
            _rb.AddForce(new Vector2(0f, _verticalDirection) * _movementAcceleration);
        }
        if (_verticalDirection < 0f && _onGround && !_isDelay && _dashbufferCounter < 0 && !_isDashing)
        {
            _onCrouch = true;
        }
        if(_verticalDirection == 0f) _onCrouch = false;
        
    }
    private void Jump(Vector2 direction)
    {
        _isFalling = false;
        _isJumping = true;
        _isSJumping = false;
        if (!_onGround && !_onWall)
        {
            --_extraJumpValue;
        }
        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        _rb.AddForce(direction * _jumpForce, ForceMode2D.Impulse);
    }
    private IEnumerator Dash(float x)
    {
        float dashStartTime = Time.time;
        _hasDashed = true;
        _isDashing = true;
        Vector2 dir = new Vector2(0f, 0f);
        if (x != 0f) dir = new Vector2(x, -.4f);
        while (Time.time < dashStartTime + _dashLength)
        {
            _isDashing = true;
            _rb.velocity = dir.normalized * _dashSpeed;
            if (_onWall)
            {
                _isDashing = false;
                
                yield return null;
                break;
            }
            yield return null;
        }
        if (_onGround && !_onWall)
        {
            _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
            _isSJumping = true;
        }
        _isDashing = false;
        _hasDashed = false;
    }
    private IEnumerator WallJump()
    {
        
       _inAir = true;
        float jumpTime = Time.time;
        Vector2 jumpDirection = _onRightWall ? Vector2.left : Vector2.right;
        Jump(Vector2.up * 1.2f + jumpDirection);
        Flip();
        while (Time.time < jumpTime + 0.3f)
        {
            _inAir = true;
            if (_onGround)
            {
                _inAir = false;           
                yield return null;
                break;
            }
            yield return null;
        }
        _inAir = false;
    }
    /////////////////////////////////////////////////////////////Player_Controls-END///////////////////////////////////////////////////////////////
    //-------------------------------------------------------------------------------------------------------------------------------------------//
    //////////////////////////////////////////////////////////Player_Enviroment_Physics///////////////////////////////////////////////////////////
    private void ApplyLinearDrag()
    {
        if (Mathf.Abs(_horizontalDirection) < 0.4f || _changingDirection)
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
    private void WallSlide()
    {
        _isFalling = false;
        _isJumping = false;
        _isDelay = false;
        if (!_facingRight)
        {
            Flip();
        }
        else if(_facingRight)
        {
            Flip();
        }
        
        _rb.velocity = new Vector2(_rb.velocity.x, -_maxMoveSpeed * _wallSlideModifier);
        
    }
    private void StickToWall()
    {
        _isFalling = false;
        
        _isSJumping = false;
        _isDashing = false;
        if (_onRightWall && _horizontalDirection >= 0f)
        {
            _rb.velocity = new Vector2(4f, _rb.velocity.y);
        }
        else if (!_onRightWall && _horizontalDirection <= 0f)
        {
            _rb.velocity = new Vector2(-4f, _rb.velocity.y);
        }
    }
    private void Flip()
    {
        if (_onWall && _facingRight && !_onRightWall)
        {
            _facingRight = !_facingRight;
            transform.Rotate(0f, 180f, 0f);
        }
        if (!_facingRight && _onRightWall)
        {
            _facingRight = !_facingRight;
            transform.Rotate(0f, 180f, 0f);
        }
        if(_onGround || _isJumping || _isFalling || _inAir)
        {
            _facingRight = !_facingRight;
            transform.Rotate(0f, 180f, 0f);
        }
        
        
    }
    private void FallMultiplier()
    {
        if (_rb.velocity.y < 0)
        {
            _rb.gravityScale = _fallMultiplier;
        }
        else if (_rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            _rb.gravityScale = _lowJumpFallMultiplier;
        }
        else if (_rb.velocity.y > 0)
        {
            _rb.gravityScale += 0.2f;
        }
    }
    private void CheckCollisions()
    {
        _onGround = Physics2D.Raycast(transform.position + _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer) ||
                    Physics2D.Raycast(transform.position - _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer);
        _onWall = Physics2D.Raycast(transform.position, Vector2.right, _wallRaycastLength, _wallLayer) ||
                  Physics2D.Raycast(transform.position, Vector2.left, _wallRaycastLength, _wallLayer);
        _onRightWall = Physics2D.Raycast(transform.position, Vector2.right, _wallRaycastLength, _wallLayer);
        if (_isDashing)
        {
            _onWall = Physics2D.Raycast(transform.position, Vector2.right, .54f, _wallLayer) ||
            Physics2D.Raycast(transform.position, Vector2.left, .54f, _wallLayer);
            _onRightWall = Physics2D.Raycast(transform.position, Vector2.right, .54f, _wallLayer);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + _groundRaycastOffset, transform.position + _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        Gizmos.DrawLine(transform.position - _groundRaycastOffset, transform.position - _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        //Wall Check
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * _wallRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * _wallRaycastLength);
    }
    //////////////////////////////////////////////////////////Player_Enviroment_Physics-End///////////////////////////////////////////////////////
    //------------------------------------------------------------------------------------------------------------------------------------------//
}
