using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR52: en VR el arma ya no se recarga sola (<see cref="PistolaAPie.RecargaAutomatica"/>). Con el cargador vacío (y metido) aparece junto al
    /// arma en la mano un aviso discreto: <see cref="TextoRecargar"/> si hay balas de reserva, <see cref="TextoSinBalas"/> si no. El gatillo hace el
    /// clic vacío (PistolaAPie / ArmasEspecialesVR). Se recarga con A y el gesto (<see cref="RecargaManualVR"/>). Texto uGUI en un Canvas World
    /// Space colgado del arma, de cara a la cabeza. Lo agrega <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(130)]
    public class AvisoRecargaVR : MonoBehaviour
    {
        public const string TextoRecargar = "A: RECARGAR";
        public const string TextoSinBalas = "SIN BALAS";

        /// <summary>Dónde va el aviso respecto a la empuñadura del arma (m, espacio del arma).</summary>
        public static readonly Vector3 PosicionEnArma = new Vector3(0f, 0.09f, 0.02f);
        /// <summary>Alto de la letra (m).</summary>
        public const float AltoLetra = 0.012f;
        private const int TamanoLetra = 40;
        private const float AnchoUnidades = 360f;
        private const float AltoUnidades = 60f;
        private const float Alfa = 0.85f;

        private AdaptadorVR adaptador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private PistolaVR pistolaVR;
        private RecargaManualVR recarga;
        private Canvas lienzo;
        private Text texto;

        /// <summary>True si el aviso se ve.</summary>
        public bool Visible => lienzo != null && lienzo.gameObject.activeSelf;

        /// <summary>Lo que dice el aviso (vacío si no hay).</summary>
        public string Texto => Visible ? texto.text : string.Empty;

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            pistolaVR = GetComponent<PistolaVR>();
            recarga = GetComponent<RecargaManualVR>();
            if (adaptador == null || pistola == null || armas == null || pistolaVR == null)
            {
                Debug.LogWarning("[VR] AvisoRecargaVR sin AdaptadorVR, PistolaAPie, ArmasAPie o PistolaVR en " + name + ".", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Mostrar(false);
        }

        private void OnDestroy()
        {
            if (lienzo != null)
            {
                Destroy(lienzo.gameObject);
            }
        }

        private void LateUpdate()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            ArmaAPie activa = armas.ArmaActiva;
            Transform arma = pistolaVR.Arma;
            bool conCargador = PistolaAPie.EsDeFuego(activa) && activa != ArmaAPie.Granada && pistola.Cargador != null;
            bool metido = recarga == null || recarga.Estado == EstadoCargadorVR.EnArma;
            bool ver = adaptador.Armado && !MenusVR.MenuAbierto && !Pausa.Activa && conCargador && metido && arma != null && pistolaVR.ArmaVisible
                && ReglaRecargaManualVR.MostrarAviso(pistola.Balas, pistola.Recargando);
            if (!ver)
            {
                Mostrar(false);
                return;
            }
            Asegurar();
            if (lienzo.transform.parent != arma)
            {
                lienzo.transform.SetParent(arma, false);
            }
            lienzo.transform.localPosition = PosicionEnArma;
            Camera cabeza = adaptador.Camara;
            if (cabeza != null)
            {
                // De cara a la cabeza (el Canvas se lee desde su -Z).
                Vector3 hacia = lienzo.transform.position - cabeza.transform.position;
                if (hacia.sqrMagnitude > 1e-6f)
                {
                    lienzo.transform.rotation = Quaternion.LookRotation(hacia, cabeza.transform.up);
                }
            }
            texto.text = pistola.Reserva != 0 ? TextoRecargar : TextoSinBalas;
            Mostrar(true);
        }

        private void Mostrar(bool ver)
        {
            if (lienzo != null && lienzo.gameObject.activeSelf != ver)
            {
                lienzo.gameObject.SetActive(ver);
            }
        }

        private void Asegurar()
        {
            if (lienzo != null)
            {
                return;
            }
            var raiz = new GameObject("AvisoRecargaVR", typeof(RectTransform));
            raiz.SetActive(false);
            raiz.AddComponent<IgnorarOcultarVR>();
            lienzo = raiz.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)raiz.transform;
            rt.sizeDelta = new Vector2(AnchoUnidades, AltoUnidades);
            rt.localScale = Vector3.one * (AltoLetra / TamanoLetra);

            var objetoTexto = new GameObject("Texto", typeof(RectTransform));
            objetoTexto.transform.SetParent(raiz.transform, false);
            var rtTexto = (RectTransform)objetoTexto.transform;
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = Vector2.zero;
            rtTexto.offsetMax = Vector2.zero;
            texto = objetoTexto.AddComponent<Text>();
            EstiloCyberfix estilo = KitUI.Estilo;
            Font fuente = estilo != null ? estilo.Fuente : null;
            texto.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>(EstiloCyberfix.NombreFuenteIntegrada);
            texto.fontSize = TamanoLetra;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            Color color = estilo != null ? estilo.Naranja : new Color(1f, 0.502f, 0.102f, 1f);
            color.a = Alfa;
            texto.color = color;
        }
    }
}
