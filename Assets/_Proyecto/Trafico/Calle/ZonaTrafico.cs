using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1345: una zona de la ciudad (caja orientada) que le dice al tráfico qué carros sacar ahí. Su <see cref="Nombre"/> casa con una zona de ConfigCarrosZonas.
    /// </summary>
    public class ZonaTrafico : MonoBehaviour, IZonaDelMundo
    {
        private static readonly List<ZonaTrafico> activas = new List<ZonaTrafico>();

        [SerializeField] private string nombre = "";
        [Tooltip("Tamaño de la caja en el espacio local de la zona (m).")]
        [SerializeField] private Vector3 tamano = new Vector3(80f, 20f, 80f);

        /// <summary>Las zonas prendidas ahora (para no buscarlas cada vez).</summary>
        public static IReadOnlyList<ZonaTrafico> Activas => activas;

        public string Nombre => nombre;
        public Vector3 Tamano => tamano;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => activas.Clear();

        private void OnEnable()
        {
            activas.Add(this);
            Registro<IZonaDelMundo>.Agregar(this); // T-1409: los peatones preguntan por la zona sin referenciar Trafico
        }

        private void OnDisable()
        {
            activas.Remove(this);
            Registro<IZonaDelMundo>.Quitar(this);
        }

        string IZonaDelMundo.NombreZona => nombre;

        bool IZonaDelMundo.ContienePunto(Vector3 mundo) => Contiene(mundo);

        /// <summary>Lo usan el creador de editor y las pruebas.</summary>
        public void Definir(string nuevoNombre, Vector3 nuevoTamano)
        {
            nombre = nuevoNombre;
            tamano = nuevoTamano;
        }

        /// <summary>Si el punto del mundo cae dentro de la caja orientada (sin física).</summary>
        public bool Contiene(Vector3 mundo)
        {
            Vector3 local = transform.InverseTransformPoint(mundo);
            Vector3 mitad = tamano * 0.5f;
            return Mathf.Abs(local.x) <= mitad.x && Mathf.Abs(local.y) <= mitad.y && Mathf.Abs(local.z) <= mitad.z;
        }

        /// <summary>La primera zona activa que contiene el punto, o null.</summary>
        public static ZonaTrafico De(Vector3 mundo)
        {
            for (int i = 0; i < activas.Count; i++)
            {
                if (activas[i].Contiene(mundo))
                {
                    return activas[i];
                }
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, tamano);
        }
    }
}
