# Regras de versionamento

Este projeto adota o modelo de `E:/Dev.Dietcode/MDs Regras/Versionamento.md`.

## Formato X.Y.Z

- `X`: versão principal do produto.
- `Y`: subversão.
- `Z`: build incremental da subversão atual.

Cada entrega com alteração incrementa `Z` em uma unidade. Quando `Y` aumenta,
`Z` volta a `0`. `X` muda somente em uma nova versão principal do produto.

Versão atual: **1.0.2**. A entrega das fases 0 a 2 incrementa Z de 1 para 2. A regra específica do Grupag Gateway
que mantém `X = 4` não se aplica a este produto.

## Projetos .NET

Manter estes valores alinhados em todos os seis `.csproj`, incluindo testes:

```xml
<Version>1.0.2</Version>
<AssemblyVersion>1.0.2</AssemblyVersion>
<FileVersion>1.0.2.0</FileVersion>
```

O quarto número de `FileVersion` fica reservado para revisão e permanece `0`,
salvo decisão explícita em contrário.

Antes de cada publish manual, incrementar `Z` em todos os projetos do produto.
O comando `dotnet publish` não incrementa a versão automaticamente, mesmo
quando reutiliza artefatos de `bin` e `obj`.
