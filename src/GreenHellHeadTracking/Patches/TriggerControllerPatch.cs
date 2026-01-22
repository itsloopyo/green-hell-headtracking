using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using CameraUnlock.Core.Unity.Harmony;
using UnityEngine;

namespace GreenHellHeadTracking.Patches
{
    internal static class TriggerControllerPatch
    {
        private static FieldInfo? _cameraMainField;

        public static void GetCrossHairDirPostfix(ref Vector3 __result)
        {
            if (Mod.IsTrackingActive)
            {
                __result = Mod.AimDirection;
            }
        }

        public static IEnumerable<CodeInstruction> UpdateBestTriggerTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            if (_cameraMainField == null)
            {
                _cameraMainField = AccessTools.Field(Type.GetType("TriggerController, Assembly-CSharp"), "m_CameraMain");
            }

            if (_cameraMainField == null)
            {
                return instructions;
            }

            var getAimDirectionMethod = AccessTools.Method(typeof(TriggerControllerPatch), nameof(GetAimDirection));

            return TranspilerPatterns.ReplaceCameraForwardWithMethod(
                instructions,
                _cameraMainField,
                getAimDirectionMethod
            );
        }

        public static Vector3 GetAimDirection()
        {
            if (Mod.IsTrackingActive)
            {
                return Mod.AimDirection;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                return cam.transform.forward;
            }

            return Vector3.forward;
        }
    }
}
