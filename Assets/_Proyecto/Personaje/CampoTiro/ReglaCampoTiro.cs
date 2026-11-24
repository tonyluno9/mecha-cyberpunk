using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): reglas puras del campo de tiro: medidas de la sala, vida de cada objetivo, dónde se colocan según cuántos y a qué distancia,
    /// cuándo reaparecen y la precisión. Todas las coordenadas son locales a la sala (el centro del campo es el origen).
    /// </summary>
    public static class ReglaCampoTiro
    {
        public const int MinimoObjetivos = 1;
        public const int MaximoObjetivos = 5;
        public const float SegundosReaparicion = 4f;
        public const float SeparacionObjetivos = 3f;
        public const float MitadAncho = 20f;
        public const float FondoAtras = -12f;
        public const float FondoAdelante = 44f;
        public const float AlturaSala = 12f;
        public const float ZBarrera = -4f;
        public const float ZInicioJugador = -10f;
        public const float MargenPared = 1f;
        public const float MargenBarrera = 0.9f;
        /// <summary>
        /// Dónde está la terminal de configuración (locales a la sala): a la derecha del punto de salida, casi a la par (V11: antes en 3.2, -9,
        /// justo enfrente a la derecha, y tapaba el arma del HUD y la de la mano en VR). Se ve al voltear a la derecha.
        /// </summary>
        public const float XTerminal = 4.5f;
        public const float ZTerminal = -9.5f;
        /// <summary>Giro de la terminal (°): su cara mira hacia -X, al punto de salida del jugador.</summary>
        public const float GiroTerminal = -90f;
        public const float MetrosLetrero = 2.5f;

        public static int LimitarCuantos(int cuantos) => Mathf.Clamp(cuantos, MinimoObjetivos, MaximoObjetivos);

        /// <summary>Metros desde la barrera hasta los objetivos.</summary>
        public static float Metros(DistanciaTiro distancia)
        {
            switch (distancia)
            {
                case DistanciaTiro.Cerca: return 8f;
                case DistanciaTiro.Media: return 18f;
                default: return 32f;
            }
        }

        public static float Vida(TipoObjetivoTiro tipo)
        {
            switch (tipo)
            {
                case TipoObjetivoTiro.Centinela: return 120f;
                case TipoObjetivoTiro.RobotPesado: return 400f;
                case TipoObjetivoTiro.Perro: return 80f;
                case TipoObjetivoTiro.Elite: return 250f;
                case TipoObjetivoTiro.Skull: return 100f;
                case TipoObjetivoTiro.Policia: return 120f;
                default: return 100f;
            }
        }

        /// <summary>Altura (m) de la plataforma bajo el objetivo: los bajos (perro) se suben para que los costales no los tapen.</summary>
        public static float AlturaPlataforma(TipoObjetivoTiro tipo) => tipo == TipoObjetivoTiro.Perro ? 0.9f : 0f;

        public static string Nombre(TipoObjetivoTiro tipo)
        {
            switch (tipo)
            {
                case TipoObjetivoTiro.Centinela: return "CENTINELA";
                case TipoObjetivoTiro.RobotPesado: return "ROBOT PESADO";
                case TipoObjetivoTiro.Perro: return "PERRO ROBOT";
                case TipoObjetivoTiro.Elite: return "ELITE";
                case TipoObjetivoTiro.Skull: return "SKULL";
                case TipoObjetivoTiro.Policia: return "POLICÍA";
                default: return "GUARDIA";
            }
        }

        /// <summary>Posiciones locales de los objetivos: en fila, centrados, a la distancia pedida detrás de la barrera.</summary>
        public static Vector3[] Posiciones(int cuantos, DistanciaTiro distancia)
        {
            int n = LimitarCuantos(cuantos);
            Vector3[] posiciones = new Vector3[n];
            float z = ZBarrera + Metros(distancia);
            for (int i = 0; i < n; i++)
            {
                posiciones[i] = new Vector3((i - (n - 1) * 0.5f) * SeparacionObjetivos, 0f, z);
            }
            return posiciones;
        }

        /// <summary>True si el jugador está a menos de MetrosLetrero (en el plano) de la terminal.</summary>
        public static bool EstaCercaDeLaTerminal(Vector3 jugador, Vector3 terminal)
        {
            jugador.y = terminal.y;
            return (jugador - terminal).sqrMagnitude <= MetrosLetrero * MetrosLetrero;
        }

        public static bool DebeReaparecer(float segundosCaido) => segundosCaido >= SegundosReaparicion;

        /// <summary>Aciertos entre disparos, de 0 a 1 (0 sin disparos).</summary>
        public static float Precision(int aciertos, int disparos) => disparos <= 0 ? 0f : Mathf.Clamp01((float)aciertos / disparos);

        /// <summary>Deja una posición local dentro de la sala y de este lado de la barrera.</summary>
        public static Vector3 LimitarAlJugador(Vector3 local)
        {
            local.x = Mathf.Clamp(local.x, -MitadAncho + MargenPared, MitadAncho - MargenPared);
            local.z = Mathf.Clamp(local.z, FondoAtras + MargenPared, ZBarrera - MargenBarrera);
            return local;
        }

        public static bool EstaLimitado(Vector3 local) => (LimitarAlJugador(local) - local).sqrMagnitude > 0.0001f;
    }
}
