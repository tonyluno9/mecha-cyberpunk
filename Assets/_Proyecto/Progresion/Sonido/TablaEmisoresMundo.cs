using System;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>Clase de fuente del mundo: define cuántas pueden sonar a la vez.</summary>
    public enum GrupoEmisorMundo
    {
        Interior = 0,
        Objeto = 1,
        Lamina = 2,
        Fiesta = 3,
        Puntual = 4,
    }

    /// <summary>
    /// T-1535: dónde suena cada sonido de ambiente de objetos e interiores. Se busca por NOMBRE en la escena en ejecución (no hay que editar el YAML de la
    /// ciudad ni sus prefabs): <see cref="SonidosMundo"/> recorre la escena, crea un punto de sonido en cada ancla y deja sonar solo las más cercanas.
    /// Los radios salen de la política de cada ID (TablaMezcla20nov); aquí solo van los topes y los tiempos de los sonidos que no son bucle.
    /// </summary>
    public static class TablaEmisoresMundo
    {
        /// <summary>Un tipo de ancla: cómo se llama en la escena, qué IDs suenan ahí y de qué grupo es.</summary>
        public readonly struct Definicion
        {
            public Definicion(string nombre, bool prefijo, GrupoEmisorMundo grupo, bool centrado, float minimoSegundos, float maximoSegundos, params string[] ids)
            {
                Nombre = nombre;
                Prefijo = prefijo;
                Grupo = grupo;
                Centrado = centrado;
                MinimoSegundos = minimoSegundos;
                MaximoSegundos = maximoSegundos;
                Ids = ids;
            }

            public string Nombre { get; }
            /// <summary>True: vale cualquier objeto cuyo nombre empiece así; false: el nombre exacto.</summary>
            public bool Prefijo { get; }
            public GrupoEmisorMundo Grupo { get; }
            /// <summary>True: el punto de sonido va al centro de los renderers del ancla (locales cuya raíz está en el origen); false: en el pivote.</summary>
            public bool Centrado { get; }
            /// <summary>Si el máximo es mayor que cero, el sonido no es bucle: suena cada tantos segundos (entre mínimo y máximo) mientras el jugador esté cerca.</summary>
            public float MinimoSegundos { get; }
            public float MaximoSegundos { get; }
            public string[] Ids { get; }
            public bool EsPuntual => MaximoSegundos > 0f;
        }

        /// <summary>Cuántas fuentes de cada grupo suenan a la vez (por índice de <see cref="GrupoEmisorMundo"/>). Entre todas, bastante menos de las 12 voces del gestor.</summary>
        public static readonly int[] TopePorGrupo = { 2, 2, 1, 1, 2 };

        /// <summary>Cuántas fuentes con el mismo ID pueden sonar a la vez (el gestor deja 2 por ID de objeto y 1 por interior).</summary>
        public const int TopePorId = 2;

        /// <summary>Cada cuántos segundos se revisa qué fuentes quedan cerca.</summary>
        public const float SegundosRevision = 0.5f;

        /// <summary>Cuánto se espera tras cargar la escena para recorrerla (lo que colocan en ejecución las tiendas ya está puesto).</summary>
        public const float SegundosAntesDeRecorrer = 2f;

        /// <summary>Segunda pasada, para lo que se coloca más tarde (la tiendita, las expendedoras).</summary>
        public const float SegundosSegundaPasada = 8f;

        public static readonly Definicion[] Definiciones =
        {
            // Interiores (3D, 15 m): el taller de Cánepa, el CIC de Tino (radio y ventilador juntos), la armería y las bodegas (si Mundo nombra alguna "Bodega...").
            new Definicion("Lugar_TallerMecanico", false, GrupoEmisorMundo.Interior, true, 0f, 0f, "INT_TALLER_CANEPA"),
            new Definicion("Lugar_CIC", false, GrupoEmisorMundo.Interior, true, 0f, 0f, "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR"),
            new Definicion("T931_Armero", false, GrupoEmisorMundo.Interior, true, 0f, 0f, "INT_ARMERIA_VENTILADOR"),
            new Definicion("Bodega", true, GrupoEmisorMundo.Interior, true, 0f, 0f, "INT_BODEGA_GOTERAS"),
            // Objetos: zumbidos bajitos, solo los más cercanos al jugador.
            new Definicion("C08_Transformador", true, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_TRANSFORMADOR"),
            new Definicion("Transformador_Poste", true, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_TRANSFORMADOR"),
            new Definicion("C10_AireAcondicionado", true, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_AIRE_ACONDICIONADO"),
            new Definicion("Azotea_AC_Tinaco", true, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_AIRE_ACONDICIONADO"),
            new Definicion("CY08_CamaraCorporativa", true, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_CAMARA_SEGURIDAD"),
            // Refrigeradores: la tiendita (la coloca ColocadorTiendita al cargar) y la clínica.
            new Definicion("Tiendita", false, GrupoEmisorMundo.Objeto, true, 0f, 0f, "OBJ_REFRIGERADOR"),
            new Definicion("Clinica_Camilla", false, GrupoEmisorMundo.Objeto, false, 0f, 0f, "OBJ_REFRIGERADOR"),
            // La lámina de los toldos: solo suena con lluvia (SonidosMundo lo pregunta a ClimaSonoro).
            new Definicion("Lamina", false, GrupoEmisorMundo.Lamina, false, 0f, 0f, "CLI_LLUVIA_SOBRE_LAMINA"),
            // Una casa con fiesta: la bocina con la cumbia (Mundo nombra "Bocina_Fiesta" a la que quiera).
            new Definicion("Bocina_Fiesta", true, GrupoEmisorMundo.Fiesta, false, 0f, 0f, "RAD_EST1_CUMBIA_PERCUSION"),
            // Eventos de 1 a 7 s que no son bucle: la expendedora (zumbido y golpe de la lata) y la tele que se reinicia (casa y hacker).
            new Definicion("CY06_Expendedora", true, GrupoEmisorMundo.Puntual, false, 18f, 40f, "OBJ_MAQUINA_EXPENDEDORA"),
            new Definicion("Expendedora_", true, GrupoEmisorMundo.Puntual, false, 18f, 40f, "OBJ_MAQUINA_EXPENDEDORA"),
            new Definicion("Set_Hacker", false, GrupoEmisorMundo.Puntual, true, 45f, 90f, "OBJ_TELE_REINICIO"),
            new Definicion("H06_TeleVieja", true, GrupoEmisorMundo.Puntual, false, 60f, 120f, "OBJ_TELE_REINICIO"),
        };

        /// <summary>El índice de la primera definición que le toca al nombre, o -1 si ninguna.</summary>
        public static int Buscar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return -1;
            for (int i = 0; i < Definiciones.Length; i++)
            {
                Definicion d = Definiciones[i];
                if (d.Prefijo ? nombre.StartsWith(d.Nombre, StringComparison.Ordinal) : nombre == d.Nombre) return i;
            }
            return -1;
        }
    }
}
