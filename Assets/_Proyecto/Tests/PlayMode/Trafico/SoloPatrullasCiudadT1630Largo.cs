using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1630, comprobación larga en Ciudad_Prologo con el jugador quieto: 3 minutos de juego (a velocidad 3). Cuenta carros en movimiento por tipo (civiles del pool, patrullas, estacionados),
    /// choques entre carros del tráfico, paradas de patrullas en rojo y estacionados que se mueven; toma dos capturas de cruces con semáforo desde arriba.
    /// Escribe docs/capturas/_tareas/T16/registro.txt. Es Explicit: solo corre si se pide por nombre.
    /// </summary>
    public class SoloPatrullasCiudadT1630Largo
    {
        private const float SegundosDeJuego = 180f;
        private const float Escala = 3f;
        private const float SegundosEntreMuestras = 0.5f;
        private const float SegundosEntreLineas = 30f;
        private const float SegundosPrimeraCaptura = 50f;
        private const float SegundosEntreCapturas = 70f;
        private const float VelocidadEnMovimiento = 1f;
        private const float VelocidadParada = 0.3f;
        private const float VelocidadEstacionadoMovido = 0.5f;
        private const int MaximoDePatrullas = 4;
        private const float TopeSegundosReales = 600f;
        private const int Capturas = 2;
        private const string Carpeta = "docs/capturas/_tareas/T16/";

        [UnityTest, Explicit("Larga: 3 minutos de juego en Ciudad_Prologo")]
        public IEnumerator TresMinutos_SoloPatrullasYSemaforos_SinChoques()
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
            CarroEstacionado[] estacionados = Object.FindObjectsByType<CarroEstacionado>(FindObjectsSortMode.None);
            Vector3[] posiciones = new Vector3[estacionados.Length];
            for (int i = 0; i < estacionados.Length; i++)
            {
                posiciones[i] = estacionados[i].transform.position;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1630 solo patrullas, Ciudad_Prologo, jugador quieto, " + SegundosDeJuego + " s de juego (timeScale " + Escala + ")");
            sb.AppendLine("CirculanCiviles = " + trafico.CirculanCiviles + "; pool de " + trafico.Carros.Count + "; semáforos en la escena: " + RegistroSemaforos.Activos.Count + "; carros estacionados: " + estacionados.Length);
            Time.timeScale = Escala;
            float inicio = Time.time;
            float proximaMuestra = inicio;
            float proximaLinea = inicio + SegundosEntreLineas;
            float proximaCaptura = inicio + SegundosPrimeraCaptura;
            int capturas = 0;
            float realInicio = Time.realtimeSinceStartup;
            int muestras = 0;
            int sumaPatrullasMoviendose = 0;
            int maxPatrullas = 0;
            int maxCivilesMoviendose = 0;
            int maxEstacionadosMoviendose = 0;
            int paradasEnRojo = 0;
            int muestrasParadasEnRojo = 0;
            Dictionary<PatrullaMundo, bool> enRojo = new Dictionary<PatrullaMundo, bool>();
            while (Time.time - inicio < SegundosDeJuego)
            {
                Assert.Less(Time.realtimeSinceStartup - realInicio, TopeSegundosReales, "Se pasó del tiempo real");
                if (Time.time >= proximaMuestra)
                {
                    proximaMuestra += SegundosEntreMuestras;
                    muestras++;
                    int civiles = 0;
                    foreach (CarroTrafico c in CarroTrafico.Activos)
                    {
                        civiles += c != null && c.Velocidad > VelocidadEnMovimiento ? 1 : 0;
                    }
                    maxCivilesMoviendose = Mathf.Max(maxCivilesMoviendose, civiles);
                    int patrullas = 0;
                    int patrullasMoviendose = 0;
                    foreach (PatrullaMundo p in PatrullaMundo.Activas)
                    {
                        if (p == null)
                        {
                            continue;
                        }
                        patrullas++;
                        patrullasMoviendose += p.Rapidez > VelocidadEnMovimiento ? 1 : 0;
                        bool paradaEnRojo = p.EnRonda && p.Rapidez < VelocidadParada
                            && RegistroSemaforos.Consultar(p.transform.position, p.transform.forward, out FaseCruce fase, out float metros)
                            && fase != FaseCruce.Verde && metros > -CalculoCruceSemaforo.ToleranciaLinea && metros < 6f;
                        enRojo.TryGetValue(p, out bool antes);
                        paradasEnRojo += paradaEnRojo && !antes ? 1 : 0;
                        muestrasParadasEnRojo += paradaEnRojo ? 1 : 0;
                        enRojo[p] = paradaEnRojo;
                    }
                    maxPatrullas = Mathf.Max(maxPatrullas, patrullas);
                    sumaPatrullasMoviendose += patrullasMoviendose;
                    int movidos = 0;
                    foreach (CarroEstacionado e in estacionados)
                    {
                        Rigidbody cuerpo = e != null ? e.GetComponent<Rigidbody>() : null;
                        movidos += cuerpo != null && !cuerpo.isKinematic && cuerpo.linearVelocity.magnitude > VelocidadEstacionadoMovido ? 1 : 0;
                    }
                    maxEstacionadosMoviendose = Mathf.Max(maxEstacionadosMoviendose, movidos);
                }
                if (Time.time >= proximaLinea)
                {
                    sb.AppendLine("t=" + (Time.time - inicio).ToString("0") + " s: patrullas " + PatrullaMundo.Activas.Count + ", choques entre carros del tráfico " + EstadisticasTrafico.Choques
                        + ", paradas en rojo hasta ahora " + paradasEnRojo);
                    proximaLinea += SegundosEntreLineas;
                }
                if (Time.time >= proximaCaptura && capturas < Capturas)
                {
                    proximaCaptura += SegundosEntreCapturas;
                    capturas++;
                    yield return CapturarCruce(capturas);
                }
                yield return null;
            }
            Time.timeScale = 1f;
            int desplazados = 0;
            for (int i = 0; i < estacionados.Length; i++)
            {
                desplazados += estacionados[i] != null && Vector3.Distance(posiciones[i], estacionados[i].transform.position) > 0.5f ? 1 : 0;
            }
            sb.AppendLine("FIN: carros civiles del pool en movimiento (máximo a la vez) " + maxCivilesMoviendose + ", patrullas vivas (máximo) " + maxPatrullas
                + ", patrullas en movimiento (promedio por muestra) " + (muestras > 0 ? (sumaPatrullasMoviendose / (float)muestras).ToString("0.0") : "0")
                + ", estacionados en movimiento (máximo a la vez) " + maxEstacionadosMoviendose + ", estacionados que se desplazaron más de 0.5 m " + desplazados + " de " + estacionados.Length);
            sb.AppendLine("Choques entre carros del tráfico " + EstadisticasTrafico.Choques + ", atorados " + EstadisticasTrafico.Atorados + "; paradas de patrulla en rojo " + paradasEnRojo
                + " (" + (muestras > 0 ? (muestrasParadasEnRojo * SegundosEntreMuestras).ToString("0") : "0") + " s en total), segundos reales " + (Time.realtimeSinceStartup - realInicio).ToString("0"));
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + "registro.txt", sb.ToString());
            Debug.Log("[Prueba] T-1630 largo:\n" + sb);
            Assert.AreEqual(0, maxCivilesMoviendose, "Ningún civil del pool circula");
            Assert.AreEqual(0, EstadisticasTrafico.Choques, "0 choques entre carros del tráfico");
            Assert.LessOrEqual(maxPatrullas, MaximoDePatrullas, "A lo más 4 patrullas");
            Assert.AreEqual(0, desplazados, "Los estacionados se quedan donde están");
        }

        // Cámara en alto sobre un cruce con semáforo (el de las patrullas más cercano si hay), mirando hacia abajo.
        private static IEnumerator CapturarCruce(int numero)
        {
            IReadOnlyList<SemaforoCruce> cruces = RegistroSemaforos.Activos;
            if (cruces.Count == 0)
            {
                Debug.Log("[Prueba] T-1630: no hay semáforos para capturar.");
                yield break;
            }
            SemaforoCruce elegido = cruces[(numero - 1) % cruces.Count];
            float menor = float.MaxValue;
            foreach (SemaforoCruce c in cruces)
            {
                foreach (PatrullaMundo p in PatrullaMundo.Activas)
                {
                    float d = p != null ? Vector3.Distance(p.transform.position, c.transform.position) : float.MaxValue;
                    if (d < menor && (numero == 1 || c != cruces[0]))
                    {
                        menor = d;
                        elegido = c;
                    }
                }
            }
            GameObject camObjeto = new GameObject("CamCapturaSemaforo");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.farClipPlane = 400f;
            camObjeto.transform.position = elegido.transform.position + new Vector3(8f, 28f, -18f);
            camObjeto.transform.LookAt(elegido.transform.position);
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
            Debug.Log("[Prueba] captura " + numero + " del cruce en " + elegido.transform.position + ": eje X " + elegido.Fase(0) + ", eje Z " + elegido.Fase(1));
            Object.Destroy(img);
            Object.Destroy(rt);
            Object.Destroy(camObjeto);
        }
    }
}
