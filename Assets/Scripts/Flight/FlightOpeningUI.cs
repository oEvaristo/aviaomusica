using UnityEngine;

namespace ProximoVoo
{
    public sealed class FlightOpeningUI : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private TMPro.TMP_Text status;

        public void Configure(FlightController controller, GameObject menu, TMPro.TMP_Text statusLabel)
        {
            flight = controller;
            startPanel = menu;
            status = statusLabel;
        }

        private void Update()
        {
            if (flight == null || startPanel == null || status == null) return;
            bool ready = flight.Phase == FlightPhase.Ready;
            if (startPanel.activeSelf != ready) startPanel.SetActive(ready);
            string message = "";
            if (flight.IsPaused) message = "Pausado — Espaço para continuar";
            else switch (flight.Phase)
            {
                case FlightPhase.StartingEngine: message = "Ligando o motor…"; break;
                case FlightPhase.Rolling: message = "Decolagem automática"; break;
                case FlightPhase.TakingOff: message = "Subindo…"; break;
                case FlightPhase.Flying:
                    if (flight.PhaseElapsed < 5f) message = "O avião é seu • W para subir   S para descer";
                    break;
            }
            if (status.text != message) status.text = message;
            if (status.gameObject.activeSelf != (message.Length > 0))
                status.gameObject.SetActive(message.Length > 0);
        }
    }
}
