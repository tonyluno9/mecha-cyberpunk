using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el interior del mercado negro de T-320 dentro del prefab del barrio, igual que CreadorCasinoInterior con el casino: un cuarto de
    /// puestos con muros gruesos con collider, mesas del kit con toldo, estantes, cajas, mallas de alambre, luces de sodio y la
    /// <see cref="ZonaInterior"/> (luces y mezcla sonora). El mostrador de Tony (UI/Prefabs/MostradorMercadoNegro.prefab) se instancia por
    /// ruta, sin referenciar el ensamblado de UI, y NO se edita: queda frente a la entrada, de cara a la puerta.
    /// A diferencia del casino, el cuarto arranca justo detrás de la fachada (sin el vestíbulo de 3 m de CreadorLugares), para que el mostrador
    /// quede cerca de la puerta sin tapar el paso. Abre de 09:00 a 18:00 (DatosLugar_MercadoNegro) y cierra con la noche.
    /// Corre después de CreadorCasinoInterior y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorMercadoNegroInterior.Crear
    /// </summary>
    public static class CreadorMercadoNegroInterior
    {
        public const string RutaMostrador = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";

        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaKit = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const string RutaPropMesa = RutaKit + "Prop_Mesa.prefab";

        // Medidas del mercado (m): el edificio mide 12 × 12 (CreadorBarrio), con fachada de 1 m.
        private const float AnchoPuerta = 3f;
        private const float GrosorFachada = 1f;
        private const float DistanciaEntrada = 1.5f;
        private const float AnchoInterior = 10f;
        private const float FondoInterior = 10.5f;
        private const float AltoInterior = 4.2f;
        private const float GrosorRevestimiento = 0.1f;

        // Distribución (x lateral, d = metros desde el muro del fondo hacia la puerta).
        private const float DistanciaMostrador = 9.1f;          // origen del mostrador: 1.4 m dentro de la puerta
        private static readonly Vector2[] PuestosIzquierda = { new Vector2(-3.6f, 3.2f), new Vector2(-3.6f, 6.4f) };
        private static readonly Vector2[] PuestosDerecha = { new Vector2(3.6f, 3.2f), new Vector2(3.6f, 6.4f) };
        private static readonly Vector2[] CajasSueltas = { new Vector2(-4.2f, 9.4f), new Vector2(4.2f, 9.6f), new Vector2(4.3f, 1.1f) };
        private static readonly float[] XEstantes = { -3.2f, 0f, 3.2f };
        private const float IntensidadSodio = 16f;
        private const float AlturaColgante = 0.8f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-320")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.22f, 0.17f, 0.12f), 0.1f, default);
            Material piso = Mat("Piso", new Color(0.14f, 0.12f, 0.1f), 0.35f, default);
            Material techo = Mat("Techo", new Color(0.12f, 0.1f, 0.09f), 0f, default);
            Material madera = Mat("Madera", new Color(0.38f, 0.24f, 0.12f), 0.1f, default);
            Material metal = Mat("Metal", new Color(0.2f, 0.21f, 0.23f), 0.5f, default);
            Material lona = Mat("Lona", new Color(0.55f, 0.2f, 0.12f), 0.1f, default);
            Material lonaAzul = Mat("LonaAzul", new Color(0.12f, 0.3f, 0.4f), 0.1f, default);
            Material caja = Mat("Caja", new Color(0.45f, 0.33f, 0.18f), 0.1f, default);
            Material lampara = Mat("Lampara", new Color(1f, 0.6f, 0.2f), 0.3f, new Color(2.8f, 1.4f, 0.3f));
            Material neonTurquesa = Mat("NeonTurquesa", new Color(0.1f, 0.9f, 0.8f), 0.2f, new Color(0.3f, 3f, 2.6f));
            Material neonNaranja = Mat("NeonNaranja", new Color(1f, 0.5f, 0.1f), 0.2f, new Color(3.4f, 1.5f, 0.2f));

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform mercado = BuscarPorNombre(raiz.transform, "Lugar_MercadoNegro");
            Transform viejo = mercado.Find("Interior");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            LugarMundo lugarMundo = mercado.GetComponent<LugarMundo>();
            PuertaLocal puertaLocal = mercado.Find("Puerta").GetComponent<PuertaLocal>();
            Transform entrada = mercado.Find("Entrada");

            float zFrente = entrada.localPosition.z - DistanciaEntrada;
            float zFrenteInterior = zFrente - GrosorFachada;
            float zFondoInterior = zFrenteInterior - FondoInterior;
            // La fachada no se borra, así que las medidas salen de ella y el creador se puede repetir solo.
            Transform ladoIzquierdo = mercado.Find("FachadaIzquierda");
            Material muroExterior = ladoIzquierdo.GetComponent<MeshRenderer>().sharedMaterial;
            float altoCuerpo = ladoIzquierdo.localScale.y;
            float anchoCuerpo = AnchoPuerta + 2f * ladoIzquierdo.localScale.x;

            interior = new GameObject("Interior").transform;
            interior.SetParent(mercado, false);

            // Quita el vestíbulo y el macizo de CreadorLugares: la fachada (con la puerta) queda como único frente.
            foreach (string pieza in new[] { "CuerpoFondo", "CuerpoLadoIzquierdo", "CuerpoLadoDerecho", "CuerpoTecho" })
            {
                Transform vieja = mercado.Find(pieza);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
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
            // Frente interior: a los lados de la puerta, pegado a la cara de adentro de la fachada.
            float anchoFrente = (AnchoInterior - AnchoPuerta) / 2f;
            Caja(revestimiento, "MuroFrenteIzq", Pos(-(AnchoPuerta / 2f + anchoFrente / 2f), AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);
            Caja(revestimiento, "MuroFrenteDer", Pos(AnchoPuerta / 2f + anchoFrente / 2f, AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);
            // Tablones del pasillo central y rodapié.
            Caja(revestimiento, "PasilloCentral", Pos(0f, 0.045f, FondoInterior / 2f, zFondoInterior), new Vector3(2.4f, 0.02f, FondoInterior - 0.4f), madera, false);
            Caja(revestimiento, "RodapieFondo", Pos(0f, 0.15f, 0.15f, zFondoInterior), new Vector3(AnchoInterior - 0.4f, 0.3f, 0.1f), metal, false);

            Transform mobiliario = Grupo("Mobiliario");
            Transform servicios = Grupo("Servicios");

            // El mostrador de Tony: se lee el prefab, no se edita. Su letrero se lee desde -z (la cara de un Canvas), así que se gira 180°
            // para que el cliente que entra por la puerta (que mira hacia -z en el barrio) vea el texto al derecho.
            Prop(servicios, RutaMostrador, Pos(0f, 0f, DistanciaMostrador, zFondoInterior), 180f).name = "MostradorMercadoNegro";

            // Puestos a los dos lados: mesa del kit, toldo de lona sostenido por dos postes y cajas encima.
            int numero = 1;
            foreach (Vector2 p in PuestosIzquierda)
            {
                Puesto(mobiliario, "Puesto_" + numero, Pos(p.x, 0f, p.y, zFondoInterior), 90f, numero % 2 == 0 ? lona : lonaAzul, madera, metal, caja);
                numero++;
            }
            foreach (Vector2 p in PuestosDerecha)
            {
                Puesto(mobiliario, "Puesto_" + numero, Pos(p.x, 0f, p.y, zFondoInterior), -90f, numero % 2 == 0 ? lona : lonaAzul, madera, metal, caja);
                numero++;
            }

            // Estantes de metal contra el muro del fondo, con cajas.
            foreach (float x in XEstantes)
            {
                Caja(mobiliario, "Estante", Pos(x, 1.1f, 0.45f, zFondoInterior), new Vector3(2.4f, 2.2f, 0.7f), metal, true);
                Caja(mobiliario, "CajaEstanteBaja", Pos(x - 0.5f, 0.35f, 0.5f, zFondoInterior), new Vector3(0.7f, 0.7f, 0.55f), caja, false);
                Caja(mobiliario, "CajaEstanteAlta", Pos(x + 0.4f, 1.55f, 0.5f, zFondoInterior), new Vector3(0.8f, 0.6f, 0.55f), caja, false);
            }
            // Cajas sueltas junto a los muros y una pila a la derecha de la entrada.
            foreach (Vector2 c in CajasSueltas)
            {
                Caja(mobiliario, "CajaSuelta", Pos(c.x, 0.4f, c.y, zFondoInterior), new Vector3(0.8f, 0.8f, 0.8f), caja, true);
            }
            Caja(mobiliario, "CajaApilada", Pos(4.3f, 1.15f, 1.1f, zFondoInterior), new Vector3(0.7f, 0.7f, 0.7f), caja, false);

            // Neón: rótulo del fondo, tira de techo sobre el pasillo y una franja en cada muro lateral.
            Transform neon = Grupo("Neon");
            Caja(neon, "RotuloFondo", Pos(0f, 3.3f, 0.12f, zFondoInterior), new Vector3(4.5f, 0.45f, 0.05f), neonNaranja, false);
            Caja(neon, "TiraTecho", Pos(0f, AltoInterior - 0.12f, FondoInterior / 2f, zFondoInterior), new Vector3(0.1f, 0.04f, FondoInterior - 1.5f), neonTurquesa, false);
            Caja(neon, "FranjaIzquierda", Pos(-mitadInterior + 0.12f, 3.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.08f, FondoInterior - 1.5f), neonTurquesa, false);
            Caja(neon, "FranjaDerecha", Pos(mitadInterior - 0.12f, 3.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.08f, FondoInterior - 1.5f), neonTurquesa, false);

            // Luces de sodio: lámparas colgantes sobre cada par de puestos y sobre el mostrador, más un relleno suave al fondo.
            Transform grupoLuces = Grupo("Luces");
            Color sodio = new Color(1f, 0.62f, 0.25f);
            System.Collections.Generic.List<Light> luces = new System.Collections.Generic.List<Light>
            {
                Colgante(grupoLuces, lampara, "LuzSodioMostrador", Pos(0f, AltoInterior, DistanciaMostrador - 0.5f, zFondoInterior), sodio),
                Colgante(grupoLuces, lampara, "LuzSodioPuestosIzqAtras", Pos(-3.2f, AltoInterior, 3.2f, zFondoInterior), sodio),
                Colgante(grupoLuces, lampara, "LuzSodioPuestosIzqFrente", Pos(-3.2f, AltoInterior, 6.4f, zFondoInterior), sodio),
                Colgante(grupoLuces, lampara, "LuzSodioPuestosDerAtras", Pos(3.2f, AltoInterior, 3.2f, zFondoInterior), sodio),
                Colgante(grupoLuces, lampara, "LuzSodioPuestosDerFrente", Pos(3.2f, AltoInterior, 6.4f, zFondoInterior), sodio),
                Colgante(grupoLuces, lampara, "LuzSodioPasillo", Pos(0f, AltoInterior, 5f, zFondoInterior), sodio),
                Luz(grupoLuces, "LuzRellenoFondo", Pos(0f, 3.4f, 2f, zFondoInterior), new Color(0.2f, 0.9f, 0.8f), 6f, 8f),
                Luz(grupoLuces, "LuzRellenoPuerta", Pos(0f, 3.6f, 9.5f, zFondoInterior), sodio, IntensidadSodio / 2f, 9f)
            };

            // Zona: del umbral interior (cara de adentro de la puerta) al muro del fondo.
            float profundidadZona = zFrenteInterior - zFondoInterior;
            GameObject zonaObjeto = new GameObject("ZonaInterior");
            zonaObjeto.transform.SetParent(interior, false);
            zonaObjeto.transform.localPosition = new Vector3(0f, AltoInterior / 2f, zFondoInterior + profundidadZona / 2f);
            BoxCollider cajaZona = zonaObjeto.AddComponent<BoxCollider>();
            cajaZona.isTrigger = true;
            cajaZona.size = new Vector3(AnchoInterior, AltoInterior, profundidadZona);
            ZonaInterior zona = zonaObjeto.AddComponent<ZonaInterior>();
            SerializedObject so = new SerializedObject(zona);
            so.FindProperty("lugar").objectReferenceValue = lugarMundo;
            so.FindProperty("puerta").objectReferenceValue = puertaLocal;
            SerializedProperty propLuces = so.FindProperty("luces");
            propLuces.arraySize = luces.Count;
            for (int i = 0; i < luces.Count; i++)
            {
                propLuces.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-320 creada: interior del mercado negro con " + (PuestosIzquierda.Length + PuestosDerecha.Length) + " puestos, el mostrador de Tony y " + luces.Count + " luces.");
        }

        // Posición local dentro del mercado: x, altura y, metros desde el muro del fondo del interior hacia la puerta.
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

        // Mesa del kit con toldo: la mesa (Prop_Mesa) mira al pasillo; 'giro' la rota en y.
        private static void Puesto(Transform padre, string nombre, Vector3 posicion, float giro, Material lona, Material madera, Material metal, Material caja)
        {
            Transform puesto = new GameObject(nombre).transform;
            puesto.SetParent(padre, false);
            puesto.localPosition = posicion;
            puesto.localRotation = Quaternion.Euler(0f, giro, 0f);
            Prop(puesto, RutaPropMesa, Vector3.zero, 0f);
            foreach (float dx in new[] { -1f, 1f })
            {
                foreach (float dz in new[] { -0.6f, 0.6f })
                {
                    Caja(puesto, "Poste", new Vector3(dx, 1.15f, dz), new Vector3(0.06f, 2.3f, 0.06f), metal, false);
                }
            }
            Caja(puesto, "Toldo", new Vector3(0f, 2.33f, 0f), new Vector3(2.4f, 0.05f, 1.6f), lona, false);
            Caja(puesto, "Mercancia", new Vector3(0.3f, 1.0f, 0f), new Vector3(0.6f, 0.3f, 0.5f), caja, false);
            Caja(puesto, "TableroTrasero", new Vector3(0f, 0.35f, -0.65f), new Vector3(2.2f, 0.7f, 0.05f), madera, false);
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

        // Instancia un prefab dentro del interior; el frente del prop mira a +z y 'giro' lo rota en y.
        private static GameObject Prop(Transform padre, string ruta, Vector3 posicion, float giro)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("No existe el prefab " + ruta + "; corre CreadorArte antes y mezcla main (el mostrador es de Tony).");
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.localPosition = posicion;
            go.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
            return go;
        }

        // Lámpara que cuelga del techo (cable y pantalla emisiva) con su luz de sodio debajo.
        private static Light Colgante(Transform padre, Material material, string nombre, Vector3 techo, Color color)
        {
            Caja(padre, nombre + "_Cable", techo - Vector3.up * (AlturaColgante / 2f), new Vector3(0.03f, AlturaColgante, 0.03f), material, false);
            GameObject pantalla = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pantalla.name = nombre + "_Pantalla";
            pantalla.transform.SetParent(padre, false);
            pantalla.transform.localPosition = techo - Vector3.up * AlturaColgante;
            pantalla.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
            pantalla.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(pantalla.GetComponent<Collider>());
            return Luz(padre, nombre, techo - Vector3.up * (AlturaColgante + 0.3f), color, 9f, 9f);
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
            string ruta = RutaMateriales + "/MercadoInt_" + nombre + ".mat";
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
