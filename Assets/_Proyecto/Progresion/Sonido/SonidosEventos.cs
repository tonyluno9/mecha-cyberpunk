using System.Collections;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: pide los sonidos de los eventos globales que nadie más pedía: misión fallida y temporizador de misión (<see cref="ServicioMisiones"/>), instalar un
    /// implante con el Doc (<see cref="IImplantes"/>), el golpe al escudo del jugador (<see cref="IEscudo"/>) y activar o apagar el visor (<see cref="VisorJugador"/>).
    /// Los servicios pueden aparecer después (el registro se revisa cada segundo), así que se vuelve a enlazar solo. La crea <see cref="ArranqueSonidosT1535"/>.
    /// </summary>
    public class SonidosEventos : MonoBehaviour
    {
        private const float SegundosEnlace = 1f;

        private ServicioMisiones misiones;
        private IImplantes implantes;
        private IEscudo escudo;
        private float proximoEnlace;
        private float ultimoTic = float.NegativeInfinity;
        private bool visorPuesto;

        private void OnDisable() => Desenlazar();

        private void Update()
        {
            float ahora = Time.unscaledTime;
            if (ahora >= proximoEnlace)
            {
                proximoEnlace = ahora + SegundosEnlace;
                Enlazar();
            }
            if (misiones != null && misiones.Actual != null && ReglaSonidoEventos.TocaTemporizador(misiones.SegundosRestantes, ahora, ultimoTic))
            {
                ultimoTic = ahora;
                Sonidos.Reproducir(ReglaSonidoEventos.Temporizador);
            }
            bool visor = VisorJugador.ModoActivo;
            if (visor != visorPuesto)
            {
                visorPuesto = visor;
                SonarEnJugador(ReglaSonidoEventos.VisorActivar); // el mismo sonido al encender y al apagar (LEEME: bajito o en reversa)
            }
        }

        private void Enlazar()
        {
            ServicioMisiones m = Registro<IMisiones>.Actual as ServicioMisiones;
            if (m != misiones)
            {
                if (misiones != null) misiones.Terminada -= AlTerminarMision;
                misiones = m;
                if (misiones != null) misiones.Terminada += AlTerminarMision;
            }
            IImplantes i = Registro<IImplantes>.Actual;
            if (i != implantes)
            {
                if (implantes != null) implantes.Instalado -= AlInstalarImplante;
                implantes = i;
                if (implantes != null) implantes.Instalado += AlInstalarImplante;
            }
            IEscudo e = Registro<IEscudo>.Actual;
            if (e != escudo)
            {
                if (escudo != null) escudo.Absorbio -= AlAbsorberEscudo;
                escudo = e;
                if (escudo != null) escudo.Absorbio += AlAbsorberEscudo;
            }
        }

        private void Desenlazar()
        {
            if (misiones != null) misiones.Terminada -= AlTerminarMision;
            if (implantes != null) implantes.Instalado -= AlInstalarImplante;
            if (escudo != null) escudo.Absorbio -= AlAbsorberEscudo;
            misiones = null;
            implantes = null;
            escudo = null;
        }

        // La misión cumplida ya suena (HUD_LOGRO) cuando el objetivo se cumple; aquí solo la que falla o se abandona.
        private void AlTerminarMision(DatosMision datos, bool exito, ResumenMision resumen)
        {
            if (!exito) Sonidos.Reproducir(ReglaSonidoEventos.MisionFallida);
        }

        private void AlInstalarImplante(EspacioImplante espacio, int nivel) => StartCoroutine(Instalar());

        private IEnumerator Instalar()
        {
            float inicio = Time.unscaledTime;
            foreach (ReglaSonidoEventos.Paso paso in ReglaSonidoEventos.InstalarImplante)
            {
                while (Time.unscaledTime - inicio < paso.Segundos) yield return null;
                SonarEnJugador(paso.Id);
            }
        }

        private void AlAbsorberEscudo(float puntos) => SonarEnJugador(ReglaSonidoEventos.EscudoGolpe);

        private static void SonarEnJugador(string id)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null) Sonidos.ReproducirEn(id, jugador.Raiz.position);
            else Sonidos.Reproducir(id);
        }
    }
}
