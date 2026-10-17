using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el interior del clandestino de T-089 dentro del prefab del barrio: muros con collider, vestíbulo, una fosa de peleas
    /// visual (plataforma con rejas y una puerta hacia la entrada) rodeada de gradas con público, un bar chico y su ZonaInterior.
    /// La jaula real de la arena (24 × 24 m, Arena/JaulaClandestino.prefab) sigue en Clandestino.unity: no cabe en este edificio.
    /// Corre después de CreadorCallejon y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorClandestino.Crear
    /// </summary>
    public static class CreadorClandestino
    {
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        // Medidas (m): el edificio mide 24.67 × 24.67 × 16 en CreadorBarrio y el vestíbulo 3 m en CreadorLugares.
        private const float AnchoPuerta = 3f;
        private const float GrosorFachada = 1f;
        private const float ProfundidadVestibulo = 3f;
        private const float DistanciaEntrada = 1.5f;
        private const float AnchoInterior = 22f;
        private const float FondoInterior = 19.5f;
        private const float AltoInterior = 12f;
        private const float GrosorRevestimiento = 0.1f;

        // La fosa: plataforma cuadrada centrada en x = 0, a RingD metros del muro del fondo; la puerta mira a la entrada.
        private const float RingD = 9f;
        private const float RingMitad = 5.2f;
        private const float RingAlto = 0.4f;
        private const float PuertaMitad = 1.2f;
        private const float BordeD = RingD + RingMitad + 1f;    // punto frente a la puerta de la fosa
        private const int Escalones = 3;
        private const float AnchoEscalon = 1.2f;
        private const float AltoEscalon = 0.4f;
        private const float MetrosPorPersona = 1.6f;

        private static Transform interior;

        [MenuItem("Mecha Cyberpunk/Crear T-089")]
        public static void Crear()
        {
            Material muro = Mat("Muro", new Color(0.22f, 0.16f, 0.26f), 0.1f, default);
            Material piso = Mat("Piso", new Color(0.14f, 0.14f, 0.18f), 0.5f, default);
            Material techo = Mat("Techo", new Color(0.12f, 0.1f, 0.15f), 0f, default);
            Material metal = Mat("Metal", new Color(0.2f, 0.21f, 0.25f), 0.5f, default);
            Material grada = Mat("Grada", new Color(0.3f, 0.3f, 0.34f), 0.2f, default);
            Material lona = Mat("Lona", new Color(0.5f, 0.12f, 0.12f), 0.2f, default);
            Material madera = Mat("Madera", new Color(0.3f, 0.17f, 0.09f), 0.3f, default);
            Material neonCian = Mat("NeonCian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));
            Material neonMagenta = Mat("NeonMagenta", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 2f));
            Material[] publico =
            {
                Mat("PublicoA", new Color(0.55f, 0.2f, 0.25f), 0.1f, default),
                Mat("PublicoB", new Color(0.2f, 0.35f, 0.55f), 0.1f, default),
                Mat("PublicoC", new Color(0.55f, 0.5f, 0.2f), 0.1f, default),
                Mat("PublicoD", new Color(0.25f, 0.45f, 0.3f), 0.1f, default)
            };

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform lugar = BuscarPorNombre(raiz.transform, "Lugar_Clandestino");
            Transform viejo = lugar.Find("Interior");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            LugarMundo lugarMundo = lugar.GetComponent<LugarMundo>();
            PuertaLocal puertaLocal = lugar.Find("Puerta").GetComponent<PuertaLocal>();
            Transform entrada = lugar.Find("Entrada");

            float zFrente = entrada.localPosition.z - DistanciaEntrada;
            float zFrenteInterior = zFrente - GrosorFachada - ProfundidadVestibulo;
            float zFondoInterior = zFrenteInterior - FondoInterior;
            float altoCuerpo = lugar.Find("CuerpoLadoIzquierdo").localScale.y;
            float anchoCuerpo = AnchoPuerta + 2f * lugar.Find("CuerpoLadoIzquierdo").localScale.x;
            Material muroExterior = lugar.Find("CuerpoLadoIzquierdo").GetComponent<MeshRenderer>().sharedMaterial;

            interior = new GameObject("Interior").transform;
            interior.SetParent(lugar, false);

            Transform fondoViejo = lugar.Find("CuerpoFondo");
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

            // Fosa: plataforma, cuatro postes, rejas por tres lados y por el frente con una puerta, y una lona en el piso.
            Transform fosa = Grupo("Fosa");
            Caja(fosa, "Plataforma", Pos(0f, RingAlto / 2f, RingD, zFondoInterior), new Vector3(2f * RingMitad, RingAlto, 2f * RingMitad), metal, true);
            Caja(fosa, "Lona", Pos(0f, RingAlto + 0.01f, RingD, zFondoInterior), new Vector3(2f * RingMitad - 0.6f, 0.02f, 2f * RingMitad - 0.6f), lona, false);
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    Caja(fosa, "Poste", Pos(sx * RingMitad, RingAlto + 1.4f, RingD + sz * RingMitad, zFondoInterior), new Vector3(0.3f, 2.8f, 0.3f), metal, true);
                }
            }
            foreach (float alturaReja in new[] { 1.0f, 1.9f, 2.7f })
            {
                Material materialReja = alturaReja > 2.5f ? neonCian : metal;
                float y = RingAlto + alturaReja;
                Caja(fosa, "RejaAtras", Pos(0f, y, RingD - RingMitad, zFondoInterior), new Vector3(2f * RingMitad, 0.12f, 0.1f), materialReja, true);
                Caja(fosa, "RejaIzq", Pos(-RingMitad, y, RingD, zFondoInterior), new Vector3(0.1f, 0.12f, 2f * RingMitad), materialReja, true);
                Caja(fosa, "RejaDer", Pos(RingMitad, y, RingD, zFondoInterior), new Vector3(0.1f, 0.12f, 2f * RingMitad), materialReja, true);
                float largoFrente = RingMitad - PuertaMitad;
                Caja(fosa, "RejaFrenteIzq", Pos(-(PuertaMitad + largoFrente / 2f), y, RingD + RingMitad, zFondoInterior), new Vector3(largoFrente, 0.12f, 0.1f), materialReja, true);
                Caja(fosa, "RejaFrenteDer", Pos(PuertaMitad + largoFrente / 2f, y, RingD + RingMitad, zFondoInterior), new Vector3(largoFrente, 0.12f, 0.1f), materialReja, true);
            }

            // Gradas en los dos lados y al fondo, con público de cilindros sin collider.
            Transform gradas = Grupo("Gradas");
            Transform grupoPublico = Grupo("Publico");
            int numeroPersona = 0;
            for (int k = 0; k < Escalones; k++)
            {
                float alto = AltoEscalon * (k + 1);
                float xLado = mitadInterior - AnchoEscalon * (k + 0.5f);
                foreach (float signo in new[] { -1f, 1f })
                {
                    Caja(gradas, "GradaLado", Pos(signo * xLado, alto / 2f, 9.5f, zFondoInterior), new Vector3(AnchoEscalon, alto, 12f), grada, true);
                    for (float d = 4f; d <= 15f; d += MetrosPorPersona)
                    {
                        Persona(grupoPublico, Pos(signo * xLado, alto + 0.45f, d, zFondoInterior), publico[numeroPersona++ % publico.Length]);
                    }
                }
                float dFondo = AnchoEscalon * (k + 0.5f);
                Caja(gradas, "GradaFondo", Pos(0f, alto / 2f, dFondo, zFondoInterior), new Vector3(2f * (mitadInterior - 4f), alto, AnchoEscalon), grada, true);
                for (float x = -(mitadInterior - 4.5f); x <= mitadInterior - 4.5f; x += MetrosPorPersona)
                {
                    Persona(grupoPublico, Pos(x, alto + 0.45f, dFondo, zFondoInterior), publico[numeroPersona++ % publico.Length]);
                }
            }

            // Bar chico contra el muro del frente, a la izquierda de la puerta, con banquitos.
            Transform bar = Grupo("BarChico");
            Caja(bar, "Barra", Pos(-7.5f, 0.55f, FondoInterior - 1.3f, zFondoInterior), new Vector3(6f, 1.1f, 0.8f), madera, true);
            Caja(bar, "NeonBarra", Pos(-7.5f, 1.2f, FondoInterior - 1.3f, zFondoInterior), new Vector3(6f, 0.06f, 0.82f), neonMagenta, false);
            for (int i = 0; i < 4; i++)
            {
                Cilindro(bar, "Banquito", Pos(-9.5f + i * 1.3f, 0.35f, FondoInterior - 2.4f, zFondoInterior), madera);
            }
            Caja(bar, "Estante", Pos(-7.5f, 1.8f, FondoInterior - 0.2f, zFondoInterior), new Vector3(6f, 0.08f, 0.3f), metal, false);

            // Letrero de neón sobre la fosa y tiras en los muros.
            Transform neon = Grupo("Neon");
            Caja(neon, "LetreroFosa", Pos(0f, 6f, 0.2f, zFondoInterior), new Vector3(7f, 1.2f, 0.1f), neonMagenta, false);
            Caja(neon, "TiraIzq", Pos(-mitadInterior + 0.12f, 5f, FondoInterior / 2f, zFondoInterior), new Vector3(0.05f, 0.1f, 17f), neonCian, false);
            Caja(neon, "TiraDer", Pos(mitadInterior - 0.12f, 5f, FondoInterior / 2f, zFondoInterior), new Vector3(0.05f, 0.1f, 17f), neonCian, false);

            // Luces: cuatro focos sobre la fosa, dos de neón y dos cálidas en el bar (8 en total).
            Transform grupoLuces = Grupo("Luces");
            Color fria = new Color(0.85f, 0.9f, 1f);
            Color calida = new Color(1f, 0.7f, 0.4f);
            Light[] luces =
            {
                Luz(grupoLuces, "FocoFosa1", Pos(-3f, AltoInterior - 1.5f, RingD - 3f, zFondoInterior), fria, 12f, 14f),
                Luz(grupoLuces, "FocoFosa2", Pos(3f, AltoInterior - 1.5f, RingD - 3f, zFondoInterior), fria, 12f, 14f),
                Luz(grupoLuces, "FocoFosa3", Pos(-3f, AltoInterior - 1.5f, RingD + 3f, zFondoInterior), fria, 12f, 14f),
                Luz(grupoLuces, "FocoFosa4", Pos(3f, AltoInterior - 1.5f, RingD + 3f, zFondoInterior), fria, 12f, 14f),
                Luz(grupoLuces, "LuzNeonFondo", Pos(0f, 5f, 2f, zFondoInterior), new Color(0.9f, 0.1f, 0.7f), 8f, 12f),
                Luz(grupoLuces, "LuzNeonLados", Pos(0f, 4.5f, 12f, zFondoInterior), new Color(0.1f, 0.8f, 0.9f), 6f, 12f),
                Luz(grupoLuces, "LuzBar1", Pos(-8.5f, 3f, FondoInterior - 2f, zFondoInterior), calida, 6f, 8f),
                Luz(grupoLuces, "LuzBar2", Pos(-5f, 3f, FondoInterior - 2f, zFondoInterior), calida, 6f, 8f)
            };

            // Punto frente a la puerta de la fosa, para pruebas y para quien quiera guiar al jugador.
            GameObject borde = new GameObject("BordeFosa");
            borde.transform.SetParent(interior, false);
            borde.transform.localPosition = Pos(0f, 0.02f, BordeD, zFondoInterior);

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
            Debug.Log("[Mundo] T-089 creada: interior del clandestino con " + luces.Length + " luces y " + numeroPersona + " personas.");
        }

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

        private static void Persona(Transform padre, Vector3 posicion, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Persona";
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void Cilindro(Transform padre, string nombre, Vector3 posicion, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = new Vector3(0.4f, 0.35f, 0.4f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
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
            string ruta = RutaMateriales + "/Clandestino_" + nombre + ".mat";
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
