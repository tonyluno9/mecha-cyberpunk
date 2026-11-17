using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1320 (playtest 17-nov, punto 17): la calle junto a la casa (los "techos apilados sin fin" de la manzana de la casa) y la calle de la
    /// armería, rehechas con intención:
    /// - K09 Plaza: el patio de la cuadra noreste (detrás de la calle K01, z = -43) se abre a la calle quitando las cuatro casas del frente
    ///   (Casa_12 a Casa_15 de S2c_CalleDorada, con sus escaleras, pretiles y rutas de techo): empedrado, kiosco, árboles, bancas, puestos de
    ///   comida y foquitos colgados. La escena de acoso del lote se queda.
    /// - K10 Calle comercial: el tramo de la K01 entre el cruce (x = -110.5) y la calle de la casa (x = -57): toldos sobre las fachadas,
    ///   puestos a la orilla del arroyo (esa calle no lleva tráfico), foquitos en zigzag de fachada a fachada y neones.
    /// - La calle de la armería (el callejón al oriente de El Mercado, x -25..-13, z 9..45): toldo y letrero ARMERÍA en la puerta, foquitos,
    ///   puestos ordenados en la orilla oriente y el paso a la puerta despejado.
    /// Idempotente: rehace su raíz <see cref="Raiz"/> y vuelve a apagar lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1320Calles.Aplicar (y .Fotos)
    /// </summary>
    public static class ColocarT1320Calles
    {
        public const string Raiz = "T1320_Calles";
        public const string Carpeta = "docs/capturas/t-1320/";
        private const int Semilla = 1320;

        // ---- Plaza (K09)
        /// <summary>Casas del frente de la cuadra que se quitan para abrir la plaza a la calle.</summary>
        public static readonly string[] CasasPlaza =
        {
            "S2c_CalleDorada/Casas/Casa_12", "S2c_CalleDorada/Casas/Casa_13", "S2c_CalleDorada/Casas/Casa_14", "S2c_CalleDorada/Casas/Casa_15",
        };
        /// <summary>Lo que quede por completo dentro de esta caja (escaleras, pretiles, rutas de techo, neones de las casas quitadas) se apaga.</summary>
        public static readonly Bounds ZonaQuitar = new Bounds(new Vector3(-77f, 10f, -28.5f), new Vector3(20.6f, 30f, 19.8f));
        public const float PlazaX0 = -87f;
        public const float PlazaX1 = -67f;
        public const float PlazaZ0 = -37.1f;   // fachada norte de la calle K01
        public const float PlazaZ1 = -19f;
        public static readonly Vector3 CentroKiosco = new Vector3(-75.5f, 0f, -29.5f);

        // ---- Calle comercial (K10)
        public const float ComercioX0 = -105f;
        public const float ComercioX1 = -59f;
        public const float FachadaNorte = -37.25f;
        public const float FachadaSur = -48.75f;
        public const float AlturaFoquitos = 5.1f;

        // ---- Calle de la armería
        public const float XPuertaArmeria = -25.6f;
        public const float ZPuertaArmeria = 26f;
        /// <summary>Frente del portón E02 del Mercado (la entrada de la armería), que mide 3.8 m de alto.</summary>
        public const float XFrentePorton = -24.15f;
        public const float AlturaLetrero = 4.55f;
        public static readonly Bounds CallejonArmeria = new Bounds(new Vector3(-19.5f, 2f, 27f), new Vector3(11f, 6f, 36f));
        /// <summary>Paso libre de la entrada del callejón (avenida) a la puerta de la armería: aquí no queda ningún objeto con collider.</summary>
        public static readonly Bounds PasoArmeria = new Bounds(new Vector3(-22.9f, 1f, 18f), new Vector3(2.4f, 2f, 16f));   // el carril de T-504

        private static readonly Color Verde = new Color(0.18f, 0.42f, 0.2f);
        private static readonly Color Madera = new Color(0.42f, 0.27f, 0.16f);
        private static readonly Color Cobre = new Color(0.25f, 0.55f, 0.5f);
        private static readonly Color Foco = new Color(1f, 0.78f, 0.45f);

        [MenuItem("Mecha Cyberpunk/T-1320 Calles (plaza, calle comercial y armería)")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            int apagados = Apagar();
            Physics.SyncTransforms();
            Plaza(o);
            CalleComercial(o);
            Armeria(o);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("T-1320: " + apagados + " objetos apagados, " + o.Neones + " neones, " + o.Cables + " cables");
            ColocarS2.Guardar("t1320-calles");
        }

        // ================================================================ Lo que se quita

        /// <summary>Apaga las casas del frente de la plaza, lo que quedaba encima o pegado a ellas y lo que estorbaba el paso a la armería.</summary>
        public static int Apagar()
        {
            int n = 0;
            foreach (string ruta in CasasPlaza)
            {
                Transform t = ColocarS2.Buscar(ruta);
                if (t != null && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    n++;
                }
            }
            // Una pasada anterior apagó por error el transformador del poste de la calle: se vuelve a prender.
            // Las barreras de T-504 no estorban el carril de la armería: se quedan (una pasada anterior las apagó).
            GameObject piezas504 = GameObject.Find("T504_Mercado");
            if (piezas504 != null)
            {
                foreach (Transform b504 in piezas504.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("C18_BarreraCalle")))
                {
                    b504.gameObject.SetActive(true);
                }
            }
            Transform transformador = ColocarS2.Buscar("S2c_CalleDorada/Postes/Poste_1/C08_Transformador");
            if (transformador != null)
            {
                transformador.gameObject.SetActive(true);
            }
            foreach (GameObject g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (g.name == Raiz || g.name.StartsWith("Barrio") || g.name.StartsWith("GTA_") || g.name.StartsWith("S2_PandillasAcoso"))
                {
                    continue;
                }
                foreach (Transform t in g.GetComponentsInChildren<Transform>(false))
                {
                    if (t == g.transform || !t.gameObject.activeInHierarchy || !Limites(t, out Bounds b))
                    {
                        continue;
                    }
                    bool enPlaza = Dentro(ZonaQuitar, b) && b.max.y > 0.6f && !t.name.StartsWith("Poste") && !t.name.StartsWith("C08_");
                    bool estorbaArmeria = PasoArmeria.Intersects(b) && b.size.x < 4f && b.size.z < 4f && b.max.y < 4f
                        && t.GetComponentInChildren<Collider>() != null && !t.name.StartsWith("Poste") && !EsPiso(t);
                    if (enPlaza || estorbaArmeria)
                    {
                        t.gameObject.SetActive(false);
                        n++;
                        ColocarS2.Informe.AppendLine("T-1320 apagado: " + Ruta(t));
                    }
                }
            }
            return n;
        }

        private static bool EsPiso(Transform t) => t.name.Contains("Arroyo") || t.name.Contains("Banqueta") || t.name.Contains("Piso") || t.name.Contains("Calle");

        private static bool Dentro(Bounds caja, Bounds b) => caja.Contains(b.min) && caja.Contains(b.max);

        /// <summary>Caja de lo visible y de los colliders de <paramref name="t"/> (y sus hijos activos).</summary>
        private static bool Limites(Transform t, out Bounds b)
        {
            b = default;
            bool hay = false;
            // Con la malla y la matriz del transform: Renderer.bounds sale viejo justo después de instanciar una pieza en el editor.
            foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(false))
            {
                MeshRenderer r = f.GetComponent<MeshRenderer>();
                if (f.sharedMesh == null || r == null || !r.enabled)
                {
                    continue;
                }
                Bounds m = f.sharedMesh.bounds;
                Matrix4x4 a = f.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 q = a.MultiplyPoint3x4(m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!hay) { b = new Bounds(q, Vector3.zero); hay = true; } else { b.Encapsulate(q); }
                }
            }
            // Las partículas (el humo del puesto de tacos) no cuentan: su caja recién creada está en el origen.
            foreach (SkinnedMeshRenderer r in t.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!hay) { b = r.bounds; hay = true; } else { b.Encapsulate(r.bounds); }
            }
            // La caja de un collider recién movido no cambia hasta sincronizar la física.
            Physics.SyncTransforms();
            foreach (Collider c in t.GetComponentsInChildren<Collider>(false))
            {
                if (!c.enabled) { continue; }
                if (!hay) { b = c.bounds; hay = true; } else { b.Encapsulate(c.bounds); }
            }
            return hay;
        }

        // ================================================================ K09 Plaza

        private static void Plaza(KitS2c.Obra o)
        {
            Transform plaza = Hijo(o.Raiz, "Plaza_K09");
            float piso = KitS2c.Suelo(new Vector3(-77f, 0f, -26f), 0.2f);
            // Empedrado (sobre el piso que ya hay; sin collider para no hacer escalón).
            KitS2c.Losa(o, plaza, "Empedrado", new Vector3((PlazaX0 + PlazaX1) * 0.5f, piso + 0.005f, (PlazaZ0 + PlazaZ1) * 0.5f), Quaternion.identity,
                new Vector3(PlazaX1 - PlazaX0, 0.01f, PlazaZ1 - PlazaZ0), MatEmpedrado(), 3f, false).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Kiosco(o, plaza, CentroKiosco + Vector3.up * piso);

            // Árboles en las orillas, bancas mirando al kiosco, puestos de comida y mesas.
            Vector3[] arboles =
            {
                new Vector3(-85f, piso, -35f), new Vector3(-69f, piso, -35f), new Vector3(-85f, piso, -21f), new Vector3(-69f, piso, -21f),
                new Vector3(-80.5f, piso, -35.5f), new Vector3(-71f, piso, -27f),
            };
            foreach (Vector3 p in arboles)
            {
                Arbol(o, plaza, p);
            }
            foreach ((Vector3 p, float giro) in new[]
            {
                (new Vector3(-75.5f, piso, -34.2f), 0f), (new Vector3(-81.5f, piso, -29.5f), 90f), (new Vector3(-75.5f, piso, -24.6f), 180f),
                (new Vector3(-70.5f, piso, -31.5f), -90f),
            })
            {
                Banca(o, plaza, p, Quaternion.Euler(0f, giro, 0f));
            }
            Poner(plaza, "C12_PuestoTacos", new Vector3(-84.6f, piso, -31.5f), Quaternion.Euler(0f, 90f, 0f), true);
            Poner(plaza, "C13_PuestoCiber", new Vector3(-68.6f, piso, -23.5f), Quaternion.Euler(0f, -90f, 0f), true);
            Poner(plaza, "CY09_PuestoImplantes", new Vector3(-68.6f, piso, -34f), Quaternion.Euler(0f, -90f, 0f), true);
            Poner(plaza, "H04_MesaSillas", new Vector3(-82f, piso, -33.5f), Quaternion.Euler(0f, 15f, 0f), true);
            Poner(plaza, "H04_MesaSillas", new Vector3(-70.8f, piso, -21.5f), Quaternion.Euler(0f, -20f, 0f), true);
            Poner(plaza, "C11_Basurero", new Vector3(-86.3f, piso, -36.4f), Quaternion.identity, true);

            // Faroles en las cuatro esquinas y foquitos del kiosco a cada farol.
            Vector3 punta = CentroKiosco + Vector3.up * (piso + 4.6f);
            foreach (Vector3 p in new[] { new Vector3(-86f, piso, -36.2f), new Vector3(-68f, piso, -36.2f), new Vector3(-86f, piso, -20f), new Vector3(-68f, piso, -20f) })
            {
                Vector3 cabeza = Farol(o, plaza, p);
                Foquitos(o, plaza, punta, cabeza, 0.5f, 2.2f, false);
            }
            for (int i = 0; i < 4; i++)
            {
                KitS2c.Charco(o, plaza, new Vector3(o.Rango(PlazaX0 + 2f, PlazaX1 - 2f), piso, o.Rango(PlazaZ0 + 1f, PlazaZ1 - 2f)), o.Rango(1f, 2f), o.Rango(0.7f, 1.4f), o.Rango(0f, 180f));
            }
        }

        /// <summary>Kiosco de plaza (K09): base octagonal, ocho columnas, techo de cobre en cono, foco adentro.</summary>
        private static void Kiosco(KitS2c.Obra o, Transform padre, Vector3 c)
        {
            Transform k = Hijo(padre, "Kiosco");
            const int lados = 8;
            const float radio = 2.6f;
            const float altoBase = 0.3f;
            const float altoColumna = 3f;
            Material piedra = KitS2c.Mat("T1320_Piedra", () => KitS2c.Lit(new Color(0.62f, 0.6f, 0.56f), 0.2f, KitS2c.TexMuro()));
            Material hierro = KitS2c.Mat("T1320_Hierro", () => KitS2c.Lit(new Color(0.12f, 0.13f, 0.14f), 0.5f, KitS2c.TexMuro()));
            Material cobre = KitS2c.Mat("T1320_Cobre", () => KitS2c.Lit(Cobre, 0.45f, KitS2c.TexMuro()));
            GameObject b = Prisma(o, k, "Base", c, lados, radio, radio, altoBase, piedra);
            BoxCollider bc = b.AddComponent<BoxCollider>();
            bc.center = Vector3.up * altoBase * 0.5f;
            bc.size = new Vector3(radio * 1.5f, altoBase, radio * 1.5f);
            for (int i = 0; i < lados; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / lados;
                Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radio - 0.3f) + Vector3.up * (altoBase + altoColumna * 0.5f);
                GameObject col = KitS2c.Losa(o, k, "Columna", p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.14f, altoColumna, 0.14f), hierro, 1f, true);
                col.name = "Columna_" + i;
            }
            // Barandal bajo entre columnas (menos en la entrada que mira a la calle).
            for (int i = 0; i < lados; i++)
            {
                float a0 = (i + 0.5f) * Mathf.PI * 2f / lados, a1 = (i + 1.5f) * Mathf.PI * 2f / lados;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * (radio - 0.3f), p1 = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * (radio - 0.3f);
                Vector3 medio = (p0 + p1) * 0.5f;
                if (Vector3.Dot((medio - c).normalized, Vector3.back) > 0.85f)
                {
                    continue;
                }
                KitS2c.Losa(o, k, "Barandal", medio + Vector3.up * (altoBase + 0.85f), Quaternion.LookRotation(p1 - p0), new Vector3(0.06f, 0.06f, (p1 - p0).magnitude), hierro, 1f, false);
            }
            float arriba = altoBase + altoColumna;
            Prisma(o, k, "Alero", c + Vector3.up * arriba, lados, radio + 0.35f, radio + 0.35f, 0.18f, cobre);
            Prisma(o, k, "Techo", c + Vector3.up * (arriba + 0.18f), lados, radio + 0.35f, 0.25f, 1.5f, cobre);
            Prisma(o, k, "Linternilla", c + Vector3.up * (arriba + 1.68f), lados, 0.35f, 0.05f, 0.7f, cobre);
            GameObject foco = Prisma(o, k, "Foco", c + Vector3.up * (arriba - 0.45f), 6, 0.18f, 0.18f, 0.3f, MatFoco());
            foco.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            KitS2c.Luz(k, c + Vector3.up * (arriba - 0.7f), LightType.Point, Foco, 6f, 9f, 0f);
        }

        /// <summary>Árbol bajo de banqueta: tronco y dos copas de facetas (verde de noche, como K09).</summary>
        private static void Arbol(KitS2c.Obra o, Transform padre, Vector3 pie)
        {
            Transform a = Hijo(padre, "Arbol");
            Material tronco = KitS2c.Mat("T1320_Tronco", () => KitS2c.Lit(new Color(0.3f, 0.22f, 0.15f), 0.15f, KitS2c.TexMuro()));
            Material hojas = KitS2c.Mat("T1320_Hojas", () => KitS2c.Lit(Verde, 0.15f, KitS2c.TexMuro()));
            float alto = o.Rango(2.4f, 3f);
            GameObject t = Prisma(o, a, "Tronco", pie, 6, 0.17f, 0.12f, alto, tronco);
            CapsuleCollider cc = t.AddComponent<CapsuleCollider>();
            cc.center = Vector3.up * alto * 0.5f;
            cc.radius = 0.2f;
            cc.height = alto;
            float r = o.Rango(1.4f, 1.9f);
            float giroCopa = o.Rango(0f, 60f);
            float y0 = alto - 0.5f;
            Prisma(o, a, "CopaBaja", pie + Vector3.up * y0, 7, r * 0.45f, r, r * 0.45f, hojas).transform.rotation = Quaternion.Euler(0f, giroCopa, 0f);
            Prisma(o, a, "Copa", pie + Vector3.up * (y0 + r * 0.45f), 7, r, r * 0.85f, r * 0.5f, hojas).transform.rotation = Quaternion.Euler(0f, giroCopa, 0f);
            Prisma(o, a, "CopaAlta", pie + Vector3.up * (y0 + r * 0.95f), 7, r * 0.85f, 0.15f, r * 0.75f, hojas).transform.rotation = Quaternion.Euler(0f, giroCopa, 0f);
            // Jardinera de piedra alrededor del tronco.
            Material piedra = KitS2c.Mat("T1320_Piedra", () => KitS2c.Lit(new Color(0.62f, 0.6f, 0.56f), 0.2f, KitS2c.TexMuro()));
            Prisma(o, a, "Jardinera", pie, 8, 0.75f, 0.75f, 0.28f, piedra);
        }

        /// <summary>Banca de madera con patas de fierro, de frente a <paramref name="giro"/> * +Z.</summary>
        private static void Banca(KitS2c.Obra o, Transform padre, Vector3 pie, Quaternion giro)
        {
            Transform b = Hijo(padre, "Banca");
            Material madera = KitS2c.Mat("T1320_Madera", () => KitS2c.Lit(Madera, 0.25f, KitS2c.TexMuro()));
            Material hierro = KitS2c.Mat("T1320_Hierro", () => KitS2c.Lit(new Color(0.12f, 0.13f, 0.14f), 0.5f, KitS2c.TexMuro()));
            KitS2c.Losa(o, b, "Asiento", pie + giro * new Vector3(0f, 0.45f, 0f), giro, new Vector3(1.8f, 0.07f, 0.5f), madera, 1f, true);
            KitS2c.Losa(o, b, "Respaldo", pie + giro * new Vector3(0f, 0.8f, -0.24f), giro * Quaternion.Euler(-12f, 0f, 0f), new Vector3(1.8f, 0.45f, 0.05f), madera, 1f, false);
            foreach (float x in new[] { -0.75f, 0.75f })
            {
                KitS2c.Losa(o, b, "Pata", pie + giro * new Vector3(x, 0.22f, 0f), giro, new Vector3(0.06f, 0.45f, 0.45f), hierro, 1f, false);
            }
        }

        /// <summary>Farol de plaza (poste corto con su linterna y luz). Devuelve la cabeza, para colgar foquitos.</summary>
        private static Vector3 Farol(KitS2c.Obra o, Transform padre, Vector3 pie)
        {
            Transform f = Hijo(padre, "Farol");
            Material hierro = KitS2c.Mat("T1320_Hierro", () => KitS2c.Lit(new Color(0.12f, 0.13f, 0.14f), 0.5f, KitS2c.TexMuro()));
            const float alto = 3.6f;
            GameObject poste = Prisma(o, f, "Poste", pie, 6, 0.1f, 0.07f, alto, hierro);
            CapsuleCollider cc = poste.AddComponent<CapsuleCollider>();
            cc.center = Vector3.up * alto * 0.5f;
            cc.radius = 0.12f;
            cc.height = alto;
            GameObject linterna = Prisma(o, f, "Linterna", pie + Vector3.up * alto, 6, 0.2f, 0.26f, 0.45f, MatFoco());
            linterna.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Prisma(o, f, "Capucha", pie + Vector3.up * (alto + 0.45f), 6, 0.32f, 0.05f, 0.25f, hierro);
            KitS2c.Luz(f, pie + Vector3.up * (alto + 0.2f), LightType.Point, KitS2c.Sodio, 5f, 9f, 0f);
            return pie + Vector3.up * (alto + 0.3f);
        }

        // ================================================================ K10 Calle comercial

        private static void CalleComercial(KitS2c.Obra o)
        {
            Transform calle = Hijo(o.Raiz, "CalleComercial_K10");
            float arroyo = KitS2c.Suelo(new Vector3(-80f, 0f, ColocarS2cCalle.ZCentro), 0.2f);
            float banqueta = arroyo + ColocarS2cCalle.AltoBanqueta;
            // Toldos sobre las fachadas, donde hay casa (la plaza queda abierta) y no hay ya un toldo.
            List<Vector3> toldos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => (t.name == "Toldo" || t.name.StartsWith("TL01")) && t.root.name != Raiz).Select(t => t.position).ToList();
            for (float x = ComercioX0 + 1.5f; x < ComercioX1 - 1f; x += 6f)
            {
                foreach ((float z, Vector3 haciaCalle) in new[] { (FachadaNorte, Vector3.back), (FachadaSur, Vector3.forward) })
                {
                    bool plaza = z == FachadaNorte && x > PlazaX0 - 0.5f && x < PlazaX1 + 0.5f;
                    bool yaHay = toldos.Any(p => Mathf.Abs(p.x - x) < 3.6f && Mathf.Abs(p.z - z) < 3f);
                    if (plaza || yaHay || o.Chance(0.25f) || !HayFachada(new Vector3(x, banqueta + 1.5f, z + haciaCalle.z * 1.2f), -haciaCalle))
                    {
                        continue;
                    }
                    // T-1331 (playtest 17-nov): sin toldo TL01; sus patas quedaban en el aire. El neón se queda.
                    if (o.Chance(0.55f))
                    {
                        KitS2c.Neon(o, calle, new Vector3(x + o.Rango(1.5f, 2.4f), banqueta + 3.7f, z), haciaCalle, Vector3.right, o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa, o.Chance(0.4f));
                    }
                }
            }
            // T-1331 (playtest 17-nov): sin puestos en el arroyo; sus techitos quedaban en medio de la calle.
            int k = 0;
            // Foquitos en zigzag de fachada a fachada.
            bool alNorte = true;
            for (float x = ComercioX0; x < ComercioX1; x += 4.5f)
            {
                Vector3 a = new Vector3(x, banqueta + AlturaFoquitos, alNorte ? FachadaNorte - 0.1f : FachadaSur + 0.1f);
                Vector3 b = new Vector3(x + 4.5f, banqueta + AlturaFoquitos - 0.3f, alNorte ? FachadaSur + 0.1f : FachadaNorte - 0.1f);
                Foquitos(o, calle, a, b, 0.9f, 1.6f, k++ % 3 == 0);
                alNorte = !alNorte;
            }
        }

        private static bool HayFachada(Vector3 desde, Vector3 haciaFachada)
        {
            return Physics.Raycast(desde, haciaFachada, out RaycastHit h, 2.5f) && h.collider.transform.root.name != Raiz;
        }

        /// <summary>Toldo TL01 de 3 m pegado a la fachada en <paramref name="p"/> (orilla de arriba), saliendo hacia <paramref name="haciaCalle"/>.</summary>
        private static void Toldo(KitS2c.Obra o, Transform padre, Vector3 p, Vector3 haciaCalle)
        {
            GameObject t = KitS2c.Pieza(padre, "TL01_Toldo", p, Quaternion.LookRotation(haciaCalle), Vector3.one, false);
            if (Limites(t.transform, out Bounds b))
            {
                float ancho = Mathf.Max(b.size.x, b.size.z);
                if (ancho > 0.1f)
                {
                    t.transform.localScale = Vector3.one * Mathf.Clamp(3f / ancho, 0.5f, 3f);
                }
                Limites(t.transform, out b);
                // La orilla de atrás del toldo va contra la fachada, a la altura pedida.
                Vector3 atras = b.center - haciaCalle * (Vector3.Dot(b.extents, new Vector3(Mathf.Abs(haciaCalle.x), 0f, Mathf.Abs(haciaCalle.z))));
                t.transform.position += new Vector3(p.x - b.center.x, p.y - b.max.y, 0f) + Vector3.Project(p - atras, haciaCalle) + haciaCalle * 0.05f;
            }
            Color[] colores = { new Color(0.85f, 0.25f, 0.3f), new Color(0.95f, 0.55f, 0.15f), new Color(0.15f, 0.55f, 0.55f), new Color(0.75f, 0.2f, 0.6f) };
            Color c = colores[o.Azar.Next(colores.Length)];
            Material m = KitS2c.Mat("T1320_Toldo_" + ColorUtility.ToHtmlStringRGB(c), () => KitS2c.Lit(c, 0.25f, KitS2c.TexMuro()));
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>())
            {
                r.sharedMaterial = m;
            }
        }

        // ================================================================ Calle de la armería

        private static void Armeria(KitS2c.Obra o)
        {
            Transform calle = Hijo(o.Raiz, "CalleArmeria");
            float piso = KitS2c.Suelo(new Vector3(-20f, 0f, 24f), 0.2f);
            // Toldo y letrero ARMERÍA sobre la puerta (la fachada da al oriente).
            Vector3 puerta = new Vector3(XFrentePorton, piso, ZPuertaArmeria);
            Material neon = KitS2c.Mat("T1320_TableroArmeria", () => KitS2c.Lit(new Color(0.08f, 0.07f, 0.07f), 0.4f, KitS2c.TexMuro()));
            KitS2c.Losa(o, calle, "Tablero_ARMERIA", puerta + new Vector3(0.05f, AlturaLetrero, 0f), Quaternion.identity, new Vector3(0.08f, 0.95f, 4.2f), neon, 1f, false);
            GameObject letrero = ColocarS2t.TextoMundo(calle, "Letrero_ARMERIA", "ARMERÍA", puerta + new Vector3(0.11f, AlturaLetrero, 0f), new Vector2(3.8f, 0.8f), Vector3.right, new Color(1f, 0.55f, 0.15f));
            KitS2c.Luz(letrero.transform, puerta + new Vector3(0.9f, AlturaLetrero - 0.1f, 0f), LightType.Point, new Color(1f, 0.55f, 0.15f), 2.5f, 6f, 0f);
            KitS2c.Neon(o, calle, puerta + new Vector3(0f, 2.6f, 2.6f), Vector3.right, Vector3.forward, KitS2c.Magenta, true);
            // Puestos ordenados en la orilla oriente del callejón, mirando a la fachada de la armería.
            string[] puestos = { "C13_PuestoCiber", "CY09_PuestoImplantes", "C12_PuestoTacos" };
            int i = 0;
            for (float z = 14f; z <= 38f; z += 8f)
            {
                Poner(calle, puestos[i % puestos.Length], new Vector3(-15f, piso, z), Quaternion.Euler(0f, -90f, 0f), true);
                i++;
            }
            Poner(calle, "C17_Huacales", new Vector3(-24.5f, piso, 29.6f), Quaternion.Euler(0f, 20f, 0f), true);
            // Foquitos en zigzag a lo largo del callejón y dos faroles.
            bool poniente = true;
            for (float z = 16f; z < 44f; z += 4f)   // el tianguis de la avenida (z < 11) va sin focos colgando (T-873)
            {
                Vector3 a = new Vector3(poniente ? -25.4f : -13.6f, piso + 4.6f, z);
                Vector3 b = new Vector3(poniente ? -13.6f : -25.4f, piso + 4.4f, z + 4f);
                Foquitos(o, calle, a, b, 0.8f, 1.5f, ((int)z / 4) % 3 == 0);
                poniente = !poniente;
            }
            Farol(o, calle, new Vector3(-13.8f, piso, 18f));
            Farol(o, calle, new Vector3(-13.8f, piso, 34f));
            for (int c = 0; c < 4; c++)
            {
                KitS2c.Charco(o, calle, new Vector3(o.Rango(-23f, -16f), piso, o.Rango(12f, 40f)), o.Rango(1f, 2.4f), o.Rango(0.7f, 1.5f), o.Rango(0f, 180f));
            }
        }

        // ================================================================ Piezas comunes

        /// <summary>Cable con focos cada <paramref name="cada"/> m (y una luz cálida en medio si <paramref name="conLuz"/>).</summary>
        private static void Foquitos(KitS2c.Obra o, Transform padre, Vector3 a, Vector3 b, float flecha, float cada, bool conLuz)
        {
            KitS2c.Cable(o, a, b, flecha, 0.012f);
            int tramos = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(a, b) / 1.2f), 6, 24);
            Vector3[] p = CalculoCatenaria.Puntos(a, b, flecha, tramos);
            float largo = 0f;
            float siguiente = cada * 0.5f;
            Transform focos = Hijo(padre, "Foquitos");
            for (int i = 1; i < p.Length; i++)
            {
                float d = Vector3.Distance(p[i - 1], p[i]);
                while (largo + d >= siguiente)
                {
                    Vector3 q = Vector3.Lerp(p[i - 1], p[i], (siguiente - largo) / d);
                    GameObject f = Prisma(o, focos, "Foco", q + Vector3.down * 0.12f, 5, 0.05f, 0.05f, 0.1f, MatFoco());
                    f.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    siguiente += cada;
                }
                largo += d;
            }
            if (conLuz)
            {
                KitS2c.Luz(focos, p[p.Length / 2] + Vector3.down * 0.3f, LightType.Point, Foco, 2.2f, 6f, 0f);
            }
        }

        /// <summary>Instancia una pieza del kit con el fondo de lo visible sobre <paramref name="piso"/>.</summary>
        private static GameObject Poner(Transform padre, string codigo, Vector3 piso, Quaternion giro, bool collider)
        {
            GameObject g = KitS2c.Pieza(padre, codigo, piso, giro, Vector3.one, collider);
            if (Limites(g.transform, out Bounds b))
            {
                g.transform.position += new Vector3(piso.x - b.center.x, piso.y - b.min.y, piso.z - b.center.z);
            }
            return g;
        }

        /// <summary>Prisma de <paramref name="lados"/> caras, de radio <paramref name="radioAbajo"/> a <paramref name="radioArriba"/>, parado en <paramref name="pie"/>.</summary>
        private static GameObject Prisma(KitS2c.Obra o, Transform padre, string nombre, Vector3 pie, int lados, float radioAbajo, float radioArriba, float alto, Material material)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = pie;
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            float perimetro = 0f;
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
                Vector3 b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radioAbajo, b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radioAbajo;
                Vector3 c0 = new Vector3(Mathf.Cos(a0) * radioArriba, alto, Mathf.Sin(a0) * radioArriba), c1 = new Vector3(Mathf.Cos(a1) * radioArriba, alto, Mathf.Sin(a1) * radioArriba);
                float ancho = Vector3.Distance(b0, b1);
                int k = v.Count;
                v.AddRange(new[] { b0, b1, c1, c0 });
                uv.AddRange(new[] { new Vector2(perimetro, 0f), new Vector2(perimetro + ancho, 0f), new Vector2(perimetro + ancho, alto), new Vector2(perimetro, alto) });
                t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
                perimetro += ancho;
                // Tapas de arriba y abajo.
                int s = v.Count;
                v.AddRange(new[] { new Vector3(0f, alto, 0f), c0, c1, Vector3.zero, b1, b0 });
                uv.AddRange(new[] { Vector2.zero, new Vector2(c0.x, c0.z), new Vector2(c1.x, c1.z), Vector2.zero, new Vector2(b1.x, b1.z), new Vector2(b0.x, b0.z) });
                t.AddRange(new[] { s, s + 1, s + 2, s + 3, s + 4, s + 5 });
            }
            Mesh m = new Mesh { name = o.Nombre + "_" + nombre + "_" + o.Mallas.Count };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Material MatFoco() => KitS2c.Mat("T1320_Foco", () => KitS2c.Emision(Foco * 2.2f, KitS2c.TexMuro()));

        /// <summary>Empedrado de la plaza (K09): piedras irregulares grises con junta oscura, en una textura que se repite cada 3 m.</summary>
        private static Material MatEmpedrado()
        {
            return KitS2c.Mat("T1320_Empedrado", () =>
            {
                Material m = KitS2c.Lit(new Color(0.78f, 0.76f, 0.72f), 0.35f, TexEmpedrado());
                return m;
            });
        }

        private static Texture2D TexEmpedrado()
        {
            const string nombre = "T1320_Empedrado";
            Texture2D ya = KitS2c.Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 512;
            const int celdas = 12;
            System.Random azar = new System.Random(Semilla);
            Vector2[] centros = new Vector2[celdas * celdas];
            float[] tono = new float[celdas * celdas];
            for (int j = 0; j < celdas; j++)
            {
                for (int i = 0; i < celdas; i++)
                {
                    centros[j * celdas + i] = new Vector2(i + 0.2f + (float)azar.NextDouble() * 0.6f, j + 0.2f + (float)azar.NextDouble() * 0.6f);
                    tono[j * celdas + i] = 0.55f + (float)azar.NextDouble() * 0.3f;
                }
            }
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x, y) * celdas / n;
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    int cerca = 0;
                    int ci = Mathf.FloorToInt(p.x), cj = Mathf.FloorToInt(p.y);
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        for (int di = -1; di <= 1; di++)
                        {
                            int ii = (ci + di + celdas) % celdas, jj = (cj + dj + celdas) % celdas;
                            Vector2 c = centros[jj * celdas + ii] + new Vector2(ci + di - ii, cj + dj - jj);
                            float d = (p - c).magnitude;
                            if (d < d1) { d2 = d1; d1 = d; cerca = jj * celdas + ii; }
                            else if (d < d2) { d2 = d; }
                        }
                    }
                    float borde = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d2 - d1) * 5f));
                    float grano = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.15f;
                    float c2 = Mathf.Lerp(0.18f, tono[cerca] + grano, borde);
                    t.SetPixel(x, y, new Color(c2, c2 * 0.98f, c2 * 0.94f, 1f));
                }
            }
            t.Apply();
            return KitS2c.Guardar(nombre, t, true, TextureWrapMode.Repeat);
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static string Ruta(Transform t) => t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;

        // ================================================================ Fotos

        private static readonly (string nombre, Vector3 desde, Vector3 hacia)[] Tomas =
        {
            ("plaza-k09", new Vector3(-77f, 1.7f, -46.5f), new Vector3(-76f, 2.2f, -28f)),
            ("plaza-dentro", new Vector3(-84f, 1.7f, -22f), new Vector3(-72f, 1.8f, -33f)),
            ("calle-k10", new Vector3(-104f, 1.7f, -43.5f), new Vector3(-70f, 2.4f, -42.5f)),
            ("calle-k10-oriente", new Vector3(-60f, 1.7f, -42.5f), new Vector3(-95f, 2.4f, -43.5f)),
            ("armeria-calle", new Vector3(-18.5f, 1.7f, 11f), new Vector3(-21f, 2.3f, 40f)),
            ("armeria-puerta", new Vector3(-16f, 1.7f, 22f), new Vector3(-25.6f, 2.4f, 27f)),
            ("alto-plaza", new Vector3(-62f, 26f, -58f), new Vector3(-78f, 0f, -30f)),
        };

        /// <summary>Diagnóstico: posiciones de lo puesto y del portón del Mercado en Logs/t1320-diag.txt.</summary>
        public static void Diagnostico()
        {
            ColocarS2.Abrir();
            var sb = new System.Text.StringBuilder();
            foreach (string grupo in new[] { Raiz + "/CalleComercial_K10", Raiz + "/CalleArmeria" })
            {
                Transform g = GameObject.Find(grupo)?.transform;
                if (g == null) { sb.AppendLine("FALTA " + grupo); continue; }
                foreach (Transform h in g)
                {
                    if (Limites(h, out Bounds b)) sb.AppendLine(grupo + "/" + h.name + " " + b.center.ToString("F1") + " " + b.size.ToString("F1"));
                }
            }
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if ((go.name.StartsWith("E02") || go.name.Contains("Porton") || go.name.StartsWith("C18")) && Limites(go.transform, out Bounds b) && b.center.x > -40f && b.center.x < 0f && b.center.z > 0f && b.center.z < 50f)
                {
                    sb.AppendLine("OBJ " + Ruta(go.transform) + " " + b.center.ToString("F1") + " " + b.size.ToString("F1"));
                }
            }
            File.WriteAllText("Logs/t1320-diag.txt", sb.ToString());
        }

        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory(Carpeta);
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, Carpeta + nombre + ".png", nombre.StartsWith("alto") ? 60f : 70f);
            }
            ColocarS2cLook.Foto(Tomas[0].desde, Tomas[0].hacia, Carpeta + "plaza-k09-dia.png", 70f, 13f);
        }
    }
}
