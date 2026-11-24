using System;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR27 y T95, lógica pura de los gestos de armas:
    /// - Zonas del cuerpo relativas a la cabeza, en sus ejes de plano (x a la derecha, y arriba, z adelante, con solo el giro horizontal de la cabeza).
    ///   Cadera derecha: una caja amplia al lado derecho, de la cintura al muslo. Espalda (T95): encima o detrás del hombro derecho, a cualquier altura
    ///   de la cabeza para arriba; no hay que atinar a un punto. Cadera izquierda: ~(-0.2, -0.6, 0), para tomar cargadores.
    /// - Qué arma sale con el grip derecho en cada zona: en la cadera, la corta (pistola o revólver); en la espalda, la larga (T95: fusil primero,
    ///   luego escopeta, luego las demás largas). Guardar está en <see cref="ReglaFundaVR"/> (soltar el grip con el arma en su zona).
    /// </summary>
    public static class ReglaGestosArmaVR
    {
        public static readonly Vector3 CentroCaderaDerecha = new Vector3(0.2f, -0.6f, 0f);
        public static readonly Vector3 CentroHombroDerecho = new Vector3(0.15f, -0.1f, -0.12f);
        public static readonly Vector3 CentroCaderaIzquierda = new Vector3(-0.2f, -0.6f, 0f);
        /// <summary>Radio de la zona de la cadera izquierda (m).</summary>
        public const float RadioCadera = 0.22f;

        // T95: cadera derecha, caja en los ejes de la cabeza (m).
        public const float CaderaXMin = 0.02f;
        public const float CaderaXMax = 0.5f;
        public const float CaderaYMin = -1.05f;
        public const float CaderaYMax = -0.35f;
        public const float CaderaZMin = -0.35f;
        public const float CaderaZMax = 0.22f;

        // T95: espalda (encima o detrás del hombro derecho), en los ejes de la cabeza (m).
        /// <summary>Lo más a la izquierda que puede ir la mano (un poco pasada la nuca).</summary>
        public const float EspaldaXMin = -0.25f;
        public const float EspaldaXMax = 0.55f;
        /// <summary>Lo más bajo: algo por debajo del hombro.</summary>
        public const float EspaldaYMin = -0.4f;
        public const float EspaldaYMax = 0.7f;
        /// <summary>Del hombro a los ojos la mano debe ir detrás de este plano (el hombro está algo detrás de los ojos).</summary>
        public const float EspaldaZDetras = 0.02f;
        /// <summary>De la altura de los ojos para arriba basta con no ir muy al frente: encima de la cabeza o del hombro.</summary>
        public const float EspaldaZEncima = 0.15f;
        /// <summary>Lo más lejos de la cabeza en horizontal (m): más lejos es el brazo estirado, no la espalda.</summary>
        public const float EspaldaRadioHorizontal = 0.6f;

        /// <summary>T95: orden en que sale el arma larga: fusil (rifle de asalto), escopeta y luego las demás.</summary>
        public static readonly ArmaAPie[] OrdenLargas =
        {
            ArmaAPie.Fusil, ArmaAPie.Escopeta, ArmaAPie.Subfusil, ArmaAPie.Sniper, ArmaAPie.RiflePlasma, ArmaAPie.Lanzagranadas
        };

        /// <summary>Orden de las cortas si la última que se tuvo ya no está.</summary>
        public static readonly ArmaAPie[] OrdenCortas = { ArmaAPie.Pistola, ArmaAPie.Revolver };

        /// <summary>
        /// La mano en los ejes de la cabeza (solo el giro horizontal): <paramref name="mano"/> y <paramref name="cabeza"/> en el mismo espacio,
        /// <paramref name="frenteCabeza"/> hacia donde mira la cabeza (se aplana).
        /// </summary>
        public static Vector3 EnCabeza(Vector3 mano, Vector3 cabeza, Vector3 frenteCabeza) => EnCabeza(mano, cabeza, frenteCabeza, Vector3.up);

        /// <summary>
        /// Igual, pero si la cabeza mira casi recto arriba o abajo el giro horizontal sale de <paramref name="arribaCabeza"/> (T95: al voltear al
        /// piso o al cielo las zonas no brincan).
        /// </summary>
        public static Vector3 EnCabeza(Vector3 mano, Vector3 cabeza, Vector3 frenteCabeza, Vector3 arribaCabeza)
        {
            Vector3 f = new Vector3(frenteCabeza.x, 0f, frenteCabeza.z);
            if (f.sqrMagnitude < 0.04f)
            {
                // Mirando abajo, la coronilla apunta al frente; mirando arriba, hacia atrás.
                Vector3 g = frenteCabeza.y < 0f ? arribaCabeza : -arribaCabeza;
                g.y = 0f;
                if (g.sqrMagnitude > 1e-6f)
                {
                    f = g;
                }
            }
            f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            Quaternion giro = Quaternion.LookRotation(f, Vector3.up);
            return Quaternion.Inverse(giro) * (mano - cabeza);
        }

        /// <summary>La zona en que está la mano (<paramref name="local"/> en los ejes de la cabeza, ver EnCabeza).</summary>
        public static ZonaArmaVR Zona(Vector3 local, bool derecha)
        {
            if (!derecha)
            {
                return Vector3.Distance(local, CentroCaderaIzquierda) <= RadioCadera ? ZonaArmaVR.CaderaIzquierda : ZonaArmaVR.Ninguna;
            }
            if (EnEspalda(local))
            {
                return ZonaArmaVR.HombroDerecho;
            }
            return EnCaderaDerecha(local) ? ZonaArmaVR.CaderaDerecha : ZonaArmaVR.Ninguna;
        }

        /// <summary>T95: la mano derecha está en la cadera derecha (caja amplia).</summary>
        public static bool EnCaderaDerecha(Vector3 local) =>
            local.x >= CaderaXMin && local.x <= CaderaXMax && local.y >= CaderaYMin && local.y <= CaderaYMax && local.z >= CaderaZMin && local.z <= CaderaZMax;

        /// <summary>T95: la mano derecha está encima o detrás del hombro derecho, del hombro para arriba.</summary>
        public static bool EnEspalda(Vector3 local)
        {
            if (local.x < EspaldaXMin || local.x > EspaldaXMax || local.y < EspaldaYMin || local.y > EspaldaYMax)
            {
                return false;
            }
            if (local.x * local.x + local.z * local.z > EspaldaRadioHorizontal * EspaldaRadioHorizontal)
            {
                return false;
            }
            return local.z <= (local.y >= 0f ? EspaldaZEncima : EspaldaZDetras);
        }

        /// <summary>Arma corta: va en la cadera (pistola y revólver).</summary>
        public static bool EsCorta(ArmaAPie arma) => arma == ArmaAPie.Pistola || arma == ArmaAPie.Revolver;

        /// <summary>Arma larga: va en la espalda (todas las de fuego menos las cortas; la granada va en la rueda).</summary>
        public static bool EsLarga(ArmaAPie arma) => arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && !EsCorta(arma);

        /// <summary>True si <paramref name="arma"/> se guarda en <paramref name="zona"/> (corta en la cadera, larga en la espalda).</summary>
        public static bool VaEn(ZonaArmaVR zona, ArmaAPie arma) =>
            (zona == ZonaArmaVR.CaderaDerecha && EsCorta(arma)) || (zona == ZonaArmaVR.HombroDerecho && EsLarga(arma));

        /// <summary>T95: el arma larga que sale de la espalda: la primera que se tenga en <see cref="OrdenLargas"/>; Punos si ninguna.</summary>
        public static ArmaAPie ElegirLarga(Func<ArmaAPie, bool> tiene)
        {
            foreach (ArmaAPie a in OrdenLargas)
            {
                if (tiene(a))
                {
                    return a;
                }
            }
            return ArmaAPie.Punos;
        }

        /// <summary>El arma corta que sale de la cadera: la última corta si se sigue teniendo; si no, la primera de <see cref="OrdenCortas"/>.</summary>
        public static ArmaAPie ElegirCorta(Func<ArmaAPie, bool> tiene, ArmaAPie ultima)
        {
            if (EsCorta(ultima) && tiene(ultima))
            {
                return ultima;
            }
            foreach (ArmaAPie a in OrdenCortas)
            {
                if (tiene(a))
                {
                    return a;
                }
            }
            return ArmaAPie.Punos;
        }

        /// <summary>
        /// El arma que debe quedar en la mano tras el grip derecho en <paramref name="zona"/>: la <paramref name="corta"/> o la <paramref name="larga"/>
        /// equipada (o los puños si esa clase de arma ya estaba en la mano). Devuelve <paramref name="activa"/> si no hay nada que hacer (otra zona,
        /// o no se tiene esa clase de arma: pasa <see cref="ArmaAPie.Punos"/> como corta o larga).
        /// </summary>
        public static ArmaAPie QueSacar(ZonaArmaVR zona, ArmaAPie activa, ArmaAPie corta, ArmaAPie larga)
        {
            if (VaEn(zona, activa))
            {
                return ArmaAPie.Punos;
            }
            return QueSacarSinGuardar(zona, activa, corta, larga);
        }

        /// <summary>T95: como <see cref="QueSacar"/> pero sin guardar: si el arma de esa zona ya está en la mano, no cambia nada.</summary>
        public static ArmaAPie QueSacarSinGuardar(ZonaArmaVR zona, ArmaAPie activa, ArmaAPie corta, ArmaAPie larga)
        {
            if (zona == ZonaArmaVR.CaderaDerecha && !EsCorta(activa))
            {
                return EsCorta(corta) ? corta : activa;
            }
            if (zona == ZonaArmaVR.HombroDerecho && !EsLarga(activa))
            {
                return EsLarga(larga) ? larga : activa;
            }
            return activa;
        }
    }
}
