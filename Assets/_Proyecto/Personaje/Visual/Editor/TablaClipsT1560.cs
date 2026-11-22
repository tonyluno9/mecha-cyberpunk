using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1560: qué hace la importación con la raíz (root) de un clip.
    /// Con Apply Root Motion apagado (todos los visuales del juego) el movimiento que Unity extrae de la raíz se ignora y lo que se queda en la pose se ve.
    /// </summary>
    public enum RaizClipT1560
    {
        /// <summary>No se toca: el clip conserva lo que ya tenía (clips aprobados antes del 21-nov: moto, carro, parkour, escaleras, locomoción).</summary>
        SinCambio,
        /// <summary>Clip en el sitio (la cadera casi no se desplaza): Bake Into Pose en rotación, altura y XZ. Se ve exactamente lo que Tony aprobó en Blender.</summary>
        Sitio,
        /// <summary>El clip trae desplazamiento horizontal (cae, sale volando, camina, salta): rotación y altura en la pose, XZ como root motion (lo recoge RaizCaidaJugador o se ignora).</summary>
        Avanza,
        /// <summary>Sube en vertical sin avanzar (escalera): rotación y XZ en la pose, la altura como root motion para que el cuerpo suba con la cápsula y no dos veces.</summary>
        Sube,
        /// <summary>Sube y avanza (colgado a la cornisa): rotación en la pose, altura y XZ como root motion.</summary>
        SubeYAvanza,
        /// <summary>Camina en círculo: ni la rotación ni la altura ni el XZ se hornean (si el giro se hornea el cuerpo da vueltas en el sitio).</summary>
        Gira,
    }

    /// <summary>T-1560: la decisión de importación de un clip.</summary>
    public sealed class DecisionClipT1560
    {
        public string Nombre;
        /// <summary>PJ01 (también los NPC_*), E15, E16, E18 o E20.</summary>
        public string Duenio;
        /// <summary>Clip del 21-nov (o reemplazado ese día): la tabla manda todo. Falso = ya aprobado antes: solo compresión y avatar.</summary>
        public bool Nuevo;
        /// <summary>El FBX trae el esqueleto mixamorig del personaje (E15, E16, E18, E20) y no la armadura BoneRoot de PJ01: avatar y escala del personaje.</summary>
        public bool RigPropio;
        /// <summary>Último cuadro del FBX según la bitácora (el primero es 1). 0 = no se fija.</summary>
        public int Ultimo;
        public bool Bucle;
        public RaizClipT1560 Raiz;
    }

    /// <summary>
    /// T-1560 (decisión de Tony 21-nov, "en Unity se ven deformadas"): tabla de decisiones de importación de los clips PJ01_*, NPC_*, E15_*, E16_*, E18_* y E20_*. Es lógica pura (sin AssetDatabase):
    /// la usan <see cref="AnimacionesT1560"/>, el postprocesador <see cref="ImportadorClipsT1560"/> (que la reaplica en cada reimportación) y las pruebas.
    /// </summary>
    public static class TablaClipsT1560
    {
        /// <summary>
        /// Escala de los clips cuyo FBX trae la armadura de PJ01: la del modelo PJ01.fbx (globalScale 1.88 y userData "escala=1.88"), que es la de todos los clips PJ01_* ya importados.
        /// El 1.8367 del documento de Tony es el factor de escala de la armadura en Blender (1.837 m de alto allí, 1.88 en Unity: ver AgarresArmas.EscalaBlenderAUnity): el avatar Humanoid
        /// normaliza el largo de piernas, así que no cambia la pose, pero con 1.88 el esqueleto del clip coincide con el de PJ01.
        /// </summary>
        public const float EscalaPJ01 = ImportadorClipsPJ01.EscalaClips;

        public const string CarpetaModeloE20 = "Assets/Arte/Personajes/E20_MatonSkulls/";
        public const string RutaModeloE20 = CarpetaModeloE20 + "E20_MatonSkulls_a.fbx";

        /// <summary>Prefijos de los clips que esta tarea importa, con su dueño.</summary>
        public static readonly (string Prefijo, string Duenio)[] Prefijos =
        {
            ("PJ01_", "PJ01"), ("NPC_", "PJ01"), ("E15_", "E15"), ("E16_", "E16"), ("E18_", "E18"), ("E20_", "E20"),
        };

        /// <summary>
        /// Los clips del 21-nov (nuevos o reemplazados) con su último cuadro según la bitácora y el FBX (24 fps los de Mixamo, 30 los de Blender; el primero siempre es 1). Incluye los rangos que
        /// pidió Tony: PJ01_Muerte_Frente 1-51, PJ01_Dano_Levantarse_BocaArriba 1-88, NPC_Balazo_Frente y Espalda 1-25, E15_Muerte y E16_Muerte 1-71, PJ01_Conductor_SerSacado 1-74 y
        /// PJ01_Carro_SacarConductor 1-98 (el .meta traía los de las versiones anteriores del FBX).
        /// </summary>
        public static readonly (string Nombre, int Ultimo)[] ClipsNuevos =
        {
            ("E15_Caida_Aire", 25), ("E15_Caida_Aterrizaje", 61), ("E15_Caida_Levantarse", 41), ("E15_Cojear", 50), ("E15_Herido_Idle", 281), ("E15_Muerte", 71),
            ("E16_Bloquear_Tambaleo", 41), ("E16_Caida_Aire", 25), ("E16_Caida_Aterrizaje", 71), ("E16_Caida_Levantarse", 49), ("E16_Cojear", 50), ("E16_Encenderse", 72),
            ("E16_Herido_Idle", 281), ("E16_Muerte", 71), ("E20_Golpe_Brazo", 34),
            ("NPC_Balazo_Espalda", 25), ("NPC_Balazo_Frente", 25), ("NPC_Caminar_Casual", 50), ("NPC_Caminar_Circulo", 522), ("NPC_Celular_Caminando", 1268),
            ("NPC_Dano_Derecha", 29), ("NPC_Dano_Izquierda", 29), ("NPC_Discutir_1", 625), ("NPC_Discutir_2", 625), ("NPC_Discutir_3", 625), ("NPC_Herido_PedirAyuda", 153),
            ("NPC_LlamarPolicia", 1166), ("NPC_Mensajear_Caminando_1", 100), ("NPC_Mensajear_Caminando_2", 121), ("NPC_Platicar_A", 179), ("NPC_Platicar_B", 119),
            ("NPC_Platicar_C", 114), ("NPC_Platicar_D", 309), ("NPC_SacadoDelCarro_Huir", 174), ("NPC_Senalar", 109), ("NPC_Senalar_2", 84), ("NPC_Teclear", 495),
            ("NPC_Trabajar_Aparato", 194), ("NPC_Voltear_Atras", 122),
            ("PJ01_Arrestado_Arrodillarse", 325), ("PJ01_Beber", 178), ("PJ01_Burla", 50), ("PJ01_Caerse", 69), ("PJ01_Camilla_Acostarse", 173), ("PJ01_Carro_SacarConductor", 98),
            ("PJ01_Colgado_Idle", 142), ("PJ01_Colgado_Subir", 116), ("PJ01_Conductor_SerSacado", 74), ("PJ01_Dano_Empujon", 26), ("PJ01_Dano_ExplosionCercana", 30),
            ("PJ01_Dano_Levantarse_BocaArriba", 88), ("PJ01_Derribo_Explosion", 40), ("PJ01_Escalera_SubirMixamo", 61), ("PJ01_Golpe_Der", 49), ("PJ01_Golpe_Izq", 44),
            ("PJ01_Golpe_Protesis", 49), ("PJ01_Granada_Agachado", 106), ("PJ01_Granada_DePie", 97), ("PJ01_Herido_Idle", 281), ("PJ01_Lanzar", 67),
            ("PJ01_Muerte_Atropello", 54), ("PJ01_Muerte_CaidaAltura", 46), ("PJ01_Muerte_Explosion", 56), ("PJ01_Muerte_Frente", 51),
            ("PJ01_Pandillero_Burla1", 50), ("PJ01_Pandillero_Burla2", 156), ("PJ01_Pandillero_BurlaGesto", 110), ("PJ01_Pandillero_Empujar", 23), ("PJ01_Pandillero_Gritar", 236),
            ("PJ01_Pandillero_HeridoArrastrandose", 55), ("PJ01_Pandillero_Patada", 61), ("PJ01_Pandillero_SacarArma", 99), ("PJ01_Parkour_SaltarBardaMixamo", 48),
            ("PJ01_Presumir", 141), ("PJ01_Recargar_A", 124), ("PJ01_Recargar_B", 100), ("PJ01_Rendirse", 144), ("PJ01_Rifle_CaminarMixamo", 34), ("PJ01_Rifle_Guardar", 64),
            ("PJ01_Rifle_Sacar", 62), ("PJ01_Salto_AdelanteMixamo", 29), ("PJ01_Salto_B_Mixamo", 31), ("PJ01_Senalar", 70), ("PJ01_Sentado", 90),
        };

        /// <summary>
        /// Clips cuyo FBX trae el esqueleto del propio personaje (mixamorig:*, con su malla) y no la armadura de PJ01 (BoneRoot). Los de E15 y E16 no traen el hueso Head (el avatar mapea la cabeza al cuello, ver
        /// ClipsRobotsNovaT1553.DescripcionSinCabeza). Los cuatro últimos son los de T-1553, que ya estaban bien y no se reconfiguran.
        /// </summary>
        public static readonly string[] ClipsConRigPropio =
        {
            "E15_Caida_Aire", "E15_Caida_Aterrizaje", "E15_Caida_Levantarse", "E15_Cojear", "E15_Herido_Idle", "E15_Muerte",
            "E16_Bloquear_Tambaleo", "E16_Caida_Aire", "E16_Caida_Aterrizaje", "E16_Caida_Levantarse", "E16_Cojear", "E16_Encenderse", "E16_Herido_Idle", "E16_Muerte",
            "E20_Golpe_Brazo",
            "E16_CaminarEscudo", "E18_Caminar", "E18_Correr", "E18_Golpe_Der",
        };

        /// <summary>
        /// Ciclos del 21-nov (loopTime): quietos y animaciones que se repiten hasta que alguien las quite. El resto de los clips nuevos va una sola vez (gestos, reacciones, muertes, caídas, golpes,
        /// subir y bajar). Los clips aprobados antes del 21-nov conservan su bucle.
        /// </summary>
        public static readonly string[] Ciclos =
        {
            "NPC_Platicar_A", "NPC_Platicar_B", "NPC_Platicar_C", "NPC_Platicar_D", "NPC_Discutir_1", "NPC_Discutir_2", "NPC_Discutir_3", "NPC_Celular_Caminando",
            "NPC_Mensajear_Caminando_1", "NPC_Mensajear_Caminando_2", "NPC_Caminar_Casual", "NPC_Caminar_Circulo", "NPC_Teclear", "NPC_Trabajar_Aparato", "NPC_LlamarPolicia",
            "NPC_Herido_PedirAyuda", "PJ01_Herido_Idle", "PJ01_Sentado", "PJ01_Colgado_Idle", "PJ01_Escalera_SubirMixamo", "PJ01_Pandillero_HeridoArrastrandose", "PJ01_Rifle_CaminarMixamo",
            "E15_Herido_Idle", "E16_Herido_Idle", "E15_Cojear", "E16_Cojear",
        };

        /// <summary>
        /// Clips nuevos en el sitio: la cadera se desplaza menos de 0.05 m en total y menos de 0.40 (unidades de la armadura) del primer cuadro, medido en el FBX. Se hornean rotación, altura y XZ.
        /// Los que no están aquí ni en las listas de abajo avanzan (<see cref="RaizClipT1560.Avanza"/>).
        /// </summary>
        public static readonly string[] ClipsEnElSitio =
        {
            "E15_Caida_Aire", "E15_Herido_Idle", "E16_Bloquear_Tambaleo", "E16_Caida_Aire", "E16_Encenderse", "E16_Herido_Idle", "E20_Golpe_Brazo",
            "NPC_Balazo_Espalda", "NPC_Balazo_Frente", "NPC_Dano_Derecha", "NPC_Dano_Izquierda", "NPC_Discutir_1", "NPC_Discutir_2", "NPC_Discutir_3", "NPC_Herido_PedirAyuda",
            "NPC_LlamarPolicia", "NPC_Platicar_A", "NPC_Platicar_B", "NPC_Platicar_C", "NPC_Platicar_D", "NPC_Senalar", "NPC_Senalar_2", "NPC_Teclear", "NPC_Trabajar_Aparato",
            "NPC_Voltear_Atras", "PJ01_Arrestado_Arrodillarse", "PJ01_Beber", "PJ01_Burla", "PJ01_Colgado_Idle", "PJ01_Golpe_Der", "PJ01_Golpe_Izq", "PJ01_Golpe_Protesis",
            "PJ01_Granada_Agachado", "PJ01_Granada_DePie", "PJ01_Herido_Idle", "PJ01_Lanzar", "PJ01_Pandillero_Burla1", "PJ01_Pandillero_Burla2", "PJ01_Pandillero_BurlaGesto",
            "PJ01_Pandillero_Empujar", "PJ01_Pandillero_Gritar", "PJ01_Pandillero_Patada", "PJ01_Pandillero_SacarArma", "PJ01_Presumir", "PJ01_Recargar_B", "PJ01_Rendirse",
            "PJ01_Rifle_Guardar", "PJ01_Rifle_Sacar", "PJ01_Senalar", "PJ01_Sentado",
        };

        /// <summary>Sube la escalera de mano (1.15 m por ciclo de root motion en Y; la cápsula sube con AnimacionEscalera): la altura va al root motion.</summary>
        public const string ClipEscaleraSubir = "PJ01_Escalera_SubirMixamo";
        /// <summary>Se cuelga de la cornisa y sube a ella: altura y XZ al root motion.</summary>
        public const string ClipColgadoSubir = "PJ01_Colgado_Subir";
        /// <summary>Camina en círculo: sin hornear nada.</summary>
        public const string ClipCaminarCirculo = "NPC_Caminar_Circulo";

        private static Dictionary<string, int> ultimoPorClip;

        private static Dictionary<string, int> UltimoPorClip
        {
            get
            {
                if (ultimoPorClip == null)
                {
                    ultimoPorClip = ClipsNuevos.ToDictionary(c => c.Nombre, c => c.Ultimo);
                }
                return ultimoPorClip;
            }
        }

        /// <summary>El dueño del clip por su prefijo (PJ01, E15, E16, E18 o E20); null si el nombre no es de esta tarea.</summary>
        public static string DuenioDe(string nombre)
        {
            foreach ((string prefijo, string duenio) in Prefijos)
            {
                if (nombre != null && nombre.StartsWith(prefijo))
                {
                    return duenio;
                }
            }
            return null;
        }

        /// <summary>La decisión de importación del clip, o null si no es de esta tarea.</summary>
        public static DecisionClipT1560 Decidir(string nombre)
        {
            string duenio = DuenioDe(nombre);
            if (duenio == null)
            {
                return null;
            }
            bool rigPropio = System.Array.IndexOf(ClipsConRigPropio, nombre) >= 0;
            bool nuevo = UltimoPorClip.TryGetValue(nombre, out int ultimo);
            var d = new DecisionClipT1560 { Nombre = nombre, Duenio = duenio, Nuevo = nuevo, RigPropio = rigPropio, Ultimo = ultimo, Raiz = RaizClipT1560.SinCambio };
            if (!nuevo)
            {
                return d;
            }
            d.Bucle = System.Array.IndexOf(Ciclos, nombre) >= 0;
            if (nombre == ClipEscaleraSubir)
            {
                d.Raiz = RaizClipT1560.Sube;
            }
            else if (nombre == ClipColgadoSubir)
            {
                d.Raiz = RaizClipT1560.SubeYAvanza;
            }
            else if (nombre == ClipCaminarCirculo)
            {
                d.Raiz = RaizClipT1560.Gira;
            }
            else
            {
                d.Raiz = System.Array.IndexOf(ClipsEnElSitio, nombre) >= 0 ? RaizClipT1560.Sitio : RaizClipT1560.Avanza;
            }
            return d;
        }

        /// <summary>Ruta del modelo del dueño (E15, E16, E18 o E20) cuyo avatar y escala usan los clips con rig propio; PJ01 para los demás.</summary>
        public static string RutaModeloDe(string duenio)
        {
            switch (duenio)
            {
                case "E15": return ClipsRobotsNovaT1553.RutaModeloE15;
                case "E16": return ClipsRobotsNovaT1553.RutaModeloE16;
                case "E18": return ClipsRobotsNovaT1553.RutaModeloE18;
                case "E20": return RutaModeloE20;
                default: return ImportadorClipsPJ01.RutaModeloOrigen;
            }
        }

        /// <summary>Ruta del FBX del clip (las carpetas de E16 y E18 para los clips que pasaron a ellas, la de PJ01 para los demás).</summary>
        public static string RutaClip(string nombre) => ClipsRobotsNovaT1553.RutaClip(nombre);

        /// <summary>Los nombres de todos los clips de esta tarea que hay en disco (PJ01/Clips y las carpetas Clips de E16 y E18), ordenados.</summary>
        public static string[] ClipsEnDisco()
        {
            var nombres = new SortedSet<string>();
            foreach (string carpeta in ClipsRobotsNovaT1553.CarpetasDeClips())
            {
                if (!Directory.Exists(carpeta))
                {
                    continue;
                }
                foreach (string archivo in Directory.GetFiles(carpeta, "*.fbx"))
                {
                    string nombre = Path.GetFileNameWithoutExtension(archivo);
                    if (DuenioDe(nombre) != null)
                    {
                        nombres.Add(nombre);
                    }
                }
            }
            return nombres.ToArray();
        }

        /// <summary>True si la ruta es el FBX de un clip de esta tarea (para el postprocesador).</summary>
        public static bool EsClipDeLaTabla(string ruta)
        {
            if (ruta == null || !ruta.EndsWith(".fbx"))
            {
                return false;
            }
            bool enCarpeta = ruta.StartsWith(ImportadorClipsPJ01.Carpeta) || ClipsRobotsNovaT1553.EsClipDeRobot(ruta);
            return enCarpeta && DuenioDe(Path.GetFileNameWithoutExtension(ruta)) != null;
        }
    }
}
