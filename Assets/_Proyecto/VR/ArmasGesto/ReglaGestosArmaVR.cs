using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR27, lógica pura de los gestos de armas:
    /// - Zonas del cuerpo relativas a la cabeza, en sus ejes de plano (x a la derecha, y arriba, z adelante, con solo el giro horizontal de la cabeza):
    ///   cadera derecha ~(0.2, -0.6, 0), hombro derecho ~(0.15, -0.1, -0.12) (arriba y atrás), cadera izquierda ~(-0.2, -0.6, 0).
    /// - Qué arma sale con el grip derecho en cada zona: en la cadera, la corta (pistola o revólver); en el hombro, la larga (escopeta, fusil, sniper,
    ///   subfusil, lanzagranadas o rifle de plasma). Si ya se tenía en la mano, se guarda (puños).
    /// </summary>
    public static class ReglaGestosArmaVR
    {
        public static readonly Vector3 CentroCaderaDerecha = new Vector3(0.2f, -0.6f, 0f);
        public static readonly Vector3 CentroHombroDerecho = new Vector3(0.15f, -0.1f, -0.12f);
        public static readonly Vector3 CentroCaderaIzquierda = new Vector3(-0.2f, -0.6f, 0f);
        /// <summary>Radio de la zona de cada cadera (m).</summary>
        public const float RadioCadera = 0.22f;
        /// <summary>Radio de la zona del hombro (m).</summary>
        public const float RadioHombro = 0.2f;

        /// <summary>
        /// La mano en los ejes de la cabeza (solo el giro horizontal): <paramref name="mano"/> y <paramref name="cabeza"/> en el mismo espacio,
        /// <paramref name="frenteCabeza"/> hacia donde mira la cabeza (se aplana).
        /// </summary>
        public static Vector3 EnCabeza(Vector3 mano, Vector3 cabeza, Vector3 frenteCabeza)
        {
            Vector3 f = new Vector3(frenteCabeza.x, 0f, frenteCabeza.z);
            f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            Quaternion giro = Quaternion.LookRotation(f, Vector3.up);
            return Quaternion.Inverse(giro) * (mano - cabeza);
        }

        /// <summary>La zona en que está la mano (<paramref name="local"/> en los ejes de <see cref="EnCabeza"/>).</summary>
        public static ZonaArmaVR Zona(Vector3 local, bool derecha)
        {
            if (!derecha)
            {
                return Vector3.Distance(local, CentroCaderaIzquierda) <= RadioCadera ? ZonaArmaVR.CaderaIzquierda : ZonaArmaVR.Ninguna;
            }
            float cadera = Vector3.Distance(local, CentroCaderaDerecha);
            float hombro = Vector3.Distance(local, CentroHombroDerecho);
            if (hombro <= RadioHombro && hombro < cadera)
            {
                return ZonaArmaVR.HombroDerecho;
            }
            return cadera <= RadioCadera ? ZonaArmaVR.CaderaDerecha : ZonaArmaVR.Ninguna;
        }

        /// <summary>Arma corta: va en la cadera (pistola y revólver).</summary>
        public static bool EsCorta(ArmaAPie arma) => arma == ArmaAPie.Pistola || arma == ArmaAPie.Revolver;

        /// <summary>Arma larga: va en el hombro (todas las de fuego menos las cortas; la granada va en la rueda).</summary>
        public static bool EsLarga(ArmaAPie arma) => arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && !EsCorta(arma);

        /// <summary>
        /// El arma que debe quedar en la mano tras el grip derecho en <paramref name="zona"/>: la <paramref name="corta"/> o la <paramref name="larga"/>
        /// equipada (o los puños si esa clase de arma ya estaba en la mano). Devuelve <paramref name="activa"/> si no hay nada que hacer (otra zona,
        /// o no se tiene esa clase de arma: pasa <see cref="ArmaAPie.Punos"/> como corta o larga).
        /// </summary>
        public static ArmaAPie QueSacar(ZonaArmaVR zona, ArmaAPie activa, ArmaAPie corta, ArmaAPie larga)
        {
            if (zona == ZonaArmaVR.CaderaDerecha)
            {
                if (EsCorta(activa))
                {
                    return ArmaAPie.Punos;
                }
                return EsCorta(corta) ? corta : activa;
            }
            if (zona == ZonaArmaVR.HombroDerecho)
            {
                if (EsLarga(activa))
                {
                    return ArmaAPie.Punos;
                }
                return EsLarga(larga) ? larga : activa;
            }
            return activa;
        }
    }
}
