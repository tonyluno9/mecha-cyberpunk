using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Progreso de las ligas (T-154): implementa <see cref="IProgresoLigas"/> y se guarda con clave <c>ligas</c>.
    /// Va en ServiciosProgresion.prefab.
    /// </summary>
    public class ServicioLigas : MonoBehaviour, IProgresoLigas, IGuardable
    {
        public const string Clave = "ligas";
        private static readonly string[] NombresPorDefecto = { "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO" };

        [SerializeField] private DatosLigas datos;

        [Serializable]
        private class DatosGuardados
        {
            public int liga;
            public int siguiente;
            public int campeones;
        }

        private ModeloLigas modelo;

        public DatosLigas Datos => datos;
        public int CantidadLigas => Modelo.CantidadLigas;
        public int LigaActual => Modelo.LigaActual;
        public int Siguiente => Modelo.Siguiente;
        public int CampeonesVencidos => Modelo.CampeonesVencidos;
        public int RivalesPorLiga => Modelo.RivalesPorLiga;
        public string ClaveGuardado => Clave;

        public event Action Cambio;
        public event Action<int> CampeonVencido;

        private ModeloLigas Modelo
        {
            get
            {
                if (modelo == null) Construir();
                return modelo;
            }
        }

        private void Construir()
        {
            int ligas = datos != null && datos.CantidadLigas > 0 ? datos.CantidadLigas : NombresPorDefecto.Length;
            int rivales = datos != null ? Mathf.Max(2, datos.RivalesPorLiga) : 5;
            modelo = new ModeloLigas(ligas, rivales);
            modelo.Cambio += () => Cambio?.Invoke();
            modelo.CampeonVencido += liga => CampeonVencido?.Invoke(liga);
        }

        private void Awake() => Construir();

        private void OnEnable()
        {
            Registro<IProgresoLigas>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioLigas>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IProgresoLigas>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioLigas>.Quitar(this);
        }

        public string NombreLiga(int liga)
        {
            if (liga < 1 || liga > CantidadLigas) return "";
            string[] nombres = datos != null && datos.CantidadLigas > 0 ? datos.Nombres : NombresPorDefecto;
            return nombres[liga - 1] ?? "";
        }

        public bool EsJefe(int indice) => Modelo.EsJefe(indice);
        public bool Vencido(int liga, int indice) => Modelo.Vencido(liga, indice);
        public bool PuedeRetar(int liga, int indice) => Modelo.PuedeRetar(liga, indice);
        public ResultadoVictoria RegistrarVictoria(int liga, int indice) => Modelo.RegistrarVictoria(liga, indice);
        public bool Poner(int liga, int siguiente, int campeones) => Modelo.Poner(liga, siguiente, campeones);
        public void Reiniciar() => Modelo.Reiniciar();

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados
        {
            liga = Modelo.LigaActual,
            siguiente = Modelo.Siguiente,
            campeones = Modelo.CampeonesVencidos
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
            if (leidos == null || !Modelo.Poner(leidos.liga, leidos.siguiente, leidos.campeones))
            {
                Debug.LogWarning("[Progresion] Las ligas no entendieron lo guardado; no se cambia nada.");
            }
        }
    }
}
