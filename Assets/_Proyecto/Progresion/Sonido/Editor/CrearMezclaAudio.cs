using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Sonido.Editor
{
    /// <summary>
    /// Crea el AudioMixer del juego (Resources/MezclaCyberCity.mixer) con los grupos Master, Musica, Efectos y Voces y el volumen de
    /// cada uno expuesto (VolGeneral, VolMusica, VolEfectos, VolVoces). Unity no tiene API pública para crear mezcladores: se usa la
    /// del editor (AudioMixerController) por reflexión. Repetible: si ya existe, solo comprueba.
    /// </summary>
    public static class CrearMezclaAudio
    {
        public const string Ruta = "Assets/_Proyecto/Progresion/Sonido/Resources/" + MezclaAudio.Recurso + ".mixer";
        private const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("Mecha Cyberpunk/Sonido/Crear AudioMixer (Master, Musica, Efectos, Voces)")]
        public static void Aplicar()
        {
            AudioMixer existente = AssetDatabase.LoadAssetAtPath<AudioMixer>(Ruta);
            if (existente == null)
            {
                Crear();
            }
            Debug.Log(Diagnostico());
        }

        private static void Crear()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Ruta));
            Assembly editor = typeof(UnityEditor.Editor).Assembly;
            Type tControlador = editor.GetType("UnityEditor.Audio.AudioMixerController");
            Type tGrupo = editor.GetType("UnityEditor.Audio.AudioMixerGroupController");
            Type tRuta = editor.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            Type tRutaBase = editor.GetType("UnityEditor.Audio.AudioParameterPath");
            object controlador = tControlador.GetMethod("CreateMixerControllerAtPath", Todo).Invoke(null, new object[] { Ruta });
            object master = tControlador.GetProperty("masterGroup", Todo).GetValue(controlador);

            Exponer(controlador, tControlador, tGrupo, tRuta, tRutaBase, master, MezclaAudio.ParamGeneral);
            foreach ((string nombre, string parametro) in new[] { (MezclaAudio.GrupoMusica, MezclaAudio.ParamMusica), (MezclaAudio.GrupoEfectos, MezclaAudio.ParamEfectos), (MezclaAudio.GrupoVoces, MezclaAudio.ParamVoces) })
            {
                object grupo = tControlador.GetMethod("CreateNewGroup", Todo).Invoke(controlador, new object[] { nombre, false });
                tControlador.GetMethod("AddChildToParent", Todo).Invoke(controlador, new[] { grupo, master });
                Exponer(controlador, tControlador, tGrupo, tRuta, tRutaBase, grupo, parametro);
            }
            EditorUtility.SetDirty((Object)controlador);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(Ruta);
        }

        private static void Exponer(object controlador, Type tControlador, Type tGrupo, Type tRuta, Type tRutaBase, object grupo, string nombre)
        {
            object guid = tGrupo.GetMethod("GetGUIDForVolume", Todo).Invoke(grupo, null);
            object ruta = Activator.CreateInstance(tRuta, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { grupo, guid }, null);
            tControlador.GetMethods(Todo).First(m => m.Name == "AddExposedParameter" && m.GetParameters().Length == 1).Invoke(controlador, new[] { ruta });
            // El parámetro recién expuesto se llama "MyExposedParam..."; se renombra.
            PropertyInfo pExpuestos = tControlador.GetProperty("exposedParameters", Todo);
            Array expuestos = (Array)pExpuestos.GetValue(controlador);
            for (int i = 0; i < expuestos.Length; i++)
            {
                object e = expuestos.GetValue(i);
                FieldInfo fGuid = e.GetType().GetField("guid", Todo);
                FieldInfo fNombre = e.GetType().GetField("name", Todo);
                if (fGuid.GetValue(e).Equals(guid))
                {
                    fNombre.SetValue(e, nombre);
                    expuestos.SetValue(e, i);
                }
            }
            pExpuestos.SetValue(controlador, expuestos);
            tControlador.GetMethod("OnChangedExposedParameter", Todo)?.Invoke(controlador, null);
        }

        /// <summary>Comprueba el mezclador: grupos y parámetros expuestos que responden.</summary>
        public static string Diagnostico()
        {
            AudioMixer m = AssetDatabase.LoadAssetAtPath<AudioMixer>(Ruta);
            StringBuilder sb = new StringBuilder("[Sonido] Mezclador " + Ruta + ": ");
            if (m == null)
            {
                return sb.Append("NO EXISTE").ToString();
            }
            foreach (string g in new[] { MezclaAudio.GrupoMaster, MezclaAudio.GrupoMusica, MezclaAudio.GrupoEfectos, MezclaAudio.GrupoVoces })
            {
                sb.Append(g).Append(m.FindMatchingGroups(g).Any(x => x.name == g) ? " ok, " : " FALTA, ");
            }
            foreach (string p in new[] { MezclaAudio.ParamGeneral, MezclaAudio.ParamMusica, MezclaAudio.ParamEfectos, MezclaAudio.ParamVoces })
            {
                sb.Append(p).Append(m.GetFloat(p, out float v) ? " expuesto (" + v.ToString("0.0") + " dB), " : " NO EXPUESTO, ");
            }
            return sb.ToString();
        }
    }
}
