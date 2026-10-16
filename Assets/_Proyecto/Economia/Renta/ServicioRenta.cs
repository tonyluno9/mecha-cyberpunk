using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// La renta de la casa en la escena (T-107): envuelve <see cref="ModeloRenta"/>, cobra al empezar cada día del
    /// <see cref="IRelojMundo"/>, se registra como <see cref="IRenta"/> y se guarda (clave "renta"). Va en ServiciosEconomia.prefab.
    /// </summary>
    public class ServicioRenta : MonoBehaviour, IRenta, IGuardable
    {
        public const string Clave = "renta";

        [Serializable]
        private class DatosGuardados
        {
            public int diaProximoCobro;
            public int adeudo;
        }

        [SerializeField] private DatosEconomia datos;

        private ModeloRenta modelo;
        private IRelojMundo reloj;

        public int Monto => modelo != null ? modelo.Monto : 0;
        public int DiaProximoCobro => modelo != null ? modelo.DiaProximoCobro : 0;
        public int Adeudo => modelo != null ? modelo.Adeudo : 0;
        public bool TallerBloqueado => modelo != null && modelo.Bloqueado;
        public string ClaveGuardado => Clave;

        public event Action Cambio;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Economia] ServicioRenta en " + name + " no tiene DatosEconomia.", this);
                enabled = false;
                return;
            }
            modelo = new ModeloRenta(datos.Renta, datos.DiasEntreRentas, 1 + datos.DiasEntreRentas);
            modelo.Cambio += AlCambiar;
        }

        private void OnEnable()
        {
            if (modelo == null) return;
            Registro<IRenta>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<IRelojMundo>.Agregado += Conectar;
            Registro<IRelojMundo>.Quitado += AlQuitarseReloj;
        }

        private void Start()
        {
            if (modelo == null) return;
            if (reloj == null) Conectar(Registro<IRelojMundo>.Actual);
        }

        private void OnDisable()
        {
            Registro<IRenta>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<IRelojMundo>.Agregado -= Conectar;
            Registro<IRelojMundo>.Quitado -= AlQuitarseReloj;
            Desconectar();
        }

        public bool PagarAdeudo() => modelo != null && modelo.PagarAdeudo(Registro<ICartera>.Actual);

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { diaProximoCobro = DiaProximoCobro, adeudo = Adeudo });

        public void Cargar(string texto)
        {
            if (modelo == null) return;
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null)
            {
                Debug.LogWarning("[Economia] La renta no entendió los datos guardados; no se cambia nada.");
                return;
            }
            modelo.Restablecer(leidos.diaProximoCobro, leidos.adeudo);
        }

        private void Conectar(IRelojMundo nuevo)
        {
            if (nuevo == null || ReferenceEquals(nuevo, reloj)) return;
            Desconectar();
            reloj = nuevo;
            reloj.DiaCambio += AlCambiarDia;
        }

        private void Desconectar()
        {
            if (reloj == null) return;
            reloj.DiaCambio -= AlCambiarDia;
            reloj = null;
        }

        private void AlQuitarseReloj(IRelojMundo quitado)
        {
            if (ReferenceEquals(quitado, reloj)) Desconectar();
        }

        private void AlCambiarDia(int dia) => modelo?.AlEmpezarDia(dia, Registro<ICartera>.Actual);

        private void AlCambiar() => Cambio?.Invoke();
    }
}
