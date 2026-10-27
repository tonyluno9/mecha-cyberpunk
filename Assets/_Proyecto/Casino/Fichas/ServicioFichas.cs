using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Casino.Fichas
{
    /// <summary>
    /// Las fichas del jugador en la escena (T-302): envuelve <see cref="CarteraFichas"/>, se registra como <see cref="IFichas"/> y se guarda
    /// como <see cref="IGuardable"/> con clave "fichas". Va en ServicioFichas.prefab, en una escena que siempre esté cargada (como la cartera).
    /// </summary>
    public class ServicioFichas : MonoBehaviour, IFichas, IGuardable
    {
        public const string Clave = "fichas";

        [Serializable]
        private class DatosGuardados
        {
            public int fichas;
        }

        [SerializeField] private DatosFichas datos;

        private readonly CarteraFichas cartera = new CarteraFichas();

        public DatosFichas Datos => datos;
        public int Saldo => cartera.Saldo;
        public string ClaveGuardado => Clave;

        /// <summary>Se avisa tras cada cambio con la diferencia (negativa al gastar).</summary>
        public event Action<int> Cambio
        {
            add => cartera.Cambio += value;
            remove => cartera.Cambio -= value;
        }

        /// <summary>Para pruebas: usa estos datos de cambio.</summary>
        public void Inyectar(DatosFichas nuevosDatos)
        {
            datos = nuevosDatos;
        }

        private void OnEnable()
        {
            Registro<IFichas>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IFichas>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public bool PuedePagar(int cantidad) => cartera.PuedeRetirar(cantidad);

        public bool IntentarPagar(int cantidad) => cartera.Retirar(cantidad);

        public void Abonar(int cantidad) => cartera.Comprar(cantidad);

        /// <summary>True si la cartera de dinero alcanza para un lote.</summary>
        public bool PuedeComprar(ICartera dinero) => datos != null && dinero != null && dinero.PuedePagar(datos.DineroPorLote);

        /// <summary>Cambia un lote de dinero por un lote de fichas. Cobra primero; si no alcanza no pasa nada.</summary>
        public bool Comprar(ICartera dinero)
        {
            if (!PuedeComprar(dinero) || !dinero.IntentarPagar(datos.DineroPorLote, MotivoDinero.Casino))
            {
                return false;
            }
            cartera.Comprar(datos.FichasPorLote);
            return true;
        }

        /// <summary>Lotes completos de fichas que se pueden cambiar por dinero ahora.</summary>
        public int LotesCobrables => datos != null ? cartera.LotesCobrables(datos.FichasPorLote) : 0;

        /// <summary>Cambia todos los lotes completos de fichas por dinero. Devuelve el dinero abonado (0 si no había un lote).</summary>
        public int Cobrar(ICartera dinero)
        {
            if (datos == null || dinero == null)
            {
                return 0;
            }
            int fichas = cartera.Cobrar(datos.FichasPorLote);
            int pago = fichas / datos.FichasPorLote * datos.DineroPorLote;
            if (pago > 0)
            {
                dinero.Abonar(pago, MotivoDinero.Casino);
            }
            return pago;
        }

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { fichas = Saldo });

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || !cartera.Restablecer(leidos.fichas))
            {
                Debug.LogWarning("[Casino] Las fichas no entendieron los datos guardados; no se cambia nada.");
            }
        }
    }
}
