using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-910 (B7): coloca las piezas nuevas de calle y casino. Semáforo ES01 con ciclo (lo llama <see cref="ColocarS2cKit"/> desde cada esquina),
    /// basura BS01a-e suelta en las banquetas de la calle de prueba (lo llama <see cref="ColocarS2cCalle"/>), pasarela PS01 en lugar de los N02
    /// apagados de Neón Alto, puerta de cristal PC01 de dos hojas en el casino (lo llama <see cref="ColocarS2oCasino"/>) y la lata O01 en la
    /// bandeja de la expendedora CY06.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT910.Pasarelas  (y ColocarT910.LataEnExpendedora).
    /// </summary>
    public static class ColocarT910
    {
        public const string RutaConfigSemaforo = "Assets/_Proyecto/Mundo/Datos/ConfigSemaforo_Mundo.asset";
        public const string RutaMaterialLuz = "Assets/Arte/Calle/ES01_Semaforo/ES01_Luz.mat";
        public const string RaizPasarelas = "T910_Pasarelas";
        public const string NombreSemaforo = "Semaforo";
        public const string NombreLata = "Lata_O01";
        public const int SemillaBasura = 9101;
        public const int PiezasBasura = 16;
        /// <summary>Largo de la pasarela PS01 (m): de pared a pared de Neón Alto (x 37 a 57).</summary>
        public const float AnchoPasarela = 20f;
        /// <summary>
        /// Dónde cruza la cubierta de cada pasarela (z). Los N02 estaban en z 38 y 72, pero ahí camina la gente de las banquetas (T-510, z 14 a 46):
        /// con las escaleras encima caminarían trepando. Las cubiertas van más al norte, donde la banqueta está libre; las escaleras bajan al sur.
        /// </summary>
        public static readonly float[] ZCubiertas = { 60f, 73.5f };
        private const float CubiertaDesdeCentro = 5.6f;
        /// <summary>Lentes del ES01 respecto al pivote del prefab (medidas con ImportarT910.MedirSemaforo): x, z de la cara y alturas rojo, ámbar, verde.</summary>
        private static readonly Vector3[] Lentes = { new Vector3(-1f, 3.74f, -0.215f), new Vector3(-1f, 3.36f, -0.215f), new Vector3(-1f, 2.98f, -0.215f) };
        private const float DiametroLente = 0.2f;
        private const float GrosorLente = 0.02f;
        /// <summary>Dónde cae la lata O01 en la bandeja de la CY06 (pivote del prefab: centro en x y z, base en el suelo).</summary>
        private static readonly Vector3 PosicionLata = new Vector3(-0.21f, 0.34f, -0.4f);

        /// <summary>
        /// Batchmode: pone las piezas en Ciudad_Prologo sin rehacer los suburbios (rehacerlos desharía las pasadas posteriores de luces, gente y
        /// limpieza): cambia solo los semáforos y las bolsas copiadas, y luego coloca las pasarelas y la puerta del casino.
        /// </summary>
        public static void TodoEnEscena()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            CambiarSemaforos();
            CambiarBolsas();
            ColocarS2.Guardar("t910-escena");
            Pasarelas();
            ColocarS2oCasino.Colocar();
        }

        /// <summary>Cambia cada semáforo viejo (poste N08 con micas) por el ES01 con ciclo, en el mismo lugar y mirando al mismo cruce.</summary>
        public static void CambiarSemaforos()
        {
            int n = 0;
            foreach (Transform viejo in ColocarS2.Todos().Where(t => t.name == NombreSemaforo && t.GetComponent<SemaforoCiclo>() == null && t.Find("Poste") != null).ToArray())
            {
                Transform poste = viejo.Find("Poste");
                Vector3 pie = viejo.position;
                Vector3 haciaCruce = -poste.forward;
                haciaCruce.y = 0f;
                haciaCruce.Normalize();
                // Giro de la esquina: la dirección al cruce es (-1,0,-1) girada 0, 90, 180 o -90°; las calles cruzadas son las de giro impar.
                float yaw = Mathf.Atan2(haciaCruce.x, haciaCruce.z) * Mathf.Rad2Deg;
                bool contrafase = Mathf.Abs(Mathf.RoundToInt((yaw + 135f) / 90f)) % 2 != 0;
                Transform padre = viejo.parent;
                int indice = viejo.GetSiblingIndex();
                Object.DestroyImmediate(viejo.gameObject);
                GameObject nuevo = Semaforo(null, padre, pie, haciaCruce, contrafase);
                nuevo.transform.SetSiblingIndex(indice);
                n++;
            }
            ColocarS2.Informe.AppendLine("Semáforos ES01 en lugar de los N08: " + n);
        }

        /// <summary>Quita las copias de bolsas del callejón de la calle de prueba y pone la basura BS01.</summary>
        public static void CambiarBolsas()
        {
            Transform[] bolsas = ColocarS2.Todos().Where(t => (t.name == "Bolsas1" || t.name == "Bolsas2") && t.parent != null && t.parent.parent != null && t.parent.parent.name == ColocarS2cCalle.Raiz).ToArray();
            Transform padre = bolsas.Length > 0 ? bolsas[0].parent : ColocarS2.Todos().First(t => t.name == ColocarS2cCalle.Raiz);
            foreach (Transform b in bolsas)
            {
                Object.DestroyImmediate(b.gameObject);
            }
            Object.DestroyImmediate(ColocarS2.Todos().FirstOrDefault(t => t.name == "Basura_BS01")?.gameObject);
            Basura(padre, KitS2c.Suelo(new Vector3(-95f, 0f, ColocarS2cCalle.ZCentro), 0.2f) + 0.02f + ColocarS2cCalle.AltoBanqueta);
            ColocarS2.Informe.AppendLine("Bolsas copiadas quitadas: " + bolsas.Length);
        }

        /// <summary>Diagnóstico: recorridos de la gente que camina cerca de Neón Alto.</summary>
        public static void DiagGente()
        {
            ColocarS2.Abrir();
            foreach (var g in Object.FindObjectsByType<MechaCyberpunk.Personaje.Visual.GenteDeBanqueta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.PuntoA.x > 34f && g.PuntoA.x < 60f)
                {
                    Debug.Log("[T910gente] " + g.Modo + " " + g.PuntoA.ToString("F1") + " -> " + g.PuntoB.ToString("F1"));
                }
            }
        }

        // ------------------------------------------------------------------ semáforo

        public static ConfigSemaforo Config()
        {
            ConfigSemaforo c = AssetDatabase.LoadAssetAtPath<ConfigSemaforo>(RutaConfigSemaforo);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<ConfigSemaforo>();
                AssetDatabase.CreateAsset(c, RutaConfigSemaforo);
            }
            return c;
        }

        private static Material MaterialLuz()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialLuz);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterialLuz);
            }
            m.SetTexture("_BaseMap", KitS2c.TexOvalo());
            m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f));
            m.SetFloat("_Smoothness", 0.8f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// El semáforo ES01 de una esquina: el modelo con la cara hacia el cruce, tres discos sobre sus lentes y el SemaforoCiclo que los enciende
        /// de uno en uno. <paramref name="contrafase"/>: esquina de la calle cruzada (medio ciclo de diferencia).
        /// </summary>
        public static GameObject Semaforo(KitS2c.Obra o, Transform padre, Vector3 pie, Vector3 haciaCruce, bool contrafase)
        {
            haciaCruce.y = 0f;
            haciaCruce.Normalize();
            GameObject s = new GameObject(NombreSemaforo);
            s.transform.SetParent(padre, false);
            s.transform.position = pie;
            // El modelo trae las lentes en su cara -Z: esa cara mira al cruce.
            Quaternion giro = Quaternion.LookRotation(-haciaCruce);
            GameObject poste = KitS2c.Pieza(s.transform, "ES01_Semaforo", pie, giro, Vector3.one);
            poste.name = "Poste";
            Material luz = MaterialLuz();
            Renderer[] discos = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(d.GetComponent<Collider>());
                d.name = "Lente_" + (i == 0 ? "Roja" : i == 1 ? "Ambar" : "Verde");
                d.transform.SetParent(poste.transform, false);
                d.transform.localPosition = Lentes[i];
                d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                d.transform.localScale = new Vector3(DiametroLente, GrosorLente * 0.5f, DiametroLente);
                MeshRenderer r = d.GetComponent<MeshRenderer>();
                r.sharedMaterial = luz;
                r.shadowCastingMode = ShadowCastingMode.Off;
                discos[i] = r;
            }
            Light l = KitS2c.Luz(s.transform, poste.transform.TransformPoint(Lentes[0] + Vector3.back * 0.5f), LightType.Point, new Color(1f, 0.2f, 0.15f), 0.6f, 3f, 0f);
            SemaforoCiclo ciclo = s.AddComponent<SemaforoCiclo>();
            SerializedObject so = new SerializedObject(ciclo);
            so.FindProperty("config").objectReferenceValue = Config();
            SerializedProperty arr = so.FindProperty("lentes");
            arr.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = discos[i];
            }
            so.FindProperty("luz").objectReferenceValue = l;
            so.FindProperty("contrafase").boolValue = contrafase;
            so.ApplyModifiedPropertiesWithoutUndo();
            return s;
        }

        // ------------------------------------------------------------------ basura

        /// <summary>
        /// Basura BS01a-e suelta sobre las dos banquetas de la calle de prueba: pieza, giro y lugar al azar con semilla fija (<see cref="SemillaBasura"/>).
        /// Sin collider (el prefab no lo trae): no estorba al jugador. Se salta el cruce con la calle de norte a sur.
        /// </summary>
        public static void Basura(Transform padre, float banqueta)
        {
            System.Random azar = new System.Random(SemillaBasura);
            Transform raiz = new GameObject("Basura_BS01").transform;
            raiz.SetParent(padre, false);
            string[] piezas = { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "BS01d_Basura", "BS01e_Basura" };
            float desde = ColocarS2cCalle.XPoniente + 4f;
            float hasta = ColocarS2cCalle.XOriente - 4f;
            int n = 0;
            while (n < PiezasBasura)
            {
                float x = desde + (float)azar.NextDouble() * (hasta - desde);
                bool lado = azar.Next(2) == 0;
                float fondo = 0.35f + (float)azar.NextDouble() * 1.3f;
                float giro = (float)azar.NextDouble() * 360f;
                string pieza = piezas[azar.Next(piezas.Length)];
                if (Mathf.Abs(x - ColocarS2cCalle.XCruce) < ColocarS2cCalle.MedioCruce + 0.5f)
                {
                    continue;
                }
                float z = lado ? ColocarS2cCalle.ZSur + fondo : ColocarS2cCalle.ZNorte - fondo;
                KitS2c.Pieza(raiz, pieza, new Vector3(x, banqueta, z), Quaternion.Euler(0f, giro, 0f), Vector3.one, false);
                n++;
            }
            ColocarS2.Informe.AppendLine("Basura BS01: " + n + " piezas sueltas en las banquetas (semilla " + SemillaBasura + ")");
        }

        // ------------------------------------------------------------------ pasarela

        /// <summary>
        /// Pasarela PS01 (K07) en lugar de los dos N02 de Neón Alto (que siguen apagados): de banqueta a banqueta sobre la calle, con la escalera a cada
        /// lado. El collider es la malla del modelo (peldaños y cubierta), con la calle libre por debajo.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-910/Colocar pasarelas PS01")]
        public static void Pasarelas()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(RaizPasarelas));
            Transform raiz = new GameObject(RaizPasarelas).transform;
            Transform[] puentes = ColocarS2.Todos().Where(t => t.name == ColocarS2oPuentes.NombrePuente).ToArray();
            GameObject prefab = KitS2c.Prefab("PS01_Pasarela");
            for (int i = 0; i < puentes.Length && i < ZCubiertas.Length; i++)
            {
                // La cubierta queda al extremo +z del modelo (a 0.65 m de su borde); las escaleras bajan hacia -z.
                Vector3 p = new Vector3(ColocarS2cNeonAlto.XCalle, 0f, ZCubiertas[i] - CubiertaDesdeCentro);
                Physics.SyncTransforms();
                float y = SueloDe(new Vector3(ColocarS2cNeonAlto.XCalle - 8.5f, 30f, p.z), puentes[i].position.y);
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                g.name = "PS01_Pasarela";
                g.transform.position = new Vector3(p.x, y, p.z);
                ColocarS2.Informe.AppendLine("PS01 en " + g.transform.position.ToString("F1") + ", caja " + ArteT250.Limites(g).size.ToString("F1") + " (suelo y " + y.ToString("F2") + ")");
            }
            ColocarS2.Informe.AppendLine("Pasarelas PS01: " + puentes.Length + " (N02 siguen apagados)");
            ColocarS2.Guardar("t910-pasarelas");
        }

        private static float SueloDe(Vector3 desde, float respaldo)
        {
            // El suelo más bajo en las dos banquetas (un carro o un poste en medio no cuentan).
            float bajo = float.MaxValue;
            foreach (float x in new[] { desde.x, desde.x + 17f })
            {
                foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(x, desde.y, desde.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.point.y > 0.1f && hit.point.y < 1f)
                    {
                        bajo = Mathf.Min(bajo, hit.point.y);
                    }
                }
            }
            if (bajo < float.MaxValue)
            {
                return bajo;
            }
            return respaldo > 0f && respaldo < 1f ? respaldo : 0.2f;
        }

        // ------------------------------------------------------------------ puerta del casino

        /// <summary>
        /// La puerta de cristal PC01 de dos hojas: ocupa el vano que antes llenaba la hoja de cubo. La HojaPuerta de siempre sigue mandando
        /// (collider, abrir sola, cerrarse) pero ya no se dibuja; <see cref="HojasCorredizas"/> desliza las dos hojas de cristal a los lados.
        /// El FBX es una sola malla: se cortó por el centro (ver ImportarT910.CrearPuertaCorrediza).
        /// </summary>
        public static void PuertaCristal(Transform hoja, float ancho, Vector3 dirLargo)
        {
            Transform puerta = hoja.parent;
            foreach (Transform viejo in puerta.Cast<Transform>().Where(t => t.name == "PC01_PuertaCorrediza").ToArray())
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            MeshRenderer visible = hoja.GetComponent<MeshRenderer>();
            Bounds b = visible.bounds;
            visible.enabled = false;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarT910.PrefabPuertaCorrediza);
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, puerta);
            Vector3 haciaCalle = Vector3.left; // la fachada del casino da al poniente
            g.transform.rotation = Quaternion.LookRotation(-haciaCalle);
            g.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            const float anchoModelo = 2.3f;
            const float altoModelo = 2.2f;
            float sx = ancho / anchoModelo;
            float sy = b.size.y / altoModelo;
            g.transform.localScale = Vector3.one;
            // Escala en el espacio del padre: la rotación es de 90°, así que se reparte por ejes de modelo (x ancho, y alto, z fondo).
            g.transform.localScale = new Vector3(sx, sy, Mathf.Min(sx, sy) * 0.5f);
            float desliza = ancho * 0.3f / sx;
            HojasCorredizas hc = g.AddComponent<HojasCorredizas>();
            SerializedObject so = new SerializedObject(hc);
            so.FindProperty("hoja").objectReferenceValue = hoja.GetComponent<HojaPuerta>();
            so.FindProperty("izquierda").objectReferenceValue = g.transform.Find("PC01_HojaIzq");
            so.FindProperty("derecha").objectReferenceValue = g.transform.Find("PC01_HojaDer");
            so.FindProperty("deslizamientoIzquierda").vector3Value = new Vector3(-desliza, 0f, 0f);
            so.FindProperty("deslizamientoDerecha").vector3Value = new Vector3(desliza, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Puerta PC01 de dos hojas: ancho " + ancho.ToString("F2") + " m, alto " + b.size.y.ToString("F2") + " m, cada hoja se desliza " + (desliza * sx).ToString("F2") + " m");
        }

        // ------------------------------------------------------------------ lata

        /// <summary>Pone la lata O01 (bebida energética RAYO) en la bandeja de entrega de la expendedora CY06 (prefab: las máquinas de la ciudad la heredan).</summary>
        [MenuItem("Mecha Cyberpunk/T-910/Lata en la expendedora")]
        public static void LataEnExpendedora()
        {
            const string ruta = "Assets/Arte/Calle/Prefabs/CY06_Expendedora.prefab";
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            foreach (Transform viejo in raiz.transform.Cast<Transform>().Where(t => t.name == NombreLata).ToArray())
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject lata = (GameObject)PrefabUtility.InstantiatePrefab(KitS2c.Prefab("O01_BebidaRayo"), raiz.transform);
            lata.name = NombreLata;
            lata.transform.localPosition = PosicionLata;
            lata.transform.localRotation = Quaternion.identity;
            lata.transform.localScale = Vector3.one;
            foreach (Collider c in lata.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            PrefabUtility.UnloadPrefabContents(raiz);
            Debug.Log("[Arte] T-910: lata O01 en la bandeja de CY06_Expendedora");
        }
    }
}
