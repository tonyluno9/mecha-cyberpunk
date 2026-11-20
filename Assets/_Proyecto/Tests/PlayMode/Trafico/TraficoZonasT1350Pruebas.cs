using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1350: con el grafo REAL de la ciudad (el que arman TraficoCalle y GestorPersecucion) las calles de las 4 zonas nuevas están unidas a la cuadrícula,
    /// no quedan nodos sueltos ni callejones sin salida, los carros de tráfico las pisan y el cierre de tramos no las tapa por barandales laterales.
    /// No usa al jugador ni la entrada.
    /// </summary>
    [Category("Zonas")]
    [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
    public class TraficoZonasT1350Pruebas
    {
        private const string RutaEscena = CierresCiudadT986Pruebas.RutaEscena;
        private const int CuadrosDeEspera = 30;
        private const float SegundosDeJuego = 60f;
        private const float DistanciaATramoDeZona = 8f;
        private const float DistanciaALimite = 15f;
        private const float DiferenciaDeAlturaMaxima = 4f;
        private static readonly string[] Zonas = { "Malecon", "Cerro", "Autopista", "Puerto" };
        private HashSet<int> raicesAntes;

        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene ciudad = SceneManager.GetActiveScene();
            if (ciudad.path == RutaEscena)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("VaciaZonasT1350"));
                yield return SceneManager.UnloadSceneAsync(ciudad);
                if (raicesAntes != null)
                {
                    CierresCiudadT986Pruebas.QuitarRaicesNuevas(raicesAntes);
                }
            }
            Registros.LimpiarTodos();
        }

        private IEnumerator CargarCiudad()
        {
            Registros.LimpiarTodos();
            raicesAntes = CierresCiudadT986Pruebas.InstantaneaDeRaices();
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < CuadrosDeEspera; i++)
            {
                yield return null;
            }
        }

        private static bool HayCamino(GrafoCalles grafo, int desde, Func<Vector3, bool> destino)
        {
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (destino(grafo.Nodo(i)) && grafo.Camino(desde, i).Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static float DistanciaPlanoASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pa = new Vector2(p.x - a.x, p.z - a.z);
            Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(pa, ab) / ab.sqrMagnitude) : 0f;
            return (pa - ab * t).magnitude;
        }

        [UnityTest]
        public IEnumerator Zonas_GrafoReal_EstanUnidasSinCallejones()
        {
            yield return CargarCiudad();
            foreach (string zona in Zonas)
            {
                Assert.IsTrue(CalleZona.Activas.Any(c => c.Zona == zona), "hay CalleZona activas de " + zona);
            }
            GrafoCalles grafo = CierresCiudadT986Pruebas.GrafoDeLaCiudad(out Transform barrio);
            Assert.IsNotNull(grafo, "la ciudad tiene un GestorPersecucion con manzanas");
            int cruce = grafo.MasCercano(new Vector3(0f, 0.2f, 0f));

            Assert.IsTrue(HayCamino(grafo, cruce, p => p.z < -220f), "camino del centro al Malecon (z < -220)");
            Assert.IsTrue(HayCamino(grafo, cruce, p => p.y > 9f), "camino del centro a la autopista (y > 9)");
            Assert.IsTrue(HayCamino(grafo, cruce, p => p.x > 150f && p.z < -60f), "camino del centro al Puerto");
            Assert.IsTrue(HayCamino(grafo, cruce, p => p.x < -300f && p.y > 5f), "camino del centro al Cerro (alto)");

            Collider[] limites = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.name == "LimiteInvisible").ToArray();
            List<string> sueltos = new List<string>();
            List<string> callejones = new List<string>();
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                int grado = grafo.Vecinos(i).Count;
                Vector3 n = grafo.Nodo(i);
                if (grado == 0 && grafo.EsDeZona(i))
                {
                    sueltos.Add(i + " " + n);
                }
                if (grado == 1 && !limites.Any(l => Vector3.Distance(l.ClosestPoint(n), n) < DistanciaALimite))
                {
                    callejones.Add(i + " " + n + (grafo.EsDeZona(i) ? " (zona)" : ""));
                }
            }
            Assert.IsEmpty(sueltos, "ningun nodo de zona sin vecinos: " + string.Join("; ", sueltos));
            Assert.IsEmpty(callejones, "ningun extremo sin salida lejos de un LimiteInvisible: " + string.Join("; ", callejones));
        }

        [UnityTest]
        public IEnumerator Zonas_GrafoReal_ElCierreDeTramosNoTapaLasCallesDeZona()
        {
            yield return CargarCiudad();
            GrafoCalles grafo = CierresCiudadT986Pruebas.GrafoDeLaCiudad(out _);
            Assert.IsNotNull(grafo);
            Physics.SyncTransforms();
            List<string> cerrados = new List<string>();
            for (int a = 0; a < grafo.Cantidad; a++)
            {
                foreach (int b in grafo.Vecinos(a))
                {
                    if (b > a && (grafo.EsDeZona(a) || grafo.EsDeZona(b)) && BloqueosDeCalles.Bloqueo(grafo.Nodo(a), grafo.Nodo(b), out Collider cierre, out float metros))
                    {
                        cerrados.Add(a + "-" + b + " " + grafo.Nodo(a) + " -> " + grafo.Nodo(b) + " a " + metros.ToString("F1") + " m: " + BloqueosDeCalles.Ruta(cierre));
                    }
                }
            }
            Assert.IsEmpty(cerrados, "ningun tramo de zona se cierra por barandales o parapetos laterales: " + string.Join("; ", cerrados));
        }

        [UnityTest]
        public IEnumerator Zonas_CarrosDeTrafico_PisanUnaCalleDeZonaEn60Segundos()
        {
            yield return CargarCiudad();
            List<(Vector3 a, Vector3 b)> tramos = new List<(Vector3, Vector3)>();
            foreach (CalleZona calle in CalleZona.Activas)
            {
                List<Vector3> puntos = calle.Puntos();
                for (int i = 1; i < puntos.Count; i++)
                {
                    tramos.Add((puntos[i - 1], puntos[i]));
                }
            }
            Assert.IsNotEmpty(tramos, "hay calles de zona");
            bool pisada = false;
            string quien = "";
            float fin = Time.time + SegundosDeJuego;
            while (Time.time < fin && !pisada)
            {
                foreach (CarroTrafico carro in CarroTrafico.Activos)
                {
                    if (carro == null)
                    {
                        continue;
                    }
                    Vector3 p = carro.transform.position;
                    foreach ((Vector3 a, Vector3 b) in tramos)
                    {
                        if (Mathf.Abs(p.y - Mathf.Lerp(a.y, b.y, 0.5f)) <= DiferenciaDeAlturaMaxima + Mathf.Abs(a.y - b.y) && DistanciaPlanoASegmento(p, a, b) < DistanciaATramoDeZona)
                        {
                            pisada = true;
                            quien = carro.name + " en " + p;
                            break;
                        }
                    }
                    if (pisada)
                    {
                        break;
                    }
                }
                yield return null;
            }
            Assert.IsTrue(pisada, "algun CarroTrafico pisa una calle de zona en " + SegundosDeJuego + " s (activos al final: " + CarroTrafico.Activos.Count + ")");
            Debug.Log("[T-1350] carro en zona: " + quien);
        }
    }
}
