using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Misiones;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Tests.Pelea;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-196 c): en Pruebas_Misiones, LOS DEL CALLEJÓN en el mecha con su KO por código: la misión falla una sola vez por KO y el doctor
    /// lleva al jugador a pie a la camilla con el mecha en el taller. Atajos permitidos y anotados: aceptar la misión por código, colocar
    /// al jugador cerca del callejón y el daño de KO al mecha por código. Llamar al mecha y subir se juega con M y E; el mecha sigue
    /// apagado, así que la bajada normal está bloqueada y el doctor usa la bajada forzada.
    /// </summary>
    public class CaerEnLaCalleMisionPruebas : RecorridoEmboscadaBase
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        private const string IdCallejon = "barrio_callejon";
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const float DanioKOPorCodigo = 100000f;
        private const float MaxSegundosDoctor = 40f;
        private const float DistanciaCamillaMax = 1.5f;
        private const float YawNorte = 0f;
        private const float ToleranciaFraccion = 0.01f;

        private string carpeta;
        private readonly List<string> atajos = new List<string>();
        private readonly List<(TipoMision, bool)> terminadas = new List<(TipoMision, bool)>();

        protected override string VariableCaptura => "CAPTURAR_T196";

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "doctor_mision_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            EventosPelea.LimpiarSuscriptores();
            atajos.Clear();
            terminadas.Clear();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            EventosPelea.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
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

        [UnityTest]
        public IEnumerator Callejon_ConElMechaEnKO_LaMisionFallaUnaVezYElDoctorLlevaAlJugadorALaCamilla()
        {
            yield return CargarEscena(RutaEscena);
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            interaccion = Object.FindFirstObjectByType<MechaCyberpunk.Personaje.Interaccion.InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MechaCyberpunk.Mecha.Montaje.MontajeMecha>();
            PeleaDeMision peleas = Object.FindFirstObjectByType<PeleaDeMision>();
            DoctorClinica doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(jugador, "Pruebas_Misiones trae al jugador");
            Assert.IsNotNull(montaje, "hay un mecha");
            Assert.IsNotNull(peleas, "la escena trae PeleasDeMision");
            Assert.IsNotNull(doctor, "la escena trae al doctor");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<MechaCyberpunk.Mecha.Encendido.EncendidoMecha>();
            vidaMecha = montaje.GetComponent<MechaCyberpunk.Combate.Vida.VidaMecha>();
            sensibilidadAPie = UnityEditor.AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = UnityEditor.AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mecha.ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
            ServicioReputacion reputacion = Registro<ServicioReputacion>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            Assert.IsNotNull(misiones, "hay misiones");
            Assert.IsNotNull(reputacion, "hay reputación");
            Assert.IsNotNull(cartera, "hay cartera");
            ResumenMision resumen = default;
            misiones.Terminada += (d, exito, r) => resumen = r;
            Registro<IMisiones>.Actual.Terminada += (tipo, exito) => terminadas.Add((tipo, exito));
            saltarMedicion = true;
            for (int i = 0; i < FramesSinMedirTrasEscena; i++) yield return Frame();
            yield return Esperar(FramesParaElHud * 0.02f);
            EmpezarMedicion();
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");

            DatosMision datos = misiones.Catalogo.Buscar(IdCallejon);
            Assert.IsNotNull(datos, "el catálogo trae " + IdCallejon);
            PenalizacionMision castigo = ReglaMisiones.Penalizacion(datos);
            int dinero = cartera.Dinero;
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            DatosDoctor datosDoctor = doctor.Datos;

            Assert.IsTrue(misiones.Aceptar(datos), "se acepta la misión (atajo: por código)");
            atajos.Add("Aceptar por código: " + IdCallejon);
            yield return Frame();
            yield return Frame();
            Assert.IsTrue(peleas.HayPelea, "se levanta la emboscada");

            // Se acerca al callejón a pie, llama al mecha con M y sube con E.
            Vector3 puntoPelea = Punto("Mision_Pelea_Callejon").Pose.position;
            jugador.Colocar(new Pose(new Vector3(0f, AlturaJugador, 6f), Quaternion.Euler(0f, YawNorte, 0f)));
            atajos.Add("Colocar: calle norte-sur");
            yield return Esperar(FramesParaElHud * 0.02f);
            director = peleas.Director;
            yield return CaminarA(new Vector3(puntoPelea.x - 1f, AlturaJugador, puntoPelea.z - 6f), DistanciaLlegada, "la boca del callejón",
                () => director.Estado != EstadoEmboscada.Esperando);
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "los pandilleros terminan de aparecer");

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
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "el jugador va en el mecha");
            Assert.AreNotEqual(EstadoLlamado.EnTaller, llamado.Estado, "el mecha anda en la calle");
            Assert.IsFalse(montaje.PuedeBajar, "apagado, el mecha no deja bajar por el camino normal");

            atajos.Add("RecibirDanioSinGuardia al mecha del jugador (KO por código)");
            vidaMecha.RecibirDanioSinGuardia(new InfoDanio(DanioKOPorCodigo, TipoDanio.Golpe, montaje.transform.position, Vector3.forward, null));
            Assert.IsTrue(vidaMecha.EstaKO, "el mecha cayó en KO");
            yield return Frame();
            yield return Frame();
            Assert.IsFalse(misiones.HayMision, "la misión falló al caer el mecha");
            Assert.AreEqual(1, terminadas.Count, "falló una vez hasta ahora");

            float fin = Time.time + MaxSegundosDoctor;
            while ((doctor.Ocupado || doctor.Caidas == 0) && Time.time < fin) yield return Frame();
            yield return Esperar(FramesParaElHud * 0.02f);

            Assert.AreEqual(1, doctor.Caidas, "una sola secuencia del doctor");
            Assert.IsFalse(doctor.Ocupado, "la secuencia terminó");
            CollectionAssert.AreEqual(new[] { (TipoMision.Barrio, false) }, terminadas, "la misión falló una sola vez (sin doble castigo)");
            Assert.AreEqual(MotivoFalloMision.KO, resumen.Motivo, "falló por KO");
            Assert.AreEqual(barrio + castigo.Barrio, reputacion.Valor(TipoReputacion.Barrio), "barrio con una sola penalización");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "el doctor bajó al jugador del mecha");
            IPuntoAparicion camilla = null;
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == datosDoctor.IdAparicionCamilla) camilla = punto;
            }
            Assert.IsNotNull(camilla, "la escena trae la camilla");
            Vector3 delta = jugador.transform.position - camilla.Pose.position;
            delta.y = 0f;
            Assert.Less(delta.magnitude, DistanciaCamillaMax, "despierta en la camilla");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "el mecha volvió al taller");
            Assert.IsFalse(vidaMecha.EstaKO, "el mecha ya no está en KO");
            Assert.GreaterOrEqual(vidaMecha.Actual / vidaMecha.Maxima, 0.15f - ToleranciaFraccion, "no baja del piso de emergencia");
            Assert.AreEqual(dinero - datosDoctor.Cobro, cartera.Dinero, "el doctor cobró");
            Assert.IsFalse(peleas.HayPelea, "la emboscada se retiró");
            Assert.AreEqual(datosDoctor.TextoObjetivo, objetivos.Actual.Texto, "objetivo de volver a casa");
            Debug.Log("[T-196] c) vida del mecha " + vidaMecha.Actual / vidaMecha.Maxima + ", barrio " + barrio + " -> "
                + reputacion.Valor(TipoReputacion.Barrio) + ", atajos: " + string.Join("; ", atajos));
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
            Assert.IsTrue(medidor.Cumple, "sin tirones: " + medidor.Resumen());
        }
    }
}
