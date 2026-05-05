using Backend.Util.Input;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    public static class InputSystem
    {
        private static readonly Subject<Vector2> onPointerDownSubject = new Subject<Vector2>();

        public static Observable<Vector2> OnPointerDown => onPointerDownSubject;

        private static PuzzleControl puzzleAction;

        public static void Initialize()
        {
            Dispose();

            puzzleAction = new PuzzleControl();

            puzzleAction.Puzzle.Press.started += context =>
            {
                Vector2 screenPos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
                onPointerDownSubject.OnNext(screenPos);
            };

            puzzleAction.Puzzle.Enable();
        }

        public static void Dispose()
        {
            puzzleAction?.Puzzle.Disable();
            puzzleAction?.Dispose();
            puzzleAction = null;
        }
    }
}
