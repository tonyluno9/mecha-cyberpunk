using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Apuntado asistido suave (T-424, D-tony-289): al apuntar con clic derecho, si un enemigo (los <see cref="ILecturaRival"/> a pie y
    /// vivos: pandilleros y jefes; nunca aliados, peatones ni vecinos) está cerca del centro de la retícula y hay línea de vista, la cámara
    /// se jala hacia él con <see cref="ReglaApuntadoAsistido.Correccion"/> (nunca un salto). <see cref="HayBlanco"/> prende el color de
    /// la retícula (lo lee el HUD). El nivel sale de <see cref="IOpciones"/>; con el zoom del sniper la fuerza casi desaparece.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    public class ApuntadoAsistido : MonoBehaviour
    {
        private const int MaxChoques = 8;

        [SerializeField] private DatosApuntadoAsistido datos;

        private readonly RaycastHit[] choques = new RaycastHit[MaxChoques];
        private readonly List<Vector3> centros = new List<Vector3>();
        private readonly List<ILecturaRival> enemigos = new List<ILecturaRival>();
        private ControladorAPie controlador;
        private ZoomSniper zoom;
        private Camera camara;
        private Vector3 origen;
        private Transform raizBlanco;

        /// <summary>True si hay un enemigo con línea de vista sobre la retícula.</summary>
        public bool HayBlanco { get; private set; }

        /// <summary>El enemigo marcado (null si no hay).</summary>
        public Transform Blanco => HayBlanco ? raizBlanco : null;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            zoom = GetComponent<ZoomSniper>();
            if (datos == null)
            {
                Debug.LogError("[Personaje] Falta DatosApuntadoAsistido en " + name, this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            HayBlanco = false;
            raizBlanco = null;
        }

        private void Update()
        {
            HayBlanco = false;
            raizBlanco = null;
            if (!controlador.ControlActivo)
            {
                return;
            }
            if (camara == null || !camara.enabled)
            {
                camara = GetComponentInChildren<Camera>(false);
                if (camara == null)
                {
                    return;
                }
            }

            IOpciones opciones = Registro<IOpciones>.Actual;
            NivelApuntadoAsistido nivel = opciones != null ? opciones.ApuntadoAsistido : NivelApuntadoAsistido.Suave;
            origen = camara.transform.position;
            Vector3 adelante = camara.transform.forward;

            centros.Clear();
            enemigos.Clear();
            IReadOnlyList<ILecturaRival> todos = Registro<ILecturaRival>.Todos;
            for (int i = 0; i < todos.Count; i++)
            {
                ILecturaRival r = todos[i];
                if (r.EsMecha || r.Raiz == null || !r.Raiz.gameObject.activeInHierarchy || r.Vida == null || r.Vida.EstaKO)
                {
                    continue;
                }
                enemigos.Add(r);
                centros.Add(r.Raiz.position + Vector3.up * datos.AlturaPecho);
            }
            if (enemigos.Count == 0)
            {
                return;
            }

            int indice = ReglaApuntadoAsistido.ElegirBlanco(origen, adelante, centros, datos.Radio(nivel), datos.DistanciaMaxima,
                i => TieneLineaDeVista(enemigos[i].Raiz, centros[i]));
            if (indice < 0)
            {
                return;
            }
            HayBlanco = true;
            raizBlanco = enemigos[indice].Raiz;

            float fuerza = datos.Fuerza(nivel);
            if (!controlador.Apuntando || fuerza <= 0f)
            {
                return;
            }
            fuerza *= ReglaApuntadoAsistido.FactorZoom(zoom != null ? zoom.Mezcla : 0f, datos.FactorConZoom);
            Vector3 hacia = centros[indice] - origen;
            float giroObjetivo = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            float inclinacionObjetivo = -Mathf.Asin(Mathf.Clamp(hacia.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float dt = Time.deltaTime;
            float errorGiro = Mathf.DeltaAngle(controlador.GiroCamara, giroObjetivo);
            float errorInclinacion = inclinacionObjetivo - controlador.Inclinacion;
            controlador.Empujar(ReglaApuntadoAsistido.Correccion(errorGiro, fuerza, dt, datos.MaximoGradosPorSegundo),
                ReglaApuntadoAsistido.Correccion(errorInclinacion, fuerza, dt, datos.MaximoGradosPorSegundo));
        }

        // Nada que no sea el jugador ni el enemigo entre la cámara y el pecho del enemigo.
        private bool TieneLineaDeVista(Transform raizEnemigo, Vector3 centro)
        {
            Vector3 hacia = centro - origen;
            float distancia = hacia.magnitude;
            int cantidad = Physics.RaycastNonAlloc(origen, hacia / distancia, choques, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Transform t = choques[i].collider.transform;
                if (t.root == transform.root || t.IsChildOf(raizEnemigo))
                {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}
