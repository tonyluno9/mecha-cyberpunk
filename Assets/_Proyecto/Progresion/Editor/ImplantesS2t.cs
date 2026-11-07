using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-866 (playtest 6-nov tarde): los implantes que pidió Tony, comprables con el Doc. Las piernas pasan a EXO DE PIERNAS, los brazos a EXO DE
    /// BRAZOS (además de pegar más y patear menos, ahora recargan más rápido) y los ojos a VISOR (además de la vida en números y apuntar más
    /// lejos, ven a los enemigos a través de las cosas con VisorRayosX). El modelo de cada implante llega del chat de arte (IMP01-IMP03); hasta
    /// entonces funcionan igual. Arma también el material Resources/VisorRayosX. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.ImplantesS2t.Aplicar
    /// </summary>
    public static class ImplantesS2t
    {
        public const string Carpeta = "Assets/_Proyecto/Progresion/Datos/Implantes/";
        public const string RutaMaterialVisor = "Assets/_Proyecto/Progresion/Apoyo/Resources/VisorRayosX.mat";
        public const string ShaderVisor = "MechaCyberpunk/Progresion/VisorRayosX";

        /// <summary>(id, nombre, descripción, reducción de recarga o -1 para no tocarla).</summary>
        public static readonly (string id, string nombre, string descripcion, float recarga)[] Cambios =
        {
            ("implante_piernas_1", "EXO DE PIERNAS I", "EXOESQUELETO DE CADERA A TOBILLO: CORRES, SALTAS Y RUEDAS MÁS", -1f),
            ("implante_piernas_2", "EXO DE PIERNAS II", "EL EXO COMPLETO: CORRES, SALTAS Y RUEDAS MUCHO MÁS", -1f),
            ("implante_brazos_1", "EXO DE BRAZOS I", "EXO DE HOMBROS Y BRAZOS: RECARGAS MÁS RÁPIDO, PEGAS MÁS Y EL ARMA PATEA MENOS", 0.25f),
            ("implante_brazos_2", "EXO DE BRAZOS II", "EL EXO COMPLETO: RECARGAS VOLANDO, PUÑOS AL DOBLE Y EL ARMA CASI NO PATEA", 0.45f),
            ("implante_ojos_1", "VISOR I", "VISOR DE CABEZA: VES A LOS ENEMIGOS A TRAVÉS DE LAS PAREDES Y SU VIDA", -1f),
            ("implante_ojos_2", "VISOR II", "VISOR COMPLETO: ENEMIGOS A TRAVÉS DE LAS PAREDES, SU VIDA EN NÚMEROS Y APUNTAS MÁS LEJOS", -1f),
        };

        [MenuItem("Mecha Cyberpunk/S2t/7 Implantes: exo de piernas, exo de brazos y visor")]
        public static void Aplicar()
        {
            foreach (var c in Cambios)
            {
                string ruta = Carpeta + "DatosImplante_" + c.id + ".asset";
                DatosImplante d = AssetDatabase.LoadAssetAtPath<DatosImplante>(ruta);
                if (d == null)
                {
                    Debug.LogError("[Progresion] Falta " + ruta);
                    continue;
                }
                SerializedObject so = new SerializedObject(d);
                so.FindProperty("nombre").stringValue = c.nombre;
                so.FindProperty("descripcion").stringValue = c.descripcion;
                if (c.recarga >= 0f)
                {
                    so.FindProperty("efectos.reduccionRecarga").floatValue = c.recarga;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialVisor);
            if (m == null)
            {
                Shader s = Shader.Find(ShaderVisor);
                if (s == null)
                {
                    Debug.LogError("[Progresion] Falta el shader " + ShaderVisor);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(RutaMaterialVisor));
                    AssetDatabase.CreateAsset(new Material(s) { name = "VisorRayosX" }, RutaMaterialVisor);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-866: implantes listos (exo de piernas, exo de brazos, visor)");
        }
    }
}
