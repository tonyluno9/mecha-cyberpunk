using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T94: la regla pura de la fase 0 del modo libre (solo SKULLS, antes de la policía). Dura al menos <see cref="ParametrosRespuesta.SegundosFaseSkulls"/>;
    /// pasado ese tiempo termina en cuanto llevas <see cref="ParametrosRespuesta.SkullsFaseSkulls"/> SKULLS vencidos, y siempre termina a
    /// <see cref="ParametrosRespuesta.SegundosMaximosFaseSkulls"/> para que no se atore. Mientras dura manda un goteo constante de grupos pequeños
    /// (cada <see cref="ParametrosRespuesta.SegundosEntreGruposSkulls"/> s, hasta <see cref="ParametrosRespuesta.SkullsVivosFaseSkulls"/> vivos y dentro
    /// del tope de enemigos del modo libre); una fracción llega armada. Sin Unity: el tiempo entra como dt y el azar (0 a 1) como parámetro.
    /// </summary>
    public sealed class ReglaFaseSkulls
    {
        /// <summary>Si no hubo dónde poner un grupo, se vuelve a intentar tras estos segundos (no es de balance: solo evita buscar cada cuadro).</summary>
        public const float SegundosReintento = 1f;

        private readonly ParametrosRespuesta p;
        private float proximoGrupo;

        public ReglaFaseSkulls(ParametrosRespuesta parametros)
        {
            p = parametros ?? ParametrosRespuesta.PorOmision();
            proximoGrupo = Math.Max(0f, p.SegundosPrimerGrupoSkulls);
        }

        /// <summary>Segundos que lleva la fase.</summary>
        public float Segundos { get; private set; }

        /// <summary>SKULLS vencidos en la fase.</summary>
        public int Muertes { get; private set; }

        /// <summary>Grupos mandados.</summary>
        public int Grupos { get; private set; }

        /// <summary>True cuando la fase ya se cumplió (llega la policía).</summary>
        public bool Terminada
        {
            get
            {
                if (p.SegundosMaximosFaseSkulls > 0f && Segundos >= p.SegundosMaximosFaseSkulls)
                {
                    return true;
                }
                return Segundos >= p.SegundosFaseSkulls && (p.SkullsFaseSkulls <= 0 || Muertes >= p.SkullsFaseSkulls);
            }
        }

        /// <summary>Avanza el reloj. Devuelve true si con este paso la fase quedó terminada.</summary>
        public bool Avanzar(float dt)
        {
            bool antes = Terminada;
            Segundos += Math.Max(0f, dt);
            return !antes && Terminada;
        }

        /// <summary>Cayó un SKULL durante la fase.</summary>
        public void RegistrarMuerte()
        {
            if (!Terminada)
            {
                Muertes++;
            }
        }

        /// <summary>
        /// Cuántos SKULLS deben salir ahora (0 si no toca). <paramref name="vivos"/>: SKULLS del goteo en pie; <paramref name="espacio"/>: lugares que deja
        /// el tope de enemigos del modo libre; <paramref name="azar"/> (0 a 1) elige el tamaño del grupo. No avanza el turno: llama a
        /// <see cref="RegistrarGrupo"/> si salió o a <see cref="RegistrarFallo"/> si no hubo dónde.
        /// </summary>
        public int CuantosGenerar(int vivos, int espacio, float azar)
        {
            if (Terminada || Segundos < proximoGrupo)
            {
                return 0;
            }
            int libres = Math.Min(p.SkullsVivosFaseSkulls - vivos, espacio);
            if (libres <= 0)
            {
                return 0; // sin lugar: sale en cuanto caiga uno (sin huecos)
            }
            int minimo = Math.Max(1, p.SkullsPorGrupoMin);
            int maximo = Math.Max(minimo, p.SkullsPorGrupoMax);
            float a = Math.Max(0f, Math.Min(0.9999f, azar));
            int tamano = minimo + (int)(a * (maximo - minimo + 1));
            return Math.Min(tamano, libres);
        }

        /// <summary>Salió un grupo: el siguiente toca en <see cref="ParametrosRespuesta.SegundosEntreGruposSkulls"/> s.</summary>
        public void RegistrarGrupo()
        {
            Grupos++;
            proximoGrupo = Segundos + p.SegundosEntreGruposSkulls;
        }

        /// <summary>No hubo dónde ponerlo: se reintenta pronto.</summary>
        public void RegistrarFallo() => proximoGrupo = Segundos + SegundosReintento;

        /// <summary>True si el SKULL con este azar (0 a 1) llega armado.</summary>
        public bool EsArmado(float azar) => azar < p.FraccionSkullsArmados;
    }
}
