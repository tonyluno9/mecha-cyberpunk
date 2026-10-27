using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-312: guarda V1 a V3 a las 12:00 y 22:00 y mide el tiempo de fotograma (400 fotogramas por vista). Se lanza a mano con -testFilter MedirFotogramaAsfalto; T312_PREFIJO nombra la corrida.</summary>
    public class MedirFotogramaAsfalto
    {
        [UnityTest, Explicit("Herramienta de medición de T-312 (capturas y tiempo de fotograma en Logs/t312/); no es una prueba.")]
        public IEnumerator Medir()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Pruebas_Alan.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            reloj.Pausar(true);
            string prefijo = System.Environment.GetEnvironmentVariable("T312_PREFIJO") ?? "t312";
            Directory.CreateDirectory("Logs/t312");
            List<string> lineas = new List<string>();
            foreach (float hora in new[] { 12f, 22f })
            {
                for (int v = 1; v <= 3; v++)
                {
                    VistasReferencia.Ir(v, hora);
                    for (int i = 0; i < 30; i++) yield return null;
                    CapturaPruebas.Guardar(Camera.main != null ? Camera.main : Object.FindFirstObjectByType<CamaraLibre>().Camara, "Logs/t312/" + prefijo + "-V" + v + "_" + (int)hora + ".png");
                    List<float> t = new List<float>();
                    for (int i = 0; i < 400; i++) { yield return null; t.Add(Time.unscaledDeltaTime * 1000f); }
                    t.Sort();
                    float s = 0; foreach (float x in t) s += x;
                    lineas.Add("V" + v + "_" + (int)hora + " media_ms=" + (s / t.Count).ToString("F2") + " p50=" + t[200].ToString("F2") + " p95=" + t[380].ToString("F2"));
                }
            }
            File.WriteAllLines("Logs/t312/" + prefijo + "-fotograma.txt", lineas);
            foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(r);
        }
    }
}
