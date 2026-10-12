using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Datos;
using MechaCyberpunk.Combate.Golpe;
using UnityEngine;

namespace MechaCyberpunk.Combate.Brazos
{
    /// <summary>
    /// Mueve por código los pivotes de los brazos de la cabina, sin Animator. Brazo derecho: atrás al preparar, al frente
    /// en el impacto y de regreso en la recarga. Con la guardia, los dos brazos se cruzan frente al cristal.
    /// </summary>
    public class AnimadorBrazos : MonoBehaviour
    {
        [SerializeField] private Transform brazoIzquierdo;
        [SerializeField] private Transform brazoDerecho;
        [SerializeField] private ConfigBrazos config;

        private GolpeMecha golpe;
        private GuardiaMecha guardia;
        private Vector3 reposoIzquierdo;
        private Vector3 reposoDerecho;
        private Quaternion giroReposoIzquierdo;
        private Quaternion giroReposoDerecho;

        public Transform BrazoIzquierdo => brazoIzquierdo;
        public Transform BrazoDerecho => brazoDerecho;

        private void Awake()
        {
            golpe = GetComponent<GolpeMecha>();
            guardia = GetComponent<GuardiaMecha>();
            if (brazoIzquierdo == null || brazoDerecho == null || config == null)
            {
                Debug.LogError("[Combate] Faltan referencias en AnimadorBrazos de " + name, this);
                enabled = false;
                return;
            }
            reposoIzquierdo = brazoIzquierdo.localPosition;
            reposoDerecho = brazoDerecho.localPosition;
            giroReposoIzquierdo = brazoIzquierdo.localRotation;
            giroReposoDerecho = brazoDerecho.localRotation;
        }

        private void OnDisable()
        {
            if (brazoIzquierdo != null && brazoDerecho != null)
            {
                brazoIzquierdo.SetLocalPositionAndRotation(reposoIzquierdo, giroReposoIzquierdo);
                brazoDerecho.SetLocalPositionAndRotation(reposoDerecho, giroReposoDerecho);
            }
        }

        private void Update()
        {
            // Pose del golpe (solo el brazo derecho): desplazamiento y giro respecto al reposo.
            Vector3 desplazamiento = Vector3.zero;
            Vector3 giro = Vector3.zero;
            if (golpe != null)
            {
                float p = golpe.Progreso;
                switch (golpe.Fase)
                {
                    case FaseGolpe.Preparando:
                        desplazamiento = config.DesplazamientoPreparacion * p;
                        giro = config.GiroPreparacion * p;
                        break;
                    case FaseGolpe.Recuperando:
                        float vuelta = Mathf.SmoothStep(0f, 1f, p);
                        desplazamiento = Vector3.Lerp(config.DesplazamientoImpacto, Vector3.zero, vuelta);
                        giro = Vector3.Lerp(config.GiroImpacto, Vector3.zero, vuelta);
                        break;
                }
            }

            float g = guardia != null ? Mathf.SmoothStep(0f, 1f, guardia.Progreso) : 0f;
            Vector3 desplazamientoGuardia = config.DesplazamientoGuardia;
            Vector3 giroGuardia = config.GiroGuardia;

            MoverBrazo(brazoDerecho, reposoDerecho, giroReposoDerecho,
                Vector3.Lerp(desplazamiento, desplazamientoGuardia, g), Vector3.Lerp(giro, giroGuardia, g));
            MoverBrazo(brazoIzquierdo, reposoIzquierdo, giroReposoIzquierdo,
                ReflejarDesplazamiento(desplazamientoGuardia) * g, ReflejarGiro(giroGuardia) * g);
        }

        // Refleja una pose del lado derecho al izquierdo: lo lateral y los giros en Y y Z cambian de signo.
        private static Vector3 ReflejarDesplazamiento(Vector3 v) => new Vector3(-v.x, v.y, v.z);
        private static Vector3 ReflejarGiro(Vector3 v) => new Vector3(v.x, -v.y, -v.z);

        private void MoverBrazo(Transform brazo, Vector3 reposo, Quaternion giroReposo, Vector3 desplazamiento, Vector3 giro)
        {
            float factor = 1f - Mathf.Exp(-config.Suavizado * Time.deltaTime);
            Vector3 objetivo = reposo + desplazamiento;
            Quaternion giroObjetivo = giroReposo * Quaternion.Euler(giro);
            brazo.SetLocalPositionAndRotation(
                Vector3.Lerp(brazo.localPosition, objetivo, factor),
                Quaternion.Slerp(brazo.localRotation, giroObjetivo, factor));
        }
    }
}
