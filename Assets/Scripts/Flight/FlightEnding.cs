using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProximoVoo
{
    [DefaultExecutionOrder(120)]
    public sealed class FlightEnding : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private GameObject farewellPanel;
        [SerializeField] private GameObject retryPanel;

        public void Configure(FlightController controller, Camera camera, Material material, GameObject farewell, GameObject retry)
        {
            flight = controller;
            farewellPanel = farewell;
            retryPanel = retry;
        }

        private void Awake()
        {
            farewellPanel.SetActive(false);
            retryPanel.SetActive(false);
        }

        private void LateUpdate()
        {
            bool farewell = flight.Phase == FlightPhase.Farewell;
            bool retry = flight.Phase == FlightPhase.Completed;
            float night = farewell ? Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((flight.PhaseElapsed - flight.NightfallDelay) / flight.NightfallDuration)) : retry ? 1f : 0f;
            if (globalLight != null) globalLight.intensity = 1f - night;
            farewellPanel.SetActive(farewell && flight.PhaseElapsed >= flight.FarewellStart);
            if (retryPanel.activeSelf != retry)
            {
                retryPanel.SetActive(retry);
                if (retry && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(retryPanel.GetComponentInChildren<UnityEngine.UI.Button>().gameObject);
            }
        }
    }
}
