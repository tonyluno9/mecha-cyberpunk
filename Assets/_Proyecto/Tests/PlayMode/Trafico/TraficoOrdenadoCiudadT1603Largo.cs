using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1603, comprobación larga en Ciudad_Prologo con el jugador quieto: 3 minutos de juego (a velocidad 3), choques y atorones entre carros, vivos cada 30 s.
    /// Escribe docs/capturas/_tareas/T13/registro.txt y dos capturas en alto. Es Explicit: solo corre si se pide por nombre.
    /// </summary>
    public class TraficoOrdenadoCiudadT1603Largo
    {
        private const float SegundosDeJuego = 180f;
        private const float Escala = 3f;
        private const string Carpeta = "docs/capturas/_tareas/T13/";

        [UnityTest, Explicit("Larga: 3 minutos de juego en Ciudad_Prologo")]
        public IEnumerator TresMinutosDeTrafico_SinChoquesNiAtorones()
        {
            LogAssert.ignoreFailingMessages = true;
            Registros.LimpiarTodos();
            EstadisticasTrafico.Reiniciar();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            TraficoCalle trafico = null;
            float limite = Time.realtimeSinceStartup + 60f;
            while ((trafico = Object.FindFirstObjectByType<TraficoCalle>()) == null || trafico.Carros.Count == 0 || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae el tráfico");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
            EstadisticasTrafico.Reiniciar();
            EstadisticasTrafico.RegistrarEnArchivo = true;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1603 tráfico ordenado, Ciudad_Prologo, jugador quieto, " + SegundosDeJuego + " s de juego (timeScale " + Escala + ")");
            sb.AppendLine("Hora: " + (reloj != null ? (reloj.EsDeNoche ? "noche" : "día") : "?") + "; carros del pool: " + trafico.Carros.Count);
            Time.timeScale = Escala;
            float inicio = Time.time;
            float proxima = inicio + 30f;
            float proximaCaptura = inicio + 60f;
            int capturas = 0;
            int maxVivos = 0;
            int minVivos = int.MaxValue;
            float realInicio = Time.realtimeSinceStartup;
            while (Time.time - inicio < SegundosDeJuego)
            {
                Assert.Less(Time.realtimeSinceStartup - realInicio, 500f, "Se pasó del tiempo real");
                int vivos = trafico.Vivos;
                maxVivos = Mathf.Max(maxVivos, vivos);
                minVivos = Mathf.Min(minVivos, vivos);
                if (Time.time >= proxima)
                {
                    sb.AppendLine("t=" + (Time.time - inicio).ToString("0") + " s: vivos " + vivos + ", choques " + EstadisticasTrafico.Choques + ", atorados " + EstadisticasTrafico.Atorados);
                    proxima += 30f;
                }
                if (Time.time >= proximaCaptura && capturas < 2)
                {
                    proximaCaptura += 60f;
                    capturas++;
                    yield return CapturarCruce(trafico, capturas);
                }
                yield return null;
            }
            Time.timeScale = 1f;
            sb.AppendLine("FIN: choques " + EstadisticasTrafico.Choques + ", atorados (>" + EstadisticasTrafico.SegundosAtorado + " s) " + EstadisticasTrafico.Atorados
                + ", vivos minimo " + minVivos + ", maximo " + maxVivos + ", segundos reales " + (Time.realtimeSinceStartup - realInicio).ToString("0"));
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + "registro.txt", sb.ToString());
            Debug.Log("[Prueba] T-1603 largo:\n" + sb);
            Assert.LessOrEqual(maxVivos, 20, "Nunca más de 20 vivos");
            Assert.AreEqual(0, EstadisticasTrafico.Atorados, "Ningún atorado de más de 8 s");
        }

        // Cámara en alto sobre el sitio de la ciudad donde más carros hay juntos (un cruce lleno), mirando hacia abajo.
        private static IEnumerator CapturarCruce(TraficoCalle trafico, int numero)
        {
            List<CarroTrafico> vivos = new List<CarroTrafico>();
            foreach (CarroTrafico c in trafico.Carros)
            {
                if (c != null && c.gameObject.activeSelf)
                {
                    vivos.Add(c);
                }
            }
            if (vivos.Count == 0)
            {
                yield break;
            }
            CarroTrafico centro = vivos[0];
            int mejor = -1;
            foreach (CarroTrafico a in vivos)
            {
                int cerca = 0;
                foreach (CarroTrafico b in vivos)
                {
                    cerca += Vector3.Distance(a.transform.position, b.transform.position) < 35f ? 1 : 0;
                }
                if (cerca > mejor)
                {
                    mejor = cerca;
                    centro = a;
                }
            }
            GameObject camObjeto = new GameObject("CamCapturaCruce");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.farClipPlane = 400f;
            camObjeto.transform.position = centro.transform.position + new Vector3(10f, 30f, -22f);
            camObjeto.transform.LookAt(centro.transform.position);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D img = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            img.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory(Carpeta);
            File.WriteAllBytes(Carpeta + "cruce-" + numero + ".png", img.EncodeToPNG());
            Debug.Log("[Prueba] captura cruce " + numero + " en " + centro.transform.position + " con " + mejor + " carros a 35 m");
            Object.Destroy(img);
            Object.Destroy(rt);
            Object.Destroy(camObjeto);
        }
    }
}
