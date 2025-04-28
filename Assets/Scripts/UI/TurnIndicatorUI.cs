using UnityEngine;
using TMPro;
using PaiSho.Game;

namespace PaiSho.UI
{
    public class TurnIndicatorUI : MonoBehaviour
    {
        public TMP_Text turnText; // TMP_Text instead of old UI Text!

        private void Update()
        {
            if (!GameManager.Instance) return;

            string phaseText = GameManager.Instance.IsSpringPhase() ? "Spring Phase" : "Normal Play";
            string playerText = GameManager.Instance.GetCurrentPlayer().ToString();

            turnText.text = $"Current Turn: {playerText}\nPhase: {phaseText}";
        }
    }
}
