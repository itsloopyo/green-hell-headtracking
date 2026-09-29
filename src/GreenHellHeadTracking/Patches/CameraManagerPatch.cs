namespace GreenHellHeadTracking.Patches
{
    internal static class CameraManagerPatch
    {
        public static void LateUpdatePrefix()
        {
            Mod.RemoveTrackingOffset();
            Mod.RefreshCameraCache();
            Mod.UpdateTrackingState();
        }

        public static void LateUpdatePostfix()
        {
            Mod.ApplyHeadTracking();
        }
    }
}
