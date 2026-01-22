namespace GreenHellHeadTracking.Patches
{
    internal static class CameraManagerPatch
    {
        public static void LateUpdatePrefix()
        {
            Mod.RemoveTrackingOffset();
        }

        public static void LateUpdatePostfix()
        {
            Mod.ApplyHeadTracking();
        }
    }
}
