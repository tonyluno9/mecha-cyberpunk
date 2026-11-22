using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Bug de CONTINUAR (inicio de partida): una posición guardada solo vale si hay piso sólido debajo, el jugador cabe ahí y está
    /// dentro del mapa (en una zona del territorio, cuando la escena lo tiene). Si no, se busca el punto válido más cercano alrededor;
    /// si tampoco hay, el jugador aparece en el inicio de la escena. Lógica pura: las consultas a la física las hace quien la usa.
    /// </summary>
    public static class ReglaPosicionSegura
    {
        /// <summary>Por qué un punto no sirve para aparecer (o <see cref="Motivo.Valida"/>).</summary>
        public enum Motivo
        {
            Valida,
            SinPiso,
            PisoBajoElMapa,
            FueraDelMapa,
            Ocupada,
        }

        /// <summary>Decide si se puede aparecer en un punto con lo que se midió ahí.</summary>
        public static Motivo Evaluar(bool hayPiso, float alturaPiso, float alturaMinimaMapa, bool dentroDelMapa, bool libre)
        {
            if (!hayPiso)
            {
                return Motivo.SinPiso;
            }
            if (alturaPiso < alturaMinimaMapa)
            {
                return Motivo.PisoBajoElMapa;
            }
            if (!dentroDelMapa)
            {
                return Motivo.FueraDelMapa;
            }
            return libre ? Motivo.Valida : Motivo.Ocupada;
        }

        /// <summary>
        /// Los puntos que se prueban alrededor de <paramref name="centro"/> (solo X y Z), del más cercano al más lejano: el centro y
        /// luego anillos cada <paramref name="paso"/> m hasta <paramref name="radioMaximo"/>, con un punto cada ~<paramref name="paso"/> m.
        /// </summary>
        public static List<Vector3> Candidatos(Vector3 centro, float radioMaximo, float paso)
        {
            List<Vector3> lista = new List<Vector3> { centro };
            if (paso <= 0f)
            {
                return lista;
            }
            for (float r = paso; r <= radioMaximo + 1e-3f; r += paso)
            {
                int cantidad = Mathf.Max(6, Mathf.CeilToInt(2f * Mathf.PI * r / paso));
                for (int i = 0; i < cantidad; i++)
                {
                    float a = i * 2f * Mathf.PI / cantidad;
                    lista.Add(new Vector3(centro.x + Mathf.Cos(a) * r, centro.y, centro.z + Mathf.Sin(a) * r));
                }
            }
            return lista;
        }

        /// <summary>Red de seguridad: true si el jugador ya cayó más de <paramref name="caidaMaxima"/> m bajo el último punto seguro, o bajo el mapa.</summary>
        public static bool DebeRescatar(float alturaJugador, float alturaUltimoSeguro, float caidaMaxima, float alturaMinimaMapa)
        {
            return alturaJugador < alturaMinimaMapa || alturaJugador < alturaUltimoSeguro - caidaMaxima;
        }
    }
}
