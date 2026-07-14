using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
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
                if (_collider != null) _collider.isTrigger = true;
            }

            if (freezeRigidbody)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.simulated = true;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            droppedObject = collision.gameObject;
            onHoverEnter?.Invoke(droppedObject);
        }

        private void Update()
        {
            if (droppedObject == null)
            {
                return;
            }

            if (droppedObject.GetComponent<Drag2DHandler>() == null)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                onDrop?.Invoke(droppedObject);
                if (snapDroppedToCenter && droppedObject != null)
                {
                    var drag = droppedObject.GetComponent<Drag2DHandler>();
                    if (drag != null && !drag.isDragging)
                    {
                        Vector3 center = _collider != null ? _collider.bounds.center : transform.position;
                        droppedObject.transform.position = new Vector3(center.x + snapOffset.x, center.y + snapOffset.y, droppedObject.transform.position.z);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            onHoverExit?.Invoke(collision.gameObject);
            if (collision.gameObject == droppedObject)
            {
                droppedObject = null;
            }
        }
    }
}