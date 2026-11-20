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
            if (ReglaIndicadorDanio.Angulo(referencia.forward, info.Direccion, out float grados))
            {
                Mostrar(grados);
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
