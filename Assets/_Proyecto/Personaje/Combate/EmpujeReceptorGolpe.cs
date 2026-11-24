using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1487: empuje de un golpe a pie sobre quien lo recibe. Se agrega solo al enemigo golpeado (si tiene CharacterController y no tiene
    /// Rigidbody) y lo desliza hacia atrás con <see cref="EmpujeLineal"/>. Respeta las colisiones porque mueve con el CharacterController.
    /// </summary>
    public class EmpujeReceptorGolpe : MonoBehaviour
    {
        private readonly EmpujeLineal empuje = new EmpujeLineal();
        private CharacterController cuerpo;
        // CharacterController.Move ignora pasos menores que minMoveDistance: con muchos cuadros por segundo casi todo el empuje se perdía.
        private Vector3 pendiente;

        public bool Empujando => empuje.Activo;

        public void Empujar(Vector3 velocidad, float segundos)
        {
            // VR (vr/quest3): al jugador con visor no se le empuja (marea).
            if (MechaCyberpunk.Core.ModoVR.Activo && GetComponent<MechaCyberpunk.Personaje.Jugador.JugadorAPie>() != null)
            {
                return;
            }
            velocidad.y = 0f;
            empuje.Iniciar(velocidad, segundos);
            pendiente = Vector3.zero;
        }

        private void Awake()
        {
            cuerpo = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!empuje.Activo || cuerpo == null || !cuerpo.enabled)
            {
                return;
            }
            pendiente += empuje.Avanzar(Time.deltaTime);
            if (pendiente.magnitude > cuerpo.minMoveDistance || !empuje.Activo)
            {
                cuerpo.Move(pendiente);
                pendiente = Vector3.zero;
            }
        }
    }
}
