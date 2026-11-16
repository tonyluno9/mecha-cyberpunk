using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1101: las cajas hechas con KitS2c.MallaCaja (cuerpos de casas y edificios) tenían las 4 caras laterales con el triángulo al revés:
    /// desde afuera no se dibujaban y se veía a través de las paredes. Recorre los assets de malla de Assets/Arte/Mundo, busca mallas de
    /// caja (24 vértices, 12 triángulos, normales sobre los ejes) y voltea el triángulo cuyo normal geométrico no coincide con el normal del
    /// vértice. Las normales no cambian (ya apuntaban hacia afuera). Idempotente: una malla ya correcta no se toca.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CorregirCarasT1101.Corregir
    /// </summary>
    public static class CorregirCarasT1101
    {
        private const string Carpeta = "Assets/Arte";
        private const int VerticesCaja = 24;
        private const int IndicesCaja = 36;
        private const float UmbralEje = 0.999f;

        [MenuItem("Mecha Cyberpunk/S2/T-1101 Corregir caras invertidas de cajas")]
        public static void Corregir()
        {
            int revisadas = 0;
            int corregidas = 0;
            int triangulos = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith(".asset"))
                {
                    continue;
                }
                foreach (Mesh m in AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Mesh>())
                {
                    int n = CorregirMalla(m);
                    revisadas++;
                    if (n > 0)
                    {
                        corregidas++;
                        triangulos += n;
                        EditorUtility.SetDirty(m);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] CorregirCarasT1101: mallas revisadas " + revisadas + ", corregidas " + corregidas + ", triángulos volteados " + triangulos);
        }

        /// <summary>Voltea los triángulos de una caja cuyo frente mira hacia adentro. Devuelve cuántos volteó.</summary>
        public static int CorregirMalla(Mesh m)
        {
            if (m.vertexCount != VerticesCaja || m.subMeshCount != 1)
            {
                return 0;
            }
            int[] t = m.GetTriangles(0);
            if (t.Length != IndicesCaja)
            {
                return 0;
            }
            Vector3[] v = m.vertices;
            Vector3[] nor = m.normals;
            if (nor.Length != VerticesCaja || nor.Any(x => Mathf.Max(Mathf.Abs(x.x), Mathf.Abs(x.y), Mathf.Abs(x.z)) < UmbralEje))
            {
                return 0;
            }
            int volteados = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 geo = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (Vector3.Dot(geo, nor[t[i]]) < 0f)
                {
                    (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
                    volteados++;
                }
            }
            if (volteados > 0)
            {
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
            }
            return volteados;
        }
    }
}
