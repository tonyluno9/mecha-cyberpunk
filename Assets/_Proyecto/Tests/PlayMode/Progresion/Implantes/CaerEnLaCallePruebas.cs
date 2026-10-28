using System;
using System.Collections;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-196: caer en la calle en Pruebas_Implantes. Atajos permitidos y anotados: reloj pausado a las 23:00, dinero, calor y vida del mecha
    /// por código, el daño al jugador por código en las pruebas b, d y e (la prueba a lo deja caer la pandilla de verdad), <c>Colocar</c>,
    /// llamar al mecha con M (real) y empezar la pelea del flujo con <c>EntradaArena.Empezar</c>.
    /// </summary>
    public class CaerEnLaCallePruebas : BaseImplantesPruebas
    {
        private const string RutaDatosDoctor = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Base.asset";
        private const string RutaDatosEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-196-1.png";
        private const float HoraCaida = 23f;
        private const float DanioKO = 100000f;
        private const float AlturaOjos = 0.05f;
        private const float MaxSegundosKO = 120f;
        private const float MaxSegundosDoctor = 40f;
        private const float MaxSegundosCasa = 8f;
        private const float MaxSegundosLlamado = 15f;
        private const float MaxSegundosPelea = 60f;
        private const float SegundosSinDoctor = 9f;
        private const float DistanciaCamillaMax = 1.5f;
        private const float ToleranciaFraccion = 0.01f;
        private const float ToleranciaHora = 0.05f;
        private const float SegundosEntreClics = 0.25f;
        private const float CalorInicial = 40f;
        private const int CobroEsperadoDeuda = -200;
        private const int DineroInicial = 300;

        private static readonly Vector3 InicioCerca = new Vector3(20f, AlturaOjos, -6f);
        private static readonly Vector3 InicioLejosDeLaPandilla = new Vector3(0f, AlturaOjos, -8f);
        private static readonly Vector3 AntesDeCasa = new Vector3(5f, AlturaOjos, 12f);

        private DoctorClinica doctor;
        private DatosDoctor datos;
        private VidaAPie vidaJugador;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private LlamadoMecha llamado;
        private ServicioObjetivos objetivos;
        private float fraccionPiso;
        private bool vioNegro;
        private bool vioSinControl;
        private bool vioAvisoCobro;
        private bool capturaHecha;
        private readonly System.Collections.Generic.List<string> medidas = new System.Collections.Generic.List<string>();

        private sealed class DobleHistoria : IHistoria
        {
            public EtapaPrologo Etapa => EtapaPrologo.Emboscada;

            public event Action<EtapaPrologo> EtapaCambio
            {
                add { }
                remove { }
            }
        }

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            medidas.Clear();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // ---- Preparación ----

        private IEnumerator Preparar(bool aNoche)
        {
            yield return CargarEscena();
            doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(doctor, "Pruebas_Implantes trae al doctor");
            datos = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatosDoctor);
            Assert.IsNotNull(datos, "existe DatosDoctor_Base");
            vidaJugador = jugador.GetComponent<VidaAPie>();
            persistente = Object.FindFirstObjectByType<MechaPersistente>();
            Assert.IsNotNull(persistente, "hay mecha de la historia");
            vidaMecha = persistente.GetComponent<VidaMecha>();
            llamado = persistente.GetComponent<LlamadoMecha>();
            objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            Assert.IsNotNull(objetivos, "hay objetivos");
            string[] guias = AssetDatabase.FindAssets("t:ConfigMechaDaniado");
            Assert.AreEqual(1, guias.Length, "hay una ConfigMechaDaniado");
            fraccionPiso = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mecha.ConfigMechaDaniado>(AssetDatabase.GUIDToAssetPath(guias[0])).PisoVidaPersistente;
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "el mecha empieza en el taller");
            if (aNoche)
            {
                reloj.AvanzarHasta(HoraCaida);
                reloj.Pausar(true);
                atajos.Add("Reloj a las 23:00 y pausado");
                yield return Cuadros(FramesParaElHud);
            }
            vioNegro = false;
            vioSinControl = false;
            vioAvisoCobro = false;
            capturaHecha = false;
        }

        private void Colocar(Vector3 posicion)
        {
            jugador.Colocar(new Pose(posicion, Quaternion.identity));
            atajos.Add("Colocar: " + posicion);
        }

        private void MatarPorCodigo()
        {
            IRecibeDanio recibe = jugador.GetComponent<IRecibeDanio>();
            recibe.RecibirDanio(new InfoDanio(DanioKO, TipoDanio.Golpe, jugador.transform.position, Vector3.forward, null));
            atajos.Add("Daño de KO al jugador por código");
            Assert.IsTrue(vidaJugador.EstaKO, "el jugador cayó en KO");
        }

        // Llama al mecha con M desde un sitio seguro y lo deja en la calle con esa fracción de vida.
        private IEnumerator MechaEnLaCalle(float fraccion)
        {
            Colocar(InicioLejosDeLaPandilla);
            yield return Cuadros(FramesParaElHud);
            yield return Tecla(teclado.mKey);
            float fin = Time.time + MaxSegundosLlamado;
            while (llamado.Estado != EstadoLlamado.EnCalle && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado, "el mecha llegó a la calle");
            if (fraccion > 0f)
            {
                vidaMecha.FijarVida(vidaMecha.Maxima * fraccion);
                atajos.Add("Vida del mecha fijada en " + fraccion);
            }
        }

        // Espera que la secuencia del doctor termine y anota lo que se ve mientras corre.
        private IEnumerator EsperarDoctor(bool capturar)
        {
            float fin = Time.time + MaxSegundosDoctor;
            while ((doctor.Ocupado || doctor.Caidas == 0) && Time.time < fin)
            {
                Observar();
                yield return null;
                Vigilar();
            }
            Assert.IsFalse(doctor.Ocupado, "la secuencia del doctor terminó");
            Assert.AreEqual(1, doctor.Caidas, "una sola secuencia por caída");
            Observar();
            yield return Cuadros(FramesParaElHud);
            Observar();
            if (capturar && !capturaHecha)
            {
                capturaHecha = true;
                string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
                CapturarConHud(completa);
            }
        }

        private void Observar()
        {
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null && fundido.Opaco) vioNegro = true;
            if (doctor.Ocupado && !controlador.ControlActivo) vioSinControl = true;
            if (hud.TextoAvisoDinero.gameObject.activeInHierarchy && hud.TextoAvisoDinero.text.Contains("500")) vioAvisoCobro = true;
        }

        private void VerificarDespertar(int dineroEsperado, float horaEsperada)
        {
            Assert.IsTrue(vioNegro, "la pantalla se fue a negro");
            Assert.IsTrue(vioSinControl, "durante la secuencia no hay control");
            Assert.IsTrue(vioAvisoCobro, "el HUD anunció el cobro");
            Assert.IsFalse(vidaJugador.EstaKO, "el jugador ya no está en KO");
            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual, 0.01f, "vida llena");
            Assert.IsTrue(controlador.ControlActivo, "el control volvió");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "el jugador está a pie");
            IPuntoAparicion camilla = null;
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == datos.IdAparicionCamilla) camilla = punto;
            }
            Assert.IsNotNull(camilla, "la escena trae la camilla");
            Vector3 delta = jugador.transform.position - camilla.Pose.position;
            delta.y = 0f;
            Assert.Less(delta.magnitude, DistanciaCamillaMax, "despierta en la camilla");
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "dinero después del cobro");
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, 0.001f, "calor borrado");
            Assert.AreEqual(horaEsperada, Normalizar(reloj.Hora), ToleranciaHora, "hora al despertar");
            Assert.IsTrue(objetivos.HayObjetivo, "hay objetivo");
            Assert.AreEqual(datos.TextoObjetivo, objetivos.Actual.Texto, "objetivo de volver a casa");
            Assert.AreEqual(IdLugar.Casa, objetivos.Actual.Lugar, "el objetivo apunta a la casa");
            string esperado = ReglaDoctor.TextoDoctor(dineroEsperado, datos.TextoDoctor, datos.TextoDeuda);
            Assert.AreEqual(esperado, doctor.UltimoTexto, "lo que dice el doctor");
            PantallaSubtitulos subtitulos = Object.FindFirstObjectByType<PantallaSubtitulos>();
            Assert.AreEqual(esperado, subtitulos.Subtitulo, "el subtítulo está en pantalla");
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Pandilla_TumbaAlJugadorDeNoche_DespiertaEnLaCamillaConDeudaYVuelveACasa()
        {
            yield return Preparar(true);
            PonerDinero(DineroInicial);
            Registro<ICalor>.Actual.Sumar(CalorInicial, MotivoCalor.Otro);
            atajos.Add("Calor sumado por código: " + CalorInicial);
            Assert.Greater(Registro<ICalor>.Actual.Valor, 0f, "hay calor antes de caer");
            float vidaMechaAntes = vidaMecha.Actual;

            Colocar(InicioCerca);
            float fin = Time.time + MaxSegundosKO;
            while (!vidaJugador.EstaKO && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.IsTrue(vidaJugador.EstaKO, "la pandilla dejó al jugador en KO (quieto, sin defenderse)");
            Assert.IsTrue(doctor.Ocupado, "el doctor atiende la caída");
            Assert.Greater(Registro<ICalor>.Actual.Valor, 0f, "al caer todavía hay calor (el doctor es quien lo borra)");
            yield return EsperarDoctor(true);

            int esperado = DineroInicial - datos.Cobro;
            Assert.AreEqual(CobroEsperadoDeuda, esperado, "300 - 500 = -200");
            VerificarDespertar(esperado, ReglaDoctor.HoraDeDespertar(HoraCaida, datos.HorasInconsciente));
            Assert.AreEqual(vidaMechaAntes, vidaMecha.Actual, 0.01f, "el mecha en el taller no cambió");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "sigue en el taller");
            Assert.IsTrue(hud.TextoDinero.text.Contains("200"), "el HUD dice la deuda: " + hud.TextoDinero.text);
            medidas.Add("dinero " + cartera.Dinero + ", hora " + Normalizar(reloj.Hora).ToString("F2", CultureInfo.InvariantCulture));

            // Vuelve a casa por su cuenta: al entrar a Casa se quita el objetivo.
            Colocar(AntesDeCasa);
            Press(teclado.wKey);
            fin = Time.time + MaxSegundosCasa;
            while (objetivos.HayObjetivo && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Release(teclado.wKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(objetivos.HayObjetivo, "al entrar a Casa se quita el objetivo");
            Assert.AreEqual(1, doctor.Caidas, "sigue siendo una sola caída");
            Debug.Log("[T-196] a) " + string.Join("; ", medidas));
            VerificarFinal("a");
        }

        [UnityTest]
        public IEnumerator MechaEnLaCalleConOchentaPorCiento_VuelveAlTallerConVeintePorCientoYElBancoCobraLoCalculado()
        {
            yield return Preparar(true);
            PonerDinero(DineroAlto);
            yield return MechaEnLaCalle(0.8f);
            Colocar(InicioCerca);
            MatarPorCodigo();
            yield return EsperarDoctor(false);

            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "el mecha volvió solo al taller");
            float fraccion = vidaMecha.Actual / vidaMecha.Maxima;
            Assert.AreEqual(datos.FraccionVidaMecha, fraccion, ToleranciaFraccion, "queda con el 20 %");
            Assert.IsFalse(vidaMecha.EstaKO, "el mecha no está en KO");

            // f) Reparar en el banco cuesta lo calculado desde los assets.
            BancoTaller banco = persistente.GetComponent<BancoTaller>();
            DatosEconomia precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaDatosEconomia);
            Assert.IsNotNull(precios, RutaDatosEconomia);
            float danio = vidaMecha.Maxima * (1f - datos.FraccionVidaMecha);
            int costo = ReglaReparacion.Costo(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            Assert.AreEqual(costo, banco.Costo, "el banco cobra el daño que dejó el doctor");
            Assert.Greater(costo, 0, "hay algo que pagar");
            medidas.Add("vida " + (fraccion * 100f).ToString("F0", CultureInfo.InvariantCulture) + " %, reparación $" + costo);
            Debug.Log("[T-196] b) " + string.Join("; ", medidas));
            VerificarFinal("b");
        }

        [UnityTest]
        public IEnumerator MechaEnLaCalleConDiezPorCiento_QuedaEnElPisoDeEmergencia()
        {
            yield return Preparar(true);
            yield return MechaEnLaCalle(0.1f);
            Colocar(InicioCerca);
            MatarPorCodigo();
            yield return EsperarDoctor(false);

            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "el mecha volvió solo al taller");
            Assert.AreEqual(fraccionPiso, vidaMecha.Actual / vidaMecha.Maxima, ToleranciaFraccion, "queda en el piso de emergencia");
            Debug.Log("[T-196] b) piso " + fraccionPiso + ", vida " + vidaMecha.Actual / vidaMecha.Maxima);
            VerificarFinal("b2");
        }

        [UnityTest]
        public IEnumerator PerderUnaPeleaDelFlujo_NoLlamaAlDoctor()
        {
            yield return Preparar(true);
            PonerDinero(DineroInicial);
            MostradorPeleas peleas = Object.FindFirstObjectByType<MostradorPeleas>();
            Assert.IsNotNull(peleas, "la escena trae el mostrador de peleas");
            EntradaArena entrada = peleas.GetComponent<EntradaArena>();
            DirectorPelea flujo = peleas.GetComponentInChildren<DirectorPelea>(true);
            vidaMecha.FijarVida(vidaMecha.Maxima * datos.FraccionVidaMecha);
            atajos.Add("Vida del mecha al 20 % y pelea empezada con EntradaArena.Empezar");
            Colocar(InicioLejosDeLaPandilla);
            Assert.IsTrue(entrada.Empezar(flujo), "empieza la entrada a la arena");

            float fin = Time.time + MaxSegundosPelea;
            while (flujo.Estado != EstadoFlujoPelea.Peleando && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(EstadoFlujoPelea.Peleando, flujo.Estado, "la pelea empezó");
            bool clic = false;
            float proximo = 0f;
            fin = Time.time + MaxSegundosPelea;
            while (!vidaMecha.EstaKO && Time.time < fin)
            {
                if (Time.time >= proximo)
                {
                    clic = !clic;
                    proximo = Time.time + SegundosEntreClics / 2f;
                    if (clic) Press(raton.leftButton); else Release(raton.leftButton);
                }
                yield return null;
                Vigilar();
            }
            Release(raton.leftButton);
            Assert.IsTrue(vidaMecha.EstaKO, "el rival dejó al mecha en KO con golpes reales");
            fin = Time.time + MaxSegundosPelea;
            while (entrada.EnCurso && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            float finEspera = Time.time + SegundosSinDoctor;
            while (Time.time < finEspera)
            {
                yield return null;
                Vigilar();
            }

            Assert.AreEqual(0, doctor.Caidas, "el doctor no atendió nada");
            Assert.IsFalse(doctor.Ocupado, "el doctor no está ocupado");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "no cobró");
            Assert.IsFalse(objetivos.HayObjetivo && objetivos.Actual.Texto == datos.TextoObjetivo, "no hay objetivo de volver a casa");
            VerificarFinal("d");
        }

        [UnityTest]
        public IEnumerator ConLaHistoriaFueraDeLibre_ElKOAPieNoLlamaAlDoctor()
        {
            yield return Preparar(true);
            PonerDinero(DineroInicial);
            DobleHistoria doble = new DobleHistoria();
            Registro<IHistoria>.Agregar(doble);
            atajos.Add("Doble de historia en Emboscada");
            Colocar(InicioLejosDeLaPandilla);
            MatarPorCodigo();
            float fin = Time.time + SegundosSinDoctor;
            while (Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(0, doctor.Caidas, "el doctor no atendió nada");
            Assert.IsTrue(vidaJugador.EstaKO, "el jugador sigue en KO");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "no cobró");
            Registro<IHistoria>.Quitar(doble);
            VerificarFinal("e");
        }
    }
}
