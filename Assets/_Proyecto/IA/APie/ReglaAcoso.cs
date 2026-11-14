using System;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Lógica pura del acoso (S2, 14-nov). Fases: Acosando (molestan al civil), Sorpresa (les pegaron sin ver quién: buscan un rato),
    /// Alerta (te vieron: pelean con la IA normal) y Vencidos (cayeron todos). El tiempo y el azar entran por parámetro.
    /// </summary>
    public sealed class ReglaAcoso
    {
        public enum Fase
        {
            Acosando,
            Sorpresa,
            Alerta,
            Vencidos
        }

        public enum Gesto
        {
            Ninguno,
            Burla,
            Empujon
        }

        public enum Vista
        {
            No,
            Oido,
            Ojos
        }

        public struct Entrada
        {
            public bool JugadorDetectado;
            public bool DanioRecibido;
            public bool JugadorDesdeArriba;
            public bool TodosVencidos;
        }

        public struct Salida
        {
            public Gesto Gesto;
            public bool EmpujonTira;
            public bool EntroSorpresa;
            public bool EntroAlerta;
            public bool EntroVencidos;
        }

        private readonly float gestoMinimo;
        private readonly float gestoMaximo;
        private readonly float probabilidadEmpujon;
        private readonly float probabilidadCaida;
        private readonly float retraso;
        private readonly float retrasoDesdeArriba;
        private readonly Func<float> azar;
        private float proximoGesto;
        private float restanteSorpresa;

        public Fase Actual { get; private set; } = Fase.Acosando;
        public float SorpresaRestante => restanteSorpresa;

        public ReglaAcoso(float gestoMinimo, float gestoMaximo, float probabilidadEmpujon, float probabilidadCaida, float retraso, float retrasoDesdeArriba, Func<float> azar)
        {
            this.gestoMinimo = gestoMinimo;
            this.gestoMaximo = Mathf.Max(gestoMinimo, gestoMaximo);
            this.probabilidadEmpujon = probabilidadEmpujon;
            this.probabilidadCaida = probabilidadCaida;
            this.retraso = retraso;
            this.retrasoDesdeArriba = retrasoDesdeArriba;
            this.azar = azar ?? (() => UnityEngine.Random.value);
            proximoGesto = SiguientePeriodo();
        }

        private float SiguientePeriodo() => Mathf.Lerp(gestoMinimo, gestoMaximo, azar());

        public Salida Avanzar(float dt, Entrada entrada)
        {
            Salida salida = new Salida();
            if (Actual == Fase.Vencidos)
            {
                return salida;
            }
            if (entrada.TodosVencidos)
            {
                Actual = Fase.Vencidos;
                salida.EntroVencidos = true;
                return salida;
            }
            switch (Actual)
            {
                case Fase.Acosando:
                    if (entrada.JugadorDetectado)
                    {
                        Actual = Fase.Alerta;
                        salida.EntroAlerta = true;
                    }
                    else if (entrada.DanioRecibido)
                    {
                        Actual = Fase.Sorpresa;
                        restanteSorpresa = entrada.JugadorDesdeArriba ? retrasoDesdeArriba : retraso;
                        salida.EntroSorpresa = true;
                    }
                    else
                    {
                        proximoGesto -= dt;
                        if (proximoGesto <= 0f)
                        {
                            proximoGesto = SiguientePeriodo();
                            bool empujon = azar() < probabilidadEmpujon;
                            salida.Gesto = empujon ? Gesto.Empujon : Gesto.Burla;
                            salida.EmpujonTira = empujon && azar() < probabilidadCaida;
                        }
                    }
                    break;
                case Fase.Sorpresa:
                    restanteSorpresa -= dt;
                    if (entrada.JugadorDetectado || restanteSorpresa <= 0f)
                    {
                        restanteSorpresa = 0f;
                        Actual = Fase.Alerta;
                        salida.EntroAlerta = true;
                    }
                    break;
            }
            return salida;
        }

        /// <summary>
        /// Si un pandillero en <paramref name="origen"/> que mira hacia <paramref name="adelante"/> percibe a <paramref name="objetivo"/>:
        /// Oido a menos de <paramref name="distanciaOido"/> en cualquier dirección; Ojos dentro de la distancia, del cono horizontal y por debajo
        /// de la inclinación máxima sobre su horizonte. La línea de vista libre la comprueba quien llama.
        /// </summary>
        public static Vista Evaluar(Vector3 origen, Vector3 adelante, Vector3 objetivo, float distanciaVista, float anguloVista, float inclinacionMaxima, float distanciaOido)
        {
            Vector3 delta = objetivo - origen;
            float distancia = delta.magnitude;
            if (distancia <= distanciaOido)
            {
                return Vista.Oido;
            }
            if (distancia > distanciaVista)
            {
                return Vista.No;
            }
            Vector3 plano = new Vector3(delta.x, 0f, delta.z);
            Vector3 frente = new Vector3(adelante.x, 0f, adelante.z);
            if (plano.sqrMagnitude < 1e-6f || frente.sqrMagnitude < 1e-6f)
            {
                return Vista.No;
            }
            if (Vector3.Angle(frente, plano) > anguloVista * 0.5f)
            {
                return Vista.No;
            }
            float inclinacion = Mathf.Atan2(delta.y, plano.magnitude) * Mathf.Rad2Deg;
            return inclinacion <= inclinacionMaxima ? Vista.Ojos : Vista.No;
        }

        /// <summary>True si el jugador está al menos <paramref name="alturaMinima"/> más alto que los pandilleros.</summary>
        public static bool EstaDesdeArriba(float alturaJugador, float alturaPandilleros, float alturaMinima) => alturaJugador - alturaPandilleros >= alturaMinima;
    }
}
