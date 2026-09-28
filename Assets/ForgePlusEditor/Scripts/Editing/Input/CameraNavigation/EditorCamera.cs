using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using UnityEngine;

namespace ForgePlus.CameraNavigation
{
    [RequireComponent(typeof(Camera))]
    public class EditorCamera : MonoBehaviour
    {
        // TODO: Convert this to use Unity's new input system.
        private const KeyCode forward = KeyCode.W;
        private const KeyCode backward = KeyCode.S;
        private const KeyCode left = KeyCode.A;
        private const KeyCode right = KeyCode.D;
        private const KeyCode up = KeyCode.E;
        private const KeyCode down = KeyCode.Q;
        private const KeyCode turbo = KeyCode.LeftShift;
        private const KeyCode rotateA = KeyCode.Space;
        private const KeyCode rotateB = KeyCode.Mouse1;
        private const KeyCode frameSelected = KeyCode.F;

        [SerializeField]
        private float maxVelocity = 5f;

        [SerializeField]
        private float maxTurboVelocity = 30f;

        [SerializeField]
        private float accelerationPerSecond = 5f;

        [SerializeField]
        private float turboAccelerationPerSecond = 30f;

        [SerializeField]
        private float decelerationPerSecond = 40f;

        [SerializeField]
        private float framingDuration = 0.25f;

        private Vector3 currentVelocityVector = Vector3.zero;
        private int blockerCount = 0;

        private bool isFraming = false;
        private Vector3 framingStartPosition;
        private Vector3 framingTargetPosition;
        private float framingStartTime;

        public void FrameSelected()
        {
            if (!SelectionFramingBounds.TryGetBounds(SelectionManager.Instance.Selection, out var bounds))
            {
                return;
            }

            var camera = GetComponent<Camera>();

            // Fit the bounds' enclosing sphere into the narrower of the two fields of view
            var halfVerticalFieldOfView = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var halfHorizontalFieldOfView = Mathf.Atan(Mathf.Tan(halfVerticalFieldOfView) * camera.aspect);
            var distance = bounds.extents.magnitude / Mathf.Sin(Mathf.Min(halfVerticalFieldOfView, halfHorizontalFieldOfView));

            framingTargetPosition = bounds.center - transform.forward * distance;
            currentVelocityVector = Vector3.zero;

            if (!isActiveAndEnabled || framingDuration <= 0f)
            {
                // Not navigating (such as while a menu is open), so there's no Update to animate in
                transform.position = framingTargetPosition;
                isFraming = false;

                return;
            }

            framingStartPosition = transform.position;
            framingStartTime = Time.time;
            isFraming = true;
        }

        public void OnInputBlockerChanged(bool isBlocking)
        {
            if (isBlocking)
            {
                blockerCount++;

                enabled = false;
            }
            else
            {
                blockerCount--;

                if (blockerCount <= 0)
                {
                    enabled = true;
                }
            }
        }

        private void Start()
        {
            UIBlocking.Instance.OnChanged += OnInputBlockerChanged;

            // Start this disabled because the menu starts active
            enabled = false;
            blockerCount++;
        }

        private void OnDisable()
        {
            if (isFraming)
            {
                transform.position = framingTargetPosition;
                isFraming = false;
            }
        }

