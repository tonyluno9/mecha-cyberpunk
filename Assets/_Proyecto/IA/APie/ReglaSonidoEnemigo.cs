using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>Qué enemigo de Nova suena (T-1533). Se decide por el nombre del modelo (E12, E14, E15, E16, E17, E18).</summary>
    public enum TipoSonoroEnemigo
    {
        /// <summary>Un pandillero, policía u otro: sin sonidos propios de Nova ni de robot.</summary>
        Ninguno,
        /// <summary>Guardia de Nova con armadura (el modelo base GuardiaNova).</summary>
        GuardiaNova,
        /// <summary>Guardia pesado con escudo (E12).</summary>
        GuardiaPesado,
        /// <summary>Robot centinela (E15).</summary>
        Centinela,
        /// <summary>Robot pesado (E16).</summary>
        RobotPesado,
        /// <summary>Perro robot (E17).</summary>
        Perro,
        /// <summary>Élite con jetpack (E18).</summary>
        Elite,
        /// <summary>Jefe final robot gigante (E14).</summary>
        JefeFinal
    }

    /// <summary>
    /// T-1533: lógica pura de los sonidos de los enemigos de Nova y los robots: qué ID pide cada tipo en cada evento, cada cuántos metros da un paso y el fade
    /// del jetpack. Sin Unity: el MonoBehaviour <see cref="SonidosEnemigoAPie"/> lo lee y lo aplica.
    /// </summary>
    public static class ReglaSonidoEnemigo
    {
        public const string IdPasosCaminar = "NOV_PASOS_CAMINAR";
        public const string IdPasosCorrer = "NOV_PASOS_CORRER";
        public const string IdPasoMetal = "PAS_METAL";
        public const string IdPasoPerro = "ROB_PERRO_MOVER";
        public const string IdArmaduraMover = "NOV_ARMADURA_MOVER";
        public const string IdServos = "ROB_SERVOS";
        public const string IdMuerteRobot = "ROB_MUERTE";
        public const string IdPlacaCae = "ROB_PES_PLACA_CAE";
        public const string IdHidraulica = "ROB_PES_HIDRAULICA";
        public const string IdLlegadaPesado = "ROB_PES_LLEGADA";
        public const string IdVozAlerta = "ROB_VOZ_ALERTA";
        public const string IdGrunido = "ROB_PERRO_GRUNIDO";
        public const string IdChispas = "ROB_CHISPAS_DANIO";
        public const string IdEncender = "ROB_ENCENDER";
        public const string IdEscudoActivar = "NOV_ESCUDO_ACTIVAR";
        public const string IdEscudoImpacto = "NOV_ESCUDO_IMPACTO";
        public const string IdJetpack = "NOV_JETPACK_LOOP";

        /// <summary>Velocidad (m/s) desde la que se oyen pasos de carrera.</summary>
        public const float VelocidadCorrer = 3.2f;
        /// <summary>Velocidad (m/s) bajo la que el enemigo se considera parado.</summary>
        public const float VelocidadParado = 0.3f;
        /// <summary>Segundos mínimos entre dos voces de alerta del mismo robot.</summary>
        public const float SegundosEntreAlertas = 12f;
        /// <summary>Segundos mínimos entre dos tandas de chispas al recibir daño.</summary>
        public const float SegundosEntreChispas = 1.5f;
        /// <summary>Cada cuántos pasos de Nova suena además el roce de la armadura.</summary>
        public const int PasosPorRoce = 2;
        /// <summary>Segundos mínimos entre dos hidráulicas del mismo gigante al arrancar.</summary>
        public const float SegundosEntreServos = 3f;
        /// <summary>Segundos del fade de entrada y de salida del jetpack.</summary>
        public const float SegundosFadeJetpack = 0.6f;
        /// <summary>Tono (pitch) al que arranca el motor del jetpack; sube a 1 con el fade de entrada y baja con el de salida.</summary>
        public const float TonoMinimoJetpack = 0.6f;

        /// <summary>
        /// El tipo de enemigo según los nombres que se le encuentren (controlador del Animator, avatar, modelo, datos...). Un perro robot lo marca
        /// <paramref name="perroRobot"/> aunque el nombre no diga E17. El nombre genérico GuardiaNova solo gana si ningún otro nombre dice más.
        /// </summary>
        public static TipoSonoroEnemigo Tipo(IEnumerable<string> nombres, bool perroRobot)
        {
            if (perroRobot)
            {
                return TipoSonoroEnemigo.Perro;
            }
            if (nombres == null)
            {
                return TipoSonoroEnemigo.Ninguno;
            }
            TipoSonoroEnemigo mejor = TipoSonoroEnemigo.Ninguno;
            foreach (string nombre in nombres)
            {
                TipoSonoroEnemigo t = TipoDeNombre(nombre);
                if (t != TipoSonoroEnemigo.Ninguno && (mejor == TipoSonoroEnemigo.Ninguno || mejor == TipoSonoroEnemigo.GuardiaNova))
                {
                    mejor = t;
                }
            }
            return mejor;
        }

        /// <summary>El tipo que dice un solo nombre (Ninguno si no es de Nova).</summary>
        public static TipoSonoroEnemigo TipoDeNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return TipoSonoroEnemigo.Ninguno;
            }
            if (Tiene(nombre, "E12_") || Tiene(nombre, "GuardiaPesadoE12") || Tiene(nombre, "GuardiaPesado_E12")) return TipoSonoroEnemigo.GuardiaPesado;
            if (Tiene(nombre, "E14_") || Tiene(nombre, "JefeFinalNova")) return TipoSonoroEnemigo.JefeFinal;
            if (Tiene(nombre, "E15_") || Tiene(nombre, "CentinelaNova")) return TipoSonoroEnemigo.Centinela;
            if (Tiene(nombre, "E16_") || Tiene(nombre, "RobotPesadoNova") || Tiene(nombre, "RobotPesadoE16")) return TipoSonoroEnemigo.RobotPesado;
            if (Tiene(nombre, "E17_") || Tiene(nombre, "PerroRobot")) return TipoSonoroEnemigo.Perro;
            if (Tiene(nombre, "E18_") || Tiene(nombre, "EliteNova")) return TipoSonoroEnemigo.Elite;
            if (Tiene(nombre, "GuardiaNova")) return TipoSonoroEnemigo.GuardiaNova;
            return TipoSonoroEnemigo.Ninguno;
        }

        private static bool Tiene(string texto, string parte) => texto.IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>True si es un robot (suena a robot y no a persona): centinela, pesado, perro y el jefe final.</summary>
        public static bool EsRobot(TipoSonoroEnemigo tipo) =>
            tipo == TipoSonoroEnemigo.Centinela || tipo == TipoSonoroEnemigo.RobotPesado || tipo == TipoSonoroEnemigo.Perro || tipo == TipoSonoroEnemigo.JefeFinal;

        /// <summary>True si es de Nova con armadura (suenan los pasos y el roce de Nova).</summary>
        public static bool LlevaArmaduraNova(TipoSonoroEnemigo tipo) =>
            tipo == TipoSonoroEnemigo.GuardiaNova || tipo == TipoSonoroEnemigo.Elite || tipo == TipoSonoroEnemigo.GuardiaPesado;

        /// <summary>True si es un gigante (el robot pesado y el jefe final): suena su llegada y su hidráulica.</summary>
        public static bool EsGigante(TipoSonoroEnemigo tipo) => tipo == TipoSonoroEnemigo.RobotPesado || tipo == TipoSonoroEnemigo.JefeFinal;

        /// <summary>True si lleva jetpack al caer del cielo: la élite (E18), el centinela (E15) y el robot pesado (E16).</summary>
        public static bool TieneJetpack(TipoSonoroEnemigo tipo) =>
            tipo == TipoSonoroEnemigo.Elite || tipo == TipoSonoroEnemigo.Centinela || tipo == TipoSonoroEnemigo.RobotPesado;

        /// <summary>True si grita al detectar al jugador con la voz de robot (centinela, pesado, perro).</summary>
        public static bool TieneVozAlerta(TipoSonoroEnemigo tipo) =>
            tipo == TipoSonoroEnemigo.Centinela || tipo == TipoSonoroEnemigo.RobotPesado || tipo == TipoSonoroEnemigo.Perro;

        /// <summary>True si suena NOV_ESCUDO_ACTIVAR cuando se agacha tras una cubierta (los guardias de Nova, la élite y el centinela).</summary>
        public static bool EscudoAlCubrirse(TipoSonoroEnemigo tipo) =>
            tipo == TipoSonoroEnemigo.GuardiaNova || tipo == TipoSonoroEnemigo.Elite || tipo == TipoSonoroEnemigo.Centinela;

        /// <summary>True si suena NOV_ESCUDO_ACTIVAR cuando lo provocan y levanta el escudo (el guardia pesado E12).</summary>
        public static bool EscudoAlPelear(TipoSonoroEnemigo tipo) => tipo == TipoSonoroEnemigo.GuardiaPesado;

        /// <summary>El ID del paso de este tipo ("" si no da pasos): caminando o corriendo.</summary>
        public static string IdPaso(TipoSonoroEnemigo tipo, bool corriendo)
        {
            switch (tipo)
            {
                case TipoSonoroEnemigo.Centinela: return IdPasoMetal;
                case TipoSonoroEnemigo.Perro: return IdPasoPerro;
                case TipoSonoroEnemigo.GuardiaNova:
                case TipoSonoroEnemigo.Elite: return corriendo ? IdPasosCorrer : IdPasosCaminar;
                case TipoSonoroEnemigo.GuardiaPesado:
                case TipoSonoroEnemigo.RobotPesado:
                case TipoSonoroEnemigo.JefeFinal: return IdPasosCaminar; // sin variante de tono bajo: ver el reporte
                default: return string.Empty;
            }
        }

        /// <summary>Metros que recorre entre un paso y el siguiente (el perro suena cada tanto porque su archivo dura 1.8 s).</summary>
        public static float MetrosPorPaso(TipoSonoroEnemigo tipo, bool corriendo)
        {
            switch (tipo)
            {
                case TipoSonoroEnemigo.Perro: return 8f;
                case TipoSonoroEnemigo.Centinela: return corriendo ? 1.8f : 1.2f;
                case TipoSonoroEnemigo.GuardiaPesado:
                case TipoSonoroEnemigo.RobotPesado: return 1.8f;
                case TipoSonoroEnemigo.JefeFinal: return 2.2f;
                default: return corriendo ? 1.7f : 1.1f;
            }
        }

        /// <summary>Si ya va corriendo con esta velocidad (m/s).</summary>
        public static bool Corre(float velocidad) => velocidad >= VelocidadCorrer;

        /// <summary>Si está en movimiento con esta velocidad (m/s).</summary>
        public static bool EnMovimiento(float velocidad) => velocidad > VelocidadParado;

        /// <summary>El ID del sonido de apagado tras morir ("" si no hay): lo que cae o se apaga después del <see cref="IdMuerteRobot"/>.</summary>
        public static string IdApagado(TipoSonoroEnemigo tipo)
        {
            switch (tipo)
            {
                case TipoSonoroEnemigo.RobotPesado:
                case TipoSonoroEnemigo.JefeFinal: return IdPlacaCae;
                case TipoSonoroEnemigo.Centinela:
                case TipoSonoroEnemigo.Perro: return IdServos;
                default: return string.Empty;
            }
        }

        /// <summary>Segundos después de la muerte a los que suena el apagado (que no tape el estallido de la muerte).</summary>
        public static float SegundosHastaApagado(TipoSonoroEnemigo tipo) => EsGigante(tipo) ? 0.7f : 0.45f;

        /// <summary>El ID que suena al detectar al jugador ("" si el tipo no grita).</summary>
        public static string IdAlerta(TipoSonoroEnemigo tipo) => TieneVozAlerta(tipo) ? IdVozAlerta : string.Empty;

        /// <summary>El ID al recibir daño: chispas para los robots, nada para los demás (ya tienen el impacto del arma).</summary>
        public static string IdDanio(TipoSonoroEnemigo tipo) => EsRobot(tipo) ? IdChispas : string.Empty;

        /// <summary>
        /// Nivel (0 a 1) del jetpack tras <paramref name="dt"/> segundos de fade: sube a 1 en <see cref="SegundosFadeJetpack"/> mientras vuela y baja a 0
        /// cuando deja de volar. <paramref name="nivel"/> es el de ahora.
        /// </summary>
        public static float NivelJetpack(float nivel, bool vuela, float dt)
        {
            float paso = SegundosFadeJetpack > 0f ? dt / SegundosFadeJetpack : 1f;
            float meta = vuela ? 1f : 0f;
            if (nivel < meta)
            {
                return Math.Min(meta, nivel + paso);
            }
            return Math.Max(meta, nivel - paso);
        }

        /// <summary>Tono del bucle del jetpack para un nivel de fade: de <see cref="TonoMinimoJetpack"/> (apagado) a 1 (a todo).</summary>
        public static float TonoJetpack(float nivel) => TonoMinimoJetpack + (1f - TonoMinimoJetpack) * Math.Max(0f, Math.Min(1f, nivel));
    }

    /// <summary>Cuenta los metros recorridos y avisa cuando toca un paso. Lógica pura.</summary>
    public sealed class AcumuladorPasos
    {
        private float acumulado;

        /// <summary>Pasos dados desde el principio.</summary>
        public int Pasos { get; private set; }

        /// <summary>Suma <paramref name="metros"/> recorridos; true si ya toca un paso (cada <paramref name="metrosPorPaso"/>).</summary>
        public bool Avanzar(float metros, float metrosPorPaso)
        {
            if (metros <= 0f || metrosPorPaso <= 0f)
            {
                return false;
            }
            acumulado += metros;
            if (acumulado < metrosPorPaso)
            {
                return false;
            }
            acumulado -= metrosPorPaso;
            if (acumulado > metrosPorPaso)
            {
                acumulado = 0f; // un salto grande (teletransporte) no da una ráfaga de pasos
            }
            Pasos++;
            return true;
        }

        /// <summary>Vuelve a cero (parado: el primer paso no sale al instante).</summary>
        public void Reiniciar()
        {
            acumulado = 0f;
        }
    }
}
