using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Escribe WAV provisionales (16 bits, 44.1 kHz, mono) generados por código en Assets/Arte/Audio/Provisional/&lt;SEC&gt;/&lt;ID&gt;.wav (T-407).
    /// Solo los IDs P1 que el juego conecta (sin AMB: el ambiente queda en silencio hasta que lleguen los finales). Si ya existe el archivo no lo reescribe.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.GeneradorSonidosProvisionales.Crear
    /// </summary>
    public static class GeneradorSonidosProvisionales
    {
        public const string Carpeta = "Assets/Arte/Audio/Provisional";
        public const int Frecuencia = 44100;

        private enum Tipo { Disparo, Golpe, Paso, Impacto, Tono, Bucle, Barrido, Fanfarria, Voz }

        private struct Receta
        {
            public string Id;
            public Tipo Tipo;
            public float Segundos;
            public float Frecuencia;
            public float Fuerza;
        }

        private static Receta R(string id, Tipo tipo, float segundos, float frecuencia = 0f, float fuerza = 1f)
        {
            return new Receta { Id = id, Tipo = tipo, Segundos = segundos, Frecuencia = frecuencia, Fuerza = fuerza };
        }

        private static readonly Receta[] Recetas =
        {
            R("ARM_PIS_DISPARO", Tipo.Disparo, 0.25f, 0f, 0.8f), R("ARM_ESC_DISPARO", Tipo.Disparo, 0.5f, 0f, 1f),
            R("ARM_SUB_DISPARO", Tipo.Disparo, 0.15f, 0f, 0.6f), R("ARM_PIS_RECARGA", Tipo.Golpe, 0.3f, 900f, 0.5f),
            R("ARM_ESC_RECARGA_CARTUCHO", Tipo.Golpe, 0.2f, 700f, 0.5f), R("ARM_SUB_RECARGA", Tipo.Golpe, 0.3f, 800f, 0.5f),
            R("ARM_VACIO", Tipo.Golpe, 0.08f, 1500f, 0.5f), R("ARM_CAMBIAR", Tipo.Golpe, 0.12f, 600f, 0.4f),
            R("ARM_BALA_IMPACTO_CONCRETO", Tipo.Impacto, 0.15f, 2500f, 0.5f), R("ARM_BALA_IMPACTO_CARNE", Tipo.Impacto, 0.15f, 600f, 0.6f),
            R("ARM_LASER_AVISO", Tipo.Tono, 0.15f, 2200f, 0.4f), R("PUN_GOLPE_AIRE", Tipo.Barrido, 0.18f, 1200f, 0.3f),
            R("PUN_GOLPE_CARNE", Tipo.Golpe, 0.18f, 300f, 0.9f), R("JUG_PASO_ASFALTO", Tipo.Paso, 0.12f, 400f, 0.4f),
            R("JUG_SALTO", Tipo.Barrido, 0.2f, 500f, 0.3f), R("JUG_CAER", Tipo.Golpe, 0.2f, 200f, 0.7f),
            R("JUG_DANIO", Tipo.Voz, 0.25f, 220f, 0.6f), R("PAN_RIVAL_ALERTA", Tipo.Voz, 0.3f, 330f, 0.5f),
            R("PAN_RIVAL_CAE", Tipo.Voz, 0.4f, 160f, 0.6f), R("PAN_JEFE_ENTRADA", Tipo.Fanfarria, 1.0f, 110f, 0.7f),
            R("PAN_ALIADO_RECLUTA", Tipo.Voz, 0.3f, 380f, 0.5f), R("VEH_ALARMA_ROBO", Tipo.Barrido, 3f, 1400f, 0.4f),
            R("VEH_VIEJO_MOTOR_RALENTI", Tipo.Bucle, 1f, 70f, 0.5f), R("CAL_SUBE_NIVEL", Tipo.Fanfarria, 0.8f, 440f, 0.4f),
            R("ZON_RESPETO_SUBE", Tipo.Fanfarria, 0.6f, 523f, 0.4f), R("ZON_CONQUISTADA", Tipo.Fanfarria, 3f, 392f, 0.5f),
            R("ECO_COMPRA", Tipo.Tono, 0.25f, 1320f, 0.4f), R("ECO_SIN_DINERO", Tipo.Tono, 0.25f, 180f, 0.5f),
            R("ECO_ARMA_RECOGER", Tipo.Golpe, 0.2f, 1000f, 0.5f), R("ECO_CLINICA_DESPERTAR", Tipo.Tono, 0.6f, 880f, 0.3f),
            R("UI_MOVER", Tipo.Tono, 0.05f, 1200f, 0.3f), R("UI_ACEPTAR", Tipo.Tono, 0.12f, 1600f, 0.35f),
            R("UI_CANCELAR", Tipo.Tono, 0.12f, 800f, 0.35f),
            R("ARM_SNI_DISPARO", Tipo.Disparo, 0.8f, 0f, 1f), R("ARM_SNI_RECARGA", Tipo.Golpe, 0.4f, 500f, 0.5f),
            R("ARM_SNI_ZOOM", Tipo.Golpe, 0.08f, 1800f, 0.35f),
        };

        public static int CantidadRecetas => Recetas.Length;

        [MenuItem("Mecha Cyberpunk/Sonido/Generar provisionales")]
        public static void Crear()
        {
            int nuevos = 0;
            foreach (Receta r in Recetas)
            {
                string seccion = r.Id.Substring(0, r.Id.IndexOf('_'));
                string carpeta = Carpeta + "/" + seccion;
                Directory.CreateDirectory(carpeta);
                string ruta = carpeta + "/" + r.Id + ".wav";
                if (File.Exists(ruta)) continue;
                EscribirWav(ruta, Generar(r));
                nuevos++;
            }
            AssetDatabase.Refresh();
            Debug.Log("[Sonido] GeneradorSonidosProvisionales: " + nuevos + " WAV nuevos de " + Recetas.Length + ".");
        }

        private static float[] Generar(Receta r)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(r.Segundos * Frecuencia));
            float[] datos = new float[n];
            System.Random azar = new System.Random(r.Id.GetHashCode() & 0x7fffffff);
            float filtro = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Frecuencia;
                float progreso = (float)i / n;
                float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
                float valor;
                switch (r.Tipo)
                {
                    case Tipo.Disparo:
                        filtro += (ruido - filtro) * 0.35f;
                        valor = filtro * Mathf.Exp(-t * 14f / r.Segundos * 0.5f) * 1.5f;
                        break;
                    case Tipo.Golpe:
                        valor = (Mathf.Sin(2f * Mathf.PI * r.Frecuencia * t) * 0.6f + ruido * 0.4f) * Mathf.Exp(-t * 25f);
                        break;
                    case Tipo.Paso:
                        filtro += (ruido - filtro) * 0.15f;
                        valor = filtro * Mathf.Exp(-t * 35f) * 2f;
                        break;
                    case Tipo.Impacto:
                        valor = (ruido * 0.7f + Mathf.Sin(2f * Mathf.PI * r.Frecuencia * t) * 0.3f) * Mathf.Exp(-t * 30f);
                        break;
                    case Tipo.Tono:
                        valor = Mathf.Sin(2f * Mathf.PI * r.Frecuencia * t) * Mathf.Sin(Mathf.PI * progreso);
                        break;
                    case Tipo.Bucle:
                        // Entero de ciclos en el clip: empalma sin corte.
                        float ciclos = Mathf.Round(r.Frecuencia * r.Segundos);
                        valor = Mathf.Sin(2f * Mathf.PI * ciclos * progreso) * 0.6f + Mathf.Sin(4f * Mathf.PI * ciclos * progreso) * 0.3f;
                        break;
                    case Tipo.Barrido:
                        float f = r.Frecuencia * (r.Segundos > 1f ? 0.7f + 0.6f * Mathf.PingPong(t * 1.5f, 1f) : 0.5f + progreso);
                        filtro += 2f * Mathf.PI * f / Frecuencia;
                        valor = Mathf.Sin(filtro) * (r.Segundos > 1f ? 1f : Mathf.Sin(Mathf.PI * progreso));
                        break;
                    case Tipo.Fanfarria:
                        float nota = r.Frecuencia * (progreso < 0.33f ? 1f : progreso < 0.66f ? 1.25f : 1.5f);
                        valor = Mathf.Sin(2f * Mathf.PI * nota * t) * (1f - progreso);
                        break;
                    default: // Voz: tono con vibrato que baja
                        valor = Mathf.Sin(2f * Mathf.PI * r.Frecuencia * (1f - 0.3f * progreso) * t + Mathf.Sin(t * 40f)) * 0.7f
                            + ruido * 0.2f * Mathf.Sin(Mathf.PI * progreso);
                        valor *= Mathf.Sin(Mathf.PI * progreso);
                        break;
                }
                datos[i] = Mathf.Clamp(valor * r.Fuerza, -1f, 1f);
            }
            return datos;
        }

        private static void EscribirWav(string ruta, float[] muestras)
        {
            using (FileStream fs = new FileStream(ruta, FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                int bytes = muestras.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Frecuencia);
                w.Write(Frecuencia * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (float m in muestras) w.Write((short)Mathf.RoundToInt(m * short.MaxValue));
            }
        }
    }
}
