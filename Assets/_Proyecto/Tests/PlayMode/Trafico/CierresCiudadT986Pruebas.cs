using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
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
    /// T-986: en la ciudad real (Ciudad_Prologo, la de la demo) casi ningún tramo de la persecución queda cerrado: solo los que de verdad tienen una barda o un
    /// cierre de lado a lado. Mide cuántos tramos tiene el grafo, cuántos cerraba la primera versión de BloqueosDeCalles (dos carriles, cualquier sólido
    /// a lo largo de todo el tramo) y cuántos cierra la actual (cuatro carriles en una misma franja de 3 m), y deja la lista con el collider que cerró cada uno.
    /// </summary>
    public class CierresCiudadT986Pruebas
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int MaximoDeTramosCerrados = 12;
        private HashSet<int> raicesAntes;

        /// <summary>Los objetos de la raíz que hay ahora (la ciudad deja algunos persistentes al cargarse: visor, huecos de bala, modo video).</summary>
        public static HashSet<int> InstantaneaDeRaices()
        {
            return new HashSet<int>(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.parent == null).Select(t => t.gameObject.GetInstanceID()));
        }

        /// <summary>Quita los objetos de la raíz que no estaban en <paramref name="antes"/>: deja la sesión como estaba antes de cargar la ciudad.</summary>
        public static void QuitarRaicesNuevas(HashSet<int> antes)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.parent == null && !antes.Contains(t.gameObject.GetInstanceID())).ToArray())
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        /// <summary>El grafo de calles de la persecución de la ciudad, armado igual que GestorPersecucion (manzanas + afueras), sin cierres marcados.</summary>
        public static GrafoCalles GrafoDeLaCiudad(out Transform barrio)
        {
            GestorPersecucion gestor = Object.FindObjectsByType<GestorPersecucion>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            barrio = gestor != null ? (gestor.transform.parent != null ? gestor.transform.parent : gestor.transform) : null;
            GrafoCalles grafo = barrio != null ? GestorPersecucion.ArmarCallesDe(barrio) : null;
            if (grafo != null)
            {
                TraficoCalle.AlargarHaciaLasAfueras(grafo);
            }
            return grafo;
        }

        // La primera versión: dos carriles, cualquier sólido fijo en cada uno a lo largo de todo el tramo.
        private static bool Antes(Vector3 desde, Vector3 hasta, out string quien)
        {
            quien = "";
            Vector3 tramo = hasta - desde;
            tramo.y = 0f;
            float largo = tramo.magnitude;
            if (largo <= 2f)
            {
                return false;
            }
            Vector3 dir = tramo / largo;
            Vector3 derecha = Vector3.Cross(Vector3.up, dir);
            Collider a = Primero(desde, dir, derecha * 1.6f, largo);
            Collider b = a != null ? Primero(desde, dir, -derecha * 1.6f, largo) : null;
            quien = BloqueosDeCalles.Ruta(a) + " + " + BloqueosDeCalles.Ruta(b);
            return a != null && b != null;
        }

        private static Collider Primero(Vector3 desde, Vector3 dir, Vector3 lado, float largo)
        {
            RaycastHit[] golpes = Physics.SphereCastAll(desde + dir + lado + Vector3.up * 1.2f, 0.7f, dir, largo - 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit g in golpes.OrderBy(x => x.distance))
            {
                if (g.collider.attachedRigidbody == null && g.collider.GetComponentInParent<MechaCyberpunk.Trafico.Peatones.PeatonMundo>() == null)
                {
                    return g.collider;
                }
            }
            return null;
        }

        // La ciudad no se queda cargada para las pruebas que siguen: se cambia a una escena vacía y se descarga.
        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene ciudad = SceneManager.GetActiveScene();
            if (ciudad.path == RutaEscena)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("VaciaCierresT986"));
                yield return SceneManager.UnloadSceneAsync(ciudad);
                if (raicesAntes != null)
                {
                    QuitarRaicesNuevas(raicesAntes);
                }
            }
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator CiudadPrologo_CasiNingunTramoDeLaPersecucionQuedaCerrado()
        {
            Registros.LimpiarTodos();
            raicesAntes = InstantaneaDeRaices();
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }
            GrafoCalles grafo = GrafoDeLaCiudad(out Transform barrio);
            Assert.IsNotNull(grafo, "La ciudad tiene un GestorPersecucion con manzanas");
            Physics.SyncTransforms();
            int tramos = 0;
            int antes = 0;
            List<string> ahora = new List<string>();
            List<string> viejos = new List<string>();
            for (int a = 0; a < grafo.Cantidad; a++)
            {
                foreach (int b in grafo.Vecinos(a))
                {
                    if (b <= a)
                    {
                        continue;
                    }
                    tramos++;
                    if (Antes(grafo.Nodo(a), grafo.Nodo(b), out string quienAntes))
                    {
                        antes++;
                        viejos.Add("antes " + a + "-" + b + " " + grafo.Nodo(a) + " -> " + grafo.Nodo(b) + " " + quienAntes);
                    }
                    if (BloqueosDeCalles.Bloqueo(grafo.Nodo(a), grafo.Nodo(b), out Collider cierre, out float metros))
                    {
                        ahora.Add("ahora " + a + "-" + b + " " + grafo.Nodo(a) + " -> " + grafo.Nodo(b) + " a " + metros.ToString("F1") + " m: " + BloqueosDeCalles.Ruta(cierre));
                    }
                }
            }
            Transform[] finales = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("Final_") || t.name.StartsWith("Cierre_")).ToArray();
            Debug.Log("[T-986] cierres por nombre en la escena: " + string.Join("; ", finales.Select(t => t.name + " activo=" + t.gameObject.activeInHierarchy + " " + t.position)));
            int conUnCarrilTapado = 0;
            for (int a = 0; a < grafo.Cantidad; a++)
            {
                foreach (int b in grafo.Vecinos(a))
                {
                    if (b > a)
                    {
                        Vector3 d = grafo.Nodo(b) - grafo.Nodo(a);
                        d.y = 0f;
                        Vector3 dir = d.normalized;
                        conUnCarrilTapado += Primero(grafo.Nodo(a), dir, Vector3.Cross(Vector3.up, dir) * 1.6f, d.magnitude) != null ? 1 : 0;
                    }
                }
            }
            Debug.Log("[T-986] tramos con el carril derecho tapado por algo sólido: " + conUnCarrilTapado + "; colliders en escena " + Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length);
            Debug.Log("[T-986] ciudad: " + grafo.Cantidad + " nodos, " + tramos + " tramos; cierra antes " + antes + ", ahora " + ahora.Count + " (barrio " + barrio.name + ")");
            foreach (string l in viejos.Take(25))
            {
                Debug.Log("[T-986] " + l);
            }
            foreach (string l in ahora)
            {
                Debug.Log("[T-986] " + l);
            }
            Assert.LessOrEqual(ahora.Count, MaximoDeTramosCerrados, "En la ciudad abierta solo cierran los tramos con barda o cierre de lado a lado");
            Assert.Less(ahora.Count, tramos / 4, "Y son una parte chica del grafo");
        }
    }
}
