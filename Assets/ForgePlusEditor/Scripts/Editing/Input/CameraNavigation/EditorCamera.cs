using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
using RuntimeCore.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgePlus.CameraNavigation
{
    [RequireComponent(typeof(Camera))]
    public class EditorCamera : MonoBehaviour
    {
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

        // How far each notch of the mouse wheel moves the camera forward or back, in meters
        [SerializeField]
        private float wheelMoveDistancePerNotch = 0.5f;

        private Vector3 currentVelocityVector = Vector3.zero;
        private int blockerCount = 0;

        private bool isFraming = false;
        private Vector3 framingStartPosition;
        private Vector3 framingTargetPosition;
        private float framingStartTime;

        public void FrameSelected()
        {
            if (SelectionFramingBounds.TryGetBounds(SelectionManager.Instance.Selection, out var bounds))
            {
                Frame(bounds);
            }
        }

        public void OnInputBlockerChanged(bool isBlocking)
        {
            if (isBlocking)
            {
                blockerCount++;

                // There's no navigating to animate the framing in while blocked
                if (isFraming)
                {
                    FinishFraming();
                }
            }
            else
            {
                blockerCount--;
            }
        }

        private bool IsNavigationBlocked
        {
            get
            {
                return blockerCount > 0;
            }
        }

        // Frames the level's first player spawn, or polygon 0 if it has no player spawns
        private void OnLevelOpened(string levelName)
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return;
            }

            ISelectable target = null;
            var firstSpawnIndex = short.MaxValue;

            foreach (var mapObject in level.MapObjects)
            {
                if (mapObject.Value.NativeObject.type == map._saved_player && mapObject.Key < firstSpawnIndex)
                {
                    firstSpawnIndex = mapObject.Key;
                    target = mapObject.Value;
                }
            }

            if (target == null && level.Polygons.TryGetValue(0, out var firstPolygon))
            {
                target = firstPolygon;
            }

            if (target != null && SelectionFramingBounds.TryGetBounds(target, out var bounds))
            {
                Frame(bounds);
            }
        }

        private void Frame(Bounds bounds)
        {
            var camera = GetComponent<Camera>();

            // Fit the bounds' enclosing sphere into the narrower of the two fields of view
            var halfVerticalFieldOfView = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var halfHorizontalFieldOfView = Mathf.Atan(Mathf.Tan(halfVerticalFieldOfView) * camera.aspect);
            var distance = bounds.extents.magnitude / Mathf.Sin(Mathf.Min(halfVerticalFieldOfView, halfHorizontalFieldOfView));

            framingTargetPosition = bounds.center - transform.forward * distance;
            currentVelocityVector = Vector3.zero;

            if (IsNavigationBlocked || framingDuration <= 0f)
            {
                // Not navigating (such as while a menu is open), so there's nothing to animate in
                FinishFraming();

                return;
            }

            framingStartPosition = transform.position;
            framingStartTime = Time.time;
            isFraming = true;
        }

        private void FinishFraming()
        {
            transform.position = framingTargetPosition;
            isFraming = false;
        }

        private void Start()
        {
            UIBlocking.Instance.OnChanged += OnInputBlockerChanged;
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
        }

        private void Update()
        {
            var wheelNotches = GetWheelMoveInput();

            // While blocked (such as by the menu), only the wheel moves the camera
            if (IsNavigationBlocked)
            {
                MoveByWheel(wheelNotches);
                return;
            }

            #region Framing
            if (Hotkeys.WasPressed(ForgePlusInput.Camera.FrameSelected))
            {
                FrameSelected();
            }

            if (isFraming)
            {
                if (HasNavigationInput(wheelNotches))
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
            var isTurboMode = Hotkeys.IsPressed(ForgePlusInput.Camera.Turbo);
            var acceleration = isTurboMode ? turboAccelerationPerSecond : accelerationPerSecond;
            acceleration *= Time.deltaTime;
            var maxVelocity = isTurboMode ? maxTurboVelocity : this.maxVelocity;

            UpdateVelocityAxis(ref currentVelocityVector.x, Hotkeys.ReadAxis(ForgePlusInput.Camera.Strafe), acceleration, maxVelocity);
            UpdateVelocityAxis(ref currentVelocityVector.y, Hotkeys.ReadAxis(ForgePlusInput.Camera.Elevate), acceleration, maxVelocity);
            UpdateVelocityAxis(ref currentVelocityVector.z, Hotkeys.ReadAxis(ForgePlusInput.Camera.Advance), acceleration, maxVelocity);

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
            if (Hotkeys.IsPressed(ForgePlusInput.Camera.EnableLook))
            {
                var eulerRotation = transform.eulerAngles;
                var look = ForgePlusInput.Camera.Look.ReadValue<Vector2>();

                eulerRotation.y += look.x;

                eulerRotation.x -= look.y;

                if (eulerRotation.x > 180f)
                {
                    eulerRotation.x = (eulerRotation.x - 360f);
                }

                eulerRotation.x = Mathf.Clamp(eulerRotation.x, -90f, 90f);

                transform.eulerAngles = eulerRotation;
            }
            #endregion Looking

            MoveByWheel(wheelNotches);
        }

        // The input is an axis action's value (0 while neither or both of its keys are held)
        private void UpdateVelocityAxis(ref float axialVelocity, float input, float acceleration, float maxVelocity)
        {
            var axialVelocityDirection = Mathf.Sign(axialVelocity);
            var deceleration = decelerationPerSecond * Time.deltaTime;

            if (input == 0f ||
                (input > 0f && axialVelocity < 0f) ||
                (input < 0f && axialVelocity > 0f))
            {
                // Deceleration
                // (if there's no input, or if velocity is currently opposed to the input direction)
                axialVelocity -= axialVelocityDirection * GetScaledDeceleration(deceleration, Mathf.Abs(axialVelocity));

                if (input == 0f &&
                    ((axialVelocityDirection > 0f && axialVelocity < 0f) ||
                     (axialVelocityDirection < 0f && axialVelocity > 0f)))
                {
                    // If it crossed 0 velocity, make it 0
                    // (only if there's no input, as we want to allow acceleration to continue if there is input)
                    axialVelocity = 0f;
                }
            }
            else
            {
                var inputDirection = Mathf.Sign(input);

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

        private bool HasNavigationInput(float wheelNotches)
        {
            return Hotkeys.ReadAxis(ForgePlusInput.Camera.Strafe) != 0f ||
                   Hotkeys.ReadAxis(ForgePlusInput.Camera.Elevate) != 0f ||
                   Hotkeys.ReadAxis(ForgePlusInput.Camera.Advance) != 0f ||
                   Hotkeys.IsPressed(ForgePlusInput.Camera.EnableLook) ||
                   wheelNotches != 0f;
        }

        private void MoveByWheel(float wheelNotches)
        {
            transform.position += transform.forward * (wheelNotches * wheelMoveDistancePerNotch);
        }

        // The wheel's notches this frame, unless the pointer that scrolled is over the UI (which the wheel scrolls instead)
        private float GetWheelMoveInput()
        {
            var wheelMoveAction = ForgePlusInput.Camera.WheelMove;
            var notches = Hotkeys.ReadAxis(wheelMoveAction);
            if (notches == 0f)
            {
                return 0f;
            }

            var pointer = wheelMoveAction.activeControl?.device as Pointer;
            var ui = ForgePlusUI.Instance;
            if (pointer != null && ui && ui.IsPointerOverUI(pointer.position.ReadValue()))
            {
                return 0f;
            }

            return notches;
        }
    }
}
