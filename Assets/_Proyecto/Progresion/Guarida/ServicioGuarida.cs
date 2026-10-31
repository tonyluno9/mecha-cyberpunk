using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// La guarida del jugador (T-448): armero, garaje y cama, cada uno con tres niveles que se compran con Biits en su punto
    /// (<see cref="GuaridaPunto"/>). Implementa <see cref="IGuarida"/> (va en <c>Registro&lt;IGuarida&gt;</c>) y se guarda en la clave "guarida".
    /// </summary>
    public class ServicioGuarida : MonoBehaviour, IGuarida, IGuardable
    {
        public const string Clave = "guarida";

        [SerializeField] private DatosGuarida datos;

        [Serializable]
        private class DatosGuardados
        {
            public string[] ids = Array.Empty<string>();
            public int[] niveles = Array.Empty<int>();
        }

        private readonly Dictionary<string, int> niveles = new Dictionary<string, int>();

        public string ClaveGuardado => Clave;
        public DatosGuarida Datos => datos;

        /// <summary>Se avisa al comprar un nivel y al cargar una partida.</summary>
        public event Action Cambio;

        private void OnEnable()
        {
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioGuarida>.Agregar(this);
            Registro<IGuarida>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioGuarida>.Quitar(this);
            Registro<IGuarida>.Quitar(this);
        }

        public int Nivel(string id) => id != null && niveles.TryGetValue(id, out int n) ? n : 0;

        public string Nombre(string id)
        {
            int i = datos != null ? datos.IndiceDe(id) : -1;
            return i >= 0 ? datos.Mejoras[i].nombre : string.Empty;
        }

        public int NivelMaximo => datos != null ? datos.NivelMaximo : 0;

        public int PrecioSiguiente(string id)
        {
            int i = datos != null ? datos.IndiceDe(id) : -1;
            return i < 0 ? 0 : ReglaGuarida.PrecioSiguiente(datos.Mejoras[i].precio, Nivel(id), datos.FactoresPrecio, datos.NivelMaximo);
        }

        /// <summary>Compra el siguiente nivel de la mejora. False si no hay dinero, ya está en el máximo o no existe.</summary>
        public bool Comprar(string id)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            int precio = PrecioSiguiente(id);
            if (cartera == null || precio <= 0)
            {
                return false;
            }
            if (!cartera.IntentarPagar(precio, MotivoDinero.Compra))
            {
                Sonidos.Reproducir("ECO_SIN_DINERO");
                return false;
            }
            niveles[id] = Nivel(id) + 1;
            Sonidos.Reproducir("ECO_COMPRA");
            Cambio?.Invoke();
            return true;
        }

        // IGuarida
        public float BonoDanioArmas => datos != null ? ReglaGuarida.PorNivel(datos.BonoDanioArmero, Nivel(DatosGuarida.IdArmero), 0f) : 0f;
        public int NivelGaraje => Nivel(DatosGuarida.IdGaraje);
        public float FactorVidaCarro => datos != null ? ReglaGuarida.PorNivel(datos.FactorVidaGaraje, NivelGaraje, 1f) : 1f;
        public bool CamaCura => Nivel(DatosGuarida.IdCama) >= 1;
        public float CamaBajaCalor => datos != null ? ReglaGuarida.PorNivel(datos.CalorBorradoCama, Nivel(DatosGuarida.IdCama), 0f) : 0f;

        public string Guardar()
        {
            List<string> ids = new List<string>();
            List<int> lista = new List<int>();
            foreach (KeyValuePair<string, int> par in niveles)
            {
                if (par.Value > 0)
                {
                    ids.Add(par.Key);
                    lista.Add(par.Value);
                }
            }
            return JsonUtility.ToJson(new DatosGuardados { ids = ids.ToArray(), niveles = lista.ToArray() });
        }

        public void Cargar(string texto)
        {
            DatosGuardados g;
            try
            {
                g = JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (Exception)
            {
                g = null;
            }
            niveles.Clear();
            if (g != null && g.ids != null && g.niveles != null)
            {
                for (int i = 0; i < g.ids.Length && i < g.niveles.Length; i++)
                {
                    if (datos != null && datos.IndiceDe(g.ids[i]) >= 0)
                    {
                        niveles[g.ids[i]] = ReglaGuarida.RecortarNivel(g.niveles[i], datos.NivelMaximo);
                    }
                }
            }
            Cambio?.Invoke();
        }
    }
}
