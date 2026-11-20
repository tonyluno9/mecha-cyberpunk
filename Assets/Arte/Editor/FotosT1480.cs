using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1480: fotos de la escena de ahora y de la de antes de la expansión (copiada en Assets/_Temp_T1480/Ciudad_Antes.unity)
    /// para compararlas. Salida en docs/capturas/t-1480/. No guarda ninguna escena.
    /// </summary>
    public static class FotosT1480
    {
        private const string RutaAntes = "Assets/_Temp_T1480/Ciudad_Antes.unity";
        private const string Carpeta = "docs/capturas/t-1480/";
        private const string Lista = "Logs/t1480-fotos.txt";
        private const float HoraDia = 13f;

        private static readonly Vector2[] Salidas =
        {
            new Vector2(264, 0), new Vector2(-264, 0), new Vector2(-47, 216),
            new Vector2(47, 216), new Vector2(-47, -216), new Vector2(47, -216),
        };

        public static void FotosAhora()
        {
            ColocarS2.Abrir();
            Tomar("ahora");
        }

        public static void FotosAntes()
        {
            EditorSceneManager.OpenScene(RutaAntes, OpenSceneMode.Single);
            Tomar("antes");
        }

        private static void Tomar(string sufijo)
        {
            List<string> hechas = new List<string>();
            void F(string nombre, Vector3 desde, Vector3 hacia, float campo = 55f)
            {
                // Noche (hora por defecto) y día.
                string rn = Carpeta + nombre + "-" + sufijo + ".png";
                ColocarS2cLook.Foto(desde, hacia, rn, campo);
                hechas.Add(rn);
                string rd = Carpeta + nombre + "-dia-" + sufijo + ".png";
                ColocarS2cLook.Foto(desde, hacia, rd, campo, HoraDia);
                hechas.Add(rd);
            }

            F("aerea-ciudad", new Vector3(0, 420, -260), Vector3.zero, 60f);
            F("aerea-sur", new Vector3(0, 160, -60), new Vector3(0, 0, -215));
            F("aerea-norte", new Vector3(0, 160, 60), new Vector3(0, 0, 215));
            F("aerea-este", new Vector3(60, 160, 0), new Vector3(265, 0, 0));
            F("aerea-oeste", new Vector3(-60, 160, 0), new Vector3(-265, 0, 0));
            for (int i = 0; i < Salidas.Length; i++)
            {
                Vector2 s = Salidas[i];
                Vector2 dir = s.normalized;
                Vector2 p = s - dir * 60f;
                F("calle-salida" + i, new Vector3(p.x, 1.7f, p.y), new Vector3(s.x, 6f, s.y));
            }

            Directory.CreateDirectory("Logs");
            File.AppendAllLines(Lista, hechas);
        }
    }
}
