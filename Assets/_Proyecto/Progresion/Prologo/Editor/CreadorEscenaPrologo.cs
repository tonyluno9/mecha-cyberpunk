using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion.Prologo.Pruebas;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Genera Pruebas_Prologo.unity (T-127): el barrio greybox donde Tony arma y prueba el prólogo sin esperar a Alan.
    /// Es la única forma de cambiar esa escena: las tareas siguientes (T-128 en adelante) agregan su parte DENTRO de
    /// <see cref="Crear"/>, como un método por sección, y no con métodos sueltos. Orden fijo e idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaPrologo.Crear
    /// </summary>
    public static class CreadorEscenaPrologo
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        public const string IdCasaPuerta = "Casa_Puerta";
        public const string IdCasaCama = "Casa_Cama";
        public const string IdClandestinoSalida = "Clandestino_Salida";

        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaEmboscada = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos";
        private const string RutaMostrador = "Assets/_Proyecto/Progresion/Prologo/Prefabs/MostradorPeleas.prefab";
        private const float HoraAnochecerClandestino = 20f;
        private const string RutaConfigEntrada = RutaDatos + "/ConfigEntradaArena_Base.asset";
        private const string TeclaEntrarArena = "<Keyboard>/t";
        private const string RutaCaptura = "docs/capturas/T-127-1.png";

        // Medidas en metros (1 unidad = 1 m).
        private const float LadoPiso = 200f;
        private const float AnchoCalle = 8f;
        private const float AlturaEdificio = 12f;
        private const float AlturaCasa = 3.5f;
        private const float Grosor = 0.4f;
        private const float AnchoPuerta = 3f;
        private const float XCalleOeste = -70f;
        private const float XEsquina = 40f;
        private const float ZClandestino = 75f;
        private const float ZCallejon = 40f;
        private const float ProfundidadCallejon = 25f;

        private static readonly Vector3 PosicionCasaPuerta = new Vector3(-70f, 0f, 2.5f);
        private static readonly Vector3 PosicionCasaCama = new Vector3(-72.5f, 0f, 10.5f);
        private static readonly Vector3 PosicionTaller = new Vector3(-55f, 0f, 8f);
        private static readonly Vector3 PosicionArena = new Vector3(40f, 0f, 100f);
        private static readonly Vector3 PosicionEmboscada = new Vector3(36f, 0f, ZCallejon);
        private static readonly Vector3 PosicionCarro = new Vector3(40f, 0f, 20f);

        private static Material matPiso;
        private static Material matAsfalto;
        private static Material matEdificio;
        private static Material matCasa;
        private static Material matTaller;
        private static Material matClandestino;
        private static Material matBanco;

        [MenuItem("Mecha Cyberpunk/Crear escena Pruebas_Prologo")]
        public static void Crear()
        {
            CrearMateriales();

            // Unity numera los objetos al azar al guardar: regenerar la misma escena cambiaría el archivo sin cambiar nada real.
            // Por eso, si lo que se arma es igual a lo que ya hay, no se guarda (idempotente).
            string firmaActual = null;
            if (File.Exists(RutaEscena))
            {
                firmaActual = Firma(EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single));
            }

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Orden fijo: cada tarea siguiente agrega su sección al final de esta lista.
            ArmarMundo();
            ArmarCasa();
            ArmarTaller();
            ArmarClandestino();
            ArmarCallejon();
            ArmarCarroYRuta();
            ArmarJugadorYServicios();
            ArmarEntradaArena();
            ArmarMostradorPeleas();

            string firmaNueva = Firma(escena);
            if (firmaActual != null && firmaActual == firmaNueva)
            {
                Debug.Log("[Progresion] Pruebas_Prologo ya estaba completa; no se guarda de nuevo.");
                return;
            }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] Pruebas_Prologo creada.");
        }

        /// <summary>
        /// Captura aérea (T-127-1) en memoria con Camera.Render, sin modificar la escena guardada.
        /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaPrologo.CapturarVistaAerea
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Capturar vista aérea de Pruebas_Prologo")]
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
            camara.orthographicSize = 80f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 400f;
            objetoCamara.transform.SetPositionAndRotation(new Vector3(-10f, 200f, 40f), Quaternion.Euler(90f, 0f, 0f));

            const int lado = 1600;
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

            Caja("Piso", new Vector3(0f, -0.5f, 20f), new Vector3(LadoPiso, 1f, LadoPiso), matPiso, true);
            // Calle este-oeste en z = 0 (x de -70 a 40) y calle norte-sur en x = 40 (z de 0 a 75). Un poco más largas para cubrir la esquina.
            Caja("Calle este-oeste", new Vector3(-15f, 0.01f, 0f), new Vector3(116f, 0.02f, AnchoCalle), matAsfalto, false);
            Caja("Calle norte-sur", new Vector3(XEsquina, 0.01f, 35.5f), new Vector3(AnchoCalle, 0.02f, 79f), matAsfalto, false);
            Caja("Callejón piso", new Vector3(XEsquina - ProfundidadCallejon / 2f, 0.01f, ZCallejon),
                new Vector3(ProfundidadCallejon, 0.02f, 8f), matAsfalto, false);

            // Edificios de cajas a los lados de las calles. El callejón queda entre el bloque A y el bloque B.
            Caja("Edificios sur", new Vector3(-15f, AlturaEdificio / 2f, -14f), new Vector3(130f, AlturaEdificio, 20f), matEdificio, true);
            Caja("Edificios bloque A", new Vector3(-4.5f, AlturaEdificio / 2f, 20f), new Vector3(81f, AlturaEdificio, 32f), matEdificio, true);
            Caja("Edificios bloque B", new Vector3(25.5f, AlturaEdificio / 2f, 59.5f), new Vector3(21f, AlturaEdificio, 31f), matEdificio, true);
            Caja("Edificios este", new Vector3(52f, AlturaEdificio / 2f, 39.5f), new Vector3(16f, AlturaEdificio, 71f), matEdificio, true);
            Caja("Fondo del callejón", new Vector3(XEsquina - ProfundidadCallejon - 0.25f, AlturaEdificio / 2f, ZCallejon),
                new Vector3(0.5f, AlturaEdificio, 8f), matEdificio, true);
        }

        private static void ArmarCasa()
        {
            // Casa de 10 x 8 m con el centro en (-70, 8) y la puerta en el muro sur (hacia la calle).
            GameObject casa = new GameObject("Casa");
            casa.transform.position = new Vector3(XCalleOeste, 0f, 8f);
            float zSur = 4.2f;
            float lado = (10f - AnchoPuerta) / 2f;
            Hija(casa, Caja("Casa muro sur oeste", new Vector3(XCalleOeste - AnchoPuerta / 2f - lado / 2f, AlturaCasa / 2f, zSur), new Vector3(lado, AlturaCasa, Grosor), matCasa, true));
            Hija(casa, Caja("Casa muro sur este", new Vector3(XCalleOeste + AnchoPuerta / 2f + lado / 2f, AlturaCasa / 2f, zSur), new Vector3(lado, AlturaCasa, Grosor), matCasa, true));
            Hija(casa, Caja("Casa muro norte", new Vector3(XCalleOeste, AlturaCasa / 2f, 11.8f), new Vector3(10f, AlturaCasa, Grosor), matCasa, true));
            Hija(casa, Caja("Casa muro oeste", new Vector3(XCalleOeste - 4.8f, AlturaCasa / 2f, 8f), new Vector3(Grosor, AlturaCasa, 8f), matCasa, true));
            Hija(casa, Caja("Casa muro este", new Vector3(XCalleOeste + 4.8f, AlturaCasa / 2f, 8f), new Vector3(Grosor, AlturaCasa, 8f), matCasa, true));
            Hija(casa, Caja("Casa techo", new Vector3(XCalleOeste, AlturaCasa + Grosor / 2f, 8f), new Vector3(10.4f, Grosor, 8.4f), matCasa, true));

            ArmarLugar("Lugar Casa", IdLugar.Casa, "Casa", new Vector3(XCalleOeste, 0f, 8f), new Vector3(9f, 3f, 7f), PosicionCasaPuerta);
            Aparicion(IdCasaCama, ModoJugador.APie, PosicionCasaCama, Quaternion.identity);
            Aparicion(IdCasaPuerta, ModoJugador.APie, PosicionCasaPuerta, Quaternion.Euler(0f, 90f, 0f));
            Vacio("Sitio_Cama", new Vector3(-73f, 0f, 11f));
            Vacio("Sitio_Espejo", new Vector3(-67f, 0f, 11.4f));
        }

        private static void ArmarTaller()
        {
            // Cobertizo abierto hacia la calle (sur). El estacionamiento es la pose del taller, con +Z hacia la salida.
            GameObject taller = new GameObject("Taller");
            taller.transform.SetPositionAndRotation(PosicionTaller, Quaternion.Euler(0f, 180f, 0f));
            TallerDePrueba contrato = taller.AddComponent<TallerDePrueba>();

            GameObject muros = new GameObject("Taller muros");
            muros.transform.position = PosicionTaller;
            Hija(muros, Caja("Taller muro oeste", new Vector3(-60.2f, 3f, 8f), new Vector3(Grosor, 6f, 8f), matTaller, true));
            Hija(muros, Caja("Taller muro este", new Vector3(-49.8f, 3f, 8f), new Vector3(Grosor, 6f, 8f), matTaller, true));
            Hija(muros, Caja("Taller muro fondo", new Vector3(-55f, 3f, 11.8f), new Vector3(10.8f, 6f, Grosor), matTaller, true));
            Hija(muros, Caja("Taller techo", new Vector3(-55f, 6.2f, 8f), new Vector3(10.8f, Grosor, 8.4f), matTaller, true));

            // Banco de trabajo en el punto de interacción del taller.
            Vector3 banco = contrato.PuntoInteraccion;
            Hija(muros, Caja("Taller banco", new Vector3(banco.x, 0.5f, banco.z), new Vector3(1.5f, 1f, 0.8f), matBanco, false));

            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha));
            mecha.transform.SetPositionAndRotation(PosicionTaller, Quaternion.Euler(0f, 180f, 0f));
        }

        private static void ArmarClandestino()
        {
            // Edificio de 36 x 40 m al norte; puerta en el muro sur en (40, 75). La arena queda a 25 m de la puerta.
            GameObject edificio = new GameObject("Clandestino");
            edificio.transform.position = new Vector3(XEsquina, 0f, 95f);
            float altura = 7f;
            float lado = (36f - AnchoPuerta) / 2f;
            Hija(edificio, Caja("Clandestino muro sur oeste", new Vector3(XEsquina - AnchoPuerta / 2f - lado / 2f, altura / 2f, ZClandestino + Grosor / 2f), new Vector3(lado, altura, Grosor), matClandestino, true));
            Hija(edificio, Caja("Clandestino muro sur este", new Vector3(XEsquina + AnchoPuerta / 2f + lado / 2f, altura / 2f, ZClandestino + Grosor / 2f), new Vector3(lado, altura, Grosor), matClandestino, true));
            Hija(edificio, Caja("Clandestino muro norte", new Vector3(XEsquina, altura / 2f, 114.8f), new Vector3(36f, altura, Grosor), matClandestino, true));
            Hija(edificio, Caja("Clandestino muro oeste", new Vector3(XEsquina - 17.8f, altura / 2f, 95f), new Vector3(Grosor, altura, 40f), matClandestino, true));
            Hija(edificio, Caja("Clandestino muro este", new Vector3(XEsquina + 17.8f, altura / 2f, 95f), new Vector3(Grosor, altura, 40f), matClandestino, true));
            Hija(edificio, Caja("Clandestino techo", new Vector3(XEsquina, altura + Grosor / 2f, 95f), new Vector3(36.4f, Grosor, 40.4f), matClandestino, true));

            ArmarLugar("Lugar Clandestino", IdLugar.Clandestino, "Clandestino", new Vector3(XEsquina, 0f, 95f), new Vector3(34f, 4f, 38f), new Vector3(XEsquina, 0f, 72f));

            GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaArena));
            arena.transform.SetPositionAndRotation(PosicionArena, Quaternion.identity);

            Aparicion(IdClandestinoSalida, ModoJugador.APie, new Vector3(XEsquina, 0f, 72f), Quaternion.Euler(0f, 180f, 0f));
            Vacio("Sitio_Mostrador", new Vector3(XEsquina + 4f, 0f, 78f));
        }

        private static void ArmarCallejon()
        {
            // La emboscada en la entrada del callejón (a la izquierda de la calle norte-sur) con +Z hacia el fondo (-X).
            GameObject emboscada = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaEmboscada));
            emboscada.transform.SetPositionAndRotation(PosicionEmboscada, Quaternion.Euler(0f, -90f, 0f));

            // Punto de llamado cuyo radio cubre la entrada del callejón y la calle frente a ella, sin llegar a la ruta casa-esquina.
            GameObject punto = new GameObject("PuntoLlamado Callejón");
            punto.transform.SetPositionAndRotation(PosicionEmboscada, Quaternion.Euler(0f, -90f, 0f));
            punto.AddComponent<PuntoLlamadoDePrueba>().Configurar(8f, true);
        }

        private static void ArmarCarroYRuta()
        {
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro));
            carro.transform.SetPositionAndRotation(PosicionCarro, Quaternion.Euler(0f, 180f, 0f));

            PuntoHistoria("Prologo_Carro", new Vector3(38f, 0f, 22f));
            PuntoHistoria("Prologo_Esquina_1", new Vector3(XEsquina, 0f, 0f));
        }

        private static void ArmarJugadorYServicios()
        {
            // Día 1, 21:00 por defecto. La noche empieza a las 20:00, la hora a la que abre el clandestino (T-129).
            RelojDePrueba reloj = new GameObject("RelojDePrueba").AddComponent<RelojDePrueba>();
            SerializedObject soReloj = new SerializedObject(reloj);
            soReloj.FindProperty("horaAnochecer").floatValue = HoraAnochecerClandestino;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("GuardadoDePrueba").AddComponent<GuardadoDePrueba>();
            new GameObject("DineroDePrueba").AddComponent<MechaCyberpunk.Economia.Pruebas.DineroDePrueba>();

            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(PosicionCasaPuerta + new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f));
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = IdCasaPuerta;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();
        }

        // T-128: el flujo de la pelea tutorial (sin arrancar solo), las pantallas de historia (fundido) y la entrada a la arena con su tecla T.
        private static void ArmarEntradaArena()
        {
            ConfigEntradaArena config = AssetDatabase.LoadAssetAtPath<ConfigEntradaArena>(RutaConfigEntrada);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(RutaDatos))
                {
                    AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Datos");
                }
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigEntradaArena>(), RutaConfigEntrada);
                AssetDatabase.SaveAssets();
                config = AssetDatabase.LoadAssetAtPath<ConfigEntradaArena>(RutaConfigEntrada);
            }

            GameObject flujo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujoTutorial));
            flujo.transform.position = PosicionArena;
            DirectorPelea director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("empezarAlArrancar").boolValue = false;
            soDirector.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefabPantallas = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria);
            PrefabUtility.InstantiatePrefab(prefabPantallas);

            GameObject objeto = new GameObject("EntradaArena");
            EntradaArena entrada = objeto.AddComponent<EntradaArena>();
            SerializedObject soEntrada = new SerializedObject(entrada);
            soEntrada.FindProperty("config").objectReferenceValue = config;
            soEntrada.FindProperty("prefabPantallas").objectReferenceValue = prefabPantallas;
            soEntrada.ApplyModifiedPropertiesWithoutUndo();

            EntradaArenaDePrueba prueba = objeto.AddComponent<EntradaArenaDePrueba>();
            SerializedObject soPrueba = new SerializedObject(prueba);
            soPrueba.FindProperty("entrada").objectReferenceValue = entrada;
            soPrueba.FindProperty("director").objectReferenceValue = director;
            SerializedProperty accion = soPrueba.FindProperty("entrar");
            accion.FindPropertyRelative("m_Name").stringValue = "Entrar a la arena";
            accion.FindPropertyRelative("m_Type").intValue = (int)InputActionType.Button;
            SerializedProperty bindings = accion.FindPropertyRelative("m_SingletonActionBindings");
            bindings.arraySize = 1;
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("m_Path").stringValue = TeclaEntrarArena;
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("m_Id").stringValue = System.Guid.NewGuid().ToString();
            soPrueba.ApplyModifiedPropertiesWithoutUndo();
        }

        // T-129: el mostrador de peleas junto a la entrada del clandestino; su flujo casero usa la misma arena que el del tutorial.
        private static void ArmarMostradorPeleas()
        {
            GameObject sitio = GameObject.Find("Sitio_Mostrador");
            GameObject mostrador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador));
            mostrador.transform.position = sitio != null ? sitio.transform.position : Vector3.zero;
            mostrador.GetComponentInChildren<DirectorPelea>(true).transform.position = PosicionArena;
        }

        // ---------- Ayudas ----------

        private static void ArmarLugar(string nombreObjeto, IdLugar id, string nombre, Vector3 centroZona, Vector3 tamanoZona, Vector3 entrada)
        {
            GameObject objeto = new GameObject(nombreObjeto);
            objeto.transform.position = centroZona;
            GameObject puntoEntrada = new GameObject("Entrada");
            puntoEntrada.transform.SetParent(objeto.transform, false);
            puntoEntrada.transform.position = entrada;

            LugarDePrueba lugar = objeto.AddComponent<LugarDePrueba>();
            SerializedObject soLugar = new SerializedObject(lugar);
            soLugar.FindProperty("idLugar").enumValueIndex = (int)id;
            soLugar.FindProperty("nombre").stringValue = nombre;
            soLugar.FindProperty("entrada").objectReferenceValue = puntoEntrada.transform;
            soLugar.ApplyModifiedPropertiesWithoutUndo();

            GameObject zona = new GameObject("Zona");
            zona.transform.SetParent(objeto.transform, false);
            zona.transform.localPosition = new Vector3(0f, tamanoZona.y / 2f, 0f);
            BoxCollider caja = zona.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = tamanoZona;
            SerializedObject soZona = new SerializedObject(zona.AddComponent<ZonaLugarDePrueba>());
            soZona.FindProperty("lugar").objectReferenceValue = lugar;
            soZona.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Aparicion(string id, ModoJugador modo, Vector3 posicion, Quaternion rotacion)
        {
            GameObject objeto = new GameObject("PuntoAparicion " + id);
            objeto.transform.SetPositionAndRotation(posicion, rotacion);
            SerializedObject so = new SerializedObject(objeto.AddComponent<PuntoAparicionDePrueba>());
            so.FindProperty("id").stringValue = id;
            so.FindProperty("modo").enumValueIndex = (int)modo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PuntoHistoria(string id, Vector3 posicion)
        {
            GameObject objeto = new GameObject("PuntoHistoria " + id);
            objeto.transform.position = posicion;
            SerializedObject so = new SerializedObject(objeto.AddComponent<PuntoHistoriaDePrueba>());
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Vacio(string nombre, Vector3 posicion)
        {
            new GameObject(nombre).transform.position = posicion;
        }

        private static void Hija(GameObject padre, GameObject hijo)
        {
            hijo.transform.SetParent(padre.transform, true);
        }

        private static GameObject Caja(string nombre, Vector3 centro, Vector3 tamano, Material material, bool conCollider)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(caja.GetComponent<Collider>());
            }
            return caja;
        }

        private static void CrearMateriales()
        {
            if (!AssetDatabase.IsValidFolder(RutaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Materiales");
            }
            matPiso = CrearMaterial("Prologo_Piso", new Color(0.2f, 0.21f, 0.23f));
            matAsfalto = CrearMaterial("Prologo_Asfalto", new Color(0.08f, 0.08f, 0.1f));
            matEdificio = CrearMaterial("Prologo_Edificio", new Color(0.3f, 0.26f, 0.38f));
            matCasa = CrearMaterial("Prologo_Casa", new Color(0.8f, 0.55f, 0.25f));
            matTaller = CrearMaterial("Prologo_Taller", new Color(0.25f, 0.6f, 0.7f));
            matClandestino = CrearMaterial("Prologo_Clandestino", new Color(0.85f, 0.15f, 0.55f));
            matBanco = CrearMaterial("Prologo_Banco", new Color(0.9f, 0.85f, 0.2f));
        }

        private static Material CrearMaterial(string nombre, Color color)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        // Texto con todo lo que importa de la escena: objetos en orden, componentes y sus valores serializados.
        private static string Firma(Scene escena)
        {
            StringBuilder texto = new StringBuilder();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                FirmarObjeto(raiz.transform, texto);
            }
            return texto.ToString();
        }

        private static void FirmarObjeto(Transform objeto, StringBuilder texto)
        {
            texto.Append(objeto.name).Append('|').Append(objeto.gameObject.activeSelf).Append('|')
                .Append(PrefabUtility.GetCorrespondingObjectFromSource(objeto.gameObject)).Append('\n');
            foreach (Component componente in objeto.GetComponents<Component>())
            {
                if (componente == null)
                {
                    continue;
                }
                texto.Append(componente.GetType().Name).Append('\n');
                if (componente is RectTransform)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(componente);
                SerializedProperty propiedad = so.GetIterator();
                while (propiedad.NextVisible(true))
                {
                    if (propiedad.propertyPath.EndsWith("m_Id"))
                    {
                        continue; // los ids de las InputAction se inventan al crearlas
                    }
                    if (propiedad.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Object referencia = propiedad.objectReferenceValue;
                        texto.Append(propiedad.propertyPath).Append('=').Append(referencia != null ? referencia.name : "null").Append('\n');
                    }
                    else if (propiedad.propertyType != SerializedPropertyType.Generic)
                    {
                        texto.Append(propiedad.propertyPath).Append('=').Append(propiedad.boxedValue).Append('\n');
                    }
                }
            }
            foreach (Transform hijo in objeto)
            {
                FirmarObjeto(hijo, texto);
            }
        }
    }
}
