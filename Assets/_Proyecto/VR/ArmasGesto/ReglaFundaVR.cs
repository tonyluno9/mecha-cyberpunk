using MechaCyberpunk.Personaje.Combate;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// T95, lógica pura del grip derecho para sacar y guardar:
    /// - Pulsar el grip en una zona saca el arma de esa zona (cadera: corta; espalda: larga) si no estaba ya en la mano. Es al instante.
    /// - Soltar el grip con el arma en su zona (pistola o revólver en la cadera, larga en la espalda) la guarda: quedan los puños.
    /// - Si el arma salió en este mismo apretón, para guardarla la mano tiene que salir antes de la zona (si no, sacar y soltar ahí mismo la
    ///   guardaría al momento).
    /// </summary>
    public sealed class ReglaFundaVR
    {
        private bool sostenido;
        private bool sacoEnEste;
        private bool salio;
        private ZonaArmaVR zonaSaque;

        /// <summary>True si la última llamada a <see cref="Avanzar"/> pidió guardar (no sacar).</summary>
        public bool Guardo { get; private set; }

        /// <summary>True mientras el grip sigue apretado desde que se pulsó.</summary>
        public bool Sostenido => sostenido;

        /// <summary>
        /// Un cuadro. <paramref name="pulso"/> y <paramref name="solto"/>: el grip derecho se pulsó o se soltó este cuadro. Devuelve el arma que debe
        /// quedar en la mano; igual a <paramref name="activa"/> si no hay nada que hacer.
        /// </summary>
        public ArmaAPie Avanzar(bool pulso, bool solto, ZonaArmaVR zona, ArmaAPie activa, ArmaAPie corta, ArmaAPie larga)
        {
            Guardo = false;
            if (pulso)
            {
                sostenido = true;
                salio = false;
                ArmaAPie destino = ReglaGestosArmaVR.QueSacarSinGuardar(zona, activa, corta, larga);
                sacoEnEste = destino != activa;
                zonaSaque = zona;
                if (!solto)
                {
                    return destino;
                }
                activa = destino;
            }
            if (!sostenido)
            {
                return activa;
            }
            if (sacoEnEste && zona != zonaSaque)
            {
                salio = true;
            }
            if (!solto)
            {
                return activa;
            }
            sostenido = false;
            if ((!sacoEnEste || salio) && ReglaGestosArmaVR.VaEn(zona, activa))
            {
                Guardo = true;
                return ArmaAPie.Punos;
            }
            return activa;
        }

        /// <summary>Olvida el apretón en curso (pausa, menú, cambio de modo).</summary>
        public void Reiniciar()
        {
            sostenido = false;
            sacoEnEste = false;
            salio = false;
            Guardo = false;
        }
    }
}
