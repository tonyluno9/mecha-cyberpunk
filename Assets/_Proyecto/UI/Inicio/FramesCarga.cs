namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T100: los frames de las cinemáticas que salen en la pantalla de carga (pedido de Tony: solo estos, ya no L01-L06).
    /// Viven en Assets/Arte/UI/Carga/Frames/Resources/CargaFrames/ (1920×1080, escalados con Lanczos desde los PNG de
    /// imageassets/ui/carga/frames-cinematicas/). La lista es fija para cargar solo el frame elegido, no los 22 a la vez;
    /// una prueba EditMode comprueba que coincide con los PNG de la carpeta.
    /// </summary>
    public static class FramesCarga
    {
        /// <summary>Carpeta dentro de Resources (lo que recibe Resources.Load).</summary>
        public const string CarpetaRecursos = "CargaFrames";
        /// <summary>La carpeta de los PNG en el proyecto (para el importador y las pruebas).</summary>
        public const string CarpetaAssets = "Assets/Arte/UI/Carga/Frames/Resources/CargaFrames";

        /// <summary>Los nombres de los frames, sin extensión.</summary>
        public static readonly string[] Nombres =
        {
            "Carga_C04_F08_Se_escabulle",
            "Carga_C04_F09_Camara_huida",
            "Carga_C04_F10_Barril_vacio",
            "Carga_C04_F11_Lo_buscan",
            "Carga_C05_F12_Cuarto_llega",
            "Carga_C05_F12B_Maletin_en_mesa",
            "Carga_C05_F12C_Pulgares_huella",
            "Carga_C05_F13_Abre_maletin",
            "Carga_C05_F14_Contenido",
            "Carga_C05_F15_Ventana_Skulls",
            "Carga_C06_G01_Guardias_se_van",
            "Carga_C06_G02_Sale_de_casa",
            "Carga_C06_G03_En_la_ranura_voltea",
            "Carga_C07_G04_Lo_que_ve",
            "Carga_C07_G05_Duda",
            "Carga_C07_G06_Regresa",
            "Carga_C08_I01_Doc_prepara_suero",
            "Carga_C08_I02_Inyeccion",
            "Carga_C08_I03_Venas_cian",
            "Carga_C09_J01_Inventor_golpea_pistola",
            "Carga_C09_J02_Avienta_pistola",
            "Carga_C09_J03_Atrapa_pistola",
        };

        public static int Cantidad => Nombres.Length;

        /// <summary>La ruta en Resources del frame <paramref name="indice"/>.</summary>
        public static string Ruta(int indice) => CarpetaRecursos + "/" + Nombres[indice];
    }
}
