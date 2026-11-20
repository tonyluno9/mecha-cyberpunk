using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Reglas puras de los implantes (T-191, D-tony-166): qué se ofrece, cuánto cuesta y qué suma todo junto.</summary>
    public static class ReglaImplantes
    {
        /// <summary>Lo que cuesta instalar el implante teniendo ese nivel en su espacio: el precio menos el del nivel que ya tienes.</summary>
        public static int Costo(CatalogoImplantes catalogo, DatosImplante implante, int nivelInstalado)
        {
            int costo = implante.Precio;
            if (nivelInstalado > 0)
            {
                DatosImplante actual = catalogo.Buscar(implante.Espacio, nivelInstalado);
                if (actual != null) costo -= actual.Precio;
            }
            return costo < 0 ? 0 : costo;
        }

        /// <summary>La opción de un espacio: el siguiente nivel al instalado. La liga se revisa antes que el dinero; con dinero negativo no se compra (D-tony-85).</summary>
        public static OfertaImplante Oferta(CatalogoImplantes catalogo, EspacioImplante espacio, int nivelInstalado, int liga, int dinero)
        {
            DatosImplante siguiente = catalogo.Buscar(espacio, nivelInstalado + 1);
            if (siguiente == null) return new OfertaImplante(null, EstadoOfertaImplante.YaLoMejor, 0, 0);
            int costo = Costo(catalogo, siguiente, nivelInstalado);
            if (liga < siguiente.LigaMinima) return new OfertaImplante(siguiente, EstadoOfertaImplante.LigaBaja, costo, siguiente.LigaMinima);
            if (costo > 0 && dinero < costo) return new OfertaImplante(siguiente, EstadoOfertaImplante.SinDinero, costo, 0); // T-1005: lo gratis se compra aun con deuda
            return new OfertaImplante(siguiente, EstadoOfertaImplante.Instalar, costo, 0);
        }

        /// <summary>T-1529: vida máxima extra que da un implante instalado según su nivel (I: +10, II: +25). Es el valor que lleva cada DatosImplante.</summary>
        public static float VidaMaximaExtra(int nivel) => nivel >= 2 ? 25f : (nivel == 1 ? 10f : 0f);

        /// <summary>La combinación de los efectos de todo lo instalado. Sin nada: <see cref="EfectosImplantes.Ninguno"/>.</summary>
        public static EfectosImplantes Combinar(IEnumerable<DatosImplante> instalados)
        {
            EfectosImplantes total = EfectosImplantes.Ninguno;
            if (instalados == null) return total;
            foreach (DatosImplante d in instalados)
            {
                if (d != null) total = EfectosImplantes.Combinar(total, d.Efectos);
            }
            return total;
        }
    }
}
