using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseuAR
{
    /// <summary>
    /// Representa uma peça instanciada na cena AR (via placa ou via posicionamento livre).
    /// Cuida do modelo, dos hotspots, de girar/ampliar e de qual peça está "ativa" na interface.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ExhibitController : MonoBehaviour
    {
        /// <summary>Peça atualmente em foco (a que recebe gestos e aparece no cabeçalho).</summary>
        public static ExhibitController Active { get; private set; }
        public static event Action<ExhibitController> ActiveChanged;

        public event Action<HotspotView> HotspotVisited;

        [SerializeField] float minScaleFactor = 0.25f;
        [SerializeField] float maxScaleFactor = 4f;

        public MuseumItemData Data { get; private set; }
        public Transform ModelPivot { get; private set; }
        public IReadOnlyList<HotspotView> Hotspots => hotspots;
        public bool HotspotsVisible { get; private set; } = true;

        public int VisitedCount
        {
            get
            {
                int count = 0;
                foreach (var h in hotspots) if (h && h.Visited) count++;
                return count;
            }
        }

        readonly List<HotspotView> hotspots = new List<HotspotView>();
        float scaleFactor = 1f;
        bool keepUpright;
        Vector3 uprightForward = Vector3.forward;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
            ActiveChanged = null;
        }

        public static ExhibitController Spawn(MuseumItemData data, Transform parent, HotspotStyle style)
        {
            var root = new GameObject($"Exhibit [{data.displayName}]");
            root.transform.SetParent(parent, false);
            var exhibit = root.AddComponent<ExhibitController>();
            exhibit.Build(data, style);
            return exhibit;
        }

        void Build(MuseumItemData data, HotspotStyle style)
        {
            Data = data;

            ModelPivot = new GameObject("ModelPivot").transform;
            ModelPivot.SetParent(transform, false);
            ModelPivot.localPosition = data.spawnOffset;
            ModelPivot.localRotation = Quaternion.Euler(data.spawnRotation);
            ApplyScale();

            if (data.modelPrefab)
            {
                Instantiate(data.modelPrefab, ModelPivot, false);
            }
            else
            {
                Debug.LogWarning($"[Museu AR] '{data.displayName}' está sem modelPrefab. Usando um cubo provisório.");
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.transform.SetParent(ModelPivot, false);
                placeholder.transform.localScale = Vector3.one * 0.1f;
                placeholder.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            }

            foreach (var point in ModelPivot.GetComponentsInChildren<HotspotPoint>(true))
            {
                var view = HotspotView.Create(point, style);
                view.Owner = this;
                hotspots.Add(view);
            }
        }

        // ---------------- Foco ----------------

        public void Activate()
        {
            if (Active == this) return;
            Active = this;
            ActiveChanged?.Invoke(this);
        }

        public static void ClearActive()
        {
            if (Active == null) return;
            Active = null;
            ActiveChanged?.Invoke(null);
        }

        void OnDisable()
        {
            if (Active == this) ClearActive();
        }

        // ---------------- Manipulação ----------------

        /// <summary>Gira a peça em torno do eixo vertical.</summary>
        public void RotateBy(float degrees)
        {
            ModelPivot.Rotate(transform.up, degrees, Space.World);
        }

        /// <summary>Multiplica o zoom atual (ex.: 1.1 = 10% maior).</summary>
        public void ScaleBy(float factor)
        {
            scaleFactor = Mathf.Clamp(scaleFactor * factor, minScaleFactor, maxScaleFactor);
            ApplyScale();
        }

        public void ResetView()
        {
            scaleFactor = 1f;
            ModelPivot.localRotation = Quaternion.Euler(Data.spawnRotation);
            ApplyScale();
        }

        void ApplyScale()
        {
            ModelPivot.localScale = Vector3.one * (Data.initialScale * scaleFactor);
        }

        /// <summary>
        /// Mantém a peça "em pé" (alinhada à gravidade) e virada para onde o visitante estava.
        /// Essencial para placas coladas na parede: sem isso a peça apareceria deitada.
        /// </summary>
        public void EnableUpright(Camera viewer)
        {
            keepUpright = true;
            Vector3 forward = viewer
                ? viewer.transform.position - transform.position
                : transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            uprightForward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            ApplyUpright();
        }

        void LateUpdate()
        {
            if (keepUpright) ApplyUpright();
        }

        void ApplyUpright()
        {
            transform.rotation = Quaternion.LookRotation(uprightForward, Vector3.up);
        }

        // ---------------- Hotspots ----------------

        public void SetHotspotsVisible(bool visible)
        {
            HotspotsVisible = visible;
            foreach (var h in hotspots)
                if (h) h.gameObject.SetActive(visible);
        }

        public void ToggleHotspots() => SetHotspotsVisible(!HotspotsVisible);

        internal void NotifyHotspotVisited(HotspotView view) => HotspotVisited?.Invoke(view);
    }
}
