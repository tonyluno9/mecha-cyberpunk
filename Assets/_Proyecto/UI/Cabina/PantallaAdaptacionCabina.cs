using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de adaptación de la cabina (T-203): muestra lo que el rival que tienes enfrente aprendió de ti ("GUARDIA I · TIRAS MUCHOS
    /// PUÑOS"), hace parpadear la fila cuando lo usa y, al cerrar la pelea, dice qué aprendió u olvidó con una pista. Elige el
    /// <see cref="ILecturaAdaptacion"/> registrado más cercano al mecha (se reelige cada <see cref="ConfigPantallaAdaptacion.SegundosReeleccion"/>
    /// y al registrarse o quitarse uno). Se prende con el encendido del mecha. Los textos solo cambian cuando cambia el valor y no se crean
    /// objetos por cuadro: las filas son un grupo fijo que arma el creador.
    /// </summary>
    public class PantallaAdaptacionCabina : MonoBehaviour
    {
        private const int Tipos = 3;
        private const int LineasFin = 3;

        [Tooltip("El encendido del mecha: la pantalla se prende con las luces de la cabina. Vacío: siempre prendida.")]
        [SerializeField] private EncendidoMecha encendido;
        [Tooltip("La cámara de la cabina: de ahí se mide la distancia al rival.")]
        [SerializeField] private Camera camara;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [Tooltip("Una fila por adaptación activa (3).")]
        [SerializeField] private Text[] filas;
        [Tooltip("Líneas APRENDIÓ y OLVIDÓ del final de la pelea (3).")]
        [SerializeField] private Text[] lineasFin;
        [SerializeField] private Text textoPista;
        [SerializeField] private ConfigPantallaAdaptacion config;

        private ILecturaAdaptacion rival;
        private float proximaReeleccion;
        private readonly TipoAdaptacion[] tipoDeFila = new TipoAdaptacion[Tipos];
        private readonly float[] parpadeoHasta = new float[Tipos];
        private readonly bool[] parpadeando = new bool[Tipos];
        private int filasActivas;
        private bool hayFin;
        private float finHasta;
        private bool pendienteRefrescar = true;

        public Canvas Lienzo => lienzo;
        /// <summary>La pantalla se ve.</summary>
        public bool Visible => lienzo != null && lienzo.enabled;
        /// <summary>El rival que se lee ahora, o null.</summary>
        public ILecturaAdaptacion Rival => rival;
        public string TextoTitulo => textoTitulo.text;
        /// <summary>Cuántas filas de adaptación activa se ven.</summary>
        public int FilasVisibles => filasActivas;
        /// <summary>El texto de una fila (vacío si no se usa).</summary>
        public string TextoFila(int indice) => filas[indice].text;
        /// <summary>Se ve el aviso de fin de pelea.</summary>
        public bool FinVisible => hayFin;
        public string TextoFin(int indice) => lineasFin[indice].text;
        public string TextoPista => textoPista.text;
        /// <summary>La fila de esa adaptación está parpadeando.</summary>
        public bool Parpadeando(TipoAdaptacion tipo) => parpadeando[(int)tipo];
        public Color ColorFila(int indice) => filas[indice].color;

        private void Awake()
        {
            if (lienzo == null || fondo == null || textoTitulo == null || textoPista == null || config == null
                || filas == null || filas.Length < Tipos || lineasFin == null || lineasFin.Length < LineasFin)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaAdaptacionCabina de " + name, this);
                enabled = false;
                return;
            }
            if (camara == null)
            {
                Camera[] hijas = transform.root.GetComponentsInChildren<Camera>(true);
                camara = hijas.Length > 0 ? hijas[0] : null;
            }
            if (camara == null)
            {
                Debug.LogError("[UI] PantallaAdaptacionCabina de " + name + " no encuentra la cámara de la cabina.", this);
                enabled = false;
                return;
            }
            fondo.color = config.ColorFondo;
            textoTitulo.color = config.ColorBase;
            textoTitulo.text = config.Titulo;
            textoPista.color = config.ColorPista;
            for (int i = 0; i < Tipos; i++)
            {
                filas[i].color = config.ColorBase;
            }
            for (int i = 0; i < LineasFin; i++)
            {
                lineasFin[i].color = config.ColorFin;
            }
            lienzo.enabled = false;
        }

        private void OnEnable()
        {
            Registro<ILecturaAdaptacion>.Agregado += AlCambiarRegistro;
            Registro<ILecturaAdaptacion>.Quitado += AlCambiarRegistro;
            EventosPelea.PeleaPresentada += AlPresentarse;
            EventosPelea.PeleaIniciada += AlIniciar;
            proximaReeleccion = 0f;
            pendienteRefrescar = true;
        }

        private void OnDisable()
        {
            Registro<ILecturaAdaptacion>.Agregado -= AlCambiarRegistro;
            Registro<ILecturaAdaptacion>.Quitado -= AlCambiarRegistro;
            EventosPelea.PeleaPresentada -= AlPresentarse;
            EventosPelea.PeleaIniciada -= AlIniciar;
            Escuchar(null);
            if (lienzo != null)
            {
                lienzo.enabled = false;
            }
        }

        private void AlCambiarRegistro(ILecturaAdaptacion _) => proximaReeleccion = 0f;

        private void AlPresentarse(InfoPelea _) => BorrarFin();

        private void AlIniciar() => BorrarFin();

        private void Escuchar(ILecturaAdaptacion nuevo)
        {
            if (nuevo == rival)
            {
                return;
            }
            if (rival != null)
            {
                rival.Usada -= AlUsar;
                rival.Aprendio -= AlAprender;
                rival.NivelesCambiados -= AlCambiarNiveles;
            }
            rival = nuevo;
            BorrarFin();
            for (int i = 0; i < Tipos; i++)
            {
                parpadeando[i] = false;
                parpadeoHasta[i] = 0f;
            }
            if (rival != null)
            {
                rival.Usada += AlUsar;
                rival.Aprendio += AlAprender;
                rival.NivelesCambiados += AlCambiarNiveles;
            }
            pendienteRefrescar = true;
        }

        private void AlCambiarNiveles() => pendienteRefrescar = true;

        private void AlUsar(TipoAdaptacion tipo)
        {
            int i = (int)tipo;
            if (i >= 0 && i < Tipos)
            {
                parpadeoHasta[i] = Time.time + config.SegundosParpadeo;
            }
        }

        private void AlAprender(IReadOnlyList<CambioAdaptacion> cambios)
        {
            BorrarFin();
            int lineas = 0;
            bool hayPista = false;
            for (int i = 0; i < cambios.Count && lineas < LineasFin; i++)
            {
                CambioAdaptacion cambio = cambios[i];
                string nombre = config.Nombre(cambio.Tipo);
                if (cambio.Subio)
                {
                    Poner(lineasFin[lineas++], string.Format(config.FormatoAprendio, nombre, config.Nivel(cambio.NivelDespues)));
                    if (!hayPista)
                    {
                        hayPista = true;
                        Poner(textoPista, config.Pista(cambio.Tipo));
                    }
                }
                else if (cambio.Bajo)
                {
                    Poner(lineasFin[lineas++], string.Format(config.FormatoOlvido, nombre));
                }
            }
            hayFin = lineas > 0;
            finHasta = Time.time + config.SegundosAvisoFin;
            pendienteRefrescar = true;
        }

        private void BorrarFin()
        {
            if (hayFin)
            {
                pendienteRefrescar = true;
            }
            hayFin = false;
            for (int i = 0; i < LineasFin; i++)
            {
                Poner(lineasFin[i], string.Empty);
            }
            Poner(textoPista, string.Empty);
        }

        private void LateUpdate() => Refrescar();

        /// <summary>Lo de cada cuadro. No crea objetos.</summary>
        public void Refrescar()
        {
            bool prendida = encendido == null || encendido.Paso >= PasoEncendido.Paso2;
            if (!prendida)
            {
                if (lienzo.enabled)
                {
                    lienzo.enabled = false;
                }
                return;
            }
            if (Time.time >= proximaReeleccion)
            {
                proximaReeleccion = Time.time + config.SegundosReeleccion;
                Escuchar(Elegir());
            }
            if (hayFin && Time.time >= finHasta)
            {
                BorrarFin();
            }
            if (pendienteRefrescar)
            {
                pendienteRefrescar = false;
                PintarFilas();
            }
            ActualizarParpadeo();
            bool visible = rival != null && (filasActivas > 0 || hayFin);
            if (lienzo.enabled != visible)
            {
                lienzo.enabled = visible;
            }
        }

        // El más cercano a la cámara, a la distancia máxima o menos. Descarta los destruidos.
        private ILecturaAdaptacion Elegir()
        {
            IReadOnlyList<ILecturaAdaptacion> todos = Registro<ILecturaAdaptacion>.Todos;
            Vector3 origen = camara.transform.position;
            float mejor = config.DistanciaMaxima * config.DistanciaMaxima;
            ILecturaAdaptacion elegido = null;
            for (int i = 0; i < todos.Count; i++)
            {
                ILecturaAdaptacion candidato = todos[i];
                Transform raiz = candidato.Raiz;
                if (raiz == null || !raiz.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float cuadrada = (raiz.position - origen).sqrMagnitude;
                if (cuadrada <= mejor)
                {
                    mejor = cuadrada;
                    elegido = candidato;
                }
            }
            return elegido;
        }

        private void PintarFilas()
        {
            filasActivas = 0;
            if (rival != null)
            {
                for (int i = 0; i < Tipos; i++)
                {
                    TipoAdaptacion tipo = (TipoAdaptacion)i;
                    int nivel = rival.Nivel(tipo);
                    if (nivel <= 0)
                    {
                        continue;
                    }
                    tipoDeFila[filasActivas] = tipo;
                    Poner(filas[filasActivas], string.Format(config.FormatoFila, config.Nombre(tipo), config.Nivel(nivel), config.Habito(tipo)));
                    filasActivas++;
                }
            }
            for (int i = filasActivas; i < Tipos; i++)
            {
                Poner(filas[i], string.Empty);
            }
        }

        // Colorea la fila de la adaptación usada mientras dure el parpadeo, alternando; solo asigna cuando el color cambia.
        private void ActualizarParpadeo()
        {
            float ahora = Time.time;
            for (int f = 0; f < Tipos; f++)
            {
                bool usada = f < filasActivas;
                int t = usada ? (int)tipoDeFila[f] : -1;
                bool activo = usada && ahora < parpadeoHasta[t];
                bool prendido = activo && Mathf.FloorToInt(ahora * config.ParpadeosPorSegundo) % 2 == 0;
                Color color = prendido ? config.ColorParpadeo : config.ColorBase;
                if (filas[f].color != color)
                {
                    filas[f].color = color;
                }
                if (usada)
                {
                    parpadeando[t] = activo;
                }
            }
        }

        private static void Poner(Text texto, string cadena)
        {
            if (texto.text != cadena)
            {
                texto.text = cadena;
            }
        }
    }
}
