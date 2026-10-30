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
            public int cara;
            public int ropa;
            public int implante;
            // T-415: opcionales; un perfil viejo sin ellos deja null y 0 (predeterminados).
            public string nombrePandilla;
            public int colorPandilla;
        }

        [SerializeField] private DatosPerfil datos;

        private string nombre;
        private string alias;
        private AparienciaJugador apariencia = AparienciaJugador.Predeterminada;
        private string nombrePandilla = ColoresPandilla.NombrePredeterminado;
        private int colorPandilla = ColoresPandilla.Predeterminado;

        public string Nombre => nombre ?? (datos != null ? datos.NombreRespaldo : string.Empty);
        public string Alias => alias ?? (datos != null ? datos.AliasRespaldo : string.Empty);

        public AparienciaJugador Apariencia => apariencia;

        public string NombrePandilla => nombrePandilla;
        public int ColorPandilla => colorPandilla;

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

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados
        {
            nombre = Nombre,
            alias = Alias,
            cara = apariencia.Cara,
            ropa = apariencia.Ropa,
            implante = apariencia.Implante,
            nombrePandilla = nombrePandilla,
            colorPandilla = colorPandilla
        });

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
            // Un texto viejo sin los campos de apariencia deja cara, ropa e implante en 0 (la Predeterminada).
            string n = ReglaAlias.Normalizar(leidos.nombre, datos.NombreRespaldo);
            string a = ReglaAlias.Normalizar(leidos.alias, datos.AliasRespaldo);
            AparienciaJugador ap = ReglaApariencia.Normalizar(new AparienciaJugador(leidos.cara, leidos.ropa, leidos.implante));
            string np = ColoresPandilla.Limpiar(leidos.nombrePandilla);
            int cp = ColoresPandilla.Normalizar(leidos.colorPandilla);
            if (n == Nombre && a == Alias && ap == apariencia && np == nombrePandilla && cp == colorPandilla) return;
            nombre = n;
            alias = a;
            apariencia = ap;
            nombrePandilla = np;
            colorPandilla = cp;
            Cambio?.Invoke();
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

        /// <summary>
        /// Cambia la apariencia pasándola por <see cref="ReglaApariencia"/> (índices negativos a 0). Avisa
        /// <see cref="Cambio"/> solo si cambió de verdad.
        /// </summary>
        public void FijarApariencia(AparienciaJugador nueva)
        {
            AparienciaJugador normalizada = ReglaApariencia.Normalizar(nueva);
            if (normalizada == apariencia) return;
            apariencia = normalizada;
            Cambio?.Invoke();
        }

        /// <summary>
        /// Cambia el nombre (pasa por <see cref="ColoresPandilla.Limpiar"/>) y el color de tu pandilla (T-415; fuera de rango = el
        /// predeterminado). Avisa <see cref="Cambio"/> solo si cambió de verdad.
        /// </summary>
        public void FijarPandilla(string nuevoNombre, int nuevoColor)
        {
            string n = ColoresPandilla.Limpiar(nuevoNombre);
            int c = ColoresPandilla.Normalizar(nuevoColor);
            if (n == nombrePandilla && c == colorPandilla) return;
            nombrePandilla = n;
            colorPandilla = c;
            Cambio?.Invoke();
        }
    }
}
