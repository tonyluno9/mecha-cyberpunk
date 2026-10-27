using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
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
    /// T-184: las misiones a pie en Pruebas_Misiones con entrada simulada (WASD y E, J para saltar al día). Todos los números salen de los
    /// assets y se imprimen. Atajos permitidos y anotados: Aceptar por código (hasta T-186, que trae el teléfono, el bar y la calle),
    /// Colocar al jugador a 8 m del punto mirándolo, Time.timeScale para dejar pasar los 150 s del límite y Guardar/Cargar por código.
    /// </summary>
    public class MisionesAPiePruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string IdTiendita = "barrio_tiendita";
        private const string IdEntrega = "corp_entrega";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaInicial = 8f;
        private const float DistanciaParaUsar = 1.8f;
        private const float AlturaInicio = 0.05f;
        private const float MaxSegundosCaminando = 10f;
        private const float MaxSegundosAviso = 3f;
        private const float EscalaDeTiempo = 30f;
        private const float MargenLimite = 20f;
        private const float MaxSegundosReales = 60f;
        private const float ToleranciaFlecha = 10f;
        private const float ToleranciaPunto = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private HashSet<GameObject> raicesIniciales;
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<(TipoMision, bool)> terminadas = new List<(TipoMision, bool)>();
        private readonly MedidorFrames medidor = new MedidorFrames();
        private int framesSinMedir;

        private JugadorAPie jugador;
        private HudAPie hud;
        private ServicioMisiones misiones;
        private ServicioReputacion reputacion;
        private ServicioFavores favores;
        private ICartera cartera;
        private RelojDePrueba reloj;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "misiones_a_pie_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            atajos.Clear();
            terminadas.Clear();
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
            Assert.IsNotNull(jugador, "Pruebas_Misiones trae al jugador");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.AreEqual(1, Registro<ServicioMisiones>.Cantidad, "un servicio de misiones");
            Assert.AreEqual(1, Registro<IMisiones>.Cantidad, "un IMisiones");
            misiones = Registro<ServicioMisiones>.Actual;
            reputacion = Registro<ServicioReputacion>.Actual;
            favores = Registro<ServicioFavores>.Actual;
            cartera = Registro<ICartera>.Actual;
            reloj = Object.FindFirstObjectByType<RelojDePrueba>();
            Assert.IsNotNull(reputacion, "Hay reputación");
            Assert.IsNotNull(favores, "Hay favores");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj de prueba");
            Registro<IMisiones>.Actual.Terminada += (tipo, exito) => terminadas.Add((tipo, exito));
            // La historia queda en Libre en el primer Start.
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");
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

        // Pone al jugador a 8 m al sur del punto, mirándolo, y camina con W hasta quedar a menos de 1.8 m.
        private IEnumerator CaminarAlPunto(string idPunto)
        {
            Vector3 destino = Punto(idPunto).Pose.position;
            jugador.Colocar(new Pose(destino + new Vector3(0f, AlturaInicio, -DistanciaInicial), Quaternion.identity));
            atajos.Add("Colocar: a " + DistanciaInicial.ToString("F0", CultureInfo.InvariantCulture) + " m de " + idPunto);
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
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, destino), DistanciaParaUsar + 0.2f, "llegó caminando a " + idPunto);
        }

        // Camina al punto, espera que el HUD ofrezca el texto del paso y pulsa E.
        private IEnumerator UsarPaso(string idPunto, string textoEsperado)
        {
            yield return CaminarAlPunto(idPunto);
            float fin = Time.time + MaxSegundosAviso;
            while (hud.TextoAvisoInteraccion.text != textoEsperado && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.AreEqual(textoEsperado, hud.TextoAvisoInteraccion.text, "el HUD ofrece el paso");
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator CompletarTiendita()
        {
            DatosMision datos = Mision(IdTiendita);
            Assert.IsTrue(misiones.Aceptar(datos), "se acepta la tiendita (atajo: por código)");
            atajos.Add("Aceptar por código: " + IdTiendita);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            yield return UsarPaso(datos.Pasos[1].IdPunto, "E · ENTREGAR: " + datos.Pasos[1].Texto);
        }

        private void VerificarFinal()
        {
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
            Debug.Log("[T-184] atajos: " + string.Join("; ", atajos) + " | " + medidor.Resumen());
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Arranca_RegistraElServicioYLosPuntosDelCatalogo()
        {
            yield return CargarEscena();

            var ids = new HashSet<string>();
            foreach (DatosMision m in misiones.Catalogo.Misiones)
            {
                if (!string.IsNullOrEmpty(m.IdEncargo)) ids.Add(m.IdEncargo);
                foreach (PasoMision p in m.Pasos)
                {
                    if (!string.IsNullOrEmpty(p.IdPunto)) ids.Add(p.IdPunto);
                }
            }
            ids.Add("Mision_Escondite_Azotea");
            ids.Add("Mision_Escondite_Lavanderia");
            foreach (string id in ids)
            {
                Assert.IsNotNull(Punto(id), "hay un punto " + id);
            }
            Assert.AreEqual(ids.Count, Registro<IPuntoMision>.Cantidad, "ni más ni menos puntos que ids del catálogo");
            foreach (DatosMision m in misiones.Catalogo.Misiones)
            {
                Debug.Log("[T-184] " + m.Id + " -> " + misiones.Motivo(m));
            }
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(Mision(IdTiendita)), "la tiendita se ofrece de noche en Libre");
            Assert.IsTrue(misiones.Ofertas(OrigenMision.Calle, "Mision_Encargo_DonaLupe").Exists(o => o.Datos.Id == IdTiendita), "Ofertas por encargo");

            bool guardable = false;
            foreach (IGuardable g in Registro<IGuardable>.Todos) guardable |= g.ClaveGuardado == "misiones";
            Assert.IsTrue(guardable, "se guarda con la clave misiones");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Tiendita_FlechaApuntaRecogeEntregaYCobra()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdTiendita);
            string idRecoger = datos.Pasos[0].IdPunto;
            string idEntregar = datos.Pasos[1].IdPunto;
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int cantidadFavor = favores.Cantidad(datos.Favor.Tipo);
            var resumenes = new List<ResumenMision>();
            misiones.Terminada += (d, exito, r) => resumenes.Add(r);
            Debug.Log("[T-184] esperado tiendita: pago " + datos.Pago + ", barrio +" + datos.DeltaBarrio + ", favor " + datos.Favor.Tipo +
                      " | antes: dinero " + dinero + ", barrio " + barrio + ", favores " + cantidadFavor);

            Assert.IsTrue(misiones.Aceptar(datos), "se acepta (atajo: por código)");
            atajos.Add("Aceptar por código: " + IdTiendita);
            Assert.IsTrue(misiones.HayMision);
            Assert.AreEqual(datos.Titulo, Registro<IMisiones>.Actual.Titulo);
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Assert.IsTrue(objetivos.HayObjetivo, "hay objetivo");
            Assert.AreEqual(datos.Pasos[0].Texto, objetivos.Actual.Texto, "el objetivo dice el paso");
            Assert.Less(Vector3.Distance(Punto(idRecoger).Pose.position, objetivos.Actual.Posicion), ToleranciaPunto, "el objetivo apunta a la farmacia");

            // La flecha apunta a la farmacia: de frente a ella el ángulo es casi 0 y la distancia es la medida.
            Vector3 destino = Punto(idRecoger).Pose.position;
            jugador.Colocar(new Pose(destino + new Vector3(0f, AlturaInicio, -DistanciaInicial), Quaternion.identity));
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(0f, hud.AnguloFlecha, ToleranciaFlecha, "la flecha del HUD apunta a la farmacia (°)");
            Assert.AreEqual(CalculoFlechaObjetivo.TextoDistancia(DistanciaInicial), hud.TextoDistancia.text, "la distancia del HUD");

            yield return UsarPaso(idRecoger, "E · RECOGER: " + datos.Pasos[0].Texto);
            Assert.IsTrue(misiones.HayMision, "sigue la misión");
            Assert.AreEqual(1, misiones.IndicePaso, "pasa al segundo paso");
            Assert.AreEqual(datos.Pasos[1].Texto, objetivos.Actual.Texto, "el objetivo pasa a Doña Lupe");
            Assert.Less(Vector3.Distance(Punto(idEntregar).Pose.position, objetivos.Actual.Posicion), ToleranciaPunto, "el objetivo apunta a Doña Lupe");
            Assert.AreEqual(0, terminadas.Count, "todavía no termina");

            yield return UsarPaso(idEntregar, "E · ENTREGAR: " + datos.Pasos[1].Texto);

            Assert.IsFalse(misiones.HayMision, "terminó");
            Assert.IsFalse(objetivos.HayObjetivo, "se quita el objetivo");
            CollectionAssert.AreEqual(new[] { (TipoMision.Barrio, true) }, terminadas, "IMisiones.Terminada(Barrio, true) una vez");
            Assert.AreEqual(dinero + datos.Pago, cartera.Dinero, "pago (que $)");
            Assert.AreEqual(barrio + datos.DeltaBarrio, reputacion.Valor(TipoReputacion.Barrio), "reputación del barrio (puntos)");
            Assert.AreEqual(cantidadFavor + 1, favores.Cantidad(datos.Favor.Tipo), "un favor");
            Assert.AreEqual(1, resumenes.Count);
            Assert.AreEqual(datos.Pago, resumenes[0].Pago, "resumen: pago");
            Assert.AreEqual(datos.DeltaBarrio, resumenes[0].DeltaBarrio, "resumen: barrio");
            Assert.AreSame(datos.Favor, resumenes[0].Favor, "resumen: favor");
            Assert.AreEqual(1, misiones.VecesCumplida(IdTiendita));
            Assert.IsFalse(((PuntoMisionDePrueba)Punto(idRecoger)).Activo, "el punto de la farmacia se apaga");
            Assert.IsFalse(((PuntoMisionDePrueba)Punto(idEntregar)).Activo, "el punto de Doña Lupe se apaga");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Repetible_NoSeOfreceHastaElDiaSiguiente()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdTiendita);
            int dia = Registro<IRelojMundo>.Actual.Dia;
            yield return CompletarTiendita();
            Assert.AreEqual(1, terminadas.Count);
            Assert.AreEqual(MotivoMision.HechaHoy, misiones.Motivo(datos), "hecha hoy: no se ofrece");
            Assert.IsFalse(misiones.Aceptar(datos), "no se puede aceptar otra vez");
            Assert.AreEqual(dia, misiones.UltimoDiaCumplida(IdTiendita), "último día");

            yield return Tecla(teclado.jKey);
            Assert.AreEqual(dia + 1, Registro<IRelojMundo>.Actual.Dia, "J salta al día siguiente");
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(datos), "al día siguiente se ofrece de nuevo");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Abandonar_ALaMitad_CastigaLoMismoQueFallar()
        {
            yield return CargarEscena();
            DatosMision datos = Mision(IdTiendita);
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            PenalizacionMision castigo = ReglaMisiones.Penalizacion(datos);
            ResumenMision resumen = default;
            misiones.Terminada += (d, exito, r) => resumen = r;
            Debug.Log("[T-184] esperado abandono: barrio " + castigo.Barrio + ", corporaciones " + castigo.Corporaciones);

            Assert.IsTrue(misiones.Aceptar(datos));
            atajos.Add("Aceptar por código: " + IdTiendita);
            yield return UsarPaso(datos.Pasos[0].IdPunto, "E · RECOGER: " + datos.Pasos[0].Texto);
            misiones.Abandonar();

            Assert.IsFalse(misiones.HayMision);
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo, "se quita el objetivo");
            CollectionAssert.AreEqual(new[] { (TipoMision.Barrio, false) }, terminadas, "Terminada(Barrio, false) una vez");
            Assert.AreEqual(dinero, cartera.Dinero, "sin pago");
            Assert.AreEqual(barrio + castigo.Barrio, reputacion.Valor(TipoReputacion.Barrio), "barrio castigado (puntos)");
            Assert.AreEqual(corporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones igual");
            Assert.AreEqual(MotivoFalloMision.Abandono, resumen.Motivo);
            Assert.AreEqual(0, misiones.VecesCumplida(IdTiendita), "no cuenta como cumplida");
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(datos), "se puede volver a intentar");
            misiones.Abandonar();
            Assert.AreEqual(1, terminadas.Count, "abandonar sin misión no avisa otra vez");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Entrega_SiSePasaElTiempo_FallaSinPagoYCastigaALasCorporaciones()
        {
            yield return CargarEscena();
            yield return Tecla(teclado.jKey); // de día: la entrega solo se ofrece de día
            DatosMision datos = Mision(IdEntrega);
            Assert.AreEqual(FranjaDia.Dia, Registro<IRelojMundo>.Actual.Franja, "es de día tras la J");
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            PenalizacionMision castigo = ReglaMisiones.Penalizacion(datos);
            ResumenMision resumen = default;
            misiones.Terminada += (d, exito, r) => resumen = r;
            Debug.Log("[T-184] esperado límite: " + datos.LimiteSegundos + " s, corporaciones " + castigo.Corporaciones + ", barrio " + castigo.Barrio);
            Assert.AreEqual(-3, castigo.Corporaciones, "la penalización es de 3 puntos");
            Assert.AreEqual(0, castigo.Barrio, "la entrega no subía el barrio");

            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(datos), "la entrega se ofrece de día");
            Assert.IsTrue(misiones.Aceptar(datos));
            atajos.Add("Aceptar por código: " + IdEntrega);
            Assert.AreEqual(datos.LimiteSegundos, misiones.SegundosRestantes, 1f, "corre el límite (s)");

            // Tiempo acelerado para no esperar 150 s reales; el límite lo cuenta el servicio con Time.deltaTime.
            atajos.Add("Time.timeScale = " + EscalaDeTiempo.ToString("F0", CultureInfo.InvariantCulture));
            Time.timeScale = EscalaDeTiempo;
            float juegoInicial = Time.time;
            float limiteReal = Time.unscaledTime + MaxSegundosReales;
            while (misiones.HayMision && Time.unscaledTime < limiteReal)
            {
                yield return null;
                Vigilar();
            }
            Time.timeScale = 1f;
            float juegoTranscurrido = Time.time - juegoInicial;
            Debug.Log("[T-184] pasaron " + juegoTranscurrido.ToString("F1", CultureInfo.InvariantCulture) + " s de juego");

            Assert.IsFalse(misiones.HayMision, "la misión falla al acabarse el tiempo");
            Assert.GreaterOrEqual(juegoTranscurrido, datos.LimiteSegundos - MargenLimite, "pasó casi todo el límite (s de juego)");
            CollectionAssert.AreEqual(new[] { (TipoMision.Corporativa, false) }, terminadas, "Terminada(Corporativa, false) una vez");
            Assert.AreEqual(MotivoFalloMision.Tiempo, resumen.Motivo, "falló por tiempo");
            Assert.AreEqual(dinero, cartera.Dinero, "sin pago");
            Assert.AreEqual(corporaciones + castigo.Corporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "corporaciones -3 (puntos)");
            Assert.AreEqual(barrio, reputacion.Valor(TipoReputacion.Barrio), "barrio igual");
            Assert.AreEqual(0, misiones.VecesCumplida(IdEntrega), "no cuenta como cumplida");
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo, "se quita el objetivo");
            VerificarFinal();
        }

        [UnityTest]
        public IEnumerator Guardar_ConservaVecesYDia_YCargarCancelaLaMisionEnCursoSinCastigo()
        {
            yield return CargarEscena();
            DatosMision tiendita = Mision(IdTiendita);
            int dia1 = Registro<IRelojMundo>.Actual.Dia;
            yield return CompletarTiendita();
            yield return Tecla(teclado.jKey); // día siguiente, de día
            DatosMision entrega = Mision(IdEntrega);

            ServicioGuardado guardado = ServicioGuardado.Actual;
            Assert.IsNotNull(guardado, "hay ServicioGuardado");
            atajos.Add("Guardar y Cargar por código");
            Assert.IsTrue(guardado.Guardar(), "guarda");
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);

            Assert.IsTrue(misiones.Aceptar(entrega), "se acepta la entrega después de guardar");
            terminadas.Clear();
            int cambios = 0;
            Registro<IMisiones>.Actual.Cambio += () => cambios++;
            Assert.IsTrue(guardado.Cargar(), "carga");
            yield return Cuadros(FramesParaElHud);

            Assert.IsFalse(misiones.HayMision, "la misión en curso no se guarda: al cargar queda cancelada");
            Assert.AreEqual(0, terminadas.Count, "cancelar al cargar no avisa Terminada");
            Assert.Greater(cambios, 0, "al cargar se avisa Cambio");
            Assert.AreEqual(corporaciones, reputacion.Valor(TipoReputacion.Corporaciones), "sin castigo: corporaciones (puntos)");
            Assert.AreEqual(barrio, reputacion.Valor(TipoReputacion.Barrio), "sin castigo: barrio (puntos)");
            Assert.AreEqual(dinero, cartera.Dinero, "sin castigo: dinero");
            Assert.AreEqual(1, misiones.VecesCumplida(IdTiendita), "la tiendita sigue cumplida una vez");
            Assert.AreEqual(dia1, misiones.UltimoDiaCumplida(IdTiendita), "el último día se conserva");
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo, "sin objetivo");
            Assert.IsFalse(((PuntoMisionDePrueba)Punto(entrega.Pasos[0].IdPunto)).Activo, "el punto queda apagado");
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(entrega), "se puede volver a aceptar");
            Assert.AreEqual(MotivoMision.Disponible, misiones.Motivo(tiendita), "la tiendita vuelve a ofrecerse el día siguiente");
            VerificarFinal();
        }
    }
}
