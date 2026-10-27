using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-159: retar en las ligas desde el mostrador de peleas de Pruebas_Economia. Entrada simulada (E, flechas, Enter, Esc, L, clics);
    /// lo único que no es entrada es dejar el progreso en un punto con <c>Poner</c>, dar dinero y rematar al rival con daño directo
    /// después de daño real al jugador. Los números salen de los assets.
    /// </summary>
    public class LigasMostradorPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasJefe = "DON FIERRO";
        private const int DineroBase = 500;
        private const int DineroParaApostar = 5000;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesDeEspera = 6;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 90f;
        private const float MaxSegundosGolpes = 90f;
        private const float MaxSegundosSalida = 30f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaFregada = 0.2f;
        private const float DistanciaAlMostrador = 2f;
        private const float AlturaInicio = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantalla;
        private VidaMecha vidaMecha;
        private ICartera cartera;
        private ServicioLigas ligas;
        private CalendarioLigas calendario;
        private readonly List<string> errores = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
        private HashSet<GameObject> raicesIniciales;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "ligas_mostrador_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            terminadas.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        // ---- Preparación y utilidades ----

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            MontajeMecha montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Pruebas_Economia trae el mostrador de peleas");
            Assert.IsNotNull(montaje, "Pruebas_Economia trae el mecha de la historia");
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantalla = mostrador.GetComponentInChildren<PantallaApuesta>(true);
            vidaMecha = montaje.GetComponent<VidaMecha>();
            cartera = Registro<ICartera>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(ligas, "Hay ServicioLigas");
            Assert.IsNotNull(calendario, "Existe el calendario");
            Assert.AreEqual(DineroBase, cartera.Dinero, "Se empieza con $500");
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            EventosPelea.PeleaTerminada += terminadas.Add;
            yield return null;
        }

        private IEnumerator IrAlMostrador()
        {
            Vector3 posicion = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(posicion, Quaternion.identity));
            for (int i = 0; i < FramesParaElHud; i++)
            {
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                yield return null;
            }
        }

        private OpcionCartelera Opcion(TipoOpcionCartelera tipo, int liga, int indice)
        {
            foreach (OpcionCartelera opcion in mostrador.Opciones)
            {
                if (opcion.Tipo == tipo && opcion.Liga == liga && opcion.Indice == indice)
                {
                    return opcion;
                }
            }
            Assert.Fail("No está en la lista: " + tipo + " " + liga + "-" + indice + ". Hay: " + string.Join(", ", mostrador.Opciones));
            return default;
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private IEnumerator ClicsHasta(Func<bool> condicion)
        {
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosGolpes;
            while (!condicion() && Time.time < fin)
            {
                if (Time.time >= proximoClic)
                {
                    clic = !clic;
                    proximoClic = Time.time + SegundosEntreClics / 2f;
                    if (clic) Press(raton.leftButton); else Release(raton.leftButton);
                }
                yield return null;
            }
            Release(raton.leftButton);
            yield return null;
        }

        // Retar la opción: abre la pantalla de apuesta (los carteles traen apuesta). Enter pelea con lo elegido.
        private IEnumerator RetarYAbrir(OpcionCartelera opcion)
        {
            Assert.IsTrue(mostrador.Retar(opcion), "Retar " + opcion + " empieza: " + mostrador.Texto);
            Assert.IsTrue(pantalla.Visible, "Se ve la pantalla de apuesta");
            yield return null;
        }

        private IEnumerator EsperarPelea(string alias)
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.IsNotNull(director.Rival, "Hay rival");
            Assert.AreEqual(alias, director.Rival.Nombre, "El rival es el del cartel");
        }

        private IEnumerator GanarLaPelea()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private IEnumerator PerderLaPelea()
        {
            yield return ClicsHasta(() => vidaMecha.EstaKO);
            Assert.IsTrue(vidaMecha.EstaKO, "El rival lo dejó en KO con golpes reales");
        }

        private IEnumerator EsperarFinDeLaEntrada()
        {
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Siguiente_Ganar_PagaElPremioDelCartelYAvanzaElSiguiente()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            CartelPelea cartel = calendario.Cartel(1, 1);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, 1, 1);
            Assert.AreEqual(AliasPrimero, mostrador.AliasDe(opcion), "El alias sale del prefab del cartel");
            Assert.AreEqual(cartel.premioGanar, mostrador.PremioDe(opcion), "El premio mostrado es el del cartel");

            yield return RetarYAbrir(opcion);
            Assert.AreEqual(AliasPrimero, Registro<IApuestasPelea>.Actual.Rival, "La apuesta anuncia al rival del cartel");
            StringAssert.Contains(FormatoDinero.Texto(cartel.premioGanar), pantalla.Premio, "La pantalla dice el premio del cartel");
            yield return Tecla(teclado.enterKey);
            yield return EsperarPelea(AliasPrimero);
            yield return GanarLaPelea();
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador);
            Assert.AreEqual(DineroBase + cartel.premioGanar, cartera.Dinero, "Cobró el premio del cartel");
            Assert.AreEqual(1, ligas.LigaActual);
            Assert.AreEqual(2, ligas.Siguiente, "El siguiente pasa a 2");
            Assert.AreEqual(opcion, mostrador.UltimaOpcion, "Dice qué se peleó");
            Assert.IsNull(director.CartelActual, "El cartel se quitó");
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "En Libre ganar una liga no cambia la etapa");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Revancha_Ganar_PagaLaMitadYNoCambiaElProgreso()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 3, 0));
            yield return IrAlMostrador();
            CartelPelea cartel = calendario.Cartel(1, 1);
            int esperado = Mathf.RoundToInt(cartel.premioGanar * ligas.Datos.FraccionPremioRevancha);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Revancha, 1, 1);
            Assert.AreEqual(esperado, opcion.Premio, "La revancha muestra la fracción del premio");
            Assert.Less(esperado, cartel.premioGanar);

            yield return RetarYAbrir(opcion);
            yield return Tecla(teclado.enterKey);
            yield return EsperarPelea(AliasPrimero);
            yield return GanarLaPelea();
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador);
            Assert.AreEqual(DineroBase + esperado, cartera.Dinero, "Paga la mitad");
            Assert.AreEqual(1, ligas.LigaActual);
            Assert.AreEqual(3, ligas.Siguiente, "El progreso no cambia");
            Assert.AreEqual(opcion, mostrador.UltimaOpcion);
            Assert.IsNull(director.CartelActual);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Siguiente_Perder_NoCambiaElProgresoYSePuedeRetarOtraVez()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 2, 0));
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            yield return IrAlMostrador();
            CartelPelea cartel = calendario.Cartel(1, 2);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, 1, 2);

            yield return RetarYAbrir(opcion);
            yield return Tecla(teclado.enterKey);
            yield return EsperarPelea(mostrador.AliasDe(opcion));
            yield return PerderLaPelea();
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(LadoPelea.Rival, terminadas[0].Ganador);
            Assert.AreEqual(DineroBase + cartel.premioPerder, cartera.Dinero, "Solo el premio de perder del cartel");
            Assert.AreEqual(1, ligas.LigaActual);
            Assert.AreEqual(2, ligas.Siguiente, "Perder no cambia el progreso");
            Assert.IsNull(director.CartelActual);

            // Repara el mecha y vuelve a retar: se puede, y salir sin pelear deja todo igual.
            vidaMecha.FijarVida(vidaMecha.Maxima);
            yield return IrAlMostrador();
            yield return RetarYAbrir(Opcion(TipoOpcionCartelera.Siguiente, 1, 2));
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesDeEspera);
            Assert.IsFalse(pantalla.Visible);
            Assert.IsNull(director.CartelActual, "Salir de la apuesta quita el cartel");
            Assert.AreEqual(DineroBase + cartel.premioPerder, cartera.Dinero, "Sin cobrar");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_ApostarElMaximoYGanar_SubeDeLigaYAbonaPremioMasApuesta()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 5, 0));
            cartera.Abonar(DineroParaApostar, MotivoDinero.Otro);
            int dineroAntes = cartera.Dinero;
            yield return IrAlMostrador();
            CartelPelea cartel = calendario.Cartel(1, 5);
            DatosApuestaPelea datos = cartel.apuesta;
            int maximo = datos.Montos.Where(m => m <= datos.ApuestaMaxima).Max();
            int ganancia = (int)Math.Round(maximo * (double)datos.Momio, MidpointRounding.AwayFromZero);
            var campeones = new List<int>();
            ligas.CampeonVencido += campeones.Add;

            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, 1, 5);
            Assert.IsTrue(opcion.EsJefe, "El quinto es el jefe");
            Assert.AreEqual(AliasJefe, mostrador.AliasDe(opcion));
            yield return RetarYAbrir(opcion);
            for (int i = 1; i < pantalla.CantidadOpciones; i++)
            {
                yield return Tecla(teclado.rightArrowKey);
            }
            Assert.AreEqual(maximo, pantalla.Monto, "Elige el monto más alto de la apuesta del jefe");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dineroAntes - maximo, cartera.Dinero, "Se cobra lo apostado");
            yield return EsperarPelea(AliasJefe);
            yield return GanarLaPelea();
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador);
            Assert.AreEqual(dineroAntes + cartel.premioGanar + ganancia, cartera.Dinero, "Premio + apuesta x (1 + momio), neto de lo cobrado");
            Assert.AreEqual(2, ligas.LigaActual, "Sube a la liga 2");
            Assert.AreEqual(1, ligas.Siguiente);
            CollectionAssert.AreEqual(new[] { 1 }, campeones, "Avisa CampeonVencido(1) una vez");
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Retar_OpcionFueraDeLaLista_DevuelveFalseSinCobrarNiPelear()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 2, 0));
            yield return IrAlMostrador();

            OpcionCartelera liga3 = new OpcionCartelera(TipoOpcionCartelera.Siguiente, 3, 1, false, 100);
            OpcionCartelera rival4 = new OpcionCartelera(TipoOpcionCartelera.Siguiente, 1, 4, false, 100);
            OpcionCartelera bloqueada = Opcion(TipoOpcionCartelera.Bloqueada, 2, 5);
            Assert.IsFalse(mostrador.Retar(liga3), "La liga 3 no está en la lista");
            Assert.IsFalse(mostrador.Retar(rival4), "El rival 4 no es el siguiente");
            Assert.IsFalse(mostrador.Retar(bloqueada), "La liga que sigue está bloqueada");
            Assert.IsFalse(pantalla.Visible);
            Assert.IsFalse(entrada.EnCurso);
            Assert.IsNull(director.CartelActual);
            Assert.AreEqual(DineroBase, cartera.Dinero);

            // En la etapa de la pelea casera solo se ofrece la casera.
            OpcionCartelera siguiente = Opcion(TipoOpcionCartelera.Siguiente, 1, 2);
            ((ServicioHistoria)Registro<IHistoria>.Actual).Cargar("{\"etapa\":\"" + EtapaPrologo.PeleaCasera + "\"}");
            Assert.AreEqual(EtapaPrologo.PeleaCasera, Registro<IHistoria>.Actual.Etapa);
            Assert.AreEqual(1, mostrador.Opciones.Count, "Solo la casera");
            Assert.AreEqual(TipoOpcionCartelera.Casera, mostrador.Opciones[0].Tipo);
            Assert.IsFalse(mostrador.Retar(siguiente), "Una pelea de liga no se ofrece en la pelea casera");
            Assert.IsTrue(mostrador.Retar(mostrador.Opciones[0]), "La casera sí se puede retar");
            Assert.IsTrue(pantalla.Visible);
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesDeEspera);
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(DineroBase, cartera.Dinero, "Sin cobrar");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator TeclaL_EnPruebasEconomia_VenceAlSiguienteRival()
        {
            yield return CargarEscena();
            Assert.AreEqual(1, ligas.Siguiente);
            yield return Tecla(teclado.lKey);
            Assert.AreEqual(2, ligas.Siguiente, "L avanza el siguiente rival");
            yield return Tecla(teclado.lKey);
            Assert.AreEqual(3, ligas.Siguiente);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
