using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using UnityEngine;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>
    /// El contacto del bar (T-186): un <see cref="IInteractuable"/> que de noche abre la <see cref="PantallaMisiones"/> con las misiones de
    /// origen Bar (las ilegales y los robos). Abierto según el <see cref="ILugar"/> Bar (sin ese lugar, abierto). De día solo avisa
    /// "EL CONTACTO LLEGA DE NOCHE" y la E no hace nada. Alan lo coloca en su bar; aquí es un punto con un letrero.
    /// </summary>
    public class ContactoBar : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigPantallaMisiones config;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar
        {
            get
            {
                PantallaMisiones pantalla = Registro<PantallaMisiones>.Actual;
                return pantalla != null && !pantalla.Visible && Decidir(out _);
            }
        }

        public string Texto
        {
            get
            {
                Decidir(out string texto);
                return texto;
            }
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[UI] Falta la config en ContactoBar de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IInteractuable>.Quitar(this);
        }

        public void Usar()
        {
            PantallaMisiones pantalla = Registro<PantallaMisiones>.Actual;
            if (!PuedeUsar)
            {
                return;
            }
            pantalla.Mostrar(config.TituloContacto, OrigenMision.Bar, null, null, false);
        }

        // Decide si se puede usar y qué dice el aviso.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            ILugar lugar = BuscarBar();
            if (lugar != null && !lugar.EstaAbierto)
            {
                texto = config.TextoContactoCerrado;
                return false;
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null && !reloj.EsDeNoche)
            {
                texto = config.TextoContactoDeDia;
                return false;
            }
            texto = config.TextoContacto;
            return true;
        }

        private static ILugar BuscarBar()
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id == IdLugar.Bar)
                {
                    return lugar;
                }
            }
            return null;
        }
    }
}
