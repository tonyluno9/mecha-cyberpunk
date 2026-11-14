using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-999: la emboscada desde un techo en la ciudad real (Ciudad_Prologo). El jugador, en una azotea que no lo delata, daña a un pandillero
    /// sin ser visto: el grupo sigue acosando hasta el disparo, queda en sorpresa el retraso de DatosAcoso (más largo si el jugador está
    /// 2,5 m más alto) y después entra en alerta. Control: a ras de suelo, de frente y a 10 m, la alerta llega en menos de 0,5 s.
    /// Además comprueba que la azotea vecina del grupo de la azotea se cruza a su techo.
    /// </summary>
    public class EmboscadaTechoT999Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaDatosAcoso = "Assets/_Proyecto/IA/Datos/DatosAcoso_Base.asset";
        private const string CarpetaFotos = "docs/capturas/t999";
        private const string VariableCapturas = "T999_CAPTURAS";
        private const float EsperaSinVer = 2f;
        private const float ToleranciaRetraso = 0.2f;
        private const float SegundosAlertaDeFrente = 0.5f;
        private const float DistanciaDeFrente = 10f;
        private const float AlturaPecho = 1.1f;
        private const float AlturaOjosJugador = 1.6f;
        private const float DistanciaMinimaOido = 6f;
        private const float DistanciaMaximaTiro = 30f;
        private const float SaltoMaximo = 2.5f;
        private const float PasoSondeo = 0.25f;
        private const float AlcanceSondeo = 22f;
        private const float TolTecho = 0.8f;
        private const float DistanciaVecina = 10f;
        private const float DistanciaCarrera = 12f;
        private const float IntervaloSalto = 0.9f;
        private const float MargenAdentro = 2f;
        private const float EsperaInicial = 1.5f;
        private const float MaxSegundosFase = 8f;
        private const int Ancho = 1280;
        private const int Alto = 720;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject jugador;
        private ControladorAPie controlador;
        private CharacterController cc;
        private DatosAcoso acoso;

        [UnitySetUp]
        public IEnumerator DescargarCiudadPrevia()
        {
            yield return DescargarCiudades("EmboscadaT999_VaciaAntes");
        }

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            acoso = UnityEditor.AssetDatabase.LoadAssetAtPath<DatosAcoso>(RutaDatosAcoso);
            Assert.IsNotNull(acoso, "Falta DatosAcoso_Base.asset");
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            yield return DescargarCiudades("EmboscadaT999_VaciaDespues");
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private static IEnumerator DescargarCiudades(string nombreVacia)
        {
            bool hayCiudad = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                hayCiudad |= SceneManager.GetSceneAt(i).path.Contains("Ciudad");
            }
            if (!hayCiudad)
            {
                yield break;
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene(nombreVacia));
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        private IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorAPie>();
            Assert.IsNotNull(controlador, "Ciudad_Prologo debe traer al jugador a pie");
            jugador = controlador.gameObject;
            cc = jugador.GetComponent<CharacterController>();
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            controlador.enabled = false;
            controlador.enabled = true;
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse");
            int estables = 0;
            int intentos = 0;
            while (estables < 10 && intentos++ < 1000)
            {
                yield return null;
                estables = Time.deltaTime < 0.05f ? estables + 1 : 0;
            }
            // Tras empezar, la escena coloca al jugador en su punto de salida: se espera antes de teletransportarlo.
            yield return Esperar(EsperaInicial);
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            Vector3 plano = new Vector3(mirar.x, 0f, mirar.z);
            if (plano.sqrMagnitude > 1e-4f)
            {
                jugador.transform.rotation = Quaternion.LookRotation(plano);
            }
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
            Physics.SyncTransforms();
        }

        // ------------------------------------------------------------------ geometría

        private static GrupoAcoso[] Grupos() => Object.FindObjectsByType<GrupoAcoso>(FindObjectsSortMode.None).OrderBy(g => g.name).ToArray();

        private static IEnumerable<PandilleroAPie> EnPie(GrupoAcoso g) => g.GetComponent<PandillaAPie>().Pandilleros.Where(p => p != null && p.gameObject.activeSelf && !p.EstaKO);

        private static bool TechoEn(Vector3 xz, float y, out float techoY)
        {
            techoY = 0f;
            if (!Physics.Raycast(new Vector3(xz.x, y + 3f, xz.z), Vector3.down, out RaycastHit h, 8f, ~0, QueryTriggerInteraction.Ignore) || h.normal.y < 0.9f)
            {
                return false;
            }
            techoY = h.point.y;
            return Mathf.Abs(h.point.y - y) <= TolTecho;
        }

        private sealed class Vecina
        {
            public Vector3 direccion;
            public float hueco;
            public float escalonMaximo;
            public Vector3 dentro;
        }

        // A espaldas del grupo (los pandilleros miran al civil, así que la espalda es el lado contrario a su frente) y en línea cardinal:
        // la azotea vecina a DistanciaVecina del centro. Mide el hueco más largo sin techo y el escalón más alto entre las dos azoteas.
        private static Vecina BuscarVecina(GrupoAcoso g)
        {
            Vector3 c = g.transform.position;
            Vector3 frente = Vector3.zero;
            foreach (PandilleroAPie p in EnPie(g))
            {
                frente += p.transform.forward;
            }
            frente.y = 0f;
            Vector3 espalda = -frente.normalized;
            Vector3 d = Mathf.Abs(espalda.x) > Mathf.Abs(espalda.z) ? new Vector3(Mathf.Sign(espalda.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(espalda.z));
            Vecina v = new Vecina { direccion = d };
            float inicioHueco = -1f;
            float escalon = 0f;
            for (float t = PasoSondeo; t <= DistanciaVecina; t += PasoSondeo)
            {
                Vector3 p = c + d * t;
                if (TechoEn(p, c.y, out float y))
                {
                    escalon = Mathf.Max(escalon, Mathf.Abs(y - c.y));
                    if (inicioHueco >= 0f)
                    {
                        v.hueco = Mathf.Max(v.hueco, t - inicioHueco);
                        inicioHueco = -1f;
                    }
                }
                else if (inicioHueco < 0f)
                {
                    inicioHueco = t - PasoSondeo;
                }
            }
            v.escalonMaximo = escalon;
            if (inicioHueco >= 0f)
            {
                v.hueco = float.MaxValue;
            }
            v.dentro = c + d * DistanciaVecina;
            return v;
        }

        // Línea de vista pecho a pecho sin triggers: solo tapa lo que no es el grupo ni el jugador.
        private bool LineaLibre(Vector3 desde, Vector3 hasta, GrupoAcoso g)
        {
            RaycastHit[] golpes = Physics.RaycastAll(desde, hasta - desde, (hasta - desde).magnitude, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in golpes)
            {
                if (h.transform.IsChildOf(g.transform) || h.transform.IsChildOf(jugador.transform))
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private sealed class TechoElegido
        {
            public GrupoAcoso grupo;
            public PuntoTrepar acceso;
            public Vector3 posicion;
            public PandilleroAPie objetivo;
            public float distancia;
        }

        // Con la regla real del acoso: alguien del grupo (mirando al civil) vería u oiría al jugador parado ahí.
        private bool LoVeAlguien(GrupoAcoso g, Vector3 posicion)
        {
            Vector3 objetivo = posicion + Vector3.up * acoso.AlturaPechoJugador;
            foreach (PandilleroAPie p in EnPie(g))
            {
                Vector3 hacia = g.Civil.transform.position - p.transform.position;
                hacia.y = 0f;
                Vector3 ojos = p.transform.position + Vector3.up * acoso.AlturaOjos;
                if (ReglaAcoso.Evaluar(ojos, hacia.normalized, objetivo, acoso.DistanciaVista, acoso.AnguloVista, acoso.InclinacionMaximaVista, acoso.DistanciaOido) != ReglaAcoso.Vista.No)
                {
                    return true;
                }
            }
            return false;
        }

        // El techo accesible (S2_Techos) más cercano a un grupo de suelo, con línea de vista a un pandillero y fuera del alcance del oído.
        private TechoElegido BuscarTechoJuntoAGrupoDeSuelo()
        {
            Transform raiz = GameObject.Find("S2_Techos")?.transform;
            Assert.IsNotNull(raiz, "Falta S2_Techos");
            TechoElegido mejor = null;
            foreach (GrupoAcoso g in Grupos().Where(x => !x.name.Contains("Azotea")))
            {
                foreach (PuntoTrepar punto in raiz.GetComponentsInChildren<PuntoTrepar>(true))
                {
                    Vector3 pos = punto.Salida + punto.Frente * 0.5f;
                    if (!TechoEn(pos, punto.Salida.y, out _))
                    {
                        pos = punto.Salida;
                    }
                    Vector3 ojos = pos + Vector3.up * AlturaPecho;
                    foreach (PandilleroAPie p in EnPie(g))
                    {
                        Vector3 pecho = p.transform.position + Vector3.up * AlturaPecho;
                        float d = Vector3.Distance(ojos, pecho);
                        if (d < DistanciaMinimaOido || d > DistanciaMaximaTiro || punto.Salida.y - p.transform.position.y < 2f)
                        {
                            continue;
                        }
                        if (!LineaLibre(ojos, pecho, g) || (mejor != null && d >= mejor.distancia) || LoVeAlguien(g, pos))
                        {
                            continue;
                        }
                        mejor = new TechoElegido { grupo = g, acceso = punto, posicion = pos, objetivo = p, distancia = d };
                    }
                }
            }
            return mejor;
        }

        // ------------------------------------------------------------------ disparo

        private IEnumerator EquiparPistola()
        {
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            // El jugador del prólogo empieza con los puños: se le da la pistola como la daría el mercado.
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal != null && !arsenal.Tiene(ArmaAPie.Pistola))
            {
                arsenal.DarMejora(ArmaAPie.Pistola);
            }
            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            float limite = Time.time + 3f;
            while (armas.ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            yield return Esperar(0.3f);
        }

        // Gira la cámara con Empujar hasta que mira al punto (el signo de la inclinación se descubre en el primer intento).
        private IEnumerator ApuntarA(Vector3 punto)
        {
            float signo = 1f;
            float errorAntes = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                Transform cam = controlador.Camara;
                Vector3 d = (punto - cam.position).normalized;
                Vector3 f = cam.forward;
                float error = Vector3.Angle(f, d);
                if (error < 0.7f)
                {
                    yield break;
                }
                if (i > 0 && error > errorAntes)
                {
                    signo = -signo;
                }
                errorAntes = error;
                float giro = Vector3.SignedAngle(Vector3.ProjectOnPlane(f, Vector3.up), Vector3.ProjectOnPlane(d, Vector3.up), Vector3.up);
                float elevF = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
                float elevD = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
                controlador.Empujar(giro, signo * (elevF - elevD));
                yield return null;
                yield return null;
            }
        }

        private sealed class Resultado
        {
            public bool disparoReal;
            public bool danio;
            public float tiempoDanio;
            public float tiempoSorpresa = -1f;
            public float tiempoAlerta = -1f;
            public ReglaAcoso.Fase faseAlDanio;
        }

        // Daña al pandillero (tiro real si se puede, si no el daño directo con origen el jugador) y mide cuándo entra en sorpresa y en alerta.
        private IEnumerator DaniarYMedir(GrupoAcoso g, PandilleroAPie objetivo, Resultado r, bool intentarTiro)
        {
            r.danio = false;
            // Cualquiera del grupo que reciba el daño cuenta (la bala puede pegarle a otro pandillero que estorba).
            foreach (PandilleroAPie otro in g.GetComponent<PandillaAPie>().Pandilleros)
            {
                otro.DanioRecibido += (p, info) =>
                {
                    if (!r.danio)
                    {
                        r.danio = true;
                        r.tiempoDanio = Time.time;
                        r.faseAlDanio = g.Fase;
                    }
                };
            }
            if (intentarTiro)
            {
                yield return EquiparPistola();
                if (jugador.GetComponent<ArmasAPie>().ArmaActiva == ArmaAPie.Pistola)
                {
                    Press(raton.rightButton);
                    yield return Esperar(0.9f);
                    yield return ApuntarA(objetivo.transform.position + Vector3.up * acoso.AlturaOjos);
                    Press(raton.leftButton);
                    yield return null;
                    Release(raton.leftButton);
                    float limite = Time.time + 1f;
                    while (!r.danio && Time.time < limite)
                    {
                        yield return null;
                    }
                    r.disparoReal = r.danio;
                    Release(raton.rightButton);
                }
            }
            if (!r.danio)
            {
                objetivo.RecibirDanio(new InfoDanio(5f, TipoDanio.Disparo, objetivo.transform.position + Vector3.up * AlturaPecho, Vector3.forward, null));
            }
            float fin = Time.time + MaxSegundosFase;
            while (Time.time < fin && r.tiempoAlerta < 0f)
            {
                if (r.tiempoSorpresa < 0f && g.Fase == ReglaAcoso.Fase.Sorpresa)
                {
                    r.tiempoSorpresa = Time.time;
                }
                if (g.Fase == ReglaAcoso.Fase.Alerta)
                {
                    r.tiempoAlerta = Time.time;
                }
                yield return null;
            }
        }

        private IEnumerator EmboscadaDesde(Vector3 posicion, GrupoAcoso g, PandilleroAPie objetivo, string etiqueta)
        {
            Poner(posicion + Vector3.up * 0.05f, objetivo.transform.position - posicion);
            yield return Esperar(EsperaSinVer);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, etiqueta + ": el grupo no debe haberlo visto en " + EsperaSinVer + " s");
            Assert.IsFalse(EnPie(g).Any(p => p.Alerta), etiqueta + ": nadie en alerta antes del daño");
            Resultado r = new Resultado();
            float desnivel = jugador.transform.position.y - objetivo.transform.position.y;
            float esperado = desnivel >= acoso.AlturaDesdeArriba ? acoso.RetrasoEmboscadaDesdeArriba : acoso.RetrasoEmboscada;
            yield return DaniarYMedir(g, objetivo, r, true);
            Debug.Log("[T999] " + etiqueta + ": tiro real=" + r.disparoReal + ", fase al daño=" + r.faseAlDanio + ", desnivel=" + desnivel.ToString("F2")
                + " m, retraso esperado=" + esperado + " s, sorpresa a +" + (r.tiempoSorpresa - r.tiempoDanio).ToString("F2")
                + " s, alerta a +" + (r.tiempoAlerta - r.tiempoDanio).ToString("F2") + " s");
            Assert.IsTrue(r.danio, etiqueta + ": el pandillero debe recibir el daño");
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, r.faseAlDanio, etiqueta + ": hasta el daño seguían acosando");
            Assert.GreaterOrEqual(r.tiempoAlerta, 0f, etiqueta + ": después del retraso el grupo debe entrar en alerta (fase " + g.Fase + ")");
            float espera = r.tiempoAlerta - r.tiempoDanio;
            Assert.GreaterOrEqual(espera, esperado - ToleranciaRetraso, etiqueta + ": no entra en alerta antes del retraso de sorpresa (" + espera.ToString("F2") + " s)");
            Assert.LessOrEqual(espera, esperado + 1.5f, etiqueta + ": y la alerta llega justo después del retraso");
            Assert.GreaterOrEqual(r.tiempoSorpresa, 0f, etiqueta + ": pasa por sorpresa");
        }

        // ------------------------------------------------------------------ casos

        [UnityTest]
        public IEnumerator Azotea_DesdeLaVecina_ElGrupoNoLoVeYDespuesDelDanioHayRetrasoYAlerta()
        {
            yield return Cargar();
            GrupoAcoso g = Grupos().First(x => x.name.Contains("Azotea"));
            Vecina v = BuscarVecina(g);
            Assert.IsNotNull(v, "El grupo de la azotea debe tener una azotea vecina");
            Vector3 posicion = v.dentro;
            Assert.IsTrue(TechoEn(posicion, g.transform.position.y, out float y), "Hay techo donde se coloca al jugador en la vecina");
            posicion.y = y;
            PandilleroAPie objetivo = EnPie(g).OrderBy(p => Vector3.Distance(p.transform.position, posicion)).First(p => LineaLibre(posicion + Vector3.up * AlturaOjosJugador, p.transform.position + Vector3.up * acoso.AlturaOjos, g));
            yield return EmboscadaDesde(posicion, g, objetivo, "Azotea vecina");
        }

        [UnityTest]
        public IEnumerator TechoJuntoAGrupoDeSuelo_ElGrupoNoLoVeYDespuesDelDanioHayRetrasoLargoYAlerta()
        {
            yield return Cargar();
            TechoElegido t = BuscarTechoJuntoAGrupoDeSuelo();
            Assert.IsNotNull(t, "Algún techo accesible debe tener línea de vista a un grupo de suelo");
            Debug.Log("[T999] Techo elegido: " + t.acceso.name + " junto a " + t.grupo.name + ", distancia " + t.distancia.ToString("F1") + " m, techo y=" + t.posicion.y.ToString("F2"));
            yield return EmboscadaDesde(t.posicion, t.grupo, t.objetivo, "Techo junto a " + t.grupo.name);
        }

        [UnityTest]
        public IEnumerator ASuelo_DeFrenteA10Metros_LaAlertaLlegaEnMenosDeMedioSegundo()
        {
            yield return Cargar();
            Vector3 posicion = Vector3.zero;
            GrupoAcoso elegido = null;
            foreach (GrupoAcoso g in Grupos())
            {
                foreach (PandilleroAPie p in EnPie(g))
                {
                    // De frente al pandillero: en la dirección en que mira, a 10 m, sobre suelo plano y sin nada de por medio.
                    Vector3 dir = p.transform.forward;
                    dir.y = 0f;
                    dir.Normalize();
                    Vector3 xz = p.transform.position + dir * DistanciaDeFrente;
                    if (!Physics.Raycast(xz + Vector3.up * 2f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore) || h.normal.y < 0.9f
                        || Mathf.Abs(h.point.y - p.transform.position.y) > 0.3f)
                    {
                        continue;
                    }
                    Vector3 pos = h.point;
                    if (Physics.CheckCapsule(pos + Vector3.up * 0.4f, pos + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    // Todo el camino pecho a pecho debe estar libre (a todos los pandilleros que lo vean).
                    if (!LineaLibre(p.transform.position + Vector3.up * acoso.AlturaOjos, pos + Vector3.up * acoso.AlturaPechoJugador, g))
                    {
                        continue;
                    }
                    elegido = g;
                    posicion = pos;
                    break;
                }
                if (elegido != null)
                {
                    break;
                }
            }
            Assert.IsNotNull(elegido, "Algún grupo debe tener 10 m despejados al frente de un pandillero");
            // Lejos primero (no lo ven) y luego de golpe a 10 m de frente.
            Poner(posicion - (posicion - elegido.transform.position).normalized * 60f + Vector3.up * 60f, Vector3.forward);
            yield return Esperar(0.5f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, elegido.Fase, "Antes de aparecer acosan");
            Poner(posicion + Vector3.up * 0.05f, elegido.transform.position - posicion);
            float inicio = Time.time;
            float alerta = -1f;
            while (Time.time - inicio < 2f && alerta < 0f)
            {
                if (elegido.Fase == ReglaAcoso.Fase.Alerta)
                {
                    alerta = Time.time - inicio;
                }
                yield return null;
            }
            Debug.Log("[T999] Control a ras de suelo en " + elegido.name + ": alerta a " + alerta.ToString("F2") + " s");
            Assert.GreaterOrEqual(alerta, 0f, "Debe entrar en alerta");
            Assert.LessOrEqual(alerta, SegundosAlertaDeFrente + 0.05f, "Alerta de frente a 10 m en <= 0,5 s");
        }

        [UnityTest]
        public IEnumerator Azotea_LaVecinaSeCruzaAlTechoDelGrupo_ElHuecoNoPasaDeLoQueSaltaElJugador()
        {
            yield return Cargar();
            GrupoAcoso g = Grupos().First(x => x.name.Contains("Azotea"));
            Vecina v = BuscarVecina(g);
            Assert.IsNotNull(v, "El grupo de la azotea debe tener una azotea vecina");
            Debug.Log("[T999] Azotea vecina hacia " + v.direccion + ": hueco " + v.hueco.ToString("F2") + " m, escalón máximo " + v.escalonMaximo.ToString("F2") + " m");
            Assert.LessOrEqual(v.hueco, SaltoMaximo, "El hueco entre azoteas supera el salto de " + SaltoMaximo + " m");
            // Se cruza de verdad: del techo vecino al del grupo caminando y saltando con la entrada simulada.
            Vector3 salida = g.transform.position + v.direccion * DistanciaCarrera;
            Assert.IsTrue(TechoEn(salida, g.transform.position.y, out float y0), "Hay techo en la vecina");
            salida.y = y0;
            Poner(salida + Vector3.up * 0.05f, -v.direccion);
            yield return Esperar(0.5f);
            Vector3 meta = g.transform.position - v.direccion * 4f;
            yield return ApuntarA(meta + Vector3.up * AlturaOjosJugador);
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            float fin = Time.time + 10f;
            float proximoSalto = Time.time;
            float llego = -1f;
            while (Time.time < fin)
            {
                Vector3 resto = meta - jugador.transform.position;
                resto.y = 0f;
                if (resto.magnitude < 1f && Mathf.Abs(jugador.transform.position.y - g.transform.position.y) < 1f)
                {
                    llego = 10f - (fin - Time.time);
                    break;
                }
                // Salta cada tanto (el escalón entre azoteas y el borde del techo).
                if (Time.time >= proximoSalto)
                {
                    proximoSalto = Time.time + IntervaloSalto;
                    Press(teclado.spaceKey);
                    yield return null;
                    Release(teclado.spaceKey);
                }
                yield return null;
            }
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            Debug.Log("[T999] Cruce a pie de la vecina al grupo: " + (llego >= 0f ? "llegó en " + llego.ToString("F1") + " s" : "no llegó (pos " + jugador.transform.position + ")"));
            Assert.GreaterOrEqual(llego, 0f, "El jugador debe llegar del techo vecino al techo del grupo");
        }

        [UnityTest]
        public IEnumerator Captura_EmboscadaDesdeLaAzoteaVecina()
        {
            if (System.Environment.GetEnvironmentVariable(VariableCapturas) != "1")
            {
                Assert.Ignore("Captura: se toma con " + VariableCapturas + "=1");
            }
            yield return Cargar();
            GrupoAcoso g = Grupos().First(x => x.name.Contains("Azotea"));
            Vecina v = BuscarVecina(g);
            Assert.IsNotNull(v);
            Vector3 posicion = v.dentro;
            TechoEn(posicion, g.transform.position.y, out float y);
            posicion.y = y;
            Poner(posicion + Vector3.up * 0.05f, g.transform.position - posicion);
            yield return Esperar(0.5f);
            Directory.CreateDirectory(CarpetaFotos);
            GameObject go = new GameObject("CamT999");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.depth = 100f;
            go.transform.position = posicion + Vector3.up * 1.6f;
            go.transform.LookAt(g.transform.position + Vector3.up * AlturaPecho);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            cam.enabled = false;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(CarpetaFotos + "/emboscada.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
