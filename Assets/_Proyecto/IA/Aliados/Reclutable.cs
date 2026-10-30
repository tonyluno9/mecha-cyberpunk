using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// Un chavo del barrio que se une a tu pandilla con E (T-405): pide respeto en una zona y a veces dinero. Con <c>soloHistoria</c>
    /// solo la historia (T-406) lo recluta. Al unirse, su muñeco desaparece y aparece como aliado.
    /// </summary>
    public class Reclutable : MonoBehaviour, IInteractuable
    {
        [SerializeField] private string nombre = "EL CHATO";
        [Min(0)] [SerializeField] private int costo;
        [Tooltip("Respeto mínimo en la zona (puntos).")]
        [Min(0)] [SerializeField] private int respetoMinimo = 10;
        [SerializeField] private string zona = "fosa";
        [SerializeField] private bool soloHistoria;
        [Tooltip("Muñeco que se ve mientras espera.")]
        [SerializeField] private GameObject visual;

        private PandillaJugador pandilla;
        private bool registrado;

        public string Nombre => nombre;
        public bool SoloHistoria { get => soloHistoria; set => soloHistoria = value; }
        public Vector3 Posicion => transform.position;

        /// <summary>True: la historia (T-406) lo libera y se une gratis, sin pedir respeto ni dinero.</summary>
        public bool LiberadoPorHistoria { get; set; }

        public void Configurar(string nuevoNombre, int nuevoCosto, int nuevoRespeto, string nuevaZona)
        {
            nombre = nuevoNombre;
            costo = nuevoCosto;
            respetoMinimo = nuevoRespeto;
            zona = nuevaZona;
        }

        private PandillaJugador Pandilla
        {
            get
            {
                if (pandilla == null)
                {
                    pandilla = Registro<IPandillaJugador>.Actual as PandillaJugador;
                }
                return pandilla;
            }
        }

        private int RespetoActual
        {
            get
            {
                ITerritorio territorio = Registro<ITerritorio>.Actual;
                return territorio != null ? territorio.Respeto(zona) : 0;
            }
        }

        private int DineroActual => Registro<ICartera>.Actual != null ? Registro<ICartera>.Actual.Dinero : 0;

        public string Texto
        {
            get
            {
                if (LiberadoPorHistoria && Pandilla != null && Pandilla.Cantidad < Pandilla.Maximo)
                {
                    return "Hablar con " + nombre;
                }
                if (Pandilla == null || soloHistoria)
                {
                    return nombre + " ESPERA ALGO MÁS";
                }
                if (Pandilla.Cantidad >= Pandilla.Maximo)
                {
                    return "TU PANDILLA ESTÁ LLENA";
                }
                if (RespetoActual < respetoMinimo)
                {
                    ITerritorio territorio = Registro<ITerritorio>.Actual;
                    string lugar = territorio != null ? territorio.Nombre(zona) : zona;
                    return "TE FALTA RESPETO EN " + lugar + ": " + respetoMinimo;
                }
                if (DineroActual < costo)
                {
                    return "TE FALTA DINERO: $" + costo;
                }
                return "Reclutar a " + nombre + (costo > 0 ? " · $" + costo : " · gratis");
            }
        }

        public bool PuedeUsar => Pandilla != null && Pandilla.Cantidad < Pandilla.Maximo
            && (LiberadoPorHistoria || (!soloHistoria && RespetoActual >= respetoMinimo && DineroActual >= costo));

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            if (!LiberadoPorHistoria && costo > 0 && !Registro<ICartera>.Actual.IntentarPagar(costo, MotivoDinero.Otro))
            {
                return;
            }
            Pandilla.Reclutar(nombre);
            Sonidos.ReproducirEn("PAN_ALIADO_RECLUTA", transform.position);
        }

        private void Update()
        {
            bool disponible = Pandilla == null || !Pandilla.Tiene(nombre);
            if (visual != null && visual.activeSelf != disponible)
            {
                visual.SetActive(disponible);
            }
            if (disponible != registrado)
            {
                registrado = disponible;
                if (disponible)
                {
                    Registro<IInteractuable>.Agregar(this);
                }
                else
                {
                    Registro<IInteractuable>.Quitar(this);
                }
            }
        }

        private void OnDisable()
        {
            if (registrado)
            {
                registrado = false;
                Registro<IInteractuable>.Quitar(this);
            }
        }
    }
}
