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

        public bool Empujando => empuje.Activo;

        public void Empujar(Vector3 velocidad, float segundos)
        {
            velocidad.y = 0f;
            empuje.Iniciar(velocidad, segundos);
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
            cuerpo.Move(empuje.Avanzar(Time.deltaTime));
        }
    }
}
