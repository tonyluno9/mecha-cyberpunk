using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1488: crea un ConfigCarro por tipo de vehículo (Compacto, Sedán, Camioneta, Deportivo, Pesado) con los valores de <see cref="PerfilManejo"/>,
    /// actualiza los de moto, pone a cada prefab Carro_* el de su tipo y ajusta la cámara del carro (distancia por velocidad). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CrearPerfilesManejoT1488.Crear
    /// </summary>
    public static class CrearPerfilesManejoT1488
    {
        private const string CarpetaDatos = "Assets/_Proyecto/Vehiculos/Datos/";
        private const string RutaBase = CarpetaDatos + "ConfigCarro_Viejo.asset";
        private const string RutaMoto = CarpetaDatos + "ConfigCarro_Moto.asset";
        private const string RutaMotoReparto = CarpetaDatos + "ConfigCarro_MotoReparto.asset";
        private const string RutaCamara = CarpetaDatos + "ConfigCamaraCarro_Viejo.asset";
        private const string CarpetaPrefabs = "Assets/_Proyecto/Vehiculos/Prefabs/";
        private const float VelocidadMotoReparto = 18f;
        private const float FuerzaMotorMotoReparto = 1250f;
        private const float DistanciaExtraCamara = 3.5f;
        private const float KmhDistanciaExtraCamara = 90f;

        /// <summary>Tipo de manejo de cada modelo de carro (los que no están aquí son sedán). Los tamaños y nombres salen de T-742, T-1342 y la guía de modelos.</summary>
        public static readonly Dictionary<string, TipoManejo> TipoPorModelo = new Dictionary<string, TipoManejo>
        {
            { "V01a", TipoManejo.Compacto },   // carro viejito
            { "V01b", TipoManejo.Deportivo },  // carro deportivo
            { "V02", TipoManejo.Sedan },       // sedán viejo
            { "V03", TipoManejo.Camioneta },   // pickup de trabajo
            { "V04", TipoManejo.Camioneta },   // combi vieja
            { "V05", TipoManejo.Sedan },       // patrulla
            { "V06", TipoManejo.Camioneta },   // patrulla camioneta
            { "PA01", TipoManejo.Sedan },      // T-1510: patrulla sedán nueva
            { "PA02", TipoManejo.Camioneta },  // T-1510: patrulla SUV nueva
            { "V12", TipoManejo.Deportivo },   // deportivo propio
            { "V13", TipoManejo.Sedan },       // sedán familiar
            { "V14", TipoManejo.Compacto },    // compacto viejo
            { "V15", TipoManejo.Camioneta },
            { "V16", TipoManejo.Pesado },      // microbús / pesero
            { "V17", TipoManejo.Pesado },      // camión de carga
            { "V18", TipoManejo.Pesado },      // tractocamión
            { "V21", TipoManejo.Compacto },
        };

        public static string RutaConfig(TipoManejo tipo)
        {
            return CarpetaDatos + (tipo == TipoManejo.Moto ? "ConfigCarro_Moto" : "ConfigCarro_" + tipo) + ".asset";
        }

        [MenuItem("Mecha Cyberpunk/T-1488/Crear perfiles de manejo")]
        public static void Crear()
        {
            var informe = new StringBuilder();
            informe.AppendLine("tipo\tmasa\tvmax\tacel m/s2\tfreno m/s2\tcabeceo grados\tzeta\trigidez\tamortiguamiento");
            foreach (TipoManejo tipo in PerfilManejo.Tipos)
            {
                ConfigCarro config = ObtenerConfig(tipo);
                Aplicar(config, PerfilManejo.Para(tipo));
                PerfilManejo f = PerfilManejo.Para(tipo);
                informe.AppendLine($"{tipo}\t{f.Masa}\t{f.VelocidadMaxima}\t{f.AceleracionMotor:F1}\t{f.DesaceleracionFreno:F1}\t{f.CabeceoAlFrenarGrados:F1}\t{f.Zeta}\t{f.Rigidez:F0}\t{f.Amortiguamiento:F0}");
            }
            ReglasMotoReparto();
            AssetDatabase.SaveAssets();

            foreach (string ruta in Directory.GetFiles(CarpetaPrefabs, "Carro_*.prefab"))
            {
                informe.AppendLine(AsignarAPrefab(ruta.Replace('\\', '/')));
            }
            informe.AppendLine(AjustarCamara());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1488-perfiles.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-1488: perfiles de manejo listos:\n" + informe);
        }

        private static ConfigCarro ObtenerConfig(TipoManejo tipo)
        {
            string ruta = RutaConfig(tipo);
            ConfigCarro config = AssetDatabase.LoadAssetAtPath<ConfigCarro>(ruta);
            if (config != null)
            {
                return config;
            }
            // Se parte de una copia del carro base para conservar lo que no cambia (largos de suspensión, bajar, etc.).
            
            AssetDatabase.CopyAsset(RutaBase, ruta);
            return AssetDatabase.LoadAssetAtPath<ConfigCarro>(ruta);
        }

        private static void Aplicar(ConfigCarro config, PerfilManejo perfil)
        {
            var so = new SerializedObject(config);
            foreach (KeyValuePair<string, float> campo in perfil.Campos())
            {
                SerializedProperty p = so.FindProperty(campo.Key);
                if (p == null)
                {
                    Debug.LogError($"[Vehiculos] T-1488: ConfigCarro no tiene el campo '{campo.Key}'.");
                    continue;
                }
                if (p.propertyType == SerializedPropertyType.Integer)
                {
                    p.intValue = Mathf.RoundToInt(campo.Value);
                }
                else
                {
                    p.floatValue = campo.Value;
                }
            }
            so.FindProperty("tipoManejo").enumValueIndex = (int)perfil.Tipo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // La moto de reparto (V07) conserva su tope y su motor de T-695.
        private static void ReglasMotoReparto()
        {
            ConfigCarro reparto = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaMotoReparto);
            if (reparto == null)
            {
                return;
            }
            Aplicar(reparto, PerfilManejo.Para(TipoManejo.Moto));
            var so = new SerializedObject(reparto);
            so.FindProperty("velocidadMaxima").floatValue = VelocidadMotoReparto;
            so.FindProperty("fuerzaMotor").floatValue = FuerzaMotorMotoReparto;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reparto);
        }

        public static string AsignarAPrefab(string ruta)
        {
            string codigo = Path.GetFileNameWithoutExtension(ruta).Substring("Carro_".Length);
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                CarroViejo carro = raiz.GetComponent<CarroViejo>();
                if (carro == null)
                {
                    return codigo + "\tsin CarroViejo, se salta";
                }
                string rutaConfig;
                if (raiz.GetComponent<ManejoMoto>() != null)
                {
                    rutaConfig = codigo == "V07" ? RutaMotoReparto : RutaMoto;
                }
                else
                {
                    TipoManejo tipo = TipoPorModelo.TryGetValue(codigo, out TipoManejo t) ? t : TipoManejo.Sedan;
                    rutaConfig = RutaConfig(tipo);
                }
                ConfigCarro config = AssetDatabase.LoadAssetAtPath<ConfigCarro>(rutaConfig);
                var so = new SerializedObject(carro);
                SerializedProperty p = so.FindProperty("config");
                if (p.objectReferenceValue != config)
                {
                    p.objectReferenceValue = config;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    return codigo + "\t" + Path.GetFileNameWithoutExtension(rutaConfig);
                }
                return codigo + "\t" + Path.GetFileNameWithoutExtension(rutaConfig) + " (ya estaba)";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static string AjustarCamara()
        {
            ConfigCamaraCarro camara = AssetDatabase.LoadAssetAtPath<ConfigCamaraCarro>(RutaCamara);
            if (camara == null)
            {
                return "FALTA " + RutaCamara;
            }
            var so = new SerializedObject(camara);
            so.FindProperty("distanciaExtraVelocidad").floatValue = DistanciaExtraCamara;
            so.FindProperty("kmhDistanciaExtra").floatValue = KmhDistanciaExtraCamara;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(camara);
            return $"Cámara: distancia extra {DistanciaExtraCamara} m a {KmhDistanciaExtraCamara} km/h; FOV +12° a 100 km/h; temblor 3 a 18 m/s";
        }
    }
}
