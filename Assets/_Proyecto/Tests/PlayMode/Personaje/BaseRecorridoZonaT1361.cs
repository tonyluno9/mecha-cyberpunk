using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1361: base de los recorridos en Play de una zona (Cerro, Puerto). Instancia SOLO el prefab de la zona, sin la escena de la ciudad,
    /// y mete al jugador a pie o a un carro con entrada simulada (teclado). Cada prueba junta hallazgos (atascos, caídas, saltos que no salen)
    /// en Logs/t1361-recorrido-nombre.txt y, de los puntos malos, hasta 2 capturas en docs/capturas/bugs-18nov-B/.
    /// Las rutas salen de los CalleZona y de los objetos del prefab, no de coordenadas escritas aquí.
    /// </summary>
    public abstract class BaseRecorridoZonaT1361 : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V09.prefab";
        private const string CarpetaCapturas = "docs/capturas/bugs-18nov-B";
        private const string CarpetaInformes = "Logs";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const int MaxCapturas = 2;

        // Caminar.
        private const float AnguloParaAvanzar = 40f;
        private const float SuavizadoGiro = 0.6f;
        private const float SegundosMuestraAtasco = 1f;
        private const float AvanceMinimoAtasco = 0.3f;
        private const int AtascosSeguidosParaAbortar = 4;
        public const int MaxAtascosPorParte = 2;

        // Carro.
        private const float RapidezAtoro = 1f;
        private const float SegundosAtoro = 2f;
        private const float SegundosArranque = 2.5f;
        private const float AnguloCurvaCarro = 25f;
        private const float RapidezCurvaCarro = 5f;
        private const float MargenFrenado = 1.5f;
        private const float AnguloVuelta = 15f;
        private const float AlturaSobreSueloCarro = 0.4f;
        private const float MargenSueloCarro = 0.5f;
        private const float InclinacionVolcado = 0.3f;
        private const float SegundosVolcado = 1f;
        private const float CercaDelFinalCarro = 4f;

        // Saltos.
        private const float SegundosSalto = 6f;
        private const float AlturaRayoBorde = 0.6f;
        private const float AdelanteBorde = 0.8f;
        private const float MargenTecho = 0.8f;
        private const float MargenPlanta = 0.3f;
        private const float CaidaALaCalle = 3f;

        // Trepar.
        private const float SegundosTrepar = 8f;
        private const float MargenAlturaTrepar = 0.4f;
        private const float DistanciaPartida = 1.5f;

        protected Keyboard teclado;
        protected Mouse raton;
        protected GameObject zona;
        protected GameObject jugador;
        protected GameObject carroObjeto;
        protected ControladorAPie controlador;
        protected TreparAPie trepar;
        protected CharacterController cc;
        protected CarroViejo carro;
        protected bool llego;
        protected int atascos;
        protected int caidas;
        protected int saltosFallidos;
        protected int treparFallidos;
        protected int volcados;

        private GameObject luz;
        private DatosPersonaje datos;
        private readonly List<string> hallazgos = new List<string>();
        private int capturas;
        private Vector3 ultimoRumbo = Vector3.forward;
        private bool wAbajo;
        private bool correrAbajo;
        private bool cw;
        private bool cs;
        private bool ca;
        private bool cd;

        /// <summary>Ruta del prefab de la zona.</summary>
        protected abstract string RutaZona { get; }

        /// <summary>Por debajo de esta altura el jugador "cayó al vacío".</summary>
        protected abstract float AlturaVacio { get; }

        public override void Setup()
        {
            base.Setup();
            MechaCyberpunk.Core.Pausa.Fijar(false);
            Time.timeScale = 1f;
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            hallazgos.Clear();
            capturas = 0;
            atascos = caidas = saltosFallidos = treparFallidos = volcados = 0;
            wAbajo = correrAbajo = cw = cs = ca = cd = false;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (Object o in new Object[] { jugador, carroObjeto, zona, luz, datos })
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator EscribirInforme()
        {
            Time.timeScale = 1f;
            SoltarTodo();
            string nombre = NombrePrueba();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1361 recorrido: " + TestContext.CurrentContext.Test.Name + " (" + RutaZona + ")");
            sb.AppendLine("Resultado: " + TestContext.CurrentContext.Result.Outcome.Status);
            sb.AppendLine("Atascos: " + atascos + "  Caidas al vacio: " + caidas + "  Saltos fallidos: " + saltosFallidos
                + "  Trepar fallidos: " + treparFallidos + "  Volcados: " + volcados);
            sb.AppendLine("Hallazgos: " + hallazgos.Count);
            foreach (string h in hallazgos)
            {
                sb.AppendLine("  " + h);
            }
            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", CarpetaInformes, "t1361-recorrido-" + nombre + ".txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, sb.ToString());
            yield return null;
        }

        private static string NombrePrueba()
        {
            string nombre = TestContext.CurrentContext.Test.Name;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nombre = nombre.Replace(c, '_');
            }
            return nombre.Replace(' ', '_');
        }

        // ---------------------------------------------------------------------------------------------------------------- escena

        /// <summary>Calienta los frames, instancia el prefab de la zona y una luz para las capturas.</summary>
        protected IEnumerator Preparar()
        {
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaZona);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaZona);
            zona = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            luz = new GameObject("LuzRecorridoT1361");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.8f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            yield return null;
            yield return null;
            Physics.SyncTransforms();
        }

        protected static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        protected static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        protected static float Plano(Vector3 a, Vector3 b)
        {
            return RutaRecorridoT1361.Plano(a, b);
        }

        /// <summary>El CalleZona de la zona con ese nombre, como ruta.</summary>
        protected RutaRecorridoT1361 RutaDe(string nombreCalle)
        {
            CalleZona calle = zona.GetComponentsInChildren<CalleZona>(true).FirstOrDefault(c => c.name == nombreCalle);
            Assert.IsNotNull(calle, "No hay un CalleZona llamado " + nombreCalle + " en " + RutaZona);
            Assert.GreaterOrEqual(calle.Cantidad, 2, nombreCalle + " debe tener al menos 2 puntos");
            return new RutaRecorridoT1361(calle.Puntos());
        }

        /// <summary>La altura del piso bajo (x, z), mirando desde <paramref name="desdeY"/> + 6 m hacia abajo; ignora al jugador y al carro.</summary>
        protected float SueloEn(Vector3 p, float desdeY)
        {
            RaycastHit[] golpes = Physics.RaycastAll(new Vector3(p.x, desdeY + 6f, p.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.NaN;
            foreach (RaycastHit g in golpes)
            {
                if ((jugador != null && g.collider.transform.IsChildOf(jugador.transform))
                    || (carroObjeto != null && g.collider.transform.IsChildOf(carroObjeto.transform)))
                {
                    continue;
                }
                if (float.IsNaN(mejor) || g.point.y > mejor)
                {
                    mejor = g.point.y;
                }
            }
            return float.IsNaN(mejor) ? p.y : mejor;
        }

        // ---------------------------------------------------------------------------------------------------------------- hallazgos

        protected void Hallazgo(string texto, Vector3 posicion, bool capturar)
        {
            string linea = "t=" + Time.time.ToString("F1") + " " + texto + " en " + posicion.ToString("F1");
            if (capturar && capturas < MaxCapturas)
            {
                capturas++;
                string ruta = Capturar(posicion, capturas);
                linea += " | captura " + ruta;
            }
            hallazgos.Add(linea);
        }

        protected void Nota(string texto)
        {
            hallazgos.Add("t=" + Time.time.ToString("F1") + " " + texto);
        }

        private string Capturar(Vector3 foco, int numero)
        {
            GameObject go = new GameObject("CamCapturaT1361");
            Camera cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 500f;
            cam.fieldOfView = 60f;
            go.transform.position = foco - ultimoRumbo * 9f + Vector3.up * 4.5f;
            go.transform.LookAt(foco + Vector3.up);
            const int ancho = 1280;
            const int alto = 720;
            RenderTexture rt = new RenderTexture(ancho, alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            textura.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            string relativa = CarpetaCapturas + "/recorrido-" + NombrePrueba() + "-" + numero + ".png";
            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativa));
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            return relativa;
        }

        // ---------------------------------------------------------------------------------------------------------------- jugador

        protected void CrearJugador(Vector3 posicion, Vector3 mirar)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaJugador);
            jugador = Object.Instantiate(prefab, posicion, Quaternion.LookRotation(Horizontal(mirar)));
            controlador = jugador.GetComponent<ControladorAPie>();
            trepar = jugador.GetComponent<TreparAPie>();
            cc = jugador.GetComponent<CharacterController>();
            Assert.IsNotNull(controlador, "JugadorAPie debe traer ControladorAPie");
            Assert.IsNotNull(trepar, "JugadorAPie debe traer TreparAPie");
            jugador.GetComponent<MechaCyberpunk.Personaje.Vida.EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            datos = ScriptableObject.CreateInstance<DatosPersonaje>();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
            // La zona pesa millones de triángulos: el jugador no renderiza (las capturas usan su propia cámara).
            foreach (Camera c in jugador.GetComponentsInChildren<Camera>(true))
            {
                c.enabled = false;
            }
            foreach (AudioListener a in jugador.GetComponentsInChildren<AudioListener>(true))
            {
                a.enabled = false;
            }
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            controlador.enabled = false;
            controlador.enabled = true;
        }

        private static Vector3 Horizontal(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
        }

        protected void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            jugador.transform.rotation = Quaternion.LookRotation(Horizontal(mirar));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
            ultimoRumbo = Horizontal(mirar);
        }

        /// <summary>Crea al jugador (o lo traslada) al inicio de la ruta, sobre el piso, mirando por ella.</summary>
        protected IEnumerator PonerEnRuta(RutaRecorridoT1361 ruta)
        {
            Vector3 inicio = ruta.Inicio;
            Vector3 dir = ruta.Direccion(0f);
            Vector3 pos = new Vector3(inicio.x, SueloEn(inicio, inicio.y) + 0.05f, inicio.z);
            if (jugador == null)
            {
                CrearJugador(pos, dir);
            }
            else
            {
                Poner(pos, dir);
            }
            yield return Esperar(0.5f);
        }

        protected void Avanzar(bool activo)
        {
            if (wAbajo == activo)
            {
                return;
            }
            wAbajo = activo;
            if (activo)
            {
                Press(teclado.wKey);
            }
            else
            {
                Release(teclado.wKey);
            }
        }

        protected void Correr(bool activo)
        {
            if (correrAbajo == activo)
            {
                return;
            }
            correrAbajo = activo;
            if (activo)
            {
                Press(teclado.leftShiftKey);
            }
            else
            {
                Release(teclado.leftShiftKey);
            }
        }

        protected void SoltarTodo()
        {
            if (teclado == null)
            {
                return;
            }
            Avanzar(false);
            Correr(false);
            Aplicar(false, false, false, false);
        }

        protected IEnumerator Saltar()
        {
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return null;
        }

        private void MirarHacia(Vector3 destino)
        {
            Vector3 d = Horizontal(destino - jugador.transform.position);
            float error = Mathf.DeltaAngle(controlador.GiroCamara, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
            controlador.Empujar(error * SuavizadoGiro, 0f);
        }

        /// <summary>
        /// Camina por la ruta con W (y Shift) girando hacia un punto adelante. Pone <see cref="llego"/> si queda a menos de
        /// <paramref name="radioFin"/> del final. Registra cada atasco (avanza menos de 0.3 m en 1 s empujando) y cada caída al vacío.
        /// </summary>
        protected IEnumerator Caminar(RutaRecorridoT1361 ruta, float maxSegundos, float radioFin = 2f, float adelante = 3f, bool correr = true)
        {
            llego = false;
            float s = 0f;
            float inicio = Time.time;
            Vector3 pMuestra = jugador.transform.position;
            float tMuestra = Time.time;
            bool empujoTodo = true;
            int seguidos = 0;
            Correr(correr);
            float zonaFinal = Mathf.Min(6f, ruta.Largo * 0.5f);
            while (Time.time - inicio < maxSegundos)
            {
                yield return null;
                Vector3 p = jugador.transform.position;
                if (p.y < AlturaVacio)
                {
                    caidas++;
                    Hallazgo("CAIDA AL VACIO a " + (s).ToString("F0") + " m de " + ruta.Largo.ToString("F0"), p, true);
                    break;
                }
                s = ruta.Proyectar(p, Mathf.Max(0f, s - 3f), s + 15f);
                if (s > ruta.Largo - zonaFinal && Plano(p, ruta.Final) < radioFin)
                {
                    llego = true;
                    break;
                }
                Vector3 destino = ruta.PuntoEn(Mathf.Min(s + adelante, ruta.Largo));
                if (Plano(p, destino) < 0.05f)
                {
                    destino = ruta.Final;
                }
                ultimoRumbo = Horizontal(destino - p);
                Vector3 d = Horizontal(destino - p);
                float error = Mathf.DeltaAngle(controlador.GiroCamara, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                controlador.Empujar(error * SuavizadoGiro, 0f);
                bool avanzar = Mathf.Abs(error) < AnguloParaAvanzar;
                empujoTodo &= avanzar;
                Avanzar(avanzar);
                if (Time.time - tMuestra >= SegundosMuestraAtasco)
                {
                    float avance = Plano(p, pMuestra);
                    if (empujoTodo && avance < AvanceMinimoAtasco)
                    {
                        atascos++;
                        seguidos++;
                        Hallazgo("ATASCO a " + s.ToString("F0") + " m de " + ruta.Largo.ToString("F0") + " (avanzo " + avance.ToString("F2") + " m en 1 s)", p, true);
                        if (seguidos >= AtascosSeguidosParaAbortar)
                        {
                            Nota("se aborta la parte: " + seguidos + " atascos seguidos");
                            break;
                        }
                        yield return Saltar();
                    }
                    else if (avance >= AvanceMinimoAtasco)
                    {
                        seguidos = 0;
                    }
                    pMuestra = jugador.transform.position;
                    tMuestra = Time.time;
                    empujoTodo = true;
                }
            }
            SoltarTodo();
            yield return null;
            if (!llego && Time.time - inicio >= maxSegundos)
            {
                Hallazgo("NO LLEGA al final en " + maxSegundos.ToString("F0") + " s (s=" + s.ToString("F0") + " de " + ruta.Largo.ToString("F0") + ")", jugador.transform.position, true);
            }
        }

        /// <summary>Aserciones de una parte a pie: llegó, sin caídas y a lo más <see cref="MaxAtascosPorParte"/> atascos.</summary>
        protected void AfirmarParte(string nombre)
        {
            Assert.IsTrue(llego, nombre + ": no llegó al final (ver Logs/t1361-recorrido-" + NombrePrueba() + ".txt)");
            Assert.AreEqual(0, caidas, nombre + ": cayó al vacío");
            Assert.LessOrEqual(atascos, MaxAtascosPorParte, nombre + ": demasiados atascos");
        }

        // ---------------------------------------------------------------------------------------------------------------- carro

        private const float MetrosParaBordeZona = 10f;
        private const float ParadaAntesDelBorde = 8f;
        private const float MargenAlturaPiso = 2f;

        private static bool HayPisoEn(Vector3 p)
        {
            foreach (RaycastHit g in Physics.RaycastAll(new Vector3(p.x, p.y + 6f, p.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (g.point.y > p.y - MargenAlturaPiso)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Recorta la ruta a 8 m de cada extremo que está en el borde de la zona (unión con otra zona): el punto 10 m más allá, en la
        /// dirección de la calle, no tiene piso. Lo de fuera de la zona no está en la prueba.
        /// </summary>
        protected RutaRecorridoT1361 SinBordes(RutaRecorridoT1361 ruta)
        {
            float s0 = 0f;
            float s1 = ruta.Largo;
            float tope = Mathf.Min(ParadaAntesDelBorde, ruta.Largo * 0.25f);
            if (!HayPisoEn(ruta.Inicio - ruta.Direccion(0f) * MetrosParaBordeZona))
            {
                s0 = tope;
                Nota("el inicio de la calle es borde de la zona: se arranca a " + tope.ToString("F0") + " m");
            }
            if (!HayPisoEn(ruta.Final + ruta.Direccion(ruta.Largo) * MetrosParaBordeZona))
            {
                s1 = ruta.Largo - tope;
                Nota("el final de la calle es borde de la zona: se para a " + tope.ToString("F0") + " m del extremo");
            }
            return ruta.Tramo(s0, s1);
        }

        protected void CrearCarro(RutaRecorridoT1361 ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaCarro);
            Vector3 inicio = ruta.Inicio;
            Vector3 pos = new Vector3(inicio.x, SueloEn(inicio, inicio.y) + AlturaSobreSueloCarro, inicio.z);
            carroObjeto = Object.Instantiate(prefab, pos, Quaternion.LookRotation(ruta.Direccion(0f)));
            carro = carroObjeto.GetComponent<CarroViejo>();
            Assert.IsNotNull(carro, "Carro_V09 debe traer CarroViejo");
            carro.TomarControl(true);
            ultimoRumbo = ruta.Direccion(0f);
        }

        private void Aplicar(bool nw, bool ns, bool na, bool nd)
        {
            CambiarTecla(Key.W, ref cw, nw);
            CambiarTecla(Key.S, ref cs, ns);
            // Soltar un giro y apretar el contrario en el mismo cuadro deja a la acción sin leer el nuevo: primero se suelta.
            bool cambiaDeLado = (ca && nd) || (cd && na);
            CambiarTecla(Key.A, ref ca, na && !cambiaDeLado);
            CambiarTecla(Key.D, ref cd, nd && !cambiaDeLado);
        }

        private void CambiarTecla(Key tecla, ref bool actual, bool nuevo)
        {
            if (actual == nuevo)
            {
                return;
            }
            actual = nuevo;
            if (nuevo)
            {
                Press(teclado[tecla]);
            }
            else
            {
                Release(teclado[tecla]);
            }
        }

        /// <summary>
        /// Maneja el carro por la ruta con W/S/A/D. Registra atoros (menos de 1 m/s por 2 s fuera de las vueltas), volcaduras y caídas bajo el piso
        /// (y menor que la de la calle - 0.5 m). Pone <see cref="llego"/> si llega a menos de 4 m del final.
        /// </summary>
        protected IEnumerator Manejar(RutaRecorridoT1361 ruta, float maxSegundos, float rapidezMaxima)
        {
            llego = false;
            float s = 0f;
            float inicio = Time.time;
            float lento = 0f;
            float volcado = 0f;
            int atorosParte = 0;
            while (Time.time - inicio < maxSegundos)
            {
                yield return null;
                Transform t = carro.transform;
                Vector3 p = t.position;
                s = ruta.Proyectar(p, Mathf.Max(0f, s - 3f), s + 25f);
                float yCalle = ruta.PuntoEn(s).y;
                if (p.y < yCalle - MargenSueloCarro)
                {
                    caidas++;
                    Hallazgo("CARRO BAJO EL PISO (y " + p.y.ToString("F2") + " vs calle " + yCalle.ToString("F2") + ")", p, true);
                    break;
                }
                if (s > ruta.Largo - CercaDelFinalCarro)
                {
                    llego = true;
                    break;
                }
                if (Vector3.Dot(t.up, Vector3.up) < InclinacionVolcado)
                {
                    volcado += Time.deltaTime;
                    if (volcado >= SegundosVolcado)
                    {
                        volcados++;
                        Hallazgo("CARRO VOLCADO", p, true);
                        break;
                    }
                }
                else
                {
                    volcado = 0f;
                }
                float velocidad = carro.Velocidad;
                Vector3 objetivo = ruta.PuntoEn(Mathf.Min(s + 8f + Mathf.Abs(velocidad) * 0.6f, ruta.Largo));
                Vector3 rumbo = Horizontal(objetivo - p);
                ultimoRumbo = t.forward;
                float angulo = Vector3.SignedAngle(Horizontal(t.forward), rumbo, Vector3.up);
                float giroAdelante = Vector3.Angle(ruta.Direccion(s), ruta.Direccion(Mathf.Min(s + 20f, ruta.Largo)));
                float deseada = rapidezMaxima;
                bool enVuelta = Mathf.Abs(angulo) > AnguloCurvaCarro || giroAdelante > AnguloCurvaCarro;
                if (enVuelta)
                {
                    deseada = Mathf.Min(deseada, RapidezCurvaCarro);
                }
                bool acelerar = velocidad < deseada;
                bool frenar = velocidad > deseada + MargenFrenado;
                Aplicar(acelerar, frenar, angulo < -AnguloVuelta * 0.2f, angulo > AnguloVuelta * 0.2f);
                if (Time.time - inicio > SegundosArranque && carro.Rapidez < RapidezAtoro && !enVuelta)
                {
                    lento += Time.deltaTime;
                    if (lento >= SegundosAtoro)
                    {
                        atascos++;
                        atorosParte++;
                        Hallazgo("CARRO ATORADO a " + s.ToString("F0") + " m de " + ruta.Largo.ToString("F0"), p, true);
                        lento = -SegundosAtoro;
                        if (atorosParte >= AtascosSeguidosParaAbortar)
                        {
                            Nota("se aborta la parte: " + atorosParte + " atoros");
                            break;
                        }
                    }
                }
                else if (lento > 0f)
                {
                    lento = 0f;
                }
            }
            Aplicar(false, false, false, false);
            yield return null;
            if (!llego && Time.time - inicio >= maxSegundos)
            {
                Hallazgo("CARRO NO LLEGA al final en " + maxSegundos.ToString("F0") + " s (s=" + s.ToString("F0") + " de " + ruta.Largo.ToString("F0") + ")", carro.transform.position, true);
            }
        }

        protected void AfirmarParteCarro(string nombre)
        {
            Assert.IsTrue(llego, nombre + ": el carro no llegó al final (ver Logs/t1361-recorrido-" + NombrePrueba() + ".txt)");
            Assert.AreEqual(0, volcados, nombre + ": el carro se volteó");
            Assert.AreEqual(0, caidas, nombre + ": el carro cayó bajo el piso");
            Assert.LessOrEqual(atascos, MaxAtascosPorParte, nombre + ": demasiados atoros");
        }

        // ---------------------------------------------------------------------------------------------------------------- trepar y saltar

        /// <summary>Un techo o azotea: la caja del collider y la altura de arriba.</summary>
        protected struct Superficie
        {
            public string Nombre;
            public Bounds Caja;
            public Vector3 Centro => Caja.center;
            public float Techo => Caja.max.y;
        }

        /// <summary>Hueco horizontal (m) entre las cajas de dos superficies; 0 si se tocan o se traslapan.</summary>
        protected static float Hueco(Superficie a, Superficie b)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(a.Centro.x - b.Centro.x) - (a.Caja.extents.x + b.Caja.extents.x));
            float dz = Mathf.Max(0f, Mathf.Abs(a.Centro.z - b.Centro.z) - (a.Caja.extents.z + b.Caja.extents.z));
            return new Vector2(dx, dz).magnitude;
        }

        /// <summary>Corre desde atrás de <paramref name="a"/> hacia <paramref name="b"/> y salta en el borde. Falla si no queda sobre b (o agarrado y subido).</summary>
        protected IEnumerator SaltarEntre(Superficie a, Superficie b, bool saltoSiempre = false)
        {
            Vector3 dir = Horizontal(b.Centro - a.Centro);
            float h = Mathf.Abs(dir.x) * a.Caja.extents.x + Mathf.Abs(dir.z) * a.Caja.extents.z;
            Vector3 inicio = a.Centro - dir * Mathf.Max(0f, h - 1f);
            inicio.y = a.Techo + 0.05f;
            float hueco = Hueco(a, b);
            float desnivel = b.Techo - a.Techo;
            string etiqueta = a.Nombre + " -> " + b.Nombre + " (hueco " + hueco.ToString("F2") + " m, desnivel " + desnivel.ToString("F2") + " m)";
            Poner(inicio, dir);
            yield return Esperar(0.4f);
            Correr(true);
            Avanzar(true);
            float t0 = Time.time;
            Vector3 pMuestra = jugador.transform.position;
            float tMuestra = Time.time;
            bool exito = false;
            bool cayo = false;
            while (Time.time - t0 < SegundosSalto)
            {
                yield return null;
                Vector3 p = jugador.transform.position;
                MirarHacia(b.Centro);
                if (trepar.Trepando)
                {
                    Avanzar(false);
                    Correr(false);
                    continue;
                }
                bool dentro = Mathf.Abs(p.x - b.Centro.x) <= b.Caja.extents.x + MargenPlanta
                    && Mathf.Abs(p.z - b.Centro.z) <= b.Caja.extents.z + MargenPlanta;
                if (dentro && controlador.EnPiso && Mathf.Abs(p.y - b.Techo) < MargenTecho)
                {
                    exito = true;
                    break;
                }
                if (p.y < Mathf.Min(a.Techo, b.Techo) - CaidaALaCalle)
                {
                    cayo = true;
                    break;
                }
                if (controlador.EnPiso)
                {
                    Vector3 adelante = p + Horizontal(b.Centro - p) * AdelanteBorde + Vector3.up * AlturaRayoBorde;
                    bool hayPiso = Physics.Raycast(adelante, Vector3.down, AlturaRayoBorde * 2f, ~0, QueryTriggerInteraction.Ignore);
                    if (!hayPiso || saltoSiempre)
                    {
                        yield return Saltar();
                        saltoSiempre = false;
                        tMuestra = Time.time;
                        pMuestra = jugador.transform.position;
                        continue;
                    }
                }
                if (Time.time - tMuestra >= 0.5f)
                {
                    if (controlador.EnPiso && Plano(p, pMuestra) < 0.15f)
                    {
                        yield return Saltar();
                    }
                    pMuestra = jugador.transform.position;
                    tMuestra = Time.time;
                }
            }
            Avanzar(false);
            Correr(false);
            if (!exito)
            {
                saltosFallidos++;
                Hallazgo((cayo ? "SALTO: CAE " : "SALTO NO LLEGA ") + etiqueta, jugador.transform.position, true);
            }
            yield return Esperar(0.3f);
        }

        /// <summary>Sube por un PuntoTrepar con W desde 1.5 m delante de él. Suma a <see cref="treparFallidos"/> si no llega arriba con piso.</summary>
        protected IEnumerator Subir(PuntoTrepar p)
        {
            Vector3 partida = p.PieAbajo - p.Frente * DistanciaPartida + Vector3.up * 0.05f;
            if (jugador == null)
            {
                CrearJugador(partida, p.Frente);
            }
            Poner(partida, p.Frente);
            // TreparAPie recuerda el punto cercano y un teletransporte no manda OnTriggerExit: se reinicia para que no se enganche al anterior.
            trepar.enabled = false;
            yield return null;
            trepar.enabled = true;
            yield return Esperar(0.5f);
            float yInicio = p.transform.position.y;
            bool trepo = false;
            Press(teclado.wKey);
            wAbajo = true;
            float inicio = Time.time;
            while (Time.time - inicio < SegundosTrepar)
            {
                trepo |= trepar.Trepando;
                if (trepo && !trepar.Trepando && jugador.transform.position.y > yInicio + p.AlturaSubida - MargenAlturaTrepar)
                {
                    break;
                }
                yield return null;
            }
            Avanzar(false);
            yield return Esperar(0.5f);
            float y = jugador.transform.position.y;
            bool piso = Physics.RaycastAll(jugador.transform.position + Vector3.up * 0.3f, Vector3.down, 0.6f, ~0, QueryTriggerInteraction.Ignore)
                .Any(h => !h.collider.transform.IsChildOf(jugador.transform));
            string motivo = null;
            if (!trepo)
            {
                motivo = "no se engancha";
            }
            else if (y < yInicio + p.AlturaSubida - MargenAlturaTrepar)
            {
                motivo = "no llega arriba: sube " + (y - yInicio).ToString("F2") + " de " + p.AlturaSubida.ToString("F2");
            }
            else if (!piso)
            {
                motivo = "llega sin piso bajo los pies";
            }
            if (motivo != null)
            {
                treparFallidos++;
                Hallazgo("TREPAR " + p.name + " (" + (p.transform.parent != null ? p.transform.parent.name : "") + "): " + motivo, jugador.transform.position, true);
            }
        }
    }
}
