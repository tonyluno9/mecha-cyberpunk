using System;
using System.IO;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-097: genera cuatro clips provisionales por código (lluvia, zumbido de neón, tráfico lejano y una música de bajo con bombo) como WAV de 16 bits
    /// en Assets/Arte/Audio/, crea ConfigAudioCiudad_Mundo y pone el objeto AmbienteSonoro en la raíz del prefab del barrio. Los clips se
    /// reemplazan por audio final sin tocar nada más. Es determinista (semillas fijas) y se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorAudio.Crear
    /// </summary>
    public static class CreadorAudio
    {
        private const string RutaAudio = "Assets/Arte/Audio";
        private const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigAudioCiudad_Mundo.asset";
        private const string RutaConfigLluvia = "Assets/_Proyecto/Mundo/Datos/ConfigLluvia_Mundo.asset";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const int Frecuencia = 22050;
        private const float SegundosCruce = 0.3f;

        [MenuItem("Mecha Cyberpunk/Crear T-097")]
        public static void Crear()
        {
            if (!Directory.Exists(RutaAudio))
            {
                Directory.CreateDirectory(RutaAudio);
            }
            AudioClip lluvia = Guardar("Ambiente_Lluvia", Lluvia());
            AudioClip neon = Guardar("Ambiente_Neon", Neon());
            AudioClip trafico = Guardar("Ambiente_Trafico", Trafico());
            AudioClip musica = Guardar("Ambiente_Musica", Musica());

            ConfigAudioCiudad config = AssetDatabase.LoadAssetAtPath<ConfigAudioCiudad>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigAudioCiudad>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            ConfigLluvia configLluvia = AssetDatabase.LoadAssetAtPath<ConfigLluvia>(RutaConfigLluvia);
            if (configLluvia == null)
            {
                throw new InvalidOperationException("Falta ConfigLluvia_Mundo; corre CreadorLluvia antes.");
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = raiz.transform.Find("AmbienteSonoro");
            if (viejo != null)
            {
                UnityEngine.Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject objeto = new GameObject("AmbienteSonoro");
            objeto.transform.SetParent(raiz.transform, false);
            AmbienteSonoro ambiente = objeto.AddComponent<AmbienteSonoro>();
            SerializedObject so = new SerializedObject(ambiente);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("configLluvia").objectReferenceValue = configLluvia;
            so.FindProperty("clipLluvia").objectReferenceValue = lluvia;
            so.FindProperty("clipNeon").objectReferenceValue = neon;
            so.FindProperty("clipTrafico").objectReferenceValue = trafico;
            so.FindProperty("clipMusica").objectReferenceValue = musica;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-097 creada: ambiente sonoro con 4 capas provisionales.");
        }

        // Ruido blanco con un filtro que le quita los graves: el siseo de la lluvia sobre el asfalto.
        private static float[] Lluvia()
        {
            Random azar = new Random(97);
            float[] s = new float[4 * Frecuencia];
            float anterior = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
                float pasaBajos = anterior + 0.35f * (ruido - anterior);
                anterior = pasaBajos;
                s[i] = (ruido - pasaBajos) * 0.5f;
            }
            return Empalmar(s);
        }

        // 100 Hz y sus armónicos: 200 ciclos exactos en 2 s, empalma solo.
        private static float[] Neon()
        {
            float[] s = new float[2 * Frecuencia];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Frecuencia;
                s[i] = 0.35f * Mathf.Sin(2f * Mathf.PI * 100f * t) + 0.18f * Mathf.Sin(2f * Mathf.PI * 200f * t) + 0.1f * Mathf.Sin(2f * Mathf.PI * 300f * t);
            }
            return s;
        }

        // Ruido marrón (graves) con un vaivén lento de volumen: el tráfico a lo lejos.
        private static float[] Trafico()
        {
            Random azar = new Random(98);
            float[] s = new float[6 * Frecuencia];
            float acumulado = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                acumulado += (float)(azar.NextDouble() * 2.0 - 1.0) * 0.06f;
                acumulado *= 0.995f;
                float t = i / (float)Frecuencia;
                float vaiven = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 6f);
                s[i] = Mathf.Clamp(acumulado, -1f, 1f) * vaiven * 1.5f;
            }
            return Empalmar(s);
        }

        // 8 s a 120 bpm: bombo en cada negra y una línea de bajo de ocho notas con decaimiento.
        private static float[] Musica()
        {
            float[] s = new float[8 * Frecuencia];
            float negra = 60f / 120f;
            float[] notas = { 55f, 55f, 82.4f, 55f, 65.4f, 65.4f, 98f, 65.4f };
            for (int golpe = 0; golpe < 16; golpe++)
            {
                int inicio = (int)(golpe * negra * Frecuencia);
                float nota = notas[golpe % notas.Length];
                for (int i = 0; i < (int)(negra * Frecuencia) && inicio + i < s.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float bombo = Mathf.Sin(2f * Mathf.PI * (50f + 90f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 12f) * 0.7f;
                    float bajo = Mathf.Sin(2f * Mathf.PI * nota * t) * Mathf.Exp(-t * 3f) * 0.4f;
                    s[inicio + i] += bombo + bajo;
                }
            }
            return s;
        }

        // Mezcla el final con el principio para que el bucle no haga clic.
        private static float[] Empalmar(float[] s)
        {
            int cruce = (int)(SegundosCruce * Frecuencia);
            for (int i = 0; i < cruce; i++)
            {
                float peso = i / (float)cruce;
                int fin = s.Length - cruce + i;
                s[i] = s[i] * peso + s[fin] * (1f - peso);
            }
            float[] recortado = new float[s.Length - cruce];
            Array.Copy(s, recortado, recortado.Length);
            return recortado;
        }

        private static AudioClip Guardar(string nombre, float[] muestras)
        {
            string ruta = RutaAudio + "/" + nombre + ".wav";
            byte[] bytes = Wav(muestras);
            if (!File.Exists(ruta) || !AreIguales(File.ReadAllBytes(ruta), bytes))
            {
                File.WriteAllBytes(ruta, bytes);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }

        private static byte[] Wav(float[] muestras)
        {
            using (MemoryStream memoria = new MemoryStream())
            using (BinaryWriter escritor = new BinaryWriter(memoria))
            {
                int datos = muestras.Length * 2;
                escritor.Write(new[] { 'R', 'I', 'F', 'F' });
                escritor.Write(36 + datos);
                escritor.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                escritor.Write(16);
                escritor.Write((short)1);
                escritor.Write((short)1);
                escritor.Write(Frecuencia);
                escritor.Write(Frecuencia * 2);
                escritor.Write((short)2);
                escritor.Write((short)16);
                escritor.Write(new[] { 'd', 'a', 't', 'a' });
                escritor.Write(datos);
                foreach (float muestra in muestras)
                {
                    escritor.Write((short)(Mathf.Clamp(muestra, -1f, 1f) * short.MaxValue));
                }
                return memoria.ToArray();
            }
        }

        private static bool AreIguales(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
