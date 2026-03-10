using System;
using MelonLoader;
using HarmonyLib;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Math;
using CameraUnlock.Core.Aim;
using CameraUnlock.Core.Input;
using CameraUnlock.Core.Unity.Tracking;
using Vec3 = CameraUnlock.Core.Data.Vec3;
using GreenHellHeadTracking.Patches;
using UnityEngine;

[assembly: MelonInfo(typeof(GreenHellHeadTracking.Mod), "Green Hell Head Tracking", "1.0.1", "itsloopyo")]
[assembly: MelonGame("Creepy Jar", "Green Hell")]

namespace GreenHellHeadTracking
{
    public class Mod : MelonMod, IHotkeyListener
    {
        private static OpenTrackReceiver? _receiver;
        private static TrackingProcessor? _processor;
        private static PoseInterpolator? _poseInterpolator;
        private static HotkeyHandler? _hotkeyHandler;
        private static TrackingLossHandler? _trackingLossHandler;
        private static BaseRotationTracker? _baseRotationTracker;
        private static GreenHellGameStateDetector? _gameStateDetector;

        private static bool _trackingEnabled = true;
        private static Transform? _cachedCameraTransform;
        private static Camera? _cachedCamera;

        // Always-on rotation smoothing: 0.15 ≈ 70ms settling (frame-rate independent).
        // Ensures silky output at high refresh rates even with a slow tracker.
        private const float RotationSmoothing = 0.15f;

        private static Quaternion _smoothedTrackingRotation = Quaternion.identity;
        private static bool _hasSmoothingState;
        private static Vector2 _smoothedScreenOffset = Vector2.zero;

        private static float _cachedHFov;
        private static float _lastVFov;
        private static float _lastAspect;

        private static PositionProcessor? _positionProcessor;
        private static PositionInterpolator? _positionInterpolator;
        private static bool _positionEnabled = true;
        private static bool _autoRecentered;
        private static bool _positionCentered;
        private static bool _hasCentered;

        private static Vector3 _pendingPositionOffset;
        private static bool _hasRenderData;

        private static Mod? _instance;

        public static bool IsTrackingActive => _trackingEnabled &&
                                               _receiver != null &&
                                               _receiver.IsReceiving &&
                                               (_gameStateDetector == null || _gameStateDetector.IsInGameplay);

        public static Vector3 AimDirection => _baseRotationTracker?.BaseForward ?? Vector3.forward;

        public override void OnInitializeMelon()
        {
            _instance = this;
            LoggerInstance.Msg("Green Hell Head Tracking initializing...");

            _receiver = new OpenTrackReceiver();
            _processor = new TrackingProcessor();
            _poseInterpolator = new PoseInterpolator();

            _processor.Sensitivity = new SensitivitySettings(1f, 1f, 1f, false, true, false);
            _processor.SmoothingFactor = 0f;

            _trackingLossHandler = new TrackingLossHandler();
            _baseRotationTracker = new BaseRotationTracker();

            _positionProcessor = new PositionProcessor
            {
                Settings = new PositionSettings(
                    1.0f, 1.0f, 1.0f,
                    100f, 100f, 100f,
                    0.15f,
                    invertX: false, invertY: false, invertZ: false
                ),
                NeckModelSettings = NeckModelSettings.Disabled,
                TrackerPivotForward = 0.01f
            };
            _positionInterpolator = new PositionInterpolator();

            _gameStateDetector = new GreenHellGameStateDetector(() => Time.time);

            _hotkeyHandler = new HotkeyHandler(
                keyCode => UnityEngine.Input.GetKeyDown((KeyCode)keyCode),
                null,
                this,
                0.3f
            );
            _hotkeyHandler.SetRecenterKey((int)KeyCode.Home);
            _hotkeyHandler.SetToggleKey((int)KeyCode.End);

            if (!_receiver.Start(OpenTrackReceiver.DefaultPort))
            {
                LoggerInstance.Error("Failed to start OpenTrack receiver - port may be in use");
                return;
            }

            ApplyCameraPatches();
            ApplyTriggerPatches();

            LoggerInstance.Msg("Green Hell Head Tracking initialized on port " + OpenTrackReceiver.DefaultPort);
        }

