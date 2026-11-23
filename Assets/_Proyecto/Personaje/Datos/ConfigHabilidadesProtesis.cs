using System;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Tiempos de una habilidad de prótesis: cuánto dura con el implante nivel I y II y cuánto tarda en recargar (s).</summary>
    [Serializable]
    public struct TiemposHabilidad
    {
        [Min(0.1f)] public float duracionNivel1;
        [Min(0.1f)] public float duracionNivel2;
        [Min(0.1f)] public float recarga;
    }

    /// <summary>
    /// Datos de las habilidades de prótesis (T-992). Diseño en docs/diseno/rueda-y-habilidades.md. Se carga con Resources
    /// (<see cref="NombreRecurso"/>), así que no hace falta tocar el prefab del jugador.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config habilidades de prótesis", fileName = "ConfigHabilidadesProtesis")]
    public class ConfigHabilidadesProtesis : ScriptableObject
    {
        public const string NombreRecurso = "ConfigHabilidadesProtesis";

        [Header("Tiempos")]
        [SerializeField] private TiemposHabilidad piernas = new TiemposHabilidad { duracionNivel1 = 6f, duracionNivel2 = 10f, recarga = 20f };
        [SerializeField] private TiemposHabilidad brazos = new TiemposHabilidad { duracionNivel1 = 6f, duracionNivel2 = 10f, recarga = 25f };
        [SerializeField] private TiemposHabilidad cabeza = new TiemposHabilidad { duracionNivel1 = 8f, duracionNivel2 = 14f, recarga = 30f };
        [Tooltip("T-1017: el brazo láser dispara mientras dura.")]
        [SerializeField] private TiemposHabilidad laser = new TiemposHabilidad { duracionNivel1 = 4f, duracionNivel2 = 4f, recarga = 16f };
        [Tooltip("T-1017: la hombrera lanza sus misiles al activarse; la duración es cuánto se ve activa.")]
        [SerializeField] private TiemposHabilidad misiles = new TiemposHabilidad { duracionNivel1 = 1f, duracionNivel2 = 1f, recarga = 22f };

        [Header("PIERNAS")]
        [Tooltip("Multiplica la velocidad al correr mientras dura.")]
        [Min(1f)] [SerializeField] private float factorCorrer = 1.5f;
        [Tooltip("Multiplica la velocidad al caminar mientras dura.")]
        [Min(1f)] [SerializeField] private float factorCaminar = 1.3f;
        [Tooltip("Multiplica la altura del salto mientras dura (súper salto).")]
        [Min(1f)] [SerializeField] private float factorSalto = 2.2f;

        [Header("BRAZOS")]
        [Tooltip("Multiplica el daño del golpe a pie mientras dura.")]
        [Min(1f)] [SerializeField] private float factorGolpe = 2.5f;
        [Tooltip("Impulso (m/s) que se le da al cuerpo con física que recibe el golpe mientras dura. 0 = sin empuje.")]
        [Min(0f)] [SerializeField] private float empujeGolpe = 6f;
        [Tooltip("Multiplica el tiempo de recarga del arma mientras dura (0.4 = recarga en 40 % del tiempo).")]
        [Range(0.05f, 1f)] [SerializeField] private float factorTiempoRecarga = 0.4f;

        [Tooltip("T-1658: con BRAZOS el golpe es más rápido que el normal (ya ralentizado en T33): multiplica la rapidez de la animación y acorta preparación y recarga. 1.2 = a 0.9x de la rapidez de antes de T33.")]
        [Range(1f, 2f)] [SerializeField] private float factorRapidezGolpe = 1.2f;
        [Tooltip("T-1658: masa (kg) desde la cual un cuerpo con física cuenta como pesado (un carro) y recibe el empuje pequeño en vez del normal.")]
        [Min(1f)] [SerializeField] private float masaCuerpoPesado = 150f;
        [Tooltip("T-1658: impulso (m/s) al golpear con BRAZOS un cuerpo pesado, como un carro: lo mueve unos centímetros sin sacarlo volando.")]
        [Min(0f)] [SerializeField] private float empujeCuerpoPesado = 0.6f;

        [Header("LÁSER (T-1017)")]
        [Tooltip("Daño por segundo del brazo láser a todo lo que toca (puntos/s).")]
        [Min(0f)] [SerializeField] private float danioLaser = 80f;
        [Tooltip("Alcance del brazo láser (m).")]
        [Min(1f)] [SerializeField] private float alcanceLaser = 45f;
        [Tooltip("Ancho del rayo del brazo (m).")]
        [Min(0.01f)] [SerializeField] private float anchoLaser = 0.08f;
        [Tooltip("Radio con que el láser toca lo que cruza (m).")]
        [Min(0.01f)] [SerializeField] private float radioLaser = 0.2f;
        [SerializeField] private Color colorLaser = new Color(1f, 0.15f, 0.2f, 1f);

        [Header("MISILES (T-1017)")]
        [Tooltip("Misiles que lanza la hombrera cada vez.")]
        [Range(1, 12)] [SerializeField] private int cantidadMisiles = 4;
        [Tooltip("Segundos entre un misil y el siguiente (s).")]
        [Min(0f)] [SerializeField] private float segundosEntreMisiles = 0.15f;
        [Tooltip("Daño de la explosión de cada misil (puntos, en el centro).")]
        [Min(0f)] [SerializeField] private float danioMisil = 90f;
        [Tooltip("Radio de la explosión de cada misil (m).")]
        [Min(0.1f)] [SerializeField] private float radioMisil = 3.5f;
        [Tooltip("Velocidad de vuelo del misil (m/s).")]
        [Min(1f)] [SerializeField] private float velocidadMisil = 20f;
        [Tooltip("Cuánto gira el misil hacia su blanco (°/s).")]
        [Min(1f)] [SerializeField] private float giroMisil = 220f;
        [Tooltip("Distancia a la que busca blancos (m).")]
        [Min(1f)] [SerializeField] private float busquedaMisil = 45f;
        [Tooltip("Semiángulo del cono al frente de la cámara donde busca blancos (°).")]
        [Range(5f, 180f)] [SerializeField] private float conoMisil = 75f;
        [Tooltip("Segundos que vuela un misil antes de explotar solo (s).")]
        [Min(0.5f)] [SerializeField] private float vidaMisil = 5f;

        [Header("Entrada")]
        [Tooltip("Asset de entrada del jugador a pie (ControlesAPie): mapa APie, acciones HabilidadPiernas, HabilidadBrazos y HabilidadCabeza.")]
        [SerializeField] private InputActionAsset controles;

        public float FactorCorrer => factorCorrer;
        public float FactorCaminar => factorCaminar;
        public float FactorSalto => factorSalto;
        public float FactorGolpe => factorGolpe;
        public float EmpujeGolpe => empujeGolpe;
        public float FactorTiempoRecarga => factorTiempoRecarga;
        public float FactorRapidezGolpe => factorRapidezGolpe;
        public float MasaCuerpoPesado => masaCuerpoPesado;
        public float EmpujeCuerpoPesado => empujeCuerpoPesado;
        public InputActionAsset Controles => controles;
        public float DanioLaser => danioLaser;
        public float AlcanceLaser => alcanceLaser;
        public float AnchoLaser => anchoLaser;
        public float RadioLaser => radioLaser;
        public Color ColorLaser => colorLaser;
        public int CantidadMisiles => cantidadMisiles;
        public float SegundosEntreMisiles => segundosEntreMisiles;
        public float DanioMisil => danioMisil;
        public float RadioMisil => radioMisil;
        public float VelocidadMisil => velocidadMisil;
        public float GiroMisil => giroMisil;
        public float BusquedaMisil => busquedaMisil;
        public float ConoMisil => conoMisil;
        public float VidaMisil => vidaMisil;

        public TiemposHabilidad Tiempos(HabilidadProtesis habilidad)
        {
            switch (habilidad)
            {
                case HabilidadProtesis.Piernas: return piernas;
                case HabilidadProtesis.Brazos: return brazos;
                case HabilidadProtesis.Laser: return laser;
                case HabilidadProtesis.Misiles: return misiles;
                default: return cabeza;
            }
        }

        /// <summary>El nombre de la acción de entrada de cada habilidad.</summary>
        public static string NombreAccion(HabilidadProtesis habilidad)
        {
            switch (habilidad)
            {
                case HabilidadProtesis.Piernas: return "HabilidadPiernas";
                case HabilidadProtesis.Brazos: return "HabilidadBrazos";
                case HabilidadProtesis.Laser: return "HabilidadLaser";
                case HabilidadProtesis.Misiles: return "HabilidadMisiles";
                default: return "HabilidadCabeza";
            }
        }

#if UNITY_EDITOR
        /// <summary>Solo para el creador del editor.</summary>
        public void Llenar(InputActionAsset asset) => controles = asset;
#endif
    }
}
