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

[assembly: MelonInfo(typeof(GreenHellHeadTracking.Mod), "Green Hell Head Tracking", "1.3.0", "itsloopyo")]
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
        private static GreenHellGameStateDetector? _gameStateDetector;

        private static bool _trackingEnabled = true;
        private static Transform? _cachedCameraTransform;
        private static Camera? _cachedCamera;
        private static Camera? _cachedOutlineCamera;

        private const float MaxRaycastDistance = 1000f;
        private const float MinRaycastDistance = 0.5f;
        private static float _aimDepth = MaxRaycastDistance;

        // Below these the faded pose is under a hundredth of a degree and a tenth of a
        // millimetre, so it is dropped and the camera goes back to the game's own matrix.
        private const float FadedRotationSqr = 1e-8f;
        private const float FadedPositionSqr = 1e-8f;

        // Processor handles all rotation smoothing internally (per-axis Euler, no phantom roll).
        private static Quaternion _smoothedTrackingRotation = Quaternion.identity;

        private static PositionProcessor? _positionProcessor;
        private static PositionInterpolator? _positionInterpolator;
        private static bool _positionEnabled = true;
        private static bool _rotationEnabled = true;
        private static bool _worldSpaceYaw;
        private const float PositionLimitYUp = 0.15f;
        private const float PositionLimitYDown = 0.05f;

        // Processor space with Green Hell's x sign applied. Kept relative to the body and
        // turned into world space when the matrix is built, so a held or fading lean
        // follows the player as they turn.
        private static Vec3 _positionOffset;

        // True while the camera should render with the tracked view matrix this frame.
        private static bool _hasRenderData;

        private static Mod? _instance;
        private static bool _wasReceiving;

        public override void OnInitializeMelon()
        {
            _instance = this;
            LoggerInstance.Msg("Green Hell Head Tracking initializing...");

            _receiver = new OpenTrackReceiver();
            _processor = new TrackingProcessor();
            _poseInterpolator = new PoseInterpolator();

            _processor.Sensitivity = new SensitivitySettings(1f, 1f, 1f, false, true, false);
            // Two smoothing parameters, selected per connection by source address.
            // This mod ships no config file, so both take the core defaults.
            _processor.LocalSmoothing = SmoothingUtils.DefaultLocalSmoothing;
            _processor.RemoteSmoothing = SmoothingUtils.DefaultRemoteSmoothing;

            _trackingLossHandler = new TrackingLossHandler();

            _positionProcessor = new PositionProcessor
            {
                // 0.40 forward / 0.10 back is the intended asymmetry on z, not a swap.
                Settings = new PositionSettings(
                    1.0f, 1.0f, 1.0f,
                    PositionSettings.Default.LimitX, PositionLimitYUp, PositionLimitYDown,
                    PositionSettings.Default.LimitZ, PositionSettings.Default.LimitZBack,
                    SmoothingUtils.DefaultLocalSmoothing, SmoothingUtils.DefaultRemoteSmoothing,
                    false, false, false
                ),
                TrackerPivotForward = 0.01f
            };
            _positionInterpolator = new PositionInterpolator();

            // Unscaled: the pause menu sets Time.timeScale to 0, which freezes Time.time and
            // would hold the detector's throttled answer for as long as the game is paused.
            _gameStateDetector = new GreenHellGameStateDetector(() => Time.unscaledTime);

            // Nav-cluster keys + Ctrl+Shift+<letter> chord alternatives so users
            // on tenkeyless/60% boards still have hotkeys.
            _hotkeyHandler = new HotkeyHandler(
                keyCode =>
                {
                    var kc = (KeyCode)keyCode;
                    if (kc == KeyCode.End) return ChordHotkeys.IsActionPressed(kc, ChordHotkeys.ToggleLetter);
                    return UnityEngine.Input.GetKeyDown(kc);
                },
                null,
                this,
                0.3f
            );
            _hotkeyHandler.SetToggleKey((int)KeyCode.End);

            _receiver.Log = msg => LoggerInstance.Msg(msg);
            _receiver.Start(OpenTrackReceiver.DefaultPort);

            ApplyCameraPatches();
            ApplyHUDPatches();

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

        public override void OnDeinitializeMelon()
        {
            if (_cachedCamera != null) _cachedCamera.ResetWorldToCameraMatrix();
            if (_cachedOutlineCamera != null) _cachedOutlineCamera.ResetWorldToCameraMatrix();
            if (_receiver != null) _receiver.Dispose();
            if (_gameStateDetector != null) _gameStateDetector.Dispose();
        }

        public override void OnUpdate()
        {
            _hotkeyHandler?.Update(Time.unscaledTime);

            MonitorConnectionState();

            if (ChordHotkeys.IsActionPressed(KeyCode.PageUp, ChordHotkeys.PositionLetter))
            {
                CycleTrackingMode();
            }

            if (ChordHotkeys.IsActionPressed(KeyCode.PageDown, ChordHotkeys.FourthToggleLetter))
            {
                _worldSpaceYaw = !_worldSpaceYaw;
                _instance?.LoggerInstance.Msg("Yaw mode: " + (_worldSpaceYaw ? "world-space (horizon-locked)" : "camera-local"));
            }
        }

        // Without this the log has no evidence that tracker packets ever reached the
        // mod, which is the first thing to establish when a user reports no tracking.
        private static void MonitorConnectionState()
        {
            bool isReceiving = _receiver != null && _receiver.IsReceiving;
            if (isReceiving == _wasReceiving) return;

            _wasReceiving = isReceiving;
            _instance?.LoggerInstance.Msg(isReceiving
                ? "OpenTrack connected - receiving tracker data"
                : "OpenTrack disconnected - no tracker data");
        }

        // Three-state cycle: full -> rotation only -> position only -> full ...
        private static void CycleTrackingMode()
        {
            bool newRot, newPos;
            if (_rotationEnabled && _positionEnabled)
            {
                newRot = true;
                newPos = false;
            }
            else if (_rotationEnabled && !_positionEnabled)
            {
                newRot = false;
                newPos = true;
            }
            else
            {
                newRot = true;
                newPos = true;
            }

            if (!newPos && _positionEnabled)
            {
                _positionProcessor?.ResetSmoothing();
                _positionInterpolator?.Reset();
                _positionOffset = Vec3.Zero;
            }
            if (!newRot && _rotationEnabled)
            {
                _processor?.ResetSmoothing();
                _poseInterpolator?.Reset();
                _smoothedTrackingRotation = Quaternion.identity;
            }

            _rotationEnabled = newRot;
            _positionEnabled = newPos;

            string label = newRot && newPos
                ? "full (rotation + position)"
                : newRot
                    ? "rotation only (position disabled)"
                    : "position only (rotation disabled)";
            _instance?.LoggerInstance.Msg("Tracking mode: " + label);
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

        // Required by IHotkeyListener. The tracker app owns the centre, so the
        // mod binds no key to this.
        public void OnHotkeyRecenter()
        {
        }

        internal static void RemoveTrackingOffset()
        {
            if (!_hasRenderData) return;
            _hasRenderData = false;
            ResetViewMatrix();
        }

        internal static void ResetTrackingState()
        {
            RemoveTrackingOffset();
            CrosshairMover.ResetCrosshair();
            _positionOffset = Vec3.Zero;
            _smoothedTrackingRotation = Quaternion.identity;
            _processor?.ResetSmoothing();
            _positionProcessor?.Reset();
            _poseInterpolator?.Reset();
            _positionInterpolator?.Reset();
        }

        internal static void RefreshCameraCache()
        {
            // Unity's overloaded == returns true for destroyed objects, triggering re-query.
            if (_cachedCamera != null) return;

            _cachedOutlineCamera = null;
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

        // Runs before CameraManager.LateUpdate, which calls HUDManager.UpdateAfterCamera
        // part way through, so the HUD projects its markers with this frame's pose.
        internal static void UpdateTrackingState()
        {
            // Head motion and tracker packets run on the wall clock. The game scales
            // Time.deltaTime for slow motion (MainLevel) and yes/no dialogs (0.5).
            float deltaTime = Time.unscaledDeltaTime;

            // Only actual signal loss feeds the loss handler. Gameplay pauses
            // (walkie talkie, menus) are NOT signal loss and must not trigger
            // the fade.
            var lossState = _trackingLossHandler!.Update(_receiver!.IsReceiving, deltaTime);

            if (!_trackingEnabled || !_gameStateDetector!.IsInGameplay)
            {
                _hasRenderData = false;
                return;
            }

            switch (lossState)
            {
                case TrackingLossState.Active:
                    ApplyActiveTracking(deltaTime);
                    break;
                case TrackingLossState.Holding:
                    _hasRenderData = HasPose();
                    break;
                case TrackingLossState.Fading:
                case TrackingLossState.Stabilizing:
                    ApplyFadingTracking(deltaTime);
                    break;
            }
        }

        internal static void ApplyHeadTracking()
        {
            // Apply tracking offset via the view matrix instead of Camera.onPreCull
            // (Camera events throw MissingMethodException under MelonLoader).
            if (!_hasRenderData || _cachedCamera == null)
            {
                CrosshairMover.ResetCrosshair();
                return;
            }

            SetHeadTrackedViewMatrix();

            // Reticle compensation: raycast along the clean aim to find the target
            // distance, then project through the head-tracked view matrix. A miss
            // projects the aim direction itself.
            Vector3 aimDir = _cachedCameraTransform!.forward;
            RaycastHit hit;
            if (!Physics.Raycast(_cachedCameraTransform.position, aimDir, out hit, MaxRaycastDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                _aimDepth = MaxRaycastDistance;
            }
            else if (hit.distance >= MinRaycastDistance)
            {
                _aimDepth = hit.distance;
            }

            CrosshairMover.OffsetCrosshair(
                CanvasCompensation.CalculateAimScreenOffset(_cachedCamera, aimDir, _aimDepth, 1f));
        }

        /// <summary>
        /// Computes and sets the head-tracked worldToCameraMatrix on _cachedCamera.
        /// Head tracking is applied in the camera's local frame so that yaw always
        /// pans left/right on screen, even at steep pitch angles.
        /// </summary>
        private static void SetHeadTrackedViewMatrix()
        {
            var cameraRotation = _cachedCameraTransform!.rotation;

            // Default camera-local: yaw always pans view horizontally regardless
            // of game pitch. World-space (PGDN toggle) locks yaw to world up so
            // the horizon stays level, at the cost of degenerating into roll
            // near +/-90 pitch.
            Quaternion modifiedRot;
            if (_worldSpaceYaw)
            {
                var euler = _smoothedTrackingRotation.eulerAngles;
                float trackYaw = euler.y > 180f ? euler.y - 360f : euler.y;
                float trackPitch = euler.x > 180f ? euler.x - 360f : euler.x;
                float trackRoll = euler.z > 180f ? euler.z - 360f : euler.z;
                Quaternion worldYaw = Quaternion.AngleAxis(trackYaw, Vector3.up);
                Quaternion localPR = Quaternion.Euler(trackPitch, 0f, trackRoll);
                modifiedRot = worldYaw * cameraRotation * localPR;
            }
            else
            {
                modifiedRot = cameraRotation * _smoothedTrackingRotation;
            }
            Vector3 modifiedPos = _cachedCameraTransform.position
                + PositionApplicator.ToHorizonLockedWorld(_positionOffset, cameraRotation);

            Matrix4x4 viewMatrix = Matrix4x4.TRS(modifiedPos, modifiedRot, Vector3.one).inverse;
            viewMatrix.m20 = -viewMatrix.m20;
            viewMatrix.m21 = -viewMatrix.m21;
            viewMatrix.m22 = -viewMatrix.m22;
            viewMatrix.m23 = -viewMatrix.m23;
            _cachedCamera!.worldToCameraMatrix = viewMatrix;

            // Outline camera is a child of the main camera - it inherits the
            // transform but not the overridden worldToCameraMatrix. Apply the
            // same matrix so the outline renders from the tracked viewpoint.
            if (_cachedOutlineCamera != null)
                _cachedOutlineCamera.worldToCameraMatrix = viewMatrix;
        }

        /// <summary>
        /// Temporarily applies the head-tracked view matrix so that WorldToScreenPoint
        /// calls in HUDManager.UpdateAfterCamera project to where the frame is drawn.
        /// </summary>
        internal static bool ApplyTrackingToViewMatrix()
        {
            if (!_hasRenderData || _cachedCamera == null) return false;
            SetHeadTrackedViewMatrix();
            return true;
        }

        internal static void ResetViewMatrix()
        {
            if (_cachedCamera != null)
                _cachedCamera.ResetWorldToCameraMatrix();
            if (_cachedOutlineCamera != null)
                _cachedOutlineCamera.ResetWorldToCameraMatrix();
        }

        private static bool HasPose()
        {
            var q = _smoothedTrackingRotation;
            return q.x != 0f || q.y != 0f || q.z != 0f
                || _positionOffset.X != 0f || _positionOffset.Y != 0f || _positionOffset.Z != 0f;
        }

        private static void ApplyActiveTracking(float deltaTime)
        {
            var rawPose = _receiver!.GetLatestPose();

            // Sample-rate-to-frame-rate interpolation is gated on receiving data, never on
            // the smoothing value: LocalSmoothing is 0.0, and a smoothing-based gate would
            // leave every local user with stepped motion on a high-refresh display.
            rawPose = _poseInterpolator!.Update(rawPose, deltaTime);

            // A connection change (local tracker <-> remote device) swaps which smoothing
            // parameter applies, so refresh the flag every frame from the receiver.
            bool isRemoteConnection = _receiver.IsRemoteConnection;
            _processor!.IsRemoteConnection = isRemoteConnection;
            _positionProcessor!.IsRemoteConnection = isRemoteConnection;

            var processed = _processor.Process(rawPose, deltaTime);

            // Processor handles smoothing internally (per-axis Euler, connection-selected
            // LocalSmoothing / RemoteSmoothing). Use its output directly - no second layer.
            float yaw = 0f, pitch = 0f, roll = 0f;
            if (_rotationEnabled)
            {
                yaw = processed.Yaw;
                pitch = processed.Pitch;
                roll = processed.Roll;
            }
            _smoothedTrackingRotation = CameraRotationComposer.GetTrackingOnlyRotation(yaw, pitch, roll);

            _positionOffset = Vec3.Zero;
            if (_positionEnabled)
            {
                var interpolatedPos = _positionInterpolator!.Update(_receiver.GetLatestPosition(), deltaTime);
                var headRotQ = QuaternionUtils.FromYawPitchRoll(yaw, pitch, roll);
                Vec3 posOffset = _positionProcessor.Process(interpolatedPos, headRotQ, deltaTime);
                // Negate X to match Green Hell's coordinate convention. Z is NOT negated
                // here: PositionApplicator flips it, at the point the offset leaves the
                // pipeline for Unity's +z-forward world space.
                _positionOffset = new Vec3(-posOffset.X, posOffset.Y, posOffset.Z);
            }

            _hasRenderData = true;
        }

        private static void ApplyFadingTracking(float deltaTime)
        {
            float t = _trackingLossHandler!.GetFadeInterpolation(deltaTime);

            var q = Quaternion.Slerp(_smoothedTrackingRotation, Quaternion.identity, t);
            _smoothedTrackingRotation = new Vector3(q.x, q.y, q.z).sqrMagnitude < FadedRotationSqr
                ? Quaternion.identity
                : q;

            var p = Vec3.Lerp(_positionOffset, Vec3.Zero, t);
            _positionOffset = p.SqrMagnitude < FadedPositionSqr ? Vec3.Zero : p;

            _hasRenderData = HasPose();
        }
    }

}
