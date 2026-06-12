using System;
using System.Threading;
using Backend.Util.Input;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backend.Object.GameSystems
{
    public static class InputSystem
    {
        private static readonly Subject<Vector2> onPointerPressedSubject = new Subject<Vector2>();
        private static readonly Subject<Vector2> onPointerMovedSubject = new Subject<Vector2>();
        private static readonly Subject<Vector2> onPointerReleasedSubject = new Subject<Vector2>();

        public static Observable<Vector2> OnPointerPressed => onPointerPressedSubject;
        public static Observable<Vector2> OnPointerMoved => onPointerMovedSubject;
        public static Observable<Vector2> OnPointerReleased => onPointerReleasedSubject;

        private static PuzzleControl puzzleAction;
        private static CancellationTokenSource moveCts;

        public static void Initialize()
        {
            Dispose();

            puzzleAction = new PuzzleControl();

            puzzleAction.Puzzle.Press.started += OnPressStarted;
            puzzleAction.Puzzle.Press.canceled += OnPressCanceled;

            puzzleAction.Puzzle.Enable();
        }

        public static void Dispose()
        {
            CancelMoveTracker();

            if (puzzleAction != null)
            {
                puzzleAction.Puzzle.Press.started -= OnPressStarted;
                puzzleAction.Puzzle.Press.canceled -= OnPressCanceled;
                puzzleAction.Disable();
                puzzleAction.Dispose();
                puzzleAction = null;
            }
        }

        private static void OnPressStarted(InputAction.CallbackContext _)
        {
            var pos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
            onPointerPressedSubject.OnNext(pos);

            CancelMoveTracker();
            moveCts = new CancellationTokenSource();
            TrackMoveAsync(moveCts.Token).Forget();
        }

        private static void OnPressCanceled(InputAction.CallbackContext _)
        {
            CancelMoveTracker();
            var pos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
            onPointerReleasedSubject.OnNext(pos);
        }

        private static async UniTaskVoid TrackMoveAsync(CancellationToken token)
        {
            Vector2 last = Vector2.positiveInfinity;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    var pos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
                    if (pos != last)
                    {
                        onPointerMovedSubject.OnNext(pos);
                        last = pos;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void CancelMoveTracker()
        {
            if (moveCts == null) return;
            moveCts.Cancel();
            moveCts.Dispose();
            moveCts = null;
        }
    }
}
