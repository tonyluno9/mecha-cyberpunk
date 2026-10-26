using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lógica pura de las dos reputaciones: recorte, niveles y avisos.</summary>
    public class ModeloReputacion
    {
        private readonly DatosReputacion datos;
        private readonly int[] valores = new int[2];

        public ModeloReputacion(DatosReputacion datos)
        {
            this.datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        public event Action Cambio;
        public event Action<TipoReputacion, int> NivelCambio;

        public int Minimo => datos.Minimo;
        public int Maximo => datos.Maximo;

        public int Valor(TipoReputacion tipo) => valores[(int)tipo];

        public int Nivel(TipoReputacion tipo) => NivelDe(Valor(tipo));

        public string NombreNivel(TipoReputacion tipo)
        {
            string[] nombres = tipo == TipoReputacion.Barrio ? datos.NombresBarrio : datos.NombresCorporaciones;
            int indice = Nivel(tipo) + 2;
            return nombres != null && indice >= 0 && indice < nombres.Length ? nombres[indice] ?? "" : "";
        }

        public int NivelDe(int valor)
        {
            int n1 = datos.Umbrales[0];
            int n2 = datos.Umbrales[1];
            if (valor >= n2) return 2;
            if (valor >= n1) return 1;
            if (valor <= -n2) return -2;
            if (valor <= -n1) return -1;
            return 0;
        }

        private int Recortar(int valor) => Math.Max(datos.Minimo, Math.Min(datos.Maximo, valor));

        /// <summary>Suma (o resta) a una reputación, recortada al rango. Avisa Cambio si el valor cambia y NivelCambio si cruza un umbral.</summary>
        public void Sumar(TipoReputacion tipo, int cantidad)
        {
            int indice = (int)tipo;
            int antes = valores[indice];
            int despues = Recortar(antes + cantidad);
            if (despues == antes) return;
            int nivelAntes = NivelDe(antes);
            valores[indice] = despues;
            Cambio?.Invoke();
            int nivelDespues = NivelDe(despues);
            if (nivelDespues != nivelAntes) NivelCambio?.Invoke(tipo, nivelDespues);
        }

        /// <summary>Fija los dos valores (cargar partida), recortados. Avisa Cambio, no NivelCambio.</summary>
        public void Restablecer(int barrio, int corporaciones)
        {
            valores[(int)TipoReputacion.Barrio] = Recortar(barrio);
            valores[(int)TipoReputacion.Corporaciones] = Recortar(corporaciones);
            Cambio?.Invoke();
        }
    }

    /// <summary>Efecto de la reputación en otras reglas.</summary>
    public static class ReglaReputacion
    {
        /// <summary>Multiplicador del calor sumado según el nivel de corporaciones (-2..2). Sin datos o nivel fuera de rango: 1.</summary>
        public static float MultiplicadorCalor(DatosReputacion datos, int nivelCorporaciones)
        {
            if (datos == null || datos.MultiplicadorCalor == null) return 1f;
            int indice = nivelCorporaciones + 2;
            if (indice < 0 || indice >= datos.MultiplicadorCalor.Length) return 1f;
            return datos.MultiplicadorCalor[indice];
        }
    }
}
