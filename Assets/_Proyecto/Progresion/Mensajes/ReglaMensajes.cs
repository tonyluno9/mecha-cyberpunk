using System.Collections.Generic;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Reglas puras de los mensajes del teléfono (T-449): el catálogo de lo que te escriben los aliados y los profes, y cuándo le toca a cada uno
    /// según cómo vas en el juego. Cada mensaje llega una sola vez.
    /// </summary>
    public static class ReglaMensajes
    {
        public const string IdLibre = "libre";
        public const string IdGuarida = "guarida";
        public const string IdImplantes = "implantes";
        public const string IdZona = "zona";
        public const string IdCalor = "calor";
        public const string IdContratos = "contratos";

        public const int DineroGuarida = 500;
        public const int DineroImplantes = 2000;
        public const int ZonasParaAvisar = 2;
        public const int NivelCalorParaAvisar = 2;
        public const int DiaContratos = 2;

        /// <summary>Un mensaje del catálogo.</summary>
        public readonly struct Mensaje
        {
            public Mensaje(string id, string remitente, string texto)
            {
                Id = id;
                Remitente = remitente;
                Texto = texto;
            }

            public string Id { get; }
            public string Remitente { get; }
            public string Texto { get; }
        }

        /// <summary>Cómo va el juego ahora mismo (lo que mira la regla).</summary>
        public struct Contexto
        {
            /// <summary>La historia ya terminó: el jugador está en el juego libre.</summary>
            public bool Libre;
            public int Dinero;
            /// <summary>Cuántas zonas son del jugador.</summary>
            public int ZonasTuyas;
            public int NivelCalor;
            public int Dia;
        }

        private static readonly Mensaje[] Catalogo =
        {
            new Mensaje(IdLibre, "EL INVENTOR", "Ya eres libre, chamaco. Pásate por el CIC: tengo mejoras para tu mecha."),
            new Mensaje(IdGuarida, "EL MECÁNICO", "Tu casa se puede mejorar: armero, garaje y cama. Busca los postes turquesa junto a tu cama."),
            new Mensaje(IdImplantes, "EL DOC", "Con Ƀ2,000 en la bolsa ven a I+D+I: tengo implantes que te van a salvar el pellejo."),
            new Mensaje(IdZona, "EL HACKER", "Esa zona ya es nuestra. Pasa por los postes turquesa: cada negocio te paga por día."),
            new Mensaje(IdCalor, "EL HACKER", "La policía ya te trae en el radar. Duerme en tu cama o guarda el carro: se les olvida."),
            new Mensaje(IdContratos, "NOVA", "Hay contratos corporativos de día. Ábrelos en tu teléfono (T) en la pestaña CONTRATOS."),
        };

        public static IReadOnlyList<Mensaje> Todos => Catalogo;

        /// <summary>El mensaje con ese id, o false si no existe.</summary>
        public static bool Buscar(string id, out Mensaje mensaje)
        {
            foreach (Mensaje m in Catalogo)
            {
                if (m.Id == id)
                {
                    mensaje = m;
                    return true;
                }
            }
            mensaje = default;
            return false;
        }

        /// <summary>Si el mensaje ya le toca al jugador según cómo va.</summary>
        public static bool LeToca(string id, Contexto c)
        {
            switch (id)
            {
                case IdLibre: return c.Libre;
                case IdGuarida: return c.Libre && c.Dinero >= DineroGuarida;
                case IdImplantes: return c.Dinero >= DineroImplantes;
                case IdZona: return c.ZonasTuyas >= ZonasParaAvisar;
                case IdCalor: return c.NivelCalor >= NivelCalorParaAvisar;
                case IdContratos: return c.Libre && c.Dia >= DiaContratos;
                default: return false;
            }
        }

        /// <summary>Los ids que le tocan ahora y todavía no se han entregado, en el orden del catálogo.</summary>
        public static List<string> Pendientes(Contexto c, ISet<string> entregados)
        {
            List<string> lista = new List<string>();
            foreach (Mensaje m in Catalogo)
            {
                if ((entregados == null || !entregados.Contains(m.Id)) && LeToca(m.Id, c))
                {
                    lista.Add(m.Id);
                }
            }
            return lista;
        }
    }
}
