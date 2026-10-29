using System;
using System.Collections;
using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-136: el recorrido de T-135 (a a g) con las escenas de Alan: Ciudad_Prologo.unity y Flashback.unity, con cambios de escena reales.
    /// Hereda de T-135 lo que no depende de la geometría (creación, pelea del campeonato, la pelea de la emboscada, el flashback y el final)
    /// y cambia los tramos de calle: sale del clandestino a pie al carro de Alan, maneja con W/A/S/D hasta el callejón (la emboscada se activa
    /// al acercarse), regresa en el carro por las esquinas y entra a la casa por su puerta. No guarda ni edita ninguna escena de Alan.
    /// Atajos: ninguno en las caminatas; en el manejo, solo tramos de más de 40 m (se anotan).
    /// </summary>
    public class PrologoCiudadPruebas : RecorridoPrologoPruebas
    {
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaFlashbackAlan = "Assets/_Proyecto/Escenas/Flashback.unity";
        private const string IdEsquina1 = "Prologo_Esquina_1";
        private const string IdEsquina2 = "Prologo_Esquina_2";
        private const string TextoAEmboscada = "Maneja a casa";
        private const string VariableCapturaCiudad = "CAPTURAR_T136";
        private const string RutaCapturaCiudad1 = "docs/capturas/T-136-1.png";
        private const string RutaCapturaCiudad2 = "docs/capturas/T-136-2.png";
        private const string RutaCapturaCiudad3 = "docs/capturas/T-136-3.png";
        private const string RutaCapturaCiudad4 = "docs/capturas/T-136-4.png";
        private const float LimiteJuegoCiudad = 900f;
        private const float RadioCarro = 7f;
        private const float RadioParada = 5f;
        private const float RadioPuerta = 0.5f;
        private const float MetrosAntesDeLaPuerta = 3f;
        private const float MetrosDelPasilloALaCama = -0.25f;
        private const float SegundosPuertaAbriendo = 1.5f;
        private const float MaxSegundosManejo = 90f;
        private const float MaxSegundosParaBajar = 10f;
        private const float GradosSinGiro = 8f;
        private const float GradosParaManiobra = 110f;
        private const float SegundosPorFaseManiobra = 1.4f;
        private const float GradosParaFrenar = 35f;
        private const float MetrosPorSegundoRecto = 11f;
        private const float MetrosPorSegundoEnVuelta = 4.5f;
        private const float SegundosEntreTeclas = 0.3f;
        private const float DistanciaAlCarro = 2.2f;
        private const float MetrosDelExtremo = 4f;
        private const int IntentosPuerta = 3;
        private const float MetrosLibresAlRodear = 3f;
        private const float MetrosAlRodear = 3.5f;
        private const float MetrosDelLado = 2.2f;
        private const float FraccionRadioLlamado = 0.5f;
        private const float DistanciaAlaPuertaDelClandestino = 1.5f;

        protected override string RutaDatosEscenas => "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        protected override string EscenaPrologo => RutaCiudad;
        protected override string EscenaFlashback => RutaFlashbackAlan;
        protected override float LimiteSegundosJuego => LimiteJuegoCiudad;
        protected override bool CapturaPedida => Environment.GetEnvironmentVariable(VariableCapturaCiudad) == "1";
        protected override string RutaDeCaptura(string ruta) => ruta == "docs/capturas/T-135-4.png" ? RutaCapturaCiudad4 : (ruta.Contains("T-136") ? ruta : null);

        private static T Buscar<T>(System.Collections.Generic.IReadOnlyList<T> lista, Func<T, bool> condicion, string que)
        {
            foreach (T item in lista)
            {
                if (condicion(item))
                {
                    return item;
                }
            }
            Assert.Fail("Falta " + que);
            return default;
        }

        private static IMontable Carro() => Buscar(Registro<IMontable>.Todos, m => m.Modo == ModoJugador.Carro, "el carro (IMontable en modo Carro)");

        private void Teclas(bool acelera, bool frena, bool izquierda, bool derecha, bool freno = false)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(
                acelera ? Key.W : Key.None, frena ? Key.S : Key.None, izquierda ? Key.A : Key.None, derecha ? Key.D : Key.None,
                freno ? Key.Space : Key.None));
        }

        // El conductor de prueba: gira A o D según el ángulo al destino y acelera con W. Termina con el carro llegando (o cuando se cumple 'antes').
        private IEnumerator Manejar(string nombre, IMontable carro, Vector3 destino, float radio, Func<bool> antes = null)
        {
            Transform cuerpo = carro.Asiento;
            float inicio = Time.time;
            Vector3 anterior = cuerpo.position;
            int maniobras = 0;
            while (DistanciaPlana(cuerpo.position, destino) > radio && (antes == null || !antes()))
            {
                if (Time.time - inicio > MaxSegundosManejo)
                {
                    Teclas(false, false, false, false);
                    Assert.Fail(nombre + ": el carro no llegó a " + destino + "; quedó en " + cuerpo.position);
                }
                Vector3 hacia = destino - cuerpo.position;
                hacia.y = 0f;
                float angulo = Vector3.SignedAngle(Vector3.ProjectOnPlane(cuerpo.forward, Vector3.up), hacia, Vector3.up);
                float metrosPorSegundo = Vector3.Distance(cuerpo.position, anterior) / Mathf.Max(Time.deltaTime, 0.0001f);
                if (Mathf.Abs(angulo) > GradosParaManiobra)
                {
                    // El destino queda atrás: maniobra de varios puntos (adelante girando hacia el destino, reversa girando al revés).
                    bool izq = angulo < 0f;
                    float finFase = Time.time + SegundosPorFaseManiobra;
                    while (Time.time < finFase)
                    {
                        Teclas(true, false, izq, !izq);
                        yield return null;
                        Vigilar();
                    }
                    finFase = Time.time + SegundosPorFaseManiobra;
                    while (Time.time < finFase)
                    {
                        Teclas(false, true, !izq, izq);
                        yield return null;
                        Vigilar();
                    }
                    anterior = cuerpo.position;
                    maniobras++;
                    continue;
                }
                anterior = cuerpo.position;
                float objetivo = Mathf.Abs(angulo) > GradosParaFrenar ? MetrosPorSegundoEnVuelta : MetrosPorSegundoRecto;
                Teclas(metrosPorSegundo < objetivo, metrosPorSegundo >= objetivo, angulo < -GradosSinGiro, angulo > GradosSinGiro);
                yield return null;
                Vigilar();
            }
            Teclas(false, false, false, false);
            yield return null;
            Vigilar();
            tramos.Add(nombre + ": " + (Time.time - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s (manejo, " + maniobras + " maniobras)");
        }

        // Atajo anotado (solo manejo de más de 40 m): el carro se lleva con su Rigidbody a 10 m del destino, mirando a él, y los últimos metros se manejan.
        private IEnumerator AcortarManejo(IMontable carro, Vector3 destino, string que)
        {
            Transform cuerpo = carro.Asiento;
            float lejos = DistanciaPlana(cuerpo.position, destino);
            if (lejos <= MetrosParaAtajo)
            {
                yield break;
            }
            Rigidbody rb = ((Component)carro).GetComponent<Rigidbody>();
            Vector3 hacia = destino - cuerpo.position;
            hacia.y = 0f;
            hacia.Normalize();
            Vector3 nueva = destino - hacia * MetrosTrasAtajo;
            nueva.y = rb.position.y;
            rb.position = nueva;
            rb.rotation = Quaternion.LookRotation(hacia, Vector3.up);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            atajos.Add("Carro movido (" + que + "): manejo de " + lejos.ToString("F0", CultureInfo.InvariantCulture) + " m acortado a "
                + MetrosTrasAtajo.ToString("F0", CultureInfo.InvariantCulture) + " m");
            yield return null;
            yield return null;
            Vigilar();
        }

        // Si el carro queda entre el jugador y el destino, se rodea por un lado antes de seguir derecho.
        private IEnumerator RodearCarro(IMontable carro, Vector3 destino)
        {
            Vector3 centro = carro.Asiento.position;
            Vector3 origen = jugador.Raiz.position;
            Vector3 trayecto = destino - origen;
            trayecto.y = 0f;
            Vector3 alCarro = centro - origen;
            alCarro.y = 0f;
            float t = Mathf.Clamp01(Vector3.Dot(alCarro, trayecto) / Mathf.Max(trayecto.sqrMagnitude, 0.01f));
            float lejosDeLaLinea = (alCarro - trayecto * t).magnitude;
            Debug.Log("[Prueba T-136] Rodear carro: jugador " + origen + ", carro " + centro + ", destino " + destino + ", a " + lejosDeLaLinea.ToString("F1") + " m de la línea");
            if (lejosDeLaLinea > MetrosLibresAlRodear)
            {
                yield break;
            }
            Vector3 lado = Vector3.Cross(Vector3.up, trayecto.normalized) * MetrosAlRodear;
            Vector3 a = centro + lado;
            Vector3 b = centro - lado;
            Vector3 paso = DistanciaPlana(origen, a) < DistanciaPlana(origen, b) ? a : b;
            paso.y = AlturaJugador;
            yield return CaminarA(paso, DistanciaLlegada, "rodeando el carro");
        }

        // Frena con Espacio hasta que el carro puede bajar y baja con E.
        private IEnumerator FrenarYBajar(IMontable carro)
        {
            float inicio = Time.time;
            Teclas(false, false, false, false, true);
            while (!carro.PuedeBajar)
            {
                if (Time.time - inicio > MaxSegundosParaBajar)
                {
                    Teclas(false, false, false, false);
                    Assert.Fail("El carro no frenó para bajar");
                }
                yield return null;
                Vigilar();
            }
            Teclas(false, false, false, false);
            yield return null;
            Vigilar();
            yield return Tecla(teclado.eKey);
            yield return EsperarSegundos(SegundosEntreTeclas);
            Assert.IsFalse(carro.Ocupado, "E baja del carro");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Bajó a pie");
        }

        private IEnumerator SubirAlCarro(IMontable carro)
        {
            // El cuerpo del carro estorba si se llega por el lado contrario a la puerta: se rodea por el extremo más cercano, en el lado de la puerta.
            Vector3 centro = carro.Asiento.position;
            Vector3 eje = Vector3.ProjectOnPlane(carro.Asiento.forward, Vector3.up).normalized;
            Vector3 lado = carro.PuntoSubida - centro;
            lado.y = 0f;
            Vector3 haciaPuerta = Vector3.Project(lado, Vector3.Cross(Vector3.up, eje)).normalized * MetrosDelLado;
            Vector3 extremoA = centro + eje * MetrosDelExtremo + haciaPuerta;
            Vector3 extremoB = centro - eje * MetrosDelExtremo + haciaPuerta;
            Vector3 extremo = DistanciaPlana(jugador.Raiz.position, extremoA) < DistanciaPlana(jugador.Raiz.position, extremoB) ? extremoA : extremoB;
            if (DistanciaPlana(jugador.Raiz.position, carro.PuntoSubida) > DistanciaAlCarro + MetrosDelExtremo)
            {
                extremo.y = AlturaJugador;
                yield return CaminarA(extremo, DistanciaLlegada, "el extremo del carro");
            }
            Debug.Log("[Prueba T-136] Carro en " + carro.Asiento.position + ", puerta " + carro.PuntoSubida + ", jugador " + jugador.Raiz.position);
            yield return CaminarA(carro.PuntoSubida, DistanciaAlCarro, "junto al carro");
            yield return MirarA(carro.PuntoSubida, "la puerta del carro");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Montable, 3f, "la interacción ofrece el carro");
            yield return Tecla(teclado.eKey);
            yield return EsperarSegundos(SegundosEntreTeclas);
            Assert.IsTrue(carro.Ocupado, "E junto al carro sube");
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "El jugador va en el carro");
        }

        // Atajo anotado: un tramo de manejo de más de 40 m se acorta moviendo el carro con Colocar no existe; no se usa. Se maneja siempre.

        // c) Sale a pie al carro, maneja al callejón (la emboscada se activa al acercarse) y baja.
        protected override IEnumerator LlegarALaEmboscada()
        {
            IMontable carro = Carro();
            Vector3 salida = jugador.Raiz.position;
            yield return SubirAlCarro(carro);
            // D-tony-187: ya en el carro, el objetivo y la flecha apuntan a la emboscada, no al carro.
            yield return EsperarHasta(() => Objetivos.HayObjetivo && Objetivos.Actual.Texto == TextoAEmboscada, 3f, "el objetivo en el carro dice '" + TextoAEmboscada + "'");
            Assert.Less(DistanciaPlana(Objetivos.Actual.Posicion, emboscada.transform.position), ToleranciaPunto, "El objetivo en el carro apunta a la emboscada");
            yield return Manejar("c1) clandestino a la esquina 1", carro, PuntoHistoria(IdEsquina1).Pose.position, RadioCarro);
            yield return Manejar("c2) esquina 1 al callejón (por la calle de la casa)", carro, PuntoHistoria(IdEsquina2).Pose.position, RadioParada, () => emboscada.Estado != EstadoEmboscada.Esperando);
            metrosHastaEmboscada = DistanciaPlana(salida, jugador.Raiz.position);
            yield return null;
            Vigilar();
            Assert.AreNotEqual(EstadoEmboscada.Esperando, emboscada.Estado, "Al acercarse al callejón la pandilla se activa");
            yield return FrenarYBajar(carro);
        }

        protected override IEnumerator IrALaZonaDeLlamado()
        {
            IPuntoLlamadoMecha punto = Buscar(Registro<IPuntoLlamadoMecha>.Todos, p => p.Habilitado, "un punto de llamado del mecha");
            Vector3 destino = punto.Llegada.position;
            Capturar(jugador.Camara, RutaCapturaCiudad2);
            yield return CaminarA(destino, Mathf.Max(DistanciaLlegada, punto.Radio * FraccionRadioLlamado), "la zona de llamado del callejón");
        }

        // d) Baja del mecha, lo devuelve, vuelve al carro, maneja por las esquinas, baja y entra a la casa por su puerta.
        protected override IEnumerator RegresarACasa()
        {
            float inicio = Time.time;
            yield return EsperarHasta(() => montaje.PuedeBajar, 5f, "el mecha puede bajar (detenido)");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return Tecla(teclado.mKey);

            IMontable carro = Carro();
            Assert.AreEqual(TextoRegreso, Objetivos.Actual.Texto, "El objetivo es regresar en el carro");
            yield return SubirAlCarro(carro);
            Vector3 esquina1 = PuntoHistoria(IdEsquina1).Pose.position;
            yield return AcortarManejo(carro, esquina1, "callejón a la esquina 1");
            yield return Manejar("d1) callejón a la esquina 1", carro, esquina1, 1f, HayObjetivoDeCasa);
            yield return EsperarHasta(HayObjetivoDeCasa, 3f, "el objetivo pasa a la casa");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, Historia.Etapa, "Sigue el regreso a casa");
            Assert.AreEqual(IdLugar.Casa, Objetivos.Actual.Lugar);
            Vector3 esquina2 = PuntoHistoria(IdEsquina2).Pose.position;
            yield return AcortarManejo(carro, esquina2, "esquina 1 a la casa");
            yield return Manejar("d2) esquina 1 frente a la casa", carro, esquina2, RadioParada);
            yield return FrenarYBajar(carro);

            IPuerta puerta = Buscar(Registro<IPuerta>.Todos, p => p.Lugar == IdLugar.Casa, "la puerta de la casa");
            Vector3 entrada = Aparicion(IdPuerta).Pose.position;
            Vector3 eje = puerta.Posicion - entrada;
            eje.y = 0f;
            eje.Normalize();
            float inicioCaminata = Time.time;
            yield return RodearCarro(carro, entrada - eje * MetrosAntesDeLaPuerta);
            yield return CaminarA(entrada - eje * MetrosAntesDeLaPuerta, RadioPuerta, "el eje de la puerta de la casa");
            yield return CaminarA(entrada, RadioPuerta, "frente a la puerta de la casa");
            yield return MirarA(puerta.Posicion, "la puerta de la casa");
            yield return Tecla(teclado.eKey);
            yield return EsperarSegundos(SegundosPuertaAbriendo);
            Assert.IsTrue(puerta.EstaAbierta, "E frente a la puerta de la casa la abre");
            Vector3 centroCasa = GameObject.Find("Lugar_Casa").transform.position;
            centroCasa.y = AlturaJugador;
            yield return CaminarA(centroCasa, DistanciaLlegada, "el centro de la casa", () => Historia.Etapa == EtapaPrologo.Dormir);
            yield return EsperarHasta(() => Historia.Etapa == EtapaPrologo.Dormir, 3f, "entrar a la casa lleva a dormir");
            yield return null;
            Vigilar();
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            Assert.AreEqual(TextoCama, Objetivos.Actual.Texto, "El objetivo es la cama");
            Assert.Less(DistanciaPlana(Objetivos.Actual.Posicion, cama.Posicion), ToleranciaPunto, "En la cama");
            tramos.Add("d3) caminata de la calle a la casa: " + (Time.time - inicioCaminata).ToString("F1", CultureInfo.InvariantCulture) + " s");
            AnotarTramo("d) regreso a casa", inicio);
        }

        // La mesa y los bancos quedan a un lado: se rodea por el pasillo entre los bancos y los pies de la cama.
        protected override IEnumerator IrALaCama()
        {
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            
            Vector3 pasillo = new Vector3(cama.Posicion.x + MetrosDelPasilloALaCama, AlturaJugador, Aparicion(IdPuerta).Pose.position.z);
            yield return CaminarA(pasillo, RadioPuerta, "el pasillo de la cama");
            Vector3 haciaCama = cama.Posicion - pasillo;
            haciaCama.y = 0f;
            yield return CaminarA(cama.Posicion - haciaCama.normalized * DistanciaCamaraDeLaCama, RadioPuerta, "junto a la cama");
            yield return MirarA(cama.Posicion, "la cama");
            Debug.Log("[Prueba T-136] Frente a la cama: jugador " + jugador.Raiz.position + ", cama " + cama.Posicion + ", candidato " + interaccion.TipoCandidato);
            Capturar(jugador.Camara, RutaCapturaCiudad3);
        }

        // f) Despierta en la cama, repara en el banco (dentro de la casa) y camina al clandestino por las calles de Alan.
        protected override IEnumerator DespertarReparaYPelearLaCasera()
        {
            float inicio = Time.time;
            yield return EntrarAPrologo("el prólogo tras el flashback");
            yield return EsperarHasta(() => prologo.Iniciado && Historia.Etapa == EtapaPrologo.PeleaCasera, MaxSegundosEstado, "el despertar");

            IPuntoAparicion cama = Aparicion(IdCama);
            Assert.Less(DistanciaPlana(jugador.Raiz.position, cama.Pose.position), ToleranciaJugador - 0.1f, "El jugador despierta en Casa_Cama");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(DineroInicial, Cartera.Dinero, "Despierta con $500");
            Assert.AreEqual(PorcentajeDespertar, vidaMecha.Actual / vidaMecha.Maxima * 100f, ToleranciaPorcentaje, "Mecha al 45 %");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "Y en su taller");
            Assert.AreEqual(HoraDespertar, Reloj.Hora, ToleranciaHora, "Son las 20:00");
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, "Calor en 0");
            Assert.IsTrue(subtitulos.SubtituloVisible, "El aviso está visible");
            yield return Cuadros(FramesParaElHud, false);
            Assert.AreEqual(TextoClandestino, hud.TextoObjetivo.text, "El HUD dice a dónde ir");
            AnotarTramo("f1) despertar", inicio);

            float inicioBanco = Time.time;
            Assert.Greater(persistente.Danio, 0f, "El mecha despertó dañado");
            
            Vector3 punto = banco.Posicion;
            Vector3 pasillo = new Vector3(cama.Pose.position.x + MetrosDelPasilloALaCama, AlturaJugador, Aparicion(IdPuerta).Pose.position.z);
            yield return CaminarA(pasillo, RadioPuerta, "el pasillo de la cama");
            yield return CaminarA(punto, DistanciaAlBanco, "el banco del taller");
            yield return MirarA(punto, "el banco del taller");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el banco");
            Assert.IsTrue(banco.PuedeUsar, "El banco ofrece reparar: " + banco.Texto);
            int costo = banco.Costo;
            Assert.Greater(costo, 0, "Reparar cuesta");
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroInicial - costo, Cartera.Dinero, "Dinero = 500 - costo exacto de la reparación (" + costo + ")");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, 0.01f, "Vida al máximo");
            AnotarTramo("f2) reparar en el banco (costo $" + costo + ")", inicioBanco);

            // Al clandestino a pie, por la puerta de la casa y las dos esquinas: sin atajos.
            float inicioCasera = Time.time;
            Assert.AreEqual(TextoClandestino, Objetivos.Actual.Texto, "Antes de entrar el objetivo es el clandestino");
            IPuerta puertaCasa = Buscar(Registro<IPuerta>.Todos, p => p.Lugar == IdLugar.Casa, "la puerta de la casa");
            Vector3 entradaCasa = Aparicion(IdPuerta).Pose.position;
            yield return CaminarA(pasillo, RadioPuerta, "el pasillo de la casa");
            yield return CaminarA(new Vector3(entradaCasa.x - MetrosAntesDeLaPuerta, AlturaJugador, entradaCasa.z), RadioPuerta, "frente a la puerta por dentro");
            yield return MirarA(puertaCasa.Posicion, "la puerta de la casa");
            if (!puertaCasa.EstaAbierta)
            {
                yield return Tecla(teclado.eKey);
                yield return EsperarSegundos(SegundosPuertaAbriendo);
            }
            yield return CaminarA(entradaCasa, RadioPuerta, "la salida de la casa");
            yield return CaminarA(PuntoHistoria(IdEsquina2).Pose.position, DistanciaLlegada, "la esquina 2");
            yield return CaminarA(PuntoHistoria(IdEsquina1).Pose.position, DistanciaLlegada, "la esquina 1");
            IPuerta puertaClandestino = Buscar(Registro<IPuerta>.Todos, p => p.Lugar == IdLugar.Clandestino, "la puerta del clandestino");
            Vector3 salida = Aparicion(IdSalida).Pose.position;
            yield return CaminarA(salida, DistanciaAlaPuertaDelClandestino, "la entrada del clandestino");
            yield return MirarA(puertaClandestino.Posicion, "la puerta del clandestino");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Puerta, 3f, "la interacción ofrece la puerta del clandestino");
            for (int intento = 0; intento < IntentosPuerta && !puertaClandestino.EstaAbierta; intento++)
            {
                yield return Tecla(teclado.eKey);
                yield return EsperarSegundos(SegundosPuertaAbriendo);
            }
            Assert.IsTrue(puertaClandestino.EstaAbierta, "E abre la puerta del clandestino");
            yield return CaminarA(salida + (puertaClandestino.Posicion - salida).normalized * MetrosAdentro, DistanciaLlegada, "adentro del clandestino");
            yield return EsperarHasta(() => Objetivos.Actual.Texto == TextoMostrador, 3f, "al entrar al clandestino el objetivo pasa al mostrador");
            Assert.Less(Vector3.Distance(Objetivos.Actual.Posicion, mostrador.Posicion), ToleranciaPunto, "El mostrador");
            yield return CaminarA(mostrador.Posicion, DistanciaAlMostrador, "el mostrador");
            yield return MirarA(mostrador.Posicion, "el mostrador");
            Capturar(jugador.Camara, RutaCapturaCiudad1);
            if (!Reloj.EsDeNoche)
            {
                Reloj.AvanzarHasta(HoraAdelantada);
                atajos.Add("AvanzarHasta(" + HoraAdelantada.ToString("F1", CultureInfo.InvariantCulture) + "): todavía era de día al llegar al mostrador");
                yield return null;
                Vigilar();
            }
            yield return EsperarHasta(() => mostrador.PuedeUsar, 3f, "el mostrador ofrece la pelea: " + mostrador.Texto);
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el mostrador");

            vigilarVida = false;
            int dineroAntes = Cartera.Dinero;
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey);
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => flujoCasero.Estado == MechaCyberpunk.Pelea.Flujo.EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea casera");
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales de la casera");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((MechaCyberpunk.Combate.Vida.VidaMecha)flujoCasero.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: rival casero rematado tras daño real al mecha");
            yield return EsperarHasta(() => pantallaContinuara.Visible, MaxSegundosEstado, "la pantalla de continuará");
            yield return null;
            Vigilar();
            Assert.AreEqual(dineroAntes + PremioCasero, Cartera.Dinero, "Ganar la casera paga $600");
            AnotarTramo("f3) ir al clandestino y ganar la casera", inicioCasera);
        }
    }
}
