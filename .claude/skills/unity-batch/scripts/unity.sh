#!/usr/bin/env bash
# Roda o editor da Unity sem interface grafica: compilar, testar, buildar, executar metodo.
#
# Existe porque a linha de comando da Unity tem armadilhas que custam uma rodada de
# 5 minutos cada vez que se erra: -quit nao pode ser combinado com -runTests, o codigo
# de saida nem sempre reflete falha de compilacao, e o resultado util esta enterrado
# em um log de 20 mil linhas.
#
# Uso:
#   unity.sh compile              verifica se o projeto compila
#   unity.sh test [editmode|playmode|all]
#   unity.sh build                build de Windows x64
#   unity.sh method <Namespace.Classe.Metodo>
#   unity.sh doctor               diagnostico do ambiente
#
# Rode a partir de qualquer lugar do repositorio.

set -uo pipefail

# ------------------------------------------------------------------ localizacao

find_repo_root() {
    local dir="${BASH_SOURCE[0]}"
    dir="$(cd "$(dirname "$dir")" && pwd)"
    while [ "$dir" != "/" ]; do
        [ -d "$dir/unity/LoboBranco" ] && { echo "$dir"; return 0; }
        dir="$(dirname "$dir")"
    done
    # Fallback: procura a partir do diretorio atual
    dir="$(pwd)"
    while [ "$dir" != "/" ]; do
        [ -d "$dir/unity/LoboBranco" ] && { echo "$dir"; return 0; }
        dir="$(dirname "$dir")"
    done
    return 1
}

REPO="$(find_repo_root)" || { echo "ERRO: nao achei a raiz do repositorio (esperava unity/LoboBranco)."; exit 2; }
PROJECT="$REPO/unity/LoboBranco"
LOGDIR="$REPO/unity"

# A versao vem do proprio projeto. Fixar a versao no script e como o script apodrece.
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$PROJECT/ProjectSettings/ProjectVersion.txt" 2>/dev/null | tr -d '\r')"
[ -z "$VERSION" ] && { echo "ERRO: nao consegui ler a versao em ProjectVersion.txt."; exit 2; }

UNITY=""
for candidate in \
    "/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" \
    "/e/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" \
    "/d/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
do
    [ -f "$candidate" ] && { UNITY="$candidate"; break; }
done
[ -z "$UNITY" ] && { echo "ERRO: editor $VERSION nao encontrado. Instale pelo Unity Hub."; exit 2; }

# Caminho no formato que o Unity.exe espera (barras normais, letra de unidade).
win_path() { printf '%s' "$1" | sed 's|^/\([a-z]\)/|\U\1:/|'; }
PROJECT_WIN="$(win_path "$PROJECT")"

# --------------------------------------------------------------------- guardas

assert_editor_closed() {
    # A Unity trava o projeto. Uma instancia grafica aberta faz o batchmode falhar
    # com uma mensagem obscura sobre lockfile, entao vale avisar antes.
    if [ -f "$PROJECT/Temp/UnityLockfile" ] && tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi "Unity.exe"; then
        echo "ERRO: ha uma instancia da Unity com este projeto aberto. Feche o editor antes."
        exit 3
    fi
}

# ------------------------------------------------------------------- relatorio

report_compile_errors() {
    local log="$1"
    local errors
    errors="$(grep -h "error CS" "$log" 2>/dev/null | sed 's/^[[:space:]]*//' | sort -u)"

    if [ -n "$errors" ]; then
        echo
        echo "=== ERROS DE COMPILACAO ==="
        echo "$errors" | head -40
        local count
        count="$(echo "$errors" | wc -l)"
        [ "$count" -gt 40 ] && echo "... e mais $((count - 40))."
        return 1
    fi
    return 0
}

report_log_tail() {
    echo
    echo "=== FIM DO LOG ($1) ==="
    tail -n 15 "$1"
}

# Linhas que o nosso codigo imprime, para nao se perder no ruido do engine.
report_our_logs() {
    grep -h "\[Setup\]\|\[Sandbox\]\|\[Build\]" "$1" 2>/dev/null | tail -20
}

# --------------------------------------------------------------------- comandos

cmd_compile() {
    assert_editor_closed
    local log="$LOGDIR/batch-compile.log"
    echo "Compilando $VERSION..."
    "$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT_WIN" \
             -logFile "$(win_path "$log")" >/dev/null 2>&1
    local code=$?

    if ! report_compile_errors "$log"; then
        echo
        echo "RESULTADO: falhou. Log completo em $log"
        return 1
    fi

    if [ $code -ne 0 ]; then
        report_log_tail "$log"
        echo "RESULTADO: a Unity saiu com codigo $code sem erro de compilacao. Veja o log."
        return 1
    fi

    echo "RESULTADO: compila limpo."
    return 0
}

