using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-923 (playtest 9-nov, sesión B): interiores de la casa y del Doc. Cada paso es repetible y toca lo mínimo de Ciudad_Prologo:
    ///  1. I+D+I del Doc: el muro viejo MuroFondo (textura vieja T506_CasinoInt_Muro) tenía su cara de dentro en el mismo plano que el forro
    ///     nuevo (z -15.90): las dos capas peleaban (rayas moradas y se veía lo de atrás). Se corre 4 cm hacia fuera.
    ///  2. Cuarto: se quitan los pósters con textura rota y la puerta pasa del modelo L02 al P10 (la hoja con su collider y HojaPuerta no se tocan).
    ///  3. Entrada de la casa desde dentro: el marco viejo Marco_T536 (material liso T506_Barrio_Casa) se veía como un panel naranja plano
    ///     alrededor de la reja; se apaga su dibujo (los colliders se quedan) y se ve la fachada P11.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT923.Aplicar
    /// </summary>
    public static class ColocarT923
    {
        public const string RutaIdi = "T531_Locales/IDI";
        public const string RutaCuarto = "T550_Cuartos/Cuarto_Habitacion";
        public const string RutaP10 = "Assets/Arte/Exteriores/Prefabs/P10.prefab";
        public const string NombreEntrada = "T923_EntradaInterior";
        public const float ZMuroFondoIdi = -15.99f;

        [MenuItem("Mecha Cyberpunk/T923/Interiores de la casa y el Doc")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            MuroIdi();
            Cuarto();
            EntradaCasa();
            ColocarS2.Guardar("t923-interiores");
        }

        private static void MuroIdi()
        {
            Transform fondo = ColocarS2.Buscar(RutaIdi + "/MuroFondo");
            if (fondo == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaIdi + "/MuroFondo");
                return;
            }
            fondo.position = new Vector3(fondo.position.x, fondo.position.y, ZMuroFondoIdi);
            ColocarS2.Informe.AppendLine("I+D+I: MuroFondo en z " + ZMuroFondoIdi);
        }

        public static void QuitarPosters(Transform cuarto)
        {
            int n = 0;
            foreach (Transform p in cuarto.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Poster")).ToArray())
            {
                if (p != null)
                {
                    Object.DestroyImmediate(p.gameObject);
                    n++;
                }
            }
            ColocarS2.Informe.AppendLine("Cuarto: " + n + " pósters quitados");
        }

        private static void Cuarto()
        {
            Transform cuarto = ColocarS2.Buscar(RutaCuarto);
            if (cuarto == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaCuarto);
                return;
            }
            QuitarPosters(cuarto);
            Transform hoja = cuarto.Find("Puerta_Cuarto/Hoja");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaP10);
            if (hoja == null || prefab == null)
            {
                ColocarS2.Informe.AppendLine("FALTA la hoja o " + RutaP10);
                return;
            }
            Transform viejo = hoja.Find("Puerta_L02");
            Transform previo = hoja.Find("Puerta_P10");
            BoxCollider cj = hoja.GetComponent<BoxCollider>();
            Vector3 tam = cj != null ? cj.size : new Vector3(1f, 2.15f, 0.12f);
            float ancho = tam.x, alto = tam.y;
            if (viejo != null)
            {
                Bounds vb = ArteT250.Limites(viejo.gameObject);
                ancho = vb.size.x;
                alto = vb.size.y;
                Object.DestroyImmediate(viejo.gameObject);
            }
            if (previo != null)
            {
                Object.DestroyImmediate(previo.gameObject);
            }
            GameObject p10 = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hoja);
            p10.name = "Puerta_P10";
            foreach (Collider c in p10.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }
            p10.transform.rotation = Quaternion.identity;
            Bounds b = ArteT250.Limites(p10);
            // El lado largo en horizontal va a lo ancho del vano (x): la pared del cuarto corre de este a oeste.
            if (b.size.z > b.size.x)
            {
                p10.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                b = ArteT250.Limites(p10);
            }
            p10.transform.localScale *= alto / Mathf.Max(0.01f, b.size.y);
            b = ArteT250.Limites(p10);
            if (b.size.x > ancho)
            {
                Vector3 s = p10.transform.localScale;
                p10.transform.localScale = new Vector3(s.x * ancho / b.size.x, s.y, s.z);
                b = ArteT250.Limites(p10);
            }
            // Centrada en la hoja y con los pies en el piso (el centro de la hoja está a media altura).
            p10.transform.position += new Vector3(hoja.position.x - b.center.x, (hoja.position.y - tam.y * 0.5f) - b.min.y, hoja.position.z - b.center.z);
            ColocarS2.Informe.AppendLine("Cuarto: puerta P10 " + ArteT250.Limites(p10).size.ToString("F2") + " en la hoja " + tam.ToString("F2"));
        }

        private static void EntradaCasa()
        {
            Transform cuarto = ColocarS2.Buscar(RutaCuarto);
            // Versiones anteriores del paso ponían un muro de respaldo (T923_EntradaInterior): estorbaba a la E de la puerta y no se veía.
            Object.DestroyImmediate(GameObject.Find(NombreEntrada));
            // El marco viejo de la puerta (T-536, material T506_Barrio_Casa liso) se veía por dentro como un panel naranja plano: se apaga su
            // dibujo (los colliders se quedan) y queda la fachada P11.
            Transform marco = ColocarS2.Buscar("Barrio/Manzana_4/Lugar_Casa/Puerta/Marco_T536");
            if (marco != null)
            {
                foreach (Renderer rm in marco.GetComponentsInChildren<Renderer>(true))
                {
                    rm.enabled = false;
                }
            }
            ColocarS2.Informe.AppendLine("Entrada de la casa: marco viejo sin dibujar");
        }
    }
}
