using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>Lo que pasó en un cuadro de <see cref="ReglaRuedaGripVR"/>.</summary>
    public enum EventoRuedaGripVR
    {
        Nada,
        /// <summary>Se apretó el grip: desde aquí la mano mueve el selector.</summary>
        Tomo,
        /// <summary>Se soltó el grip: elegir lo que está bajo el selector.</summary>
        Solto
    }

    /// <summary>
    /// VR46, lógica pura: el selector de la rueda de armas (mantener X) con el grip izquierdo.
    /// - Rueda abierta sin grip: el selector se queda en el centro aunque la mano se mueva.
    /// - Al apretar el grip, donde está la mano es el centro y se toman los ejes de ese momento (los de la cabeza: así girar la cabeza después
    ///   no mueve el selector). Mover la mano <see cref="MetrosAlBorde"/> hacia un lado lleva el selector al borde de ese lado.
    /// - Al soltar el grip avisa <see cref="EventoRuedaGripVR.Solto"/> (el componente elige y equipa) y el selector vuelve al centro.
    /// - Si el grip ya estaba apretado al abrir, no cuenta hasta que se suelte y se vuelva a apretar.
    /// Posiciones y ejes en un mismo espacio (el del rig); el resultado en unidades de la rueda (radio <c>radio</c>).
    /// </summary>
    public sealed class ReglaRuedaGripVR
    {
        /// <summary>Lo que hay que mover la mano (m) para llevar el selector del centro al borde.</summary>
        public const float MetrosAlBorde = 0.1f;

        private Vector3 manoBase;
        private Vector3 ejeDerecha = Vector3.right;
        private Vector3 ejeArriba = Vector3.up;
        private bool tomado;
        private bool esperaSoltar;
        private Vector2 cursor;

        /// <summary>True mientras el grip mueve el selector.</summary>
        public bool Tomado => tomado;

        /// <summary>El selector (unidades de la rueda, +y arriba, 0 al centro).</summary>
        public Vector2 Cursor => cursor;

        /// <summary>La rueda se abrió. <paramref name="gripApretado"/>: si el grip ya estaba abajo (no cuenta hasta soltarlo).</summary>
        public void Abrir(bool gripApretado)
        {
            tomado = false;
            esperaSoltar = gripApretado;
            cursor = Vector2.zero;
        }

        /// <summary>
        /// Un cuadro con el grip <paramref name="grip"/>, la mano en <paramref name="mano"/> y los ejes del plano de la rueda en este momento
        /// (solo se usan al apretar el grip). Devuelve lo que pasó; el selector queda en <see cref="Cursor"/>.
        /// </summary>
        public EventoRuedaGripVR Avanzar(bool grip, Vector3 mano, Vector3 derecha, Vector3 arriba, float radio)
        {
            if (esperaSoltar)
            {
                esperaSoltar = grip;
                cursor = Vector2.zero;
                return EventoRuedaGripVR.Nada;
            }
            if (!tomado)
            {
                cursor = Vector2.zero;
                if (!grip)
                {
                    return EventoRuedaGripVR.Nada;
                }
                tomado = true;
                manoBase = mano;
                ejeDerecha = derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
                ejeArriba = arriba.sqrMagnitude > 1e-6f ? arriba.normalized : Vector3.up;
                return EventoRuedaGripVR.Tomo;
            }
            if (!grip)
            {
                tomado = false;
                return EventoRuedaGripVR.Solto;
            }
            Vector3 d = mano - manoBase;
            Vector2 movido = new Vector2(Vector3.Dot(d, ejeDerecha), Vector3.Dot(d, ejeArriba)) / MetrosAlBorde * radio;
            cursor = Vector2.ClampMagnitude(movido, radio);
            return EventoRuedaGripVR.Nada;
        }

        /// <summary>Vuelve el selector al centro sin esperar a que se suelte nada (tras soltar sin nada bajo el selector).</summary>
        public void Centrar()
        {
            tomado = false;
            cursor = Vector2.zero;
        }
    }
}
