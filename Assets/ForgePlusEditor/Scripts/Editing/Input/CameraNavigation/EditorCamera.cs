using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using Rect = UnityEngine.Rect;

namespace ForgePlus.CameraNavigation
{
    // Flies through the level in perspective, or (while Orthographic is on) looks straight down on it from above, with the
    // map's up (the world's +Z) at the top of the view. The view's projection is blended between the two as the camera
    // moves to its new place, and becomes truly orthographic once it's there.
    //
    // While orthographic, looking around is locked, Advance (W and S) moves the camera up and down the view, Elevate (Q and
    // E) still raises and lowers it (what's above it isn't drawn), and the wheel zooms.
    //
    // Holding Pan Modifier (space) while pressing Select (the left mouse button), or pressing Pan (the middle mouse
    // button), grabs the level and pans across it (horizontally) as the pointer moves: what was grabbed stays under the
    // pointer. Looking straight down, that's wherever the pointer is; in perspective, it's the height of what's under the
    // pointer, or if nothing is, of the level's floor or ceiling corner nearest to the pointer's line of sight (favoring
    // nearer ones).
    //
    // The camera never leaves the level's bounds (across all its points, and from its lowest floor to above its highest
    // ceiling, where the orthographic view looks down from).
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

        // Into or out of the orthographic view
        [SerializeField]
        private float projectionTransitionDuration = 0.5f;

        // How much each notch of the mouse wheel shrinks (or grows) the orthographic view
        [SerializeField]
        private float wheelZoomFactorPerNotch = 0.85f;

        // Meters above the level's highest point that the orthographic view looks down from
        private const float OrthographicClearance = 1f;

        // The orthographic view's half height, in meters
        private const float MinimumOrthographicSize = 0.5f;
        private const float MaximumOrthographicSize = 500f;

        // How much of the level (in world units) the orthographic view is wide when switching to it
        private const float OrthographicStartingWidth = 14f;

        // Moving across the orthographic view goes as fast as at this size, and faster when zoomed further out
        private const float OrthographicSpeedReferenceSize = 10f;

        // How far down the view tilts when it's back in perspective
        private const float PerspectiveReturnPitch = 30f;

        // Picking what height to pan at (with nothing under the pointer), a corner this far along the pointer's line of
        // sight counts as twice as far from it as one right at the camera
        private const float PanCornerDistanceBias = 20f;

        private static readonly Quaternion OrthographicRotation = Quaternion.Euler(90f, 0f, 0f);

        private Camera viewCamera;

        private Vector3 currentVelocityVector = Vector3.zero;
        private int blockerCount = 0;

        private bool isFraming = false;
        private Vector3 framingStartPosition;
        private Vector3 framingTargetPosition;
        private float framingStartSize;
        private float framingTargetSize;
        private float framingStartTime;

        private bool isOrthographic;
        private bool isTransitioning;
        private float transitionStartTime;
        private Vector3 transitionStartPosition;
        private Vector3 transitionTargetPosition;
        private Quaternion transitionStartRotation;
        private Quaternion transitionTargetRotation;
        private Matrix4x4 transitionStartProjection;
        private Matrix4x4 transitionTargetProjection;
        private float orthographicSize = 10f;

        // What was last selected (or dragged, which selects it), to frame when going back to perspective (or when focusing
        // with nothing selected)
        private ISelectable lastSelected;

        private bool isPanning;
        private float panHeight;
        private Vector3 panGrabPoint;

        public event Action OnOrthographicChanged;

        // Whether the orthographic view is on (including while the view moves into it)
        public bool IsOrthographic
        {
            get
            {
                return isOrthographic;
            }
            set
            {
                if (value == isOrthographic)
                {
                    return;
                }

                isOrthographic = value;

                if (value)
                {
                    BeginOrthographicTransition();
                }
                else
                {
                    BeginPerspectiveTransition();
                }

                OnOrthographicChanged?.Invoke();
            }
        }

