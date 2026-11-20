using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1505: recibe los eventos de pisada de los clips de locomoción de PJ01 (<see cref="ReglaPasosAnimados.FuncionPaso"/>). Filtra por peso (solo el clip con más
    /// peso, contando la transición) y avisa con <see cref="Paso"/>; quien lo escucha (ControladorAPie) toca el sonido. Va en el mismo objeto que el Animator;
    /// VisualPersonaje lo agrega solo, así nadie deja avisos de "evento sin receptor". También dice si el clip dominante lleva eventos de paso
    /// (<see cref="ClipActivoTienePasos"/>): entonces el sonido por distancia se apaga.
    /// </summary>
    public class ReceptorPasosPJ01 : MonoBehaviour
    {
        private Animator animador;
        private readonly List<AnimatorClipInfo> actuales = new List<AnimatorClipInfo>();
        private readonly List<AnimatorClipInfo> siguientes = new List<AnimatorClipInfo>();
        private readonly List<AnimationClip> clips = new List<AnimationClip>();
        private readonly List<float> pesos = new List<float>();

        /// <summary>Pie que pisa (0 izquierdo, 1 derecho). Solo se avisa del clip dominante.</summary>
        public event Action<int> Paso;

        /// <summary>Pasos avisados (para pruebas).</summary>
        public int PasosAvisados { get; private set; }

        private void Awake()
        {
            animador = GetComponent<Animator>();
        }

        private void RecogerClips()
        {
            clips.Clear();
            pesos.Clear();
            if (animador == null || !animador.isActiveAndEnabled || animador.runtimeAnimatorController == null)
            {
                return;
            }
            animador.GetCurrentAnimatorClipInfo(0, actuales);
            bool enTransicion = animador.IsInTransition(0);
            float avance = enTransicion ? animador.GetAnimatorTransitionInfo(0).normalizedTime : 0f;
            foreach (AnimatorClipInfo info in actuales)
            {
                clips.Add(info.clip);
                pesos.Add(enTransicion ? ReglaPasosAnimados.PesoEnTransicion(info.weight, false, avance) : info.weight);
            }
            if (enTransicion)
            {
                animador.GetNextAnimatorClipInfo(0, siguientes);
                foreach (AnimatorClipInfo info in siguientes)
                {
                    clips.Add(info.clip);
                    pesos.Add(ReglaPasosAnimados.PesoEnTransicion(info.weight, true, avance));
                }
            }
        }

        /// <summary>True si el clip con más peso de la capa base lleva eventos de paso (entonces el sonido por distancia se apaga).</summary>
        public bool ClipActivoTienePasos()
        {
            RecogerClips();
            int i = ReglaPasosAnimados.IndiceDominante(pesos);
            return i >= 0 && ReglaPasosAnimados.TienePasos(clips[i]);
        }

        /// <summary>Evento de pisada del clip (intParameter: pie).</summary>
        public void AlPasoPJ01(AnimationEvent evento)
        {
            RecogerClips();
            int indice = -1;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] == evento.animatorClipInfo.clip)
                {
                    indice = i;
                    break;
                }
            }
            if (!ReglaPasosAnimados.DebeSonar(indice, pesos))
            {
                return;
            }
            PasosAvisados++;
            Paso?.Invoke(evento.intParameter);
        }
    }
}
