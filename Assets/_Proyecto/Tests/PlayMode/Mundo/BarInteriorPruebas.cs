using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Interior del bar (T-043): se entra y se sale a pie sin cargar escenas, avisa al jugador entrar y salir, no tiene huecos y apaga sus luces.</summary>
    public class BarInteriorPruebas : InputTestFixture
    {
        private const float SegundosMaximosAlCentro = 10f;
        private const float MetrosAlCentro = 1f;
        private const float MetrosAntesDeLaPuertaParaE = 2f;
        private const float SegundosSalir = 2f;
        private const float SegundosEsperaCierre = 4f;    // el config cierra a los 3 s sin nadie
        private const float MetrosFueraEnLaCalle = -1f;
        private const float SegundosMaximosPorFrame = 0.1f;
        private const float SegundosFisica = 0.3f;
        private const float AlturaApoyo = 0.05f;
        private const float HoraBarCerrado = 4.5f;
        private const float DistanciaLejos = 40f;
        private const float AlcanceRayos = 15f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;

        private Keyboard teclado;
        private RelojMundo reloj;
        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;
        private int escenasCargadas;
        private float peorFrame;
        private readonly System.Collections.Generic.List<string> consola = new System.Collections.Generic.List<string>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            EventosLugares.LimpiarSuscriptores();
        }

        public override void TearDown()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            Application.logMessageReceived -= AlRecibirLog;
            EventosLugares.LimpiarSuscriptores();
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(hora);
            zona = EscenaLugares.ZonaDe(IdLugar.Bar);
            Assert.IsNotNull(zona, "Ciudad debe traer la ZonaInterior del bar");
        }

        private void AlRecibirLog(string texto, string traza, LogType tipo)
        {
            consola.Add(texto);
        }

        private int CuantasVeces(string texto)
        {
            int cuenta = 0;
            foreach (string linea in consola)
            {
                if (linea == texto)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            escenasCargadas++;
        }

        private Vector3 CentroDelInterior()
        {
            return zona.GetComponent<BoxCollider>().bounds.center;
        }

        private float DistanciaPlanaAlCentro()
        {
            Vector3 d = camara.transform.position - CentroDelInterior();
            d.y = 0f;
            return d.magnitude;
        }

        private Vector3 SobreElSuelo(Vector3 punto)
        {
            return new Vector3(punto.x, zona.GetComponent<BoxCollider>().bounds.min.y + AlturaApoyo, punto.z);
        }

        private Vector3 HaciaAdentro()
        {
            return EscenaLugares.HaciaLaPuerta(IdLugar.Bar);
        }

        private void MirarHacia(Vector3 direccion, Vector3 posicion)
        {
            camara.Colocar(new Pose(posicion, Quaternion.LookRotation(direccion, Vector3.up)));
        }

        private IEnumerator PresionarE()
        {
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
        }

        // Camina con W hasta que se cumple la condición o se acaba el tiempo; devuelve los segundos usados por 'tiempoUsado'.
        private IEnumerator CaminarHasta(System.Func<bool> llego, float segundosMaximos, System.Action<float> tiempoUsado)
        {
            Press(teclado.wKey);
            float inicio = Time.time;
            while (!llego() && Time.time - inicio < segundosMaximos)
            {
                yield return null;
                peorFrame = Mathf.Max(peorFrame, Time.unscaledDeltaTime);
            }
            Release(teclado.wKey);
            tiempoUsado?.Invoke(Time.time - inicio);
        }

        [UnityTest]
        public IEnumerator Bar_A22_DeLaBanquetaAlCentroYDeVueltaALaCalle_SinCargarEscenas()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Assert.AreEqual(EscenaLugares.HoraNoche, reloj.Hora, 0.01f);
            Assert.AreEqual(1, SceneManager.sceneCount);
            escenasCargadas = 0;
            peorFrame = 0f;
            SceneManager.sceneLoaded += AlCargarEscena;

            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta);

            float segundosAlCentro = -1f;
            yield return CaminarHasta(() => DistanciaPlanaAlCentro() < MetrosAlCentro, SegundosMaximosAlCentro, t => segundosAlCentro = t);
            Assert.Less(DistanciaPlanaAlCentro(), MetrosAlCentro, "Debe llegar al centro del interior en menos de 10 s (usó " + segundosAlCentro + " s)");
            Assert.Less(segundosAlCentro, SegundosMaximosAlCentro);
            yield return null;
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Bar).JugadorDentro, "Con la cámara al centro el bar sabe que el jugador está dentro");

            // Da la vuelta (media vuelta de la cámara), camina a la puerta, E y sale a la calle.
            Vector3 haciaPuerta = -EscenaLugares.HaciaLaPuerta(IdLugar.Bar);
            MirarHacia(haciaPuerta, camara.transform.position);
            yield return null;
            Assert.Greater(Vector3.Dot(camara.Camara.transform.forward, haciaPuerta), 0.9f, "Tras la vuelta mira hacia la puerta");
            yield return EscenaLugares.Esperar(SegundosEsperaCierre);
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Bar).EstaAbierta, "Sin nadie en el umbral la puerta se cerró sola");
            yield return CaminarHasta(() => Vector3.Dot(EscenaLugares.Puerta(IdLugar.Bar).Posicion - camara.transform.position, haciaPuerta) < MetrosAntesDeLaPuertaParaE,
                SegundosMaximosAlCentro, null);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "Desde dentro la puerta abre");

            yield return CaminarHasta(() => EscenaLugares.MetrosDentro(camara, IdLugar.Bar) < MetrosFueraEnLaCalle, SegundosSalir + SegundosMaximosAlCentro, null);
            Assert.Less(EscenaLugares.MetrosDentro(camara, IdLugar.Bar), MetrosFueraEnLaCalle, "Sale a la calle");

            Assert.AreEqual(1, SceneManager.sceneCount, "No hay escenas extra");
            Assert.AreEqual(0, escenasCargadas, "No llega ningún sceneLoaded");
            Assert.Less(peorFrame, SegundosMaximosPorFrame, "Ningún frame dura más de 100 ms (peor: " + peorFrame + " s)");
        }

        [UnityTest]
        public IEnumerator Bar_EntrarYSalir_AvisaUnaVezCadaUnoYMarcaJugadorDentro()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            int entradas = 0;
            int salidas = 0;
            EventosLugares.JugadorEntro += id => { if (id == IdLugar.Bar) { entradas++; } };
            EventosLugares.JugadorSalio += id => { if (id == IdLugar.Bar) { salidas++; } };
            ILugar bar = EscenaLugares.Lugar(IdLugar.Bar);
            Assert.IsFalse(bar.JugadorDentro);

            Application.logMessageReceived += AlRecibirLog;
            MirarHacia(HaciaAdentro(), SobreElSuelo(CentroDelInterior()));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.AreEqual(1, entradas, "JugadorEntro(Bar) llega una vez");
            Assert.AreEqual(0, salidas);
            Assert.IsTrue(bar.JugadorDentro);
            Assert.IsTrue(zona.JugadorDentro);

            // Se queda dentro varios frames: no repite el aviso aunque tenga varios colliders.
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.AreEqual(1, entradas);

            Assert.AreEqual(1, CuantasVeces("[Mundo] Entró al Bar"), "La consola dice Entró al Bar una vez");
            MirarHacia(Vector3.forward, bar.PosicionEntrada + Vector3.up * AlturaApoyo);
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.AreEqual(1, entradas);
            Assert.AreEqual(1, salidas, "JugadorSalio(Bar) llega una vez");
            Assert.AreEqual(1, CuantasVeces("[Mundo] Salió del Bar"), "La consola dice Salió del Bar una vez");
            Assert.IsFalse(bar.JugadorDentro);
        }

        [UnityTest]
        public IEnumerator Bar_PuertaCerrada_36RayosDesdeElCentroChocanConUnMuroACortaDistancia()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Bar).EstaAbierta, "La puerta empieza cerrada");
            BoxCollider caja = zona.GetComponent<BoxCollider>();
            Vector3 centro = caja.bounds.center;
            float suelo = caja.bounds.min.y;
            foreach (float altura in new[] { 1f, 3f })
            {
                for (int i = 0; i < CantidadRayos; i++)
                {
                    Vector3 direccion = Quaternion.Euler(0f, i * GradosPorVuelta / CantidadRayos, 0f) * Vector3.forward;
                    Vector3 origen = new Vector3(centro.x, suelo + altura, centro.z);
                    bool choco = Physics.Raycast(origen, direccion, out RaycastHit golpe, AlcanceRayos, ~0, QueryTriggerInteraction.Ignore);
                    Assert.IsTrue(choco, "El rayo " + i + " a " + altura + " m de alto no choca con nada a menos de " + AlcanceRayos + " m: hay un hueco");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " a " + altura + " m choca con algo que no es del bar: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Bar_Luces_ApagadasConLaCamaraLejosYPrendidasDentro()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            Vector3 lejos = EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada + EscenaLugares.HaciaLaPuerta(IdLugar.Bar) * -DistanciaLejos;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.Greater(zona.Luces.Length, 0);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Con la cámara lejos la luz '" + luz.name + "' debe estar apagada");
            }
            Assert.IsFalse(zona.LucesPrendidas);

            Vector3 centro = CentroDelInterior();
            camara.Colocar(new Pose(SobreElSuelo(centro), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Dentro la luz '" + luz.name + "' debe estar prendida");
            }
        }

        [UnityTest]
        public IEnumerator Bar_A0430ConLaPuertaCerradaYAdentro_EAbreYLaCamaraSale()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Vector3 centro = CentroDelInterior();
            MirarHacia(EscenaLugares.HaciaLaPuerta(IdLugar.Bar), SobreElSuelo(centro));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Bar).JugadorDentro);

            reloj.AvanzarHasta(HoraBarCerrado);
            yield return EscenaLugares.Esperar(SegundosFisica);
            ILugar bar = EscenaLugares.Lugar(IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            Assert.IsFalse(bar.EstaAbierto, "A las 04:30 el bar está cerrado");
            Assert.IsFalse(puerta.EstaAbierta, "La puerta está cerrada");

            Vector3 haciaPuerta = -EscenaLugares.HaciaLaPuerta(IdLugar.Bar);
            Vector3 junto = puerta.Posicion - haciaPuerta * MetrosAntesDeLaPuertaParaE;
            MirarHacia(haciaPuerta, SobreElSuelo(junto));
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "Desde dentro, aunque esté cerrado, la puerta abre");
            Assert.IsTrue(puerta.EstaAbierta);

            yield return CaminarHasta(() => EscenaLugares.MetrosDentro(camara, IdLugar.Bar) < MetrosFueraEnLaCalle, SegundosSalir + SegundosMaximosAlCentro, null);
            Assert.Less(EscenaLugares.MetrosDentro(camara, IdLugar.Bar), MetrosFueraEnLaCalle, "La cámara sale a la calle");
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsFalse(bar.JugadorDentro);
        }
    }
}
