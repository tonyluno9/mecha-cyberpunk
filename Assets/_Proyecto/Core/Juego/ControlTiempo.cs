using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T81: dueño único de <c>Time.timeScale</c> y <c>Time.fixedDeltaTime</c>. Cada pantalla o efecto que quiere cámara lenta o pausa
    /// hace <see cref="Pedir"/> con su propio dueño y <see cref="Soltar"/> al terminar. La escala real es el mínimo de los pedidos
    /// activos (0 si alguno pausa) y 1 si no hay ninguno; así nadie "restaura el valor de antes" ni pisa el de otro.
    /// </summary>
    public static class ControlTiempo
    {
        private static readonly Dictionary<object, float> pedidos = new Dictionary<object, float>();
        private static float baseFija;

        /// <summary>Escala que se aplicaría con los pedidos actuales (sin leer Time).</summary>
        public static float Escala => Calcular(pedidos.Values);

        public static int Cantidad => pedidos.Count;

        public static bool Pide(object dueno) => dueno != null && pedidos.ContainsKey(dueno);

        /// <summary>Mínimo de las escalas (0 si alguna pausa); 1 si no hay ninguna. Función pura.</summary>
        public static float Calcular(IEnumerable<float> escalas)
        {
            float escala = 1f;
            if (escalas == null)
            {
                return escala;
            }
            foreach (float e in escalas)
            {
                escala = Mathf.Min(escala, Mathf.Max(0f, e));
            }
            return escala;
        }

        public static void Pedir(object dueno, float escala)
        {
            if (dueno == null)
            {
                return;
            }
            pedidos[dueno] = Mathf.Max(0f, escala);
            Aplicar();
        }

        public static void Soltar(object dueno)
        {
            if (dueno == null || !pedidos.Remove(dueno))
            {
                return;
            }
            Aplicar();
        }

        /// <summary>Quita todos los pedidos y vuelve a velocidad normal.</summary>
        public static void SoltarTodo()
        {
            pedidos.Clear();
            Aplicar();
        }

        /// <summary>"escala=… pedidos=[dueño=escala, …]" para depurar.</summary>
        public static string Depurar()
        {
            StringBuilder sb = new StringBuilder("escala=").Append(Escala.ToString("0.###")).Append(" pedidos=[");
            bool primero = true;
            foreach (KeyValuePair<object, float> p in pedidos)
            {
                if (!primero)
                {
                    sb.Append(", ");
                }
                primero = false;
                sb.Append(p.Key is Object o ? (o != null ? o.name : "(destruido)") : p.Key.ToString()).Append('=').Append(p.Value.ToString("0.###"));
            }
            return sb.Append(']').ToString();
        }

        private static void Aplicar()
        {
            if (baseFija <= 0f)
            {
                baseFija = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            }
            float escala = Escala;
            Time.timeScale = escala;
            Time.fixedDeltaTime = escala > 0f ? baseFija * escala : baseFija;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            pedidos.Clear();
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            if (baseFija > 0f)
            {
                Aplicar();
            }
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo == LoadSceneMode.Single)
            {
                SoltarTodo();
            }
        }
    }
}
