using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1561: qué hace el personaje en cada momento. Orden de importancia de las acciones: la de más arriba gana y las de abajo se interrumpen.
    /// </summary>
    public enum AccionPersonaje
    {
        Locomocion = 0,
        Guardia = 1,
        Apuntar = 2,
        Golpe = 3,
        Disparo = 4,
        Recarga = 5,
        Granada = 6,
        Interaccion = 7,
        Carro = 8,
        Dano = 9,
        Derribo = 10,
        Muerte = 11,
    }

    /// <summary>T-1561: qué hace el personaje con el arma al ponerse a correr.</summary>
    public enum AccionArmaAlCorrer
    {
        /// <summary>No tiene arma levantada, o corre con ella (hay clip de correr con esa arma).</summary>
        Mantener = 0,
        /// <summary>Guarda el arma (Pistola_Guardar o Rifle_Guardar) y corre normal.</summary>
        GuardarYCorrer = 1,
    }

    /// <summary>
    /// T-1561 (decisión de Tony, 21-nov: "transiciones con lógica"): reglas puras de modo y transición del jugador. Sin Animator ni tiempo propio (todo entra como parámetro):
    /// 1) pelear -> correr: la guardia de puños solo existe parado o caminando despacio; con Shift se sale del modo pelea y los brazos bajan en ~0.2 s;
    /// 2) correr -> pelear: al golpear corriendo, primero sube la guardia (~0.2 s) y luego golpea;
    /// 3) armas: sacar -> apuntar -> disparar (el tiro sale con el arma ya en la pose), recargar bloquea disparar, correr guarda el arma salvo que haya clip de correr con ella;
    /// 4) granada: de pie o agachado, se suelta en el cuadro del lanzamiento y no al apretar;
    /// 5) prioridades: daño interrumpe golpe y recarga; derribo y muerte ganan a todo;
    /// 6) en el aire la capa de arriba (armas y puños) se apaga.
    /// Los cuadros salen de BITACORA.md (clips de Mixamo del 21-nov) y de medir la mano derecha de cada clip (velocidad máxima de la muñeca = suelta).
    /// </summary>
    public static class ReglasTransicionesT1561
    {
        /// <summary>Parámetros bool del Animator que mantienen las poses fijas (Sentarse y Colgarse son en bucle hasta que se apagan).</summary>
        public const string ParametroSentado = "Sentado";
        public const string ParametroColgado = "Colgado";

        // ---------- Guardia y modo pelea ----------

        /// <summary>Velocidad (m/s) hasta la que se puede ir en guardia: caminar despacio (PJ01_Caminar = 1.62 m/s). Más rápido, los brazos van sueltos.</summary>
        public const float VelocidadMaximaGuardia = 2f;
        /// <summary>Lo que tardan los brazos en salir de la guardia al correr (s): duración de la transición PunosGuardia -> Vacio.</summary>
        public const float SegundosSalidaGuardia = 0.2f;
        /// <summary>Lo que tarda en subir la guardia antes del primer golpe si se golpea corriendo (s).</summary>
        public const float SegundosSubirGuardia = 0.2f;
        /// <summary>Duración (s) de las transiciones cortas de la capa de brazos (golpe, sacar, guardar).</summary>
        public const float MezclaCorta = 0.12f;

        /// <summary>True si la guardia de puños puede verse: sin pedir correr y a velocidad de caminar despacio o menos.</summary>
        public static bool GuardiaPermitida(float velocidad, bool pidiendoCorrer)
        {
            return !pidiendoCorrer && velocidad <= VelocidadMaximaGuardia;
        }

        /// <summary>True si hay que dejar el modo pelea por completo (sprint): después de soltar Shift no vuelve la guardia sola.</summary>
        public static bool DebeSalirDePelea(bool pidiendoCorrer)
        {
            return pidiendoCorrer;
        }

        /// <summary>True si, para golpear, primero hay que subir la guardia: va más rápido que caminar despacio y no la tiene arriba.</summary>
        public static bool NecesitaSubirGuardia(float velocidad, bool guardiaArriba)
        {
            return velocidad > VelocidadMaximaGuardia && !guardiaArriba;
        }

        /// <summary>True si el golpe ya puede empezar: no hace falta subir la guardia, ya está arriba o ya pasó el tiempo de subirla.</summary>
        public static bool GuardiaLista(float velocidad, bool guardiaArriba, float segundosDesdePeticion)
        {
            return !NecesitaSubirGuardia(velocidad, guardiaArriba) || segundosDesdePeticion >= SegundosSubirGuardia;
        }

        // ---------- Armas ----------

        /// <summary>Con el rifle (y armas largas) corre guardándolo. Decisión que Tony puede cambiar: con true corre con el rifle en la mano (PJ01_Rifle_Correr).</summary>
        public const bool RifleCorreConArma = false;
        /// <summary>Hay clip de correr con la pistola (PJ01_Pistola_Correr): corre con ella hasta que se acaba el tiempo de arma levantada.</summary>
        public const bool PistolaCorreConArma = true;

        /// <summary>Qué hace al correr con esa arma levantada.</summary>
        public static AccionArmaAlCorrer AccionAlCorrer(TipoArmaVisual tipo)
        {
            switch (tipo)
            {
                case TipoArmaVisual.Pistola:
                    return PistolaCorreConArma ? AccionArmaAlCorrer.Mantener : AccionArmaAlCorrer.GuardarYCorrer;
                case TipoArmaVisual.ArmaLarga:
                    return RifleCorreConArma ? AccionArmaAlCorrer.Mantener : AccionArmaAlCorrer.GuardarYCorrer;
                default:
                    return AccionArmaAlCorrer.Mantener;
            }
        }

        /// <summary>Segundos máximos que se espera a que el arma llegue a la pose antes de disparar de todos modos (red de seguridad: un evento perdido no deja al jugador sin poder disparar).</summary>
        public const float SegundosMaximosSacar = 1.0f;
        /// <summary>Segundos que se recuerda un disparo pedido mientras el arma se saca (s).</summary>
        public const float SegundosRecordarDisparo = 1.2f;

        /// <summary>
        /// El disparo real puede salir: el arma está en la mano y en la pose, y no hay una acción que lo bloquee (recarga, granada, interacción...).
        /// Con <paramref name="segundosEsperando"/> pasado de <see cref="SegundosMaximosSacar"/> sale aunque falte el evento.
        /// </summary>
        public static bool PuedeDisparar(bool armaEnPose, bool recargando, AccionPersonaje actual, float segundosEsperando)
        {
            if (recargando || BloqueaDisparo(actual))
            {
                return false;
            }
            return armaEnPose || segundosEsperando >= SegundosMaximosSacar;
        }

        /// <summary>True si la acción en curso no deja disparar.</summary>
        public static bool BloqueaDisparo(AccionPersonaje actual)
        {
            return actual == AccionPersonaje.Recarga || actual >= AccionPersonaje.Granada;
        }

        /// <summary>True si la acción en curso no deja empezar un golpe de puños.</summary>
        public static bool BloqueaGolpe(AccionPersonaje actual)
        {
            return actual >= AccionPersonaje.Recarga;
        }

        /// <summary>
        /// Una recarga que ya empezó cuando el jugador echa a correr: la pistola sigue con PJ01_Pistola_RecargarCaminar (en la capa de brazos); las armas largas la cancelan
        /// (no se recarga un fusil corriendo: se guarda y corre).
        /// </summary>
        public static bool RecargaSigueAlCorrer(TipoArmaVisual tipo)
        {
            return tipo == TipoArmaVisual.Pistola;
        }

        // ---------- Granada ----------

        public const float FotogramasPorSegundo = 24f;
        /// <summary>Cuántas veces más rápido que el clip se juegan PJ01_Granada_DePie y PJ01_Granada_Agachado (duran 4.0 y 4.4 s tal como vienen).</summary>
        public const float RapidezGranada = 1.8f;
        /// <summary>Rapidez (x) de PJ01_Rifle_Sacar y PJ01_Rifle_Guardar (duran 2.6 s tal como vienen): el arma larga sale en ~1.1 s.</summary>
        public const float RapidezSacarArmaLarga = 1.8f;
        /// <summary>Fracción de PJ01_Rifle_Sacar a partir de la cual sigue al reposo o a apuntar (la pose de apuntar ya está lista).</summary>
        public const float SalidaSacarArmaLarga = 0.8f;
        /// <summary>Fracción de PJ01_Rifle_Guardar a partir de la cual pasa directo a reposo (cuadro 50 de 64).</summary>
        public const float SalidaGuardarArmaLarga = 0.78f;
        /// <summary>Duración (s) de PJ01_Granada_DePie (97 cuadros a 24 por segundo: 96 intervalos, tal como viene).</summary>
        public const float SegundosClipGranadaDePie = 4f;
        /// <summary>Duración (s) de PJ01_Granada_Agachado (106 cuadros).</summary>
        public const float SegundosClipGranadaAgachado = 105f / FotogramasPorSegundo;
        /// <summary>Cuadro (1 = el primero del clip) en el que la mano suelta la granada de pie: la muñeca derecha llega a su velocidad máxima (3.2 m/s). Medido con Blender sobre el FBX (el cuadro 1 es el tiempo 0).</summary>
        public const int CuadroSueltaDePie = 54;
        /// <summary>Cuadro en el que la mano suelta la granada agachado (muñeca a 2.7 m/s).</summary>
        public const int CuadroSueltaAgachado = 56;

        /// <summary>Segundos del clip (a rapidez 1) hasta el cuadro de la suelta.</summary>
        public static float SegundosClipHastaSuelta(bool agachado)
        {
            return ((agachado ? CuadroSueltaAgachado : CuadroSueltaDePie) - 1) / FotogramasPorSegundo;
        }

        /// <summary>Segundos reales entre apretar y soltar la granada (con la rapidez del estado).</summary>
        public static float SegundosHastaSoltarGranada(bool agachado)
        {
            return SegundosClipHastaSuelta(agachado) / RapidezGranada;
        }

        /// <summary>Segundos reales que dura todo el gesto de la granada.</summary>
        public static float SegundosGestoGranada(bool agachado)
        {
            return (agachado ? SegundosClipGranadaAgachado : SegundosClipGranadaDePie) / RapidezGranada;
        }

        /// <summary>Nombre del clip de la granada según la postura.</summary>
        public static string ClipGranada(bool agachado)
        {
            return agachado ? "PJ01_Granada_Agachado" : "PJ01_Granada_DePie";
        }

        // ---------- Prioridades ----------

        /// <summary>True si <paramref name="nueva"/> puede cortar a <paramref name="actual"/>. Las acciones ligeras (locomoción, guardia, apuntar, golpe, disparo) ceden a cualquiera; las largas solo a las de más arriba que el daño.</summary>
        public static bool Interrumpe(AccionPersonaje nueva, AccionPersonaje actual)
        {
            if (actual == AccionPersonaje.Muerte)
            {
                return false;
            }
            if (actual <= AccionPersonaje.Disparo)
            {
                return nueva != actual;
            }
            if (actual == AccionPersonaje.Dano)
            {
                return nueva >= AccionPersonaje.Derribo;
            }
            if (actual == AccionPersonaje.Derribo)
            {
                return nueva == AccionPersonaje.Muerte;
            }
            if (actual == AccionPersonaje.Interaccion || actual == AccionPersonaje.Carro)
            {
                return nueva >= AccionPersonaje.Dano && nueva > actual;
            }
            // Recarga y granada: las corta el daño, el derribo y la muerte.
            return nueva >= AccionPersonaje.Dano;
        }

        /// <summary>Fracción de la vida máxima que debe quitar un golpe para interrumpir un golpe, una recarga o un lanzamiento.</summary>
        public const float FraccionDanoQueInterrumpe = 0.08f;

        /// <summary>True si un daño de esa cantidad corta lo que el jugador esté haciendo con los brazos (golpe, recarga, granada).</summary>
        public static bool DanoInterrumpe(float cantidad, float vidaMaxima)
        {
            return vidaMaxima > 0f && cantidad >= vidaMaxima * FraccionDanoQueInterrumpe;
        }

        // ---------- Capa de arriba ----------

        /// <summary>
        /// Peso al que va la capa de brazos y torso (armas y puños): 0 al caer, cubrirse, agacharse sin apuntar (salvo que lance una granada), ver el celular, en un gesto o
        /// en el aire; manejando solo al pitar; si no, 1. Al aterrizar vuelve a 1 sola.
        /// </summary>
        public static float PesoObjetivoCapaArriba(bool caido, bool cubierto, bool agachado, bool apuntando, bool lanzandoGranada, bool viendoTelefono, bool enGesto, bool enSuelo, bool manejando, bool pitando)
        {
            if (manejando)
            {
                return pitando && !caido ? 1f : 0f;
            }
            bool agachadoSinBrazos = agachado && !apuntando && !lanzandoGranada;
            return caido || cubierto || agachadoSinBrazos || viendoTelefono || enGesto || !enSuelo ? 0f : 1f;
        }

        // ---------- Carro ----------

        /// <summary>Cuadros de PJ01_Carro_SacarConductor (v4): pasa directo a PJ01_Carro_Entrar al terminar.</summary>
        public const int CuadrosSacarConductor = 98;
        /// <summary>Cuadros que el conductor arranca después que el jugador (PJ01_Carro_SacarConductor frames 1-17 = paso de Carro_Entrar; 0.75 s).</summary>
        public const int CuadrosRetrasoConductor = 18;
        public const float SegundosSacarConductor = CuadrosSacarConductor / FotogramasPorSegundo;
        public const float SegundosRetrasoConductor = CuadrosRetrasoConductor / FotogramasPorSegundo;
        /// <summary>NPC_SacadoDelCarro_Huir (174 cuadros): sacado 1-38, cae 39-74, se levanta 85-144, huye corriendo 145-174.</summary>
        public const int CuadrosConductorHuir = 174;
        public const int CuadroConductorHuye = 145;

        /// <summary>Segundos desde que empieza el clip del jugador hasta que el conductor empieza a huir corriendo (para Trafico/ConductorRobado).</summary>
        public static float SegundosHastaQueConductorHuye()
        {
            return SegundosRetrasoConductor + (CuadroConductorHuye - 1) / FotogramasPorSegundo;
        }

        // ---------- Interacciones ----------

        /// <summary>Interacciones que antes de empezar guardan el arma y sacan al personaje del modo pelea.</summary>
        public static bool InteraccionGuardaArma(GestoVisual gesto)
        {
            switch (gesto)
            {
                case GestoVisual.AbrirPuerta:
                case GestoVisual.Boton:
                case GestoVisual.Recoger:
                case GestoVisual.Beber:
                case GestoVisual.Sentarse:
                case GestoVisual.CamillaAcostarse:
                case GestoVisual.ColgadoSubir:
                case GestoVisual.Colgarse:
                case GestoVisual.CarroEntrar:
                case GestoVisual.CarroSalir:
                case GestoVisual.MotoSubir:
                case GestoVisual.MotoBajar:
                    return true;
                default:
                    return false;
            }
        }

        // ---------- NPC ----------

        /// <summary>Segundos de mezcla al pasar de platicar, discutir o hablar por celular a asustarse o huir (no de golpe).</summary>
        public const float SegundosMezclaNpc = 0.2f;
        /// <summary>Segundos que un civil herido pide ayuda (NPC_Herido_PedirAyuda en bucle) antes de volver a lo suyo.</summary>
        public const float SegundosPedirAyuda = 6f;

        /// <summary>Qué hace un civil herido que no murió: la mitad pide ayuda y la otra mitad sigue herido de pie (PJ01_Herido_Idle). Determinista por identificador.</summary>
        public static bool HeridoPideAyuda(int identificador)
        {
            return (identificador & 1) == 0;
        }

        /// <summary>El E16 solo reacciona con E16_Bloquear_Tambaleo a golpes fuertes: ese fracción de su vida o más.</summary>
        public const float FraccionGolpeFuerteRobot = 0.1f;

        public static bool GolpeFuerteParaRobot(float cantidad, float vidaMaxima)
        {
            return vidaMaxima > 0f && cantidad >= vidaMaxima * FraccionGolpeFuerteRobot;
        }

        /// <summary>El E15 (pequeño) no usa encenderse ni bloquear; el E16 sí.</summary>
        public static bool RobotUsaBloquear(string nombreRobot)
        {
            return nombreRobot != null && nombreRobot.StartsWith("E16", StringComparison.Ordinal);
        }
    }
}
