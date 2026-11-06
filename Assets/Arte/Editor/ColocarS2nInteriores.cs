using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Armas;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2n (playtest del 6-nov), interiores del CIC de Tino y del I+D+I del Doc: revisión de lo que está encimado o flotando y capturas de la silla
    /// del Doc y del banco de Tino en la escena real.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nInteriores.Revisar (informe en Logs/s2n-revisar-interiores.txt)
    /// y -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nInteriores.FotosMenus (docs/capturas/s2n-banco-tino.png y Logs/s2n-silla-consultorio-editor.png).
    /// </summary>
    public static class ColocarS2nInteriores
    {
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string RutaTaller = "S2_Interiores/TallerTino";
        public const string RutaConsultorio = "S2_Interiores/ConsultorioDoc";
        /// <summary>Caja de cada local (x0, x1, z0, z1), de ColocarS2Interiores.</summary>
        public static readonly Rect CajaTaller = Rect.MinMaxRect(-77.3f, 10.2f, -69.3f, 16f);
        public static readonly Rect CajaConsultorio = Rect.MinMaxRect(-104f, -16f, -96f, -10.2f);
        private const float PisoLocal = 0.22f;
        /// <summary>Encimado: los límites de dos piezas se cruzan más de esto en los tres ejes (m).</summary>
        private const float Cruce = 0.04f;
        /// <summary>Flotando: la base queda más arriba que esto sobre el piso o lo que tiene abajo (m).</summary>
        private const float Hueco = 0.04f;

        /// <summary>T-812a: coloca el banco de Tino y la silla del Doc, revisa y saca las fotos.</summary>
        public static void ColocarMenus()
        {
            ColocarS2nBancoArmas.Colocar();
            ColocarS2nSillaClinica.Colocar();
            Revisar();
            FotosMenus();
        }

        /// <summary>Desde la entrada del CIC, como la referencia (vano al frente, pizarrón al fondo, tablero de herramientas a la izquierda).</summary>
        public static readonly Vector3 OjoCic = new Vector3(-72.4f, 1.95f, 10.5f);
        public static readonly Vector3 MiraCic = new Vector3(-73.6f, 1.05f, 15.6f);

        /// <summary>T-811: rehace el CIC de Tino, vuelve a poner el banco de armas, revisa y saca la foto lado a lado.</summary>
        [MenuItem("Mecha Cyberpunk/S2n/CIC de Tino (T-811)")]
        public static void ColocarCic()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            ColocarS2Interiores.RehacerTallerTino();
            ColocarS2.Guardar("s2n-cic");
            ColocarS2nBancoArmas.Colocar();
            Revisar();
            ColocarS2.Foto(OjoCic, MiraCic, "Logs/s2n-cic-juego.png", 78f, 60f);
            ColocarS2.LadoALado("docs/referencias-interiores/taller-tino.png", "Logs/s2n-cic-juego.png", "docs/capturas/s2n-cic-tino.png");
        }

        /// <summary>Desde la entrada del consultorio, como la referencia (sillón al centro, repisa al fondo, monitores a la derecha).</summary>
        public static readonly Vector3 OjoConsultorio = new Vector3(-98.4f, 1.9f, -10.65f);
        public static readonly Vector3 MiraConsultorio = new Vector3(-102.3f, 0.95f, -14.5f);
        public const string PrefabDoc = "Visual_Doctor";

        /// <summary>T-812: el Doc más grande, el consultorio rehecho (sin encimados y menos verde), la silla otra vez, revisión y foto lado a lado.</summary>
        [MenuItem("Mecha Cyberpunk/S2n/I+D+I del Doc (T-812)")]
        public static void ColocarConsultorio()
        {
            EscalarDoc();
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            ColocarS2Interiores.RehacerConsultorio();
            ColocarS2.Guardar("s2n-consultorio");
            ColocarS2nSillaClinica.Colocar();
            Revisar();
            ColocarS2.Foto(OjoConsultorio, MiraConsultorio, "Logs/s2n-consultorio-juego.png", 72f, 60f);
            ColocarS2.LadoALado("docs/referencias-interiores/consultorio-doc.png", "Logs/s2n-consultorio-juego.png", "docs/capturas/s2n-consultorio-doc.png");
        }

        /// <summary>Lo de ColocarS2.Escala solo para el prefab del Doc (con su factor de la tabla ColocarS2.Personajes): no toca a nadie más.</summary>
        public static void EscalarDoc()
        {
            (string prefab, float medida, float relativa) doc = ColocarS2.Personajes.First(p => p.prefab == PrefabDoc);
            string ruta = "Assets/_Proyecto/Personaje/Visual/Prefabs/" + PrefabDoc + ".prefab";
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            Transform modelo = raiz.transform.Find("Giro/Modelo");
            float factor = ColocarS2.AlturaJugador * doc.relativa / doc.medida;
            modelo.localScale = Vector3.one * factor;
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            PrefabUtility.UnloadPrefabContents(raiz);
            Debug.Log("[Arte] " + PrefabDoc + ": escala " + factor.ToString("F3"));
        }

        [MenuItem("Mecha Cyberpunk/S2n/Interiores: revisar encimados y flotantes")]
        public static void Revisar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            RevisarLocal("CIC de Tino", RutaTaller, CajaTaller);
            RevisarLocal("I+D+I del Doc", RutaConsultorio, CajaConsultorio);
            System.IO.File.WriteAllText("Logs/s2n-revisar-interiores.txt", ColocarS2.Informe.ToString());
            Debug.Log("[Arte] s2n-revisar-interiores:\n" + ColocarS2.Informe);
        }

        /// <summary>Las piezas del local (hijas directas, sin forro ni luces) con sus límites.</summary>
        public static List<(string nombre, Bounds limites)> Piezas(Transform local)
        {
            List<(string, Bounds)> lista = new List<(string, Bounds)>();
            foreach (Transform t in local)
            {
                if (t.name == "Forro" || t.name == "Luces" || t.name == "SerieFocos" || !t.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (t.GetComponentsInChildren<Renderer>().Length == 0)
                {
                    continue;
                }
                lista.Add((t.name + "#" + t.GetSiblingIndex(), ArteT250.Limites(t.gameObject)));
            }
            return lista;
        }

        /// <summary>Volumen del cruce de dos límites (0 si alguno de los ejes se cruza menos de 'minimo').</summary>
        public static float Cruzan(Bounds a, Bounds b, float minimo)
        {
            float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float y = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
            float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            return x > minimo && y > minimo && z > minimo ? x * y * z : 0f;
        }

        private static void RevisarLocal(string titulo, string ruta, Rect caja)
        {
            StringBuilderLinea("== " + titulo + " (" + ruta + ")");
            Transform local = ColocarS2.Buscar(ruta);
            if (local == null)
            {
                StringBuilderLinea("  FALTA el local");
                return;
            }
            List<(string nombre, Bounds limites)> piezas = Piezas(local);
            foreach ((string nombre, Bounds b) in piezas)
            {
                StringBuilderLinea("  " + nombre.PadRight(30) + " min " + b.min.ToString("F2") + " max " + b.max.ToString("F2") + " tam " + b.size.ToString("F2"));
            }
            // Personajes, interactuables y lo que no es del local pero se ve dentro.
            foreach (Transform t in ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && caja.Contains(new Vector2(t.position.x, t.position.z))))
            {
                if (t.GetComponent<PersonajeApoyo>() != null || t.GetComponent<SillaClinica>() != null || t.GetComponent<BancoArmas>() != null)
                {
                    Bounds b = ArteT250.Limites(t.gameObject);
                    StringBuilderLinea("  * " + ColocarS2.Ruta(t) + " pos " + t.position.ToString("F2") + " límites " + b.min.ToString("F2") + ".." + b.max.ToString("F2"));
                    foreach (Transform h in t)
                    {
                        StringBuilderLinea("      " + h.name + " " + h.position.ToString("F2") + " giro " + h.eulerAngles.ToString("F0"));
                    }
                }
            }
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 c = r.bounds.center;
                if (!r.enabled || !caja.Contains(new Vector2(c.x, c.z)) || r.transform.IsChildOf(local) || c.y > 4f || c.y < -0.5f)
                {
                    continue;
                }
                StringBuilderLinea("  (ajeno) " + ColocarS2.Ruta(r.transform) + " " + r.bounds.min.ToString("F2") + ".." + r.bounds.max.ToString("F2"));
            }
            // Personajes contra piezas.
            foreach (PersonajeApoyo p in ColocarS2.Todos().Select(t => t.GetComponent<PersonajeApoyo>()).Where(p => p != null && p.gameObject.activeInHierarchy))
            {
                Vector3 pos = p.transform.position;
                if (!caja.Contains(new Vector2(pos.x, pos.z)))
                {
                    continue;
                }
                Bounds cuerpo = new Bounds(pos + Vector3.up * 0.8f, new Vector3(0.6f, 1.6f, 0.6f));
                foreach ((string nombre, Bounds b) in piezas)
                {
                    float v = Cruzan(cuerpo, b, Cruce);
                    if (v > 0f)
                    {
                        StringBuilderLinea("  ATRAVIESA " + p.name + " <-> " + nombre + " (" + v.ToString("F3") + " m3)");
                    }
                }
            }
            // Piezas encimadas.
            for (int i = 0; i < piezas.Count; i++)
            {
                for (int j = i + 1; j < piezas.Count; j++)
                {
                    float v = Cruzan(piezas[i].limites, piezas[j].limites, Cruce);
                    if (v > 0f)
                    {
                        StringBuilderLinea("  ENCIMA " + piezas[i].nombre + " <-> " + piezas[j].nombre + " (" + v.ToString("F3") + " m3)");
                    }
                }
            }
            // Flotantes: la base no toca el piso ni la tapa de otra pieza que tenga abajo.
            foreach ((string nombre, Bounds b) in piezas)
            {
                float apoyo = PisoLocal;
                foreach ((string otro, Bounds o) in piezas)
                {
                    if (otro == nombre || o.max.y > b.min.y + Hueco)
                    {
                        continue;
                    }
                    bool debajo = o.min.x < b.max.x && o.max.x > b.min.x && o.min.z < b.max.z && o.max.z > b.min.z;
                    if (debajo)
                    {
                        apoyo = Mathf.Max(apoyo, o.max.y);
                    }
                }
                float hueco = b.min.y - apoyo;
                if (hueco > Hueco)
                {
                    StringBuilderLinea("  FLOTA " + nombre + " " + hueco.ToString("F2") + " m sobre su apoyo (" + apoyo.ToString("F2") + ")");
                }
            }
            foreach (Light l in local.GetComponentsInChildren<Light>())
            {
                StringBuilderLinea("  luz " + l.name + " " + l.type + " color " + l.color + " int " + l.intensity.ToString("F1") + " alcance " + l.range.ToString("F1"));
            }
        }

        private static void StringBuilderLinea(string s) => ColocarS2.Informe.AppendLine(s);

        // ---------------------------------------------------------------- Capturas de la silla y el banco

        /// <summary>Fotos sueltas para revisar: variable S2N_FOTOS = "dx,dy,dz>hx,hy,hz>ruta>campo|..." (sin guardar la escena).</summary>
        public static void FotosLibres()
        {
            ColocarS2.Abrir();
            string lista = System.Environment.GetEnvironmentVariable("S2N_FOTOS") ?? string.Empty;
            foreach (string foto in lista.Split('|').Where(f => f.Contains(">")))
            {
                string[] p = foto.Split('>');
                ColocarS2.Foto(Vector(p[0]), Vector(p[1]), p[2], p.Length > 3 ? float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture) : 60f, 60f);
            }
        }

        private static Vector3 Vector(string s)
        {
            float[] v = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(v[0], v[1], v[2]);
        }

        [MenuItem("Mecha Cyberpunk/S2n/Interiores: fotos de la silla y el banco")]
        public static void FotosMenus()
        {
            ColocarS2.Abrir();
            // La foto buena de la silla sale en Play (InterioresS2nPruebas con CAPTURAR_S2N_INTERIORES=1); esta es la de revisión, en el editor.
            FotoSilla("Logs/s2n-silla-consultorio-editor.png");
            FotoBanco("docs/capturas/s2n-banco-tino.png");
        }

        /// <summary>El jugador sentado con la misma pose de PoseSillaClinica, visto con la cámara cercana de la silla (sin guardar la escena).</summary>
        public static void FotoSilla(string ruta)
        {
            SillaClinica silla = Object.FindFirstObjectByType<SillaClinica>();
            if (silla == null)
            {
                Debug.LogError("[Arte] No hay SillaClinica en la escena");
                return;
            }
            SerializedObject so = new SerializedObject(silla);
            Transform asiento = (Transform)so.FindProperty("asiento").objectReferenceValue;
            Transform salida = (Transform)so.FindProperty("salida").objectReferenceValue;
            ConfigSillaClinica config = silla.Config;
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(new Vector3(asiento.position.x, salida.position.y, asiento.position.z), asiento.rotation);
            Animator animador = jugador.GetComponentInChildren<Animator>();
            PoseSillaClinica pose = animador.gameObject.AddComponent<PoseSillaClinica>();
            typeof(PoseSillaClinica).GetField("animador", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pose, animador);
            pose.Configurar(asiento.position + Vector3.up * config.AlturaCadera, asiento.forward, config.Recostado);
            pose.Peso = 1f;
            MethodInfo tarde = typeof(PoseSillaClinica).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            tarde.Invoke(pose, null);
            Vector3 camara = asiento.position + asiento.right * config.PosicionCamara.x + Vector3.up * config.PosicionCamara.y + asiento.forward * config.PosicionCamara.z;
            ColocarS2.Foto(camara, asiento.position + Vector3.up * config.AlturaMira, ruta, config.CampoVision, 50f);
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform pie = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
            Debug.Log("[Arte] silla: asiento " + asiento.position.ToString("F2") + " frente " + asiento.forward.ToString("F2") + " cadera "
                + (cadera != null ? cadera.position.ToString("F2") : "?") + " pie " + (pie != null ? pie.position.ToString("F2") : "?")
                + " cabeza " + (cabeza != null ? cabeza.position.ToString("F2") : "?") + " cámara " + camara.ToString("F2"));
            Object.DestroyImmediate(jugador);
        }

        /// <summary>El jugador parado en el punto de uso del banco, visto por detrás y de lado (sin guardar la escena).</summary>
        public static void FotoBanco(string ruta)
        {
            BancoArmas banco = Object.FindFirstObjectByType<BancoArmas>();
            if (banco == null)
            {
                Debug.LogError("[Arte] No hay BancoArmas en la escena");
                return;
            }
            Vector3 punto = banco.Posicion;
            Vector3 hacia = banco.transform.position - punto;
            hacia.y = 0f;
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(punto, Quaternion.LookRotation(hacia.sqrMagnitude > 0.001f ? hacia.normalized : Vector3.forward));
            Vector3 camara = punto - hacia.normalized * 1.5f + Vector3.up * 1.65f + Vector3.Cross(Vector3.up, hacia.normalized) * 0.9f;
            ColocarS2.Foto(camara, banco.transform.position, ruta, 60f, 50f);
            Debug.Log("[Arte] banco: punto de uso " + punto.ToString("F2") + " banco " + banco.transform.position.ToString("F2"));
            Object.DestroyImmediate(jugador);
        }
    }
}
