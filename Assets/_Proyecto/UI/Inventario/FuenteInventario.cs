using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;

namespace MechaCyberpunk.UI.Inventario
{
    /// <summary>
    /// Arma la lista de casillas del inventario (T-904) con lo que el jugador trae de verdad: las armas del <see cref="ArsenalJugador"/>, las balas de
    /// <see cref="ReservaBalas"/>, los objetos del <see cref="ServicioInventario"/> y los implantes de <see cref="IImplantes"/>. Solo consulta.
    /// </summary>
    public static class FuenteInventario
    {
        private const string IconoPunos = "Cat_Cuchillo";
        private static readonly string[] NivelesRomanos = { "BASE", "I", "II", "III" };

        /// <summary>Las casillas de hoy, en el orden armas, munición, consumibles, implantes y otros.</summary>
        public static List<EntradaInventario> Leer()
        {
            List<EntradaInventario> lista = new List<EntradaInventario>();
            AgregarArmas(lista);
            AgregarMunicion(lista);
            AgregarObjetos(lista);
            AgregarImplantes(lista);
            // OrderBy es estable: dentro de una categoría queda el orden en que se agregó.
            return lista.OrderBy(e => e.Categoria).ToList();
        }

        private static void AgregarArmas(List<EntradaInventario> lista)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                int nivel = arsenal.Nivel(arma);
                if (nivel < 0)
                {
                    continue;
                }
                DatosArma datos = arsenal.Datos(arma);
                string nombre = datos != null ? datos.Nombre.ToUpperInvariant() : NombreDeArma(arma);
                string icono = arma == ArmaAPie.Punos ? IconoPunos : "Cat_" + arma;
                EntradaInventario entrada = new EntradaInventario("arma_" + arma, nombre, CategoriaInventario.Armas, 1, 0, nivel, icono, string.Empty);
                entrada.ConDato("NIVEL", NivelesRomanos[Math.Min(Math.Max(nivel, 0), NivelesRomanos.Length - 1)]);
                if (datos != null)
                {
                    entrada.ConDato("DAÑO", datos.Danio.ToString("0.#"));
                    if (datos.Cargador > 0)
                    {
                        entrada.ConDato("CARGADOR", datos.Cargador.ToString());
                    }
                }
                ReservaBalas reserva = Registro<ReservaBalas>.Actual;
                if (reserva != null && ModeloReservaBalas.EsLimitada(arma))
                {
                    entrada.ConDato("RESERVA", reserva.Cantidad(arma) + " / " + ReservaBalas.Maximo(arma));
                }
                lista.Add(entrada);
            }
        }

        private static void AgregarMunicion(List<EntradaInventario> lista)
        {
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            if (reserva == null)
            {
                return;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (!ModeloReservaBalas.EsLimitada(arma))
                {
                    continue;
                }
                int balas = reserva.Cantidad(arma);
                int maximo = ReservaBalas.Maximo(arma);
                DatosMunicionArma datos = ReservaBalas.Datos(arma);
                string nombre = datos != null ? datos.Nombre.ToUpperInvariant() : "BALAS DE " + NombreDeArma(arma);
                string descripcion = datos != null && datos.Oferta != null ? datos.Oferta.Descripcion : string.Empty;
                EntradaInventario entrada = new EntradaInventario("balas_" + arma, nombre, CategoriaInventario.Municion, balas, maximo, 0, "Cat_Municion", descripcion);
                entrada.ConDato("ARMA", NombreDeArma(arma));
                entrada.ConDato("EN RESERVA", balas + " / " + maximo);
                lista.Add(entrada);
            }
        }

        private static void AgregarObjetos(List<EntradaInventario> lista)
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario == null)
            {
                return;
            }
            foreach (DatosObjeto objeto in inventario.Todos)
            {
                // Las armas y las balas ya salen de su propia fuente.
                if (objeto == null || objeto is DatosArma || objeto is DatosMunicionArma)
                {
                    continue;
                }
                int cantidad = inventario.Cantidad(objeto);
                if (cantidad <= 0)
                {
                    continue;
                }
                bool consumible = objeto.Tipo == TipoObjeto.Consumible || objeto.Tipo == TipoObjeto.KitReparacion;
                CategoriaInventario categoria = consumible ? CategoriaInventario.Consumibles : CategoriaInventario.Otros;
                string descripcion = objeto is DatosConsumible c ? c.Descripcion : objeto.Oferta != null ? objeto.Oferta.Descripcion : string.Empty;
                EntradaInventario entrada = new EntradaInventario(objeto.Id, objeto.Nombre.ToUpperInvariant(), categoria, cantidad, objeto.Maximo, 0,
                    consumible ? "Cat_Consumibles" : null, descripcion);
                entrada.ConDato("TIPO", Espaciar(objeto.Tipo.ToString()));
                entrada.ConDato("LLEVAS", cantidad + " / " + objeto.Maximo);
                if (objeto is DatosConsumible datos)
                {
                    if (datos.Curacion > 0f)
                    {
                        entrada.ConDato("CURACIÓN", datos.Curacion.ToString("0.#"));
                    }
                    if (datos.Escudo > 0f)
                    {
                        entrada.ConDato("ESCUDO", datos.Escudo.ToString("0.#"));
                    }
                }
                if (objeto.ValorVenta > 0)
                {
                    entrada.ConDato("VALE", Moneda.Texto(objeto.ValorVenta));
                }
                lista.Add(entrada);
            }
        }

        private static void AgregarImplantes(List<EntradaInventario> lista)
        {
            IImplantes implantes = Registro<IImplantes>.Actual;
            if (implantes == null)
            {
                return;
            }
            foreach (EspacioImplante espacio in Enum.GetValues(typeof(EspacioImplante)))
            {
                int nivel = implantes.Nivel(espacio);
                if (nivel <= 0)
                {
                    continue;
                }
                string nombre = implantes.Nombre(espacio);
                string titulo = string.IsNullOrEmpty(nombre) ? Espaciar(espacio.ToString()) : nombre.ToUpperInvariant();
                EntradaInventario entrada = new EntradaInventario("implante_" + espacio, titulo, CategoriaInventario.Implantes, 1, 0, nivel, "Cat_Implantes", string.Empty);
                entrada.ConDato("ESPACIO", Espaciar(espacio.ToString()));
                entrada.ConDato("NIVEL", NivelesRomanos[Math.Min(nivel, NivelesRomanos.Length - 1)]);
                lista.Add(entrada);
            }
        }

        private static string NombreDeArma(ArmaAPie arma) => arma == ArmaAPie.Punos ? "PUÑOS" : arma.ToString().ToUpperInvariant();

        // "KitReparacion" pasa a "KIT REPARACION".
        private static string Espaciar(string nombre) => Regex.Replace(nombre, "(?<=[a-z])(?=[A-Z])", " ").ToUpperInvariant();
    }
}
