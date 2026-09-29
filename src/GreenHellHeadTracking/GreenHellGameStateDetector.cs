using System;
using System.Reflection;
using CameraUnlock.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GreenHellHeadTracking
{
    /// <summary>
    /// Game state detector for Green Hell.
    /// Detects when the player is in menus, inventory, pause screen, etc.
    /// Head tracking is disabled during these states for better UX.
    /// </summary>
    public class GreenHellGameStateDetector : GameStateDetectorBase
    {
        private readonly MethodInfo? _playerGet;
        private readonly MethodInfo? _playerIsDead;
        private bool _deathCheckFailed;

        public GreenHellGameStateDetector(GetCurrentTime getTime)
            : base(getTime)
        {
            var playerType = Type.GetType("Player, Assembly-CSharp");
            _playerGet = playerType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            _playerIsDead = playerType?.GetMethod("IsDead", BindingFlags.Public | BindingFlags.Instance);
            if (_playerGet == null || _playerIsDead == null)
            {
                MelonLoader.MelonLogger.Warning("[GreenHellHeadTracking] Player.Get/IsDead not found - death screen detection disabled");
            }

            AddMenuScene("MainMenu");
            AddMenuScene("Loading");
        }

        protected override string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        protected override bool IsGamePaused()
        {
            return Time.timeScale < 0.01f;
        }

        protected override bool IsCursorVisible()
        {
            // Green Hell uses CursorLockMode.None even during gameplay,
            // so check Cursor.visible instead of lockState. Opening the backpack
            // (Inventory3DManager) shows it.
            return Cursor.visible;
        }

        protected override bool IsPlayerDead()
        {
            if (_playerGet == null || _playerIsDead == null || _deathCheckFailed) return false;

            try
            {
                // Re-read on every check, never cached: after a death and reload the
                // previous player is a destroyed object whose HP still reads 0.
                var player = _playerGet.Invoke(null, null) as UnityEngine.Object;
                if (player == null) return false;
                return (bool)_playerIsDead.Invoke(player, null);
            }
            catch (Exception ex)
            {
                _deathCheckFailed = true;
                MelonLoader.MelonLogger.Warning("[GreenHellHeadTracking] Player.IsDead failed, death screen detection disabled: "
                    + (ex.InnerException ?? ex));
                return false;
            }
        }
    }
}
