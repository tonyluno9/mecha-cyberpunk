using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-805 (noche 6-nov, sesión 2): la calle junto a la casa (la salida sur oeste de T-530, x = -47) ya no se va derecho al vacío:
    /// 35 m después de la orilla de la ciudad cruza una calle real (z -129..-115) que da vuelta hacia la salida sur este (x = 47) y regresa
    /// a la ciudad; las dos calles hacen un circuito. La calle cruzada lleva banquetas, edificios del kit a los dos lados (sin huecos ni
    /// callejones abiertos) y farolas. Los edificios de relleno de T-530 que quedaban sobre la calle nueva se apagan (con su kit de S2_Calles
    /// y sus cables). El carro robable 17 (T-804 lo movió y quedó pegado al 16) se estaciona en la calle nueva, libre.
    /// No toca Barrio.prefab. Correr después de ColocarT530 y ColocarS2Calles. Idempotente: rehace la raíz S2n_Calles.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nCalles.Colocar
    /// </summary>
    public static class ColocarS2nCalles
    {
        public const string NombreRaiz = "S2n_Calles";
        /// <summary>Calle cruzada nueva (m): de z0 a z1, entre las calles de las salidas sur (x de -X a X).</summary>
        public const float CruceZ0 = -129f;
        public const float CruceZ1 = -115f;
        public const float CruceX = 40f;
        public const float Banqueta = 3f;
        private const float AltoBanqueta = 0.2f;
        /// <summary>Banda de edificios al norte (entre la orilla y la calle) y al sur de la calle nueva.</summary>
        private const float NorteZ1 = -89f;
        private const float SurZ0 = -165f;
        /// <summary>Un edificio de T-530 que dejaría menos de esto (m) entre él y la calle nueva se cambia por uno que llega a la banqueta.</summary>
        private const float FondoMinimo = 6f;
        private const float EsquinaKit = 0.8f;
        private const float LodKit = 0.06f;
        private const float LodLejos = 0.004f;
        public const string RutaCarro17 = "GTA_T511/CarroRobable_17";
        /// <summary>Estacionado en la calle nueva (lado sur, a 5.8 m del centro como los demás), mirando al este: 55 m libres adelante. En el
        /// carril de la casa estorbaba el camino de la casa al callejón (RecorridoPrologoCiudadPruebas) y junto al 16 se atoraba.</summary>
        public static readonly Vector3 LugarCarro17 = new Vector3(-20f, 0.5f, -127.8f);
        private static readonly string[] Salidas = { "T530_Afueras/Salida_4", "T530_Afueras/Salida_5" };

        [MenuItem("Mecha Cyberpunk/S2n/5 Calles (circuito sur, callejones, carro 17)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Transform s4 = ColocarS2.Buscar(Salidas[0]);
            Transform s5 = ColocarS2.Buscar(Salidas[1]);
            if (s4 == null || s5 == null)
            {
                Debug.LogError("[Arte] T-805: faltan las salidas sur de T530_Afueras (corre ColocarT530 primero)");
                return;
            }
            Renderer calle4 = s4.Find("Calle").GetComponent<Renderer>();
            Material asfalto = calle4.sharedMaterial;
            Material banqueta = s4.Cast<Transform>().First(t => t.name == "Banqueta").GetComponent<Renderer>().sharedMaterial;
            List<Transform> edificios = s4.Cast<Transform>().Concat(s5.Cast<Transform>()).Where(t => t.name == "Edificio").ToList();
            Material[] fachadas = edificios.Select(e => e.GetComponent<Renderer>().sharedMaterial).Where(m => m != null).Distinct().ToArray();

            // 1. Piso: calle cruzada, banquetas partidas de las salidas, banquetas de la calle nueva.
            Transform piso = Grupo(raiz, "Piso");
            // En tramos cuadrados: un solo cubo de 80 m estira la textura del asfalto en rayas.
            int tramos = Mathf.CeilToInt(CruceX * 2f / (CruceZ1 - CruceZ0));
            float largoTramo = CruceX * 2f / tramos;
            for (int i = 0; i < tramos; i++)
            {
                Caja(piso, "CalleCruzada_" + i, asfalto, -CruceX + largoTramo * i, -CruceX + largoTramo * (i + 1), -1f, 0f, CruceZ0, CruceZ1);
            }
            foreach (Transform salida in new[] { s4, s5 })
            {
                float signo = Mathf.Sign(salida.Find("Calle").position.x);
                foreach (Transform b in salida.Cast<Transform>().Where(t => t.name == "Banqueta").ToList())
                {
                    // Cubo de T-530 (sin giro que cambie ejes): con el objeto ya apagado (segunda pasada) el renderer no da bounds.
                    Bounds bb = new Bounds(b.position, Abs(b.rotation * b.lossyScale));
                    bool interior = Mathf.Abs(bb.center.x) < Mathf.Abs(salida.Find("Calle").position.x);
                    if (!interior)
                    {
                        continue;
                    }
                    b.gameObject.SetActive(false);
                    Caja(piso, "Banqueta_" + salida.name + "_Norte", banqueta, bb.min.x, bb.max.x, -1f, AltoBanqueta, CruceZ1 + Banqueta, bb.max.z);
                    Caja(piso, "Banqueta_" + salida.name + "_Sur", banqueta, bb.min.x, bb.max.x, -1f, AltoBanqueta, bb.min.z, CruceZ0 - Banqueta);
                    ColocarS2.Informe.AppendLine("Banqueta interior de " + salida.name + " partida en la calle nueva (signo " + signo + ")");
                }
            }
            Caja(piso, "Banqueta_Cruce_Norte", banqueta, -CruceX, CruceX, -1f, AltoBanqueta, CruceZ1, CruceZ1 + Banqueta);
            Caja(piso, "Banqueta_Cruce_Sur", banqueta, -CruceX, CruceX, -1f, AltoBanqueta, CruceZ0 - Banqueta, CruceZ0);

            // 2. Edificios: fuera los de T-530 que quedaban sobre la calle nueva; se rellenan las dos bandas sin huecos.
            float interiorX = CruceX - Banqueta;           // 37: cara de los edificios interiores de las salidas
            List<Transform> apagados = new List<Transform>();
            System.Random azar = new System.Random(805);
            Transform casas = Grupo(raiz, "Edificios");
            int nuevos = 0, piezas = 0;
            (float x0, float x1, float caraSalida)[] columnas =
            {
                (-interiorX, -15f, -1f),   // interior de la salida 4: también da a la calle x = -47
                (-15f, 0f, 0f),
                (0f, 15f, 0f),
                (15f, interiorX, 1f),      // interior de la salida 5
            };
            foreach ((float x0, float x1, float caraSalida) in columnas)
            {
                foreach ((float za, float zb, float frente) in new[] { (CruceZ1 + Banqueta, NorteZ1, -1f), (SurZ0, CruceZ0 - Banqueta, 1f) })
                {
                    foreach ((float a, float b) in Huecos(edificios, apagados, x0, x1, za, zb))
                    {
                        piezas += Edificio(casas, fachadas, azar, x0, x1, a, b, frente, caraSalida, za, zb);
                        nuevos++;
                    }
                }
            }
            ApagarRestos(apagados);
            ColocarS2.Informe.AppendLine("Edificios de T-530 apagados: " + apagados.Count + "; nuevos: " + nuevos + " con " + piezas + " piezas del kit");

            // 3. Farolas en las banquetas de la calle nueva (copias de las del barrio, con su luz).
            int farolas = Farolas(raiz);
            ColocarS2.Informe.AppendLine("Farolas en la calle nueva: " + farolas + "; luces de postes: " + ColocarS2.LucesDePostes());

            // 4. Carro robable 17: libre en el carril de la casa.
            Carro17();
            ColocarS2.Guardar("s2n-calles");
        }

        /// <summary>Fotos de la calle de la casa, la calle nueva y vista cenital en docs/capturas/s2n-calles-&lt;cuando&gt;-*.png.</summary>
        public static void Fotos(string cuando)
        {
            ColocarS2.Abrir();
            string p = "docs/capturas/s2n-calles-" + cuando + "-";
            ColocarS2.Foto(new Vector3(-50f, 1.7f, -78f), new Vector3(-47f, 1.5f, -160f), p + "casa-sur.png", 70f);
            ColocarS2.Foto(new Vector3(-47f, 1.7f, -118f), new Vector3(20f, 3f, -122f), p + "vuelta.png", 70f);
            ColocarS2.Foto(new Vector3(30f, 1.7f, -126f), new Vector3(-40f, 3f, -120f), p + "cruce-oeste.png", 70f);
            ColocarS2.Foto(new Vector3(-47f, 70f, -60f), new Vector3(-10f, 0f, -135f), p + "alto.png", 70f);
        }

        public static void ColocarConFotos()
        {
            Fotos("antes");
            Colocar();
            Fotos("despues");
        }

        /// <summary>Tramos de [za, zb] (z) sin edificio en la columna [x0, x1]. Apaga los de T-530 que tocan la calle nueva o que dejarían una tira angosta.</summary>
        private static List<(float, float)> Huecos(List<Transform> edificios, List<Transform> apagados, float x0, float x1, float za, float zb)
        {
            List<(Transform t, Bounds b)> columna = edificios.Where(e => e.gameObject.activeSelf && !apagados.Contains(e)).Select(e => (e, e.GetComponent<Renderer>().bounds))
                .Where(p => Mathf.Min(p.Item2.max.x, x1) - Mathf.Max(p.Item2.min.x, x0) > 1f).ToList();
            foreach ((Transform t, Bounds b) in columna)
            {
                bool tocaCalle = b.max.z > CruceZ0 - Banqueta + 0.1f && b.min.z < CruceZ1 + Banqueta - 0.1f;
                bool angosto = (b.min.z > za + 0.1f && b.min.z - za < FondoMinimo && b.max.z > za) || (b.max.z < zb - 0.1f && zb - b.max.z < FondoMinimo && b.min.z < zb);
                bool dentro = b.max.z > za && b.min.z < zb;
                if (tocaCalle || (angosto && dentro))
                {
                    apagados.Add(t);
                }
            }
            List<(float a, float b)> cubiertos = columna.Where(p => !apagados.Contains(p.t)).Select(p => (Mathf.Max(p.b.min.z, za), Mathf.Min(p.b.max.z, zb)))
                .Where(p => p.Item2 - p.Item1 > 0.1f).OrderBy(p => p.Item1).ToList();
            List<(float, float)> huecos = new List<(float, float)>();
            float z = za;
            foreach ((float a, float b) in cubiertos)
            {
                if (a - z > 0.5f)
                {
                    huecos.Add((z, a));
                }
                z = Mathf.Max(z, b);
            }
            if (zb - z > 0.5f)
            {
                huecos.Add((z, zb));
            }
            return huecos;
        }

        // Cubo con collider y fachada, vestido con el kit en su cara a la calle nueva (y a la calle de la salida si es de esquina), LODGroup como S2_Calles.
        private static int Edificio(Transform padre, Material[] fachadas, System.Random azar, float x0, float x1, float z0, float z1, float frente, float caraSalida, float bandaZ0, float bandaZ1)
        {
            float alto = 10f + (float)azar.NextDouble() * 18f;
            GameObject cubo = Caja(padre, "Edificio", fachadas.Length > 0 ? fachadas[azar.Next(fachadas.Length)] : null, x0, x1, 0f, alto, z0, z1);
            cubo.isStatic = true;
            Bounds b = new Bounds(new Vector3((x0 + x1) * 0.5f, alto * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, alto, z1 - z0));
            Transform kit = new GameObject("Kit").transform;
            kit.SetParent(cubo.transform.parent, false);
            int n = 0;
            // Cara a la calle nueva: solo si el edificio llega a la banqueta de la calle nueva.
            bool daAlCruce = frente < 0f ? Mathf.Abs(z0 - bandaZ0) < 0.1f : Mathf.Abs(z1 - bandaZ1) < 0.1f;
            if (daAlCruce)
            {
                float cara = frente < 0f ? z0 : z1;
                n += ColocarS2Calles.Vestir(kit, true, new Vector3(0f, 0f, frente), cara, x0, x1, alto, azar);
            }
            if (caraSalida != 0f)
            {
                float cara = caraSalida < 0f ? x0 : x1;
                // Sin la esquina que ya viste la cara del cruce (no se enciman las piezas).
                float a = daAlCruce && frente > 0f ? z0 : z0 + (daAlCruce ? EsquinaKit : 0f);
                float c = daAlCruce && frente > 0f ? z1 - EsquinaKit : z1;
                n += ColocarS2Calles.Vestir(kit, false, new Vector3(caraSalida, 0f, 0f), cara, a, c, alto, azar);
            }
            n += ColocarS2Calles.Azotea(kit, b, azar);
            LODGroup lod = kit.gameObject.AddComponent<LODGroup>();
            Renderer rc = cubo.GetComponent<Renderer>();
            lod.SetLODs(new[] { new LOD(LodKit, kit.GetComponentsInChildren<Renderer>().Concat(new[] { rc }).ToArray()), new LOD(LodLejos, new[] { rc }) });
            lod.RecalculateBounds();
            return n;
        }

        // Los edificios apagados se llevan su kit de S2_Calles, sus ventanas de T-530 y los cables que colgaban de ellos.
        private static void ApagarRestos(List<Transform> apagados)
        {
            List<Bounds> cajas = new List<Bounds>();
            foreach (Transform e in apagados)
            {
                Renderer r = e.GetComponent<Renderer>();
                cajas.Add(r.bounds);
                foreach (LODGroup lod in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (lod.GetLODs().Any(l => l.renderers.Contains(r)))
                    {
                        lod.gameObject.SetActive(false);
                    }
                }
                e.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("  apagado " + ColocarS2.Ruta(e) + " " + r.bounds.min.ToString("F1") + ".." + r.bounds.max.ToString("F1"));
            }
            foreach (string s in Salidas)
            {
                Transform salida = ColocarS2.Buscar(s);
                foreach (Transform v in salida.Cast<Transform>().Where(t => t.name == "Ventanas"))
                {
                    if (cajas.Any(c => Plano(c, 1f).Contains(new Vector3(v.position.x, 0f, v.position.z))))
                    {
                        v.gameObject.SetActive(false);
                    }
                }
                Transform kits = ColocarS2.Buscar(ColocarS2Calles.NombreRaiz + "/" + salida.name);
                if (kits == null)
                {
                    continue;
                }
                foreach (LineRenderer lr in kits.GetComponentsInChildren<LineRenderer>(true))
                {
                    Vector3 p0 = lr.GetPosition(0);
                    Vector3 p1 = lr.GetPosition(lr.positionCount - 1);
                    if (cajas.Any(c => Plano(c, 1f).Contains(new Vector3(p0.x, 0f, p0.z)) || Plano(c, 1f).Contains(new Vector3(p1.x, 0f, p1.z))))
                    {
                        lr.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static Bounds Plano(Bounds b, float margen)
        {
            return new Bounds(new Vector3(b.center.x, 0f, b.center.z), new Vector3(b.size.x + margen * 2f, 1f, b.size.z + margen * 2f));
        }

        // Farolas cada 20 m alternando banquetas, con el brazo hacia la calle.
        private static int Farolas(Transform raiz)
        {
            Transform modelo = ColocarS2.Buscar("Barrio/Farolas")?.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Farola_") && t.gameObject.activeInHierarchy);
            if (modelo == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA Barrio/Farolas/Farola_*");
                return 0;
            }
            Transform grupo = Grupo(raiz, "Farolas");
            int n = 0;
            for (float x = -30f; x <= 30f; x += 20f)
            {
                bool norte = n % 2 == 0;
                float z = norte ? CruceZ1 + Banqueta * 0.5f : CruceZ0 - Banqueta * 0.5f;
                GameObject f = Object.Instantiate(modelo.gameObject, grupo);
                f.name = "Farola_S2n_" + n;
                f.transform.rotation = modelo.rotation;
                MeshFilter mf = f.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null && m.GetComponent<Renderer>().enabled);
                // Gira la farola hasta que su lámpara apunte hacia la calle.
                Vector3 hacia = norte ? Vector3.back : Vector3.forward;
                if (mf != null && ColocarS2.CabezaLampara(mf, out Vector3 cabeza))
                {
                    Vector3 brazo = cabeza - f.transform.position;
                    brazo.y = 0f;
                    float giro = Vector3.SignedAngle(brazo, hacia, Vector3.up);
                    f.transform.rotation = Quaternion.AngleAxis(giro, Vector3.up) * f.transform.rotation;
                }
                f.transform.position = new Vector3(x, modelo.position.y, z);
                n++;
            }
            return n;
        }

        private static void Carro17()
        {
            Transform carro = ColocarS2.Buscar(RutaCarro17);
            if (carro == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA " + RutaCarro17);
                return;
            }
            carro.SetPositionAndRotation(new Vector3(LugarCarro17.x, carro.position.y, LugarCarro17.z), Quaternion.Euler(0f, 90f, 0f));
            ColocarS2.Informe.AppendLine("Carro robable 17 en " + carro.position + " mirando al este");
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(padre, false);
            return g;
        }

        private static GameObject Caja(Transform padre, string nombre, Material m, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = nombre;
            c.transform.SetParent(padre, false);
            c.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            c.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            Renderer r = c.GetComponent<Renderer>();
            r.sharedMaterial = m;
            GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            if (y1 <= AltoBanqueta + 0.01f)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            return c;
        }
    }
}
