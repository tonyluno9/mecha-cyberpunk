using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Un paso de una misión (T-183): qué hacer, dónde y cuánto calor suma al cumplirse.</summary>
    [Serializable]
    public class PasoMision
    {
        [Tooltip("Qué hay que hacer.")]
        [SerializeField] private TipoPaso tipo = TipoPaso.IrA;

        [Tooltip("Id del punto de misión (prefijo Mision_). Vacío en Escapar.")]
        [SerializeField] private string idPunto = "";

        [Tooltip("Texto que ve el jugador en pantalla (texto corto).")]
        [SerializeField] private string texto = "";

        [Tooltip("Calor que suma al cumplir el paso (puntos de calor, 0 a 100).")]
        [Range(0f, 100f)] [SerializeField] private float calor;

        [Tooltip("Radio para dar por llegado en un paso IrA (metros).")]
        [Min(0.5f)] [SerializeField] private float radio = 4f;

        public TipoPaso Tipo => tipo;
        public string IdPunto => idPunto ?? "";
        public string Texto => texto ?? "";
        public float Calor => Mathf.Max(0f, calor);
        public float Radio => Mathf.Max(0.5f, radio);

        public PasoMision() { }

        public PasoMision(TipoPaso tipo, string idPunto, string texto, float calor = 0f, float radio = 4f)
        {
            this.tipo = tipo;
            this.idPunto = idPunto ?? "";
            this.texto = texto ?? "";
            this.calor = calor;
            this.radio = radio;
        }
    }
}
