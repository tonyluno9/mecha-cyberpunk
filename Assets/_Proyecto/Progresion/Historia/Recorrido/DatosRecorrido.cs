using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625: lo que el recorrido necesita de assets: las 9 cinemáticas y los prefabs de los dos SKULLS del paso 3. Vive en Resources/DatosRecorrido.
    /// Si falta, el recorrido usa <see cref="TablaRecorrido"/> para las cinemáticas y se salta la escena de los SKULLS con un warning.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del recorrido", fileName = "DatosRecorrido")]
    public class DatosRecorrido : ScriptableObject
    {
        public const string NombreRecurso = "DatosRecorrido";

        [Tooltip("C01 a C09 en cualquier orden; se buscan por id.")]
        [SerializeField] private DatosCinematica[] cinematicas = System.Array.Empty<DatosCinematica>();
        [Tooltip("Prefabs de SKULLS normales (con PandilleroAPie) para el paso 3. Se usan dos: el primero y el segundo (si hay uno solo, se repite).")]
        [SerializeField] private GameObject[] prefabsSkull = System.Array.Empty<GameObject>();
        [Tooltip("Distancia (m) a la que salen los SKULLS delante del jugador en el paso 3.")]
        [Min(4f)] [SerializeField] private float distanciaAparicionSkulls = 10f;
        [Tooltip("Metros que debe alejarse de donde despierta para que le salgan los SKULLS si no hay marcador Historia_Molestia.")]
        [Min(5f)] [SerializeField] private float metrosParaMolestia = 22f;
        [Tooltip("Metros desde donde despierta hasta la ranura si no hay marcador Historia_Ranura.")]
        [Min(10f)] [SerializeField] private float metrosHastaRanura = 45f;

        public IReadOnlyList<GameObject> PrefabsSkull => prefabsSkull;
        public float DistanciaAparicionSkulls => distanciaAparicionSkulls;
        public float MetrosParaMolestia => metrosParaMolestia;
        public float MetrosHastaRanura => metrosHastaRanura;

        public DatosCinematica Buscar(string id)
        {
            foreach (DatosCinematica c in cinematicas)
            {
                if (c != null && c.Id == id)
                {
                    return c;
                }
            }
            return null;
        }

        /// <summary>Para crear los datos desde código (editor).</summary>
        public void Configurar(DatosCinematica[] nuevasCinematicas, GameObject[] nuevosSkulls)
        {
            cinematicas = nuevasCinematicas;
            prefabsSkull = nuevosSkulls;
        }
    }
}
