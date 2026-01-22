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
        private sealed class ReflectedComponent
        {
            private readonly string _componentName;
            private readonly Func<object?> _getInstance;
            private readonly Func<object, object?> _getValue;
            private object? _cachedInstance;
            private bool _failed;

            public ReflectedComponent(string componentName, Func<object?> getInstance, Func<object, object?> getValue)
            {
                _componentName = componentName;
                _getInstance = getInstance;
                _getValue = getValue;
            }

            public bool TryGetValue(out bool value)
            {
                value = false;
                if (_failed) return false;

                try
                {
                    _cachedInstance ??= _getInstance();
                    if (_cachedInstance == null) return false;

                    var raw = _getValue(_cachedInstance);
                    if (raw is bool b)
                    {
                        value = b;
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _failed = true;
                    _cachedInstance = null;
                    MelonLoader.MelonLogger.Warning($"[GreenHellHeadTracking] {_componentName} reflection failed: {ex.Message}");
                    return false;
                }
            }

            public void ClearCacheIfDestroyed()
            {
                if (_cachedInstance is UnityEngine.Object obj && obj == null)
                {
                    _cachedInstance = null;
                }
            }
        }

        private static ReflectedComponent? _cursorManager;
        private static ReflectedComponent? _menuInGame;
        private static ReflectedComponent? _mainMenu;
        private static ReflectedComponent? _dialogsManager;
        private static ReflectedComponent? _inventory;
        private static ReflectedComponent? _player;

        private static bool _reflectionInitialized;
        private static float _lastCacheValidationTime;
        private const float CacheValidationInterval = 1.0f;

        public GreenHellGameStateDetector(GetCurrentTime getTime)
            : base(getTime)
        {
            InitializeReflection();

            AddMenuScene("MainMenu");
            AddMenuScene("Loading");
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            var cursorType = Type.GetType("CursorManager, Assembly-CSharp");
            var cursorGet = cursorType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            var cursorProp = cursorType?.GetProperty("IsCursorVisible", BindingFlags.Public | BindingFlags.Instance);
            if (cursorGet != null && cursorProp != null)
            {
                _cursorManager = new ReflectedComponent(
                    "CursorManager",
                    () => cursorGet.Invoke(null, null),
                    inst => cursorProp.GetValue(inst));
            }

            var menuType = Type.GetType("MenuInGame, Assembly-CSharp");
            var menuProp = menuType?.GetProperty("m_Active", BindingFlags.NonPublic | BindingFlags.Instance);
            if (menuType != null && menuProp != null)
            {
                _menuInGame = new ReflectedComponent(
                    "MenuInGame",
                    () => UnityEngine.Object.FindObjectOfType(menuType),
                    inst => menuProp.GetValue(inst));
            }

            var mainMenuType = Type.GetType("MainMenu, Assembly-CSharp");
            var mainMenuGet = mainMenuType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            var mainMenuProp = mainMenuType?.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (mainMenuGet != null && mainMenuProp != null)
            {
                _mainMenu = new ReflectedComponent(
                    "MainMenu",
                    () => mainMenuGet.Invoke(null, null),
                    inst => mainMenuProp.GetValue(inst));
            }

            var dialogsType = Type.GetType("DialogsManager, Assembly-CSharp");
            var dialogsGet = dialogsType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            var dialogsProp = dialogsType?.GetProperty("IsDialogActive", BindingFlags.Public | BindingFlags.Instance);
            if (dialogsGet != null && dialogsProp != null)
            {
                _dialogsManager = new ReflectedComponent(
                    "DialogsManager",
                    () => dialogsGet.Invoke(null, null),
                    inst => dialogsProp.GetValue(inst));
            }

            var inventoryType = Type.GetType("Inventory, Assembly-CSharp");
            var inventoryGet = inventoryType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            var inventoryField = inventoryType?.GetField("m_Opened", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inventoryGet != null && inventoryField != null)
            {
                _inventory = new ReflectedComponent(
                    "Inventory",
                    () => inventoryGet.Invoke(null, null),
                    inst => inventoryField.GetValue(inst));
            }

            var playerType = Type.GetType("Player, Assembly-CSharp");
            var playerGet = playerType?.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
            var playerIsDead = playerType?.GetMethod("IsDead", BindingFlags.Public | BindingFlags.Instance);
            if (playerGet != null && playerIsDead != null)
            {
                _player = new ReflectedComponent(
                    "Player",
                    () => playerGet.Invoke(null, null),
                    inst => playerIsDead.Invoke(inst, null));
            }
        }

        protected override string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        private void ValidateCacheIfNeeded(float currentTime)
        {
            if (currentTime - _lastCacheValidationTime < CacheValidationInterval) return;
            _lastCacheValidationTime = currentTime;

            _cursorManager?.ClearCacheIfDestroyed();
            _menuInGame?.ClearCacheIfDestroyed();
            _mainMenu?.ClearCacheIfDestroyed();
            _dialogsManager?.ClearCacheIfDestroyed();
            _inventory?.ClearCacheIfDestroyed();
            _player?.ClearCacheIfDestroyed();
        }

        protected override bool IsGamePaused()
        {
            return Time.timeScale < 0.01f;
        }

        protected override bool IsCursorVisible()
        {
            // Check lock state directly — CursorManager.IsCursorVisible is too
            // broad and fires during in-game overlays (walkie talkie) where the
            // player still has camera control.
            return Cursor.lockState != CursorLockMode.Locked;
        }

        protected override bool IsMenuVisible()
        {
            if (_menuInGame != null && _menuInGame.TryGetValue(out bool menuActive) && menuActive)
                return true;
            if (_mainMenu != null && _mainMenu.TryGetValue(out bool mainMenuActive) && mainMenuActive)
                return true;
            return false;
        }

        protected override bool IsInventoryOpen()
        {
            return _inventory != null && _inventory.TryGetValue(out bool opened) && opened;
        }

        protected override bool IsPlayerDead()
        {
            return _player != null && _player.TryGetValue(out bool dead) && dead;
        }

        protected override bool HasCameraControl()
        {
            return true;
        }
    }
}
