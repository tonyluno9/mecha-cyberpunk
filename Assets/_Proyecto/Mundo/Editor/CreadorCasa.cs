using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el interior de la casa de T-086 dentro del prefab del barrio: muros con collider, sala con cama y cocineta, y el taller
    /// del mecha (Estacionamiento libre de 2 m de radio y 5 m de alto, banco de trabajo, estantes), con su ZonaInterior.
    /// Corre después de CreadorBar y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorCasa.Crear
    /// </summary>
    public static class CreadorCasa
    {
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        // Medidas de la casa (m): 14 × 12 × 7 en CreadorBarrio, vestíbulo de 3 m en CreadorLugares.
        private const float AnchoPuerta = 3f;
        private const float GrosorFachada = 1f;
        private const float ProfundidadVestibulo = 3f;
        private const float DistanciaEntrada = 1.5f;
        private const float AnchoInterior = 13f;
        private const float FondoInterior = 7.5f;
        private const float AltoInterior = 5.6f;
        private const float GrosorRevestimiento = 0.1f;

        // Taller a la izquierda, sala a la derecha; x en m desde el centro, d en m desde el muro del fondo.
        private const float TallerX = -3.5f;
        private const float TallerD = 4f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-086")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.55f, 0.48f, 0.4f), 0.1f, default);
            Material piso = Mat("Piso", new Color(0.32f, 0.3f, 0.3f), 0.35f, default);
            Material techo = Mat("Techo", new Color(0.5f, 0.46f, 0.42f), 0f, default);
            Material madera = Mat("Madera", new Color(0.4f, 0.25f, 0.14f), 0.3f, default);
            Material tela = Mat("Tela", new Color(0.35f, 0.2f, 0.25f), 0.1f, default);
            Material metal = Mat("Metal", new Color(0.25f, 0.27f, 0.3f), 0.5f, default);
            Material franja = Mat("FranjaTaller", new Color(0.9f, 0.8f, 0.1f), 0.2f, new Color(0.8f, 0.7f, 0.05f));
            Material fluorescente = Mat("Fluorescente", new Color(0.9f, 0.95f, 1f), 0.2f, new Color(2.6f, 2.8f, 3.2f));

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform casa = BuscarPorNombre(raiz.transform, "Lugar_Casa");
            Transform viejo = casa.Find("Interior");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            LugarMundo lugarMundo = casa.GetComponent<LugarMundo>();
            PuertaLocal puertaLocal = casa.Find("Puerta").GetComponent<PuertaLocal>();
            Transform entrada = casa.Find("Entrada");

            float zFrente = entrada.localPosition.z - DistanciaEntrada;
            float zFrenteInterior = zFrente - GrosorFachada - ProfundidadVestibulo;
            float zFondoInterior = zFrenteInterior - FondoInterior;
            float altoCuerpo = casa.Find("CuerpoLadoIzquierdo").localScale.y;
            float anchoCuerpo = AnchoPuerta + 2f * casa.Find("CuerpoLadoIzquierdo").localScale.x;
            Material muroExterior = casa.Find("CuerpoLadoIzquierdo").GetComponent<MeshRenderer>().sharedMaterial;

            interior = new GameObject("Interior").transform;
            interior.SetParent(casa, false);

            Transform fondoViejo = casa.Find("CuerpoFondo");
            if (fondoViejo != null)
            {
                Object.DestroyImmediate(fondoViejo.gameObject);
            }
            Transform estructura = Grupo("Estructura");
            float mitadAncho = anchoCuerpo / 2f;
            float mitadInterior = AnchoInterior / 2f;
            float grosorLado = mitadAncho - mitadInterior;
            float zCentroInterior = (zFrenteInterior + zFondoInterior) / 2f;
            float zFondoCuerpo = -zFrente;
            float profundidadTrasera = zFondoInterior - zFondoCuerpo;
            Caja(estructura, "MuroFondo", new Vector3(0f, altoCuerpo / 2f, zFondoCuerpo + profundidadTrasera / 2f), new Vector3(anchoCuerpo, altoCuerpo, profundidadTrasera), muroExterior, true);
            Caja(estructura, "MuroIzquierdo", new Vector3(-(mitadInterior + grosorLado / 2f), altoCuerpo / 2f, zCentroInterior), new Vector3(grosorLado, altoCuerpo, FondoInterior), muroExterior, true);
            Caja(estructura, "MuroDerecho", new Vector3(mitadInterior + grosorLado / 2f, altoCuerpo / 2f, zCentroInterior), new Vector3(grosorLado, altoCuerpo, FondoInterior), muroExterior, true);
            float altoSobreTecho = altoCuerpo - AltoInterior;
            Caja(estructura, "TechoInterior", new Vector3(0f, AltoInterior + altoSobreTecho / 2f, zCentroInterior), new Vector3(AnchoInterior, altoSobreTecho, FondoInterior), muroExterior, true);

            Transform revestimiento = Grupo("Revestimiento");
            Caja(revestimiento, "Piso", Pos(0f, 0.02f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, 0.04f, FondoInterior), piso, false);
            Caja(revestimiento, "Techo", Pos(0f, AltoInterior - GrosorRevestimiento / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(AnchoInterior, GrosorRevestimiento, FondoInterior), techo, false);
            Caja(revestimiento, "MuroAtras", Pos(0f, AltoInterior / 2f, GrosorRevestimiento / 2f, zFondoInterior), new Vector3(AnchoInterior, AltoInterior, GrosorRevestimiento), muro, false);
            Caja(revestimiento, "MuroIzq", Pos(-mitadInterior + GrosorRevestimiento / 2f, AltoInterior / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(GrosorRevestimiento, AltoInterior, FondoInterior), muro, false);
            Caja(revestimiento, "MuroDer", Pos(mitadInterior - GrosorRevestimiento / 2f, AltoInterior / 2f, FondoInterior / 2f, zFondoInterior), new Vector3(GrosorRevestimiento, AltoInterior, FondoInterior), muro, false);
            float anchoFrente = (AnchoInterior - AnchoPuerta) / 2f;
            Caja(revestimiento, "MuroFrenteIzq", Pos(-(AnchoPuerta / 2f + anchoFrente / 2f), AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);
            Caja(revestimiento, "MuroFrenteDer", Pos(AnchoPuerta / 2f + anchoFrente / 2f, AltoInterior / 2f, FondoInterior - GrosorRevestimiento / 2f, zFondoInterior), new Vector3(anchoFrente, AltoInterior, GrosorRevestimiento), muro, false);

            // Taller: banco de trabajo contra el muro del fondo, estantes en el muro izquierdo y franja amarilla alrededor del mecha.
            Transform taller = Grupo("Taller");
            Caja(taller, "Banco", Pos(TallerX, 0.45f, 0.5f, zFondoInterior), new Vector3(4.5f, 0.9f, 0.8f), metal, true);
            Caja(taller, "Herramientas", Pos(TallerX, 1.3f, 0.15f, zFondoInterior), new Vector3(4.5f, 0.9f, 0.1f), madera, false);
            for (int i = 0; i < 3; i++)
            {
                Caja(taller, "Estante" + i, Pos(-mitadInterior + 0.25f, 0.8f + i * 0.7f, 4f, zFondoInterior), new Vector3(0.4f, 0.06f, 3.2f), metal, true);
            }
            Caja(taller, "PiezasIzq", Pos(-mitadInterior + 0.25f, 1.0f, 3.2f, zFondoInterior), new Vector3(0.3f, 0.3f, 0.5f), madera, false);
            Caja(taller, "PiezasDer", Pos(-mitadInterior + 0.25f, 1.7f, 5f, zFondoInterior), new Vector3(0.3f, 0.3f, 0.7f), metal, false);
            for (int i = 0; i < 4; i++)
            {
                float angulo = i * 90f;
                Vector3 centro = Pos(TallerX, 0.045f, TallerD, zFondoInterior) + Quaternion.Euler(0f, angulo, 0f) * Vector3.forward * 2.05f;
                Caja(taller, "Franja" + i, centro, new Vector3(1.4f, 0.01f, 0.1f), franja, false).transform.localRotation = Quaternion.Euler(0f, angulo + 90f, 0f);
            }

            GameObject tallerObjeto = new GameObject("TallerMecha");
            tallerObjeto.transform.SetParent(interior, false);
            Transform marcaEstacionamiento = Marca(tallerObjeto.transform, "Estacionamiento", Pos(TallerX, 0.04f, TallerD, zFondoInterior));
            Vector3 haciaPuerta = Pos(0f, 0f, FondoInterior + ProfundidadVestibulo, zFondoInterior) - marcaEstacionamiento.localPosition;
            haciaPuerta.y = 0f;
            marcaEstacionamiento.localRotation = Quaternion.LookRotation(haciaPuerta.normalized, Vector3.up);
            Transform marcaInteraccion = Marca(tallerObjeto.transform, "PuntoInteraccion", Pos(TallerX, 0.04f, 1.4f, zFondoInterior));
            TallerCasa tallerCasa = tallerObjeto.AddComponent<TallerCasa>();
            SerializedObject soTaller = new SerializedObject(tallerCasa);
            soTaller.FindProperty("estacionamiento").objectReferenceValue = marcaEstacionamiento;
            soTaller.FindProperty("puntoInteraccion").objectReferenceValue = marcaInteraccion;
            soTaller.ApplyModifiedPropertiesWithoutUndo();

            // Sala: cama (cubos hasta T-087), mesita, mesa con dos bancos y cocineta contra el muro derecho.
            Transform sala = Grupo("Sala");
            Caja(sala, "CamaBase", Pos(5.4f, 0.25f, 1.3f, zFondoInterior), new Vector3(1.8f, 0.5f, 2.2f), madera, true);
            Caja(sala, "CamaColchon", Pos(5.4f, 0.6f, 1.3f, zFondoInterior), new Vector3(1.7f, 0.2f, 2.1f), tela, false);
            Caja(sala, "Mesita", Pos(4.1f, 0.3f, 0.45f, zFondoInterior), new Vector3(0.5f, 0.6f, 0.5f), madera, true);
            Caja(sala, "Mesa", Pos(3.0f, 0.7f, 4.5f, zFondoInterior), new Vector3(1.3f, 0.06f, 0.9f), madera, true);
            Caja(sala, "PataMesa", Pos(3.0f, 0.35f, 4.5f, zFondoInterior), new Vector3(0.12f, 0.7f, 0.12f), madera, false);
            Caja(sala, "Banco1", Pos(3.0f, 0.22f, 3.7f, zFondoInterior), new Vector3(0.9f, 0.44f, 0.35f), madera, true);
            Caja(sala, "Banco2", Pos(3.0f, 0.22f, 5.3f, zFondoInterior), new Vector3(0.9f, 0.44f, 0.35f), madera, true);
            Caja(sala, "Cocineta", Pos(mitadInterior - 0.35f, 0.45f, 5.2f, zFondoInterior), new Vector3(0.7f, 0.9f, 3f), metal, true);
            Caja(sala, "Refrigerador", Pos(mitadInterior - 0.4f, 0.9f, 3.3f, zFondoInterior), new Vector3(0.7f, 1.8f, 0.7f), metal, true);

            // Luces: dos fluorescentes sobre el taller y tres cálidas en la sala y la entrada (5 en total).
            Transform grupoLuces = Grupo("Luces");
            Color fria = new Color(0.8f, 0.9f, 1f);
            Color calida = new Color(1f, 0.75f, 0.45f);
            Caja(grupoLuces, "TuboTaller1", Pos(TallerX - 1f, AltoInterior - 0.1f, TallerD, zFondoInterior), new Vector3(0.1f, 0.05f, 2.5f), fluorescente, false);
            Caja(grupoLuces, "TuboTaller2", Pos(TallerX + 1f, AltoInterior - 0.1f, TallerD, zFondoInterior), new Vector3(0.1f, 0.05f, 2.5f), fluorescente, false);
            Light[] luces =
            {
                Luz(grupoLuces, "LuzTaller1", Pos(TallerX - 1f, AltoInterior - 0.8f, TallerD, zFondoInterior), fria, 9f, 9f),
                Luz(grupoLuces, "LuzTaller2", Pos(TallerX + 1f, AltoInterior - 0.8f, TallerD, zFondoInterior), fria, 9f, 9f),
                Luz(grupoLuces, "LuzSala", Pos(4f, AltoInterior - 0.8f, 3f, zFondoInterior), calida, 10f, 9f),
                Luz(grupoLuces, "LuzCama", Pos(5f, 1.8f, 1.2f, zFondoInterior), calida, 4f, 5f),
                Luz(grupoLuces, "LuzEntrada", Pos(0f, AltoInterior - 0.8f, FondoInterior - 0.8f, zFondoInterior), calida, 6f, 7f)
            };

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
            Debug.Log("[Mundo] T-086 creada: interior de la casa con taller y " + luces.Length + " luces.");
        }

        private static Vector3 Pos(float x, float y, float desdeElFondo, float zFondoInterior)
        {
            return new Vector3(x, y, zFondoInterior + desdeElFondo);
        }

        private static Transform Marca(Transform padre, string nombre, Vector3 posicion)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            return go.transform;
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
            string ruta = RutaMateriales + "/Casa_" + nombre + ".mat";
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
