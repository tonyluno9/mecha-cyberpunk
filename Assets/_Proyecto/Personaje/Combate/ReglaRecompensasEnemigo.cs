using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-drops-24nov: reglas puras de lo que suelta un enemigo al caer (balas y botiquín), a qué arma van las balas y si el jugador
    /// (o una de sus manos en VR) está lo bastante cerca para recoger. Sin MonoBehaviour; el azar llega inyectado.
    /// </summary>
    public static class ReglaRecompensasEnemigo
    {
        /// <summary>Qué suelta: balas, botiquín, las dos cosas o nada.</summary>
        public readonly struct Tirada
        {
            public bool Municion { get; }
            public bool Botiquin { get; }

            public Tirada(bool municion, bool botiquin)
            {
                Municion = municion;
                Botiquin = botiquin;
            }
        }

        /// <summary>Dos números de 0 a 1 del azar: el primero decide las balas, el segundo el botiquín.</summary>
        public static Tirada Tirar(float probabilidadMunicion, float probabilidadBotiquin, Func<float> azar)
        {
            bool municion = azar() < probabilidadMunicion;
            bool botiquin = azar() < probabilidadBotiquin;
            return new Tirada(municion, botiquin);
        }

        /// <summary>
        /// El arma que recibe las balas: la del enemigo si el jugador la tiene y gasta reserva; si no, la de la mano; si no, la primera que tenga
        /// y gaste reserva. Null si no tiene ninguna (las balas se quedan en el piso).
        /// </summary>
        public static ArmaAPie? ElegirArma(ArmaAPie? delEnemigo, ArmaAPie activa, Func<ArmaAPie, bool> tiene)
        {
            if (delEnemigo.HasValue && UsaBalasDelPiso(delEnemigo.Value) && tiene(delEnemigo.Value))
            {
                return delEnemigo.Value;
            }
            if (UsaBalasDelPiso(activa) && tiene(activa))
            {
                return activa;
            }
            foreach (ArmaAPie arma in (ArmaAPie[])Enum.GetValues(typeof(ArmaAPie)))
            {
                if (UsaBalasDelPiso(arma) && tiene(arma))
                {
                    return arma;
                }
            }
            return null;
        }

        /// <summary>True si el arma gasta balas de reserva que se recogen del piso (todas menos puños, pistola y la granada que se lanza).</summary>
        public static bool UsaBalasDelPiso(ArmaAPie arma)
        {
            return arma != ArmaAPie.Punos && arma != ArmaAPie.Pistola && arma != ArmaAPie.Granada;
        }

        /// <summary>True si el cuerpo (en el plano) o alguna mano (en 3D) está dentro de su radio del objeto.</summary>
        public static bool Cerca(Vector3 objeto, Vector3? cuerpo, float radioCuerpo, Vector3[] manos, int cuantasManos, float radioMano)
        {
            if (cuerpo.HasValue)
            {
                Vector3 d = cuerpo.Value - objeto;
                d.y = 0f;
                if (d.sqrMagnitude <= radioCuerpo * radioCuerpo)
                {
                    return true;
                }
            }
            for (int i = 0; i < cuantasManos && manos != null && i < manos.Length; i++)
            {
                if ((manos[i] - objeto).sqrMagnitude <= radioMano * radioMano)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
