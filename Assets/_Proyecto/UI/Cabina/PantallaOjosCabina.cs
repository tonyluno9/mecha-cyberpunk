using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de OJOS de la cabina (T-194): apagada sin el implante. Con OJOS elige, entre los <see cref="ILecturaRival"/> registrados,
    /// al rival mecha vivo a la distancia y ángulo de <see cref="ConfigPantallaOjos"/> más centrado en la vista, y muestra su nombre y su
    /// vida (en números también con el nivel que los trae) y "PRÓXIMO: GOLPE" o "PRÓXIMO: HABILIDAD" cuando está preparando o le falta
    /// menos que la anticipación del implante. Sin rival a la vista dice "SIN OBJETIVO". Se refresca en LateUpdate (después de que los
    /// rivales deciden) y no crea objetos por cuadro: los textos solo cambian cuando cambia el valor.
    /// </summary>
    public class PantallaOjosCabina : MonoBehaviour
    {
        private const float CambioMinimoBarra = 0.001f;
        private const float MinimoDistancia = 0.0001f;

        [Tooltip("El encendido del mecha: la pantalla se prende con las luces de la cabina. Vacío: siempre prendida.")]
        [SerializeField] private EncendidoMecha encendido;
        [Tooltip("La cámara de la cabina: de ahí se mide la distancia y el ángulo al rival.")]
        [SerializeField] private Camera camara;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoNombre;
        [SerializeField] private Image fondoBarra;
        [SerializeField] private Image relleno;
        [SerializeField] private Text textoNumeros;
        [SerializeField] private Text textoAviso;
        [SerializeField] private ConfigPantallaOjos config;

        private EfectosImplantes efectos = EfectosImplantes.Ninguno;
        private IImplantes escuchados;
        private ILecturaRival objetivo;
        private float fraccionPintada = -1f;
        private int actualPintado = -1;
        private int maximaPintada = -1;

        public Canvas Lienzo => lienzo;
        /// <summary>La pantalla está prendida (con OJOS instalados y la cabina encendida).</summary>
        public bool Visible => lienzo != null && lienzo.enabled;
        /// <summary>El rival que se lee ahora, o null.</summary>
        public ILecturaRival Objetivo => objetivo;
        public string TextoNombre => textoNombre.text;
        public string TextoNumeros => textoNumeros.text;
        public string TextoAviso => textoAviso.text;
        /// <summary>Se ve el aviso del próximo ataque.</summary>
        public bool AvisoPrendido => textoAviso.text.Length > 0;
        public bool SinObjetivo => objetivo == null;
        /// <summary>Fracción de la barra de vida (0 a 1).</summary>
        public float Barra => relleno.rectTransform.anchorMax.x;
        public Color ColorBarra => relleno.color;
        public Color ColorAviso => textoAviso.color;

        private void Awake()
        {
            if (lienzo == null || fondo == null || textoNombre == null || fondoBarra == null || relleno == null
                || textoNumeros == null || textoAviso == null || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaOjosCabina de " + name, this);
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
                Debug.LogError("[UI] PantallaOjosCabina de " + name + " no encuentra la cámara de la cabina.", this);
                enabled = false;
                return;
            }
            fondo.color = config.ColorFondo;
            fondoBarra.color = config.ColorFondoBarra;
            relleno.color = config.ColorBarra;
            textoNombre.color = config.ColorTexto;
            textoNumeros.color = config.ColorTexto;
            textoAviso.color = config.ColorAviso;
            lienzo.enabled = false;
        }

        private void OnEnable()
        {
            Registro<IImplantes>.Agregado += AlAgregarImplantes;
            Registro<IImplantes>.Quitado += AlQuitarImplantes;
            Escuchar(Registro<IImplantes>.Actual);
        }

        private void OnDisable()
        {
            Registro<IImplantes>.Agregado -= AlAgregarImplantes;
            Registro<IImplantes>.Quitado -= AlQuitarImplantes;
            if (escuchados != null)
            {
                escuchados.Cambio -= LeerEfectos;
                escuchados = null;
            }
            if (lienzo != null)
            {
                lienzo.enabled = false;
            }
        }

        private void AlAgregarImplantes(IImplantes nuevos) => Escuchar(nuevos);

        private void AlQuitarImplantes(IImplantes quitados)
        {
            if (quitados == escuchados)
            {
                Escuchar(Registro<IImplantes>.Actual);
            }
        }

        private void Escuchar(IImplantes implantes)
        {
            if (escuchados != null)
            {
                escuchados.Cambio -= LeerEfectos;
            }
            escuchados = implantes;
            if (escuchados != null)
            {
                escuchados.Cambio += LeerEfectos;
            }
            LeerEfectos();
        }

        private void LeerEfectos()
        {
            efectos = escuchados != null ? escuchados.Efectos : EfectosImplantes.Ninguno;
            actualPintado = -1;
            maximaPintada = -1;
        }

        private void LateUpdate() => Refrescar();

        /// <summary>Lo de cada cuadro. No crea objetos.</summary>
        public void Refrescar()
        {
            bool prendida = efectos.verVidaRival && (encendido == null || encendido.Paso >= PasoEncendido.Paso2);
            if (lienzo.enabled != prendida)
            {
                lienzo.enabled = prendida;
            }
            if (!prendida)
            {
                objetivo = null;
                return;
            }
            objetivo = Elegir();
            if (objetivo == null)
            {
                PintarSinObjetivo();
                return;
            }
            PintarRival(objetivo);
        }

        // El rival mecha vivo, a la distancia y ángulo permitidos, más centrado en la vista (menor ángulo).
        private ILecturaRival Elegir()
        {
            IReadOnlyList<ILecturaRival> todos = Registro<ILecturaRival>.Todos;
            Transform ojo = camara.transform;
            Vector3 origen = ojo.position;
            Vector3 adelante = ojo.forward;
            float mejorAngulo = config.AnguloMaximo;
            ILecturaRival mejor = null;
            for (int i = 0; i < todos.Count; i++)
            {
                ILecturaRival candidato = todos[i];
                if (!candidato.EsMecha || candidato.Raiz == null || !candidato.Raiz.gameObject.activeInHierarchy
                    || candidato.Vida == null || candidato.Vida.EstaKO)
                {
                    continue;
                }
                Vector3 hacia = candidato.Raiz.position - origen;
                float distancia = hacia.magnitude;
                if (distancia > config.DistanciaMaxima || distancia < MinimoDistancia)
                {
                    continue;
                }
                float angulo = Vector3.Angle(adelante, hacia);
                if (angulo <= mejorAngulo)
                {
                    mejorAngulo = angulo;
                    mejor = candidato;
                }
            }
            return mejor;
        }

        private void PintarSinObjetivo()
        {
            Poner(textoNombre, config.TextoSinObjetivo);
            textoNombre.color = config.ColorSinObjetivo;
            Poner(textoNumeros, string.Empty);
            Poner(textoAviso, string.Empty);
            PintarBarra(0f);
            actualPintado = -1;
            maximaPintada = -1;
        }

        private void PintarRival(ILecturaRival rival)
        {
            Poner(textoNombre, rival.Nombre);
            textoNombre.color = config.ColorTexto;
            IVida vida = rival.Vida;
            PintarBarra(vida.Maxima > 0f ? Mathf.Clamp01(vida.Actual / vida.Maxima) : 0f);
            if (efectos.vidaRivalEnNumeros)
            {
                int actual = Mathf.CeilToInt(vida.Actual);
                int maxima = Mathf.CeilToInt(vida.Maxima);
                if (actual != actualPintado || maxima != maximaPintada)
                {
                    actualPintado = actual;
                    maximaPintada = maxima;
                    textoNumeros.text = string.Format(config.FormatoNumeros, actual, maxima);
                }
            }
            else
            {
                Poner(textoNumeros, string.Empty);
            }

            float segundos = rival.SegundosParaAtacar;
            bool aviso = rival.Preparando || (segundos >= 0f && segundos <= efectos.anticipacionAtaque);
            if (!aviso)
            {
                Poner(textoAviso, string.Empty);
                return;
            }
            Poner(textoAviso, rival.Proximo == TipoAtaqueLeido.Habilidad ? config.TextoProximoHabilidad : config.TextoProximoGolpe);
        }

        private void PintarBarra(float fraccion)
        {
            if (Mathf.Abs(fraccion - fraccionPintada) > CambioMinimoBarra)
            {
                fraccionPintada = fraccion;
                relleno.rectTransform.anchorMax = new Vector2(fraccion, 1f);
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
