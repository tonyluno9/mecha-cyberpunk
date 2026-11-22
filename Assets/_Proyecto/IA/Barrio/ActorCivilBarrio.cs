using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T12 y T15): un civil de la ciudad que participa en una escena del barrio. Para que la escena lo mueva se le apaga el componente de
    /// paseo (PeatonMundo, de Trafico, que IA no referencia: se busca por nombre) y se le devuelve al terminar, con un empujón a huir. Todo lo demás
    /// (gestos, caída) va por su <see cref="VisualPersonaje"/>, sin tocar el código de peatones.
    /// </summary>
    public sealed class ActorCivilBarrio
    {
        public const string NombreComponentePaseo = "PeatonMundo";
        private const string MensajeOirDisparo = "AlOirDisparo";
        private const float GradosPorSegundoGiro = 360f;
        private const float DeslizamientoEmpujon = 0.4f;

        private readonly AnimacionCivilMundo civil;
        private MonoBehaviour paseo;
        private bool sujeto;

        public ActorCivilBarrio(AnimacionCivilMundo civil)
        {
            this.civil = civil;
        }

        public AnimacionCivilMundo Civil => civil;
        public Transform Transform => civil != null ? civil.transform : null;
        public VisualPersonaje Visual => civil != null ? civil.Visual : null;
        public bool Vivo => civil != null && civil.isActiveAndEnabled && !civil.MuerteAvisada;
        public bool Caido => Visual != null && Visual.Caido;

        /// <summary>True si es un civil que puede participar: activo, vivo, de pie y con visual.</summary>
        public static bool Sirve(AnimacionCivilMundo c)
        {
            return c != null && c.isActiveAndEnabled && !c.MuerteAvisada && c.Visual != null && !c.Visual.Caido;
        }

        /// <summary>Le quita el paseo y lo deja quieto (en idle).</summary>
        public void Sujetar()
        {
            if (sujeto || civil == null)
            {
                return;
            }
            foreach (MonoBehaviour m in civil.GetComponents<MonoBehaviour>())
            {
                if (m != null && m.GetType().Name == NombreComponentePaseo)
                {
                    paseo = m;
                    break;
                }
            }
            if (paseo != null)
            {
                paseo.enabled = false;
            }
            if (Visual != null)
            {
                Visual.Velocidad = 0f;
            }
            sujeto = true;
        }

        /// <summary>Lo suelta: se levanta si estaba caído, le devuelve el paseo y lo manda a huir lejos de <paramref name="desde"/> (oye un disparo ahí).</summary>
        public void Soltar(Vector3 desde)
        {
            if (!sujeto || civil == null)
            {
                sujeto = false;
                return;
            }
            sujeto = false;
            if (Visual != null && Visual.Caido)
            {
                Visual.Levantar();
            }
            if (paseo != null)
            {
                paseo.enabled = true;
                paseo.SendMessage(MensajeOirDisparo, desde, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void Mirar(Vector3 punto, float dt)
        {
            if (civil == null || Caido)
            {
                return;
            }
            Vector3 d = punto - civil.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-6f)
            {
                return;
            }
            civil.transform.rotation = Quaternion.RotateTowards(civil.transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), GradosPorSegundoGiro * dt);
        }

        /// <summary>Empujón: el clip de empujón y, si el controlador no lo tiene, la reacción de golpe con un pequeño deslizamiento.</summary>
        public void Empujar(Vector3 direccion)
        {
            if (Visual == null)
            {
                return;
            }
            direccion.y = 0f;
            direccion = direccion.sqrMagnitude < 1e-6f ? civil.transform.forward : direccion.normalized;
            if (!Visual.Empujar())
            {
                Visual.Herir(direccion);
                DesplazamientoSeguro.Mover(civil.transform, direccion, DeslizamientoEmpujon); // T-1643: sin atravesar paredes
            }
        }

        public void Tumbar(Vector3 direccion)
        {
            if (Visual == null)
            {
                return;
            }
            direccion.y = 0f;
            Visual.Herir(direccion.sqrMagnitude < 1e-6f ? civil.transform.forward : direccion.normalized);
            Visual.Tumbar();
        }

        public void Levantar()
        {
            if (Visual != null && Visual.Caido)
            {
                Visual.Levantar();
            }
        }

        /// <summary>Suplica: la reacción de susto (manos a la cara) del civil, si el controlador la tiene.</summary>
        public void Suplicar()
        {
            if (civil != null)
            {
                civil.AlAsustarCivil();
            }
        }
    }
}