        // Focusing on something flashes it (FocusFlash), to tell it from what else is in view. With nothing selected, it
        // focuses on what was last selected, or if nothing has been (or it's gone), the object nearest to the camera.
        public void FrameSelected()
        {
            var targets = SelectionManager.Instance.Selection.ToList();
            if (targets.Count == 0 && TryGetFocusTarget(out var target))
            {
                targets.Add(target);
            }

            if (SelectionFramingBounds.TryGetBounds(targets, out var bounds))
            {
                Frame(bounds);
                FocusFlash.Flash(targets, FramingTimeLeft);
            }
        }

        // The area is in viewport coordinates (0 to 1, from the bottom left)
        public void Frame(IReadOnlyList<ISelectable> targets, Rect viewportArea)
        {
            if (SelectionFramingBounds.TryGetBounds(targets, out var bounds))
            {
                Frame(bounds, viewportArea);
                FocusFlash.Flash(targets, FramingTimeLeft);
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

        // So what's framed flashes once it's in view
        private float FramingTimeLeft
        {
            get
            {
                return isFraming ? framingDuration : 0f;
            }
        }

        private bool IsNavigationBlocked
        {
            get
            {
                return blockerCount > 0;
            }
        }

        // Frames the level's first player spawn, or polygon 0 if it has no player spawns (but a level rebuilt after an
        // edit stays where it's being looked at)
        private void OnLevelOpened(string levelName)
        {
            var level = LevelEntity_Level.Instance;
            if (!level || MapsLoading.Instance.IsRebuildingLevel)
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

        private void OnSelectionChanged()
        {
            var selection = SelectionManager.Instance.Selection;
            if (selection.Count > 0)
            {
                lastSelected = selection[selection.Count - 1];
            }
        }

        private void Frame(Bounds bounds)
        {
            Frame(bounds, new Rect(0f, 0f, 1f, 1f));
        }

        private void Frame(Bounds bounds, Rect viewportArea)
        {
            // Framing goes from where the view ends up
            if (isTransitioning)
            {
                FinishTransition();
            }

            framingStartSize = orthographicSize;
            framingTargetSize = orthographicSize;

            if (isOrthographic)
            {
                // Above it (and above where it reaches, so it isn't cut off), with it filling the area
                var horizontalRadius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
                framingTargetSize = ClampOrthographicSize(horizontalRadius / Mathf.Min(viewportArea.height, viewportArea.width * viewCamera.aspect));

                var areaOffset = (viewportArea.center * 2f) - Vector2.one;
                framingTargetPosition = new Vector3(bounds.center.x, Mathf.Max(transform.position.y, bounds.max.y + OrthographicClearance), bounds.center.z)
                                        - transform.right * (areaOffset.x * framingTargetSize * viewCamera.aspect)
                                        - transform.up * (areaOffset.y * framingTargetSize);
            }
            else
            {
                framingTargetPosition = FramingPosition(bounds, transform.rotation, viewportArea);
            }

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

        // Where the camera, turned this way, sees the bounds' enclosing sphere fill the narrower of the area's two fields
        // of view
        private Vector3 FramingPosition(Bounds bounds, Quaternion rotation, Rect viewportArea)
        {
            var verticalExtent = Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var horizontalExtent = verticalExtent * viewCamera.aspect;
            var halfFieldOfView = Mathf.Min(Mathf.Atan(verticalExtent * viewportArea.height), Mathf.Atan(horizontalExtent * viewportArea.width));
            var distance = bounds.extents.magnitude / Mathf.Sin(halfFieldOfView);

            // Then move it from the view's center to the area's
            var areaOffset = (viewportArea.center * 2f) - Vector2.one;

            return bounds.center - rotation * Vector3.forward * distance
                   - rotation * Vector3.right * (areaOffset.x * horizontalExtent * distance)
                   - rotation * Vector3.up * (areaOffset.y * verticalExtent * distance);
        }

        private void FinishFraming()
        {
            transform.position = framingTargetPosition;
            SetOrthographicSize(framingTargetSize);
            isFraming = false;
        }

        // Straight down from above the level (where the camera is across it), as wide as the starting width
        private void BeginOrthographicTransition()
        {
            isFraming = false;
            currentVelocityVector = Vector3.zero;

            var halfWidth = OrthographicStartingWidth * world.WORLD_ONE / GeometryUtilities.WorldUnitIncrementsPerMeter * 0.5f;
            orthographicSize = ClampOrthographicSize(halfWidth / viewCamera.aspect);

            var position = transform.position;
            BeginTransition(new Vector3(position.x, LevelTop() + OrthographicClearance, position.z),
                            OrthographicRotation,
                            PerspectiveProjection(),
                            OrthographicProjection());
        }

        // Tilted down, framing what was last selected (or, if nothing has been, the highest polygon nearest to where the
        // camera is across the level)
        private void BeginPerspectiveTransition()
        {
            isFraming = false;
            currentVelocityVector = Vector3.zero;

            var rotation = Quaternion.Euler(PerspectiveReturnPitch, 0f, 0f);
            var position = transform.position;

            if (TryGetReturnTarget(out var target) && SelectionFramingBounds.TryGetBounds(target, out var bounds))
            {
                position = FramingPosition(bounds, rotation, new Rect(0f, 0f, 1f, 1f));
            }

            var startProjection = viewCamera.orthographic ? OrthographicProjection() : viewCamera.projectionMatrix;
            viewCamera.orthographic = false;
            BeginTransition(position, rotation, startProjection, PerspectiveProjection());
        }

        private void BeginTransition(Vector3 targetPosition, Quaternion targetRotation, Matrix4x4 startProjection, Matrix4x4 targetProjection)
        {
            // From where a transition that's still going has got to
            if (isTransitioning)
            {
                startProjection = viewCamera.projectionMatrix;
            }

            viewCamera.orthographic = false;
            viewCamera.projectionMatrix = startProjection;

            transitionStartTime = Time.time;
            transitionStartPosition = transform.position;
            transitionTargetPosition = targetPosition;
            transitionStartRotation = transform.rotation;
            transitionTargetRotation = targetRotation;
            transitionStartProjection = startProjection;
            transitionTargetProjection = targetProjection;
            isTransitioning = true;

            if (projectionTransitionDuration <= 0f)
            {
                FinishTransition();
            }
        }

        private void UpdateTransition()
        {
            var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - transitionStartTime) / projectionTransitionDuration));

            transform.SetPositionAndRotation(Vector3.Lerp(transitionStartPosition, transitionTargetPosition, progress),
                                             Quaternion.Slerp(transitionStartRotation, transitionTargetRotation, progress));

            var projection = new Matrix4x4();
            for (var i = 0; i < 16; i++)
            {
                projection[i] = Mathf.Lerp(transitionStartProjection[i], transitionTargetProjection[i], progress);
            }

            viewCamera.projectionMatrix = projection;

            if (progress >= 1f)
            {
                FinishTransition();
            }
        }

        // Truly orthographic (or perspective) once it's there
        private void FinishTransition()
        {
            transform.SetPositionAndRotation(transitionTargetPosition, transitionTargetRotation);
            viewCamera.ResetProjectionMatrix();
            viewCamera.orthographic = isOrthographic;
            viewCamera.orthographicSize = orthographicSize;
            isTransitioning = false;
        }

        private Matrix4x4 PerspectiveProjection()
        {
            return Matrix4x4.Perspective(viewCamera.fieldOfView, viewCamera.aspect, viewCamera.nearClipPlane, viewCamera.farClipPlane);
        }

        private Matrix4x4 OrthographicProjection()
        {
            var halfWidth = orthographicSize * viewCamera.aspect;
            return Matrix4x4.Ortho(-halfWidth, halfWidth, -orthographicSize, orthographicSize, viewCamera.nearClipPlane, viewCamera.farClipPlane);
        }

        private void SetOrthographicSize(float size)
        {
            orthographicSize = ClampOrthographicSize(size);

            if (viewCamera.orthographic)
            {
                viewCamera.orthographicSize = orthographicSize;
            }
        }

        private static float ClampOrthographicSize(float size)
        {
            return Mathf.Clamp(size, MinimumOrthographicSize, MaximumOrthographicSize);
        }

        // The top of everything in the level (its highest ceiling, or wherever a platform reaches)
        private static float LevelTop()
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return 0f;
            }

