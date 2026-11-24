using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// VR50: mientras el jugador está en el campo de tiro, barre la sala (y un margen alrededor) cada <see cref="ReglaIntrusosCampoTiro.SegundosEntreBarridos"/>
    /// y apaga a cualquier peatón, pandillero, policía o carro que haya entrado o la esté cruzando (lo que generan otros sistemas cerca del jugador).
    /// La sala es de código (no tiene NavMesh), así que nadie la camina por diseño; esto es la red de seguridad. Lo llama <see cref="CorredorCampoTiro"/>.
    /// </summary>
    public static class GuardiaSalaCampoTiro
    {
        private const int MaximoChoques = 512;
        private static readonly Collider[] choques = new Collider[MaximoChoques];
        private static readonly HashSet<GameObject> vistos = new HashSet<GameObject>();
        private static float proximoBarrido;

        /// <summary>Cuántos intrusos se apagaron desde que arrancó el juego (para pruebas).</summary>
        public static int Expulsados { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            vistos.Clear();
            proximoBarrido = 0f;
            Expulsados = 0;
        }

        /// <summary>Barre la sala si ya toca. <paramref name="sala"/> es la raíz de la sala (su posición es el centro del campo).</summary>
        public static void Vigilar(GameObject sala, float ahora)
        {
            if (sala == null || ahora < proximoBarrido)
            {
                return;
            }
            proximoBarrido = ahora + ReglaIntrusosCampoTiro.SegundosEntreBarridos;
            Barrer(sala);
        }

        /// <summary>Barre la sala ahora y devuelve cuántos intrusos apagó.</summary>
        public static int Barrer(GameObject sala)
        {
            if (sala == null)
            {
                return 0;
            }
            Transform raizSala = sala.transform;
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null && jugador.Raiz != null ? jugador.Raiz : null;
            float margen = ReglaIntrusosCampoTiro.MargenVigilancia;
            float largo = ReglaCampoTiro.FondoAdelante - ReglaCampoTiro.FondoAtras;
            Vector3 centro = raizSala.position + new Vector3(0f, ReglaCampoTiro.AlturaSala * 0.5f, ReglaCampoTiro.FondoAtras + largo * 0.5f);
            Vector3 mitad = new Vector3(ReglaCampoTiro.MitadAncho + margen, ReglaCampoTiro.AlturaSala * 0.5f + margen, largo * 0.5f + margen);
            int n = Physics.OverlapBoxNonAlloc(centro, mitad, choques, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            vistos.Clear();
            int apagados = 0;
            for (int i = 0; i < n; i++)
            {
                Collider c = choques[i];
                choques[i] = null;
                if (c == null)
                {
                    continue;
                }
                Transform t = c.transform;
                bool esDelJugador = (raizJugador != null && t.IsChildOf(raizJugador)) || c.GetComponentInParent<IJugador>() != null;
                bool esDeLaSala = t.IsChildOf(raizSala);
                bool esObjetivo = c.GetComponentInParent<ObjetivoTiro>() != null;
                if (esDelJugador || esDeLaSala || esObjetivo)
                {
                    continue;
                }
                NavMeshAgent agente = c.GetComponentInParent<NavMeshAgent>();
                CharacterController controlador = c.GetComponentInParent<CharacterController>();
                Rigidbody cuerpo = c.attachedRigidbody;
                Component recibe = c.GetComponentInParent<IRecibeDanio>() as Component;
                bool personaje = recibe != null && recibe.GetComponentInChildren<Animator>(true) != null;
                float masa = cuerpo != null && !cuerpo.isKinematic ? cuerpo.mass : 0f;
                if (!ReglaIntrusosCampoTiro.EsIntruso(false, false, false, agente != null, controlador != null, masa, personaje))
                {
                    continue;
                }
                GameObject actor = agente != null ? agente.gameObject
                    : controlador != null ? controlador.gameObject
                    : masa >= ReglaIntrusosCampoTiro.MasaVehiculo ? cuerpo.gameObject
                    : recibe.gameObject;
                if (actor == null || !actor.activeInHierarchy || !vistos.Add(actor))
                {
                    continue;
                }
                Debug.Log("[CampoTiro] VR50: " + actor.name + " entró a la sala del campo de tiro; se apaga.");
                actor.SetActive(false);
                apagados++;
                Expulsados++;
            }
            return apagados;
        }
    }
}
