using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MuseuAR
{
    /// <summary>
    /// MODO VISITA: quando a câmera reconhece a placa AR de uma peça, faz a peça 3D aparecer sobre ela.
    /// O nome da imagem na Reference Image Library deve ser igual ao campo referenceImageName da peça.
    /// (Escrito para AR Foundation 6.x — usa o evento trackablesChanged.)
    /// </summary>
    public class ImageTrackingSpawner : MonoBehaviour
    {
        [SerializeField] ARTrackedImageManager imageManager;
        [SerializeField] MuseumCatalog catalog;
        [SerializeField] HotspotStyle hotspotStyle;

        [Tooltip("Mantém a peça em pé (gravidade) e virada para o visitante. Recomendado para placas na parede.")]
        [SerializeField] bool keepUpright = true;

        [Tooltip("ARCore/ARKit reportam 'Limited' quando a placa sai do enquadramento. " +
                 "Desmarcado = a peça continua visível mesmo sem ver a placa (melhor para girar e olhar de perto).")]
        [SerializeField] bool hideWhenLimited = false;

        /// <summary>Disparado quando uma placa começa a ser rastreada.</summary>
        public static event Action<MuseumItemData> ItemRecognized;

        class Entry
        {
            public ARTrackedImage image;
            public ExhibitController exhibit;
            public bool wasTracking;
        }

        readonly Dictionary<TrackableId, Entry> entries = new Dictionary<TrackableId, Entry>();
        readonly Dictionary<MuseumItemData, ExhibitController> simulated = new Dictionary<MuseumItemData, ExhibitController>();
        bool exhibitsAllowed = true;

        public MuseumCatalog Catalog => catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ItemRecognized = null;

        void Reset()
        {
            imageManager = FindAnyObjectByType<ARTrackedImageManager>();
        }

        void OnEnable()
        {
            if (!imageManager)
            {
                Debug.LogError("[Museu AR] ImageTrackingSpawner sem ARTrackedImageManager.");
                return;
            }
            imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        void OnDisable()
        {
            if (imageManager) imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var image in args.added) HandleImage(image);
            foreach (var image in args.updated) HandleImage(image);

            foreach (var removed in args.removed)
            {
                if (!entries.TryGetValue(removed.Key, out var entry)) continue;
                if (entry.exhibit) Destroy(entry.exhibit.gameObject);
                entries.Remove(removed.Key);
            }
        }

        void HandleImage(ARTrackedImage image)
        {
            if (!entries.TryGetValue(image.trackableId, out var entry))
            {
                entry = new Entry { image = image };
                entries[image.trackableId] = entry;

                string imageName = image.referenceImage.name;
                var data = catalog ? catalog.FindByImageName(imageName) : null;
                if (!data)
                {
                    if (string.IsNullOrEmpty(imageName))
                        Debug.Log("[Museu AR] Imagem fora da biblioteca PlacasAR ignorada (normal no XR Simulation, cujo ambiente tem imagens próprias).");
                    else
                        Debug.LogWarning($"[Museu AR] Placa '{imageName}' reconhecida, mas nenhuma peça do catálogo usa esse referenceImageName.");
                    return;
                }

                entry.exhibit = ExhibitController.Spawn(data, image.transform, hotspotStyle);
                if (keepUpright) entry.exhibit.EnableUpright(Camera.main);
            }

            if (!entry.exhibit) return;

            bool tracking = image.trackingState == TrackingState.Tracking;
            bool visible = IsVisible(image);

            if (entry.exhibit.gameObject.activeSelf != visible)
                entry.exhibit.gameObject.SetActive(visible);

            // Acabou de ser (re)encontrada -> vira a peça em foco
            if (tracking && !entry.wasTracking && visible)
            {
                entry.exhibit.Activate();
                ItemRecognized?.Invoke(entry.exhibit.Data);
            }
            entry.wasTracking = tracking;
        }

        bool IsVisible(ARTrackedImage image)
        {
            if (!exhibitsAllowed || !image) return false;
            switch (image.trackingState)
            {
                case TrackingState.Tracking: return true;
                case TrackingState.Limited: return !hideWhenLimited;
                default: return false;
            }
        }

        /// <summary>Usado na troca de modo: esconde/mostra todas as peças das placas.</summary>
        public void SetExhibitsVisible(bool allowed)
        {
            exhibitsAllowed = allowed;
            foreach (var entry in entries.Values)
            {
                if (!entry.exhibit) continue;
                entry.exhibit.gameObject.SetActive(IsVisible(entry.image));
                if (!allowed) entry.wasTracking = false;
            }
            foreach (var exhibit in simulated.Values)
                if (exhibit) exhibit.gameObject.SetActive(allowed);
        }

        public void ClearAll()
        {
            foreach (var entry in entries.Values)
                if (entry.exhibit) Destroy(entry.exhibit.gameObject);
            entries.Clear();
            foreach (var exhibit in simulated.Values)
                if (exhibit) Destroy(exhibit.transform.parent.gameObject);
            simulated.Clear();
        }

        /// <summary>
        /// Modo PC: faz de conta que a câmera reconheceu a placa da peça, colocando-a à frente do visitante.
        /// Chamar de novo para a mesma peça a traz de volta para a frente da câmera.
        /// </summary>
        public void SimulateRecognition(MuseumItemData data, Camera viewer)
        {
            if (!data || !viewer || !exhibitsAllowed) return;

            Vector3 forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            Vector3 position = viewer.transform.position + forward.normalized * 0.9f + Vector3.down * 0.3f;

            if (!simulated.TryGetValue(data, out var exhibit) || !exhibit)
            {
                var plate = new GameObject($"Placa simulada [{data.displayName}]");
                exhibit = ExhibitController.Spawn(data, plate.transform, hotspotStyle);
                simulated[data] = exhibit;
            }

            // Como no museu: uma placa por vez na frente da câmera
            foreach (var other in simulated.Values)
                if (other && other != exhibit) other.gameObject.SetActive(false);

            exhibit.transform.parent.position = position;
            exhibit.gameObject.SetActive(true);
            exhibit.EnableUpright(viewer);
            exhibit.Activate();
            ItemRecognized?.Invoke(data);
        }
    }
}
