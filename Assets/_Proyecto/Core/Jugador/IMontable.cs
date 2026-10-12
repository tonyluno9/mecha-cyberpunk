using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Algo a lo que el jugador se sube y de lo que se baja: el carro (Alan, Vehiculos/) o el mecha (Tony).
    /// Lo implementa un componente del propio carro o mecha; quien lo usa es el lado del jugador (el que implementa
    /// <see cref="IJugador"/>), que llama a <see cref="IntentarSubir"/> e <see cref="IntentarBajar"/>.
    ///
    /// Reglas de registro: se agrega a <c>Registro&lt;IMontable&gt;</c> en <c>OnEnable</c> y se quita en <c>OnDisable</c>.
    ///
    /// Quién hace qué:
    /// - La tecla Interactuar (E) es del jugador: el montable NO la lee para subir ni para bajar.
    /// - El jugador decide desde qué distancia a <see cref="PuntoSubida"/> se puede subir; no es parte de este contrato.
    /// - Al subir, el montable prende su <see cref="Camara"/> y su <c>AudioListener</c> y empieza a leer su propia entrada
    ///   (manejar, mover el mecha). El jugador apaga su propia cámara, su <c>AudioListener</c> y su
    ///   <c>CharacterController</c> mientras va dentro, cambia <see cref="IJugador.Modo"/> y avisa
    ///   <see cref="IJugador.ModoCambio"/>. Solo hay un <c>AudioListener</c> prendido a la vez.
    /// - Mientras el jugador va dentro, su raíz sigue al <see cref="Asiento"/> (posición y rotación) en <c>LateUpdate</c>;
    ///   el montable no hace nada con el transform del jugador.
    /// - Al bajar, el montable apaga su cámara, su <c>AudioListener</c> y su entrada, y entrega una pose libre en el piso;
    ///   el jugador la usa con <see cref="IJugador.Colocar"/> y vuelve a prender lo suyo.
    ///
    /// Aviso para el mundo: en modo Carro o Mecha el <c>CharacterController</c> del jugador está apagado y los triggers
    /// no lo ven. Para saber si el jugador en carro está dentro de una zona, usa
    /// <c>Registro&lt;IJugador&gt;.Actual.Raiz.position</c> (sigue al asiento) o los colliders del propio montable.
    /// </summary>
    public interface IMontable
    {
        /// <summary>Qué modo toma el jugador al subir: <see cref="ModoJugador.Carro"/> o <see cref="ModoJugador.Mecha"/>. Nunca APie.</summary>
        ModoJugador Modo { get; }

        /// <summary>Nombre para el HUD, por ejemplo "Carro viejo".</summary>
        string Nombre { get; }

        /// <summary>
        /// Punto en el piso (m, mundo) junto a la puerta del conductor, o junto a la escotilla, del lado de fuera.
        /// El jugador lo usa para saber qué tan cerca está.
        /// </summary>
        Vector3 PuntoSubida { get; }

        /// <summary>True mientras el jugador va dentro.</summary>
        bool Ocupado { get; }

        /// <summary>
        /// False si está ocupado, cerrado o todavía no disponible (por ejemplo, el mecha aún está llegando).
        /// Si es false, <see cref="IntentarSubir"/> devuelve false.
        /// </summary>
        bool PuedeSubir { get; }

        /// <summary>
        /// False si va demasiado rápido o está en pleno encendido. Si es false, <see cref="IntentarBajar"/> devuelve false.
        /// </summary>
        bool PuedeBajar { get; }

        /// <summary>
        /// Transform al que se pega la raíz del jugador mientras va dentro (el asiento del conductor o la silla de la cabina).
        /// Su posición y rotación definen dónde está el jugador.
        /// </summary>
        Transform Asiento { get; }

        /// <summary>
        /// La cámara del montable. Está apagada, con su <c>AudioListener</c> también apagado, mientras
        /// no esté <see cref="Ocupado"/>.
        /// </summary>
        Camera Camara { get; }

        /// <summary>
        /// Intenta que el jugador suba. Solo sube si <see cref="PuedeSubir"/>; si no, devuelve false y no cambia nada
        /// ni avisa. Al subir: prende su <see cref="Camara"/> y su <c>AudioListener</c>, empieza a leer su propia entrada,
        /// queda <see cref="Ocupado"/> y avisa <see cref="OcupadoCambio"/>(true). Devuelve si subió.
        /// </summary>
        bool IntentarSubir(IJugador jugador);

        /// <summary>
        /// Intenta que el jugador baje. Solo baja si <see cref="Ocupado"/> y <see cref="PuedeBajar"/>; si no, devuelve
        /// false y no cambia nada ni avisa (<paramref name="salida"/> queda en <c>Pose.identity</c> y no debe usarse).
        /// Al bajar: apaga su cámara, su <c>AudioListener</c> y su entrada, deja de estar ocupado, avisa
        /// <see cref="OcupadoCambio"/>(false) y devuelve en <paramref name="salida"/> una pose en el piso, libre de
        /// obstáculos, donde dejar al jugador. Devuelve si bajó.
        /// </summary>
        bool IntentarBajar(out Pose salida);

        /// <summary>Avisa al cambiar <see cref="Ocupado"/>: true al subir el jugador, false al bajar. Solo avisa cambios reales.</summary>
        event Action<bool> OcupadoCambio;
    }
}
