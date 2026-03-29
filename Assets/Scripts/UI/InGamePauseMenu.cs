using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class InGamePauseMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField]
        private GameObject pausePanel;

        [SerializeField]
        private GameObject pauseMenuContent;

        [SerializeField]
        private GameObject settingsPanel;

        [SerializeField]
        private GameObject pauseDimOverlay;

        [Header("Buttons")]
        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button exitToMenuButton;

        [Header("Navigation")]
        [SerializeField]
        private int mainMenuSceneIndex = 0;

        [SerializeField]
        private SettingsMenu settingsMenu;

        private bool isPaused;
        private void Awake()
        {
            if (pauseMenuContent == null)
                pauseMenuContent = pausePanel;

            if (pauseDimOverlay == null)
                pauseDimOverlay = pausePanel;

            this.ValidateSerializedFields();

            AddCameraToCanvases();

            EnsureGraphicRaycaster(pausePanel);
            EnsureGraphicRaycaster(settingsPanel);

            SetActiveSafe(pausePanel, false);
            SetActiveSafe(pauseMenuContent, false);
            SetActiveSafe(settingsPanel, false);
            SetActiveSafe(pauseDimOverlay, false);

            settingsMenu.InitializeAndLoad();
            settingsMenu.ApplySavedAudioSettings();

            continueButton.onClick.AddListener(OnContinueButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            exitToMenuButton.onClick.AddListener(OnExitToMenuButtonClicked);
        }

        private void AddCameraToCanvases()
        {
            var canvases = transform.GetComponentsInChildren<Canvas>(true);
            Camera targetCamera = Camera.main;

            if (targetCamera == null)
                targetCamera = FindFirstObjectByType<Camera>();

            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    canvas.worldCamera = targetCamera;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        private void OnDisable()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (exitToMenuButton != null)
                exitToMenuButton.onClick.RemoveListener(OnExitToMenuButtonClicked);
        }

        public void OnContinueButtonClicked()
        {
            ContinueGame();
        }

        public void OnSettingsButtonClicked()
        {
            SetActiveSafe(pausePanel, true);
            SetActiveSafe(pauseDimOverlay, true);
            SetActiveSafe(pauseMenuContent, false);
            SetActiveSafe(settingsPanel, true);
            settingsMenu.ApplySavedAudioSettings();
        }

        public void OnBackFromSettingsClicked()
        {
            if (!isPaused)
                return;

            SetActiveSafe(settingsPanel, false);
            SetActiveSafe(pausePanel, true);
            SetActiveSafe(pauseDimOverlay, true);
            SetActiveSafe(pauseMenuContent, true);
        }

        public void OnExitToMenuButtonClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneIndex);
        }

        private void TogglePause()
        {
            if (isPaused)
                ContinueGame();
            else
                PauseGame();
        }

        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            SetActiveSafe(pausePanel, true);
            SetActiveSafe(pauseDimOverlay, true);
            SetActiveSafe(pauseMenuContent, true);
            SetActiveSafe(settingsPanel, false);
        }

        private void ContinueGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            SetActiveSafe(settingsPanel, false);
            SetActiveSafe(pauseMenuContent, false);
            SetActiveSafe(pauseDimOverlay, false);
            SetActiveSafe(pausePanel, false);
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private static void EnsureGraphicRaycaster(GameObject root)
        {
            if (root == null)
                return;

            Canvas canvas = root.GetComponent<Canvas>();

            if (canvas == null)
                canvas = root.GetComponentInParent<Canvas>(true);

            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
