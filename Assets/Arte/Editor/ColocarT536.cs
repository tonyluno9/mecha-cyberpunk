using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-536 (plan de la noche 3-nov, paso 1: puertas). Idempotente sobre Ciudad_Prologo:
    /// - Puertas a escala humana: el hueco de 3 x 3.2 m de la casa, el bar y el clandestino queda de 1.6 x 2.4 m con jambas y dintel del
    ///   mismo muro, y la hoja a esa medida (El Mercado y el Casino tienen su portón y su entrada de modelo, se quedan).
    /// - Sin "pared negra" al cruzar: una luz cálida en el pasillo entre la fachada y el local, para que se vea desde la calle.
    /// (La hoja se abre sola al acercarte: PuertaLocal, T-536.)
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT536.Colocar
    /// </summary>
    public static class ColocarT536
    {
        public const float AnchoPuerta = 1.6f;
        public const float AltoPuerta = 2.4f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreMarco = "Marco_T536";
        private const string NombreLuz = "LuzPasillo_T536";
        private static readonly string[] ConMarco = { "Lugar_Casa", "Lugar_Bar", "Lugar_Clandestino", "Lugar_Casino" };
        // El Casino: su entrada de modelo (E01) traía la puerta cerrada dentro de la malla y tapaba el interior: se quita y queda un marco dorado más ancho.
        private const float AnchoCasino = 2.6f;
        private const float AltoCasino = 2.8f;
        public static readonly Color ColorPasillo = new Color(1f, 0.78f, 0.55f);

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-536/Puertas")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (MonoBehaviour puerta in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.GetType().Name == "PuertaLocal"))
            {
                Arreglar(puerta.transform);
            }
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t536-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-536 listo:\n" + Informe);
        }

        private static void Arreglar(Transform puerta)
        {
            Transform lugar = puerta.parent;
            Transform hoja = puerta.Find("Hoja");
            if (lugar == null || hoja == null)
            {
                return;
            }
            Vector3 adentro = lugar.position - puerta.position;
            adentro.y = 0f;
            adentro = adentro.sqrMagnitude > 0.01f ? adentro.normalized : -puerta.forward;
            float piso = hoja.GetComponent<Renderer>().bounds.min.y;

            foreach (Transform viejo in puerta.Cast<Transform>().Where(t => t.name == NombreMarco || t.name == NombreLuz).ToList())
            {
                Object.DestroyImmediate(viejo.gameObject);
            }

            // Luz del pasillo: medio metro adentro del hueco hasta el local, a 2.6 m.
            GameObject luz = new GameObject(NombreLuz);
            luz.transform.SetParent(puerta, false);
            luz.transform.position = puerta.position + adentro * 2f + Vector3.up * 2.6f;
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = ColorPasillo;
            l.intensity = 3f;
            l.range = 6f;
            l.shadows = LightShadows.None;

            if (!ConMarco.Contains(lugar.name))
            {
                Informe.AppendLine(lugar.name + ": luz del pasillo (su entrada de modelo se queda)");
                return;
            }
            // El hueco viejo mide lo que la hoja original (3 x 3.2): jambas y dintel lo cierran hasta 1.6 x 2.4.
            Vector3 escalaVieja = new Vector3(3f, 3.2f, hoja.localScale.z);
            bool casino = lugar.name == "Lugar_Casino";
            float ancho = casino ? AnchoCasino : AnchoPuerta;
            float alto = casino ? AltoCasino : AltoPuerta;
            if (casino)
            {
                foreach (Transform e01 in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("E01_EntradaCasino")))
                {
                    e01.gameObject.SetActive(false);
                }
            }
            Vector3 lado = hoja.right;
            Material dorado = casino ? Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.name == "T506_CasinoInt_Dorado") : null;
            Material muro = dorado != null ? dorado : lugar.Cast<Transform>().Select(t => t.GetComponent<Renderer>()).FirstOrDefault(r => r != null && (r.name.StartsWith("Fachada") || r.name == "Dintel"))?.sharedMaterial
                ?? hoja.GetComponent<Renderer>().sharedMaterial;
            Transform marco = new GameObject(NombreMarco).transform;
            marco.SetParent(puerta, false);
            float jamba = (escalaVieja.x - ancho) * 0.5f;
            Vector3 centroHueco = new Vector3(hoja.position.x, piso, hoja.position.z);
            Pieza(marco, "Jamba", muro, centroHueco + lado * (ancho * 0.5f + jamba * 0.5f) + Vector3.up * (escalaVieja.y * 0.5f), hoja.rotation, new Vector3(jamba, escalaVieja.y, escalaVieja.z + 0.1f));
            Pieza(marco, "Jamba", muro, centroHueco - lado * (ancho * 0.5f + jamba * 0.5f) + Vector3.up * (escalaVieja.y * 0.5f), hoja.rotation, new Vector3(jamba, escalaVieja.y, escalaVieja.z + 0.1f));
            Pieza(marco, "Dintel", muro, centroHueco + Vector3.up * ((alto + escalaVieja.y) * 0.5f), hoja.rotation, new Vector3(ancho, escalaVieja.y - alto, escalaVieja.z + 0.1f));
            hoja.localScale = new Vector3(ancho, alto, hoja.localScale.z);
            hoja.position = new Vector3(hoja.position.x, piso + alto * 0.5f, hoja.position.z);
            Informe.AppendLine(lugar.name + ": puerta de " + ancho + " x " + alto + " m con jambas y dintel, y luz del pasillo" + (casino ? " (sin la entrada E01 que tapaba)" : ""));
        }

        private static void Pieza(Transform padre, string nombre, Material m, Vector3 centro, Quaternion giro, Vector3 tam)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(centro, giro);
            go.transform.localScale = tam;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            go.isStatic = true;
        }
    }
}
