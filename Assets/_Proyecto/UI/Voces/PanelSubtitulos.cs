using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Voces
{
    /// <summary>
    /// T-1624 (T15): muestra los subtítulos que publica <see cref="Subtitulos"/> (Core): abajo al centro, letra fina, nombre del que habla en su color y
    /// fondo semitransparente. Solo si el que habla está a 15 m o menos y a la vista, y la opción está encendida. Se crea solo al cargar una escena.
    /// </summary>
    public class PanelSubtitulos : MonoBehaviour
    {
        private const int OrdenCanvas = 45;
        private const int MaximoLineas = 2;
        private const int TamanoLetra = 26;
        private const float AnchoPanel = 1100f;
        private const float MargenAbajo = 150f;
        private const int RellenoHorizontal = 24;
        private const int RellenoVertical = 10;
        private const float AlfaFondo = 0.55f;
        private const float MitadPantalla = 0.5f;
        private static readonly Vector2 ReferenciaCanvas = new Vector2(1920f, 1080f);

        private struct Entrada
        {
            public string Texto;
            public float Fin;
        }

        private static PanelSubtitulos instancia;
        private readonly List<Entrada> activas = new List<Entrada>();
        private readonly StringBuilder armador = new StringBuilder();
        private GameObject panel;
        private Text texto;

        public static PanelSubtitulos Instancia => instancia;
        /// <summary>Texto que se ve ahora (vacío si no hay).</summary>
        public string TextoActual => texto != null && panel != null && panel.activeSelf ? texto.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (instancia != null)
            {
                return;
            }
            GameObject objeto = new GameObject("PanelSubtitulos");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<PanelSubtitulos>();
        }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            instancia = this;
            Armar();
        }

        private void OnEnable() => Subtitulos.Publicada += AlPublicar;

        private void OnDisable() => Subtitulos.Publicada -= AlPublicar;

        private void OnDestroy()
        {
            if (instancia == this)
            {
                instancia = null;
            }
        }

        private void Armar()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OrdenCanvas;
            CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = ReferenciaCanvas;
            escala.matchWidthOrHeight = MitadPantalla;

            panel = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rf = (RectTransform)panel.transform;
            rf.anchorMin = new Vector2(MitadPantalla, 0f);
            rf.anchorMax = new Vector2(MitadPantalla, 0f);
            rf.pivot = new Vector2(MitadPantalla, 0f);
            rf.anchoredPosition = new Vector2(0f, MargenAbajo);
            rf.sizeDelta = new Vector2(AnchoPanel, 0f);
            Image fondo = panel.GetComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, AlfaFondo);
            fondo.raycastTarget = false;
            ContentSizeFitter ajuste = panel.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup capa = panel.AddComponent<VerticalLayoutGroup>();
            capa.padding = new RectOffset(RellenoHorizontal, RellenoHorizontal, RellenoVertical, RellenoVertical);
            capa.childControlWidth = true;
            capa.childControlHeight = true;
            capa.childForceExpandWidth = true;
            capa.childForceExpandHeight = false;

            GameObject t = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            t.transform.SetParent(panel.transform, false);
            texto = t.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = TamanoLetra;
            texto.fontStyle = FontStyle.Normal;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.supportRichText = true;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = Color.white;
            panel.SetActive(false);
        }

        private void AlPublicar(Subtitulos.Linea linea)
        {
            if (string.IsNullOrEmpty(linea.Texto))
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador != null && jugador.Camara != null ? jugador.Camara : Camera.main;
            if (camara == null)
            {
                return;
            }
            Vector3 origen = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : camara.transform.position;
            float distancia = Vector3.Distance(origen, linea.Posicion);
            Vector3 vista = camara.WorldToViewportPoint(linea.Posicion);
            if (!ReglaSubtitulos.DebeVerse(OpcionSubtitulos.Activado, distancia, vista))
            {
                return;
            }
            string hex = ColorUtility.ToHtmlStringRGB(linea.ColorHablante);
            string nombre = string.IsNullOrEmpty(linea.Hablante) ? string.Empty : "<color=#" + hex + "><b>" + linea.Hablante + ":</b></color> ";
            activas.Add(new Entrada { Texto = nombre + linea.Texto, Fin = Time.time + ReglaSubtitulos.Duracion(linea.Duracion, linea.Texto) });
            while (activas.Count > MaximoLineas)
            {
                activas.RemoveAt(0);
            }
            Refrescar();
        }

        private void Update()
        {
            if (activas.Count == 0)
            {
                return;
            }
            bool cambio = false;
            for (int i = activas.Count - 1; i >= 0; i--)
            {
                if (Time.time >= activas[i].Fin)
                {
                    activas.RemoveAt(i);
                    cambio = true;
                }
            }
            if (cambio)
            {
                Refrescar();
            }
        }

        private void Refrescar()
        {
            if (activas.Count == 0)
            {
                panel.SetActive(false);
                return;
            }
            armador.Clear();
            for (int i = 0; i < activas.Count; i++)
            {
                if (i > 0)
                {
                    armador.Append('\n');
                }
                armador.Append(activas[i].Texto);
            }
            texto.text = armador.ToString();
            panel.SetActive(true);
        }
    }
}
