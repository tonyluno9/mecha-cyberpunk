using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Las zonas con dueño del barrio (D-tony-217 a D-tony-219): quién manda en cada una y cuánto respeto tienes ahí.
    /// La implementa Progresion (<c>ServicioTerritorio</c>) y se agrega a <c>Registro&lt;ITerritorio&gt;</c> en <c>OnEnable</c>.
    /// IA avisa de cada pandillero vencido; UI y la historia leen. Los ids de zona son estables ("fosa", "tianguis", "neon").
    /// </summary>
    public interface ITerritorio
    {
        /// <summary>Los ids de todas las zonas.</summary>
        IReadOnlyList<string> Zonas { get; }

        /// <summary>El id de la zona que contiene la posición (solo X y Z), o "" si está fuera de todas.</summary>
        string ZonaEn(Vector3 posicion);

        /// <summary>Nombre para mostrar de la zona ("EL TIANGUIS"). "" si no existe.</summary>
        string Nombre(string zona);

        /// <summary>Quién manda hoy en la zona (<see cref="IdPandilla.Ninguna"/> si no existe).</summary>
        IdPandilla Dueno(string zona);

        /// <summary>Tu respeto en la zona, de 0 a 100.</summary>
        int Respeto(string zona);

        /// <summary>True si el jefe de la zona rival debe aparecer (respeto 70 o más y la zona no es tuya).</summary>
        bool JefeDisponible(string zona);

        /// <summary>Un pandillero cayó vencido en esa posición. <paramref name="jefe"/> true si era el jefe de la zona (la conquista).</summary>
        void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe);

        /// <summary>El jugador cayó en esa posición: baja el respeto de la zona donde cayó.</summary>
        void RegistrarCaida(Vector3 posicion);

        /// <summary>Cambió el respeto o el dueño de esa zona.</summary>
        event Action<string> Cambio;

        /// <summary>El jugador conquistó la zona (se avisa con el dueño anterior).</summary>
        event Action<string, IdPandilla> Conquistada;
    }
}
