using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Casino.Cajera;
using MechaCyberpunk.Casino.Fichas;
using MechaCyberpunk.Casino.Tragamonedas;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
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
    /// <summary>
    /// Interior del casino (T-303): de noche la puerta abre y de día no; adentro hay 6 tragamonedas y la cajera registradas como IInteractuable;
    /// con entrada simulada (W y E en la cámara libre) se entra, se cambian fichas, se juega una vez y se sale sin atravesar muros ni dejar errores.
    /// El uso de las máquinas y la cajera llama a Usar() de su IInteractuable desde el punto de uso (la E del jugador a pie es de Tony y no se referencia desde Mundo).
    /// </summary>
    public class CasinoInteriorPruebas : InputTestFixture
    {
        private const int CantidadTragamonedas = 6;
        private const int IndiceMaquinaDePrueba = 2;       // x = -3 m: la ruta desde el pasillo central no roza columnas
        private const float SegundosMaximosCaminando = 14f;
        private const float MetrosAlPunto = 0.9f;
        private const float MetrosDentroDeLaPuerta = 2f;
        private const float MetrosDelPasilloAlFondo = 12f;   // desde la puerta, a media sala
        private const float SegundosFisica = 0.3f;
        private const float SegundosMaximosGiro = 6f;
        private const float SegundosMezcla = 2.5f;
        private const float SegundosSalir = 12f;
        private const float MetrosFueraEnLaCalle = -1f;
        private const float AlturaApoyo = 0.05f;
        private const float AlturaOjos = 1.6f;
        private const float Margen = 0.02f;
        private const float DistanciaLejos = 60f;
        private const float AlcanceRayos = 30f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;
        private const int DineroInicial = 500;
        private const float InclinacionCapturaGrados = 10f;
        private const float MetrosCapturaFuera = 9f;
        private const float MetrosCapturaDentro = 7f;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }

            public event System.Action<int, MotivoDinero> Cambio;

            public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                Dinero += cantidad;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo)
            {
                Dinero -= cantidad;
            }
        }

        private Keyboard teclado;
        private RelojMundo reloj;
        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;
        private int escenasCargadas;

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
            EventosLugares.LimpiarSuscriptores();
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            escenasCargadas++;
        }

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(hora);
            zona = EscenaLugares.ZonaDe(IdLugar.Casino);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Casino, out camaraObjeto);
            yield return EscenaLugares.Calentar();
        }

        private static List<TragamonedasMundo> Tragamonedas()
        {
            List<TragamonedasMundo> lista = new List<TragamonedasMundo>();
            foreach (TragamonedasMundo t in Object.FindObjectsByType<TragamonedasMundo>(FindObjectsSortMode.None))
            {
                LugarMundo lugar = t.GetComponentInParent<LugarMundo>();
                if (lugar != null && lugar.Id == IdLugar.Casino)
                {
                    lista.Add(t);
                }
            }
            lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return lista;
        }

        private static CajeraMundo Cajera()
        {
            CajeraMundo cajera = Object.FindFirstObjectByType<CajeraMundo>();
            Assert.IsNotNull(cajera, "El casino trae a la cajera");
            Assert.AreEqual(IdLugar.Casino, cajera.GetComponentInParent<LugarMundo>().Id);
            return cajera;
        }

        private Vector3 CentroDelInterior()
        {
            return zona.GetComponent<BoxCollider>().bounds.center;
        }

        private Vector3 SobreElSuelo(Vector3 punto)
        {
            return new Vector3(punto.x, zona.GetComponent<BoxCollider>().bounds.min.y + AlturaApoyo, punto.z);
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

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        // Camina con W hacia el punto (mirándolo) hasta quedar a menos de MetrosAlPunto; falla si se acaba el tiempo (atorado en un muro o un mueble).
        private IEnumerator CaminarA(Vector3 destino)
        {
            Vector3 haciaDestino = destino - camara.transform.position;
            haciaDestino.y = 0f;
            MirarHacia(haciaDestino.normalized, camara.transform.position);
            yield return null;
            Press(teclado.wKey);
            float inicio = Time.time;
            while (DistanciaPlana(camara.transform.position, destino) > MetrosAlPunto && Time.time - inicio < SegundosMaximosCaminando)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.LessOrEqual(DistanciaPlana(camara.transform.position, destino), MetrosAlPunto,
                "Llegó al punto sin atorarse en un muro o un mueble (quedó a " + DistanciaPlana(camara.transform.position, destino) + " m)");
        }

        // Pasillo central de la sala (sobre el eje de la puerta), sobre el piso y a media sala.
        private Vector3 PuntoDelPasillo()
        {
            return SobreElSuelo(EscenaLugares.Puerta(IdLugar.Casino).Posicion + EscenaLugares.HaciaLaPuerta(IdLugar.Casino) * MetrosDelPasilloAlFondo);
        }

        // Entra por la puerta del casino con E y W hasta que el casino marca al jugador dentro.
        private IEnumerator EntrarAlCasino()
        {
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 22:00 la puerta del casino abre");
            Press(teclado.wKey);
            float inicio = Time.time;
            while (!EscenaLugares.Lugar(IdLugar.Casino).JugadorDentro && Time.time - inicio < SegundosMaximosCaminando)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casino).JugadorDentro, "Entró al casino caminando");
        }

        [UnityTest]
        public IEnumerator Casino_A12NoAbreYA22Abre()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.Casino).EstaAbierto, "A las 12:00 el casino está cerrado");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta, "A las 12:00 la puerta no abre");
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Casino).EstaAbierta);

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casino).EstaAbierto, "A las 22:00 el casino está abierto");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 22:00 la puerta abre");
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.Casino).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Casino_TieneSeisTragamonedasYLaCajeraRegistradasComoInteractuables()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            List<TragamonedasMundo> maquinas = Tragamonedas();
            Assert.AreEqual(CantidadTragamonedas, maquinas.Count, "Seis tragamonedas dentro del casino");
            HashSet<IInteractuable> registrados = new HashSet<IInteractuable>(Registro<IInteractuable>.Todos);
            Bounds sala = zona.GetComponent<BoxCollider>().bounds;
            foreach (TragamonedasMundo maquina in maquinas)
            {
                Assert.IsTrue(registrados.Contains(maquina), maquina.name + " está registrada como IInteractuable");
                Assert.IsTrue(sala.Contains(maquina.Posicion), maquina.name + " tiene su punto de uso dentro de la sala");
            }
            CajeraMundo cajera = Cajera();
            Assert.IsTrue(registrados.Contains(cajera.OpcionCambiar), "Cambiar fichas registrado");
            Assert.IsTrue(registrados.Contains(cajera.OpcionCobrar), "Cobrar fichas registrado");
            Assert.IsNotNull(Object.FindFirstObjectByType<ServicioFichas>(), "El casino trae el servicio de fichas");
            Assert.AreEqual(1, Registro<IFichas>.Cantidad, "Un solo servicio de fichas");
        }

        [UnityTest]
        public IEnumerator Casino_A22_SeEntraSeCambianFichasSeJuegaUnaVezYSeSale_SinCargarEscenasNiAtravesarMuros()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Assert.AreEqual(1, SceneManager.sceneCount);
            escenasCargadas = 0;
            SceneManager.sceneLoaded += AlCargarEscena;
            CarteraDoble cartera = new CarteraDoble { Dinero = DineroInicial };
            ServicioFichas servicio = Object.FindFirstObjectByType<ServicioFichas>();
            CajeraMundo cajera = Cajera();
            cajera.Inyectar(cartera, servicio);
            List<TragamonedasMundo> maquinas = Tragamonedas();
            Assert.AreEqual(CantidadTragamonedas, maquinas.Count);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.Casino);

            yield return EntrarAlCasino();
            yield return CaminarA(PuntoDelPasillo());

            // A la cajera: cambia dinero por fichas.
            yield return CaminarA(cajera.OpcionCambiar.Posicion);
            Assert.IsTrue(cajera.OpcionCambiar.PuedeUsar, "Con dinero se puede cambiar");
            int fichasAntes = servicio.Saldo;
            int dineroAntes = cartera.Dinero;
            cajera.OpcionCambiar.Usar();
            Assert.Greater(servicio.Saldo, fichasAntes, "La cajera da fichas");
            Assert.Less(cartera.Dinero, dineroAntes, "Y cobra el dinero");
            int fichas = servicio.Saldo;

            // A una tragamonedas: una jugada completa.
            yield return CaminarA(PuntoDelPasillo());
            TragamonedasMundo maquina = maquinas[IndiceMaquinaDePrueba];
            yield return CaminarA(maquina.Posicion);
            Assert.IsTrue(maquina.PuedeUsar, "Con fichas la máquina se puede usar");
            int apuesta = maquina.Apuesta;
            maquina.Usar();
            Assert.IsTrue(maquina.Girando, "La máquina gira");
            float inicio = Time.time;
            while (maquina.Girando && Time.time - inicio < SegundosMaximosGiro)
            {
                yield return null;
            }
            Assert.IsFalse(maquina.Girando, "El giro termina");
            Assert.AreEqual(1, maquina.GirosCobrados);
            Assert.AreEqual(fichas - apuesta + maquina.UltimoPremio, servicio.Saldo, "Las fichas cuadran: apuesta cobrada y premio abonado");

            // De vuelta a la calle por la misma puerta.
            yield return CaminarA(PuntoDelPasillo());
            yield return CaminarA(EscenaLugares.Puerta(IdLugar.Casino).Posicion + haciaPuerta * MetrosDentroDeLaPuerta);
            MirarHacia(-haciaPuerta, camara.transform.position);
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "Desde dentro la puerta abre");
            Press(teclado.wKey);
            inicio = Time.time;
            while (EscenaLugares.MetrosDentro(camara, IdLugar.Casino) > MetrosFueraEnLaCalle && Time.time - inicio < SegundosSalir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.Less(EscenaLugares.MetrosDentro(camara, IdLugar.Casino), MetrosFueraEnLaCalle, "Sale a la calle");

            Assert.AreEqual(1, SceneManager.sceneCount, "No hay escenas extra");
            Assert.AreEqual(0, escenasCargadas, "No llega ningún sceneLoaded");
        }

        [UnityTest]
        public IEnumerator Casino_Muros_36RayosDesdeElCentroChocanConAlgoDelCasino()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Casino).EstaAbierta, "La puerta empieza cerrada");
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
                    Assert.IsTrue(choco, "El rayo " + i + " a " + altura + " m no choca con nada a menos de " + AlcanceRayos + " m: hay un hueco");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " a " + altura + " m choca con algo que no es del casino: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Casino_Luces_ApagadasLejosYPrendidasDentro_ConLasDeLasMaquinas()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Vector3 lejos = EscenaLugares.Lugar(IdLugar.Casino).PosicionEntrada - EscenaLugares.HaciaLaPuerta(IdLugar.Casino) * DistanciaLejos;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.Greater(zona.Luces.Length, CantidadTragamonedas, "La zona maneja las luces del salón y las de las máquinas");
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Con la cámara lejos la luz '" + luz.name + "' debe estar apagada");
            }
            camara.Colocar(new Pose(SobreElSuelo(CentroDelInterior()), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Dentro la luz '" + luz.name + "' debe estar prendida");
            }
        }

        [UnityTest]
        public IEnumerator Casino_Ambiente_DentroSuenaMusicaYBajaLaLluvia_YAlSalirSeVaLaMusica()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            AmbienteSonoro ambiente = Object.FindFirstObjectByType<AmbienteSonoro>();
            Assert.IsNotNull(ambiente, "El barrio trae el AmbienteSonoro");
            yield return EscenaLugares.Esperar(SegundosMezcla);
            float lluviaAfuera = ambiente.VolumenLluvia;
            Assert.Greater(lluviaAfuera, Margen, "De noche afuera llueve");
            Assert.AreEqual(0f, ambiente.VolumenMusica, Margen, "Sin música afuera");
            Vector3 afuera = camara.transform.position;

            camara.Colocar(new Pose(SobreElSuelo(CentroDelInterior()), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            Assert.IsTrue(zona.JugadorDentro);
            Assert.Greater(ambiente.VolumenMusica, Margen, "Dentro suena la música");
            Assert.Less(ambiente.VolumenLluvia, lluviaAfuera - Margen, "Dentro baja la lluvia");
            Assert.AreEqual(0f, ambiente.VolumenNeon, Margen, "Dentro no se oye el neón");

            camara.Colocar(new Pose(afuera, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            Assert.AreEqual(0f, ambiente.VolumenMusica, Margen, "Al salir se va la música");
        }

        [UnityTest]
        public IEnumerator Casino_Capturas_NocheFueraYDentro()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            ILugar casino = EscenaLugares.Lugar(IdLugar.Casino);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.Casino);

            // 1: fachada abierta de noche, desde la calle.
            Vector3 desde = casino.PosicionEntrada - haciaPuerta * MetrosCapturaFuera;
            desde.y = casino.PosicionEntrada.y + AlturaOjos;
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(-InclinacionCapturaGrados, 0f, 0f)));
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-303-1.png");

            // 2 y 3: dentro, hacia las máquinas del fondo y hacia la cajera (el fondo está más adentro que el centro, en el sentido de la puerta).
            Vector3 dentro = SobreElSuelo(CentroDelInterior()) - haciaPuerta * MetrosCapturaDentro;
            dentro.y = zona.GetComponent<BoxCollider>().bounds.min.y + AlturaOjos;
            camara.Colocar(new Pose(dentro, Quaternion.LookRotation(haciaPuerta, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-303-2.png");

            Vector3 haciaCajera = Cajera().transform.position - dentro;
            haciaCajera.y = 0f;
            camara.Colocar(new Pose(dentro, Quaternion.LookRotation(haciaCajera.normalized, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-303-3.png");
        }
    }
}
