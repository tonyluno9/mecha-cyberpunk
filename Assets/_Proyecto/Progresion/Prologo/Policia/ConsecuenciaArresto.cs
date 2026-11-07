using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Inicio;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Policia
{
    /// <summary>
    /// Lo que pasa después de un arresto (T-690). Va en la raíz del jugador (el doctor de la clínica lo agrega al enlazar al jugador) y recibe el
    /// mensaje de Unity <c>AlSerArrestado</c> que manda el gestor de persecución (Trafico no conoce esta área). Quita el control, muestra la
    /// pantalla ARRESTADO (Teko, neón, glitch), con la pantalla cubierta cobra la multa (25 % del dinero), baja del vehículo si iba en uno,
    /// lo coloca en casa y le devuelve el control al quitarse la pantalla. El calor ya lo borró el gestor.
    /// </summary>
    public class ConsecuenciaArresto : MonoBehaviour
    {
        public const string Titulo = "ARRESTADO";
        public const string IdCasa = "Casa_Puerta";
        public const string IdCasaRespaldo = "Casa_Cama";
        private static readonly Color ColorPolicia = new Color(0.25f, 0.55f, 1f, 1f);

        [Range(0f, 1f)] [SerializeField] private float porcentajeMulta = ReglaMultaArresto.PorcentajePorDefecto;
        [Min(0.05f)] [SerializeField] private float segundosEntrada = 0.8f;
        [Min(0f)] [SerializeField] private float segundosPantalla = 2.4f;
        [Min(0f)] [SerializeField] private float segundosNegro = 0.6f;
        [Min(0.05f)] [SerializeField] private float segundosSalida = 0.9f;

        private bool ocupado;

        /// <summary>La pantalla mientras se ve (null fuera de la secuencia).</summary>
        public PantallaAviso Pantalla { get; private set; }

        /// <summary>True desde el arresto hasta que se devuelve el control.</summary>
        public bool Ocupado => ocupado;

        /// <summary>Lo último que se cobró de multa.</summary>
        public int UltimaMulta { get; private set; }

        public float PorcentajeMulta => porcentajeMulta;

        /// <summary>Para las pruebas: tiempos más cortos.</summary>
        public void ConfigurarTiempos(float entrada, float pantalla, float negro, float salida)
        {
            segundosEntrada = Mathf.Max(0.05f, entrada);
            segundosPantalla = Mathf.Max(0f, pantalla);
            segundosNegro = Mathf.Max(0f, negro);
            segundosSalida = Mathf.Max(0.05f, salida);
        }

        /// <summary>Mensaje de Unity que manda Trafico (GestorPersecucion.MensajeArresto).</summary>
        public void AlSerArrestado()
        {
            if (ocupado || !isActiveAndEnabled)
            {
                return;
            }
            ocupado = true;
            StartCoroutine(Secuencia());
        }

        private void OnDisable()
        {
            if (Pantalla != null)
            {
                Pantalla.Cerrar();
                Pantalla = null;
            }
            ocupado = false;
        }

        private IEnumerator Secuencia()
        {
            ControladorAPie control = GetComponent<ControladorAPie>();
            if (control != null)
            {
                control.ActivarControl(false);
            }
            ICartera cartera = Registro<ICartera>.Actual;
            int multa = ReglaMultaArresto.Multa(cartera != null ? cartera.Dinero : 0, porcentajeMulta);
            string detalle = multa > 0
                ? "MULTA: " + multa + " (" + Mathf.RoundToInt(porcentajeMulta * 100f) + " % DE TU DINERO)"
                : "NO TRAÍAS DINERO: TE SOLTARON";
            PantallaAviso aviso = PantallaAviso.Crear(Titulo, detalle, ColorPolicia);
            Pantalla = aviso;
            yield return aviso.Aparecer(segundosEntrada);
            yield return Esperar(segundosPantalla);

            // Con la pantalla cubierta.
            UltimaMulta = 0;
            if (cartera != null && multa > 0 && cartera.IntentarPagar(multa, MotivoDinero.Otro))
            {
                UltimaMulta = multa;
            }
            JugadorAPie aPie = GetComponent<JugadorAPie>();
            if (aPie != null && aPie.MontableActual != null)
            {
                aPie.Bajar();
            }
            Registro<ICalor>.Actual?.Borrar();
            IPuntoAparicion casa = Buscar(IdCasa) ?? Buscar(IdCasaRespaldo);
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && casa != null)
            {
                jugador.Colocar(casa.Pose);
            }
            else
            {
                Debug.LogWarning("[Progresion] El arresto no encontró '" + IdCasa + "' ni '" + IdCasaRespaldo + "': el jugador se queda donde está.", this);
            }
            yield return null;
            yield return Esperar(segundosNegro);
            if (control != null)
            {
                control.ActivarControl(true);
            }
            ocupado = false;
            yield return aviso.Desvanecer(segundosSalida);
            Pantalla = null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float t = 0f;
            while (t < segundos)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
        }

        private static IPuntoAparicion Buscar(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto != null && punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }
    }
}
