using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Misiones
{
    /// <summary>
    /// T-186: dónde salen las misiones, en Pruebas_Misiones con entrada simulada (WASD, E, T, flechas, Enter, Esc y J). Todos los números
    /// salen de los assets. Atajos permitidos y anotados: Colocar al jugador a 8 m del punto mirándolo, AvanzarHasta(21) para volver a la
    /// noche después de J (que lleva a las 8:00) y, en la prueba de la tienda, Colocar frente al mostrador.
    /// </summary>
    public class OrigenMisionesPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaMisiones_Base.asset";
        private const string VariableCaptura = "CAPTURAR_T186";
        private const string RutaCaptura1 = "docs/capturas/T-186-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-186-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-186-3.png";
        private const string IdTiendita = "barrio_tiendita";
        private const string IdEntrega = "corp_entrega";
        private const string IdCobro = "corp_cobro";
        private const string IdLimpieza = "corp_limpieza";
        private const string IdCarga = "ilegal_carga";
        private const string IdPelea = "ilegal_pelea";
        private const string IdBodega = "robo_bodega";
        private const string IdCamion = "robo_camion";
        private const string IdSoplo = "robo_soplo";
        private const string PuntoDonaLupe = "Mision_Encargo_DonaLupe";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const int HoraNoche = 21;
        private const int MaxPasosEnLista = 6;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaInicial = 8f;
        private const float DistanciaParaUsar = 1.8f;
        private const float DistanciaTienda = 2f;
        private const float AlturaInicio = 0.05f;
        private const float MaxSegundosCaminando = 12f;
        private const float MaxSegundosAviso = 3f;
        private const float SegundosIntentoCaminar = 1f;
        private const float ToleranciaPosicion = 0.02f;
        private const float ToleranciaPunto = 0.05f;
        private const float YawNorte = 0f;
        private const float YawSur = 180f;
        private const float DistanciaPlanoCanvas = 0.35f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private HashSet<GameObject> raicesIniciales;
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly MedidorFrames medidor = new MedidorFrames();
        private int framesSinMedir;

        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private HudAPie hud;
        private ServicioMisiones misiones;
        private ServicioReputacion reputacion;
        private ICartera cartera;
        private IRelojMundo reloj;
        private PantallaTelefono telefono;
        private PantallaMisiones lista;
        private ConfigPantallaMisiones config;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "origen_misiones_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            atajos.Clear();
            medidor.Reiniciar();
            framesSinMedir = 0;
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
        }

        // ---- Esperas ----

        private void Vigilar()
        {
            if (framesSinMedir > 0) framesSinMedir--;
            else medidor.Registrar(Time.unscaledDeltaTime);
        }

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                yield return null;
                Vigilar();
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Vigilar();
            Release(tecla);
            yield return null;
            Vigilar();
        }

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            framesSinMedir = FramesSinMedirTrasEscena;
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                Vigilar();
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            telefono = Object.FindFirstObjectByType<PantallaTelefono>();
            lista = Object.FindFirstObjectByType<PantallaMisiones>();
            Assert.IsNotNull(jugador, "Pruebas_Misiones trae al jugador");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(telefono, "El jugador trae el teléfono");
            Assert.IsNotNull(lista, "El jugador trae la pantalla de misiones");
            controlador = jugador.GetComponent<ControladorAPie>();
            misiones = Registro<ServicioMisiones>.Actual;
            reputacion = Registro<ServicioReputacion>.Actual;
            cartera = Registro<ICartera>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaMisiones>(RutaConfig);
            Assert.IsNotNull(misiones, "Hay misiones");
            Assert.IsNotNull(reputacion, "Hay reputación");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj de prueba");
            Assert.IsNotNull(config, "Existe la config de las pantallas de misiones");
            Assert.AreSame(lista, Registro<PantallaMisiones>.Actual, "la pantalla se encuentra por Registro");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");
            Assert.IsTrue(reloj.EsDeNoche, "la escena empieza de noche");
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

        private static float DistanciaPlana(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // Pone al jugador a 8 m del destino, mirándolo, y camina con W hasta quedar a menos de 1.8 m.
        private IEnumerator CaminarA(Vector3 destino, float yaw, string nombre)
        {
            Vector3 atras = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * DistanciaInicial;
            jugador.Colocar(new Pose(destino + atras + new Vector3(0f, AlturaInicio, 0f), Quaternion.Euler(0f, yaw, 0f)));
            atajos.Add("Colocar: a " + DistanciaInicial.ToString("F0", CultureInfo.InvariantCulture) + " m de " + nombre);
            yield return Cuadros(FramesParaElHud);
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminando;
            while (DistanciaPlana(jugador.Raiz.position, destino) > DistanciaParaUsar && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Release(teclado.wKey);
            yield return null;
            Vigilar();
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, destino), DistanciaParaUsar + 0.2f, "llegó caminando a " + nombre);
        }

        private IEnumerator EsperarTextoHud(string esperado)
        {
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != esperado && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(esperado, hud.TextoAvisoInteraccion.text, "el HUD dice lo que ofrece la E");
        }

        // Camina al punto, espera que el HUD ofrezca el texto del paso y pulsa E.
        private IEnumerator UsarPaso(string idPunto, string textoEsperado)
        {
            yield return CaminarA(Punto(idPunto).Pose.position, YawNorte, idPunto);
            yield return EsperarTextoHud(textoEsperado);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator AbrirTelefono()
        {
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(telefono.Abierto, "T abre el teléfono");
            Assert.IsFalse(controlador.ControlActivo, "con el teléfono abierto el control a pie queda congelado");
        }

        private IEnumerator CerrarTelefonoConT()
        {
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(telefono.Abierto, "T cierra el teléfono");
            Assert.IsTrue(controlador.ControlActivo, "al cerrar el control vuelve");
        }

        // Baja por la lista (↓) hasta que la fila elegida es la misión pedida.
        private IEnumerator ElegirEnLista(string id)
        {
            for (int i = 0; i < MaxPasosEnLista && lista.Oferta(lista.Indice).Datos.Id != id; i++)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            Assert.AreEqual(id, lista.Oferta(lista.Indice).Datos.Id, "la fila elegida es " + id);
        }

        private int FilaDe(string id)
        {
            for (int i = 0; i < lista.CantidadFilas; i++)
            {
                if (lista.Oferta(i).Datos.Id == id) return i;
            }
            Assert.Fail("la lista no trae " + id);
            return -1;
        }

        private void CapturarConHud(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1") return;
            Camera camara = jugador.Camara;
            var movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                lienzo.worldCamera = camara;
                lienzo.planeDistance = DistanciaPlanoCanvas;
                movidos.Add(lienzo);
            }
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
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            rt.Release();
            Object.Destroy(rt);
            foreach (Canvas lienzo in movidos) lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[T-186] captura en " + ruta);
        }

        private void VerificarFinal()
        {
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
            Debug.Log("[T-186] atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Telefono_DeNocheNadieContesta_DeDiaListaLasCorporativasAceptaYAbandonaConConfirmacion()
        {
            yield return CargarEscena();
            DatosMision entrega = Mision(IdEntrega);
            DatosMision limpieza = Mision(IdLimpieza);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int penalizacion = ReglaMisiones.Penalizacion(entrega).Corporaciones;
            Debug.Log("[T-186] esperado: " + entrega.Titulo + " límite " + entrega.LimiteSegundos + " s, castigo corporaciones " + penalizacion);

            // De noche: nadie contesta.
            yield return AbrirTelefono();
            Assert.AreEqual(PestanaTelefono.Contratos, telefono.Pestana, "abre en CONTRATOS");
            Assert.AreEqual(EstadoContratos.NadieContesta, telefono.EstadoContratos);
            Assert.AreEqual("NADIE CONTESTA · LLAMA DE DÍA", telefono.Contenido);
            Assert.IsFalse(lista.Visible, "de noche no hay lista de contratos");
            yield return CerrarTelefonoConT();

            // J lleva a las 8:00: de día.
            yield return Tecla(teclado.jKey);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja, "es de día tras la J");
            yield return AbrirTelefono();
            Assert.AreEqual(EstadoContratos.Lista, telefono.EstadoContratos);
            Assert.IsTrue(lista.Visible && lista.Embebida, "la lista va dentro del teléfono");
            Assert.AreEqual(3, lista.CantidadFilas, "las tres corporativas");
            int filaLimpieza = FilaDe(IdLimpieza);
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(filaLimpieza), "LIMPIEZA DE ZONA en gris");
            StringAssert.Contains(limpieza.Titulo, lista.TextoFila(filaLimpieza));
            StringAssert.Contains("NECESITAS SER CONTRATISTA CON LAS CORPORACIONES", lista.TextoFila(filaLimpieza), "con su motivo");
            int filaEntrega = FilaDe(IdEntrega);
            StringAssert.Contains(entrega.Titulo, lista.TextoFila(filaEntrega));
            StringAssert.Contains("$" + entrega.Pago, lista.TextoFila(filaEntrega), "el pago");
            StringAssert.Contains("BARRIO " + FormatoHud.Firmado(entrega.DeltaBarrio) + " · CORP. " + FormatoHud.Firmado(entrega.DeltaCorporaciones),
                lista.TextoFila(filaEntrega), "el efecto en reputación");
            CapturarConHud(RutaCaptura1);

            yield return ElegirEnLista(IdEntrega);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "Enter acepta la misión elegida");
            Assert.AreEqual(entrega.Titulo, misiones.Titulo);
            Assert.IsFalse(telefono.Abierto, "al aceptar se cierra el teléfono");
            Assert.IsTrue(controlador.ControlActivo, "y el control vuelve");

            // Pestaña MISIÓN.
            yield return AbrirTelefono();
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Mision, telefono.Pestana);
            StringAssert.Contains(entrega.Titulo, telefono.Contenido);
            StringAssert.Contains(entrega.Pasos[0].Texto, telefono.Contenido, "el paso actual");
            StringAssert.Contains("PASO 1/" + entrega.Pasos.Length, telefono.Contenido);
            StringAssert.Contains("TIEMPO 02:", telefono.Contenido, "el tiempo restante del límite de " + entrega.LimiteSegundos + " s");
            StringAssert.Contains("Enter · ABANDONAR", telefono.Contenido);

            // Abandonar pide confirmación.
            yield return Tecla(teclado.enterKey);
            Assert.IsTrue(telefono.Confirmando, "el primer Enter pide confirmar");
            Assert.IsTrue(misiones.HayMision, "todavía no la deja");
            StringAssert.Contains("¿ABANDONAR", telefono.Contenido);
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(telefono.Confirmando, "Esc cancela la confirmación");
            Assert.IsTrue(telefono.Abierto, "Esc de la confirmación no cierra el teléfono");
            yield return Tecla(teclado.enterKey);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(misiones.HayMision, "el segundo Enter abandona");
            Assert.AreEqual(corporaciones + penalizacion, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones −3 (puntos)");
            Assert.AreEqual(barrio, reputacion.Valor(TipoReputacion.Barrio), "barrio igual");
            Assert.AreEqual("CORPORACIONES " + FormatoHud.Firmado(penalizacion), hud.TextoAvisoReputacion.text, "el HUD lo dice");
            Assert.AreEqual("MISIÓN FALLIDA", hud.TextoAvisoMision.text);
            Assert.IsFalse(telefono.Abierto, "abandonar cierra el teléfono");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Calle_EEnDonaLupe_MuestraLaTienditaYAceptarApuntaALaFarmacia()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdTiendita);
            Vector3 farmacia = Punto(datos.Pasos[0].IdPunto).Pose.position;
            yield return CaminarA(Punto(PuntoDonaLupe).Pose.position, YawNorte, PuntoDonaLupe);
            yield return EsperarTextoHud("E · AYUDAR: " + datos.Titulo);
            Assert.IsTrue(((PuntoMisionDePrueba)Punto(PuntoDonaLupe)).Activo, "el punto se activa mientras ofrece una misión");

            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(lista.Visible, "E abre la pantalla de misiones");
            Assert.IsFalse(lista.Embebida);
            Assert.IsFalse(controlador.ControlActivo, "con la pantalla abierta el control queda congelado");
            Assert.AreEqual(1, lista.CantidadFilas, "solo la misión de ese vecino");
            StringAssert.Contains(datos.Titulo, lista.TextoFila(0));
            StringAssert.Contains("BARRIO +" + datos.DeltaBarrio, lista.TextoFila(0));
            StringAssert.Contains("FAVOR: " + datos.Favor.Nombre.ToUpperInvariant(), lista.TextoFila(0), "el favor DESCUENTO");
            StringAssert.Contains("$" + datos.Pago, lista.TextoFila(0));

            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "Enter acepta");
            Assert.AreEqual(datos.Id, misiones.Actual.Id);
            Assert.IsFalse(lista.Visible, "la pantalla se cierra al aceptar");
            Assert.IsTrue(controlador.ControlActivo, "el control vuelve");
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Assert.IsTrue(objetivos.HayObjetivo, "hay objetivo");
            Assert.Less(Vector3.Distance(farmacia, objetivos.Actual.Posicion), ToleranciaPunto, "el objetivo apunta a la farmacia");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Bar_DeDiaDiceQueLlegaDeNoche_DeNocheListaLasIlegalesYElSoploEnGris()
        {
            yield return CargarEscena();
            ContactoBar contacto = Object.FindFirstObjectByType<ContactoBar>();
            Assert.IsNotNull(contacto, "el bar trae al contacto");
            Vector3 donde = contacto.Posicion;

            yield return Tecla(teclado.jKey);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja, "es de día tras la J");
            yield return CaminarA(donde, YawSur, "el contacto del bar");
            yield return EsperarTextoHud("EL CONTACTO LLEGA DE NOCHE");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(lista.Visible, "de día la E no abre nada");
            Assert.IsTrue(controlador.ControlActivo, "y no congela");

            atajos.Add("AvanzarHasta(" + HoraNoche + ")");
            reloj.AvanzarHasta(HoraNoche);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(reloj.EsDeNoche, "ahora es de noche");
            yield return EsperarTextoHud("E · HABLAR CON EL CONTACTO");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(lista.Visible, "de noche la E abre la lista");
            Assert.AreEqual("CONTACTO DEL BAR", lista.Titulo);
            Assert.AreEqual(5, lista.CantidadFilas, "las cinco de origen Bar");
            foreach (string id in new[] { IdCarga, IdPelea, IdBodega })
            {
                int fila = FilaDe(id);
                StringAssert.Contains(Mision(id).Titulo, lista.TextoFila(fila));
                Assert.AreNotEqual(config.ColorApagado, lista.ColorFila(fila), id + " se puede aceptar");
            }
            int filaSoplo = FilaDe(IdSoplo);
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(filaSoplo), "EL SOPLO en gris");
            StringAssert.Contains("NECESITAS 1 INFORMACIÓN", lista.TextoFila(filaSoplo));
            int filaCamion = FilaDe(IdCamion);
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(filaCamion), "el camión pide liga 2");
            StringAssert.Contains("LIGA " + Mision(IdCamion).LigaMinima, lista.TextoFila(filaCamion));
            CapturarConHud(RutaCaptura3);

            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(lista.Visible, "Esc cierra");
            Assert.IsTrue(controlador.ControlActivo, "el control vuelve");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator CobroDeAdeudo_AvisaBarrioBajaCorporacionesSubeYLaReputacionMuestraMenos12()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdCobro);
            int dinero = cartera.Dinero;
            Debug.Log("[T-186] esperado cobro: pago " + datos.Pago + ", barrio " + datos.DeltaBarrio + ", corporaciones " + datos.DeltaCorporaciones);

            yield return Tecla(teclado.jKey);
            yield return AbrirTelefono();
            Assert.AreEqual(EstadoContratos.Lista, telefono.EstadoContratos);
            yield return ElegirEnLista(IdCobro);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "se acepta desde el teléfono");

            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            yield return UsarPaso(datos.Pasos[1].IdPunto, "E · ENTREGAR: " + datos.Pasos[1].Texto);

            Assert.IsFalse(misiones.HayMision, "terminó");
            Assert.AreEqual(dinero + datos.Pago, cartera.Dinero, "pago (que $)");
            Assert.AreEqual(datos.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "barrio (puntos)");
            Assert.AreEqual(datos.DeltaCorporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones (puntos)");
            Assert.AreEqual("BARRIO −12 · CORPORACIONES +10", hud.TextoAvisoReputacion.text, "el HUD avisa el cambio");
            Assert.IsTrue(hud.GrupoAvisoReputacion.activeSelf, "el aviso se ve");
            Assert.AreEqual("MISIÓN CUMPLIDA · +" + FormatoDinero.Texto(datos.Pago), hud.TextoAvisoMision.text);
            Assert.AreEqual("+" + FormatoDinero.Texto(datos.Pago) + " · MISIÓN", hud.TextoAvisoDinero.text, "el aviso de dinero dice MISIÓN");

            yield return AbrirTelefono();
            yield return Tecla(teclado.rightArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Reputacion, telefono.Pestana);
            StringAssert.Contains("BARRIO", telefono.TextoBarra(0));
            StringAssert.Contains("−12", telefono.TextoBarra(0), "el barrio en −12");
            StringAssert.Contains(reputacion.NombreNivel(TipoReputacion.Barrio), telefono.TextoBarra(0), "con el nombre del nivel");
            StringAssert.Contains("+10", telefono.TextoBarra(1));
            StringAssert.Contains(reputacion.NombreNivel(TipoReputacion.Corporaciones), telefono.TextoBarra(1));
            yield return Cuadros(FramesParaElHud);
            CapturarConHud(RutaCaptura2);
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator ConUnaPantallaAbierta_ElJugadorNoCaminaYConLaTiendaAbiertaTNoAbreElTelefono()
        {
            yield return CargarEscena();
            Vector3 antes = jugador.Raiz.position;
            yield return AbrirTelefono();
            Press(teclado.wKey);
            yield return new WaitForSeconds(SegundosIntentoCaminar);
            Release(teclado.wKey);
            yield return Cuadros(FramesParaElHud);
            Assert.Less(Vector3.Distance(antes, jugador.Raiz.position), ToleranciaPosicion, "con el teléfono abierto no camina (m)");
            yield return CerrarTelefonoConT();

            // La tienda del mercado negro: se abre con E y congela el control; con ella abierta, T no hace nada.
            MostradorTienda mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            Assert.IsNotNull(mostrador, "la escena trae el mostrador del mercado negro");
            Vector3 pos = mostrador.Posicion + new Vector3(0f, AlturaInicio, -DistanciaTienda);
            jugador.Colocar(new Pose(pos, Quaternion.Euler(0f, YawNorte, 0f)));
            atajos.Add("Colocar: frente al mostrador de la tienda");
            yield return Cuadros(FramesParaElHud);
            float fin = Time.time + MaxSegundosAviso;
            while (!hud.TextoAvisoInteraccion.text.StartsWith("E · ") && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            StringAssert.StartsWith("E · ", hud.TextoAvisoInteraccion.text, "el HUD ofrece la tienda");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(mostrador.PantallaAbierta, "la tienda se abrió");
            Assert.IsFalse(controlador.ControlActivo, "la tienda congela el control");
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(telefono.Abierto, "con la tienda abierta, T no abre el teléfono");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(mostrador.PantallaAbierta, "Esc cierra la tienda");
            yield return CerrarTelefonoSiSeAbrio();
            VerificarFinal();
        }

        // Si la tienda ya cerró y el control volvió, T vuelve a abrir el teléfono.
        private IEnumerator CerrarTelefonoSiSeAbrio()
        {
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(telefono.Abierto, "con la tienda cerrada T sí abre el teléfono");
            yield return CerrarTelefonoConT();
        }
    }
}
