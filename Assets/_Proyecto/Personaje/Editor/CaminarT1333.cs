using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1333: arregla el caminar del jugador a pie. Reaplica a los cuatro clips de locomoción de PJ01 los ajustes del importador (bucle, raíz) por si algún importador los dejó distintos,
    /// mide la rapidez real con que avanzan y la deja en los visuales del jugador (Visual_woman y Visual_PJ01, que traían 0.997 y 2.51 de los clips viejos de Tony).
    /// En juego, AnimacionJugador.ClipsDeTony ya no apaga el ajuste de rapidez. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CaminarT1333.Aplicar
    /// </summary>
    public static class CaminarT1333
    {
        public static readonly string[] ClipsLocomocion = { "PJ01_Caminar_SinImplante", "PJ01_Caminar_ConImplante", "PJ01_Correr_SinImplante", "PJ01_Correr_ConImplante" };
        public static readonly string[] VisualesDelJugador = { "woman", "PJ01" };

        [MenuItem("Mecha Cyberpunk/T-1333/Arreglar caminar del jugador")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            foreach (string nombre in ClipsLocomocion)
            {
                string ruta = ImportadorClipsPJ01.Carpeta + nombre + ".fbx";
                ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null)
                {
                    Debug.LogError("[T-1333] No existe el clip " + ruta);
                    continue;
                }
                ImportadorClipsPJ01.ConfigurarModelo(importador);
                importador.SaveAndReimport();
                ImportadorClipsPJ01.CopiarDescripcion(importador);
                ImportadorClipsPJ01.ConfigurarClip(importador, ruta);
                importador.SaveAndReimport();
            }

            float caminar = MedicionClips.Medir("Loco_Caminar_SinImplante");
            float correr = MedicionClips.Medir("Loco_Correr_SinImplante");
            Debug.Log("[T-1333] medido: caminar " + caminar.ToString("F3") + " m/s (constante " + ReglasAnimacion.VelocidadClipCaminarSinImplante + "), correr "
                + correr.ToString("F3") + " m/s (constante " + ReglasAnimacion.VelocidadClipCorrerSinImplante + "). Si difieren más de 10 %, actualizar las constantes de ReglasAnimacion.");
            Debug.Log("[T-1333] con el jugador a " + ReglasAnimacion.VelocidadAndar + " y " + ReglasAnimacion.VelocidadCarrera + " m/s la rapidez del clip sería "
                + (ReglasAnimacion.VelocidadAndar / caminar).ToString("F2") + "x al caminar (tope " + ReglasAnimacion.RapidezMaximaCaminar + ") y "
                + (ReglasAnimacion.VelocidadCarrera / correr).ToString("F2") + "x al correr (tope " + ReglasAnimacion.RapidezMaximaLocomocion + ")");

            foreach (string modelo in VisualesDelJugador)
            {
                string rutaPrefab = CreadorVisualPersonaje.RutaPrefab(modelo);
                GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
                try
                {
                    contenido.GetComponent<VisualPersonaje>().ConfigurarClips(caminar, correr, ReglasAnimacion.RapidezMaximaLocomocion);
                    PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
                Debug.Log("[T-1333] Visual_" + modelo + ": velocidadClipCaminar " + caminar.ToString("F3") + ", velocidadClipCorrer " + correr.ToString("F3"));
            }
            AssetDatabase.SaveAssets();
        }
    }
}
