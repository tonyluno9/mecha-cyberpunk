using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Opciones;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    public enum PestanaOpciones
    {
        Graficos,
        Audio,
        Controles,
    }

    /// <summary>
    /// Las opciones de la pantalla de inicio, en tres pestañas (GRÁFICOS, AUDIO y CONTROLES) sobre un panel de cristal. Cada fila lee del
    /// <see cref="ServicioOpciones"/> al abrirse y le escribe al instante lo que el jugador cambia (el servicio lo aplica y lo guarda).
    /// Teclado, control y ratón: ↑ ↓ eligen, ← → cambian, Q y E (o LB y RB) cambian de pestaña, Esc o B regresan.
    /// </summary>
    public class PanelOpcionesInicio : MonoBehaviour
    {
        public const string TextoTitulo = "OPCIONES";
        public const string TextoVolver = "VOLVER";
        public const string TextoAyuda = "↑ ↓ ELEGIR  ·  ← → CAMBIAR  ·  Q E PESTAÑA  ·  ESC VOLVER";
        public const string TextoControles =
            "MOVER  W A S D        CORRER  SHIFT        SALTAR  ESPACIO\n"
            + "INTERACTUAR  E        APUNTAR  CLIC DERECHO        DISPARAR  CLIC IZQUIERDO\n"
            + "RECARGAR  R        ARMAS  1 A 5 O RUEDA        MAPA  M        TELÉFONO  T        PAUSA  ESC";
        public static readonly string[] TextosPestanas = { "GRÁFICOS", "AUDIO", "CONTROLES" };
        public static readonly string[] TextosVolumen = { "VOLUMEN GENERAL", "EFECTOS", "VOCES", "AMBIENTE", "MÚSICA", "INTERFAZ" };
        public static readonly string[] TextosCalidad = { "BAJA", "MEDIA", "ALTA" };
        public static readonly string[] TextosApuntado = { "APAGADO", "SUAVE", "FUERTE" };

        private const float AnchoPanel = 1000f;
        private const float AltoPanel = 860f;
        private const float AltoFila = 64f;
        private const float SeparacionFilas = 8f;
        private const float AnchoFila = 900f;
        private const float AnchoPestana = 280f;
        private const float AltoPestana = 66f;
        private const float YPestanas = -140f;
        private const float YFilas = -236f;

        [SerializeField] private BotonInicio[] pestanas = new BotonInicio[0];
        [SerializeField] private RectTransform[] contenidos = new RectTransform[0];
        [SerializeField] private FilaOpcion calidad;
        [SerializeField] private FilaOpcion resolucion;
        [SerializeField] private FilaOpcion pantallaCompleta;
        [SerializeField] private FilaOpcion[] volumenes = new FilaOpcion[0];
        [SerializeField] private FilaOpcion sensibilidad;
        [SerializeField] private FilaOpcion invertirY;
        [SerializeField] private FilaOpcion apuntado;
        [SerializeField] private FilaOpcion tutorial;
        [SerializeField] private BotonInicio volver;

        private ServicioOpciones servicio;
        private PestanaOpciones actual;
        private bool refrescando;

        public PestanaOpciones Pestana => actual;
        public BotonInicio Volver => volver;
        public BotonInicio[] Pestanas => pestanas;
        public FilaOpcion Calidad => calidad;
        public FilaOpcion Resolucion => resolucion;
        public FilaOpcion PantallaCompleta => pantallaCompleta;
        public FilaOpcion[] Volumenes => volumenes;
        public FilaOpcion Sensibilidad => sensibilidad;
        public FilaOpcion InvertirY => invertirY;
        public FilaOpcion Apuntado => apuntado;
        public FilaOpcion Tutorial => tutorial;

        /// <summary>El primer elemento a elegir al abrir (la pestaña activa).</summary>
        public BotonInicio PrimerElegido => pestanas.Length > (int)actual ? pestanas[(int)actual] : volver;

        /// <summary>Se dispara al pulsar VOLVER (el menú decide qué mostrar).</summary>
        public event Action PidioVolver;

        public static PanelOpcionesInicio Crear(Transform padre)
        {
            RectTransform raiz = UIInicio.Nodo(padre, "PanelOpciones");
            UIInicio.Punto(raiz, new Vector2(0.5f, 0.5f), new Vector2(420f, 0f), new Vector2(AnchoPanel, AltoPanel));
            PanelOpcionesInicio panel = raiz.gameObject.AddComponent<PanelOpcionesInicio>();

            Image cristal = UIInicio.Imagen(raiz, "Cristal", UIInicio.PanelFondo, SpritesInicio.Panel);
            cristal.type = Image.Type.Sliced;
            UIInicio.Estirar(cristal.rectTransform);
            Image borde = UIInicio.Imagen(raiz, "Borde", UIInicio.ConAlfa(UIInicio.Cian, 0.75f), SpritesInicio.PanelBorde, true);
            borde.type = Image.Type.Sliced;
            UIInicio.Estirar(borde.rectTransform);

            Text titulo = UIInicio.Texto(raiz, "Titulo", TextoTitulo, UIInicio.FuenteTitulos, 78, UIInicio.Blanco, TextAnchor.MiddleLeft);
            UIInicio.Punto(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(54f, -52f), new Vector2(600f, 90f));
            UIInicio.Halo(titulo, UIInicio.Cian, 0.5f, 3f);
            Image rayaTitulo = UIInicio.Imagen(raiz, "RayaTitulo", UIInicio.ConAlfa(UIInicio.Cian, 0.8f), SpritesInicio.DegradadoH, true);
            UIInicio.Punto(rayaTitulo.rectTransform, new Vector2(0f, 1f), new Vector2(54f, -100f), new Vector2(AnchoPanel - 108f, 3f));

            int cantidad = TextosPestanas.Length;
            panel.pestanas = new BotonInicio[cantidad];
            panel.contenidos = new RectTransform[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                BotonInicio pestana = BotonInicio.Crear(raiz, "Pestana" + i, TextosPestanas[i], new Vector2(AnchoPestana, AltoPestana), 46, TextAnchor.MiddleLeft, UIInicio.Cian);
                UIInicio.Punto(pestana.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(54f + i * (AnchoPestana + 14f), YPestanas), new Vector2(AnchoPestana, AltoPestana));
                PestanaOpciones cual = (PestanaOpciones)i;
                pestana.Elegido += _ => panel.Mostrar(cual);
                pestana.AlPulsar.AddListener(() => panel.Mostrar(cual));
                panel.pestanas[i] = pestana;
                RectTransform contenido = UIInicio.Nodo(raiz, "Contenido" + TextosPestanas[i]);
                UIInicio.Punto(contenido, new Vector2(0.5f, 1f), new Vector2(0f, YFilas), new Vector2(AnchoFila, 520f));
                contenido.pivot = new Vector2(0.5f, 1f);
                panel.contenidos[i] = contenido;
            }

            float y = 0f;
            panel.calidad = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearSelector(f, "Calidad", "CALIDAD", TextosCalidad, (int)CalidadGrafica.Alta, false));
            panel.resolucion = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearSelector(f, "Resolucion", "RESOLUCIÓN", new[] { "1920 x 1080" }, 0, false));
            panel.pantallaCompleta = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearConmutador(f, "PantallaCompleta", "PANTALLA COMPLETA", true));

            y = 0f;
            panel.volumenes = new FilaOpcion[TextosVolumen.Length];
            for (int i = 0; i < TextosVolumen.Length; i++)
            {
                string texto = TextosVolumen[i];
                panel.volumenes[i] = Fila(panel.contenidos[1], ref y, f => FilaOpcion.CrearDeslizador(f, "Volumen" + texto, texto, 0f, 1f, ReglaOpciones.PasoVolumen,
                    ReglaOpciones.VolumenPorDefecto, ReglaInicio.TextoVolumen));
            }

            y = 0f;
            panel.sensibilidad = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearDeslizador(f, "Sensibilidad", "SENSIBILIDAD DEL RATÓN", ReglaOpciones.SensibilidadMinima,
                ReglaOpciones.SensibilidadMaxima, ReglaOpciones.PasoSensibilidad, 1f, v => v.ToString("0.00") + "x"));
            panel.invertirY = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearConmutador(f, "InvertirY", "INVERTIR EJE Y", false));
            panel.apuntado = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearSelector(f, "Apuntado", "APUNTADO ASISTIDO", TextosApuntado, (int)NivelApuntadoAsistido.Suave, true));
            panel.tutorial = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearConmutador(f, "Tutorial", "AVISOS DE TUTORIAL", true));
            Text controles = UIInicio.Texto(panel.contenidos[2], "Controles", TextoControles, UIInicio.FuenteTexto, 27, UIInicio.TextoSuave, TextAnchor.UpperLeft);
            UIInicio.Punto(controles.rectTransform, new Vector2(0f, 1f), new Vector2(34f, y - 22f), new Vector2(AnchoFila - 40f, 130f));
            controles.lineSpacing = 1.35f;
            Image rayaControles = UIInicio.Imagen(panel.contenidos[2], "RayaControles", UIInicio.ConAlfa(UIInicio.Naranja, 0.6f), SpritesInicio.DegradadoH, true);
            UIInicio.Punto(rayaControles.rectTransform, new Vector2(0f, 1f), new Vector2(34f, y - 10f), new Vector2(AnchoFila - 120f, 2f));

            panel.volver = BotonInicio.Crear(raiz, "Volver", TextoVolver, new Vector2(340f, 70f), 52, TextAnchor.MiddleLeft, UIInicio.Naranja);
            UIInicio.Punto(panel.volver.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(54f, 64f), new Vector2(340f, 70f));
            panel.volver.AlPulsar.AddListener(() => panel.PidioVolver?.Invoke());
            Text ayuda = UIInicio.Texto(raiz, "Ayuda", TextoAyuda, UIInicio.FuenteTexto, 24, UIInicio.TextoApagado, TextAnchor.MiddleRight);
            UIInicio.Punto(ayuda.rectTransform, new Vector2(1f, 0f), new Vector2(-54f, 40f), new Vector2(660f, 36f));
            panel.Conectar();
            panel.ConectarNavegacion();
            panel.Mostrar(PestanaOpciones.Graficos);
            return panel;
        }

        private static FilaOpcion Fila(RectTransform padre, ref float y, Func<Transform, FilaOpcion> crear)
        {
            FilaOpcion fila = crear(padre);
            UIInicio.Punto(fila.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y - AltoFila * 0.5f), new Vector2(AnchoFila, AltoFila));
            y -= AltoFila + SeparacionFilas;
            return fila;
        }

        private ServicioOpciones Servicio
        {
            get
            {
                if (servicio == null)
                {
                    servicio = Registro<IOpciones>.Actual as ServicioOpciones;
                }
                return servicio;
            }
        }

        private void Conectar()
        {
            calidad.Cambio += f => { if (!refrescando) Servicio?.FijarCalidad((CalidadGrafica)f.Indice); };
            resolucion.Cambio += AlCambiarResolucion;
            pantallaCompleta.Cambio += f => { if (!refrescando) Servicio?.FijarPantallaCompleta(f.Activo); };
            for (int i = 0; i < volumenes.Length; i++)
            {
                SeccionSonido seccion = (SeccionSonido)i;
                volumenes[i].Cambio += f => { if (!refrescando) Servicio?.FijarVolumen(seccion, f.Valor); };
            }
            sensibilidad.Cambio += f => { if (!refrescando) Servicio?.FijarSensibilidad(f.Valor); };
            invertirY.Cambio += f => { if (!refrescando) Servicio?.FijarInvertirY(f.Activo); };
            apuntado.Cambio += f => { if (!refrescando) Servicio?.FijarApuntadoAsistido((NivelApuntadoAsistido)f.Indice); };
            tutorial.Cambio += f => { if (!refrescando) Servicio?.FijarTutorial(f.Activo); };
        }

        private void OnEnable()
        {
            Refrescar();
        }

        private void Update()
        {
            int paso = 0;
            Keyboard teclado = Keyboard.current;
            Gamepad control = Gamepad.current;
            if ((teclado != null && teclado.eKey.wasPressedThisFrame) || (control != null && control.rightShoulder.wasPressedThisFrame))
            {
                paso = 1;
            }
            else if ((teclado != null && teclado.qKey.wasPressedThisFrame) || (control != null && control.leftShoulder.wasPressedThisFrame))
            {
                paso = -1;
            }
            if (paso != 0)
            {
                CambiarPestana(paso);
            }
        }

        /// <summary>Pasa a la pestaña siguiente (1) o anterior (-1), dando la vuelta.</summary>
        public void CambiarPestana(int paso)
        {
            int nueva = ReglaInicio.MoverIndice((int)actual, paso, pestanas.Length, true);
            Mostrar((PestanaOpciones)nueva);
            pestanas[nueva].ElegirSinSonido();
            Sonidos.Reproducir(BotonInicio.SonidoMover);
        }

        private FilaOpcion[][] FilasPorPestana()
        {
            return new[]
            {
                new[] { calidad, resolucion, pantallaCompleta },
                volumenes,
                new[] { sensibilidad, invertirY, apuntado, tutorial },
            };
        }

        // La navegación automática de Unity falla entre una pestaña de la orilla y las filas anchas (elige una fila lejana): se ata a mano.
        private void ConectarNavegacion()
        {
            FilaOpcion[][] filas = FilasPorPestana();
            for (int t = 0; t < pestanas.Length; t++)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnDown = filas[t][0];
                nav.selectOnLeft = t > 0 ? pestanas[t - 1] : null;
                nav.selectOnRight = t < pestanas.Length - 1 ? pestanas[t + 1] : null;
                pestanas[t].navigation = nav;
                for (int r = 0; r < filas[t].Length; r++)
                {
                    Navigation fila = new Navigation { mode = Navigation.Mode.Explicit };
                    fila.selectOnUp = r == 0 ? (Selectable)pestanas[t] : filas[t][r - 1];
                    fila.selectOnDown = r == filas[t].Length - 1 ? (Selectable)volver : filas[t][r + 1];
                    filas[t][r].navigation = fila;
                }
            }
        }

        public void Mostrar(PestanaOpciones cual)
        {
            actual = cual;
            if (volver != null)
            {
                FilaOpcion[] ultimas = FilasPorPestana()[(int)cual];
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = ultimas[ultimas.Length - 1] };
                volver.navigation = nav;
            }
            for (int i = 0; i < contenidos.Length; i++)
            {
                if (contenidos[i] != null)
                {
                    contenidos[i].gameObject.SetActive(i == (int)cual);
                }
            }
        }

        /// <summary>Pone cada fila con lo que tiene el servicio, sin avisar al servicio.</summary>
        public void Refrescar()
        {
            ServicioOpciones s = Servicio;
            if (s == null || calidad == null)
            {
                return;
            }
            refrescando = true;
            calidad.FijarIndice((int)s.Calidad);
            string[] textos = new string[s.Resoluciones.Count];
            int elegida = 0;
            for (int i = 0; i < textos.Length; i++)
            {
                textos[i] = ReglaInicio.TextoResolucion(s.Resoluciones[i]);
                if (s.Resoluciones[i] == s.Resolucion)
                {
                    elegida = i;
                }
            }
            if (textos.Length > 0)
            {
                resolucion.FijarOpciones(textos, elegida);
            }
            pantallaCompleta.FijarActivo(s.PantallaCompleta);
            for (int i = 0; i < volumenes.Length; i++)
            {
                volumenes[i].FijarValor(s.Volumen((SeccionSonido)i));
            }
            sensibilidad.FijarValor(s.Sensibilidad);
            invertirY.FijarActivo(s.InvertirY);
            apuntado.FijarIndice((int)s.ApuntadoAsistido);
            tutorial.FijarActivo(s.Tutorial);
            refrescando = false;
        }

        private void AlCambiarResolucion(FilaOpcion fila)
        {
            ServicioOpciones s = Servicio;
            if (!refrescando && s != null && fila.Indice >= 0 && fila.Indice < s.Resoluciones.Count)
            {
                s.FijarResolucion(s.Resoluciones[fila.Indice]);
            }
        }
    }
}
