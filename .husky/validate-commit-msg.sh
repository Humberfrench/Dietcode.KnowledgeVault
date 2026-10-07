#!/bin/sh

commit_regex='^(feat|fix|chore|docs|refactor|test|style|perf|ci|build)(\([a-zA-Z0-9._/-]+\))?: [^ ].{2,99}$'

if ! grep -qE "$commit_regex" "$1"; then
    echo ""
    echo "❌ Commit rejeitado."
    echo ""
    echo "Use o padrão:"
    echo "  <tipo>(escopo opcional): mensagem"
    echo ""
    echo "Tipos válidos:"
    echo "  feat | fix | chore | docs | refactor | test | style | perf | ci | build"
    echo ""
    echo "Exemplos:"
    echo "  feat: adiciona consulta de tarifas"
    echo "  fix(gateway): corrige processamento do pagamento"
    echo "  refactor(api): reorganiza serviço"
    echo ""
    exit 1
fi

exit 0
