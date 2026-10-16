using System;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Datos
{
    /// <summary>
    /// Datos de una emboscada (T-071): quién aparece, dónde (relativo al prefab, +Z hacia el fondo del callejón),
    /// cuándo se activa y qué dice el objetivo.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Pelea/Datos emboscada", fileName = "DatosEmboscada")]
    public class DatosEmboscada : ScriptableObject
    {
        /// <summary>Pose de un rival relativa al prefab de la emboscada.</summary>
        [Serializable]
        public struct PoseRival
        {
            [Tooltip("Posición local al prefab (m). +Z es hacia el fondo.")]
            public Vector3 posicion;
            [Tooltip("Giro en Y local (grados). 180 = mira hacia la entrada.")]
            public float giroY;
        }

        [Tooltip("Prefab del rival (IA/Prefabs/RivalPandilla). Debe tener un IPeleador.")]
        [SerializeField] private GameObject prefabRival;
        [Tooltip("Dónde aparece cada rival, relativo al prefab.")]
        [SerializeField] private PoseRival[] poses =
        {
            new PoseRival { posicion = new Vector3(-1.5f, 0.05f, 14f), giroY = 180f },
            new PoseRival { posicion = new Vector3(1.5f, 0.05f, 14f), giroY = 180f }
        };
        [Tooltip("Distancia horizontal al prefab a la que se activa sola (m).")]
        [Min(0.1f)] [SerializeField] private float radioActivacion = 12f;
        [Tooltip("Segundos entre que empieza y la pandilla puede actuar.")]
        [Min(0f)] [SerializeField] private float segundosAparicion = 2f;
        [Tooltip("Segundos que un rival caído se queda en el piso antes de retirarse.")]
        [Min(0f)] [SerializeField] private float segundosRetiroCuerpo = 4f;
        [Tooltip("Distancia (m) entre el punto de subida del mecha y una llegada de llamado para considerar que el mecha ya llegó.")]
        [Min(0.1f)] [SerializeField] private float metrosMechaLlegado = 6f;
        [Tooltip("Si es verdadero, fija el objetivo de la historia en cada fase.")]
        [SerializeField] private bool fijaObjetivos = true;
        [SerializeField] private string textoLlamar = "Llama a tu mecha (M)";
        [SerializeField] private string textoSubir = "Sube a tu mecha";
        [SerializeField] private string textoDerrotar = "Derrota a la pandilla";

        public GameObject PrefabRival => prefabRival;
        public PoseRival[] Poses => poses;
        public float RadioActivacion => radioActivacion;
        public float SegundosAparicion => segundosAparicion;
        public float SegundosRetiroCuerpo => segundosRetiroCuerpo;
        public float MetrosMechaLlegado => metrosMechaLlegado;
        public bool FijaObjetivos => fijaObjetivos;
        public string TextoLlamar => textoLlamar;
        public string TextoSubir => textoSubir;
        public string TextoDerrotar => textoDerrotar;
    }
}
