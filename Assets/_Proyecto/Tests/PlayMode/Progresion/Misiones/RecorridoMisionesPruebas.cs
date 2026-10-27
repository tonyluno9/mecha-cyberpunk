using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Misiones;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Tests.Pelea;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
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
    /// T-188: el recorrido de las misiones y la reputación (cierre del bloque 5 de T-F7) en Pruebas_Misiones, en Libre, con entrada
    /// simulada (caminar, E, T, flechas, Enter, Esc, J, M, F, Espacio, Q, F5, F9 y ratón). Un día y una noche de trabajos: ayudar a Doña
    /// Lupe, un contrato corporativo por teléfono, el robo de la bodega con escondite, vender la chatarra, el descuento, defender al taquero
    /// en el mecha, y guardar y cargar. Todos los números salen de los assets. Atajos permitidos y anotados: Colocar al jugador cerca de un
    /// punto, dinero inicial por código, AvanzarHasta(21) para ir a la noche (J solo lleva a las 8:00), el favor ESCONDITE dado por código
    /// en el paso d (el real se gana en el f), ServicioLigas.Poner para abrir la liga 2 en el paso h, y en el paso i los cambios por código
    /// antes de cargar.
    /// </summary>
    public class RecorridoMisionesPruebas : RecorridoEmboscadaBase
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaMisiones_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaCaptura1 = "docs/capturas/T-188-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-188-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-188-3.png";
        private const string IdTiendita = "barrio_tiendita";
        private const string IdLuz = "barrio_luz";
        private const string IdCallejon = "barrio_callejon";
        private const string IdEntrega = "corp_entrega";
        private const string IdCobro = "corp_cobro";
        private const string IdLimpieza = "corp_limpieza";
        private const string IdBodega = "robo_bodega";
        private const string PuntoDonaLupe = "Mision_Encargo_DonaLupe";
        private const string PuntoVecindad = "Mision_Encargo_Vecindad";
        private const string PuntoTaquero = "Mision_Encargo_Taquero";
        private const string PuntoPelea = "Mision_Pelea_Callejon";
        private const string PuntoEscondite = "Mision_Escondite_Azotea";
        private const string TextoLlamar = "Llama a tu mecha (M)";
        private const string TextoEsconderte = "E · ESCONDERTE (FAVOR)";
        private const string TextoHechaHoy = "YA LO HICISTE HOY";
        private const string TextoConocido = "AHORA ERES CONOCIDO EN EL BARRIO";
        private const string FilaMisiles = "MISILES";
        private const string FilaChatarra = "CHATARRA";
        private const int DineroInicial = 1000;
        private const int HoraNoche = 21;
        private const int NivelCalorRobo = 2;
        private const int NivelConocido = 1;
        private const int LigaDosAbierta = 2;
        private const int MaxPasosEnLista = 6;
        private const int PestanasHastaReputacion = 2;
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const int ModificacionBarrio = -30;
        private const int ModificacionDinero = 777;
        private const float DistanciaInicial = 8f;
        private const float DistanciaParaUsar = 1.8f;
        private const float DistanciaTienda = 2f;
        private const float MaxSegundosCaminando = 12f;
        private const float MaxSegundosAviso = 3f;
        private const float MaxSegundosAvisoReputacion = 12f;
        private const float DistanciaACamaraLetrero = 9f;
        private const float AlturaCamaraLetrero = 3.5f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const float DistanciaCapturaHud = 7f;
        private const float YawNorte = 0f;
        private const float YawSur = 180f;

        private string carpeta;
        private HashSet<GameObject> raicesIniciales;
        private readonly List<string> atajos = new List<string>();
        private readonly List<string> tiempos = new List<string>();
        private readonly List<(TipoMision, bool)> terminadas = new List<(TipoMision, bool)>();
        private float inicioPaso;
        private bool capturaHudHecha;

        private HudAPie hud;
        private ControladorAPie controlador;
        private ServicioMisiones misiones;
        private ServicioReputacion reputacion;
        private ServicioFavores favores;
        private ServicioInventario inventario;
        private ServicioLigas ligas;
        private ServicioObjetivos objetivos;
        private PeleaDeMision peleas;
        private ICartera cartera;
        private IRelojMundo reloj;
        private PantallaTelefono telefono;
        private PantallaMisiones lista;
        private ConfigPantallaMisiones config;
        private CalendarioLigas calendario;
        private ReaccionReputacionDePrueba reaccion;
        private MostradorTienda mostrador;
        private ContactoBar contacto;

        protected override string VariableCaptura => "CAPTURAR_T188";

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_misiones_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            atajos.Clear();
            tiempos.Clear();
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

        // ---- Preparación ----

        private IEnumerator CargarMisiones()
        {
            yield return CargarEscena(RutaEscena);
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            interaccion = Object.FindFirstObjectByType<MechaCyberpunk.Personaje.Interaccion.InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MechaCyberpunk.Mecha.Montaje.MontajeMecha>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            peleas = Object.FindFirstObjectByType<PeleaDeMision>();
            telefono = Object.FindFirstObjectByType<PantallaTelefono>();
            lista = Object.FindFirstObjectByType<PantallaMisiones>();
            reaccion = Object.FindFirstObjectByType<ReaccionReputacionDePrueba>();
            mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            contacto = Object.FindFirstObjectByType<ContactoBar>();
            Assert.IsNotNull(jugador, "Pruebas_Misiones trae al jugador");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            Assert.IsNotNull(montaje, "Hay un mecha");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(peleas, "La escena trae PeleasDeMision");
            Assert.IsNotNull(telefono, "El jugador trae el teléfono");
            Assert.IsNotNull(lista, "El jugador trae la pantalla de misiones");
            Assert.IsNotNull(reaccion, "La escena trae el letrero de reputación");
            Assert.IsNotNull(mostrador, "La escena trae el mercado negro");
            Assert.IsNotNull(contacto, "El bar trae al contacto");
            controlador = jugador.GetComponent<ControladorAPie>();
            llamado = montaje.GetComponent<MechaCyberpunk.Mecha.Llamado.LlamadoMecha>();
            encendido = montaje.GetComponent<MechaCyberpunk.Mecha.Encendido.EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            pantallaEncendido = Object.FindFirstObjectByType<MechaCyberpunk.UI.Cabina.PantallaEncendido>(FindObjectsInactive.Include);
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mecha.ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            calor = Registro<ICalor>.Actual;
            misiones = Registro<ServicioMisiones>.Actual;
            reputacion = Registro<ServicioReputacion>.Actual;
            favores = Registro<ServicioFavores>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            cartera = Registro<ICartera>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaMisiones>(RutaConfig);
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            Assert.IsNotNull(calor, "Hay calor");
            Assert.IsNotNull(misiones, "Hay misiones");
            Assert.IsNotNull(reputacion, "Hay reputación");
            Assert.IsNotNull(favores, "Hay favores");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.IsNotNull(ligas, "Hay ligas");
            Assert.IsNotNull(objetivos, "Hay objetivos");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj");
            Assert.IsNotNull(config, "Existe la config de las pantallas de misiones");
            Assert.IsNotNull(calendario, "Existe el calendario de ligas");
            Registro<IMisiones>.Actual.Terminada += (tipo, exito) => terminadas.Add((tipo, exito));
            saltarMedicion = true;
            for (int i = 0; i < FramesSinMedirTrasEscena; i++) yield return Frame();
            yield return Esperar(FramesParaElHud * 0.02f);
            EmpezarMedicion();
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");
            Assert.IsTrue(reloj.EsDeNoche, "empieza de noche");
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

        private void Colocar(Vector3 posicion, float yaw, string que)
        {
            jugador.Colocar(new Pose(new Vector3(posicion.x, AlturaJugador, posicion.z), Quaternion.Euler(0f, yaw, 0f)));
            atajos.Add("Colocar: " + que);
        }

        private void Marcar(string paso)
        {
            tiempos.Add(paso + "=" + (Time.time - inicioPaso).ToString("F1", CultureInfo.InvariantCulture) + " s");
            inicioPaso = Time.time;
        }

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++) yield return Frame();
        }

        // Pone al jugador a 8 m del destino, mirándolo, y camina con W hasta quedar a menos de 1.8 m.
        private IEnumerator IrA(Vector3 destino, float yaw, string nombre)
        {
            Vector3 atras = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * DistanciaInicial;
            Colocar(destino + atras, yaw, DistanciaInicial.ToString("F0", CultureInfo.InvariantCulture) + " m de " + nombre);
            yield return Cuadros(FramesParaElHud);
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminando;
            while (DistanciaPlana(jugador.transform.position, destino) > DistanciaParaUsar && Time.time < fin) yield return Frame();
            Release(teclado.wKey);
            yield return Frame();
            Assert.LessOrEqual(DistanciaPlana(jugador.transform.position, destino), DistanciaParaUsar + 0.2f, "llegó caminando a " + nombre);
        }

        private IEnumerator EsperarTextoHud(string esperado)
        {
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != esperado && Time.time < fin) yield return Frame();
            Assert.AreEqual(esperado, hud.TextoAvisoInteraccion.text, "el HUD dice lo que ofrece la E");
        }

        private IEnumerator UsarPaso(string idPunto, string textoEsperado)
        {
            yield return IrA(Punto(idPunto).Pose.position, YawNorte, idPunto);
            yield return EsperarTextoHud(textoEsperado);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
        }

        // E en un encargo de la calle, Enter para aceptar la única oferta.
        private IEnumerator AceptarEnLaCalle(string idPunto, DatosMision datos)
        {
            yield return IrA(Punto(idPunto).Pose.position, YawNorte, idPunto);
            yield return EsperarTextoHud("E · AYUDAR: " + datos.Titulo);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(lista.Visible, "E abre la lista del encargo");
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "se aceptó " + datos.Id);
            Assert.AreEqual(datos.Id, misiones.Actual.Id);
        }

        private IEnumerator AbrirTelefono()
        {
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(telefono.Abierto, "T abre el teléfono");
            Assert.IsFalse(controlador.ControlActivo, "con el teléfono abierto el control a pie queda congelado");
        }

        private IEnumerator CerrarTelefono()
        {
            yield return Tecla(teclado.tKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(telefono.Abierto, "T cierra el teléfono");
            Assert.IsTrue(controlador.ControlActivo, "al cerrar el control vuelve");
        }

        private IEnumerator ElegirEnLista(string id)
        {
            for (int i = 0; i < MaxPasosEnLista && lista.Oferta(lista.Indice).Datos.Id != id; i++) yield return Tecla(teclado.downArrowKey);
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

        private IEnumerator EsperarAvisoReputacionApagado()
        {
            yield return EsperarHasta(() => !hud.GrupoAvisoReputacion.activeSelf, MaxSegundosAvisoReputacion, "el aviso de reputación anterior se apaga");
        }

        private IEnumerator AbrirTienda()
        {
            Colocar(mostrador.Posicion + new Vector3(0f, 0f, -DistanciaTienda), YawNorte, "frente al mostrador del mercado negro");
            yield return Cuadros(FramesParaElHud);
            yield return EsperarHasta(() => hud.TextoAvisoInteraccion.text.StartsWith("E · "), MaxSegundosAviso, "el HUD ofrece la tienda");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(mostrador.PantallaAbierta, "la tienda se abrió");
        }

        private static int FilaConTexto(PantallaTienda pantalla, string texto)
        {
            for (int f = 0; f < pantalla.CantidadFilas; f++)
            {
                if (pantalla.TextoFila(f).Contains(texto)) return f;
            }
            return -1;
        }

        private IEnumerator ElegirFila(PantallaTienda pantalla, int fila)
        {
            for (int i = 0; i < MaxPasosEnLista && pantalla.IndiceElegido != fila; i++)
            {
                yield return Tecla(pantalla.IndiceElegido < fila ? teclado.downArrowKey : teclado.upArrowKey);
            }
            Assert.AreEqual(fila, pantalla.IndiceElegido, "la fila elegida");
        }

        private static bool HayRivalesEnLaEscena()
        {
            foreach (MonoBehaviour candidato in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Rival) return true;
            }
            return false;
        }

        private void CapturarConHud(string ruta, Camera camaraElegida = null)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1") return;
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

        // Una cámara temporal frente al letrero de reputación, mirándolo desde el oeste.
        private void CapturarLetrero(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1") return;
            Vector3 muro = reaccion.transform.position;
            GameObject objeto = new GameObject("CamaraLetrero");
            Camera camara = objeto.AddComponent<Camera>();
            objeto.transform.position = new Vector3(muro.x - DistanciaACamaraLetrero, AlturaCamaraLetrero, muro.z);
            objeto.transform.LookAt(new Vector3(muro.x, AlturaCamaraLetrero - 0.5f, muro.z));
            Capturar(ruta, camara);
            Object.Destroy(objeto);
        }

        private float MultiplicadorCalor() =>
            ReglaReputacion.MultiplicadorCalor(reputacion.Datos, reputacion.Nivel(TipoReputacion.Corporaciones));

        // ---- El recorrido ----

        [UnityTest]
        public IEnumerator Recorrido_UnDiaYUnaNocheDeTrabajos_LaReputacionLosFavoresYElDineroCuadran()
        {
            yield return CargarMisiones();
            inicioPaso = Time.time;
            float inicioTotal = Time.time;

            DatosMision tiendita = Mision(IdTiendita);
            DatosMision luz = Mision(IdLuz);
            DatosMision callejon = Mision(IdCallejon);
            DatosMision entrega = Mision(IdEntrega);
            DatosMision cobro = Mision(IdCobro);
            DatosMision limpieza = Mision(IdLimpieza);
            DatosMision bodega = Mision(IdBodega);
            RecompensaMision rTiendita = ReglaMisiones.Recompensa(tiendita);
            RecompensaMision rCobro = ReglaMisiones.Recompensa(cobro);
            RecompensaMision rBodega = ReglaMisiones.Recompensa(bodega);
            RecompensaMision rCallejon = ReglaMisiones.Recompensa(callejon);
            DatosObjeto chatarra = rBodega.Chatarra;
            Assert.IsNotNull(chatarra, "el robo da chatarra");
            DatosObjeto misiles = null;
            foreach (DatosObjeto o in mostrador.EnVenta)
            {
                if (o.Nombre.ToUpperInvariant() == FilaMisiles) misiles = o;
            }
            Assert.IsNotNull(misiles, "el mercado vende misiles");
            int precioMisil = misiles.Oferta.Precio;
            int descuentoMisil = TiendaBasica.PrecioCon(misiles.Oferta, favores.Datos(TipoFavor.Descuento).Valor);
            int ventaChatarra = rBodega.ChatarraCantidad * chatarra.ValorVenta;
            CartelPelea primerRival = calendario.Cartel(1, 1);
            int premioPrimerRival = primerRival.premioGanar;

            // Lo que se espera al final, desde los assets.
            int dineroEsperado = DineroInicial + rTiendita.Pago + rCobro.Pago + ventaChatarra - descuentoMisil + rCallejon.Pago + rTiendita.Pago;
            int barrioEsperado = tiendita.DeltaBarrio + cobro.DeltaBarrio + bodega.DeltaBarrio + callejon.DeltaBarrio + tiendita.DeltaBarrio;
            int corporacionesEsperadas = cobro.DeltaCorporaciones + bodega.DeltaCorporaciones;
            int misilesEsperados = 1;
            Debug.Log("[T-188] esperado final: dinero " + dineroEsperado + ", barrio " + barrioEsperado + ", corporaciones " + corporacionesEsperadas +
                      ", misiles +" + misilesEsperados + ", DESCUENTO 1, ESCONDITE 1, INFORMACION 0, chatarra 0 | tiendita $" + rTiendita.Pago +
                      " vs rival 1 de la liga 1 $" + premioPrimerRival + " | misil $" + precioMisil + " -> $" + descuentoMisil + " | venta $" + ventaChatarra);

            // a) Inicio de noche.
            int misilesAntes = inventario.Cantidad(misiles);
            if (cartera.Dinero < DineroInicial) cartera.Abonar(DineroInicial - cartera.Dinero, MotivoDinero.Otro);
            else if (cartera.Dinero > DineroInicial) cartera.IntentarPagar(cartera.Dinero - DineroInicial, MotivoDinero.Otro);
            atajos.Add("Dinero inicial fijado en " + DineroInicial + " por código");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "a) dinero fijado");
            Assert.AreEqual(0, reputacion.Valor(TipoReputacion.Barrio), "a) barrio 0");
            Assert.AreEqual(0, reputacion.Valor(TipoReputacion.Corporaciones), "a) corporaciones 0");
            foreach (TipoFavor tipo in Enum.GetValues(typeof(TipoFavor)))
            {
                Assert.AreEqual(0, favores.Cantidad(tipo), "a) sin favores " + tipo);
            }
            Assert.AreEqual(0, inventario.Cantidad(chatarra), "a) sin chatarra");
            Assert.AreEqual(string.Empty, reaccion.Texto, "a) el letrero está vacío");
            Assert.AreEqual(0f, calor.Valor, "a) sin calor");
            Marcar("a");

            // b) Ayudar a Doña Lupe.
            yield return AceptarEnLaCalle(PuntoDonaLupe, tiendita);
            yield return UsarPaso(tiendita.Pasos[0].IdPunto, "E · RECOGER: " + tiendita.Pasos[0].Texto);
            yield return UsarPaso(tiendita.Pasos[1].IdPunto, "E · ENTREGAR: " + tiendita.Pasos[1].Texto);
            Assert.IsFalse(misiones.HayMision, "b) terminó la tiendita");
            Assert.AreEqual(DineroInicial + rTiendita.Pago, cartera.Dinero, "b) +$50");
            Assert.AreEqual(tiendita.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "b) barrio +8");
            StringAssert.Contains("BARRIO " + FormatoHud.Firmado(tiendita.DeltaBarrio), hud.TextoAvisoReputacion.text, "b) el HUD lo dice");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento), "b) 1 DESCUENTO");
            Assert.Less(rTiendita.Pago, premioPrimerRival, "b) una misión de barrio paga menos que una pelea");
            Debug.Log("[T-188] una pelea paga más que una misión de barrio: tiendita $" + rTiendita.Pago + " < rival 1 de la liga 1 $" + premioPrimerRival);
            Marcar("b");

            // c) De día, un contrato corporativo por teléfono.
            yield return EsperarAvisoReputacionApagado();
            yield return Tecla(teclado.jKey);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja, "c) es de día tras la J");
            yield return AbrirTelefono();
            Assert.AreEqual(EstadoContratos.Lista, telefono.EstadoContratos, "c) el teléfono lista contratos");
            yield return ElegirEnLista(IdCobro);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "c) se acepta el cobro por teléfono");
            yield return UsarPaso(cobro.Pasos[0].IdPunto, "E · RECOGER: " + cobro.Pasos[0].Texto);
            yield return UsarPaso(cobro.Pasos[1].IdPunto, "E · ENTREGAR: " + cobro.Pasos[1].Texto);
            Assert.IsFalse(misiones.HayMision, "c) terminó el cobro");
            Assert.AreEqual(DineroInicial + rTiendita.Pago + rCobro.Pago, cartera.Dinero, "c) +$500");
            Assert.AreEqual("BARRIO −12 · CORPORACIONES +10", hud.TextoAvisoReputacion.text, "c) el HUD avisa el cambio");
            CapturarConHud(RutaCaptura1);
            Assert.AreEqual(tiendita.DeltaBarrio + cobro.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "c) barrio 8 − 12 = −4");
            Assert.AreEqual(cobro.DeltaCorporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "c) corporaciones +10");
            yield return AbrirTelefono();
            for (int i = 0; i < PestanasHastaReputacion; i++) yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Reputacion, telefono.Pestana, "c) pestaña REPUTACIÓN");
            StringAssert.Contains("−4", telefono.TextoBarra(0), "c) el barrio muestra −4");
            StringAssert.Contains("+10", telefono.TextoBarra(1), "c) corporaciones +10");
            yield return CerrarTelefono();
            Assert.AreEqual(string.Empty, reaccion.Texto, "c) el letrero sigue vacío (nivel 0 en las dos)");
            Marcar("c");

            // d) De noche, el robo de la bodega y esconderse con un favor.
            atajos.Add("AvanzarHasta(" + HoraNoche + ")");
            reloj.AvanzarHasta(HoraNoche);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(reloj.EsDeNoche, "d) es de noche");
            yield return IrA(contacto.Posicion, YawSur, "el contacto del bar");
            yield return EsperarTextoHud("E · HABLAR CON EL CONTACTO");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(lista.Visible, "d) la lista del contacto");
            yield return ElegirEnLista(IdBodega);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "d) se aceptó la bodega");
            yield return UsarPaso(bodega.Pasos[0].IdPunto, "E · RECOGER: " + bodega.Pasos[0].Texto);
            float calorEsperado = bodega.Pasos[0].Calor * MultiplicadorCalor();
            Assert.AreEqual(calorEsperado, calor.Valor, 0.5f, "d) el botín sube el calor");
            Assert.AreEqual(NivelCalorRobo, calor.Nivel, "d) calor nivel 2");
            Assert.AreEqual(TipoPaso.Escapar, misiones.PasoActual.Tipo, "d) pasa a Escapar");
            favores.Dar(TipoFavor.Escondite);
            atajos.Add("Dar favor ESCONDITE por código (el real se gana en f)");
            int chatarraAntes = inventario.Cantidad(chatarra);
            yield return IrA(Punto(PuntoEscondite).Pose.position, YawNorte, PuntoEscondite);
            yield return EsperarTextoHud(TextoEsconderte);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(0f, calor.Valor, 0.001f, "d) el calor quedó en 0");
            Assert.IsFalse(misiones.HayMision, "d) la bodega se cumplió");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Escondite), "d) el favor se gastó");
            Assert.AreEqual(chatarraAntes + rBodega.ChatarraCantidad, inventario.Cantidad(chatarra), "d) +6 de chatarra");
            Assert.AreEqual(0, reputacion.Valor(TipoReputacion.Corporaciones), "d) corporaciones 10 − 10 = 0");
            Assert.AreEqual(tiendita.DeltaBarrio + cobro.DeltaBarrio + bodega.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "d) barrio −4 + 3 = −1");
            Marcar("d");

            // e) El mercado negro: vender la chatarra y comprar un misil con el descuento.
            int dineroAntesMercado = cartera.Dinero;
            yield return AbrirTienda();
            PantallaTienda pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantalla.Pestana, "e) flecha derecha pasa a VENDER");
            for (int i = 0; i < rBodega.ChatarraCantidad; i++)
            {
                int fila = FilaConTexto(pantalla, FilaChatarra);
                Assert.GreaterOrEqual(fila, 0, "e) hay una fila de chatarra en la venta " + i);
                yield return ElegirFila(pantalla, fila);
                yield return Tecla(teclado.enterKey);
            }
            Assert.AreEqual(dineroAntesMercado + ventaChatarra, cartera.Dinero, "e) +$300 por la chatarra");
            Assert.AreEqual(0, inventario.Cantidad(chatarra), "e) ya no queda chatarra");
            yield return Tecla(teclado.leftArrowKey);
            Assert.AreEqual(PestanaTienda.Comprar, pantalla.Pestana, "e) flecha izquierda vuelve a COMPRAR");
            int filaMisiles = FilaConTexto(pantalla, FilaMisiles);
            Assert.GreaterOrEqual(filaMisiles, 0, "e) la tienda lista misiles");
            yield return ElegirFila(pantalla, filaMisiles);
            StringAssert.Contains("$" + precioMisil + " → $" + descuentoMisil, pantalla.TextoFila(filaMisiles), "e) la fila muestra la rebaja");
            int dineroAntesMisil = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dineroAntesMisil - descuentoMisil, cartera.Dinero, "e) el misil cuesta $56 con el descuento");
            Assert.AreEqual(misilesAntes + misilesEsperados, inventario.Cantidad(misiles), "e) entró 1 misil");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Descuento), "e) el descuento se gastó");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(mostrador.PantallaAbierta, "e) Esc cierra la tienda");
            Marcar("e");

            // f) La misma noche: LOS DEL CALLEJÓN en el mecha.
            int dineroAntesCallejon = cartera.Dinero;
            int barrioAntesCallejon = reputacion.Valor(TipoReputacion.Barrio);
            yield return AceptarEnLaCalle(PuntoTaquero, callejon);
            yield return Frame();
            yield return Frame();
            Assert.IsTrue(peleas.HayPelea, "f) al empezar el paso Pelear se levanta la emboscada");
            Assert.AreEqual(TextoLlamar, objetivos.Actual.Texto, "f) a pie, el objetivo pide llamar al mecha");
            Vector3 puntoPelea = Punto(PuntoPelea).Pose.position;
            Colocar(new Vector3(0f, 0f, 6f), YawNorte, "calle norte-sur, a 25 m del callejón");
            yield return Esperar(FramesParaElHud * 0.02f);
            director = peleas.Director;
            yield return CaminarA(new Vector3(puntoPelea.x - 1f, AlturaJugador, puntoPelea.z - 6f), DistanciaLlegada, "la boca del callejón",
                () => director.Estado != EstadoEmboscada.Esperando);
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "los pandilleros terminan de aparecer");
            Assert.AreEqual(2, director.Rivales.Count, "f) aparecen dos pandilleros");
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
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "f) con el mecha encendido los pandilleros pelean");
            yield return Pelear(null, null);
            yield return Frame();
            Assert.IsFalse(peleas.HayPelea, "f) al vencer se retira la emboscada");
            Assert.IsTrue(misiones.HayMision, "f) falta avisar al taquero");
            yield return BajarYDevolver();
            Vector3 taquero = Punto(PuntoTaquero).Pose.position;
            yield return CaminarA(new Vector3(taquero.x, AlturaJugador, taquero.z), DistanciaLlegada, "el taquero", () => !misiones.HayMision);
            yield return Esperar(FramesParaElHud * 0.02f);
            Assert.IsFalse(misiones.HayMision, "f) avisar al taquero termina la misión");
            Assert.AreEqual(dineroAntesCallejon + rCallejon.Pago, cartera.Dinero, "f) +$100");
            Assert.AreEqual(barrioAntesCallejon + callejon.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "f) barrio +12");
            Assert.AreEqual(callejon.DeltaBarrio + tiendita.DeltaBarrio + cobro.DeltaBarrio + bodega.DeltaBarrio, 11, "f) los datos suman barrio 11");
            Assert.AreEqual(11, reputacion.Valor(TipoReputacion.Barrio), "f) barrio 11");
            Assert.AreEqual(1, favores.Cantidad(rCallejon.Favor.Tipo), "f) 1 ESCONDITE real");
            Assert.AreEqual(TipoFavor.Escondite, rCallejon.Favor.Tipo);
            Assert.IsFalse(HayRivalesEnLaEscena(), "f) no quedan rivales");
            Assert.AreEqual(string.Empty, reaccion.Texto, "f) el letrero sigue vacío con barrio 11");
            Assert.AreNotEqual(MotivoMision.Disponible, misiones.Motivo(luz), "f) LUZ PARA LA VECINDAD aún no se ofrece");
            Marcar("f");

            // g) El día siguiente: Doña Lupe otra vez y el barrio sube a CONOCIDO.
            yield return Tecla(teclado.jKey);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja, "g) es de día tras la J");
            yield return EsperarAvisoReputacionApagado();
            yield return IrA(Punto(PuntoDonaLupe).Pose.position, YawNorte, PuntoDonaLupe);
            yield return EsperarTextoHud("E · AYUDAR: " + tiendita.Titulo);
            Assert.AreNotEqual(TextoHechaHoy, hud.TextoAvisoInteraccion.text, "g) ya no dice YA LO HICISTE HOY");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "g) se aceptó la tiendita otra vez");
            yield return UsarPaso(tiendita.Pasos[0].IdPunto, "E · RECOGER: " + tiendita.Pasos[0].Texto);
            yield return UsarPaso(tiendita.Pasos[1].IdPunto, "E · ENTREGAR: " + tiendita.Pasos[1].Texto);
            Assert.IsFalse(misiones.HayMision, "g) terminó la tiendita");
            Assert.AreEqual(barrioEsperado, reputacion.Valor(TipoReputacion.Barrio), "g) barrio 19");
            Assert.AreEqual(19, barrioEsperado, "g) los datos suman 19");
            Assert.AreEqual(NivelConocido, reputacion.Nivel(TipoReputacion.Barrio), "g) nivel 1");
            Assert.AreEqual("CONOCIDO", reputacion.NombreNivel(TipoReputacion.Barrio), "g) CONOCIDO");
            Assert.AreEqual(TextoConocido, hud.TextoAvisoReputacion2.text, "g) el HUD lo dice");
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            string alias = perfil != null ? perfil.Alias : string.Empty;
            Assert.AreEqual("GRAFITI: " + alias, reaccion.Texto, "g) el letrero dice GRAFITI: <ALIAS>");
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(luz), "g) LUZ PARA LA VECINDAD ya se ofrece");
            Assert.IsTrue(misiones.Ofertas(OrigenMision.Calle, PuntoVecindad).Exists(o => o.Datos.Id == IdLuz), "g) y sale en el encargo de la vecindad");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento), "g) otro DESCUENTO");
            TextMesh malla = reaccion.GetComponentInChildren<TextMesh>();
            Assert.IsNotNull(malla, "g) el letrero tiene su TextMesh");
            Assert.AreEqual(reaccion.Texto, malla.text, "g) el TextMesh del letrero dice lo mismo");
            Debug.Log("[T-188] letrero: escala del mundo " + malla.transform.lossyScale + ", límites " + malla.GetComponent<Renderer>().bounds);
            Debug.Log("[T-188] hecha hoy: el HUD en Doña Lupe dice '" + hud.TextoAvisoInteraccion.text + "'");
            Marcar("g");

            // h) Liga 2: LIMPIEZA DE ZONA sigue en gris por la reputación con las corporaciones.
            Assert.IsTrue(ligas.Poner(LigaDosAbierta, 1, 1), "h) la liga 2 se abre");
            atajos.Add("ServicioLigas.Poner(" + LigaDosAbierta + ", 1, 1)");
            Assert.AreEqual(0, reputacion.Valor(TipoReputacion.Corporaciones), "h) corporaciones en 0");
            Assert.AreEqual("SIN FICHA", reputacion.NombreNivel(TipoReputacion.Corporaciones), "h) SIN FICHA");
            yield return AbrirTelefono();
            Assert.AreEqual(PestanaTelefono.Contratos, telefono.Pestana);
            Assert.AreEqual(EstadoContratos.Lista, telefono.EstadoContratos, "h) de día hay lista");
            int filaLimpieza = FilaDe(IdLimpieza);
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(filaLimpieza), "h) LIMPIEZA DE ZONA en gris");
            StringAssert.Contains("NECESITAS SER CONTRATISTA CON LAS CORPORACIONES", lista.TextoFila(filaLimpieza), "h) con su motivo");
            Assert.AreNotEqual(MotivoMision.Disponible, misiones.Motivo(limpieza));
            Assert.AreEqual(config.ColorApagado, lista.ColorFila(filaLimpieza));
            for (int i = 0; i < PestanasHastaReputacion; i++) yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTelefono.Reputacion, telefono.Pestana);
            StringAssert.Contains("CONOCIDO", telefono.TextoBarra(0), "h) la barra del barrio dice CONOCIDO");
            StringAssert.Contains("19", telefono.TextoBarra(0));
            yield return Cuadros(FramesParaElHud);
            CapturarConHud(RutaCaptura2);
            yield return CerrarTelefono();
            Colocar(reaccion.transform.position + new Vector3(-DistanciaACamaraLetrero, 0f, 0f), 90f, "junto al muro del letrero");
            yield return Cuadros(FramesParaElHud);
            CapturarLetrero(RutaCaptura3);
            Marcar("h");

            // i) F5, cambiar todo por código, F9: todo vuelve y la misión en curso queda cancelada sin castigo.
            yield return AbrirTelefono();
            yield return ElegirEnLista(IdEntrega);
            yield return Tecla(teclado.enterKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(misiones.HayMision, "i) hay una misión en curso antes de F5");
            int dineroGuardado = cartera.Dinero;
            int barrioGuardado = reputacion.Valor(TipoReputacion.Barrio);
            int corporacionesGuardadas = reputacion.Valor(TipoReputacion.Corporaciones);
            int descuentoGuardado = favores.Cantidad(TipoFavor.Descuento);
            int esconditeGuardado = favores.Cantidad(TipoFavor.Escondite);
            int informacionGuardada = favores.Cantidad(TipoFavor.Informacion);
            int terminadasAntes = terminadas.Count;
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(File.Exists(ServicioGuardado.RutaForzada), "i) F5 escribió la partida");
            reputacion.Sumar(TipoReputacion.Barrio, ModificacionBarrio);
            reputacion.Sumar(TipoReputacion.Corporaciones, -ModificacionBarrio);
            favores.Gastar(TipoFavor.Descuento);
            favores.Dar(TipoFavor.Informacion);
            favores.Dar(TipoFavor.Informacion);
            inventario.Agregar(chatarra, 2);
            cartera.Abonar(ModificacionDinero, MotivoDinero.Otro);
            atajos.Add("Cambiar reputación, favores, chatarra y dinero por código antes de F9");
            Assert.AreNotEqual(barrioGuardado, reputacion.Valor(TipoReputacion.Barrio));
            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "i) el dinero vuelve");
            Assert.AreEqual(barrioGuardado, reputacion.Valor(TipoReputacion.Barrio), "i) el barrio vuelve");
            Assert.AreEqual(corporacionesGuardadas, reputacion.Valor(TipoReputacion.Corporaciones), "i) corporaciones vuelve");
            Assert.AreEqual(descuentoGuardado, favores.Cantidad(TipoFavor.Descuento), "i) DESCUENTO vuelve");
            Assert.AreEqual(esconditeGuardado, favores.Cantidad(TipoFavor.Escondite), "i) ESCONDITE vuelve");
            Assert.AreEqual(informacionGuardada, favores.Cantidad(TipoFavor.Informacion), "i) INFORMACIÓN vuelve");
            Assert.AreEqual(0, inventario.Cantidad(chatarra), "i) la chatarra vuelve");
            Assert.AreEqual(misilesAntes + misilesEsperados, inventario.Cantidad(misiles), "i) los misiles vuelven");
            Assert.IsFalse(misiones.HayMision, "i) la misión en curso queda cancelada");
            Assert.AreEqual(terminadasAntes, terminadas.Count, "i) cancelar al cargar no avisa Terminada: sin castigo");
            Assert.AreEqual("GRAFITI: " + alias, reaccion.Texto, "i) el letrero vuelve a decir GRAFITI");
            Marcar("i");

            // Final: todo cuadra con lo calculado desde los assets.
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "final: dinero");
            Assert.AreEqual(barrioEsperado, reputacion.Valor(TipoReputacion.Barrio), "final: barrio");
            Assert.AreEqual(corporacionesEsperadas, reputacion.Valor(TipoReputacion.Corporaciones), "final: corporaciones");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento), "final: DESCUENTO");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Escondite), "final: ESCONDITE");
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Informacion), "final: INFORMACIÓN");
            Assert.AreEqual(0, inventario.Cantidad(chatarra), "final: chatarra");
            Assert.AreEqual(misilesAntes + misilesEsperados, inventario.Cantidad(misiles), "final: misiles");
            Debug.Log("[T-188] final: dinero " + cartera.Dinero + ", barrio " + reputacion.Valor(TipoReputacion.Barrio) + ", corporaciones " +
                      reputacion.Valor(TipoReputacion.Corporaciones) + ", DESCUENTO " + favores.Cantidad(TipoFavor.Descuento) + ", ESCONDITE " +
                      favores.Cantidad(TipoFavor.Escondite) + ", INFORMACIÓN " + favores.Cantidad(TipoFavor.Informacion) + ", chatarra " +
                      inventario.Cantidad(chatarra) + ", misiles " + inventario.Cantidad(misiles));
            Debug.Log("[T-188] tiempo de juego por paso: " + string.Join(", ", tiempos) + " | total " +
                      (Time.time - inicioTotal).ToString("F1", CultureInfo.InvariantCulture) + " s");

            // j) Consola sin errores y sin tirones.
            AssertSinErrores();
            Assert.IsTrue(medidor.Cumple, "j) sin tirones: " + medidor.Resumen());
            Debug.Log("[T-188] atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }
    }
}
