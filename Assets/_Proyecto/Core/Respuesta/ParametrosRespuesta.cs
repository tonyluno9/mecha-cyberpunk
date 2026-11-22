using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>T-1620: todos los números del director de respuesta (datos serializables; los guarda <see cref="ConfigRespuesta"/>).</summary>
    [Serializable]
    public class ParametrosRespuesta
    {
        [Tooltip("Topes y cooldowns por tipo de unidad.")]
        public AjusteTipoRespuesta[] Tipos = new AjusteTipoRespuesta[0];
        [Tooltip("Máximo de unidades vivas de respuesta en total.")]
        public int TopeTotal = 8;
        [Tooltip("Nivel más alto de la jerarquía (1 a 8) al que PUEDE llegar la respuesta con 1, 2, 3, 4 y 5 estrellas.")]
        public int[] NivelMaximoPorEstrellas = { 1, 2, 3, 5, 8 };
        [Tooltip("Segundos que un nivel tiene para detenerte; pasado el tiempo (y sigues ahí) se desbloquea el siguiente (s).")]
        public float SegundosParaSubir = 45f;
        [Tooltip("Segundos que tarda en llegar la primera unidad de un nivel recién desbloqueado (s).")]
        public float RetrasoNivelNuevo = 10f;
        [Tooltip("Cada cuántas unidades perdidas de un tipo se permite una más a la vez.")]
        public int PerdidasParaUnoMas = 2;
        [Tooltip("Segundos mínimos entre dos unidades nuevas del mismo tipo (nada de golpe) (s).")]
        public float SegundosEntreGenerados = 4f;
        [Tooltip("Estrellas con las que responde NOVA en una misión protegida que la permite.")]
        public int EstrellasNovaEnMision = 4;
        [Tooltip("T-1631 (T17): con estas estrellas (el máximo) NOVA INTERVIENE: deja de llegar policía, la que hay se retira y llega solo Nova.")]
        public int EstrellasModoNova = 5;
        [Tooltip("T-1631: máximo de unidades de NOVA vivas a la vez (humanoides, medianos, perros, grandes y drones de Nova; el jefe no cuenta) en modo Nova.")]
        public int TopeNovaVivos = 3;
        [Tooltip("T-1631: unidades de un nivel de NOVA que hay que vencer para que se desbloquee el siguiente en modo Nova (el tiempo también lo desbloquea).")]
        public int PerdidasNovaParaSubir = 2;
        [Tooltip("T-1631: nivel de la jerarquía (1 a 8) desde el que llegan los drones de NOVA en cápsula en modo Nova.")]
        public int NivelDronNova = 6;
        [Tooltip("T-1631: un policía a pie que se retira solo desaparece si está al menos a esta distancia del jugador y fuera de su vista (m).")]
        public float MetrosMinimosRetiroPolicia = 15f;
        [Tooltip("Escribe una línea [Respuesta] en consola por cada unidad que sale o se retira.")]
        public bool Registrar = true;

        public AjusteTipoRespuesta AjusteDe(TipoRespuesta tipo)
        {
            if (Tipos != null)
            {
                foreach (AjusteTipoRespuesta a in Tipos)
                {
                    if (a != null && a.Tipo == tipo)
                    {
                        return a;
                    }
                }
            }
            return null;
        }

        /// <summary>Los valores de la tarea T8.</summary>
        public static ParametrosRespuesta PorOmision()
        {
            return new ParametrosRespuesta
            {
                Tipos = new[]
                {
                    new AjusteTipoRespuesta(TipoRespuesta.PoliciaAPie, 6, 6, 0f, 0f),
                    new AjusteTipoRespuesta(TipoRespuesta.Patrulla, 3, 1, 30f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.Camioneta, 2, 1, 35f, 45f),
                    new AjusteTipoRespuesta(TipoRespuesta.Dron, 2, 1, 25f, 35f),
                    new AjusteTipoRespuesta(TipoRespuesta.NovaHumanoide, 2, 1, 20f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.RobotMediano, 1, 1, 25f, 35f),
                    new AjusteTipoRespuesta(TipoRespuesta.Perro, 2, 1, 20f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.RobotGrande, 1, 1, 40f, 60f),
                    new AjusteTipoRespuesta(TipoRespuesta.Jefe, 1, 1, 120f, 120f)
                }
            };
        }
    }
}
