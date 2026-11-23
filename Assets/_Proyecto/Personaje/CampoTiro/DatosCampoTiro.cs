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
        [SerializeField] private Material materialRejilla;
        [SerializeField] private Material materialHolo;
        [SerializeField] private RuntimeAnimatorController controlCentinela;
        [SerializeField] private RuntimeAnimatorController controlRobotPesado;
        [SerializeField] private RuntimeAnimatorController controlPerro;
        [SerializeField] private RuntimeAnimatorController controlElite;
        [SerializeField] private RuntimeAnimatorController controlGuardia;

        public GameObject[] Costales => costales;
        public Material MaterialBlanco => materialBlanco;
        public Material MaterialTurquesa => materialTurquesa;
        /// <summary>Unlit opaco con la cuadrícula holográfica cian sobre fondo casi negro (se le pone el tiling por superficie).</summary>
        public Material MaterialRejilla => materialRejilla;
        /// <summary>Unlit transparente cian con líneas: barrera, terminal y objetivos.</summary>
        public Material MaterialHolo => materialHolo;

        /// <summary>El AnimatorController de cada Nova (su estado por defecto es Idle).</summary>
        public RuntimeAnimatorController Control(TipoObjetivoTiro tipo)
        {
            switch (tipo)
            {
                case TipoObjetivoTiro.Centinela: return controlCentinela;
                case TipoObjetivoTiro.RobotPesado: return controlRobotPesado;
                case TipoObjetivoTiro.Perro: return controlPerro;
                case TipoObjetivoTiro.Elite: return controlElite;
                default: return controlGuardia;
            }
        }

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
        public void Configurar(GameObject[] costalesNuevos, GameObject centinela, GameObject pesado, GameObject perro, GameObject elite, GameObject guardia, Material blanco, Material turquesa,
            Material rejilla, Material holo, RuntimeAnimatorController cCentinela, RuntimeAnimatorController cPesado, RuntimeAnimatorController cPerro, RuntimeAnimatorController cElite, RuntimeAnimatorController cGuardia)
        {
            materialRejilla = rejilla;
            materialHolo = holo;
            controlCentinela = cCentinela;
            controlRobotPesado = cPesado;
            controlPerro = cPerro;
            controlElite = cElite;
            controlGuardia = cGuardia;
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
