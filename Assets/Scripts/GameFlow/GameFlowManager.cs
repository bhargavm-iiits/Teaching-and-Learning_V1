using System;
using UnityEngine;

namespace NumberLinePlayground.Flow
{
    /// The high-level state machine from the game design doc:
    /// Boot -> ComfortSetup -> Profile -> Tutorial -> Hub
    ///   -> ConceptIntro -> LevelStart -> LevelPlay -> LevelComplete -> Checkpoint (x3 levels)
    ///   -> QuizIntro -> Question -> Feedback (x3 questions)
    ///   -> Result -> { ConceptComplete | SkillRepair | NextApproach | GuidedMode }
    ///   -> Bridge -> (next concept) ... -> BossMission -> ReportCard
    ///
    /// This is scaffolding: it compiles and can be driven/tested standalone, but nothing in
    /// the current NumberLinePlayground scene is wired to it yet.
    public enum GameState
    {
        Boot,
        ComfortSetup,
        Profile,
        Tutorial,
        Hub,
        ConceptIntro,
        LevelStart,
        LevelPlay,
        LevelComplete,
        Checkpoint,
        QuizIntro,
        Question,
        Feedback,
        Result,
        ConceptComplete,
        SkillRepair,
        NextApproach,
        GuidedMode,
        Bridge,
        BossMission,
        ReportCard,
    }

    public class GameFlowManager : MonoBehaviour
    {
        /// Fires after the state has already changed, with (previous, current).
        public event Action<GameState, GameState> StateChanged;

        [SerializeField] GameState currentState = GameState.Boot;
        public GameState CurrentState => currentState;

        public void ChangeState(GameState next)
        {
            if (next == currentState) return;
            var previous = currentState;
            currentState = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
