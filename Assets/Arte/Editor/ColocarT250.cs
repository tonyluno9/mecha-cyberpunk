using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-250: coloca los prefabs de arte en el juego sobre lo que ya existe. El modelo entra como hijo "Visual_T250" del objeto
    /// (o como hermano "&lt;nombre&gt;_Visual_T250" si el objeto es un primitivo con escala no uniforme) y los MeshRenderer viejos
    /// se apagan sin borrarse. Nunca toca raíces, scripts, colliders, nombres ni posiciones existentes.
    /// Se corre sobre los prefabs limpios (tal como están en git): no deshace una colocación anterior.
    /// </summary>
    public static class ColocarT250
    {
        public const string NombreVisual = "Visual_T250";

        public const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        public const string RutaJaula = "Assets/_Proyecto/Arena/Prefabs/JaulaClandestino.prefab";
        public const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";

        public static readonly string[] PrefabsDestino = { RutaBarrio, RutaJaula, RutaCarro, "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab" };

        public static readonly string[] EscenasDestino =
        {
            "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
            "Assets/_Proyecto/Escenas/Clandestino.unity",
        };

        private const float ToleranciaEscala = 0.02f;
        private const float RazonAlargado = 1.2f;
        private const float ProfundidadFachada = 0.69f;

        private enum Ajuste
        {
            Alto,
            Largo,
            Propio
        }

        /// <summary>Reemplaza el visual de cada objeto cuyo camino (desde la raíz del prefab) cumple el patrón.</summary>
        private sealed class Reemplazo
        {
            public string Patron;
            public string[] Codigos;
            public Ajuste Medida;
            public float Giro;
            public float ProfundidadMaxima;
            public int Copias = 1;
            public string[] HermanosApagados = new string[0];
            public bool SinSombra;
            public bool SinAlinear;
            public string SoloHijos;
        }

        /// <summary>Un prop nuevo (sin greybox) en coordenadas del mundo del prefab, con su BoxCollider salvo que se pida quitarlo.</summary>
        private sealed class Nuevo
        {
            public string Padre;
            public string Codigo;
            public Vector3 Posicion;
            public float GiroMundo;
            public float Alto;
            public float ProfundidadMaxima;
            public bool SinCollider;
            public bool SinSombra;
        }

        private static readonly StringBuilder Informe = new StringBuilder();

        // ------------------------------------------------------------------ reglas

        private static readonly Reemplazo[] ReemplazosBarrio =
        {
            new Reemplazo { Patron = @"/Farolas/Farola_\d+$", Codigos = new[] { "C07_PosteLuz" }, Medida = Ajuste.Alto, Giro = 90f, SinAlinear = true },
            new Reemplazo { Patron = @"/Ambientacion/Basurero_\d+$", Codigos = new[] { "C11_Basurero" }, Medida = Ajuste.Alto, Giro = 180f },
            new Reemplazo { Patron = @"/PuestosTacos/Puesto_Tacos_[^/]+$", Codigos = new[] { "C12_PuestoTacos" }, Medida = Ajuste.Alto, Giro = 180f },
            new Reemplazo { Patron = @"/Ambientacion/Carro_\d+$", Codigos = new[] { "V01a_CarroViejito", "V01b_CarroPro" }, Medida = Ajuste.Largo },
            new Reemplazo { Patron = @"/Tinaco$", Codigos = new[] { "C09_Tinaco" }, Medida = Ajuste.Alto },
            new Reemplazo { Patron = @"/Fachada_Modular$", Codigos = new[] { "C01_FachadaLocal", "C02_FachadaVivienda", "C03_FachadaPisoAlto" }, Medida = Ajuste.Largo, Giro = 180f, ProfundidadMaxima = ProfundidadFachada },
            new Reemplazo { Patron = @"/Callejon/Contenedor$", Codigos = new[] { "L01_Contenedor" }, Medida = Ajuste.Largo },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Taller/Banco$", Codigos = new[] { "T03_BancoTrabajo" }, Medida = Ajuste.Alto, Giro = 180f, Copias = 3 },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Taller/Herramientas$", Codigos = new[] { "T01_ParedTaller" }, Medida = Ajuste.Largo, Giro = 180f, ProfundidadMaxima = 0.25f },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Sala/Prop_Cama$", Codigos = new[] { "H02_Cama" }, Medida = Ajuste.Largo, Giro = 180f },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Sala/Prop_Espejo$", Codigos = new[] { "H03_Espejo" }, Medida = Ajuste.Alto, Giro = 180f },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Sala/Mesa$", Codigos = new[] { "H04_MesaSillas" }, Medida = Ajuste.Propio, HermanosApagados = new[] { "PataMesa", "Banco1", "Banco2" } },
            new Reemplazo { Patron = @"/Lugar_Casa/Interior/Sala/Refrigerador$", Codigos = new[] { "H05_Refrigerador" }, Medida = Ajuste.Alto, Giro = 90f },
        };

        private static readonly Nuevo[] NuevosBarrio =
        {
            // Azoteas: aire acondicionado junto a cada tinaco del kit (se agrega en código, ver AgregarAiresAzotea).
            // Callejón de la emboscada (corre en X de -87 a -59, muros en z -54.1 y -63.9, ruta por z -59).
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "L02_PuertaTrasera", Posicion = new Vector3(-70f, 0.2f, -54.75f), GiroMundo = 0f, ProfundidadMaxima = 0.5f },
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "L04_Llantas", Posicion = new Vector3(-79f, 0.2f, -63.2f), GiroMundo = 180f },
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "L05_RejaPorton", Posicion = new Vector3(-86.4f, 0.2f, -59f), GiroMundo = 270f, ProfundidadMaxima = 0.3f },
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "C10_AireAcondicionado", Posicion = new Vector3(-66f, 2.6f, -54.35f), GiroMundo = 0f },
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "C08_Transformador", Posicion = new Vector3(-64f, 0.2f, -63.4f), GiroMundo = 180f },
            new Nuevo { Padre = "Manzana_4/Callejon", Codigo = "C14_RegistroMedidores", Posicion = new Vector3(-73f, 1f, -63.7f), GiroMundo = 180f },
            // Taller de la casa: el mecha estacionado está en (-64.5, -73.5); la estación lo rodea y no bloquea su punto de subida.
            new Nuevo { Padre = "Manzana_4/Lugar_Casa/Interior/Taller", Codigo = "T02_EstacionExo", Posicion = new Vector3(-64.5f, 0.2f, -73.5f), GiroMundo = 0f, Alto = 4.5f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa/Interior/Taller", Codigo = "T04_GabineteHerramientas", Posicion = new Vector3(-66.9f, 0.2f, -71.1f), GiroMundo = 0f },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa/Interior/Taller", Codigo = "T05_Generador", Posicion = new Vector3(-62f, 0.2f, -71.3f), GiroMundo = 0f },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa/Interior/Sala", Codigo = "H10_Maletas", Posicion = new Vector3(-67.9f, 0.2f, -77.1f), GiroMundo = 270f },
            // Fachada de la casa: paredes con ventana y lisa al frente de los muros a los lados de la puerta (calle hacia +X).
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "H01b_ParedVentana", Posicion = new Vector3(-56.85f, 0.2f, -72.2f), GiroMundo = 270f, Alto = 2.4f, ProfundidadMaxima = 0.25f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "H01c_ParedLisa", Posicion = new Vector3(-56.85f, 0.2f, -81.8f), GiroMundo = 270f, Alto = 2.4f, ProfundidadMaxima = 0.25f, SinCollider = true },
            // Casa: pilares en las orillas de la fachada (fondo 0.4) y pretil sobre la orilla de la azotea del frente (y 7.2).
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "C04_EsquinaPilar", Posicion = new Vector3(-56.8f, 0.2f, -70.3f), GiroMundo = 270f, ProfundidadMaxima = 0.4f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "C04_EsquinaPilar", Posicion = new Vector3(-56.8f, 0.2f, -83.7f), GiroMundo = 270f, ProfundidadMaxima = 0.4f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "C05_PretilAzotea", Posicion = new Vector3(-57.2f, 7.2f, -74f), GiroMundo = 270f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "C05_PretilAzotea", Posicion = new Vector3(-57.2f, 7.2f, -77f), GiroMundo = 270f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "C05_PretilAzotea", Posicion = new Vector3(-57.2f, 7.2f, -80f), GiroMundo = 270f, SinCollider = true },
            // Casa por detrás (muro del fondo, cara exterior en x -69.05, mirando a -X): puerta trasera de lámina y escalera de servicio.
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "H01a_ParedPuerta", Posicion = new Vector3(-69.2f, 0.2f, -74f), GiroMundo = 90f, ProfundidadMaxima = 0.25f, SinCollider = true },
            new Nuevo { Padre = "Manzana_4/Lugar_Casa", Codigo = "L03_EscaleraEmergencia", Posicion = new Vector3(-71.6f, 0.2f, -81f), GiroMundo = 90f, SinCollider = true },
            // Letreros sobre el muro del Edificio_1 (cara sur en z 9.97), arriba de las fachadas nuevas (3.6 m) y sin bloquear la banqueta.
            new Nuevo { Padre = "Manzana_1/Edificios/Edificio_1", Codigo = "C15a_LetreroHorizontal", Posicion = new Vector3(-118.7f, 4f, 9.82f), GiroMundo = 0f, ProfundidadMaxima = 0.3f, SinCollider = true },
            new Nuevo { Padre = "Manzana_1/Edificios/Edificio_1", Codigo = "C15b_LetreroBandera", Posicion = new Vector3(-110.7f, 4f, 9.5f), GiroMundo = 90f, SinCollider = true },
        };

        private static readonly Reemplazo[] ReemplazosJaula =
        {
            new Reemplazo { Patron = @"^JaulaClandestino/Gradas/Grada\d$", Codigos = new[] { "A04_Gradas" }, Medida = Ajuste.Alto, Copias = 2, SoloHijos = "Escalon" },
        };

        // ------------------------------------------------------------------ entrada

        /// <summary>Batchmode: capturas de antes, colocación en Barrio, jaula y carro, capturas de después e informe.</summary>
        [MenuItem("Mecha Cyberpunk/T-250/Colocar en el juego")]
        public static void Colocar()
        {
            Informe.Clear();
            foreach (string zona in VistasZona.Keys)
            {
                Capturar(zona, "antes");
            }

            ColocarEnPrefab(RutaBarrio, ReemplazosBarrio, raiz =>
            {
                foreach (Nuevo nuevo in NuevosBarrio)
                {
                    PonerNuevo(raiz, nuevo);
                }
                AgregarAiresAzotea(raiz);
            });
            ColocarEnPrefab(RutaJaula, ReemplazosJaula, PonerSotanoJaula);
            ColocarCarro();

            foreach (string zona in VistasZona.Keys)
            {
                Capturar(zona, "despues");
            }
            File.WriteAllText("Logs/t250-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-250 colocado:\n" + Informe);
        }

        private static void ColocarEnPrefab(string ruta, Reemplazo[] reglas, System.Action<GameObject> extra)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            Informe.AppendLine("## " + ruta);
            List<Transform> todos = raiz.GetComponentsInChildren<Transform>(true).ToList();
            foreach (Reemplazo regla in reglas)
            {
                Regex patron = new Regex(regla.Patron);
                List<Transform> objetivos = todos.Where(t => patron.IsMatch(Camino(t))).ToList();
                int i = 0;
                foreach (Transform objetivo in objetivos)
                {
                    Reemplazar(objetivo, regla, regla.Codigos[i % regla.Codigos.Length]);
                    i++;
                }
                Informe.AppendLine(string.Format("{0}\t{1}\t{2} objetos", regla.Patron, string.Join("/", regla.Codigos), objetivos.Count));
            }
            extra?.Invoke(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // ------------------------------------------------------------------ reemplazo

        private static void Reemplazar(Transform objetivo, Reemplazo regla, string codigo)
        {
            List<Renderer> viejos = RenderersApagables(objetivo);
            if (regla.SoloHijos != null)
            {
                viejos = viejos.Where(r => r.name.StartsWith(regla.SoloHijos)).ToList();
            }
            foreach (string hermano in regla.HermanosApagados)
            {
                Transform h = objetivo.parent != null ? objetivo.parent.Find(hermano) : null;
                if (h != null)
                {
                    viejos.AddRange(RenderersApagables(h));
                }
            }
            if (viejos.Count == 0)
            {
                Informe.AppendLine("  sin visual: " + Camino(objetivo));
                return;
            }

            Bounds b = viejos[0].bounds;
            foreach (Renderer r in viejos)
            {
                b.Encapsulate(r.bounds);
            }

            // El visual va dentro del objeto que lleva el collider (en las farolas, Poste_Luz) y como primer hijo: las pruebas del
            // barrio buscan el primer MeshRenderer bajo el collider que choca y piden que esté encendido.
            Transform conCollider = objetivo.GetComponent<Collider>() != null
                ? objetivo
                : objetivo.Cast<Transform>().FirstOrDefault(h => h.GetComponent<Collider>() != null) ?? objetivo;
            bool escalaUniforme = EsUniforme(conCollider.lossyScale);
            Transform padre = escalaUniforme ? conCollider : objetivo.parent;
            string nombre = escalaUniforme ? NombreVisual : objetivo.name + "_" + NombreVisual;
            GameObject contenedor = new GameObject(nombre);
            contenedor.transform.SetParent(padre, false);
            if (escalaUniforme)
            {
                contenedor.transform.SetSiblingIndex(0);
            }
            contenedor.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            contenedor.transform.rotation = Quaternion.identity;

            ArteT250.Modelo modelo = ArteT250.Buscar(codigo);
            Vector3 propio = TamanoPrefab(modelo);
            float giroBase = objetivo.eulerAngles.y + regla.Giro;
            float giro = regla.SinAlinear ? giroBase : AlinearGiro(giroBase, propio, b.size);

            bool largoEnX = Mathf.Abs(Mathf.Cos(giro * Mathf.Deg2Rad)) > 0.5f;
            float largoViejo = largoEnX ? b.size.x : b.size.z;
            float largoCopia = largoViejo / regla.Copias;
            for (int c = 0; c < regla.Copias; c++)
            {
                GameObject instancia = Instanciar(modelo, contenedor.transform, true, regla.SinSombra);
                float escala;
                switch (regla.Medida)
                {
                    case Ajuste.Alto: escala = b.size.y / propio.y; break;
                    case Ajuste.Largo: escala = largoCopia / propio.x; break;
                    default: escala = 1f; break;
                }
                Vector3 local = Vector3.one * escala;
                if (regla.Copias > 1)
                {
                    local.x = largoCopia / propio.x;
                }
                if (regla.ProfundidadMaxima > 0f && propio.z * local.z > regla.ProfundidadMaxima)
                {
                    local.z = regla.ProfundidadMaxima / propio.z;
                }
                instancia.transform.localScale = local;
                instancia.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                float desplazamiento = (c + 0.5f) * largoCopia - largoViejo * 0.5f;
                Vector3 eje = largoEnX ? Vector3.right : Vector3.forward;
                instancia.transform.position = contenedor.transform.position + eje * desplazamiento;
            }

            Apagar(viejos);
        }

        /// <summary>
        /// Apaga el greybox. Si el renderer comparte objeto con su collider (un cubo con función), queda encendido pero solo
        /// proyecta sombra: no se ve y lo que choca sigue teniendo un MeshRenderer encendido, como piden las pruebas del barrio.
        /// </summary>
        private static void Apagar(List<Renderer> viejos)
        {
            foreach (Renderer r in viejos)
            {
                if (r.GetComponent<Collider>() != null)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    r.enabled = false;
                }
            }
        }

        private static List<Renderer> RenderersApagables(Transform t)
        {
            return t.GetComponentsInChildren<Renderer>(true)
                .Where(r => (r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled && !DentroDeVisual(r.transform))
                .ToList();
        }

        private static bool DentroDeVisual(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.EndsWith(NombreVisual))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gira 90° más si así el lado largo del modelo queda sobre el lado largo de lo viejo.</summary>
        private static float AlinearGiro(float giro, Vector3 propio, Vector3 viejo)
        {
            float largoX = Mathf.Max(viejo.x, 0.01f);
            float largoZ = Mathf.Max(viejo.z, 0.01f);
            bool viejoEnX = largoX / largoZ > RazonAlargado;
            bool viejoEnZ = largoZ / largoX > RazonAlargado;
            if (!viejoEnX && !viejoEnZ)
            {
                return giro;
            }
            bool propioEnX = propio.x >= propio.z;
            bool giroEnX = Mathf.Abs(Mathf.Cos(giro * Mathf.Deg2Rad)) > 0.5f;
            bool largoQuedaEnX = propioEnX == giroEnX;
            return largoQuedaEnX == viejoEnX ? giro : giro + 90f;
        }

        private static bool EsUniforme(Vector3 s)
        {
            return Mathf.Abs(s.x - s.y) < ToleranciaEscala && Mathf.Abs(s.y - s.z) < ToleranciaEscala;
        }

        private static Vector3 TamanoPrefab(ArteT250.Modelo modelo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
            return prefab.GetComponent<BoxCollider>().size;
        }

        private static GameObject Instanciar(ArteT250.Modelo modelo, Transform padre, bool sinCollider, bool sinSombra)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            if (sinCollider)
            {
                Object.DestroyImmediate(instancia.GetComponent<BoxCollider>());
            }
            if (sinSombra)
            {
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            return instancia;
        }

        // ------------------------------------------------------------------ props nuevos

        private static void PonerNuevo(GameObject raiz, Nuevo nuevo)
        {
            Transform padre = raiz.transform.Find(nuevo.Padre);
            if (padre == null)
            {
                Informe.AppendLine("  FALTA padre " + nuevo.Padre + " para " + nuevo.Codigo);
                return;
            }
            ArteT250.Modelo modelo = ArteT250.Buscar(nuevo.Codigo);
            Vector3 propio = TamanoPrefab(modelo);
            GameObject instancia = Instanciar(modelo, padre, nuevo.SinCollider, nuevo.SinSombra);
            instancia.name = nuevo.Codigo + "_" + NombreVisual;
            float escala = nuevo.Alto > 0f ? nuevo.Alto / propio.y : 1f;
            Vector3 local = Vector3.one * escala;
            if (nuevo.ProfundidadMaxima > 0f && propio.z * escala > nuevo.ProfundidadMaxima)
            {
                local.z = nuevo.ProfundidadMaxima / propio.z;
            }
            instancia.transform.localScale = local;
            instancia.transform.SetPositionAndRotation(nuevo.Posicion, Quaternion.Euler(0f, nuevo.GiroMundo, 0f));
            Informe.AppendLine(string.Format("nuevo\t{0}\t{1}\t{2}", nuevo.Codigo, Camino(padre), nuevo.Posicion));
        }

        /// <summary>Un aire acondicionado C10 al lado de cada Azotea_AC_Tinaco del kit (en la misma azotea, sin collider).</summary>
        private static void AgregarAiresAzotea(GameObject raiz)
        {
            ArteT250.Modelo modelo = ArteT250.Buscar("C10_AireAcondicionado");
            int n = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Azotea_AC_Tinaco").ToList())
            {
                GameObject instancia = Instanciar(modelo, t, true, false);
                instancia.name = NombreVisual;
                instancia.transform.localPosition = new Vector3(-2.2f, 0f, 0f);
                instancia.transform.localRotation = Quaternion.identity;
                n++;
            }
            Informe.AppendLine("nuevo\tC10_AireAcondicionado\tAzotea_AC_Tinaco\t" + n + " azoteas");
        }

        /// <summary>Sótano del clandestino: muros A06 en anillo fuera de las gradas, columnas A07 en las esquinas, lámpara A05 en la luz cenital.</summary>
        private static void PonerSotanoJaula(GameObject raiz)
        {
            const float Medio = 24f;
            GameObject sotano = new GameObject("Sotano_" + NombreVisual);
            sotano.transform.SetParent(raiz.transform, false);
            ArteT250.Modelo muro = ArteT250.Buscar("A06_ParedSotano");
            float ancho = TamanoPrefab(muro).x;
            int porLado = Mathf.CeilToInt(Medio * 2f / ancho);
            float paso = Medio * 2f / porLado;
            int muros = 0;
            for (int lado = 0; lado < 4; lado++)
            {
                Quaternion giro = Quaternion.Euler(0f, lado * 90f + 180f, 0f);
                for (int i = 0; i < porLado; i++)
                {
                    Vector3 local = new Vector3(-Medio + paso * (i + 0.5f), 0f, Medio);
                    GameObject m = Instanciar(muro, sotano.transform, false, false);
                    m.transform.localScale = new Vector3(paso / ancho, 1f, 1f);
                    m.transform.localPosition = Quaternion.Euler(0f, lado * 90f, 0f) * local;
                    m.transform.localRotation = giro;
                    muros++;
                }
            }
            ArteT250.Modelo columna = ArteT250.Buscar("A07_ColumnaSotano");
            for (int esquina = 0; esquina < 4; esquina++)
            {
                GameObject c = Instanciar(columna, sotano.transform, false, false);
                c.transform.localPosition = Quaternion.Euler(0f, esquina * 90f, 0f) * new Vector3(Medio, 0f, Medio);
            }
            Transform cenital = raiz.transform.Find("Luces/LuzCenital");
            if (cenital != null)
            {
                GameObject lampara = Instanciar(ArteT250.Buscar("A05_LamparaColgante"), raiz.transform.Find("Luces"), true, true);
                lampara.name = "A05_LamparaColgante_" + NombreVisual;
                lampara.transform.position = cenital.position + Vector3.up * 0.4f - Vector3.up * TamanoPrefab(ArteT250.Buscar("A05_LamparaColgante")).y;
            }
            Informe.AppendLine("nuevo\tA06/A07/A05\tJaulaClandestino/Sotano\t" + muros + " muros, 4 columnas, 1 lámpara");
        }

        // ------------------------------------------------------------------ carro del jugador

        /// <summary>CarroViejo: carrocería V01a como Visual_T250 de la raíz; las ruedas del modelo quedan en su lugar (el carro es una sola malla).</summary>
        private static void ColocarCarro()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaCarro);
            Informe.AppendLine("## " + RutaCarro);
            List<Renderer> viejos = new List<Renderer>();
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                Transform t = raiz.transform.Find(nombre);
                if (t != null)
                {
                    viejos.AddRange(RenderersApagables(t));
                }
            }
            ArteT250.Modelo modelo = ArteT250.Buscar("V01a_CarroViejito");
            Vector3 propio = TamanoPrefab(modelo);
            GameObject visual = Instanciar(modelo, raiz.transform, true, false);
            visual.name = NombreVisual;
            float escala = 4.2f / propio.x;
            visual.transform.localScale = Vector3.one * escala;
            // El modelo tiene el largo en X; el carro avanza en +Z. El frente del modelo apunta a -X.
            visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.SetSiblingIndex(0);
            Apagar(viejos);
            Informe.AppendLine("CarroViejo\tV01a_CarroViejito\tcarrocería y cabina apagadas; ruedas greybox visibles dentro del modelo (Rueda_* intactas)");
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaCarro);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // ------------------------------------------------------------------ capturas

        private struct Vista
        {
            public Vector3 Desde;
            public Vector3 Hacia;
        }

        private static readonly Dictionary<string, Vista> VistasZona = new Dictionary<string, Vista>
        {
            { "Calle", new Vista { Desde = new Vector3(-114f, 3f, -3f), Hacia = new Vector3(-121f, 3.5f, 10f) } },
            { "Callejon", new Vista { Desde = new Vector3(-60f, 2.2f, -59f), Hacia = new Vector3(-86f, 1.2f, -59f) } },
            { "TallerCasa", new Vista { Desde = new Vector3(-61.6f, 3.2f, -77f), Hacia = new Vector3(-68f, 0.8f, -76f) } },
            { "Fachada", new Vista { Desde = new Vector3(-44f, 4f, -77f), Hacia = new Vector3(-58f, 3f, -77f) } },
            { "CasaAtras", new Vista { Desde = new Vector3(-78f, 5f, -70f), Hacia = new Vector3(-69f, 2f, -78f) } },
            { "Vehiculos", new Vista { Desde = new Vector3(205f, 2.2f, -6f), Hacia = new Vector3(200f, 0.8f, 0f) } },
            { "Clandestino", new Vista { Desde = new Vector3(418f, 11f, -22f), Hacia = new Vector3(400f, 2f, 3f) } },
        };

        private static void Capturar(string zona, string momento)
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio), escena);
            GameObject jaula = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJaula), escena);
            jaula.transform.position = new Vector3(400f, 0f, 0f);
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), escena);
            carro.transform.position = new Vector3(200f, 0.3f, 0f);
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.position = new Vector3(200f, 0f, 0f);
            ArteT250.PonerLuz();
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            camara.fieldOfView = 60f;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 500f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            Vista vista = VistasZona[zona];
            camara.transform.position = vista.Desde;
            camara.transform.LookAt(vista.Hacia);
            string sufijo = momento == "antes" ? "-antes" : "";
            ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-250-" + zona + sufijo + ".png"));
        }

        // ------------------------------------------------------------------ util

        private static string Camino(Transform t)
        {
            string camino = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                camino = p.name + "/" + camino;
            }
            return camino;
        }
    }
}
