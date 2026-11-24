using UnityEngine;

namespace MechaCyberpunk.VR.Manos
{
    /// <summary>
    /// VR40: lógica pura de las manos MN01 de VR: los 17 huesos y los 9 clips que trae cada FBX, qué pose base lleva cada mano según lo que hace
    /// (arma, granada, cargador, menú), cómo se mezclan las poses con el grip y el gatillo (solo el índice), y el pulso de la emisión cian de la
    /// prótesis de brazos.
    /// </summary>
    public static class ReglaManosMN01VR
    {
        public const string ClipAbierta = "Mano_Abierta";
        public const string ClipPuno = "Mano_Puno";
        public const string ClipGatillo = "Mano_Gatillo";
        public const string ClipAgarrePistola = "Mano_AgarrePistola";
        public const string ClipAgarreArmaLarga = "Mano_AgarreArmaLarga_L";
        public const string ClipCargador = "Mano_Cargador_L";
        public const string ClipGranada = "Mano_Granada";
        public const string ClipSenalar = "Mano_Senalar";
        public const string ClipAbrirCerrar = "Mano_AbrirCerrar";

        /// <summary>Los 9 clips de cada FBX (los 8 de pose y el de prueba en bucle).</summary>
        public static readonly string[] Clips =
        {
            ClipAbierta, ClipPuno, ClipGatillo, ClipAgarrePistola, ClipAgarreArmaLarga, ClipCargador, ClipGranada, ClipSenalar, ClipAbrirCerrar,
        };

        private static readonly string[] Base =
        {
            "Forearm", "Hand", "Thumb1", "Thumb2", "Thumb3", "Index1", "Index2", "Index3", "Middle1", "Middle2", "Middle3",
            "Ring1", "Ring2", "Ring3", "Little1", "Little2", "Little3",
        };

        public const int CantidadHuesos = 17;
        public const int IndiceMano = 1;
        public const int IndiceIndice1 = 5;
        public const int IndiceIndice3 = 7;
        public const int IndiceMedio1 = 8;
        public const int IndiceMenique1 = 14;

        /// <summary>Segundos característicos de la mezcla entre poses (la mano no salta de una a otra).</summary>
        public const float SegundosMezcla = 0.06f;
        /// <summary>Periodo (s) del pulso de la emisión de la prótesis.</summary>
        public const float PeriodoPulso = 1.6f;
        public const float BrilloMinimo = 0.6f;
        public const float BrilloMaximo = 1.6f;
        /// <summary>Cian de la prótesis (HDR: se multiplica por el brillo).</summary>
        public static readonly Color ColorProtesis = new Color(0.1f, 0.9f, 1f, 1f);

        /// <summary>Los nombres de los 17 huesos de una mano (sufijo _R o _L), en el orden de las poses.</summary>
        public static string[] Huesos(bool derecha)
        {
            var nombres = new string[Base.Length];
            string sufijo = derecha ? "_R" : "_L";
            for (int i = 0; i < Base.Length; i++)
            {
                nombres[i] = Base[i] + sufijo;
            }
            return nombres;
        }

        /// <summary>True si el hueso es una falange del índice (lo único que mueve el gatillo).</summary>
        public static bool EsIndice(int hueso) => hueso >= IndiceIndice1 && hueso <= IndiceIndice3;

        /// <summary>El nombre del clip sin el prefijo de la toma de Blender ("Armature|Mano_Puno" → "Mano_Puno").</summary>
        public static string NombreLimpio(string clip)
        {
            if (string.IsNullOrEmpty(clip))
            {
                return clip;
            }
            int barra = clip.LastIndexOf('|');
            return barra >= 0 ? clip.Substring(barra + 1) : clip;
        }

        /// <summary>
        /// La pose base de una mano. Derecha: menú abierto → Señalar; granada en la mano → Granada; arma visible → Pistola (también con armas
        /// largas: la derecha va en la empuñadura); si no, Libre (o Señalar si apunta a una UI sin arma). Izquierda: cargador nuevo en la mano →
        /// Cargador; arma larga en la derecha → apoyo en el guardamanos; si no, Libre.
        /// </summary>
        public static PoseManoVR Elegir(bool derecha, bool menuAbierto, bool apuntaUI, bool armaVisible, bool armaLarga, bool granada, bool cargadorEnMano)
        {
            if (derecha)
            {
                if (menuAbierto)
                {
                    return PoseManoVR.Senalar;
                }
                if (granada)
                {
                    return PoseManoVR.Granada;
                }
                if (armaVisible)
                {
                    return PoseManoVR.Pistola;
                }
                return apuntaUI ? PoseManoVR.Senalar : PoseManoVR.Libre;
            }
            if (cargadorEnMano)
            {
                return PoseManoVR.Cargador;
            }
            if (armaVisible && armaLarga && !menuAbierto)
            {
                return PoseManoVR.ArmaLargaApoyo;
            }
            return PoseManoVR.Libre;
        }

