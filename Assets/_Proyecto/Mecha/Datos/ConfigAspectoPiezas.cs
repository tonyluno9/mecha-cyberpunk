using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Materiales greybox de cada rama de pieza y el de las franjas de nivel (T-170, colores de D-tony-122).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config aspecto de piezas", fileName = "ConfigAspectoPiezas_Nueva")]
    public class ConfigAspectoPiezas : ScriptableObject
    {
        [Tooltip("Pieza de serie: gris.")]
        [SerializeField] private Material deSerie;

        [Tooltip("Rama militar: azul acero.")]
        [SerializeField] private Material militar;

        [Tooltip("Rama tech: turquesa.")]
        [SerializeField] private Material tech;

        [Tooltip("Rama cuerpo a cuerpo: naranja.")]
        [SerializeField] private Material cuerpoACuerpo;

        [Tooltip("Rama fuego: magenta.")]
        [SerializeField] private Material fuego;

        [Tooltip("Franjas de nivel del antebrazo (las greybox que crea el creador usan este).")]
        [SerializeField] private Material franja;

        [Tooltip("Marco de la cabina (postes y viga) cuando la pieza de cabina es de serie: el oscuro de siempre.")]
        [SerializeField] private Material marcoDeSerie;

        public Material Franja => franja;

        /// <summary>El material de la rama (null si falta en la config).</summary>
        public Material DeRama(RamaPieza rama)
        {
            switch (rama)
            {
                case RamaPieza.Militar: return militar;
                case RamaPieza.Tech: return tech;
                case RamaPieza.CuerpoACuerpo: return cuerpoACuerpo;
                case RamaPieza.Fuego: return fuego;
                default: return deSerie;
            }
        }

        /// <summary>El material del marco de la cabina para esa rama.</summary>
        public Material MarcoDeRama(RamaPieza rama)
        {
            return rama == RamaPieza.DeSerie ? marcoDeSerie : DeRama(rama);
        }

        public bool EstaCompleta => deSerie != null && militar != null && tech != null && cuerpoACuerpo != null
            && fuego != null && franja != null && marcoDeSerie != null;
    }
}
