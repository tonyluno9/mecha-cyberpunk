using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-409 del creador de la ciudad de pandillas: el minimapa (canvas de orden 25) y las cuatro marcas de lugar (El Mercado, I+D+I, el CIC
    /// y la casa) bajo GTA_T409, cada una en el punto de su lugar. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.T409
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT409 = "GTA_T409";

        private struct LugarT409
        {
            public string Objeto;
            public TipoMarcaMapa Tipo;
        }

        private static readonly LugarT409[] LugaresT409 =
        {
            new LugarT409 { Objeto = "Lugar_MercadoNegro", Tipo = TipoMarcaMapa.Mercado },
            new LugarT409 { Objeto = "Clinica_Camilla", Tipo = TipoMarcaMapa.Clinica },
            new LugarT409 { Objeto = "Lugar_CIC", Tipo = TipoMarcaMapa.Cic },
            new LugarT409 { Objeto = IdAparicion, Tipo = TipoMarcaMapa.Casa },
        };

        [MenuItem("Mecha Cyberpunk/Pandillas/T-409 Minimapa")]
        public static void T409()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T409Interno();
            Guardar(escena);
        }

        private static void T409Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT409);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT409);
            raiz.AddComponent<Minimapa>();
            int puestas = 0;
            foreach (LugarT409 lugar in LugaresT409)
            {
                Transform punto = BuscarTransform(lugar.Objeto);
                if (punto == null)
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T409: no está " + lugar.Objeto + "; su icono no sale en el mapa.");
                    continue;
                }
                GameObject marca = new GameObject("Marca_" + lugar.Tipo);
                marca.transform.SetParent(raiz.transform, false);
                marca.transform.position = punto.position;
                SerializedObject so = new SerializedObject(marca.AddComponent<MarcaLugar>());
                so.FindProperty("tipo").enumValueIndex = (int)lugar.Tipo;
                so.ApplyModifiedPropertiesWithoutUndo();
                puestas++;
            }
            Debug.Log("[Progresion] CreadorCiudadPandillas.T409: minimapa y " + puestas + " marcas de lugar.");
        }
    }
}
