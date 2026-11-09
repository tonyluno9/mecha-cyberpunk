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
    /// S1 (T-650): agarre de cada arma en la mano del modelo Humanoid. T-736: el jugador es PJ01 (rig de Tripo de 78 huesos, mano derecha = R_Hand); su
    /// agarre de la PISTOLA es el caso A de agarre.txt de Tony (PJ01 a 1.80 m con el Scale Factor del importador: R_Hand con lossyScale 1): palma en la empuñadura,
    /// índice en el gatillo, cañón al frente y corredera arriba. Las armas largas (fusil, escopeta, sniper) siguen con su agarre de siempre (plan de PJ01, A6).
    /// </summary>
    public static class AgarresArmas
    {
        /// <summary>Nombre del punto de la mano izquierda (hijo del arma).</summary>
        public const string NombreAgarreIzquierdo = "AgarreIzq";

        /// <summary>
        /// Posición (m) del arma en R_Hand de PJ01. T-749: el caso A de agarre.txt (0.0129, 0.1181, 0.0080) está escrito para un R_Hand girado 180° en Y respecto al de PJ01 en Unity
        /// (por eso X y Z van con el signo contrario) y para PJ01 a 1.837; aquí se importa a <c>ImportadorClipsPJ01.EscalaClips</c> = 1.88. Medido en W01_Pistola_agarre.blend.
        /// </summary>
        public static readonly Vector3 PosicionPJ01 = new Vector3(-0.0132f, 0.1209f, -0.0081f);
        /// <summary>El cuaternión de agarre.txt de Tony (x, y, z, w): nodo Pistola (cañón +Z, corredera +Y) respecto a su R_Hand.</summary>
        public static readonly Quaternion CuaternionTony = new Quaternion(-0.05021f, 0.72950f, 0.66647f, 0.14534f);
        /// <summary>T-749: el R_Hand de PJ01 en Unity tiene los ejes girados 180° en Y respecto al de agarre.txt (comprobado en la pose de Blender: el resultado queda a 0.2° del arma de Blender).</summary>
        public static readonly Quaternion GiroManoUnity = Quaternion.Euler(0f, 180f, 0f);
        public static readonly Quaternion GiroModeloPistola = Quaternion.Euler(0f, 180f, 0f);
        public static Vector3 GiroPJ01 => (GiroManoUnity * CuaternionTony).eulerAngles;
        /// <summary>Alto de PJ01 en R_Hand si el modelo se importara a escala 1 y se agrandara en su objeto (caso B de agarre.txt); solo por referencia.</summary>
        public const float EscalaCasoB = 0.5444f;

        private static readonly Vector3 GiroEnMano = new Vector3(-90f, 0f, 0f);
        // T-679: los modelos W02-W04 llevan la empuñadura (la palma) en su origen, igual que la pistola W01.
        private static readonly Vector3 PosicionArmaLarga = new Vector3(-0.025f, 0.06f, -0.035f);
        private static readonly Vector3 GiroManoIzquierdaLarga = new Vector3(316f, 88f, 179f);

        /// <summary>Nombre del hueso de la mano derecha de PJ01 (el jugador).</summary>
        public const string ManoDerechaPJ01 = "R_Hand";

        // Agarre de los modelos de Tripo de 65 huesos mixamorig (man, woman...): Y hacia los dedos, X hacia el dorso; el arma se gira -90 en X. Solo para cuando
        // el jugador cambia a uno de esos modelos (ModeloSegunPerfil); PJ01 usa los valores de agarre.txt.
        private static readonly Vector3 GiroEnManoMixamo = new Vector3(-90f, 0f, 0f);
        private static readonly Vector3 PosicionEnManoMixamo = new Vector3(-0.025f, 0.06f, -0.035f);

        /// <summary>
        /// El agarre de un arma para la mano de un modelo: la pistola en la mano de PJ01 (R_Hand) usa agarre.txt de Tony; las armas largas y cualquier otra mano,
        /// el agarre de los modelos mixamorig.
        /// </summary>
        public static bool TryObtener(string arma, string nombreMano, out AgarreArma agarre)
        {
            bool tiene = TryObtener(arma, out agarre);
            if (tiene && (nombreMano != ManoDerechaPJ01 || arma != "Pistola"))
            {
                agarre = new AgarreArma(PosicionEnManoMixamo, GiroEnManoMixamo, agarre.PosicionIzquierda, agarre.RotacionIzquierda);
            }
            return tiene;
        }

        /// <summary>El agarre de un arma por su nombre en el socket (Pistola, Fusil, Escopeta, Sniper) para PJ01 (R_Hand); false si no se conoce.</summary>
        public static bool TryObtener(string arma, out AgarreArma agarre)
        {
            switch (arma)
            {
                case "Pistola":
                    agarre = new AgarreArma(PosicionPJ01, GiroPJ01, new Vector3(-0.12f, -0.06f, -0.08f), new Vector3(9f, 84f, 96f));
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
            return GirarHacia(armaRotacion, deseado, peso, maximoGrados);
        }

        /// <summary>
        /// T-681: giro que lleva el cañón del arma exactamente hacia <paramref name="direccion"/> (por ejemplo, de la boca al punto de mira), con la corredera arriba.
        /// Con <paramref name="peso"/> 0 no gira; el giro del cañón y el del ladeo se limitan a <paramref name="maximoGrados"/>.
        /// </summary>
        public static Quaternion CorreccionHaciaDireccion(Quaternion armaRotacion, Vector3 direccion, float peso, float maximoGrados)
        {
            if (direccion.sqrMagnitude < 1e-6f || peso <= 0f)
            {
                return Quaternion.identity;
            }
            return GirarHacia(armaRotacion, direccion.normalized, peso, maximoGrados);
        }

        private static Quaternion GirarHacia(Quaternion armaRotacion, Vector3 deseado, float peso, float maximoGrados)
        {
            Vector3 canon = armaRotacion * Vector3.forward;
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
