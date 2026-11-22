using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1604 (T6, modelos): V01a, V01b y V05 se reemplazaron en Blender por versiones por piezas (Carroceria, Cofre, Motor, Puerta_D/I, Rueda_DD/DI/TD/TI, Interior, Interior_Casco) sin Cristal_*.
    /// Aplicar: reimporta los tres FBX (escala real), rearma Carro_V01a / V01b / V05 con los creadores de siempre (bisagras desde la puerta y el cofre nuevos), quita todo Cristal_*,
    /// ajusta las cajas de choque a la carrocería y escribe Logs/t1604-modelos.txt.
    /// CambiarCarrosViejos: en Ciudad_Prologo cambia las instancias de los prefabs viejos de una malla por su Carro_Vxx (T-1554 ya dejó V10 y V11 como descartados: no se cambian).
    /// Diagnostico: solo informe (Logs/t1604-diagnostico.txt).
    /// Orden: Aplicar, CambiarCarrosViejos y Diagnostico.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.ModelosBlenderT1604.Aplicar (y .CambiarCarrosViejos, .Diagnostico). Correrlo otra vez deja lo mismo.
    /// </summary>
    public static class ModelosBlenderT1604
    {
        public static readonly string[] Codigos = { "V01a", "V01b", "V05" };

        public const string RutaInformeModelos = "Logs/t1604-modelos.txt";
        public const string RutaInformeDiagnostico = "Logs/t1604-diagnostico.txt";
        public const string RutaEscena = VehiculosT1554.RutaEscenaPrologo;

        public const float LargoMinimo = 3.8f;
        public const float LargoMaximo = 5f;
        public const float LargoObjetivo = 4.4f;
        private const float AnchoMinimoRealista = 1.6f;
        private const float AnchoMaximoRealista = 2.3f;

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        // ------------------------------------------------------------------ 1. Aplicar

        [MenuItem("Mecha Cyberpunk/T-1604/Aplicar carros V01a V01b V05")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== T-1604: carros V01a, V01b y V05 por piezas (FBX nuevos del 22-nov)");

            // Medidas de antes (los prefabs todavía tienen el armado viejo).
            Dictionary<string, string> antes = new Dictionary<string, string>();
            foreach (string codigo in Codigos)
            {
                antes[codigo] = MedidasDePrefab(codigo);
            }

            foreach (string codigo in Codigos)
            {
                informe.AppendLine(ImportarFbx(codigo));
            }
            AssetDatabase.Refresh();

            foreach (string codigo in Codigos)
            {
                CreadorCarrosT1343.ArmarCarroPrefab(codigo, informe);
            }
            CarrosRevisadosT1507.PonerRinPolicia("V05", informe);

            foreach (string codigo in Codigos)
            {
                informe.AppendLine(Terminar(codigo));
            }
            AssetDatabase.SaveAssets();

            informe.AppendLine();
            informe.AppendLine("== Medidas antes / despues (visual en el marco del prefab, largo x ancho x alto)");
            foreach (string codigo in Codigos)
            {
                informe.AppendLine(codigo + "\tantes: " + antes[codigo]);
                informe.AppendLine(codigo + "\tdespues: " + MedidasDePrefab(codigo));
            }
            informe.AppendLine();
            informe.AppendLine("== Piezas encontradas");
            foreach (string codigo in Codigos)
            {
                informe.Append(Piezas(codigo));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInformeModelos, informe.ToString());
            Debug.Log("[Vehiculos] T-1604:\n" + informe);
        }

        /// <summary>Importador del FBX (sin materiales ni animación, sin remaps a nodos viejos como Cristal_*) y escala real. Devuelve una línea de informe.</summary>
        public static string ImportarFbx(string codigo)
        {
            string ruta = CreadorCarrosBlenderT1312.RutaFbx(codigo);
            ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                return codigo + "\tFALTA el FBX " + ruta;
            }
            bool cambio = false;
            // Los nodos de antes (Cristal_*) ya no existen: se quitan sus remaps de material guardados en el .meta.
            foreach (var par in importador.GetExternalObjectMap().Where(p => p.Key.name.StartsWith("Cristal")).ToList())
            {
                importador.RemoveRemap(par.Key);
                cambio = true;
            }
            if (cambio)
            {
                importador.SaveAndReimport();
            }
            CreadorCarrosBlenderT1312.ConfigurarImportador(codigo);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

            Vector3 tamano = TamanoFbx(ruta);
            float largo = Mathf.Max(tamano.x, tamano.y, tamano.z);
            string linea = string.Format("{0}\tFBX importado: {1:F2} x {2:F2} x {3:F2} m (escala {4:F3})", codigo, tamano.x, tamano.y, tamano.z, importador.globalScale);
            if (largo > 0.01f && (largo < LargoMinimo || largo > LargoMaximo))
            {
                float factor = LargoObjetivo / largo;
                importador.globalScale *= factor;
                importador.SaveAndReimport();
                Vector3 corregido = TamanoFbx(ruta);
                linea += string.Format("; fuera de {0}-{1} m: escala x{2:F3} -> {3:F2} x {4:F2} x {5:F2} m", LargoMinimo, LargoMaximo, factor, corregido.x, corregido.y, corregido.z);
            }
            return linea;
        }

        // Tamaño de todas las mallas del FBX en el marco de su raíz.
        private static Vector3 TamanoFbx(string ruta)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (fbx == null)
            {
                return Vector3.zero;
            }
            Quaternion deRaiz = Quaternion.Inverse(fbx.transform.localRotation);
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in fbx.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 aRaiz = fbx.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 p = deRaiz * aRaiz.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(p);
                    }
                }
            }
            return b.size;
        }

        // Quita lo que quede de Cristal_* y ajusta las cajas de choque a la carrocería nueva.
        private static string Terminar(string codigo)
        {
            if (!File.Exists(RutaPrefab(codigo)))
            {
                return codigo + "\tNO hay prefab";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                int cristales = 0;
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(PiezasCarroT1342.PrefijoCristal)).ToList())
                {
                    if (t != null)
                    {
                        Object.DestroyImmediate(t.gameObject);
                        cristales++;
                    }
                }
                string cajas = AjustarCajas(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
                return codigo + "\tCristal_* quitados: " + cristales + "; cajas de choque: " + cajas;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Recorta las cajas de choque (cubos Carroceria y Cabina de la raíz, sin rotación) a los límites del modelo (Visual_T511). Nunca las agranda.
        /// Devuelve el tamaño final de cada una.
        /// </summary>
        public static string AjustarCajas(GameObject raiz)
        {
            Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            if (visual == null)
            {
                return "sin Visual_T511";
            }
            Bounds modelo = CreadorCarrosBlenderT1312.LimitesEn(visual.gameObject, raiz.transform);
            StringBuilder sb = new StringBuilder();
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                Transform t = raiz.transform.Find(nombre);
                BoxCollider caja = t != null ? t.GetComponent<BoxCollider>() : null;
                if (caja == null || caja.size.x <= 0f || caja.size.y <= 0f || caja.size.z <= 0f)
                {
                    continue;
                }
                Vector3 centro = t.localPosition + Vector3.Scale(t.localScale, caja.center);
                Vector3 tamano = Vector3.Scale(t.localScale, caja.size);
                Vector3 minimo = Vector3.Max(centro - tamano * 0.5f, modelo.min);
                Vector3 maximo = Vector3.Min(centro + tamano * 0.5f, modelo.max);
                if (maximo.x <= minimo.x || maximo.y <= minimo.y || maximo.z <= minimo.z)
                {
                    continue;
                }
                Vector3 nuevoTamano = maximo - minimo;
                t.localScale = new Vector3(nuevoTamano.x / caja.size.x, nuevoTamano.y / caja.size.y, nuevoTamano.z / caja.size.z);
                t.localPosition = (minimo + maximo) * 0.5f - Vector3.Scale(t.localScale, caja.center);
                sb.Append(nombre).Append(' ').Append(nuevoTamano.ToString("F2")).Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        // ------------------------------------------------------------------ 2. Cambiar los carros viejos de la escena

        [MenuItem("Mecha Cyberpunk/T-1604/Cambiar carros viejos de Ciudad_Prologo")]
        public static void CambiarCarrosViejos()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("== T-1604: carros viejos de una malla en Ciudad_Prologo");
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                bool cambio = VehiculosT1554.ReemplazarViejos(escena.GetRootGameObjects(), true, sb);
                if (cambio)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    sb.AppendLine("Ciudad_Prologo.unity guardada");
                }
                else
                {
                    sb.AppendLine("Sin cambios: la escena no se guarda");
                }
                sb.AppendLine("Instancias viejas que quedan: " + ViejosConReemplazo(escena.GetRootGameObjects(), false).Count + " con reemplazo posible, "
                    + ViejosConReemplazo(escena.GetRootGameObjects(), true).Count + " en total (las demas son V10 o V11, descartados por T-1554)");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInformeModelos, "\n" + sb);
            Debug.Log("[Vehiculos] T-1604:\n" + sb);
        }

        /// <summary>Instancias sueltas de prefabs viejos de una malla; con <paramref name="todos"/> falso solo las que tienen un Carro_Vxx que las reemplace (V10 y V11 están descartados).</summary>
        public static List<GameObject> ViejosConReemplazo(IEnumerable<GameObject> raices, bool todos)
        {
            return VehiculosT1554.InstanciasViejas(raices, out _).Where(go =>
            {
                if (todos)
                {
                    return true;
                }
                Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(go);
                return VehiculosT1554.RutaDeReemplazo(AssetDatabase.GetAssetPath(fuente)) != null;
            }).ToList();
        }

        // ------------------------------------------------------------------ 3. Diagnóstico

        [MenuItem("Mecha Cyberpunk/T-1604/Diagnostico")]
        public static void Diagnostico()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("== T-1604 diagnostico (solo informe)");
            sb.AppendLine("Ciudad_Prologo: instancias de prefabs viejos de una malla");
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject go in VehiculosT1554.InstanciasViejas(escena.GetRootGameObjects(), out int anidados))
                {
                    string ruta = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(go));
                    string reemplazo = VehiculosT1554.RutaDeReemplazo(ruta);
                    sb.AppendLine("\t" + go.name + " (" + Path.GetFileName(ruta) + ") en " + go.transform.position.ToString("F2") + " -> " + (reemplazo != null ? Path.GetFileName(reemplazo) : "descartado"));
                }
                sb.AppendLine("\tinstancias dentro de otro prefab: " + anidados);
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
            foreach (string codigo in Codigos)
            {
                sb.AppendLine();
                sb.AppendLine(codigo + "\t" + MedidasDePrefab(codigo));
                sb.Append(Piezas(codigo));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInformeDiagnostico, sb.ToString());
            Debug.Log("[Vehiculos] T-1604:\n" + sb);
        }

        // ------------------------------------------------------------------ medidas y comprobaciones (también las usan las pruebas)

        public static string MedidasDePrefab(string codigo)
        {
            if (!File.Exists(RutaPrefab(codigo)))
            {
                return "sin prefab";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                if (visual == null)
                {
                    return "sin Visual_T511";
                }
                Bounds b = CreadorCarrosT511.LimitesExactos(visual.gameObject);
                string aviso = b.size.x < AnchoMinimoRealista || b.size.x > AnchoMaximoRealista ? " (ancho fuera de " + AnchoMinimoRealista + "-" + AnchoMaximoRealista + " m)" : "";
                return string.Format("{0:F2} x {1:F2} x {2:F2} m{3}", b.size.z, b.size.x, b.size.y, aviso);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // Piezas del FBX, piezas del prefab y referencias rotas.
        private static string Piezas(string codigo)
        {
            StringBuilder sb = new StringBuilder();
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            sb.AppendLine(codigo + "\tFBX: " + (fbx == null ? "NO existe" : string.Join(", ", fbx.transform.Cast<Transform>().Select(t => t.name))));
            if (!File.Exists(RutaPrefab(codigo)))
            {
                sb.AppendLine(codigo + "\tNO hay prefab");
                return sb.ToString();
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                if (visual != null)
                {
                    sb.AppendLine(codigo + "\tprefab (Visual_T511): " + string.Join(", ", visual.GetComponentsInChildren<Transform>(true).Where(t => t != visual).Select(t => t.name)));
                }
                sb.AppendLine(codigo + "\tCristal_* en el prefab: " + raiz.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith(PiezasCarroT1342.PrefijoCristal)));
                List<string> nulos = ReferenciasRotas(raiz);
                sb.AppendLine(codigo + "\treferencias nulas o rotas: " + (nulos.Count == 0 ? "ninguna" : string.Join("; ", nulos)));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Referencias que deberían apuntar a algo y no lo hacen: las bisagras de PuertasCarro, las ruedas de RuedasVisualesCarro, scripts faltantes
        /// y cualquier referencia a un objeto que ya no existe (id distinto de cero con valor nulo) en los componentes del prefab.
        /// </summary>
        public static List<string> ReferenciasRotas(GameObject raiz)
        {
            List<string> lista = new List<string>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                int faltantes = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (faltantes > 0)
                {
                    lista.Add(t.name + ": " + faltantes + " scripts faltantes");
                }
            }
            PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
            if (puertas == null)
            {
                lista.Add("sin PuertasCarro");
            }
            else
            {
                SerializedObject so = new SerializedObject(puertas);
                foreach (string campo in new[] { "bisagraPuertaIzquierda", "bisagraPuertaDerecha", "bisagraCofre" })
                {
                    SerializedProperty p = so.FindProperty(campo);
                    if (p == null || p.objectReferenceValue == null)
                    {
                        lista.Add("PuertasCarro." + campo + " vacia");
                    }
                }
            }
            RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
            if (ruedas == null)
            {
                lista.Add("sin RuedasVisualesCarro");
            }
            else
            {
                SerializedObject so = new SerializedObject(ruedas);
                foreach (string campo in new[] { "direccionDelantera", "giro" })
                {
                    SerializedProperty p = so.FindProperty(campo);
                    for (int i = 0; p != null && i < p.arraySize; i++)
                    {
                        if (p.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            lista.Add("RuedasVisualesCarro." + campo + "[" + i + "] vacia");
                        }
                    }
                }
            }
            foreach (Component c in raiz.GetComponentsInChildren<Component>(true))
            {
                if (c == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(c);
                SerializedProperty it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
                    {
                        lista.Add(c.GetType().Name + " en " + c.gameObject.name + "." + it.propertyPath + " apunta a algo que no existe");
                    }
                }
            }
            return lista;
        }
    }
}
