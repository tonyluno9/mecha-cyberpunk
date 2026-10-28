using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Interior del mercado negro (T-320): abre a las 12:00 y cierra a las 22:00; el mostrador de Tony (MostradorMercadoNegro.prefab, de solo lectura)
    /// queda a menos de 3 m de la Entrada, es un IInteractuable y solo se ofrece con el lugar abierto. Con entrada simulada (W y E en la cámara libre)
    /// se entra por la puerta; el mostrador necesita al jugador a pie de Tony, que Mundo no referencia: se crea por reflexión un JugadorAPie mínimo
    /// (sin servicios ni guardado) y se compra el kit con una cartera de prueba.
    /// </summary>
    public class MercadoNegroInteriorPruebas : InputTestFixture
    {
        private const string NombreTipoJugador = "MechaCyberpunk.Personaje.Jugador.JugadorAPie";
        private const string NombreMostrador = "MostradorMercadoNegro";
        private const float MetrosMaximosAEntrada = 3f;
        private const float XPasoLibre = 0.8f;             // x local por donde se entra: el mostrador ocupa el lado izquierdo del paso
        private const float MetrosAlPunto = 0.9f;
        private const float SegundosMaximosCaminando = 14f;
        private const float SegundosFisica = 0.3f;
        private const float AlturaApoyo = 0.05f;
        private const float AlturaOjos = 1.6f;
        private const float MetrosCapturaFuera = 5f;
        private const float InclinacionCapturaGrados = 8f;
        private const float AlcanceRayos = 30f;
        private const int CantidadRayos = 36;
        private const float GradosPorVuelta = 360f;
        private const int DineroInicial = 500;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }

            public event Action<int, MotivoDinero> Cambio;

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
        private GameObject jugadorObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;
        private CarteraDoble cartera;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            EventosLugares.LimpiarSuscriptores();
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            EventosLugares.LimpiarSuscriptores();
            if (cartera != null)
            {
                Registro<ICartera>.Quitar(cartera);
                cartera = null;
            }
            if (jugadorObjeto != null)
            {
                Object.DestroyImmediate(jugadorObjeto);
            }
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(hora);
            zona = EscenaLugares.ZonaDe(IdLugar.MercadoNegro);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.MercadoNegro, out camaraObjeto);
            yield return EscenaLugares.Calentar();
        }

        private static Transform Lugar()
        {
            return EscenaLugares.ZonaDe(IdLugar.MercadoNegro).GetComponentInParent<LugarMundo>().transform;
        }

        // El mostrador del mercado, por la interfaz de Core (el MostradorTienda es de Tony y Mundo no lo referencia).
        private static IInteractuable Mostrador()
        {
            foreach (IInteractuable candidato in Registro<IInteractuable>.Todos)
            {
                Component componente = candidato as Component;
                if (componente != null && componente.name == NombreMostrador && componente.GetComponentInParent<LugarMundo>() != null
                    && componente.GetComponentInParent<LugarMundo>().Id == IdLugar.MercadoNegro)
                {
                    return candidato;
                }
            }
            Assert.Fail("No hay un mostrador registrado dentro del mercado negro");
            return null;
        }

        private static ITienda TiendaDelMercado()
        {
            foreach (ITienda tienda in Registro<ITienda>.Todos)
            {
                if (tienda.Lugar == IdLugar.MercadoNegro)
                {
                    return tienda;
                }
            }
            Assert.Fail("No hay una tienda del mercado negro registrada");
            return null;
        }

        private Vector3 EnElLugar(float x, float y, float z)
        {
            return Lugar().TransformPoint(new Vector3(x, y, z));
        }

        // Pone a la cámara libre en la calle, un poco a la derecha del eje de la puerta, para pasar junto al mostrador.
        private void ColocarEnElPasoLibre()
        {
            Vector3 entrada = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada;
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro);
            Vector3 posicion = entrada + Lugar().right * XPasoLibre + Vector3.up * AlturaApoyo;
            camara.Colocar(new Pose(posicion, Quaternion.LookRotation(haciaPuerta, Vector3.up)));
        }

        // El mismo jugador a pie de Tony, mínimo: CharacterController y cámara hija; sin servicios ni guardado.
        private GameObject CrearJugadorAPie(Vector3 posicion)
        {
            Type tipo = null;
            foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                tipo = ensamblado.GetType(NombreTipoJugador);
                if (tipo != null)
                {
                    break;
                }
            }
            Assert.IsNotNull(tipo, "Existe el jugador a pie " + NombreTipoJugador);
            GameObject objeto = new GameObject("JugadorAPieDePrueba");
            objeto.SetActive(false);
            objeto.AddComponent<CharacterController>();
            GameObject hijo = new GameObject("Camara");
            hijo.transform.SetParent(objeto.transform, false);
            Camera lente = hijo.AddComponent<Camera>();
            lente.enabled = false;
            Component jugador = objeto.AddComponent(tipo);
            FieldInfo campo = tipo.GetField("camara", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(campo, "El jugador a pie guarda su cámara en 'camara'");
            campo.SetValue(jugador, lente);
            objeto.transform.position = posicion;
            objeto.SetActive(true);
            return objeto;
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

        // Camina con W hacia el punto hasta quedar a menos de MetrosAlPunto; falla si se atora en un muro o un mueble.
        private IEnumerator CaminarA(Vector3 destino)
        {
            Vector3 haciaDestino = destino - camara.transform.position;
            haciaDestino.y = 0f;
            camara.Colocar(new Pose(camara.transform.position, Quaternion.LookRotation(haciaDestino.normalized, Vector3.up)));
            yield return null;
            Press(teclado.wKey);
            float inicio = Time.time;
            while (DistanciaPlana(camara.transform.position, destino) > MetrosAlPunto && Time.time - inicio < SegundosMaximosCaminando)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.LessOrEqual(DistanciaPlana(camara.transform.position, destino), MetrosAlPunto,
                "Llegó al punto sin atorarse (quedó a " + DistanciaPlana(camara.transform.position, destino) + " m)");
        }

        [UnityTest]
        public IEnumerator Mercado_A12AbreYA22NoAbre()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "A las 12:00 el mercado negro está abierto");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 12:00 la puerta abre");
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
            EscenaLugares.Puerta(IdLugar.MercadoNegro).Cerrar();

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "A las 22:00 el mercado negro está cerrado");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta, "A las 22:00 la puerta no abre");
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Mostrador_EsInteractuableACercaDeLaEntrada_YSoloFuncionaConElLugarAbierto()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            IInteractuable mostrador = Mostrador();
            Vector3 entrada = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada;
            Assert.Less(DistanciaPlana(mostrador.Posicion, entrada), MetrosMaximosAEntrada, "El mostrador queda a menos de 3 m de la Entrada");

            Object.DestroyImmediate(camaraObjeto);
            camaraObjeto = null;
            jugadorObjeto = CrearJugadorAPie(EnElLugar(XPasoLibre, AlturaApoyo, 6.5f));
            yield return null;
            Assert.IsTrue(mostrador.PuedeUsar, "De día el mostrador se ofrece: " + mostrador.Texto);

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.IsFalse(mostrador.PuedeUsar, "De noche el mostrador no se ofrece");
            StringAssert.Contains("CERRADO", mostrador.Texto, "Y dice que el mercado está cerrado");
            Assert.AreEqual(0, errores.Count, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Mercado_A12_SeEntraSeAbreElMostradorYSeCompraUnKit_ConCarteraDePrueba()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            ColocarEnElPasoLibre();
            yield return null;

            // Se entra con E y W por el paso libre junto al mostrador.
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 12:00 la puerta abre");
            Press(teclado.wKey);
            float inicio = Time.time;
            while (!EscenaLugares.Lugar(IdLugar.MercadoNegro).JugadorDentro && Time.time - inicio < SegundosMaximosCaminando)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.MercadoNegro).JugadorDentro, "Entró al mercado caminando");
            yield return CaminarA(EnElLugar(XPasoLibre, AlturaApoyo, 0f));
            Assert.IsTrue(zona.JugadorDentro);

            // Al mostrador: el jugador a pie se para frente a él, en el hueco de la puerta.
            Object.DestroyImmediate(camaraObjeto);
            camaraObjeto = null;
            IInteractuable mostrador = Mostrador();
            jugadorObjeto = CrearJugadorAPie(EnElLugar(XPasoLibre, AlturaApoyo, 6.2f));
            yield return null;
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador se ofrece: " + mostrador.Texto);
            Assert.Less(DistanciaPlana(jugadorObjeto.transform.position, mostrador.Posicion), 2.5f, "El jugador está al alcance del mostrador");

            cartera = new CarteraDoble { Dinero = DineroInicial };
            Registro<ICartera>.Agregar(cartera);
            mostrador.Usar();
            yield return null;
            Assert.IsFalse(mostrador.PuedeUsar, "Con la pantalla abierta el mostrador no se vuelve a ofrecer");

            ITienda tienda = TiendaDelMercado();
            DatosOferta kit = null;
            foreach (DatosOferta oferta in tienda.Ofertas)
            {
                if (oferta.Tipo == TipoOferta.KitReparacion)
                {
                    kit = oferta;
                }
            }
            Assert.IsNotNull(kit, "El mercado vende un kit");
            int comprados = 0;
            tienda.Comprado += oferta => comprados++;
            Assert.AreEqual(ResultadoCompra.Comprado, tienda.Comprar(kit, cartera), "Con dinero se compra el kit");
            Assert.AreEqual(DineroInicial - kit.Precio, cartera.Dinero, "Cobra el precio del kit");
            Assert.AreEqual(1, comprados, "Avisa una compra");
            yield return null;
            Assert.AreEqual(0, errores.Count, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Mercado_Muros_36RayosDesdeElCentroChocanConAlgoDelMercado()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
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
                    Assert.IsTrue(choco, "El rayo " + i + " a " + altura + " m no choca con nada: hay un hueco");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " a " + altura + " m choca con algo que no es del mercado: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Mercado_Luces_ApagadasLejosYPrendidasDentro()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Vector3 lejos = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada - EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro) * 60f;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.GreaterOrEqual(zona.Luces.Length, 6, "La zona maneja las lámparas de sodio y las luces del cuarto");
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Con la cámara lejos la luz '" + luz.name + "' debe estar apagada");
            }
            camara.Colocar(new Pose(EnElLugar(XPasoLibre, AlturaApoyo, 0f), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Dentro la luz '" + luz.name + "' debe estar prendida");
            }
        }

        [UnityTest]
        public IEnumerator Mercado_Capturas_DiaFueraYDentro()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro);
            Assert.AreEqual(ResultadoPuerta.Abierta, EscenaLugares.Puerta(IdLugar.MercadoNegro).IntentarAbrir(), "La puerta abre para las capturas");

            // 1: desde el callejón, el mercado abierto de día.
            Vector3 desde = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada - haciaPuerta * MetrosCapturaFuera;
            desde.y = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada.y + AlturaOjos;
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(-InclinacionCapturaGrados, 0f, 0f)));
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-1.png");

            // 2: dentro, al fondo del cuarto mirando a la puerta: se ven el mostrador de Tony y los puestos.
            Vector3 fondo = EnElLugar(0f, AlturaOjos, -3.8f);
            camara.Colocar(new Pose(fondo, Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(4f, 0f, 0f)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-2.png");

            // 3: junto a la puerta mirando al fondo: puestos, cajas y letrero.
            Vector3 cerca = EnElLugar(0f, AlturaOjos, 4.2f);
            camara.Colocar(new Pose(cerca, Quaternion.LookRotation(-haciaPuerta, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-3.png");
        }
    }
}
