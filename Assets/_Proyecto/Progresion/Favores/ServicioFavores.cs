using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Bolsillo de favores (T-182): descuento, información y escondite. Se guarda con clave <c>favores</c>.
    /// Va en ServiciosProgresion.prefab. Ganarlos es de las misiones (T-184); usarlos, de T-187.
    /// </summary>
    public class ServicioFavores : MonoBehaviour, IGuardable
    {
        public const string Clave = "favores";
        private const int MaximoPorDefecto = 3;

        [SerializeField] private DatosFavor[] favores;

        [Serializable]
        private class EntradaGuardada
        {
            public string tipo;
            public int cantidad;
        }

        [Serializable]
        private class DatosGuardados
        {
            public List<EntradaGuardada> favores = new List<EntradaGuardada>();
        }

        private ModeloFavores modelo;

        public string ClaveGuardado => Clave;
        public int Maximo => Modelo.Maximo;

        public event Action Cambio;

        private ModeloFavores Modelo
        {
            get
            {
                if (modelo == null) Construir();
                return modelo;
            }
        }

        private void Construir()
        {
            int maximo = MaximoPorDefecto;
            if (favores != null)
            {
                foreach (DatosFavor f in favores)
                {
                    if (f != null)
                    {
                        maximo = f.Maximo;
                        break;
                    }
                }
            }
            modelo = new ModeloFavores(maximo);
            modelo.Cambio += () => Cambio?.Invoke();
        }

        private void Awake() => Construir();

        private void OnEnable()
        {
            Registro<ServicioFavores>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ServicioFavores>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public int Cantidad(TipoFavor tipo) => Modelo.Cantidad(tipo);
        public bool Dar(TipoFavor tipo) => Modelo.Dar(tipo);
        public bool Gastar(TipoFavor tipo) => Modelo.Gastar(tipo);

        /// <summary>Los datos del tipo, o null si no están asignados.</summary>
        public DatosFavor Datos(TipoFavor tipo)
        {
            if (favores == null) return null;
            foreach (DatosFavor f in favores)
            {
                if (f != null && f.Tipo == tipo) return f;
            }
            return null;
        }

        public string Guardar()
        {
            var guardado = new DatosGuardados();
            foreach (TipoFavor tipo in Enum.GetValues(typeof(TipoFavor)))
            {
                guardado.favores.Add(new EntradaGuardada { tipo = tipo.ToString(), cantidad = Modelo.Cantidad(tipo) });
            }
            return JsonUtility.ToJson(guardado);
        }

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
            if (leidos == null || leidos.favores == null)
            {
                Debug.LogWarning("[Progresion] Los favores no entendieron lo guardado; no se cambia nada.");
                return;
            }
            foreach (EntradaGuardada entrada in leidos.favores)
            {
                if (entrada == null || !Enum.TryParse(entrada.tipo, out TipoFavor tipo) || !Enum.IsDefined(typeof(TipoFavor), tipo))
                {
                    Debug.LogWarning("[Progresion] Los favores no conocen el tipo '" + (entrada?.tipo ?? "") + "'; se ignora.");
                    continue;
                }
                Modelo.Poner(tipo, entrada.cantidad);
            }
        }
    }
}
