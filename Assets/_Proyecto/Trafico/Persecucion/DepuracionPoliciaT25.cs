using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T25: métodos estáticos para probar a la policía en Play por eval (no se usan en el juego).</summary>
    public static class DepuracionPoliciaT25
    {
        /// <summary>Cuántas patrullas andan de ronda ahora (el tope es <c>ConfigPersecucion.PatrullasDeRonda</c>, 2).</summary>
        public static int ContarRondas()
        {
            int n = 0;
            foreach (PatrullaMundo p in PatrullaMundo.Activas)
            {
                if (p != null && p.EnRonda && !p.Robada && !p.Retirando)
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>Una línea por patrulla activa: nombre, si es ronda, y vida del DanioCarro (-1 si no tiene).</summary>
        public static string ListarPatrullas()
        {
            StringBuilder texto = new StringBuilder();
            foreach (PatrullaMundo p in PatrullaMundo.Activas)
            {
                if (p == null)
                {
                    continue;
                }
                DanioCarro d = p.GetComponent<DanioCarro>();
                texto.AppendLine(p.name + " ronda=" + p.EnRonda + " vida=" + (d != null ? d.Vida.ToString("F0") : "-1") + " robada=" + p.Robada);
            }
            return texto.ToString();
        }

        /// <summary>Le pega daño de bala a la patrulla activa más cercana al jugador y devuelve su vida antes y después (ver que baje y suban las estrellas).</summary>
        public static string DispararALaPatrullaMasCercana(float cantidad = 30f)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 origen = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : Vector3.zero;
            PatrullaMundo mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (PatrullaMundo p in PatrullaMundo.Activas)
            {
                if (p == null)
                {
                    continue;
                }
                float d = Vector3.Distance(origen, p.transform.position);
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = p;
                }
            }
            if (mejor == null)
            {
                return "no hay patrullas activas";
            }
            IRecibeDanio recibe = mejor.GetComponent<IRecibeDanio>();
            DanioCarro danio = mejor.GetComponent<DanioCarro>();
            float antes = danio != null ? danio.Vida : -1f;
            recibe?.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Disparo, mejor.transform.position + Vector3.up, Vector3.down, null));
            return mejor.name + " vida " + antes.ToString("F0") + " -> " + (danio != null ? danio.Vida.ToString("F0") : "-1");
        }

        /// <summary>true si el arresto está encendido (debe ser false: la policía ya no arresta).</summary>
        public static bool ArrestoEncendido() => GestorPersecucion.ArrestoActivo;
    }
}
