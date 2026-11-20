using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1507: los carros y motos revisados en Blender (19-nov) ya vienen en metros. Quita la escala vieja (x4.5 de Tripo) de Visual_T511 y de las llantas,
    /// rearma cada carro con su FBX nuevo (T-1343 / T-1312), pone el rin de policía en V05, V06 y V11 y rearma las piezas de V22 (cristal nuevo).
    /// Las patrullas se rehacen aparte con <c>MechaCyberpunk.Trafico.Editor.PatrullasT1507.Rehacer</c> (corre después).
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CarrosRevisadosT1507.ArmarTodos. Correrlo otra vez deja lo mismo.
    /// </summary>
    public static class CarrosRevisadosT1507
    {
        /// <summary>Vehículos cuyo FBX ya está en metros (sin escala en Unity).</summary>
        public static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V09", "V10", "V11", "C19", "V07", "V08" };

        /// <summary>Carros armados con T-1343 (carrocería, ruedas, puertas, cofre).</summary>
        public static readonly string[] Carros = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V09", "V10", "V11" };

        /// <summary>Carros de policía con rin nuevo (Llanta_Hule, Rin_AceroNegro, Rin_Cromo).</summary>
        public static readonly string[] Policia = { "V05", "V06", "V11" };

        public const string CarpetaMateriales = "Assets/Arte/Vehiculos/Materiales/";
        public const string RutaInforme = "Logs/t1507-carros.txt";

        public static bool EnMetros(string codigo) => Codigos.Contains(codigo);

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1507/Carros revisados en metros")]
        public static void ArmarTodos()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in Codigos.Append("V22"))
            {
                CreadorCarrosBlenderT1312.ConfigurarImportador(codigo);
            }
            AssetDatabase.Refresh();

            foreach (string codigo in Carros)
            {
                CreadorCarrosT1343.ArmarCarroPrefab(codigo, informe);
            }
            ArmarMotoC19(informe);
            CreadorCarrosT1343.ArmarMotoPrefab("V07", "Cuerpo", 0f, informe);
            CreadorCarrosT1343.ArmarMotoPrefab("V08", "Cuerpo", 0f, informe);
            foreach (string codigo in Policia)
            {
                PonerRinPolicia(codigo, informe);
            }
            RearmarPiezasV22(informe);

            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1507:\n" + informe);
        }

        private static void ArmarMotoC19(StringBuilder informe)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(CreadorCarrosBlenderT1312.CodigoMoto));
            try
            {
                informe.AppendLine(CreadorCarrosBlenderT1312.ArmarMoto(raiz));
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(CreadorCarrosBlenderT1312.CodigoMoto));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // V22: el cristal se rehízo en Blender (solo el parabrisas): las piezas fijas y móviles se vuelven a copiar del FBX.
        private static void RearmarPiezasV22(StringBuilder informe)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx("V22"));
            if (fbx == null || !System.IO.File.Exists(RutaPrefab("V22")))
            {
                informe.AppendLine("V22\tFALTA el FBX o el prefab");
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab("V22"));
            try
            {
                Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                CreadorCarrosBlenderT1312.Materiales("V22", out Material carroceria, out _);
                PiezasCarroT1342.ArmarPiezasMoviles(fbx, visual, carroceria, informe);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab("V22"));
                informe.AppendLine("V22\tpiezas (cristal, puertas, cofre) rearmadas con el FBX nuevo");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ rin de policía

        public static Material MaterialPlano(string nombre, Color color, float metalico, float suavidad)
        {
            string ruta = CarpetaMateriales + nombre + ".mat";
            if (!AssetDatabase.IsValidFolder("Assets/Arte/Vehiculos/Materiales"))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Vehiculos", "Materiales");
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Hule, acero negro y cromo, en ese orden.</summary>
        public static Material[] MaterialesRinPolicia()
        {
            return new[]
            {
                MaterialPlano("Llanta_Hule", new Color(0.03f, 0.03f, 0.035f), 0f, 0.15f),
                MaterialPlano("Rin_AceroNegro", new Color(0.06f, 0.06f, 0.07f), 0.8f, 0.55f),
                MaterialPlano("Rin_Cromo", new Color(0.85f, 0.86f, 0.9f), 1f, 0.9f),
            };
        }

        /// <summary>
        /// Orden de materiales de una llanta de 3 submallas: el hule es la de más radio, luego el rin de acero y al centro el cromo
        /// (radio = mayor de las dos medidas que no son el eje, que es la dimensión más angosta de la llanta). Devuelve, por submalla, el índice 0 (hule), 1 (acero) o 2 (cromo).
        /// </summary>
        public static int[] IndicesRin(Mesh malla)
        {
            if (malla == null || malla.subMeshCount != 3)
            {
                return null;
            }
            Vector3 total = malla.bounds.size;
            int eje = total.x <= total.y && total.x <= total.z ? 0 : (total.y <= total.z ? 1 : 2);
            float[] radio = new float[3];
            for (int i = 0; i < 3; i++)
            {
                Vector3 e = malla.GetSubMesh(i).bounds.extents;
                radio[i] = Mathf.Max(eje == 0 ? 0f : e.x, eje == 1 ? 0f : e.y, eje == 2 ? 0f : e.z);
            }
            int[] orden = Enumerable.Range(0, 3).OrderByDescending(i => radio[i]).ThenBy(i => i).ToArray();
            int[] resultado = new int[3];
            for (int posicion = 0; posicion < 3; posicion++)
            {
                resultado[orden[posicion]] = posicion;
            }
            return resultado;
        }

        public static string PonerRinPolicia(string codigo, StringBuilder informe)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            try
            {
                string linea = AplicarRin(raiz, codigo);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
                informe?.AppendLine(linea);
                return linea;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Pone los tres materiales en cada Rueda_xx/Direccion/Giro/Modelo de <paramref name="raiz"/>. Devuelve una línea de informe.</summary>
        public static string AplicarRin(GameObject raiz, string codigo)
        {
            Material[] materiales = MaterialesRinPolicia();
            int cambiadas = 0;
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Transform modelo = raiz.transform.Find(nombre + "/Direccion/Giro/Modelo");
                MeshFilter filtro = modelo != null ? modelo.GetComponent<MeshFilter>() : null;
                MeshRenderer dibujo = modelo != null ? modelo.GetComponent<MeshRenderer>() : null;
                int[] indices = filtro != null ? IndicesRin(filtro.sharedMesh) : null;
                if (dibujo == null || indices == null)
                {
                    continue;
                }
                dibujo.sharedMaterials = indices.Select(i => materiales[i]).ToArray();
                cambiadas++;
            }
            return codigo + "\trin de policía en " + cambiadas + " de 4 llantas";
        }
    }
}
