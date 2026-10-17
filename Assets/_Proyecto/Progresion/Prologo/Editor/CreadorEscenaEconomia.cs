using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Genera Pruebas_Economia.unity (T-143): una escena greybox chica (unos 40 x 30 m) para probar la economía después de la demo
    /// sin pasar por el prólogo. Reutiliza las ayudas de <see cref="CreadorEscenaPrologo"/>. Idempotente: si lo que se arma es igual
    /// a lo que ya hay, no se guarda de nuevo. Las tareas siguientes agregan su parte DENTRO de <see cref="Crear"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaEconomia.Crear
    /// </summary>
    public static class CreadorEscenaEconomia
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        public const string IdInicio = "Economia_Inicio";

        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaMostrador = CreadorMostradorPeleas.RutaPrefab;
        private const string RutaMostradorTienda = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales/";
        private const string RutaCaptura = "docs/capturas/T-143-1.png";
        private const float HoraAnochecer = 20f;

        // Medidas en metros (1 unidad = 1 m). El piso va de x -20 a 20 y de z -5 a 25.
        private const float AnchoPiso = 40f;
        private const float LargoPiso = 30f;
        private const float CentroPisoZ = 10f;
        private const float AlturaMuro = 4f;
        private const float GrosorMuro = 0.4f;
        private const float AlturaLetrero = 3f;
        private const float TamanoLetra = 0.5f;

        public static readonly Vector3 PosicionInicio = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 PosicionTaller = new Vector3(-12f, 0f, 3f);
        public static readonly Vector3 PosicionMostrador = new Vector3(0f, 0f, 15f);
        public static readonly Vector3 PosicionSalida = new Vector3(0f, 0f, 12f);
        public static readonly Vector3 PosicionMercado = new Vector3(8f, 0f, 6f);
        public static readonly Vector3 PosicionArena = new Vector3(100f, 0f, 100f);

        private static Material matPiso;
        private static Material matMuro;
        private static Material matTaller;
        private static Material matPeleas;
        private static Material matMercado;
        private static Material matBanco;

        [MenuItem("Mecha Cyberpunk/Crear escena Pruebas_Economia")]
        public static void Crear()
        {
            CargarMateriales();

            string firmaActual = null;
            if (File.Exists(RutaEscena))
            {
                firmaActual = CreadorEscenaPrologo.Firma(EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single));
            }

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ArmarMundo();
            ArmarTaller();
            ArmarMostradorPeleas();
            ArmarMercado();
            ArmarJugadorYServicios();

            string firmaNueva = CreadorEscenaPrologo.Firma(escena);
            if (firmaActual != null && firmaActual == firmaNueva)
            {
                Debug.Log("[Progresion] Pruebas_Economia ya estaba completa; no se guarda de nuevo.");
                return;
            }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] Pruebas_Economia creada.");
        }

        /// <summary>
        /// Captura aérea (T-143-1) en memoria con Camera.Render, sin modificar la escena guardada.
        /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaEconomia.CapturarVistaAerea
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Capturar vista aérea de Pruebas_Economia")]
        public static void CapturarVistaAerea()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject luzCaptura = new GameObject("LuzCaptura");
            Light luz = luzCaptura.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.2f;
            luzCaptura.transform.rotation = Quaternion.Euler(70f, 20f, 0f);

            GameObject objetoCamara = new GameObject("CamaraCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.orthographic = true;
            camara.orthographicSize = 18f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 100f;
            objetoCamara.transform.SetPositionAndRotation(new Vector3(0f, 40f, CentroPisoZ), Quaternion.Euler(90f, 0f, 0f));

            const int lado = 1400;
            RenderTexture destino = new RenderTexture(lado, lado, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, lado, lado), 0, 0);
            textura.Apply();
            RenderTexture.active = anterior;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            camara.targetTexture = null;
            destino.Release();
            Object.DestroyImmediate(destino);
            Object.DestroyImmediate(objetoCamara);
            Object.DestroyImmediate(luzCaptura);
            Debug.Log("[Progresion] Captura guardada en " + RutaCaptura);
        }

        // ---------- Secciones ----------

        private static void ArmarMundo()
        {
            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            componenteLuz.color = new Color(0.55f, 0.6f, 0.9f);
            componenteLuz.intensity = 0.5f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CreadorEscenaPrologo.Caja("Piso", new Vector3(0f, -0.5f, CentroPisoZ), new Vector3(AnchoPiso, 1f, LargoPiso), matPiso, true);
            float mitadX = AnchoPiso / 2f;
            float mitadZ = LargoPiso / 2f;
            CreadorEscenaPrologo.Caja("Muro sur", new Vector3(0f, AlturaMuro / 2f, CentroPisoZ - mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro norte", new Vector3(0f, AlturaMuro / 2f, CentroPisoZ + mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro oeste", new Vector3(-mitadX, AlturaMuro / 2f, CentroPisoZ), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro este", new Vector3(mitadX, AlturaMuro / 2f, CentroPisoZ), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);
        }

        private static void ArmarTaller()
        {
            // El estacionamiento mira al este (+X); el banco queda a 3 m del mecha (desplazamiento del doble del taller).
            Quaternion rotacion = Quaternion.Euler(0f, 90f, 0f);
            GameObject taller = new GameObject("Taller");
            taller.transform.SetPositionAndRotation(PosicionTaller, rotacion);
            TallerDePrueba contrato = taller.AddComponent<TallerDePrueba>();

            Vector3 banco = contrato.PuntoInteraccion;
            CreadorEscenaPrologo.Hija(taller, CreadorEscenaPrologo.Caja("Taller banco", new Vector3(banco.x, 0.5f, banco.z), new Vector3(1.5f, 1f, 0.8f), matBanco, false));
            CreadorEscenaPrologo.Caja("Taller suelo", new Vector3(PosicionTaller.x, 0.01f, PosicionTaller.z), new Vector3(8f, 0.02f, 9f), matTaller, false);
            Letrero("Letrero TALLER", "TALLER", new Vector3(PosicionTaller.x, 0f, PosicionTaller.z + 5.5f), matTaller);

            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha));
            mecha.transform.SetPositionAndRotation(PosicionTaller, rotacion);
        }

        private static void ArmarMostradorPeleas()
        {
            CreadorEscenaPrologo.Vacio("Sitio_Mostrador", PosicionMostrador);
            GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaArena));
            arena.transform.SetPositionAndRotation(PosicionArena, Quaternion.identity);

            GameObject mostrador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador));
            mostrador.transform.position = PosicionMostrador;
            mostrador.GetComponentInChildren<DirectorPelea>(true).transform.position = PosicionArena;

            CreadorEscenaPrologo.Aparicion(CreadorEscenaPrologo.IdClandestinoSalida, ModoJugador.APie, PosicionSalida, Quaternion.Euler(0f, 180f, 0f));
            CreadorEscenaPrologo.Caja("Peleas suelo", new Vector3(PosicionMostrador.x, 0.01f, PosicionMostrador.z - 1f), new Vector3(7f, 0.02f, 5f), matPeleas, false);
            Letrero("Letrero PELEAS", "PELEAS", new Vector3(PosicionMostrador.x, 0f, PosicionMostrador.z + 3f), matPeleas);
        }

        private static void ArmarMercado()
        {
            // Sitio del mostrador del mercado negro (T-146, MostradorMercadoNegro.prefab) con su lugar siempre abierto (el horario por defecto no cierra nunca).
            CreadorEscenaPrologo.Vacio("Sitio_Mercado", PosicionMercado);
            CreadorEscenaPrologo.ArmarLugar("Lugar MercadoNegro", IdLugar.MercadoNegro, "Mercado negro", PosicionMercado, new Vector3(8f, 3f, 6f), PosicionMercado + new Vector3(0f, 0f, -3f));
            GameObject tienda = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostradorTienda));
            tienda.transform.position = PosicionMercado;
            CreadorEscenaPrologo.Caja("Mercado suelo", new Vector3(PosicionMercado.x, 0.01f, PosicionMercado.z), new Vector3(7f, 0.02f, 5f), matMercado, false);
            Letrero("Letrero MERCADO", "MERCADO", new Vector3(PosicionMercado.x, 0f, PosicionMercado.z + 3.5f), matMercado);
        }

        private static void ArmarJugadorYServicios()
        {
            // 21:00 de noche (la noche empieza a las 20:00, como en Pruebas_Prologo).
            RelojDePrueba reloj = new GameObject("RelojDePrueba").AddComponent<RelojDePrueba>();
            SerializedObject soReloj = new SerializedObject(reloj);
            soReloj.FindProperty("horaAnochecer").floatValue = HoraAnochecer;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("GuardadoDePrueba").AddComponent<GuardadoDePrueba>();
            new GameObject("DineroDePrueba").AddComponent<MechaCyberpunk.Economia.Pruebas.DineroDePrueba>();
            new GameObject("HistoriaLibreDePrueba").AddComponent<HistoriaLibreDePrueba>();

            GameObject inventario = new GameObject("InventarioDePrueba");
            MechaCyberpunk.Economia.Pruebas.InventarioDePrueba prueba = inventario.AddComponent<MechaCyberpunk.Economia.Pruebas.InventarioDePrueba>();
            SerializedObject soInventario = new SerializedObject(prueba);
            soInventario.FindProperty("brazo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            soInventario.FindProperty("kit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            soInventario.ApplyModifiedPropertiesWithoutUndo();

            CreadorEscenaPrologo.Aparicion(IdInicio, ModoJugador.APie, PosicionInicio, Quaternion.identity);
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(PosicionInicio + new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = IdInicio;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Ayudas ----------

        // Letrero: un tablero de color de pie (con el texto de cara al inicio) y el mismo texto tendido en el piso para verlo desde el aire.
        private static void Letrero(string nombre, string texto, Vector3 posicion, Material material)
        {
            GameObject raiz = new GameObject(nombre);
            raiz.transform.position = posicion;
            CreadorEscenaPrologo.Hija(raiz, CreadorEscenaPrologo.Caja(nombre + " tablero", posicion + new Vector3(0f, AlturaLetrero, 0f), new Vector3(5f, 1.2f, 0.1f), material, false));
            Texto(raiz, texto, posicion + new Vector3(0f, AlturaLetrero, -0.1f), Quaternion.identity);
            Texto(raiz, texto, posicion + new Vector3(0f, 0.03f, -2f), Quaternion.Euler(90f, 0f, 0f));
        }

        private static void Texto(GameObject padre, string texto, Vector3 posicion, Quaternion rotacion)
        {
            GameObject objeto = new GameObject("Texto " + texto);
            objeto.transform.SetParent(padre.transform, false);
            objeto.transform.SetPositionAndRotation(posicion, rotacion);
            TextMesh mesh = objeto.AddComponent<TextMesh>();
            mesh.text = texto;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 100;
            mesh.characterSize = TamanoLetra / 10f;
            mesh.color = Color.white;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = fuente;
            objeto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        private static void CargarMateriales()
        {
            matPiso = CargarMaterial("Prologo_Piso");
            matMuro = CargarMaterial("Prologo_Edificio");
            matTaller = CargarMaterial("Prologo_Taller");
            matPeleas = CargarMaterial("Prologo_Clandestino");
            matMercado = CargarMaterial("Prologo_Casa");
            matBanco = CargarMaterial("Prologo_Banco");
        }

        private static Material CargarMaterial(string nombre)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + nombre + ".mat");
            if (material == null)
            {
                throw new System.InvalidOperationException("Falta el material " + nombre + "; corre antes CreadorEscenaPrologo.Crear.");
            }
            return material;
        }
    }
}
