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
        private const float HoldThreshold = 0.4f;

        private static readonly Subject<Vector2> onPointerPressedSubject = new Subject<Vector2>();
        private static readonly Subject<Vector2> onPointerHoldBeganSubject = new Subject<Vector2>();
        private static readonly Subject<(Vector2 pos, bool wasHold)> onPointerReleasedSubject = new Subject<(Vector2, bool)>();

        public static Observable<Vector2> OnPointerPressed => onPointerPressedSubject;
        public static Observable<Vector2> OnPointerHoldBegan => onPointerHoldBeganSubject;
        public static Observable<(Vector2 pos, bool wasHold)> OnPointerReleased => onPointerReleasedSubject;

        private static PuzzleControl puzzleAction;
        private static CancellationTokenSource holdCts;
        private static bool holdTriggered;
        private static Vector2 lastPressPos;

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
            CancelHoldTimer();

            if (puzzleAction != null)
            {
                puzzleAction.Puzzle.Press.started -= OnPressStarted;
                puzzleAction.Puzzle.Press.canceled -= OnPressCanceled;
                puzzleAction.Puzzle.Disable();
                puzzleAction.Dispose();
                puzzleAction = null;
            }
        }

        private static void OnPressStarted(InputAction.CallbackContext _)
        {
            lastPressPos = puzzleAction.Puzzle.Position.ReadValue<Vector2>();
            holdTriggered = false;

            onPointerPressedSubject.OnNext(lastPressPos);

            CancelHoldTimer();
            holdCts = new CancellationTokenSource();
            HoldTimerAsync(holdCts.Token).Forget();
        }

        private static void OnPressCanceled(InputAction.CallbackContext _)
        {
            bool wasHold = holdTriggered;
            CancelHoldTimer();
            holdTriggered = false;

            onPointerReleasedSubject.OnNext((lastPressPos, wasHold));
        }

        private static async UniTaskVoid HoldTimerAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(HoldThreshold), cancellationToken: token);
                if (token.IsCancellationRequested) return;

                holdTriggered = true;
                onPointerHoldBeganSubject.OnNext(lastPressPos);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void CancelHoldTimer()
        {
            if (holdCts == null) return;
            holdCts.Cancel();
            holdCts.Dispose();
            holdCts = null;
        }
    }
}
