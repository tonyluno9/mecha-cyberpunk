using System;
using MechaCyberpunk.Economia;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Datos
{
    /// <summary>
    /// Una pelea de la cartelera: rival, premio, apuesta, presentador, liga y si es de jefe (T-155). No es ScriptableObject: vivirá
    /// dentro del calendario de peleas. <see cref="Flujo.DirectorPelea.PrepararCartel"/> lo aplica antes de cada pelea.
    /// </summary>
    [Serializable]
    public class CartelPelea
    {
        [Tooltip("Prefab del rival (IA/Prefabs/RivalBasico) que se crea para esta pelea.")]
        public GameObject prefabRival;
        [Min(0)] public int premioGanar;
        [Min(0)] public int premioPerder;
        [Tooltip("Opcional: reglas de apuesta. Sin esto, la pelea no tiene apuesta.")]
        public DatosApuestaPelea apuesta;
        [Tooltip("Opcional: lo que dice el presentador. Sin esto, la pelea no tiene presentación.")]
        public ConfigPresentador presentador;
        [Range(0, 4)] public int liga;
        public bool esJefe;

        public static CartelPelea Crear(GameObject prefabRival, int premioGanar, int premioPerder = 0, int liga = 0, bool esJefe = false,
            DatosApuestaPelea apuesta = null, ConfigPresentador presentador = null)
        {
            return new CartelPelea
            {
                prefabRival = prefabRival,
                premioGanar = Mathf.Max(0, premioGanar),
                premioPerder = Mathf.Max(0, premioPerder),
                apuesta = apuesta,
                presentador = presentador,
                liga = Mathf.Clamp(liga, 0, 4),
                esJefe = esJefe
            };
        }
    }
}
