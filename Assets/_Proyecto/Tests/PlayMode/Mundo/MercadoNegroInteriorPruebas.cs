using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Interior del mercado negro (T-320): de día la puerta abre y de noche no; el mostrador de Tony (MostradorMercadoNegro, un IInteractuable)
    /// queda frente a la entrada y solo se ofrece con el lugar abierto; con entrada simulada (E y W en la cámara libre) se entra, se abre el mostrador
    /// y se compra un kit con una cartera de prueba. Mundo no referencia los ensamblados de Tony: el jugador a pie se instancia por ruta
    /// y la compra va por los contratos de Core (ITienda, ICartera).
    /// </summary>
    public class MercadoNegroInteriorPruebas : InputTestFixture
    {
        private const string RutaJugadorAPie = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string IdKit = "KitReparacion";
        // La Entrada queda a 1.5 m de la calle y la fachada mide 1 m: a menos de 3 m de la Entrada solo está el grosor del muro.
        // El mostrador queda a 1.4 m dentro de la puerta, en el eje de la puerta y sin tapar el paso.
        private const float MetrosMaximosEntradaAlMostrador = 4.5f;
        private const float MetrosMaximosPuertaAlMostrador = 3f;
        private const float MetrosMaximosDelEje = 0.5f;
        private const float MetrosFrenteAlMostrador = 1f;       // del lado de la puerta: el cliente llega de la calle
        private const float SegundosMaximosCaminando = 14f;
        private const float SegundosFisica = 0.3f;
        private const float SegundosMezcla = 2.5f;
        private const float AlturaApoyo = 0.05f;
        private const float AlturaOjos = 1.6f;
        private const float DistanciaLejos = 60f;
        private const float AlcanceRayos = 30f;
        private const int CantidadRayos = 36;
        private const int PuntosPorLado = 5;
        private const float GradosPorVuelta = 360f;
        private const int DineroInicial = 500;
        private const int PrecioKit = 250;
        private const float InclinacionCapturaGrados = 10f;
        private const float MetrosCapturaFuera = 3.2f;       // el callejón del mercado mide unos 4.5 m de ancho
        private const float MetrosCapturaAlFondo = 1.6f;      // del muro del fondo, para ver el cuarto de puestos
        private const float MetrosCapturaLateral = 1.2f;
        private const float MetrosCapturaDelUmbral = 0.15f;   // adentro de la cara interior de la fachada
        private const float Margen = 0.02f;

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
        private GameObject jugadorObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;
        private CarteraDoble cartera;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            EventosLugares.LimpiarSuscriptores();
        }

        public override void TearDown()
        {
            EventosLugares.LimpiarSuscriptores();
            if (cartera != null)
            {
                Registro<ICartera>.Quitar(cartera);
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

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(hora);
            zona = EscenaLugares.ZonaDe(IdLugar.MercadoNegro);
            camara = EscenaLugares.ColocarFrenteA(IdLugar.MercadoNegro, out camaraObjeto);
            yield return EscenaLugares.Calentar();
        }

        private static IInteractuable Mostrador()
        {
            foreach (IInteractuable interactuable in Registro<IInteractuable>.Todos)
            {
                Component componente = interactuable as Component;
                LugarMundo lugar = componente != null ? componente.GetComponentInParent<LugarMundo>() : null;
                // T-321: el mostrador de la clínica también cuelga del mercado; se distingue por su grupo.
                if (lugar != null && lugar.Id == IdLugar.MercadoNegro && !EsDeLaClinica(componente.transform))
                {
                    return interactuable;
                }
            }
            Assert.Fail("El mercado negro trae su mostrador registrado como IInteractuable");
            return null;
        }

        private static bool EsDeLaClinica(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name == "Clinica")
                {
                    return true;
                }
            }
            return false;
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
            Assert.Fail("El mostrador registra la tienda del mercado negro");
            return null;
        }

        private static DatosOferta Kit(ITienda tienda)
        {
            foreach (DatosOferta oferta in tienda.Ofertas)
            {
                if (oferta.Id == IdKit)
                {
                    return oferta;
                }
            }
            Assert.Fail("La tienda del mercado vende el kit de reparación");
            return null;
        }

        private Vector3 SobreElSuelo(Vector3 punto)
        {
            return new Vector3(punto.x, zona.GetComponent<BoxCollider>().bounds.min.y + AlturaApoyo, punto.z);
        }

        private IEnumerator PresionarE()
        {
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
        }

        // Entra por la puerta del mercado con E y W hasta que el lugar marca al jugador dentro.
        private IEnumerator EntrarAlMercado()
        {
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 12:00 la puerta del mercado abre");
            Press(teclado.wKey);
            float inicio = Time.time;
            while (!EscenaLugares.Lugar(IdLugar.MercadoNegro).JugadorDentro && Time.time - inicio < SegundosMaximosCaminando)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.MercadoNegro).JugadorDentro, "Entró al mercado caminando");
        }

        // El jugador a pie de Tony, frente al mostrador (se instancia por ruta: Mundo no referencia su ensamblado).
        private void PonerJugadorFrenteAlMostrador(IInteractuable mostrador)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugadorAPie);
            Assert.IsNotNull(prefab, "Existe " + RutaJugadorAPie);
            Vector3 posicion = SobreElSuelo(mostrador.Posicion - EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro) * MetrosFrenteAlMostrador);
            jugadorObjeto = Object.Instantiate(prefab, posicion, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator Mercado_A12AbreYA22No()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "A las 12:00 el mercado está abierto");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "A las 12:00 la puerta abre");
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
            EscenaLugares.Puerta(IdLugar.MercadoNegro).Cerrar();

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "A las 22:00 el mercado está cerrado");
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta, "A las 22:00 la puerta no abre");
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Mostrador_EstaFrenteALaEntrada_DentroDelMercadoYEsSolido()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            IInteractuable mostrador = Mostrador();
            ILugar mercado = EscenaLugares.Lugar(IdLugar.MercadoNegro);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro);
            Vector3 puerta = EscenaLugares.Puerta(IdLugar.MercadoNegro).Posicion;

            Assert.IsTrue(zona.GetComponent<BoxCollider>().bounds.Contains(mostrador.Posicion), "El mostrador está dentro del mercado");
            Assert.Less(Vector3.Distance(mercado.PosicionEntrada, mostrador.Posicion), MetrosMaximosEntradaAlMostrador, "Cerca de la Entrada");
            Assert.Less(Vector3.Distance(puerta, mostrador.Posicion), MetrosMaximosPuertaAlMostrador, "A menos de 3 m de la puerta");
            Vector3 lateral = Vector3.Cross(Vector3.up, haciaPuerta);
            Assert.Less(Mathf.Abs(Vector3.Dot(mostrador.Posicion - puerta, lateral)), MetrosMaximosDelEje, "Frente a la puerta, en su eje");

            Component componente = (Component)mostrador;
            Assert.IsNotNull(componente.GetComponentInChildren<Collider>(), "El mostrador es sólido");
        }

        [UnityTest]
        public IEnumerator Mostrador_SoloSeOfreceConElLugarAbierto()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            IInteractuable mostrador = Mostrador();
            PonerJugadorFrenteAlMostrador(mostrador);
            yield return null;
            yield return null;
            Assert.IsTrue(mostrador.PuedeUsar, "A las 12:00 el mostrador se ofrece");
            StringAssert.Contains("Mercado", mostrador.Texto);

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.IsFalse(mostrador.PuedeUsar, "A las 22:00 el mercado cerró y el mostrador no se ofrece");
            StringAssert.Contains("CERRADO", mostrador.Texto);
        }

        [UnityTest]
        public IEnumerator Mercado_A12_SeEntraSeAbreElMostradorYSeCompraUnKit()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            cartera = new CarteraDoble { Dinero = DineroInicial };
            Registro<ICartera>.Agregar(cartera);
            IInteractuable mostrador = Mostrador();

            yield return EntrarAlMercado();

            PonerJugadorFrenteAlMostrador(mostrador);
            yield return null;
            yield return null;
            Assert.IsTrue(mostrador.PuedeUsar, "Con el mercado abierto el mostrador se puede usar");
            mostrador.Usar();
            Assert.IsFalse(mostrador.PuedeUsar, "Con la pantalla abierta el mostrador ya no se ofrece de nuevo");

            ITienda tienda = TiendaDelMercado();
            DatosOferta kit = Kit(tienda);
            Assert.AreEqual(PrecioKit, kit.Precio);
            ResultadoCompra resultado = tienda.Comprar(kit, cartera);
            Assert.AreEqual(ResultadoCompra.Comprado, resultado, "Se compra el kit");
            Assert.AreEqual(DineroInicial - PrecioKit, cartera.Dinero, "Y se cobra su precio");
            Assert.AreEqual(1, SceneManager.sceneCount, "No hay escenas extra");
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
                    Assert.IsTrue(choco, "El rayo " + i + " a " + altura + " m no choca con nada a menos de " + AlcanceRayos + " m: hay un hueco");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "El rayo " + i + " a " + altura + " m choca con algo que no es del mercado: " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Mercado_Techo_DesdeUnaCuadriculaDelCuartoUnRayoHaciaArribaChocaConElEdificio()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Bounds sala = zona.GetComponent<BoxCollider>().bounds;
            for (int i = 0; i < PuntosPorLado; i++)
            {
                for (int j = 0; j < PuntosPorLado; j++)
                {
                    float x = Mathf.Lerp(sala.min.x, sala.max.x, (i + 0.5f) / PuntosPorLado);
                    float z = Mathf.Lerp(sala.min.z, sala.max.z, (j + 0.5f) / PuntosPorLado);
                    Vector3 origen = new Vector3(x, sala.min.y + AlturaOjos, z);
                    bool choco = Physics.Raycast(origen, Vector3.up, out RaycastHit golpe, AlcanceRayos, ~0, QueryTriggerInteraction.Ignore);
                    Assert.IsTrue(choco, "Desde (" + i + "," + j + ") se ve el cielo: falta techo");
                    Assert.IsNotNull(golpe.collider.GetComponentInParent<LugarMundo>(), "Sobre (" + i + "," + j + ") hay algo que no es del mercado: " + golpe.collider.name);
                    Assert.LessOrEqual(golpe.point.y, sala.max.y + Margen, "El techo sobre (" + i + "," + j + ") está a " + (golpe.point.y - sala.min.y) + " m y no a la altura del cuarto (" + sala.size.y + " m): " + golpe.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Mercado_Luces_ApagadasLejosYPrendidasDentro()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Vector3 lejos = EscenaLugares.Lugar(IdLugar.MercadoNegro).PosicionEntrada - EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro) * DistanciaLejos;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.Greater(zona.Luces.Length, 0, "La zona maneja las luces de sodio");
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Con la cámara lejos la luz '" + luz.name + "' debe estar apagada");
            }
            camara.Colocar(new Pose(SobreElSuelo(zona.GetComponent<BoxCollider>().bounds.center), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Dentro la luz '" + luz.name + "' debe estar prendida");
            }
        }

        [UnityTest]
        public IEnumerator Clinica_Captura_VanoYMostradorDesdeElPasillo()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro);
            Transform entradaClinica = ((Component)EscenaLugares.Lugar(IdLugar.Clinica)).transform.Find("EntradaClinica");
            Assert.IsNotNull(entradaClinica, "La clínica tiene su punto de entrada");
            Vector3 ojos = entradaClinica.position - haciaPuerta * 1.4f;   // HaciaLaPuerta apunta al fondo del cuarto
            ojos.y = entradaClinica.position.y + AlturaOjos;
            camara.Colocar(new Pose(ojos, Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(InclinacionCapturaGrados / 2f, 0f, 0f)));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-321-1.png");
        }

        [UnityTest]
        public IEnumerator Mercado_Capturas_DiaFueraYDentro()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            ILugar mercado = EscenaLugares.Lugar(IdLugar.MercadoNegro);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.MercadoNegro);

            // 1: la fachada abierta de día, desde el callejón.
            Vector3 desde = mercado.PosicionEntrada - haciaPuerta * MetrosCapturaFuera;
            desde.y = mercado.PosicionEntrada.y + AlturaOjos;
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(haciaPuerta, Vector3.up) * Quaternion.Euler(-InclinacionCapturaGrados, 0f, 0f)));
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-1.png");

            // 2: dentro, desde el fondo hacia el cuarto de puestos (y la puerta al otro extremo).
            Bounds sala = zona.GetComponent<BoxCollider>().bounds;
            float profundidad = Mathf.Abs(Vector3.Dot(sala.size, haciaPuerta));
            Vector3 lateral = Vector3.Cross(Vector3.up, haciaPuerta);
            Vector3 dentro = sala.center + haciaPuerta * (profundidad / 2f - MetrosCapturaAlFondo) - lateral * MetrosCapturaLateral;
            dentro.y = sala.min.y + AlturaOjos;
            Vector3 haciaElCuarto = -haciaPuerta + lateral * MetrosCapturaLateral / profundidad;
            camara.Colocar(new Pose(dentro, Quaternion.LookRotation(haciaElCuarto.normalized, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-2.png");

            // 3: el mostrador de Tony de frente (con el letrero al derecho), desde el umbral: ya dentro de la zona, con las luces prendidas.
            Vector3 umbral = sala.center - haciaPuerta * (profundidad / 2f) + haciaPuerta * MetrosCapturaDelUmbral;
            umbral.y = sala.min.y + AlturaOjos;
            Vector3 haciaMostrador = Mostrador().Posicion - umbral;
            haciaMostrador.y = 0f;
            camara.Colocar(new Pose(umbral, Quaternion.LookRotation(haciaMostrador.normalized, Vector3.up) * Quaternion.Euler(InclinacionCapturaGrados, 0f, 0f)));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-320-3.png");
        }
    }
}
