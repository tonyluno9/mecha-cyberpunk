using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// El perfil del jugador (T-070): su nombre y su alias, con valores de respaldo hasta que la creación de personaje los
    /// llene. Se registra como <see cref="IPerfilJugador"/>. Va en ServiciosProgresion.prefab: un servicio por jugador.
    /// </summary>
    public class ServicioPerfil : MonoBehaviour, IPerfilJugador, IGuardable
    {
        public const string Clave = "perfil";

        [Serializable]
        private class DatosGuardados
        {
            public string nombre;
            public string alias;
        }

        [SerializeField] private DatosPerfil datos;

        private string nombre;
        private string alias;

        public string Nombre => nombre ?? (datos != null ? datos.NombreRespaldo : string.Empty);
        public string Alias => alias ?? (datos != null ? datos.AliasRespaldo : string.Empty);

        public string ClaveGuardado => Clave;

        public event Action Cambio;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Progresion] ServicioPerfil en " + name + " no tiene DatosPerfil.", this);
                enabled = false;
                return;
            }
            nombre = datos.NombreRespaldo;
            alias = datos.AliasRespaldo;
        }

        private void OnEnable()
        {
            if (datos == null) return;
            Registro<IPerfilJugador>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPerfilJugador>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { nombre = Nombre, alias = Alias });

        public void Cargar(string texto)
        {
            if (datos == null) return;
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
                Debug.LogWarning("[Progresion] El perfil no entendió los datos guardados; no se cambia nada.");
                return;
            }
            Fijar(leidos.nombre, leidos.alias);
        }

        /// <summary>
        /// Cambia nombre y alias pasándolos por <see cref="ReglaAlias"/> (vacío o null = respaldo). Avisa
        /// <see cref="Cambio"/> solo si alguno cambió de verdad.
        /// </summary>
        public void Fijar(string nuevoNombre, string nuevoAlias)
        {
            if (datos == null) return;
            string n = ReglaAlias.Normalizar(nuevoNombre, datos.NombreRespaldo);
            string a = ReglaAlias.Normalizar(nuevoAlias, datos.AliasRespaldo);
            if (n == nombre && a == alias) return;
            nombre = n;
            alias = a;
            Cambio?.Invoke();
        }
    }
}
