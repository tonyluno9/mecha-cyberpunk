using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Editor;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Creador aditivo de la ciudad del juego de pandillas (D-tony-205). Abre Ciudad_Prologo (que ya tiene el arte de T-250, no se regenera),
    /// rehace solo la raíz GTA_T40x de cada tarea y apaga (nunca borra) lo que estorba. Cada tarea agrega su método (T401...) y Todo() los corre en orden.
    /// Idempotente: correrlo dos veces deja la escena igual.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.Todo
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizT400 = "GTA_T400";
        public const string RaizT401 = "GTA_T401";
        public const string RaizT402 = "GTA_T402";
        public const string RaizT403 = "GTA_T403";
        public const string RaizT404 = "GTA_T404";
        public const string RaizT405 = "GTA_T405";
        public const string RaizT406 = "GTA_T406";
        public const string RaizT407 = "GTA_T407";
        public const float HoraInicial = 19f;
        public const float DistanciaMuneco1 = 8f;
        public const float DistanciaMuneco2 = 12f;

        private const string RutaCarroViejo = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string TipoCarroEstacionado = "MechaCyberpunk.Vehiculos.Carro.CarroEstacionado, MechaCyberpunk.Vehiculos";
        private const string RutaMuneco = "Assets/_Proyecto/Personaje/Pruebas/MunecoAPie.prefab";
        private const string RutaRelojPandillas = "Assets/_Proyecto/Progresion/Prologo/Datos/ConfigRelojMundo_Pandillas.asset";
        private const string IdAparicion = "Casa_Puerta";
        private static readonly string[] ApagadosT400 = { "MechaEstacionado", "Emboscada", "DirectorPrologo", "MostradorPeleas" };

        [MenuItem("Mecha Cyberpunk/Pandillas/Todo")]
        public static void Todo()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T400Interno();
            T401Interno();
            T402Interno();
            T403Interno();
            T404Interno();
            T405Interno();
            T406Interno();
            T407Interno();
            T414Interno();
            T416Interno();
            T417Interno();
            T418Interno();
            T426Interno();
            T427Interno();
            T428Interno();
            T429Interno();
            T409Interno();
            T412Interno();
            T410Interno();
            T411Interno();
            T413Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-407 Sonido, mercado y Tianguis")]
        public static void T407()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T407Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-404 Zonas y mapa")]
        public static void T404()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T404Interno();
            Guardar(escena);
        }

        private const string CarpetaZonas = "Assets/_Proyecto/Progresion/Datos/Territorio";
        private const float LimiteZonaX = 47f;
        private const float ExtensionZona = 400f;
        private const float DesvioJefe = 4f;

        private static T CrearAsset<T>(string ruta) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, ruta);
            }
            return asset;
        }

        private static DatosZona CrearZona(string id, string nombre, IdPandilla dueno, Color color, float xMin, float xMax, float zMin = -ExtensionZona, float zMax = ExtensionZona)
        {
            DatosZona zona = CrearAsset<DatosZona>(CarpetaZonas + "/DatosZona_" + id + ".asset");
            SerializedObject so = new SerializedObject(zona);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("duenoInicial").enumValueIndex = (int)dueno;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("rectangulo").rectValue = new Rect(xMin, zMin, xMax - xMin, zMax - zMin);
            so.FindProperty("respetoInicial").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zona);
            return zona;
        }

        // Datos del jefe: el pandillero armado de la pandilla con 250 de vida y una escopeta (8 perdigones de 6, cada 1.6 s, 15 m, aviso de 0.5 s).
        private static DatosPandilleroAPie CrearDatosJefe(string ruta, DatosPandilleroAPie baseDatos, string nombre, float vida = 250f, int perdigones = 8, float danio = 6f, float recarga = 1.6f, int dinero = 300)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(ruta);
            if (datos == null)
            {
                datos = Object.Instantiate(baseDatos);
                AssetDatabase.CreateAsset(datos, ruta);
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("vidaMaxima").floatValue = vida;
            so.FindProperty("perdigones").intValue = perdigones;
            so.FindProperty("danioDisparo").floatValue = danio;
            so.FindProperty("preparacionGolpe").floatValue = 0.5f;
            so.FindProperty("recargaGolpe").floatValue = recarga;
            so.FindProperty("distanciaMinTiro").floatValue = 6f;
            so.FindProperty("distanciaMaxTiro").floatValue = 15f;
            so.FindProperty("distanciaAciertoCerca").floatValue = 8f;
            so.FindProperty("distanciaAciertoLejos").floatValue = 15f;
            so.FindProperty("aciertoCerca").floatValue = 0.7f;
            so.FindProperty("aciertoLejos").floatValue = 0.4f;
            so.FindProperty("maxAtacando").intValue = 1;
            so.FindProperty("dinero").intValue = dinero;
            so.FindProperty("calorAlVencer").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        // T-404: el servicio de territorio con las 3 zonas, el HUD de zona, el mapa con M, los atajos de prueba y un jefe apagado por zona rival.
        private static void T404Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT404);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            CreadorPandilleroArmado.Crear();
            DatosZona fosa = CrearZona("fosa", "SUBURBIOS", IdPandilla.Jugador, new Color(1f, 0.5f, 0.1f), -ExtensionZona, -LimiteZonaX);
            DatosZona tianguis = CrearZona("tianguis", "EL TIANGUIS", IdPandilla.Calaveras, new Color(1f, 0.1f, 0.8f), -LimiteZonaX, LimiteZonaX);
            // T-428: LA REFINERÍA recorta a NEÓN ALTO la mitad sur (z < 0), donde está Manzana_6.
            DatosZona neon = CrearZona("neon", "NEÓN ALTO", IdPandilla.Viboras, new Color(0.45f, 1f, 0.1f), LimiteZonaX, ExtensionZona, 0f, ExtensionZona);
            DatosZona refineria = CrearZona("refineria", "LA REFINERÍA", IdPandilla.Nova, new Color(0.62f, 0.4f, 1f), LimiteZonaX, ExtensionZona, -ExtensionZona, 0f);

            GameObject raiz = new GameObject(RaizT404);
            ServicioTerritorio servicio = raiz.AddComponent<ServicioTerritorio>();
            SerializedObject soServicio = new SerializedObject(servicio);
            SerializedProperty lista = soServicio.FindProperty("zonas");
            lista.arraySize = 4;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = fosa;
            lista.GetArrayElementAtIndex(1).objectReferenceValue = tianguis;
            lista.GetArrayElementAtIndex(2).objectReferenceValue = neon;
            lista.GetArrayElementAtIndex(3).objectReferenceValue = refineria;
            soServicio.ApplyModifiedPropertiesWithoutUndo();
            raiz.AddComponent<MechaCyberpunk.UI.Hud.HudTerritorio>();
            raiz.AddComponent<MechaCyberpunk.UI.Mapa.PantallaMapa>();
