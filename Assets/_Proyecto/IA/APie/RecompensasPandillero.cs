using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-drops-24nov: al caer un <see cref="PandilleroAPie"/> (policía, NOVA, SKULLS u otros) dice a <see cref="RecompensasEnemigo"/> su bando y su
    /// arma, para que suelte balas y de vez en cuando un botiquín. Lo llama PandilleroAPie justo después de <c>Vencido</c>.
    /// </summary>
    public static class RecompensasPandillero
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AlArrancar()
        {
            RecompensasEnemigo.ReemplazaMunicionDelBotin = true;
        }

        public static void AlCaer(PandilleroAPie enemigo)
        {
            if (enemigo == null || enemigo.Datos == null)
            {
                return;
            }
            RecompensasEnemigo.ReemplazaMunicionDelBotin = true;
            RecompensasEnemigo.Soltar(enemigo.transform.position, Faccion(enemigo), Arma(enemigo));
        }

        private static FaccionRecompensa Faccion(PandilleroAPie enemigo)
        {
            if (enemigo.Datos.EsPolicia)
            {
                return FaccionRecompensa.Policia;
            }
            if (enemigo.EsDeNova)
            {
                return FaccionRecompensa.Nova;
            }
            if (enemigo.EsSkull || enemigo.Pandilla == IdPandilla.Calaveras)
            {
                return FaccionRecompensa.Skulls;
            }
            return FaccionRecompensa.Otros;
        }

        private static ArmaAPie? Arma(PandilleroAPie enemigo)
        {
            if (enemigo.ReglaArma != null)
            {
                return ArmaDe(enemigo.ReglaArma.Tipo);
            }
            return enemigo.Datos.UsaPistola || enemigo.Datos.EsPolicia ? ArmaAPie.Pistola : (ArmaAPie?)null;
        }

        /// <summary>El arma del jugador que usa las mismas balas que el arma del enemigo.</summary>
        public static ArmaAPie ArmaDe(TipoArmaEnemigo tipo)
        {
            switch (tipo)
            {
                case TipoArmaEnemigo.Escopeta: return ArmaAPie.Escopeta;
                case TipoArmaEnemigo.Subfusil: return ArmaAPie.Subfusil;
                default: return ArmaAPie.Pistola;
            }
        }
    }
}
