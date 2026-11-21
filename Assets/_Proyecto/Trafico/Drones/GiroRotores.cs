using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1397: hace girar cada descendiente cuyo nombre empieza con "Rotor_" sobre su Y local (el pivote del modelo está en el centro de giro).
    /// Solo cambia el visual: no toca colisión ni IA del dron.
    /// </summary>
    public class GiroRotores : MonoBehaviour
    {
        public const string PrefijoRotor = "Rotor_";

        [SerializeField, Min(0f), Tooltip("Velocidad de giro de cada rotor en °/s.")] private float velocidad = 1800f;
        [SerializeField, Tooltip("Si es true, los rotores de índice impar giran al revés (pares contrarrotantes).")] private bool alternarSentido = true;

        [SerializeField, Min(0f), Tooltip("T-1398: más allá de esta distancia (m) a la cámara los rotores no giran. 0 = sin corte.")] private float distanciaMaxima = 120f;

        private const float SegundosEntreControles = 0.5f;

        private Transform[] rotores;
        private Vector3[] ejes;
        private Camera camara;
        private float proximoControl;
        private bool enAlcance = true;

        public float Velocidad => velocidad;
        public float DistanciaMaxima => distanciaMaxima;
        public int CantidadRotores => rotores != null ? rotores.Length : 0;

        private void Awake()
        {
            Buscar();
        }

        /// <summary>Busca los rotores entre los descendientes (también los apagados).</summary>
        public void Buscar()
        {
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t.name.StartsWith(PrefijoRotor))
                {
                    lista.Add(t);
                }
            }
            rotores = lista.ToArray();
            // T-1554: los rotores vienen de Blender (Z arriba): el eje del disco no siempre es la Y local. Se gira sobre el eje más delgado de su malla.
            ejes = new Vector3[rotores.Length];
            for (int i = 0; i < rotores.Length; i++)
            {
                MeshFilter mf = rotores[i].GetComponent<MeshFilter>();
                ejes[i] = mf != null && mf.sharedMesh != null ? CalculoGiroRotores.EjeDelDisco(mf.sharedMesh.bounds.size) : Vector3.up;
            }
        }

        /// <summary>El eje local (unitario) sobre el que gira el rotor <paramref name="i"/>.</summary>
        public Vector3 EjeDe(int i) => ejes != null && i < ejes.Length ? ejes[i] : Vector3.up;

        private void Update()
        {
            if (distanciaMaxima > 0f && Time.time >= proximoControl)
            {
                proximoControl = Time.time + SegundosEntreControles;
                if (camara == null)
                {
                    camara = Camera.main;
                }
                enAlcance = camara == null || CalculoGiroRotores.DentroDeAlcance((camara.transform.position - transform.position).sqrMagnitude, distanciaMaxima);
            }
            if (enAlcance)
            {
                Girar(Time.deltaTime);
            }
        }

        /// <summary>Gira todos los rotores lo que toca en <paramref name="dt"/> segundos.</summary>
        public void Girar(float dt)
        {
            if (rotores == null)
            {
                Buscar();
            }
            for (int i = 0; i < rotores.Length; i++)
            {
                rotores[i].Rotate(EjeDe(i), CalculoGiroRotores.GradosEnPaso(velocidad, dt, i, alternarSentido), Space.Self);
            }
        }
    }
}
