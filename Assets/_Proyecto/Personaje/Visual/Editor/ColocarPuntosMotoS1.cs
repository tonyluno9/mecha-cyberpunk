using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// S1 (T-660): pone en cada moto (Carro_V07, Carro_V08, Carro_C19) los puntos donde se apoya el conductor: asiento, manubrio y estribos
    /// (<see cref="PuntosMoto"/>). Los puntos cuelgan del visual de la moto (el que se inclina en las curvas) para que el conductor se incline con ella.
    /// Se miden como fracciones de las medidas del modelo (largo, alto) y se ajustan por moto mirando la captura. Idempotente: si CreadorCarrosT511
    /// regenera una moto, se vuelve a correr.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ColocarPuntosMotoS1.Colocar
    /// </summary>
    public static class ColocarPuntosMotoS1
    {
        private const string CarpetaPrefabs = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_";
        private const string NombreVisual = "Visual_T511";
        private static readonly string[] Prefijos = { "Punto_Asiento", "Punto_ManubrioIzq", "Punto_ManubrioDer", "Punto_EstriboIzq", "Punto_EstriboDer" };

        /// <summary>Fracciones del modelo (z de atrás a adelante, y de abajo a arriba) y medidas fijas (m) de cada moto.</summary>
        private struct Medidas
        {
            public float AsientoZ, AsientoY, ManubrioZ, ManubrioY, ManubrioX, EstriboZ, EstriboY, EstriboX, Torso;
        }

        private static Medidas Para(string codigo)
        {
            // Medidas de una moto normal; cada modelo se ajusta con la captura s1-moto-<código>-lado.png.
            Medidas m = new Medidas
            {
                AsientoZ = 0.42f, AsientoY = 0.62f,
                ManubrioZ = 0.68f, ManubrioY = 0.88f, ManubrioX = 0.28f,
                EstriboZ = 0.48f, EstriboY = 0.2f, EstriboX = 0.22f,
                Torso = 20f,
            };
            if (codigo == "V07")
            {
                // Moto de reparto con caja: se maneja casi derecho.
                m.AsientoZ = 0.38f;
                m.Torso = 12f;
            }
            else if (codigo == "C19")
            {
                m.Torso = 22f;
            }
            return m;
        }

        [MenuItem("Mecha Cyberpunk/S1/Poner puntos de conductor en las motos")]
        public static void Colocar()
        {
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in new[] { "V07", "V08", "C19" })
            {
                informe.AppendLine(Poner(codigo));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s1-puntos-moto.txt", informe.ToString());
            Debug.Log("[Personaje] S1 puntos de moto:\n" + informe);
        }

        public static string Poner(string codigo)
        {
            string ruta = CarpetaPrefabs + codigo + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                return codigo + "\tFALTA el prefab";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform visual = raiz.transform.Find(NombreVisual);
                if (visual == null)
                {
                    return codigo + "\tFALTA " + NombreVisual;
                }
                foreach (string nombre in Prefijos)
                {
                    Transform viejo = visual.Find(nombre);
                    if (viejo != null)
                    {
                        Object.DestroyImmediate(viejo.gameObject);
                    }
                }
                Bounds caja = Limites(raiz.transform, visual);
                Medidas m = Para(codigo);
                float x = caja.center.x;
                float z0 = caja.min.z;
                float y0 = caja.min.y;
                float largo = caja.size.z;
                float alto = caja.size.y;
                Transform asiento = Punto(raiz.transform, visual, Prefijos[0], new Vector3(x, y0 + alto * m.AsientoY, z0 + largo * m.AsientoZ));
                Transform manoI = Punto(raiz.transform, visual, Prefijos[1], new Vector3(x - m.ManubrioX, y0 + alto * m.ManubrioY, z0 + largo * m.ManubrioZ));
                Transform manoD = Punto(raiz.transform, visual, Prefijos[2], new Vector3(x + m.ManubrioX, y0 + alto * m.ManubrioY, z0 + largo * m.ManubrioZ));
                Transform pieI = Punto(raiz.transform, visual, Prefijos[3], new Vector3(x - m.EstriboX, y0 + alto * m.EstriboY, z0 + largo * m.EstriboZ));
                Transform pieD = Punto(raiz.transform, visual, Prefijos[4], new Vector3(x + m.EstriboX, y0 + alto * m.EstriboY, z0 + largo * m.EstriboZ));

                PuntosMoto puntos = raiz.GetComponent<PuntosMoto>();
                if (puntos == null)
                {
                    puntos = raiz.AddComponent<PuntosMoto>();
                }
                SerializedObject so = new SerializedObject(puntos);
                so.FindProperty("asiento").objectReferenceValue = asiento;
                so.FindProperty("manubrioIzquierdo").objectReferenceValue = manoI;
                so.FindProperty("manubrioDerecho").objectReferenceValue = manoD;
                so.FindProperty("estriboIzquierdo").objectReferenceValue = pieI;
                so.FindProperty("estriboDerecho").objectReferenceValue = pieD;
                so.FindProperty("inclinacionTorso").floatValue = m.Torso;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return string.Format("{0}\tlargo {1:F2}\talto {2:F2}\tasiento {3}", codigo, largo, alto, asiento.localPosition.ToString("F2"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // El punto se pone por su posición en el espacio de la moto (frente = +Z) y queda hijo del visual.
        private static Transform Punto(Transform raiz, Transform visual, string nombre, Vector3 enLaMoto)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(visual, false);
            objeto.transform.position = raiz.TransformPoint(enLaMoto);
            objeto.transform.rotation = raiz.rotation;
            return objeto.transform;
        }

        // Caja del modelo en el espacio de la moto (sin los puntos que se están creando).
        private static Bounds Limites(Transform raiz, Transform visual)
        {
            bool hay = false;
            Bounds total = new Bounds();
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = raiz.InverseTransformPoint(esquina);
                    if (!hay)
                    {
                        total = new Bounds(local, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        total.Encapsulate(local);
                    }
                }
            }
            return total;
        }
    }
}
