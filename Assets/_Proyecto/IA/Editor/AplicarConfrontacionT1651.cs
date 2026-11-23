using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1651 (T26 y T27): crea (o actualiza) los ConfigConfrontacion de cada facción en IA/Resources/Confrontacion/. Idempotente: vuelve a poner los valores de aquí.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarConfrontacionT1651.Aplicar
    /// </summary>
    public static class AplicarConfrontacionT1651
    {
        private const string Carpeta = "Assets/_Proyecto/IA/Resources/Confrontacion";

        private struct Valores
        {
            public string Nombre;
            public string Hablante;
            public Color Color;
            public CuandoPelea Pelea;
            public float Deteccion, Advertencia, Quedarse, DejarSeguir, Retirada, Acercamiento, Frases, Gesto, EntreEmpujones, Danio, Velocidad;
            public bool SacaArma;
            public int ParaTirar, MaxEmpujones;
        }

        [MenuItem("Mecha Cyberpunk/T1651/Crear configs de confrontacion")]
        public static void Aplicar()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/IA/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/IA", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/IA/Resources", "Confrontacion");
            }
            Crear(new Valores
            {
                Nombre = "Config_Skulls", Hablante = "SKULL", Color = new Color(1f, 0.25f, 0.8f), Pelea = CuandoPelea.SoloSiElJugadorAtaca,
                Deteccion = 20f, Advertencia = 20f, Quedarse = 8f, DejarSeguir = 14f, Retirada = 3f, Acercamiento = 2.2f, Velocidad = 1.62f,
                Frases = 5f, Gesto = 2.2f, SacaArma = false, EntreEmpujones = 4f, ParaTirar = 2, MaxEmpujones = 4, Danio = 1f
            });
            Crear(new Valores
            {
                Nombre = "Config_Snakes", Hablante = "SNAKE", Color = new Color(0.6f, 1f, 0.2f), Pelea = CuandoPelea.SoloSiElJugadorAtaca,
                Deteccion = 20f, Advertencia = 20f, Quedarse = 8f, DejarSeguir = 14f, Retirada = 3f, Acercamiento = 2.2f, Velocidad = 1.62f,
                Frases = 4.5f, Gesto = 2f, SacaArma = true, EntreEmpujones = 3.5f, ParaTirar = 2, MaxEmpujones = 5, Danio = 1f
            });
            Crear(new Valores
            {
                Nombre = "Config_Guardias", Hablante = "GUARDIA", Color = new Color(0.7f, 0.5f, 1f), Pelea = CuandoPelea.SoloSiDisparas,
                Deteccion = 14f, Advertencia = 8f, Quedarse = 14f, DejarSeguir = 20f, Retirada = 3f, Acercamiento = 5f,
                Frases = 6f, Gesto = 2f, SacaArma = false, EntreEmpujones = 4f, ParaTirar = 1, MaxEmpujones = 0, Danio = 0f
            });
            // Policía y Nova: solo guardan lo que ya usan (Trafico y RespuestaNova); no los lee el código de confrontación de calle.
            Crear(new Valores
            {
                Nombre = "Config_Policia", Hablante = "POLICIA", Color = new Color(0.3f, 0.6f, 1f), Pelea = CuandoPelea.SoloPorDelitos,
                Deteccion = 0f, Advertencia = 0f, Quedarse = 0f, DejarSeguir = 0f, Retirada = 0f, Acercamiento = 0f,
                Frases = 6f, Gesto = 2f, SacaArma = true, EntreEmpujones = 4f, ParaTirar = 1, MaxEmpujones = 0, Danio = 0f
            });
            Crear(new Valores
            {
                Nombre = "Config_Nova", Hablante = "NOVA", Color = new Color(0.3f, 0.65f, 1f), Pelea = CuandoPelea.SinAdvertencia,
                Deteccion = 40f, Advertencia = 0f, Quedarse = 0f, DejarSeguir = 0f, Retirada = 0f, Acercamiento = 0f,
                Frases = 6f, Gesto = 2f, SacaArma = true, EntreEmpujones = 4f, ParaTirar = 1, MaxEmpujones = 0, Danio = 0f
            });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[T-1651] configs de confrontacion listas en " + Carpeta);
        }

        private static void Crear(Valores v)
        {
            string ruta = Carpeta + "/" + v.Nombre + ".asset";
            ConfigConfrontacion config = AssetDatabase.LoadAssetAtPath<ConfigConfrontacion>(ruta);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigConfrontacion>();
                AssetDatabase.CreateAsset(config, ruta);
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("nombreHablante").stringValue = v.Hablante;
            so.FindProperty("colorHablante").colorValue = v.Color;
            so.FindProperty("cuandoPelea").enumValueIndex = (int)v.Pelea;
            so.FindProperty("distanciaDeteccion").floatValue = v.Deteccion;
            so.FindProperty("segundosCerca").floatValue = v.Advertencia;
            so.FindProperty("distanciaCerca").floatValue = v.Quedarse;
            so.FindProperty("distanciaDejarDeSeguir").floatValue = v.DejarSeguir;
            so.FindProperty("velocidadCaminar").floatValue = v.Velocidad > 0f ? v.Velocidad : 1.62f;
            so.FindProperty("segundosRetirada").floatValue = v.Retirada;
            so.FindProperty("distanciaAcercamiento").floatValue = Mathf.Max(0.5f, v.Acercamiento);
            so.FindProperty("segundosEntreFrases").floatValue = v.Frases;
            so.FindProperty("segundosGesto").floatValue = v.Gesto;
            so.FindProperty("sacaArmaEnAdvertencia").boolValue = v.SacaArma;
            so.FindProperty("segundosEntreEmpujones").floatValue = v.EntreEmpujones;
            so.FindProperty("empujonesParaTirar").intValue = v.ParaTirar;
            so.FindProperty("empujonesMaximos").intValue = v.MaxEmpujones;
            so.FindProperty("danioEmpujon").floatValue = v.Danio;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
