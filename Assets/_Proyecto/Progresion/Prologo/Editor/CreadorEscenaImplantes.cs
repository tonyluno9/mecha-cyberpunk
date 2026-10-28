using System;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MechaCyberpunk.UI.Historia;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Genera Pruebas_Implantes.unity (T-193), la escena de todo el bloque de implantes: la clínica con su camilla y su espejo, una pista a pie
    /// con tres cajas de salto calculadas desde los assets, tres muñecos, el taller con el mecha, la arena con el mostrador de peleas, la casa y
    /// un sitio vacío para la pandilla. Idempotente: si lo que se arma es igual a lo que ya hay, no se guarda de nuevo. Las tareas siguientes
    /// (T-194 a T-196) agregan su parte DENTRO de <see cref="Crear"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaImplantes.Crear
    /// </summary>
    public static class CreadorEscenaImplantes
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Implantes.unity";
        public const string IdInicio = "Implantes_Inicio";
        public const string IdCamilla = "Clinica_Camilla";
        public const string IdCasaCama = CreadorEscenaPrologo.IdCasaCama;
        public const string NombreSitioPandilla = "Sitio_Pandilla";

        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaMuneco = "Assets/_Proyecto/Personaje/Pruebas/MunecoAPie.prefab";
        private const string RutaDoctor = CreadorDoctorClinica.RutaPrefab;
        private const string RutaPandilla = "Assets/_Proyecto/IA/Prefabs/PandillaAPie.prefab";
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaMostradorPeleas = CreadorMostradorPeleas.RutaPrefab;
        private const string RutaMostradorClinica = "Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab";
        private const string RutaEspejo = CreadorCamaEspejo.RutaPrefabEspejo;
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaDatosPersonaje = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Implantes/CatalogoImplantes_Base.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales/";
        private const string RutaCaptura = "docs/capturas/T-193-2.png";
        private const float HoraInicial = 10f;
        private const float HoraAnochecer = 20f;
        private const float HoraAbreClinica = 8f;
        private const float HoraCierraClinica = 20f;

        // Medidas en metros (1 unidad = 1 m). El piso va de x -40 a 40 y de z -30 a 30.
        private const float AnchoPiso = 80f;
        private const float LargoPiso = 60f;
        private const float AlturaMuro = 4f;
        private const float GrosorMuro = 0.4f;
        private const float AnchoCalle = 8f;
        private const float AlturaLetrero = 3f;
        private const float TamanoLetra = 0.5f;
        private const float TamanoLetraPiso = 0.9f;
        private const float LargoPista = 30f;
        private const float SeparacionMarcas = 5f;
        private const float LadoCajaSalto = 2f;
        private const float SeparacionCajasSalto = 6f;

        public static readonly Vector3 PosicionInicio = new Vector3(-25f, 0f, 8f);
        public static readonly Vector3 PosicionClinica = new Vector3(-25f, 0f, 17f);
        public static readonly Vector3 PosicionCamilla = new Vector3(-30f, 0f, 14f);
        public static readonly Vector3 PosicionAparicionCamilla = new Vector3(-30f, 0f, 11.5f);
        public static readonly Vector3 PosicionEspejo = new Vector3(-20f, 0f, 12f);
        public static readonly Vector3 PosicionPista = new Vector3(-35f, 0f, -12f);
        public static readonly Vector3 PosicionCajasSalto = new Vector3(-30f, 0f, -22f);
        public static readonly Vector3 PosicionMunecos = new Vector3(5f, 0f, -12f);
        public static readonly Vector3 PosicionTaller = new Vector3(25f, 0f, -20f);
        public static readonly Vector3 PosicionMostradorPeleas = new Vector3(25f, 0f, 15f);
        public static readonly Vector3 PosicionSalidaArena = new Vector3(25f, 0f, 12f);
        public static readonly Vector3 PosicionArena = new Vector3(100f, 0f, 100f);
        public static readonly Vector3 PosicionCasa = new Vector3(5f, 0f, 20f);
        public static readonly Vector3 PosicionSitioPandilla = new Vector3(20f, 0f, 0f);

        private static Material matPiso;
        private static Material matAsfalto;
        private static Material matMuro;
        private static Material matClinica;
        private static Material matCamilla;
        private static Material matPista;
        private static Material matCajas;
        private static Material matTaller;
        private static Material matBanco;
        private static Material matPeleas;
        private static Material matCasa;

        [MenuItem("Mecha Cyberpunk/Crear escena Pruebas_Implantes")]
        public static void Crear()
        {
            CargarMateriales();
            CreadorDoctorClinica.Crear();

            string firmaActual = null;
            if (File.Exists(RutaEscena))
            {
                firmaActual = CreadorEscenaPrologo.Firma(EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single));
            }

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ArmarMundo();
            ArmarClinica();
            ArmarPista();
            ArmarTaller();
            ArmarArena();
            ArmarCasaYPandilla();
            ArmarJugadorYServicios();

            string firmaNueva = CreadorEscenaPrologo.Firma(escena);
            if (firmaActual != null && firmaActual == firmaNueva)
            {
                Debug.Log("[Progresion] Pruebas_Implantes ya estaba completa; no se guarda de nuevo.");
                return;
            }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] Pruebas_Implantes creada.");
        }

        /// <summary>
        /// Las alturas (m) de las tres cajas de salto: el alcance real del jugador es salto + stepOffset, asi que cada caja queda a medio camino entre
        /// el alcance del nivel anterior y el del suyo (base, piernas I, piernas II), sumando el escalon a las dos ultimas; calculado con la altura de salto de DatosPersonaje y la tabla de implantes.
        /// </summary>
        public static float[] AlturasCajasSalto()
        {
            DatosPersonaje datos = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatosPersonaje);
            CatalogoImplantes catalogo = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            float baseSalto = datos.AlturaSalto;
            float nivel1 = baseSalto * catalogo.Buscar(EspacioImplante.Piernas, 1).Efectos.multiplicadorSalto;
            float nivel2 = baseSalto * catalogo.Buscar(EspacioImplante.Piernas, 2).Efectos.multiplicadorSalto;
            GameObject jugador = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);
            float escalon = jugador.GetComponent<CharacterController>().stepOffset;
            float[] alturas = { baseSalto / 2f + escalon / 2f, (baseSalto + nivel1) / 2f + escalon, (nivel1 + nivel2) / 2f + escalon };
            if (alturas[0] <= escalon)
            {
                throw new InvalidOperationException("La caja base no supera el stepOffset del jugador.");
            }
            return alturas;
        }

        /// <summary>
        /// Captura aérea (T-193-2) en memoria con Camera.Render, sin guardar la escena.
        /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaImplantes.CapturarVistaAerea
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Capturar vista aérea de Pruebas_Implantes")]
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
            camara.orthographicSize = 31f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 100f;
            objetoCamara.transform.SetPositionAndRotation(new Vector3(0f, 40f, 0f), Quaternion.Euler(90f, 0f, 0f));

            const int ancho = 1800;
            const int alto = 1200;
            RenderTexture destino = new RenderTexture(ancho, alto, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
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
            componenteLuz.color = new Color(0.6f, 0.65f, 0.9f);
            componenteLuz.intensity = 0.8f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CreadorEscenaPrologo.Caja("Piso", new Vector3(0f, -0.5f, 0f), new Vector3(AnchoPiso, 1f, LargoPiso), matPiso, true);
            float mitadX = AnchoPiso / 2f;
            float mitadZ = LargoPiso / 2f;
            CreadorEscenaPrologo.Caja("Muro sur", new Vector3(0f, AlturaMuro / 2f, -mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro norte", new Vector3(0f, AlturaMuro / 2f, mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro oeste", new Vector3(-mitadX, AlturaMuro / 2f, 0f), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro este", new Vector3(mitadX, AlturaMuro / 2f, 0f), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);
            // La calle este-oeste del medio (sin collider).
            CreadorEscenaPrologo.Caja("Calle este-oeste", new Vector3(0f, 0.01f, 0f), new Vector3(AnchoPiso, 0.02f, AnchoCalle), matAsfalto, false);
        }

        private static void ArmarClinica()
        {
            CreadorEscenaPrologo.ArmarLugar("Lugar Clinica", IdLugar.Clinica, "Clínica del Doc", PosicionClinica, new Vector3(12f, 3f, 10f), PosicionClinica + new Vector3(0f, 0f, -4f));
            // Horario de prueba: abierto de 8:00 a 20:00 (así se puede probar "cerrada").
            SerializedObject soLugar = new SerializedObject(GameObject.Find("Lugar Clinica").GetComponent<MechaCyberpunk.Personaje.Pruebas.LugarDePrueba>());
            soLugar.FindProperty("horario.abre").floatValue = HoraAbreClinica;
            soLugar.FindProperty("horario.cierra").floatValue = HoraCierraClinica;
            soLugar.FindProperty("horario.siempreAbierto").boolValue = false;
            soLugar.ApplyModifiedPropertiesWithoutUndo();

            CreadorEscenaPrologo.Caja("Clínica suelo", new Vector3(PosicionClinica.x, 0.03f, PosicionClinica.z - 1f), new Vector3(11f, 0.02f, 8f), matClinica, false);
            GameObject mostrador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostradorClinica));
            mostrador.transform.position = PosicionClinica;
            Letrero("Letrero CLINICA", "CLÍNICA", PosicionClinica + new Vector3(0f, 0f, 3.5f));

            CreadorEscenaPrologo.Caja("Camilla", PosicionCamilla + new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.6f, 2f), matCamilla, true);
            CreadorEscenaPrologo.Aparicion(IdCamilla, ModoJugador.APie, PosicionAparicionCamilla, Quaternion.identity);

            GameObject espejo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaEspejo));
            espejo.transform.position = PosicionEspejo;

            // El doctor (T-196): atiende las caídas en la calle.
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDoctor));
        }

        private static void ArmarPista()
        {
            // Una recta de 30 m (a lo largo de +X) marcada cada 5 m, con su número en el piso.
            CreadorEscenaPrologo.Caja("Pista", PosicionPista + new Vector3(LargoPista / 2f, 0.02f, 0f), new Vector3(LargoPista, 0.02f, 3f), matPista, false);
            for (float metro = 0f; metro <= LargoPista + 0.01f; metro += SeparacionMarcas)
            {
                Vector3 marca = PosicionPista + new Vector3(metro, 0.03f, 0f);
                CreadorEscenaPrologo.Caja("Marca " + metro.ToString("0") + " m", marca, new Vector3(0.2f, 0.02f, 3f), matBanco, false);
                TextoPiso("Texto marca " + metro.ToString("0"), metro.ToString("0") + " m", marca + new Vector3(0f, 0.02f, -2.2f), 0.5f);
            }
            Letrero("Letrero PISTA", "PISTA 30 m", PosicionPista + new Vector3(LargoPista / 2f, 0f, -3f));

            // Tres cajas de salto: la que se sube con el salto base, la que solo se sube con PIERNAS I y la que solo con PIERNAS II.
            float[] alturas = AlturasCajasSalto();
            string[] nombres = { "BASE", "PIERNAS I", "PIERNAS II" };
            for (int i = 0; i < alturas.Length; i++)
            {
                Vector3 centro = PosicionCajasSalto + new Vector3(i * SeparacionCajasSalto, alturas[i] / 2f, 0f);
                CreadorEscenaPrologo.Caja("Caja salto " + nombres[i], centro, new Vector3(LadoCajaSalto, alturas[i], LadoCajaSalto), matCajas, true);
                Letrero("Letrero " + nombres[i], nombres[i], new Vector3(centro.x, 0f, PosicionCajasSalto.z - 2.5f));
            }

            GameObject prefabMuneco = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuneco);
            for (int i = 0; i < 3; i++)
            {
                GameObject muneco = (GameObject)PrefabUtility.InstantiatePrefab(prefabMuneco);
                muneco.transform.position = PosicionMunecos + new Vector3(i * 3f, 0f, 0f);
                muneco.name = "Muneco " + (i + 1);
            }
        }

        private static void ArmarTaller()
        {
            // El estacionamiento mira al este (+X); el banco queda a 3 m del mecha.
            Quaternion rotacion = Quaternion.Euler(0f, 90f, 0f);
            GameObject taller = new GameObject("Taller");
            taller.transform.SetPositionAndRotation(PosicionTaller, rotacion);
            TallerDePrueba contrato = taller.AddComponent<TallerDePrueba>();

            Vector3 banco = contrato.PuntoInteraccion;
            CreadorEscenaPrologo.Hija(taller, CreadorEscenaPrologo.Caja("Taller banco", new Vector3(banco.x, 0.5f, banco.z), new Vector3(1.5f, 1f, 0.8f), matBanco, false));
            CreadorEscenaPrologo.Caja("Taller suelo", new Vector3(PosicionTaller.x, 0.03f, PosicionTaller.z), new Vector3(8f, 0.02f, 9f), matTaller, false);
            Letrero("Letrero TALLER", "TALLER", PosicionTaller + new Vector3(0f, 0f, 5.5f));

            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha));
            mecha.transform.SetPositionAndRotation(PosicionTaller, rotacion);

            GameObject llamado = new GameObject("PuntoLlamado Taller");
            llamado.transform.SetPositionAndRotation(PosicionTaller + new Vector3(8f, 0f, 8f), Quaternion.Euler(0f, 90f, 0f));
            llamado.AddComponent<PuntoLlamadoDePrueba>().Configurar(40f, true);
        }

        private static void ArmarArena()
        {
            CreadorEscenaPrologo.Vacio("Sitio_Mostrador", PosicionMostradorPeleas);
            GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaArena));
            arena.transform.SetPositionAndRotation(PosicionArena, Quaternion.identity);

            GameObject mostrador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostradorPeleas));
            mostrador.transform.position = PosicionMostradorPeleas;
            mostrador.GetComponentInChildren<MechaCyberpunk.Pelea.Flujo.DirectorPelea>(true).transform.position = PosicionArena;

            CreadorEscenaPrologo.Aparicion(CreadorEscenaPrologo.IdClandestinoSalida, ModoJugador.APie, PosicionSalidaArena, Quaternion.Euler(0f, 180f, 0f));
            CreadorEscenaPrologo.Caja("Peleas suelo", new Vector3(PosicionMostradorPeleas.x, 0.03f, PosicionMostradorPeleas.z - 1f), new Vector3(7f, 0.02f, 5f), matPeleas, false);
            Letrero("Letrero PELEAS", "PELEAS", PosicionMostradorPeleas + new Vector3(0f, 0f, 3f));
        }

        private static void ArmarCasaYPandilla()
        {
            CreadorEscenaPrologo.ArmarLugar("Lugar Casa", IdLugar.Casa, "Casa", PosicionCasa, new Vector3(8f, 3f, 6f), PosicionCasa + new Vector3(0f, 0f, -4f));
            CreadorEscenaPrologo.Caja("Casa suelo", new Vector3(PosicionCasa.x, 0.03f, PosicionCasa.z), new Vector3(7f, 0.02f, 5f), matCasa, false);
            CreadorEscenaPrologo.Aparicion(IdCasaCama, ModoJugador.APie, PosicionCasa, Quaternion.identity);
            Letrero("Letrero CASA", "CASA", PosicionCasa + new Vector3(0f, 0f, 3.5f));

            // El sitio de la pandilla (T-195), en la calle, a más de 25 m de la clínica.
            CreadorEscenaPrologo.Vacio(NombreSitioPandilla, PosicionSitioPandilla);
            GameObject pandilla = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilla));
            pandilla.name = "PandillaAPie";
            pandilla.transform.position = PosicionSitioPandilla;
            Letrero("Letrero PANDILLA", "PANDILLA", PosicionSitioPandilla + new Vector3(0f, 0f, -3f));
        }

        private static void ArmarJugadorYServicios()
        {
            // 10:00 de día (anochece a las 20:00); la tecla J salta a las 8:00.
            RelojDePrueba reloj = new GameObject("RelojDePrueba").AddComponent<RelojDePrueba>();
            SerializedObject soReloj = new SerializedObject(reloj);
            soReloj.FindProperty("horaInicial").floatValue = HoraInicial;
            soReloj.FindProperty("horaAnochecer").floatValue = HoraAnochecer;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("GuardadoDePrueba").AddComponent<GuardadoDePrueba>();
            new GameObject("DineroDePrueba").AddComponent<MechaCyberpunk.Economia.Pruebas.DineroDePrueba>();
            new GameObject("ImplantesDePrueba").AddComponent<ImplantesDePrueba>();
            new GameObject("LigasDePrueba").AddComponent<LigasDePrueba>();
            new GameObject("HistoriaLibreDePrueba").AddComponent<HistoriaLibreDePrueba>();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria));
            // Los subtítulos del doc (PantallasHistoria.prefab solo trae el fundido y el "Continuará").
            PantallaSubtitulos subtitulos = CreadorEscenaFlashback.CrearSubtitulos(null);
            // Unity iguala el tamaño máximo de un Text a su tamaño al recargar la escena; se fija igual desde el principio para que el creador sea idempotente.
            foreach (Text texto in subtitulos.GetComponentsInChildren<Text>(true)) texto.resizeTextMaxSize = texto.fontSize;

            CreadorEscenaPrologo.Aparicion(IdInicio, ModoJugador.APie, PosicionInicio, Quaternion.identity);
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(PosicionInicio + new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = IdInicio;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Ayudas ----------

        // Letrero: un texto de pie (de cara al sur) y el mismo tendido en el piso para verlo desde el aire. Sin escala en el padre: no se deforma.
        private static void Letrero(string nombre, string texto, Vector3 posicion)
        {
            GameObject raiz = new GameObject(nombre);
            raiz.transform.position = posicion;
            Texto(raiz, texto, posicion + new Vector3(0f, AlturaLetrero, 0f), Quaternion.identity, TamanoLetra);
            Texto(raiz, texto, posicion + new Vector3(0f, 0.05f, -1.2f), Quaternion.Euler(90f, 0f, 0f), TamanoLetraPiso);
        }

        private static void TextoPiso(string nombre, string texto, Vector3 posicion, float tamano)
        {
            GameObject raiz = new GameObject(nombre);
            raiz.transform.position = posicion;
            Texto(raiz, texto, posicion, Quaternion.Euler(90f, 0f, 0f), tamano);
        }

        private static void Texto(GameObject padre, string texto, Vector3 posicion, Quaternion rotacion, float tamano)
        {
            GameObject objeto = new GameObject("Texto " + texto.Replace('\n', ' '));
            objeto.transform.SetParent(padre.transform, false);
            objeto.transform.SetPositionAndRotation(posicion, rotacion);
            TextMesh mesh = objeto.AddComponent<TextMesh>();
            mesh.text = texto;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 100;
            mesh.characterSize = tamano / 10f;
            mesh.color = Color.white;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = fuente;
            objeto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        private static void CargarMateriales()
        {
            matPiso = CargarMaterial("Prologo_Piso");
            matAsfalto = CargarMaterial("Prologo_Asfalto");
            matMuro = CargarMaterial("Prologo_Edificio");
            matClinica = CargarMaterial("Prologo_Taller");
            matCamilla = CargarMaterial("Prologo_Cama");
            matPista = CargarMaterial("Prologo_Mostrador");
            matCajas = CargarMaterial("Prologo_Clandestino");
            matTaller = CargarMaterial("Prologo_Taller");
            matBanco = CargarMaterial("Prologo_Banco");
            matPeleas = CargarMaterial("Prologo_Clandestino");
            matCasa = CargarMaterial("Prologo_Casa");
        }

        private static Material CargarMaterial(string nombre)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + nombre + ".mat");
            if (material == null)
            {
                throw new InvalidOperationException("Falta el material " + nombre + "; corre antes CreadorEscenaPrologo.Crear.");
            }
            return material;
        }
    }
}