        /// <summary>El clip de la pose base (en Libre es la mezcla Abierta→Puño y se devuelve Abierta).</summary>
        public static string ClipDe(PoseManoVR pose)
        {
            switch (pose)
            {
                case PoseManoVR.Pistola: return ClipAgarrePistola;
                case PoseManoVR.ArmaLargaApoyo: return ClipAgarreArmaLarga;
                case PoseManoVR.Cargador: return ClipCargador;
                case PoseManoVR.Granada: return ClipGranada;
                case PoseManoVR.Senalar: return ClipSenalar;
                default: return ClipAbierta;
            }
        }

        /// <summary>True si en esa pose el gatillo dobla el índice (Libre y Pistola).</summary>
        public static bool GatilloDobla(PoseManoVR pose) => pose == PoseManoVR.Libre || pose == PoseManoVR.Pistola;

        /// <summary>
        /// La rotación objetivo de un hueso: la de la pose base (en Libre, Abierta→Puño por <paramref name="grip"/>), y si es del índice y la pose
        /// lo permite, de ahí hacia Mano_Gatillo por <paramref name="gatillo"/>.
        /// </summary>
        public static Quaternion Rotacion(int hueso, PoseManoVR pose, float grip, float gatillo, Quaternion abierta, Quaternion puno, Quaternion basePose,
            Quaternion gatilloPose)
        {
            Quaternion r = pose == PoseManoVR.Libre ? Quaternion.Slerp(abierta, puno, Mathf.Clamp01(grip)) : basePose;
            if (EsIndice(hueso) && GatilloDobla(pose))
            {
                r = Quaternion.Slerp(r, gatilloPose, Mathf.Clamp01(gatillo));
            }
            return r;
        }

        /// <summary>T87: índice del hueso del antebrazo (raíz del rig; la mano es su único hijo).</summary>
        public const int IndiceAntebrazo = 0;
        /// <summary>T87: escala (uniforme) a la que se encoge el antebrazo oculto: sus vértices quedan a menos de 1 mm del punto de corte.</summary>
        public const float EscalaAntebrazoOculto = 0.001f;
        /// <summary>T87: metros detrás de la muñeca (hacia el codo) donde se juntan los vértices del antebrazo: cierra el puño del guante en punta corta.</summary>
        public const float RetrocesoCorteAntebrazo = 0.03f;

        /// <summary>
        /// T87: cómo ocultar el antebrazo sin mover la mano. El antebrazo (<paramref name="posAntebrazo"/>, <paramref name="rotAntebrazo"/>, locales a su
        /// padre, escala 1) se encoge a <paramref name="escala"/> con su origen en el punto de corte, a <paramref name="retroceso"/> m de la muñeca hacia
        /// el codo; la mano (<paramref name="posMano"/>, local al antebrazo) se recoloca y se contra-escala por 1/<paramref name="escala"/> para que su
        /// pose en el mundo (y la de los dedos) quede idéntica. Devuelve la nueva posición local del antebrazo y la de la mano.
        /// </summary>
        public static void ColapsoAntebrazo(Vector3 posAntebrazo, Quaternion rotAntebrazo, Vector3 posMano, float retroceso, float escala,
            out Vector3 nuevaPosAntebrazo, out Vector3 nuevaPosMano)
        {
            float largo = posMano.magnitude;
            float k = largo > 1e-6f ? Mathf.Clamp01(retroceso / largo) : 0f;
            float s = Mathf.Max(1e-6f, escala);
            nuevaPosAntebrazo = posAntebrazo + rotAntebrazo * (posMano * (1f - k));
            nuevaPosMano = posMano * (k / s);
        }

        /// <summary>Fracción (0..1) del camino hacia la pose objetivo que se recorre en <paramref name="dt"/> s.</summary>
        public static float Mezcla(float dt) => dt <= 0f ? 0f : 1f - Mathf.Exp(-dt / SegundosMezcla);

        /// <summary>Brillo (multiplicador del cian) de la prótesis a los <paramref name="segundos"/>: pulso suave entre el mínimo y el máximo.</summary>
        public static float Pulso(float segundos)
        {
            float onda = 0.5f + 0.5f * Mathf.Sin(segundos * 2f * Mathf.PI / PeriodoPulso);
            return Mathf.Lerp(BrilloMinimo, BrilloMaximo, onda);
        }
    }
}