        private void Update()
        {
            #region Framing
            if (Hotkeys.GetKeyDown(frameSelected))
            {
                FrameSelected();
            }

            if (isFraming)
            {
                if (HasNavigationInput())
                {
                    isFraming = false;
                }
                else
                {
                    var progress = Mathf.Clamp01((Time.time - framingStartTime) / framingDuration);

                    transform.position = Vector3.Lerp(framingStartPosition, framingTargetPosition, Mathf.SmoothStep(0f, 1f, progress));

                    if (progress >= 1f)
                    {
                        isFraming = false;
                    }

                    return;
                }
            }
            #endregion Framing

            #region Movement
            var isTurboMode = Hotkeys.GetKey(turbo);
            var acceleration = isTurboMode ? turboAccelerationPerSecond : accelerationPerSecond;
            acceleration *= Time.deltaTime;
            var maxVelocity = isTurboMode ? maxTurboVelocity : this.maxVelocity;

            UpdateVelocityAxis(ref currentVelocityVector.x, right, left, acceleration, maxVelocity);
            UpdateVelocityAxis(ref currentVelocityVector.y, up, down, acceleration, maxVelocity);
            UpdateVelocityAxis(ref currentVelocityVector.z, forward, backward, acceleration, maxVelocity);

            var worldVelocityVector = (transform.right * currentVelocityVector.x) +
                                      (Vector3.up * currentVelocityVector.y) +
                                      (transform.forward * currentVelocityVector.z);

            var highestAxialVelocity = Mathf.Max(Mathf.Abs(worldVelocityVector.x), Mathf.Abs(worldVelocityVector.y), Mathf.Abs(worldVelocityVector.z));

            if (worldVelocityVector.sqrMagnitude > highestAxialVelocity * highestAxialVelocity)
            {
                worldVelocityVector = worldVelocityVector.normalized * highestAxialVelocity;
            }

            transform.position += worldVelocityVector * Time.deltaTime;
            #endregion Movement

            #region Looking
            if (Hotkeys.GetKey(rotateA) || Input.GetKey(rotateB))
            {
                var eulerRotation = transform.eulerAngles;

                eulerRotation.y += Input.GetAxis("Mouse X") * 1f;

                eulerRotation.x -= Input.GetAxis("Mouse Y") * 1f;

                if (eulerRotation.x > 180f)
                {
                    eulerRotation.x = (eulerRotation.x - 360f);
                }

                eulerRotation.x = Mathf.Clamp(eulerRotation.x, -90f, 90f);

                transform.eulerAngles = eulerRotation;
            }
            #endregion Looking
        }

        private void UpdateVelocityAxis(ref float axialVelocity, KeyCode positiveKey, KeyCode negativeKey, float acceleration, float maxVelocity)
        {
            bool hasPositiveInput = Hotkeys.GetKey(positiveKey);
            bool hasNegativeInput = Hotkeys.GetKey(negativeKey);

            // If neither or both inputs is active, consider it to be no input
            var inputIsZero = (!hasPositiveInput && !hasNegativeInput) || (hasPositiveInput && hasNegativeInput);

            var axialVelocityDirection = Mathf.Sign(axialVelocity);
            var deceleration = decelerationPerSecond * Time.deltaTime;

            if (inputIsZero ||
                (hasPositiveInput && axialVelocity < 0f) ||
                (hasNegativeInput && axialVelocity > 0f))
            {
                // Deceleration
                // (if there's no input, or if velocity is currently opposed to the input direction)
                axialVelocity -= axialVelocityDirection * GetScaledDeceleration(deceleration, Mathf.Abs(axialVelocity));

                if (inputIsZero &&
                    ((axialVelocityDirection > 0f && axialVelocity < 0f) ||
                     (axialVelocityDirection < 0f && axialVelocity > 0f)))
                {
                    // If it crossed 0 velocity, make it 0
                    // (only if there's no input, as we want to allow acceleration to continue if there is input)
                    axialVelocity = 0f;
                }
            }
            else if (hasPositiveInput || hasNegativeInput)
            {
                var inputDirection = hasPositiveInput ? 1f : -1f;

                // Accelerate
                axialVelocity += inputDirection * acceleration;

                // Corrective Deceleration (if faster than max velocity)
                var absoluteAxialVelocity = Mathf.Abs(axialVelocity);
                if (absoluteAxialVelocity > maxVelocity)
                {
                    var distanceFromMaxVelocity = absoluteAxialVelocity - maxVelocity;
                    var correctiveDeceleration = GetScaledDeceleration(deceleration, Mathf.Abs(axialVelocity));

                    axialVelocity -= inputDirection * Mathf.Min(distanceFromMaxVelocity, GetScaledDeceleration(deceleration, correctiveDeceleration));
                }
            }
        }

        private float GetScaledDeceleration(float deceleration, float absoluteAxialVelocity)
        {
            return Mathf.Max(deceleration, deceleration * absoluteAxialVelocity / this.maxVelocity);
        }

        private bool HasNavigationInput()
        {
            return Hotkeys.GetKey(forward) ||
                   Hotkeys.GetKey(backward) ||
                   Hotkeys.GetKey(left) ||
                   Hotkeys.GetKey(right) ||
                   Hotkeys.GetKey(up) ||
                   Hotkeys.GetKey(down) ||
                   Hotkeys.GetKey(rotateA) ||
                   Input.GetKey(rotateB);
        }
    }
}
