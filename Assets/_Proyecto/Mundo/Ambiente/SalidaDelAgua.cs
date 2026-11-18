using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-1358: red de seguridad del mar. Si el jugador queda más de <see cref="SegundosMaximos"/> bajo el nivel de nado dentro del
    /// área del mar, lo pone en el punto de orilla más cercano. Un carro que cae al agua (cualquier cuerpo con Rigidbody que no sea el
    /// jugador) se apaga, como hace el tráfico con los carros perdidos. Si el jugador va montado en el carro, se mueve el carro.
    /// </summary>
    public class SalidaDelAgua : MonoBehaviour
    {
        private const int MaxColliders = 64;

        [Tooltip("Por debajo de esta altura (m) se cuenta como en el agua.")]
        [SerializeField] private float nivelAgua = -2.5f;

        [Tooltip("Segundos en el agua antes de sacarlo (s).")]
        [Min(0.1f)] [SerializeField] private float segundosMaximos = 3f;

        [Tooltip("Cada cuántos segundos se revisa (s).")]
        [Min(0.05f)] [SerializeField] private float intervalo = 0.5f;

        [Tooltip("Centro del área del mar, en espacio de mundo (m).")]
        [SerializeField] private Vector3 centroMar = new Vector3(-75f, -4f, -300f);

        [Tooltip("Tamaño del área del mar (m).")]
        [SerializeField] private Vector3 tamanoMar = new Vector3(820f, 4f, 70f);

        [Tooltip("Puntos de orilla donde se saca al jugador, en espacio de mundo (m). La y es el piso.")]
        [SerializeField] private Vector3[] puntosOrilla = new Vector3[0];

        private readonly Collider[] golpes = new Collider[MaxColliders];
        private readonly Dictionary<Transform, float> tiempos = new Dictionary<Transform, float>();
        private float proximaRevision;

        public float SegundosMaximos => segundosMaximos;
        public float NivelAgua => nivelAgua;
        public Vector3[] PuntosOrilla => puntosOrilla;

        /// <summary>Define el área y los puntos de orilla (lo usa el script de colocación).</summary>
        public void Definir(Vector3 centro, Vector3 tamano, Vector3[] puntos)
        {
            centroMar = centro;
            tamanoMar = tamano;
            puntosOrilla = puntos;
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + intervalo;
            Revisar(intervalo);
        }

        private void Revisar(float dt)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null ? jugador.Raiz : null;
            int n = Physics.OverlapBoxNonAlloc(centroMar, tamanoMar * 0.5f, golpes, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Rigidbody cuerpo = golpes[i].attachedRigidbody;
                Transform raiz = cuerpo != null ? cuerpo.transform : golpes[i].transform.root;
                if (cuerpo == null && raiz != raizJugador)
                {
                    continue;
                }
                bool enAgua = CalculoSalidaDelAgua.EstaEnAgua(raiz.position.y, nivelAgua);
                tiempos.TryGetValue(raiz, out float t);
                t = CalculoSalidaDelAgua.Acumular(t, enAgua, dt);
                tiempos[raiz] = t;
                if (t < segundosMaximos)
                {
                    continue;
                }
                tiempos.Remove(raiz);
                bool esJugador = raizJugador != null && (raiz == raizJugador || raizJugador.IsChildOf(raiz) || raiz.IsChildOf(raizJugador));
                if (esJugador)
                {
                    Sacar(raiz);
                }
                else
                {
                    raiz.gameObject.SetActive(false);
                }
            }
            if (n == 0)
            {
                tiempos.Clear();
            }
        }

        private void Sacar(Transform raiz)
        {
            Vector3 destino = CalculoSalidaDelAgua.PuntoMasCercano(raiz.position, puntosOrilla);
            CharacterController control = raiz.GetComponent<CharacterController>();
            if (control != null)
            {
                control.enabled = false;
            }
            raiz.position = destino;
            if (control != null)
            {
                control.enabled = true;
            }
            Rigidbody cuerpo = raiz.GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            Debug.Log("[Mundo] " + raiz.name + " sacado del agua a " + destino);
        }
    }
}
