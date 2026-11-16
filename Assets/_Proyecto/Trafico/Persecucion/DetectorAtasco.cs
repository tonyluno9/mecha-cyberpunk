using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1006: lógica pura que detecta una patrulla atorada. Mide cuánto avanza en cada ventana de tiempo mientras pide avanzar; si casi no se mueve
    /// pide reversa. Si en el mismo lugar acumula varias reversas, o pasa demasiado tiempo, pide recolocarla. Se libera sola al alejarse del lugar.
    /// </summary>
    public sealed class DetectorAtasco
    {
        private readonly ParametrosAtasco p;
        private float segundosVentana;
        private Vector3 puntoVentana;
        private bool zonaAbierta;
        private Vector3 zona;
        private float segundosZona;
        private int eventosZona;
        private int reversasVistas;

        public DetectorAtasco(ParametrosAtasco parametros)
        {
            p = parametros;
        }

        /// <summary>True mientras hay un lugar donde se considera atorada.</summary>
        public bool Atorada => zonaAbierta;

        /// <summary>Reversas contadas en el lugar donde se atoró.</summary>
        public int EventosEnLaZona => eventosZona;

        public void Reiniciar()
        {
            segundosVentana = 0f;
            zonaAbierta = false;
            segundosZona = 0f;
            eventosZona = 0;
        }

        /// <summary>
        /// Un paso. <paramref name="quiereAvanzar"/> es true si pide velocidad; <paramref name="reversasConductor"/> el contador de reversas del conductor
        /// (cada reversa que sube cuenta como un intento en el lugar); <paramref name="enReversa"/> true mientras da reversa.
        /// </summary>
        public AccionAtasco Actualizar(Vector3 posicion, bool quiereAvanzar, int reversasConductor, bool enReversa, float dt)
        {
            posicion.y = 0f;
            int nuevas = reversasConductor - reversasVistas;
            reversasVistas = reversasConductor;
            if (nuevas > 0)
            {
                AbrirZona(posicion);
                eventosZona += nuevas;
            }
            if (zonaAbierta)
            {
                segundosZona += dt;
                if (!enReversa && Vector3.Distance(posicion, zona) > p.RadioLiberada)
                {
                    zonaAbierta = false;
                    segundosZona = 0f;
                    eventosZona = 0;
                }
                else if (eventosZona >= p.ReversasParaRecolocar || segundosZona >= p.SegundosParaRecolocar)
                {
                    // Tras recolocarla (o si no se pudo) hay que acumular de nuevo antes de pedirlo otra vez.
                    segundosZona = 0f;
                    eventosZona = 0;
                    segundosVentana = 0f;
                    puntoVentana = posicion;
                    return AccionAtasco.Recolocar;
                }
            }
            if (!quiereAvanzar || enReversa)
            {
                segundosVentana = 0f;
                puntoVentana = posicion;
                return AccionAtasco.Ninguna;
            }
            segundosVentana += dt;
            if (segundosVentana < p.VentanaSegundos)
            {
                return AccionAtasco.Ninguna;
            }
            float avance = Vector3.Distance(posicion, puntoVentana);
            segundosVentana = 0f;
            puntoVentana = posicion;
            if (avance >= p.DistanciaMinima)
            {
                return AccionAtasco.Ninguna;
            }
            AbrirZona(posicion);
            return AccionAtasco.Reversa;
        }

        private void AbrirZona(Vector3 posicion)
        {
            if (zonaAbierta)
            {
                return;
            }
            zonaAbierta = true;
            zona = posicion;
            segundosZona = 0f;
            eventosZona = 0;
        }
    }
}
