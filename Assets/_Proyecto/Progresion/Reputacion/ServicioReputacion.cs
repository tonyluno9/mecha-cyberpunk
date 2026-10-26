using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Reputación de barrio y corporaciones (T-182): implementa <see cref="IReputacion"/> y se guarda con clave <c>reputacion</c>.
    /// Va en ServiciosProgresion.prefab. <see cref="Sumar"/> es solo para Tony (misiones).
    /// </summary>
    public class ServicioReputacion : MonoBehaviour, IReputacion, IGuardable
    {
        public const string Clave = "reputacion";

        [SerializeField] private DatosReputacion datos;

        [Serializable]
        private class DatosGuardados
        {
            public int barrio;
            public int corporaciones;
        }

        private ModeloReputacion modelo;

        public DatosReputacion Datos => datos;
        public int Minimo => Modelo.Minimo;
        public int Maximo => Modelo.Maximo;
        public string ClaveGuardado => Clave;

        public event Action Cambio;
        public event Action<TipoReputacion, int> NivelCambio;

        private ModeloReputacion Modelo
        {
            get
            {
                if (modelo == null) Construir();
                return modelo;
            }
        }

        private void Construir()
        {
            var d = datos;
            if (d == null)
            {
                Debug.LogError("[Progresion] ServicioReputacion en " + name + " no tiene DatosReputacion; se usan valores por defecto.", this);
                d = ScriptableObject.CreateInstance<DatosReputacion>();
            }
            modelo = new ModeloReputacion(d);
            modelo.Cambio += () => Cambio?.Invoke();
            modelo.NivelCambio += (tipo, nivel) => NivelCambio?.Invoke(tipo, nivel);
        }

        private void Awake() => Construir();

        private void OnEnable()
        {
            Registro<IReputacion>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioReputacion>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IReputacion>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioReputacion>.Quitar(this);
        }

        public int Valor(TipoReputacion tipo) => Modelo.Valor(tipo);
        public int Nivel(TipoReputacion tipo) => Modelo.Nivel(tipo);
        public string NombreNivel(TipoReputacion tipo) => Modelo.NombreNivel(tipo);

        /// <summary>Solo Tony (misiones): cada misión dice cuánto mueve cada reputación.</summary>
        public void Sumar(TipoReputacion tipo, int cantidad) => Modelo.Sumar(tipo, cantidad);

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados
        {
            barrio = Modelo.Valor(TipoReputacion.Barrio),
            corporaciones = Modelo.Valor(TipoReputacion.Corporaciones)
        });

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
            if (leidos == null)
            {
                Debug.LogWarning("[Progresion] La reputación no entendió lo guardado; no se cambia nada.");
                return;
            }
            Modelo.Restablecer(leidos.barrio, leidos.corporaciones);
        }
    }
}