            var top = float.MinValue;
            foreach (var polygon in level.Level.PolygonList)
            {
                top = Mathf.Max(top, polygon.ceiling_height);
            }

            foreach (var platform in level.Level.PlatformList)
            {
                top = Mathf.Max(top, platform.maximum_ceiling_height);
            }

            return top == float.MinValue ? 0f : top / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }

        // What was last selected, or else the polygon under the camera (the highest, where several are) or nearest to it
        private bool TryGetReturnTarget(out ISelectable target)
        {
            return TryGetLastSelected(out target) || TryGetPolygonNearCamera(out target);
        }

        // What was last selected, or else the object nearest to the camera (or, with no objects, the nearest polygon)
        private bool TryGetFocusTarget(out ISelectable target)
        {
            return TryGetLastSelected(out target) || TryGetObjectNearCamera(out target) || TryGetPolygonNearCamera(out target);
        }

        // If it's still in the level
        private bool TryGetLastSelected(out ISelectable target)
        {
            var level = LevelEntity_Level.Instance;
            var isStillThere = level &&
                               (lastSelected is Object unityObject ? (bool) unityObject :
                                lastSelected is LevelEntity_Point point ? point.ParentLevel == level && point.NativeIndex < level.Level.EndpointList.Count :
                                lastSelected != null);

            target = isStillThere ? lastSelected : null;
            return isStillThere;
        }

        private bool TryGetObjectNearCamera(out ISelectable target)
        {
            target = null;

            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return false;
            }

            var position = transform.position;
            var nearestDistance = float.MaxValue;

            foreach (var mapObject in level.MapObjects.Values)
            {
                if (!mapObject)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(mapObject.transform.position - position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    target = mapObject;
                }
            }

            return target != null;
        }

        private bool TryGetPolygonNearCamera(out ISelectable target)
        {
            target = null;

            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return false;
            }

            var data = level.Level;
            var position = transform.position;
            var location = new world_point2d((short) Mathf.Clamp(Mathf.RoundToInt(position.x * GeometryUtilities.WorldUnitIncrementsPerMeter), short.MinValue, short.MaxValue),
                                             (short) Mathf.Clamp(Mathf.RoundToInt(-position.z * GeometryUtilities.WorldUnitIncrementsPerMeter), short.MinValue, short.MaxValue));

            short bestIndex = -1;
            var bestDistance = float.MaxValue;
            var bestFloor = int.MinValue;

            for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
            {
                var polygon = data.PolygonList[polygonIndex];
                if (map.POLYGON_IS_DETACHED(polygon) || !level.Polygons.ContainsKey(polygonIndex))
                {
                    continue;
                }

                var distance = map.point_in_polygon(data, polygonIndex, location) ? 0f :
                               Vector2.Distance(new Vector2(location.x, location.y), new Vector2(polygon.center.x, polygon.center.y));

                if (distance < bestDistance || (distance == bestDistance && polygon.floor_height > bestFloor))
                {
                    bestIndex = polygonIndex;
                    bestDistance = distance;
                    bestFloor = polygon.floor_height;
                }
            }

            if (bestIndex >= 0)
            {
                target = level.Polygons[bestIndex];
                return true;
            }

            return false;
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            UIBlocking.Instance.OnChanged += OnInputBlockerChanged;
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            SelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
        }

        private void Update()
        {
            // Not navigation, so it carries on while navigation is blocked
            if (isTransitioning)
            {
                UpdateTransition();

                // Nothing else moves the camera while it's going into (or out of) the orthographic view
                if (isTransitioning)
                {
                    return;
                }
            }

            var wheelNotches = GetWheelMoveInput();

            // While blocked (such as by the menu), only the wheel moves the camera
            if (IsNavigationBlocked)
            {
                MoveByWheel(wheelNotches);
                return;
            }

            UpdatePanning();

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
                    var easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                    transform.position = Vector3.Lerp(framingStartPosition, framingTargetPosition, easedProgress);
                    SetOrthographicSize(Mathf.Lerp(framingStartSize, framingTargetSize, easedProgress));

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

            // Looking down, advancing moves up the view (and moving across it is as fast on screen however far out it is)
            var advanceDirection = isOrthographic ? transform.up : transform.forward;
            var acrossScale = isOrthographic ? Mathf.Max(1f, orthographicSize / OrthographicSpeedReferenceSize) : 1f;

            var worldVelocityVector = (transform.right * (currentVelocityVector.x * acrossScale)) +
                                      (Vector3.up * currentVelocityVector.y) +
                                      (advanceDirection * (currentVelocityVector.z * acrossScale));

            var highestAxialVelocity = Mathf.Max(Mathf.Abs(worldVelocityVector.x), Mathf.Abs(worldVelocityVector.y), Mathf.Abs(worldVelocityVector.z));

            if (worldVelocityVector.sqrMagnitude > highestAxialVelocity * highestAxialVelocity)
            {
                worldVelocityVector = worldVelocityVector.normalized * highestAxialVelocity;
            }

            transform.position += worldVelocityVector * Time.deltaTime;
            #endregion Movement

            #region Looking
            // Looking down, the view's up stays the map's up
            if (!isOrthographic && Hotkeys.IsPressed(ForgePlusInput.Camera.EnableLook))
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
                   (!isOrthographic && Hotkeys.IsPressed(ForgePlusInput.Camera.EnableLook)) ||
                   isPanning ||
                   wheelNotches != 0f;
        }

        // After everything that moves it (including framing and the orthographic transition)
        private void LateUpdate()
        {
            if (TryGetLevelBounds(out var bounds))
            {
                var position = transform.position;
                var clamped = new Vector3(Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                                          Mathf.Clamp(position.y, bounds.min.y, bounds.max.y),
                                          Mathf.Clamp(position.z, bounds.min.z, bounds.max.z));

                if (clamped != position)
                {
                    transform.position = clamped;
                }
            }
        }

        // Across all its points, and from its lowest floor to above its highest ceiling (as high as the orthographic view
        // looks down from), including where its platforms reach
        private static bool TryGetLevelBounds(out Bounds bounds)
        {
            bounds = default;

            var level = LevelEntity_Level.Instance;
            if (!level || level.Level.EndpointList.Count == 0)
            {
                return false;
            }

            var data = level.Level;
            int minimumX = int.MaxValue, maximumX = int.MinValue, minimumY = int.MaxValue, maximumY = int.MinValue;
            foreach (var endpoint in data.EndpointList)
            {
                minimumX = Mathf.Min(minimumX, endpoint.vertex.x);
                maximumX = Mathf.Max(maximumX, endpoint.vertex.x);
                minimumY = Mathf.Min(minimumY, endpoint.vertex.y);
                maximumY = Mathf.Max(maximumY, endpoint.vertex.y);
            }

            int bottom = int.MaxValue, top = int.MinValue;
            foreach (var polygon in data.PolygonList)
            {
                bottom = Mathf.Min(bottom, polygon.floor_height);
                top = Mathf.Max(top, polygon.ceiling_height);
            }

            foreach (var platform in data.PlatformList)
            {
                bottom = Mathf.Min(bottom, platform.minimum_floor_height);
                top = Mathf.Max(top, platform.maximum_ceiling_height);
            }

            if (bottom > top)
            {
                bottom = top = 0;
            }

            var scale = GeometryUtilities.WorldUnitIncrementsPerMeter;
            var minimum = new Vector3(minimumX / scale, bottom / scale, -maximumY / scale);
            var maximum = new Vector3(maximumX / scale, top / scale + OrthographicClearance, -minimumY / scale);
            bounds.SetMinMax(minimum, maximum);

            return true;
        }

        // Begins while Pan Modifier is held as Select is pressed, or as Pan is pressed (not over the UI), and goes on while
        // either is still held
        private void UpdatePanning()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                isPanning = false;
                return;
            }

            var position = pointer.position.ReadValue();

            if (!isPanning)
            {
                var begins = (Hotkeys.IsPressed(ForgePlusInput.Camera.PanModifier) && ForgePlusInput.Editing.Select.WasPressedThisFrame()) ||
                             Hotkeys.WasPressed(ForgePlusInput.Camera.Pan);

                var ui = ForgePlusUI.Instance;
                if (begins && !(ui && ui.IsPointerOverUI(position)))
                {
                    BeginPan(position);
                }

                return;
            }

            var continues = (Hotkeys.IsPressed(ForgePlusInput.Camera.PanModifier) && ForgePlusInput.Editing.Select.IsPressed()) ||
                            ForgePlusInput.Camera.Pan.IsPressed();

            if (!continues)
            {
                isPanning = false;
                return;
            }

            // What was grabbed, back under the pointer
            if (TryIntersectHeight(viewCamera.ScreenPointToRay(position), panHeight, out var pointed))
            {
                var movement = panGrabPoint - pointed;
                movement.y = 0f;
                transform.position += movement;
            }
        }

        private void BeginPan(Vector2 screenPosition)
        {
            var ray = viewCamera.ScreenPointToRay(screenPosition);

            if (isOrthographic)
            {
                // Looking straight down, any height is the same
                panHeight = transform.position.y - 1f;
            }
            else if (Physics.Raycast(ray, out var hit, viewCamera.farClipPlane))
            {
                panHeight = hit.point.y;
            }
            else if (TryGetCornerNearRay(ray, out var corner))
            {
                panHeight = corner.y;
            }
            else
            {
                return;
            }

            if (TryIntersectHeight(ray, panHeight, out panGrabPoint))
            {
                isPanning = true;
                isFraming = false;
            }
        }

        // The level's floor or ceiling corner nearest to the line of sight (by the angle between them), favoring those
        // nearer to the camera
        private bool TryGetCornerNearRay(Ray ray, out Vector3 corner)
        {
            corner = default;

            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return false;
            }

            var data = level.Level;
            var bestScore = float.MaxValue;

            for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
            {
                var polygon = data.PolygonList[polygonIndex];
                if (map.POLYGON_IS_DETACHED(polygon))
                {
                    continue;
                }

                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var vertex = GeometryUtilities.GetMeshVertex(data, polygon.endpoint_indexes[i]);

                    foreach (var height in new[] { polygon.floor_height, polygon.ceiling_height })
                    {
                        vertex.y = height / GeometryUtilities.WorldUnitIncrementsPerMeter;

                        var along = Vector3.Dot(vertex - ray.origin, ray.direction);
                        if (along <= viewCamera.nearClipPlane)
                        {
                            continue;
                        }

                        var offset = Vector3.Distance(vertex, ray.origin + ray.direction * along);
                        var score = offset / along * (1f + along / PanCornerDistanceBias);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            corner = vertex;
                        }
                    }
                }
            }

            return bestScore < float.MaxValue;
        }

        // Where the ray crosses the height, ahead of where it starts
        private static bool TryIntersectHeight(Ray ray, float height, out Vector3 point)
        {
            point = default;

            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            var distance = (height - ray.origin.y) / ray.direction.y;
            if (distance <= 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * distance;
            return true;
        }

        // Looking down, the wheel zooms instead
        private void MoveByWheel(float wheelNotches)
        {
            if (wheelNotches == 0f || isTransitioning)
            {
                return;
            }

            if (isOrthographic)
            {
                SetOrthographicSize(orthographicSize * Mathf.Pow(wheelZoomFactorPerNotch, wheelNotches));
                return;
            }

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
