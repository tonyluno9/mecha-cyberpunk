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

        [Header("Entrada")]
        [Tooltip("Asset de entrada del jugador a pie (ControlesAPie): mapa APie, acciones HabilidadPiernas, HabilidadBrazos y HabilidadCabeza.")]
        [SerializeField] private InputActionAsset controles;

        public float FactorCorrer => factorCorrer;
        public float FactorCaminar => factorCaminar;
        public float FactorSalto => factorSalto;
        public float FactorGolpe => factorGolpe;
        public float EmpujeGolpe => empujeGolpe;
        public float FactorTiempoRecarga => factorTiempoRecarga;
        public InputActionAsset Controles => controles;

        public TiemposHabilidad Tiempos(HabilidadProtesis habilidad)
        {
            switch (habilidad)
            {
                case HabilidadProtesis.Piernas: return piernas;
                case HabilidadProtesis.Brazos: return brazos;
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
                default: return "HabilidadCabeza";
            }
        }

#if UNITY_EDITOR
        /// <summary>Solo para el creador del editor.</summary>
        public void Llenar(InputActionAsset asset) => controles = asset;
#endif
    }
}
