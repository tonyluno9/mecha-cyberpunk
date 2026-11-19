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
    /// T-1343: los carros y motos viejos con los FBX nuevos de Blender (Assets/Arte/Vehiculos/Piezas/&lt;código&gt;/&lt;código&gt;.fbx).
    /// Carros V02, V03, V04, V06, V09, V10 (ya armados por T-1312, conservan su tamaño) y V01a, V01b, V05, V11 (conservan el largo y el lugar de su carrocería de antes):
    /// carrocería, ruedas (Direccion / Giro / Modelo), cristales transparentes y, si el FBX las trae, puertas y cofre con bisagra (<see cref="PiezasCarroT1342"/>).
    /// Motos V07 y V08 (sobre su prefab, con su largo de antes) y MT01 y MT02 (prefab nuevo, copia de Carro_C19, de 2.1 y 2.4 m): el cuerpo del FBX y las dos ruedas que giran en Ruedas_T743.
    /// Si no hay prefab Carro_V11 se anota y se salta. Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorCarrosT1343.ArmarTodos (y .Capturar o .ArmarYCapturar).
    /// </summary>
    public static class CreadorCarrosT1343
    {
        /// <summary>Carros ya armados por T-1312: su tamaño no se toca.</summary>
        public static readonly string[] CarrosT1312 = CreadorCarrosBlenderT1312.Carros;

        /// <summary>Carros que pasan al flujo de T-1312 conservando el tamaño de su carrocería de antes (V11 solo si existe su prefab).</summary>
        public static readonly string[] CarrosAnteriores = { "V01a", "V01b", "V05", "V11" };

        /// <summary>Moto, nombre del cuerpo en el FBX y largo real en m (0 = conservar el largo que tiene el prefab).</summary>
        public static readonly (string codigo, string cuerpo, float largo)[] Motos =
        {
            ("V07", "Cuerpo", 0f), ("V08", "Cuerpo", 0f), ("MT01", "Carroceria", 2.1f), ("MT02", "Carroceria", 2.4f),
        };

        public const string RutaInforme = "Logs/t1343-carros.txt";
        public const string CarpetaCapturas = "docs/capturas/";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string PrefijoMotoNueva = "MT";
        public const string CodigoMotoBase = "C19";

        private const float FraccionMaximaCaballete = 0.05f;
        private const float AnguloParadoMoto = 28f;
        private const float AnguloRapidoMoto = 10f;
        private const float SeparacionJugador = 0.8f;
        private const float GradosPuertaAbierta = 70f;
        private const float GradosCofreAbierto = 55f;
        private const string MarcaCarroceriaVieja = "Carroceria_T742";

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        public static bool ExistePrefab(string codigo) => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo)) != null;

        public static string[] TodosLosCodigos() => CarrosT1312.Concat(CarrosAnteriores).Concat(Motos.Select(m => m.codigo)).ToArray();

        [MenuItem("Mecha Cyberpunk/T-1343/Carros viejos y motos de Blender")]
        public static void ArmarTodos()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in TodosLosCodigos())
            {
                CreadorCarrosBlenderT1312.ConfigurarImportador(codigo);
            }
            AssetDatabase.Refresh();
            foreach (string codigo in CarrosT1312.Concat(CarrosAnteriores))
            {
                ArmarCarroPrefab(codigo, informe);
            }
            foreach (var m in Motos)
            {
                ArmarMotoPrefab(m.codigo, m.cuerpo, m.largo, informe);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1343:\n" + informe);
        }

        // ------------------------------------------------------------------ carros

        public static void ArmarCarroPrefab(string codigo, StringBuilder informe)
        {
            if (!ExistePrefab(codigo))
            {
                informe.AppendLine(codigo + "\tNO hay " + RutaPrefab(codigo) + ": se salta");
                return;
            }
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            if (fbx == null || fbx.transform.Find("Carroceria") == null)
            {
                informe.AppendLine(codigo + "\tFALTA " + CreadorCarrosBlenderT1312.RutaFbx(codigo) + " o su Carroceria");
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                PrepararCarroceriaVieja(raiz, codigo);
                informe.AppendLine(CreadorCarrosBlenderT1312.ArmarCarro(raiz, codigo));
                Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                CreadorCarrosBlenderT1312.Materiales(codigo, out Material materialCarroceria, out _);
                PiezasCarroT1342.ArmarPiezasMoviles(fbx, visual, materialCarroceria, informe);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // Los carros armados por T-742 (V01a, V01b, V05, V11) traen una Carroceria de antes, sin las ruedas. El armado de T-1312 conserva la escala de una Carroceria ya armada
        // (hecha con el FBX nuevo); aquí no lo es, así que se le cambia el nombre para que tome sus límites como "el modelo de antes" y la nueva mida lo mismo en largo, centrada igual.
        // Si el visual ya tiene cristales o bisagras, este mismo creador lo armó antes y se deja como está.
        private static void PrepararCarroceriaVieja(GameObject raiz, string codigo)
        {
            if (CarrosT1312.Contains(codigo))
            {
                return;
            }
            Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            if (visual == null || visual.Find("Carroceria") == null)
            {
                return;
            }
            bool armadoAqui = visual.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith(PiezasCarroT1342.PrefijoCristal) || t.name.StartsWith("Bisagra_"));
            if (!armadoAqui)
            {
                visual.Find("Carroceria").name = MarcaCarroceriaVieja;
            }
        }

        // ------------------------------------------------------------------ motos

        private static void ArmarMotoPrefab(string codigo, string nombreCuerpo, float largo, StringBuilder informe)
        {
            if (!ExistePrefab(codigo))
            {
                if (!codigo.StartsWith(PrefijoMotoNueva) || !ExistePrefab(CodigoMotoBase))
                {
                    informe.AppendLine(codigo + "\tNO hay " + RutaPrefab(codigo) + ": se salta");
                    return;
                }
                AssetDatabase.CopyAsset(RutaPrefab(CodigoMotoBase), RutaPrefab(codigo));
                AssetDatabase.Refresh();
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                raiz.name = "Carro_" + codigo;
                informe.AppendLine(ArmarMoto(raiz, codigo, nombreCuerpo, largo));
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Como <see cref="CreadorCarrosBlenderT1312.ArmarMoto"/> pero para cualquier moto: <paramref name="nombreCuerpo"/> es el hijo del FBX con la malla del cuerpo y
        /// <paramref name="largoPedido"/> el largo en m (0 = el largo del cuerpo que ya tiene el prefab). Devuelve una línea de informe.
        /// </summary>
        public static string ArmarMoto(GameObject raiz, string codigo, string nombreCuerpo, float largoPedido)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            Transform cuerpo = fbx != null ? fbx.transform.Find(nombreCuerpo) : null;
            Transform rd = fbx != null ? fbx.transform.Find("Rueda_D") : null;
            Transform rt = fbx != null ? fbx.transform.Find("Rueda_T") : null;
            if (cuerpo == null || rd == null || rt == null)
            {
                return codigo + "\tFALTA " + CreadorCarrosBlenderT1312.RutaFbx(codigo) + " o su " + nombreCuerpo + " / Rueda_D / Rueda_T";
            }
            Quaternion deRaiz = fbx.transform.localRotation;
            CreadorCarrosBlenderT1312.Materiales(codigo, out Material materialCuerpo, out Material materialRueda);
            Transform envoltorio = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            Transform visual = envoltorio.Find("Visual");
            MeshFilter filtro = visual.GetComponent<MeshFilter>();
            MeshRenderer dibujo = visual.GetComponent<MeshRenderer>();
            ManejoMoto manejo = raiz.GetComponent<ManejoMoto>();
            Mesh malla = cuerpo.GetComponent<MeshFilter>().sharedMesh;

            // Largo: el pedido o el del cuerpo de antes (solo el cuerpo: sin las ruedas, así correrlo otra vez da lo mismo).
            float largoActual = CreadorCarrosT511.LimitesExactos(visual.gameObject).size.z;
            float largoFinal = largoPedido > 0f ? largoPedido : largoActual;

            dibujo.sharedMaterial = materialCuerpo;
            filtro.sharedMesh = malla;
            visual.localRotation = Quaternion.Inverse(envoltorio.localRotation) * Quaternion.Euler(0f, 180f, 0f) * deRaiz;
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            Bounds sinEscala = CreadorCarrosT511.LimitesExactos(visual.gameObject);
            visual.localScale = Vector3.one * (largoFinal / sinEscala.size.z);
            Vector3 centroD = CentroEnModelo(rd);
            Vector3 centroT = CentroEnModelo(rt);
            Vector3 medio = visual.TransformPoint((centroD + centroT) * 0.5f);
            visual.localPosition = new Vector3(-medio.x, 0f, -medio.z);
            float escala = visual.lossyScale.x;
            Vector3 mundoD = visual.TransformPoint(centroD);
            Vector3 mundoT = visual.TransformPoint(centroT);
            float radioD = rd.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * escala;
            float radioT = rt.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * escala;

            // Caballete: V07 y V08 usan la zona medida en T-592; las motos nuevas no (no se midió su caballete): la malla "sin caballete" es la misma.
            string nota;
            Mesh sinCaballete = null;
            if (codigo.StartsWith(PrefijoMotoNueva))
            {
                sinCaballete = malla;
                nota = "sin zona de caballete medida: la malla sin caballete es la misma";
            }
            else
            {
                Matrix4x4 aPrefab = raiz.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;
                Mesh recortada = CreadorMotosT592.QuitarCaballete(malla, aPrefab, CreadorMotosT592.ZonaCaballete(codigo), out _, out int quitados);
                int total = malla.triangles.Length / 3;
                if (recortada != null && quitados > total * FraccionMaximaCaballete)
                {
                    Object.DestroyImmediate(recortada);
                    recortada = null;
                    sinCaballete = malla; // ManejoMoto necesita una malla para ocultar el caballete: la misma, sin recorte
                    nota = "la zona de caballete tocaba " + quitados + " de " + total + " triángulos (más del " + (FraccionMaximaCaballete * 100f) + "%): no se quita nada";
                }
                else
                {
                    nota = "caballete quitado: " + quitados + " triángulos";
                }
                if (recortada != null)
                {
                    recortada.name = "MallaMoto_" + codigo + "_SinCaballete";
                    string rutaSin = CreadorMotosT592.CarpetaMallas + recortada.name + ".asset";
                    Mesh guardada = AssetDatabase.LoadAssetAtPath<Mesh>(rutaSin);
                    if (guardada == null)
                    {
                        AssetDatabase.CreateAsset(recortada, rutaSin);
                        guardada = recortada;
                    }
                    else
                    {
                        EditorUtility.CopySerialized(recortada, guardada);
                    }
                    sinCaballete = guardada;
                }
            }
            SerializedObject sm = new SerializedObject(manejo);
            sm.FindProperty("mallaCompleta").objectReferenceValue = malla;
            sm.FindProperty("mallaSinCaballete").objectReferenceValue = sinCaballete;
            sm.FindProperty("bajarAlOcultar").floatValue = 0f;
            sm.ApplyModifiedPropertiesWithoutUndo();

            // Ruedas.
            Transform viejas = envoltorio.Find(CreadorMotosPiezasT743.NombreRuedas);
            if (viejas != null)
            {
                Object.DestroyImmediate(viejas.gameObject);
            }
            GameObject ruedas = new GameObject(CreadorMotosPiezasT743.NombreRuedas);
            ruedas.transform.SetParent(envoltorio, false);
            ruedas.transform.localRotation = Quaternion.Inverse(envoltorio.localRotation);
            Transform direccionDel = ArmarRuedaMoto(ruedas.transform, "Rueda_Del", mundoD, rd.GetComponent<MeshFilter>().sharedMesh, visual.rotation, escala, materialRueda, out Transform giroDel);
            ArmarRuedaMoto(ruedas.transform, "Rueda_Tras", mundoT, rt.GetComponent<MeshFilter>().sharedMesh, visual.rotation, escala, materialRueda, out Transform giroTras);

            RuedasVisualesCarro comp = raiz.GetComponent<RuedasVisualesCarro>();
            if (comp == null)
            {
                comp = raiz.AddComponent<RuedasVisualesCarro>();
            }
            float entreEjes = Mathf.Abs(Vector3.Dot(mundoD - mundoT, raiz.transform.forward));
            SerializedObject sc = new SerializedObject(comp);
            CreadorCarrosBlenderT1312.Poner(sc.FindProperty("direccionDelantera"), new[] { direccionDel });
            CreadorCarrosBlenderT1312.Poner(sc.FindProperty("giro"), new[] { giroDel, giroTras });
            sc.FindProperty("radio").floatValue = (radioD + radioT) * 0.5f;
            sc.FindProperty("distanciaEjes").floatValue = entreEjes;
            sc.FindProperty("anguloParado").floatValue = AnguloParadoMoto;
            sc.FindProperty("anguloRapido").floatValue = AnguloRapidoMoto;
            sc.ApplyModifiedPropertiesWithoutUndo();

            Bounds final = CreadorCarrosT511.LimitesExactos(visual.gameObject);
            string aviso = final.size.x > final.size.z ? " AVISO: el eje largo del cuerpo no es Z" : "";
            return string.Format("{0}\tmoto: cuerpo {1:F2} x {2:F2} x {3:F2} m (antes {4:F2} m de largo, escala {5:F3}), ruedas r {6:F3} / {7:F3} m, entre ejes {8:F2} m; {9}{10}",
                codigo, final.size.x, final.size.y, final.size.z, largoActual, escala, radioD, radioT, entreEjes, nota, aviso);
        }

        // Centro (en el marco de la malla del FBX, sin la rotación de raíz) de la caja de la malla de una pieza.
        private static Vector3 CentroEnModelo(Transform pieza)
        {
            return pieza.localPosition + pieza.localRotation * Vector3.Scale(pieza.localScale, pieza.GetComponent<MeshFilter>().sharedMesh.bounds.center);
        }

        private static Transform ArmarRuedaMoto(Transform padre, string nombre, Vector3 centroMundo, Mesh malla, Quaternion rotacionMundo, float escala, Material material, out Transform giro)
        {
            GameObject rueda = new GameObject(nombre);
            rueda.transform.SetParent(padre, false);
            rueda.transform.position = centroMundo;
            return CreadorCarrosBlenderT1312.ArmarCadena(rueda.transform, centroMundo, malla, rotacionMundo, escala, material, out giro);
        }

        // ------------------------------------------------------------------ capturas

        [MenuItem("Mecha Cyberpunk/T-1343/Armar y capturar")]
        public static void ArmarYCapturar()
        {
            ArmarTodos();
            Capturar();
        }

        /// <summary>
        /// Una foto por carro o moto (docs/capturas/T-1343-&lt;código&gt;.png) en diagonal frontal con el jugador a pie al lado, y otra con las puertas y el cofre abiertos a mano
        /// (T-1343-&lt;código&gt;-abierto.png) para los que tienen bisagras. Escena vacía que no se guarda.
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

            foreach (string codigo in TodosLosCodigos())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                if (prefab == null)
                {
                    Debug.LogWarning("[Vehiculos] T-1343: no hay " + RutaPrefab(codigo));
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
                    Transform subida = carro.transform.Find("PuntoSubida") ?? carro.transform.Find(CreadorCarrosPiezasT742.NombrePuntoPuerta);
                    jugador.transform.position = new Vector3(cuerpo.min.x - SeparacionJugador, 0f, subida != null ? subida.position.z : 0f);
                    jugador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                Bounds foco = cuerpo;
                if (jugador != null)
                {
                    foco.Encapsulate(jugador.transform.position + Vector3.up * 1.8f);
                }
                Encuadrar(camara, foco, new Vector3(-0.8f, 0.3f, 1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, "T-1343-" + codigo + ".png"));

                string visual = CreadorCarrosPiezasT742.NombreVisual;
                Transform bisagraI = carro.transform.Find(visual + "/Bisagra_Puerta_I");
                Transform bisagraD = carro.transform.Find(visual + "/Bisagra_Puerta_D");
                Transform bisagraCofre = carro.transform.Find(visual + "/Bisagra_Cofre");
                if (bisagraI != null || bisagraD != null || bisagraCofre != null)
                {
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
                    Guardar(camara, Path.Combine(CarpetaCapturas, "T-1343-" + codigo + "-abierto.png"));
                }

                Object.DestroyImmediate(carro);
                if (jugador != null)
                {
                    Object.DestroyImmediate(jugador);
                }
            }
            Debug.Log("[Vehiculos] T-1343: capturas en " + CarpetaCapturas);
        }

        // Copias de las de CreadorCarrosT1342 (son privadas allá).
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
