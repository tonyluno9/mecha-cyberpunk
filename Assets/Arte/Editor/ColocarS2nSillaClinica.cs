using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Progresion.Apoyo;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-813 (playtest del 6-nov): la silla del Doc. Pone una <see cref="SillaClinica"/> en el sillón I07 del consultorio (S2_Interiores/ConsultorioDoc,
    /// de ColocarS2Interiores: caja x -104..-96, z -16..-10.2) con el brazo robótico I06 que cuelga sobre él. El asiento sale del perfil de la malla
    /// del sillón: la cadera sobre el cojín, la espalda hacia el cabezal (la punta alta, hacia la calle en el I07) y la vista hacia los pies; el
    /// jugador se para y se levanta del lado del cuarto (+X), mirando al Doc. Antes arma ConfigSillaClinica_Base. Idempotente:
    /// rehace su raíz S2n_SillaClinica.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nSillaClinica.Colocar
    /// (Solo datos, sin abrir la escena: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nSillaClinica.PrepararDatos)
    /// </summary>
    public static class ColocarS2nSillaClinica
    {
        public const string NombreRaiz = "S2n_SillaClinica";
        public const string RutaConsultorio = "S2_Interiores/ConsultorioDoc";
        public const string RutaDoc = "GTA_T417/Apoyo_JOEL";
        public const string RutaConfig = "Assets/_Proyecto/Progresion/Apoyo/Datos/ConfigSillaClinica_Base.asset";
        /// <summary>Centro del sillón de ColocarS2Interiores.Consultorio, por si el modelo no está.</summary>
        public static readonly Vector3 CentroSillon = new Vector3(-102.1f, 0.22f, -13.4f);
        private const float AlturaAsiento = 0.55f;
        private const float LadoSalida = 0.55f;
        // La cadera queda a esta distancia de la punta del cabezal (m): la espalda en el respaldo y las piernas sobre el sillón.
        private const float DesdeCabezal = 0.75f;
        // El asiento va así de arriba de la superficie del sillón (m): con la AlturaCadera de la config, la cadera no se hunde en el cojín.
        private const float SobreCojin = 0.05f;
        // Tramo del perfil alrededor de la cadera donde se mide el cojín (m).
        private const float TramoCadera = 0.2f;
        private const float AnchoCentro = 0.25f;
        // Alto de la coronilla sobre el asiento con el jugador sentado (animado mide ~1.40 m) y lo que queda libre bajo el brazo (m).
        private const float CabezaSentado = 0.7f;
        private const float MargenBrazo = 0.12f;
        private const float BajadaMinima = 0.05f;
        private const float AlturaMira = 0.6f;
        private const float BajadaMaxima = 0.35f;
        private const int Tramos = 10;

        [MenuItem("Mecha Cyberpunk/S2n/Silla del Doc (T-813)")]
        public static void Colocar()
        {
            ConfigSillaClinica config = PrepararDatos();
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));

            Transform consultorio = ColocarS2.Buscar(RutaConsultorio);
            Transform sillon = consultorio != null ? consultorio.Find("I07") : null;
            Transform brazo = consultorio != null ? consultorio.Find("I06") : null;
            Bounds limites = sillon != null ? ArteT250.Limites(sillon.gameObject) : new Bounds(CentroSillon + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 2f));
            float piso = limites.min.y;

            GameObject raiz = new GameObject(NombreRaiz);
            // El punto de uso (E) va al lado del sillón, del lado del cuarto.
            raiz.transform.position = new Vector3(limites.max.x + LadoSalida * 0.5f, piso, limites.center.z);
            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(raiz.transform, false);
            // El cabezal es la punta alta del sillón (en el I07 queda hacia la calle, +Z): el jugador mira hacia la otra punta, con las piernas encima.
            List<(float z, float alto)> perfil = Perfil(sillon, limites);
            Vector3 frente = Vector3.forward;
            Vector3 posAsiento = new Vector3(limites.center.x, piso + Mathf.Min(AlturaAsiento, limites.size.y * 0.6f), limites.center.z);
            if (perfil.Count >= 4)
            {
                float altoMin = Mathf.Max(perfil[0].alto, perfil[1].alto);
                float altoMax = Mathf.Max(perfil[perfil.Count - 1].alto, perfil[perfil.Count - 2].alto);
                bool cabezalEnMax = altoMax >= altoMin;
                frente = cabezalEnMax ? Vector3.back : Vector3.forward;
                float zCadera = cabezalEnMax ? limites.max.z - DesdeCabezal : limites.min.z + DesdeCabezal;
                float cojin = perfil.Where(p => Mathf.Abs(p.z - zCadera) <= TramoCadera).Select(p => p.alto).DefaultIfEmpty(posAsiento.y).Max();
                posAsiento = new Vector3(limites.center.x, cojin + SobreCojin, zCadera);
            }
            asiento.SetPositionAndRotation(posAsiento, Quaternion.LookRotation(frente));
            Transform salida = new GameObject("Salida").transform;
            salida.SetParent(raiz.transform, false);
            salida.SetPositionAndRotation(new Vector3(limites.max.x + LadoSalida, piso, limites.center.z), Quaternion.LookRotation(Vector3.right));

            // El brazo baja hasta quedar un poco arriba de la cabeza del jugador sentado (no se la atraviesa).
            if (brazo != null)
            {
                float bajada = Mathf.Clamp(ArteT250.Limites(brazo.gameObject).min.y - (asiento.position.y + CabezaSentado + MargenBrazo), BajadaMinima, BajadaMaxima);
                SerializedObject sc = new SerializedObject(config);
                sc.FindProperty("bajadaBrazo").floatValue = bajada;
                // La cámara cercana mira un poco más arriba de la cadera: cabeza, torso y la punta del brazo en cuadro.
                sc.FindProperty("alturaMira").floatValue = AlturaMira;
                sc.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                ColocarS2.Informe.AppendLine("Bajada del brazo: " + bajada.ToString("F2") + " m");
            }
            SillaClinica silla = raiz.AddComponent<SillaClinica>();
            SerializedObject so = new SerializedObject(silla);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("asiento").objectReferenceValue = asiento;
            so.FindProperty("salida").objectReferenceValue = salida;
            so.FindProperty("brazo").objectReferenceValue = brazo;
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Perfil del sillón (z: alto): " + string.Join(" ", Perfil(sillon, limites).Select(p => p.z.ToString("F2") + ":" + p.alto.ToString("F2"))));
            ColocarS2.Informe.AppendLine("Silla del Doc en " + asiento.position.ToString("F2")
                + (sillon != null ? " sobre " + ColocarS2.Ruta(sillon) : " (FALTA el sillón I07: posición fija)")
                + (brazo != null ? ", brazo " + ColocarS2.Ruta(brazo) : ", sin brazo I06"));
            // EL DOC (T-546 lo dejó mirando al sillón, -X): de frente a la entrada (+Z), que es por donde llegas; lo de enfrente de él queda libre
            // (antes "frente al Doc" era encima del sillón). No se mueve de su lugar.
            Transform doc = ColocarS2.Buscar(RutaDoc);
            if (doc != null)
            {
                doc.rotation = Quaternion.LookRotation(Vector3.forward);
                ColocarS2.Informe.AppendLine("EL DOC mira a la entrada desde " + doc.position.ToString("F2"));
            }
            ColocarS2.Guardar("s2n-silla-clinica");
        }

        /// <summary>Alto de la superficie del sillón por tramos a lo largo (z), con los vértices de su malla cerca de la línea del centro.</summary>
        public static List<(float z, float alto)> Perfil(Transform sillon, Bounds limites)
        {
            List<(float, float)> perfil = new List<(float, float)>();
            if (sillon == null)
            {
                return perfil;
            }
            List<Vector3> puntos = new List<Vector3>();
            foreach (MeshFilter mf in sillon.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null)
                {
                    continue;
                }
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    Vector3 w = mf.transform.TransformPoint(v);
                    if (Mathf.Abs(w.x - limites.center.x) < AnchoCentro)
                    {
                        puntos.Add(w);
                    }
                }
            }
            for (int i = 0; i < Tramos; i++)
            {
                float z0 = limites.min.z + limites.size.z * i / Tramos;
                float z1 = limites.min.z + limites.size.z * (i + 1) / Tramos;
                float alto = puntos.Where(w => w.z >= z0 && w.z < z1).Select(w => w.y).DefaultIfEmpty(limites.min.y).Max();
                perfil.Add(((z0 + z1) * 0.5f, alto));
            }
            return perfil;
        }

        /// <summary>Lo que no toca la escena: la config de la silla.</summary>
        public static ConfigSillaClinica PrepararDatos()
        {
            ConfigSillaClinica config = AssetDatabase.LoadAssetAtPath<ConfigSillaClinica>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigSillaClinica>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
    }
}
