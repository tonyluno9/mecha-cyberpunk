using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Los negocios que puedes tomar en tus zonas y lo que pagan por día de juego (T-429). Asset: DatosNegocios_Base.
    /// Equilibrio con El Mercado (armas de Ƀ150 a Ƀ900, renta Ƀ500 cada 7 días): ver D-tony-302.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de negocios", fileName = "DatosNegocios_Nuevo")]
    public class DatosNegocios : ScriptableObject
    {
        [Serializable]
        public struct Negocio
        {
            public string id;
            public string nombre;
            [Tooltip("Id de la zona donde está; si la zona deja de ser tuya, deja de pagar.")]
            public string zona;
            [Min(1)] public int precio;
            [Tooltip("Biits por día de juego en el nivel 1.")]
            [Min(0)] public int ingresoBase;
        }

        [SerializeField] private Negocio[] negocios = Array.Empty<Negocio>();
        [Tooltip("Multiplicador del ingreso en el nivel 1, 2 y 3.")]
        [SerializeField] private float[] multiplicadores = { 1f, 1.6f, 2.4f };
        [Tooltip("Precio de pasar del nivel 1 al 2 y del 2 al 3, como múltiplo del precio de tomarlo.")]
        [SerializeField] private float[] factoresMejora = { 1.25f, 2f };

        public Negocio[] Negocios => negocios;
        public float[] Multiplicadores => multiplicadores;
        public float[] FactoresMejora => factoresMejora;
        public int NivelMaximo => Mathf.Max(1, multiplicadores.Length);

        public int IndiceDe(string id)
        {
            for (int i = 0; i < negocios.Length; i++)
            {
                if (negocios[i].id == id)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
