using Backend.Util.Management;
using UnityEngine;
using R3;
using Backend.Util.Input;

namespace Backend.Object.Management
{
    public class InputManager : SingletonGameObject<InputManager>
    {
        private static readonly Subject<Vector2> onPointerDownSubject = new Subject<Vector2>();

        public static Observable<Vector2> OnPointerDown => onPointerDownSubject;

        private PuzzleControl puzzleAction;

        protected override void OnAwake()
        {
            puzzleAction = new PuzzleControl();

            puzzleAction.Puzzle.Press.started += context =>
            {
                Vector2 screenPos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
                onPointerDownSubject.OnNext(screenPos);
            };
        }

        private void OnEnable()
        {
            puzzleAction?.Puzzle.Enable();
        }

        private void OnDisable()
        {
            puzzleAction?.Puzzle.Disable();
        }

        private void OnDestroy()
        {
            onPointerDownSubject?.Dispose();
        }
    }
}
