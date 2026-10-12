#!/usr/bin/env bash
# Editor vivo de Unity controlado con la Unity CLI (unity command).
# Uso: bash herramientas/unity-vivo.sh {iniciar [escena]|play|detener-play|captura <archivo>|eval '<C#>'|comandos|detener}
cd "$(dirname "$0")/.." || exit 1
mkdir -p Logs
PID_ARCHIVO=Logs/unity-vivo.pid

unity_exe="${UNITY_EXE:-}"
if [ -z "$unity_exe" ] && [ -f .claude/maquina.local.md ]; then
  unity_exe=$(grep -m1 -E '^(- )?(unity_exe|Unity\.exe):' .claude/maquina.local.md | sed -E 's/^(- )?[^:]+:[[:space:]]*//' | tr -d '\r')
fi

uc() { unity command --project-path . "$@"; }

editor_corriendo() {
  powershell -NoProfile -Command 'if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited -and $_.Path -like "*\Editor\Unity.exe" }) { exit 0 } else { exit 1 }' >/dev/null 2>&1
}

iniciar() {
  if [ ! -f "$unity_exe" ]; then echo "ERROR: no encuentro Unity.exe: '$unity_exe'"; return 1; fi
  if editor_corriendo; then
    echo "UNITY ABIERTO: ya hay un editor corriendo. Ciérralo (o usa 'detener' si lo lanzó este script)."; return 3
  fi
  rm -f Logs/unity-vivo.log 2>/dev/null
  "$unity_exe" -batchmode -projectPath . -logFile Logs/unity-vivo.log >/dev/null 2>&1 &
  # PID de Windows (el $! de Git Bash es otro número y taskkill no lo reconoce)
  echo "$(cat /proc/$!/winpid 2>/dev/null || echo $!)" > "$PID_ARCHIVO"
  echo "Unity lanzado (pid $(cat "$PID_ARCHIVO")), esperando a la CLI (máx. 180 s)..."
  local i
  for i in $(seq 1 90); do
    if ! tasklist //FI "PID eq $(cat "$PID_ARCHIVO")" 2>/dev/null | grep -qi unity; then
      echo "FALLO: Unity terminó antes de responder. Revisa Logs/unity-vivo.log"
      grep -E 'error CS|another Unity instance' Logs/unity-vivo.log | sort -u | head -n 5
      rm -f "$PID_ARCHIVO"; return 1
    fi
    if uc editor_status --result-only 2>/dev/null | grep -q "ready"; then
      echo "Unity responde a la CLI."
      if [ -n "$1" ]; then
        uc open_scene --path "$1" 2>&1
      fi
      return 0
    fi
    sleep 2
  done
  echo "FALLO: la CLI no respondió en 180 s. Revisa Logs/unity-vivo.log"
  return 1
}

detener() {
  if [ -f "$PID_ARCHIVO" ]; then
    local pid; pid=$(cat "$PID_ARCHIVO")
    taskkill //PID "$pid" //T //F >/dev/null 2>&1 || kill "$pid" 2>/dev/null
    rm -f "$PID_ARCHIVO"
    echo "Unity detenido (pid $pid)."
  else
    echo "No hay Logs/unity-vivo.pid: este script no lanzó ningún editor."
  fi
}

case "$1" in
  iniciar) shift; iniciar "$@" ;;
  play) uc editor_play ;;
  detener-play) uc editor_stop ;;
  captura)
    # ruta absoluta: el editor resuelve las relativas desde su propia carpeta de trabajo
    destino="$2"; case "$destino" in /*|?:*) ;; *) destino="$PWD/$destino" ;; esac
    mkdir -p "$(dirname "$destino")"
    uc screenshot --output "$(cygpath -m "$destino")" --width 1280 --height 720 ;;
  eval) uc eval "$2" ;;
  consola) uc console ;;
  comandos) uc --format json ;;
  detener) detener ;;
  *) echo "Uso: bash herramientas/unity-vivo.sh {iniciar [escena]|play|detener-play|captura <archivo>|eval '<C#>'|comandos|detener}"; exit 2 ;;
esac
