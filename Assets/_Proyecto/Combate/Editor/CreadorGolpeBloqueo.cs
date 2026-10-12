using System;
using System.Linq;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Brazos;
using MechaCyberpunk.Combate.Datos;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Combate.Editor
{
    /// <summary>
    /// Arma lo de T-012 en MechaJugador.prefab y Pruebas_Tony.unity (idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.Combate.Editor.CreadorGolpeBloqueo.Crear
    /// </summary>
    public static class CreadorGolpeBloqueo
    {
        private const string RutaCombate = "Assets/_Proyecto/Combate";
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaEntrada = "Assets/_Proyecto/Mecha/Entrada/ControlesMecha.inputactions";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const string RutaConfigBrazos = RutaCombate + "/Datos/ConfigBrazos_Base.asset";
        private const string RutaDatosSaco = RutaCombate + "/Datos/DatosMecha_SacoPrueba.asset";
        private const string RutaDatosAtacante = RutaCombate + "/Datos/DatosMecha_AtacantePrueba.asset";
        private const string RutaMaterialSaco = RutaCombate + "/Materiales/Prueba_Saco.mat";
        private const string RutaMaterialAtacante = RutaCombate + "/Materiales/Prueba_Atacante.mat";

        private const float VidaSaco = 1000f;
        private const float PreparacionAtacante = 0.6f;
        private const float LadoSaco = 4f;
        private static readonly Vector3 TamanoAtacante = new Vector3(3f, 4f, 1f);
        // Saco detrás-izquierda del jugador (hay que girar para golpearlo); atacante de frente, a 2.9 m del jugador (dentro del alcance de 3 m).
        private static readonly Vector3 PosicionSaco = new Vector3(-6f, LadoSaco / 2f, -4f);
        private static readonly Vector3 PosicionAtacante = new Vector3(0f, TamanoAtacante.y / 2f, 2.9f);

        [MenuItem("Mecha Cyberpunk/Crear T-012")]
        public static void Crear()
        {
            System.IO.Directory.CreateDirectory(RutaCombate + "/Materiales");
            AssetDatabase.Refresh();
            ConfigBrazos config = CrearAsset<ConfigBrazos>(RutaConfigBrazos);
            DatosMecha datosSaco = CrearDatos(RutaDatosSaco, "Saco de prueba", VidaSaco, null);
            DatosMecha datosAtacante = CrearDatos(RutaDatosAtacante, "Atacante de prueba", null, PreparacionAtacante);
            Material materialSaco = CrearMaterial(RutaMaterialSaco, new Color(0.3f, 0.55f, 0.6f));
            Material materialAtacante = CrearMaterial(RutaMaterialAtacante, new Color(0.4f, 0.4f, 0.5f));
            AssetDatabase.SaveAssets();

            ArmarPrefab(config);
            ArmarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Combate] T-012 creada.");
        }

        private static T CrearAsset<T>(string ruta) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, ruta);
            }
            return asset;
        }

        private static DatosMecha CrearDatos(string ruta, string nombre, float? vidaMaxima, float? preparacion)
        {
            DatosMecha datos = CrearAsset<DatosMecha>(ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = nombre;
            if (vidaMaxima.HasValue)
            {
                so.FindProperty("vidaMaxima").floatValue = vidaMaxima.Value;
            }
            if (preparacion.HasValue)
            {
                so.FindProperty("preparacionGolpe").floatValue = preparacion.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, ruta);
            }
            return material;
        }

        private static InputActionReference Referencia(string nombreAccion)
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == nombreAccion);
        }

        private static T Componente<T>(GameObject objeto) where T : Component
        {
            T existente = objeto.GetComponent<T>();
            return existente != null ? existente : objeto.AddComponent<T>();
        }

        private static void Asignar(UnityEngine.Object componente, string campo, UnityEngine.Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ArmarPrefab(ConfigBrazos config)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                GolpeMecha golpe = Componente<GolpeMecha>(raiz);
                Asignar(golpe, "golpear", Referencia("Golpear"));
                GuardiaMecha guardia = Componente<GuardiaMecha>(raiz);
                Asignar(guardia, "bloquear", Referencia("Bloquear"));

                AnimadorBrazos animador = Componente<AnimadorBrazos>(raiz);
                Asignar(animador, "brazoIzquierdo", raiz.transform.Find("Sacudida/Cabina/BrazoIzquierdo"));
                Asignar(animador, "brazoDerecho", raiz.transform.Find("Sacudida/Cabina/BrazoDerecho"));
                Asignar(animador, "config", config);

                // El control del peleador apaga movimiento, golpe y guardia (en KO, por ejemplo).
                PeleadorMecha peleador = raiz.GetComponent<PeleadorMecha>();
                SerializedObject so = new SerializedObject(peleador);
                SerializedProperty lista = so.FindProperty("controlados");
                // Se conserva lo que ya había (ControladorMecha) y se agregan golpe y guardia sin repetirlos.
                System.Collections.Generic.List<UnityEngine.Object> controlados =
                    new System.Collections.Generic.List<UnityEngine.Object>();
                for (int i = 0; i < lista.arraySize; i++)
                {
                    controlados.Add(lista.GetArrayElementAtIndex(i).objectReferenceValue);
                }
                foreach (Behaviour nuevo in new Behaviour[] { golpe, guardia })
                {
                    if (!controlados.Contains(nuevo))
                    {
                        controlados.Add(nuevo);
                    }
                }
                lista.arraySize = controlados.Count;
                for (int i = 0; i < controlados.Count; i++)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = controlados[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void ArmarEscena()
        {
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            // Abrir la escena descarga los assets sin uso: se cargan de nuevo para no asignar referencias muertas.
            DatosMecha datosSaco = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosSaco);
            DatosMecha datosAtacante = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosAtacante);
            Material materialSaco = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialSaco);
            Material materialAtacante = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialAtacante);
            GameObject jugador = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.GetComponent<PeleadorMecha>() != null && raiz.name == "MechaJugador")
                {
                    jugador = raiz;
                }
            }
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == "SacoDePrueba" || raiz.name == "AtacanteDePrueba")
                {
                    UnityEngine.Object.DestroyImmediate(raiz);
                }
            }
            if (jugador == null)
            {
                throw new InvalidOperationException("Pruebas_Tony no tiene el mecha del jugador.");
            }

            // Saco: cubo de 4 m con vida, lado Rival.
            GameObject saco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            saco.name = "SacoDePrueba";
            saco.transform.position = PosicionSaco;
            saco.transform.localScale = Vector3.one * LadoSaco;
            saco.GetComponent<MeshRenderer>().sharedMaterial = materialSaco;
            VidaMecha vidaSaco = saco.AddComponent<VidaMecha>();
            Asignar(vidaSaco, "datos", datosSaco);
            PeleadorMecha peleadorSaco = saco.AddComponent<PeleadorMecha>();
            SerializedObject soPeleador = new SerializedObject(peleadorSaco);
            soPeleador.FindProperty("lado").enumValueIndex = (int)LadoPelea.Rival;
            soPeleador.ApplyModifiedPropertiesWithoutUndo();
            saco.AddComponent<SacoDePrueba>();

            // Atacante: bloque de 3 x 4 x 1 m frente al jugador (su cara queda a 2.4 m: se ve de frente sobre el panel).
            GameObject atacante = GameObject.CreatePrimitive(PrimitiveType.Cube);
            atacante.name = "AtacanteDePrueba";
            atacante.transform.position = PosicionAtacante;
            atacante.transform.localScale = TamanoAtacante;
            MeshRenderer visual = atacante.GetComponent<MeshRenderer>();
            visual.sharedMaterial = materialAtacante;
            AtacanteDePrueba componente = atacante.AddComponent<AtacanteDePrueba>();
            Asignar(componente, "datos", datosAtacante);
            Asignar(componente, "objetivo", jugador);
            Asignar(componente, "visual", visual);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
    }
}
