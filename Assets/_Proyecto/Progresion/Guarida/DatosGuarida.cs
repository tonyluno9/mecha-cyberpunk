using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Los números de la guarida (T-448): las tres mejoras (armero, garaje y cama), su precio y lo que da cada nivel. Los niveles van del 1 al 3;
    /// el precio del nivel n es <c>precio * factoresPrecio[n - 1]</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de la guarida", fileName = "DatosGuarida_Nuevo")]
    public class DatosGuarida : ScriptableObject
    {
        public const string IdArmero = "armero";
        public const string IdGaraje = "garaje";
        public const string IdCama = "cama";

        [Serializable]
        public struct Mejora
        {
            public string id;
            public string nombre;
            [Min(1)] public int precio;
        }

        [SerializeField] private Mejora[] mejoras = Array.Empty<Mejora>();
        [Tooltip("Precio del nivel 1, 2 y 3 como múltiplo del precio de la mejora.")]
        [SerializeField] private float[] factoresPrecio = { 1f, 2f, 4f };
        [Tooltip("ARMERO: fracción extra de daño de las armas de fuego a pie en el nivel 1, 2 y 3.")]
        [SerializeField] private float[] bonoDanioArmero = { 0.08f, 0.16f, 0.25f };
        [Tooltip("GARAJE: cuántas veces la vida normal aguanta el carro en el nivel 1, 2 y 3.")]
        [SerializeField] private float[] factorVidaGaraje = { 1f, 1.6f, 2.4f };
        [Tooltip("CAMA: fracción del calor que se borra al dormir en el nivel 1, 2 y 3 (el nivel 1 solo cura).")]
        [SerializeField] private float[] calorBorradoCama = { 0f, 0.5f, 1f };

        public Mejora[] Mejoras => mejoras;
        public float[] FactoresPrecio => factoresPrecio;
        public float[] BonoDanioArmero => bonoDanioArmero;
        public float[] FactorVidaGaraje => factorVidaGaraje;
        public float[] CalorBorradoCama => calorBorradoCama;
        public int NivelMaximo => Mathf.Max(1, factoresPrecio.Length);

        public int IndiceDe(string id)
        {
            for (int i = 0; i < mejoras.Length; i++)
            {
                if (mejoras[i].id == id)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
