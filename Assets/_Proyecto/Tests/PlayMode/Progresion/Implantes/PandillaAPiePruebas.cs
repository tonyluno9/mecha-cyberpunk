using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-195: la pandilla a pie de Pruebas_Implantes, de noche, con entrada simulada (WASD, Shift, ratón, clic, tecla 2). Atajos permitidos y
    /// anotados: <c>AvanzarHasta</c> para la noche y el día, <c>Colocar</c> al jugador cerca del sitio, implantes por código, retirar a
    /// los pandilleros que estorban a una medición, <c>VidaAPie.Restaurar</c> para que el jugador no caiga en KO a media medición y mover
    /// el mecha junto al jugador. El daño al pandillero siempre sale de golpes y tiros reales.
    /// </summary>
    public class PandillaAPiePruebas : BaseImplantesPruebas
    {
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Base.asset";
        private const string RutaDatosPistola = "Assets/_Proyecto/Personaje/Datos/DatosPistola_Base.asset";
        private const string RutaControles = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-195-1.png";
        private const string IdBrazos1 = "implante_brazos_1";
        private const string IdBrazos2 = "implante_brazos_2";
        private const string IdPiernas1 = "implante_piernas_1";
        private const string IdOjos1 = "implante_ojos_1";
        private const string IdOjos2 = "implante_ojos_2";
        private const float HoraDia = 8f;
        private const float AlturaOjos = 0.05f;
        private const float GananciaMirada = 0.7f;
        private const float MaxDeltaRaton = 600f;
        private const float ToleranciaMiraGrados = 2.5f;
        private const float DistanciaGolpeRealizable = 1.5f;
        private const float DistanciaAcercarse = 1.4f;
        private const float SegundosEntreDisparos = 0.35f;
        private const float AlturaCentro = 0.9f;
        private const float DesvioParaVerElBrazo = 0.4f;
        private const float SegundosParaCapturar = 0.4f;
        private const float VidaMinimaSegura = 40f;
        private const float MaxSegundosVencer = 30f;
        private const float SegundosPersecucion = 1.5f;
        private const float SegundosDeRitmo = 3.8f;
        private const float MaxSegundosFuga = 25f;
        private const float MaxSegundosRegreso = 15f;
        private const float MargenRegreso = 1f;
        private const float DistanciaMetaFuga = 8f;
        private const float DistanciaInicioFuga = 8f;
        private const float ToleranciaGolpePasado = 0.2f;
        private const float DistanciaMecha = 1.5f;
        private const float SegundosMecha = 6f;
        private const int FramesMaxApuntar = 90;
        private const int MaxSimultaneosPreparando = 2;

        private static readonly Vector3 SitioPandilla = new Vector3(20f, 0f, 0f);
        private static readonly Vector3 InicioCerca = new Vector3(20f, AlturaOjos, -6f);
        private static readonly Vector3 InicioLejos = new Vector3(20f, AlturaOjos, -9f);
        private static readonly Vector3 InicioFuga = new Vector3(12f, AlturaOjos, 0f);

        private DatosPandilleroAPie datos;
        private DatosPersonaje datosPersonaje;
        private DatosPistola datosPistola;
        private ConfigControlesAPie controles;
        private PandillaAPie pandilla;
        private VidaAPie vidaJugador;
        private GolpeAPie golpeJugador;
        private ArmasAPie armas;
        private PistolaAPie pistola;
        private bool clicPulsado;
        private readonly List<string> medidas = new List<string>();

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            clicPulsado = false;
            medidas.Clear();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // ---- Preparación ----

        private IEnumerator CargarNoche(bool aNoche = true)
        {
            yield return CargarEscena();
            datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            datosPersonaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatosPersonaje);
            datosPistola = AssetDatabase.LoadAssetAtPath<DatosPistola>(RutaDatosPistola);
            controles = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControles);
            Assert.IsNotNull(datos, "existe DatosPandilleroAPie_Base");
            Assert.IsNotNull(datosPistola, "existe DatosPistola_Base");
            Assert.IsNotNull(controles, "existe ConfigControlesAPie_Base");
            pandilla = Object.FindFirstObjectByType<PandillaAPie>();
            Assert.IsNotNull(pandilla, "Pruebas_Implantes trae la pandilla en Sitio_Pandilla");
            vidaJugador = jugador.GetComponent<VidaAPie>();
            golpeJugador = jugador.GetComponent<GolpeAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
            pistola = jugador.GetComponent<PistolaAPie>();
            Assert.IsNotNull(vidaJugador);
            Assert.IsNotNull(golpeJugador);
            Assert.IsNotNull(armas);
            Assert.IsNotNull(pistola);
            if (aNoche)
            {
                reloj.AvanzarHasta(HoraNoche);
                atajos.Add("Reloj avanzado a las 21:00");
                yield return Cuadros(FramesParaElHud);
            }
        }

        private void Colocar(Vector3 posicion, float giro)
        {
            jugador.Colocar(new Pose(posicion, Quaternion.Euler(0f, giro, 0f)));
            atajos.Add("Colocar: " + posicion);
        }

        private void MantenerVivo()
        {
            if (vidaJugador.Actual < VidaMinimaSegura)
            {
                vidaJugador.Restaurar();
            }
        }

        private IEnumerator Cuadro()
        {
            yield return null;
            Vigilar();
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                MantenerVivo();
                yield return Cuadro();
            }
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // Deja un solo pandillero a la derecha del sitio (el que está en x > sitio y más al sur) y retira a los demás.
        private PandilleroAPie DejarUno()
        {
            PandilleroAPie elegido = null;
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                if (elegido == null || Distancia(p.Hogar, InicioCerca) < Distancia(elegido.Hogar, InicioCerca)) elegido = p;
            }
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                if (p != elegido) p.gameObject.SetActive(false);
            }
            atajos.Add("Retirados los otros pandilleros para medir con uno");
            return elegido;
        }

        // ---- Entrada simulada ----

        // Un paso de mirada con el ratón hacia un punto del mundo: devuelve el error angular que queda (°).
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

        private IEnumerator Apuntar(Vector3 punto)
        {
            for (int i = 0; i < FramesMaxApuntar; i++)
            {
                float error = PasoDeMira(punto);
                yield return Cuadro();
                if (error < ToleranciaMiraGrados) break;
            }
            Set(raton.delta, Vector2.zero);
            yield return Cuadro();
        }

        private bool Mirando(Vector3 punto)
        {
            Transform camara = controlador.Camara;
            return Vector3.Angle(camara.forward, punto - camara.position) < ToleranciaMiraGrados * 2f;
        }

        // Vence a un pandillero con puños o pistola; devuelve cuántos impactos reales le dieron.
        private int golpesDados;
        private int clicsDados;

        private IEnumerator Vencer(PandilleroAPie p, bool conPistola)
        {
            golpesDados = 0;
            clicsDados = 0;
            float ultimaVida = p.Actual;
            float ultimoDisparo = -10f;
            float fin = Time.time + MaxSegundosVencer;
            while (!p.EstaKO && p.gameObject.activeSelf && Time.time < fin)
            {
                MantenerVivo();
                Vector3 centro = p.transform.position + Vector3.up * AlturaCentro;
                PasoDeMira(centro);
                float distancia = Distancia(p.transform.position, jugador.transform.position);
                bool apuntando = Mirando(centro);
                if (!conPistola)
                {
                    if (distancia > DistanciaAcercarse) Press(teclado.wKey); else Release(teclado.wKey);
                }
                if (clicPulsado)
                {
                    Release(raton.leftButton);
                    clicPulsado = false;
                }
                else if (apuntando)
                {
                    bool puede = conPistola
                        ? Time.time - ultimoDisparo >= SegundosEntreDisparos && !pistola.Recargando
                        : distancia <= DistanciaGolpeRealizable && golpeJugador.Fase == FaseGolpe.Listo;
                    if (puede)
                    {
                        Press(raton.leftButton);
                        clicPulsado = true;
                        ultimoDisparo = Time.time;
                        clicsDados++;
                    }
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

        private IEnumerator CambiarAPistola()
        {
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "la tecla 2 saca la pistola");
        }

        private static int Necesarios(float vida, float danio) => Mathf.CeilToInt(vida / danio - 0.0001f);

        // ---- a) De día no hay pandilla; a las 21:00 aparecen 3; al amanecer se retiran ----

        [UnityTest]
        public IEnumerator Horario_DeDiaNoHayPandilla_ALas21Aparecen3_YAlAmanecerSeRetiran()
        {
            yield return CargarNoche(false);
            Assert.AreEqual(0, pandilla.Presentes, "a las 10:00 no hay pandilla");
            Assert.AreEqual(0, ContarRegistrados(), "de día los OJOS no leen a nadie");

            reloj.AvanzarHasta(HoraNoche);
            atajos.Add("Reloj avanzado a las 21:00");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(datos.TamanoPandilla, pandilla.Presentes, "a las 21:00 aparecen 3");
            Assert.AreEqual(3, pandilla.Pandilleros.Length);
            Assert.AreEqual(datos.TamanoPandilla, ContarRegistrados(), "los 3 se pueden leer");
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                Assert.AreEqual(datos.VidaMaxima, p.Actual, 0.001f, "con la vida de los datos");
                Assert.AreEqual(EstadoPandillero.Esperando, p.Estado);
                Assert.AreEqual(p.Hogar.x, p.transform.position.x, 0.2f, "en su punto");
                Assert.AreEqual(Distancia(SitioPandilla, p.Hogar), datos.RadioFormacion, 0.2f, "alrededor del sitio");
            }

            reloj.AvanzarHasta(HoraDia);
            atajos.Add("Reloj avanzado a las 8:00 del día siguiente");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(0, pandilla.Presentes, "al amanecer, sin pelea, se retiran");
            Assert.AreEqual(0, ContarRegistrados());
            VerificarFinal("Horario");
        }

        private static int ContarRegistrados()
        {
            int cuenta = 0;
            foreach (var lectura in Registro<MechaCyberpunk.Combate.Lectura.ILecturaRival>.Todos)
            {
                if (!lectura.EsMecha && lectura.Raiz != null && lectura.Raiz.gameObject.activeInHierarchy) cuenta++;
            }
            return cuenta;
        }

        // ---- b) Persiguen, pegan con el daño y el ritmo de los datos, nunca más de 2 a la vez ----

        [UnityTest]
        public IEnumerator Pelea_PersiguenYPegan_ConElDanioYElRitmoDeLosDatos_ConMaximoDosAtacando()
        {
            yield return CargarNoche();
            Colocar(InicioLejos, 0f);
            yield return Cuadros(FramesParaElHud);

            // Caminar hacia ellos: salen a perseguir.
            float distanciaInicial = DistanciaMinima();
            Press(teclado.wKey);
            bool persiguieron = false;
            float fin = Time.time + SegundosPersecucion;
            while (Time.time < fin)
            {
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    if (p.Estado == EstadoPandillero.Persiguiendo) persiguieron = true;
                }
                yield return Cuadro();
            }
            Release(teclado.wKey);
            Assert.IsTrue(persiguieron, "al caminar hacia ellos, persiguen");
            Assert.Less(DistanciaMinima(), distanciaInicial, "se acercaron al jugador");

            // Quedarse quieto y medir.
            var estados = new Dictionary<PandilleroAPie, EstadoPandillero>();
            var impactos = new Dictionary<PandilleroAPie, List<float>>();
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                estados[p] = p.Estado;
                impactos[p] = new List<float>();
            }
            vidaJugador.Restaurar();
            atajos.Add("VidaAPie.Restaurar antes de medir");
            float vidaInicial = vidaJugador.Actual;
            int total = 0;
            int maxAtacando = 0;
            int maxPreparando = 0;
            float inicioMedida = -1f;
            float limite = Time.time + MaxSegundosFuga;
            while (Time.time < limite && (inicioMedida < 0f || Time.time - inicioMedida < SegundosDeRitmo))
            {
                int atacando = 0;
                int preparando = 0;
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    EstadoPandillero ahora = p.Estado;
                    if (estados[p] == EstadoPandillero.Preparando && ahora == EstadoPandillero.Recargando)
                    {
                        impactos[p].Add(Time.time);
                        total++;
                        if (inicioMedida < 0f) inicioMedida = Time.time;
                    }
                    estados[p] = ahora;
                    if (ahora == EstadoPandillero.Preparando || ahora == EstadoPandillero.Recargando) atacando++;
                    if (ahora == EstadoPandillero.Preparando) preparando++;
                }
                maxAtacando = Mathf.Max(maxAtacando, atacando);
                maxPreparando = Mathf.Max(maxPreparando, preparando);
                yield return Cuadro();
            }
            float perdida = vidaInicial - vidaJugador.Actual;
            float ciclo = datos.PreparacionGolpe + datos.RecargaGolpe;
            float menorSeparacion = float.MaxValue;
            int conVarios = 0;
            foreach (var par in impactos)
            {
                if (par.Value.Count >= 2) conVarios++;
                for (int i = 1; i < par.Value.Count; i++) menorSeparacion = Mathf.Min(menorSeparacion, par.Value[i] - par.Value[i - 1]);
            }
            medidas.Add("impactos=" + total + " perdida=" + perdida.ToString("0.#", CultureInfo.InvariantCulture)
                + " danio/golpe=" + datos.DanioGolpe + " ciclo=" + ciclo + " menorSeparacion=" + menorSeparacion.ToString("0.00", CultureInfo.InvariantCulture)
                + " maxAtacando=" + maxAtacando + " maxPreparando=" + maxPreparando);
            Debug.Log("[T-195] " + string.Join(" | ", medidas));

            Assert.GreaterOrEqual(total, 3, "pegaron varias veces");
            Assert.AreEqual(total * datos.DanioGolpe, perdida, 0.01f, "cada golpe quita el daño de los datos");
            Assert.LessOrEqual(maxAtacando, datos.MaxAtacando, "nunca más de 2 atacando a la vez");
            Assert.LessOrEqual(maxPreparando, MaxSimultaneosPreparando);
            Assert.GreaterOrEqual(conVarios, 1, "al menos uno golpeó dos veces");
            Assert.GreaterOrEqual(menorSeparacion, ciclo - ToleranciaGolpePasado, "el mismo pandillero respeta aviso + recarga");
            VerificarFinal("Pelea");
        }

        private float DistanciaMinima()
        {
            float mejor = float.MaxValue;
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                if (p.gameObject.activeSelf) mejor = Mathf.Min(mejor, Distancia(p.transform.position, jugador.transform.position));
            }
            return mejor;
        }

        // ---- c) Golpes y tiros para tumbar a uno ----

        private IEnumerator TumbarConPunos(string[] implantesPorCodigo, float multiplicadorEsperado)
        {
            yield return CargarNoche();
            if (implantesPorCodigo.Length > 0) CargarImplantes(implantesPorCodigo);
            Assert.AreEqual(multiplicadorEsperado, implantes.Efectos.multiplicadorGolpeAPie, 0.0001f, "efecto de los brazos");
            Colocar(InicioCerca, 0f);
            PandilleroAPie p = DejarUno();
            yield return Cuadros(FramesParaElHud);
            float danio = datosPersonaje.DanioGolpe * implantes.Efectos.multiplicadorGolpeAPie;
            int esperados = Necesarios(datos.VidaMaxima, danio);
            yield return Vencer(p, false);
            medidas.Add("puños mult=" + multiplicadorEsperado.ToString(CultureInfo.InvariantCulture) + " danio=" + danio.ToString("0.#", CultureInfo.InvariantCulture)
                + " vida=" + datos.VidaMaxima + " golpesEsperados=" + esperados + " golpesDados=" + golpesDados);
            Debug.Log("[T-195] " + string.Join(" | ", medidas));
            Assert.AreEqual(esperados, golpesDados, "golpes para tumbar a uno, calculados desde los assets");
            VerificarFinal("Tumbar puños");
        }

        [UnityTest]
        public IEnumerator Tumbar_SinImplante_Cuatro_Golpes()
        {
            yield return TumbarConPunos(new string[0], 1f);
            Assert.AreEqual(4, golpesDados, "los datos de D-tony-169: 4 golpes sin implante");
        }

        [UnityTest]
        public IEnumerator Tumbar_ConBrazos1_Tres_Golpes()
        {
            yield return TumbarConPunos(new[] { IdBrazos1 }, Datos(EspacioImplante.Brazos, 1).Efectos.multiplicadorGolpeAPie);
            Assert.AreEqual(3, golpesDados, "BRAZOS I: 3 golpes");
        }

        [UnityTest]
        public IEnumerator Tumbar_ConBrazos2_Dos_Golpes()
        {
            yield return TumbarConPunos(new[] { IdBrazos2 }, Datos(EspacioImplante.Brazos, 2).Efectos.multiplicadorGolpeAPie);
            Assert.AreEqual(2, golpesDados, "BRAZOS II: 2 golpes");
        }

        [UnityTest]
        public IEnumerator Tumbar_ConPistola_Tres_Tiros()
        {
            yield return CargarNoche();
            Colocar(InicioCerca, 0f);
            PandilleroAPie p = DejarUno();
            yield return Cuadros(FramesParaElHud);
            yield return CambiarAPistola();
            int esperados = Necesarios(datos.VidaMaxima, datosPistola.Danio);
            yield return Vencer(p, true);
            medidas.Add("pistola danio=" + datosPistola.Danio + " vida=" + datos.VidaMaxima + " tirosEsperados=" + esperados + " tirosDados=" + golpesDados);
            Debug.Log("[T-195] " + string.Join(" | ", medidas));
            Assert.AreEqual(esperados, golpesDados, "tiros para tumbar a uno, calculados desde los assets");
            Assert.AreEqual(3, golpesDados, "los datos de D-tony-169: 3 tiros");
            VerificarFinal("Tumbar pistola");
        }

        // ---- d) Correr lejos: se rinden y vuelven; con PIERNAS I se sale antes de su radio ----

        // Corre al oeste (Shift + W) desde el inicio. Devuelve (segundos para salir de su radio, segundos hasta pasar la correa).
        private float segundosSalirDelRadio;
        private bool vencieronLaCorrea;

        private IEnumerator CorrerLejos()
        {
            segundosSalirDelRadio = -1f;
            vencieronLaCorrea = false;
            Colocar(InicioFuga, 270f);
            yield return Cuadros(FramesParaElHud);
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            float inicio = Time.time;
            float fin = inicio + MaxSegundosFuga;
            while (Time.time < fin)
            {
                MantenerVivo();
                yield return Cuadro();
                if (segundosSalirDelRadio < 0f && DistanciaMinima() > datos.RadioDeteccion) segundosSalirDelRadio = Time.time - inicio;
                if (Distancia(jugador.transform.position, SitioPandilla) > datos.Correa + DistanciaMetaFuga)
                {
                    vencieronLaCorrea = true;
                    break;
                }
            }
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            yield return Cuadro();
        }

        [UnityTest]
        public IEnumerator Fuga_CorrerMasAllaDeLaCorrea_VuelvenAlPunto_YConPiernasSeSaleAntesDeSuRadio()
        {
            yield return CargarNoche();
            yield return CorrerLejos();
            float sinImplante = segundosSalirDelRadio;
            Assert.IsTrue(vencieronLaCorrea, "el jugador pasó la correa");
            Assert.Greater(sinImplante, 0f, "salió de su radio");
            // Los pandilleros se rinden y vuelven a su punto.
            float fin = Time.time + MaxSegundosRegreso;
            bool todosEnSuPunto = false;
            while (Time.time < fin && !todosEnSuPunto)
            {
                MantenerVivo();
                yield return Cuadro();
                todosEnSuPunto = true;
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    if (p.Estado != EstadoPandillero.Esperando || Distancia(p.transform.position, p.Hogar) > MargenRegreso) todosEnSuPunto = false;
                }
            }
            Assert.IsTrue(todosEnSuPunto, "tras la correa, todos vuelven a su punto y esperan");

            // Mismo recorrido con PIERNAS I (escena nueva).
            yield return CargarNoche();
            CargarImplantes(IdPiernas1);
            yield return CorrerLejos();
            float conPiernas = segundosSalirDelRadio;
            medidas.Add("salirDelRadio sin=" + sinImplante.ToString("0.00", CultureInfo.InvariantCulture)
                + " s, conPiernas1=" + conPiernas.ToString("0.00", CultureInfo.InvariantCulture)
                + " s, correr x" + implantes.Efectos.multiplicadorCorrer.ToString(CultureInfo.InvariantCulture));
            Debug.Log("[T-195] " + string.Join(" | ", medidas));
            Assert.Greater(conPiernas, 0f, "con PIERNAS también salió de su radio");
            Assert.Less(conPiernas, sinImplante, "con PIERNAS I se sale antes de su radio");
            VerificarFinal("Fuga");
        }

        // ---- e) OJOS en el HUD a pie ----

        [UnityTest]
        public IEnumerator Ojos_MuestranLaBarraYElAvisoDeGolpe_YSinOjosNoMuestranNada()
        {
            yield return CargarNoche();
            Colocar(InicioCerca, 0f);
            // Se retira al pandillero del norte: quedan dos frente al jugador.
            PandilleroAPie retirado = null;
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                if (retirado == null || p.Hogar.z > retirado.Hogar.z) retirado = p;
            }
            retirado.gameObject.SetActive(false);
            atajos.Add("Retirado el pandillero del norte");
            PandilleroAPie objetivo = null;
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                if (p != retirado && (objetivo == null || p.Hogar.x > objetivo.Hogar.x)) objetivo = p;
            }
            MechaCyberpunk.UI.ConfigHudAPie configHud = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.UI.ConfigHudAPie>("Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset");
            Assert.IsNotNull(configHud);
            Assert.IsNotNull(hud.GrupoVidaRival, "el HUD trae la barra de OJOS");
            Assert.IsNotNull(hud.GrupoGolpeRival, "el HUD trae el aviso de golpe");
            yield return Cuadros(FramesParaElHud);

            // Sin OJOS: nada, mirando al pandillero.
            int frames = 0;
            float fin = Time.time + 1.5f;
            while (Time.time < fin)
            {
                MantenerVivo();
                PasoDeMira(objetivo.transform.position + Vector3.up * AlturaCentro);
                yield return Cuadro();
                frames++;
                Assert.IsFalse(hud.GrupoVidaRival.activeSelf, "sin OJOS no hay barra");
                Assert.IsFalse(hud.GrupoGolpeRival.activeSelf, "sin OJOS no hay aviso de golpe");
            }
            Assert.Greater(frames, 10);

            // Un tiro real para que la barra no esté llena.
            yield return CambiarAPistola();
            Vector3 centro = objetivo.transform.position + Vector3.up * AlturaCentro;
            yield return Apuntar(centro);
            float antes = objetivo.Actual;
            for (int intento = 0; intento < 20 && objetivo.Actual >= antes; intento++)
            {
                PasoDeMira(objetivo.transform.position + Vector3.up * AlturaCentro);
                Press(raton.leftButton);
                yield return Cuadro();
                Release(raton.leftButton);
                yield return Cuadro();
                MantenerVivo();
            }
            Assert.AreEqual(antes - datosPistola.Danio, objetivo.Actual, 0.001f, "un tiro real le quitó el daño de la pistola");

            // Con OJOS I.
            CargarImplantes(IdOjos1);
            float anticipacion = implantes.Efectos.anticipacionAtaque;
            Assert.Greater(anticipacion, 0f);
            int avisosAntes = 0;
            int framesPreparando = 0;
            int preparandoSinAviso = 0;
            int barrasMalas = 0;
            int framesConBarra = 0;
            bool capturado = false;
            float inicioPreparando = -1f;
            fin = Time.time + 12f;
            while (Time.time < fin)
            {
                MantenerVivo();
                // Se mira un poco a la derecha del cuerpo (sigue dentro de la mira del HUD) para que el brazo del aviso quede en cuadro.
                Vector3 puntoMira = objetivo.transform.position + Vector3.up * AlturaCentro - controlador.Camara.right * DesvioParaVerElBrazo;
                PasoDeMira(puntoMira);
                yield return Cuadro();
                if (objetivo.EstaKO) break;
                bool avisoVisible = hud.GrupoGolpeRival.activeSelf;
                bool preparando = objetivo.Estado == EstadoPandillero.Preparando;
                if (hud.GrupoVidaRival.activeSelf)
                {
                    framesConBarra++;
                    float esperado = objetivo.Actual / objetivo.Maxima;
                    if (Mathf.Abs(hud.RellenoVidaRival.fillAmount - esperado) > 0.01f) barrasMalas++;
                    Assert.IsFalse(hud.TextoVidaRival.gameObject.activeSelf, "OJOS I no muestra números");
                }
                inicioPreparando = preparando ? (inicioPreparando < 0f ? Time.time : inicioPreparando) : -1f;
                if (preparando)
                {
                    framesPreparando++;
                    if (!avisoVisible) preparandoSinAviso++;
                }
                else if (avisoVisible)
                {
                    avisosAntes++;
                }
                if (!capturado && preparando && Time.time - inicioPreparando >= SegundosParaCapturar && avisoVisible && hud.GrupoVidaRival.activeSelf && Mirando(puntoMira))
                {
                    capturado = true;
                    CapturarConHud(RutaCaptura);
                }
            }
            medidas.Add("ojos1: framesConBarra=" + framesConBarra + " barrasMalas=" + barrasMalas + " framesPreparando=" + framesPreparando
                + " preparandoSinAviso=" + preparandoSinAviso + " avisosAntesDePreparar=" + avisosAntes + " anticipacion=" + anticipacion);
            Debug.Log("[T-195] " + string.Join(" | ", medidas));
            Assert.Greater(framesConBarra, 30, "la barra se ve mirando al pandillero");
            Assert.AreEqual(0, barrasMalas, "la barra coincide con su vida (1 %)");
            Assert.Greater(framesPreparando, 0, "el pandillero preparó algún golpe");
            Assert.LessOrEqual(preparandoSinAviso, framesPreparando * 0.02f, "el aviso ya estaba (a lo más un cuadro de retraso por golpe)");
            Assert.Greater(avisosAntes, 0, "el aviso salió antes de la preparación");

            // Con OJOS II: números.
            CargarImplantes(IdOjos2);
            yield return Cuadros(FramesParaElHud);
            PasoDeMira(objetivo.transform.position + Vector3.up * AlturaCentro);
            yield return Cuadro();
            Assert.IsTrue(hud.GrupoVidaRival.activeSelf);
            Assert.IsTrue(hud.TextoVidaRival.gameObject.activeSelf, "OJOS II muestra los números");
            Assert.AreEqual(string.Format(configHud.FormatoVidaRival, objetivo.Actual, objetivo.Maxima), hud.TextoVidaRival.text);

            // Lejos de la mira o sin mirar: nada.
            Set(raton.delta, new Vector2(1800f, 0f));
            yield return Cuadro();
            Set(raton.delta, Vector2.zero);
            yield return Cuadro();
            Assert.IsFalse(hud.GrupoVidaRival.activeSelf, "si no lo miras no hay barra");
            VerificarFinal("Ojos");
        }

        // ---- f) Vencer a los 3 paga; el día siguiente no están; la noche siguiente vuelven ----

        [UnityTest]
        public IEnumerator Dinero_VencerALosTres_Paga_YVuelvenLaNocheSiguiente()
        {
            yield return CargarNoche();
            CargarImplantes(IdBrazos2);
            Colocar(InicioCerca, 0f);
            yield return Cuadros(FramesParaElHud);
            int dineroInicial = cartera.Dinero;
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
                yield return Vencer(siguiente, false);
                ronda++;
            }
            yield return Esperar(datos.SegundosParaDesaparecer + 1f);
            Assert.AreEqual(0, pandilla.Presentes, "vencidos los 3 no queda nadie en pie");
            int ganado = cartera.Dinero - dineroInicial;
            medidas.Add("dinero ganado=" + ganado + " esperado=" + 3 * datos.Dinero + " (3 x " + datos.Dinero + ")");
            Debug.Log("[T-195] " + string.Join(" | ", medidas));
            Assert.AreEqual(3 * datos.Dinero, ganado, "vencer a los 3 paga 3 veces su dinero");

            int activos = 0;
            foreach (PandilleroAPie p in pandilla.Pandilleros) activos += p.gameObject.activeSelf ? 1 : 0;
            Assert.AreEqual(0, activos, "los vencidos desaparecen y esa noche no vuelven");

            reloj.AvanzarHasta(HoraDia);
            atajos.Add("Reloj avanzado a las 8:00 del día siguiente");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(0, pandilla.Presentes, "al día siguiente no están");
            reloj.AvanzarHasta(HoraNoche);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(datos.TamanoPandilla, pandilla.Presentes, "la noche siguiente vuelven los 3");
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                Assert.AreEqual(datos.VidaMaxima, p.Actual, 0.001f, "con la vida llena");
                Assert.IsFalse(p.EstaKO);
            }
            VerificarFinal("Dinero");
        }

        // ---- g) Subir al mecha a su lado: se dispersan y no pegan ----

        [UnityTest]
        public IEnumerator Mecha_SubirAlMechaASuLado_SeDispersanYNoPegan()
        {
            yield return CargarNoche();
            MontajeMecha mecha = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(mecha, "la escena trae el mecha");
            Colocar(InicioCerca, 0f);
            // Atajo: el mecha se mueve junto al jugador (en la escena queda en el taller).
            Vector3 desplazamiento = mecha.PuntoSubida - mecha.transform.position;
            Vector3 destino = jugador.transform.position + Vector3.forward * DistanciaMecha - desplazamiento;
            destino.y = mecha.transform.position.y;
            CharacterController ccMecha = mecha.GetComponent<CharacterController>();
            if (ccMecha != null) ccMecha.enabled = false;
            mecha.transform.position = destino;
            if (ccMecha != null) ccMecha.enabled = true;
            Physics.SyncTransforms();
            atajos.Add("Mecha movido junto al jugador");
            yield return Cuadros(FramesParaElHud);

            // Ya persiguen: espera a que alguno esté cerca antes de subir.
            float fin = Time.time + 5f;
            while (Time.time < fin && DistanciaMinima() > datos.AlcanceGolpe * 2f)
            {
                yield return Cuadro();
            }
            Assert.Less(DistanciaMinima(), datos.AlcanceGolpe * 2f + 0.5f, "la pandilla ya está encima del jugador");
            float esperaE = Time.time + 3f;
            while (hud.TextoAvisoInteraccion.text.Length == 0 && Time.time < esperaE) yield return Cuadro();
            yield return Tecla(teclado.eKey);
            float finSubir = Time.time + 5f;
            while (jugador.Modo != ModoJugador.Mecha && Time.time < finSubir)
            {
                MantenerVivo();
                yield return Cuadro();
            }
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "E sube al mecha: " + hud.TextoAvisoInteraccion.text);

            float vidaAlSubir = vidaJugador.Actual;
            bool alguienPreparando = false;
            float finMecha = Time.time + SegundosMecha;
            while (Time.time < finMecha)
            {
                yield return Cuadro();
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    if (Time.time > finMecha - SegundosMecha + 1.5f && (p.Estado == EstadoPandillero.Preparando || p.Estado == EstadoPandillero.Persiguiendo)) alguienPreparando = true;
                }
            }
            Assert.IsFalse(alguienPreparando, "tras subir al mecha nadie persigue ni prepara golpe");
            Assert.AreEqual(vidaAlSubir, vidaJugador.Actual, 0.001f, "no pegan al jugador en el mecha");
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                Assert.AreEqual(EstadoPandillero.Esperando, p.Estado, "se dispersaron y esperan en su punto");
                Assert.Less(Distancia(p.transform.position, p.Hogar), MargenRegreso + 0.5f);
            }
            VerificarFinal("Mecha");
        }
    }
}
