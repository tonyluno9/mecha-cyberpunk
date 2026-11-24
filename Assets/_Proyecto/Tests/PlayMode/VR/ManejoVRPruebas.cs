using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR21: en VR se maneja. B cerca sube; la cabeza queda dentro de la cabina, sobre el asiento del conductor y mirando al frente, pegada al
    /// vehículo; el gatillo derecho acelera (analógico), el izquierdo frena, el stick izquierdo dirige y el derecho ya no gira la vista; solo la
    /// cabeza va al visor (la cámara del carro de PC se apaga); B baja y el rig vuelve a pie recentrado. Igual en moto.
    /// </summary>
    public class ManejoVRPruebas : BaseJugadorVR
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaMoto = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_C19.prefab";
        private const float AlturaPiso = 0f;

        private CarroViejo creado;

        private IEnumerator CrearVehiculo(string ruta, Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta el prefab " + ruta);
            GameObject objeto = Object.Instantiate(prefab, posicion, Quaternion.identity);
            extras.Add(objeto);
            yield return Segundos(1f);
            creado = objeto.GetComponentInChildren<CarroViejo>();
            Assert.IsNotNull(creado, "El vehículo es un CarroViejo.");
            CollectionAssert.Contains(Registro<IMontable>.Todos, creado, "El vehículo se registra.");
        }

        // Junto a la puerta del conductor, mirando al vehículo; B sube.
        private IEnumerator SubirConB(CarroViejo vehiculo)
        {
            jugadorAPie.Colocar(new Pose(vehiculo.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            float fin = Time.time + 1.5f;
            while (!ReferenceEquals(interaccion.MontableCandidato, vehiculo) && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreSame(vehiculo, interaccion.MontableCandidato, "El vehículo es el candidato de B.");
            yield return Pulsar(controlDer.secondaryButton);
            fin = Time.time + 1f;
            while (jugadorAPie.Modo != ModoJugador.Carro && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "En VR B sube al vehículo.");
            Assert.AreSame(vehiculo, jugadorAPie.MontableActual);
            // El fundido de subir y que el vehículo se asiente.
            yield return Segundos(0.6f);
        }

        // Frena con el gatillo izquierdo (y el freno de mano, A) hasta casi parar.
        private IEnumerator Parar(CarroViejo vehiculo)
        {
            Stick(false, Vector2.zero);
            Set(controlDer.trigger, 0f);
            Set(controlIzq.trigger, 1f);
            float fin = Time.time + 8f;
            while (vehiculo.Velocidad > 0.3f && Time.time < fin)
            {
                yield return null;
            }
            Set(controlIzq.trigger, 0f);
            Mantener(controlDer.primaryButton);
            fin = Time.time + 5f;
            while (vehiculo.Rapidez > 0.3f && Time.time < fin)
            {
                yield return null;
            }
            Soltar(controlDer.primaryButton);
            yield return Cuadros(3);
            Assert.Less(vehiculo.Rapidez, 0.5f, "Se detiene para poder bajar.");
        }

        private IEnumerator BajarConB(CarroViejo vehiculo, ManejoVR manejo)
        {
            yield return Pulsar(controlDer.secondaryButton);
            float fin = Time.time + 1f;
            while (jugadorAPie.Modo != ModoJugador.APie && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "B baja.");
            Assert.IsFalse(manejo.Manejando);
            Assert.AreEqual(0, manejo.BindingsPuestos);
            Assert.AreEqual(0, BindingsVR(vehiculo), "Los bindings VR del vehículo se quitan al bajar.");
            yield return Cuadros(5);
            Camera cabeza = adaptador.Camara;
            Vector3 pies = jugador.transform.position;
            Debug.Log("[VR] A pie tras bajar: cabeza a " + (cabeza.transform.position.y - pies.y).ToString("0.00") + " m sobre los pies.");
            Assert.AreEqual(AdaptadorVR.AlturaOjos, cabeza.transform.position.y - pies.y, 0.05f, "Recalibrado a pie: ojos a 1.72 m.");
            Assert.Less(Vector2.Distance(new Vector2(pies.x, pies.z), new Vector2(cabeza.transform.position.x, cabeza.transform.position.z)), 0.05f,
                "La cabeza vuelve sobre la cápsula.");
            Assert.IsTrue(cabeza.transform.IsChildOf(jugador.transform), "La cabeza sigue en el rig del jugador.");
            Assert.IsTrue(cabeza.enabled && SistemaVR.VaAlVisor(cabeza), "La cabeza sigue en el visor.");
            Assert.IsFalse(vehiculo.Camara.enabled, "La cámara del vehículo queda apagada.");
            Behaviour pistola = jugador.GetComponent<PistolaVR>();
            if (pistola != null)
            {
                Assert.IsTrue(pistola.enabled, "La pistola VR vuelve al bajar.");
            }
        }

        private static int BindingsVR(CarroViejo vehiculo)
        {
            int cuenta = 0;
            InputActionMap mapa = vehiculo.MapaControles;
            if (mapa == null)
            {
                return 0;
            }
            foreach (InputBinding b in mapa.bindings)
            {
                if (b.groups == "VR")
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        private static Bounds BoundsColisiones(CarroViejo vehiculo)
        {
            Bounds caja = default;
            bool hay = false;
            foreach (Collider c in vehiculo.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger)
                {
                    continue;
                }
                if (!hay)
                {
                    caja = c.bounds;
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(c.bounds);
                }
            }
            Assert.IsTrue(hay, "El vehículo tiene colisiones.");
            return caja;
        }

        private static float Alineacion(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Dot(a.normalized, b.normalized);
        }

        // La cabeza al subir: en los ojos del conductor, mirando al frente, solo ella en el visor.
        private ManejoVR RevisarCabezaAlSubir(CarroViejo vehiculo, out Vector3 local, out float altura)
        {
            ManejoVR manejo = jugador.GetComponent<ManejoVR>();
            Assert.IsNotNull(manejo, "El manejo VR se agrega solo al jugador al armarse el rig.");
            Assert.IsTrue(manejo.Manejando);
            Assert.AreSame(vehiculo, manejo.Vehiculo);
            Camera cabeza = adaptador.Camara;
            local = vehiculo.transform.InverseTransformPoint(cabeza.transform.position);
            altura = cabeza.transform.position.y - AlturaPiso;
            Debug.Log("[VR] " + vehiculo.Nombre + ": cabeza en " + local.ToString("F2") + " (local), a " + altura.ToString("0.00") + " m del piso; ojos "
                + manejo.OjosLocal.ToString("F2") + (manejo.AsientoEstimado ? " estimados" : " del asiento") + ".");
            Assert.Less(Vector3.Distance(cabeza.transform.position, manejo.OjosMundo), 0.05f, "La cabeza va en los ojos del conductor.");
            Assert.Greater(Alineacion(cabeza.transform.forward, vehiculo.transform.forward), 0.98f, "Mira al frente del vehículo.");
            Assert.IsTrue(cabeza.enabled, "La cabeza sigue prendida dentro del vehículo.");
            Assert.IsTrue(SistemaVR.VaAlVisor(cabeza), "La cabeza va al visor.");
            Assert.IsFalse(vehiculo.Camara.enabled && SistemaVR.VaAlVisor(vehiculo.Camara), "La cámara del vehículo de PC no va al visor.");
            Assert.IsFalse(vehiculo.Camara.enabled, "La cámara del vehículo de PC (sacudidas, FOV) se apaga en VR.");
            AudioListener oido = cabeza.GetComponent<AudioListener>();
            if (oido != null)
            {
                Assert.IsTrue(oido.enabled, "Se oye desde la cabeza.");
            }
            Assert.Greater(manejo.BindingsPuestos, 2, "Gatillos y stick van a las acciones del vehículo.");
            return manejo;
        }

        [UnityTest]
        public IEnumerator Carro_BSube_CabezaEnElAsiento_GatillosYStickManejan_BBaja()
        {
            yield return ArmarVR();
            yield return CrearVehiculo(RutaCarro, new Vector3(0f, 0f, 4f));
            CarroViejo carro = creado;
            yield return SubirConB(carro);

            ManejoVR manejo = RevisarCabezaAlSubir(carro, out Vector3 local, out float altura);
            Assert.Less(local.x, -0.1f, "Del lado del conductor (izquierda).");
            Assert.That(altura, Is.InRange(0.9f, 1.6f), "A la altura de los ojos de alguien sentado.");
            Bounds cabina = BoundsColisiones(carro);
            cabina.Expand(0.05f);
            Assert.IsTrue(cabina.Contains(adaptador.Camara.transform.position), "La cabeza va dentro de la cabina (no sobre el techo).");

            MedidorCabezaVR medidor = jugador.AddComponent<MedidorCabezaVR>();
            medidor.Empezar(adaptador.Camara.transform, carro.transform, manejo);

            // Gatillo derecho a medias: el acelerador es analógico.
            Vector3 inicio = carro.transform.position;
            Set(controlDer.trigger, 0.3f);
            yield return Segundos(0.3f);
            Debug.Log("[VR] Gatillo derecho a 0.3: acelerar " + carro.EntradaActual.Acelerar.ToString("0.00") + ".");
            Assert.AreEqual(0.3f, carro.EntradaActual.Acelerar, 0.05f, "El gatillo derecho acelera en proporción.");

            // A fondo: avanza.
            Set(controlDer.trigger, 1f);
            yield return Segundos(2f);
            float avance = Vector3.Dot(carro.transform.position - inicio, carro.transform.forward);
            Debug.Log("[VR] Con el gatillo derecho: " + avance.ToString("0.0") + " m, " + carro.Velocidad.ToString("0.0") + " m/s.");
            Assert.Greater(carro.Velocidad, 2f, "El gatillo derecho hace avanzar.");
            Assert.Greater(avance, 2f);

            // El stick derecho ya no gira la vista dentro del carro.
            float giroRig = adaptador.GiroRig;
            Stick(true, new Vector2(1f, 0f));
            yield return Cuadros(10);
            Stick(true, Vector2.zero);
            Assert.AreEqual(giroRig, adaptador.GiroRig, 0.01f, "Dentro del vehículo el stick derecho no gira.");

            // Stick izquierdo a la derecha: dobla a la derecha (y la cabeza sigue al frente del carro).
            float yawAntes = carro.transform.eulerAngles.y;
            Stick(false, new Vector2(1f, 0f));
            yield return Segundos(1.2f);
            float doblado = Mathf.DeltaAngle(yawAntes, carro.transform.eulerAngles.y);
            Debug.Log("[VR] Con el stick izquierdo: volante " + carro.Volante.ToString("0.00") + ", dobló " + doblado.ToString("0") + "°.");
            Assert.Greater(carro.Volante, 0.5f, "El stick izquierdo mueve el volante.");
            Assert.Greater(doblado, 10f, "Dobla a la derecha.");
            Assert.Greater(Alineacion(adaptador.Camara.transform.forward, carro.transform.forward), 0.98f, "La cabeza gira con el carro.");
            Stick(false, Vector2.zero);
            if (OpcionesVR.Vineta)
            {
                Assert.IsTrue(medidor.VinetaVista, "Al doblar o acelerar se pone la viñeta (VIÑETA: SÍ).");
            }

            // Gatillo izquierdo: frena.
            Set(controlDer.trigger, 0f);
            yield return Cuadros(2);
            float antes = carro.Velocidad;
            Set(controlIzq.trigger, 1f);
            yield return Segundos(0.6f);
            Debug.Log("[VR] Con el gatillo izquierdo: de " + antes.ToString("0.0") + " a " + carro.Velocidad.ToString("0.0") + " m/s.");
            Assert.Greater(carro.EntradaActual.Frenar, 0.9f, "El gatillo izquierdo frena.");
            Assert.Less(carro.Velocidad, antes - 1f, "Frena.");

            medidor.Terminar();
            Debug.Log("[VR] Cabeza en el carro: se apartó como mucho " + medidor.DesvioMaximo.ToString("0.000") + " m de su lugar en " + medidor.Cuadros + " cuadros.");
            Assert.Greater(medidor.Cuadros, 30);
            Assert.Less(medidor.DesvioMaximo, 0.1f, "La cabeza va pegada al carro, sin retraso.");

            yield return Parar(carro);
            yield return BajarConB(carro, manejo);
        }

        [UnityTest]
        public IEnumerator Moto_BSube_CabezaSobreElAsiento_GatilloAvanza_BBaja()
        {
            yield return ArmarVR();
            yield return CrearVehiculo(RutaMoto, new Vector3(0f, 0f, 4f));
            CarroViejo moto = creado;
            Assert.IsNotNull(moto.GetComponent<ManejoMoto>(), "El C19 es una moto.");
            yield return SubirConB(moto);

            ManejoVR manejo = RevisarCabezaAlSubir(moto, out Vector3 local, out float altura);
            Assert.Less(Mathf.Abs(local.x), 0.15f, "En moto la cabeza va al centro, sobre el asiento.");
            Assert.That(altura, Is.InRange(1.0f, 1.9f), "A la altura de los ojos de quien va en moto.");

            Vector3 inicio = moto.transform.position;
            Set(controlDer.trigger, 1f);
            yield return Segundos(1.5f);
            float avance = Vector3.Dot(moto.transform.position - inicio, moto.transform.forward);
            Debug.Log("[VR] Moto con el gatillo derecho: " + avance.ToString("0.0") + " m, " + moto.Velocidad.ToString("0.0") + " m/s.");
            Assert.Greater(moto.Velocidad, 1.5f, "El gatillo derecho hace avanzar la moto.");
            Assert.Greater(avance, 1f);
            Assert.Less(Vector3.Distance(adaptador.Camara.transform.position, manejo.OjosMundo), 0.3f, "La cabeza va con la moto.");

            yield return Parar(moto);
            yield return BajarConB(moto, manejo);
        }
    }
}
