using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    /// <summary>
    /// Drags a 2D world object with the pointer through the EventSystem. The camera that sees this
    /// object needs a <see cref="Physics2DRaycaster"/> and the scene needs an <see cref="EventSystem"/>.
    /// Dropping while overlapping a <see cref="Drop2DHandler"/> hands the object to it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("FoundationPlatform/Drag2D Handler")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/Drag2DHandler.png")]
    [DesignerIcon(DesignerSymbol.Input)]
    public class Drag2DHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UnityEvent onDrag;
        public UnityEvent onDragStart;
        public UnityEvent onDragEnd;

        [Header("Options")] public bool lockX = false;
        public bool lockY = false;
        public bool clampToCamera = true;
        public bool useSmoothing = true;
        public float smoothingSpeed = 25f;

        [Header("Runtime")] public bool isDragging;

        private Vector3 worldDragOffset;
        private BoxCollider2D boxCollider;
        private Drop2DHandler hoveredDrop;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void OnEnable()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                boxCollider.size = spriteRenderer.sprite.bounds.size;
            }
        }

        internal void SetHoveredDrop(Drop2DHandler drop) => hoveredDrop = drop;

        internal void ClearHoveredDrop(Drop2DHandler drop)
        {
            if (hoveredDrop == drop)
            {
                hoveredDrop = null;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera;
            worldDragOffset = transform.position - PointerToWorld(cam, eventData.position);
            isDragging = true;
            onDragStart?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera;
            Vector3 target = PointerToWorld(cam, eventData.position) + worldDragOffset;

            Vector3 current = transform.position;
            if (lockX) target.x = current.x;
            if (lockY) target.y = current.y;
            target.z = current.z;

            if (clampToCamera && cam.orthographic)
            {
                float distance = Mathf.Abs(cam.WorldToScreenPoint(current).z);
                Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, distance));
                Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, distance));
                Vector2 extents = boxCollider.bounds.extents;
                target.x = Mathf.Clamp(target.x, min.x + extents.x, max.x - extents.x);
                target.y = Mathf.Clamp(target.y, min.y + extents.y, max.y - extents.y);
            }

            transform.position = useSmoothing
                ? Vector3.Lerp(current, target, Time.deltaTime * smoothingSpeed)
                : target;
            onDrag?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            onDragEnd?.Invoke();
            if (hoveredDrop != null)
            {
                hoveredDrop.AcceptDrop(this);
            }
        }

        private Vector3 PointerToWorld(Camera cam, Vector2 screenPosition)
        {
            float distance = Mathf.Abs(cam.WorldToScreenPoint(transform.position).z);
            return cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
        }
    }
}
