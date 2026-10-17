using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Historia;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Creacion
{
    /// <summary>
    /// Título y creación de personaje (T-126). Con partida guardada muestra CONTINUAR y NUEVA PARTIDA; la creación pide nombre,
    /// alias, cara, ropa e implante viendo el cuerpo en vivo, y EMPEZAR arranca la partida nueva en PeleaTutorial.
    /// Todo con teclado: ↑ ↓ Tab cambian de fila, ← → de opción, Enter confirma. Es modal: lee Enter y Esc por su cuenta.
    /// </summary>
    public class PantallaCreacion : MonoBehaviour
    {
        public enum Panel { Titulo, Confirmar, Creacion }

        public const int FilaNombre = 0;
        public const int FilaAlias = 1;
        public const int FilaCara = 2;
        public const int FilaRopa = 3;
        public const int FilaImplante = 4;
        public const int FilaEmpezar = 5;
        private const int CantidadFilas = 6;

        [SerializeField] private ConfigPantallaCreacion config;
        [SerializeField] private DatosEscenas datosEscenas;
        [SerializeField] private CatalogoApariencia catalogo;
        [SerializeField] private VistaPreviaApariencia vista;
        [SerializeField] private RawImage imagenVista;
        [SerializeField] private Canvas canvas;
        [Header("Título")]
        [SerializeField] private GameObject panelTitulo;
        [SerializeField] private Text textoJuego;
        [SerializeField] private Text textoContinuar;
        [SerializeField] private Text textoNuevaPartida;
        [SerializeField] private Text textoConfirmacion;
        [SerializeField] private Text textoAyudaTitulo;
        [Header("Creación")]
        [SerializeField] private GameObject panelCreacion;
        [SerializeField] private Text textoCrea;
        [SerializeField] private Text[] etiquetas = new Text[CantidadFilas];
        [SerializeField] private Text[] valores = new Text[CantidadFilas];
        [SerializeField] private CampoTextoTeclado campoNombre;
        [SerializeField] private CampoTextoTeclado campoAlias;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;

        private ServicioGuardado guardado;
        private ServicioPerfil perfil;
        private ServicioHistoria historia;
        private Panel panel;
        private int opcionTitulo;
        private int fila;
        private int cara;
        private int ropa;
        private int implante;
        private bool transicionando;

        public Panel PanelActual => panel;
        public int OpcionTitulo => opcionTitulo;
        public int Fila => fila;
        public int Cara => cara;
        public int Ropa => ropa;
        public int Implante => implante;
        public string Nombre => campoNombre.Valor;
        public string Alias => campoAlias.Valor;
        public string Aviso => textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string Confirmacion => textoConfirmacion.gameObject.activeSelf ? textoConfirmacion.text : string.Empty;
        public bool Transicionando => transicionando;
        public Canvas Canvas => canvas;
        public VistaPreviaApariencia Vista => vista;
        public RawImage ImagenVista => imagenVista;

        /// <summary>Texto del valor de una fila de opciones (cara, ropa o implante).</summary>
        public string TextoValor(int indiceFila) => valores[indiceFila].text;

        private void Start()
        {
            guardado = ServicioGuardado.Actual;
            perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            AplicarEstilo();
            if (guardado != null && guardado.HayPartida) MostrarTitulo(); else AbrirCreacion();
        }

        private void OnDisable()
        {
            if (vista != null) vista.Mostrar(false);
        }

        private void AplicarEstilo()
        {
            textoJuego.text = config.TextoJuego;
            textoJuego.fontSize = config.TamanoTitulo;
            textoJuego.color = config.ColorElegido;
            textoContinuar.text = config.TextoContinuar;
            textoNuevaPartida.text = config.TextoNuevaPartida;
            textoConfirmacion.text = config.TextoConfirmar;
            textoConfirmacion.color = config.ColorAviso;
            textoAyudaTitulo.text = config.TextoAyudaTitulo;
            textoCrea.text = config.TextoCrea;
            textoCrea.fontSize = config.TamanoTexto + 12;
            textoCrea.color = config.ColorElegido;
            textoAviso.text = config.TextoAviso;
            textoAviso.color = config.ColorAviso;
            textoAyuda.text = config.TextoAyuda;
            foreach (Text t in new[] { textoContinuar, textoNuevaPartida, textoConfirmacion, textoAviso })
            {
                t.fontSize = config.TamanoTexto;
            }
            textoAyuda.fontSize = config.TamanoAyuda;
            textoAyudaTitulo.fontSize = config.TamanoAyuda;
            textoAyuda.color = config.ColorNormal;
            textoAyudaTitulo.color = config.ColorNormal;
            string[] nombres = { config.TextoNombre, config.TextoAlias, config.TextoCara, config.TextoRopa, config.TextoImplante, config.TextoEmpezar };
            for (int i = 0; i < CantidadFilas; i++)
            {
                etiquetas[i].fontSize = config.TamanoTexto;
                if (valores[i] != null) valores[i].fontSize = config.TamanoTexto;
                etiquetas[i].text = nombres[i];
            }
        }

        private void MostrarTitulo()
        {
            panel = Panel.Titulo;
            opcionTitulo = 0;
            panelTitulo.SetActive(true);
            panelCreacion.SetActive(false);
            vista.Mostrar(false);
            imagenVista.gameObject.SetActive(false);
            campoNombre.Enfocar(false);
            campoAlias.Enfocar(false);
            textoConfirmacion.gameObject.SetActive(false);
            RepintarTitulo();
        }

        private void AbrirCreacion()
        {
            panel = Panel.Creacion;
            panelTitulo.SetActive(false);
            panelCreacion.SetActive(true);
            imagenVista.gameObject.SetActive(true);
            imagenVista.texture = vista.Textura;
            vista.PermitirGiroManual(false);
            vista.Mostrar(true);
            campoNombre.Limpiar();
            campoAlias.Limpiar();
            cara = 0;
            ropa = 0;
            implante = 0;
            textoAviso.gameObject.SetActive(false);
            IrAFila(FilaNombre);
            AplicarCuerpo();
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null || transicionando) return;
            switch (panel)
            {
                case Panel.Titulo:
                    ActualizarTitulo(teclado);
                    break;
                case Panel.Confirmar:
                    ActualizarConfirmar(teclado);
                    break;
                default:
                    ActualizarCreacion(teclado);
                    break;
            }
        }

        private static bool Enter(Keyboard t) => t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame;

        private void ActualizarTitulo(Keyboard teclado)
        {
            if (teclado.upArrowKey.wasPressedThisFrame || teclado.downArrowKey.wasPressedThisFrame)
            {
                opcionTitulo = 1 - opcionTitulo;
                RepintarTitulo();
            }
            else if (Enter(teclado))
            {
                if (opcionTitulo == 0)
                {
                    StartCoroutine(Continuar());
                }
                else
                {
                    panel = Panel.Confirmar;
                    textoConfirmacion.gameObject.SetActive(true);
                }
            }
        }

        private void ActualizarConfirmar(Keyboard teclado)
        {
            if (Enter(teclado))
            {
                if (guardado != null) guardado.Borrar();
                if (historia != null) historia.Reiniciar();
                AbrirCreacion();
            }
            else if (teclado.escapeKey.wasPressedThisFrame)
            {
                MostrarTitulo();
            }
        }

        private void ActualizarCreacion(Keyboard teclado)
        {
            bool retroceder = teclado.upArrowKey.wasPressedThisFrame
                || (teclado.tabKey.wasPressedThisFrame && teclado.shiftKey.isPressed);
            bool avanzar = teclado.downArrowKey.wasPressedThisFrame
                || (teclado.tabKey.wasPressedThisFrame && !teclado.shiftKey.isPressed);
            if (retroceder)
            {
                IrAFila((fila + CantidadFilas - 1) % CantidadFilas);
            }
            else if (avanzar)
            {
                IrAFila((fila + 1) % CantidadFilas);
            }
            else if (teclado.leftArrowKey.wasPressedThisFrame)
            {
                CambiarOpcion(-1);
            }
            else if (teclado.rightArrowKey.wasPressedThisFrame)
            {
                CambiarOpcion(1);
            }
            else if (Enter(teclado))
            {
                if (fila == FilaEmpezar) Empezar(); else IrAFila(fila + 1);
            }
        }

        private void IrAFila(int nueva)
        {
            fila = Mathf.Clamp(nueva, 0, CantidadFilas - 1);
            campoNombre.Enfocar(fila == FilaNombre);
            campoAlias.Enfocar(fila == FilaAlias);
            RepintarCreacion();
        }

        private void CambiarOpcion(int paso)
        {
            switch (fila)
            {
                case FilaCara:
                    cara = CatalogoApariencia.Modulo(cara + paso, catalogo.CantidadCaras);
                    break;
                case FilaRopa:
                    ropa = CatalogoApariencia.Modulo(ropa + paso, catalogo.CantidadRopas);
                    break;
                case FilaImplante:
                    implante = CatalogoApariencia.Modulo(implante + paso, catalogo.CantidadImplantes);
                    break;
                default:
                    return;
            }
            AplicarCuerpo();
        }

        private void AplicarCuerpo()
        {
            vista.Cuerpo.Aplicar(new AparienciaJugador(cara, ropa, implante));
            RepintarCreacion();
        }

        private void RepintarTitulo()
        {
            textoContinuar.color = opcionTitulo == 0 ? config.ColorElegido : config.ColorNormal;
            textoNuevaPartida.color = opcionTitulo == 1 ? config.ColorElegido : config.ColorNormal;
            textoContinuar.text = (opcionTitulo == 0 ? "> " : "  ") + config.TextoContinuar;
            textoNuevaPartida.text = (opcionTitulo == 1 ? "> " : "  ") + config.TextoNuevaPartida;
        }

        private void RepintarCreacion()
        {
            valores[FilaCara].text = "◄ " + catalogo.Cara(cara).nombre + " ►";
            valores[FilaRopa].text = "◄ " + catalogo.Ropa(ropa).nombre + " ►";
            valores[FilaImplante].text = "◄ " + catalogo.Implante(implante).nombre + " ►";
            for (int i = 0; i < CantidadFilas; i++)
            {
                Color color = i == fila ? config.ColorElegido : config.ColorNormal;
                etiquetas[i].color = color;
                if (valores[i] != null) valores[i].color = color;
            }
            campoNombre.Configurar(fila == FilaNombre ? config.ColorElegido : config.ColorNormal, config.TamanoTexto, config.SegundosParpadeo);
            campoAlias.Configurar(fila == FilaAlias ? config.ColorElegido : config.ColorNormal, config.TamanoTexto, config.SegundosParpadeo);
        }

        private void Empezar()
        {
            if (campoNombre.Valor.Trim().Length == 0 || campoAlias.Valor.Trim().Length == 0)
            {
                textoAviso.gameObject.SetActive(true);
                return;
            }
            textoAviso.gameObject.SetActive(false);
            StartCoroutine(EmpezarPartida());
        }

        private IEnumerator EmpezarPartida()
        {
            transicionando = true;
            if (guardado != null) guardado.Borrar();
            if (perfil != null)
            {
                perfil.Fijar(campoNombre.Valor, campoAlias.Valor);
                perfil.FijarApariencia(new AparienciaJugador(cara, ropa, implante));
            }
            if (historia != null)
            {
                historia.Reiniciar();
                historia.Avanzar(EtapaPrologo.PeleaTutorial);
            }
            yield return Fundir();
            CambioEscena.Ir(datosEscenas.RutaPara(EtapaPrologo.PeleaTutorial));
        }

        private IEnumerator Continuar()
        {
            transicionando = true;
            if (guardado == null || !guardado.Cargar())
            {
                transicionando = false;
                AbrirCreacion();
                yield break;
            }
            EtapaPrologo etapa = historia != null ? historia.Etapa : EtapaPrologo.Ninguna;
            if (etapa == EtapaPrologo.Ninguna || etapa == EtapaPrologo.CreacionPersonaje)
            {
                transicionando = false;
                AbrirCreacion();
                yield break;
            }
            string ruta = datosEscenas.RutaPara(etapa);
            yield return Fundir();
            CambioEscena.Ir(ruta);
        }

        private static IEnumerator Fundir()
        {
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null) yield return fundido.Fundir(1f, fundido.Config.SegundosSalida);
        }
    }
}
