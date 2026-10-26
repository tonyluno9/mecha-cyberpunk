using System;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de habilidades del tablero (T-177): una fila por ranura (tecla, nombre y nivel en el color de la rama, munición) con su
    /// estado (LISTO, barra de enfriamiento con los segundos, SIN MUNICIÓN o —). Con las dos ranuras sin munición avisa "SOLO BRAZOS".
    /// Con el escudo activo el marco de la pantalla y una franja del borde del cristal se ponen turquesa. Está apagada si el mecha no
    /// tiene <see cref="UsoHabilidades"/> o si las dos ranuras están vacías. Las filas se rehacen con <see cref="UsoHabilidades.Cambio"/>;
    /// mientras corre un enfriamiento solo se mueve la barra y el texto de los segundos (cadenas ya hechas: sin basura por cuadro).
    /// </summary>
    public class PantallaHabilidadesCabina : MonoBehaviour
    {
        private const int Filas = 2;
        private const int MaximoSegundos = 99;
        private const float CambioMinimoBarra = 0.001f;

        [Serializable]
        public class FilaHabilidad
        {
            public Text texto;
            public Image fondoBarra;
            public Image relleno;
            public Text estado;
        }

        [SerializeField] private UsoHabilidades uso;
        [SerializeField] private LanzadorHabilidades lanzador;
        [Tooltip("El encendido del mecha: la pantalla se prende con las luces de la cabina. Vacío: siempre prendida.")]
        [SerializeField] private EncendidoMecha encendido;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image marco;
        [SerializeField] private Image fondo;
        [SerializeField] private FilaHabilidad[] filas = new FilaHabilidad[Filas];
        [SerializeField] private Text textoPie;
        [Tooltip("Franja del borde del cristal que se prende con el escudo.")]
        [SerializeField] private Canvas lienzoFranja;
        [SerializeField] private Image[] franjas;
        [SerializeField] private ConfigPantallaHabilidades config;

        private readonly bool[] enfriando = new bool[Filas];
        private readonly int[] segundosPintados = new int[Filas];
        private readonly float[] fraccionPintada = new float[Filas];
        private string[] segundos;
        private bool hayAlguna;
        private bool escudoPintado;

        public Canvas Lienzo => lienzo;
        public bool Visible => lienzo != null && lienzo.enabled;
        public bool FranjaVisible => lienzoFranja != null && lienzoFranja.enabled;
        public Color ColorMarco => marco.color;
        public Color ColorFranja => franjas != null && franjas.Length > 0 ? franjas[0].color : Color.clear;
        public string TextoFila(int fila) => filas[fila].texto.text;
        public string TextoEstado(int fila) => filas[fila].estado.text;
        public Color ColorEstado(int fila) => filas[fila].estado.color;
        public Color ColorFila(int fila) => filas[fila].texto.color;
        public float Barra(int fila) => filas[fila].relleno.rectTransform.anchorMax.x;
        public string TextoPie => textoPie.text;

        private static RanuraPieza Ranura(int fila) => fila == 0 ? RanuraPieza.BrazoIzquierdo : RanuraPieza.Torso;

        private void Awake()
        {
            if (uso == null)
            {
                // Sin UsoHabilidades no hay nada que mostrar: la pantalla se queda apagada.
                if (lienzo != null) lienzo.enabled = false;
                if (lienzoFranja != null) lienzoFranja.enabled = false;
                enabled = false;
                return;
            }
            if (lienzo == null || marco == null || fondo == null || filas == null || filas.Length != Filas || textoPie == null
                || config == null || lanzador == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaHabilidadesCabina de " + name, this);
                enabled = false;
                return;
            }
            segundos = new string[MaximoSegundos + 1];
            for (int i = 0; i < segundos.Length; i++)
            {
                segundos[i] = string.Format(config.FormatoSegundos, i);
            }
            fondo.color = config.ColorFondo;
            for (int i = 0; i < Filas; i++)
            {
                filas[i].fondoBarra.color = config.ColorFondoBarra;
                filas[i].estado.color = config.ColorTextoBarra;
                segundosPintados[i] = -1;
                fraccionPintada[i] = -1f;
            }
        }

        private void OnEnable()
        {
            uso.Cambio += Reconstruir;
            Reconstruir();
        }

        private void OnDisable()
        {
            if (uso != null) uso.Cambio -= Reconstruir;
        }

        private void Update() => Refrescar();

        /// <summary>Lo de cada cuadro: encendido, escudo y barras en marcha. No crea objetos.</summary>
        public void Refrescar()
        {
            bool prendida = encendido == null || encendido.Paso >= PasoEncendido.Paso2;
            if (lienzo.enabled != (prendida && hayAlguna)) lienzo.enabled = prendida && hayAlguna;
            bool escudo = lanzador.EscudoActivo;
            if (escudo != escudoPintado) PintarEscudo(escudo);
            if (lienzoFranja != null && lienzoFranja.enabled != (prendida && escudo)) lienzoFranja.enabled = prendida && escudo;

            for (int i = 0; i < Filas; i++)
            {
                if (!enfriando[i]) continue;
                float restante = uso.EnfriamientoRestante(Ranura(i));
                if (restante <= 0f)
                {
                    Reconstruir();
                    return;
                }
                PintarBarra(i, restante);
            }
        }

        private void PintarEscudo(bool escudo)
        {
            escudoPintado = escudo;
            marco.color = escudo ? config.ColorEscudo : config.ColorMarco;
            if (franjas != null)
            {
                for (int i = 0; i < franjas.Length; i++) franjas[i].color = config.ColorEscudo;
            }
        }

        private void PintarBarra(int fila, float restante)
        {
            float total = uso.EnfriamientoTotal(Ranura(fila));
            float fraccion = total > 0f ? Mathf.Clamp01(restante / total) : 0f;
            if (Mathf.Abs(fraccion - fraccionPintada[fila]) > CambioMinimoBarra)
            {
                fraccionPintada[fila] = fraccion;
                filas[fila].relleno.rectTransform.anchorMax = new Vector2(fraccion, 1f);
            }
            int entero = Mathf.Clamp(Mathf.CeilToInt(restante), 0, MaximoSegundos);
            if (entero != segundosPintados[fila])
            {
                segundosPintados[fila] = entero;
                filas[fila].estado.text = segundos[entero];
            }
        }

        // Rehace las dos filas. Solo corre cuando algo cambia (habilidad, munición, motivo, pelea), no cada cuadro.
        private void Reconstruir()
        {
            bool enPelea = uso.EnPelea;
            int sinMunicion = 0;
            int conHabilidad = 0;
            for (int i = 0; i < Filas; i++)
            {
                RanuraPieza ranura = Ranura(i);
                FilaHabilidad fila = filas[i];
                string tecla = TextoHabilidad.Tecla(ranura);
                DatosHabilidad habilidad = uso.Habilidad(ranura);
                enfriando[i] = false;
                segundosPintados[i] = -1;
                fraccionPintada[i] = -1f;
                fila.relleno.rectTransform.anchorMax = new Vector2(0f, 1f);
                if (habilidad == null)
                {
                    Poner(fila.texto, string.Format(config.FormatoFilaVacia, tecla));
                    fila.texto.color = config.ColorGris;
                    Poner(fila.estado, config.TextoVacia);
                    fila.estado.color = config.ColorGris;
                    continue;
                }

                conHabilidad++;
                int municion = uso.Municion(ranura);
                string etiqueta = TextoHabilidad.Etiqueta(habilidad, uso.Nivel(ranura));
                Poner(fila.texto, municion >= 0
                    ? string.Format(config.FormatoFila, tecla, etiqueta, municion)
                    : string.Format(config.FormatoFilaSinMunicion, tecla, etiqueta));
                fila.texto.color = enPelea ? habilidad.ColorRama : config.ColorGris;

                bool vacia = municion <= 0;
                if (vacia) sinMunicion++;
                float restante = uso.EnfriamientoRestante(ranura);
                if (vacia)
                {
                    Poner(fila.estado, config.TextoSinMunicion);
                    fila.estado.color = enPelea ? config.ColorSinMunicion : config.ColorGris;
                }
                else if (enPelea && restante > 0f)
                {
                    enfriando[i] = true;
                    fila.relleno.color = config.ColorEnfriando;
                    fila.estado.color = config.ColorTextoBarra;
                    PintarBarra(i, restante);
                }
                else
                {
                    Poner(fila.estado, config.TextoListo);
                    fila.estado.color = enPelea ? config.ColorListo : config.ColorGris;
                }
            }

            hayAlguna = conHabilidad > 0;
            Poner(textoPie, conHabilidad == Filas && sinMunicion == Filas ? config.TextoSoloBrazos : string.Empty);
            textoPie.color = enPelea ? config.ColorSinMunicion : config.ColorGris;
            PintarEscudo(lanzador.EscudoActivo);
            bool prendida = encendido == null || encendido.Paso >= PasoEncendido.Paso2;
            lienzo.enabled = prendida && hayAlguna;
        }

        private static void Poner(Text texto, string cadena)
        {
            if (texto.text != cadena) texto.text = cadena;
        }
    }
}
