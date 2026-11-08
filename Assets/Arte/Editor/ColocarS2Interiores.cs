using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 zona 3 (docs/playtest-05nov.md): el consultorio del Doc (I+D+I) y el taller de Tino (CIC) se arman en su propio local (están en la ruta de la
    /// demo: el Doc y Tino no se mueven) según docs/referencias-interiores/consultorio-doc.png y taller-tino.png; el taller del mecánico (Canepa, frente a
    /// la casa) lo arma ColocarS2nPuertas dentro del Edificio_45 (T-804; antes era un cuarto aparte con fundido). Idempotente: rehace la raíz S2_Interiores y el cuarto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Interiores.Colocar
    /// </summary>
    public static class ColocarS2Interiores
    {
        public const string NombreRaiz = "S2_Interiores";
        public const string NombreTallerMecanico = "Cuarto_TallerMecanico";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        private const string T = "Assets/Arte/TallerCasa/Prefabs/";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const string L = "Assets/Arte/Callejon/Prefabs/";
        private const string V = "Assets/Arte/Vehiculos/Prefabs/";
        private static readonly Color Calido = new Color(1f, 0.72f, 0.42f);
        private static readonly Color Turquesa = new Color(0.3f, 1f, 0.9f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);
        private static readonly Color Cian = new Color(0.4f, 0.88f, 0.95f);
        private static readonly Color Fluorescente = new Color(0.88f, 0.96f, 1f);
        /// <summary>Tinte del azulejo gris del consultorio (pared) y de su piso, un poco más oscuro.</summary>
        private static readonly Color AzulejoConsultorio = new Color(0.66f, 0.86f, 0.88f);
        private static readonly Color PisoConsultorio = new Color(0.3f, 0.38f, 0.4f);
        /// <summary>Cuánto del color del azulejo X02 se queda en su copia gris (0 = gris puro).</summary>
        private const float ColorQueQueda = 0.15f;

        [MenuItem("Mecha Cyberpunk/S2/3 Interiores (Doc, Tino, mecánico)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Consultorio(raiz);
            TallerTino(raiz);
            ColocarS2nPuertas.TallerMecanico(); // T-804: el taller ya no es un cuarto aparte: va dentro del Edificio_45
            ColocarT550.AislarDelSol();
            ColocarS2.Guardar("s2-interiores");
        }

        // ---------------------------------------------------------------- Consultorio del Doc (I+D+I)

        private static void Consultorio(Transform raiz)
        {
            Apagar("T546_Interiores/ConsultorioDoc");
            // La clínica de El Mercado que T-531 mudó aquí: su mostrador, letrero, cruz y tubo eran cubos. La interacción va por posición y se queda.
            foreach (string n in new[] { "MostradorClinica/Mesa", "MostradorClinica/Letrero", "CruzVerde", "Lampara_Tubo" })
            {
                Transform t = ColocarS2.Buscar("Barrio/Manzana_2/Lugar_MercadoNegro/Interior/Clinica/" + n);
                if (t != null)
                {
                    foreach (Renderer rend in t.GetComponents<Renderer>())
                    {
                        rend.enabled = false;
                    }
                    foreach (Collider col in t.GetComponents<Collider>())
                    {
                        col.enabled = false;
                    }
                }
            }
            Transform r = Grupo(raiz, "ConsultorioDoc");
            // Local de T-531: x -104 a -96, fondo z -16, frente z -10.2 (hacia la calle, +Z), piso 0.22, techo 3.75.
            Caja caja = new Caja(-103.9f, -96.1f, -15.9f, -10.45f, 0.22f, 3.72f);
            // T-812: el azulejo X02 es verde; el consultorio va con su copia gris azulada (como la referencia: blanco-gris con luz cian).
            Material azulejo = ColocarT550.Mat("S2_Pared_Consultorio", AzulejoConsultorio);
            azulejo.SetTexture("_BaseMap", TexturaGris(2));
            azulejo.SetTextureScale("_BaseMap", AssetDatabase.LoadAssetAtPath<Material>(ImportarT550.RutaMaterialPared(2)).GetTextureScale("_BaseMap"));
            EditorUtility.SetDirty(azulejo);
            Material piso = ColocarT550.Mat("S2_Piso_Consultorio", PisoConsultorio);
            piso.SetTexture("_BaseMap", azulejo.GetTexture("_BaseMap"));
            piso.SetTextureScale("_BaseMap", azulejo.GetTextureScale("_BaseMap"));
            EditorUtility.SetDirty(piso);
            AssetDatabase.SaveAssets();
            Forrar(r, caja, 2, 5, 2, 0.45f, frenteHaciaMasZ: true, centroVano: VanoConsultorioX, anchoVano: 2.6f, paredPropia: azulejo, pisoPropio: piso);

            GameObject sillon = Modelo(r, I + "I07", new Vector3(-102.1f, 0f, -13.4f), 0f, caja.Piso, ejeLargoEnZ: true);
            Modelo(r, I + "I06", new Vector3(-102.1f, 0f, -13.4f), 0f, caja.Techo, colgar: true);
            // T-812: las vitrinas iban 12 cm dentro de las paredes y encima de los huacales y del tambo; los monitores de ruedas iban sobre el gabinete.
            Modelo(r, I + "I08", new Vector3(-103.45f, 0f, -12.35f), 90f, caja.Piso);            // vitrina de implantes, pared izquierda
            Modelo(r, I + "I08", new Vector3(-96.55f, 0f, -12.35f), 270f, caja.Piso);            // y en la derecha
            Modelo(r, I + "I09", new Vector3(-103.6f, 0f, -14.6f), 90f, caja.Piso);              // lavabos
            Modelo(r, I + "I09", new Vector3(-96.4f, 0f, -13.8f), 270f, caja.Piso);
            Modelo(r, T + "T08_RepisaTaller", new Vector3(-102.1f, 0f, -15.55f), 180f, caja.Piso, escala: 0.85f);  // repisa de medicinas al fondo
            Modelo(r, C + "CY05_ServidorCallejero", new Vector3(-96.95f, 0f, -15.4f), 180f, caja.Piso);
            Modelo(r, T + "T04_GabineteHerramientas", new Vector3(-98.9f, 0f, -15.45f), 180f, caja.Piso, escala: 0.8f);
            Modelo(r, I + "I10", new Vector3(-100.8f, 0f, -14.95f), 200f, caja.Piso, escala: 1.35f);   // monitores de signos vitales, de pie junto al sillón
            Modelo(r, I + "I10", new Vector3(-99.9f, 0f, -15.2f), 165f, caja.Piso, escala: 1.35f);
            Modelo(r, C + "C17_Huacales", new Vector3(-103.25f, 0f, -10.95f), 10f, caja.Piso, escala: 0.8f);
            Modelo(r, C + "C16_Tambo", new Vector3(-96.75f, 0f, -10.85f), 0f, caja.Piso);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-103.85f, 0f, -13.9f), 90f, caja.Piso + 1.1f, sinCollider: true);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-97.95f, 0f, -15.85f), 180f, caja.Piso + 1.1f, sinCollider: true);

            Transform luces = Grupo(r, "Luces");
            Luz(luces, "Tubo_Cian_1", new Vector3(-101.5f, caja.Techo - 0.2f, -12.4f), Cian, 6f, 8f);
            Luz(luces, "Tubo_Cian_2", new Vector3(-98.5f, caja.Techo - 0.2f, -14.2f), Cian, 5.5f, 8f);
            Luz(luces, "Tubo_Fluorescente", new Vector3(-100f, caja.Techo - 0.2f, -11.2f), Fluorescente, 4f, 7f);
            Luz(luces, "Relleno_Fondo", new Vector3(-100f, 2.2f, -15.2f), Cian, 2.5f, 6f);
            Luz(luces, "Vitrina_Izq", new Vector3(-103.1f, 1.4f, -12.35f), Cian, 1.5f, 2f);
            Luz(luces, "Vitrina_Der", new Vector3(-96.9f, 1.4f, -12.35f), Cian, 1.5f, 2f);
            Luz(luces, "Foco_Calido_Pared", new Vector3(-103.5f, 2.6f, -12.8f), Calido, 1.6f, 4f);
            Luz(luces, "Brillo_Monitores", new Vector3(-100.35f, caja.Piso + 1.3f, -14.7f), Cian, 1.2f, 2.5f);
            Foco(luces, "Lampara_Cirugia", new Vector3(-102.1f, caja.Techo - 1.1f, -13.4f), Calido, 9f, 4.5f, 55f);
            ApagarLuzDelLocal("T531_Locales/IDI/Luz_IDI");
            OcultarCascaron("T531_Locales/IDI");
            ColocarS2.Informe.AppendLine("Consultorio: " + r.childCount + " piezas; sillón " + (sillon != null ? ArteT250.Limites(sillon).size.ToString("F2") : "FALTA"));
        }

        // ---------------------------------------------------------------- Taller de Tino (CIC)

        /// <summary>Rehace solo el consultorio del Doc dentro de S2_Interiores (T-812). Después hay que volver a poner la silla (ColocarS2nSillaClinica).</summary>
        public static void RehacerConsultorio()
        {
            Transform raiz = ColocarS2.Buscar(NombreRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NombreRaiz).transform;
            }
            Transform viejo = raiz.Find("ConsultorioDoc");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Consultorio(raiz);
        }

        /// <summary>
        /// Copia gris (casi sin color, con <see cref="ColorQueQueda"/>) de la textura del kit X0n, en Assets/Arte/Interiores/Texturas/X0n_Gris.png.
        /// Se rehace con código de editor desde el PNG original (no hace falta que la textura se pueda leer).
        /// </summary>
        public static Texture2D TexturaGris(int n)
        {
            string origen = "Assets/Arte/Interiores/Texturas/X0" + n + ".png";
            string destino = "Assets/Arte/Interiores/Texturas/X0" + n + "_Gris.png";
            if (!System.IO.File.Exists(destino))
            {
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(System.IO.File.ReadAllBytes(origen));
                Color[] px = t.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    float g = px[i].grayscale;
                    px[i] = Color.Lerp(new Color(g, g, g, px[i].a), px[i], ColorQueQueda);
                }
                t.SetPixels(px);
                t.Apply();
                System.IO.File.WriteAllBytes(destino, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(destino);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(origen);
                TextureImporter nuevo = (TextureImporter)AssetImporter.GetAtPath(destino);
                if (imp != null && nuevo != null)
                {
                    nuevo.wrapMode = imp.wrapMode;
                    nuevo.maxTextureSize = imp.maxTextureSize;
                    nuevo.mipmapEnabled = imp.mipmapEnabled;
                    nuevo.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
        }

        /// <summary>Rehace solo el taller de Tino dentro de S2_Interiores (T-811), sin tocar el consultorio ni el taller del mecánico.</summary>
        public static void RehacerTallerTino()
        {
            Transform raiz = ColocarS2.Buscar(NombreRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NombreRaiz).transform;
            }
            Transform viejo = raiz.Find("TallerTino");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            TallerTino(raiz);
        }

        // T-811 (playtest del 6-nov): Tino (de pie en -74.5, 13.5, mirando a la calle) ya no atraviesa un banco; los bancos van a escala de una
        // mesa de trabajo (cubierta a ~0.8 m y ~2 m de largo, contra el jugador animado de 1.40 m); el pizarrón va pegado a la pared sin encimarse;
        // la lámpara, el dron y el monitor se apoyan en su cubierta real (no en lo alto del tablero perforado). Como la referencia: serie de focos
        // cálidos, tablero con herramientas, repisas con cajas, monitores, drones colgando de cables, tambos, llantas y cables.
        private static void TallerTino(Transform raiz)
        {
            Apagar("T546_Interiores/TallerTino");
            Transform r = Grupo(raiz, "TallerTino");
            // El mostrador y la repisa de cubos de T-417 se quedan solo como colisión (no se ven).
            foreach (string n in new[] { "Mesa", "MesaTapa", "Pared", "Repisa_0", "Repisa_1", "Repisa_2" })
            {
                OcultarCubo("GTA_T417/Lugar_CIC/" + n);
            }
            Transform cic = ColocarS2.Buscar("GTA_T417/Lugar_CIC");
            if (cic != null)
            {
                foreach (Transform t in cic.Cast<Transform>().Where(t => t.name.StartsWith("Caja_")))
                {
                    OcultarCubo("GTA_T417/Lugar_CIC/" + t.name);
                }
            }
            // Local de T-531: x -77.3 a -69.3, fondo z 16, frente z 10.2 (hacia la calle, -Z).
            Caja caja = new Caja(-77.2f, -69.4f, 10.45f, 15.9f, 0.22f, 3.72f);
            Forrar(r, caja, 3, 5, 1, 0.38f, frenteHaciaMasZ: false, centroVano: -73.3f, anchoVano: 2.6f);

            // Banco principal (el de BancoArmas), a la derecha de Tino; el otro, contra la pared izquierda (su tablero perforado trae las herramientas).
            GameObject banco = Mesa(r, T + "T03_BancoTrabajo", CentroBancoTino, 0f, caja.Piso);
            float cubierta = banco != null ? Cubierta(banco) : caja.Piso + CubiertaMesa;
            Mesa(r, T + "T03_BancoTrabajo", new Vector3(-76.62f, 0f, 13.1f), 270f, caja.Piso);       // el tablero perforado contra la pared
            Modelo(r, T + "T06_Soldadora", new Vector3(-71.05f, 0f, 12.15f), 200f, caja.Piso, escala: 0.8f);
            Modelo(r, I + "I21", new Vector3(-71.75f, 0f, 13.75f), 210f, cubierta, escala: 0.8f, sinCollider: true);       // lámpara de brazo
            Modelo(r, C + "CY07b_Dron", new Vector3(-72.75f, 0f, 13.3f), 30f, cubierta, escala: 0.35f, sinCollider: true);  // el dron en reparación

            // Fondo: repisas con cajas, gabinete con monitor, el pizarrón pegado a la pared y la compu en la esquina derecha.
            Modelo(r, T + "T08_RepisaTaller", new Vector3(-75.25f, 0f, 15.58f), 180f, caja.Piso, escala: 0.95f);
            GameObject drones = Modelo(r, I + "I20", new Vector3(-76.7f, 0f, 15.15f), 90f, caja.Piso);                       // estante de drones
            GameObject gabinete = Modelo(r, T + "T04_GabineteHerramientas", new Vector3(-73.75f, 0f, 15.52f), 180f, caja.Piso, escala: 0.8f);
            Modelo(r, I + "I10", new Vector3(-73.75f, 0f, 15.5f), 170f, gabinete != null ? Cubierta(gabinete) : caja.Piso + 0.9f, escala: 0.8f, sinCollider: true);
            Cuadro(r, "Pizarron", "Assets/Arte/Interiores/Materiales/T546_Pizarron_T546_Pizarron.mat", new Vector3(-71.6f, 2.0f, caja.Z1 - SeparacionPared), Vector3.back, new Vector2(2.0f, 1.25f));
            GameObject escritorio = Modelo(r, I + "I02", new Vector3(-69.85f, 0f, 14.55f), 270f, caja.Piso);                 // compu con monitores
            float tablero = escritorio != null ? ArteT250.Limites(escritorio).max.y : caja.Piso + 1f;
            Modelo(r, I + "I03", new Vector3(-70.75f, 0f, 14.2f), 100f, caja.Piso);                                         // su silla
            Modelo(r, C + "C16_Tambo", new Vector3(-70.95f, 0f, 15.45f), 0f, caja.Piso);
            Modelo(r, C + "C16_Tambo", new Vector3(-69.85f, 0f, 12.75f), 30f, caja.Piso);

            // Frente: generador y huacales a la izquierda del vano, llantas a la derecha.
            Modelo(r, T + "T05_Generador", new Vector3(-76.35f, 0f, 11.3f), 0f, caja.Piso, escala: 0.9f);
            Modelo(r, C + "C17_Huacales", new Vector3(-75.25f, 0f, 10.95f), 10f, caja.Piso, escala: 0.8f);
            Modelo(r, L + "L04_Llantas", new Vector3(-70.2f, 0f, 11.15f), 0f, caja.Piso);

            // Drones colgando del techo con su cable, y el mazo de cables de la pared derecha.
            Colgado(r, C + "CY07b_Dron", new Vector3(-75.0f, 0f, 12.2f), 0f, caja.Techo, 0.85f, 0.5f);
            Colgado(r, C + "CY07b_Dron", new Vector3(-71.3f, 0f, 12.1f), 140f, caja.Techo, 1.0f, 0.5f);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-69.45f, 0f, 13.6f), 270f, caja.Piso + 1.1f, sinCollider: true);

            Transform luces = Grupo(r, "Luces");
            SerieFocos(r, new Vector3(-77.1f, caja.Techo - 0.15f, 11.0f), new Vector3(-69.5f, caja.Techo - 0.2f, 14.8f), 16);
            SerieFocos(r, new Vector3(-77.1f, caja.Techo - 0.2f, 15.5f), new Vector3(-69.5f, caja.Techo - 0.15f, 11.6f), 16);
            Luz(luces, "Serie_1", new Vector3(-75.0f, caja.Techo - 0.6f, 12.5f), Calido, 2.4f, 6f);
            Luz(luces, "Serie_2", new Vector3(-71.5f, caja.Techo - 0.6f, 14.0f), Calido, 2.4f, 6f);
            Luz(luces, "Tubo_Calido_Pared", new Vector3(-76.7f, 2.7f, 13.1f), Calido, 2.4f, 5f);   // sobre el tablero de herramientas
            Foco(luces, "Lampara_Banco", new Vector3(-71.95f, cubierta + 0.9f, 13.5f), Calido, 6f, 3.5f, 70f);
            Luz(luces, "Brillo_Monitores", new Vector3(-70.2f, tablero + 0.2f, 14.3f), Turquesa, 1.6f, 3f);
            Luz(luces, "Relleno", new Vector3(-73.3f, 2.6f, 11.5f), Calido, 2.0f, 7f);
            ApagarLuzDelLocal("T531_Locales/CIC/Luz_CIC");
            OcultarCascaron("T531_Locales/CIC");
            Luz(luces, "Relleno_Fondo", new Vector3(-72.0f, 2.4f, 15.0f), Calido, 2.4f, 6f);
            ColocarS2.Informe.AppendLine("Taller de Tino: " + r.childCount + " piezas; cubierta del banco a " + (cubierta - caja.Piso).ToString("F2") + " m"
                + (drones == null ? " (FALTA I20)" : ""));
        }

        /// <summary>Centro del banco principal del CIC (el de BancoArmas): a la derecha de Tino, que está en (-74.5, 13.5), y lo bastante lejos para que
        /// frente a Tino (hasta ~2.2 m) la E sea de él y no del punto de uso del banco.</summary>
        public static readonly Vector3 CentroBancoTino = new Vector3(-72.3f, 0.22f, 13.45f);
        /// <summary>Alto de la cubierta de una mesa de trabajo sobre el piso (m), contra el jugador animado de 1.40 m.</summary>
        public const float CubiertaMesa = 0.8f;
        /// <summary>Lo que se despega de la pared un cuadro (m): sin encimarse con ella.</summary>
        private const float SeparacionPared = 0.03f;

        /// <summary>Largo de una mesa de trabajo (m).</summary>
        public const float LargoMesa = 2.0f;

        /// <summary>Un banco o mesa escalado para que su cubierta (la superficie horizontal más grande y alta) quede a <see cref="CubiertaMesa"/>
        /// y su lado largo mida a lo más <see cref="LargoMesa"/> (escala en planta y en alto por separado).</summary>
        private static GameObject Mesa(Transform padre, string ruta, Vector3 centro, float yaw, float piso)
        {
            GameObject g = Modelo(padre, ruta, centro, yaw, piso);
            if (g == null)
            {
                return null;
            }
            float alto = Cubierta(g) - piso;
            if (alto > 0.1f)
            {
                float fy = CubiertaMesa / alto;
                Bounds b0 = ArteT250.Limites(g);
                float fxz = Mathf.Min(fy, LargoMesa / Mathf.Max(b0.size.x, b0.size.z));
                g.transform.localScale = Vector3.Scale(g.transform.localScale, new Vector3(fxz, fy, fxz));
                Bounds b = ArteT250.Limites(g);
                g.transform.position += new Vector3(centro.x - b.center.x, piso - b.min.y, centro.z - b.center.z);
            }
            return g;
        }

        /// <summary>
        /// Alto (m, mundo) de la cubierta de un mueble: de sus caras que miran hacia arriba, la más alta cuya área es al menos la mitad de la mayor
        /// (en un banco con tablero perforado es la mesa, no el canto del tablero).
        /// </summary>
        public static float Cubierta(GameObject g)
        {
            Dictionary<int, float> areas = new Dictionary<int, float>();
            const float Tramo = 0.02f;
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>())
            {
                Mesh m = mf.sharedMesh;
                if (m == null)
                {
                    continue;
                }
                Vector3[] v = m.vertices;
                int[] t = m.triangles;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = mf.transform.TransformPoint(v[t[i]]);
                    Vector3 b = mf.transform.TransformPoint(v[t[i + 1]]);
                    Vector3 c = mf.transform.TransformPoint(v[t[i + 2]]);
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    float area = n.magnitude * 0.5f;
                    if (area < 1e-6f || Mathf.Abs(n.normalized.y) < 0.9f)
                    {
                        continue;
                    }
                    int k = Mathf.RoundToInt((a.y + b.y + c.y) / 3f / Tramo);
                    areas[k] = (areas.TryGetValue(k, out float ya) ? ya : 0f) + area;
                }
            }
            if (areas.Count == 0)
            {
                return ArteT250.Limites(g).max.y;
            }
            float mayor = areas.Values.Max();
            return areas.Where(p => p.Value >= mayor * 0.5f).Max(p => p.Key) * Tramo;
        }

        /// <summary>Un modelo colgando del techo con su cable (LineRenderer negro) hasta la tapa.</summary>
        private static void Colgado(Transform padre, string ruta, Vector3 centro, float yaw, float techo, float bajada, float escala)
        {
            GameObject g = Modelo(padre, ruta, centro, yaw, techo - bajada, escala: escala, colgar: true);
            if (g == null)
            {
                return;
            }
            Bounds b = ArteT250.Limites(g);
            LineRenderer lr = new GameObject("Cable").AddComponent<LineRenderer>();
            lr.transform.SetParent(g.transform, true);
            lr.positionCount = 2;
            lr.SetPositions(new[] { new Vector3(b.center.x, techo, b.center.z), new Vector3(b.center.x, b.max.y - 0.02f, b.center.z) });
            lr.widthMultiplier = 0.015f;
            lr.sharedMaterial = ColocarT550.Mat("S2_Cable", new Color(0.04f, 0.04f, 0.045f));
            lr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- Ayudas

        /// <summary>T-890: el vano del frente del consultorio, donde cae la puerta de la fachada P12.</summary>
        public const float VanoConsultorioX = -102f;

        public readonly struct Caja
        {
            public readonly float X0, X1, Z0, Z1, Piso, Techo;
            public Caja(float x0, float x1, float z0, float z1, float piso, float techo)
            {
                X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Piso = piso; Techo = techo;
            }
        }

        /// <summary>Paredes, techo y piso del kit X por dentro del local, y el frente cerrado con un vano de entrada (ladrillo X03 por fuera).</summary>
        private static void Forrar(Transform r, Caja k, int pared, int techo, int piso, float oscuroPiso, bool frenteHaciaMasZ, float centroVano, float anchoVano,
            Material paredPropia = null, Material pisoPropio = null)
        {
            Material mp = paredPropia != null ? paredPropia : ColocarT550.Mat("S2_Pared_X0" + pared, new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(pared));
            Material mt = ColocarT550.Mat("S2_Techo_X0" + techo, new Color(0.5f, 0.47f, 0.44f), ImportarT550.RutaMaterialPared(techo));
            Material mf = pisoPropio != null ? pisoPropio : ColocarT550.Mat("S2_Piso_X0" + piso, new Color(0.42f, 0.4f, 0.38f) * oscuroPiso * 2f, ImportarT550.RutaMaterialPared(piso));
            Material ladrillo = ColocarT550.Mat("S2_Pared_X03", new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(3));
            Transform f = Grupo(r, "Forro");
            float y0 = k.Piso + 0.012f, y1 = k.Techo;
            Vector3 p(float x, float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(f, "Piso", mf, p(k.X0, y0, k.Z0), p(k.X0, y0, k.Z1), p(k.X1, y0, k.Z1), p(k.X1, y0, k.Z0), Vector3.up, -0.05f);
            ColocarT550.Superficie(f, "Techo", mt, p(k.X0, y1, k.Z0), p(k.X1, y1, k.Z0), p(k.X1, y1, k.Z1), p(k.X0, y1, k.Z1), Vector3.down, 0.05f);
            ColocarT550.Superficie(f, "Pared_Izquierda", mp, p(k.X0, y0, k.Z0), p(k.X0, y1, k.Z0), p(k.X0, y1, k.Z1), p(k.X0, y0, k.Z1), Vector3.right, -0.05f);
            ColocarT550.Superficie(f, "Pared_Derecha", mp, p(k.X1, y0, k.Z1), p(k.X1, y1, k.Z1), p(k.X1, y1, k.Z0), p(k.X1, y0, k.Z0), Vector3.left, 0.05f);
            float zFondo = frenteHaciaMasZ ? k.Z0 : k.Z1;
            float zFrente = frenteHaciaMasZ ? k.Z1 : k.Z0;
            Vector3 haciaDentroFondo = frenteHaciaMasZ ? Vector3.forward : Vector3.back;
            ColocarT550.Superficie(f, "Pared_Fondo", mp, p(k.X0, y0, zFondo), p(k.X0, y1, zFondo), p(k.X1, y1, zFondo), p(k.X1, y0, zFondo), haciaDentroFondo, frenteHaciaMasZ ? -0.05f : 0.05f);
            // Frente: dos paños a los lados del vano y uno encima (alto del vano 2.4 m), por dentro y por fuera.
            float v0 = centroVano - anchoVano * 0.5f, v1 = centroVano + anchoVano * 0.5f, alto = k.Piso + 2.4f;
            Vector3 haciaDentro = -haciaDentroFondo;   // del frente hacia dentro
            Vector3 haciaFuera = haciaDentroFondo;
            void Pano(string n, float xa, float xb, float ya, float yb)
            {
                ColocarT550.Superficie(f, n + "_Dentro", mp, p(xa, ya, zFrente - haciaFuera.z * 0.02f), p(xa, yb, zFrente - haciaFuera.z * 0.02f), p(xb, yb, zFrente - haciaFuera.z * 0.02f), p(xb, ya, zFrente - haciaFuera.z * 0.02f), haciaDentro, haciaFuera.z * 0.08f);
                ColocarT550.Superficie(f, n + "_Fuera", ladrillo, p(xa, ya, zFrente + haciaFuera.z * 0.1f), p(xa, yb, zFrente + haciaFuera.z * 0.1f), p(xb, yb, zFrente + haciaFuera.z * 0.1f), p(xb, ya, zFrente + haciaFuera.z * 0.1f), haciaFuera, 0f);
            }
            Pano("Frente_Izq", k.X0, v0, y0, y1);
            Pano("Frente_Der", v1, k.X1, y0, y1);
            Pano("Frente_Arriba", v0, v1, alto, y1);
            // Los superficies de frente no llevan collider de más en el vano: el collider de cada paño cubre solo su paño.
            foreach (Renderer rend in f.GetComponentsInChildren<Renderer>())
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
            // El NavMesh se horneó con el local abierto: el frente nuevo talla su hueco para que la IA no camine a través de él.
            foreach (Transform t in f.Cast<Transform>().Where(t => t.name.StartsWith("Frente_") && t.name.EndsWith("_Dentro")))
            {
                Obstaculo(t.gameObject);
            }
        }

        /// <summary>Obstáculo de navegación que talla el NavMesh horneado, con la medida de su BoxCollider (un poco más grueso).</summary>
        public static void Obstaculo(GameObject g)
        {
            BoxCollider caja = g.GetComponent<BoxCollider>();
            UnityEngine.AI.NavMeshObstacle o = g.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (o == null)
            {
                o = g.AddComponent<UnityEngine.AI.NavMeshObstacle>(); // sin ??: el null de Unity no es null de C#
            }
            o.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            if (caja != null)
            {
                o.center = caja.center;
                o.size = caja.size + new Vector3(0.1f, 0f, 0.1f);
            }
            else
            {
                Bounds b = ArteT250.Limites(g);
                o.center = g.transform.InverseTransformPoint(b.center);
                o.size = new Vector3(b.size.x / Mathf.Max(g.transform.lossyScale.x, 0.01f), b.size.y / Mathf.Max(g.transform.lossyScale.y, 0.01f), b.size.z / Mathf.Max(g.transform.lossyScale.z, 0.01f));
            }
            o.carving = true;
            o.carveOnlyStationary = true;
        }

        /// <summary>Pone un prefab con su centro en planta en 'centro' (x, z del mundo o local del cuarto ya convertido), girado 'yaw', apoyado en 'y'
        /// (o colgando de 'y' si 'colgar'). 'ejeLargoEnZ' gira 90° más si el lado largo queda en X.</summary>
        public static GameObject Modelo(Transform padre, string ruta, Vector3 centro, float yaw, float y, float escala = 1f, bool colgar = false, bool sinCollider = false, bool ejeLargoEnZ = false)
        {
            string archivo = ruta.EndsWith(".prefab") ? ruta : ruta + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(archivo);
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA " + archivo);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = prefab.name;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            g.transform.localScale *= escala;
            if (ejeLargoEnZ)
            {
                Bounds b0 = ArteT250.Limites(g);
                if (b0.size.x > b0.size.z)
                {
                    g.transform.rotation = Quaternion.Euler(0f, yaw + 90f, 0f);
                }
            }
            Bounds b = ArteT250.Limites(g);
            Vector3 d = new Vector3(centro.x - b.center.x, colgar ? y - b.max.y : y - b.min.y, centro.z - b.center.z);
            g.transform.position += d;
            if (colgar || sinCollider)
            {
                foreach (Collider col in g.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            return g;
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static void Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
        {
            Light l = new GameObject(nombre).AddComponent<Light>();
            l.transform.SetParent(padre, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }

        private static void Foco(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance, float angulo)
        {
            Light l = new GameObject(nombre).AddComponent<Light>();
            l.transform.SetParent(padre, false);
            l.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
            l.type = LightType.Spot;
            l.spotAngle = angulo;
            l.innerSpotAngle = angulo * 0.5f;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.Soft;
        }

        // Serie de focos colgando en curva entre dos puntos del techo: cable negro (LineRenderer) y focos cálidos emisivos.
        private static void SerieFocos(Transform padre, Vector3 a, Vector3 b, int focos)
        {
            Transform serie = Grupo(padre, "SerieFocos");
            Material foco = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Interiores/Materiales/T546_Foco.mat");
            Material cable = ColocarT550.Mat("S2_Cable", new Color(0.04f, 0.04f, 0.045f));
            const int Tramos = 24;
            Vector3[] puntos = new Vector3[Tramos + 1];
            for (int i = 0; i <= Tramos; i++)
            {
                float t = (float)i / Tramos;
                puntos[i] = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.45f;
            }
            LineRenderer lr = serie.gameObject.AddComponent<LineRenderer>();
            lr.positionCount = puntos.Length;
            lr.SetPositions(puntos);
            lr.widthMultiplier = 0.012f;
            lr.sharedMaterial = cable;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 1; i <= focos; i++)
            {
                float t = (float)i / (focos + 1);
                Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.45f + 0.06f);
                GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                f.name = "Foco";
                f.transform.SetParent(serie, false);
                f.transform.position = pos;
                f.transform.localScale = Vector3.one * 0.07f;
                Renderer rend = f.GetComponent<Renderer>();
                rend.sharedMaterial = foco;
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void Cuadro(Transform padre, string nombre, string rutaMaterial, Vector3 pos, Vector3 frente, Vector2 tam)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = nombre;
            q.transform.SetParent(padre, false);
            q.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-frente, Vector3.up));
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            q.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        }

        private static void Apagar(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private static void OcultarCubo(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            Renderer rend = t != null ? t.GetComponent<Renderer>() : null;
            if (rend != null)
            {
                rend.enabled = false;
            }
        }

        // El cascarón de cubos del local (techo y muros) ya lo tapa el forro: deja de verse, su colisión se queda.
        private static void OcultarCascaron(string ruta)
        {
            foreach (string n in new[] { "Techo", "MuroFondo", "MuroIzq", "MuroDer", "Piso" })
            {
                OcultarCubo(ruta + "/" + n);
            }
        }

        private static void ApagarLuzDelLocal(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            Light l = t != null ? t.GetComponent<Light>() : null;
            if (l != null)
            {
                l.enabled = false;
            }
        }
    }
}
