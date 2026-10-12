using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Un lugar real de la ciudad. Lo implementa Alan (Mundo/), uno por lugar, y lo agrega a
    /// <c>Registro&lt;ILugar&gt;</c>. Lo consultan Tony (objetivos, renta, misiones) y el HUD.
    /// </summary>
    public interface ILugar
    {
        /// <summary>Qué lugar es. Único entre los ILugar registrados y nunca <c>Ninguno</c>.</summary>
        IdLugar Id { get; }

        /// <summary>Nombre del letrero, tal como se muestra al jugador.</summary>
        string Nombre { get; }

        /// <summary>Horario de apertura. Es dato; no incluye cierres especiales.</summary>
        HorarioLugar Horario { get; }

        /// <summary>
        /// ¿Está abierto ahora? Combina el horario con el reloj del mundo y con cualquier cierre especial
        /// (por ejemplo, "cerrado en la demo"). Es lo que manda: si es false, su puerta no abre.
        /// </summary>
        bool EstaAbierto { get; }

        /// <summary>True mientras el jugador está dentro del lugar.</summary>
        bool JugadorDentro { get; }

        /// <summary>Punto frente a la puerta, del lado de la calle. El objetivo de Tony apunta aquí.</summary>
        Vector3 PosicionEntrada { get; }

        /// <summary>Avisa al cambiar <c>EstaAbierto</c>: true al abrir, false al cerrar. Solo avisa cambios reales.</summary>
        event Action<bool> EstadoCambio;
    }
}
