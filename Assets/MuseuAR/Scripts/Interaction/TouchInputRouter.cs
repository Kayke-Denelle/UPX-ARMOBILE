using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MuseuAR
{
    /// <summary>
    /// Central de toques (Input System):
    ///  • Toque rápido num hotspot  -> HotspotTapped
    ///  • Toque rápido no vazio     -> EmptyTap (usado pelo modo Acervo para posicionar)
    ///  • Arrastar 1 dedo           -> gira a peça ativa
    ///  • Pinça 2 dedos             -> zoom na peça ativa
    /// No PC/editor o mouse faz o mesmo: clique = toque, arrastar (botão esquerdo) = girar, roda = zoom.
    /// O botão direito fica livre para a câmera do XR Simulation (segurar direito + WASD).
    /// Obs.: não usamos TouchSimulation porque ele desativa o mouse e trava a câmera do XR Simulation.
    /// </summary>
    public class TouchInputRouter : MonoBehaviour
    {
        [SerializeField] Camera arCamera;
        [SerializeField] LayerMask raycastMask = ~0;
        [SerializeField] float maxRayDistance = 20f;

        [Header("Toque")]
        [SerializeField] float tapMaxDuration = 0.35f;
        [SerializeField] float tapMaxMovePixels = 25f;

        [Header("Gestos")]
        [SerializeField] bool gesturesEnabled = true;
        [SerializeField] float rotateDegreesPerPixel = 0.35f;
        [Tooltip("Zoom por 'clique' da roda do mouse (PC).")]
        [SerializeField] float mouseWheelZoomStep = 1.1f;

        public static event Action<HotspotView> HotspotTapped;
        public static event Action<Vector2> EmptyTap;

        Vector2 touchStartPos;
        float touchStartTime;
        bool isDragging;
        bool startedOverUI;
        bool isPinching;
        float lastPinchDistance;
        bool mouseDown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            HotspotTapped = null;
            EmptyTap = null;
        }

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count > 0)
            {
                UpdateTouches(touches);
                return;
            }
            isPinching = false;
            UpdateMouse();
        }

        // ---------------- Celular ----------------

        void UpdateTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
        {
            if (touches.Count >= 2)
            {
                HandlePinch(touches[0], touches[1]);
                isDragging = true; // impede que o fim da pinça conte como toque
                return;
            }
            isPinching = false;

            var touch = touches[0];
            switch (touch.phase)
            {
                case TouchPhase.Began: PointerDown(touch.screenPosition); break;
                case TouchPhase.Moved: PointerMoved(touch.screenPosition, touch.delta); break;
                case TouchPhase.Ended: PointerUp(touch.screenPosition); break;
            }
        }

        void HandlePinch(Touch a, Touch b)
        {
            float distance = Vector2.Distance(a.screenPosition, b.screenPosition);
            if (!isPinching)
            {
                isPinching = true;
                lastPinchDistance = distance;
                return;
            }
            if (gesturesEnabled && lastPinchDistance > 1f && ExhibitController.Active)
                ExhibitController.Active.ScaleBy(distance / lastPinchDistance);
            lastPinchDistance = distance;
        }

        // ---------------- PC (mouse) ----------------

        void UpdateMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Botão direito = navegação do XR Simulation; não mistura com gestos.
            if (mouse.rightButton.isPressed)
            {
                mouseDown = false;
                return;
            }

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                mouseDown = true;
                PointerDown(position);
            }
            else if (mouseDown && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0f) PointerMoved(position, delta);
            }
            else if (mouseDown && mouse.leftButton.wasReleasedThisFrame)
            {
                mouseDown = false;
                PointerUp(position);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (gesturesEnabled && Mathf.Abs(scroll) > 0.01f && ExhibitController.Active &&
                !UIUtils.IsPointerOverUI(position))
                ExhibitController.Active.ScaleBy(scroll > 0f ? mouseWheelZoomStep : 1f / mouseWheelZoomStep);
        }

        // ---------------- Comum ----------------

        void PointerDown(Vector2 position)
        {
            touchStartPos = position;
            touchStartTime = Time.unscaledTime;
            isDragging = false;
            startedOverUI = UIUtils.IsPointerOverUI(position);
        }

        void PointerMoved(Vector2 position, Vector2 delta)
        {
            if (startedOverUI) return;
            if (!isDragging && (position - touchStartPos).sqrMagnitude > tapMaxMovePixels * tapMaxMovePixels)
                isDragging = true;
            if (isDragging && gesturesEnabled && ExhibitController.Active)
                ExhibitController.Active.RotateBy(-delta.x * rotateDegreesPerPixel);
        }

        void PointerUp(Vector2 position)
        {
            if (!startedOverUI && !isDragging && Time.unscaledTime - touchStartTime <= tapMaxDuration)
                HandleTap(position);
        }

        void HandleTap(Vector2 screenPosition)
        {
            if (!arCamera) arCamera = Camera.main;
            if (!arCamera) return;

            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide))
            {
                var hotspot = hit.collider.GetComponentInParent<HotspotView>();
                if (hotspot)
                {
                    HotspotTapped?.Invoke(hotspot);
                    return;
                }

                var exhibit = hit.collider.GetComponentInParent<ExhibitController>();
                if (exhibit)
                {
                    exhibit.Activate();
                    return;
                }
            }

            EmptyTap?.Invoke(screenPosition);
        }
    }
}
