using UnityEngine;

namespace FoundationPlatform.Behaviours
{
    public class CameraLookConstraint : MonoBehaviour
    {
        public enum UpdatePhase
        {
            Update,
            LateUpdate,
            FixedUpdate
        }

        [Header("Target Camera")]
        [SerializeField] private Camera cameraToLookAt;
        [SerializeField] private bool fallbackToMainCamera = true;

        [Header("Behaviour")]
        [SerializeField] private UpdatePhase updatePhase = UpdatePhase.LateUpdate;
        [SerializeField] private bool onlyRotateAroundY = true;
        [SerializeField] private bool faceAwayFromCamera = false;
        [SerializeField] private Vector3 localRotationOffsetEuler = Vector3.zero;

        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void OnEnable()
        {
            EnsureCamera();
        }

        private void Update()
        {
            if (updatePhase == UpdatePhase.Update)
            {
                ApplyLookAt();
            }
        }

        private void LateUpdate()
        {
            if (updatePhase == UpdatePhase.LateUpdate)
            {
                ApplyLookAt();
            }
        }

        private void FixedUpdate()
        {
            if (updatePhase == UpdatePhase.FixedUpdate)
            {
                ApplyLookAt();
            }
        }

        public void SetCamera(Camera camera)
        {
            cameraToLookAt = camera;
        }

        private void EnsureCamera()
        {
            if (cameraToLookAt == null && fallbackToMainCamera)
            {
                cameraToLookAt = Camera.main;
            }
        }

        private void ApplyLookAt()
        {
            if (cameraToLookAt == null)
            {
                EnsureCamera();
                if (cameraToLookAt == null)
                {
                    return;
                }
            }

            Transform cameraTransform = cameraToLookAt.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 selfPosition = cachedTransform.position;

            // Direction from the object toward the camera.
            Vector3 direction = cameraPosition - selfPosition;

            if (faceAwayFromCamera)
            {
                // Invert before any Y-flattening so the two options compose correctly.
                direction = -direction;
            }

            if (onlyRotateAroundY)
            {
                direction.y = 0f;
            }

            Vector3 targetPosition = selfPosition + direction;

            cachedTransform.LookAt(targetPosition);

            if (localRotationOffsetEuler != Vector3.zero)
            {
                cachedTransform.Rotate(localRotationOffsetEuler, Space.Self);
            }
        }
    }
}