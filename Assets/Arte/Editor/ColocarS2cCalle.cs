using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 1 de docs/plan-look-ciudad.md §3: la calle de prueba ("calle dorada"), idéntica a K01, en el barrio de la casa.
    /// Es una calle nueva de 74 m que cruza la manzana de la casa (Manzana_4) de oriente a poniente por su fila de en medio, del lado norte del
    /// callejón de los SKULLS: sale a la calle de la casa (x = -57) y a la del poniente (x = -131). La red de calles del tráfico, la policía y
    /// los peatones (3 × 2 manzanas) no cambia: por esta calle no pasan carros. Se apagan los edificios greybox de esa fila (Edificio_34, 43,
    /// 40 y 37) con lo que tenían encima, y se arma con KitS2c: casas de 2 pisos a los dos lados, banquetas de 2 m, arroyo de 7.5 m, postes,
    /// cables, charcos, un carro estacionado. Idempotente: rehace su raíz S2c_CalleDorada.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cCalle.Colocar
    /// </summary>
    public static class ColocarS2cCalle
    {
        public const string Raiz = "S2c_CalleDorada";
        public const int Semilla = 7110;

        public const float XOriente = -57f;     // línea de fachadas de la calle de la casa
        public const float XPoniente = -131f;   // línea de fachadas de la calle del poniente
        public const float XBordeOriente = -54f;
        public const float XBordePoniente = -134f;
        public const float ZCentro = -43f;
        public const float MedioAncho = 5.75f;  // de fachada a fachada: 11.5 m
        public const float Banqueta = 2f;
        public const float AltoBanqueta = 0.15f;
        public const float XMedio = -87f;       // donde cambia el fondo de las cuadras (callejón de los SKULLS al oriente)

        public static float ZSur => ZCentro - MedioAncho;
        public static float ZNorte => ZCentro + MedioAncho;

        /// <summary>Edificios greybox de la fila de en medio de la Manzana_4 que ocupa la calle nueva.</summary>
        public static readonly string[] Edificios =
        {
            "Barrio/Manzana_4/Edificios/Edificio_34", "Barrio/Manzana_4/Edificios/Edificio_43",
            "Barrio/Manzana_4/Edificios/Edificio_40", "Barrio/Manzana_4/Edificios/Edificio_37",
        };

        /// <summary>Lo que se apaga dentro de la franja de la calle nueva (adornos de esos edificios y luces de sus azoteas).</summary>
        public static readonly Bounds Franja = new Bounds(new Vector3((XOriente + XPoniente) * 0.5f, 20f, (-53.6f - 32.4f) * 0.5f),
            new Vector3(XOriente - XPoniente + 1.2f, 40f, 53.6f - 32.4f));

        /// <summary>La cara norte del callejón de los SKULLS (z = -54), donde estaban las fachadas de atrás de los edificios apagados.</summary>
        public static readonly Bounds CaraCallejon = new Bounds(new Vector3(-72f, 0f, -54.1f), new Vector3(31.5f, 1f, 1.6f));
        /// <summary>Lo alto de las casas nuevas (planta baja + piso alto del kit, con EscalaAlto).</summary>
        public const float AzoteaCasas = 6.2f;
        /// <summary>Fondo de las casas del sur junto al callejón: su muro de atrás queda donde estaba la cara norte del callejón (z = -54).</summary>
        public const float FondoSurCallejon = 5.25f;
        /// <summary>La cara norte del callejón: el muro de atrás de las casas del sur.</summary>
        public const float ZMuroCallejon = ZCentro - MedioAncho - FondoSurCallejon;

        private static readonly string[] RaicesDeAdorno = { "Barrio", "T501_Suburbios", "T512_LuzZonas", "T508_CallesLlenas", "AMB_Noche", "T509_Pulido", "T542_Letreros", "S2t_Calles" };
        private static readonly string[] NoTocar = { "Lugar_", "PuntosAparicion", "PuntosHistoria", "Peatones", "Persecucion", "/Calles", "Banqueta", "AmbienteSonoro", "Callejon" };

        // Toma de K01: el jugador en medio de la calle mirando al poniente, la cámara 3.8 m atrás a la altura del hombro un poco alta.
        public static readonly Vector3 JugadorFoto = new Vector3(-64f, 0f, ZCentro - 0.2f);
        public static readonly Vector3 CamaraFoto = new Vector3(-60.2f, 2.45f, ZCentro - 0.45f);
        public static readonly Vector3 MiraFoto = new Vector3(-100f, -0.6f, ZCentro - 0.1f);
        public const float CampoFoto = 63f;

        [MenuItem("Mecha Cyberpunk/S2c/1 Calle de prueba (K01)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Apagar();
            KitS2c.OlvidarMateriales("S2c_");
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            float suelo = KitS2c.Suelo(new Vector3(-95f, 0f, ZCentro), 0.2f);
            float arroyo = suelo + 0.02f;
            float banqueta = arroyo + AltoBanqueta;
            ColocarS2.Informe.AppendLine("Suelo de la manzana " + suelo.ToString("0.000") + ", arroyo " + arroyo.ToString("0.000"));

            Transform piso = Hijo(o.Raiz, "Piso");
            Transform casas = Hijo(o.Raiz, "Casas");
            Transform postes = Hijo(o.Raiz, "Postes");
            Transform cosas = Hijo(o.Raiz, "Cosas");

            // ---- Piso: arroyo, banquetas, líneas.
            float zArroyoSur = ZSur + Banqueta, zArroyoNorte = ZNorte - Banqueta;
            KitS2c.Losa(o, piso, "Arroyo", new Vector3((XBordeOriente + XBordePoniente) * 0.5f, arroyo - 0.05f, ZCentro), Quaternion.identity,
                new Vector3(XBordeOriente - XBordePoniente, 0.1f, zArroyoNorte - zArroyoSur), KitS2c.MatAsfalto(), 8f, true);
            foreach ((string n, float z0, float z1) in new[] { ("BanquetaSur", ZSur - 0.3f, zArroyoSur), ("BanquetaNorte", zArroyoNorte, ZNorte + 0.3f) })
            {
                KitS2c.Losa(o, piso, n, new Vector3((XOriente + XPoniente) * 0.5f, banqueta - (AltoBanqueta + 0.1f) * 0.5f, (z0 + z1) * 0.5f), Quaternion.identity,
                    new Vector3(XOriente - XPoniente, AltoBanqueta + 0.1f, z1 - z0), KitS2c.MatBanqueta(), 2f, true);
            }
            for (float x = XOriente - 1f; x > XPoniente + 1f; x -= 4f)
            {
                KitS2c.Linea(piso, new Vector3(x, arroyo, ZCentro), new Vector3(x - 1.6f, arroyo, ZCentro), 0.12f, arroyo);
            }
            foreach (float z in new[] { zArroyoSur + 0.35f, zArroyoNorte - 0.35f })
            {
                KitS2c.Linea(piso, new Vector3(XOriente, arroyo, z), new Vector3(XPoniente, arroyo, z), 0.1f, arroyo);
            }

            // ---- Casas. La primera de cada lado (la que se ve junto a la cámara en K01) va como en la imagen.
            // La casa 0 de cada lado queda junto a la cámara de la toma; de la 1 en adelante es lo que se ve en K01.
            KitS2c.Estilo Sur(int i) => i == 1 ? new KitS2c.Estilo { PlantaBaja = "C01_FachadaLocal_cy", ColorPiso = 1, Neon = null, Foco = true }
                : i == 2 ? new KitS2c.Estilo { PlantaBaja = "C02_FachadaVivienda_cy", ColorPiso = 0, Neon = KitS2c.Magenta, NeonBandera = false }
                : i == 3 ? new KitS2c.Estilo { PlantaBaja = "C01_FachadaLocal_cy", ColorPiso = 2, Neon = KitS2c.Turquesa }
                : KitS2c.EstiloAlAzar(o);
            KitS2c.Estilo Norte(int i) => i == 1 ? new KitS2c.Estilo { PlantaBaja = "C02_FachadaVivienda_cy", ColorPiso = 2, Foco = true }
                : i == 2 ? new KitS2c.Estilo { PlantaBaja = "C01_FachadaLocal_cy", ColorPiso = 0, Neon = KitS2c.Turquesa, NeonBandera = false }
                : KitS2c.EstiloAlAzar(o);
            List<Vector3> altosSur = KitS2c.Frente(o, casas, new Vector3(XOriente, banqueta, ZSur), new Vector3(XMedio, banqueta, ZSur), Vector3.forward, FondoSurCallejon, Sur);
            int ya = altosSur.Count;
            altosSur.AddRange(KitS2c.Frente(o, casas, new Vector3(XMedio, banqueta, ZSur), new Vector3(XPoniente, banqueta, ZSur), Vector3.forward, 10.2f, i => Sur(i + ya)));
            List<Vector3> altosNorte = KitS2c.Frente(o, casas, new Vector3(XOriente, banqueta, ZNorte), new Vector3(XMedio, banqueta, ZNorte), Vector3.back, 5.25f, Norte);
            int yb = altosNorte.Count;
            altosNorte.AddRange(KitS2c.Frente(o, casas, new Vector3(XMedio, banqueta, ZNorte), new Vector3(XPoniente, banqueta, ZNorte), Vector3.back, 3f, i => Norte(i + yb)));
            // Esquinas sobre la calle de la casa y la del poniente (donde estaban los edificios apagados).
            KitS2c.Frente(o, casas, new Vector3(XOriente, suelo, -54f), new Vector3(XOriente, suelo, ZSur), Vector3.right, 5.25f, i => KitS2c.EstiloAlAzar(o));
            KitS2c.Frente(o, casas, new Vector3(XOriente, suelo, ZNorte), new Vector3(XOriente, suelo, -32f), Vector3.right, 5.25f, i => KitS2c.EstiloAlAzar(o));
            KitS2c.Frente(o, casas, new Vector3(XPoniente, suelo, ZSur), new Vector3(XPoniente, suelo, -59f), Vector3.left, 6f, i => KitS2c.EstiloAlAzar(o));
            KitS2c.Losa(o, casas, "EsquinaNoroeste", new Vector3(XPoniente + 1.5f, suelo + 3.6f, (ZNorte - 35f) * 0.5f), Quaternion.identity,
                new Vector3(3f, 7.2f, -35f - ZNorte), KitS2c.MatCuerpo(1), 3f, true);

            // ---- Postes con su luz y halo, alternando lados; cables entre postes, hacia las casas y cruzando la calle.
            List<Vector3> puntasNorte = new List<Vector3>();
            List<Vector3> puntasSur = new List<Vector3>();
            for (float x = -68f; x > XPoniente + 4f; x -= 24f)
            {
                puntasNorte.Add(KitS2c.Poste(o, postes, new Vector3(x, banqueta, zArroyoNorte + 0.45f), Vector3.back, arroyo, true));
            }
            for (float x = -80f; x > XPoniente + 4f; x -= 24f)
            {
                puntasSur.Add(KitS2c.Poste(o, postes, new Vector3(x, banqueta, zArroyoSur - 0.45f), Vector3.forward, arroyo, o.Chance(0.4f)));
            }
            Cablear(o, puntasNorte, altosNorte, altosSur);
            Cablear(o, puntasSur, altosSur, altosNorte);
            for (int i = 0; i < 5; i++)
            {
                Vector3 a = o.Uno(altosSur);
                Vector3 b = altosNorte.OrderBy(p => Mathf.Abs(p.x - a.x - o.Rango(-6f, 6f))).First();
                KitS2c.Cable(o, a, b, o.Rango(0.6f, 1.3f));
            }

            // ---- Charcos (dos grandes frente a la cámara, como en K01), alcantarillas, basura y el carro estacionado.
            KitS2c.Charco(o, piso, new Vector3(-67.5f, arroyo, ZCentro - 1.6f), 3.6f, 2.2f, 80f);
            KitS2c.Charco(o, piso, new Vector3(-70.5f, arroyo, ZCentro + 1.9f), 3f, 1.6f, 100f);
            KitS2c.Charco(o, piso, new Vector3(-75f, arroyo, ZCentro + 0.4f), 2.2f, 1.4f, 70f);
            KitS2c.Charco(o, piso, new Vector3(-63.5f, arroyo, ZCentro + 2.3f), 2.6f, 1.5f, 95f);
            KitS2c.Charco(o, piso, new Vector3(-64.5f, arroyo, ZCentro - 2.6f), 2.2f, 1.2f, 85f);
            KitS2c.Charco(o, piso, new Vector3(-79f, arroyo, ZCentro - 2.2f), 2.8f, 1.6f, 75f);
            for (int i = 0; i < 12; i++)
            {
                KitS2c.Charco(o, piso, new Vector3(o.Rango(-80f, XPoniente + 3f), arroyo, ZCentro + o.Rango(-3f, 3f)), o.Rango(1.2f, 3.4f), o.Rango(0.8f, 2f), o.Rango(0f, 180f));
            }
            for (int i = 0; i < 3; i++)
            {
                KitS2c.Charco(o, piso, new Vector3(o.Rango(-62f, XPoniente + 3f), banqueta, (i % 2 == 0 ? ZSur + 1f : ZNorte - 1f)), o.Rango(0.8f, 1.4f), o.Rango(0.6f, 1f), o.Rango(0f, 180f));
            }
            Copiar(cosas, "Tapa", new[] { new Vector3(-73f, arroyo, zArroyoNorte - 0.35f), new Vector3(-97f, arroyo, zArroyoSur + 0.35f), new Vector3(-118f, arroyo, zArroyoNorte - 0.35f) });
            Copiar(cosas, "Bolsas1", new[] { new Vector3(-69.5f, banqueta, ZSur + 0.5f), new Vector3(-104f, banqueta, ZNorte - 0.5f) });
            Copiar(cosas, "Bolsas2", new[] { new Vector3(-88f, banqueta, ZSur + 0.5f) });
            KitS2c.Pieza(cosas, "C16_Tambo", new Vector3(-61.8f, banqueta, ZNorte - 0.6f), Quaternion.identity, Vector3.one);
            KitS2c.Pieza(cosas, "C11_Basurero", new Vector3(-92f, banqueta, ZNorte - 0.6f), Quaternion.LookRotation(Vector3.back), Vector3.one);
            KitS2c.Pieza(cosas, "C17_Huacales", new Vector3(-83.5f, banqueta, ZSur + 0.6f), Quaternion.Euler(0f, 20f, 0f), Vector3.one);
            Carro(cosas, new Vector3(-71.5f, arroyo, zArroyoSur + 1.1f), arroyo);

            PegarAlCallejon();
            SacarGenteDeLasCasas(o.Raiz);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Calle de prueba: " + o.Casas + " casas, " + o.Ventanas + " ventanas cálidas, " + o.Neones + " neones, " + o.Postes + " postes, "
                                         + o.Cables + " cables, " + o.Charcos + " charcos");
            ColocarS2.Guardar("s2c-calle");
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static void Cablear(KitS2c.Obra o, List<Vector3> puntas, List<Vector3> mismoLado, List<Vector3> otroLado)
        {
            for (int i = 0; i < puntas.Count; i++)
            {
                Vector3 p = puntas[i];
                Vector3 a = new Vector3(1f, 0f, 0f);
                if (i + 1 < puntas.Count)
                {
                    Vector3 q = puntas[i + 1];
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 d = Vector3.down * (k * 0.22f);
                        KitS2c.Cable(o, p + d + a * 0.0f, q + d, o.Rango(0.55f, 0.95f));
                    }
                }
                foreach (Vector3 c in mismoLado.OrderBy(c => (c - p).sqrMagnitude).Take(3))
                {
                    KitS2c.Cable(o, p + Vector3.down * 0.3f, c, o.Rango(0.25f, 0.5f), 0.02f);
                }
                // Mazo que cruza la calle hacia las casas de enfrente (en K01 salen 4 o 5 cables de cada poste).
                foreach (Vector3 c in otroLado.OrderBy(c => (c - p).sqrMagnitude).Take(3))
                {
                    for (int k = 0; k < 2; k++)
                    {
                        KitS2c.Cable(o, p + Vector3.down * (0.1f + k * 0.18f), c + Vector3.down * (k * 0.25f), o.Rango(0.8f, 1.4f));
                    }
                }
            }
        }

        /// <summary>Copias de un objeto de la escena (alcantarilla, bolsas de basura) en otros puntos, apoyadas en el piso.</summary>
        private static void Copiar(Transform padre, string nombre, Vector3[] puntos)
        {
            Transform modelo = ColocarS2.Todos().FirstOrDefault(t => t.name == nombre && t.GetComponentInChildren<Renderer>() != null && t.gameObject.activeInHierarchy);
            if (modelo == null)
            {
                ColocarS2.Informe.AppendLine("  no encontré " + nombre + " para copiar");
                return;
            }
            Renderer r0 = modelo.GetComponentInChildren<Renderer>();
            float bajo = r0.bounds.min.y - modelo.position.y;
            foreach (Vector3 p in puntos)
            {
                GameObject c = Object.Instantiate(modelo.gameObject, padre);
                c.name = nombre;
                c.transform.SetPositionAndRotation(p + Vector3.up * (0.005f - bajo), modelo.rotation * Quaternion.Euler(0f, p.x * 37f % 360f, 0f));
                c.transform.localScale = modelo.lossyScale;
                c.SetActive(true);
            }
        }

        private static void Carro(Transform padre, Vector3 p, float arroyo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab");
            GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            c.name = "CarroEstacionado";
            c.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0f, -6f, 0f));
            Renderer[] rs = c.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            c.transform.position += Vector3.up * (arroyo - b.min.y);
            if (c.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider bc = c.AddComponent<BoxCollider>();
                bc.center = c.transform.InverseTransformPoint(b.center + Vector3.up * (arroyo - b.min.y));
                bc.size = Quaternion.Inverse(c.transform.rotation) * b.size;
                bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z));
            }
        }

        /// <summary>Apaga los edificios de la fila de en medio y lo que estaba encima de ellos dentro de la franja.</summary>
        private static void Apagar()
        {
            HashSet<Object> referidos = ColocarS2.Referidos();
            int n = 0;
            foreach (string e in Edificios)
            {
                Transform t = ColocarS2.Buscar(e);
                if (t != null && t.gameObject.activeSelf)
                {
                    n += ColocarS2.Quitar(t.gameObject, referidos);
                }
            }
            List<GameObject> fuera = new List<GameObject>();
            foreach (Transform t in ColocarS2.Todos().ToList())
            {
                if (t == null || !t.gameObject.activeInHierarchy || !RaicesDeAdorno.Contains(t.root.name))
                {
                    continue;
                }
                string ruta = ColocarS2.Ruta(t);
                if (NoTocar.Any(ruta.Contains))
                {
                    continue;
                }
                Renderer r = t.GetComponent<Renderer>();
                Light l = t.GetComponent<Light>();
                // Lo que colgaba de la cara norte del callejón (los edificios apagados) y queda arriba de las azoteas nuevas.
                bool flotaEnCallejon = r != null && CaraCallejon.Contains(new Vector3(r.bounds.center.x, 0f, r.bounds.center.z)) && r.bounds.max.y > AzoteaCasas;
                if (r != null && (Contiene(Franja, r.bounds) || flotaEnCallejon) || l != null && r == null && Franja.Contains(t.position))
                {
                    fuera.Add(t.gameObject);
                }
            }
            foreach (GameObject g in fuera)
            {
                if (g != null && g.activeInHierarchy)
                {
                    n += ColocarS2.Quitar(g, referidos);
                }
            }
            ColocarS2.Informe.AppendLine("Franja de la calle nueva: " + n + " objetos fuera");
        }

        /// <summary>Lo que cuelga en la cara norte del callejón y quedó separado del muro nuevo (lámparas, medidores) se pega a él.</summary>
        private static void PegarAlCallejon()
        {
            int n = 0;
            foreach (Transform t in new[] { "S2_Callejon", "T502_Callejon", "T512_LuzZonas" }.Select(r => GameObject.Find(r)).Where(g => g != null)
                         .SelectMany(g => g.GetComponentsInChildren<Transform>()).Where(t => t.parent != null && t.GetComponent<Renderer>() == null && t.GetComponentInChildren<Renderer>() != null).ToList())
            {
                Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                if (b.size.z > 1f || !CaraCallejon.Contains(new Vector3(b.center.x, 0f, b.center.z)))
                {
                    continue;
                }
                float hueco = ZMuroCallejon - b.max.z;
                if (hueco > 0.03f && hueco < 1f)
                {
                    t.position += Vector3.forward * (hueco - 0.01f);
                    n++;
                }
            }
            ColocarS2.Informe.AppendLine("Pegadas al muro del callejón: " + n);
        }

        /// <summary>La gente parada de T-508 que quedó pegada a las casas nuevas (la física la empujaría) se aparta hacia la calle.</summary>
        private static void SacarGenteDeLasCasas(Transform raiz)
        {
            Physics.SyncTransforms();
            Collider[] propios = raiz.GetComponentsInChildren<Collider>().Where(c => c.enabled).ToArray();
            GameObject t508 = GameObject.Find("T508_CallesLlenas");
            if (t508 == null)
            {
                return;
            }
            foreach (Animator a in t508.GetComponentsInChildren<Animator>())
            {
                Transform t = a.transform;
                for (int i = 0; i < 12 && propios.Any(c => c.bounds.Intersects(new Bounds(t.position + Vector3.up, new Vector3(0.9f, 1.8f, 0.9f)))); i++)
                {
                    Collider c = propios.First(x => x.bounds.Intersects(new Bounds(t.position + Vector3.up, new Vector3(0.9f, 1.8f, 0.9f))));
                    Vector3 fuera = t.position - c.bounds.center;
                    fuera.y = 0f;
                    t.position += (Mathf.Abs(fuera.x) > Mathf.Abs(fuera.z) ? new Vector3(Mathf.Sign(fuera.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(fuera.z))) * 0.25f;
                    ColocarS2.Informe.AppendLine("  apartada " + ColocarS2.Ruta(t) + " → " + t.position);
                }
                // Se para en el piso que tiene debajo (si estaba sobre un edificio apagado, ya no flota).
                if (Physics.Raycast(t.position + Vector3.up * 0.6f, Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.point.y - t.position.y) > 0.02f)
                {
                    ColocarS2.Informe.AppendLine("  al piso " + ColocarS2.Ruta(t) + ": " + t.position.y.ToString("0.00") + " → " + h.point.y.ToString("0.00") + " (" + h.collider.name + ")");
                    t.position = new Vector3(t.position.x, h.point.y, t.position.z);
                }
            }
        }

        private static bool Contiene(Bounds caja, Bounds b) => caja.Contains(b.min) && caja.Contains(b.max);

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/S2c/1b Fotos de la calle de prueba")]
        public static void Fotos()
        {
            Fotos(EditorPrefs.GetInt("S2cRonda", 1));
        }

        public static void Fotos(int ronda)
        {
            ColocarS2.Abrir();
            float suelo = KitS2c.Suelo(new Vector3(-95f, 0f, ZCentro + 0.1f), 0.2f);
            GameObject jugador = GameObject.Find("JugadorAPie");
            if (jugador != null)
            {
                jugador.transform.SetPositionAndRotation(JugadorFoto + Vector3.up * suelo, Quaternion.Euler(0f, 90f, 0f));
            }
            string juego = "docs/capturas/s2c-calle-prueba-juego-" + ronda + ".png";
            ColocarS2cLook.Foto(CamaraFoto + Vector3.up * suelo, MiraFoto + Vector3.up * suelo, juego, CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K01_Barrio_HUDnuevo.png", juego, "docs/capturas/s2c-calle-prueba-" + ronda + ".png");
            ColocarS2cLook.Foto(new Vector3(-118f, 2.2f + suelo, ZCentro + 0.6f), new Vector3(-70f, 1.8f + suelo, ZCentro), "docs/capturas/s2c-calle-prueba-oriente-" + ronda + ".png", 60f);
            ColocarS2cLook.Foto(new Vector3(-50f, 3f + suelo, ZCentro - 9f), new Vector3(-62f, 2f + suelo, ZCentro), "docs/capturas/s2c-calle-prueba-entrada-" + ronda + ".png", 60f);
            ColocarS2cLook.Foto(new Vector3(-75f, 2f + suelo, -59f), new Vector3(-75f, 3f + suelo, -50f), "docs/capturas/s2c-calle-prueba-callejon-" + ronda + ".png", 70f);
            ColocarS2cLook.Foto(new Vector3(-80f, 34f, ZCentro - 30f), new Vector3(-90f, 0f, ZCentro), "docs/capturas/s2c-calle-prueba-alto-" + ronda + ".png", 60f);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Debug.Log("[Arte] Fotos de la calle de prueba, ronda " + ronda);
        }

        /// <summary>Batchmode: coloca y toma las fotos de la ronda -s2cRonda N.</summary>
        public static void ColocarYFotos()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-s2cRonda");
            int ronda = i >= 0 ? int.Parse(args[i + 1]) : 1;
            ColocarS2cLook.Aplicar();
            Colocar();
            Fotos(ronda);
        }
    }
}