        private void ApplyCameraPatches()
        {
            try
            {
                var cameraManagerType = Type.GetType("CameraManager, Assembly-CSharp");
                if (cameraManagerType == null)
                {
                    LoggerInstance.Error("CameraManager type not found - head tracking will NOT work!");
                    return;
                }

                var lateUpdateMethod = AccessTools.Method(cameraManagerType, "LateUpdate");
                if (lateUpdateMethod == null)
                {
                    LoggerInstance.Error("CameraManager.LateUpdate not found - head tracking will NOT work!");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(CameraManagerPatch), nameof(CameraManagerPatch.LateUpdatePrefix));
                var postfix = new HarmonyMethod(typeof(CameraManagerPatch), nameof(CameraManagerPatch.LateUpdatePostfix));
                HarmonyInstance.Patch(lateUpdateMethod, prefix: prefix, postfix: postfix);

                LoggerInstance.Msg("Patched CameraManager.LateUpdate (prefix+postfix) - head tracking active");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to apply CameraManager patches: " + ex);
            }
        }

        private void ApplyTriggerPatches()
        {
            try
            {
                var triggerControllerType = Type.GetType("TriggerController, Assembly-CSharp");
                if (triggerControllerType == null)
                {
                    LoggerInstance.Warning("TriggerController type not found - aim decoupling disabled");
                    return;
                }

                var getCrossHairDirMethod = AccessTools.Method(triggerControllerType, "GetCrossHairDir");
                if (getCrossHairDirMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(TriggerControllerPatch), nameof(TriggerControllerPatch.GetCrossHairDirPostfix));
                    HarmonyInstance.Patch(getCrossHairDirMethod, postfix: postfix);
                    LoggerInstance.Msg("Patched TriggerController.GetCrossHairDir - aim decoupling active");
                }
                else
                {
                    LoggerInstance.Warning("TriggerController.GetCrossHairDir not found");
                }

                var updateBestTriggerMethod = AccessTools.Method(triggerControllerType, "UpdateBestTrigger");
                if (updateBestTriggerMethod != null)
                {
                    var transpiler = new HarmonyMethod(typeof(TriggerControllerPatch), nameof(TriggerControllerPatch.UpdateBestTriggerTranspiler));
                    HarmonyInstance.Patch(updateBestTriggerMethod, transpiler: transpiler);
                    LoggerInstance.Msg("Patched TriggerController.UpdateBestTrigger (transpiler) - dot product fix active");
                }
                else
                {
                    LoggerInstance.Warning("TriggerController.UpdateBestTrigger not found");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to apply TriggerController patches: " + ex);
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (_cachedCamera != null) _cachedCamera.ResetWorldToCameraMatrix();
            if (_receiver != null) _receiver.Dispose();
            if (_gameStateDetector != null) _gameStateDetector.Dispose();
        }

        public override void OnUpdate()
        {
            _hotkeyHandler?.Update(Time.time);

            if (UnityEngine.Input.GetKeyDown(KeyCode.PageUp))
            {
                _positionEnabled = !_positionEnabled;
                if (!_positionEnabled)
                {
                    _positionProcessor?.ResetSmoothing();
                    _positionInterpolator?.Reset();
                }
                _instance?.LoggerInstance.Msg("Position tracking " + (_positionEnabled ? "enabled" : "disabled"));
            }

        }

        public void OnHotkeyToggle(bool enabled)
        {
            _trackingEnabled = enabled;
            if (!_trackingEnabled)
            {
                ResetTrackingState();
            }
            _instance?.LoggerInstance.Msg("Head tracking " + (_trackingEnabled ? "enabled" : "disabled"));
        }

        public void OnHotkeyRecenter()
        {
            _processor?.Recenter();
            _trackingLossHandler?.TriggerStabilization();
            if (_receiver != null)
            {
                _positionProcessor?.SetCenter(_receiver.GetLatestPosition());
            }
            _poseInterpolator?.Reset();
            _positionInterpolator?.Reset();
            _instance?.LoggerInstance.Msg("Head tracking recentered");
        }

        internal static void RemoveTrackingOffset()
        {
            if (_cachedCamera != null && _hasRenderData)
            {
                _cachedCamera.ResetWorldToCameraMatrix();
                _hasRenderData = false;
                CrosshairMover.ResetCrosshair();
            }
        }

        internal static void ResetTrackingState()
        {
            RemoveTrackingOffset();
            _hasRenderData = false;
            _pendingPositionOffset = Vector3.zero;
            _hasSmoothingState = false;
            _positionCentered = false;
            _positionProcessor?.Reset();
            _poseInterpolator?.Reset();
            _positionInterpolator?.Reset();
        }

        internal static void ApplyHeadTracking()
        {
            if (_receiver == null || _processor == null || _trackingLossHandler == null)
            {
                return;
            }

            // Cache Camera.main — avoids per-frame FindObjectWithTag.
            // Unity's overloaded == returns true for destroyed objects, triggering re-query.
            if (_cachedCamera == null)
            {
                _cachedCamera = Camera.main;
                if (_cachedCamera == null) return;
                _cachedCameraTransform = _cachedCamera.transform;
            }

            float deltaTime = Time.deltaTime;

            bool shouldTrack = _trackingEnabled &&
                              (_gameStateDetector == null || _gameStateDetector.IsInGameplay);

            // Only actual signal loss feeds the loss handler. Gameplay pauses
            // (walkie talkie, menus) are NOT signal loss and must not trigger
            // auto-recenter or fade — the prefix already removed the visual
            // offset, so just return early.
            var lossState = _trackingLossHandler.Update(_receiver.IsReceiving, deltaTime);

            if (!shouldTrack)
            {
                _hasRenderData = false;
                _pendingPositionOffset = Vector3.zero;
                return;
            }

            switch (lossState)
            {
                case TrackingLossState.Active:
                    _autoRecentered = false;
                    ApplyActiveTracking(deltaTime);
                    break;
                case TrackingLossState.Holding:
                    ReapplyLastRotation();
                    break;
                case TrackingLossState.Fading:
                case TrackingLossState.Stabilizing:
                    ApplyFadingTracking(deltaTime);
                    break;
            }

            if (_trackingLossHandler.NeedsRecenter && !_autoRecentered)
            {
                _autoRecentered = true;
                _processor.Recenter();
                _trackingLossHandler.ClearRecenterFlag();
                if (_receiver != null)
                {
                    _positionProcessor?.SetCenter(_receiver.GetLatestPosition());
                }
                _positionInterpolator?.Reset();
                _instance?.LoggerInstance.Msg("Auto-recentered after tracking loss");
            }

            // Apply tracking offset via the view matrix instead of Camera.onPreCull
            // (Camera events throw MissingMethodException under MelonLoader).
            // Unity camera space uses OpenGL convention (forward = -Z), so we
            // negate the third row of the inverted TRS to flip the Z axis.
            if (_hasRenderData && _cachedCamera != null && _cachedCameraTransform != null)
            {
                Quaternion baseRotation = _cachedCameraTransform.rotation;
                Quaternion modifiedRot;

                {
                    // Spherical yaw: yaw and pitch rotate within the camera's local
                    // basis (like DL2), keeping the horizon stable without locking to world-up.
                    Vector3 origFwd = baseRotation * Vector3.forward;
                    Vector3 origUp = baseRotation * Vector3.up;
                    Vector3 origRight = baseRotation * Vector3.right;

                    var euler = _smoothedTrackingRotation.eulerAngles;
                    float yawRad = (euler.y > 180f ? euler.y - 360f : euler.y) * Mathf.Deg2Rad;
                    float pitchRad = (euler.x > 180f ? euler.x - 360f : euler.x) * Mathf.Deg2Rad;
                    float rollRad = (euler.z > 180f ? euler.z - 360f : euler.z) * Mathf.Deg2Rad;

                    float cosY = Mathf.Cos(yawRad), sinY = Mathf.Sin(yawRad);
                    float cosP = Mathf.Cos(pitchRad), sinP = Mathf.Sin(pitchRad);

                    Vector3 fwd = cosP * cosY * origFwd + cosP * sinY * origRight - sinP * origUp;

                    float dot = Vector3.Dot(fwd, origUp);
                    Vector3 up = (origUp - fwd * dot).normalized;

                    if (Mathf.Abs(rollRad) > 0.0001f)
                    {
                        up = Quaternion.AngleAxis(rollRad * Mathf.Rad2Deg, fwd) * up;
                    }

                    modifiedRot = Quaternion.LookRotation(fwd, up);
                }
                Transform? parent = _cachedCameraTransform.parent;
                Vector3 worldPosOffset = parent != null
                    ? parent.TransformDirection(_pendingPositionOffset)
                    : _pendingPositionOffset;
                Vector3 modifiedPos = _cachedCameraTransform.position + worldPosOffset;
                Matrix4x4 viewMatrix = Matrix4x4.TRS(modifiedPos, modifiedRot, Vector3.one).inverse;
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;
                _cachedCamera.worldToCameraMatrix = viewMatrix;
            }
        }

        private static void ApplyActiveTracking(float deltaTime)
        {
            if (_receiver == null || _processor == null || _poseInterpolator == null || _cachedCameraTransform == null || _cachedCamera == null)
            {
                throw new InvalidOperationException("ApplyActiveTracking called without required components initialized");
            }

            var rawPose = _receiver.GetLatestPose();

            // Auto-recenter on first valid tracking frame
            if (!_hasCentered)
            {
                _hasCentered = true;
                _processor.RecenterTo(rawPose);
                _positionProcessor?.SetCenter(_receiver.GetLatestPosition());
                _poseInterpolator.Reset();
                _positionInterpolator?.Reset();
            }

            // Velocity extrapolation between tracker samples — fills in frames
            // so a 30Hz tracker looks smooth on a 240Hz display.
            var interpolatedPose = _poseInterpolator.Update(rawPose, deltaTime);
            var processed = _processor.Process(interpolatedPose, _receiver.IsRemoteConnection, deltaTime);

            var yawQ = Quaternion.AngleAxis(processed.Yaw, Vector3.up);
            var pitchQ = Quaternion.AngleAxis(processed.Pitch, Vector3.right);
            var rollQ = Quaternion.AngleAxis(processed.Roll, Vector3.forward);
            var rawOffset = yawQ * pitchQ * rollQ;

            float smoothing = SmoothingUtils.GetEffectiveSmoothing(RotationSmoothing, _receiver.IsRemoteConnection);

            if (!_hasSmoothingState)
            {
                _smoothedTrackingRotation = rawOffset;
                _hasSmoothingState = true;
            }
            else
            {
                float t = SmoothingUtils.CalculateSmoothingFactor(smoothing, deltaTime);
                _smoothedTrackingRotation = t >= 1f ? rawOffset : Quaternion.Slerp(_smoothedTrackingRotation, rawOffset, t);
            }

            var gameRotation = _cachedCameraTransform.localRotation;

            _baseRotationTracker?.Update(_cachedCameraTransform, gameRotation, _smoothedTrackingRotation);

            // Position processing: tracker position + neck model
            _pendingPositionOffset = Vector3.zero;
            if (_positionEnabled && _receiver != null && _positionProcessor != null && _positionInterpolator != null)
            {
                var rawPos = _receiver.GetLatestPosition();
                if (!_positionCentered)
                {
                    _positionProcessor.SetCenter(rawPos);
                    _positionCentered = true;
                }
                var interpolatedPos = _positionInterpolator.Update(rawPos, deltaTime);
                var euler = _smoothedTrackingRotation.eulerAngles;
                float yaw = euler.y > 180f ? euler.y - 360f : euler.y;
                float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
                float roll = euler.z > 180f ? euler.z - 360f : euler.z;
                var headRotQ = QuaternionUtils.FromYawPitchRoll(yaw, pitch, roll);
                Vec3 posOffset = _positionProcessor.Process(interpolatedPos, headRotQ, _receiver.IsRemoteConnection, deltaTime);
                posOffset = new Vec3(-posOffset.X, posOffset.Y, -posOffset.Z);
                var posVec = new Vector3(posOffset.X, posOffset.Y, posOffset.Z);
                // Rotate position offset by yaw only so forward/back movement
                // stays on the horizon regardless of camera pitch.
                var yawOnly = Quaternion.Euler(0f, gameRotation.eulerAngles.y, 0f);
                posVec = yawOnly * posVec;
                _pendingPositionOffset = posVec;
            }

            UpdateCrosshairFromRotation(_smoothedTrackingRotation);
            _hasRenderData = true;
        }

        private static void ReapplyLastRotation()
        {
            if (_cachedCameraTransform == null)
            {
                throw new InvalidOperationException("ReapplyLastRotation called without cached camera");
            }

            if (_hasSmoothingState)
            {
                _hasRenderData = true;

                CrosshairMover.OffsetCrosshair(_smoothedScreenOffset);
            }
        }

        private static void ApplyFadingTracking(float deltaTime)
        {
            if (_cachedCameraTransform == null || _trackingLossHandler == null)
            {
                throw new InvalidOperationException("ApplyFadingTracking called without required components initialized");
            }

            _smoothedTrackingRotation = _trackingLossHandler.ApplyFade(_smoothedTrackingRotation, deltaTime);

            _hasRenderData = true;

            UpdateCrosshairFromRotation(_smoothedTrackingRotation);

            // Camera is always clean (tracking only applied during rendering),
            // so localRotation IS the game rotation.
            var gameRotation = _cachedCameraTransform.localRotation;
            _baseRotationTracker?.Update(_cachedCameraTransform, gameRotation, _smoothedTrackingRotation);
        }

        private static void UpdateCrosshairFromRotation(Quaternion trackingRotation)
        {
            if (_cachedCamera == null)
            {
                throw new InvalidOperationException("UpdateCrosshairFromRotation called without cached camera");
            }

            // Decompose the smoothed tracking rotation back to Euler angles.
            // Our composition is YXZ (yaw*pitch*roll), matching Unity's eulerAngles convention.
            var euler = trackingRotation.eulerAngles;
            float yaw = euler.y > 180f ? euler.y - 360f : euler.y;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            float roll = euler.z > 180f ? euler.z - 360f : euler.z;

            float vFov = _cachedCamera.fieldOfView;
            float aspect = _cachedCamera.aspect;

            // Cache hFov — CalculateHorizontalFov does Tan+Atan, but FOV/aspect rarely change.
            if (vFov != _lastVFov || aspect != _lastAspect)
            {
                _lastVFov = vFov;
                _lastAspect = aspect;
                _cachedHFov = ScreenOffsetCalculator.CalculateHorizontalFov(vFov, aspect);
            }

            ScreenOffsetCalculator.Calculate(
                yaw, pitch, roll,
                _cachedHFov, vFov,
                Screen.width, Screen.height,
                1.0f,
                out float offsetX, out float offsetY
            );

            _smoothedScreenOffset = new Vector2(offsetX, offsetY);
            CrosshairMover.OffsetCrosshair(_smoothedScreenOffset);
        }

    }

}
