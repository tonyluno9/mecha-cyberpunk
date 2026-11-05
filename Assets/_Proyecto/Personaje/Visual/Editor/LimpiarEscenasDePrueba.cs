using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-653: el Test Framework crea Assets/InitTestScene&lt;guid&gt;.unity al iniciar PlayMode y la borra al terminar.
    /// Si la corrida se cae o se mata (timeout, taskkill), la escena se queda. Al cargar el editor se borran las viejas.
    /// </summary>
    [InitializeOnLoad]
    public static class LimpiarEscenasDePrueba
    {
        /// <summary>Edad mínima (horas) para considerar una escena de prueba como resto abandonado.</summary>
        public const double HorasMinimas = 1.0;

        static LimpiarEscenasDePrueba()
        {
            Limpiar(Application.dataPath, DateTime.UtcNow);
        }

        /// <summary>Borra las InitTestScene*.unity (y su .meta) de la carpeta con más de <see cref="HorasMinimas"/> horas. Devuelve cuántas.</summary>
        public static int Limpiar(string carpetaAssets, DateTime ahoraUtc)
        {
            int borradas = 0;
            if (!Directory.Exists(carpetaAssets)) return 0;
            foreach (string ruta in Directory.GetFiles(carpetaAssets, "InitTestScene*.unity", SearchOption.TopDirectoryOnly))
            {
                if ((ahoraUtc - File.GetLastWriteTimeUtc(ruta)).TotalHours < HorasMinimas) continue;
                try
                {
                    File.Delete(ruta);
                    if (File.Exists(ruta + ".meta")) File.Delete(ruta + ".meta");
                    borradas++;
                }
                catch (IOException) { /* en uso: se deja */ }
            }
            return borradas;
        }
    }
}
