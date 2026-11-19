using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// T-1462: apaga fondos lejanos (paneles pintados, bruma de altura, skyline) cuando la cámara sube por encima de un umbral:
    /// vistos desde arriba se ven como placas planas o manchas blancas. Solo apaga el renderer, no el objeto.
    /// </summary>
    public class OcultarPorAltura : MonoBehaviour
    {
        [Tooltip("Renderers que se apagan por altura.")]
        [SerializeField] private Renderer[] renderers;

        [Tooltip("Altura de la cámara sobre el piso (m) a partir de la cual se apagan.")]
        [Min(0f)]
        [SerializeField] private float umbral = 25f;

        [Tooltip("Margen (m) por debajo del umbral para volver a encenderlos.")]
        [Min(0f)]
        [SerializeField] private float histeresis = 3f;

        [Tooltip("Altura del piso del mundo (m).")]
        [SerializeField] private float alturaPiso;

        [Tooltip("Si es verdad, se invierte: los renderers solo se ven con la cámara por encima del umbral (fondos que existen para las vistas aéreas).")]
        [SerializeField] private bool soloArriba;

        private Camera camara;
        private bool oculto;

        public bool Oculto => oculto;
        public float Umbral => umbral;
        public int Cantidad => renderers == null ? 0 : renderers.Length;

        private void Update()
        {
            if (camara == null)
            {
                camara = Camera.main;
            }
            if (camara != null)
            {
                Aplicar(camara.transform.position.y);
            }
        }

        private void OnDisable()
        {
            Aplicar(float.NegativeInfinity);
        }

        /// <summary>Pone el estado para una cámara a esa altura absoluta (m); con <see cref="float.NegativeInfinity"/> restaura todo visible. Las fotos del editor la llaman a mano.</summary>
        public void Aplicar(float alturaCamara)
        {
            bool nuevo;
            if (float.IsNegativeInfinity(alturaCamara))
            {
                nuevo = false; // restaurar: todo visible
            }
            else if (soloArriba)
            {
                nuevo = CalculoOcultarAltura.OcultoPorDebajo(oculto, alturaCamara - alturaPiso, umbral, histeresis);
            }
            else
            {
                nuevo = CalculoOcultarAltura.Oculto(oculto, alturaCamara - alturaPiso, umbral, histeresis);
            }
            if (nuevo == oculto)
            {
                return;
            }
            oculto = nuevo;
            if (renderers == null)
            {
                return;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.enabled = !oculto;
                }
            }
        }

        /// <summary>Aplica la altura a todos los componentes de la escena (editor: fotos y validación).</summary>
        public static void AplicarATodos(float alturaCamara)
        {
            foreach (OcultarPorAltura o in FindObjectsByType<OcultarPorAltura>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                o.Aplicar(alturaCamara);
            }
        }
    }
}
