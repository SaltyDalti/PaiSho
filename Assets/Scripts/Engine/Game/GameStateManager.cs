
using UnityEngine;

namespace PaiSho.Game
{
    public enum GamePhase
    {
        Spring,
        Play,
        End
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;

        private GamePhase currentPhase = GamePhase.Spring;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public GamePhase GetCurrentPhase()
        {
            return currentPhase;
        }

        public void SetPhase(GamePhase phase)
        {
            currentPhase = phase;
            Debug.Log($"Game phase changed to: {phase}");
        }

        public bool IsSpringPhase()
        {
            return currentPhase == GamePhase.Spring;
        }

        public bool IsPlayPhase()
        {
            return currentPhase == GamePhase.Play;
        }

        public bool IsEndPhase()
        {
            return currentPhase == GamePhase.End;
        }

        public void AdvancePhase()
        {
            if (currentPhase == GamePhase.Spring)
                SetPhase(GamePhase.Play);
            else if (currentPhase == GamePhase.Play)
                SetPhase(GamePhase.End);
        }
    }
}
