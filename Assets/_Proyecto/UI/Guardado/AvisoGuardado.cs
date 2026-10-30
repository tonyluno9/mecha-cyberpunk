using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Guardado
{
    /// <summary>
    /// El aviso "GUARDADO" (T-412): abajo a la derecha, en turquesa, durante unos segundos y con el sonido UI_GUARDADO, cada vez que se
    /// escribe una ranura (<see cref="ServicioGuardado.RanuraEscrita"/>: el autoguardado al dormir o al conquistar, y las manuales).
    /// </summary>
    public class AvisoGuardado : MonoBehaviour
    {
        public const string Texto = "GUARDADO";
        public const string Sonido = "UI_GUARDADO";
        public const int OrdenCanvas = 30;
        private const float AnchoTexto = 360f;

        [Tooltip("Cuánto se ve el aviso (s, tiempo real).")]
        [Min(0.1f)] [SerializeField] private float segundos = 2f;
        [SerializeField] private Text texto;

        private float visibleHasta = -1f;

        /// <summary>True mientras se ve "GUARDADO".</summary>
        public bool Visible => texto != null && texto.gameObject.activeSelf;

        public Text TextoAviso => texto;

        /// <summary>Arma su canvas con el kit (lo llama el creador del prefab, o Awake si falta).</summary>
        public void Armar()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAvisoGuardado", OrdenCanvas);
            texto = KitUI.CrearTexto(canvas.transform, "Guardado", RolTexto.Hud, TextAnchor.MiddleRight);
            KitUI.Colocar(texto.rectTransform, DisposicionHud.AvisoGuardado, AnchoTexto);
            texto.text = Texto;
            texto.color = KitUI.Estilo.Turquesa;
            texto.gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (texto == null)
            {
                Armar();
            }
            texto.gameObject.SetActive(false);
        }

        private void OnEnable() => ServicioGuardado.RanuraEscrita += AlEscribir;

        private void OnDisable() => ServicioGuardado.RanuraEscrita -= AlEscribir;

        private void AlEscribir(int ranura) => Mostrar();

        public void Mostrar()
        {
            visibleHasta = Time.unscaledTime + segundos;
            texto.color = KitUI.Estilo.Turquesa;
            texto.gameObject.SetActive(true);
            Sonidos.Reproducir(Sonido);
        }

        private void Update()
        {
            if (Visible && Time.unscaledTime >= visibleHasta)
            {
                texto.gameObject.SetActive(false);
            }
        }
    }
}
