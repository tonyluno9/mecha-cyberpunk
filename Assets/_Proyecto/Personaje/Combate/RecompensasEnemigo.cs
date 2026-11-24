using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-drops-24nov: pone en el piso lo que suelta un enemigo al caer, según su bando (<see cref="ConfigRecompensasEnemigo"/>): balas
    /// (<see cref="MunicionEnPiso"/>, que prefieren el arma del enemigo y si no la tuya) y de vez en cuando un <see cref="BotiquinEnPiso"/>.
    /// Desaparecen a los <see cref="ConfigRecompensasEnemigo.SegundosEnPiso"/> s. Lo llama la IA al caer un enemigo; mientras
    /// <see cref="ReemplazaMunicionDelBotin"/> es true, <see cref="ColocadorBotin"/> ya no suelta sus balas genéricas (no se duplican).
    /// </summary>
    public static class RecompensasEnemigo
    {
        private const float AlturaRayo = 2f;
        private const float AlcanceRayo = 6f;

        /// <summary>True cuando la IA avisa las caídas de los enemigos aquí (lo enciende la IA al arrancar).</summary>
        public static bool ReemplazaMunicionDelBotin { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => ReemplazaMunicionDelBotin = false;

        /// <summary>Tira el azar (<see cref="ColocadorBotin.Azar"/>, que las pruebas fijan) y pone lo que toque junto a <paramref name="posicion"/>.</summary>
        public static void Soltar(Vector3 posicion, FaccionRecompensa faccion, ArmaAPie? armaEnemigo)
        {
            ConfigRecompensasEnemigo config = ConfigRecompensasEnemigo.Actual;
            ReglaRecompensasEnemigo.Tirada tirada = ReglaRecompensasEnemigo.Tirar(config.ProbabilidadMunicion(faccion), config.ProbabilidadBotiquin,
                ColocadorBotin.Azar);
            if (!tirada.Municion && !tirada.Botiquin)
            {
                return;
            }
            Vector3 piso = BuscarPiso(posicion);
            // Las balas hacia adelante y el botiquín hacia atrás, lejos del dinero y el arma que pone ColocadorBotin a los lados.
            if (tirada.Municion)
            {
                MunicionEnPiso municion = MunicionEnPiso.Crear(piso + Vector3.forward * config.Separacion, armaEnemigo);
                Object.Destroy(municion.gameObject, config.SegundosEnPiso);
            }
            if (tirada.Botiquin)
            {
                BotiquinEnPiso botiquin = BotiquinEnPiso.Crear(piso + Vector3.back * config.Separacion);
                Object.Destroy(botiquin.gameObject, config.SegundosEnPiso);
            }
        }

        private static Vector3 BuscarPiso(Vector3 posicion)
        {
            if (Physics.Raycast(posicion + Vector3.up * AlturaRayo, Vector3.down, out RaycastHit h, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return h.point;
            }
            return posicion;
        }
    }
}
