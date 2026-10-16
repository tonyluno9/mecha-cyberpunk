using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el interior del bar de T-043 dentro del prefab del barrio: muros gruesos con collider, barra con bancos, mesas,
    /// escenario con cabina de DJ, puerta falsa a los baños, pantallas, neón, luces y la <see cref="ZonaInterior"/>.
    /// Corre después de CreadorBarrio, CreadorReloj y CreadorLugares y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorBar.Crear
    /// </summary>
    public static class CreadorBar
    {
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        // Medidas del bar (m), las mismas de CreadorBarrio y CreadorLugares.
        private const float AnchoPuerta = 3f;
        private const float GrosorFachada = 1f;
        private const float ProfundidadVestibulo = 3f;
        private const float DistanciaEntrada = 1.5f;
        private const float AnchoInterior = 20f;
        private const float FondoInterior = 14f;
        private const float AltoInterior = 4.5f;
        private const float GrosorRevestimiento = 0.1f;

        private const string RutaKit = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const string RutaPropBarra = RutaKit + "Prop_Barra.prefab";
        private const string RutaPropMesa = RutaKit + "Prop_Mesa.prefab";
        private const string RutaPropDj = RutaKit + "Prop_DJ.prefab";
        private const string RutaPropLetrero = RutaKit + "Prop_Letrero.prefab";
        private const int CantidadBarras = 5;
        private const float AnchoBarra = 3.1f;
        private const float DistanciaBarra = 2f;    // m desde el muro del fondo al centro de cada barra (su estante queda a 1.2 m)
        private const int BotellasPorEstante = 14;
        private const int TirasTecho = 3;
        private const float IntensidadNeonEscenario = 5f;
        private const float IntensidadRelleno = 13f;
        private const float AlturaColgante = 0.9f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-043")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.3f, 0.2f, 0.4f), 0.1f, default);
            Material piso = Mat("Piso", new Color(0.22f, 0.22f, 0.28f), 0.45f, default);
            Material techo = Mat("Techo", new Color(0.24f, 0.18f, 0.3f), 0f, default);
            Material madera = Mat("Madera", new Color(0.3f, 0.17f, 0.09f), 0.3f, default);
            Material cuero = Mat("Cuero", new Color(0.45f, 0.08f, 0.18f), 0.3f, default);
            Material metal = Mat("Metal", new Color(0.18f, 0.19f, 0.22f), 0.5f, default);
            Material neonCian = Mat("NeonCian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));
            Material neonMagenta = Mat("NeonMagenta", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 2f));
            Material neonNaranja = Mat("NeonNaranja", new Color(0.95f, 0.5f, 0.1f), 0.2f, new Color(3.2f, 1.4f, 0.15f));
            Material botellaAmbar = Mat("BotellaAmbar", new Color(0.5f, 0.25f, 0.05f), 0.6f, new Color(0.7f, 0.32f, 0.05f));
            Material botellaVerde = Mat("BotellaVerde", new Color(0.1f, 0.4f, 0.2f), 0.6f, new Color(0.1f, 0.55f, 0.25f));
            Material botellaAzul = Mat("BotellaAzul", new Color(0.1f, 0.25f, 0.5f), 0.6f, new Color(0.1f, 0.3f, 0.7f));
            Material lampara = Mat("Lampara", new Color(0.9f, 0.6f, 0.3f), 0.3f, new Color(2.2f, 1.3f, 0.5f));
            Material pantalla = Mat("Pantalla", new Color(0.05f, 0.3f, 0.35f), 0.4f, new Color(0.15f, 0.9f, 1.0f));

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform bar = BuscarPorNombre(raiz.transform, "Lugar_Bar");
            Transform viejo = bar.Find("Interior");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            LugarMundo lugarMundo = bar.GetComponent<LugarMundo>();
            PuertaLocal puertaLocal = bar.Find("Puerta").GetComponent<PuertaLocal>();
            Transform entrada = bar.Find("Entrada");

            float zFrente = entrada.localPosition.z - DistanciaEntrada;
            float zFrenteInterior = zFrente - GrosorFachada - ProfundidadVestibulo;
            float zFondoInterior = zFrenteInterior - FondoInterior;
            Material muroExterior = bar.Find("CuerpoLadoIzquierdo").GetComponent<MeshRenderer>().sharedMaterial;
            float altoCuerpo = bar.Find("CuerpoLadoIzquierdo").localScale.y;
            float anchoCuerpo = AnchoPuerta + 2f * bar.Find("CuerpoLadoIzquierdo").localScale.x;

            interior = new GameObject("Interior").transform;
            interior.SetParent(bar, false);

            // Quita el macizo detrás del vestíbulo y lo reemplaza por muros alrededor del interior.
            Transform fondoViejo = bar.Find("CuerpoFondo");
            if (fondoViejo != null)
            {
                Object.DestroyImmediate(fondoViejo.gameObject);
            }
            Transform estructura = Grupo("Estructura");
            float mitadAncho = anchoCuerpo / 2f;
            float mitadInterior = AnchoInterior / 2f;
            float grosorLado = mitadAncho - mitadInterior;
            float zCentroInterior = (zFrenteInterior + zFondoInterior) / 2f;
            float profundidadTrasera = zFondoInterior + zFrente;
            Caja(estructura, "MuroFondo", new Vector3(0f, altoCuerpo / 2f, -zFrente + profundidadTrasera / 2f), new Vector3(anchoCuerpo, altoCuerpo, profundidadTrasera), muroExterior, true);
            Caja(estructura, "MuroIzquierdo", new Vector3(-(mitadInterior + grosorLado / 2f), altoCuerpo / 2f, zCentroInterior), new Vector3(grosorLado, altoCuerpo, FondoInterior), muroExterior, true);
            Caja(estructura, "MuroDerecho", new Vector3(mitadInterior + grosorLado / 2f, altoCuerpo / 2f, zCentroInterior), new Vector3(grosorLado, altoCuerpo, FondoInterior), muroExterior, true);
            float altoSobreTecho = altoCuerpo - AltoInterior;
            Caja(estructura, "TechoInterior", new Vector3(0f, AltoInterior + altoSobreTecho / 2f, zCentroInterior), new Vector3(AnchoInterior, altoSobreTecho, FondoInterior), muroExterior, true);

            // Revestimiento interior sin collider: piso, techo y muros.
            Transform revestimiento = Grupo("Revestimiento");
            Caja(revestimiento, "Piso", Pos(0f, 0.02f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, 0.04f, FondoInterior), piso, false);
            Caja(revestimiento, "Techo", Pos(0f, AltoInterior - GrosorRevestimiento / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, GrosorRevestimiento, FondoInterior), techo, false);
            Caja(revestimiento, "MuroAtras", Pos(0f, AltoInterior / 2f, GrosorRevestimiento / 2f, zFondoInterior), new Vector3(AnchoInterior, AltoInterior, GrosorRevestimiento), muro, false);
            Caja(revestimiento, "MuroIzq", Pos(-mitadInterior + GrosorRevestimiento / 2f, AltoInterior / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(GrosorRevestimiento, AltoInterior, FondoInterior), muro, false);
            Caja(revestimiento, "MuroDer", Pos(mitadInterior - GrosorRevestimiento / 2f, AltoInterior / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(GrosorRevestimiento, AltoInterior, FondoInterior), muro, false);
            float anchoFrente = (AnchoInterior - AnchoPuerta) / 2f;
            Caja(revestimiento, "MuroFrenteIzq", Pos(-(AnchoPuerta / 2f + anchoFrente / 2f), AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);
            Caja(revestimiento, "MuroFrenteDer", Pos(AnchoPuerta / 2f + anchoFrente / 2f, AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);

            // Barra de 5 Prop_Barra en fila (con sus bancos), estante de botellas en el muro y tiras de neón.
            Transform mobiliario = Grupo("Mobiliario");
            for (int i = 0; i < CantidadBarras; i++)
            {
                Prop(mobiliario, RutaPropBarra, Pos((i - (CantidadBarras - 1) / 2f) * AnchoBarra, 0.02f, DistanciaBarra, zFondoInterior), 0f);
            }
            Material[] botellas = { botellaAmbar, botellaVerde, botellaAzul, botellaAmbar, botellaVerde };
            float[] alturasEstante = { 2.3f, 2.85f };
            foreach (float alturaEstante in alturasEstante)
            {
                Caja(mobiliario, "Estante", Pos(0f, alturaEstante, 0.3f, zFondoInterior), new Vector3(14f, 0.08f, 0.45f), metal, false);
                for (int i = 0; i < BotellasPorEstante; i++)
                {
                    float alto = 0.3f + 0.05f * (i % 4);
                    Caja(mobiliario, "Botella", Pos(-6.7f + i * 13.4f / (BotellasPorEstante - 1), alturaEstante + 0.04f + alto / 2f, 0.3f, zFondoInterior), new Vector3(0.15f, alto, 0.15f), botellas[i % botellas.Length], false);
                }
            }

            // Mesas redondas con sillas en una cuadrícula con pasillos de más de 1.2 m.
            float[] mesasX = { -7.5f, -4.8f, -2.1f, 3.2f };
            float[] mesasZ = { 7f, 10.5f };
            foreach (float mx in mesasX)
            {
                foreach (float mz in mesasZ)
                {
                    Prop(mobiliario, RutaPropMesa, Pos(mx, 0.02f, mz, zFondoInterior), 0f);
                }
            }

            // Escenario pequeño con la cabina Prop_DJ en la esquina derecha, mirando al salón (hacia -x).
            Caja(mobiliario, "Escenario", Pos(8.3f, 0.25f, 9.2f, zFondoInterior), new Vector3(3.4f, 0.5f, 6.5f), metal, true);
            Prop(mobiliario, RutaPropDj, Pos(9.0f, 0.5f, 9.2f, zFondoInterior), -90f);

            // Puerta falsa a los baños en el muro izquierdo.
            Caja(mobiliario, "PuertaBanos", Pos(-mitadInterior + 0.15f, 1.1f, 5.5f, zFondoInterior), new Vector3(0.1f, 2.2f, 1.3f), metal, false);
            Caja(mobiliario, "LetreroBanos", Pos(-mitadInterior + 0.15f, 2.6f, 5.5f, zFondoInterior), new Vector3(0.06f, 0.3f, 1.0f), neonCian, false);

            // Dos Prop_Letrero: en el muro del fondo y junto a la puerta de los baños.
            Prop(mobiliario, RutaPropLetrero, Pos(0f, 2.9f, 0.2f, zFondoInterior), 0f);
            Prop(mobiliario, RutaPropLetrero, Pos(-mitadInterior + 0.2f, 1.9f, 7.6f, zFondoInterior), 90f);

            // Pantallas y tiras de neón.
            Transform neon = Grupo("Neon");
            Caja(neon, "PantallaAtras1", Pos(-4.5f, 3.3f, 0.2f, zFondoInterior), new Vector3(3.5f, 1.3f, 0.06f), pantalla, false);
            Caja(neon, "PantallaAtras2", Pos(4.5f, 3.3f, 0.2f, zFondoInterior), new Vector3(3.5f, 1.3f, 0.06f), pantalla, false);
            Caja(neon, "PantallaEscenario", Pos(mitadInterior - 0.2f, 2.8f, 9.2f, zFondoInterior), new Vector3(0.06f, 1.6f, 4.5f), pantalla, false);
            Caja(neon, "TiraAtras", Pos(0f, 4.2f, 0.12f, zFondoInterior), new Vector3(18f, 0.08f, 0.04f), neonMagenta, false);
            Caja(neon, "TiraIzquierda", Pos(-mitadInterior + 0.12f, 4.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.08f, 13f), neonCian, false);
            Caja(neon, "TiraDerecha", Pos(mitadInterior - 0.12f, 4.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.08f, 13f), neonCian, false);
            for (int i = 0; i < TirasTecho; i++)
            {
                Caja(neon, "TiraTecho", Pos(0f, AltoInterior - 0.12f, 3f + i * 4.5f, zFondoInterior), new Vector3(16f, 0.04f, 0.08f), i % 2 == 0 ? neonCian : neonMagenta, false);
            }
            Caja(neon, "TiraCentro", Pos(0f, AltoInterior - 0.15f, 7f, zFondoInterior), new Vector3(0.08f, 0.04f, 9f), neonMagenta, false);

            // Lámparas colgantes cálidas sobre la barra y las mesas (cada una con su luz) y luces de relleno y de neón.
            Transform grupoLuces = Grupo("Luces");
            Color calida = new Color(1f, 0.7f, 0.4f);
            Light[] luces =
            {
                Colgante(grupoLuces, lampara, "LuzCalidaBarra1", Pos(-5f, AltoInterior, 3.4f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaBarra2", Pos(0f, AltoInterior, 3.4f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaBarra3", Pos(5f, AltoInterior, 3.4f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaMesasIzq", Pos(-4.8f, AltoInterior, 8.75f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaMesasDer", Pos(3.2f, AltoInterior, 8.75f, zFondoInterior), calida),
                Luz(grupoLuces, "LuzNeonEscenario", Pos(7.0f, 3.4f, 9.2f, zFondoInterior), new Color(0.9f, 0.1f, 0.7f), IntensidadNeonEscenario, 9f),
                Luz(grupoLuces, "LuzNeonBarra", Pos(0f, 3.0f, 1.2f, zFondoInterior), new Color(0.1f, 0.8f, 0.9f), 5f, 9f),
                Luz(grupoLuces, "LuzEntrada", Pos(0f, 3.8f, 12.5f, zFondoInterior), calida, 4f, 6f),
                Luz(grupoLuces, "LuzRellenoIzq", Pos(-5f, 4.0f, 6f, zFondoInterior), new Color(0.75f, 0.7f, 1f), IntensidadRelleno, 12f),
                Luz(grupoLuces, "LuzRellenoDer", Pos(5f, 4.0f, 6f, zFondoInterior), new Color(0.75f, 0.7f, 1f), IntensidadRelleno, 12f)
            };

            // Zona: del umbral interior (cara de adentro de la puerta) al muro del fondo.
            float zFrenteZona = zFrente - GrosorFachada;
            float profundidadZona = zFrenteZona - zFondoInterior;
            GameObject zonaObjeto = new GameObject("ZonaInterior");
            zonaObjeto.transform.SetParent(interior, false);
            zonaObjeto.transform.localPosition = new Vector3(0f, AltoInterior / 2f, zFondoInterior + profundidadZona / 2f);
            BoxCollider caja = zonaObjeto.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(AnchoInterior, AltoInterior, profundidadZona);
            ZonaInterior zona = zonaObjeto.AddComponent<ZonaInterior>();
            SerializedObject so = new SerializedObject(zona);
            so.FindProperty("lugar").objectReferenceValue = lugarMundo;
            so.FindProperty("puerta").objectReferenceValue = puertaLocal;
            SerializedProperty propLuces = so.FindProperty("luces");
            propLuces.arraySize = luces.Length;
            for (int i = 0; i < luces.Length; i++)
            {
                propLuces.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-043 creada: interior del bar con " + luces.Length + " luces.");
        }

        // Posición local dentro del bar: x, altura y, metros desde el muro del fondo del interior hacia la puerta.
        private static Vector3 Pos(float x, float y, float desdeElFondo, float zFondoInterior)
        {
            return new Vector3(x, y, zFondoInterior + desdeElFondo);
        }

        private static Transform Grupo(string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(interior, false);
            return go.transform;
        }

        private static GameObject Caja(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool conCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static Light Luz(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            luz.shadows = LightShadows.None;
            return luz;
        }

        // Instancia un prefab del kit dentro del interior; el frente del prop mira a +z y 'giro' lo rota en y.
        private static GameObject Prop(Transform padre, string ruta, Vector3 posicion, float giro)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("No existe el prefab " + ruta + "; corre CreadorArte antes.");
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.localPosition = posicion;
            go.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
            return go;
        }

        // Lámpara que cuelga del techo (cable y pantalla emisiva) con su luz cálida debajo.
        private static Light Colgante(Transform padre, Material material, string nombre, Vector3 techo, Color color)
        {
            Caja(padre, nombre + "_Cable", techo - Vector3.up * (AlturaColgante / 2f), new Vector3(0.03f, AlturaColgante, 0.03f), material, false);
            GameObject pantalla = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pantalla.name = nombre + "_Pantalla";
            pantalla.transform.SetParent(padre, false);
            pantalla.transform.localPosition = techo - Vector3.up * AlturaColgante;
            pantalla.transform.localScale = new Vector3(0.6f, 0.11f, 0.6f);
            pantalla.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(pantalla.GetComponent<Collider>());
            return Luz(padre, nombre, techo - Vector3.up * (AlturaColgante + 0.3f), color, 8f, 10f);
        }

        private static Transform BuscarPorNombre(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            throw new System.InvalidOperationException("No existe " + nombre + " en el barrio; corre CreadorBarrio y CreadorLugares antes.");
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Bar_" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
