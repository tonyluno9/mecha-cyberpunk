using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>
    /// Empuje de una habilidad: mueve el <see cref="CharacterController"/> del golpeado hacia afuera una distancia en una duración.
    /// Usa <c>Move</c>, así que las paredes lo detienen. Un empuje nuevo reemplaza al anterior. Se quita solo al terminar.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EmpujeMecha : MonoBehaviour
    {
        // Velocidad hacia abajo que lo mantiene pegado al piso mientras lo empujan (m/s).
        private const float VelocidadEnPiso = -2f;

        private CharacterController controlador;
        private Vector3 direccion;
        private float distancia;
        private float duracion;
        private float transcurrido;
        private float recorrido;

        /// <summary>Empuja a <paramref name="destino"/> (en el plano). No hace nada si no tiene controlador o la distancia es 0.</summary>
        public static void Aplicar(GameObject destino, Vector3 direccion, float distancia, float duracion)
        {
            if (distancia <= 0f || destino.GetComponent<CharacterController>() == null)
            {
                return;
            }
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 1e-6f)
            {
                return;
            }
            EmpujeMecha empuje = destino.GetComponent<EmpujeMecha>();
            if (empuje == null)
            {
                empuje = destino.AddComponent<EmpujeMecha>();
            }
            empuje.direccion = direccion.normalized;
            empuje.distancia = distancia;
            empuje.duracion = Mathf.Max(0.02f, duracion);
            empuje.transcurrido = 0f;
            empuje.recorrido = 0f;
        }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transcurrido += dt;
            float paso = ReglaEfectosHabilidad.PasoEmbestida(distancia, duracion, recorrido, dt);
            if (paso > 0f && controlador.enabled)
            {
                controlador.Move(direccion * paso + Vector3.up * (VelocidadEnPiso * dt));
            }
            recorrido += paso;
            if (recorrido >= distancia - 1e-4f || transcurrido >= duracion)
            {
                Destroy(this);
            }
        }
    }
}
