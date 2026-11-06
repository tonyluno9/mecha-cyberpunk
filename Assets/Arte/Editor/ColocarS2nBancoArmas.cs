using System.Linq;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Editor;
using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-814 (playtest del 6-nov): el banco de trabajo de Tino (CIC) abre el menú de mejora de armas con holograma. Pone un <see cref="BancoArmas"/>
    /// sobre el primer T03_BancoTrabajo del taller (S2_Interiores/TallerTino, x -77.3..-69.3, z 10.2..16), le quita a EL INVENTOR la tienda vieja
    /// (dice "Acércate al banco de trabajo para mejorar tu arma.") y apaga el MostradorCIC (el objeto se queda). Antes mete las imágenes H01-H04 y
    /// rehace la config del menú. Idempotente: rehace su raíz S2n_BancoArmas.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nBancoArmas.Colocar
    /// (Solo datos, sin abrir la escena: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nBancoArmas.PrepararDatos)
    /// </summary>
    public static class ColocarS2nBancoArmas
    {
        public const string NombreRaiz = "S2n_BancoArmas";
        public const string RutaTaller = "S2_Interiores/TallerTino";
        public const string NombreBanco = "T03_BancoTrabajo";
        public const string NombreMostradorCic = "MostradorCIC";
        public const string RutaInventor = "Assets/_Proyecto/Progresion/Apoyo/Datos/DatosPersonajeApoyo_inventor.asset";
        public const string FraseBanco = "Acércate al banco de trabajo para mejorar tu arma.";
        /// <summary>Centro del banco de T-811 (ColocarS2Interiores.TallerTino), por si el modelo no está.</summary>
        public static readonly Vector3 CentroBanco = new Vector3(-72.55f, 0.22f, 13.4f);
        /// <summary>El punto de uso queda frente al banco, hacia la calle (-Z), a esta distancia del borde (m).</summary>
        private const float FrenteBanco = 0.45f;

        [MenuItem("Mecha Cyberpunk/S2n/Banco de armas de Tino (T-814)")]
        public static void Colocar()
        {
            PrepararDatos();
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(CreadorMenuArmas.RutaConfig);

            Transform banco = BuscarBanco();
            Bounds limites = banco != null ? ArteT250.Limites(banco.gameObject) : new Bounds(CentroBanco + Vector3.up * 0.45f, new Vector3(1.4f, 0.9f, 0.7f));
            GameObject raiz = new GameObject(NombreRaiz);
            raiz.transform.position = new Vector3(limites.center.x, limites.max.y, limites.center.z);
            Transform punto = new GameObject("PuntoUso").transform;
            punto.SetParent(raiz.transform, false);
            punto.position = new Vector3(limites.center.x, limites.min.y, limites.min.z - FrenteBanco);
            BancoArmas componente = raiz.AddComponent<BancoArmas>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("puntoUso").objectReferenceValue = punto;
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Banco de armas en " + raiz.transform.position.ToString("F2") + (banco != null ? " sobre " + ColocarS2.Ruta(banco) : " (FALTA el banco: posición fija)"));

            // EL INVENTOR deja de abrir la tienda: te manda al banco.
            foreach (PersonajeApoyo p in ColocarS2.Todos().Select(t => t.GetComponent<PersonajeApoyo>()).Where(p => p != null))
            {
                if (p.Datos != null && p.Datos.Id == "inventor")
                {
                    SerializedObject sp = new SerializedObject(p);
                    sp.FindProperty("mostrador").objectReferenceValue = null;
                    sp.ApplyModifiedPropertiesWithoutUndo();
                    ColocarS2.Informe.AppendLine("Tino sin tienda: " + ColocarS2.Ruta(p.transform));
                }
            }
            foreach (MostradorTienda m in ColocarS2.Todos().Select(t => t.GetComponent<MostradorTienda>()).Where(m => m != null && m.name == NombreMostradorCic))
            {
                m.enabled = false;
                EditorUtility.SetDirty(m);
                ColocarS2.Informe.AppendLine("Mostrador del CIC apagado: " + ColocarS2.Ruta(m.transform));
            }
            ColocarS2.Guardar("s2n-banco-armas");
        }

        /// <summary>Lo que no toca la escena: las imágenes H01-H04, la config del menú y la frase de Tino.</summary>
        public static void PrepararDatos()
        {
            ImportarS2nHologramas.Importar();
            CreadorMenuArmas.Crear();
            DatosPersonajeApoyo inventor = AssetDatabase.LoadAssetAtPath<DatosPersonajeApoyo>(RutaInventor);
            if (inventor != null && (inventor.Frases.Length == 0 || inventor.Frases[0] != FraseBanco))
            {
                SerializedObject so = new SerializedObject(inventor);
                SerializedProperty frases = so.FindProperty("frases");
                frases.InsertArrayElementAtIndex(0);
                frases.GetArrayElementAtIndex(0).stringValue = FraseBanco;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        private static Transform BuscarBanco()
        {
            Transform taller = ColocarS2.Buscar(RutaTaller);
            if (taller == null)
            {
                return null;
            }
            return taller.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == NombreBanco)
                .OrderBy(t => Vector3.Distance(t.position, CentroBanco))
                .FirstOrDefault();
        }
    }
}
