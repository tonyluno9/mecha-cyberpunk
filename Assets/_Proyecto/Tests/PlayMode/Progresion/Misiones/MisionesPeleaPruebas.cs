using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Misiones;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Tests.Pelea;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Misiones
{
    /// <summary>
    /// T-185: las peleas de misión y el paso Escapar en Pruebas_Misiones con entrada simulada (WASD, ratón, M, E, F, Espacio y Q).
    /// Todos los números salen de los assets y se imprimen. Atajos permitidos y anotados: Aceptar por código (hasta T-186), Colocar al
    /// jugador cerca de un punto, y dejar la vida del mecha en KO con daño por código (prueba d). El resto se juega: llamar al mecha,
    /// subir, encenderlo, pelear con el ratón, bajar, caminar, robar con E y vender en el mercado negro.
    /// </summary>
    public class MisionesPeleaPruebas : RecorridoEmboscadaBase
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string RutaDatosCalor = "Assets/_Proyecto/Progresion/Datos/DatosCalor_Base.asset";
        private const string RutaCapturaAPie = "docs/capturas/T-185-1.png";
        private const string RutaCapturaCabina = "docs/capturas/T-185-2.png";
        private const string IdRobo = "robo_bodega";
        private const string IdCallejon = "barrio_callejon";
        private const string IdPeleaSinJaula = "ilegal_pelea";
        private const string TextoLlamar = "Llama a tu mecha (M)";
        private const string PuntoBodega = "Mision_Botin_BodegaCorp";
        private const string PuntoTaquero = "Mision_Encargo_Taquero";
        private const string FilaChatarra = "CHATARRA";
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const int NivelEsperadoRobo = 2;
        private const float DistanciaInicial = 8f;
        private const float DistanciaParaUsar = 1.8f;
        private const float MaxSegundosAviso = 3f;
        private const float ToleranciaEnfriar = 1f;
        private const float DistanciaACasaDesdeElNorte = 14f;
        private const float DistanciaMinimaCasaBotin = 25f;
        private const float YawNorte = 0f;
        private const float YawSur = 180f;
        private const float DanioKOPorCodigo = 100000f;
        private const float DistanciaCapturaHud = 7f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const float DistanciaCamaraAtras = 9f;
        private const float AlturaCamaraAtras = 10f;

        private string carpeta;
        private readonly List<string> atajos = new List<string>();
        private readonly List<(TipoMision, bool)> terminadas = new List<(TipoMision, bool)>();
        private HudAPie hud;
        private ServicioMisiones misiones;
        private ServicioReputacion reputacion;
        private ServicioInventario inventario;
        private ServicioObjetivos objetivos;
        private PeleaDeMision peleas;
        private ICartera cartera;
        private DatosCalor datosCalor;
        private bool capturaHudHecha;

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "misiones_pelea_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            atajos.Clear();
            terminadas.Clear();
            capturaHudHecha = false;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        protected override string VariableCaptura => "CAPTURAR_T185";

        // ---- Preparación ----

        private IEnumerator CargarMisiones()
        {
            yield return CargarEscena(RutaEscena);
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            interaccion = Object.FindFirstObjectByType<MechaCyberpunk.Personaje.Interaccion.InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MechaCyberpunk.Mecha.Montaje.MontajeMecha>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            peleas = Object.FindFirstObjectByType<PeleaDeMision>();
            Assert.IsNotNull(jugador, "Pruebas_Misiones trae al jugador");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            Assert.IsNotNull(montaje, "Hay un mecha");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(peleas, "La escena trae PeleasDeMision");
            llamado = montaje.GetComponent<MechaCyberpunk.Mecha.Llamado.LlamadoMecha>();
            encendido = montaje.GetComponent<MechaCyberpunk.Mecha.Encendido.EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            pantallaEncendido = Object.FindFirstObjectByType<MechaCyberpunk.UI.Cabina.PantallaEncendido>(FindObjectsInactive.Include);
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mecha.ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            calor = Registro<ICalor>.Actual;
            misiones = Registro<ServicioMisiones>.Actual;
            reputacion = Registro<ServicioReputacion>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            cartera = Registro<ICartera>.Actual;
            datosCalor = AssetDatabase.LoadAssetAtPath<DatosCalor>(RutaDatosCalor);
            Assert.IsNotNull(calor, "Hay calor");
            Assert.IsNotNull(misiones, "Hay misiones");
            Assert.IsNotNull(reputacion, "Hay reputación");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.IsNotNull(objetivos, "Hay objetivos");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(datosCalor, "Existe DatosCalor_Base");
            Registro<IMisiones>.Actual.Terminada += (tipo, exito) => terminadas.Add((tipo, exito));
            saltarMedicion = true;
            for (int i = 0; i < FramesSinMedirTrasEscena; i++) yield return Frame();
            yield return Esperar(FramesParaElHud * 0.02f);
            EmpezarMedicion();
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");
            Assert.AreEqual(FranjaDia.Noche, Registro<IRelojMundo>.Actual.Franja, "empieza de noche");
        }

        private DatosMision Mision(string id)
        {
            DatosMision datos = misiones.Catalogo.Buscar(id);
            Assert.IsNotNull(datos, "el catálogo trae " + id);
            return datos;
        }

        private static IPuntoMision Punto(string id)
        {
            foreach (IPuntoMision p in Registro<IPuntoMision>.Todos)
            {
                if (p.Id == id) return p;
            }
            Assert.Fail("la escena no trae el punto " + id);
            return null;
        }

        private void Aceptar(DatosMision datos)
        {
            Assert.IsTrue(misiones.Aceptar(datos), "se acepta " + datos.Id + " (atajo: por código)");
            atajos.Add("Aceptar por código: " + datos.Id);
        }

        private void Colocar(Vector3 posicion, float yaw, string que)
        {
            jugador.Colocar(new Pose(new Vector3(posicion.x, AlturaJugador, posicion.z), Quaternion.Euler(0f, yaw, 0f)));
            atajos.Add("Colocar: " + que);
        }

        // Camina con W y el ratón a un punto de misión, espera que el HUD ofrezca el paso y pulsa E.
        private IEnumerator UsarPaso(string idPunto, string textoEsperado)
        {
            Vector3 destino = Punto(idPunto).Pose.position;
            Colocar(destino + new Vector3(0f, 0f, -DistanciaInicial), YawNorte, DistanciaInicial.ToString("F0", CultureInfo.InvariantCulture) + " m de " + idPunto);
            yield return Esperar(FramesParaElHud * 0.02f);
            yield return CaminarA(destino, DistanciaParaUsar, idPunto);
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != textoEsperado && Time.time < fin) yield return Frame();
            Assert.AreEqual(textoEsperado, hud.TextoAvisoInteraccion.text, "el HUD ofrece el paso");
            yield return Tecla(teclado.eKey);
            yield return Esperar(FramesParaElHud * 0.02f);
        }

        // Prepara el robo de la bodega hasta que se pulsa E en el botín. Devuelve la hora de juego en que se sumó el calor.
        private IEnumerator RobarBodega(DatosMision datos)
        {
            Aceptar(datos);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
        }

        private ILugar BuscarLugar(IdLugar id)
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == id) return lugar;
            }
            Assert.Fail("la escena no trae el lugar " + id);
            return null;
        }

        private float MultiplicadorCalor() =>
            ReglaReputacion.MultiplicadorCalor(reputacion.Datos, reputacion.Nivel(TipoReputacion.Corporaciones));

        private int NivelDeCalor(float valor)
        {
            int nivel = 0;
            float[] umbrales = datosCalor.UmbralesNivel;
            for (int i = 0; i < umbrales.Length; i++)
            {
                if (valor >= umbrales[i]) nivel = i + 1;
            }
            return nivel;
        }

        private void VerificarFinal()
        {
            AssertSinErrores();
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
            Debug.Log("[T-185] atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }

        private static bool HayRivalesEnLaEscena()
        {
            foreach (MonoBehaviour candidato in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Rival) return true;
            }
            return false;
        }

        // Una cámara temporal al sur y en alto de los pandilleros, mirándolos, a los pandilleros, con el HUD (el texto del objetivo).
        private void CapturarDesdeAtras(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1") return;
            Vector3 centro = PuntoMedioDeLosRivales();
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            objeto.transform.position = centro + new Vector3(0f, AlturaCamaraAtras, -DistanciaCamaraAtras);
            objeto.transform.LookAt(centro + Vector3.up);
            CapturarConHud(ruta, camara);
            Object.Destroy(objeto);
        }

        // Lo que ve la cámara con el HUD (los canvas de pantalla pasan un instante a esa cámara; sin cámara, la del jugador).
        private void CapturarConHud(string ruta, Camera camaraElegida = null)
        {
            Camera camaraDeCaptura = camaraElegida != null ? camaraElegida : jugador.Camara;
            var movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                lienzo.worldCamera = camaraDeCaptura;
                lienzo.planeDistance = DistanciaPlanoCanvas;
                movidos.Add(lienzo);
            }
            Capturar(ruta, camaraElegida);
            foreach (Canvas lienzo in movidos) lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Robo_ECogeElBotinSumaCalorYLlegarACasaLoCumple_ElPagoEsChatarraQueSeVendeEnElMercado()
        {
            yield return CargarMisiones();
            DatosMision datos = Mision(IdRobo);
            RecompensaMision esperada = ReglaMisiones.Recompensa(datos);
            PasoMision paso = datos.Pasos[0];
            float calorEsperado = paso.Calor * MultiplicadorCalor();
            int nivelEsperado = NivelDeCalor(calorEsperado);
            int ventaChatarra = esperada.Chatarra.ValorVenta;
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            int chatarra = inventario.Cantidad(esperada.Chatarra);
            var movimientos = new List<MotivoDinero>();
            cartera.Cambio += (cantidad, motivo) => movimientos.Add(motivo);
            Debug.Log("[T-185] esperado robo: calor " + calorEsperado.ToString("F1", CultureInfo.InvariantCulture) + " (nivel " + nivelEsperado +
                      "), pago $" + esperada.Pago + ", chatarra " + esperada.ChatarraCantidad + ", corporaciones " + esperada.DeltaCorporaciones +
                      ", barrio " + esperada.DeltaBarrio + ", venta $" + ventaChatarra + " c/u");
            Assert.AreEqual(NivelEsperadoRobo, nivelEsperado, "los 50 puntos del botín son nivel 2");
            Assert.AreEqual(0, esperada.Pago, "el robo no paga dinero");
            ILugar casa = BuscarLugar(IdLugar.Casa);
            Vector3 centroCasa = ((Component)casa).GetComponentInChildren<BoxCollider>().bounds.center;
            foreach (IPuntoMision punto in Registro<IPuntoMision>.Todos)
            {
                if (punto.Tipo == TipoPuntoMision.Botin)
                {
                    Assert.GreaterOrEqual(DistanciaPlana(centroCasa, punto.Pose.position), DistanciaMinimaCasaBotin, "la casa queda lejos del botín " + punto.Id);
                }
            }

            yield return RobarBodega(datos);

            Assert.AreEqual(calorEsperado, calor.Valor, 0.5f, "E en la bodega suma el calor del paso (puntos)");
            Assert.AreEqual(NivelEsperadoRobo, calor.Nivel, "nivel 2 de calor");
            Assert.IsTrue(misiones.HayMision, "la misión sigue");
            Assert.AreEqual(TipoPaso.Escapar, misiones.PasoActual.Tipo, "pasa al paso Escapar");
            Assert.AreEqual(ServicioMisiones.TextoEscapar, objetivos.Actual.Texto, "el objetivo dice cómo escapar");
            Assert.AreEqual(IdLugar.Casa, objetivos.Actual.Lugar, "el objetivo apunta a la casa");
            Assert.AreEqual(0, terminadas.Count, "todavía no termina");

            // Camina a la casa desde el norte (la zona de la casa avisa al entrar).
            Colocar(centroCasa + new Vector3(0f, 0f, DistanciaACasaDesdeElNorte), YawSur, DistanciaACasaDesdeElNorte.ToString("F0", CultureInfo.InvariantCulture) + " m al norte de la casa");
            yield return Esperar(FramesParaElHud * 0.02f);
            yield return CaminarA(centroCasa, DistanciaLlegada, "la casa", () => !misiones.HayMision);
            yield return Esperar(FramesParaElHud * 0.02f);

            Assert.IsFalse(misiones.HayMision, "llegar a casa cumple Escapar y termina el robo");
            CollectionAssert.AreEqual(new[] { (TipoMision.Robo, true) }, terminadas, "Terminada(Robo, true) una vez");
            Assert.IsTrue(casa.JugadorDentro, "el jugador está en la casa");
            Assert.AreEqual(dinero, cartera.Dinero, "pago $0: el dinero no cambia");
            CollectionAssert.DoesNotContain(movimientos, MotivoDinero.Mision, "ningún cobro de misión");
            Assert.AreEqual(chatarra + esperada.ChatarraCantidad, inventario.Cantidad(esperada.Chatarra), "+6 de chatarra");
            Assert.AreEqual(barrio + esperada.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "barrio +3 (puntos)");
            Assert.AreEqual(corporaciones + esperada.DeltaCorporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones -10 (puntos)");
            Assert.AreEqual(esperada.ChatarraCantidad, misiones.UltimoResumen.ChatarraCantidad, "resumen: chatarra");
            Assert.AreEqual(1, misiones.VecesCumplida(IdRobo));

            // e) La chatarra se vende en el mercado negro.
            MostradorTienda mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            Assert.IsNotNull(mostrador, "la escena trae el mostrador del mercado negro");
            PantallaTienda pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
            Colocar(mostrador.transform.position + new Vector3(0f, 0f, -4f), YawNorte, "4 m del mostrador del mercado negro");
            yield return Esperar(FramesParaElHud * 0.02f);
            yield return CaminarA(mostrador.transform.position, 2f, "el mostrador");
            yield return EsperarHasta(() => mostrador.PuedeUsar, MaxSegundosAviso, "el mostrador se ofrece");
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla de la tienda");
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantalla.Pestana, "flecha derecha pasa a VENDER");
            for (int i = 0; i < esperada.ChatarraCantidad; i++)
            {
                int fila = -1;
                for (int f = 0; f < pantalla.CantidadFilas; f++)
                {
                    if (pantalla.TextoFila(f).Contains(FilaChatarra)) fila = f;
                }
                Assert.GreaterOrEqual(fila, 0, "hay una fila de chatarra en la venta " + i);
                while (pantalla.IndiceElegido < fila) yield return Tecla(teclado.downArrowKey);
                yield return Tecla(teclado.enterKey);
            }
            Assert.AreEqual(dinero + esperada.ChatarraCantidad * ventaChatarra, cartera.Dinero, "6 x $50 de venta");
            Assert.AreEqual(chatarra, inventario.Cantidad(esperada.Chatarra), "ya no queda chatarra del robo");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Robo_EsperandoEscondidoHastaQueElCalorBajaACero_LoCumpleAlTiempoDelEnfriamiento()
        {
            yield return CargarMisiones();
            DatosMision datos = Mision(IdRobo);
            yield return RobarBodega(datos);
            float inicio = Time.time;
            float valor = calor.Valor;
            float esperado = datosCalor.RetrasoEnfriamiento + valor / datosCalor.EnfriamientoPorSegundo;
            Debug.Log("[T-185] esperado enfriar: " + valor.ToString("F1", CultureInfo.InvariantCulture) + " puntos, retraso " + datosCalor.RetrasoEnfriamiento +
                      " s, " + datosCalor.EnfriamientoPorSegundo + " puntos/s -> " + esperado.ToString("F1", CultureInfo.InvariantCulture) + " s");
            Assert.AreEqual(TipoPaso.Escapar, misiones.PasoActual.Tipo, "el robo está en Escapar");
            Assert.Greater(valor, 0f, "hay calor que perder");

            yield return EsperarHasta(() => !misiones.HayMision, esperado + 10f, "el calor baja a 0 y el robo termina");
            float tardo = Time.time - inicio;
            Debug.Log("[T-185] el calor llegó a 0 en " + tardo.ToString("F2", CultureInfo.InvariantCulture) + " s");

            Assert.AreEqual(esperado, tardo, ToleranciaEnfriar, "tarda el retraso más lo que cuesta enfriar (s)");
            Assert.AreEqual(0f, calor.Valor, 0.001f, "el calor llegó a 0");
            Assert.IsFalse(BuscarLugar(IdLugar.Casa).JugadorDentro, "no entró a casa: se escondió");
            CollectionAssert.AreEqual(new[] { (TipoMision.Robo, true) }, terminadas, "Terminada(Robo, true) una vez");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Escapar_ConElCalorEnCero_SeCumpleEnElSiguienteCuadro_NoDentroDeAceptar()
        {
            yield return CargarMisiones();
            DatosMision datos = DatosMision.Crear("prueba_escapar", "ESCAPAR", TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Cualquiera,
                new[] { new PasoMision(TipoPaso.Escapar, "", "x") });
            var orden = new List<string>();
            int cuadroAceptada = -1;
            int cuadroTerminada = -1;
            misiones.Aceptada += d =>
            {
                orden.Add("Aceptada");
                cuadroAceptada = Time.frameCount;
            };
            objetivos.Cambio += o =>
            {
                if (o.Texto == ServicioMisiones.TextoEscapar) orden.Add("Objetivo");
            };
            misiones.Terminada += (d, exito, r) =>
            {
                orden.Add("Terminada");
                cuadroTerminada = Time.frameCount;
            };
            Assert.AreEqual(0f, calor.Valor, "sin calor");

            Assert.IsTrue(misiones.Aceptar(datos), "se acepta (atajo: por código)");
            atajos.Add("Aceptar por código: prueba_escapar");
            Assert.IsTrue(misiones.HayMision, "no se cumple dentro de Aceptar: el paso se vive un cuadro");
            Assert.AreEqual(TipoPaso.Escapar, misiones.PasoActual.Tipo);
            Assert.AreEqual(ServicioMisiones.TextoEscapar, objetivos.Actual.Texto, "el objetivo ya dice cómo escapar");
            yield return Frame();
            yield return Frame();

            CollectionAssert.AreEqual(new[] { "Aceptada", "Objetivo", "Terminada" }, orden, "el orden de los eventos");
            Assert.Greater(cuadroTerminada, cuadroAceptada, "termina en un cuadro posterior al de aceptar");
            Assert.IsFalse(misiones.HayMision);
            Object.DestroyImmediate(datos);
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Callejon_LlamarAlMechaVencerALosDosPandillerosYAvisarAlTaquero_PagaCienYSubeElBarrio()
        {
            yield return CargarMisiones();
            DatosMision datos = Mision(IdCallejon);
            RecompensaMision esperada = ReglaMisiones.Recompensa(datos);
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            ServicioFavores favores = Registro<ServicioFavores>.Actual;
            int cantidadFavor = favores.Cantidad(esperada.Favor.Tipo);
            Debug.Log("[T-185] esperado callejón: pago $" + esperada.Pago + ", barrio +" + esperada.DeltaBarrio + ", favor " + esperada.Favor.Tipo);
            Assert.AreEqual(100, esperada.Pago);

            Aceptar(datos);
            yield return Frame();
            yield return Frame();
            Assert.IsTrue(peleas.HayPelea, "al empezar el paso Pelear se levanta la emboscada");
            Assert.AreEqual(TextoLlamar, objetivos.Actual.Texto, "a pie, el objetivo pide llamar al mecha");
            Assert.AreEqual(0, peleas.Director.Rivales.Count, "los pandilleros aún no aparecen");

            // Se acerca al callejón a pie; con ello aparecen los pandilleros.
            Vector3 puntoPelea = Punto("Mision_Pelea_Callejon").Pose.position;
            Colocar(new Vector3(0f, 0f, 6f), YawNorte, "calle norte-sur, a 25 m del callejón");
            yield return Esperar(FramesParaElHud * 0.02f);
            director = peleas.Director;
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "lejos del callejón no pasa nada");
            yield return CaminarA(new Vector3(puntoPelea.x - 1f, AlturaJugador, puntoPelea.z - 6f), DistanciaLlegada, "la boca del callejón",
                () => director.Estado != EstadoEmboscada.Esperando);
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "los pandilleros terminan de aparecer");
            Assert.AreEqual(2, director.Rivales.Count, "aparecen dos pandilleros");
            Assert.AreEqual(TextoLlamar, objetivos.Actual.Texto, "sigue pidiendo el mecha");
            yield return Esperar(FramesParaElHud * 0.02f);
            CapturarDesdeAtras(RutaCapturaAPie);

            // Llama al mecha: llega al punto de llamado de la calle, mirando al norte.
            Pose llegada = default;
            float masAlNorte = float.MinValue;
            foreach (IPuntoLlamadoMecha punto in Registro<IPuntoLlamadoMecha>.Todos)
            {
                if (punto.Llegada.position.z > masAlNorte)
                {
                    masAlNorte = punto.Llegada.position.z;
                    llegada = punto.Llegada;
                }
            }
            Vector3 desvioSubida = Quaternion.Inverse(montaje.transform.rotation) * (montaje.PuntoSubida - montaje.transform.position);
            juntoAlMecha = llegada.position + llegada.rotation * desvioSubida;
            juntoAlMecha.y = AlturaJugador;
            yield return LlamarYSubir();
            Assert.AreEqual(datos.Pasos[0].Texto, objetivos.Actual.Texto, "en el mecha, el objetivo dice el texto del paso");
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "con el mecha encendido los pandilleros pelean");

            alFrame = () =>
            {
                IPeleador rival = RivalEnPieMasCercano();
                if (capturaHudHecha || rival == null || DistanciaPlana(rival.Raiz.position, montaje.transform.position) > DistanciaCapturaHud) return;
                capturaHudHecha = true;
                CapturarConHud(RutaCapturaCabina);
            };
            yield return Pelear(null, null);
            alFrame = null;
            yield return Frame();

            Assert.IsFalse(peleas.HayPelea, "al vencer se retira la emboscada");
            Assert.IsTrue(misiones.HayMision, "falta avisar al taquero");
            Assert.AreEqual(1, misiones.IndicePaso, "pasa al paso del taquero");
            Assert.AreEqual(0, terminadas.Count, "todavía no termina");
            Assert.AreEqual(dinero, cartera.Dinero, "aún no se cobra");

            yield return BajarYDevolver();
            Vector3 taquero = Punto(PuntoTaquero).Pose.position;
            yield return CaminarA(new Vector3(taquero.x, AlturaJugador, taquero.z), DistanciaLlegada, "el taquero", () => !misiones.HayMision);
            yield return Esperar(FramesParaElHud * 0.02f);

            Assert.IsFalse(misiones.HayMision, "avisar al taquero termina la misión");
            CollectionAssert.AreEqual(new[] { (TipoMision.Barrio, true) }, terminadas, "Terminada(Barrio, true) una vez");
            Assert.AreEqual(dinero + esperada.Pago, cartera.Dinero, "pago $100");
            Assert.AreEqual(barrio + esperada.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "barrio +12 (puntos)");
            Assert.AreEqual(cantidadFavor + 1, favores.Cantidad(esperada.Favor.Tipo), "1 ESCONDITE");
            Assert.IsFalse(HayRivalesEnLaEscena(), "no quedan rivales");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator PeleaSinJaula_ConElMechaDelJugadorEnKO_FallaPorKOYNoDejaRivales()
        {
            yield return CargarMisiones();
            DatosMision datos = Mision(IdPeleaSinJaula);
            PenalizacionMision castigo = ReglaMisiones.Penalizacion(datos);
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            ResumenMision resumen = default;
            misiones.Terminada += (d, exito, r) => resumen = r;
            Debug.Log("[T-185] esperado KO: barrio " + castigo.Barrio + ", corporaciones " + castigo.Corporaciones + " (no sube corporaciones: " + datos.DeltaCorporaciones + ")");
            Assert.AreEqual(-3, castigo.Barrio, "la misión subía el barrio: -3");
            Assert.AreEqual(0, castigo.Corporaciones, "no subía corporaciones: sin penalización");

            Aceptar(datos);
            yield return Frame();
            yield return Frame();
            Assert.IsTrue(peleas.HayPelea, "se levanta la emboscada del baldío");
            director = peleas.Director;
            Vector3 puntoPelea = Punto("Mision_Pelea_Baldio").Pose.position;
            Colocar(new Vector3(puntoPelea.x - 7.5f, 0f, puntoPelea.z - 6f), YawNorte, "a 9 m del baldío");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 8f, "los rivales del baldío aparecen");
            Assert.AreEqual(3, director.Rivales.Count, "aparecen tres rivales");
            Assert.IsTrue(HayRivalesEnLaEscena(), "hay rivales en la escena");

            atajos.Add("RecibirDanioSinGuardia al mecha del jugador (KO por código)");
            vidaMecha.RecibirDanioSinGuardia(new InfoDanio(DanioKOPorCodigo, TipoDanio.Golpe, montaje.transform.position, Vector3.forward, null));
            Assert.IsTrue(vidaMecha.EstaKO, "el mecha del jugador cayó en KO");
            yield return Frame();
            yield return Frame();
            yield return Frame();

            Assert.IsFalse(misiones.HayMision, "la misión falla");
            CollectionAssert.AreEqual(new[] { (TipoMision.Ilegal, false) }, terminadas, "Terminada(Ilegal, false) una vez");
            Assert.AreEqual(MotivoFalloMision.KO, resumen.Motivo, "falló por KO");
            Assert.AreEqual(dinero, cartera.Dinero, "sin pago");
            Assert.AreEqual(barrio + castigo.Barrio, reputacion.Valor(TipoReputacion.Barrio), "barrio -3 (puntos)");
            Assert.AreEqual(corporaciones + castigo.Corporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones sin cambio (puntos)");
            Assert.AreEqual(0, misiones.VecesCumplida(IdPeleaSinJaula), "no cuenta como cumplida");
            Assert.IsFalse(peleas.HayPelea, "la emboscada se retiró");
            Assert.IsFalse(HayRivalesEnLaEscena(), "no queda ningún rival de la emboscada en la escena");
            foreach (IPeleador rival in director.Rivales)
            {
                Assert.IsFalse(Existe(rival), "el rival se destruyó");
            }
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator AbandonarLaMision_ConLaPeleaLevantada_RetiraLaEmboscadaSinRivales()
        {
            yield return CargarMisiones();
            DatosMision datos = Mision(IdPeleaSinJaula);
            Aceptar(datos);
            yield return Frame();
            yield return Frame();
            director = peleas.Director;
            Vector3 puntoPelea = Punto("Mision_Pelea_Baldio").Pose.position;
            Colocar(new Vector3(puntoPelea.x - 7.5f, 0f, puntoPelea.z - 6f), YawNorte, "a 9 m del baldío");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 8f, "los rivales aparecen");
            Assert.IsTrue(HayRivalesEnLaEscena());

            misiones.Abandonar();
            yield return Frame();
            yield return Frame();

            Assert.IsFalse(peleas.HayPelea, "la emboscada se retira al terminar la misión");
            Assert.IsFalse(HayRivalesEnLaEscena(), "no queda ningún rival");
            CollectionAssert.AreEqual(new[] { (TipoMision.Ilegal, false) }, terminadas);
            VerificarFinal();
        }
    }
}
