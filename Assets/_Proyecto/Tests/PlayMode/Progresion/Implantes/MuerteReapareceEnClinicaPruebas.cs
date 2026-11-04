using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-586, en la ciudad de verdad (Ciudad_Prologo): daño simulado hasta morir a pie. El jugador cae, la pantalla se va a negro,
    /// despierta en I+D+I junto al Doc con la vida llena y el doctor cobra lo que alcance (con dinero de sobra el cobro completo;
    /// sin dinero suficiente se queda en 0, nunca en deuda). Con CAPTURAR_T586=1 guarda docs/capturas/T-586-reaparecer.png.
    /// </summary>
    public class MuerteReapareceEnClinicaPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCaptura = "docs/capturas/T-586-reaparecer.png";
        private const string VariableCaptura = "CAPTURAR_T586";
        private const string IdPuntoCalle = "Callejon_Entrada";
        private const string IdDoc = "doc";
        private const float DanioPorGolpe = 60f;
        private const int DineroInsuficiente = 40;
        private const int MaxGolpes = 20;
        private const float SegundosCalentamiento = 3f;
        private const float MaxSegundosSecuencia = 40f;
        private const float DistanciaMaxDelDoc = 3f;
        private const float DistanciaMaxDeLaClinica = 8f;
        private const float DistanciaMinDondeCayo = 20f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;

        private JugadorAPie jugador;
        private VidaAPie vida;
        private ControladorAPie controlador;
        private ICartera cartera;
        private DoctorClinica doctor;
        private DatosDoctor datos;
        private readonly System.Collections.Generic.List<string> cambios = new System.Collections.Generic.List<string>();

        [TearDown]
        public void Limpiar()
        {
            EventosPelea.LimpiarSuscriptores();
        }

        private IEnumerator Preparar(int dineroInicial)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float fin = Time.time + SegundosCalentamiento;
            while (Time.time < fin)
            {
                yield return null;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(jugador, "la ciudad trae al jugador");
            Assert.IsNotNull(doctor, "la ciudad trae al doctor");
            datos = doctor.Datos;
            vida = jugador.GetComponent<VidaAPie>();
            controlador = jugador.GetComponent<ControladorAPie>();
            cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "hay cartera");
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            Assert.IsNotNull(historia, "hay historia");
            historia.Avanzar(EtapaPrologo.Libre);
            Assert.AreEqual(EtapaPrologo.Libre, historia.Etapa, "juego libre: el doctor atiende las caídas");

            if (dineroInicial > cartera.Dinero)
            {
                cartera.Abonar(dineroInicial - cartera.Dinero, MotivoDinero.Otro);
            }
            else if (dineroInicial < cartera.Dinero)
            {
                cartera.IntentarPagar(cartera.Dinero - dineroInicial, MotivoDinero.Otro);
            }
            Assert.AreEqual(dineroInicial, cartera.Dinero, "dinero de partida");
            cambios.Clear();
            cartera.Cambio += (delta, motivo) => cambios.Add(delta + " " + motivo);
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == IdPuntoCalle)
                {
                    jugador.Colocar(punto.Pose);
                }
            }
            yield return null;
            yield return null;
        }

        private static Vector3 PlanoA(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d;
        }

        private IEnumerator MatarYEsperar(bool capturar)
        {
            Vector3 dondeCayo = jugador.transform.position;
            for (int i = 0; i < MaxGolpes && !vida.EstaKO; i++)
            {
                IRecibeDanio recibe = jugador.GetComponent<IRecibeDanio>();
                recibe.RecibirDanio(new InfoDanio(DanioPorGolpe, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
                yield return null;
            }
            Assert.IsTrue(vida.EstaKO, "el daño simulado dejó al jugador en KO");
            Assert.IsTrue(doctor.Ocupado || doctor.Caidas > 0, "el doctor atiende la caída (sin reaparecer de golpe)");
            Assert.IsFalse(controlador.ControlActivo, "al caer no hay control");
            Assert.Less(PlanoA(jugador.transform.position, dondeCayo).magnitude, 1f, "todavía está donde cayó mientras cae");

            bool vioNegro = false;
            float fin = Time.time + MaxSegundosSecuencia;
            while ((doctor.Ocupado || doctor.Caidas == 0) && Time.time < fin)
            {
                PantallaFundido fundido = PantallaFundido.Actual;
                if (fundido != null && fundido.Opaco) vioNegro = true;
                yield return null;
            }
            Assert.IsFalse(doctor.Ocupado, "la secuencia terminó");
            Assert.IsTrue(vioNegro, "la pantalla se fue a negro (fundido)");
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(vida.EstaKO, "ya no está en KO");
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.01f, "vida restaurada al máximo");
            Assert.IsTrue(controlador.ControlActivo, "el control volvió");
            Assert.Greater(PlanoA(jugador.transform.position, dondeCayo).magnitude, DistanciaMinDondeCayo, "reapareció lejos de donde cayó");

            ILugar clinica = null;
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == IdLugar.Clinica) clinica = lugar;
            }
            Assert.IsNotNull(clinica, "la ciudad trae I+D+I (IdLugar.Clinica)");
            Assert.Less(PlanoA(jugador.transform.position, clinica.PosicionEntrada).magnitude, DistanciaMaxDeLaClinica, "despierta en I+D+I");

            PersonajeApoyo elDoc = null;
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (p.Datos != null && p.Datos.Id == IdDoc) elDoc = p;
            }
            Assert.IsNotNull(elDoc, "El Doc está en la ciudad");
            Assert.Less(PlanoA(jugador.transform.position, elDoc.transform.position).magnitude, DistanciaMaxDelDoc, "El Doc queda al lado");

            if (capturar && Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                Capturar(Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura)));
            }
        }

        private void Capturar(string ruta)
        {
            Camera camara = jugador.Camara;
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
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator Morir_ConDinero_CaeFundeDespiertaJuntoAlDocYCobraElCobroCompleto()
        {
            yield return Preparar(1200);
            yield return MatarYEsperar(true);
            Assert.AreEqual(1200 - datos.Cobro, cartera.Dinero, "el doctor cobró el cobro completo");
            Assert.AreEqual(1, doctor.Caidas, "una sola secuencia");
        }

        [UnityTest]
        public IEnumerator Morir_SinDineroSuficiente_QuedaEn0SinDeudaYNoSeBloquea()
        {
            yield return Preparar(DineroInsuficiente);
            Assert.Greater(datos.Cobro, DineroInsuficiente, "el cobro es mayor que el dinero de la prueba");
            yield return MatarYEsperar(false);
            Assert.AreEqual(0, cartera.Dinero, "se cobró lo que había y queda en 0, no en deuda; cambios: " + string.Join(", ", cambios));
            Assert.AreEqual(datos.TextoDoctor + " " + datos.TextoDeuda, doctor.UltimoTexto, "el doctor avisa que no traía todo");
            // No se bloquea: se puede volver a caer y el doctor lo atiende otra vez sin cobrar nada.
            vida.RecibirDanio(new InfoDanio(DanioPorGolpe * MaxGolpes, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO, "puede volver a caer");
            float fin = Time.time + MaxSegundosSecuencia;
            while ((doctor.Ocupado || doctor.Caidas < 2) && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsFalse(doctor.Ocupado, "la segunda secuencia terminó");
            Assert.AreEqual(2, doctor.Caidas);
            Assert.AreEqual(0, cartera.Dinero, "sigue en 0");
            Assert.IsFalse(vida.EstaKO, "volvió a despertar");
        }
    }
}
