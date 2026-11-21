using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1540: dispara el sonido de los impactos de los clips (cuerpo que toca el piso al morir o ser derribado, golpe del escudo del E16...) en los fotogramas de
    /// <see cref="TablaImpactosPJ01"/>, con el tiempo normalizado del estado de la capa base: no modifica ningún clip ni .meta (igual que los pasos por tiempo de T-1534).
    /// Solo cuenta el clip dominante. Va en el objeto del Animator; VisualPersonaje lo agrega solo. El sonido sale por <see cref="MechaCyberpunk.Core.Sonidos"/> (sin gestor, no hace nada).
    /// </summary>
    public class ReceptorImpactosPJ01 : MonoBehaviour
    {
        private Animator animador;
        private readonly List<AnimatorClipInfo> informacion = new List<AnimatorClipInfo>();
        private readonly List<float> pesos = new List<float>();
        private readonly TablaImpactosPJ01.Impacto[] impactos = new TablaImpactosPJ01.Impacto[TablaImpactosPJ01.MaximoPorClip];
        private readonly DetectorPisadasPorTiempo[] detectores = { new DetectorPisadasPorTiempo(), new DetectorPisadasPorTiempo() };
        private AnimationClip clipDetectado;
        private int cantidad;

        /// <summary>Avisa de cada impacto: el id de sonido y dónde. Lo escuchan las pruebas; el sonido ya se toca aquí.</summary>
        public event Action<string, Vector3> Impacto;

        /// <summary>Impactos avisados (para pruebas).</summary>
        public int ImpactosAvisados { get; private set; }

        private void Awake()
        {
            animador = GetComponent<Animator>();
        }

        private void LateUpdate()
        {
            if (animador == null || !animador.isActiveAndEnabled || animador.runtimeAnimatorController == null)
            {
                return;
            }
            bool enTransicion = animador.IsInTransition(0);
            AnimatorStateInfo estado = enTransicion ? animador.GetNextAnimatorStateInfo(0) : animador.GetCurrentAnimatorStateInfo(0);
            if (enTransicion)
            {
                animador.GetNextAnimatorClipInfo(0, informacion);
            }
            else
            {
                animador.GetCurrentAnimatorClipInfo(0, informacion);
            }
            pesos.Clear();
            foreach (AnimatorClipInfo info in informacion)
            {
                pesos.Add(info.weight);
            }
            int dominante = ReglaPasosAnimados.IndiceDominante(pesos);
            AnimationClip clip = dominante >= 0 ? informacion[dominante].clip : null;
            if (clip != clipDetectado)
            {
                clipDetectado = clip;
                cantidad = clip != null ? TablaImpactosPJ01.Buscar(clip.name, impactos) : 0;
                for (int i = 0; i < detectores.Length; i++)
                {
                    detectores[i].Reiniciar();
                }
            }
            for (int i = 0; i < cantidad; i++)
            {
                float fase = impactos[i].Fase;
                if (detectores[i].Avanzar(estado.normalizedTime, fase, fase) != 0)
                {
                    Avisar(impactos[i].Sonido);
                }
            }
        }

        private void Avisar(string sonido)
        {
            ImpactosAvisados++;
            MechaCyberpunk.Core.Sonidos.ReproducirEn(sonido, transform.position);
            Impacto?.Invoke(sonido, transform.position);
        }
    }
}
