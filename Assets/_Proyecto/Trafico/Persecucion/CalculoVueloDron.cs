using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1008: el vuelo natural de un dron (lógica pura). Tiene velocidad e inercia: acelera con un límite, frena suave al llegar (llegada con desaceleración
    /// planeada), gira el rumbo con velocidad angular limitada y se inclina (cabeceo y alabeo) según la aceleración, como un cuadricóptero. Flota y se balancea
    /// un poco, y la altura se ajusta con su propia aceleración. Recibe el tiempo por parámetro.
    /// </summary>
    public sealed class CalculoVueloDron
    {
        private const float Minimo = 0.0001f;
        private const float DosVeces = 2f;
        private const float VueltaEnRadianes = 2f * Mathf.PI;
        private readonly ParametrosVueloDron p;
        private Vector3 velocidad;
        private float rumbo;
        private float cabeceo;
        private float alabeo;
        private float fase;

        public CalculoVueloDron(ParametrosVueloDron parametros, float rumboInicial = 0f)
        {
            p = parametros;
            rumbo = rumboInicial;
        }

        /// <summary>Velocidad actual en el mundo (m/s), sin contar el flote.</summary>
        public Vector3 Velocidad => velocidad;

        /// <summary>Rumbo (grados, 0 mira a +Z).</summary>
        public float Rumbo => rumbo;

        /// <summary>Cabeceo (grados): positivo baja la trompa, como al acelerar hacia delante.</summary>
        public float Cabeceo => cabeceo;

        /// <summary>Alabeo (grados): negativo inclina a la derecha, como al acelerar hacia la derecha.</summary>
        public float Alabeo => alabeo;

        /// <summary>El balanceo leve de este instante (grados), que se suma al alabeo al mostrar el dron.</summary>
        public float Balanceo => p.AmplitudBalanceo * Mathf.Sin(VueltaEnRadianes * p.FrecuenciaBalanceo * fase);

        /// <summary>La rotación al mostrar el dron: rumbo, cabeceo y alabeo más el balanceo.</summary>
        public Quaternion Rotacion => Quaternion.Euler(cabeceo, rumbo, alabeo + Balanceo);

        /// <summary>Pone la velocidad y el rumbo (al aparecer o al reiniciar).</summary>
        public void Reiniciar(float rumboInicial, Vector3 velocidadInicial)
        {
            rumbo = rumboInicial;
            velocidad = velocidadInicial;
            cabeceo = 0f;
            alabeo = 0f;
        }

        /// <summary>
        /// Un paso de <paramref name="dt"/> s desde <paramref name="posicion"/> hacia <paramref name="meta"/>. <paramref name="rumboDeseado"/> son los grados a los
        /// que quiere mirar. Devuelve cuánto se mueve en el paso (incluye el flote).
        /// </summary>
        public Vector3 Paso(Vector3 posicion, Vector3 meta, float rumboDeseado, float dt)
        {
            if (dt <= 0f)
            {
                return Vector3.zero;
            }
            Vector3 antes = velocidad;
            Vector3 haciaMeta = meta - posicion;
            float errorY = haciaMeta.y;
            haciaMeta.y = 0f;
            Vector3 deseada = VelocidadDeseadaHorizontal(haciaMeta, dt);
            Vector3 cambio = deseada - new Vector3(velocidad.x, 0f, velocidad.z);
            float tope = p.AceleracionMaxima * dt;
            if (cambio.magnitude > tope)
            {
                cambio = cambio.normalized * tope;
            }
            velocidad.x += cambio.x;
            velocidad.z += cambio.z;
            float deseadaY = VelocidadDeseada1D(errorY, p.VelocidadVertical, p.AceleracionFrenado, dt);
            velocidad.y = Mathf.MoveTowards(velocidad.y, deseadaY, p.AceleracionVertical * dt);

            // Rumbo con velocidad angular limitada.
            rumbo = Mathf.MoveTowardsAngle(rumbo, rumboDeseado, p.GradosRumboPorSegundo * dt);

            // Inclinación: sigue la aceleración horizontal vista desde el rumbo del dron.
            Vector3 aceleracion = (velocidad - antes) / dt;
            aceleracion.y = 0f;
            float rad = rumbo * Mathf.Deg2Rad;
            Vector3 adelante = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 derecha = new Vector3(adelante.z, 0f, -adelante.x);
            float cabeceoMeta = Mathf.Clamp(Vector3.Dot(aceleracion, adelante) * p.GradosPorAceleracion, -p.InclinacionMaxima, p.InclinacionMaxima);
            float alabeoMeta = Mathf.Clamp(-Vector3.Dot(aceleracion, derecha) * p.GradosPorAceleracion, -p.InclinacionMaxima, p.InclinacionMaxima);
            float k = 1f - Mathf.Exp(-p.SuavizadoInclinacion * dt);
            cabeceo += (cabeceoMeta - cabeceo) * k;
            alabeo += (alabeoMeta - alabeo) * k;

            // Flote: la diferencia de la onda entre cuadros, para no acumular error.
            float faseAntes = fase;
            fase += dt;
            float flote = p.AmplitudFlote * (Mathf.Sin(VueltaEnRadianes * p.FrecuenciaFlote * fase) - Mathf.Sin(VueltaEnRadianes * p.FrecuenciaFlote * faseAntes));
            return velocidad * dt + Vector3.up * flote;
        }

        private Vector3 VelocidadDeseadaHorizontal(Vector3 haciaMeta, float dt)
        {
            float distancia = haciaMeta.magnitude;
            if (distancia < Minimo)
            {
                return Vector3.zero;
            }
            float rapidez = Mathf.Min(p.VelocidadMaxima, Mathf.Sqrt(DosVeces * p.AceleracionFrenado * distancia), distancia / dt);
            return haciaMeta / distancia * rapidez;
        }

        private static float VelocidadDeseada1D(float error, float maxima, float aceleracion, float dt)
        {
            float distancia = Mathf.Abs(error);
            float rapidez = Mathf.Min(maxima, Mathf.Sqrt(DosVeces * aceleracion * distancia), distancia / dt);
            return Mathf.Sign(error) * rapidez;
        }
    }
}
