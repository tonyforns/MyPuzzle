using System.Collections.Generic;
using MyPuzzle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MyPuzzle.View
{
    public sealed class DragController : MonoBehaviour
    {
        [SerializeField] PuzzleSession session;
        bool dragging;
        bool pressed;
        bool hasClick;
        CellId clickCell;
        float clickTime;
        Vector2 pressScreen;
        Vector2 clickScreen;
        const float doubleClickSeconds = 0.32f;
        const float dragSlopPixels = 6f;

        void Awake()
        {
            if (session == null)
                session = GetComponent<PuzzleSession>();
        }

        void Update()
        {
            if (session == null)
                return;

            var pointer = Pointer.current;
            if (pointer == null)
                return;

            if (pointer.press.wasReleasedThisFrame)
            {
                if (dragging)
                    session.Release();

                dragging = false;
                pressed = false;
                return;
            }

            var screen = pointer.position.ReadValue();
            if (!TryProjectOntoTable(screen, out var world))
                return;

            if (pointer.press.wasPressedThisFrame && !IsOverInterface(screen))
            {
                if (IsDoubleClick(screen, world))
                {
                    hasClick = false;
                    pressed = false;
                    dragging = false;
                    session.DetachAt(world);
                }
                else
                {
                    pressed = true;
                    pressScreen = screen;
                    clickScreen = screen;
                    clickTime = Time.unscaledTime;
                    hasClick = session.TryPieceAt(world, out clickCell);
                }
            }
            else if (pressed && pointer.press.isPressed && (screen - pressScreen).sqrMagnitude >= dragSlopPixels * dragSlopPixels)
            {
                pressed = false;
                hasClick = false;
                dragging = session.Pick(world);
            }
            else if (dragging && pointer.press.isPressed)
                session.Drag(world);

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.rightButton.wasPressedThisFrame && !IsOverInterface(screen))
                session.RotateAt(world);

            if (!dragging)
                return;

            var scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.01f)
                session.RotateHeld();
            else if (scroll < -0.01f)
                session.RotateHeld(-1);
        }

        bool IsDoubleClick(Vector2 screen, Vector2 world)
        {
            if (!hasClick || Time.unscaledTime - clickTime > doubleClickSeconds)
                return false;
            if ((screen - clickScreen).sqrMagnitude > dragSlopPixels * dragSlopPixels)
                return false;

            return session.TryPieceAt(world, out var cell) && cell.Equals(clickCell);
        }

        static bool TryProjectOntoTable(Vector2 screen, out Vector2 world)
        {
            world = default;
            var camera = Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
                return false;

            var distance = -ray.origin.z / ray.direction.z;
            if (distance < 0f)
                return false;

            var point = ray.origin + (ray.direction * distance);
            world = new Vector2(point.x, point.y);
            return true;
        }

        static bool IsOverInterface(Vector2 screen)
        {
            var events = EventSystem.current;
            if (events == null)
                return false;

            var data = new PointerEventData(events) { position = screen };
            interfaceHits.Clear();
            events.RaycastAll(data, interfaceHits);
            return interfaceHits.Count > 0;
        }

        static readonly List<RaycastResult> interfaceHits = new List<RaycastResult>();
    }
}
