# Regras de versionamento

Este projeto adota o modelo de `E:/Dev.Dietcode/MDs Regras/Versionamento.md`.

## Formato X.Y.Z

- `X`: versão principal do produto.
- `Y`: subversão.
- `Z`: build incremental da subversão atual.

Cada entrega com alteração incrementa `Z` em uma unidade. Quando `Y` aumenta,
`Z` volta a `0`. `X` muda somente em uma nova versão principal do produto.

Versão atual: **1.0.6**. A entrega da fase 5 incrementa Z de 5 para 6. A regra específica do Grupag Gateway
que mantém `X = 4` não se aplica a este produto.

## Projetos .NET

Manter estes valores alinhados em todos os seis `.csproj`, incluindo testes:

```xml
<Version>1.0.6</Version>
<AssemblyVersion>1.0.6</AssemblyVersion>
<FileVersion>1.0.6.0</FileVersion>
```

O quarto número de `FileVersion` fica reservado para revisão e permanece `0`,
salvo decisão explícita em contrário.

Antes de cada publish manual, incrementar `Z` em todos os projetos do produto.
O comando `dotnet publish` não incrementa a versão automaticamente, mesmo
quando reutiliza artefatos de `bin` e `obj`.

## Identificação de fases nos commits

A partir da fase 5, informar número, nome da fase e mudança realizada no commit,
mantendo o padrão validado pelo Husky. Exemplo:

    feat(fase-05): criacao de notas com validacao e publicacao sem sobrescrita

Preservar a identidade Git configurada pelo usuário. Push não é automático.