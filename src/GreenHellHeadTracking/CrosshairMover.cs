using System;
using System.Reflection;
using CameraUnlock.Core.Unity.Utilities;
using UnityEngine;

namespace GreenHellHeadTracking
{
    internal static class CrosshairMover
    {
        private static Type? _hudCrosshairType;
        private static MethodInfo? _getInstanceMethod;
        private static FieldInfo? _crosshairLeftField;
        private static bool _initialized;
        private static bool _unavailable;
        private static bool _runtimeErrorLogged;
        private static RectTransform? _crosshairParent;
        private static object? _lastInstance;

        public static void Initialize()
        {
            if (_initialized) return;
            if (_unavailable) return;

            _hudCrosshairType = Type.GetType("HUDCrosshair, Assembly-CSharp");
            if (_hudCrosshairType == null)
            {
                _unavailable = true;
                MelonLoader.MelonLogger.Warning("[GreenHellHeadTracking] HUDCrosshair type not found - crosshair offset disabled");
                return;
            }

            _getInstanceMethod = _hudCrosshairType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            _crosshairLeftField = _hudCrosshairType.GetField("m_CrosshairLeft", BindingFlags.Public | BindingFlags.Instance);

            if (_getInstanceMethod == null || _crosshairLeftField == null)
            {
                _unavailable = true;
                MelonLoader.MelonLogger.Warning("[GreenHellHeadTracking] HUDCrosshair API not found - crosshair offset disabled");
                return;
            }

            _initialized = true;
        }

        public static void OffsetCrosshair(Vector2 offset)
        {
            if (!_initialized && !_unavailable)
            {
                Initialize();
            }

            if (_initialized)
            {
                try
                {
                    // Cache the HUD singleton — avoids MethodInfo.Invoke every frame.
                    // Re-query only when cached instance is null or Unity-destroyed.
                    var instance = _lastInstance;
                    if (instance == null || (instance is UnityEngine.Object uObj && uObj == null))
                    {
                        instance = _getInstanceMethod!.Invoke(null, null);
                        if (instance == null) return;
                        _lastInstance = instance;
                        _crosshairParent = null;
                    }

                    if (_crosshairParent == null)
                    {
                        var crosshairLeft = _crosshairLeftField!.GetValue(instance) as Component;
                        if (crosshairLeft != null)
                        {
                            _crosshairParent = crosshairLeft.transform.parent as RectTransform;
                        }
                    }

                    if (_crosshairParent != null)
                    {
                        CrosshairUtility.OffsetByScreenPixels(_crosshairParent, offset);
                    }
                }
                catch (Exception ex)
                {
                    if (!_runtimeErrorLogged)
                    {
                        _runtimeErrorLogged = true;
                        MelonLoader.MelonLogger.Warning($"[GreenHellHeadTracking] Crosshair offset error: {ex.Message}");
                    }
                    _crosshairParent = null;
                    _lastInstance = null;
                }
            }
        }

        public static void ResetCrosshair()
        {
            OffsetCrosshair(Vector2.zero);
        }
    }
}
