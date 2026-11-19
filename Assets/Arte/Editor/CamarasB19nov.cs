using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión B, 19-nov: prueba cámaras candidatas en una sola corrida para elegir el encuadre a ojo.
    /// Lee Logs/camaras-b.txt, una por línea: nombre;referencia;x,y,z;x,y,z;campo (referencia sin .png, de imageassets/referencias/look/).
    /// Por cada línea deja el lado a lado en docs/capturas/_vista19b/prueba/nombre.png. No guarda nada en la escena ni en los prefabs.
    /// </summary>
    public static class CamarasB19nov
    {
        public static void Probar()
        {
            const string carpeta = "docs/capturas/_vista19b/prueba";
            Directory.CreateDirectory(carpeta);
            var log = new StringBuilder();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            foreach (string crudo in File.ReadAllLines("Logs/camaras-b.txt"))
            {
                string linea = crudo.Trim();
                if (linea.Length == 0 || linea.StartsWith("#"))
                {
                    continue;
                }
                try
                {
                    string[] p = linea.Split(';');
                    Vector3 desde = Vec(p[2]), hacia = Vec(p[3]);
                    float campo = float.Parse(p[4], CultureInfo.InvariantCulture);
                    string juego = "Logs/camb-" + p[0] + ".png";
                    ColocarS2cLook.Foto(desde, hacia, juego, campo);
                    ColocarS2.LadoALado("imageassets/referencias/look/" + p[1] + ".png", juego, carpeta + "/" + p[0] + ".png");
                    log.AppendLine("OK " + linea);
                }
                catch (Exception e)
                {
                    log.AppendLine("FALLO " + linea + " :: " + e.Message);
                }
            }
            File.WriteAllText("Logs/camaras-b-resultado.txt", log.ToString());
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Vector3 Vec(string s)
        {
            string[] c = s.Split(',');
            return new Vector3(float.Parse(c[0], CultureInfo.InvariantCulture), float.Parse(c[1], CultureInfo.InvariantCulture), float.Parse(c[2], CultureInfo.InvariantCulture));
        }
    }
}
