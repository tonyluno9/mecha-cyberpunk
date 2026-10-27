using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// T-170: materiales por rama y de franja, ConfigAspectoPiezas_Base, las 4 franjas de cada antebrazo y el componente AspectoPiezas
    /// en MechaJugador.prefab (por herencia llega a MechaEstacionado). Idempotente: no pisa lo que ya existe.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorAspectoPiezas.Crear
    /// </summary>
    public static class CreadorAspectoPiezas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaMateriales = "Assets/_Proyecto/Mecha/Materiales/";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigAspectoPiezas_Base.asset";
        private const string RutaMarcoDeSerie = "Assets/_Proyecto/Mecha/Materiales/Cabina_Estructura.mat";
        private const string RutaCabina = "Sacudida/Cabina";
        private const int NumeroFranjas = 4;

        // Franjas: cuatro bandas finas sobre el antebrazo (escala local del Antebrazo: 0.09 x 0.09 x 0.26 m).
        private const float AnchoFranja = 1.15f;
        private const float GrosorFranjaMetros = 0.018f;
        private const float LargoAntebrazo = 0.26f;
        private const float SeparacionFranjaMetros = 0.055f;
        private const float InicioFranjaMetros = -0.085f;

        [MenuItem("Mecha Cyberpunk/Crear T-170 aspecto de piezas")]
        public static void Crear()
        {
            Material deSerie = CrearMaterial("Pieza_DeSerie", new Color(0.38f, 0.39f, 0.42f), null);
            Material militar = CrearMaterial("Pieza_Militar", new Color(0.25f, 0.42f, 0.62f), null);
            Material tech = CrearMaterial("Pieza_Tech", new Color(0f, 0.75f, 0.68f), null);
            Material cuerpoACuerpo = CrearMaterial("Pieza_CuerpoACuerpo", new Color(0.95f, 0.45f, 0.05f), null);
            Material fuego = CrearMaterial("Pieza_Fuego", new Color(0.9f, 0.1f, 0.6f), null);
            Material franja = CrearMaterial("Pieza_Franja", new Color(0.95f, 0.97f, 1f), new Color(0.9f, 0.95f, 1f) * 1.2f);
            Material marco = AssetDatabase.LoadAssetAtPath<Material>(RutaMarcoDeSerie);
            if (marco == null)
            {
                Debug.LogError("[Mecha] Falta " + RutaMarcoDeSerie + " (corre CreadorCabina antes).");
                return;
            }

            ConfigAspectoPiezas config = AssetDatabase.LoadAssetAtPath<ConfigAspectoPiezas>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigAspectoPiezas>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                var soConfig = new SerializedObject(config);
                soConfig.FindProperty("deSerie").objectReferenceValue = deSerie;
                soConfig.FindProperty("militar").objectReferenceValue = militar;
                soConfig.FindProperty("tech").objectReferenceValue = tech;
                soConfig.FindProperty("cuerpoACuerpo").objectReferenceValue = cuerpoACuerpo;
                soConfig.FindProperty("fuego").objectReferenceValue = fuego;
                soConfig.FindProperty("franja").objectReferenceValue = franja;
                soConfig.FindProperty("marcoDeSerie").objectReferenceValue = marco;
                soConfig.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                bool cambio = false;
                Transform cabina = raiz.transform.Find(RutaCabina);
                Renderer[] franjasIzq = ArmarFranjas(cabina.Find("BrazoIzquierdo/Antebrazo"), franja, ref cambio);
                Renderer[] franjasDer = ArmarFranjas(cabina.Find("BrazoDerecho/Antebrazo"), franja, ref cambio);

                AspectoPiezas aspecto = raiz.GetComponent<AspectoPiezas>();
                if (aspecto == null)
                {
                    aspecto = raiz.AddComponent<AspectoPiezas>();
                    cambio = true;
                }
                var so = new SerializedObject(aspecto);
                Poner(so, "config", config);
                PonerLista(so, "partesBrazoIzquierdo", Partes(cabina.Find("BrazoIzquierdo")));
                PonerLista(so, "franjasBrazoIzquierdo", franjasIzq);
                PonerLista(so, "partesBrazoDerecho", Partes(cabina.Find("BrazoDerecho")));
                PonerLista(so, "franjasBrazoDerecho", franjasDer);
                PonerLista(so, "marcoCabina", new[]
                {
                    cabina.Find("PosteIzquierdo").GetComponent<Renderer>(),
                    cabina.Find("PosteDerecho").GetComponent<Renderer>(),
                    cabina.Find("VigaSuperior").GetComponent<Renderer>()
                });
                Poner(so, "cuerpo", raiz.transform.Find("Cuerpo").GetComponent<Renderer>());
                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    cambio = true;
                }
                if (cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-170 aspecto de piezas creado.");
        }

        private static Renderer[] Partes(Transform brazo)
        {
            return new[]
            {
                brazo.Find("Brazo").GetComponent<Renderer>(),
                brazo.Find("Antebrazo").GetComponent<Renderer>(),
                brazo.Find("Puno").GetComponent<Renderer>()
            };
        }

        // Crea las franjas que falten (apagadas) y devuelve las 4 en orden.
        private static Renderer[] ArmarFranjas(Transform antebrazo, Material franja, ref bool cambio)
        {
            var franjas = new Renderer[NumeroFranjas];
            for (int i = 0; i < NumeroFranjas; i++)
            {
                string nombre = "Franja" + (i + 1);
                Transform existente = antebrazo.Find(nombre);
                if (existente == null)
                {
                    GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubo.name = nombre;
                    Object.DestroyImmediate(cubo.GetComponent<Collider>());
                    cubo.transform.SetParent(antebrazo, false);
                    cubo.transform.localPosition = new Vector3(0f, 0f, (InicioFranjaMetros + i * SeparacionFranjaMetros) / LargoAntebrazo);
                    cubo.transform.localScale = new Vector3(AnchoFranja, AnchoFranja, GrosorFranjaMetros / LargoAntebrazo);
                    MeshRenderer render = cubo.GetComponent<MeshRenderer>();
                    render.sharedMaterial = franja;
                    render.shadowCastingMode = ShadowCastingMode.Off;
                    render.receiveShadows = false;
                    render.enabled = false;
                    existente = cubo.transform;
                    cambio = true;
                }
                franjas[i] = existente.GetComponent<Renderer>();
            }
            return franjas;
        }

        private static void Poner(SerializedObject so, string campo, Object valor)
        {
            so.FindProperty(campo).objectReferenceValue = valor;
        }

        private static void PonerLista(SerializedObject so, string campo, Renderer[] valores)
        {
            SerializedProperty lista = so.FindProperty(campo);
            lista.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
        }

        private static Material CrearMaterial(string nombre, Color color, Color? emision)
        {
            string ruta = RutaMateriales + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null)
            {
                return m;
            }
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            if (emision.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }
    }
}
