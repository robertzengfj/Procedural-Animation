using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;


public class MovementControl : MonoBehaviour
{
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currentRunMovement;
    Vector3 _appliedMovement; //附加跳跃动作后的移动
    [Header("References")]
    //获取charactercontroller组件
    CharacterController characterController;
    PlayerInput playerInput;
    Animator animator;
    public float runMultiplier = 3.0f;
    private bool _isMovementPressed;
    private bool _isRunPressed = false;
    private bool _isJumpPressed = false;
    int _isWalkingHash;
    int _isRunningHash;
    int _isJumpingHash;
    int _jumpCount = 0;
    int jumpCountHash;
    Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();  //多次跳跃的初始速度
    Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();         //多次跳跃的重力
    Coroutine _currentJumpResetRountine = null;
    public float _roationFactorPerFrame = 15.0f;
    bool _isJumping = false;
    bool _isJumpAnimating = false;
    private float _maxJumpTime = 0.75f;
    private float _maxJumpHeight = 2.0f;
    float _gravity = -9.8f;
    float _groudedGravity = -.05f;
    private float _initialJumpVelocity;
    void Awake()
    {
        playerInput = new PlayerInput();
        //Assert.IsNotNull(playerInput, "playerInput is null");
        if (playerInput != null)
        {
            Debug.Log("playerInput in awake is not null");
        }
        else
        {
            Debug.Log("playerInput in awake is null");
        }
        characterController = GetComponent<CharacterController>();
        playerInput.CharacterControls.Movement.started += onMovemnetInput;
        playerInput.CharacterControls.Movement.performed += onMovemnetInput;
        playerInput.CharacterControls.Movement.canceled += onMovemnetInput;
        playerInput.CharacterControls.Run.started += onRun;
        //playerInput.CharacterControls.Run.performed += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;
        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;
        animator = GetComponent<Animator>();
        _isWalkingHash = Animator.StringToHash("IsWalking");
        _isRunningHash = Animator.StringToHash("IsRun");
        _isJumpingHash = Animator.StringToHash("IsJumping");
        jumpCountHash = Animator.StringToHash("JumpCount");
        setupJumpVariables();
    }
    void onMovemnetInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _currentRunMovement.x = _currentMovementInput.x * runMultiplier;
        _currentRunMovement.z = _currentMovementInput.y * runMultiplier;
        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;

    }
    
        void onRun(InputAction.CallbackContext context)
    {

        _isRunPressed = context.ReadValueAsButton();
    }
    void onJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
    }
    void handleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _currentMovement.z;
        Quaternion currentRotation = transform.rotation;
        if (_isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _roationFactorPerFrame * Time.deltaTime);
        }

    }
    void handleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        float fallMultiplier = 2.0f;
        if (characterController.isGrounded)
        {
            //set animator here
            if (_isJumpAnimating)
            {
                animator.SetBool(_isJumpingHash, false);
                _isJumpAnimating = false;
                _currentJumpResetRountine = StartCoroutine(jumpResetRoutine());
                if (_jumpCount == 3)
                {
                    _jumpCount = 0;
                    animator.SetInteger(jumpCountHash, _jumpCount);
                }
            }
            _currentMovement.y = _groudedGravity;
            _appliedMovement.y = _groudedGravity;
        }
        else if (isFalling)
        {
            Debug.Log("falling");
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
            _appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * .5f, -20.0f);
            //currentMovement.y = currentMovement.y;
            //Debug.Log("falling current movement y" + currentMovement.y);
            _currentRunMovement.y = _currentMovement.y;
        }
        else
        {
            //float groudedGravity = -9.8f;
            float previousYVelocity = _currentMovement.y;

            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);


            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * 0.5f;

            //currentMovement.y = currentMovement.y;

            _currentRunMovement.y = _currentMovement.y;

        }
    }
        void handleAnimation()
    {
        bool isWalking = animator.GetBool(_isWalkingHash);
        bool isRunning = animator.GetBool(_isRunningHash);
        
        if (_isMovementPressed && !isWalking)
        {
            Debug.Log("is movement pressed");
            animator.SetBool(_isWalkingHash, true);
        }
        else if (!_isMovementPressed && isWalking)
        {
            animator.SetBool(_isWalkingHash, false);
        }
        if (_isMovementPressed && _isRunPressed && (!isRunning))
        {
            Debug.Log("is running pressed");
            animator.SetBool(_isRunningHash, true);
        }
        else if (!_isRunPressed && isRunning)
        {

            animator.SetBool(_isRunningHash, false);
        }


    }
    void handleJump()
    {
        Debug.Log("jump count" + _jumpCount);
        if (!_isJumping && characterController.isGrounded && _isJumpPressed)
        {
            if (_jumpCount < 3 && _currentJumpResetRountine != null)
            {
                StopCoroutine(_currentJumpResetRountine);
            }
            //set animator here
            animator.SetBool(_isJumpingHash, true);
            _isJumpAnimating = true;
            _isJumping = true;
            _jumpCount += 1;
            animator.SetInteger(jumpCountHash, _jumpCount);
            _currentMovement.y = _initialJumpVelocities[_jumpCount];
            _appliedMovement.y = _initialJumpVelocities[_jumpCount];

        }else if (!_isJumpPressed && _isJumping&& characterController.isGrounded)
        {
            _isJumping = false;
        }
    }
    void setupJumpVariables()
    {
        //跳跃公式：
        float timetoApex = _maxJumpTime / 2; //到达顶部的时间
        _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timetoApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timetoApex;
        float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timetoApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timetoApex * 1.25f);
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow(timetoApex * 1.5f, 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 3)) / Mathf.Pow((timetoApex * 1.5f), 2);
        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);
        _jumpGravities.Add(0, _gravity);
        _jumpGravities.Add(1, _gravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
    }
    IEnumerator jumpResetRoutine()
    {
        //初始化一个线程，N时间后执行
        yield return new WaitForSeconds(.5f);
        _jumpCount = 0;
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        handleAnimation();
        handleRotation();
        if (_isRunPressed)
        {
            _appliedMovement.x = _currentRunMovement.x;
            _appliedMovement.z = _currentRunMovement.z;
        }
        else
        {
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
        }
        characterController.Move(_appliedMovement * Time.deltaTime);
        handleGravity();
        handleJump();
    }
    void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }
    void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