cmd_test() {
    assert_editor_closed
    local platform="${1:-all}"
    local overall=0

    run_one_platform() {
        local plat="$1"
        local log="$LOGDIR/test-$plat.log"
        local xml="$LOGDIR/test-results-$plat.xml"

        echo "Rodando testes de $plat..."
        # -runTests nao aceita -quit: a Unity precisa continuar viva ate o runner terminar.
        "$UNITY" -batchmode -nographics -projectPath "$PROJECT_WIN" \
                 -runTests -testPlatform "$plat" \
                 -testResults "$(win_path "$xml")" \
                 -logFile "$(win_path "$log")" >/dev/null 2>&1

        if ! report_compile_errors "$log"; then
            echo "  $plat: nao rodou, o projeto nao compila."
            return 1
        fi

        if [ ! -f "$xml" ]; then
            report_log_tail "$log"
            echo "  $plat: sem arquivo de resultados."
            return 1
        fi

        local summary
        summary="$(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$xml" | head -1)"
        echo "  $plat: $summary"

        local failed
        failed="$(echo "$summary" | sed -n 's/.*failed="\([0-9]*\)".*/\1/p')"

        if [ "${failed:-0}" != "0" ]; then
            echo
            echo "  --- falhas em $plat ---"
            # O nome do metodo e a mensagem estao em elementos diferentes; casar os dois
            # de forma robusta em shell nao vale a pena, entao imprime os dois blocos.
            grep -o 'methodname="[^"]*"[^>]*result="Failed"' "$xml" | sed 's/^/  /' | head -10
            perl -0ne 'while(/<message><!\[CDATA\[(.*?)\]\]><\/message>/gs){ $_=$1; next if /child tests had errors/; s/^\s+//mg; print "  $_\n---\n" }' "$xml" 2>/dev/null | head -40
            return 1
        fi
        return 0
    }

    case "$platform" in
        editmode|playmode) run_one_platform "$platform" || overall=1 ;;
        all)
            run_one_platform "editmode" || overall=1
            run_one_platform "playmode" || overall=1
            ;;
        *) echo "ERRO: plataforma '$platform'. Use editmode, playmode ou all."; return 2 ;;
    esac

    [ $overall -eq 0 ] && echo "RESULTADO: tudo passou." || echo "RESULTADO: ha testes falhando."
    return $overall
}

cmd_build() {
    assert_editor_closed
    local log="$LOGDIR/batch-build.log"
    echo "Buildando Windows x64... (o primeiro build leva varios minutos)"
    "$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT_WIN" \
             -buildTarget Win64 \
             -executeMethod LoboBranco.EditorTools.ProjectSetup.BuildWindows \
             -logFile "$(win_path "$log")" >/dev/null 2>&1
    local code=$?

    report_compile_errors "$log" || { echo "RESULTADO: falhou na compilacao."; return 1; }

    local result
    result="$(grep -h "\[Build\]" "$log" | tail -1)"

    if [ -n "$result" ]; then
        echo "$result"
    else
        report_log_tail "$log"
    fi

    if [ $code -ne 0 ]; then
        echo "RESULTADO: build falhou (codigo $code)."
        return 1
    fi

    echo "RESULTADO: build ok em $REPO/unity/Builds/Windows/"
    return 0
}

cmd_method() {
    assert_editor_closed
    local method="${1:-}"
    [ -z "$method" ] && { echo "ERRO: informe o metodo, ex: LoboBranco.EditorTools.SandboxSetup.BuildSandbox"; return 2; }

    local log="$LOGDIR/batch-method.log"
    echo "Executando $method..."
    "$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT_WIN" \
             -executeMethod "$method" \
             -logFile "$(win_path "$log")" >/dev/null 2>&1
    local code=$?

    report_compile_errors "$log" || { echo "RESULTADO: falhou na compilacao."; return 1; }
    report_our_logs "$log"

    if [ $code -ne 0 ]; then
        report_log_tail "$log"
        echo "RESULTADO: saiu com codigo $code."
        return 1
    fi

    echo "RESULTADO: ok."
    return 0
}

cmd_doctor() {
    echo "Repositorio     $REPO"
    echo "Projeto         $PROJECT"
    echo "Versao          $VERSION"
    echo "Editor          $UNITY"
    echo "Serializacao    $(sed -n 's/^  m_SerializationMode: //p' "$PROJECT/ProjectSettings/EditorSettings.asset" | tr -d '\r') (2 = Force Text)"
    echo "Fixed Timestep  $(sed -n 's/^  Fixed Timestep: //p' "$PROJECT/ProjectSettings/TimeManager.asset" | tr -d '\r')"
    echo "asmdefs         $(find "$PROJECT/Assets/_Project/Code" -name '*.asmdef' 2>/dev/null | wc -l)"
    echo "Cenas           $(ls "$PROJECT/Assets/_Project/Scenes/"*.unity 2>/dev/null | wc -l)"
    echo "IL2CPP Win64    $([ -d "$(dirname "$UNITY")/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_player_nondevelopment_il2cpp" ] && echo presente || echo AUSENTE)"

    if [ -f "$PROJECT/Temp/UnityLockfile" ] && tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi "Unity.exe"; then
        echo "Editor aberto   SIM — feche antes de rodar batchmode"
    else
        echo "Editor aberto   nao"
    fi
}

# ----------------------------------------------------------------------- main

case "${1:-}" in
    compile) shift; cmd_compile "$@" ;;
    test)    shift; cmd_test "$@" ;;
    build)   shift; cmd_build "$@" ;;
    method)  shift; cmd_method "$@" ;;
    doctor)  shift; cmd_doctor "$@" ;;
    *)
        sed -n '2,17p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
        exit 2
        ;;
esac
