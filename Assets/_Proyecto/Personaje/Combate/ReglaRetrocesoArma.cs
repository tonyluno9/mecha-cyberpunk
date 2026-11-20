using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1486: curvas puras del peso de un disparo. Sin Unity vivo (solo Mathf), para probarlas en EditMode.
    /// </summary>
    public static class ReglaRetrocesoArma
    {
        /// <summary>Fracción de la duración que tarda el arma en llegar atrás; el resto es el regreso.</summary>
        public const float FraccionSubida = 0.2f;

        /// <summary>
        /// Cuánto está el arma atrás (0 = en su lugar, 1 = todo el retroceso) a los <paramref name="transcurrido"/> s de un tiro que dura
        /// <paramref name="duracion"/> s: sube rápido y regresa suave. Fuera de [0, duracion] vale 0.
        /// </summary>
        public static float Desplazamiento(float transcurrido, float duracion)
        {
            if (duracion <= 0f || transcurrido < 0f || transcurrido >= duracion)
            {
                return 0f;
            }
            float t = transcurrido / duracion;
            if (t < FraccionSubida)
            {
                float u = t / FraccionSubida;
                return Mathf.Sin(u * Mathf.PI * 0.5f);
            }
            float v = (t - FraccionSubida) / (1f - FraccionSubida);
            return (1f - v) * (1f - v);
        }

        /// <summary>Patada de cámara que queda a los <paramref name="transcurrido"/> s (1 al instante, 0 a la duración), con caída cuadrática.</summary>
        public static float Recuperacion(float transcurrido, float duracion)
        {
            float t = Mathf.Clamp01(transcurrido / Mathf.Max(0.0001f, duracion));
            return (1f - t) * (1f - t);
        }
    }

    /// <summary>
    /// T-1486: dispersión que crece con cada tiro seguido y se recupera con el tiempo. Cada tiro suma <c>paso</c> grados hasta el
    /// <c>tope</c>; baja <c>recuperacion</c> grados por segundo. El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaFloracionDispersion
    {
        /// <summary>Apuntando, la floración cuenta solo esta parte.</summary>
        public const float FactorApuntando = 0.5f;

        private float valor;
        private float ultimo;

        /// <summary>Grados de floración a <paramref name="ahora"/> s (sin contar el factor de apuntar).</summary>
        public float Actual(float ahora, float recuperacion)
        {
            Avanzar(ahora, recuperacion);
            return valor;
        }

        /// <summary>Registra un tiro a <paramref name="ahora"/> s.</summary>
        public void Disparar(float ahora, float paso, float tope, float recuperacion)
        {
            Avanzar(ahora, recuperacion);
            valor = Mathf.Min(Mathf.Max(0f, tope), valor + Mathf.Max(0f, paso));
        }

        /// <summary>Floración que se suma al cono según si se apunta.</summary>
        public static float Aplicada(float floracion, bool apuntando) => apuntando ? floracion * FactorApuntando : floracion;

        public void Reiniciar()
        {
            valor = 0f;
            ultimo = 0f;
        }

        private void Avanzar(float ahora, float recuperacion)
        {
            float dt = Mathf.Max(0f, ahora - ultimo);
            valor = Mathf.Max(0f, valor - Mathf.Max(0f, recuperacion) * dt);
            ultimo = ahora;
        }
    }

    /// <summary>T-1486: IDs de sonido de la marca en la mira. Sin archivo propio todavía: se reusan los de HUD (ver docs/tareas/en-curso/T-1486-disparar.md).</summary>
    public static class SonidosMarcaImpacto
    {
        public const string Pegar = "HUD_NOTIFICACION";
        public const string Matar = "HUD_LOGRO";

        public static string Id(bool mato) => mato ? Matar : Pegar;
    }
}
