using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T25: un NPC arrollado por un carro sale volando (derribo con impulso): se apagan sus scripts y su CharacterController, vuela en arco con la velocidad del carro
    /// mientras cae con su clip de atropello, aterriza (se detiene contra paredes) y, si sigue vivo, se levanta y recupera sus scripts. Lo agrega <see cref="AtropelloNpcs"/>.
    /// </summary>
    public class ImpulsoNpc : MonoBehaviour
    {
        public const float SegundosEnSuelo = 1.6f;
        private const float Gravedad = -9.81f;
        private const float AlturaRayo = 0.5f;
        private const float MargenPared = 0.35f;
        private const float AlturaPared = 0.9f;
        private const float SegundosVueloMaximo = 4f;
        private const int MaxImpactos = 8;

        private readonly List<Behaviour> apagados = new List<Behaviour>();
        private CharacterController ccApagado;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private Vector3 velocidad;
        private float segundos;
        private bool volando;
        private float aterrizoEn;
        private VisualPersonaje visual;
        private bool murio;

        public bool EnVuelo => volando;

        /// <summary>true si ya aterrizó y se está levantando o se quedó tirado.</summary>
        public bool Aterrizo => !volando && aterrizoEn > 0f;

        public void Lanzar(Vector3 velocidadInicial, Vector3 direccionGolpe, bool muerto)
        {
            velocidad = velocidadInicial;
            murio = muerto;
            segundos = 0f;
            aterrizoEn = 0f;
            volando = true;
            apagados.Clear();
            foreach (MonoBehaviour b in GetComponents<MonoBehaviour>())
            {
                if (b == this || !b.enabled || b is VisualPersonaje)
                {
                    continue;
                }
                b.enabled = false;
                apagados.Add(b);
            }
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.enabled = false;
                ccApagado = cc; // CharacterController es un Collider, no un Behaviour
            }
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (visual != null && visual.gameObject.activeInHierarchy && !murio)
            {
                visual.RegistrarGolpe(direccionGolpe);
                visual.MotivoDanio = MotivoDanioVisual.Atropello;
                visual.SuspenderRaiz(true);
                visual.Caido = true;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (volando)
            {
                Volar(dt);
                return;
            }
            if (aterrizoEn > 0f && Time.time >= aterrizoEn + SegundosEnSuelo)
            {
                Terminar();
            }
        }

        private void Volar(float dt)
        {
            segundos += dt;
            velocidad.y += Gravedad * dt;
            Vector3 paso = velocidad * dt;
            Vector3 plano = new Vector3(paso.x, 0f, paso.z);
            if (plano.sqrMagnitude > 0.000001f && HayPared(plano))
            {
                velocidad.x = 0f;
                velocidad.z = 0f;
                paso.x = 0f;
                paso.z = 0f;
            }
            Vector3 nueva = transform.position + paso;
            if (velocidad.y <= 0f && BuscarSuelo(nueva, -paso.y, out float sueloY))
            {
                nueva.y = sueloY;
                transform.position = nueva;
                Aterrizar();
                return;
            }
            transform.position = nueva;
            if (segundos >= SegundosVueloMaximo)
            {
                Aterrizar();
            }
        }

        private bool HayPared(Vector3 plano)
        {
            Vector3 origen = transform.position + Vector3.up * AlturaPared;
            int n = Physics.RaycastNonAlloc(origen, plano.normalized, impactos, plano.magnitude + MargenPared, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = impactos[i].collider;
                if (!c.transform.IsChildOf(transform) && c.GetComponentInParent<IMontable>() == null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool BuscarSuelo(Vector3 desde, float caida, out float y)
        {
            Vector3 origen = desde + Vector3.up * AlturaRayo;
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, impactos, AlturaRayo + Mathf.Max(caida, 0f) + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            y = desde.y;
            bool hay = false;
            for (int i = 0; i < n; i++)
            {
                Collider c = impactos[i].collider;
                if (c.transform.IsChildOf(transform) || impactos[i].distance >= mejor)
                {
                    continue;
                }
                mejor = impactos[i].distance;
                y = impactos[i].point.y;
                hay = true;
            }
            return hay;
        }

        private void Aterrizar()
        {
            volando = false;
            aterrizoEn = Time.time;
            velocidad = Vector3.zero;
        }

        private void Terminar()
        {
            if (visual != null && !murio)
            {
                visual.Caido = false;
                visual.SuspenderRaiz(false);
            }
            Destroy(this); // OnDestroy devuelve lo apagado
        }

        private void OnDestroy()
        {
            foreach (Behaviour b in apagados)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
            if (ccApagado != null)
            {
                ccApagado.enabled = true;
            }
            apagados.Clear();
        }
    }
}