#if !DEMO_FINAL
            raiz.AddComponent<TerritorioDePrueba>();
#endif

            GameObject prefabPandillero = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPandilleroArmado.RutaPrefab);
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosCalaveras);
            DatosPandilleroAPie viboras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosViboras);
            List<Vector3> puertas = new List<Vector3>();
            foreach (GameObject objetoRaiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in objetoRaiz.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name == "Puerta" || t.name == "Casa_Puerta")
                    {
                        puertas.Add(t.position);
                    }
                }
            }
            DatosPandilleroAPie nova = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosNova);
            string[] ids = { "tianguis", "neon", "refineria" };
            string[] manzanas = { "Manzana_2", "Manzana_3", "Manzana_6" };
            IdPandilla[] pandillas = { IdPandilla.Calaveras, IdPandilla.Viboras, IdPandilla.Nova };
            DatosPandilleroAPie[] baseDatos = { calaveras, viboras, nova };
            string[] rutasJefe =
            {
                "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeCalaveras.asset",
                "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeViboras.asset",
                "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset"
            };
            // T-428: cada jefe con su nombre y su arma: SKULL 250 de vida y 8 perdigones; SNAKE 300 y 10 más rápidos; NOVA 400 y 8 de 8.
            string[] nombresJefe = { "JEFE SKULL", "LA CULEBRA", "CAPITÁN NOVA" };
            float[] vidasJefe = { 250f, 300f, 400f };
            int[] perdigonesJefe = { 8, 10, 8 };
            float[] danioJefe = { 6f, 6f, 8f };
            float[] recargaJefe = { 1.6f, 1.4f, 1.2f };
            int[] dineroJefe = { 300, 400, 600 };
            for (int i = 0; i < ids.Length; i++)
            {
                Transform manzana = BuscarTransform(manzanas[i]);
                if (manzana == null || prefabPandillero == null)
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T404: sin " + manzanas[i] + " o sin el prefab del pandillero.");
                    continue;
                }
                Vector3 punto = ElegirPuntoDeBanqueta(manzana.position, puertas, out Vector3 tangente);
                punto += tangente * DesvioJefe;
                if (Physics.Raycast(punto + Vector3.up * AlturaBusquedaPiso, Vector3.down, out RaycastHit piso, AlturaBusquedaPiso * 2f))
                {
                    punto = piso.point;
                }
                DatosPandilleroAPie datosJefe = CrearDatosJefe(rutasJefe[i], baseDatos[i], nombresJefe[i], vidasJefe[i], perdigonesJefe[i], danioJefe[i], recargaJefe[i], dineroJefe[i]);
                GameObject objetoJefe = new GameObject("PuntoJefe_" + ids[i]);
                objetoJefe.transform.SetParent(raiz.transform, false);
                objetoJefe.transform.position = punto;
                GameObject pandillero = (GameObject)PrefabUtility.InstantiatePrefab(prefabPandillero, objetoJefe.transform);
                pandillero.name = "Jefe";
                pandillero.transform.SetPositionAndRotation(punto, Quaternion.identity);
                SerializedObject soP = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datosJefe;
                soP.ApplyModifiedPropertiesWithoutUndo();
                pandillero.SetActive(false);
                JefePandilla jefe = objetoJefe.AddComponent<JefePandilla>();
                SerializedObject soJ = new SerializedObject(jefe);
                soJ.FindProperty("zona").stringValue = ids[i];
                soJ.FindProperty("pandilla").enumValueIndex = (int)pandillas[i];
                soJ.FindProperty("jefe").objectReferenceValue = pandillero.GetComponent<PandilleroAPie>();
                soJ.ApplyModifiedPropertiesWithoutUndo();
            }
            Transform m2 = BuscarTransform("Manzana_2");
            Debug.Log("[Progresion] CreadorCiudadPandillas.T404: zonas, HUD, mapa y jefes listos; Manzana_2 en " + (m2 != null ? m2.position.ToString() : "?") + ".");
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-406 Historia")]
        public static void T406()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T406Interno();
            T407Interno();
            Guardar(escena);
        }

        private const string RutaPandilleroPunos = "Assets/_Proyecto/IA/Prefabs/PandilleroAPie.prefab";
        private const string RutaDatosPunos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Base.asset";
        private const string RutaMaterialLupe = "Assets/_Proyecto/Progresion/Prologo/Materiales/Vecina_DonaLupe.mat";
        private const float DistanciaPelea = 10f;
        private const float RadioSalidaCasa = 4f; // m a la puerta: la caminata desde la cama termina a unos 2.8 m de ella

        // T-406: el director de la historia, DOÑA LUPE y los 2 Calaveras de la primera pelea. Apaga los grupos de Calaveras del tianguis
        // (el director los prende en su paso), deja al CHATO solo para la historia y quita la pistola del inicio del jugador.
        private static void T406Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT406);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT406);
            Transform puerta = BuscarTransform(IdAparicion);
            Vector3 centro = puerta != null ? puerta.position + puerta.forward * DistanciaPelea : Vector3.zero;
            Vector3 lateral = puerta != null ? puerta.right : Vector3.right;
            if (Physics.Raycast(centro + Vector3.up * 20f, Vector3.down, out RaycastHit piso, 60f))
            {
                centro = piso.point;
            }

            // DOÑA LUPE: un muñeco humano quieto, de color de vecina.
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloHumano);
            GameObject lupe = new GameObject("DonaLupe");
            lupe.transform.SetParent(raiz.transform, false);
            lupe.transform.position = centro;
            if (modelo != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo, lupe.transform);
                visual.transform.localPosition = Vector3.zero;
                Material vecina = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialLupe);
                if (vecina == null)
                {
                    vecina = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    vecina.SetColor("_BaseColor", new Color(0.95f, 0.55f, 0.15f));
                    AssetDatabase.CreateAsset(vecina, RutaMaterialLupe);
                }
                foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = vecina;
                }
            }

            // Los 2 Calaveras a puños, entre la puerta y ella.
            GameObject prefabPunos = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilleroPunos);
            DatosPandilleroAPie datosPunos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPunos);
            PandilleroAPie[] calaveras = new PandilleroAPie[2];
            for (int i = 0; i < 2; i++)
            {
                Vector3 posicion = centro + lateral * (i == 0 ? -1.5f : 1.5f) + (puerta != null ? -puerta.forward : Vector3.back) * 1.5f;
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefabPunos, raiz.transform);
                c.name = "CalaveraPunos_" + (i + 1);
                c.transform.SetPositionAndRotation(posicion, Quaternion.identity);
                SerializedObject soC = new SerializedObject(c.GetComponent<PandilleroAPie>());
                soC.FindProperty("datos").objectReferenceValue = datosPunos;
                soC.ApplyModifiedPropertiesWithoutUndo();
                c.SetActive(false);
                calaveras[i] = c.GetComponent<PandilleroAPie>();
            }

            // El CHATO queda solo para la historia.
            Reclutable chato = null;
            foreach (Reclutable r in Object.FindObjectsByType<Reclutable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.Nombre == "EL CHATO")
                {
                    chato = r;
                    SerializedObject soR = new SerializedObject(r);
                    soR.FindProperty("soloHistoria").boolValue = true;
                    soR.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Grupos de Calaveras (los del tianguis): apagados.
            List<GameObject> grupos = new List<GameObject>();
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.Pandilla == IdPandilla.Calaveras)
                {
                    g.gameObject.SetActive(false);
                    grupos.Add(g.gameObject);
                }
            }

            // El jugador no trae la pistola al inicio.
            int arsenales = 0;
            foreach (MechaCyberpunk.Personaje.Jugador.ArsenalJugador a in Object.FindObjectsByType<MechaCyberpunk.Personaje.Jugador.ArsenalJugador>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject soA = new SerializedObject(a);
                soA.FindProperty("pistolaAlInicio").boolValue = false;
                soA.ApplyModifiedPropertiesWithoutUndo();
                arsenales++;
            }

            MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas director = raiz.AddComponent<MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas>();
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("puertaCasa").objectReferenceValue = puerta;
            so.FindProperty("donaLupe").objectReferenceValue = lupe.transform;
            SerializedProperty lista = so.FindProperty("calaveras");
            lista.arraySize = calaveras.Length;
            for (int i = 0; i < calaveras.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = calaveras[i];
            }
            so.FindProperty("chato").objectReferenceValue = chato;
            so.FindProperty("radioSalida").floatValue = RadioSalidaCasa;
            SerializedProperty gl = so.FindProperty("gruposTianguis");
            gl.arraySize = grupos.Count;
            for (int i = 0; i < grupos.Count; i++)
            {
                gl.GetArrayElementAtIndex(i).objectReferenceValue = grupos[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Progresion] CreadorCiudadPandillas.T406: director, Lupe en " + centro + ", " + grupos.Count + " grupos apagados, "
                + arsenales + " arsenal(es) sin pistola, chato " + (chato != null ? "ok" : "NO"));
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-405 Aliados")]
        public static void T405()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T405Interno();
            Guardar(escena);
        }

        private const string RutaModeloHumano = "Assets/Arte/Personajes/MunecoHumano/ModeloHumano.prefab";

        // T-405: los 3 reclutables de LA FOSA (frente a la casa, en la esquina y junto al taller) y la pandilla del jugador con su HUD.
        private static void T405Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT405);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject prefabAliado = CreadorAliado.Crear();
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloHumano);
            GameObject raiz = new GameObject(RaizT405);
            PandillaJugador pandilla = raiz.AddComponent<PandillaJugador>();
            SerializedObject soPandilla = new SerializedObject(pandilla);
            soPandilla.FindProperty("prefabAliado").objectReferenceValue = prefabAliado;
            soPandilla.ApplyModifiedPropertiesWithoutUndo();
            raiz.AddComponent<MechaCyberpunk.UI.Hud.HudAliados>();

            Transform puerta = BuscarTransform(IdAparicion);
            Vector3 centro = puerta != null ? puerta.position : Vector3.zero;
            string[] nombres = { "EL CHATO", "LA GÜERA", "EL TOPO" };
            int[] costos = { 0, 200, 350 };
            int[] respetos = { 10, 40, 70 };
            Vector3[] desvios = { new Vector3(3f, 0f, 3f), new Vector3(-7f, 0f, -5f), new Vector3(-3f, 0f, -9f) };
            for (int i = 0; i < nombres.Length; i++)
            {
                Vector3 punto = centro + desvios[i];
                if (Physics.Raycast(punto + Vector3.up * 4f, Vector3.down, out RaycastHit piso, 10f))
                {
                    punto = piso.point;
                }
                GameObject objeto = new GameObject("Reclutable_" + nombres[i]);
                objeto.transform.SetParent(raiz.transform, false);
                objeto.transform.position = punto;
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo, objeto.transform);
                visual.name = "Modelo";
                visual.transform.localPosition = Vector3.zero;
                Reclutable reclutable = objeto.AddComponent<Reclutable>();
                reclutable.Configurar(nombres[i], costos[i], respetos[i], "fosa");
                SerializedObject so = new SerializedObject(reclutable);
                so.FindProperty("visual").objectReferenceValue = visual;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Progresion] T405 reclutable " + nombres[i] + " en " + punto);
            }
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-403 Cuatro armas")]
        public static void T403()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T403Interno();
            Guardar(escena);
        }

        private const string TipoCreadorArmas = "MechaCyberpunk.Personaje.Editor.CreadorArmas, MechaCyberpunk.Personaje.Editor";
        private const string RutaMostradorMercado = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";
        private const string RutaLugarMercado = "Assets/_Proyecto/Mundo/Datos/DatosLugar_MercadoNegro.asset";

        // T-403: crea las armas y el catálogo (y regenera el prefab del jugador), abre el mercado negro siempre y se asegura de que haya un
        // mostrador. El interior de Alan (Barrio.prefab) ya trae el MostradorMercadoNegro; si no hay ninguno, va uno bajo GTA_T403.
        private static void T403Interno()
        {
            System.Type tipoArmas = System.Type.GetType(TipoCreadorArmas);
            if (tipoArmas == null)
            {
                Debug.LogError("[Progresion] CreadorCiudadPandillas.T403: no encuentro CreadorArmas.");
            }
            else
            {
                tipoArmas.GetMethod("Crear").Invoke(null, null);
            }

            // Horario Siempre y sin cerrar en la demo (D-tony-216).
            Object lugar = AssetDatabase.LoadAssetAtPath<Object>(RutaLugarMercado);
            if (lugar != null)
            {
                SerializedObject so = new SerializedObject(lugar);
                so.FindProperty("horario.siempreAbierto").boolValue = true;
                so.FindProperty("cerradoEnDemo").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(lugar);
            }

            GameObject anterior = BuscarRaiz(RaizT403);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT403);
            bool hay = false;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "MostradorTienda" && mb.name.Contains("MercadoNegro"))
                {
                    hay = true;
                }
            }
            if (!hay)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostradorMercado);
                Transform mercado = BuscarTransform("Lugar_MercadoNegro");
                if (prefab != null && mercado != null)
                {
                    GameObject mostrador = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    mostrador.transform.SetPositionAndRotation(mercado.position + mercado.forward * 3f, mercado.rotation * Quaternion.Euler(0f, 180f, 0f));
                }
                else
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T403: sin Lugar_MercadoNegro o sin el prefab del mostrador.");
                }
            }
            Debug.Log("[Progresion] CreadorCiudadPandillas.T403: armas creadas, mercado negro siempre abierto, mostrador " + (hay ? "ya estaba (Barrio)" : "agregado") + ".");
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-402 Pandilleros armados")]
        public static void T402()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T402Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-401 Carros para robar")]
        public static void T401()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T401Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-400 Tercera persona")]
        public static void T400()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T400Interno();
            Guardar(escena);
        }

        private static void Guardar(Scene escena)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
        }

        private static void T400Interno()
        {
            // 1. El mecha, la emboscada y los directores del prólogo se apagan (D-tony-206). La ayuda de llamar al mecha no sale
            // sola: necesita al mecha en su taller, y está apagado.
            foreach (string nombre in ApagadosT400)
            {
                GameObject objeto = BuscarRaiz(nombre);
                if (objeto != null)
                {
                    objeto.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas: no encuentro " + nombre + " en Ciudad_Prologo.");
                }
            }

            // 2. Raíz propia, rehecha cada vez.
            GameObject anterior = BuscarRaiz(RaizT400);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT400);

            // La historia arranca en Libre para que nada mande al título ni al mecha.
            raiz.AddComponent<HistoriaLibreDePrueba>();

            // 3. Dos muñecos de prueba delante de la casa, para pegarles.
            Transform casa = BuscarTransform(IdAparicion);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuneco);
            if (casa != null && prefab != null)
            {
                float[] distancias = { DistanciaMuneco1, DistanciaMuneco2 };
                for (int i = 0; i < distancias.Length; i++)
                {
                    GameObject muneco = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    muneco.name = "MunecoPrueba_" + (i + 1);
                    Vector3 posicion = casa.position + casa.forward * distancias[i];
                    if (Physics.Raycast(posicion + Vector3.up * 20f, Vector3.down, out RaycastHit piso, 60f))
                    {
                        posicion = piso.point;
                    }
                    muneco.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(-casa.forward, Vector3.up));
                }
            }
            else
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: falta " + IdAparicion + " o el prefab del muñeco.");
            }

            // 4. Hora inicial 19:00 con una copia de la config del reloj, para no tocar el asset de Alan.
            PonerHoraInicial();
            Debug.Log("[Progresion] CreadorCiudadPandillas.T400: " + RaizT400 + " lista, mecha apagado, hora " + HoraInicial + ".");
        }

        // T-401: 6 carros de adorno (2 por zona: oeste, centro, este por la X) se apagan y en su lugar van CarroViejo robables.
        private static void T401Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT401);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroViejo);
            if (prefab == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: falta " + RutaCarroViejo);
                return;
            }
            GameObject raiz = new GameObject(RaizT401);
            Transform casa = BuscarTransform(IdAparicion);
            Vector3 referencia = casa != null ? casa.position : Vector3.zero;

            // Los adornos son los Carro_N de la escena (los apagados por una corrida anterior también cuentan).
            System.Collections.Generic.List<Transform> adornos = new System.Collections.Generic.List<Transform>();
            foreach (GameObject objetoRaiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in objetoRaiz.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^Carro_\d+$") && !t.IsChildOf(raiz.transform))
                    {
                        adornos.Add(t);
                    }
                }
            }
            if (adornos.Count < 6)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: solo hay " + adornos.Count + " Carro_N de adorno.");
            }
            adornos.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            int tercio = Mathf.Max(1, adornos.Count / 3);
            Color[] tintes =
            {
                new Color(0.1f, 0.35f, 0.7f), new Color(0.75f, 0.7f, 0.15f), new Color(0.15f, 0.55f, 0.3f),
                new Color(0.55f, 0.15f, 0.55f), new Color(0.85f, 0.85f, 0.85f), new Color(0.08f, 0.08f, 0.1f)
            };
            int numero = 0;
            for (int zona = 0; zona < 3; zona++)
            {
                int desde = zona * tercio;
                int hasta = zona == 2 ? adornos.Count : Mathf.Min(adornos.Count, desde + tercio);
                System.Collections.Generic.List<Transform> candidatos = adornos.GetRange(Mathf.Min(desde, adornos.Count), Mathf.Max(0, hasta - desde));
                candidatos.Sort((a, b) => (a.position - referencia).sqrMagnitude.CompareTo((b.position - referencia).sqrMagnitude));
                for (int i = 0; i < 2 && i < candidatos.Count; i++)
                {
                    Transform adorno = candidatos[i];
                    GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    carro.name = "CarroRobable_" + (numero + 1);
                    carro.transform.SetPositionAndRotation(adorno.position, adorno.rotation);
                    // Vehiculos es de otra área: el componente se busca por nombre en vez de referenciar su asmdef.
                    System.Type tipoEstacionado = System.Type.GetType(TipoCarroEstacionado);
                    if (tipoEstacionado == null)
                    {
                        Debug.LogError("[Progresion] CreadorCiudadPandillas: no encuentro CarroEstacionado.");
                        break;
                    }
                    Component estacionado = carro.GetComponent(tipoEstacionado);
                    if (estacionado == null)
                    {
                        estacionado = carro.AddComponent(tipoEstacionado);
                    }
                    SerializedObject so = new SerializedObject(estacionado);
                    so.FindProperty("tinte").colorValue = tintes[numero % tintes.Length];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    adorno.gameObject.SetActive(false);
                    Transform visual = adorno.parent != null ? adorno.parent.Find(adorno.name + "_Visual_T250") : null;
                    if (visual != null)
                    {
                        visual.gameObject.SetActive(false);
                    }
                    numero++;
                }
            }
            Debug.Log("[Progresion] CreadorCiudadPandillas.T401: " + numero + " carros robables en " + RaizT401 + ".");
        }


        // ---------- T-402: pandilleros armados ----------

        private const string RutaDatosDoctor = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Pandillas.asset";
        private const string RutaPrefabDoctor = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DoctorClinica.prefab";
        private const string TipoPuntoAparicion = "MechaCyberpunk.Mundo.Lugares.PuntoAparicion, MechaCyberpunk.Mundo";
        private const int CobroDoctor = 100;
        private const float HorasDormido = 2f;
        private const float DistanciaMinPuertas = 45f;
        private const float DesplazamientoBorde = 36f;
        private static readonly float[] Desplazamientos = { 38f, 42f, 46f };
        private const float AlturaRayoCalle = 3f;
        private const float PasoBorde = 6f;
        private const float SeparacionPandilleros = 1.8f;
        private const float AlturaBusquedaPiso = 40f;
        private const float AlturaMaxBanqueta = 0.6f;
        private const float RadioLibre = 0.35f;

        private struct GrupoT402
        {
            public string Manzana;
            public IdPandilla Pandilla;
            public int Cantidad;
        }

        private static void T402Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT402);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            CreadorPandilleroArmado.Crear();

            // Los muñecos de prueba de T-400 se apagan (nunca se borran).
            GameObject t400 = BuscarRaiz(RaizT400);
            if (t400 != null)
            {
                foreach (Transform hijo in t400.transform)
                {
                    if (hijo.name.StartsWith("MunecoPrueba_"))
                    {
                        hijo.gameObject.SetActive(false);
                    }
                }
            }

            GameObject raiz = new GameObject(RaizT402);
            GameObject prefabPandillero = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPandilleroArmado.RutaPrefab);
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosCalaveras);
            DatosPandilleroAPie viboras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosViboras);
            if (prefabPandillero == null || calaveras == null || viboras == null)
            {
                Debug.LogError("[Progresion] CreadorCiudadPandillas.T402: faltan el prefab o los datos del pandillero armado.");
                return;
            }

            List<Vector3> puertas = new List<Vector3>();
            foreach (GameObject objetoRaiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in objetoRaiz.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name == "Puerta" || t.name == "Casa_Puerta")
                    {
                        puertas.Add(t.position);
                    }
                }
            }

            GrupoT402[] grupos =
            {
                new GrupoT402 { Manzana = "Manzana_2", Pandilla = IdPandilla.Calaveras, Cantidad = 3 },
                new GrupoT402 { Manzana = "Manzana_5", Pandilla = IdPandilla.Calaveras, Cantidad = 2 },
                new GrupoT402 { Manzana = "Manzana_3", Pandilla = IdPandilla.Viboras, Cantidad = 3 },
                new GrupoT402 { Manzana = "Manzana_6", Pandilla = IdPandilla.Nova, Cantidad = 3 },
            };
            int total = 0;
            for (int g = 0; g < grupos.Length; g++)
            {
                Transform manzana = BuscarTransform(grupos[g].Manzana);
                if (manzana == null)
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T402: no encuentro " + grupos[g].Manzana);
                    continue;
                }
                Vector3 punto = ElegirPuntoDeBanqueta(manzana.position, puertas, out Vector3 tangente);
                GameObject objetoGrupo = new GameObject("Pandilla_" + grupos[g].Pandilla + "_" + (g + 1));
                objetoGrupo.transform.SetParent(raiz.transform, false);
                objetoGrupo.transform.position = punto;
                DatosPandilleroAPie datos = grupos[g].Pandilla == IdPandilla.Calaveras ? calaveras : grupos[g].Pandilla == IdPandilla.Nova ? AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosNova) : viboras;
                for (int i = 0; i < grupos[g].Cantidad; i++)
                {
                    GameObject pandillero = (GameObject)PrefabUtility.InstantiatePrefab(prefabPandillero, objetoGrupo.transform);
                    pandillero.name = "Pandillero_" + (i + 1);
                    float desvio = (i - (grupos[g].Cantidad - 1) * 0.5f) * SeparacionPandilleros;
                    Vector3 posicion = punto + tangente * desvio;
                    if (Physics.Raycast(posicion + Vector3.up * AlturaBusquedaPiso, Vector3.down, out RaycastHit piso, AlturaBusquedaPiso * 2f))
                    {
                        posicion = piso.point;
                    }
                    pandillero.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(Vector3.zero - new Vector3(posicion.x, 0f, posicion.z) + Vector3.forward * 0.001f, Vector3.up));
                    SerializedObject soP = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
                    soP.FindProperty("datos").objectReferenceValue = datos;
                    soP.ApplyModifiedPropertiesWithoutUndo();
                    total++;
                }
                PandillaAPie pandilla = objetoGrupo.AddComponent<PandillaAPie>();
                SerializedObject soG = new SerializedObject(pandilla);
                soG.FindProperty("datos").objectReferenceValue = datos;
                soG.FindProperty("idPandilla").enumValueIndex = (int)grupos[g].Pandilla;
                soG.ApplyModifiedPropertiesWithoutUndo();
            }

            CrearDoctor(raiz.transform);
            Debug.Log("[Progresion] CreadorCiudadPandillas.T402: " + total + " pandilleros armados en " + grupos.Length + " grupos, doctor en la clínica.");
        }

        // Recorre el borde de la manzana (la banqueta) y elige el punto libre más lejos de todas las puertas.
        private static Vector3 ElegirPuntoDeBanqueta(Vector3 centro, List<Vector3> puertas, out Vector3 tangente)
        {
            Physics.SyncTransforms();
            int sinPiso = 0, alto = 0, ocupado = 0;
            Vector3 mejor = centro;
            Vector3 mejorTangente = Vector3.right;
            float mejorPuntaje = -1f;
            foreach (float d in Desplazamientos)
            for (float u = -DesplazamientoBorde; u <= DesplazamientoBorde + 0.01f; u += PasoBorde)
            {
                Vector3[] candidatos =
                {
                    centro + new Vector3(u, 0f, d), centro + new Vector3(u, 0f, -d),
                    centro + new Vector3(d, 0f, u), centro + new Vector3(-d, 0f, u)
                };
                Vector3[] tangentes = { Vector3.right, Vector3.right, Vector3.forward, Vector3.forward };
                for (int k = 0; k < candidatos.Length; k++)
                {
                    if (!Physics.Raycast(candidatos[k] + Vector3.up * AlturaRayoCalle, Vector3.down, out RaycastHit piso, AlturaRayoCalle * 2f)
                        || piso.point.y > AlturaMaxBanqueta)
                    {
                        if (piso.collider != null) { alto++; } else { sinPiso++; }
                        continue;
                    }
                    Vector3 p = piso.point;
                    if (Physics.CheckCapsule(p + Vector3.up * RadioLibre * 1.2f, p + Vector3.up * 1.6f, RadioLibre))
                    {
                        ocupado++;
                        continue;
                    }
                    float minPuerta = float.MaxValue;
                    foreach (Vector3 puerta in puertas)
                    {
                        minPuerta = Mathf.Min(minPuerta, Vector3.Distance(p, puerta));
                    }
                    float puntaje = Mathf.Min(minPuerta, DistanciaMinPuertas);
                    if (puntaje > mejorPuntaje)
                    {
                        mejorPuntaje = puntaje;
                        mejor = p;
                        mejorTangente = tangentes[k];
                    }
                }
            }
            if (mejorPuntaje < DistanciaMinPuertas)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T402: el mejor punto de " + centro + " queda a " + mejorPuntaje + " m de una puerta (sin piso " + sinPiso + ", alto " + alto + ", ocupado " + ocupado + ").");
            }
            tangente = mejorTangente;
            return mejor;
        }

        private static void CrearDoctor(Transform padre)
        {
            DatosDoctor datos = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatosDoctor);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosDoctor>();
                AssetDatabase.CreateAsset(datos, RutaDatosDoctor);
                SerializedObject soD = new SerializedObject(datos);
                soD.FindProperty("cobro").intValue = CobroDoctor;
                soD.FindProperty("horasInconsciente").floatValue = HorasDormido;
                soD.FindProperty("textoDoctor").stringValue = "EL DOC: Otra vez tú. Son cien, mijo.";
                soD.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
            }
            CreadorDoctorClinica.Crear();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabDoctor);
            if (prefab != null)
            {
                GameObject doctor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                SerializedObject so = new SerializedObject(doctor.GetComponent<DoctorClinica>());
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // La camilla: un punto de aparición en la entrada de la clínica (en Mundo no existe uno con ese id).
            Transform entrada = BuscarTransform("EntradaClinica");
            System.Type tipo = System.Type.GetType(TipoPuntoAparicion);
            if (entrada == null || tipo == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T402: sin EntradaClinica o PuntoAparicion; el doctor usará la cama de casa.");
                return;
            }
            GameObject camilla = new GameObject("Clinica_Camilla");
            camilla.transform.SetParent(padre, false);
            camilla.transform.SetPositionAndRotation(entrada.position, entrada.rotation);
            Component punto = camilla.AddComponent(tipo);
            SerializedObject soPunto = new SerializedObject(punto);
            soPunto.FindProperty("id").stringValue = "Clinica_Camilla";
            soPunto.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PonerHoraInicial()
        {
            Component reloj = null;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (MonoBehaviour comportamiento in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comportamiento != null && comportamiento.GetType().Name == "RelojMundo")
                    {
                        reloj = comportamiento;
                    }
                }
            }
            if (reloj == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: no encuentro RelojMundo en la escena.");
                return;
            }
            SerializedObject soReloj = new SerializedObject(reloj);
            SerializedProperty propiedad = soReloj.FindProperty("config");
            ScriptableObject original = propiedad.objectReferenceValue as ScriptableObject;
            if (original == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: RelojMundo sin config.");
                return;
            }
            ScriptableObject copia = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaRelojPandillas);
            if (copia == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaRelojPandillas));
                copia = Object.Instantiate(original);
                AssetDatabase.CreateAsset(copia, RutaRelojPandillas);
            }
            SerializedObject soCopia = new SerializedObject(copia);
            soCopia.FindProperty("horaInicial").floatValue = HoraInicial;
            soCopia.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copia);
            propiedad.objectReferenceValue = copia;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuscarRaiz(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (raiz.name == nombre)
                {
                    return raiz;
                }
            }
            return null;
        }

        private static Transform BuscarTransform(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            return null;
        }
    }
}
