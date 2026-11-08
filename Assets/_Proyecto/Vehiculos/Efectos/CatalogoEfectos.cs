using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>
    /// T-745: los prefabs del Unity Particle Pack (y un material URP) que usan las explosiones, el humo, el fuego, las chispas y el trazo de las balas.
    /// Vive en Resources (CatalogoEfectos) para que <see cref="EfectosExplosion"/> y el trazo lo hallen sin que la escena lo conecte.
    /// Lo crea el editor (CreadorEfectosT745), nunca a mano.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha/Catálogo de efectos")]
    public class CatalogoEfectos : ScriptableObject
    {
        public const string NombreRecurso = "CatalogoEfectos";

        [Header("Explosiones (flipbook del pack)")]
        [SerializeField] private GameObject explosionDiminuta;
        [SerializeField] private GameObject explosionChica;
        [SerializeField] private GameObject explosionGrande;

        [Header("Humo, fuego y chispas")]
        [SerializeField] private GameObject humo;
        [SerializeField] private GameObject fuegoChico;
        [SerializeField] private GameObject fuegoMediano;
        [SerializeField] private GameObject chispas;

        [Header("Material URP")]
        [Tooltip("Mancha oscura en el suelo tras una explosión.")]
        [SerializeField] private Material materialChamusco;

        public GameObject ExplosionDiminuta => explosionDiminuta;
        public GameObject ExplosionChica => explosionChica;
        public GameObject ExplosionGrande => explosionGrande;
        public GameObject Humo => humo;
        public GameObject FuegoChico => fuegoChico;
        public GameObject FuegoMediano => fuegoMediano;
        public GameObject Chispas => chispas;
        public Material MaterialChamusco => materialChamusco;

        private static CatalogoEfectos cargado;

        /// <summary>El catálogo de Resources (null si no existe).</summary>
        public static CatalogoEfectos Actual
        {
            get
            {
                if (cargado == null)
                {
                    cargado = Resources.Load<CatalogoEfectos>(NombreRecurso);
                }
                return cargado;
            }
        }

        /// <summary>Para el editor: llena el catálogo.</summary>
        public void Asignar(GameObject diminuta, GameObject chica, GameObject grande, GameObject humoPrefab, GameObject fuegoChicoPrefab,
            GameObject fuegoMedianoPrefab, GameObject chispasPrefab, Material chamusco)
        {
            explosionDiminuta = diminuta;
            explosionChica = chica;
            explosionGrande = grande;
            humo = humoPrefab;
            fuegoChico = fuegoChicoPrefab;
            fuegoMediano = fuegoMedianoPrefab;
            chispas = chispasPrefab;
            materialChamusco = chamusco;
        }
    }
}
