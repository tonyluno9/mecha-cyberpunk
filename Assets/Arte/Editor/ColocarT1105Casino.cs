using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1105 (playtest de Tony): arreglos del casino sin rehacerlo. La cajera R01 mira al salón (su frente de modelo es -Z local), las sillas de las
    /// mesas I11 miran a su mesa (el frente del modelo I12 es -Z local), SERGIO (el Hacker) con su escritorio I02, su silla I12 y su luz pasa a la
    /// esquina noroeste, sentado frente a la pantalla, y la pared sobre el arco de la entrada se tapa por dentro con el muro del casino.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1105Casino.Aplicar
    /// </summary>
    public static class ColocarT1105Casino
    {
        public const string NombreCajera = "Cajera_R01";
        public const string NombrePanel = "T1105_MuroSobreArco";
        public const string RutaHacker = "GTA_T417/Apoyo_SERGIO";
        private const string RutaInterior = ColocarS2oCasino.RutaLugar + "/Interior/";
        /// <summary>z del conjunto del Hacker (m): esquina noroeste del salón, lejos de la entrada y de la barra.</summary>
        public const float ZHacker = 38.5f;
        private const float MuroOeste = 61f;
        private const float MitadEscritorio = 0.445f;
        private const float HolguraMuro = 0.15f;
        public const float DistanciaSilla = 1.3f;
        /// <summary>Giros (°) del Hacker: el escritorio con el lado de uso hacia +x, la silla y SERGIO de frente al escritorio (-x).</summary>
        public const float GiroEscritorio = 270f;
        public const float GiroSillaHacker = 90f;
        public const float GiroHacker = 270f;
        /// <summary>Escala de la silla I12 de Sergio: el asiento (0.62 m en el modelo) queda a la altura de sus piernas cortas para que sus pies lleguen al piso.</summary>
        public const float EscalaSilla = 0.7f;
        /// <summary>Altura del asiento I12 sin escala sobre su base (m), medida en la malla.</summary>
        public const float AsientoSilla = 0.62f;
        /// <summary>Cuánto sobre el asiento queda la cadera de Sergio sentado (m).</summary>
        public const float CaderaSobreAsiento = 0.1f;
        private const float AlturaLuz = 3.2f;
        private const float LuzDesdeEscritorio = 0.5f;
        private const float AlturaPanelMin = 3.0f;
        private const float GrosorPanel = 0.3f;
        private const float MosaicoMuro = 5.5f;

        /// <summary>x del escritorio I02 pegado al muro oeste (m).</summary>
        public static float XEscritorio => MuroOeste + HolguraMuro + MitadEscritorio;
        /// <summary>x de la silla, a <see cref="DistanciaSilla"/> hacia el salón (m).</summary>
        public static float XSilla => XEscritorio + DistanciaSilla;

        [MenuItem("Mecha Cyberpunk/T-1105/Arreglos del casino")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Cajera();
            Sillas();
            Hacker();
            PanelSobreArco();
            Physics.SyncTransforms();
            ColocarS2.Guardar("t1105-casino");
        }

        private static Transform DeRaiz(string raiz, string nombre)
        {
            GameObject g = GameObject.Find(raiz);
            return g == null ? null : g.transform.Cast<Transform>().FirstOrDefault(t => t.name == nombre);
        }

        private static void Cajera()
        {
            Transform c = DeRaiz(ColocarT930Casino.Raiz, NombreCajera);
            if (c == null) { ColocarS2.Informe.AppendLine("FALTA " + NombreCajera); return; }
            c.rotation = Quaternion.Euler(0f, ColocarT930Casino.GiroCajera, 0f);
            ColocarS2.Informe.AppendLine("Cajera mirando al salón (giro " + ColocarT930Casino.GiroCajera + ")");
        }

        /// <summary>Cada silla I12 de las mesas mira al centro de su mesa I11 más cercana. El frente del modelo es -Z, así que el forward va hacia atrás.</summary>
        private static void Sillas()
        {
            GameObject raiz = GameObject.Find(ColocarS2oCasino.Raiz);
            if (raiz == null) { ColocarS2.Informe.AppendLine("FALTA " + ColocarS2oCasino.Raiz); return; }
            Transform[] mesas = raiz.transform.Cast<Transform>().Where(t => t.name == "I11").ToArray();
            foreach (Transform silla in raiz.transform.Cast<Transform>().Where(t => t.name == "I12"))
            {
                Transform mesa = mesas.OrderBy(m => Plano(m.position - silla.position).sqrMagnitude).First();
                silla.rotation = YawSilla(silla.position, mesa.position);
            }
            ColocarS2.Informe.AppendLine("Sillas orientadas a su mesa");
        }

        public static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        /// <summary>Rotación para que el frente (-Z local) del modelo I12 mire desde la silla hacia el objetivo.</summary>
        public static Quaternion YawSilla(Vector3 silla, Vector3 objetivo)
        {
            return Quaternion.LookRotation(-Plano(objetivo - silla).normalized, Vector3.up);
        }

        private static void Hacker()
        {
            Transform escritorio = DeRaiz(ColocarT930Casino.Raiz, "I02");
            Transform silla = DeRaiz(ColocarT930Casino.Raiz, "I12");
            Transform luz = DeRaiz(ColocarT930Casino.Raiz, "LuzHacker");
            Transform sergio = ColocarS2.Buscar(RutaHacker);
            if (escritorio == null || silla == null || sergio == null) { ColocarS2.Informe.AppendLine("FALTA algo del Hacker"); return; }
            escritorio.SetPositionAndRotation(new Vector3(XEscritorio, escritorio.position.y, ZHacker), Quaternion.Euler(0f, GiroEscritorio, 0f));
            silla.SetPositionAndRotation(new Vector3(XSilla, silla.position.y, ZHacker), Quaternion.Euler(0f, GiroSillaHacker, 0f));
            silla.localScale = Vector3.one * EscalaSilla;
            PoseSentado pose = sergio.GetComponentInChildren<PoseSentado>(true);
            if (pose != null)
            {
                SerializedObject so = new SerializedObject(pose);
                so.FindProperty("alturaCadera").floatValue = AsientoSilla * EscalaSilla + CaderaSobreAsiento;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (luz != null)
            {
                luz.position = new Vector3(XEscritorio + LuzDesdeEscritorio, AlturaLuz, ZHacker);
            }
            sergio.SetPositionAndRotation(new Vector3(XSilla, sergio.position.y, ZHacker), Quaternion.Euler(0f, GiroHacker, 0f));
            ColocarS2.Informe.AppendLine("Hacker en " + sergio.position.ToString("F2") + ", escritorio en " + escritorio.position.ToString("F2"));
        }

        /// <summary>Panel del muro del casino sobre el arco de la entrada: la pared de fachada (ocre con ventana) se veía por dentro.</summary>
        private static void PanelSobreArco()
        {
            Transform izq = ColocarS2.Buscar(RutaInterior + "Revestimiento/MuroFrenteIzq");
            Transform der = ColocarS2.Buscar(RutaInterior + "Revestimiento/MuroFrenteDer");
            Transform viejo = DeRaiz(ColocarT930Casino.Raiz, NombrePanel);
            if (viejo != null) { Object.DestroyImmediate(viejo.gameObject); }
            if (izq == null || der == null) { ColocarS2.Informe.AppendLine("FALTAN los muros del frente"); return; }
            Renderer ri = izq.GetComponent<Renderer>();
            Bounds bi = ri.bounds;
            Bounds bd = der.GetComponent<Renderer>().bounds;
            Bounds primero = bi.min.z < bd.min.z ? bi : bd;
            Bounds segundo = bi.min.z < bd.min.z ? bd : bi;
            float zMin = primero.max.z;
            float zMax = segundo.min.z;
            float alto = Mathf.Max(bi.max.y, bd.max.y);
            float ancho = zMax - zMin;
            Vector3 centro = new Vector3(bi.center.x, (AlturaPanelMin + alto) * 0.5f, (zMin + zMax) * 0.5f);
            Vector3 tam = new Vector3(Mathf.Max(bi.size.x, GrosorPanel), alto - AlturaPanelMin, ancho);
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = NombrePanel;
            p.transform.SetParent(GameObject.Find(ColocarT930Casino.Raiz).transform, false);
            p.transform.position = centro;
            p.transform.localScale = tam;
            Texture2D tex = ri.sharedMaterial.GetTexture("_BaseMap") as Texture2D;
            // El mosaico de la textura del muro mide unos 5.5 m: el panel lleva esa escala, no la del cubo de 1 m.
            Material m = ColocarT930Casino.MatTex("T1105_MuroSobreArco", tex, new Vector2(ancho / MosaicoMuro, tam.y / MosaicoMuro));
            Renderer r = p.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ColocarS2.Informe.AppendLine("Panel sobre el arco en " + centro.ToString("F2") + " tamaño " + tam.ToString("F2") + " (hueco z " + zMin.ToString("F2") + " a " + zMax.ToString("F2") + ")");
        }
    }
}
