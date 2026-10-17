using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>Las reglas de la apuesta de una pelea: momio, tope, montos que ofrece la pantalla y la pieza en juego (T-144).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Economia/Datos apuesta de pelea", fileName = "DatosApuestaPelea")]
    public class DatosApuestaPelea : ScriptableObject
    {
        // Respaldo si el momio quedó en 0 o negativo (el contrato pide que sea mayor que 0).
        private const float MomioDeRespaldo = 1f;

        [Tooltip("Lo que gana el jugador por cada peso apostado si gana (1.0 = paga uno a uno). Mayor que 0.")]
        [Min(0.01f)] [SerializeField] private float momio = 1f;

        [Tooltip("Lo máximo que se puede apostar en la pelea ($).")]
        [Min(1)] [SerializeField] private int apuestaMaxima = 500;

        [Tooltip("Montos que ofrece la pantalla de apuesta, en orden ($).")]
        [SerializeField] private int[] montos = { 100, 250, 500 };

        [Tooltip("Opcional: la pieza que el rival pone en juego y que se lleva el jugador si gana apostando lo suficiente.")]
        [SerializeField] private DatosObjeto piezaEnJuego;

        [Tooltip("Lo mínimo que hay que apostar para llevarse la pieza ($).")]
        [Min(0)] [SerializeField] private int apuestaMinimaPorPieza = 250;

        public float Momio => momio > 0f ? momio : MomioDeRespaldo;
        public int ApuestaMaxima => Mathf.Max(0, apuestaMaxima);
        public int[] Montos => montos ?? System.Array.Empty<int>();
        public DatosObjeto PiezaEnJuego => piezaEnJuego;
        public int ApuestaMinimaPorPieza => Mathf.Max(0, apuestaMinimaPorPieza);

        /// <summary>Crea unos datos en memoria (para pruebas y herramientas).</summary>
        public static DatosApuestaPelea Crear(float momio, int apuestaMaxima, int[] montos, DatosObjeto pieza, int minimaPorPieza)
        {
            var d = CreateInstance<DatosApuestaPelea>();
            d.momio = momio;
            d.apuestaMaxima = apuestaMaxima;
            d.montos = montos;
            d.piezaEnJuego = pieza;
            d.apuestaMinimaPorPieza = minimaPorPieza;
            return d;
        }
    }
}
