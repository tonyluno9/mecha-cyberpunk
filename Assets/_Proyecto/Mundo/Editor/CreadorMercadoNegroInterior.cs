using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Medidas de la clínica de T-321, en metros desde el muro del fondo del cuarto (z hacia la puerta) y x local del mercado.
    /// Vive en el rincón del fondo a la izquierda: tabique con un vano hacia el pasillo, camilla contra el muro y, detrás del vano,
    /// el punto libre Clinica_Mostrador para el prefab de Tony. Las pruebas de EditMode comprueban que no choca con los puestos.
    /// </summary>
    public static class GeometriaClinica
    {
        public const float XMin = -5f;
        public const float XMax = -1.4f;
        public const float Profundidad = 1.6f;          // del muro del fondo al tabique
        public const float GrosorTabique = 0.1f;
        public const float VanoXMin = -2.8f;
        public const float VanoXMax = -1.8f;
        public const float AlturaVano = 2.2f;
        public const float CamillaXMin = -4.8f;
        public const float CamillaXMax = -3f;
        public const float CamillaZMin = 0.2f;
        public const float CamillaZMax = 0.9f;
        public const float MostradorX = -2.3f;
        public const float MostradorZ = 0.8f;
        public const float EntradaX = -2.3f;
        public const float EntradaZ = 2.3f;             // afuera del vano, en el pasillo
        public const float HoraApertura = 9f;
        public const float HoraCierre = 18f;
        public const string NombreMostrador = "Clinica_Mostrador";
        public const string RutaDatos = "Assets/_Proyecto/Mundo/Datos/DatosLugar_Clinica.asset";
    }

    /// <summary>
    /// Crea el interior del mercado negro de T-320 dentro del prefab del barrio, igual que CreadorCasinoInterior con el casino: un cuarto de
    /// 10 × 10 m con muros gruesos, cuatro puestos de lámina con toldo, estantes, cajas apiladas, luces de sodio y la <see cref="ZonaInterior"/>.
    /// El mostrador de Tony (UI/Prefabs/MostradorMercadoNegro.prefab) se instancia por ruta, sin editarlo, justo detrás de la puerta y a menos
    /// de 3 m de la Entrada; el cuarto abre de día y cierra con el horario del lugar (9:00 a 18:00).
    /// Corre después de CreadorLugares y antes de VestirCerrados; se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorMercadoNegroInterior.Crear
    /// </summary>
    public static class CreadorMercadoNegroInterior
    {
        public const string RutaMostrador = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaPropLetrero = "Assets/_Proyecto/Mundo/Prefabs/Kit/Prop_Letrero.prefab";

        // Medidas (m) de CreadorBarrio.Lugar: 12 × 12 con fachada de 1 m; el cuarto ocupa los 10 × 10 de adentro.
        private const float AnchoInterior = 10f;
        private const float FondoInterior = 10f;
        private const float AltoInterior = 4.5f;
        private const float GrosorMuro = 1f;
        private const float GrosorRevestimiento = 0.1f;
        private const float DistanciaEntrada = 1.5f;
        private const float GrosorFachada = 1f;

        // Mostrador de Tony: detrás de la puerta, a un lado del paso (el paso libre mide 1.4 m), de cara a la calle (giro 180°).
        private static readonly Vector3 PosicionMostrador = new Vector3(-1.1f, 0.04f, 4.8f);
        private const float GiroMostrador = 180f;

        // Puestos a lo largo de los muros laterales: (lado, z).
        private static readonly float[] ZPuestos = { 2f, -2f };
        private const float XPuesto = 3.7f;
        private static readonly Vector3 TamanioMostradorPuesto = new Vector3(1.1f, 1f, 2.4f);
        private static readonly Vector3 TamanioEstante = new Vector3(0.45f, 2.3f, 2.6f);
        private const float AlturaToldo = 2.7f;
        private const float AlturaColgante = 0.9f;
        private const float IntensidadSodio = 9f;
        private const float AlcanceSodio = 9f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-320")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.2f, 0.17f, 0.15f), 0.1f, default);
            Material piso = Mat("Piso", new Color(0.13f, 0.12f, 0.12f), 0.35f, default);
            Material techo = Mat("Techo", new Color(0.1f, 0.09f, 0.09f), 0f, default);
            Material lamina = Mat("Lamina", new Color(0.4f, 0.26f, 0.15f), 0.35f, default);
            Material madera = Mat("Madera", new Color(0.32f, 0.2f, 0.1f), 0.15f, default);
            Material carton = Mat("Carton", new Color(0.55f, 0.4f, 0.22f), 0.05f, default);
            Material metal = Mat("Metal", new Color(0.2f, 0.21f, 0.23f), 0.5f, default);
            Material toldoNaranja = Mat("ToldoNaranja", new Color(0.8f, 0.35f, 0.08f), 0.1f, new Color(1.2f, 0.5f, 0.1f));
            Material toldoCian = Mat("ToldoCian", new Color(0.1f, 0.6f, 0.65f), 0.1f, new Color(0.15f, 1.3f, 1.5f));
            Material neonCian = Mat("NeonCian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));
            Material neonMagenta = Mat("NeonMagenta", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 2f));
            Material sodio = Mat("Sodio", new Color(0.95f, 0.6f, 0.2f), 0.3f, new Color(2.6f, 1.4f, 0.35f));
            Material mercancia = Mat("Mercancia", new Color(0.5f, 0.15f, 0.12f), 0.3f, default);

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

            float zFrenteInterior = entrada.localPosition.z - DistanciaEntrada - GrosorFachada;   // cara de adentro de la fachada
            float zFondoInterior = zFrenteInterior - FondoInterior;
            float altoCuerpo = mercado.Find("FachadaIzquierda").localScale.y;
            float mitadInterior = AnchoInterior / 2f;

            // Lo que dejó CreadorLugares detrás de la puerta (cuerpo ahuecado) se reemplaza por muros alrededor del cuarto.
            foreach (string nombre in new[] { "CuerpoFondo", "CuerpoLadoIzquierdo", "CuerpoLadoDerecho", "CuerpoTecho" })
            {
                Transform pieza = mercado.Find(nombre);
                if (pieza != null)
                {
                    Object.DestroyImmediate(pieza.gameObject);
                }
            }
            Material muroExterior = mercado.Find("FachadaIzquierda").GetComponent<MeshRenderer>().sharedMaterial;

            interior = new GameObject("Interior").transform;
            interior.SetParent(mercado, false);
            float zCentro = (zFrenteInterior + zFondoInterior) / 2f;
            Transform estructura = Grupo("Estructura");
            Caja(estructura, "MuroFondo", new Vector3(0f, altoCuerpo / 2f, zFondoInterior - GrosorMuro / 2f), new Vector3(AnchoInterior + 2f * GrosorMuro, altoCuerpo, GrosorMuro), muroExterior, true);
            foreach (float signo in new[] { -1f, 1f })
            {
                Caja(estructura, signo < 0f ? "MuroIzquierdo" : "MuroDerecho", new Vector3(signo * (mitadInterior + GrosorMuro / 2f), altoCuerpo / 2f, zCentro), new Vector3(GrosorMuro, altoCuerpo, FondoInterior), muroExterior, true);
            }
            float altoSobreTecho = altoCuerpo - AltoInterior;
            Caja(estructura, "TechoInterior", new Vector3(0f, AltoInterior + altoSobreTecho / 2f, zCentro), new Vector3(AnchoInterior, altoSobreTecho, FondoInterior), muroExterior, true);

            // Revestimiento sin collider.
            Transform revestimiento = Grupo("Revestimiento");
            Caja(revestimiento, "Piso", Pos(0f, 0.02f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, 0.04f, FondoInterior), piso, false);
            Caja(revestimiento, "Techo", Pos(0f, AltoInterior - GrosorRevestimiento / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, GrosorRevestimiento, FondoInterior), techo, false);
            Caja(revestimiento, "MuroAtras", Pos(0f, AltoInterior / 2f, GrosorRevestimiento / 2f, zFondoInterior), new Vector3(AnchoInterior, AltoInterior, GrosorRevestimiento), muro, false);
            foreach (float signo in new[] { -1f, 1f })
            {
                Caja(revestimiento, signo < 0f ? "MuroIzq" : "MuroDer", Pos(signo * (mitadInterior - GrosorRevestimiento / 2f), AltoInterior / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(GrosorRevestimiento, AltoInterior, FondoInterior), muro, false);
            }
            // Pasillo central de láminas más claras, de la puerta al fondo.
            Caja(revestimiento, "Pasillo", Pos(0f, 0.045f, FondoInterior / 2f, zFondoInterior), new Vector3(2.4f, 0.02f, FondoInterior), lamina, false);

            Transform mobiliario = Grupo("Mobiliario");
            Transform neon = Grupo("Neon");
            Transform grupoLuces = Grupo("Luces");
            System.Collections.Generic.List<Light> luces = new System.Collections.Generic.List<Light>();
            Color calida = new Color(1f, 0.62f, 0.25f);

            // Cuatro puestos pegados a los muros laterales, de cara al pasillo: mostrador de madera, estante, toldo, mercancía y lámpara.
            int numero = 0;
            foreach (float lado in new[] { -1f, 1f })
            {
                foreach (float z in ZPuestos)
                {
                    numero++;
                    Material toldo = numero % 2 == 0 ? toldoCian : toldoNaranja;
                    float zLocal = zFondoInterior + FondoInterior / 2f + z;
                    Vector3 centro = new Vector3(lado * XPuesto, 0f, zLocal);
                    Caja(mobiliario, "Puesto" + numero + "_Mostrador", centro + new Vector3(0f, TamanioMostradorPuesto.y / 2f, 0f), TamanioMostradorPuesto, madera, true);
                    Caja(mobiliario, "Puesto" + numero + "_Estante", new Vector3(lado * (mitadInterior - TamanioEstante.x / 2f - GrosorRevestimiento), TamanioEstante.y / 2f, zLocal), TamanioEstante, metal, true);
                    GameObject cubierta = Caja(mobiliario, "Puesto" + numero + "_Toldo", centro + new Vector3(lado * 0.35f, AlturaToldo, 0f), new Vector3(1.9f, 0.07f, 2.8f), toldo, false);
                    cubierta.transform.localRotation = Quaternion.Euler(0f, 0f, lado * 8f);
                    for (int i = 0; i < 3; i++)
                    {
                        Caja(mobiliario, "Puesto" + numero + "_Mercancia" + i, centro + new Vector3(-lado * 0.1f, TamanioMostradorPuesto.y + 0.15f, -0.8f + i * 0.8f), new Vector3(0.5f, 0.3f, 0.5f), i % 2 == 0 ? mercancia : carton, false);
                        Caja(mobiliario, "Puesto" + numero + "_Estante_Caja" + i, new Vector3(lado * (mitadInterior - 0.7f), 0.5f + i * 0.7f, zLocal - 0.8f + i * 0.8f), new Vector3(0.3f, 0.3f, 0.3f), carton, false);
                    }
                    luces.Add(Colgante(grupoLuces, sodio, "LuzSodioPuesto" + numero, new Vector3(lado * (XPuesto - 0.3f), AltoInterior, zLocal), calida));
                }
            }

            // Cajas apiladas en el fondo, sólidas, con un letrero de neón sobre ellas.
            float[] xCajas = { -2.4f, 0f, 2.4f };
            for (int i = 1; i < xCajas.Length; i++)   // T-321: la pila izquierda cede su lugar a la clínica
            {
                Caja(mobiliario, "CajasFondo" + i + "_Abajo", Pos(xCajas[i], 0.45f, 0.7f, zFondoInterior), new Vector3(1.2f, 0.9f, 1.1f), carton, true);
                Caja(mobiliario, "CajasFondo" + i + "_Arriba", Pos(xCajas[i] + (i - 1) * 0.1f, 1.2f, 0.7f, zFondoInterior), new Vector3(0.9f, 0.6f, 0.9f), i % 2 == 0 ? madera : carton, false);
            }
            GameObject letrero = PrefabDeRuta(RutaPropLetrero, mobiliario, Pos(0f, 3.1f, 0.2f, zFondoInterior), 0f);
            letrero.name = "LetreroFondo";

            // El mostrador de Tony, de solo lectura: se instancia el prefab sin tocarlo.
            GameObject mostrador = PrefabDeRuta(RutaMostrador, mobiliario, new Vector3(PosicionMostrador.x, PosicionMostrador.y, PosicionMostrador.z), GiroMostrador);
            mostrador.name = "MostradorMercadoNegro";

            // Neón del cuarto: tiras en el muro del fondo y en el techo del pasillo.
            Caja(neon, "FrisoFondo", Pos(0f, 2.6f, 0.12f, zFondoInterior), new Vector3(AnchoInterior - 1f, 0.08f, 0.04f), neonCian, false);
            Caja(neon, "TiraFondoMagenta", Pos(0f, 4.2f, 0.12f, zFondoInterior), new Vector3(6f, 0.08f, 0.04f), neonMagenta, false);
            Caja(neon, "TiraPasillo", Pos(0f, AltoInterior - 0.12f, FondoInterior / 2f, zFondoInterior), new Vector3(0.08f, 0.04f, FondoInterior - 1f), neonCian, false);
            luces.Add(Luz(grupoLuces, "LuzNeonFondo", Pos(0f, 3.4f, 2.2f, zFondoInterior), new Color(0.2f, 0.9f, 0.9f), 6f, 8f));
            luces.Add(Luz(grupoLuces, "LuzEntrada", Pos(0f, 3.8f, FondoInterior - 1.2f, zFondoInterior), calida, 8f, 7f));
            luces.Add(Luz(grupoLuces, "LuzRelleno", Pos(0f, 4f, FondoInterior / 2f, zFondoInterior), new Color(0.85f, 0.8f, 0.7f), 12f, 10f));

            ConstruirClinica(zFondoInterior, grupoLuces, luces);

            // Zona: de la cara de adentro de la fachada al muro del fondo.
            GameObject zonaObjeto = new GameObject("ZonaInterior");
            zonaObjeto.transform.SetParent(interior, false);
            zonaObjeto.transform.localPosition = new Vector3(0f, AltoInterior / 2f, zCentro);
            BoxCollider caja = zonaObjeto.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(AnchoInterior, AltoInterior, FondoInterior);
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
            Debug.Log("[Mundo] T-320 creada: interior del mercado negro con 4 puestos, el mostrador de Tony y " + luces.Count + " luces.");
        }

        // T-321: cuarto de la clínica en el rincón del fondo, con su LugarMundo (IdLugar.Clinica), su vano y el punto libre del mostrador de Tony.
        private static void ConstruirClinica(float zFondoInterior, Transform grupoLuces, System.Collections.Generic.List<Light> luces)
        {
            Material baldosa = Mat("ClinicaBaldosa", new Color(0.72f, 0.78f, 0.78f), 0.6f, default);
            Material tabique = Mat("ClinicaTabique", new Color(0.55f, 0.62f, 0.62f), 0.3f, default);
            Material camilla = Mat("ClinicaCamilla", new Color(0.8f, 0.85f, 0.85f), 0.4f, default);
            Material tela = Mat("ClinicaTela", new Color(0.2f, 0.5f, 0.55f), 0.2f, default);
            Material tubo = Mat("ClinicaTubo", new Color(0.7f, 0.95f, 1f), 0.3f, new Color(2.2f, 3.2f, 3.6f));
            Material cruz = Mat("ClinicaCruz", new Color(0.1f, 0.9f, 0.5f), 0.2f, new Color(0.3f, 3f, 1.5f));

            Transform clinica = Grupo("Clinica");
            float ancho = GeometriaClinica.XMax - GeometriaClinica.XMin;
            float xCentro = (GeometriaClinica.XMin + GeometriaClinica.XMax) / 2f;
            float zTabique = zFondoInterior + GeometriaClinica.Profundidad;
            float xVano = (GeometriaClinica.VanoXMin + GeometriaClinica.VanoXMax) / 2f;

            Caja(clinica, "PisoBaldosa", new Vector3(xCentro, 0.05f, zFondoInterior + GeometriaClinica.Profundidad / 2f), new Vector3(ancho, 0.02f, GeometriaClinica.Profundidad), baldosa, false);

            // Tabique en dos tramos con el vano entre ellos, más un dintel.
            float anchoIzquierdo = GeometriaClinica.VanoXMin - GeometriaClinica.XMin;
            float anchoDerecho = GeometriaClinica.XMax - GeometriaClinica.VanoXMax;
            float alturaDintel = AltoInterior - GeometriaClinica.AlturaVano;
            Caja(clinica, "TabiqueIzquierdo", new Vector3(GeometriaClinica.XMin + anchoIzquierdo / 2f, AltoInterior / 2f, zTabique), new Vector3(anchoIzquierdo, AltoInterior, GeometriaClinica.GrosorTabique), tabique, true);
            Caja(clinica, "TabiqueDerecho", new Vector3(GeometriaClinica.VanoXMax + anchoDerecho / 2f, AltoInterior / 2f, zTabique), new Vector3(anchoDerecho, AltoInterior, GeometriaClinica.GrosorTabique), tabique, true);
            Caja(clinica, "Dintel", new Vector3(xVano, GeometriaClinica.AlturaVano + alturaDintel / 2f, zTabique), new Vector3(GeometriaClinica.VanoXMax - GeometriaClinica.VanoXMin, alturaDintel, GeometriaClinica.GrosorTabique), tabique, true);
            Caja(clinica, "CruzVerde", new Vector3(xVano, GeometriaClinica.AlturaVano + 0.1f, zTabique + 0.07f), new Vector3(0.5f, 0.12f, 0.03f), cruz, false);

            // Camilla contra el muro del fondo: base, colchón y almohada.
            float camillaAncho = GeometriaClinica.CamillaXMax - GeometriaClinica.CamillaXMin;
            float camillaFondo = GeometriaClinica.CamillaZMax - GeometriaClinica.CamillaZMin;
            float camillaX = (GeometriaClinica.CamillaXMin + GeometriaClinica.CamillaXMax) / 2f;
            float camillaZ = zFondoInterior + (GeometriaClinica.CamillaZMin + GeometriaClinica.CamillaZMax) / 2f;
            Caja(clinica, "Camilla_Base", new Vector3(camillaX, 0.3f, camillaZ), new Vector3(camillaAncho, 0.6f, camillaFondo), camilla, true);
            Caja(clinica, "Camilla_Colchon", new Vector3(camillaX, 0.65f, camillaZ), new Vector3(camillaAncho - 0.1f, 0.1f, camillaFondo - 0.1f), tela, false);
            Caja(clinica, "Camilla_Almohada", new Vector3(GeometriaClinica.CamillaXMin + 0.3f, 0.75f, camillaZ), new Vector3(0.4f, 0.1f, 0.4f), camilla, false);
            Caja(clinica, "Lampara_Tubo", new Vector3(xCentro, AltoInterior - 0.2f, zFondoInterior + 0.8f), new Vector3(2.4f, 0.06f, 0.12f), tubo, false);
            luces.Add(Luz(grupoLuces, "LuzFriaClinica", new Vector3(xCentro, AltoInterior - 0.6f, zFondoInterior + 0.8f), new Color(0.7f, 0.95f, 1f), 7f, 4.5f));

            // Punto vacío, sobre el piso, donde Tony coloca su mostrador; mismo giro que el del mercado.
            GameObject punto = new GameObject(GeometriaClinica.NombreMostrador);
            punto.transform.SetParent(clinica, false);
            punto.transform.localPosition = new Vector3(GeometriaClinica.MostradorX, 0.04f, zFondoInterior + GeometriaClinica.MostradorZ);
            punto.transform.localRotation = Quaternion.Euler(0f, GiroMostrador, 0f);

            // El lugar: entrada en el pasillo, frente al vano.
            GameObject lugarObjeto = new GameObject("Lugar_Clinica");
            lugarObjeto.transform.SetParent(clinica, false);
            GameObject entrada = new GameObject("Entrada");
            entrada.transform.SetParent(lugarObjeto.transform, false);
            entrada.transform.localPosition = new Vector3(GeometriaClinica.EntradaX, 0f, zFondoInterior + GeometriaClinica.EntradaZ);
            LugarMundo lugar = lugarObjeto.AddComponent<LugarMundo>();
            SerializedObject so = new SerializedObject(lugar);
            so.FindProperty("datos").objectReferenceValue = CrearDatosClinica();
            so.FindProperty("entrada").objectReferenceValue = entrada.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Crea (sin pisar) DatosLugar_Clinica: IdLugar.Clinica, abierta de día como el mercado. No toca el prefab del barrio.</summary>
        [MenuItem("Mecha Cyberpunk/Crear datos de la clínica (T-321)")]
        public static DatosLugar CrearDatosClinica()
        {
            DatosLugar datos = AssetDatabase.LoadAssetAtPath<DatosLugar>(GeometriaClinica.RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosLugar>();
                datos.Configurar(IdLugar.Clinica, "Clínica", new HorarioLugar(GeometriaClinica.HoraApertura, GeometriaClinica.HoraCierre), false);
                AssetDatabase.CreateAsset(datos, GeometriaClinica.RutaDatos);
                AssetDatabase.SaveAssets();
            }
            return datos;
        }

        // Posición local dentro del mercado: x, altura y, metros desde el muro del fondo del cuarto hacia la puerta.
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

        // Lámpara de sodio que cuelga del techo (cable y pantalla emisiva) con su luz cálida debajo.
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
            return Luz(padre, nombre, techo - Vector3.up * (AlturaColgante + 0.3f), color, IntensidadSodio, AlcanceSodio);
        }

        // Instancia un prefab sin editarlo; 'giro' lo rota en y.
        private static GameObject PrefabDeRuta(string ruta, Transform padre, Vector3 posicion, float giro)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("No existe el prefab " + ruta + ".");
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.localPosition = posicion;
            go.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
            return go;
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
