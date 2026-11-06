using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-853 (playtest 6-nov tarde, casa): el piso del cuarto encimado con la banqueta de la manzana (las dos a y = 0.20: parpadeaba la textura
    /// de losas), el medidor del pasillo muy chico, y junto a la casa el mazo de cables parado en la banqueta, el cargador CY10 (hacía de tablero
    /// de encargos) y el banco de trabajo del taller del mecha, que no tenían sentido ahí. El tablero de encargos pasa a la pared de la casa con un
    /// letrero del kit (C15a) que dice ENCARGOS. La entrada queda solo con la puerta y su marco: los adornos de greybox de EntradaNeon se apagan
    /// y queda el marcador Hueco_Entrada_P11 con el tamaño del vano para el modelo de entrada que llega del chat de arte.
    /// Idempotente (rehace su raíz S2t_Casa). Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tCasa.Colocar
    /// </summary>
    public static class ColocarS2tCasa
    {
        public const string NombreRaiz = "S2t_Casa";
        public const string RutaPisoCuarto = "T550_Cuartos/Cuarto_Habitacion/Piso";
        public const string RutaPisoPasillo = "T550_Cuartos/Cuarto_Habitacion/Pasillo/Piso";
        public const string RutaMedidor = "T550_Cuartos/Cuarto_Habitacion/Pasillo/C14_RegistroMedidores";
        public const string RutaTablero = "GTA_T426/TableroEncargos";
        public const string RutaEntradaNeon = "Barrio/Manzana_4/Lugar_Casa/EntradaNeon";
        /// <summary>Lo que se apaga junto a la casa.</summary>
        public static readonly string[] Apagar =
        {
            "S2_Casa/Fachada/CY03_MazoCables",
            "S2_Casa/Tablero",
            "Barrio/Manzana_4/Lugar_Casa/Interior/Taller/Banco",
            "Barrio/Manzana_4/Lugar_Casa/Interior/Taller/Banco_Visual_T250",
        };

        /// <summary>La banqueta frente a la casa y sus vecinas (en planta, y = 0).</summary>
        public static readonly Bounds BanquetaCasa = new Bounds(new Vector3(-54f, 0f, -77f), new Vector3(8f, 1f, 26f));

        /// <summary>Alto del piso del cuarto y del pasillo (m): 2.5 cm sobre la banqueta (0.20) para que no se encimen.</summary>
        public const float AlturaPiso = 0.225f;
        /// <summary>Alto del registro de medidores del pasillo (m): como uno de verdad.</summary>
        public const float AltoMedidor = 0.95f;
        private const float CentroMedidorY = 1.45f;
        private const float ParedSurPasilloZ = -78.5f;
        // Frente de la casa (ColocarS2Casa): plano x = -56.95 mirando a +X; puerta de z -78.5 a -75.5 bajo el dintel de 3.4 m.
        private const float FrenteX = -56.95f;
        private const float PuertaZ0 = -78.5f, PuertaZ1 = -75.5f, Suelo = 0.22f, AltoVano = 3.2f;
        /// <summary>El tablero de encargos en la pared, entre la puerta y la ventana del lado sur.</summary>
        public static readonly Vector3 LugarTablero = new Vector3(FrenteX + 0.12f, 1.55f, -82.4f);
        private const string C = "Assets/Arte/Calle/Prefabs/";

        [MenuItem("Mecha Cyberpunk/S2t/1 Casa (piso, medidor, banqueta, tablero, entrada)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;

            foreach (string r in new[] { RutaPisoCuarto, RutaPisoPasillo })
            {
                Transform piso = ColocarS2.Buscar(r);
                if (piso == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA " + r);
                    continue;
                }
                Renderer rd = piso.GetComponent<Renderer>();
                float delta = AlturaPiso - rd.bounds.center.y;
                piso.position += Vector3.up * delta;
                ColocarS2.Informe.AppendLine(r + ": a y " + AlturaPiso + " (" + delta.ToString("+0.000;-0.000") + ")");
            }

            Medidor();

            foreach (string r in Apagar)
            {
                Transform t = ColocarS2.Buscar(r);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
                ColocarS2.Informe.AppendLine((t == null ? "FALTA " : "apagado ") + r);
            }

            // Los mazos de cables parados en la banqueta frente a la casa (hay varios con el mismo nombre).
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name.StartsWith("CY03_MazoCables") && t.gameObject.activeInHierarchy
                && !t.root.name.StartsWith("T550") && BanquetaCasa.Contains(new Vector3(t.position.x, 0f, t.position.z))).ToArray())
            {
                t.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("apagado " + t.name + " en " + t.position.ToString("F1"));
            }

            Tablero(raiz);
            Entrada(raiz);
            ColocarS2.Guardar("s2t-casa");
        }

        private static void Medidor()
        {
            Transform m = ColocarS2.Buscar(RutaMedidor);
            if (m == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaMedidor);
                return;
            }
            Bounds b = ArteT250.Limites(m.gameObject);
            m.localScale *= AltoMedidor / Mathf.Max(0.01f, b.size.y);
            b = ArteT250.Limites(m.gameObject);
            // Pegado a la pared sur del pasillo, a la altura de un registro de verdad.
            m.position += new Vector3(0f, CentroMedidorY - b.center.y, ParedSurPasilloZ + 0.01f - b.min.z);
            ColocarS2.Informe.AppendLine("medidor del pasillo: " + ArteT250.Limites(m.gameObject).size.ToString("F2"));
        }

        private static void Tablero(Transform raiz)
        {
            Transform t = ColocarS2.Buscar(RutaTablero);
            if (t == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaTablero);
                return;
            }
            t.SetPositionAndRotation(LugarTablero, Quaternion.Euler(0f, 90f, 0f));
            GameObject letrero = ColocarS2Interiores.Modelo(raiz, C + "C15a_LetreroHorizontal", new Vector3(LugarTablero.x, 0f, LugarTablero.z), 90f, LugarTablero.y - 0.45f, 1f, sinCollider: true);
            if (letrero == null)
            {
                ColocarS2.Informe.AppendLine("FALTA C15a_LetreroHorizontal");
                return;
            }
            letrero.name = "Tablero_C15a";
            Bounds b = ArteT250.Limites(letrero);
            letrero.transform.localScale *= 1.6f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            b = ArteT250.Limites(letrero);
            letrero.transform.position += new Vector3(FrenteX + 0.01f - b.min.x, LugarTablero.y - b.center.y, LugarTablero.z - b.center.z);
            b = ArteT250.Limites(letrero);
            ColocarS2t.TextoMundo(raiz, "Tablero_Texto", "ENCARGOS", new Vector3(b.max.x + 0.02f, b.center.y, b.center.z), new Vector2(b.size.z * 0.8f, b.size.y * 0.55f),
                Vector3.right, new Color(0.3f, 0.95f, 0.9f));
            ColocarS2.Informe.AppendLine("tablero de encargos en la pared: " + LugarTablero);
        }

        private static void Entrada(Transform raiz)
        {
            Transform neon = ColocarS2.Buscar(RutaEntradaNeon);
            int apagados = 0;
            if (neon != null)
            {
                foreach (Transform h in neon.Cast<Transform>().Where(h => h.gameObject.activeSelf))
                {
                    h.gameObject.SetActive(false);
                    apagados++;
                }
            }
            // El vano para el modelo de entrada (P11): un marcador vacío del tamaño del hueco, sin malla.
            GameObject hueco = new GameObject("Hueco_Entrada_P11");
            hueco.transform.SetParent(raiz, false);
            hueco.transform.SetPositionAndRotation(new Vector3(FrenteX, Suelo, (PuertaZ0 + PuertaZ1) * 0.5f), Quaternion.Euler(0f, 90f, 0f));
            BoxCollider caja = hueco.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.enabled = false;
            caja.center = new Vector3(0f, AltoVano * 0.5f, 0f);
            caja.size = new Vector3(PuertaZ1 - PuertaZ0, AltoVano, 0.3f);
            ColocarS2.Informe.AppendLine("entrada: " + apagados + " adornos de greybox apagados; hueco listo en " + hueco.transform.position);
        }
    }
}
