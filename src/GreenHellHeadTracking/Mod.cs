using System;
using MelonLoader;
using HarmonyLib;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Math;
using CameraUnlock.Core.Input;
using CameraUnlock.Core.Unity.Tracking;
using CameraUnlock.Core.Unity.Extensions;
using Vec3 = CameraUnlock.Core.Data.Vec3;
using GreenHellHeadTracking.Patches;
using UnityEngine;

[assembly: MelonInfo(typeof(GreenHellHeadTracking.Mod), "Green Hell Head Tracking", "1.0.3", "itsloopyo")]
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
        private static Camera? _cachedOutlineCamera;

        // Always-on rotation smoothing: 0.15 ≈ 70ms settling (frame-rate independent).
        // Ensures silky output at high refresh rates even with a slow tracker.
        private const float RotationSmoothing = 0.15f;

        private const float MaxRaycastDistance = 1000f;
        private const float MinRaycastDistance = 0.5f;
        private const float DistanceSmoothingRate = 15f;
        private static float _lastHitDistance = 100f;

        // Processor handles all rotation smoothing internally (per-axis Euler, no phantom roll).
        // Smoothed tracking rotation for view matrix composition and aim offset
        private static Quaternion _smoothedTrackingRotation = Quaternion.identity;
        private static Vector2 _smoothedScreenOffset = Vector2.zero;

        private static PositionProcessor? _positionProcessor;
        private static PositionInterpolator? _positionInterpolator;
        private static bool _positionEnabled = true;
        private static bool _worldSpaceYaw;
        private const float PositionLimitYUp = 0.15f;
        private const float PositionLimitYDown = 0.05f;
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
                    0.30f, PositionLimitYUp, 0.40f, 0.10f,
                    0.15f,
                    false, false, false
                ),
                TrackerPivotForward = 0.01f
            };
            _positionInterpolator = new PositionInterpolator();

            _gameStateDetector = new GreenHellGameStateDetector(() => Time.time);

            // Nav-cluster keys + Ctrl+Shift+<letter> chord alternatives from the
            // T/Y/U/G/H/J cluster, so users on tenkeyless/60% boards still have
            // hotkeys. Letter choice per CLAUDE.md's shared convention.
            _hotkeyHandler = new HotkeyHandler(
                keyCode =>
                {
                    var kc = (KeyCode)keyCode;
                    if (UnityEngine.Input.GetKeyDown(kc)) return true;
                    if (kc == KeyCode.Home && ChordDown(KeyCode.T)) return true;
                    if (kc == KeyCode.End && ChordDown(KeyCode.Y)) return true;
                    return false;
                },
                null,
                this,
                0.3f
            );
            _hotkeyHandler.SetRecenterKey((int)KeyCode.Home);
            _hotkeyHandler.SetToggleKey((int)KeyCode.End);

            _receiver.Log = msg => LoggerInstance.Msg(msg);
            _receiver.Start(OpenTrackReceiver.DefaultPort);

            ApplyCameraPatches();
            ApplyHUDPatches();
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

        private void ApplyHUDPatches()
        {
            try
            {
                var hudManagerType = Type.GetType("HUDManager, Assembly-CSharp");
                if (hudManagerType == null)
                {
                    LoggerInstance.Warning("HUDManager type not found - HUD marker compensation disabled");
                    return;
                }

                var updateAfterCamera = AccessTools.Method(hudManagerType, "UpdateAfterCamera");
                if (updateAfterCamera == null)
                {
                    LoggerInstance.Warning("HUDManager.UpdateAfterCamera not found - HUD marker compensation disabled");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(HUDManagerPatch), nameof(HUDManagerPatch.UpdateAfterCameraPrefix));
                var postfix = new HarmonyMethod(typeof(HUDManagerPatch), nameof(HUDManagerPatch.UpdateAfterCameraPostfix));
                HarmonyInstance.Patch(updateAfterCamera, prefix: prefix, postfix: postfix);

                LoggerInstance.Msg("Patched HUDManager.UpdateAfterCamera (prefix+postfix) - HUD marker compensation active");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to apply HUDManager patches: " + ex);
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
            if (_cachedOutlineCamera != null) _cachedOutlineCamera.ResetWorldToCameraMatrix();
            if (_receiver != null) _receiver.Dispose();
            if (_gameStateDetector != null) _gameStateDetector.Dispose();
        }

        public override void OnUpdate()
        {
            _hotkeyHandler?.Update(Time.time);

            if (UnityEngine.Input.GetKeyDown(KeyCode.PageUp) || ChordDown(KeyCode.G))
            {
                _positionEnabled = !_positionEnabled;
                if (!_positionEnabled)
                {
                    _positionProcessor?.ResetSmoothing();
                    _positionInterpolator?.Reset();
                }
                _instance?.LoggerInstance.Msg("Position tracking " + (_positionEnabled ? "enabled" : "disabled"));
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.PageDown) || ChordDown(KeyCode.H))
            {
                _worldSpaceYaw = !_worldSpaceYaw;
                _instance?.LoggerInstance.Msg("Yaw mode: " + (_worldSpaceYaw ? "world-space (horizon-locked)" : "camera-local"));
            }
        }

        private static bool ChordDown(KeyCode letter)
        {
            bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
            if (!ctrl) return false;
            bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            if (!shift) return false;
            return UnityEngine.Input.GetKeyDown(letter);
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
                if (_cachedOutlineCamera != null)
                    _cachedOutlineCamera.ResetWorldToCameraMatrix();
                _hasRenderData = false;
                CrosshairMover.ResetCrosshair();
            }
        }

        internal static void ResetTrackingState()
        {
            RemoveTrackingOffset();
            _hasRenderData = false;
            _pendingPositionOffset = Vector3.zero;
            _smoothedTrackingRotation = Quaternion.identity;
            _processor?.ResetSmoothing();
            _positionCentered = false;
            _hasCentered = false;
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

                // The outline camera is a child of the main camera. It renders
                // interactable objects with a replacement shader for the outline
                // effect. We must apply the same view matrix so outlines track.
                foreach (var cam in _cachedCamera.GetComponentsInChildren<Camera>(true))
                {
                    if (cam.name == "OutlineCamera")
                    {
                        _cachedOutlineCamera = cam;
                        break;
                    }
                }
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
            if (_hasRenderData && _cachedCamera != null && _cachedCameraTransform != null)
            {
                SetHeadTrackedViewMatrix();

                // Reticle compensation: raycast along the base aim direction to find
                // the target distance, then project through the head-tracked view matrix.
                Vector3 aimDir = _cachedCameraTransform.forward;

                RaycastHit hit;
                if (Physics.Raycast(_cachedCameraTransform.position, aimDir, out hit, MaxRaycastDistance,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && hit.distance >= MinRaycastDistance)
                {
                    float t = 1f - Mathf.Exp(-DistanceSmoothingRate * Time.deltaTime);
                    _lastHitDistance = Mathf.Lerp(_lastHitDistance, hit.distance, t);
                }

                _smoothedScreenOffset = CanvasCompensation.CalculateAimScreenOffset(_cachedCamera, aimDir, _lastHitDistance, 1f);
                CrosshairMover.OffsetCrosshair(_smoothedScreenOffset);
            }
        }

        /// <summary>
        /// Computes and sets the head-tracked worldToCameraMatrix on _cachedCamera.
        /// Head tracking is applied in the camera's local frame so that yaw always
        /// pans left/right on screen, even at steep pitch angles.
        /// </summary>
        private static void SetHeadTrackedViewMatrix()
        {
            var euler = _smoothedTrackingRotation.eulerAngles;
            float trackYaw = euler.y > 180f ? euler.y - 360f : euler.y;
            float trackPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            float trackRoll = euler.z > 180f ? euler.z - 360f : euler.z;

            // Default camera-local: yaw always pans view horizontally regardless
            // of game pitch. World-space (PGDN toggle) locks yaw to world up so
            // the horizon stays level, at the cost of degenerating into roll
            // near +/-90 pitch.
            Quaternion modifiedRot;
            if (_worldSpaceYaw)
            {
                Quaternion worldYaw = Quaternion.AngleAxis(trackYaw, Vector3.up);
                Quaternion localPR = Quaternion.Euler(trackPitch, 0f, trackRoll);
                modifiedRot = worldYaw * _cachedCameraTransform!.rotation * localPR;
            }
            else
            {
                Quaternion headLocal = Quaternion.Euler(trackPitch, trackYaw, trackRoll);
                modifiedRot = _cachedCameraTransform!.rotation * headLocal;
            }
            Vector3 modifiedPos = _cachedCameraTransform.position + _pendingPositionOffset;

            Matrix4x4 viewMatrix = Matrix4x4.TRS(modifiedPos, modifiedRot, Vector3.one).inverse;
            viewMatrix.m20 = -viewMatrix.m20;
            viewMatrix.m21 = -viewMatrix.m21;
            viewMatrix.m22 = -viewMatrix.m22;
            viewMatrix.m23 = -viewMatrix.m23;
            _cachedCamera!.worldToCameraMatrix = viewMatrix;

            // Outline camera is a child of the main camera — it inherits the
            // transform but not the overridden worldToCameraMatrix. Apply the
            // same matrix so the outline renders from the tracked viewpoint.
            if (_cachedOutlineCamera != null)
                _cachedOutlineCamera.worldToCameraMatrix = viewMatrix;
        }

        /// <summary>
        /// Temporarily applies the head-tracked view matrix so that WorldToScreenPoint
        /// calls (e.g. in HUDManager.UpdateAfterCamera) project to the correct positions.
        /// Uses cached tracking data from the previous frame.
        /// </summary>
        internal static void ApplyTrackingToViewMatrix()
        {
            if (!IsTrackingActive) return;
            if (_cachedCamera == null || _cachedCameraTransform == null) return;
            if (_smoothedTrackingRotation == Quaternion.identity && _pendingPositionOffset == Vector3.zero) return;
            SetHeadTrackedViewMatrix();
        }

        internal static void ResetViewMatrix()
        {
            if (_cachedCamera != null)
                _cachedCamera.ResetWorldToCameraMatrix();
            if (_cachedOutlineCamera != null)
                _cachedOutlineCamera.ResetWorldToCameraMatrix();
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
                _instance?.LoggerInstance.Msg("Recentered to initial head position");
            }

            // Always update interpolator to maintain velocity state
            var interpolatedPose = _poseInterpolator.Update(rawPose, deltaTime);

            // Use interpolated pose only when smoothing absorbs prediction corrections;
            // at smoothing=0, interpolation creates visible correction stutters
            if (_processor.SmoothingFactor >= 0.001f)
                rawPose = interpolatedPose;

            var processed = _processor.Process(rawPose, deltaTime);

            // Processor handles smoothing internally (per-axis Euler, baseline floor).
            // Use its output directly — no second smoothing layer.
            _smoothedTrackingRotation = CameraRotationComposer.GetTrackingOnlyRotation(
                processed.Yaw, processed.Pitch, processed.Roll);

            var gameRotation = _cachedCameraTransform.localRotation;

            _baseRotationTracker?.Update(_cachedCameraTransform, gameRotation, _smoothedTrackingRotation);

            // Position processing: tracker position
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
                float eYaw = euler.y > 180f ? euler.y - 360f : euler.y;
                float ePitch = euler.x > 180f ? euler.x - 360f : euler.x;
                float eRoll = euler.z > 180f ? euler.z - 360f : euler.z;
                var headRotQ = QuaternionUtils.FromYawPitchRoll(eYaw, ePitch, eRoll);
                Vec3 posOffset = _positionProcessor.Process(interpolatedPos, headRotQ, deltaTime);
                // Asymmetric Y clamp: prevent camera going below eye height
                float clampedY = Mathf.Clamp(posOffset.Y, -PositionLimitYDown, PositionLimitYUp);
                posOffset = new Vec3(posOffset.X, clampedY, posOffset.Z);
                // Negate X and Z to match Green Hell's coordinate convention
                posOffset = new Vec3(-posOffset.X, posOffset.Y, -posOffset.Z);
                _pendingPositionOffset = PositionApplicator.ToHorizonLockedWorld(
                    posOffset, _cachedCameraTransform.rotation);
            }

            _hasRenderData = true;
        }

        private static void ReapplyLastRotation()
        {
            if (_cachedCameraTransform == null)
            {
                throw new InvalidOperationException("ReapplyLastRotation called without cached camera");
            }

            if (_smoothedTrackingRotation != Quaternion.identity)
            {
                _hasRenderData = true;
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

            // Camera is always clean (tracking only applied during rendering),
            // so localRotation IS the game rotation.
            var gameRotation = _cachedCameraTransform.localRotation;
            _baseRotationTracker?.Update(_cachedCameraTransform, gameRotation, _smoothedTrackingRotation);
        }

    }

}
