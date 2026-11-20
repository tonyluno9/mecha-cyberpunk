using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Ayudas para comprobar en Play desde la vista del jugador (integración 20-nov B). Se llaman con `unity command eval`.
    /// Usa reflexión para no depender de los ensamblados de cada área.
    /// </summary>
    public static class Capturas20nB
    {
        private const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static MonoBehaviour Jugador()
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == "ControladorAPie");
        }

        /// <summary>Pone al jugador en (x, y, z) mirando con la cámara hacia yaw (°) e inclinación (°, positiva hacia abajo).</summary>
        public static string Poner(float x, float y, float z, float yaw, float inclinacion)
        {
            MonoBehaviour c = Jugador();
            if (c == null)
            {
                return "sin jugador";
            }
            CharacterController cc = c.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            c.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            if (cc != null)
            {
                cc.enabled = true;
            }
            c.GetType().GetField("giroCamara", Todo)?.SetValue(c, yaw);
            object mirada = c.GetType().GetField("mirada", Todo)?.GetValue(c);
            mirada?.GetType().GetProperty("Inclinacion", Todo)?.SetValue(mirada, inclinacion);
            Physics.SyncTransforms();
            return "jugador en " + c.transform.position.ToString("F2") + " yaw " + yaw;
        }

        /// <summary>Mueve al jugador con su CharacterController hacia (dx, dz) en pasos de 0.1 m y dice dónde quedó (para probar muros).</summary>
        public static string Empujar(float dx, float dz, int pasos)
        {
            MonoBehaviour c = Jugador();
            CharacterController cc = c != null ? c.GetComponent<CharacterController>() : null;
            if (cc == null)
            {
                return "sin jugador";
            }
            Vector3 inicio = c.transform.position;
            Vector3 paso = new Vector3(dx, 0f, dz).normalized * 0.1f;
            for (int i = 0; i < pasos; i++)
            {
                cc.Move(paso + Vector3.down * 0.05f);
            }
            return "de " + inicio.ToString("F2") + " a " + c.transform.position.ToString("F2") + " (avanzó " + Vector3.Distance(inicio, c.transform.position).ToString("F2") + " m)";
        }

        /// <summary>Lista objetos activos cuyo nombre contiene el texto, con su posición y distancia al jugador.</summary>
        public static string Buscar(string texto, int maximo)
        {
            MonoBehaviour c = Jugador();
            Vector3 p = c != null ? c.transform.position : Vector3.zero;
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                         .Where(t => t.name.ToLowerInvariant().Contains(texto.ToLowerInvariant()))
                         .OrderBy(t => Vector3.Distance(t.position, p)).Take(maximo))
            {
                sb.AppendLine(t.name + " " + t.position.ToString("F1") + " d=" + Vector3.Distance(t.position, p).ToString("F1") + " rotY=" + t.eulerAngles.y.ToString("F0"));
            }
            return sb.ToString();
        }

        /// <summary>Cuenta componentes de un tipo (por nombre) y cuántos están fuera del rectángulo |x| > mx o |z| > mz.</summary>
        public static string Fuera(string tipo, float mx, float mz)
        {
            MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(m => m.GetType().Name == tipo && m.isActiveAndEnabled).ToArray();
            MonoBehaviour[] fuera = todos.Where(m => Mathf.Abs(m.transform.position.x) > mx || Mathf.Abs(m.transform.position.z) > mz).ToArray();
            return tipo + ": " + todos.Length + " activos, " + fuera.Length + " fuera" + string.Concat(fuera.Take(5).Select(m => " | " + m.name + " " + m.transform.position.ToString("F0")));
        }

        /// <summary>Lee un campo o propiedad (también privado) del primer componente con ese nombre de tipo.</summary>
        public static string Leer(string tipo, string miembro)
        {
            MonoBehaviour m = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(x => x.GetType().Name == tipo);
            if (m == null)
            {
                return "sin " + tipo;
            }
            object v = m.GetType().GetField(miembro, Todo)?.GetValue(m) ?? m.GetType().GetProperty(miembro, Todo)?.GetValue(m);
            return tipo + "." + miembro + " = " + v;
        }
    }
}
