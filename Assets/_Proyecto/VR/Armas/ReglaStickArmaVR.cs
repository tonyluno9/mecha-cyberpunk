using System;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR5/VR19: el stick derecho en vertical. Desde VR19 ya no cambia de arma (el stick derecho es solo giro y recentrar; las armas van en la
    /// rueda, mantener X). Abajo (&lt; -0.75) sostenido 1 s pide recentrar (una vez); arriba o un toque abajo no piden nada. Se rearma bajo 0.35.
    /// Solo cuenta si el eje vertical manda sobre el horizontal (el horizontal es el giro). Lógica pura: recibe el eje y el dt.
    /// </summary>
    public sealed class ReglaStickArmaVR
    {
        public const float Umbral = 0.75f;
        public const float Rearme = 0.35f;
        /// <summary>Abajo sostenido esto (s) recentra.</summary>
        public const float SegundosRecentrar = 1f;

        private enum Fase
        {
            Libre,
            Abajo,
            EsperaRearme
        }

        private Fase fase = Fase.Libre;
        private float segundosAbajo;

        /// <summary>Segundos que lleva el stick abajo (0 si no está abajo).</summary>
        public float SegundosAbajo => fase == Fase.Abajo ? segundosAbajo : 0f;

        /// <summary>Un cuadro con el stick en <paramref name="eje"/>. Devuelve lo que pide este cuadro (solo Nada o Recentrar).</summary>
        public GestoStickVR Avanzar(Vector2 eje, float dt)
        {
            float y = eje.y;
            bool manda = Mathf.Abs(y) >= Mathf.Abs(eje.x);
            switch (fase)
            {
                case Fase.Libre:
                    if (y < -Umbral && manda)
                    {
                        fase = Fase.Abajo;
                        segundosAbajo = 0f;
                    }
                    return GestoStickVR.Nada;
                case Fase.Abajo:
                    if (y > -Rearme)
                    {
                        fase = Fase.Libre;
                        return GestoStickVR.Nada;
                    }
                    segundosAbajo += Mathf.Max(0f, dt);
                    if (segundosAbajo >= SegundosRecentrar)
                    {
                        fase = Fase.EsperaRearme;
                        return GestoStickVR.Recentrar;
                    }
                    return GestoStickVR.Nada;
                default:
                    if (Mathf.Abs(y) < Rearme)
                    {
                        fase = Fase.Libre;
                    }
                    return GestoStickVR.Nada;
            }
        }

        /// <summary>Olvida el gesto en curso y espera a que el stick vuelva al centro (al abrir un menú, por ejemplo).</summary>
        public void Cancelar()
        {
            fase = Fase.EsperaRearme;
            segundosAbajo = 0f;
        }

        /// <summary>La siguiente (paso 1) o anterior (paso -1) arma que se tiene, dando la vuelta, como la rueda del ratón. La misma si no hay otra.</summary>
        public static ArmaAPie Siguiente(ArmaAPie actual, int paso, Func<ArmaAPie, bool> tiene)
        {
            int total = Enum.GetValues(typeof(ArmaAPie)).Length;
            for (int i = 1; i < total; i++)
            {
                ArmaAPie candidata = (ArmaAPie)((((int)actual + paso * i) % total + total) % total);
                if (tiene == null || tiene(candidata))
                {
                    return candidata;
                }
            }
            return actual;
        }
    }
}
