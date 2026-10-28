using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-197: el recorrido de un día y una noche con cromo en Pruebas_Implantes, con entrada simulada (WASD, Shift, Espacio, E, flechas,
    /// Enter, Esc, clics, ratón, F5 y F9). Atajos permitidos y anotados: <c>Colocar</c> cerca de cada sitio, dinero fijado por código
    /// (inicio, antes del paso g y antes del paso h), <c>AvanzarHasta</c> para las franjas del día, <c>ServicioLigas.Poner</c> para la
    /// liga 3, el horario de la clínica aplicado al lugar de prueba (el doble no sigue el reloj solo), <c>VidaAPie.Restaurar</c> y
    /// <c>VidaMecha.FijarVida</c> para que nadie caiga a media medición (menos en el paso h, donde cae de verdad) y <c>RecibirDanio</c> al
    /// rival mecha solo después de daño real del jugador. Los números salen de los assets.
    /// </summary>
    public class RecorridoImplantesPruebas : BaseImplantesPruebas
    {
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Base.asset";
        private const string RutaControles = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaDatosDoctor = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Base.asset";
        private const string RutaConfigOjos = "Assets/_Proyecto/UI/Datos/ConfigPantallaOjos_Base.asset";
        private const string VariableCaptura197 = "CAPTURAR_T197";
        private const string RutaCaptura1 = "docs/capturas/T-197-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-197-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-197-3.png";
        private const string TextoEspejo = "E · Mirarte al espejo";
        private const string AliasPrimero = "TORNILLO";
        private const float HoraDia = 10f;
        private const float HoraCaida = 23f;
        private const float HorasPorSegundoDePrueba = 0.02f;
        private const float ToleranciaHoraClinica = 0.25f;
        private const float ToleranciaHora = 0.05f;
        private const float ToleranciaSalto = 0.08f;
        private const float ToleranciaCarrera = 0.05f;
        private const float MetrosPista = 30f;
        private const float MetrosArranque = 10f;
        private const float DistanciaEspejo = 1.5f;
        private const float AlturaOjosJugador = 0.05f;
        private const float DistanciaSaltoCaja = 1.2f;
        private const float DistanciaAntesDeLaCaja = 3f;
        private const float MaxSegundosCaja = 4f;
        private const float MargenCima = 0.1f;
        private const float MaxSegundosCarrera = 20f;
        private const float AlturaCentro = 0.9f;
        private const float GananciaMirada = 0.7f;
        private const float MaxDeltaRaton = 600f;
        private const float ToleranciaMiraGrados = 2.5f;
        private const float DistanciaGolpeRealizable = 1.5f;
        private const float DistanciaAcercarse = 1.4f;
        private const float MaxSegundosVencer = 30f;
        private const float VidaMinimaSegura = 40f;
        private const float MaxSegundosEstado = 90f;
        private const float MaxSegundosGolpesReales = 20f;
        private const float SegundosEntreClics = 0.3f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaSegura = 0.6f;
        private const float ToleranciaBarra = 0.01f;
        private const float ToleranciaTiempo = 0.002f;
        private const float SegundosDeMedicion = 30f;
        private const float FraccionMinimaAvisada = 0.8f;
        private const float MaxSegundosKO = 120f;
        private const float MaxSegundosDoctor = 40f;
        private const float MaxSegundosCasa = 8f;
        private const float DanioMortal = 100000f;
        private const float CalorAntesDeCaer = 40f;
        private const float DistanciaCamillaMax = 1.5f;
        private const int MinimoGolpesMedidos = 3;
        private const int CuadrosSobreLaCaja = 4;
        private const int DineroDeLaCaida = 300;
        private const int DeudaEsperada = -200;
        private const int DineroParaMejorar = 20000;
        private const int DineroDePrueba = 777;
        private const int PrecioPiernasMasBrazos = 4000;
        private const int FilaOjos = 0;
        private const int FilaBrazos = 1;
        private const int FilaPiernas = 2;
        private const int FilaSistema = 3;

        private static readonly Vector3 InicioPista = new Vector3(-35f, AlturaOjosJugador, -12f);
        private static readonly Vector3 InicioCerca = new Vector3(20f, AlturaOjosJugador, -6f);
        private static readonly Vector3 AntesDeCasa = new Vector3(5f, AlturaOjosJugador, 12f);

        private readonly List<string> bitacora = new List<string>();
        private readonly List<string> medidas = new List<string>();
        private float marcaPaso;
        private bool clicPulsado;
        private bool protegerVida;
        private int golpesDados;
        private int maxAtacando;

        private DatosPandilleroAPie datosPandillero;
        private ConfigControlesAPie controles;
        private PandillaAPie pandilla;
        private VidaAPie vidaJugador;
        private GolpeAPie golpeAPie;
        private DatosPersonaje datosPersonaje;
        private LugarDePrueba lugarClinica;

        private MostradorPeleas mostradorPeleas;
        private DirectorPelea director;
        private MontajeMecha montaje;
        private VidaMecha vidaMecha;
        private PantallaOjosCabina ojos;
        private LedSinapsis led;
        private GolpeMecha golpe;
        private EncendidoMecha encendido;
        private RivalBasico rival;
        private ConfigPantallaOjos configOjos;
        private ValoresMecha basales;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            bitacora.Clear();
            medidas.Clear();
            clicPulsado = false;
            protegerVida = true;
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // ---- Utilidades ----

        private static string F(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

        private void Paso(string nombre, string detalle)
        {
            float segundos = Time.time - marcaPaso;
            marcaPaso = Time.time;
            string linea = "paso " + nombre + ": " + F(segundos) + " s de juego; " + detalle;
            bitacora.Add(linea);
            Debug.Log("[T-197] " + linea);
        }

        private IEnumerator Cuadro()
        {
            yield return null;
            Vigilar();
        }

        private void MantenerVivo()
        {
            if (protegerVida && vidaJugador.Actual < VidaMinimaSegura) vidaJugador.Restaurar();
        }

        private void MantenerVivoMecha()
        {
            if (vidaMecha.Actual < vidaMecha.Maxima * FraccionVidaSegura) vidaMecha.FijarVida(vidaMecha.Maxima);
        }

        private void Colocar(Vector3 posicion, float giro)
        {
            jugador.Colocar(new Pose(posicion, Quaternion.Euler(0f, giro, 0f)));
            atajos.Add("Colocar: " + posicion);
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static int Necesarios(float vida, float danio) => Mathf.CeilToInt(vida / danio - 0.0001f);

        private IEnumerator Bajar(int veces)
        {
            for (int i = 0; i < veces; i++) yield return Tecla(teclado.downArrowKey);
        }

        private static bool CapturaPedida => Environment.GetEnvironmentVariable(VariableCaptura197) == "1";

        private void CapturarHudEn(string rutaRelativa)
        {
            if (!CapturaPedida) return;
            Environment.SetEnvironmentVariable(VariableCaptura, "1");
            CapturarConHud(Path.GetFullPath(Path.Combine(Application.dataPath, "..", rutaRelativa)));
            Environment.SetEnvironmentVariable(VariableCaptura, null);
        }

        private void CapturarCabina(string rutaRelativa)
        {
            if (!CapturaPedida) return;
            Camera camara = montaje.GetComponentInChildren<Camera>(true);
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rutaRelativa));
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            rt.Release();
            Object.Destroy(rt);
            Debug.Log("[T-197] captura en " + ruta);
        }

        // ---- Salto y carrera ----

        private IEnumerator EsperarEnPiso()
        {
            for (int i = 0; i < 120 && !controlador.EnPiso; i++) yield return Cuadro();
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(controlador.EnPiso, "el jugador está en el piso");
        }

        // Un salto en terreno libre con Espacio: devuelve cuánto subió el cuerpo (m).
        private IEnumerator MedirSalto(Action<float> resultado)
        {
            Colocar(InicioPista, 90f);
            yield return EsperarEnPiso();
            float y0 = jugador.Raiz.position.y;
            float maximo = y0;
            Press(teclado.spaceKey);
            yield return Cuadro();
            Release(teclado.spaceKey);
            float inicio = Time.time;
            float fin = inicio + 3f;
            while (Time.time < fin)
            {
                yield return Cuadro();
                maximo = Mathf.Max(maximo, jugador.Raiz.position.y);
                if (controlador.EnPiso && Time.time > inicio + 0.2f) break;
            }
            resultado(maximo - y0);
        }

        private static Collider Caja(string nombre)
        {
            GameObject objeto = GameObject.Find("Caja salto " + nombre);
            Assert.IsNotNull(objeto, "la escena trae la caja de salto " + nombre);
            return objeto.GetComponent<Collider>();
        }

        // Camina hacia la caja saltando junto a su cara; dice si terminó de pie encima de ella.
        private IEnumerator IntentarSubir(string nombre, Action<bool> resultado)
        {
            Bounds b = Caja(nombre).bounds;
            CharacterController cc = jugador.GetComponent<CharacterController>();
            Colocar(new Vector3(b.center.x, AlturaOjosJugador, b.min.z - DistanciaAntesDeLaCaja), 0f);
            yield return EsperarEnPiso();
            bool encima = false;
            bool espacio = false;
            int cuadrosArriba = 0;
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaja;
            while (Time.time < fin && !encima)
            {
                if (espacio)
                {
                    Release(teclado.spaceKey);
                    espacio = false;
                }
                else if (controlador.EnPiso && jugador.Raiz.position.z < b.min.z && b.min.z - jugador.Raiz.position.z < DistanciaSaltoCaja)
                {
                    Press(teclado.spaceKey);
                    espacio = true;
                }
                yield return Cuadro();
                Vector3 p = jugador.Raiz.position;
                bool dentro = p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z;
                float pies = cc.bounds.min.y;
                bool reposa = dentro && controlador.EnPiso && Mathf.Abs(pies - b.max.y) <= MargenCima;
                cuadrosArriba = reposa ? cuadrosArriba + 1 : 0;
                if (cuadrosArriba >= CuadrosSobreLaCaja)
                {
                    encima = true;
                    Debug.Log("[T-197] encima de " + nombre + ": pies " + F(pies) + " m, tope " + F(b.max.y) + " m");
                }
            }
            Release(teclado.wKey);
            Release(teclado.spaceKey);
            yield return Cuadro();
            resultado(encima);
        }

        // Corre la recta de 30 m con Shift + W. Da los segundos totales y los del tramo entre el metro 10 y el 30.
        private IEnumerator CorrerRecta(Action<float, float> resultado)
        {
            Colocar(InicioPista, 90f);
            yield return EsperarEnPiso();
            float x0 = jugador.Raiz.position.x;
            float t0 = Time.time;
            float t10 = -1f;
            float t30 = -1f;
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            float fin = t0 + MaxSegundosCarrera;
            while (t30 < 0f && Time.time < fin)
            {
                yield return Cuadro();
                float recorrido = jugador.Raiz.position.x - x0;
                if (t10 < 0f && recorrido >= MetrosArranque) t10 = Time.time;
                if (recorrido >= MetrosPista) t30 = Time.time;
            }
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            yield return Cuadro();
            Assert.Greater(t30, 0f, "llegó al final de la recta de 30 m");
            resultado(t30 - t0, t30 - t10);
        }

        private void VerificarCarrera(string que, float total, float tramo, float velocidadEsperada)
        {
            float esperado = (MetrosPista - MetrosArranque) / velocidadEsperada;
            medidas.Add(que + ": recta 30 m " + F(total) + " s; tramo 10-30 m " + F(tramo) + " s (esperado " + F(esperado) + " s a " + F(velocidadEsperada) + " m/s)");
            Assert.AreEqual(esperado, tramo, esperado * ToleranciaCarrera, que + ": el tramo 10-30 m tarda lo calculado desde los assets (±5 %)");
        }

        // ---- Clínica y espejo ----

        private IEnumerator Instalar(int fila, DatosImplante esperado)
        {
            yield return AbrirClinica();
            yield return Bajar(fila);
            Assert.AreEqual(fila, pantalla.Indice, "la fila elegida");
            Assert.AreEqual(esperado.Descripcion, pantalla.Descripcion, "la descripción de lo que se instala");
            yield return Tecla(teclado.enterKey);
            Assert.IsTrue(pantalla.Confirmando, "el primer Enter pide confirmar");
            yield return Tecla(teclado.enterKey);
            yield return EsperarOperacion();
            Assert.AreEqual(esperado.Nivel, implantes.Nivel(esperado.Espacio), "quedó instalado " + esperado.Nombre);
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator InstalarVarios(string nombre, params (int fila, DatosImplante datos)[] lista)
        {
            int dinero0 = cartera.Dinero;
            float hora0 = reloj.Hora;
            float t0 = Time.time;
            int precio = 0;
            foreach (var par in lista)
            {
                int nivelActual = implantes.Nivel(par.datos.Espacio);
                int antes = nivelActual > 0 ? Datos(par.datos.Espacio, nivelActual).Precio : 0;
                precio += par.datos.Precio - antes;
                yield return Instalar(par.fila, par.datos);
            }
            float horasEsperadas = lista.Length * config.HorasOperacion + HorasPorSegundoDePrueba * (Time.time - t0);
            float delta = Normalizar(reloj.Hora - hora0);
            Assert.AreEqual(dinero0 - precio, cartera.Dinero, nombre + ": se cobró lo calculado");
            Assert.AreEqual(horasEsperadas, delta, ToleranciaHoraClinica * lista.Length, nombre + ": cada operación avanza " + F(config.HorasOperacion) + " h");
            medidas.Add(nombre + ": cobrado $" + precio + ", horas " + F(delta) + " (esperadas " + F(horasEsperadas) + ")");
        }

        private IEnumerator MirarseAlEspejo(bool conFranja, string capturaRelativa)
        {
            EspejoJugador espejo = Object.FindFirstObjectByType<EspejoJugador>();
            Assert.IsNotNull(espejo, "la escena trae el espejo");
            jugador.Colocar(new Pose(espejo.Posicion + new Vector3(0f, AlturaOjosJugador, -DistanciaEspejo), Quaternion.identity));
            atajos.Add("Colocar: frente al espejo");
            yield return Cuadros(FramesParaElHud);
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != TextoEspejo && Time.time < fin) yield return Cuadro();
            Assert.AreEqual(TextoEspejo, hud.TextoAvisoInteraccion.text, "el HUD ofrece mirarte al espejo");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud * 4);
            Assert.IsTrue(espejo.Abierto, "E abre el espejo");
            CuerpoJugador cuerpo = null;
            foreach (VistaPreviaApariencia v in Object.FindObjectsByType<VistaPreviaApariencia>(FindObjectsSortMode.None))
            {
                if (v.EstaEncendida) cuerpo = v.Cuerpo;
            }
            Assert.IsNotNull(cuerpo, "la vista del espejo está encendida");
            var texto = new List<string>();
            foreach (EspacioImplante espacio in Enum.GetValues(typeof(EspacioImplante)))
            {
                int nivel = cuerpo.NivelMostrado(espacio);
                texto.Add(espacio + "=" + nivel);
                Assert.AreEqual(implantes.Nivel(espacio), nivel, "el espejo muestra " + espacio + " como el servicio");
                if (nivel > 0) Assert.IsTrue(cuerpo.PiezasInstaladas[(int)espacio].activeSelf, "la pieza de " + espacio + " se ve en el cuerpo");
                if (conFranja)
                {
                    Assert.IsTrue(cuerpo.FranjasInstaladas[(int)espacio].activeSelf, "la franja de " + espacio + " se ve");
                }
            }
            medidas.Add("espejo " + string.Join(",", texto));
            if (capturaRelativa != null) CapturarHudEn(capturaRelativa);
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(espejo.Abierto, "Esc cierra el espejo");
            Assert.IsTrue(controlador.ControlActivo, "el control a pie vuelve");
        }

        private void AplicarHorarioDeLaClinica()
        {
            lugarClinica.PonerAbiertoPorHorario(lugarClinica.Horario.EstaAbierto(reloj.Hora));
            atajos.Add("Horario aplicado al lugar de prueba (el doble no sigue el reloj solo)");
        }

        // ---- Pandilla ----

        private float PasoDeMira(Vector3 punto)
        {
            Transform camara = controlador.Camara;
            Vector3 v = punto - camara.position;
            float yawObjetivo = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            float errorYaw = Mathf.DeltaAngle(jugador.transform.eulerAngles.y, yawObjetivo);
            float plano = new Vector2(v.x, v.z).magnitude;
            float cabeceoObjetivo = Mathf.Atan2(-v.y, plano) * Mathf.Rad2Deg;
            float errorCabeceo = cabeceoObjetivo - controlador.Inclinacion;
            float sensibilidad = controles.SensibilidadRaton;
            float dx = Mathf.Clamp(errorYaw / sensibilidad * GananciaMirada, -MaxDeltaRaton, MaxDeltaRaton);
            float dy = Mathf.Clamp(-errorCabeceo / sensibilidad * GananciaMirada, -MaxDeltaRaton, MaxDeltaRaton);
            Set(raton.delta, new Vector2(dx, dy));
            return Mathf.Max(Mathf.Abs(errorYaw), Mathf.Abs(errorCabeceo));
        }

        private bool Mirando(Vector3 punto)
        {
            Transform camara = controlador.Camara;
            return Vector3.Angle(camara.forward, punto - camara.position) < ToleranciaMiraGrados * 2f;
        }

        private void ContarAtacando()
        {
            int atacando = 0;
            foreach (PandilleroAPie q in pandilla.Pandilleros)
            {
                if (q.gameObject.activeSelf && !q.EstaKO && (q.Estado == EstadoPandillero.Preparando || q.Estado == EstadoPandillero.Recargando)) atacando++;
            }
            maxAtacando = Mathf.Max(maxAtacando, atacando);
        }

        private IEnumerator Vencer(PandilleroAPie p)
        {
            golpesDados = 0;
            float ultimaVida = p.Actual;
            float fin = Time.time + MaxSegundosVencer;
            while (!p.EstaKO && p.gameObject.activeSelf && Time.time < fin)
            {
                MantenerVivo();
                ContarAtacando();
                Vector3 centro = p.transform.position + Vector3.up * AlturaCentro;
                PasoDeMira(centro);
                float distancia = Distancia(p.transform.position, jugador.transform.position);
                if (distancia > DistanciaAcercarse) Press(teclado.wKey); else Release(teclado.wKey);
                if (clicPulsado)
                {
                    Release(raton.leftButton);
                    clicPulsado = false;
                }
                else if (Mirando(centro) && distancia <= DistanciaGolpeRealizable && golpeAPie.Fase == FaseGolpe.Listo)
                {
                    Press(raton.leftButton);
                    clicPulsado = true;
                }
                yield return Cuadro();
                if (p.Actual < ultimaVida - 0.0001f)
                {
                    golpesDados++;
                    ultimaVida = p.Actual;
                }
            }
            Release(teclado.wKey);
            if (clicPulsado)
            {
                Release(raton.leftButton);
                clicPulsado = false;
            }
            Set(raton.delta, Vector2.zero);
            yield return Cuadro();
            Assert.IsTrue(p.EstaKO, "se venció al pandillero");
        }

        // ---- Pelea en la jaula ----

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin) yield return null;
            Assert.IsTrue(condicion(), "se esperaba: " + que);
        }

        private IEnumerator GolpesReales()
        {
            IVida vidaRival = director.Rival.Vida;
            float limite = vidaRival.Maxima * FraccionDanioBuscada;
            float fin = Time.time + MaxSegundosGolpesReales;
            float proximo = 0f;
            bool clic = false;
            while (vidaRival.Actual > limite && Time.time < fin)
            {
                if (Time.time >= proximo)
                {
                    clic = !clic;
                    proximo = Time.time + SegundosEntreClics / 2f;
                    if (clic) Press(raton.leftButton); else Release(raton.leftButton);
                }
                yield return Cuadro();
            }
            Release(raton.leftButton);
            yield return Cuadro();
            Assert.Less(vidaRival.Actual, vidaRival.Maxima, "el jugador le hizo daño real al rival");
            Assert.IsFalse(vidaRival.EstaKO, "el rival sigue en pie");
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator MedirAvisoDeGolpes(float anticipacion, List<float> adelantos, List<string> aparte, List<string> fallos)
        {
            bool avisoAntes = false;
            float litDesde = -1f;
            float tiempoAnterior = Time.time;
            float ultimoDesconocido = Time.time;
            float fin = Time.time + SegundosDeMedicion;
            while (Time.time < fin && adelantos.Count + aparte.Count + fallos.Count < MinimoGolpesMedidos + 1)
            {
                yield return Cuadro();
                MantenerVivoMecha();
                float segundos = ((ILecturaRival)rival).SegundosParaAtacar;
                if (!rival.EnAviso && segundos < 0f) ultimoDesconocido = Time.time;
                if (ojos.AvisoPrendido)
                {
                    if (litDesde < 0f) litDesde = tiempoAnterior;
                }
                else
                {
                    litDesde = -1f;
                }
                tiempoAnterior = Time.time;
                bool enAviso = rival.EnAviso;
                if (enAviso && !avisoAntes)
                {
                    float adelanto = litDesde >= 0f ? Time.time - litDesde : 0f;
                    float tarde = Time.time - ultimoDesconocido;
                    string descripcion = "adelanto " + F(adelanto) + " s (desconocido hace " + F(tarde) + " s)";
                    if (adelanto >= anticipacion - Time.deltaTime - ToleranciaTiempo) adelantos.Add(adelanto);
                    else if (tarde < anticipacion + Time.deltaTime) aparte.Add(descripcion);
                    else fallos.Add(descripcion);
                }
                avisoAntes = enAviso;
            }
        }

        private IEnumerator MedirGolpeDelJugador(float esperado, Action<float> resultado)
        {
            MantenerVivoMecha();
            float impacto = -1f;
            Action alImpactar = () => impacto = Time.time;
            golpe.Impacto += alImpactar;
            yield return Cuadro();
            float t0 = -1f;
            float frameMax = 0f;
            float fin = Time.time + MaxSegundosGolpesReales;
            bool clic = false;
            float proximo = 0f;
            while (impacto < 0f && Time.time < fin)
            {
                if (Time.time >= proximo)
                {
                    clic = !clic;
                    proximo = Time.time + SegundosEntreClics / 2f;
                    if (clic)
                    {
                        if (golpe.Fase == FaseGolpe.Listo) t0 = Time.time;
                        Press(raton.leftButton);
                    }
                    else
                    {
                        Release(raton.leftButton);
                    }
                }
                yield return Cuadro();
                frameMax = Mathf.Max(frameMax, Time.deltaTime);
            }
            Release(raton.leftButton);
            golpe.Impacto -= alImpactar;
            Assert.GreaterOrEqual(impacto, 0f, "el golpe del jugador llegó al impacto");
            float medido = impacto - t0;
            Assert.GreaterOrEqual(medido, esperado - ToleranciaTiempo, "el golpe no sale antes de lo esperado");
            Assert.LessOrEqual(medido, esperado + frameMax + ToleranciaTiempo, "el golpe sale dentro de un cuadro de lo esperado");
            resultado(medido);
            yield return EsperarHasta(() => golpe.Fase == FaseGolpe.Listo, MaxSegundosEstado, "el golpe vuelve a Listo");
        }

        // ---- El recorrido ----

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator Recorrido_UnDiaYUnaNocheConCromo_JugandoComoUnaPersona()
        {
            yield return CargarEscena();
            datosPandillero = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            controles = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControles);
            datosPersonaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatosPersonaje);
            CalendarioLigas calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            DatosDoctor datosDoctor = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatosDoctor);
            configOjos = AssetDatabase.LoadAssetAtPath<ConfigPantallaOjos>(RutaConfigOjos);
            Assert.IsNotNull(datosPandillero);
            Assert.IsNotNull(controles);
            Assert.IsNotNull(calendario);
            Assert.IsNotNull(datosDoctor);
            Assert.IsNotNull(configOjos);
            pandilla = Object.FindFirstObjectByType<PandillaAPie>();
            vidaJugador = jugador.GetComponent<VidaAPie>();
            golpeAPie = jugador.GetComponent<GolpeAPie>();
            DoctorClinica doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(pandilla);
            Assert.IsNotNull(doctor);
            foreach (ILugar l in Registro<ILugar>.Todos)
            {
                if (l.Id == IdLugar.Clinica) lugarClinica = (LugarDePrueba)l;
            }
            Assert.IsNotNull(lugarClinica, "hay lugar Clinica");
            mostradorPeleas = Object.FindFirstObjectByType<MostradorPeleas>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            director = mostradorPeleas.GetComponentInChildren<DirectorPelea>(true);
            vidaMecha = montaje.GetComponent<VidaMecha>();
            ojos = montaje.GetComponentInChildren<PantallaOjosCabina>(true);
            led = montaje.GetComponentInChildren<LedSinapsis>(true);
            golpe = montaje.GetComponent<GolpeMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            basales = vidaMecha.Datos.Valores;
            DatosImplante piernas1 = Datos(EspacioImplante.Piernas, 1);
            DatosImplante piernas2 = Datos(EspacioImplante.Piernas, 2);
            DatosImplante brazos1 = Datos(EspacioImplante.Brazos, 1);
            DatosImplante brazos2 = Datos(EspacioImplante.Brazos, 2);
            DatosImplante ojos1 = Datos(EspacioImplante.Ojos, 1);
            DatosImplante ojos2 = Datos(EspacioImplante.Ojos, 2);
            DatosImplante sinapsis1 = Datos(EspacioImplante.SistemaNervioso, 1);
            DatosImplante sinapsis2 = Datos(EspacioImplante.SistemaNervioso, 2);
            marcaPaso = Time.time;

            // a) 10:00, sin implantes, dinero fijado: la caja PIERNAS I no se sube y se mide la recta.
            Assert.AreEqual(HoraDia, reloj.Hora, 0.5f, "arranca a las 10:00");
            Assert.AreEqual(0, implantes.Cantidad, "sin implantes");
            PonerDinero(DineroAlto);
            float saltoBase = 0f;
            yield return MedirSalto(v => saltoBase = v);
            float alturaCajaI = Caja("PIERNAS I").bounds.size.y;
            float alturaCajaII = Caja("PIERNAS II").bounds.size.y;
            medidas.Add("salto base " + F(saltoBase) + " m (esperado " + F(datosPersonaje.AlturaSalto) + "), caja PIERNAS I " + F(alturaCajaI) + " m");
            Assert.AreEqual(datosPersonaje.AlturaSalto, saltoBase, ToleranciaSalto, "el salto base mide lo de los datos");
            // El CharacterController sube escalones de stepOffset en pleno salto, así que el alcance real es salto + stepOffset.
            float escalon = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugadorPrefab).GetComponent<CharacterController>().stepOffset;
            float alcanceBase = saltoBase + escalon;
            medidas.Add("alcance real sin implantes " + F(alcanceBase) + " m (salto " + F(saltoBase) + " + escalón " + F(escalon) + "); cajas " + F(alturaCajaI) + "/" + F(alturaCajaII) + " m");
            bool subio = false;
            yield return IntentarSubir("PIERNAS I", s => subio = s);
            Assert.Greater(alturaCajaI, alcanceBase, "la caja PIERNAS I supera el alcance sin implantes (" + F(alcanceBase) + " m)");
            Assert.IsFalse(subio, "sin implantes la caja PIERNAS I no se sube");
            medidas.Add("sin implantes: caja PIERNAS I no se sube");
            subio = true;
            yield return IntentarSubir("PIERNAS II", s => subio = s);
            Assert.IsFalse(subio, "sin implantes la caja PIERNAS II no se sube");
            float totalSin = 0f;
            float tramoSin = 0f;
            yield return CorrerRecta((t, tr) => { totalSin = t; tramoSin = tr; });
            VerificarCarrera("sin implantes", totalSin, tramoSin, datosPersonaje.VelocidadCorrer);
            Paso("a", string.Join("; ", medidas));

            // b) Clínica: PIERNAS I y BRAZOS I, $4,000 en total, +2 h cada una; el espejo muestra las dos piezas.
            int dineroAntesB = cartera.Dinero;
            yield return InstalarVarios("b", (FilaPiernas, piernas1), (FilaBrazos, brazos1));
            Assert.AreEqual(PrecioPiernasMasBrazos, piernas1.Precio + brazos1.Precio, "los precios de los assets suman 4,000");
            Assert.AreEqual(dineroAntesB - PrecioPiernasMasBrazos, cartera.Dinero, "−$4,000 en total");
            Assert.AreEqual(piernas1.Nombre, implantes.Nombre(EspacioImplante.Piernas));
            Assert.AreEqual(brazos1.Nombre, implantes.Nombre(EspacioImplante.Brazos));
            yield return MirarseAlEspejo(false, null);
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Piernas));
            Paso("b", medidas[medidas.Count - 2] + "; " + medidas[medidas.Count - 1]);

            // c) Pista: PIERNAS I sube la caja PIERNAS I, no la PIERNAS II; la recta tarda lo esperado.
            float saltoI = 0f;
            yield return MedirSalto(v => saltoI = v);
            float saltoIEsperado = datosPersonaje.AlturaSalto * implantes.Efectos.multiplicadorSalto;
            medidas.Add("salto PIERNAS I " + F(saltoI) + " m (esperado " + F(saltoIEsperado) + "), cajas " + F(alturaCajaI) + "/" + F(alturaCajaII) + " m");
            Assert.AreEqual(saltoIEsperado, saltoI, ToleranciaSalto, "el salto con PIERNAS I mide lo calculado");
            Assert.Greater(saltoI, saltoBase, "PIERNAS I salta más");
            float alcanceI = saltoI + escalon;
            subio = false;
            yield return IntentarSubir("PIERNAS I", s => subio = s);
            Assert.IsTrue(subio, "con PIERNAS I la caja PIERNAS I sí se sube");
            subio = true;
            yield return IntentarSubir("PIERNAS II", s => subio = s);
            Assert.Greater(alturaCajaII, alcanceI, "la caja PIERNAS II supera el alcance con PIERNAS I (" + F(alcanceI) + " m)");
            Assert.IsFalse(subio, "con PIERNAS I la caja PIERNAS II no se sube");
            medidas.Add("PIERNAS I: alcance real " + F(alcanceI) + " m, caja PIERNAS II no se sube");
            float totalI = 0f;
            float tramoI = 0f;
            yield return CorrerRecta((t, tr) => { totalI = t; tramoI = tr; });
            VerificarCarrera("PIERNAS I", totalI, tramoI, datosPersonaje.VelocidadCorrer * piernas1.Efectos.multiplicadorCorrer);
            Assert.Less(tramoI, tramoSin, "con PIERNAS I la recta es más rápida");
            Paso("c", medidas[medidas.Count - 2] + "; " + medidas[medidas.Count - 1]);

            // d) 21:00: la pandilla. Tumbar a los 3 a puño con los golpes de BRAZOS I y cobrar su dinero.
            reloj.AvanzarHasta(HoraNoche);
            atajos.Add("Reloj avanzado a las 21:00");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(datosPandillero.TamanoPandilla, pandilla.Presentes, "a las 21:00 aparecen los 3");
            Colocar(InicioCerca, 0f);
            yield return Cuadros(FramesParaElHud);
            int dineroAntesD = cartera.Dinero;
            float danio = datosPersonaje.DanioGolpe * implantes.Efectos.multiplicadorGolpeAPie;
            int golpesPorPandillero = Necesarios(datosPandillero.VidaMaxima, danio);
            var golpesLista = new List<int>();
            maxAtacando = 0;
            int ronda = 0;
            while (pandilla.Presentes > 0 && ronda < 6)
            {
                PandilleroAPie siguiente = null;
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    if (!p.gameObject.activeSelf || p.EstaKO) continue;
                    if (siguiente == null || Distancia(p.transform.position, jugador.transform.position) < Distancia(siguiente.transform.position, jugador.transform.position)) siguiente = p;
                }
                if (siguiente == null) break;
                yield return Vencer(siguiente);
                golpesLista.Add(golpesDados);
                ronda++;
            }
            float finEspera = Time.time + datosPandillero.SegundosParaDesaparecer + 1f;
            while (Time.time < finEspera)
            {
                MantenerVivo();
                yield return Cuadro();
            }
            Assert.AreEqual(0, pandilla.Presentes, "vencidos los 3 no queda nadie en pie");
            Assert.AreEqual(datosPandillero.TamanoPandilla, golpesLista.Count, "se tumbó a los 3");
            foreach (int g in golpesLista) Assert.AreEqual(golpesPorPandillero, g, "golpes por pandillero con BRAZOS I, calculados desde los assets");
            Assert.LessOrEqual(maxAtacando, datosPandillero.MaxAtacando, "nunca más de " + datosPandillero.MaxAtacando + " atacando a la vez");
            Assert.AreEqual(dineroAntesD + datosPandillero.TamanoPandilla * datosPandillero.Dinero, cartera.Dinero, "cobró el dinero de los 3");
            medidas.Add("pandilla: golpes [" + string.Join(",", golpesLista) + "] (esperados " + golpesPorPandillero + " con BRAZOS I x" + F(implantes.Efectos.multiplicadorGolpeAPie)
                + "), máximo atacando " + maxAtacando + ", cobrado $" + (cartera.Dinero - dineroAntesD));
            Paso("d", medidas[medidas.Count - 1]);

            // e) Clínica cerrada de noche; al día siguiente a las 10:00, OJOS I y SINAPSIS I.
            AplicarHorarioDeLaClinica();
            Assert.IsFalse(lugarClinica.EstaAbierto, "de noche la clínica está cerrada");
            yield return IrAlMostrador(config.TextoCerrado);
            StringAssert.Contains("CERRADA", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantalla.Visible, "E no abre la clínica cerrada");
            reloj.AvanzarHasta(HoraDia);
            atajos.Add("Reloj avanzado a las 10:00 del día siguiente");
            yield return Cuadros(FramesParaElHud);
            AplicarHorarioDeLaClinica();
            Assert.IsTrue(lugarClinica.EstaAbierto, "a las 10:00 abre");
            Assert.AreEqual(0, pandilla.Presentes, "de día no hay pandilla");
            yield return InstalarVarios("e", (FilaOjos, ojos1), (FilaSistema, sinapsis1));
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Ojos));
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.SistemaNervioso));
            Paso("e", "clínica cerrada a las 21; " + medidas[medidas.Count - 1]);

            // f) Arena: E en el mostrador, pelea contra el rival 1 de la liga 1 con OJOS I y SINAPSIS I.
            reloj.AvanzarHasta(HoraNoche);
            atajos.Add("Reloj avanzado a las 21:00 (las peleas de liga abren de noche)");
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(ligas.Poner(1, 1, 0), "liga 1, rival 1");
            atajos.Add("Progreso fijado: liga 1, rival 1");
            jugador.Colocar(new Pose(mostradorPeleas.transform.position + new Vector3(0f, AlturaOjosJugador, -2f), Quaternion.identity));
            atajos.Add("Colocar: frente al mostrador de peleas");
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(mostradorPeleas.PuedeUsar, "el mostrador de peleas se puede usar: " + mostradorPeleas.Texto);
            int dineroAntesF = cartera.Dinero;
            CartelPelea cartel = calendario.Cartel(1, 1);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            if (mostradorPeleas.CarteleraAbierta)
            {
                yield return Tecla(teclado.enterKey);
                yield return Cuadros(FramesParaElHud);
            }
            if (mostradorPeleas.ApuestaAbierta) yield return Tecla(teclado.enterKey);
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.AreEqual(AliasPrimero, director.Rival.Nombre, "el rival 1 de la liga 1");
            rival = ((Component)director.Rival).GetComponent<RivalBasico>();
            IPeleador propio = montaje.GetComponent<IPeleador>();
            yield return EsperarHasta(() => propio.ControlActivo, MaxSegundosEstado, "el jugador tiene el control del mecha");
            yield return Cuadros(FramesParaElHud);
            framesSinMedir = FramesSinMedirTrasEscena;
            Assert.IsTrue(encendido.Paso >= PasoEncendido.Paso2, "la cabina está encendida");
            Assert.IsTrue(ojos.Visible, "con OJOS I la pantalla de ojos se prende");
            Assert.AreEqual(AliasPrimero, ojos.TextoNombre, "muestra el nombre del rival");
            Assert.AreEqual(1, led.Prendidos, "SINAPSIS I prende un LED");
            Assert.IsEmpty(ojos.TextoNumeros, "OJOS I no muestra números");

            yield return GolpesReales();
            yield return Cuadro();
            IVida vidaRival = director.Rival.Vida;
            Assert.AreEqual(vidaRival.Actual / vidaRival.Maxima, ojos.Barra, ToleranciaBarra, "la barra coincide con la vida del rival");

            float anticipacion = ojos1.Efectos.anticipacionAtaque;
            var adelantos = new List<float>();
            var aparte = new List<string>();
            var fallos = new List<string>();
            yield return MedirAvisoDeGolpes(anticipacion, adelantos, aparte, fallos);
            int totalGolpes = adelantos.Count + aparte.Count + fallos.Count;
            string detalleAviso = "anticipación " + F(anticipacion) + " s; golpes " + totalGolpes + "; a tiempo " + adelantos.Count + " ["
                + string.Join(", ", adelantos.ConvertAll(F)) + "]; aparte " + aparte.Count + " [" + string.Join(" | ", aparte) + "]; sin explicar " + fallos.Count
                + " [" + string.Join(" | ", fallos) + "]";
            Assert.GreaterOrEqual(totalGolpes, MinimoGolpesMedidos, "hubo golpes del rival que medir. " + detalleAviso);
            Assert.GreaterOrEqual(adelantos.Count, MinimoGolpesMedidos, "al menos " + MinimoGolpesMedidos + " golpes avisados a tiempo. " + detalleAviso);
            Assert.GreaterOrEqual(adelantos.Count, Mathf.CeilToInt((adelantos.Count + fallos.Count) * FraccionMinimaAvisada), "al menos el 80 % de los golpes explicables avisados a tiempo (los 'aparte' llegan pegados a otro golpe). " + detalleAviso);

            ValoresMecha conSinapsis = ReglaReaccionImplantes.Aplicar(basales, sinapsis1.Efectos.multiplicadorReaccionMecha);
            float golpeMedido = 0f;
            yield return MedirGolpeDelJugador(conSinapsis.preparacionGolpe, v => golpeMedido = v);
            Assert.AreEqual(conSinapsis.preparacionGolpe, vidaMecha.Datos.PreparacionGolpe, 0.0001f, "los datos del mecha traen la preparación de SINAPSIS I");
            Assert.Less(conSinapsis.preparacionGolpe, basales.preparacionGolpe, "SINAPSIS I acorta el golpe");
            medidas.Add("ojos: " + detalleAviso + "; golpe del jugador " + F(golpeMedido) + " s (esperado " + F(conSinapsis.preparacionGolpe) + ", sin implante " + F(basales.preparacionGolpe) + ")");

            float finAviso = Time.time + MaxSegundosEstado;
            while (!(ojos.AvisoPrendido && rival.EnAviso && ojos.TextoAviso == configOjos.TextoProximoGolpe) && Time.time < finAviso)
            {
                yield return Cuadro();
                MantenerVivoMecha();
            }
            CapturarCabina(RutaCaptura2);
            MantenerVivoMecha();
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio al rival después de daño real del jugador");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !mostradorPeleas.GetComponent<EntradaArena>().EnCurso, MaxSegundosEstado, "termina la salida de la arena");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(dineroAntesF + cartel.premioGanar, cartera.Dinero, "cobró el premio del cartel");
            Assert.AreEqual(2, ligas.Siguiente, "el siguiente rival pasa a 2");
            medidas.Add("premio $" + cartel.premioGanar);
            Paso("f", medidas[medidas.Count - 2] + "; " + medidas[medidas.Count - 1]);

            // g) Liga 3 por código y dinero fijado: mejorar los 4 a nivel 2 pagando la diferencia; el espejo con franjas; la caja PIERNAS II.
            reloj.AvanzarHasta(HoraDia);
            atajos.Add("Reloj avanzado a las 10:00 del día siguiente");
            yield return Cuadros(FramesParaElHud);
            AplicarHorarioDeLaClinica();
            PonerLiga(3);
            PonerDinero(DineroParaMejorar);
            int diferenciaTotal = (ojos2.Precio - ojos1.Precio) + (brazos2.Precio - brazos1.Precio) + (piernas2.Precio - piernas1.Precio) + (sinapsis2.Precio - sinapsis1.Precio);
            yield return InstalarVarios("g", (FilaOjos, ojos2), (FilaBrazos, brazos2), (FilaPiernas, piernas2), (FilaSistema, sinapsis2));
            Assert.AreEqual(DineroParaMejorar - diferenciaTotal, cartera.Dinero, "se pagó solo la diferencia de los 4");
            yield return MirarseAlEspejo(true, RutaCaptura1);
            float saltoII = 0f;
            yield return MedirSalto(v => saltoII = v);
            float saltoIIEsperado = datosPersonaje.AlturaSalto * implantes.Efectos.multiplicadorSalto;
            medidas.Add("salto PIERNAS II " + F(saltoII) + " m (esperado " + F(saltoIIEsperado) + "), caja PIERNAS II " + F(alturaCajaII) + " m");
            Assert.AreEqual(saltoIIEsperado, saltoII, ToleranciaSalto, "el salto con PIERNAS II mide lo calculado");
            subio = false;
            yield return IntentarSubir("PIERNAS II", s => subio = s);
            Assert.IsTrue(subio, "con PIERNAS II la caja PIERNAS II sí se sube");
            Paso("g", "diferencia total $" + diferenciaTotal + "; " + medidas[medidas.Count - 1]);

            // h) 23:00 con $300: dejarse tumbar por la pandilla. Despierta a las 05:00 con −$200, calor 0 y el objetivo de volver a casa.
            reloj.AvanzarHasta(HoraCaida);
            reloj.Pausar(true);
            atajos.Add("Reloj a las 23:00 y pausado");
            yield return Cuadros(FramesParaElHud);
            PonerDinero(DineroDeLaCaida);
            Registro<ICalor>.Actual.Sumar(CalorAntesDeCaer, MotivoCalor.Otro);
            atajos.Add("Calor sumado por código: " + CalorAntesDeCaer);
            Assert.Greater(Registro<ICalor>.Actual.Valor, 0f, "hay calor antes de caer");
            float vidaMechaAntes = vidaMecha.Actual;
            protegerVida = false;
            Assert.AreEqual(datosPandillero.TamanoPandilla, pandilla.Presentes, "la pandilla está en la calle");
            Colocar(InicioCerca, 0f);
            float finKO = Time.time + MaxSegundosKO;
            while (!vidaJugador.EstaKO && Time.time < finKO) yield return Cuadro();
            Assert.IsTrue(vidaJugador.EstaKO, "la pandilla dejó al jugador en KO (quieto)");
            Assert.IsTrue(doctor.Ocupado, "el doctor atiende la caída");
            bool vioNegro = false;
            bool vioSinControl = false;
            float finDoctor = Time.time + MaxSegundosDoctor;
            while ((doctor.Ocupado || doctor.Caidas == 0) && Time.time < finDoctor)
            {
                PantallaFundido fundido = PantallaFundido.Actual;
                if (fundido != null && fundido.Opaco) vioNegro = true;
                if (doctor.Ocupado && !controlador.ControlActivo) vioSinControl = true;
                yield return Cuadro();
            }
            Assert.IsFalse(doctor.Ocupado, "la secuencia del doctor terminó");
            Assert.AreEqual(1, doctor.Caidas, "una sola caída");
            yield return Cuadros(FramesParaElHud);
            CapturarHudEn(RutaCaptura3);
            Assert.IsTrue(vioNegro, "la pantalla se fue a negro");
            Assert.IsTrue(vioSinControl, "durante la secuencia no hay control");
            Assert.AreEqual(DeudaEsperada, DineroDeLaCaida - datosDoctor.Cobro, "300 − 500 = −200 según los assets");
            Assert.AreEqual(DeudaEsperada, cartera.Dinero, "dinero después del cobro");
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, 0.001f, "calor borrado");
            float horaEsperada = ReglaDoctor.HoraDeDespertar(HoraCaida, datosDoctor.HorasInconsciente);
            Assert.AreEqual(5f, horaEsperada, 0.001f, "los assets dicen que despierta a las 05:00");
            Assert.AreEqual(horaEsperada, Normalizar(reloj.Hora), ToleranciaHora, "despierta a las 05:00");
            Assert.IsFalse(vidaJugador.EstaKO, "ya no está en KO");
            Assert.IsTrue(controlador.ControlActivo, "el control volvió");
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            Assert.IsNotNull(objetivos);
            Assert.IsTrue(objetivos.HayObjetivo, "hay objetivo");
            Assert.AreEqual(datosDoctor.TextoObjetivo, objetivos.Actual.Texto, "el objetivo");
            StringAssert.Contains("VUELVE A CASA", objetivos.Actual.Texto);
            IPuntoAparicion camilla = null;
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == datosDoctor.IdAparicionCamilla) camilla = punto;
            }
            Assert.IsNotNull(camilla);
            Assert.Less(Distancia(jugador.transform.position, camilla.Pose.position), DistanciaCamillaMax, "despierta en la camilla");
            Assert.AreEqual(vidaMechaAntes, vidaMecha.Actual, 0.01f, "el mecha en el taller no cambió");
            medidas.Add("caída: dinero " + cartera.Dinero + ", hora " + F(Normalizar(reloj.Hora)) + ", vida mecha " + F(vidaMecha.Actual));
            Colocar(AntesDeCasa, 0f);
            Press(teclado.wKey);
            float finCasa = Time.time + MaxSegundosCasa;
            while (objetivos.HayObjetivo && Time.time < finCasa) yield return Cuadro();
            Release(teclado.wKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(objetivos.HayObjetivo, "al entrar a Casa se quita el objetivo");
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Ojos));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Brazos));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Piernas));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.SistemaNervioso));
            Paso("h", medidas[medidas.Count - 1]);

            // i) F5; quitar implantes y cambiar el dinero por código; F9: todo vuelve.
            int dineroGuardado = cartera.Dinero;
            float vidaMechaGuardada = vidaMecha.Actual;
            yield return Tecla(teclado.f5Key);
            yield return Cuadros(FramesParaElHud);
            implantes.Cargar("{\"ids\":[]}");
            atajos.Add("Implantes quitados por código");
            PonerDinero(DineroDePrueba);
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaSegura);
            atajos.Add("Vida del mecha cambiada por código");
            Assert.AreEqual(0, implantes.Cantidad, "sin implantes antes de F9");
            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud * 2);
            Assert.AreEqual(4, implantes.Cantidad, "F9 devuelve los 4 implantes");
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Ojos));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Brazos));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Piernas));
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.SistemaNervioso));
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "F9 devuelve el dinero");
            Assert.AreEqual(vidaMechaGuardada, vidaMecha.Actual, 0.01f, "F9 devuelve la vida del mecha");
            Paso("i", "implantes 4, dinero " + cartera.Dinero + ", vida mecha " + F(vidaMecha.Actual));

            // j) consola y tirones.
            Debug.Log("[T-197] medidas: " + string.Join(" || ", medidas));
            Debug.Log("[T-197] pasos: " + string.Join(" || ", bitacora));
            VerificarFinal("recorrido");
        }
    }
}
