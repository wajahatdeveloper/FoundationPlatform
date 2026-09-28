using UnityEngine;
using UnityEngine.Events;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    /// <summary>
    /// Drop zone for <see cref="Drag2DHandler"/> objects. Overlap is tracked by trigger; the drop fires
    /// when the dragged object's drag ends while it still overlaps this zone.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("FoundationPlatform/Drop2D Handler")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/Drop2DHandler.png")]
    [DesignerIcon(DesignerSymbol.Input)]
    public class Drop2DHandler : MonoBehaviour
    {
        public UnityEvent<GameObject> onDrop;
        public UnityEvent<GameObject> onHoverEnter;
        public UnityEvent<GameObject> onHoverExit;

        [Header("Options")] public bool makeColliderTrigger = true;
        public bool freezeRigidbody = true;
        public bool snapDroppedToCenter = false;
        public Vector2 snapOffset;

        [Header("Debug")] public GameObject droppedObject;

        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();

            if (makeColliderTrigger)
            {
                _collider.isTrigger = true;
            }

            if (freezeRigidbody)
            {
                var rb = GetComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            droppedObject = collision.gameObject;
            if (collision.TryGetComponent(out Drag2DHandler drag))
            {
                drag.SetHoveredDrop(this);
            }
            onHoverEnter?.Invoke(droppedObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out Drag2DHandler drag))
            {
                drag.ClearHoveredDrop(this);
            }
            onHoverExit?.Invoke(collision.gameObject);
            if (collision.gameObject == droppedObject)
            {
                droppedObject = null;
            }
        }

        internal void AcceptDrop(Drag2DHandler drag)
        {
            var dropped = drag.gameObject;
            onDrop?.Invoke(dropped);
            if (snapDroppedToCenter)
            {
                Vector3 center = _collider.bounds.center;
                dropped.transform.position = new Vector3(center.x + snapOffset.x, center.y + snapOffset.y, dropped.transform.position.z);
            }
        }
    }
}
