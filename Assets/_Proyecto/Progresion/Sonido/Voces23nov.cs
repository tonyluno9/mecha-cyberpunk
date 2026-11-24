using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-voces-personajes-23nov: las voces grabadas de los personajes (Assets/Arte/Audio/19_Voces/&lt;Carpeta&gt;/&lt;ID&gt;_NN.wav). Aquí viven sus IDs, su carpeta,
    /// su política de mezcla (una voz por personaje a la vez, volumen arriba de los efectos, distancia 3D) y cuáles son frases largas (Vorbis en memoria).
    /// Lo de las SKULLS comunes sigue en <see cref="ReglaMezclaSonidos.IdsSkulls"/>; aquí solo SKU_MUERTE (nueva), el matón y el jefe. Lógica pura.
    /// </summary>
    public static class Voces23nov
    {
        public const string CarpetaRaiz = "Assets/Arte/Audio/19_Voces";

        // Grupos de mezcla: uno por personaje (una sola voz a la vez; la nueva corta a la vieja).
        public const string GrupoJugador = "voz-jugador";
        public const string GrupoMaton = "voz-maton";
        public const string GrupoJefeSkulls = "voz-jefe-skulls";
        public const string GrupoPolicia = "voz-policia";
        public const string GrupoMegafono = "voz-megafono";
        public const string GrupoNova = "voz-nova";
        public const string GrupoJefeNova = "voz-jefe-nova";
        public const string GrupoDoc = "voz-doc";
        public const string GrupoOtros = "voz-otros";

        /// <summary>El bucle de respiración del jugador cansado (con fundido de entrada y salida).</summary>
        public const string RespiracionCansado = "JUG_RESPIRACION_CANSADO";

        /// <summary>Segundos del fundido de entrada y de salida de la respiración.</summary>
        public const float SegundosFundidoRespiracion = 0.8f;

        public sealed class Carpeta
        {
            public readonly string Nombre;
            public readonly string Grupo;
            public readonly bool FrasesLargas;
            public readonly float Distancia;
            public readonly string[] Ids;

            public Carpeta(string nombre, string grupo, bool frasesLargas, float distancia, params string[] ids)
            {
                Nombre = nombre;
                Grupo = grupo;
                FrasesLargas = frasesLargas;
                Distancia = distancia;
                Ids = ids;
            }
        }

        /// <summary>Las carpetas de 19_Voces que entran en esta tarea (SKULLS ya venía de T-1623: aquí solo su SKU_MUERTE nueva).</summary>
        public static readonly Carpeta[] Carpetas =
        {
            new Carpeta("Jugador", GrupoJugador, false, 0f,
                "JUG_ESFUERZO", "JUG_SALTO", "JUG_DOLOR_LEVE", "JUG_DOLOR_FUERTE", "JUG_MUERTE", "JUG_QUEMADO", "JUG_TOS", RespiracionCansado),
            new Carpeta("SKULLS", null, false, ReglaMezclaSonidos.DistanciaVozSkulls, "SKU_MUERTE"),
            new Carpeta("Maton", GrupoMaton, false, 30f,
                "SKU_MATON_PROVOCAR", "SKU_MATON_ATAQUE", "SKU_MATON_RISA", "SKU_MATON_DOLOR", "SKU_MATON_MUERTE"),
            new Carpeta("Jefe_SKULLS", GrupoJefeSkulls, true, 45f,
                "SKU_JEFE_MALETIN", "SKU_JEFE_GUARDIAS", "SKU_JEFE_CASTIGO", "SKU_JEFE_ENFRENTAMIENTO", "SKU_JEFE_DERROTA", "SKU_JEFE_DOLOR"),
            new Carpeta("Policia", GrupoPolicia, false, 35f,
                "POL_VOZ_ALTO", "POL_VOZ_RENDIRSE", "POL_VOZ_REFUERZOS", "POL_VOZ_OFICIAL_CAIDO", "POL_VOZ_ALTAVOZ", "POL_VOZ_LO_PERDIMOS", "POL_VOZ_DOLOR", "POL_VOZ_MUERTE"),
            new Carpeta("Policia/_megafono", GrupoMegafono, false, 60f,
                "POL_MEGAFONO_ALTO", "POL_MEGAFONO_RENDIRSE", "POL_MEGAFONO_LO_PERDIMOS"),
            new Carpeta("Nova", GrupoNova, false, 35f,
                "NOV_VOZ_ALERTA", "NOV_VOZ_ORDEN", "NOV_VOZ_CUBRIRSE", "NOV_VOZ_LO_PERDIMOS", "NOV_VOZ_DOLOR", "NOV_VOZ_MUERTE"),
            new Carpeta("Jefe_Nova", GrupoJefeNova, true, 60f,
                "NOV_JEFE_FRASE", "NOV_JEFE_REVELACION", "NOV_JEFE_ATAQUE", "NOV_JEFE_MUERTE"),
            new Carpeta("Doc", GrupoDoc, true, 0f,
                "DOC_VOZ_SALUDO", "DOC_VOZ_SIENTATE", "DOC_VOZ_EXPLICAR", "DOC_VOZ_REVIVIR", "DOC_VOZ_SUERO", "DOC_VOZ_DESPEDIDA"),
            // Solo en el catálogo (no se conectan en esta tarea).
            new Carpeta("Canepa", GrupoOtros, false, 25f, "CANEPA_VOZ_SALUDO", "CANEPA_VOZ_REPARAR", "CANEPA_VOZ_DESPEDIDA"),
            new Carpeta("Tino", GrupoOtros, false, 25f, "TINO_VOZ_SALUDO", "TINO_VOZ_COMPRA", "TINO_VOZ_BANCO", "TINO_VOZ_SIN_DINERO", "TINO_VOZ_DESPEDIDA"),
            new Carpeta("Mesera_Robot", GrupoOtros, false, 25f, "MESERA_ROBOT_SALUDO", "MESERA_ROBOT_SERVIR", "MESERA_ROBOT_SUERTE", "MESERA_ROBOT_DESPEDIDA"),
            new Carpeta("Peatones", GrupoOtros, false, 25f, ArmarPeatones()),
        };

        private static readonly Dictionary<string, Carpeta> PorId = ArmarPorId();

        /// <summary>Todos los IDs de voz de esta tarea.</summary>
        public static readonly string[] Ids = ArmarIds();

        private static string[] ArmarPeatones()
        {
            string[] tipos = { "ENOJO", "MIEDO", "POLICIA", "RISA", "SALUDO", "VENDEDOR" };
            List<string> ids = new List<string>();
            for (int v = 1; v <= 3; v++)
            {
                foreach (string t in tipos) ids.Add("NPC_VOZ" + v + "_" + t);
            }
            return ids.ToArray();
        }

        private static Dictionary<string, Carpeta> ArmarPorId()
        {
            Dictionary<string, Carpeta> d = new Dictionary<string, Carpeta>();
            foreach (Carpeta c in Carpetas)
            {
                foreach (string id in c.Ids) d[id] = c;
            }
            return d;
        }

        private static string[] ArmarIds()
        {
            List<string> ids = new List<string>();
            foreach (Carpeta c in Carpetas) ids.AddRange(c.Ids);
            return ids.ToArray();
        }

        /// <summary>Si el ID es una de las voces grabadas del 23-nov.</summary>
        public static bool EsVoz(string id) => id != null && PorId.ContainsKey(id);

        /// <summary>La carpeta (dentro de 19_Voces) del ID, o null.</summary>
        public static string CarpetaDe(string id) => id != null && PorId.TryGetValue(id, out Carpeta c) ? c.Nombre : null;

        /// <summary>Si la carpeta (ruta relativa a 19_Voces, como "Doc" o "Policia/_megafono") es de frases largas: Vorbis comprimido en memoria.</summary>
        public static bool EsCarpetaDeFrasesLargas(string carpetaRelativa)
        {
            foreach (Carpeta c in Carpetas)
            {
                if (c.FrasesLargas && string.Equals(c.Nombre, carpetaRelativa, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>Las del jugador y las del Doc suenan en 2D (las pide el código con Sonidos.Reproducir); las demás en 3D.</summary>
        public static bool Es2D(string id) => id != null && (id.StartsWith("JUG_", StringComparison.Ordinal) || id.StartsWith("DOC_", StringComparison.Ordinal));

        /// <summary>Segundos del fundido de entrada y salida de un bucle de voz (0: sin fundido).</summary>
        public static float SegundosFundido(string id) => id == RespiracionCansado ? SegundosFundidoRespiracion : 0f;

        /// <summary>Si la voz baja un poco la música y el ambiente mientras suena (todas menos la respiración).</summary>
        public static bool BajaPorVoz(string id) => EsVoz(id) && id != RespiracionCansado;

        /// <summary>
        /// La política de mezcla de la voz: una sola a la vez por personaje (grupo), volumen 1 (arriba de los efectos, que van de 0.5 a 0.9), un poco de
        /// variación de tono y un mínimo entre pedidos del mismo ID para no ametrallar. false si el ID no es de esta tarea (o es una SKULL común).
        /// </summary>
        public static bool TryPolitica(string id, out PoliticaSonido politica)
        {
            politica = default;
            if (id == null || !PorId.TryGetValue(id, out Carpeta c) || c.Grupo == null) return false;
            float minimo = 0.4f;
            float tono = 0.03f;
            switch (id)
            {
                case "JUG_ESFUERZO": minimo = 0.5f; tono = 0.05f; break;
                case "JUG_SALTO": minimo = 0.3f; tono = 0.04f; break;
                case "JUG_DOLOR_LEVE": minimo = 0.6f; break;
                case "JUG_DOLOR_FUERTE": minimo = 0.5f; break;
                case "JUG_TOS": minimo = 2f; break;
                case "JUG_QUEMADO": minimo = 1.5f; break;
                case "JUG_MUERTE": minimo = 0f; tono = 0f; break;
                case RespiracionCansado: minimo = 0f; tono = 0f; break;
            }
            if (c.FrasesLargas) tono = 0f; // las frases del Doc y los jefes se oyen tal cual
            politica = new PoliticaSonido(c.Grupo, 1, 1f, tono, minimo, c.Distancia, 1f, 0.04f);
            return true;
        }
    }
}
