using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): las referencias del campo de tiro (prefabs de costales y de los visuales de Nova, materiales de la sala). Vive en una carpeta
    /// Resources/ para que el build las incluya; lo crea el método de editor AplicarCampoTiro.Aplicar.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha/Datos Campo de Tiro", fileName = "DatosCampoTiro")]
    public class DatosCampoTiro : ScriptableObject
    {
        public const string RutaResources = "DatosCampoTiro";

        [SerializeField] private GameObject[] costales = new GameObject[0];
        [SerializeField] private GameObject visualCentinela;
        [SerializeField] private GameObject visualRobotPesado;
        [SerializeField] private GameObject visualPerro;
        [SerializeField] private GameObject visualElite;
        [SerializeField] private GameObject visualGuardia;
        [SerializeField] private Material materialBlanco;
        [SerializeField] private Material materialTurquesa;

        public GameObject[] Costales => costales;
        public Material MaterialBlanco => materialBlanco;
        public Material MaterialTurquesa => materialTurquesa;

        public GameObject Visual(TipoObjetivoTiro tipo)
        {
            switch (tipo)
            {
                case TipoObjetivoTiro.Centinela: return visualCentinela;
                case TipoObjetivoTiro.RobotPesado: return visualRobotPesado;
                case TipoObjetivoTiro.Perro: return visualPerro;
                case TipoObjetivoTiro.Elite: return visualElite;
                default: return visualGuardia;
            }
        }

        /// <summary>Lo usa el método de editor para llenar el asset.</summary>
        public void Configurar(GameObject[] costalesNuevos, GameObject centinela, GameObject pesado, GameObject perro, GameObject elite, GameObject guardia, Material blanco, Material turquesa)
        {
            costales = costalesNuevos;
            visualCentinela = centinela;
            visualRobotPesado = pesado;
            visualPerro = perro;
            visualElite = elite;
            visualGuardia = guardia;
            materialBlanco = blanco;
            materialTurquesa = turquesa;
        }
    }
}
