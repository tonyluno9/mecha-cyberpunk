using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-1503: guarda las últimas <see cref="Maximo"/> pisadas en charcos (posición en el mundo y momento) para que el shader MechaCyberpunk/Mundo/Charco
    /// dibuje ondas concéntricas que salen de cada una. Lógica pura: una más nueva reemplaza a la más vieja cuando ya hay <see cref="Maximo"/>.
    /// </summary>
    public sealed class ReglaOndasCharco
    {
        /// <summary>Pisadas que se recuerdan a la vez; debe coincidir con el arreglo _OndasPisadas del shader.</summary>
        public const int Maximo = 8;
        /// <summary>Segundos que dura una onda en pantalla.</summary>
        public const float Vida = 2.2f;

        private readonly Vector4[] ondas = new Vector4[Maximo]; // xyz = posición, w = momento
        private int siguiente;
        private int guardadas;

        /// <summary>Pisadas guardadas (máximo <see cref="Maximo"/>), vigentes o no.</summary>
        public int Guardadas => guardadas;

        public void Agregar(Vector3 posicion, float ahora)
        {
            ondas[siguiente] = new Vector4(posicion.x, posicion.y, posicion.z, ahora);
            siguiente = (siguiente + 1) % Maximo;
            guardadas = Mathf.Min(guardadas + 1, Maximo);
        }

        /// <summary>Momento de la pisada guardada <paramref name="i"/> (0 = la más vieja guardada).</summary>
        public float Momento(int i) => Obtener(i).w;

        public Vector3 Posicion(int i)
        {
            Vector4 o = Obtener(i);
            return new Vector3(o.x, o.y, o.z);
        }

        /// <summary>Cuántas ondas siguen vivas en <paramref name="ahora"/>.</summary>
        public int Vigentes(float ahora)
        {
            int n = 0;
            for (int i = 0; i < guardadas; i++)
            {
                if (ahora - Obtener(i).w <= Vida)
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>Llena <paramref name="destino"/> (de largo <see cref="Maximo"/>) con las ondas vivas al principio y devuelve cuántas son.</summary>
        public int Llenar(Vector4[] destino, float ahora)
        {
            int n = 0;
            for (int i = 0; i < guardadas; i++)
            {
                Vector4 o = Obtener(i);
                if (ahora - o.w <= Vida)
                {
                    destino[n++] = o;
                }
            }
            for (int i = n; i < destino.Length; i++)
            {
                destino[i] = Vector4.zero;
            }
            return n;
        }

        private Vector4 Obtener(int i)
        {
            int inicio = guardadas < Maximo ? 0 : siguiente;
            return ondas[(inicio + i) % Maximo];
        }
    }
}
