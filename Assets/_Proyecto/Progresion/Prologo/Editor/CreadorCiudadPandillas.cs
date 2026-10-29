using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
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
    public static class CreadorCiudadPandillas
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizT400 = "GTA_T400";
        public const string RaizT401 = "GTA_T401";
        public const string RaizT402 = "GTA_T402";
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
            Guardar(escena);
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
                new GrupoT402 { Manzana = "Manzana_6", Pandilla = IdPandilla.Viboras, Cantidad = 3 },
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
                DatosPandilleroAPie datos = grupos[g].Pandilla == IdPandilla.Calaveras ? calaveras : viboras;
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
