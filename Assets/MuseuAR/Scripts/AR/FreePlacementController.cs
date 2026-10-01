using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MuseuAR
{
    /// <summary>
    /// MODO ACERVO: o visitante escolhe uma peça no catálogo e toca numa superfície (mesa/chão)
    /// para posicioná-la. Ideal para peças da reserva técnica, que não estão expostas.
    /// Tocar em outro lugar reposiciona a peça mantendo zoom, rotação e curiosidades já vistas.
    /// </summary>
    public class FreePlacementController : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] HotspotStyle hotspotStyle;
        [SerializeField] Camera arCamera;
        [Tooltip("Vira a peça de frente para o visitante ao posicioná-la.")]
        [SerializeField] bool faceCamera = true;

        /// <summary>
        /// Modo PC: se nenhuma superfície AR foi detectada, usa uma "mesa virtual" abaixo da câmera.
        /// Ligado pelo DesktopSimulator; no celular fica sempre desligado.
        /// </summary>
        public static bool UseVirtualTable;
        public const float VirtualTableDropMeters = 0.6f;

        public static event Action<MuseumItemData> ItemSelected;
        public static event Action<MuseumItemData> ItemPlaced;

        public MuseumItemData SelectedItem { get; private set; }
        public ExhibitController Current => current;

        ExhibitController current;
        GameObject currentAnchor;
        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ItemSelected = null;
            ItemPlaced = null;
            UseVirtualTable = false;
        }

        void OnEnable() => TouchInputRouter.EmptyTap += OnEmptyTap;
        void OnDisable() => TouchInputRouter.EmptyTap -= OnEmptyTap;

        public void SelectItem(MuseumItemData item)
        {
            ClearPlaced();
            SelectedItem = item;
            ItemSelected?.Invoke(item);
        }

        public void ResetSelection()
        {
            ClearPlaced();
            SelectedItem = null;
        }

        public void ClearPlaced()
        {
            if (currentAnchor) Destroy(currentAnchor);
            currentAnchor = null;
            current = null;
        }

        void OnEmptyTap(Vector2 screenPosition)
        {
            if (!SelectedItem) return;
            var cam = arCamera ? arCamera : Camera.main;

            Pose pose;
            if (raycastManager && raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
                pose = hits[0].pose;
            else if (!TryVirtualTable(cam, screenPosition, out pose))
                return;

            Quaternion rotation = pose.rotation;

            if (faceCamera)
            {
                if (cam)
                {
                    Vector3 toCamera = Vector3.ProjectOnPlane(cam.transform.position - pose.position, pose.up);
                    if (toCamera.sqrMagnitude > 1e-4f)
                        rotation = Quaternion.LookRotation(toCamera.normalized, pose.up);
                }
            }

            // Âncora = a peça fica "presa" no mundo real mesmo quando o tracking se ajusta
            var newAnchor = new GameObject($"Anchor [{SelectedItem.displayName}]");
            newAnchor.transform.SetPositionAndRotation(pose.position, rotation);
            newAnchor.AddComponent<ARAnchor>();

            if (current)
                current.transform.SetParent(newAnchor.transform, false);   // reposiciona mantendo o estado
            else
                current = ExhibitController.Spawn(SelectedItem, newAnchor.transform, hotspotStyle);

            if (currentAnchor) Destroy(currentAnchor);
            currentAnchor = newAnchor;

            current.Activate();
            ItemPlaced?.Invoke(SelectedItem);
        }

        static bool TryVirtualTable(Camera cam, Vector2 screenPosition, out Pose pose)
        {
            pose = default;
            if (!UseVirtualTable || !cam) return false;

            var table = new Plane(Vector3.up, cam.transform.position + Vector3.down * VirtualTableDropMeters);
            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!table.Raycast(ray, out float distance) || distance > 5f) return false;

            pose = new Pose(ray.GetPoint(distance), Quaternion.identity);
            return true;
        }
    }
}
