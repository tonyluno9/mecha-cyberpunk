using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1554: lo que faltaba en Unity de la revisión de vehículos del 20-nov (docs/tareas-unity-vehiculos-20nov.md).
    /// 1. <c>Bisagra_Cofre</c> (y el motor) en PA01, PA02 y V22, sin tocar lo demás de sus prefabs (no se rearma el carro).
    /// 2. V16 a V19 con su escala de raíz (0.70, 0.80, 0.70 y 0.78): suspensión, radio visual de las ruedas, cámara, asiento y punto de puerta coherentes con la escala.
    /// 3. Drones: sin cambios de posición del rotor en los prefabs (el pivote ya viene centrado desde Blender) y reporte de rotores descentrados.
    /// 4. Carros viejos de una pieza (Assets/Arte/Vehiculos/Prefabs/Vxx.prefab) de Barrio.prefab y Ciudad_Prologo por Carro_Vxx.prefab, con la misma posición y rotación.
    /// 5. V10 y V11 fuera de las listas de tráfico (los prefabs se quedan en disco: otras pruebas los usan).
    /// Todo es repetible. <see cref="Diagnostico"/> no cambia nada y escribe Logs/t1554-vehiculos.txt; <see cref="Aplicar"/> cambia y escribe Logs/t1554-aplicar.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.VehiculosT1554.Diagnostico (y .Aplicar).
    /// </summary>
    public static class VehiculosT1554
    {
        public const string RutaInformeDiagnostico = "Logs/t1554-vehiculos.txt";
        public const string RutaInformeAplicar = "Logs/t1554-aplicar.txt";

        public static readonly string[] CarrosConCofreNuevo = { "PA01", "PA02", "V22" };
        public static readonly (string codigo, float escala)[] Grandes = { ("V16", 0.70f), ("V17", 0.80f), ("V18", 0.70f), ("V19", 0.78f) };
        public static readonly string[] CodigosDescartados = { "V10", "V11" };

        public const string NombreBisagraCofre = "Bisagra_Cofre";
        public const string NombreCofre = "Cofre";
        public const string CarpetaDrones = "Assets/Arte/Vehiculos/Drones/";
        public const string CarpetaPrefabsViejos = "Assets/Arte/Vehiculos/Prefabs";
        public const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        public const string RutaEscenaPrologo = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string PrefijoRotor = "Rotor_";

        // Mismas proporciones con las que CreadorCarrosT1342 armó asiento, cámara y punto de subida (en metros del mundo).
        private const float FraccionOjos = 0.78f;
        private const float FraccionFoco = 0.62f;
        private const float DistanciaPorMetroDeLargo = 6f / 4.2f;
        private const float DistanciaMinimaCamara = 4.5f;
        private const float DistanciaMaximaCamara = 8f;
        private const float SeparacionPuerta = 1f;
        private const float AlturaMinimaAsiento = 0.3f;
        private const float ToleranciaPivoteCofre = 0.35f;
        private const float ToleranciaPosicion = 0.001f;
        private const float ToleranciaDescentradoRotor = 0.06f;
        private const float ToleranciaVector2 = 1e-4f;

        private static readonly string[] NombresAnclas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };

        // ------------------------------------------------------------------ entrada

        [MenuItem("Mecha Cyberpunk/T-1554/Diagnóstico")]
        public static void Diagnostico()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== 1. Bisagra_Cofre en PA01, PA02 y V22");
            informe.Append(CofresTodos(false));
            informe.AppendLine("== 2. V16 a V19 con escala de raíz");
            informe.Append(GrandesTodos(false));
            informe.AppendLine("== 3. Rotores de drones");
            informe.Append(Drones(false));
            informe.AppendLine("== 4. Carros viejos de una pieza en Barrio y Ciudad_Prologo");
            informe.Append(Escenas(false));
            informe.AppendLine("== 5. V10 y V11 en listas de tráfico");
            informe.Append(ListasAssets(false));
            Escribir(RutaInformeDiagnostico, informe.ToString());
            Debug.Log("[Vehiculos] T-1554 diagnóstico:\n" + informe);
        }

        [MenuItem("Mecha Cyberpunk/T-1554/Aplicar")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== 1. Bisagra_Cofre en PA01, PA02 y V22");
            informe.Append(CofresTodos(true));
            informe.AppendLine("== 1b. Patrullas (cuerpo de PA01/PA02)");
            informe.AppendLine(RehacerPatrullas());
            informe.AppendLine("== 2. V16 a V19 con escala de raíz (antes -> después)");
            informe.Append(GrandesTodos(true));
            informe.AppendLine("== 3. Rotores de drones");
            informe.Append(Drones(true));
            informe.AppendLine("== 4 y 5. Barrio y Ciudad_Prologo: carros viejos y listas con V10/V11");
            informe.Append(Escenas(true));
            informe.AppendLine("== 5. V10 y V11 en assets de datos de tráfico");
            informe.Append(ListasAssets(true));
            AssetDatabase.SaveAssets();
            Escribir(RutaInformeAplicar, informe.ToString());
            Debug.Log("[Vehiculos] T-1554 aplicar:\n" + informe);
        }

        [MenuItem("Mecha Cyberpunk/T-1554/Rehacer patrullas (cofre de PA01 y PA02)")]
        public static void RehacerPatrullasMenu()
        {
            Debug.Log("[Vehiculos] T-1554: " + RehacerPatrullas());
        }

        private static void Escribir(string ruta, string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ruta, texto);
        }

        // Patrulla.prefab y PatrullaCamioneta.prefab copian el cuerpo de Carro_PA01 y Carro_PA02: para que lleven el cofre se rehacen (PatrullasPAT1510 vive en Trafico.Editor,
        // que este ensamblado no referencia, así que se llama por reflexión). Es idempotente.
        public static string RehacerPatrullas()
        {
            try
            {
                Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Trafico.Editor.PatrullasPAT1510", false)).FirstOrDefault(t => t != null);
                if (tipo == null)
                {
                    return "no se encontró PatrullasPAT1510: corre a mano Mecha Cyberpunk/T-1510/Rehacer patrullas con PA01 y PA02";
                }
                tipo.GetMethod("Rehacer", Type.EmptyTypes)?.Invoke(null, null);
                return "Patrulla.prefab y PatrullaCamioneta.prefab rehechos con PatrullasPAT1510.Rehacer";
            }
            catch (Exception e)
            {
                return "ERROR al rehacer patrullas (corre a mano Mecha Cyberpunk/T-1510/Rehacer patrullas con PA01 y PA02): " + e.Message;
            }
        }

        // ------------------------------------------------------------------ 1. cofres

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        private static string CofresTodos(bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string codigo in CarrosConCofreNuevo)
            {
                sb.AppendLine(Cofre(codigo, aplicar));
            }
            return sb.ToString();
        }

        /// <summary>El hijo Bisagra_Cofre del visual del carro con su Cofre dentro, o null.</summary>
        public static Transform BisagraCofreDe(GameObject raiz)
        {
            Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            Transform bisagra = visual != null ? visual.Find(NombreBisagraCofre) : null;
            return bisagra != null && bisagra.Find(NombreCofre) != null ? bisagra : null;
        }

        private static string Cofre(string codigo, bool aplicar)
        {
            string ruta = RutaPrefab(codigo);
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform bisagraActual = BisagraCofreDe(raiz);
                PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
                Transform enchufada = puertas != null ? new SerializedObject(puertas).FindProperty("bisagraCofre").objectReferenceValue as Transform : null;
                string estado = codigo + "\tbisagra_cofre=" + (bisagraActual != null ? "sí" : "NO") + ", PuertasCarro.bisagraCofre=" + (enchufada != null ? "sí" : "NO")
                    + ", Fija_Motor=" + (raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/" + PiezasCarroT1342.PrefijoFija + "Motor") != null ? "sí" : "NO");
                if (!aplicar)
                {
                    return estado;
                }
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
                Transform visual = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                Transform origen = fbx != null ? fbx.transform.Find(NombreCofre) : null;
                if (fbx == null || visual == null || origen == null || origen.GetComponent<MeshFilter>() == null)
                {
                    return estado + " -> OMITIDO: falta el FBX, el visual o el hijo Cofre del FBX";
                }
                Renderer carroceria = visual.Find("Carroceria") != null ? visual.Find("Carroceria").GetComponent<Renderer>() : null;
                Material material = carroceria != null ? carroceria.sharedMaterial : null;
                if (material == null)
                {
                    return estado + " -> OMITIDO: el visual no tiene Carroceria con material";
                }

                string nota = "bisagra ya estaba";
                if (bisagraActual == null)
                {
                    Transform sinBisagra = visual.Find(NombreCofre);
                    if (sinBisagra != null)
                    {
                        Object.DestroyImmediate(sinBisagra.gameObject);
                    }
                    Transform huerfana = visual.Find(NombreBisagraCofre);
                    if (huerfana != null)
                    {
                        Object.DestroyImmediate(huerfana.gameObject);
                    }
                    GameObject cofre = CreadorCarrosBlenderT1312.Pieza(origen, NombreCofre, visual, material);
                    Bounds b = CreadorCarrosBlenderT1312.LimitesEn(cofre, raiz.transform);
                    // Como en T-1342: borde trasero del cofre (menor Z, junto al parabrisas), arriba y al centro. Si el FBX trae el pivote en la bisagra se usa ese.
                    Vector3 calculado = new Vector3(b.center.x, b.max.y, b.min.z);
                    Vector3 pivote = raiz.transform.InverseTransformPoint(cofre.transform.position);
                    bool usaPivote = Vector3.Distance(pivote, calculado) <= ToleranciaPivoteCofre;
                    GameObject bisagra = new GameObject(NombreBisagraCofre);
                    bisagra.transform.SetParent(visual, false);
                    bisagra.transform.position = raiz.transform.TransformPoint(usaPivote ? pivote : calculado);
                    bisagra.transform.rotation = raiz.transform.rotation;
                    cofre.transform.SetParent(bisagra.transform, true);
                    bisagraActual = bisagra.transform;
                    nota = string.Format("bisagra creada en {0} ({1}; pivote del FBX {2}, borde calculado {3})",
                        (usaPivote ? pivote : calculado).ToString("F2"), usaPivote ? "pivote del FBX" : "borde calculado", pivote.ToString("F2"), calculado.ToString("F2"));
                }

                // Piezas fijas del FBX que faltaban (el Motor nuevo, bajo el cofre). Con el mismo prefijo Fija_ que usa T-1342.
                StringBuilder fijas = new StringBuilder();
                foreach (Transform hijo in fbx.transform.Cast<Transform>().ToList())
                {
                    if (PiezasCarroT1342.EsPiezaFija(hijo.name) && hijo.GetComponent<MeshFilter>() != null && visual.Find(PiezasCarroT1342.PrefijoFija + hijo.name) == null)
                    {
                        CreadorCarrosBlenderT1312.Pieza(hijo, PiezasCarroT1342.PrefijoFija + hijo.name, visual, material);
                        fijas.Append(hijo.name).Append(' ');
                    }
                }

                if (puertas == null)
                {
                    puertas = raiz.AddComponent<PuertasCarro>();
                    nota += "; PuertasCarro agregado (no había)";
                }
                SerializedObject so = new SerializedObject(puertas);
                so.FindProperty("bisagraCofre").objectReferenceValue = bisagraActual;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return estado + " -> " + nota + (fijas.Length > 0 ? "; piezas fijas nuevas: " + fijas.ToString().TrimEnd() : "");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ 2. V16 a V19

        private static string GrandesTodos(bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var g in Grandes)
            {
                sb.Append(Grande(g.codigo, g.escala, aplicar));
            }
            return sb.ToString();
        }

        /// <summary>Alto, ancho y largo del visual del carro en el mundo (con la raíz en el origen y su escala).</summary>
        public static Bounds LimitesVisual(GameObject raiz)
        {
            return CreadorCarrosT511.LimitesExactos(raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual).gameObject);
        }

        /// <summary>Altura (m, mundo) a la que debe estar el ancla sobre lo más bajo de su llanta para que, con el resorte en reposo, la llanta toque el piso.</summary>
        public static float AnclaSobreLlanta(GameObject raiz, string nombreAncla)
        {
            Transform ancla = raiz.transform.Find(nombreAncla);
            float fondo = CarrosPAT1510.FondoDeLlanta(raiz.transform, nombreAncla);
            return float.IsNaN(fondo) ? float.NaN : (ancla.localPosition.y - fondo) * raiz.transform.lossyScale.y;
        }

        /// <summary>Radio visible de la llanta (m, mundo) de un ancla.</summary>
        public static float RadioVisibleLlanta(GameObject raiz, string nombreAncla)
        {
            Transform ancla = raiz.transform.Find(nombreAncla);
            return ancla != null && ancla.GetComponentInChildren<MeshFilter>(true) != null ? CreadorCarrosT511.LimitesExactos(ancla.gameObject).extents.y : float.NaN;
        }

        /// <summary>Distancia (m, mundo) entre el eje delantero y el trasero medida entre Rueda_DI y Rueda_TI.</summary>
        public static float DistanciaEjesMundo(GameObject raiz)
        {
            Transform di = raiz.transform.Find("Rueda_DI");
            Transform ti = raiz.transform.Find("Rueda_TI");
            return Mathf.Abs(raiz.transform.InverseTransformPoint(di.position).z - raiz.transform.InverseTransformPoint(ti.position).z) * raiz.transform.lossyScale.z;
        }

        private static Transform Hijo(GameObject raiz, params string[] nombres)
        {
            foreach (string n in nombres)
            {
                Transform t = raiz.transform.Find(n);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        private static string Grande(string codigo, float escalaEsperada, bool aplicar)
        {
            string ruta = RutaPrefab(codigo);
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            StringBuilder sb = new StringBuilder();
            try
            {
                float escala = raiz.transform.localScale.x;
                bool cambio = false;
                sb.AppendLine(string.Format("{0}\tescala de raíz {1:F3} (esperada {2:F2}){3}", codigo, escala, escalaEsperada,
                    Mathf.Abs(escala - escalaEsperada) > 0.005f ? "  ¡NO COINCIDE!" : ""));
                ConfigCarro config = CarrosPAT1510.ConfigDe(raiz);
                Rigidbody rb = raiz.GetComponent<Rigidbody>();
                if (config == null || rb == null)
                {
                    sb.AppendLine("\tOMITIDO: sin CarroViejo con config o sin Rigidbody");
                    return sb.ToString();
                }
                Bounds vb = LimitesVisual(raiz);
                sb.AppendLine(string.Format("\tmodelo en el mundo: largo {0:F2} x ancho {1:F2} x alto {2:F2} m", vb.size.z, vb.size.x, vb.size.y));
                sb.AppendLine(string.Format("\tconfig {0}: masa {1:F0} kg, radioRueda {2:F2}, largoReposo {3:F2}, largoMaximo {4:F2}, rigidez {5:F0} (la masa y el resorte salen de aquí al jugar; no cambian con la escala)",
                    config.name, config.Masa, config.RadioRueda, config.LargoReposo, config.LargoMaximo, config.Rigidez));

                // Rigidbody: el CarroViejo pone la masa de la config al despertar; el prefab se deja igual para que el inspector no mienta.
                SerializedObject soRb = new SerializedObject(rb);
                SerializedProperty masa = soRb.FindProperty("m_Mass");
                sb.AppendLine(string.Format("\tRigidbody.masa prefab {0:F0} -> {1:F0}", masa.floatValue, config.Masa));
                if (aplicar && Mathf.Abs(masa.floatValue - config.Masa) > 0.01f)
                {
                    masa.floatValue = config.Masa;
                    soRb.ApplyModifiedPropertiesWithoutUndo();
                    cambio = true;
                }

                // Suspensión: el rayo del CarroViejo mide en metros del mundo; el ancla debe quedar AlturaAnclaEnReposo (mundo) sobre el fondo de su llanta.
                float altura = CarrosPAT1510.AlturaAnclaEnReposo(config);
                sb.AppendLine(string.Format("\tancla sobre el fondo de la llanta (mundo) debe ser {0:F3} m", altura));
                foreach (string nombre in NombresAnclas)
                {
                    Transform ancla = raiz.transform.Find(nombre);
                    Transform direccion = ancla != null ? ancla.Find("Direccion") : null;
                    float fondo = CarrosPAT1510.FondoDeLlanta(raiz.transform, nombre);
                    if (direccion == null || float.IsNaN(fondo))
                    {
                        sb.AppendLine("\t" + nombre + ": sin llanta");
                        continue;
                    }
                    float antes = AnclaSobreLlanta(raiz, nombre);
                    float yAntes = ancla.localPosition.y;
                    float yDeseada = fondo + altura / escala;
                    string linea = string.Format("\t{0}: ancla local y {1:F3} (mundo sobre llanta {2:F3}) ", nombre, yAntes, antes);
                    if (aplicar && Mathf.Abs(yAntes - yDeseada) > ToleranciaPosicion)
                    {
                        Vector3 centroLlanta = direccion.position;
                        ancla.localPosition = new Vector3(ancla.localPosition.x, yDeseada, ancla.localPosition.z);
                        direccion.position = centroLlanta; // la llanta que se ve no se mueve
                        cambio = true;
                        linea += string.Format("-> y {0:F3} (mundo sobre llanta {1:F3})", ancla.localPosition.y, AnclaSobreLlanta(raiz, nombre));
                    }
                    else
                    {
                        linea += aplicar ? "-> sin cambio" : string.Format("-> objetivo y {0:F3}", yDeseada);
                    }
                    sb.AppendLine(linea);
                }

                // Ruedas visuales: radio y distancia entre ejes en metros del mundo (el giro sale de la velocidad real).
                RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
                if (ruedas != null)
                {
                    SerializedObject soRuedas = new SerializedObject(ruedas);
                    SerializedProperty radio = soRuedas.FindProperty("radio");
                    SerializedProperty ejes = soRuedas.FindProperty("distanciaEjes");
                    float radioMundo = RadioVisibleLlanta(raiz, "Rueda_DI");
                    float ejesMundo = DistanciaEjesMundo(raiz);
                    sb.AppendLine(string.Format("\tRuedasVisualesCarro: radio {0:F3} -> {1:F3} m, distanciaEjes {2:F2} -> {3:F2} m", radio.floatValue, radioMundo, ejes.floatValue, ejesMundo));
                    if (aplicar && !float.IsNaN(radioMundo) && (Mathf.Abs(radio.floatValue - radioMundo) > ToleranciaPosicion || Mathf.Abs(ejes.floatValue - ejesMundo) > ToleranciaPosicion))
                    {
                        radio.floatValue = radioMundo;
                        ejes.floatValue = ejesMundo;
                        soRuedas.ApplyModifiedPropertiesWithoutUndo();
                        cambio = true;
                    }
                }

                // Cámara, asiento y punto de puerta. Asiento y punto van en coordenadas locales (las divide la escala); la cámara de tercera persona va en metros del mundo.
                CamaraCarro camara = raiz.GetComponentInChildren<CamaraCarro>(true);
                Transform asiento = Hijo(raiz, CreadorCarrosPiezasT742.NombreAsiento, "Asiento");
                Transform punto = Hijo(raiz, CreadorCarrosPiezasT742.NombrePuntoPuerta, "PuntoSubida");
                float ojos = camara != null && camara.Config != null ? camara.Config.AlturaOjos : 0f;
                if (camara != null && asiento != null)
                {
                    SerializedObject soCamara = new SerializedObject(camara);
                    SerializedProperty foco = soCamara.FindProperty("alturaFocoPropia");
                    SerializedProperty atras = soCamara.FindProperty("distanciaAtrasPropia");
                    float focoNuevo = vb.size.y * FraccionFoco;
                    float atrasNuevo = Mathf.Clamp(vb.size.z * DistanciaPorMetroDeLargo, DistanciaMinimaCamara, DistanciaMaximaCamara);
                    sb.AppendLine(string.Format("\tcámara tercera persona: alturaFoco {0:F2} -> {1:F2} m, distanciaAtrás {2:F2} -> {3:F2} m", foco.floatValue, focoNuevo, atras.floatValue, atrasNuevo));

                    // Primera persona: ojos = asiento (mundo) + AlturaOjos (mundo, sin escala). Se busca que queden a FraccionOjos de la altura del modelo.
                    float asientoYDeseada = Mathf.Max(AlturaMinimaAsiento, vb.size.y * FraccionOjos - ojos) / escala;
                    Vector3 asientoLocalAntes = asiento.localPosition;
                    float ojosMundoAntes = asiento.position.y + ojos;
                    sb.AppendLine(string.Format("\tasiento local y {0:F3} -> {1:F3}; ojos en primera persona (mundo) {2:F2} m -> {3:F2} m (modelo alto {4:F2} m)",
                        asientoLocalAntes.y, asientoYDeseada, ojosMundoAntes, asientoYDeseada * escala + ojos, vb.size.y));
                    if (aplicar)
                    {
                        if (Mathf.Abs(foco.floatValue - focoNuevo) > ToleranciaPosicion || Mathf.Abs(atras.floatValue - atrasNuevo) > ToleranciaPosicion)
                        {
                            foco.floatValue = focoNuevo;
                            atras.floatValue = atrasNuevo;
                            soCamara.ApplyModifiedPropertiesWithoutUndo();
                            cambio = true;
                        }
                        Vector3 asientoNuevo = new Vector3(asientoLocalAntes.x, asientoYDeseada, asientoLocalAntes.z);
                        Vector3 camaraNueva = asientoNuevo + Vector3.up * (ojos / escala);
                        if ((asientoNuevo - asientoLocalAntes).sqrMagnitude > ToleranciaPosicion * ToleranciaPosicion
                            || (camara.transform.localPosition - camaraNueva).sqrMagnitude > ToleranciaPosicion * ToleranciaPosicion)
                        {
                            asiento.localPosition = asientoNuevo;
                            camara.transform.localPosition = camaraNueva;
                            cambio = true;
                        }
                    }
                }
                if (punto != null)
                {
                    float signo = punto.localPosition.x < 0f ? -1f : 1f;
                    float xDeseada = signo * (vb.size.x * 0.5f + SeparacionPuerta) / escala;
                    float separacionAntes = Mathf.Abs(punto.localPosition.x) * escala - vb.size.x * 0.5f;
                    sb.AppendLine(string.Format("\tpunto de puerta local x {0:F3} -> {1:F3} (separación del costado al punto: {2:F2} m -> {3:F2} m)",
                        punto.localPosition.x, xDeseada, separacionAntes, SeparacionPuerta));
                    if (aplicar && Mathf.Abs(punto.localPosition.x - xDeseada) > ToleranciaPosicion)
                    {
                        punto.localPosition = new Vector3(xDeseada, punto.localPosition.y, punto.localPosition.z);
                        cambio = true;
                    }
                }

                if (cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    sb.AppendLine("\tprefab guardado");
                }
                return sb.ToString();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ 3. drones

        /// <summary>Rotores (hijos Rotor_*) de un objeto, también apagados.</summary>
        public static List<Transform> RotoresDe(GameObject raiz)
        {
            return raiz.GetComponentsInChildren<Transform>(true).Where(t => t != raiz.transform && t.name.StartsWith(PrefijoRotor)).ToList();
        }

        /// <summary>
        /// Cuánto se aleja del eje de giro (Y local del rotor) el centro de su malla, en la unidad local del rotor, y el radio aproximado del disco.
        /// Si el centro está lejos del pivote el rotor "tiembla" al girar. Devuelve false si el rotor no tiene malla.
        /// </summary>
        public static bool DescentradoDelRotor(Transform rotor, out float descentrado, out float radio, out bool ejeYEsElDelDisco)
        {
            Bounds b = CreadorCarrosBlenderT1312.LimitesEn(rotor.gameObject, rotor);
            descentrado = 0f;
            radio = 0f;
            ejeYEsElDelDisco = true;
            if (b.size == Vector3.zero)
            {
                return false;
            }
            descentrado = new Vector2(b.center.x, b.center.z).magnitude;
            radio = Mathf.Max(b.extents.x, b.extents.z);
            ejeYEsElDelDisco = b.extents.y <= Mathf.Min(b.extents.x, b.extents.z) + ToleranciaVector2;
            return true;
        }

        /// <summary>True si la posición local del rotor es distinta a la de su origen en el prefab de arriba (el FBX u otro prefab): un override de posición.</summary>
        public static bool TienePosicionSobrescrita(Transform rotor)
        {
            Transform fuente = PrefabUtility.GetCorrespondingObjectFromSource(rotor);
            return fuente != null && (rotor.localPosition - fuente.localPosition).sqrMagnitude > ToleranciaVector2 * ToleranciaVector2;
        }

        /// <summary>Prefabs de Assets/Arte/Vehiculos/Drones/Prefabs/ (los Dron_*).</summary>
        public static string[] PrefabsDeDrones()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { CarpetaDrones + "Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        }

        // Prefabs de Assets/_Proyecto y Assets/Arte que usan un dron (los Dron_*, el FBX de algún dron o el GiroRotores): ahí también puede haber overrides.
        private static string[] PrefabsQueUsanDrones()
        {
            List<string> guids = new List<string>();
            foreach (string prefab in PrefabsDeDrones())
            {
                guids.Add(AssetDatabase.AssetPathToGUID(prefab));
            }
            foreach (string fbx in AssetDatabase.FindAssets("t:Model", new[] { CarpetaDrones }).Select(AssetDatabase.GUIDToAssetPath))
            {
                guids.Add(AssetDatabase.AssetPathToGUID(fbx));
            }
            MonoScript giro = AssetDatabase.FindAssets("GiroRotores t:MonoScript").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>).FirstOrDefault(m => m != null && m.name == "GiroRotores");
            if (giro != null)
            {
                guids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(giro)));
            }
            List<string> resultado = new List<string>(PrefabsDeDrones());
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Proyecto", "Assets/Arte" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (resultado.Contains(ruta) || !File.Exists(ruta))
                {
                    continue;
                }
                string texto = File.ReadAllText(ruta);
                if (guids.Any(g => texto.Contains(g)))
                {
                    resultado.Add(ruta);
                }
            }
            return resultado.ToArray();
        }

        private static string Drones(bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string ruta in PrefabsQueUsanDrones())
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    List<Transform> rotores = RotoresDe(raiz);
                    if (rotores.Count == 0)
                    {
                        continue;
                    }
                    bool cambio = false;
                    sb.AppendLine(Path.GetFileName(ruta) + "\t" + rotores.Count + " rotores");
                    foreach (Transform rotor in rotores)
                    {
                        bool sobrescrita = TienePosicionSobrescrita(rotor);
                        string linea = "\t" + rotor.name + ": posición local " + rotor.localPosition.ToString("F3") + (sobrescrita ? ", OVERRIDE de posición" : ", sin override");
                        if (DescentradoDelRotor(rotor, out float descentrado, out float radio, out bool ejeY))
                        {
                            bool tiembla = radio > 0f && descentrado / radio > ToleranciaDescentradoRotor;
                            linea += string.Format(", centro de la malla a {0:F3} del eje ({1:P1} del radio){2}{3}", descentrado, radio > 0f ? descentrado / radio : 0f,
                                tiembla ? "  <-- DESCENTRADO (tiembla)" : "", ejeY ? "" : "  <-- el eje Y no es el del disco");
                        }
                        if (sobrescrita && aplicar)
                        {
                            SerializedProperty posicion = new SerializedObject(rotor).FindProperty("m_LocalPosition");
                            if (posicion.prefabOverride)
                            {
                                PrefabUtility.RevertPropertyOverride(posicion, InteractionMode.AutomatedAction);
                                cambio = true;
                                linea += " -> override quitado";
                            }
                            else
                            {
                                linea += " -> la diferencia no es un override de este prefab (viene de un prefab de abajo)";
                            }
                        }
                        sb.AppendLine(linea);
                    }
                    if (cambio)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                        sb.AppendLine("\tprefab guardado");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ 4 y 5. Barrio y Ciudad_Prologo

        /// <summary>Ruta del Carro_Vxx.prefab que reemplaza a un prefab viejo de Assets/Arte/Vehiculos/Prefabs/, o null si no es uno de esos o es un carro descartado.</summary>
        public static string RutaDeReemplazo(string rutaPrefabViejo)
        {
            if (string.IsNullOrEmpty(rutaPrefabViejo) || Path.GetDirectoryName(rutaPrefabViejo).Replace('\\', '/') != CarpetaPrefabsViejos)
            {
                return null;
            }
            string codigo = Path.GetFileNameWithoutExtension(rutaPrefabViejo).Split('_')[0];
            return CodigosDescartados.Contains(codigo) ? null : RutaPrefab(codigo);
        }

        /// <summary>Rutas de los prefabs de V10 y V11 (viejo, de carro y de tráfico) que no deben estar en ninguna lista.</summary>
        public static HashSet<string> RutasDescartadas()
        {
            HashSet<string> rutas = new HashSet<string>();
            foreach (string codigo in CodigosDescartados)
            {
                rutas.Add(CarpetaPrefabsViejos + "/" + codigo + ".prefab");
                rutas.Add(RutaPrefab(codigo));
                rutas.Add("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_" + codigo + ".prefab");
            }
            return rutas;
        }

        private static string Escenas(bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Barrio.prefab");
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaBarrio);
            try
            {
                bool cambio = ReemplazarViejos(new[] { barrio }, aplicar, sb);
                cambio |= QuitarDescartadosDeComponentes(new[] { barrio }, aplicar, sb);
                if (aplicar && cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(barrio, RutaBarrio);
                    sb.AppendLine("\tBarrio.prefab guardado");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(barrio);
            }

            sb.AppendLine("Ciudad_Prologo.unity");
            Scene escena = EditorSceneManager.OpenScene(RutaEscenaPrologo, OpenSceneMode.Additive);
            try
            {
                GameObject[] raices = escena.GetRootGameObjects();
                bool cambio = ReemplazarViejos(raices, aplicar, sb);
                cambio |= QuitarDescartadosDeComponentes(escena.GetRootGameObjects(), aplicar, sb);
                if (aplicar && cambio)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    sb.AppendLine("\tCiudad_Prologo.unity guardada");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
            return sb.ToString();
        }

        private struct Referencia
        {
            public Vector3 ancla;  // centro en x y z y piso (y mínima) del modelo, en el marco de la raíz del prefab
            public Vector3 tamano;
        }

        private static Referencia Medir(string rutaPrefab, bool esCarroNuevo)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                GameObject modelo = esCarroNuevo && contenido.transform.Find(CreadorCarrosPiezasT742.NombreVisual) != null
                    ? contenido.transform.Find(CreadorCarrosPiezasT742.NombreVisual).gameObject
                    : contenido;
                Bounds b = CreadorCarrosT511.LimitesExactos(modelo);
                return new Referencia { ancla = new Vector3(b.center.x, b.min.y, b.center.z), tamano = b.size };
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // Instancias de prefabs viejos (de Assets/Arte/Vehiculos/Prefabs) que no están dentro de otra instancia de prefab.
        private static List<GameObject> InstanciasViejas(IEnumerable<GameObject> raices, out int dentroDeOtraInstancia)
        {
            List<GameObject> lista = new List<GameObject>();
            dentroDeOtraInstancia = 0;
            foreach (GameObject raiz in raices)
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    {
                        continue;
                    }
                    Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    string ruta = fuente != null ? AssetDatabase.GetAssetPath(fuente) : null;
                    if (string.IsNullOrEmpty(ruta) || Path.GetDirectoryName(ruta).Replace('\\', '/') != CarpetaPrefabsViejos)
                    {
                        continue;
                    }
                    if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
                    {
                        dentroDeOtraInstancia++;
                        continue;
                    }
                    lista.Add(go);
                }
            }
            return lista;
        }

        /// <summary>Cuántas instancias de prefabs viejos de una pieza quedan en el prefab o la escena (las que están sueltas, no anidadas en otro prefab).</summary>
        public static int ContarViejos(IEnumerable<GameObject> raices)
        {
            return InstanciasViejas(raices, out _).Count;
        }

        private static bool ReemplazarViejos(IEnumerable<GameObject> raices, bool aplicar, StringBuilder sb)
        {
            List<GameObject> viejos = InstanciasViejas(raices, out int anidados);
            sb.AppendLine("\tinstancias de carros viejos de una pieza: " + viejos.Count + (anidados > 0 ? " (y " + anidados + " dentro de otra instancia de prefab: se arreglan editando ese prefab)" : ""));
            Dictionary<string, Referencia> cacheViejos = new Dictionary<string, Referencia>();
            Dictionary<string, Referencia> cacheNuevos = new Dictionary<string, Referencia>();
            bool cambio = false;
            foreach (GameObject go in viejos)
            {
                Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string rutaViejo = AssetDatabase.GetAssetPath(fuente);
                string rutaNuevo = RutaDeReemplazo(rutaViejo);
                string descripcion = "\t" + go.name + " (" + Path.GetFileName(rutaViejo) + ") en " + go.transform.position.ToString("F2") + " rot y " + go.transform.eulerAngles.y.ToString("F1");
                GameObject nuevoPrefab = rutaNuevo != null ? AssetDatabase.LoadAssetAtPath<GameObject>(rutaNuevo) : null;
                if (nuevoPrefab == null)
                {
                    sb.AppendLine(descripcion + " -> SIN reemplazo (" + (rutaNuevo == null ? "carro descartado" : "falta " + rutaNuevo) + ")");
                    continue;
                }
                if (!cacheViejos.TryGetValue(rutaViejo, out Referencia viejoRef))
                {
                    viejoRef = Medir(rutaViejo, false);
                    cacheViejos[rutaViejo] = viejoRef;
                }
                if (!cacheNuevos.TryGetValue(rutaNuevo, out Referencia nuevoRef))
                {
                    nuevoRef = Medir(rutaNuevo, true);
                    cacheNuevos[rutaNuevo] = nuevoRef;
                }
                // Si el largo del viejo está en X y el del nuevo en Z (o al revés), el nuevo se gira 90°.
                bool viejoLargoEnX = viejoRef.tamano.x > viejoRef.tamano.z * 1.15f;
                bool viejoLargoEnZ = viejoRef.tamano.z > viejoRef.tamano.x * 1.15f;
                bool nuevoLargoEnZ = nuevoRef.tamano.z > nuevoRef.tamano.x;
                Quaternion giroExtra = viejoLargoEnX && nuevoLargoEnZ || viejoLargoEnZ && !nuevoLargoEnZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                Quaternion rotacion = go.transform.rotation * giroExtra;
                Vector3 puntoViejo = go.transform.TransformPoint(viejoRef.ancla);
                Vector3 posicion = puntoViejo - rotacion * nuevoRef.ancla;
                descripcion += string.Format(" -> {0} en {1} (tam viejo {2}, nuevo {3}{4})", Path.GetFileName(rutaNuevo), posicion.ToString("F2"), viejoRef.tamano.ToString("F2"),
                    nuevoRef.tamano.ToString("F2"), giroExtra != Quaternion.identity ? ", GIRADO 90° por eje distinto" : "");
                if (!aplicar)
                {
                    sb.AppendLine(descripcion);
                    continue;
                }
                Transform padre = go.transform.parent;
                GameObject nuevo = padre != null ? (GameObject)PrefabUtility.InstantiatePrefab(nuevoPrefab, padre) : (GameObject)PrefabUtility.InstantiatePrefab(nuevoPrefab, go.scene);
                nuevo.transform.SetPositionAndRotation(posicion, rotacion);
                nuevo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
                nuevo.layer = go.layer;
                nuevo.SetActive(go.activeSelf);
                if (go.name != fuente.name)
                {
                    nuevo.name = go.name; // un nombre propio (por ejemplo "CarroEstacionado") se conserva
                }
                foreach (AddedComponent agregado in PrefabUtility.GetAddedComponents(go))
                {
                    if (agregado.instanceComponent != null && nuevo.GetComponent(agregado.instanceComponent.GetType()) == null
                        && UnityEditorInternal.ComponentUtility.CopyComponent(agregado.instanceComponent))
                    {
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(nuevo);
                    }
                }
                Object.DestroyImmediate(go);
                cambio = true;
                sb.AppendLine(descripcion + "  [reemplazado]");
            }
            return cambio;
        }

        // Quita V10 y V11 de las listas de prefabs de los componentes de MechaCyberpunk.* (spawners, tráfico, zonas…).
        private static bool QuitarDescartadosDeComponentes(IEnumerable<GameObject> raices, bool aplicar, StringBuilder sb)
        {
            HashSet<string> rutas = RutasDescartadas();
            bool cambio = false;
            int total = 0;
            foreach (GameObject raiz in raices)
            {
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb.GetType().Namespace == null || !mb.GetType().Namespace.StartsWith("MechaCyberpunk.Trafico") && !mb.GetType().Namespace.StartsWith("MechaCyberpunk.Mundo")
                        && !mb.GetType().Namespace.StartsWith("MechaCyberpunk.Vehiculos"))
                    {
                        continue;
                    }
                    SerializedObject so = new SerializedObject(mb);
                    int n = ReferenciasEnListas(so, rutas, aplicar);
                    if (n > 0)
                    {
                        total += n;
                        sb.AppendLine("\t" + mb.GetType().Name + " en " + mb.gameObject.name + ": " + n + " referencias a V10/V11" + (aplicar ? " quitadas" : ""));
                        if (aplicar)
                        {
                            EditorUtility.SetDirty(mb);
                            cambio = true;
                        }
                    }
                }
            }
            sb.AppendLine("\treferencias a V10/V11 en listas de componentes: " + total);
            return cambio;
        }

        /// <summary>
        /// Cuenta (y con <paramref name="quitar"/> borra) los elementos de listas de objetos de <paramref name="so"/> que apuntan a alguno de los prefabs de <paramref name="rutas"/>.
        /// Devuelve cuántos había.
        /// </summary>
        public static int ReferenciasEnListas(SerializedObject so, HashSet<string> rutas, bool quitar)
        {
            int total = 0;
            bool encontrado;
            do
            {
                encontrado = false;
                SerializedProperty it = so.GetIterator();
                bool entrar = true;
                while (it.Next(entrar))
                {
                    if (!it.isArray || it.propertyType != SerializedPropertyType.Generic || it.arrayElementType == null || !it.arrayElementType.StartsWith("PPtr<"))
                    {
                        continue;
                    }
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        SerializedProperty elemento = it.GetArrayElementAtIndex(i);
                        Object objeto = elemento.objectReferenceValue;
                        if (objeto == null || !rutas.Contains(AssetDatabase.GetAssetPath(objeto)))
                        {
                            continue;
                        }
                        total++;
                        if (quitar)
                        {
                            elemento.objectReferenceValue = null;
                            it.DeleteArrayElementAtIndex(i);
                            so.ApplyModifiedPropertiesWithoutUndo();
                            encontrado = true;
                            break;
                        }
                    }
                    if (encontrado)
                    {
                        break;
                    }
                }
            }
            while (encontrado);
            return total;
        }

        // Los ScriptableObject de datos de Trafico y Vehiculos (zonas, listas generales).
        private static string ListasAssets(bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            HashSet<string> rutas = RutasDescartadas();
            int total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Proyecto/Trafico", "Assets/_Proyecto/Vehiculos/Datos" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ruta);
                if (so == null)
                {
                    continue;
                }
                SerializedObject serial = new SerializedObject(so);
                int n = ReferenciasEnListas(serial, rutas, aplicar);
                if (n > 0)
                {
                    total += n;
                    sb.AppendLine(Path.GetFileName(ruta) + ": " + n + " referencias a V10/V11" + (aplicar ? " quitadas" : ""));
                    if (aplicar)
                    {
                        EditorUtility.SetDirty(so);
                    }
                }
            }
            sb.AppendLine("referencias a V10/V11 en assets de datos: " + total);
            sb.AppendLine("(la tabla en código Trafico/Datos/DisenoZonasTrafico.cs ya no lista V10)");
            return sb.ToString();
        }
    }
}
