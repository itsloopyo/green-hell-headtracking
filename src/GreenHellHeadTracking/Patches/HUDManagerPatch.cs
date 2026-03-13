namespace GreenHellHeadTracking.Patches
{
    internal static class HUDManagerPatch
    {
        private static bool _applied;

        public static void UpdateAfterCameraPrefix()
        {
            _applied = false;
            if (Mod.IsTrackingActive)
            {
                Mod.ApplyTrackingToViewMatrix();
                _applied = true;
            }
        }

        public static void UpdateAfterCameraPostfix()
        {
            if (_applied)
            {
                Mod.ResetViewMatrix();
                _applied = false;
            }
        }
    }
}
