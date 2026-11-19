using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1370: último paso de los arreglos del Cerro y del Puerto. Busca con las mismas reglas de las pruebas de la ciudad
    /// (<see cref="FlotantesS2t.Buscar"/> y <see cref="RevisarCiudadT995.Encimados"/>) lo que flota o se encima dentro de las zonas
    /// T1355_Cerro y T1357_Puerto, abriendo la escena de la ciudad solo para leer (no se guarda), y lo arregla en el contenido de los
    /// prefabs (los mismos objetos: no se recrea nada, para no romper las referencias de la escena):
    /// lo que flota hasta <see cref="FlotantesS2t.BajadaMaxima"/> m se baja al apoyo; lo que flota sin nada abajo y lo chico que queda
    /// encimado se desactiva (no se borra). Repite hasta que no quede nada o hasta <see cref="Vueltas"/> vueltas. Idempotente.
    /// Informe en Logs/t1370-asentar.txt. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.AsentarZonasT1370.Aplicar
    /// </summary>
    public static class AsentarZonasT1370
    {
        public static readonly string[] Zonas = { "T1355_Cerro", "T1357_Puerto" };
        public const string CarpetaZonas = "Assets/Arte/Mundo/Zonas/";
        public const int Vueltas = 3;

        private sealed class Arreglo
        {
            public string Zona;
            public int[] Ruta;
            public string Nombre;
            public float Bajar;
            public bool Apagar;
            public string Motivo;
        }

        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder("T-1370: flotantes y encimados de las zonas del Cerro y del Puerto\n");
            for (int vuelta = 1; vuelta <= Vueltas; vuelta++)
            {
                List<Arreglo> arreglos = Buscar();
                informe.AppendLine("Vuelta " + vuelta + ": " + arreglos.Count + " por arreglar");
                if (arreglos.Count == 0)
                {
                    break;
                }
                foreach (IGrouping<string, Arreglo> zona in arreglos.GroupBy(a => a.Zona))
                {
                    Aplicar(zona.Key, zona.ToList(), informe);
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1370-asentar.txt", informe.ToString());
            Debug.Log("[Arte] " + informe);
        }

        // Lo que marcan las pruebas, solo lo que está dentro de una de las dos zonas, con su ruta de índices desde la raíz de la zona.
        private static List<Arreglo> Buscar()
        {
            List<Arreglo> salida = new List<Arreglo>();
            HashSet<Transform> vistos = new HashSet<Transform>();
            foreach (var (pieza, _, hueco) in FlotantesS2t.Buscar())
            {
                Arreglo a = Nuevo(pieza, vistos);
                if (a == null)
                {
                    continue;
                }
                bool bajar = !float.IsInfinity(hueco) && hueco <= FlotantesS2t.BajadaMaxima;
                a.Bajar = bajar ? hueco : 0f;
                a.Apagar = !bajar;
                a.Motivo = bajar ? "flota " + hueco.ToString("F2") + " m: se baja al apoyo" : "flota sin apoyo: se desactiva";
                salida.Add(a);
            }
            foreach (RevisarCiudadT995.Encimado e in RevisarCiudadT995.Encimados())
            {
                // Se desactiva el más chico de los dos (el adorno), nunca una casa, bodega, contenedor o grúa.
                Transform chico = Volumen(e.A) <= Volumen(e.B) ? e.A : e.B;
                if (e.B == null || Volumen(chico) > MaximoAdorno)
                {
                    chico = e.A;
                }
                if (chico == null || Volumen(chico) > MaximoAdorno)
                {
                    continue;
                }
                Arreglo a = Nuevo(chico, vistos);
                if (a == null)
                {
                    continue;
                }
                a.Apagar = true;
                a.Motivo = e.Tipo + " " + e.Medida.ToString("F2") + " con " + (chico == e.A ? Nombre(e.B) : Nombre(e.A)) + ": se desactiva";
                salida.Add(a);
            }
            return salida;
        }

        /// <summary>Volumen (m³) máximo de lo que se puede desactivar por encimado: un adorno, no una construcción.</summary>
        private const float MaximoAdorno = 40f; // incluye montacargas (34 m³)

        private static float Volumen(Transform t)
        {
            if (t == null)
            {
                return float.MaxValue;
            }
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return 0f;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b.size.x * b.size.y * b.size.z;
        }

        private static string Nombre(Transform t) => t == null ? "?" : t.name;

        private static Arreglo Nuevo(Transform pieza, HashSet<Transform> vistos)
        {
            if (pieza == null || !vistos.Add(pieza))
            {
                return null;
            }
            Transform raiz = pieza;
            while (raiz.parent != null)
            {
                raiz = raiz.parent;
            }
            Transform zona = null;
            for (Transform t = pieza; t != null; t = t.parent)
            {
                if (Zonas.Contains(t.name))
                {
                    zona = t;
                }
            }
            if (zona == null || zona == pieza)
            {
                return null;
            }
            List<int> ruta = new List<int>();
            for (Transform t = pieza; t != zona; t = t.parent)
            {
                ruta.Insert(0, t.GetSiblingIndex());
            }
            return new Arreglo { Zona = zona.name, Ruta = ruta.ToArray(), Nombre = pieza.name };
        }

        private static void Aplicar(string zona, List<Arreglo> arreglos, StringBuilder informe)
        {
            string ruta = CarpetaZonas + zona + ".prefab";
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                foreach (Arreglo a in arreglos)
                {
                    Transform t = raiz.transform;
                    foreach (int i in a.Ruta)
                    {
                        t = i < t.childCount ? t.GetChild(i) : null;
                        if (t == null)
                        {
                            break;
                        }
                    }
                    if (t == null || t.name != a.Nombre)
                    {
                        informe.AppendLine("  NO COINCIDE la ruta de " + a.Nombre + " en " + zona + ": sin cambio");
                        continue;
                    }
                    if (a.Apagar)
                    {
                        t.gameObject.SetActive(false);
                    }
                    else
                    {
                        t.position += Vector3.down * a.Bajar;
                    }
                    informe.AppendLine("  " + zona + "/" + RutaNombres(t, raiz.transform) + " en " + t.position.ToString("F1") + ": " + a.Motivo);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static string RutaNombres(Transform t, Transform raiz)
        {
            List<string> partes = new List<string>();
            for (Transform x = t; x != null && x != raiz; x = x.parent)
            {
                partes.Insert(0, x.name);
            }
            return string.Join("/", partes);
        }
    }
}
