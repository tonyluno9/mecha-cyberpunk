using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>
    /// T-073: la emboscada de Tony en el callejón del barrio de Alan (Pruebas_Alan.unity). La escena se carga sin editarla ni
    /// guardarla (D-029): el jugador, el mecha y la emboscada se instancian en tiempo de juego y todo se encuentra por los
    /// registros de Core (IJugador, IPuntoAparicion, IPuntoLlamadoMecha, ITallerMecha, IMontable). Reutiliza los pasos del
    /// recorrido de T-072 (RecorridoEmboscadaBase).
    /// </summary>
    public class CallejonAlanPruebas : RecorridoEmboscadaBase
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaEmboscada = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const string RutaCaptura = "docs/capturas/T-073-1.png";
        private const string IdAparicion = "Callejon_Entrada";
        private const string NombreAnclaje = "Emboscada_Anclaje";
        private const float HoraNoche = 22f;
        private const float MetrosMaximosAlTaller = 0.1f;
        // Dónde espera el jugador el mecha, medido desde la llegada (x a la derecha, z adelante), como en T-072.
        private static readonly Vector3 EsperaLocal = new Vector3(-2.5f, 0f, 0f);
        private static readonly Vector3 JuntoAlMechaLocal = new Vector3(-2f, 0f, 1.8f);
        private const float AlturaMinimaCaptura = 6f;
        private const float AlturaMaximaCaptura = 12f;
        private const float DistanciaCamaraCaptura = 14f;
        private const float AlturaCamaraCaptura = 1.7f;
        private const float MargenAlturaPies = 0.1f;
        private const float RadioPies = 0.3f;
        private const float AlturaCuerpo = 1.7f;

        private IPuntoLlamadoMecha puntoLlamado;
        private ITallerMecha taller;
        private bool capturado;

        public override void Setup()
        {
            // Otras pruebas dejan acciones prendidas; el reinicio del fixture las dejaría con un estado viejo.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
        }

        public override void TearDown()
        {
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        // Cambiar `devices` en un mapa apagado descarta su estado de resolución: la escena los resuelve de nuevo al prenderlos.
        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        // ---- Preparación ----

        private IEnumerator CargarCallejon()
        {
            yield return CargarEscena(RutaEscena);

            // La cámara libre de Alan también es IJugador: se apaga su GameObject sin conocer su clase (como en T-060).
            foreach (IJugador otro in new List<IJugador>(Registro<IJugador>.Todos))
            {
                if (otro is Component componente)
                {
                    componente.gameObject.SetActive(false);
                }
            }

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(reloj, "El barrio trae su reloj");
            reloj.Pausar(true);
            reloj.AvanzarHasta(HoraNoche);

            taller = Registro<ITallerMecha>.Actual;
            Assert.AreEqual(1, Registro<ITallerMecha>.Cantidad, "La casa trae exactamente un taller");
            GameObject anclaje = GameObject.Find(NombreAnclaje);
            Assert.IsNotNull(anclaje, "El callejón de Alan trae el anclaje de la emboscada (" + NombreAnclaje + ")");

            GameObject jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugadorObjeto.GetComponent<AparicionJugador>().IdAparicion = IdAparicion;
            Pose pose = taller.Estacionamiento;
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha), pose.position, pose.rotation);
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaEmboscada), anclaje.transform.position, anclaje.transform.rotation);
            yield return null;
            yield return null;

            ResolverReferencias();
            yield return null;

            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo IJugador registrado");
            Assert.AreSame(jugador, Registro<IJugador>.Actual, "Y es el de Tony");
            int mechas = 0;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable.Modo == ModoJugador.Mecha)
                {
                    mechas++;
                }
            }
            Assert.AreEqual(1, mechas, "Un solo IMontable de modo Mecha");

            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha empieza en el taller");
            Assert.Less(Vector3.Distance(montaje.transform.position, taller.Estacionamiento.position), MetrosMaximosAlTaller + llamado.Config.SeparacionPiso,
                "Al empezar, el mecha está en el Estacionamiento del taller de la casa");
            Assert.IsNotNull(Registro<IPuntoLlamadoMecha>.Actual, "El callejón trae un punto de llamado");
            puntoLlamado = Registro<IPuntoLlamadoMecha>.Actual;

            Pose llegada = puntoLlamado.Llegada;
            juntoAlMecha = llegada.position + llegada.rotation * JuntoAlMechaLocal;
            EmpezarMedicion();
        }

        // a) El jugador aparece en la boca del callejón: la pandilla sale y él espera el mecha en la zona de llamado.
        private IEnumerator EntrarAlCallejon()
        {
            yield return EsperarHasta(() => director.Estado != EstadoEmboscada.Esperando, 10f, "al aparecer en la boca del callejón la pandilla sale");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "la pandilla termina de aparecer");
            Assert.AreEqual(2, director.Rivales.Count, "Aparecen dos pandilleros");
            Pose llegada = puntoLlamado.Llegada;
            Vector3 espera = llegada.position + llegada.rotation * EsperaLocal;
            yield return CaminarA(espera, DistanciaLlegada, "la zona de llamado");
        }

        // Captura del mecha bajando del cielo, desde el lado de la boca del callejón.
        private void TomarCapturaDelDescenso()
        {
            if (capturado || llamado.Estado != EstadoLlamado.Llegando)
            {
                return;
            }
            float altura = montaje.transform.position.y - puntoLlamado.Llegada.position.y;
            if (altura < AlturaMinimaCaptura || altura > AlturaMaximaCaptura)
            {
                return;
            }
            capturado = true;
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            Vector3 haciaAtras = -(montaje.transform.forward);
            haciaAtras.y = 0f;
            Vector3 posicion = montaje.transform.position + haciaAtras.normalized * DistanciaCamaraCaptura;
            posicion.y = puntoLlamado.Llegada.position.y + AlturaCamaraCaptura;
            objeto.transform.position = posicion;
            objeto.transform.LookAt(montaje.transform.position);
            Capturar(RutaCaptura, camara);
            Object.Destroy(objeto);
        }

        protected override string VariableCaptura => "CAPTURAR_T073";

        // La pose de salida queda libre: ningún collider ajeno al jugador y al mecha cruza el cuerpo del jugador (el piso no cuenta).
        private void AssertPoseDeSalidaLibre()
        {
            CharacterController cuerpo = jugador.GetComponent<CharacterController>();
            Vector3 pies = jugador.transform.position;
            Vector3 abajo = pies + Vector3.up * (RadioPies + MargenAlturaPies);
            Vector3 arriba = pies + Vector3.up * (AlturaCuerpo - RadioPies);
            Collider[] cercanos = Physics.OverlapCapsule(abajo, arriba, RadioPies, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider otro in cercanos)
            {
                if (otro.transform.root == jugador.transform.root || otro.transform.root == montaje.transform.root)
                {
                    continue;
                }
                Assert.Fail("La pose de salida (" + pies + ") cruza el collider '" + otro.name + "' de '" + otro.transform.root.name + "'");
            }
            Assert.IsNotNull(cuerpo, "El jugador tiene su CharacterController");
        }

        // ---- Prueba ----

        [UnityTest]
        public IEnumerator Emboscada_EnElCallejonDeAlan_SeGanaDesdeElTallerDeLaCasa()
        {
            capturado = false;
            yield return CargarCallejon();
            alFrame = TomarCapturaDelDescenso;
            int terminadas = 0;
            director.Terminada += () => terminadas++;
            int jugadoresMax = 1;
            int mechasMax = 1;
            alFrame += () =>
            {
                jugadoresMax = Mathf.Max(jugadoresMax, Registro<IJugador>.Cantidad);
                int mechas = 0;
                foreach (IMontable montable in Registro<IMontable>.Todos)
                {
                    mechas += montable.Modo == ModoJugador.Mecha ? 1 : 0;
                }
                mechasMax = Mathf.Max(mechasMax, mechas);
            };

            yield return EntrarAlCallejon();
            yield return LlamarYSubir();
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Con el mecha encendido la pandilla pelea");
            yield return Pelear(null, null);
            yield return BajarYDevolver();

            AssertPoseDeSalidaLibre();
            Assert.AreEqual(1, terminadas, "Terminada llega una sola vez");
            Assert.AreEqual(1, jugadoresMax, "Siempre hubo un solo IJugador registrado");
            Assert.AreEqual(1, mechasMax, "Siempre hubo un solo IMontable de modo Mecha");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador termina a pie");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha termina en el taller");
            Assert.Less(Vector3.Distance(montaje.transform.position, taller.Estacionamiento.position), MetrosMaximosAlTaller + llamado.Config.SeparacionPiso,
                "El mecha quedó en el Estacionamiento del taller de la casa");
            Assert.Greater(vidaMinima, 0f, "La vida del mecha nunca llegó a 0");
            Assert.GreaterOrEqual(vidaMinima, piso - 0.001f, "Nunca bajó del piso de piedad");
            Assert.IsTrue(Environment.GetEnvironmentVariable(VariableCaptura) != "1" || capturado, "Se tomó la captura del descenso");
            AssertSinErrores();
        }
    }
}
