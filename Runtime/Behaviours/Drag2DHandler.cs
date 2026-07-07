using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FoundationPlatform.Behaviours
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Drag2DHandler : MonoBehaviour
    {
        public UnityEvent onDrag;
        public UnityEvent onDragStart;
        public UnityEvent onDragEnd;

        [Header("Options")] public bool lockX = false;
        public bool lockY = false;
        public bool clampToCamera = true;
        public float zDepthFromCamera = 10f;
        public bool useSmoothing = true;
        public float smoothingSpeed = 25f;

        [Header("Runtime")] public bool isDragging;

        private Vector3 worldDragOffset;
        private Camera mainCamera;

        private void OnEnable()
        {
            mainCamera = Camera.main;
            var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Vector2 S = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size : Vector2.one;
                BoxCollider2D boxCollider2D = gameObject.GetComponent<BoxCollider2D>();
                boxCollider2D.size = S;
            }
        }

        void OnMouseDown()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            float distance = zDepthFromCamera;
            if (mainCamera != null)
            {
                distance = Mathf.Abs(mainCamera.WorldToScreenPoint(transform.position).z);
            }

            Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector3 worldPointAtMouse = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distance));
            worldDragOffset = transform.position - worldPointAtMouse;
            isDragging = true;
            onDragStart?.Invoke();
        }

        private void OnMouseUp()
        {
            isDragging = false;
            onDragEnd?.Invoke();
        }

        void OnMouseDrag()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            float distance = Mathf.Abs(mainCamera.WorldToScreenPoint(transform.position).z);
            Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distance));
            Vector3 target = mouseWorld + worldDragOffset;

            Vector3 current = transform.position;
            if (lockX) target.x = current.x;
            if (lockY) target.y = current.y;
            target.z = current.z; // keep original z

            if (clampToCamera && mainCamera != null && mainCamera.orthographic)
            {
                Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
                Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, distance));
                var col = GetComponent<Collider2D>();
                Vector2 extents = Vector2.zero;
                if (col != null)
                {
                    Bounds b = col.bounds;
                    extents = b.extents;
                }
                target.x = Mathf.Clamp(target.x, min.x + extents.x, max.x - extents.x);
                target.y = Mathf.Clamp(target.y, min.y + extents.y, max.y - extents.y);
            }

            if (useSmoothing)
            {
                transform.position = Vector3.Lerp(current, target, Time.deltaTime * smoothingSpeed);
            }
            else
            {
                transform.position = target;
            }
            onDrag?.Invoke();
        }
    }
}