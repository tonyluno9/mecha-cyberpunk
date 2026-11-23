using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-1709 (T74): zona de glitch intencional. Pone el zumbido de transformador (OBJ_TRANSFORMADOR, en loop 3D por el gestor de sonido, con su rolloff
    /// de la tabla de mezcla) y hace un parpadeo leve: cada 1 a 4 s, durante 0.05 a 0.15 s, las piezas visuales se desplazan 1 a 3 cm. Solo mueve piezas
    /// con Renderer y sin Collider (ni ellas ni sus hijos); si no hay ninguna, apaga los renderers ese instante. Nunca mueve colliders.
    /// </summary>
    public class ZonaGlitch : MonoBehaviour
    {
        private readonly List<Transform> piezas = new List<Transform>();
        private readonly List<Vector3> originales = new List<Vector3>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private int bucle = Sonidos.SinBucle;
        private float proximo;
        private float fin = -1f;
        private bool activo;
        private Transform ancla;
        private AudioSource fuente;

        /// <summary>True durante el instante del parpadeo (para las pruebas).</summary>
        public bool EnParpadeo => activo;

        private void OnEnable()
        {
            Reunir();
            proximo = Time.time + ReglaGlitch.Espera(Random.value);
            IniciarBucle();
        }

        /// <summary>La fuente del zumbido (la crea el gestor de sonido), o null si aún no suena. Para las pruebas.</summary>
        public AudioSource FuenteZumbido => fuente;

        /// <summary>El hijo vacío donde suena el zumbido, en el centro de las piezas visuales.</summary>
        public Transform Ancla => ancla;

        // El transform del objeto puede estar en el origen con las piezas lejos (el Elevador): el sonido sale del centro de los renderers.
        private void IniciarBucle()
        {
            if (ancla == null)
            {
                ancla = new GameObject("AnclaZumbido").transform;
                ancla.SetParent(transform, false);
            }
            ancla.position = ReglaGlitch.CentroDe(renderers, transform.position);
            bucle = Sonidos.ReproducirBucle(ReglaGlitch.IdZumbido, ancla);
            fuente = null;
            if (bucle != Sonidos.SinBucle)
            {
                foreach (AudioSource a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                {
                    if (a.name == "Bucle_" + ReglaGlitch.IdZumbido && (a.transform.position - ancla.position).sqrMagnitude < 0.25f)
                    {
                        fuente = a;
                        break;
                    }
                }
            }
        }

        // El gestor escribe el volumen en su Update; aquí, después, se sube solo el de esta zona (conserva el ducking y el volumen general).
        private void LateUpdate()
        {
            if (fuente != null && bucle != Sonidos.SinBucle)
            {
                fuente.volume = Mathf.Min(1f, fuente.volume * ReglaGlitch.AmplificacionZumbido);
            }
        }

        private void OnDisable()
        {
            Restaurar();
            if (bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucle);
                bucle = Sonidos.SinBucle;
                fuente = null;
            }
        }

        private void Update()
        {
            // Si el gestor de sonido llegó después, engancha el zumbido.
            if (bucle == Sonidos.SinBucle && Registro<ISonidos>.Actual != null)
            {
                IniciarBucle();
            }
            if (activo)
            {
                if (Time.time >= fin)
                {
                    Restaurar();
                    proximo = Time.time + ReglaGlitch.Espera(Random.value);
                }
                return;
            }
            if (Time.time >= proximo)
            {
                Empezar();
            }
        }

        private void Reunir()
        {
            piezas.Clear();
            originales.Clear();
            renderers.Clear();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                renderers.Add(r);
                if (r.GetComponentInChildren<Collider>(true) == null && r.GetComponentInParent<Collider>() == null && r.transform != transform && !piezas.Contains(r.transform))
                {
                    piezas.Add(r.transform);
                    originales.Add(r.transform.localPosition);
                }
            }
        }

        private void Empezar()
        {
            activo = true;
            fin = Time.time + ReglaGlitch.Duracion(Random.value);
            if (piezas.Count > 0)
            {
                Vector3 direccion = Random.onUnitSphere * ReglaGlitch.Desplazamiento(Random.value);
                for (int i = 0; i < piezas.Count; i++)
                {
                    if (piezas[i] != null)
                    {
                        piezas[i].localPosition = originales[i] + piezas[i].parent.InverseTransformVector(direccion);
                    }
                }
            }
            else
            {
                foreach (Renderer r in renderers)
                {
                    if (r != null)
                    {
                        r.enabled = false;
                    }
                }
            }
        }

        private void Restaurar()
        {
            if (!activo)
            {
                return;
            }
            activo = false;
            for (int i = 0; i < piezas.Count; i++)
            {
                if (piezas[i] != null)
                {
                    piezas[i].localPosition = originales[i];
                }
            }
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
        }
    }
}
