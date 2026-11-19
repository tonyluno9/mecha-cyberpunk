using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// 19-nov (sesión A): las 9 comparaciones contra las referencias de look, lado a lado (referencia a la izquierda), en docs/capturas/_vista19b/.
    /// Cada toma busca primero una marca en la escena (o en los prefabs de zona colocados): un GameObject llamado "Vista19b_&lt;TOMA&gt;"
    /// (por ejemplo Vista19b_K11) con un hijo "Mira" y, opcional, un hijo "Campo_&lt;grados&gt;". Si no hay marca, usa la cámara anterior.
    /// Así una tarea de zona fija su cámara igual a la referencia sin tocar este archivo.
    /// Batchmode (corrida aparte de los Aplicar): -executeMethod MechaCyberpunk.Arte.Editor.Vista19b.Fotos [-tomas K11,A11] [-ronda r1]
    /// Escribe Logs/vista19b.txt con la cámara usada en cada toma.
    /// </summary>
    public static class Vista19b
    {
        public const string Carpeta = "docs/capturas/_vista19b/";
        public const string Referencias = "imageassets/referencias/look/";
        public const string PrefijoMarca = "Vista19b_";

        private struct Toma
        {
            public string Clave, Archivo, Referencia;
            public Vector3 Desde, Hacia;
            public float Campo;
            public bool Aerea;
        }

        private static List<Toma> TomasBase()
        {
            var l = new List<Toma>
            {
                new Toma { Clave = "K11", Archivo = "malecon-k11", Referencia = "K11_Malecon", Desde = ZonaMaleconT1393.CamaraK11, Hacia = new Vector3(-59f, 7f, -221.7f), Campo = 60f },
                new Toma { Clave = "K12", Archivo = "malecon-k12", Referencia = "K12_Malecon_Muelle", Desde = ZonaMaleconT1413.CamaraK12, Hacia = ZonaMaleconT1413.MiraK12, Campo = ZonaMaleconT1413.CampoK12 },
                new Toma { Clave = "A11", Archivo = "malecon-a11", Referencia = "A11_Malecon_Aerea", Desde = ZonaMaleconT1413.CamaraA11, Hacia = ZonaMaleconT1413.MiraA11, Campo = ZonaMaleconT1413.CampoA11, Aerea = true },
                new Toma { Clave = "K13", Archivo = "cerro-k13", Referencia = "K13_ColoniaCerro", Desde = ZonaCerroT1416.K13bDesde, Hacia = ZonaCerroT1416.K13bHacia, Campo = ZonaCerroT1416.CampoFoto },
                new Toma { Clave = "A13", Archivo = "cerro-a13", Referencia = "A13_ColoniaCerro_Aerea", Desde = new Vector3(-388f, 66f, -28f), Hacia = new Vector3(-336f, 9f, 2f), Campo = 60f, Aerea = true },
                new Toma { Clave = "K15", Archivo = "puerto-k15", Referencia = "K15_Puerto", Desde = ZonaPuertoT1396.CamaraK15, Hacia = ZonaPuertoT1396.MiraK15, Campo = 55f },
                new Toma { Clave = "A15", Archivo = "puerto-a15", Referencia = "A15_Puerto_Aerea", Desde = new Vector3(326f, 66f, -60f), Hacia = new Vector3(240f, 0f, -205f), Campo = 62f, Aerea = true },
                new Toma { Clave = "K18", Archivo = "autopista-k18", Referencia = "K18_AutopistaElevada", Desde = new Vector3(-168f, 5.98f, 226f), Hacia = new Vector3(-197.92f, 4.22f, 227.3f), Campo = 70f },
                new Toma { Clave = "A18", Archivo = "autopista-a18", Referencia = "A18_Autopista_Aerea", Desde = new Vector3(-205f, 46f, 150f), Hacia = new Vector3(-335f, 12f, 238f), Campo = 62f, Aerea = true },
            };
            return l;
        }

        [MenuItem("Mecha Cyberpunk/Vista19b/Fotos de las 9 comparaciones")]
        public static void Fotos()
        {
            string[] args = Environment.GetCommandLineArgs();
            string filtro = Arg(args, "-tomas");
            string ronda = Arg(args, "-ronda");
            HashSet<string> solo = string.IsNullOrEmpty(filtro) ? null : new HashSet<string>(filtro.Split(',').Select(s => s.Trim().ToUpperInvariant()));

            ColocarS2.Abrir();
            Directory.CreateDirectory(Carpeta);
            Directory.CreateDirectory("Logs");
            Physics.SyncTransforms();
            var log = new StringBuilder("Vista19b " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + (string.IsNullOrEmpty(ronda) ? "" : " ronda " + ronda) + "\n");
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            float lejosAntes = ColocarS2cLook.LejosFoto;
            bool cieloAntes = ColocarS2cLook.CieloCompleto;
            ColocarS2cLook.LejosFoto = 1500f;
            ColocarS2cLook.CieloCompleto = true;
            try
            {
                foreach (Toma baseT in TomasBase())
                {
                    if (solo != null && !solo.Contains(baseT.Clave))
                    {
                        continue;
                    }
                    Toma t = baseT;
                    string origen = "constante";
                    Transform marca = todos.FirstOrDefault(x => x != null && x.name == PrefijoMarca + t.Clave);
                    if (marca == null && t.Aerea)
                    {
                        // Las aéreas de T-1414 dejaron su marca T1414_<Zona>/CamaraA.
                        string raiz = t.Clave == "A13" ? "T1414_Cerro" : t.Clave == "A15" ? "T1414_Puerto" : t.Clave == "A18" ? "T1414_Autopista" : null;
                        Transform r = raiz == null ? null : todos.FirstOrDefault(x => x != null && x.name == raiz);
                        Transform c = r != null ? r.Find(AereasT1414.NombreCamara) : null;
                        if (c != null && c.Find(AereasT1414.NombreMira) != null)
                        {
                            t.Desde = c.position;
                            t.Hacia = c.Find(AereasT1414.NombreMira).position;
                            origen = raiz + "/" + AereasT1414.NombreCamara;
                        }
                    }
                    if (marca != null && marca.Find("Mira") != null)
                    {
                        t.Desde = marca.position;
                        t.Hacia = marca.Find("Mira").position;
                        foreach (Transform h in marca)
                        {
                            if (h.name.StartsWith("Campo_") && float.TryParse(h.name.Substring(6), NumberStyles.Float, CultureInfo.InvariantCulture, out float campo))
                            {
                                t.Campo = campo;
                            }
                        }
                        origen = "marca " + RutaDe(marca);
                    }
                    string juego = "Logs/vista19b-" + t.Archivo + "-juego.png";
                    string poblados = Poblar(t);
                    try
                    {
                        ColocarS2cLook.Foto(t.Desde, t.Hacia, juego, t.Campo);
                    }
                    finally
                    {
                        Quitar();
                    }
                    origen += poblados;
                    string referencia = Referencias + t.Referencia + ".png";
                    string salida = Carpeta + t.Archivo + ".png";
                    if (File.Exists(referencia))
                    {
                        ColocarS2.LadoALado(referencia, juego, salida);
                        if (!string.IsNullOrEmpty(ronda))
                        {
                            Directory.CreateDirectory("Logs/vista19b-" + ronda);
                            File.Copy(salida, "Logs/vista19b-" + ronda + "/" + t.Archivo + ".png", true);
                        }
                    }
                    else
                    {
                        log.AppendLine("  falta la referencia " + referencia);
                    }
                    log.AppendLine(t.Clave + "\t" + t.Archivo + "\tdesde=" + V(t.Desde) + "\thacia=" + V(t.Hacia) + "\tcampo=" + t.Campo.ToString("0.#", CultureInfo.InvariantCulture) + "\t(" + origen + ")");
                    File.WriteAllText("Logs/vista19b.txt", log.ToString());
                }
            }
            finally
            {
                ColocarS2cLook.LejosFoto = lejosAntes;
                ColocarS2cLook.CieloCompleto = cieloAntes;
            }
            File.WriteAllText("Logs/vista19b.txt", log.ToString());
            Debug.Log("[Arte] " + log);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        /// <summary>Tipos de zona con ayudantes de foto: static PoblarFoto(Camera) antes de la foto (carros y peatones donde el juego los pone) y static QuitarFoto() después.</summary>
        private static IEnumerable<Type> TiposConPoblar()
        {
            return typeof(Vista19b).Assembly.GetTypes().Where(x => x.GetMethod("PoblarFoto", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(Camera) }, null) != null);
        }

        private static string Poblar(Toma t)
        {
            var go = new GameObject("Vista19b_CamaraPoblar");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = t.Campo;
            cam.aspect = 1376f / 768f;
            cam.farClipPlane = 600f;
            go.transform.position = t.Desde;
            go.transform.LookAt(t.Hacia);
            var nombres = new StringBuilder();
            try
            {
                foreach (Type tipo in TiposConPoblar())
                {
                    try
                    {
                        tipo.GetMethod("PoblarFoto", new[] { typeof(Camera) }).Invoke(null, new object[] { cam });
                        nombres.Append(" +" + tipo.Name + ".PoblarFoto");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e.InnerException ?? e);
                        nombres.Append(" FALLO " + tipo.Name + ".PoblarFoto");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            return nombres.ToString();
        }

        private static void Quitar()
        {
            foreach (Type tipo in TiposConPoblar())
            {
                var m = tipo.GetMethod("QuitarFoto", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null);
                try
                {
                    m?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }
            }
        }

        private static string Arg(string[] args, string nombre)
        {
            int i = Array.IndexOf(args, nombre);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static string V(Vector3 v) => v.x.ToString("0.0", CultureInfo.InvariantCulture) + ";" + v.y.ToString("0.0", CultureInfo.InvariantCulture) + ";" + v.z.ToString("0.0", CultureInfo.InvariantCulture);

        private static string RutaDe(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}
