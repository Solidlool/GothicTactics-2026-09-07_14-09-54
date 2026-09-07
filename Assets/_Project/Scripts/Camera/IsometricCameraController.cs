using UnityEngine;
using UnityEngine.InputSystem;

namespace GothicTactics.CameraSystem
{
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float panSpeed = 12f;
        [SerializeField, Min(0.1f)] private float zoomSpeed = 1.5f;
        [SerializeField] private Vector2 zoomLimits = new(6f, 24f);

        private Camera controlledCamera;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var input = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

                var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                transform.position += (forward * input.y + right * input.x).normalized * (panSpeed * Time.deltaTime);
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            if (controlledCamera != null && controlledCamera.orthographic)
            {
                controlledCamera.orthographicSize = Mathf.Clamp(
                    controlledCamera.orthographicSize - Mathf.Sign(scroll) * zoomSpeed,
                    zoomLimits.x,
                    zoomLimits.y);
            }
        }
    }
}
