using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1342: los carros V12 a V24 armados con los FBX de Blender que ya traen las piezas separadas (Assets/Arte/Vehiculos/Piezas/&lt;código&gt;/&lt;código&gt;.fbx):
    /// carrocería, ruedas (cadena Direccion / Giro / Modelo), puertas y cofre con bisagra, y cristales transparentes.
    /// El prefab Carro_&lt;código&gt; sale de una copia del CarroViejo (colliders, física, asiento, punto de subida, cámara y luces) y se escala al largo real de la tabla.
    /// Cada ancla Rueda_xx de la suspensión va en el lugar de su rueda del FBX. V18 (tráiler) trae tres ejes traseros: Rueda_TI y Rueda_TD son el del medio y los otros dos llevan anclas extra.
    /// El FBX viene con el frente a -Z: se gira 180° en Y como en T-1312. Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorCarrosT1342.ArmarTodos (y .Capturar o .ArmarYCapturar).
    /// </summary>
    public static class CreadorCarrosT1342
    {
        /// <summary>Código y largo real (m, eje Z del carro).</summary>
        public static readonly (string codigo, float largo)[] Carros =
        {
            ("V12", 4.5f), ("V13", 4.7f), ("V14", 3.6f), ("V15", 5.2f), ("V16", 5.5f), ("V17", 6.5f), ("V18", 12f), // pesados más cortos: Tripo los hace muy anchos y altos (medido contra el jugador)
            ("V19", 5.0f), ("V20", 4.5f), ("V21", 4.0f), ("V22", 4.6f), ("V23", 4.5f), ("V24", 4.8f),
        };

        public const string RutaInforme = "Logs/t1342-carros.txt";
        public const string CarpetaCapturas = "docs/capturas/";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string PrefijoRueda = "Rueda_";

        private const float AlturaRueda = 0.35f;
        private const float FondoBajoCajas = 0.4f;
        private const float FraccionAltoCarroceria = 0.5f;
        private const float FraccionAltoCabina = 0.92f;
        private const float FraccionAnchoCabina = 0.8f;
        private const float FraccionLargoCabina = 0.5f;
        private const float FraccionOjos = 0.78f;
        private const float FraccionFoco = 0.62f;
        private const float DistanciaPorMetroDeLargo = 6f / 4.2f;
        private const float DistanciaMinimaCamara = 4.5f;
        private const float DistanciaMaximaCamara = 8f;
        private const float SeparacionPuerta = 1f;
        private const float SeparacionJugador = 0.8f;
        private const float GradosPuertaAbierta = 70f;
        private const float GradosCofreAbierto = 55f;

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1342/Armar carros nuevos V12-V24")]
        public static void ArmarTodos()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (var c in Carros)
            {
                CreadorCarrosBlenderT1312.ConfigurarImportador(c.codigo);
            }
            AssetDatabase.Refresh();
            foreach (var c in Carros)
            {
                informe.AppendLine(ArmarUno(c.codigo, c.largo, informe));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1342:\n" + informe);
        }

        /// <summary>Nombres de las ruedas del FBX: delanteras (DI, DD) primero y el resto después, en el orden del FBX.</summary>
        public static string[] NombresRuedasFbx(GameObject fbx)
        {
            var nombres = fbx.transform.Cast<Transform>().Where(t => t.name.StartsWith(PrefijoRueda) && t.GetComponent<MeshFilter>() != null).Select(t => t.name).ToList();
            var orden = nombres.Where(n => n == "Rueda_DI").Concat(nombres.Where(n => n == "Rueda_DD")).Concat(nombres.Where(n => n != "Rueda_DI" && n != "Rueda_DD"));
            return orden.ToArray();
        }

        // Ancla de la suspensión que le toca a una rueda del FBX: las cuatro de siempre, con el eje trasero del medio (T2) como TI/TD en los de tres ejes.
        private static string AnclaDe(string rueda, string[] todas)
        {
            if (rueda == "Rueda_T2I" && !todas.Contains("Rueda_TI"))
            {
                return "Rueda_TI";
            }
            if (rueda == "Rueda_T2D" && !todas.Contains("Rueda_TD"))
            {
                return "Rueda_TD";
            }
            return rueda;
        }

        /// <summary>Arma Carro_&lt;código&gt;.prefab desde cero. Devuelve la línea de informe.</summary>
        public static string ArmarUno(string codigo, float largo, StringBuilder informe)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            if (fbx == null || fbx.transform.Find("Carroceria") == null)
            {
                return codigo + "\tFALTA " + CreadorCarrosBlenderT1312.RutaFbx(codigo) + " o su Carroceria";
            }
            GameObject baseCarro = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosT511.RutaBase);
            ConfigCamaraCarro configCamara = AssetDatabase.LoadAssetAtPath<ConfigCamaraCarro>(CreadorCarrosT511.RutaConfigCamara);
            if (baseCarro == null || configCamara == null)
            {
                return codigo + "\tFALTA el CarroViejo base o la config de cámara";
            }
            string[] ruedas = NombresRuedasFbx(fbx);
            if (!ruedas.Contains("Rueda_DI") || !ruedas.Contains("Rueda_DD") || ruedas.Length < 4)
            {
                return codigo + "\tFALTAN ruedas en el FBX: " + string.Join(", ", ruedas);
            }
            Quaternion deRaiz = fbx.transform.localRotation;
            CreadorCarrosBlenderT1312.Materiales(codigo, out Material materialCarroceria, out Material materialRueda);

            GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(baseCarro);
            PrefabUtility.UnpackPrefabInstance(raiz, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            raiz.name = "Carro_" + codigo;
            try
            {
                foreach (string viejo in new[] { "Visual_T250", CreadorCarrosPiezasT742.NombreVisual })
                {
                    Transform t = raiz.transform.Find(viejo);
                    if (t != null)
                    {
                        Object.DestroyImmediate(t.gameObject);
                    }
                }
                foreach (string nombre in new[] { "Carroceria", "Cabina" })
                {
                    raiz.transform.Find(nombre).GetComponent<Renderer>().enabled = false;
                }

                GameObject visual = new GameObject(CreadorCarrosPiezasT742.NombreVisual);
                visual.transform.SetParent(raiz.transform, false);
                visual.transform.SetSiblingIndex(0);
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * deRaiz;
                CreadorCarrosBlenderT1312.Pieza(fbx.transform.Find("Carroceria"), "Carroceria", visual.transform, materialCarroceria);
                GameObject[] medidas = ruedas.Select(n => CreadorCarrosBlenderT1312.Pieza(fbx.transform.Find(n), "Medida_" + n, visual.transform, null)).ToArray();

                // Escala al largo real (carrocería con ruedas), centrado en x y z, con lo más bajo en y = 0.
                Bounds nueva = CreadorCarrosT511.LimitesExactos(visual);
                visual.transform.localScale = Vector3.one * (largo / nueva.size.z);
                nueva = CreadorCarrosT511.LimitesExactos(visual);
                visual.transform.localPosition = new Vector3(-nueva.center.x, -nueva.min.y, -nueva.center.z);
                float escala = visual.transform.localScale.x;
                Bounds total = CreadorCarrosT511.LimitesExactos(visual);
                float L = total.size.z;
                float W = total.size.x;
                float H = total.size.y;

                // Cajas de choque a la medida (los cubos de greybox llevan los colliders).
                float cima = H * FraccionAltoCarroceria;
                Caja(raiz.transform.Find("Carroceria"), FondoBajoCajas, cima, W * 0.96f, L * 0.98f, 0f);
                Caja(raiz.transform.Find("Cabina"), cima, H * FraccionAltoCabina, W * FraccionAnchoCabina, L * FraccionLargoCabina, -L * 0.05f);

                // Anclas de la suspensión en el lugar de cada rueda (x y z reales, altura de siempre) y la cadena de cada una.
                foreach (string nombre in CreadorCarrosBlenderT1312.NombresRuedas)
                {
                    Transform ancla = raiz.transform.Find(nombre);
                    foreach (Transform hijo in ancla.Cast<Transform>().ToList())
                    {
                        Object.DestroyImmediate(hijo.gameObject);
                    }
                }
                Transform[] giro = new Transform[ruedas.Length];
                Transform[] direccionDelantera = new Transform[2];
                float sumaRadios = 0f;
                for (int i = 0; i < ruedas.Length; i++)
                {
                    Bounds b = CreadorCarrosT511.LimitesExactos(medidas[i]);
                    sumaRadios += b.extents.y;
                    string nombreAncla = AnclaDe(ruedas[i], ruedas);
                    Transform ancla = raiz.transform.Find(nombreAncla);
                    if (ancla == null)
                    {
                        GameObject extra = new GameObject(nombreAncla);
                        extra.transform.SetParent(raiz.transform, false);
                        ancla = extra.transform;
                    }
                    Vector3 local = raiz.transform.InverseTransformPoint(b.center);
                    ancla.localPosition = new Vector3(local.x, AlturaRueda, local.z);
                    Mesh malla = fbx.transform.Find(ruedas[i]).GetComponent<MeshFilter>().sharedMesh;
                    Transform direccion = CreadorCarrosBlenderT1312.ArmarCadena(ancla, b.center, malla, visual.transform.rotation, escala, materialRueda, out Transform cadenaGiro);
                    if (i < 2)
                    {
                        direccionDelantera[i] = direccion;
                    }
                    giro[i] = cadenaGiro;
                }
                foreach (GameObject m in medidas)
                {
                    Object.DestroyImmediate(m);
                }
                float distanciaEjes = Mathf.Abs(raiz.transform.Find("Rueda_DI").localPosition.z - raiz.transform.Find("Rueda_TI").localPosition.z);
                float radio = sumaRadios / ruedas.Length;
                RuedasVisualesCarro comp = raiz.GetComponent<RuedasVisualesCarro>() ?? raiz.AddComponent<RuedasVisualesCarro>();
                SerializedObject so = new SerializedObject(comp);
                CreadorCarrosBlenderT1312.Poner(so.FindProperty("direccionDelantera"), direccionDelantera);
                CreadorCarrosBlenderT1312.Poner(so.FindProperty("giro"), giro);
                so.FindProperty("radio").floatValue = radio;
                so.FindProperty("distanciaEjes").floatValue = distanciaEjes;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Asiento, punto de subida y cámara a la altura del modelo.
                float asientoZ = -L * 0.03f;
                Transform asiento = raiz.transform.Find("Asiento");
                asiento.localPosition = new Vector3(-W * 0.22f, Mathf.Max(0.3f, H * FraccionOjos - configCamara.AlturaOjos), asientoZ);
                raiz.transform.Find("PuntoSubida").localPosition = new Vector3(-(W * 0.5f + SeparacionPuerta), 0f, asientoZ);
                Transform camaraObjeto = raiz.transform.Find("CamaraCarro");
                camaraObjeto.localPosition = asiento.localPosition + Vector3.up * configCamara.AlturaOjos;
                SerializedObject soCamara = new SerializedObject(camaraObjeto.GetComponent<CamaraCarro>());
                soCamara.FindProperty("alturaFocoPropia").floatValue = H * FraccionFoco;
                soCamara.FindProperty("distanciaAtrasPropia").floatValue = Mathf.Clamp(L * DistanciaPorMetroDeLargo, DistanciaMinimaCamara, DistanciaMaximaCamara);
                soCamara.ApplyModifiedPropertiesWithoutUndo();
                if (raiz.GetComponent<CarroEstacionado>() == null)
                {
                    raiz.AddComponent<CarroEstacionado>();
                }

                PiezasCarroT1342.ArmarPiezasMoviles(fbx, visual.transform, materialCarroceria, informe);

                string piezas = string.Join(", ", fbx.transform.Cast<Transform>().Select(t => t.name));
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
                return string.Format("{0}\tlargo {1:F2} x ancho {2:F2} x alto {3:F2} m (escala {4:F3}), radio de rueda {5:F3} m, entre ejes {6:F2} m, {7} ruedas\n\tpiezas del FBX: {8}",
                    codigo, L, W, H, escala, radio, distanciaEjes, ruedas.Length, piezas);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void Caja(Transform t, float desde, float hasta, float ancho, float largo, float z)
        {
            t.localPosition = new Vector3(0f, (desde + hasta) * 0.5f, z);
            t.localScale = new Vector3(ancho, hasta - desde, largo);
        }

        // ------------------------------------------------------------------ capturas

        [MenuItem("Mecha Cyberpunk/T-1342/Armar y capturar")]
        public static void ArmarYCapturar()
        {
            ArmarTodos();
            Capturar();
        }

        /// <summary>
        /// Una foto por carro (docs/capturas/T-1342-&lt;código&gt;.png) en diagonal frontal con el jugador a pie junto a la puerta izquierda, y otra con las puertas y el cofre abiertos a mano
        /// (T-1342-&lt;código&gt;-abierto.png: puerta izquierda +70° en Y, derecha -70°, cofre -55° en X, sobre cada bisagra). Escena vacía que no se guarda.
        /// </summary>
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            GameObject luz = new GameObject("Sol");
            Light sol = luz.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.2f;
            luz.transform.rotation = Quaternion.Euler(45f, 200f, 0f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            GameObject jugadorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);

            foreach (var c in Carros)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(c.codigo));
                if (prefab == null)
                {
                    Debug.LogWarning("[Vehiculos] T-1342: no hay " + RutaPrefab(c.codigo));
                    continue;
                }
                GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                carro.transform.position = Vector3.zero;
                carro.transform.rotation = Quaternion.identity;
                GameObject jugador = null;
                Bounds cuerpo = CreadorCarrosT511.LimitesExactos(carro.transform.Find(CreadorCarrosPiezasT742.NombreVisual).gameObject);
                if (jugadorPrefab != null)
                {
                    jugador = (GameObject)PrefabUtility.InstantiatePrefab(jugadorPrefab);
                    Transform subida = carro.transform.Find("PuntoSubida");
                    jugador.transform.position = new Vector3(cuerpo.min.x - SeparacionJugador, 0f, subida != null ? subida.position.z : 0f);
                    jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                Bounds foco = cuerpo;
                if (jugador != null)
                {
                    foco.Encapsulate(jugador.transform.position + Vector3.up * 1.8f);
                }
                Encuadrar(camara, foco, new Vector3(-0.8f, 0.3f, 1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, "T-1342-" + c.codigo + ".png"));

                Transform bisagraI = carro.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/Bisagra_Puerta_I");
                Transform bisagraD = carro.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/Bisagra_Puerta_D");
                Transform bisagraCofre = carro.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/Bisagra_Cofre");
                if (bisagraI != null)
                {
                    bisagraI.Rotate(0f, GradosPuertaAbierta, 0f, Space.Self);
                }
                if (bisagraD != null)
                {
                    bisagraD.Rotate(0f, -GradosPuertaAbierta, 0f, Space.Self);
                }
                if (bisagraCofre != null)
                {
                    bisagraCofre.Rotate(-GradosCofreAbierto, 0f, 0f, Space.Self);
                }
                Encuadrar(camara, foco, new Vector3(-0.8f, 0.5f, 1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, "T-1342-" + c.codigo + "-abierto.png"));

                Object.DestroyImmediate(carro);
                if (jugador != null)
                {
                    Object.DestroyImmediate(jugador);
                }
            }
            Debug.Log("[Vehiculos] T-1342: capturas en " + CarpetaCapturas);
        }

        // Copias de ArteT250.Encuadrar y ArteT250.Guardar (el ensamblado de arte no lo referencia Vehiculos.Editor).
        private static void Encuadrar(Camera camara, Bounds b, Vector3 direccion)
        {
            camara.fieldOfView = 40f;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 1000f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            float radio = Mathf.Max(b.extents.magnitude, 1f);
            float distancia = radio / Mathf.Sin(camara.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.75f;
            camara.transform.position = b.center + direccion.normalized * distancia;
            camara.transform.LookAt(b.center);
        }

        private static void Guardar(Camera camara, string ruta)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            RenderTexture destino = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
            imagen.Apply();
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            Object.DestroyImmediate(imagen);
            destino.Release();
            Object.DestroyImmediate(destino);
        }
    }
}
