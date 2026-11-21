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
        private readonly List<bool> deSiguiente = new List<bool>();
        private readonly DetectorPisadasPorTiempo detector = new DetectorPisadasPorTiempo();
        private AnimationClip clipDetectado;

        /// <summary>Pie que pisa (0 izquierdo, 1 derecho). Solo se avisa del clip dominante.</summary>
        public event Action<int> Paso;

        /// <summary>Pasos que vinieron del tiempo normalizado y no de un evento del clip (T-1534; para pruebas).</summary>
        public int PasosPorTiempo { get; private set; }

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
            deSiguiente.Clear();
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
                deSiguiente.Add(false);
            }
            if (enTransicion)
            {
                animador.GetNextAnimatorClipInfo(0, siguientes);
                foreach (AnimatorClipInfo info in siguientes)
                {
                    clips.Add(info.clip);
                    pesos.Add(ReglaPasosAnimados.PesoEnTransicion(info.weight, true, avance));
                    deSiguiente.Add(true);
                }
            }
        }

        /// <summary>True si el clip con más peso de la capa base lleva eventos de paso (entonces el sonido por distancia se apaga).</summary>
        public bool ClipActivoTienePasos()
        {
            RecogerClips();
            int i = ReglaPasosAnimados.IndiceDominante(pesos);
            return i >= 0 && (ReglaPasosAnimados.TienePasos(clips[i]) || TablaPisadasPJ01.Buscar(clips[i] != null ? clips[i].name : null, out _));
        }

        // T-1534: los clips de la tabla de la BITACORA que no traen evento (caminar apuntando, escaleras) pisan por tiempo normalizado del estado: el clip no se toca.
        // Solo cuenta el clip dominante y solo si alguien escucha (el jugador); un clip con eventos nunca pasa por aquí, así no suena dos veces.
        private void LateUpdate()
        {
            if (Paso == null)
            {
                return;
            }
            RecogerClips();
            int i = ReglaPasosAnimados.IndiceDominante(pesos);
            AnimationClip clip = i >= 0 ? clips[i] : null;
            if (clip == null || ReglaPasosAnimados.TienePasos(clip) || !TablaPisadasPJ01.Buscar(clip.name, out TablaPisadasPJ01.Pisadas tabla))
            {
                clipDetectado = null;
                detector.Reiniciar();
                return;
            }
            if (clip != clipDetectado)
            {
                clipDetectado = clip;
                detector.Reiniciar();
            }
            AnimatorStateInfo estado = deSiguiente[i] ? animador.GetNextAnimatorStateInfo(0) : animador.GetCurrentAnimatorStateInfo(0);
            int pies = detector.Avanzar(estado.normalizedTime, tabla.FaseIzquierdo, tabla.FaseDerecho);
            if ((pies & 1) != 0)
            {
                AvisarPorTiempo(ReglaPasosAnimados.PieIzquierdo);
            }
            if ((pies & 2) != 0)
            {
                AvisarPorTiempo(ReglaPasosAnimados.PieDerecho);
            }
        }

        private void AvisarPorTiempo(int pie)
        {
            PasosAvisados++;
            PasosPorTiempo++;
            Paso?.Invoke(pie);
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
