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
    /// T-187: usar los favores, en Pruebas_Misiones con entrada simulada (WASD, E, T, flechas, Enter, Esc, F5 y F9). Todos los números
    /// salen de los assets. Atajos permitidos y anotados: Colocar al jugador a 8 m del punto mirándolo (o frente al mostrador) y dar o quitar
    /// favores, calor y dinero por código salvo donde la prueba dice que el favor viene de la misión.
    /// </summary>
    public class FavoresPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaMisiones_Base.asset";
        private const string VariableCaptura = "CAPTURAR_T187";
        private const string RutaCaptura1 = "docs/capturas/T-187-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-187-2.png";
        private const string IdTiendita = "barrio_tiendita";
        private const string IdEntrega = "corp_entrega";
        private const string IdCobro = "corp_cobro";
        private const string IdLimpieza = "corp_limpieza";
        private const string IdCarga = "ilegal_carga";
        private const string IdPelea = "ilegal_pelea";
        private const string IdBodega = "robo_bodega";
        private const string IdCamion = "robo_camion";
        private const string IdSoplo = "robo_soplo";
        private const string PuntoEscondite = "Mision_Escondite_Azotea";
        private const string TextoEsconderte = "E · ESCONDERTE (FAVOR)";
        private const string TextoSinFavor = "ESCONDITE · NECESITAS UN FAVOR";
        private const string FilaMisiles = "MISILES";
        private const int NivelCalorRobo = 2;
        private const float CalorDePrueba = 50f;
        private const string PuntoDonaLupe = "Mision_Encargo_DonaLupe";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const int HoraNoche = 21;
        private const int MaxPasosEnLista = 6;
        private const int PestanasHastaFavores = 3;
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
        private ServicioFavores favores;
        private ServicioInventario inventario;
        private ICalor calor;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "favores_" + Guid.NewGuid().ToString("N"));
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
            favores = Registro<ServicioFavores>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(favores, "Hay favores");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.IsNotNull(calor, "Hay calor");
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
            Debug.Log("[T-187] captura en " + ruta);
        }

        private void VerificarFinal()
        {
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
            Debug.Log("[T-187] atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }

        // ---- Ayudas de los favores ----

        // Acepta la misión de Doña Lupe con E y Enter, y la cumple con E en la farmacia y en la tienda (sin atajos de favor).
        private IEnumerator CumplirLaTiendita()
        {
            DatosMision datos = Mision(IdTiendita);
            yield return CaminarA(Punto(PuntoDonaLupe).Pose.position, YawNorte, PuntoDonaLupe);
            yield return EsperarTextoHud("E · AYUDAR: " + datos.Titulo);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "se aceptó " + IdTiendita);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            yield return UsarPaso(datos.Pasos[1].IdPunto, "E · ENTREGAR: " + datos.Pasos[1].Texto);
            Assert.IsFalse(misiones.HayMision, "terminó " + IdTiendita);
        }

        private IEnumerator AbrirTienda(MostradorTienda mostrador)
        {
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
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(mostrador.PantallaAbierta, "la tienda se abrió");
        }

        private static int FilaDeMisiles(PantallaTienda pantalla)
        {
            for (int f = 0; f < pantalla.CantidadFilas; f++)
            {
                if (pantalla.TextoFila(f).Contains(FilaMisiles)) return f;
            }
            Assert.Fail("la tienda no lista " + FilaMisiles);
            return -1;
        }

        private IEnumerator ElegirFila(PantallaTienda pantalla, int fila)
        {
            for (int i = 0; i < MaxPasosEnLista && pantalla.IndiceElegido != fila; i++)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            Assert.AreEqual(fila, pantalla.IndiceElegido, "la fila elegida");
        }

        private IEnumerator IrAPestanaFavores()
        {
            yield return AbrirTelefono();
            for (int i = 0; i < PestanasHastaFavores; i++) yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Favores, telefono.Pestana, "pestaña FAVORES");
        }

        private string LineaFavor(string nombre, int cantidad) => nombre + "   " + cantidad + "/" + favores.Maximo;

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Descuento_DadoPorLaTiendita_RebajaLaPrimeraCompraDeMisilesYLaSiguienteCuestaCompleta()
        {
            yield return CargarEscena();
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Descuento), "empieza sin favores");
            yield return CumplirLaTiendita();
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento), "a) Doña Lupe da 1 DESCUENTO");

            MostradorTienda mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            Assert.IsNotNull(mostrador, "la escena trae el mostrador del mercado negro");
            Assert.IsTrue(mostrador.AceptaFavorDescuento, "el mercado negro acepta el favor");
            PantallaTienda pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
            DatosObjeto misiles = null;
            foreach (DatosObjeto o in mostrador.EnVenta)
            {
                if (o.Nombre.ToUpperInvariant() == FilaMisiles) misiles = o;
            }
            Assert.IsNotNull(misiles, "el mercado vende misiles");
            int precio = misiles.Oferta.Precio;
            int rebajado = TiendaBasica.PrecioCon(misiles.Oferta, favores.Datos(TipoFavor.Descuento).Valor);
            int necesita = precio + rebajado;
            if (cartera.Dinero < necesita)
            {
                cartera.Abonar(necesita - cartera.Dinero, MotivoDinero.Otro);
                atajos.Add("Abonar dinero por código");
            }
            int dinero = cartera.Dinero;
            int lleva = inventario.Cantidad(misiles);
            Debug.Log("[T-187] esperado: misiles $" + precio + " -> $" + rebajado + ", dinero " + dinero);
            Assert.AreEqual(80, precio, "los misiles cuestan Ƀ80 en los datos");
            Assert.AreEqual(56, rebajado, "con 30 % cuestan Ƀ56");

            yield return AbrirTienda(mostrador);
            int fila = FilaDeMisiles(pantalla);
            yield return ElegirFila(pantalla, fila);
            StringAssert.Contains("Ƀ80 → Ƀ56", pantalla.TextoFila(fila), "la fila muestra el precio rebajado");
            Assert.AreEqual("FAVOR · −30 % EN ESTA COMPRA · TE QUEDAN 1", pantalla.Favor, "la línea del favor");
            CapturarConHud(RutaCaptura1);

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dinero - rebajado, cartera.Dinero, "la primera compra cobra Ƀ56");
            Assert.AreEqual(lleva + 1, inventario.Cantidad(misiles), "entró 1 misil");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Descuento), "el favor se gastó");
            Assert.AreEqual(string.Empty, pantalla.Favor, "la línea del favor se apaga");
            StringAssert.DoesNotContain("→", pantalla.TextoFila(fila), "ya no hay rebaja");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dinero - rebajado - precio, cartera.Dinero, "la segunda compra cobra Ƀ80");
            Assert.AreEqual(lleva + 2, inventario.Cantidad(misiles), "entró otro misil");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);

            yield return IrAPestanaFavores();
            StringAssert.Contains(LineaFavor("DESCUENTO", 0), telefono.Contenido, "FAVORES dice 0/3");
            StringAssert.Contains("EL MERCADO", telefono.Contenido);
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Descuento_SinDineroParaElRebajado_NoGastaElFavor()
        {
            yield return CargarEscena();
            favores.Dar(TipoFavor.Descuento);
            atajos.Add("Dar favor DESCUENTO por código");
            MostradorTienda mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            PantallaTienda pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
            const int pobre = 10;
            cartera.IntentarPagar(cartera.Dinero - pobre, MotivoDinero.Otro);
            atajos.Add("Quitar dinero por código");
            Assert.AreEqual(pobre, cartera.Dinero);
            yield return AbrirTienda(mostrador);
            yield return ElegirFila(pantalla, FilaDeMisiles(pantalla));
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(pobre, cartera.Dinero, "no cobró");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento), "sin dinero el favor no se gasta");
            Assert.AreEqual("NO TE ALCANZA", pantalla.Aviso, "avisa que no alcanza");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Escondite_ConFavorBorraElCalorYCumpleElEscaparSinFavorSoloAvisa()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdBodega);
            favores.Dar(TipoFavor.Escondite);
            atajos.Add("Dar favor ESCONDITE por código");
            Assert.IsTrue(misiones.Aceptar(datos), "se acepta el robo (atajo: por código)");
            atajos.Add("Aceptar por código: " + IdBodega);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            Assert.Greater(calor.Valor, 0f, "el botín sube el calor");
            Assert.AreEqual(NivelCalorRobo, calor.Nivel, "calor nivel 2");
            Assert.AreEqual(TipoPaso.Escapar, misiones.PasoActual.Tipo, "la misión está en Escapar");

            IPuntoMision escondite = Punto(PuntoEscondite);
            Assert.AreEqual(TipoPuntoMision.Escondite, escondite.Tipo);
            yield return CaminarA(escondite.Pose.position, YawNorte, PuntoEscondite);
            yield return EsperarTextoHud(TextoEsconderte);
            Assert.IsTrue(((PuntoMisionDePrueba)escondite).Activo, "el escondite se activa mientras hay calor");
            CapturarConHud(RutaCaptura2);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(0f, calor.Valor, 0.001f, "b) el calor quedó en 0");
            Assert.IsFalse(misiones.HayMision, "b) la misión se cumplió en el escondite");
            Assert.AreEqual(1, misiones.VecesCumplida(IdBodega));
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Escondite), "b) ESCONDITE 0/3");
            Assert.IsFalse(((PuntoMisionDePrueba)escondite).Activo, "sin calor el escondite se apaga");

            // Repetir sin favor: hay calor, pero el escondite pide un favor y la E no hace nada.
            calor.Sumar(CalorDePrueba, MotivoCalor.Otro);
            atajos.Add("Sumar calor por código");
            yield return EsperarTextoHud(TextoSinFavor);
            float antes = calor.Valor;
            Assert.Greater(antes, 0f);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(antes, calor.Valor, 0.5f, "sin favor la E no borra el calor");
            Assert.AreEqual(TextoSinFavor, hud.TextoAvisoInteraccion.text, "sigue el aviso, sin E");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Escondite_SinCalor_NoSeOfrece()
        {
            yield return CargarEscena();
            favores.Dar(TipoFavor.Escondite);
            atajos.Add("Dar favor ESCONDITE por código");
            Assert.AreEqual(0f, calor.Valor);
            IPuntoMision escondite = Punto(PuntoEscondite);
            yield return CaminarA(escondite.Pose.position, YawNorte, PuntoEscondite);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(string.Empty, hud.TextoAvisoInteraccion.text, "sin calor el escondite no dice nada");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Escondite), "el favor sigue ahí");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Informacion_ElSoploPideUnaInformacionLaGastaAlAceptarYAbandonarNoLaDevuelve()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdSoplo);
            ContactoBar contacto = Object.FindFirstObjectByType<ContactoBar>();
            Assert.IsNotNull(contacto, "el bar trae al contacto");
            RecompensaMision esperada = ReglaMisiones.Recompensa(datos);
            int chatarra = inventario.Cantidad(esperada.Chatarra);
            Debug.Log("[T-187] esperado: " + datos.Titulo + " da " + esperada.ChatarraCantidad + " " + esperada.Chatarra.Nombre);

            // Sin favor sigue en gris.
            yield return CaminarA(contacto.Posicion, YawSur, "el contacto del bar");
            yield return EsperarTextoHud("E · HABLAR CON EL CONTACTO");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(FilaDe(IdSoplo)), "sin favor EL SOPLO en gris");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);

            favores.Dar(TipoFavor.Informacion);
            atajos.Add("Dar favor INFORMACION por código");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(lista.Visible, "la lista del contacto está abierta");
            int fila = FilaDe(IdSoplo);
            Assert.AreNotEqual(config.ColorApagado, lista.ColorFila(fila), "con el favor se puede aceptar");
            StringAssert.Contains("USA 1 INFORMACIÓN", lista.TextoFila(fila), "la fila dice que gasta el favor");
            yield return ElegirEnLista(IdSoplo);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "c) EL SOPLO se aceptó");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Informacion), "c) INFORMACIÓN 0/3");

            // Abandonar no devuelve el favor (D-tony-146).
            misiones.Abandonar();
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(misiones.HayMision);
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Informacion), "abandonar no devuelve el favor");

            // Con otro favor se vuelve a aceptar y se cumple: da la chatarra.
            favores.Dar(TipoFavor.Informacion);
            atajos.Add("Dar favor INFORMACION por código");
            Assert.IsTrue(misiones.Aceptar(datos), "se acepta otra vez (atajo: por código)");
            atajos.Add("Aceptar por código: " + IdSoplo);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            calor.Borrar();
            atajos.Add("Borrar calor por código");
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(misiones.HayMision, "el soplo terminó");
            Assert.AreEqual(esperada.ChatarraCantidad, 12, "los datos dan 12 de chatarra");
            Assert.AreEqual(chatarra + esperada.ChatarraCantidad, inventario.Cantidad(esperada.Chatarra), "c) da 12 de chatarra");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Favores_ConTresDescuentosYLaTienditaCumplida_SigueEnTresYElAvisoDiceFavorPerdido()
        {
            yield return CargarEscena();
            for (int i = 0; i < favores.Maximo; i++) favores.Dar(TipoFavor.Descuento);
            atajos.Add("Dar 3 favores DESCUENTO por código");
            Assert.AreEqual(favores.Maximo, favores.Cantidad(TipoFavor.Descuento));
            yield return CumplirLaTiendita();
            Assert.AreEqual(favores.Maximo, favores.Cantidad(TipoFavor.Descuento), "d) sigue en 3");
            Assert.IsTrue(misiones.UltimoResumen.FavorPerdido, "el resumen marca el favor perdido");

            yield return AbrirTelefono();
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Mision, telefono.Pestana);
            StringAssert.Contains("FAVOR PERDIDO: YA TIENES " + favores.Maximo, telefono.Contenido, "d) el aviso de fin");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Favores_GuardarConF5CambiarPorCodigoYCargarConF9_VuelvenLosConteosGuardados()
        {
            yield return CargarEscena();
            favores.Dar(TipoFavor.Descuento);
            favores.Dar(TipoFavor.Descuento);
            favores.Dar(TipoFavor.Informacion);
            atajos.Add("Dar favores por código");
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(File.Exists(ServicioGuardado.RutaForzada), "F5 escribió la partida");

            favores.Gastar(TipoFavor.Descuento);
            favores.Gastar(TipoFavor.Informacion);
            favores.Dar(TipoFavor.Escondite);
            atajos.Add("Cambiar favores por código");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento));

            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(2, favores.Cantidad(TipoFavor.Descuento), "e) DESCUENTO 2");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Informacion), "e) INFORMACIÓN 1");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Escondite), "e) ESCONDITE 0");

            yield return IrAPestanaFavores();
            StringAssert.Contains(LineaFavor("DESCUENTO", 2), telefono.Contenido);
            StringAssert.Contains(LineaFavor("INFORMACIÓN", 1), telefono.Contenido);
            StringAssert.Contains(LineaFavor("ESCONDITE", 0), telefono.Contenido);
            StringAssert.Contains("ESCONDITES DEL BARRIO", telefono.Contenido);
            StringAssert.Contains("CONTACTO DEL BAR", telefono.Contenido);
            VerificarFinal();
        }
    }
}
