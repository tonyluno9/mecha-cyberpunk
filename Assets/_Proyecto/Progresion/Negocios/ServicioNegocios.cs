using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Los negocios de tus zonas (T-429, D-tony-302): los tomas (E en su punto) pagando una vez, los mejoras 2 veces y cada día de juego (al pasar las 00:00,
    /// IRelojMundo.DiaCambio) pagan Biits directo a la cartera según su nivel. Si la zona deja de ser tuya (T-427) el negocio no paga hasta recuperarla.
    /// Se guarda con la clave <c>negocios</c>.
    /// </summary>
    public class ServicioNegocios : MonoBehaviour, IGuardable
    {
        public const string Clave = "negocios";
        private const float SegundosBusqueda = 0.5f;

        [SerializeField] private DatosNegocios datos;

        [Serializable]
        private class DatosGuardados
        {
            public string[] ids = Array.Empty<string>();
            public int[] niveles = Array.Empty<int>();
        }

        private readonly Dictionary<string, int> niveles = new Dictionary<string, int>();
        private IRelojMundo reloj;
        private float proximaBusqueda;

        public string ClaveGuardado => Clave;
        public DatosNegocios Datos => datos;
        public event Action Cambio;
        /// <summary>Biits que acaban de pagar tus negocios (al cambiar el día).</summary>
        public event Action<int> Pagado;

        private void OnEnable()
        {
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioNegocios>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioNegocios>.Quitar(this);
            Desvincular();
        }

        private void Desvincular()
        {
            if (reloj != null)
            {
                reloj.DiaCambio -= AlCambiarDia;
                reloj = null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            IRelojMundo actual = Registro<IRelojMundo>.Actual;
            if (ReferenceEquals(actual, reloj))
            {
                return;
            }
            Desvincular();
            reloj = actual;
            if (reloj != null)
            {
                reloj.DiaCambio += AlCambiarDia;
            }
        }

        public int Cantidad => datos != null ? datos.Negocios.Length : 0;

        public DatosNegocios.Negocio Definicion(int i) => datos.Negocios[i];

        public int Nivel(string id) => id != null && niveles.TryGetValue(id, out int n) ? n : 0;

        /// <summary>True si la zona del negocio es tuya ahora.</summary>
        public bool ZonaTuya(string id)
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            int i = datos != null ? datos.IndiceDe(id) : -1;
            return territorio != null && i >= 0 && territorio.Dueno(datos.Negocios[i].zona) == IdPandilla.Jugador;
        }

        /// <summary>Lo que paga por día ahora: 0 si no lo tienes o su zona ya no es tuya.</summary>
        public int IngresoDia(string id) => ZonaTuya(id) ? IngresoNivel(id) : 0;

        /// <summary>Lo que pagaría en su nivel actual aunque la zona no sea tuya (para listarlo).</summary>
        public int IngresoNivel(string id)
        {
            int i = datos != null ? datos.IndiceDe(id) : -1;
            return i >= 0 ? ReglaNegocios.Ingreso(datos.Negocios[i].ingresoBase, Nivel(id), datos.Multiplicadores) : 0;
        }

        public int TotalPorDia
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Cantidad; i++)
                {
                    total += IngresoDia(datos.Negocios[i].id);
                }
                return total;
            }
        }

        /// <summary>Precio de tomarlo (nivel 0) o de la siguiente mejora; 0 si ya está al máximo.</summary>
        public int PrecioSiguiente(string id)
        {
            int i = datos != null ? datos.IndiceDe(id) : -1;
            return i < 0 ? 0 : ReglaNegocios.PrecioSiguiente(datos.Negocios[i].precio, Nivel(id), datos.FactoresMejora, datos.NivelMaximo);
        }

        /// <summary>Toma o mejora el negocio pagando. Devuelve false si la zona no es tuya, ya está al máximo o no alcanza el dinero.</summary>
        public bool Comprar(string id)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            int precio = PrecioSiguiente(id);
            if (cartera == null || precio <= 0 || !ZonaTuya(id))
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

        private void AlCambiarDia(int dia) => PagarDia();

        /// <summary>Paga el día de todos los negocios que rinden. Devuelve lo pagado.</summary>
        public int PagarDia()
        {
            int total = TotalPorDia;
            if (total <= 0)
            {
                return 0;
            }
            Registro<ICartera>.Actual?.Abonar(total, MotivoDinero.Premio);
            Pagado?.Invoke(total);
            return total;
        }

        /// <summary>El aviso de lo que pagaron los negocios: "+Ƀ350 DE TUS NEGOCIOS".</summary>
        public static string TextoPago(int total) => "+" + Moneda.Texto(total) + " DE TUS NEGOCIOS";

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
            if (g == null || g.ids == null || g.niveles == null || g.ids.Length != g.niveles.Length)
            {
                Debug.LogWarning("[Progresion] ServicioNegocios: el guardado no se entiende; no se cambia nada.");
                return;
            }
            niveles.Clear();
            int maximo = datos != null ? datos.NivelMaximo : 1;
            for (int k = 0; k < g.ids.Length; k++)
            {
                if (datos == null || datos.IndiceDe(g.ids[k]) >= 0)
                {
                    niveles[g.ids[k]] = Mathf.Clamp(g.niveles[k], 0, maximo);
                }
            }
            Cambio?.Invoke();
        }
    }
}
