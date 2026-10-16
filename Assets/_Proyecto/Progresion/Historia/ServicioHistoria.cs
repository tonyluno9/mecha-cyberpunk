using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// La etapa del prólogo de la partida (T-124): implementa <see cref="IHistoria"/> y se guarda con clave <c>historia</c>.
    /// Guarda la etapa por nombre para sobrevivir si el enum crece. Va en ServiciosProgresion.prefab.
    /// </summary>
    public class ServicioHistoria : MonoBehaviour, IHistoria, IGuardable
    {
        public const string Clave = "historia";

        [Serializable]
        private class DatosGuardados
        {
            public string etapa;
        }

        private readonly ModeloHistoria modelo = new ModeloHistoria();

        public EtapaPrologo Etapa => modelo.Etapa;

        public string ClaveGuardado => Clave;

        public event Action<EtapaPrologo> EtapaCambio;

        private void Awake()
        {
            modelo.EtapaCambio += etapa => EtapaCambio?.Invoke(etapa);
        }

        private void OnEnable()
        {
            Registro<IHistoria>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IHistoria>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        /// <summary>Avanza a una etapa posterior. False si no lo es.</summary>
        public bool Avanzar(EtapaPrologo nueva) => modelo.Avanzar(nueva);

        /// <summary>Nueva partida: vuelve a Ninguna.</summary>
        public void Reiniciar() => modelo.Reiniciar();

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { etapa = modelo.Etapa.ToString() });

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
            EtapaPrologo etapa = EtapaPrologo.Ninguna;
            bool valida = leidos != null && !string.IsNullOrEmpty(leidos.etapa)
                && Enum.TryParse(leidos.etapa, false, out etapa) && Enum.IsDefined(typeof(EtapaPrologo), etapa);
            if (!valida)
            {
                Debug.LogWarning("[Progresion] La historia no entendió la etapa guardada; no se cambia nada.");
                return;
            }
            modelo.Poner(etapa);
        }
    }
}
