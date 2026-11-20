using System.Collections.Generic;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1527: daño del francotirador (no mata de un tiro al cuerpo a un pandillero de 60 de vida; la cabeza x3 sí), límites del zoom con la rueda y el sonido
    /// de la cámara lenta de la rueda de armas (UI_CAMARA_LENTA). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarArmasT1527.Diagnostico / .Aplicar
    /// </summary>
    public static class AjustarArmasT1527
    {
        public const string PrefijoSniper = "Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_sniper_";
        public const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        public const string RutaCatalogoSonidos = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        public const string RutaSonidoCamaraLenta = "Assets/Arte/Audio/UI/UI_CAMARA_LENTA.wav";
        public const string IdCamaraLenta = "UI_CAMARA_LENTA";

        /// <summary>Daño por bala del francotirador en los niveles 0 a 3 (antes 70, 85, 100 y 120). Todos por debajo de los 60 de vida de un pandillero.</summary>
        public static readonly float[] DanioSniper = { 40f, 44f, 48f, 52f };

        [MenuItem("Mecha Cyberpunk/T-1527/Diagnostico armas")]
        public static void Diagnostico()
        {
            for (int n = 0; n < DanioSniper.Length; n++)
            {
                SerializedObject so = Cargar(PrefijoSniper + n + ".asset");
                Debug.Log("[T-1527] sniper nivel " + n + ": danio " + (so != null ? so.FindProperty("danio").floatValue.ToString() : "falta") + " (objetivo " + DanioSniper[n] + ")");
            }
            SerializedObject cam = Cargar(RutaCamara);
            Debug.Log("[T-1527] camara: fovSniper " + Valor(cam, "fovSniper") + ", fovSniperMinimo " + Valor(cam, "fovSniperMinimo") + ", fovSniperMaximo " + Valor(cam, "fovSniperMaximo") + ", factorPasoZoom " + Valor(cam, "factorPasoZoom"));
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogoSonidos);
            bool esta = false;
            if (catalogo != null)
            {
                foreach (CatalogoSonidos.Entrada e in catalogo.Entradas)
                {
                    esta |= e != null && e.id == IdCamaraLenta && e.finales != null && e.finales.Length > 0;
                }
            }
            Debug.Log("[T-1527] catalogo de sonidos: " + IdCamaraLenta + (esta ? " ya tiene clip" : " falta") + "; wav " + (AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoCamaraLenta) != null ? "importado" : "no importado"));
        }

        [MenuItem("Mecha Cyberpunk/T-1527/Aplicar armas")]
        public static void Aplicar()
        {
            for (int n = 0; n < DanioSniper.Length; n++)
            {
                SerializedObject so = Cargar(PrefijoSniper + n + ".asset");
                if (so == null)
                {
                    Debug.LogError("[T-1527] falta " + PrefijoSniper + n + ".asset");
                    continue;
                }
                float antes = so.FindProperty("danio").floatValue;
                so.FindProperty("danio").floatValue = DanioSniper[n];
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[T-1527] sniper nivel " + n + ": danio " + antes + " -> " + DanioSniper[n]);
            }
            SerializedObject cam = Cargar(RutaCamara);
            if (cam != null)
            {
                Poner(cam, "fovSniperMinimo", 3f);
                Poner(cam, "fovSniperMaximo", 24f);
                Poner(cam, "factorPasoZoom", 1.3f);
                cam.ApplyModifiedPropertiesWithoutUndo();
            }
            AgregarSonidoCamaraLenta();
            AssetDatabase.SaveAssets();
        }

        private static void AgregarSonidoCamaraLenta()
        {
            AssetDatabase.ImportAsset(RutaSonidoCamaraLenta);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoCamaraLenta);
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogoSonidos);
            if (clip == null || catalogo == null)
            {
                Debug.LogError("[T-1527] falta el wav " + RutaSonidoCamaraLenta + " o el catalogo de sonidos.");
                return;
            }
            List<CatalogoSonidos.Entrada> entradas = new List<CatalogoSonidos.Entrada>(catalogo.Entradas);
            CatalogoSonidos.Entrada existente = entradas.Find(e => e != null && e.id == IdCamaraLenta);
            if (existente == null)
            {
                existente = new CatalogoSonidos.Entrada { id = IdCamaraLenta };
                entradas.Add(existente);
            }
            existente.finales = new[] { clip };
            catalogo.Fijar(entradas);
            catalogo.SincronizarInterruptores();
            catalogo.FijarEncendido(IdCamaraLenta, true);
            EditorUtility.SetDirty(catalogo);
            Debug.Log("[T-1527] catalogo: " + IdCamaraLenta + " -> " + clip.name + " (encendido).");
        }

        private static SerializedObject Cargar(string ruta)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            return asset != null ? new SerializedObject(asset) : null;
        }

        private static string Valor(SerializedObject so, string campo)
        {
            SerializedProperty p = so != null ? so.FindProperty(campo) : null;
            return p != null ? p.floatValue.ToString() : "?";
        }

        private static void Poner(SerializedObject so, string campo, float valor)
        {
            SerializedProperty p = so.FindProperty(campo);
            if (p != null)
            {
                p.floatValue = valor;
            }
        }
    }
}
