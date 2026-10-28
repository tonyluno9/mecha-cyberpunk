using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el interior del casino de T-303 dentro del prefab del barrio, igual que CreadorBar con el bar: muros gruesos con collider,
    /// seis Tragamonedas.prefab pegadas al muro del fondo, la Cajera.prefab en el muro derecho con su mostrador, el ServicioFichas,
    /// columnas doradas, mesas de cartas, sillones, alfombra, neón, lámparas cálidas y la <see cref="ZonaInterior"/> (luces y mezcla sonora).
    /// Corre después de CreadorBar y se puede repetir (usa CreadorTodo.Crear). Los prefabs del Casino los crean
    /// CreadorTragamonedas y CreadorFichas (T-301 y T-302); aquí solo se instancian, por ruta, sin referenciar el ensamblado de Casino.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorCasinoInterior.Crear
    /// </summary>
    public static class CreadorCasinoInterior
    {
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaKit = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const string RutaPropMesa = RutaKit + "Prop_Mesa.prefab";
        private const string RutaPropLetrero = RutaKit + "Prop_Letrero.prefab";
        private const string RutaTragamonedas = "Assets/_Proyecto/Casino/Prefabs/Tragamonedas.prefab";
        private const string RutaCajera = "Assets/_Proyecto/Casino/Prefabs/Cajera.prefab";
        private const string RutaServicio = "Assets/_Proyecto/Casino/Prefabs/ServicioFichas.prefab";

        // Medidas del casino (m), las mismas de CreadorBarrio y CreadorLugares.
        private const float AnchoPuerta = 3f;
        private const float GrosorFachada = 1f;
        private const float ProfundidadVestibulo = 3f;
        private const float DistanciaEntrada = 1.5f;
        private const float AnchoInterior = 28f;
        private const float FondoInterior = 22f;
        private const float AltoInterior = 5.5f;
        private const float GrosorRevestimiento = 0.1f;

        // Distribución (x lateral, d = metros desde el muro del fondo hacia la puerta).
        private static readonly float[] XTragamonedas = { -11f, -7f, -3f, 3f, 7f, 11f };
        private const float DistanciaTragamonedas = 0.55f;     // el cuerpo mide 0.8 m de fondo
        private static readonly Vector3 TamanioColisionMaquina = new Vector3(1f, 1.7f, 0.8f);
        private const float XCajera = 11.5f;
        private const float DistanciaCajera = 11f;
        private static readonly Vector3 TamanioColisionMostrador = new Vector3(2.1f, 1.1f, 0.7f);
        private const float AlturaMostrador = 0.55f;
        private const float RadioColumna = 0.4f;
        private static readonly Vector2[] Columnas = { new Vector2(-6.5f, 9f), new Vector2(6.5f, 9f), new Vector2(-6.5f, 16f), new Vector2(6.5f, 16f) };
        private static readonly Vector2[] MesasCartas = { new Vector2(-9.5f, 15f), new Vector2(-9.5f, 19f) };
        private static readonly Vector2[] MesasSalon = { new Vector2(-10.5f, 8f), new Vector2(-7.5f, 10.5f) };
        private static readonly float[] DistanciasSillones = { 16.5f, 20f };
        private const float AnchoAlfombra = 4f;
        private const float AlturaAlfombra = 0.055f;
        private const float IntensidadNeonMaquinas = 5f;
        private const float IntensidadRelleno = 14f;
        private const float AlturaColgante = 0.9f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-303")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.28f, 0.1f, 0.22f), 0.15f, default);
            Material piso = Mat("Piso", new Color(0.12f, 0.1f, 0.14f), 0.55f, default);
            Material techo = Mat("Techo", new Color(0.16f, 0.1f, 0.2f), 0f, default);
            Material alfombra = Mat("Alfombra", new Color(0.45f, 0.05f, 0.1f), 0.1f, default);
            Material dorado = Mat("Dorado", new Color(0.75f, 0.55f, 0.15f), 0.65f, default);
            Material felpa = Mat("Felpa", new Color(0.05f, 0.35f, 0.18f), 0.1f, default);
            Material cuero = Mat("Cuero", new Color(0.4f, 0.06f, 0.16f), 0.3f, default);
            Material metal = Mat("Metal", new Color(0.18f, 0.19f, 0.22f), 0.5f, default);
            Material neonMagenta = Mat("NeonMagenta", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 2f));
            Material neonDorado = Mat("NeonDorado", new Color(0.95f, 0.7f, 0.15f), 0.2f, new Color(3.4f, 2.2f, 0.3f));
            Material neonCian = Mat("NeonCian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));
            Material lampara = Mat("Lampara", new Color(0.95f, 0.65f, 0.3f), 0.3f, new Color(2.4f, 1.4f, 0.5f));
            Material pantalla = Mat("Pantalla", new Color(0.35f, 0.05f, 0.3f), 0.4f, new Color(1f, 0.2f, 0.8f));

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform casino = BuscarPorNombre(raiz.transform, "Lugar_Casino");
            Transform viejo = casino.Find("Interior");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            LugarMundo lugarMundo = casino.GetComponent<LugarMundo>();
            PuertaLocal puertaLocal = casino.Find("Puerta").GetComponent<PuertaLocal>();
            Transform entrada = casino.Find("Entrada");

            float zFrente = entrada.localPosition.z - DistanciaEntrada;
            float zFrenteInterior = zFrente - GrosorFachada - ProfundidadVestibulo;
            float zFondoInterior = zFrenteInterior - FondoInterior;
            Material muroExterior = casino.Find("CuerpoLadoIzquierdo").GetComponent<MeshRenderer>().sharedMaterial;
            float altoCuerpo = casino.Find("CuerpoLadoIzquierdo").localScale.y;
            float anchoCuerpo = AnchoPuerta + 2f * casino.Find("CuerpoLadoIzquierdo").localScale.x;

            interior = new GameObject("Interior").transform;
            interior.SetParent(casino, false);

            // Quita el macizo detrás del vestíbulo y lo reemplaza por muros alrededor del interior.
            Transform fondoViejo = casino.Find("CuerpoFondo");
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
            // Alfombra roja de la puerta al fondo, con filetes dorados, y zócalo dorado en el muro del fondo.
            float largoAlfombra = FondoInterior - 1.8f;
            float centroAlfombra = 1.8f + largoAlfombra / 2f;
            Caja(revestimiento, "Alfombra", Pos(0f, AlturaAlfombra, centroAlfombra, zFondoInterior), new Vector3(AnchoAlfombra, 0.03f, largoAlfombra), alfombra, false);
            foreach (float signo in new[] { -1f, 1f })
            {
                Caja(revestimiento, "FileteAlfombra", Pos(signo * (AnchoAlfombra / 2f + 0.1f), AlturaAlfombra, centroAlfombra, zFondoInterior), new Vector3(0.12f, 0.032f, largoAlfombra), dorado, false);
            }
            Caja(revestimiento, "ZocaloFondo", Pos(0f, 0.15f, 0.15f, zFondoInterior), new Vector3(AnchoInterior - 0.4f, 0.3f, 0.1f), dorado, false);

            Transform mobiliario = Grupo("Mobiliario");
            Transform servicios = Grupo("Servicios");

            // Seis tragamonedas pegadas al muro del fondo, de cara al salón, con un hueco central para el letrero.
            Transform maquinas = new GameObject("Tragamonedas").transform;
            maquinas.SetParent(mobiliario, false);
            System.Collections.Generic.List<Light> lucesMaquinas = new System.Collections.Generic.List<Light>();
            for (int i = 0; i < XTragamonedas.Length; i++)
            {
                GameObject maquina = Prop(maquinas, RutaTragamonedas, Pos(XTragamonedas[i], 0.04f, DistanciaTragamonedas, zFondoInterior), 0f);
                maquina.name = "Tragamonedas_" + (i + 1);
                ColisionHija(maquina.transform, new Vector3(0f, TamanioColisionMaquina.y / 2f, 0f), TamanioColisionMaquina);
                lucesMaquinas.AddRange(maquina.GetComponentsInChildren<Light>(true));
            }

            // Servicio de fichas (guardable) y cajera en el muro derecho, mirando al salón (hacia -x), con su mostrador sólido.
            Prop(servicios, RutaServicio, Vector3.zero, 0f);
            GameObject cajera = Prop(mobiliario, RutaCajera, Pos(XCajera, 0.04f, DistanciaCajera, zFondoInterior), -90f);
            ColisionHija(cajera.transform, new Vector3(0f, AlturaMostrador, 0f), TamanioColisionMostrador);

            // Columnas doradas (sólidas), mesas de cartas con fieltro, mesas de salón y sillones en el muro derecho.
            foreach (Vector2 c in Columnas)
            {
                GameObject columna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                columna.name = "Columna";
                columna.transform.SetParent(mobiliario, false);
                columna.transform.localPosition = Pos(c.x, AltoInterior / 2f, c.y, zFondoInterior);
                columna.transform.localScale = new Vector3(RadioColumna * 2f, AltoInterior / 2f, RadioColumna * 2f);
                columna.GetComponent<MeshRenderer>().sharedMaterial = dorado;
            }
            foreach (Vector2 m in MesasCartas)
            {
                Caja(mobiliario, "MesaCartas", Pos(m.x, 0.45f, m.y, zFondoInterior), new Vector3(2.4f, 0.9f, 1.3f), cuero, true);
                Caja(mobiliario, "FieltroCartas", Pos(m.x, 0.92f, m.y, zFondoInterior), new Vector3(2.2f, 0.04f, 1.1f), felpa, false);
                Caja(mobiliario, "BordeCartas", Pos(m.x, 0.9f, m.y, zFondoInterior), new Vector3(2.5f, 0.03f, 1.4f), dorado, false);
            }
            foreach (Vector2 m in MesasSalon)
            {
                Prop(mobiliario, RutaPropMesa, Pos(m.x, 0.04f, m.y, zFondoInterior), 0f);
            }
            foreach (float d in DistanciasSillones)
            {
                Caja(mobiliario, "SillonAsiento", Pos(mitadInterior - 0.8f, 0.3f, d, zFondoInterior), new Vector3(1.1f, 0.6f, 3.2f), cuero, true);
                Caja(mobiliario, "SillonRespaldo", Pos(mitadInterior - 0.4f, 0.85f, d, zFondoInterior), new Vector3(0.3f, 0.9f, 3.2f), cuero, false);
            }

            // Letrero del fondo, en el hueco entre las dos filas de máquinas.
            Prop(mobiliario, RutaPropLetrero, Pos(0f, 2.9f, 0.2f, zFondoInterior), 0f);

            // Neón: marco dorado sobre las máquinas, tiras de techo, friso en los muros y pantallas laterales.
            Transform neon = Grupo("Neon");
            Caja(neon, "FrisoFondo", Pos(0f, 3.2f, 0.12f, zFondoInterior), new Vector3(AnchoInterior - 1f, 0.1f, 0.05f), neonDorado, false);
            Caja(neon, "TiraAtras", Pos(0f, 4.9f, 0.12f, zFondoInterior), new Vector3(24f, 0.08f, 0.04f), neonMagenta, false);
            Caja(neon, "FrisoIzquierdo", Pos(-mitadInterior + 0.12f, 3.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.1f, FondoInterior - 1f), neonDorado, false);
            Caja(neon, "FrisoDerecho", Pos(mitadInterior - 0.12f, 3.2f, FondoInterior / 2f, zFondoInterior), new Vector3(0.04f, 0.1f, FondoInterior - 1f), neonDorado, false);
            Caja(neon, "PantallaIzquierda1", Pos(-mitadInterior + 0.2f, 2.3f, 6f, zFondoInterior), new Vector3(0.06f, 1.5f, 3.5f), pantalla, false);
            Caja(neon, "PantallaIzquierda2", Pos(-mitadInterior + 0.2f, 2.3f, 12f, zFondoInterior), new Vector3(0.06f, 1.5f, 3.5f), pantalla, false);
            Caja(neon, "PantallaDerecha", Pos(mitadInterior - 0.2f, 2.3f, 4f, zFondoInterior), new Vector3(0.06f, 1.5f, 3.5f), pantalla, false);
            for (int i = 0; i < 4; i++)
            {
                Caja(neon, "TiraTecho", Pos(0f, AltoInterior - 0.12f, 3f + i * 5.5f, zFondoInterior), new Vector3(20f, 0.04f, 0.08f), i % 2 == 0 ? neonCian : neonMagenta, false);
            }
            Caja(neon, "TiraCentro", Pos(0f, AltoInterior - 0.15f, 11f, zFondoInterior), new Vector3(0.08f, 0.04f, 18f), neonDorado, false);
            Caja(neon, "LetreroCajera", Pos(mitadInterior - 0.12f, 3.0f, DistanciaCajera, zFondoInterior), new Vector3(0.05f, 0.35f, 2.2f), neonDorado, false);

            // Lámparas colgantes cálidas (cada una con su luz), luces de neón sobre las máquinas y la cajera, y relleno.
            Transform grupoLuces = Grupo("Luces");
            Color calida = new Color(1f, 0.7f, 0.4f);
            System.Collections.Generic.List<Light> luces = new System.Collections.Generic.List<Light>
            {
                Colgante(grupoLuces, lampara, "LuzCalidaSalonIzq", Pos(-7f, AltoInterior, 4f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaSalonDer", Pos(7f, AltoInterior, 4f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaCentro1", Pos(0f, AltoInterior, 8f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaCentro2", Pos(0f, AltoInterior, 15.5f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaMesas", Pos(-9.5f, AltoInterior, 17f, zFondoInterior), calida),
                Colgante(grupoLuces, lampara, "LuzCalidaCajera", Pos(10.5f, AltoInterior, DistanciaCajera, zFondoInterior), calida),
                Luz(grupoLuces, "LuzNeonMaquinasIzq", Pos(-7f, 3.6f, 2.2f, zFondoInterior), new Color(0.9f, 0.1f, 0.7f), IntensidadNeonMaquinas, 9f),
                Luz(grupoLuces, "LuzNeonMaquinasDer", Pos(7f, 3.6f, 2.2f, zFondoInterior), new Color(0.9f, 0.1f, 0.7f), IntensidadNeonMaquinas, 9f),
                Luz(grupoLuces, "LuzNeonDorada", Pos(0f, 4.2f, 12f, zFondoInterior), new Color(1f, 0.75f, 0.25f), 5f, 10f),
                Luz(grupoLuces, "LuzEntrada", Pos(0f, 4.2f, 20.5f, zFondoInterior), calida, 4f, 7f),
                Luz(grupoLuces, "LuzRellenoIzq", Pos(-7f, 4.5f, 12f, zFondoInterior), new Color(0.75f, 0.7f, 1f), IntensidadRelleno, 14f),
                Luz(grupoLuces, "LuzRellenoDer", Pos(7f, 4.5f, 12f, zFondoInterior), new Color(0.75f, 0.7f, 1f), IntensidadRelleno, 14f)
            };
            luces.AddRange(lucesMaquinas);

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
            propLuces.arraySize = luces.Count;
            for (int i = 0; i < luces.Count; i++)
            {
                propLuces.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-303 creada: interior del casino con " + XTragamonedas.Length + " tragamonedas, cajera y " + luces.Count + " luces.");
        }

        // Posición local dentro del casino: x, altura y, metros desde el muro del fondo del interior hacia la puerta.
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

        // Collider sólido en un hijo de la instancia: los prefabs del Casino no traen collider y no se editan desde aquí.
        private static void ColisionHija(Transform padre, Vector3 centro, Vector3 tamano)
        {
            GameObject go = new GameObject("Colision");
            go.transform.SetParent(padre, false);
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.center = centro;
            caja.size = tamano;
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
                throw new System.InvalidOperationException("No existe el prefab " + ruta + "; corre CreadorArte, CreadorTragamonedas y CreadorFichas antes.");
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
            pantalla.transform.localScale = new Vector3(0.7f, 0.11f, 0.7f);
            pantalla.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(pantalla.GetComponent<Collider>());
            return Luz(padre, nombre, techo - Vector3.up * (AlturaColgante + 0.3f), color, 8f, 11f);
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
            string ruta = RutaMateriales + "/CasinoInt_" + nombre + ".mat";
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
