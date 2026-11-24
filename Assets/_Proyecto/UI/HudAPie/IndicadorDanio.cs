using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1328: marca roja en un anillo alrededor del centro de la pantalla que apunta hacia donde viene el daño (bala o golpe al jugador a pie),
    /// y se desvanece en <see cref="Duracion"/> s. La crea <see cref="HudAPie"/>. Escucha <see cref="EventosPelea.ImpactoRecibido"/> con objetivo null
    /// (así se publica el daño al jugador a pie) y que ocurra junto al jugador.
    /// </summary>
    public class IndicadorDanio : MonoBehaviour
    {
        public const int OrdenCanvas = 16;
        public const float Duracion = 1f;
        private const float Radio = 150f;
        private const float Largo = 70f;
        private const float Grosor = 14f;
        private const float DistanciaMaxima = 3f;
        private const int MaxMarcas = 6;

        private sealed class Marca
        {
            public RectTransform pivote;
            public Image imagen;
            public float nacio = -1f;
            public float grados;
        }

        private readonly List<Marca> marcas = new List<Marca>();
        private Canvas canvas;
        private int siguiente;

        // T-indicador-24nov: destellos rojos en las orillas de la vista (índice = OrillaDanio), con su hora de nacimiento (-1 = apagado).
        private const int ResolucionDegradado = 64;
        private readonly RawImage[] orillas = new RawImage[(int)OrillaDanio.Arriba + 1];
        private readonly float[] nacioOrilla = new float[(int)OrillaDanio.Arriba + 1];
        private readonly float[] alfaOrilla = new float[(int)OrillaDanio.Arriba + 1];
        private ConfigIndicadorDanio config;
        private Texture2D degradadoHorizontal;
        private Texture2D degradadoVertical;

        /// <summary>T-indicador-24nov: la última orilla que destelló (Ninguna si no ha habido).</summary>
        public OrillaDanio UltimaOrilla { get; private set; }

        /// <summary>T-indicador-24nov: opacidad actual del destello de esa orilla (0 si está apagado).</summary>
        public float OpacidadOrilla(OrillaDanio orilla)
        {
            RawImage r = orillas[(int)orilla];
            return r != null && r.gameObject.activeSelf ? r.color.a : 0f;
        }

        /// <summary>Ángulo (°) de la última marca (0 enfrente, negativo a la izquierda), o 0 si no ha habido.</summary>
        public float UltimoAngulo { get; private set; }

        /// <summary>Cuántas marcas se ven ahora.</summary>
        public int MarcasVisibles
        {
            get
            {
                int n = 0;
                foreach (Marca m in marcas)
                {
                    if (m.nacio >= 0f && m.imagen.gameObject.activeSelf)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        /// <summary>Posición en pantalla (px, relativa al centro del canvas) de la marca visible más reciente, o null.</summary>
        public Vector2? PosicionUltimaMarca
        {
            get
            {
                Marca ultima = null;
                foreach (Marca m in marcas)
                {
                    if (m.nacio >= 0f && m.imagen.gameObject.activeSelf && (ultima == null || m.nacio >= ultima.nacio))
                    {
                        ultima = m;
                    }
                }
                if (ultima == null)
                {
                    return null;
                }
                float rad = ultima.grados * Mathf.Deg2Rad;
                return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * Radio;
            }
        }

        private void Awake()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasIndicadorDanio", OrdenCanvas);
            for (int i = 0; i < MaxMarcas; i++)
            {
                GameObject objeto = new GameObject("Marca" + i, typeof(RectTransform));
                objeto.transform.SetParent(canvas.transform, false);
                RectTransform pivote = (RectTransform)objeto.transform;
                pivote.anchorMin = pivote.anchorMax = pivote.pivot = new Vector2(0.5f, 0.5f);
                pivote.anchoredPosition = Vector2.zero;
                Image imagen = KitUI.CrearImagen(pivote, "Barra", new Color(1f, 0.1f, 0.1f, 0f));
                RectTransform r = imagen.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(Largo, Grosor);
                r.anchoredPosition = new Vector2(0f, Radio);
                imagen.gameObject.SetActive(false);
                marcas.Add(new Marca { pivote = pivote, imagen = imagen });
            }
            CrearOrillas();
        }

        // T-indicador-24nov: un degradado (fuerte en la orilla, nada hacia el centro) por lado; los de la derecha y arriba son el mismo volteado.
        private void CrearOrillas()
        {
            config = ConfigIndicadorDanio.Cargar();
            degradadoHorizontal = CrearDegradado(true);
            degradadoVertical = CrearDegradado(false);
            orillas[(int)OrillaDanio.Izquierda] = CrearOrilla("OrillaIzquierda", degradadoHorizontal, new Rect(0f, 0f, 1f, 1f));
            orillas[(int)OrillaDanio.Derecha] = CrearOrilla("OrillaDerecha", degradadoHorizontal, new Rect(1f, 0f, -1f, 1f));
            orillas[(int)OrillaDanio.Atras] = CrearOrilla("OrillaAtras", degradadoVertical, new Rect(0f, 0f, 1f, 1f));
            orillas[(int)OrillaDanio.Arriba] = CrearOrilla("OrillaArriba", degradadoVertical, new Rect(0f, 1f, 1f, -1f));
            for (int i = 0; i < nacioOrilla.Length; i++)
            {
                nacioOrilla[i] = -1f;
            }
        }

        private static Texture2D CrearDegradado(bool horizontal)
        {
            int ancho = horizontal ? ResolucionDegradado : 1;
            int alto = horizontal ? 1 : ResolucionDegradado;
            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
            {
                name = horizontal ? "DegradadoOrillaH" : "DegradadoOrillaV", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            for (int i = 0; i < ResolucionDegradado; i++)
            {
                float t = 1f - i / (float)(ResolucionDegradado - 1);
                textura.SetPixel(horizontal ? i : 0, horizontal ? 0 : i, new Color(1f, 1f, 1f, t * t));
            }
            textura.Apply();
            return textura;
        }

        private RawImage CrearOrilla(string nombre, Texture2D textura, Rect uv)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(canvas.transform, false);
            RawImage imagen = objeto.AddComponent<RawImage>();
            imagen.texture = textura;
            imagen.uvRect = uv;
            imagen.raycastTarget = false;
            Color c = config.Color;
            c.a = 0f;
            imagen.color = c;
            objeto.SetActive(false);
            return imagen;
        }

        // Ajusta el ancho del destello (más angosto en VR) y lo pega a su orilla.
        private void Colocar(OrillaDanio orilla, float ancho)
        {
            RectTransform r = orillas[(int)orilla].rectTransform;
            switch (orilla)
            {
                case OrillaDanio.Izquierda:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(ancho, 1f);
                    break;
                case OrillaDanio.Derecha:
                    r.anchorMin = new Vector2(1f - ancho, 0f);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
                case OrillaDanio.Atras:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(1f, ancho);
                    break;
                default:
                    r.anchorMin = new Vector2(0f, 1f - ancho);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
            }
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        /// <summary>T-indicador-24nov: enciende el destello de esa orilla (Frente y Ninguna no destellan).</summary>
        public void Destellar(OrillaDanio orilla)
        {
            if (orilla == OrillaDanio.Ninguna || orilla == OrillaDanio.Frente || orillas[(int)orilla] == null)
            {
                return;
            }
            bool vr = ModoVR.Activo;
            Colocar(orilla, config.Ancho(vr));
            nacioOrilla[(int)orilla] = Time.unscaledTime;
            alfaOrilla[(int)orilla] = config.Opacidad(vr);
            orillas[(int)orilla].gameObject.SetActive(true);
            UltimaOrilla = orilla;
        }

        private void ApagarOrillas()
        {
            for (int i = 0; i < orillas.Length; i++)
            {
                nacioOrilla[i] = -1f;
                if (orillas[i] != null)
                {
                    orillas[i].gameObject.SetActive(false);
                }
            }
        }

        private void AvanzarOrillas()
        {
            for (int i = 0; i < orillas.Length; i++)
            {
                if (nacioOrilla[i] < 0f || orillas[i] == null)
                {
                    continue;
                }
                float alfa = alfaOrilla[i] * ReglaIndicadorDanio.Alfa(Time.unscaledTime - nacioOrilla[i], config.Duracion);
                if (alfa <= 0f)
                {
                    nacioOrilla[i] = -1f;
                    orillas[i].gameObject.SetActive(false);
                    continue;
                }
                Color c = config.Color;
                c.a = alfa;
                orillas[i].color = c;
            }
        }

        private void OnDestroy()
        {
            if (degradadoHorizontal != null)
            {
                Destroy(degradadoHorizontal);
            }
            if (degradadoVertical != null)
            {
                Destroy(degradadoVertical);
            }
        }

        private void OnEnable()
        {
            EventosPelea.ImpactoRecibido += AlImpacto;
            EventosIndicadorDanio.FuenteMarcada += AlMarcarFuente;
        }

        // T-1528: una fuente señalada sin daño (los drones, con el visor): la misma marca del daño, hacia ese punto.
        private void AlMarcarFuente(Vector3 posicion)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Transform referencia = jugador.Camara != null ? jugador.Camara.transform : jugador.Raiz;
            Vector3 viaja = jugador.Raiz.position - posicion; // hacia donde viajaría el golpe: de la fuente al jugador
            if (ReglaIndicadorDanio.Angulo(referencia.forward, viaja, out float grados))
            {
                Mostrar(grados);
            }
        }

        private void OnDisable()
        {
            EventosPelea.ImpactoRecibido -= AlImpacto;
            EventosIndicadorDanio.FuenteMarcada -= AlMarcarFuente;
            foreach (Marca m in marcas)
            {
                m.nacio = -1f;
                m.imagen.gameObject.SetActive(false);
            }
            ApagarOrillas();
        }

        private void AlImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (objetivo != null || jugador == null || jugador.Raiz == null || info.Cantidad <= 0f)
            {
                return;
            }
            Vector3 relativo = info.Punto - jugador.Raiz.position;
            if (new Vector3(relativo.x, 0f, relativo.z).magnitude > DistanciaMaxima)
            {
                return;
            }
            Transform referencia = jugador.Camara != null ? jugador.Camara.transform : jugador.Raiz;
            // T-indicador-24nov: destello en la orilla hacia donde vino (arriba solo para disparos y explosiones; un golpe o una caída no).
            if (!ReglaIndicadorDanio.SeAlejaDelJugador(info.Punto, jugador.Raiz.position, info.Direccion))
            {
                Destellar(ReglaIndicadorDanio.Orilla(referencia.forward, info.Direccion, info.Tipo != TipoDanio.Golpe, config.GradosArriba,
                    config.GradosFrente, config.GradosAtras));
            }
            if (ReglaIndicadorDanio.Angulo(referencia.forward, info.Direccion, out float grados))
            {
                Mostrar(grados);
                Sonidos.Reproducir("IMP_BALA_ZUMBIDO"); // T-1535: el indicador de daño suena (LEEME 20-nov); el tiempo mínimo del ID evita la ráfaga
            }
        }

        /// <summary>Pone una marca hacia ese ángulo (0 enfrente, 90 derecha).</summary>
        public void Mostrar(float grados)
        {
            Marca m = marcas[siguiente];
            siguiente = (siguiente + 1) % marcas.Count;
            m.nacio = Time.unscaledTime;
            m.grados = grados;
            m.pivote.localRotation = Quaternion.Euler(0f, 0f, -grados);
            m.imagen.color = new Color(1f, 0.1f, 0.1f, 1f);
            m.imagen.gameObject.SetActive(true);
            UltimoAngulo = grados;
        }

        private void Update()
        {
            AvanzarOrillas();
            foreach (Marca m in marcas)
            {
                if (m.nacio < 0f)
                {
                    continue;
                }
                float alfa = ReglaIndicadorDanio.Alfa(Time.unscaledTime - m.nacio, Duracion);
                if (alfa <= 0f)
                {
                    m.nacio = -1f;
                    m.imagen.gameObject.SetActive(false);
                    continue;
                }
                Color c = m.imagen.color;
                c.a = alfa;
                m.imagen.color = c;
            }
        }
    }
}
