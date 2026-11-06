using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Cómo se agarra un arma: pose del arma en la mano derecha y dónde va la mano izquierda (en el espacio local del arma).</summary>
    public readonly struct AgarreArma
    {
        /// <summary>Posición del arma en el socket de la mano derecha (m, en el espacio del hueso de la mano).</summary>
        public readonly Vector3 Posicion;
        /// <summary>Rotación del arma en el socket (°).</summary>
        public readonly Vector3 Rotacion;
        /// <summary>Posición de la mano izquierda en el espacio local del arma (m).</summary>
        public readonly Vector3 PosicionIzquierda;
        /// <summary>Rotación de la mano izquierda en el espacio local del arma (°).</summary>
        public readonly Vector3 RotacionIzquierda;

        public AgarreArma(Vector3 posicion, Vector3 rotacion, Vector3 posicionIzquierda, Vector3 rotacionIzquierda)
        {
            Posicion = posicion;
            Rotacion = rotacion;
            PosicionIzquierda = posicionIzquierda;
            RotacionIzquierda = rotacionIzquierda;
        }
    }

    /// <summary>
    /// S1 (T-650): agarre de cada arma en la mano del modelo Humanoid de Tripo. En la mano derecha (hueso RightHand) los ejes son Y hacia los dedos,
    /// X hacia el dorso (la palma mira a -X) y -Z hacia el pulgar; el arma lleva el cañón a +Z y la corredera a +Y, así que se gira -90° en X
    /// (cañón por los dedos, corredera hacia el pulgar). Valores medidos con capturas de cerca (docs/capturas/s1-*.png).
    /// </summary>
    public static class AgarresArmas
    {
        /// <summary>Nombre del punto de la mano izquierda (hijo del arma).</summary>
        public const string NombreAgarreIzquierdo = "AgarreIzq";

        private static readonly Vector3 GiroEnMano = new Vector3(-90f, 0f, 0f);
        // T-679: los modelos W02-W04 llevan la empuñadura (la palma) en su origen, igual que la pistola W01.
        private static readonly Vector3 PosicionArmaLarga = new Vector3(-0.025f, 0.06f, -0.035f);
        private static readonly Vector3 GiroManoIzquierdaLarga = new Vector3(316f, 88f, 179f);

        /// <summary>El agarre de un arma por su nombre en el socket (Pistola, Fusil, Escopeta, Sniper); false si no se conoce.</summary>
        public static bool TryObtener(string arma, out AgarreArma agarre)
        {
            switch (arma)
            {
                case "Pistola":
                    agarre = new AgarreArma(new Vector3(-0.025f, 0.06f, -0.035f), GiroEnMano, new Vector3(-0.12f, -0.06f, -0.08f), new Vector3(9f, 84f, 96f));
                    return true;
                case "Fusil":
                    agarre = new AgarreArma(PosicionArmaLarga, GiroEnMano, new Vector3(-0.06f, 0.03f, 0.36f), GiroManoIzquierdaLarga);
                    return true;
                case "Escopeta":
                    agarre = new AgarreArma(PosicionArmaLarga, GiroEnMano, new Vector3(-0.06f, 0.03f, 0.36f), GiroManoIzquierdaLarga);
                    return true;
                case "Sniper":
                    agarre = new AgarreArma(PosicionArmaLarga, GiroEnMano, new Vector3(-0.06f, 0.03f, 0.38f), GiroManoIzquierdaLarga);
                    return true;
                default:
                    agarre = default;
                    return false;
            }
        }

        /// <summary>
        /// Giro que lleva el arma a apuntar al frente: el cañón (<paramref name="armaRotacion"/> * Z) mira al frente en horizontal, conservando su inclinación (o con la de <paramref name="inclinacionGrados"/>, positiva hacia arriba, si no es NaN),
        /// y la corredera queda arriba (sin ladearse). Con <paramref name="peso"/> 0 no gira; el giro del cañón y el del ladeo se limitan a <paramref name="maximoGrados"/>.
        /// </summary>
        public static Quaternion CorreccionDeArma(Quaternion armaRotacion, Vector3 frente, float peso, float maximoGrados, float inclinacionGrados = float.NaN)
        {
            Vector3 canon = armaRotacion * Vector3.forward;
            Vector3 horizontal = new Vector3(frente.x, 0f, frente.z);
            if (horizontal.sqrMagnitude < 1e-6f || peso <= 0f)
            {
                return Quaternion.identity;
            }
            float arriba = float.IsNaN(inclinacionGrados) ? Mathf.Clamp(canon.y, -1f, 1f) : Mathf.Sin(inclinacionGrados * Mathf.Deg2Rad);
            Vector3 deseado = (horizontal.normalized * Mathf.Sqrt(1f - arriba * arriba) + Vector3.up * arriba).normalized;
            Quaternion giroCanon = Quaternion.FromToRotation(canon, deseado);
            giroCanon.ToAngleAxis(out float grados, out Vector3 eje);
            if (grados > 180f) { grados -= 360f; }
            giroCanon = Quaternion.AngleAxis(Mathf.Clamp(grados, -maximoGrados, maximoGrados), eje);
            Vector3 canonNuevo = giroCanon * canon;
            Vector3 derechaNueva = giroCanon * (armaRotacion * Vector3.right);
            Vector3 derechaDeseada = Vector3.Cross(Vector3.up, canonNuevo);
            Quaternion ladeo = Quaternion.identity;
            if (derechaDeseada.sqrMagnitude > 1e-6f)
            {
                float angulo = Vector3.SignedAngle(derechaNueva, derechaDeseada.normalized, canonNuevo);
                ladeo = Quaternion.AngleAxis(Mathf.Clamp(angulo, -maximoGrados, maximoGrados), canonNuevo);
            }
            Quaternion completo = ladeo * giroCanon;
            return Quaternion.Slerp(Quaternion.identity, completo, Mathf.Clamp01(peso));
        }
    }
}
