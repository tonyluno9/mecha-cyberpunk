using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Valores de respaldo del perfil del jugador mientras la creación de personaje (T-F6) no lo llena.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de perfil", fileName = "DatosPerfil_Nuevo")]
    public class DatosPerfil : ScriptableObject
    {
        [Tooltip("Nombre que se usa si no hay uno válido.")]
        [SerializeField] private string nombreRespaldo = "JUGADOR";
        [Tooltip("Alias que se usa si no hay uno válido.")]
        [SerializeField] private string aliasRespaldo = "NOVATO";

        public string NombreRespaldo => nombreRespaldo;
        public string AliasRespaldo => aliasRespaldo;
    }
}
