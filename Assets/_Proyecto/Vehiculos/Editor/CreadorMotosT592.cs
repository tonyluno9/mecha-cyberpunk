using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-592: prepara los prefabs de moto (V07, V08, C19): ConfigCarro_Moto (ligera, ágil, frena con inercia), el componente ManejoMoto
    /// (inclinación en curvas) y una copia de la malla sin el caballete (los triángulos de su zona, por moto).
    /// CreadorCarrosT511.CrearUno lo llama al regenerar una moto. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorMotosT592.Crear
    /// </summary>
    public static class CreadorMotosT592
    {
        public const string RutaConfig = "Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Moto.asset";
        public const string RutaConfigReparto = "Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_MotoReparto.asset";
        public const string CarpetaMallas = "Assets/_Proyecto/Vehiculos/Datos/";
        public static readonly string[] Motos = { "V07", "V08", "C19" };


        [MenuItem("Mecha Cyberpunk/T-592/Preparar motos")]
        public static void Crear()
        {
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in Motos)
            {
                informe.AppendLine(Preparar(codigo));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t592-motos.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-592:\n" + informe);
        }

        public static ConfigCarro CrearConfig()
        {
            ConfigCarro existente = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfig);
            if (existente == null)
            {
                AssetDatabase.CopyAsset("Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Viejo.asset", RutaConfig);
                existente = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfig);
            }
            SerializedObject so = new SerializedObject(existente);
            // Una moto de unos 230 kg. T-695: arranca a ~7.8 m/s² (antes 11), llega a ~26 m/s, gira cerrado y frena casi de golpe (28 m/s², antes 6.5).
            Fijar(so, "masa", 230f);
            Fijar(so, "alturaCentroMasa", 0.4f);
            Fijar(so, "fuerzaAntivuelco", 1400f);
            Fijar(so, "velocidadMaxima", 26f);
            Fijar(so, "velocidadReversa", 3f);
            Fijar(so, "fuerzaMotor", 1800f);
            Fijar(so, "fuerzaReversa", 600f);
            Fijar(so, "fuerzaFreno", 6400f);
            Fijar(so, "fuerzaFrenoMano", 3000f);
            Fijar(so, "resistenciaRodadura", 140f);
            Fijar(so, "gananciaEstacionamiento", 40f);
            Fijar(so, "anguloDireccionParado", 40f);
            Fijar(so, "anguloDireccionMaximo", 16f);
            Fijar(so, "exponenteDireccion", 0.6f);
            Fijar(so, "distanciaEntreEjes", 1.4f);
            Fijar(so, "aceleracionLateralMaxima", 11f);
            Fijar(so, "agarreLateral", 11f);
            Fijar(so, "respuestaGiro", 10f);
            Fijar(so, "rigidez", 3800f);
            Fijar(so, "amortiguamiento", 800f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existente);
            return existente;
        }

        /// <summary>T-695: la moto de reparto (V07) es más lenta que las otras: menos motor (5.4 m/s²) y tope de 18 m/s. El giro y el frenado son los de la moto.</summary>
        public static ConfigCarro CrearConfigReparto()
        {
            ConfigCarro baseMoto = CrearConfig();
            AssetDatabase.SaveAssets();
            ConfigCarro reparto = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfigReparto);
            if (reparto == null)
            {
                AssetDatabase.CopyAsset(RutaConfig, RutaConfigReparto);
                reparto = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfigReparto);
            }
            SerializedObject so = new SerializedObject(reparto);
            foreach (SerializedProperty p in new[] { "masa", "alturaCentroMasa", "fuerzaAntivuelco", "velocidadReversa", "fuerzaReversa", "fuerzaFreno", "fuerzaFrenoMano",
                "resistenciaRodadura", "gananciaEstacionamiento", "anguloDireccionParado", "anguloDireccionMaximo", "exponenteDireccion", "distanciaEntreEjes",
                "aceleracionLateralMaxima", "agarreLateral", "respuestaGiro", "rigidez", "amortiguamiento" }.Select(n => so.FindProperty(n)))
            {
                p.floatValue = new SerializedObject(baseMoto).FindProperty(p.name).floatValue;
            }
            Fijar(so, "velocidadMaxima", 18f);
            Fijar(so, "fuerzaMotor", 1250f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reparto);
            return reparto;
        }

        private static void Fijar(SerializedObject so, string nombre, float valor)
        {
            so.FindProperty(nombre).floatValue = valor;
        }

        public static string Preparar(string codigo)
        {
            string ruta = CreadorCarrosT511.RutaPrefab(codigo);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                return codigo + "\tFALTA el prefab";
            }
            ConfigCarro config = codigo == "V07" ? CrearConfigReparto() : CrearConfig();
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            string resultado;
            try
            {
                SerializedObject soCarro = new SerializedObject(raiz.GetComponent<CarroViejo>());
                soCarro.FindProperty("config").objectReferenceValue = config;
                soCarro.ApplyModifiedPropertiesWithoutUndo();

                Transform envoltorio = raiz.transform.Find("Visual_T511");
                MeshFilter filtro = envoltorio.Find("Visual").GetComponent<MeshFilter>();
                Mesh completa = filtro.sharedMesh;
                // Si el filtro ya apunta a la copia (prefab preparado antes), se parte de la malla completa guardada en ManejoMoto.
                ManejoMoto previo = raiz.GetComponent<ManejoMoto>();
                if (previo != null)
                {
                    SerializedObject soPrevio = new SerializedObject(previo);
                    Mesh guardada = soPrevio.FindProperty("mallaCompleta").objectReferenceValue as Mesh;
                    completa = guardada != null ? guardada : completa;
                }
                filtro.sharedMesh = completa;
                Mesh recortada = AssetDatabase.LoadAssetAtPath<Mesh>(CarpetaMallas + "MallaMoto_" + codigo + "_SinCaballete.asset");
                float bajar = 0f;
                string estado;
                Matrix4x4 aPrefab = raiz.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;
                Mesh nueva = QuitarCaballete(completa, aPrefab, ZonaCaballete(codigo), out float alturaCaballete, out int triangulos);
                if (nueva == null)
                {
                    estado = "sin caballete detectado";
                }
                else
                {
                    nueva.name = "MallaMoto_" + codigo + "_SinCaballete";
                    if (recortada == null)
                    {
                        AssetDatabase.CreateAsset(nueva, CarpetaMallas + nueva.name + ".asset");
                        recortada = nueva;
                    }
                    else
                    {
                        EditorUtility.CopySerialized(nueva, recortada);
                    }
                    bajar = alturaCaballete;
                    estado = "caballete quitado: " + triangulos + " triángulos, baja " + bajar.ToString("F3") + " m";
                }

                ManejoMoto manejo = previo != null ? previo : raiz.AddComponent<ManejoMoto>();
                SerializedObject soMoto = new SerializedObject(manejo);
                soMoto.FindProperty("envoltorioVisual").objectReferenceValue = envoltorio;
                soMoto.FindProperty("filtroModelo").objectReferenceValue = filtro;
                soMoto.FindProperty("mallaCompleta").objectReferenceValue = completa;
                soMoto.FindProperty("mallaSinCaballete").objectReferenceValue = recortada;
                soMoto.FindProperty("bajarAlOcultar").floatValue = bajar;
                soMoto.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                resultado = codigo + "\t" + estado;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return resultado;
        }

        /// <summary>
        /// Zona del caballete de cada moto en el espacio del prefab: el modelo es una sola malla soldada, así que se quita lo que cae por
        /// completo dentro de esta caja baja (medida con las celdas de vértices de cada modelo; se revisa en la captura de T-592).
        /// </summary>
        public static Bounds ZonaCaballete(string codigo)
        {
            switch (codigo)
            {
                case "V07":
                    return CajaDesdeLimites(-0.6f, 0.6f, -0.05f, 0.31f, -0.31f, -0.09f);
                case "V08":
                    return CajaDesdeLimites(-0.6f, 0.6f, -0.05f, 0.31f, -0.21f, 0.01f);
                default:
                    return CajaDesdeLimites(-0.6f, 0.6f, -0.05f, 0.31f, -0.31f, 0f);
            }
        }

        private static Bounds CajaDesdeLimites(float x0, float x1, float y0, float y1, float z0, float z1)
        {
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
            return b;
        }

        /// <summary>
        /// Copia de la malla sin los triángulos que caen por completo dentro de <paramref name="zona"/> (espacio del prefab).
        /// <paramref name="bajar"/> es cuánto sube el punto más bajo del modelo al quitarlos. Null si no quita nada.
        /// </summary>
        public static Mesh QuitarCaballete(Mesh malla, Matrix4x4 aEspacioPrefab, Bounds zona, out float bajar, out int triangulosQuitados)
        {
            bajar = 0f;
            triangulosQuitados = 0;
            Vector3[] vertices = malla.vertices;
            int[] indices = malla.triangles;
            List<int> conservados = new List<int>();
            float minAntes = float.MaxValue;
            float minDespues = float.MaxValue;
            for (int t = 0; t < indices.Length; t += 3)
            {
                bool dentro = true;
                float minTriangulo = float.MaxValue;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = aEspacioPrefab.MultiplyPoint3x4(vertices[indices[t + k]]);
                    dentro &= zona.Contains(p);
                    minTriangulo = Mathf.Min(minTriangulo, p.y);
                }
                minAntes = Mathf.Min(minAntes, minTriangulo);
                if (dentro)
                {
                    triangulosQuitados++;
                    continue;
                }
                minDespues = Mathf.Min(minDespues, minTriangulo);
                conservados.Add(indices[t]);
                conservados.Add(indices[t + 1]);
                conservados.Add(indices[t + 2]);
            }
            if (triangulosQuitados == 0)
            {
                return null;
            }
            Mesh copia = Object.Instantiate(malla);
            copia.triangles = conservados.ToArray();
            copia.RecalculateBounds();
            bajar = Mathf.Max(0f, minDespues - minAntes);
            return copia;
        }
    }
}
