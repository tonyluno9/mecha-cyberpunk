using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// Un personaje de apoyo de la ciudad (T-417, D-tony-256): un muñeco de prueba con letrero (título y nombre) que el jugador a pie usa con E.
    /// Su servicio sale de <see cref="DatosPersonajeApoyo"/>: abre la tienda de mejoras, cura, da una pista, pinta el carro (borra el calor) o platica.
    /// <see cref="Usado"/> avisa el id (lo escucha la historia). <see cref="DarRegalo"/> deja una acción de una vez que reemplaza al servicio la siguiente vez.
    /// </summary>
    public class PersonajeApoyo : MonoBehaviour, IInteractuable
    {
        private const float DistanciaLetrero = 15f;
        private const float AlturaLetrero = 2.6f;
        private const float SegundosSubtitulo = 5f;
        private const float EscalaLetrero = 0.01f;

        [SerializeField] private DatosPersonajeApoyo datos;
        [Tooltip("Solo EL INVENTOR: la tienda de mejoras que abre.")]
        [SerializeField] private MostradorTienda mostrador;

        private Canvas letrero;
        private int ultimaFrase = -1;
        private Action regalo;
        private bool registrado;

        public DatosPersonajeApoyo Datos => datos;
        public Vector3 Posicion => transform.position;
        public bool TieneRegalo => regalo != null;
        /// <summary>El último subtítulo que dijo.</summary>
        public string UltimoTexto { get; private set; } = string.Empty;

        /// <summary>Se avisa con el id del personaje cada vez que el jugador lo usa.</summary>
        public event Action<string> Usado;

        public void DarRegalo(Action accion)
        {
            regalo = accion;
        }

        private ReglaPersonajeApoyo.Aviso Aviso
        {
            get
            {
                if (regalo != null)
                {
                    return new ReglaPersonajeApoyo.Aviso("HABLAR CON " + datos.Nombre, true);
                }
                VidaAPie vida = VidaDelJugador();
                ICalor calor = Registro<ICalor>.Actual;
                ICartera cartera = Registro<ICartera>.Actual;
                float actual = vida != null ? vida.Actual : 0f;
                float maxima = vida != null ? vida.Maxima : 0f;
                ReglaPersonajeApoyo.Aviso aviso = ReglaPersonajeApoyo.Evaluar(datos.Servicio, datos.Costo, actual, maxima,
                    calor != null ? calor.Nivel : 0, cartera != null ? cartera.Dinero : 0);
                if (datos.Servicio == ServicioApoyo.Curar)
                {
                    // T-430: con la vida llena EL DOC ofrece los implantes (la clínica de I+D+I, que no se ofrece aparte).
                    MostradorClinica clinica = BuscarClinica();
                    aviso = ReglaPersonajeApoyo.DocConImplantes(aviso, actual, maxima, vida != null && clinica != null && clinica.PuedeUsar);
                }
                return aviso;
            }
        }

        private MostradorClinica clinica;
        private float proximaBusquedaClinica;

        // La clínica de I+D+I está en el Barrio; EL DOC la quita de las interacciones sueltas y la abre él.
        private MostradorClinica BuscarClinica()
        {
            if (clinica == null && Time.unscaledTime >= proximaBusquedaClinica)
            {
                proximaBusquedaClinica = Time.unscaledTime + 1f;
                clinica = FindFirstObjectByType<MostradorClinica>(FindObjectsInactive.Exclude);
                if (clinica != null)
                {
                    Registro<IInteractuable>.Quitar(clinica);
                }
            }
            return clinica;
        }

        public string Texto => datos != null ? Aviso.Texto : string.Empty;

        public bool PuedeUsar => datos != null && Aviso.PuedeUsar;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Progresion] Falta DatosPersonajeApoyo en " + name, this);
                enabled = false;
                return;
            }
            letrero = CrearLetrero();
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void Start()
        {
            // La tienda del CIC se abre desde aquí: su propio aviso no se ofrece aparte.
            if (mostrador != null)
            {
                Registro<IInteractuable>.Quitar(mostrador);
            }
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
        }

        private void LateUpdate()
        {
            if (letrero == null)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador != null ? jugador.Camara : null;
            if (camara == null)
            {
                camara = Camera.main;
            }
            bool cerca = camara != null && (camara.transform.position - letrero.transform.position).sqrMagnitude <= DistanciaLetrero * DistanciaLetrero;
            if (letrero.enabled != cerca)
            {
                letrero.enabled = cerca;
            }
            if (cerca)
            {
                Vector3 haciaCamara = camara.transform.position - letrero.transform.position;
                haciaCamara.y = 0f;
                if (haciaCamara.sqrMagnitude > 0.0001f)
                {
                    letrero.transform.rotation = Quaternion.LookRotation(-haciaCamara.normalized, Vector3.up);
                }
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            if (regalo != null)
            {
                Action accion = regalo;
                regalo = null;
                accion();
            }
            else
            {
                switch (datos.Servicio)
                {
                    case ServicioApoyo.Mejoras:
                        if (mostrador != null)
                        {
                            mostrador.Usar();
                        }
                        break;
                    case ServicioApoyo.Curar:
                        if (Aviso.Texto == ReglaPersonajeApoyo.TextoImplantes)
                        {
                            clinica.Usar();
                        }
                        else
                        {
                            Curar();
                        }
                        break;
                    case ServicioApoyo.Informacion:
                        Decir(DarPista());
                        break;
                    case ServicioApoyo.Taller:
                        Pintar();
                        break;
                    default:
                        Platicar();
                        break;
                }
            }
            Usado?.Invoke(datos.Id);
        }

        private void Curar()
        {
            VidaAPie vida = VidaDelJugador();
            if (vida == null || !Registro<ICartera>.Actual.IntentarPagar(datos.Costo, MotivoDinero.Clinica))
            {
                return;
            }
            vida.Restaurar();
            string[] frases = datos.Frases;
            Decir(frases.Length > 0 ? frases[0] : "Ya quedaste.");
        }

        private void Pintar()
        {
            if (!Registro<ICartera>.Actual.IntentarPagar(datos.Costo, MotivoDinero.Otro))
            {
                return;
            }
            Registro<ICalor>.Actual.Borrar();
            string[] frases = datos.Frases;
            Decir(frases.Length > 0 ? frases[0] : "Listo, nadie te conoce.");
        }

        private void Platicar()
        {
            string[] frases = datos.Frases;
            int indice = ReglaPersonajeApoyo.SiguienteFrase(frases.Length, ultimaFrase, UnityEngine.Random.Range(0, Math.Max(1, frases.Length)));
            if (indice < 0)
            {
                return;
            }
            ultimaFrase = indice;
            Decir(frases[indice]);
        }

        private string DarPista()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            string frase = datos.Frases.Length > 0 ? datos.Frases[0] : "Hoy no hay nada.";
            if (territorio == null || jugador == null)
            {
                return frase;
            }
            Vector3 donde = jugador.Raiz.position;
            foreach (JefePandilla jefe in FindObjectsByType<JefePandilla>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (jefe.Vivo)
                {
                    return ReglaPersonajeApoyo.Pista(true, Mathf.RoundToInt(Vector3.Distance(donde, jefe.Jefe.transform.position)),
                        territorio.Nombre(jefe.Zona), string.Empty, 0, 0, false, frase);
                }
            }
            string zona = territorio.ZonaEn(donde);
            return ReglaPersonajeApoyo.Pista(false, 0, string.Empty, zona.Length > 0 ? territorio.Nombre(zona) : string.Empty,
                zona.Length > 0 ? territorio.Respeto(zona) : 0, ReglaTerritorio.UmbralJefe, zona.Length > 0 && territorio.Dueno(zona) == IdPandilla.Jugador, frase);
        }

        private void Decir(string texto)
        {
            UltimoTexto = texto;
            PantallaHistoriaPandillas pantalla = FindFirstObjectByType<PantallaHistoriaPandillas>();
            if (pantalla == null)
            {
                pantalla = new GameObject("PantallaApoyo").AddComponent<PantallaHistoriaPandillas>();
            }
            pantalla.MostrarSubtitulo(datos.Titulo + " · " + datos.Nombre + ": " + texto, SegundosSubtitulo);
        }

        private static VidaAPie VidaDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<VidaAPie>() : null;
        }

        // Letrero sobre la cabeza: título en blanco, nombre en su color y la función chica. Siempre mira a la cámara (LateUpdate).
        private Canvas CrearLetrero()
        {
            GameObject go = new GameObject("Letrero", typeof(Canvas));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, AlturaLetrero, 0f);
            Canvas lienzo = go.GetComponent<Canvas>();
            lienzo.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2.4f, 0.8f) / EscalaLetrero;
            rt.localScale = Vector3.one * EscalaLetrero;
            AgregarLinea(go.transform, "Titulo", datos.Titulo, Color.white, 0.66f, 1f, 30);
            AgregarLinea(go.transform, "Nombre", datos.Nombre, datos.Color, 0.33f, 0.66f, 30);
            AgregarLinea(go.transform, "Funcion", datos.Funcion, new Color(0.8f, 0.8f, 0.8f), 0f, 0.33f, 16);
            return lienzo;
        }

        private static void AgregarLinea(Transform padre, string nombre, string texto, Color color, float anclaMin, float anclaMax, int tamano)
        {
            GameObject go = new GameObject(nombre, typeof(Text));
            go.transform.SetParent(padre, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, anclaMin);
            rt.anchorMax = new Vector2(1f, anclaMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = texto;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.fontSize = tamano;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
        }
    }
}
