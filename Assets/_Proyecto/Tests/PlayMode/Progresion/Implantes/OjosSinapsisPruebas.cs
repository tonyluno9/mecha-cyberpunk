using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-194: OJOS (pantalla de la cabina con la vida y el aviso del próximo ataque del rival) y SISTEMA NERVIOSO (golpe y guardia más
    /// rápidos) en peleas reales del flujo desde el mostrador de Pruebas_Implantes. Entrada simulada: Enter y clics. Atajos permitidos y
    /// anotados: implantes por código, <c>ServicioLigas.Poner</c>, <c>Retar</c> del mostrador y vida del jugador fijada para que el rival
    /// no lo deje en KO mientras se mide. El rival nunca recibe daño que no sea de golpes reales del jugador.
    /// </summary>
    public class OjosSinapsisPruebas : BaseImplantesPruebas
    {
        private const string IdOjos1 = "implante_ojos_1";
        private const string IdOjos2 = "implante_ojos_2";
        private const string IdSinapsis1 = "implante_sistemanervioso_1";
        private const string IdSinapsis2 = "implante_sistemanervioso_2";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasJefe = "DON FIERRO";
        private const string RutaCaptura = "docs/capturas/T-194-1.png";
        private const float MaxSegundosEstado = 90f;
        private const float SegundosDeMedicion = 30f;
        private const float MaxSegundosJefe = 80f;
        private const float MaxSegundosGolpesReales = 20f;
        private const float SegundosEntreClics = 0.3f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaSegura = 0.6f;
        private const float ToleranciaBarra = 0.01f;
        private const float FraccionMinimaAvisada = 0.8f;
        private const float ToleranciaTiempo = 0.002f;
        private const float EsperaAntesDeMedir = 0.6f;
        private const int MinimoGolpesMedidos = 3;
        private const int MuestrasPorEscala = 3;
        private const int DistanciaAlMostradorPeleas = 2;

        private MostradorPeleas mostradorPeleas;
        private DirectorPelea director;
        private MontajeMecha montaje;
        private VidaMecha vidaMecha;
        private PantallaOjosCabina ojos;
        private LedSinapsis led;
        private GolpeMecha golpe;
        private GuardiaMecha guardia;
        private EncendidoMecha encendido;
        private RivalBasico rival;
        private HabilidadRival habilidad;
        private ConfigPantallaOjos configOjos;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // ---- Preparación ----

        private IEnumerator IniciarPelea(int numeroRival, string alias)
        {
            yield return CargarEscena();
            mostradorPeleas = Object.FindFirstObjectByType<MostradorPeleas>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(mostradorPeleas, "la escena trae el mostrador de peleas");
            Assert.IsNotNull(montaje, "la escena trae el mecha de la historia");
            director = mostradorPeleas.GetComponentInChildren<DirectorPelea>(true);
            PantallaApuesta apuesta = mostradorPeleas.GetComponentInChildren<PantallaApuesta>(true);
            vidaMecha = montaje.GetComponent<VidaMecha>();
            ojos = montaje.GetComponentInChildren<PantallaOjosCabina>(true);
            led = montaje.GetComponentInChildren<LedSinapsis>(true);
            golpe = montaje.GetComponent<GolpeMecha>();
            guardia = montaje.GetComponent<GuardiaMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            configOjos = UnityEditor.AssetDatabase.LoadAssetAtPath<ConfigPantallaOjos>("Assets/_Proyecto/UI/Datos/ConfigPantallaOjos_Base.asset");
            Assert.IsNotNull(ojos, "MechaEstacionado trae la pantalla de ojos");
            Assert.IsNotNull(led, "MechaEstacionado trae el LED de sinapsis");
            Assert.IsNotNull(configOjos, "existe ConfigPantallaOjos_Base");

            reloj.AvanzarHasta(HoraNoche);
            atajos.Add("Reloj avanzado a la noche (las peleas de liga abren de noche)");
            Assert.IsTrue(ligas.Poner(1, numeroRival, 0), "se pudo fijar el progreso en la liga 1, rival " + numeroRival);
            atajos.Add("Progreso fijado: liga 1, rival " + numeroRival);
            Vector3 posicion = mostradorPeleas.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostradorPeleas);
            jugador.Colocar(new Pose(posicion, Quaternion.identity));
            yield return Cuadros(FramesParaElHud);

            OpcionCartelera opcion = default;
            bool hallada = false;
            foreach (OpcionCartelera o in mostradorPeleas.Opciones)
            {
                if (o.Tipo == TipoOpcionCartelera.Siguiente && o.Liga == 1 && o.Indice == numeroRival)
                {
                    opcion = o;
                    hallada = true;
                }
            }
            Assert.IsTrue(hallada, "el mostrador ofrece liga 1 rival " + numeroRival);
            Assert.AreEqual(alias, mostradorPeleas.AliasDe(opcion), "el rival del cartel");
            Assert.IsTrue(mostradorPeleas.Retar(opcion), "Retar empieza: " + mostradorPeleas.Texto);
            atajos.Add("Retar() del mostrador");
            Assert.IsTrue(apuesta.Visible, "se ve la pantalla de apuesta");
            yield return Tecla(teclado.enterKey);
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.AreEqual(alias, director.Rival.Nombre, "el rival es el del cartel");
            rival = ((Component)director.Rival).GetComponent<RivalBasico>();
            habilidad = ((Component)director.Rival).GetComponent<HabilidadRival>();
            Assert.IsNotNull(rival, "el rival es un RivalBasico");
            IPeleador propio = montaje.GetComponent<IPeleador>();
            yield return EsperarHasta(() => propio.ControlActivo, MaxSegundosEstado, "el jugador tiene el control del mecha");
            yield return Cuadros(FramesParaElHud);
            framesSinMedir = FramesSinMedirTrasEscena;
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "se esperaba: " + que);
        }

        private IEnumerator FinDeCuadro()
        {
            yield return null;
            Vigilar();
        }

        // Clics reales hasta que el rival pierda la fracción de vida buscada.
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
                yield return FinDeCuadro();
            }
            Release(raton.leftButton);
            yield return FinDeCuadro();
            Assert.Less(vidaRival.Actual, vidaRival.Maxima, "el jugador le hizo daño real al rival");
            Assert.IsFalse(vidaRival.EstaKO, "el rival sigue en pie");
            yield return Cuadros(FramesParaElHud);
        }

        private void MantenerVivo()
        {
            if (vidaMecha.Actual < vidaMecha.Maxima * FraccionVidaSegura)
            {
                vidaMecha.FijarVida(vidaMecha.Maxima);
            }
        }

        private static string Fmt(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

        // Mide, durante SegundosDeMedicion, cuánto antes se prendió el aviso de cada golpe del rival.
        private IEnumerator MedirAvisoDeGolpes(float anticipacion, List<float> adelantos, List<string> aparte, List<string> fallos)
        {
            bool avisoAntes = false;
            float litDesde = -1f;
            float tiempoAnterior = Time.time;
            float ultimoDesconocido = Time.time;
            float fin = Time.time + SegundosDeMedicion;
            float frameMax = 0f;
            while (Time.time < fin)
            {
                yield return FinDeCuadro();
                MantenerVivo();
                frameMax = Mathf.Max(frameMax, Time.deltaTime);
                float segundos = ((ILecturaRival)rival).SegundosParaAtacar;
                if (!rival.EnAviso && segundos < 0f) ultimoDesconocido = Time.time;
                if (ojos.AvisoPrendido)
                {
                    // La pantalla se refresca en LateUpdate, después de este cuadro: lo que se ve es del cuadro anterior.
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
                    string descripcion = "adelanto " + Fmt(adelanto) + " s (desconocido hace " + Fmt(tarde) + " s)";
                    if (adelanto >= anticipacion - Time.deltaTime - ToleranciaTiempo)
                    {
                        adelantos.Add(adelanto);
                    }
                    else if (tarde < anticipacion + Time.deltaTime)
                    {
                        aparte.Add(descripcion);
                    }
                    else
                    {
                        fallos.Add(descripcion);
                    }
                }
                avisoAntes = enAviso;
            }
        }

        private void VerificarAnticipacion(string nombre, float anticipacion, List<float> adelantos, List<string> aparte, List<string> fallos)
        {
            int total = adelantos.Count + aparte.Count + fallos.Count;
            string detalle = nombre + ": anticipación " + Fmt(anticipacion) + " s; golpes " + total + "; a tiempo " + adelantos.Count
                + " [" + string.Join(", ", adelantos.ConvertAll(Fmt)) + "]; llegaron tarde (aparte) " + aparte.Count
                + " [" + string.Join(" | ", aparte) + "]; sin explicar " + fallos.Count + " [" + string.Join(" | ", fallos) + "]";
            Debug.Log("[T-194] " + detalle);
            Assert.GreaterOrEqual(total, MinimoGolpesMedidos, "hubo golpes del rival que medir. " + detalle);
            Assert.GreaterOrEqual(adelantos.Count, Mathf.CeilToInt(total * FraccionMinimaAvisada), "al menos el 80 % de los golpes avisados a tiempo. " + detalle);
        }

        // ---- a) sin OJOS ----

        [UnityTest]
        public IEnumerator SinOjos_LaPantallaEstaApagadaAunConSinapsis()
        {
            yield return IniciarPelea(1, AliasPrimero);
            for (int i = 0; i < FramesEstables; i++) yield return FinDeCuadro();
            Assert.IsFalse(ojos.Visible, "sin implantes la pantalla de ojos está apagada");

            CargarImplantes(IdSinapsis2);
            for (int i = 0; i < FramesEstables; i++) yield return FinDeCuadro();
            Assert.IsFalse(ojos.Visible, "con SINAPSIS pero sin OJOS sigue apagada");
            Assert.AreEqual(2, led.Prendidos, "el LED de sinapsis sí se prende");
            VerificarFinal("sin-ojos");
        }

        // ---- b) OJOS I ----

        [UnityTest]
        public IEnumerator OjosI_BarraCoincideConLaVidaYAvisaCadaGolpe()
        {
            yield return IniciarPelea(1, AliasPrimero);
            CargarImplantes(IdOjos1);
            float anticipacion = Datos(EspacioImplante.Ojos, 1).Efectos.anticipacionAtaque;
            yield return FinDeCuadro();
            Assert.IsTrue(encendido.Paso >= PasoEncendido.Paso2, "la cabina está encendida (" + encendido.Paso + ")");
            Assert.IsTrue(ojos.Visible, "con OJOS la pantalla se prende");
            Assert.AreEqual(AliasPrimero, ojos.TextoNombre, "muestra el nombre del rival");
            Assert.AreEqual(configOjos.ColorBarra, ojos.ColorBarra, "la barra es turquesa");

            yield return GolpesReales();
            yield return FinDeCuadro();
            IVida vidaRival = director.Rival.Vida;
            Assert.AreEqual(vidaRival.Actual / vidaRival.Maxima, ojos.Barra, ToleranciaBarra, "la barra coincide con la vida del rival");
            Assert.IsEmpty(ojos.TextoNumeros, "OJOS I no muestra números");

            var adelantos = new List<float>();
            var aparte = new List<string>();
            var fallos = new List<string>();
            yield return MedirAvisoDeGolpes(anticipacion, adelantos, aparte, fallos);
            VerificarAnticipacion("OJOS I", anticipacion, adelantos, aparte, fallos);
            VerificarFinal("ojos-1");
        }

        // ---- c) OJOS II ----

        [UnityTest]
        public IEnumerator OjosII_MuestraNumerosYAvisaConSuAnticipacion()
        {
            yield return IniciarPelea(1, AliasPrimero);
            CargarImplantes(IdOjos2);
            float anticipacion = Datos(EspacioImplante.Ojos, 2).Efectos.anticipacionAtaque;
            yield return FinDeCuadro();
            Assert.IsTrue(ojos.Visible, "con OJOS II la pantalla se prende");

            yield return GolpesReales();
            yield return FinDeCuadro();
            IVida vidaRival = director.Rival.Vida;
            Assert.AreEqual(vidaRival.Actual / vidaRival.Maxima, ojos.Barra, ToleranciaBarra, "la barra coincide con la vida del rival");
            string esperado = Mathf.CeilToInt(vidaRival.Actual).ToString(CultureInfo.InvariantCulture) + " / "
                + Mathf.CeilToInt(vidaRival.Maxima).ToString(CultureInfo.InvariantCulture);
            Assert.AreEqual(esperado, ojos.TextoNumeros, "la vida en números");

            var adelantos = new List<float>();
            var aparte = new List<string>();
            var fallos = new List<string>();
            yield return MedirAvisoDeGolpes(anticipacion, adelantos, aparte, fallos);
            VerificarAnticipacion("OJOS II", anticipacion, adelantos, aparte, fallos);
            VerificarFinal("ojos-2");
        }

        // ---- d) habilidad del jefe ----

        [UnityTest]
        public IEnumerator OjosII_ContraElJefe_AvisaLaHabilidadAntesDelAviso()
        {
            yield return IniciarPelea(5, AliasJefe);
            Assert.IsNotNull(habilidad, "DON FIERRO trae su habilidad");
            CargarImplantes(IdOjos2);
            float anticipacion = Datos(EspacioImplante.Ojos, 2).Efectos.anticipacionAtaque;
            yield return FinDeCuadro();
            Assert.IsTrue(ojos.Visible, "con OJOS II la pantalla se prende");

            int avisos = 0;
            bool pendiente = false;
            habilidad.AvisoIniciado += () => pendiente = true;
            var resultados = new List<string>();
            int buenos = 0;
            int solapados = 0;
            float litHabilidadDesde = -1f;
            float tiempoPrevio = Time.time;
            float ultimoGolpeEnAviso = -100f;
            float fin = Time.time + MaxSegundosJefe;
            while (Time.time < fin && avisos < 2)
            {
                yield return FinDeCuadro();
                MantenerVivo();
                if (rival.EnAviso) ultimoGolpeEnAviso = Time.time;
                bool etiquetaHabilidad = ojos.AvisoPrendido && ojos.TextoAviso == configOjos.TextoProximoHabilidad;
                if (etiquetaHabilidad)
                {
                    if (litHabilidadDesde < 0f) litHabilidadDesde = tiempoPrevio;
                }
                else if (!habilidad.EnAviso)
                {
                    litHabilidadDesde = -1f;
                }
                tiempoPrevio = Time.time;
                if (!pendiente) continue;
                pendiente = false;
                avisos++;
                float adelanto = litHabilidadDesde >= 0f ? Time.time - litHabilidadDesde : 0f;
                bool despuesDeGolpe = Time.time - ultimoGolpeEnAviso < anticipacion + Time.deltaTime;
                string texto = "adelanto " + Fmt(adelanto) + " s" + (despuesDeGolpe ? " (pegado a un golpe)" : string.Empty);
                resultados.Add(texto);
                if (adelanto >= anticipacion - Time.deltaTime - ToleranciaTiempo) buenos++;
                else if (despuesDeGolpe) solapados++;
                else Assert.Fail("la habilidad no se avisó a tiempo: " + texto);
            }
            Debug.Log("[T-194] DON FIERRO: anticipación " + Fmt(anticipacion) + " s; avisos de habilidad " + avisos + ": " + string.Join(" | ", resultados));
            Assert.GreaterOrEqual(avisos, 1, "el jefe lanzó al menos una habilidad en " + Fmt(MaxSegundosJefe) + " s");
            Assert.GreaterOrEqual(buenos, 1, "al menos un aviso de habilidad llegó a tiempo: " + string.Join(" | ", resultados));
            VerificarFinal("jefe");
        }

        // ---- e) SINAPSIS ----

        private IEnumerator MedirGolpe(MuestraReaccion muestra)
        {
            MantenerVivo();
            float impacto = -1f;
            Action alImpactar = () => impacto = Time.time;
            golpe.Impacto += alImpactar;
            yield return FinDeCuadro();
            float t0 = -1f;
            float frameMax = 0f;
            float fin = Time.time + MaxSegundosGolpesReales;
            bool clic = false;
            float proximo = 0f;
            // Clics como los de GolpesReales; se mide desde el clic que arranca el golpe (el que encuentra el golpe en Listo).
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
                yield return FinDeCuadro();
                frameMax = Mathf.Max(frameMax, Time.deltaTime);
            }
            Release(raton.leftButton);
            golpe.Impacto -= alImpactar;
            Assert.GreaterOrEqual(impacto, 0f, "el golpe del jugador llegó al impacto (fase " + golpe.Fase + ", control " + montaje.GetComponent<IPeleador>().ControlActivo
                + ", guardia " + guardia.Progreso + ", vida " + vidaMecha.Actual + ", golpe habilitado " + golpe.enabled + ", estado " + director.Estado + ")");
            muestra.golpe = impacto - t0;
            muestra.frameGolpe = frameMax;
            // Espera a que pase la recarga.
            yield return EsperarHasta(() => golpe.Fase == FaseGolpe.Listo, MaxSegundosEstado, "el golpe vuelve a Listo");
        }

        private IEnumerator MedirGuardia(MuestraReaccion muestra)
        {
            yield return EsperarHasta(() => !guardia.Levantada, MaxSegundosEstado, "la guardia abajo");
            yield return FinDeCuadro();
            float t0 = Time.time;
            float frameMax = 0f;
            Press(raton.rightButton);
            float fin = t0 + 2f;
            while (!guardia.EstaArriba && Time.time < fin)
            {
                yield return FinDeCuadro();
                frameMax = Mathf.Max(frameMax, Time.deltaTime);
            }
            Assert.IsTrue(guardia.EstaArriba, "la guardia llegó arriba");
            muestra.guardia = Time.time - guardia.SegundosArriba - t0;
            muestra.frameGuardia = frameMax;
            Release(raton.rightButton);
            yield return FinDeCuadro();
        }

        private sealed class MuestraReaccion
        {
            public float golpe;
            public float guardia;
            public float frameGolpe;
            public float frameGuardia;
        }

        private IEnumerator MedirEscala(string nombre, ValoresMecha basales, float multiplicador)
        {
            ValoresMecha esperado = ReglaReaccionImplantes.Aplicar(basales, multiplicador);
            DatosMecha datos = vidaMecha.Datos;
            Assert.AreEqual(basales.vidaMaxima, datos.VidaMaxima, 0.0001f, nombre + ": vida máxima igual a sin implante");
            Assert.AreEqual(basales.danioGolpe, datos.DanioGolpe, 0.0001f, nombre + ": daño igual a sin implante");
            Assert.AreEqual(basales.velocidadMaxima, datos.VelocidadMaxima, 0.0001f, nombre + ": velocidad igual a sin implante");
            Assert.AreEqual(esperado.preparacionGolpe, datos.PreparacionGolpe, 0.0001f, nombre + ": preparación de los datos");
            Assert.AreEqual(esperado.tiempoSubirGuardia, datos.TiempoSubirGuardia, 0.0001f, nombre + ": guardia de los datos");
            Assert.AreEqual(esperado.ventanaBloqueoPerfecto, datos.VentanaBloqueoPerfecto, 0.0001f, nombre + ": ventana de los datos");

            var golpes = new List<float>();
            var guardias = new List<float>();
            for (int i = 0; i < MuestrasPorEscala; i++)
            {
                var muestra = new MuestraReaccion();
                yield return MedirGolpe(muestra);
                yield return MedirGuardia(muestra);
                golpes.Add(muestra.golpe);
                guardias.Add(muestra.guardia);
                // Del clic al impacto se miden entre esperado y esperado + un cuadro; la guardia cae exacta (±1 cuadro).
                Assert.GreaterOrEqual(muestra.golpe, esperado.preparacionGolpe - ToleranciaTiempo, nombre + ": golpe no antes de lo esperado");
                Assert.LessOrEqual(muestra.golpe, esperado.preparacionGolpe + muestra.frameGolpe + ToleranciaTiempo, nombre + ": golpe dentro de un cuadro de lo esperado");
                Assert.AreEqual(esperado.tiempoSubirGuardia, muestra.guardia, muestra.frameGuardia + ToleranciaTiempo, nombre + ": guardia dentro de un cuadro de lo esperado");
            }
            Debug.Log("[T-194] " + nombre + ": golpe esperado " + Fmt(esperado.preparacionGolpe) + " s, medidos [" + string.Join(", ", golpes.ConvertAll(Fmt))
                + "]; guardia esperada " + Fmt(esperado.tiempoSubirGuardia) + " s, medidas [" + string.Join(", ", guardias.ConvertAll(Fmt))
                + "]; ventana esperada " + Fmt(esperado.ventanaBloqueoPerfecto) + " s");
        }

        [UnityTest]
        public IEnumerator Sinapsis_GolpeYGuardiaSalenAntesSinCambiarVidaDanioNiVelocidad()
        {
            yield return IniciarPelea(1, AliasPrimero);
            // Nada instalado: valores base y tiempos medidos de referencia.
            ValoresMecha basales = vidaMecha.Datos.Valores;
            Assert.AreEqual(0, led.Prendidos, "sin implante el LED está apagado");
            yield return MedirEscala("sin implante", basales, 1f);

            CargarImplantes(IdSinapsis1);
            yield return FinDeCuadro();
            Assert.AreEqual(1, led.Prendidos, "SINAPSIS I prende un LED");
            yield return MedirEscala("SINAPSIS I", basales, Datos(EspacioImplante.SistemaNervioso, 1).Efectos.multiplicadorReaccionMecha);

            CargarImplantes(IdSinapsis2);
            yield return FinDeCuadro();
            Assert.AreEqual(2, led.Prendidos, "SINAPSIS II prende dos LEDs");
            float m2 = Datos(EspacioImplante.SistemaNervioso, 2).Efectos.multiplicadorReaccionMecha;
            yield return MedirEscala("SINAPSIS II", basales, m2);
            ValoresMecha esperado = ReglaReaccionImplantes.Aplicar(basales, m2);
            Assert.Less(esperado.preparacionGolpe, basales.preparacionGolpe, "SINAPSIS II acorta el golpe");
            Assert.Less(esperado.tiempoSubirGuardia, basales.tiempoSubirGuardia, "SINAPSIS II acorta la guardia");
            Assert.Greater(esperado.ventanaBloqueoPerfecto, basales.ventanaBloqueoPerfecto, "SINAPSIS II alarga la ventana perfecta");
            VerificarFinal("sinapsis");
        }

        // ---- Captura ----

        [UnityTest]
        public IEnumerator Captura_CabinaConOjosIIYLedSinapsis()
        {
            yield return IniciarPelea(1, AliasPrimero);
            CargarImplantes(IdOjos2, IdSinapsis2);
            yield return FinDeCuadro();
            Assert.IsTrue(ojos.Visible, "la pantalla de ojos se prende");
            Assert.AreEqual(2, led.Prendidos, "el LED de sinapsis está prendido con dos luces");
            Assert.AreEqual(configOjos.ColorAviso.r > 0.9f && configOjos.ColorAviso.g > 0.2f, true, "el aviso no es rojo puro");

            yield return GolpesReales();
            float fin = Time.time + MaxSegundosEstado;
            while (!(ojos.AvisoPrendido && rival.EnAviso && ojos.TextoAviso == configOjos.TextoProximoGolpe) && Time.time < fin)
            {
                yield return FinDeCuadro();
                MantenerVivo();
            }
            Assert.AreEqual(configOjos.TextoProximoGolpe, ojos.TextoAviso, "se ve PRÓXIMO: GOLPE");
            if (Environment.GetEnvironmentVariable("CAPTURAR_T194") == "1")
            {
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
                Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
                File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
                Object.Destroy(imagen);
                rt.Release();
                Object.Destroy(rt);
                Debug.Log("[T-194] captura en " + RutaCaptura);
            }
            VerificarFinal("captura");
        }
    }
}
