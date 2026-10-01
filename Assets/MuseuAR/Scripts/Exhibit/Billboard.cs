using UnityEngine;

namespace MuseuAR
{
    /// <summary>Faz o objeto (ex.: rótulo de texto) sempre ficar de frente para a câmera.</summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("Se marcado, só gira no eixo vertical (texto nunca 'deita').")]
        public bool lockVertical = true;

        Camera cam;

        void LateUpdate()
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;

            Vector3 direction = transform.position - cam.transform.position;
            if (lockVertical) direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) return;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
