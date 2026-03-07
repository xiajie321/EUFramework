using System;
using EUFramework.Extension.EUFSMKit;
using EUFramework.Extension.EUInputControllerKit.MonoComponent;
using EUFramework.Extension.EUPlayerControllerKit.State;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUPlayerControllerKit
{
    public class EUPlayerController : MonoBehaviour
    {
        private EUFSM<EUPlayerState> _eufsm;
        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private EUPlayerInputController _playerInputController;
        private SpriteRenderer _spriteRenderer;
        public Animator Animator => _animator;
        public  Rigidbody2D Rigidbody => _rigidbody;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        private void Start()
        {
            InitComponent();
            InitEUPlayerInputController();
            _eufsm = new EUFSM<EUPlayerState>();
            _eufsm.AddState(EUPlayerState.Idle,new EUPlayerIdleState(_eufsm,this));
            _eufsm.AddState(EUPlayerState.Move,new EUPlayerMoveState(_eufsm,this));
            _eufsm.StartState(EUPlayerState.Idle);
        }
        
        private void InitComponent()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        private void InitEUPlayerInputController()
        {
            _playerInputController = new();
            _playerInputController.PlayerInputController.PlayerInputControllerEvent.AddMoveListener(Move);
            _playerInputController.PlayerInputController.PlayerInputControllerEvent.AddJumpListener(Jump);
            _playerInputController.AddInputDeviceAdded(OnInputDeviceAdded);
            _playerInputController.AddInputDeviceRemoved(OnInputDeviceRemoved);
            _playerInputController.Enable();
        }
        private Vector2 pos;
        public Vector2 Pos => pos;

        public void Move(InputAction.CallbackContext context)
        {
            // pos = context.ReadValue<Vector2>() * 5;
            // Vector3 scale = _spriteRenderer.transform.localScale;
            // float x = Mathf.Clamp(-Mathf.Floor(pos.x),-1,1);
            // if(x == 0) return;
            // _spriteRenderer.transform.localScale = new Vector3(x,scale.y,scale.z);
        }

        public void Jump(InputAction.CallbackContext context)
        {
            // if (context.performed)
            //     Rigidbody.velocity += Vector2.up*5;
        }
        private void OnInputDeviceAdded(InputDevice inputDevice)
        {
            
        }

        private void OnInputDeviceRemoved(InputDevice inputDevice)
        {
            
        }

        private void Update()
        {
            _eufsm.Update();
        }

        private void FixedUpdate()
        {
            _eufsm.FixedUpdate();
        }
    }
}
