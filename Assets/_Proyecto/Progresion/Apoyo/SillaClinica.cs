using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Misiones;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// La silla del Doc (T-813): un <see cref="IInteractuable"/>. Herido y con dinero, E te sienta (cobra lo mismo que EL DOC); sano, te sienta
    /// gratis a una revisión (T-864). Congela el control,
    /// te pone en el asiento con <see cref="IJugador.Colocar"/> y una pose de sentado por huesos (<see cref="PoseSillaClinica"/>, sin tocar el
    /// Animator Controller), prende una cámara cercana, baja el brazo robótico (I06) con su luz latiendo, llena una barra de vida mientras te cura
    /// (<see cref="VidaAPie.CurarTodo"/> al final, el mismo modelo que el Curar del Doc), sube el brazo, te levanta, devuelve el control y la cámara
    /// y el Doc dice su frase. EL DOC, herido, ya no cura de palabra: te manda a esta silla (<see cref="Actual"/>).
    /// </summary>
    public class SillaClinica : MonoBehaviour, IInteractuable
    {
        private const int OrdenBarra = 40;
        private const float AnchoBarra = 640f;
        private const float AltoBarra = 22f;
        private const float AbajoBarra = 190f;

        private static SillaClinica actual;

        [SerializeField] private ConfigSillaClinica config;
        [Tooltip("Dónde queda la cadera, mirando hacia donde mira el jugador sentado (su forward).")]
        [SerializeField] private Transform asiento;
        [Tooltip("Dónde se para el jugador al levantarse (y su orientación).")]
        [SerializeField] private Transform salida;
        [Tooltip("El brazo robótico sobre la silla (I06). Opcional.")]
        [SerializeField] private Transform brazo;

        private readonly CongelaControlAPie congela = new CongelaControlAPie();
        private bool registrado;
        private Camera camaraCercana;
        private Light luz;
        private Canvas lienzoBarra;
        private Image rellenoBarra;
        private Text textoBarra;
        private PoseSillaClinica pose;
        private Vector3 brazoArriba;

        /// <summary>La silla de la escena, o null.</summary>
        public static SillaClinica Actual => actual;

        public ConfigSillaClinica Config => config;

        /// <summary>True desde que te sientas hasta que te levantas.</summary>
        public bool EnSecuencia { get; private set; }

        /// <summary>True mientras estás sentado (con la pose puesta).</summary>
        public bool Sentado => pose != null && pose.enabled;

        /// <summary>Lo lleno de la barra de vida (0 a 1).</summary>
        public float Barra { get; private set; }

        /// <summary>La cámara cercana mientras dura la secuencia, o null.</summary>
        public Camera CamaraCercana => camaraCercana;

        /// <summary>Lo que bajó el brazo (m).</summary>
        public float BajadaActual => brazo != null ? brazoArriba.y - brazo.position.y : 0f;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !EnSecuencia && Decidir(out _);

        public string Texto
        {
            get
            {
                if (EnSecuencia)
                {
                    return string.Empty;
                }
                Decidir(out string texto);
                return texto;
            }
        }

        /// <summary>Para el código de editor y las pruebas.</summary>
        public void Configurar(ConfigSillaClinica configuracion, Transform puntoAsiento, Transform puntoSalida, Transform brazoRobot)
        {
            config = configuracion;
            asiento = puntoAsiento;
            salida = puntoSalida;
            brazo = brazoRobot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        private void Awake()
        {
            if (config == null || asiento == null || salida == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en SillaClinica de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            actual = this;
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (actual == this)
            {
                actual = null;
            }
            if (EnSecuencia)
            {
                StopAllCoroutines();
                Terminar();
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            VidaAPie vida = jugador.GetComponentInChildren<VidaAPie>();
            bool sano = vida.Actual >= vida.Maxima;
            if (!sano && !Registro<ICartera>.Actual.IntentarPagar(config.Costo, MotivoDinero.Clinica))
            {
                return;
            }
            StartCoroutine(Secuencia(jugador, vida));
        }

        private IEnumerator Secuencia(JugadorAPie jugador, VidaAPie vida)
        {
            EnSecuencia = true;
            congela.Congelar();
            float inicio = vida.Maxima > 0f ? vida.Actual / vida.Maxima : 1f;
            Barra = inicio;

            // Sentarse: al asiento, con la pose y la cámara cercana.
            jugador.Colocar(new Pose(new Vector3(asiento.position.x, salida.position.y, asiento.position.z), asiento.rotation));
            Animator animador = jugador.GetComponentInChildren<Animator>();
            if (animador != null)
            {
                pose = animador.gameObject.AddComponent<PoseSillaClinica>();
                pose.Configurar(asiento.position + Vector3.up * config.AlturaCadera, asiento.forward, config.Recostado);
                pose.Peso = 0f;
            }
            CrearCamara(jugador);
            CrearBarra();
            CrearLuz();
            yield return Durante(config.SegundosSentarse, t => PonerPeso(t));

            // El brazo baja y la luz late.
            yield return Durante(config.SegundosBajarBrazo, t => MoverBrazo(Mathf.SmoothStep(0f, 1f, t)));

            // La barra se llena; al final la vida queda llena.
            yield return Durante(config.SegundosCurar, t =>
            {
                Barra = Mathf.Lerp(inicio, 1f, t);
                PintarBarra();
            });
            vida.CurarTodo();
            Barra = 1f;
            PintarBarra();

            yield return Durante(config.SegundosSubirBrazo, t => MoverBrazo(1f - Mathf.SmoothStep(0f, 1f, t)));
            yield return Durante(config.SegundosLevantarse, t => PonerPeso(1f - t));
            Terminar();
            Decir(config.FraseFin);
        }

        // Corre una fase de la secuencia: llama a paso con t de 0 a 1 cada cuadro y hace latir la luz.
        private IEnumerator Durante(float segundos, System.Action<float> paso)
        {
            float t = 0f;
            while (t < segundos)
            {
                t += Time.deltaTime;
                paso(Mathf.Clamp01(segundos > 0f ? t / segundos : 1f));
                Latir();
                yield return null;
            }
            paso(1f);
        }

        private void PonerPeso(float t)
        {
            if (pose != null)
            {
                pose.Peso = Mathf.SmoothStep(0f, 1f, t);
            }
        }

        private void MoverBrazo(float t)
        {
            if (brazo != null)
            {
                brazo.position = brazoArriba + Vector3.down * (config.BajadaBrazo * t);
            }
        }

        private void Latir()
        {
            if (luz != null)
            {
                luz.intensity = config.IntensidadLuz * (0.6f + 0.4f * Mathf.Sin(Time.time * config.PulsoLuz * Mathf.PI * 2f));
            }
        }

        // Levantarse: fuera la pose, la cámara, la barra y la luz; el jugador queda junto a la silla y con su control.
        private void Terminar()
        {
            if (pose != null)
            {
                Destroy(pose);
                pose = null;
            }
            if (brazo != null)
            {
                brazo.position = brazoArriba;
            }
            if (camaraCercana != null)
            {
                Destroy(camaraCercana.gameObject);
                camaraCercana = null;
            }
            if (luz != null)
            {
                Destroy(luz.gameObject);
                luz = null;
            }
            if (lienzoBarra != null)
            {
                Destroy(lienzoBarra.gameObject);
                lienzoBarra = null;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && salida != null)
            {
                jugador.Colocar(new Pose(salida.position, salida.rotation));
            }
            congela.Liberar();
            EnSecuencia = false;
        }

        private void CrearCamara(IJugador jugador)
        {
            GameObject go = new GameObject("CamaraSillaClinica");
            go.transform.SetParent(transform, false);
            Vector3 lado = asiento.right * config.PosicionCamara.x + Vector3.up * config.PosicionCamara.y + asiento.forward * config.PosicionCamara.z;
            go.transform.position = asiento.position + lado;
            go.transform.LookAt(asiento.position + Vector3.up * config.AlturaMira);
            camaraCercana = go.AddComponent<Camera>();
            camaraCercana.fieldOfView = config.CampoVision;
            camaraCercana.nearClipPlane = 0.05f;
            Camera delJugador = jugador.Camara;
            camaraCercana.depth = (delJugador != null ? delJugador.depth : 0f) + 1f;
            brazoArriba = brazo != null ? brazo.position : Vector3.zero;
        }

        private void CrearLuz()
        {
            GameObject go = new GameObject("LuzBrazo");
            go.transform.SetParent(brazo != null ? brazo : transform, false);
            go.transform.position = asiento.position + Vector3.up * (config.AlturaMira + config.BajadaBrazo + 0.4f);
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = config.ColorLuz;
            luz.range = config.AlcanceLuz;
            luz.intensity = config.IntensidadLuz;
            luz.shadows = LightShadows.None;
        }

        private void CrearBarra()
        {
            EstiloCyberfix estilo = KitUI.Estilo;
            lienzoBarra = KitUI.CrearCanvas(transform, "BarraSillaClinica", OrdenBarra);
            Image fondo = KitUI.CrearImagen(lienzoBarra.transform, "Fondo", new Color(0f, 0f, 0f, 0.6f));
            KitUI.Colocar(fondo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, AbajoBarra), new Vector2(AnchoBarra + 8f, AltoBarra + 8f));
            GameObject relleno = new GameObject("Relleno", typeof(RectTransform));
            relleno.transform.SetParent(fondo.transform, false);
            rellenoBarra = relleno.AddComponent<Image>();
            rellenoBarra.color = config.ColorBarra;
            RectTransform r = rellenoBarra.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = new Vector2(Barra, 1f);
            r.offsetMin = new Vector2(4f, 4f);
            r.offsetMax = new Vector2(-4f, -4f);
            textoBarra = KitUI.CrearTexto(lienzoBarra.transform, "Texto", RolTexto.Encabezado, TextAnchor.LowerCenter);
            KitUI.Colocar(textoBarra.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, AbajoBarra + AltoBarra + 12f), new Vector2(AnchoBarra, 50f));
            textoBarra.color = estilo.Turquesa;
            PintarBarra();
        }

        private void PintarBarra()
        {
            if (rellenoBarra == null)
            {
                return;
            }
            rellenoBarra.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Barra), 1f);
            textoBarra.text = config.TextoBarra + " · " + Mathf.RoundToInt(Barra * 100f) + "%";
        }

        private void Decir(string texto)
        {
            PantallaHistoriaPandillas pantalla = FindFirstObjectByType<PantallaHistoriaPandillas>();
            if (pantalla == null)
            {
                pantalla = new GameObject("PantallaApoyo").AddComponent<PantallaHistoriaPandillas>();
            }
            pantalla.MostrarSubtitulo(config.NombreDoc + ": " + texto, config.SegundosSubtitulo);
        }

        // Herido y con dinero se ofrece; sano o sin dinero, el aviso dice por qué (sin E).
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (config == null || jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            VidaAPie vida = jugador.GetComponentInChildren<VidaAPie>();
            ICartera cartera = Registro<ICartera>.Actual;
            if (vida == null || cartera == null)
            {
                return false;
            }
            if (vida.Actual >= vida.Maxima)
            {
                // T-864 (Tony): sano también te sientas (el Doc te revisa, sin cobrar).
                texto = config.TextoSano;
                return true;
            }
            if (!cartera.PuedePagar(config.Costo))
            {
                texto = config.TextoSinDinero;
                return false;
            }
            texto = string.Format(config.FormatoAviso, Moneda.Texto(config.Costo));
            return true;
        }
    }
}
