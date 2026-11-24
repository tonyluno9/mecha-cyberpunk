using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-historia-libre-23nov: la pantalla HAS MUERTO del modo libre, con las estadísticas (eliminados de cada bando, tiempo, estrellas máximas, dinero, récord)
    /// y dos opciones: REINTENTAR (justo después del campo de tiro) y MENÚ PRINCIPAL. Su Canvas empieza con "CanvasPantallaAviso" para que en VR vaya como
    /// panel frente a la cabeza, donde el láser usa los botones (ReglaLienzosVR). Se arma por código con el kit del HUD y usa tiempo sin escala.
    /// </summary>
    public class PantallaMuerteLibre : MonoBehaviour
    {
        public const string NombreCanvas = "CanvasPantallaAvisoMuerteLibre";
        public const string TextoTitulo = "HAS MUERTO";
        public const string TextoReintentar = "REINTENTAR";
        public const string TextoMenu = "MENÚ PRINCIPAL";
        private const int OrdenDibujo = 70;
        private static readonly Vector2 TamanoPanel = new Vector2(1000f, 760f);
        private const float AltoTitulo = 150f;
        private const float AltoEstadisticas = 330f;
        private const float SeparacionBotones = 80f;
        private const float AlturaBotones = 70f;
        private const int TamanoTitulo = 120;

        public static PantallaMuerteLibre Actual { get; private set; }

        private Text estadisticas;
        private BotonCyberfix reintentar;
        private bool elegido;

        /// <summary>El texto de estadísticas que se ve (pruebas).</summary>
        public string Estadisticas => estadisticas != null ? estadisticas.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Actual = null;

        public static PantallaMuerteLibre Abrir(DirectorModoLibre director)
        {
            if (Actual != null)
            {
                return Actual;
            }
            PantallaMuerteLibre p = new GameObject("PantallaMuerteLibre").AddComponent<PantallaMuerteLibre>();
            p.Armar(director);
            return p;
        }

        private void Armar(DirectorModoLibre d)
        {
            Actual = this;
            KitUI.AsegurarEventSystem();
            Canvas canvas = KitUI.CrearCanvas(transform, NombreCanvas, OrdenDibujo);
            Image fondo = KitUI.CrearImagen(canvas.transform, "Fondo", new Color(0f, 0f, 0f, 0.75f));
            KitUI.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", TamanoPanel);

            Text titulo = KitUI.CrearTexto(panel, "Titulo", RolTexto.Titulo, TextAnchor.MiddleCenter);
            titulo.text = TextoTitulo;
            titulo.fontSize = TamanoTitulo;
            titulo.color = PantallaAviso.RojoMuerte;
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(TamanoPanel.x, AltoTitulo));

            estadisticas = KitUI.CrearTexto(panel, "Estadisticas", RolTexto.Texto, TextAnchor.UpperCenter);
            estadisticas.supportRichText = true;
            estadisticas.lineSpacing = 1.2f;
            estadisticas.text = TextoEstadisticas(d);
            KitUI.Colocar(estadisticas.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f - AltoTitulo - 10f), new Vector2(TamanoPanel.x - 80f, AltoEstadisticas));

            reintentar = KitUI.CrearBoton(panel, TextoReintentar, AlReintentar);
            BotonCyberfix menu = KitUI.CrearBoton(panel, TextoMenu, AlMenu);
            RectTransform r = (RectTransform)reintentar.transform;
            RectTransform m = (RectTransform)menu.transform;
            KitUI.Colocar(r, new Vector2(0.5f, 0f), new Vector2(0f, AlturaBotones + SeparacionBotones), r.sizeDelta);
            KitUI.Colocar(m, new Vector2(0.5f, 0f), new Vector2(0f, AlturaBotones - 10f), m.sizeDelta);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(reintentar.gameObject);
            }
            LiberarCursor();
            Sonidos.Reproducir(MechaCyberpunk.Progresion.Sonido.ReglaSonidoEventos.Muerte);
        }

        /// <summary>Las líneas de estadísticas (eliminados de cada bando, tiempo, estrellas máximas, dinero y récord).</summary>
        public static string TextoEstadisticas(DirectorModoLibre d)
        {
            if (d == null)
            {
                return string.Empty;
            }
            ICartera cartera = Registro<ICartera>.Actual;
            string record = d.NuevoRecord ? "<color=#FFB000>¡NUEVO RÉCORD!</color>" : "RÉCORD  " + MarcadorModoLibre.FormatoTiempo(DirectorModoLibre.MejorTiempo);
            return "SKULLS ELIMINADOS  " + d.SkullsMuertos
                + "\nPOLICÍAS ELIMINADOS  " + d.PoliciasMuertos
                + "\nNOVA ELIMINADOS  " + d.NovaMuertos
                + "\nTIEMPO  " + MarcadorModoLibre.FormatoTiempo(d.SegundosVivo)
                + "\nESTRELLAS MÁXIMAS  " + d.EstrellasMaximas
                + "\nDINERO  " + FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0)
                + "\n" + record;
        }

        private void Update()
        {
            LiberarCursor(); // otro sistema puede volver a bloquearlo
        }

        private static void LiberarCursor()
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Cursor.visible = true;
        }

        private void AlReintentar()
        {
            if (elegido)
            {
                return;
            }
            elegido = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            DirectorModoLibre.Reintentar();
        }

        private void AlMenu()
        {
            if (elegido)
            {
                return;
            }
            elegido = true;
            DirectorModoLibre.IrAlMenu();
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }
    }
}
