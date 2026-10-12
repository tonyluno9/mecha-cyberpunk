#!/usr/bin/env bash
# Verificación automática: compilar, editmode, playmode, todo.
# Uso: bash herramientas/verificar.sh {compilar|editmode|playmode|todo}
cd "$(dirname "$0")/.." || exit 1
mkdir -p Logs

unity_exe="${UNITY_EXE:-}"
if [ -z "$unity_exe" ] && [ -f .claude/maquina.local.md ]; then
  unity_exe=$(grep -m1 -E '^(- )?(unity_exe|Unity\.exe):' .claude/maquina.local.md | sed -E 's/^(- )?[^:]+:[[:space:]]*//' | tr -d '\r')
fi
if [ -z "$unity_exe" ] || [ ! -f "$unity_exe" ]; then
  echo "ERROR: no encuentro Unity.exe (UNITY_EXE o 'unity_exe:' en .claude/maquina.local.md): '$unity_exe'"
  exit 1
fi

abierto() {
  # Unity abierto: hay UnityLockfile y un Unity.exe de editor corriendo
  [ -f Temp/UnityLockfile ] || return 1
  # (el unity.exe del Hub/CLI no cuenta: solo el de ...\Editor\Unity.exe)
  powershell -NoProfile -Command 'if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited -and $_.Path -like "*\Editor\Unity.exe" }) { exit 0 } else { exit 1 }' >/dev/null 2>&1
}

if abierto; then
  echo "UNITY ABIERTO: hay un editor con este proyecto abierto (Temp/UnityLockfile). Ciérralo o usa herramientas/unity-vivo.sh detener."
  exit 3
fi

mostrar_errores() { grep -E 'error CS' "$1" | sort -u | head -n 8; }

compilar() {
  "$unity_exe" -batchmode -quit -projectPath . -logFile Logs/compilar.log
  local codigo=$?
  if [ $codigo -ne 0 ]; then
    echo "compilar FALLO (codigo $codigo)"; mostrar_errores Logs/compilar.log; return 1
  fi
  echo "compilar OK"
}

pruebas() {
  local modo="$1" minus; minus=$(echo "$modo" | tr 'A-Z' 'a-z')
  rm -f "Logs/resultados-$minus.xml"
  "$unity_exe" -batchmode -projectPath . -runTests -testPlatform "$modo" \
    -assemblyNames "MechaCyberpunk.Tests.$modo" -testResults "Logs/resultados-$minus.xml" -logFile "Logs/$minus.log"
  local codigo=$?
  local xml="Logs/resultados-$minus.xml"
  if [ ! -f "$xml" ]; then
    echo "$minus FALLO (codigo $codigo, sin resultados)"; mostrar_errores "Logs/$minus.log"; return 1
  fi
  local cabecera total pasaron falladas
  cabecera=$(grep -m1 '<test-run' "$xml")
  total=$(echo "$cabecera" | sed -n 's/.* total="\([0-9]*\)".*/\1/p')
  pasaron=$(echo "$cabecera" | sed -n 's/.* passed="\([0-9]*\)".*/\1/p')
  falladas=$(echo "$cabecera" | sed -n 's/.* failed="\([0-9]*\)".*/\1/p')
  if [ -z "$total" ] || [ "$total" = "0" ]; then
    echo "$minus FALLO: no corrio ninguna prueba (total=0)"; mostrar_errores "Logs/$minus.log"; return 1
  fi
  if [ "$codigo" -ne 0 ] || [ "$falladas" != "0" ]; then
    echo "$minus FALLO $pasaron/$total (fallaron $falladas)"
    grep -E '<test-case[^>]*result="Failed"' "$xml" | sed -n 's/.* fullname="\([^"]*\)".*/  fallo: \1/p' | head -n 8
    mostrar_errores "Logs/$minus.log"
    return 1
  fi
  echo "$minus $pasaron/$total"
}

case "$1" in
  compilar) compilar ;;
  editmode) pruebas EditMode ;;
  playmode) pruebas PlayMode ;;
  todo) compilar && pruebas EditMode && pruebas PlayMode ;;
  *) echo "Uso: bash herramientas/verificar.sh {compilar|editmode|playmode|todo}"; exit 2 ;;
esac
