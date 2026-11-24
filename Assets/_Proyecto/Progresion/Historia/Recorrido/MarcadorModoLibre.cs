using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-historia-libre-23nov: el contador pequeño del modo libre, con el estilo del HUD, debajo del minimapa: SKULLS, POLICÍAS y NOVA eliminados y el
    /// tiempo sobrevivido. Su Canvas se llama "CanvasMarcadorLibre" para que en VR vaya al HUD de casco (ReglaLienzosVR, prefijo "CanvasMarcador").
    /// </summary>
    public class MarcadorModoLibre : MonoBehaviour
    {
        public const string NombreCanvas = "CanvasMarcadorLibre";
        private const int OrdenDibujo = 14;
        private const float AnchoMarcador = DisposicionHud.LadoMinimapa;
        private const float SegundosEntreRefrescos = 0.25f;
        private const int SegundosPorMinuto = 60;

        private DirectorModoLibre director;
        private GameObject raiz;
        private Text bajas;
        private Text tiempo;
        private float proximo;

        /// <summary>Lo que se ve (pruebas y depuración).</summary>
        public string Texto => bajas != null ? bajas.text + " | " + tiempo.text : string.Empty;

        public void Configurar(DirectorModoLibre quien)
        {
            director = quien;
            Canvas canvas = KitUI.CrearCanvas(transform, NombreCanvas, OrdenDibujo);
            raiz = canvas.gameObject;
            RectTransform caja = new GameObject("Marcador", typeof(RectTransform)).GetComponent<RectTransform>();
            caja.SetParent(canvas.transform, false);
            KitUI.Colocar(caja, DisposicionHud.MarcadorLibre, AnchoMarcador);
            Image fondo = KitUI.CrearPanelH10(caja, "Fondo", KitUI.Estilo.Fondo);
            KitUI.Estirar(fondo.rectTransform);
            fondo.raycastTarget = false;
            bajas = KitUI.CrearTexto(caja, "Bajas", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            bajas.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            bajas.rectTransform.anchorMax = new Vector2(1f, 1f);
            tiempo = KitUI.CrearTexto(caja, "Tiempo", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            tiempo.rectTransform.anchorMin = new Vector2(0f, 0f);
            tiempo.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            tiempo.color = KitUI.Estilo.Turquesa;
            Refrescar();
        }

        private void Update()
        {
            if (raiz == null || director == null)
            {
                return;
            }
            bool visible = !FlujoPartida.EnTitulo && !CinematicaEnCurso.Activa && FaseJuego.Actual != FaseJuego.Fase.Muerto;
            if (raiz.activeSelf != visible)
            {
                raiz.SetActive(visible);
            }
            if (visible && Time.unscaledTime >= proximo)
            {
                proximo = Time.unscaledTime + SegundosEntreRefrescos;
                Refrescar();
            }
        }

        private void Refrescar()
        {
            bajas.text = "SKULLS " + director.SkullsMuertos + "   POLICÍAS " + director.PoliciasMuertos + "   NOVA " + director.NovaMuertos;
            tiempo.text = "SOBREVIVIDO " + FormatoTiempo(director.SegundosVivo);
        }

        /// <summary>Segundos como mm:ss (o h:mm:ss pasada la hora).</summary>
        public static string FormatoTiempo(float segundos)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(segundos));
            int horas = total / (SegundosPorMinuto * SegundosPorMinuto);
            int minutos = total / SegundosPorMinuto % SegundosPorMinuto;
            int s = total % SegundosPorMinuto;
            return horas > 0 ? horas + ":" + minutos.ToString("00") + ":" + s.ToString("00") : minutos.ToString("00") + ":" + s.ToString("00");
        }
    }
}
