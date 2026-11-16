using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: el tambor del revólver y del lanzagranadas. Es el hijo "Tambor" del visual del arma (su malla separada del resto) y gira una recámara
    /// por tiro alrededor del eje del cañón (el +Z del visual), suave en <c>segundos</c>. Lo maneja <see cref="ArmasEspecialesAPie"/>.
    /// </summary>
    public class TamborArma : MonoBehaviour
    {
        public const string NombreTambor = "Tambor";
        public const int RecamarasPorOmision = 6;
        private const float GradosPorVuelta = 360f;

        private Transform visual;
        private Quaternion reposo;
        private Vector3 ejeLocal = Vector3.forward;
        private float angulo;
        private float objetivo;
        private float rapidez;

        public ArmaAPie Arma { get; private set; }

        /// <summary>Grados que lleva girados (para las pruebas).</summary>
        public float Angulo => angulo;

        /// <summary>A cuántos grados va a llegar.</summary>
        public float Objetivo => objetivo;

        public void Iniciar(ArmaAPie arma, Transform visualArma)
        {
            Arma = arma;
            visual = visualArma;
            reposo = transform.localRotation;
            // El eje del cañón (el +Z del visual), en el espacio del padre del tambor.
            Vector3 mundo = visual.forward;
            ejeLocal = transform.parent != null ? transform.parent.InverseTransformDirection(mundo).normalized : mundo;
        }

        /// <summary>Gira a la siguiente de las <paramref name="recamaras"/> recámaras en <paramref name="segundos"/> s.</summary>
        public void Girar(int recamaras, float segundos)
        {
            float paso = GradosPorVuelta / Mathf.Max(1, recamaras);
            objetivo += paso;
            rapidez = paso / Mathf.Max(0.01f, segundos);
        }

        private void Update()
        {
            if (Mathf.Approximately(angulo, objetivo))
            {
                return;
            }
            angulo = Mathf.MoveTowards(angulo, objetivo, rapidez * Time.deltaTime);
            if (angulo >= GradosPorVuelta && objetivo >= GradosPorVuelta)
            {
                angulo -= GradosPorVuelta;
                objetivo -= GradosPorVuelta;
            }
            transform.localRotation = Quaternion.AngleAxis(angulo, ejeLocal) * reposo;
        }
    }
}
