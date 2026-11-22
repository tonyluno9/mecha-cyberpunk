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
    /// T-1645: 3 minutos de juego (timeScale 3) en Ciudad_Prologo con el jugador: ningún vehículo (CarroViejo, CarroEstacionado, CarroTrafico, PatrullaMundo)
    /// activo fuera del muro ni bajo y=-5 de forma sostenida (4 muestras seguidas, 2 s: el vigilante corrige cada 1 s). Escribe docs/capturas/_tareas/T19/vehiculos-fuera.txt.
    /// Es Explicit: solo corre si se pide por nombre.
    /// </summary>
    public class VehiculosDentroDelMuroT1645Pruebas
    {
        private const float SegundosDeJuego = 180f;
        private const float Escala = 3f;
        private const float SegundosEntreMuestras = 0.5f;
        private const int MuestrasSeguidas = 4;
        private const float ToleranciaPrueba = 10f;
        private const float TopeSegundosReales = 600f;
        private const string Archivo = "docs/capturas/_tareas/T19/vehiculos-fuera.txt";

        [UnityTest, Explicit("Larga: 3 minutos de juego en Ciudad_Prologo")]
        public IEnumerator TresMinutos_NingunVehiculoFueraDelMuro()
        {
            LogAssert.ignoreFailingMessages = true;
            Registros.LimpiarTodos();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            float limite = Time.realtimeSinceStartup + 60f;
            while (Object.FindFirstObjectByType<TraficoCalle>() == null || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae el tráfico y el jugador");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
            Rect? area = AreaMuro.Actual();
            Assert.IsTrue(area.HasValue, "la escena tiene muro");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1645 vehículos fuera del muro, Ciudad_Prologo, " + SegundosDeJuego + " s de juego (timeScale " + Escala + "); área " + area.Value);
            GameObject robable17 = GameObject.Find("CarroRobable_17");
            sb.AppendLine("CarroRobable_17 " + (robable17 == null ? "apagado o ausente" : "activo en " + robable17.transform.position));
            Time.timeScale = Escala;
            float inicio = Time.time;
            float proxima = inicio;
            float realInicio = Time.realtimeSinceStartup;
            Dictionary<GameObject, int> racha = new Dictionary<GameObject, int>();
            Dictionary<GameObject, string> sostenidos = new Dictionary<GameObject, string>();
            int muestras = 0;
            int maxVehiculos = 0;
            while (Time.time - inicio < SegundosDeJuego)
            {
                Assert.Less(Time.realtimeSinceStartup - realInicio, TopeSegundosReales, "Se pasó del tiempo real");
                if (Time.time >= proxima)
                {
                    proxima += SegundosEntreMuestras;
                    muestras++;
                    HashSet<GameObject> vehiculos = new HashSet<GameObject>();
                    foreach (CarroViejo c in Object.FindObjectsByType<CarroViejo>(FindObjectsSortMode.None)) { vehiculos.Add(c.gameObject); }
                    foreach (CarroEstacionado c in Object.FindObjectsByType<CarroEstacionado>(FindObjectsSortMode.None)) { vehiculos.Add(c.gameObject); }
                    foreach (CarroTrafico c in Object.FindObjectsByType<CarroTrafico>(FindObjectsSortMode.None)) { vehiculos.Add(c.gameObject); }
                    foreach (PatrullaMundo p in Object.FindObjectsByType<PatrullaMundo>(FindObjectsSortMode.None)) { vehiculos.Add(p.gameObject); }
                    maxVehiculos = Mathf.Max(maxVehiculos, vehiculos.Count);
                    foreach (GameObject v in vehiculos)
                    {
                        Vector3 p = v.transform.position;
                        CarroViejo viejo = v.GetComponent<CarroViejo>();
                        bool ocupado = viejo != null && viejo.Ocupado;
                        bool mal = !ocupado && (p.y < -5f || AreaMuro.Fuera(p, ToleranciaPrueba));
                        racha.TryGetValue(v, out int n);
                        racha[v] = mal ? n + 1 : 0;
                        if (racha[v] >= MuestrasSeguidas)
                        {
                            sostenidos[v] = v.name + " en " + p + " (t=" + (Time.time - inicio).ToString("F0") + " s)";
                        }
                    }
                }
                yield return null;
            }
            Time.timeScale = 1f;
            sb.AppendLine("Muestras: " + muestras + "; máximo de vehículos vistos: " + maxVehiculos + "; corregidos por el vigilante: " + VigilanteVehiculosMuro.Corregidos);
            sb.AppendLine("Vehículos fuera del muro o caídos de forma sostenida: " + sostenidos.Count);
            foreach (string s in sostenidos.Values)
            {
                sb.AppendLine("  " + s);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Archivo));
            File.WriteAllText(Archivo, sb.ToString());
            Debug.Log(sb.ToString());
            Assert.AreEqual(0, sostenidos.Count, string.Join("; ", sostenidos.Values));
        }
    }
}
