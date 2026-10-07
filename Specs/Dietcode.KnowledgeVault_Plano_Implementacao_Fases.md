# Dietcode.KnowledgeVault --- Plano de Implementação Fase a Fase

## 1. Objetivo deste documento

Este documento transforma a arquitetura do **Dietcode.KnowledgeVault**
em entregas pequenas, independentes e verificáveis para implementação
progressiva.

A regra principal é:

> **James deve implementar uma fase por vez. A próxima fase só começa
> depois que a anterior estiver funcional, testada e aceita.**

O projeto continuará seguindo as decisões já definidas:

-   C# / .NET 10;
-   ASP.NET Core;
-   IIS para hospedagem;
-   arquivos Markdown como fonte de verdade;
-   Obsidian como interface humana;
-   YAML Frontmatter para metadados;
-   MCP como integração planejada com IAs;
-   sem banco de dados na V1;
-   sem Redis;
-   sem mensageria;
-   sem Docker na V1;
-   Git fortemente recomendado;
-   segurança de acesso ao filesystem desde o Core.

------------------------------------------------------------------------

## Adequação aprovada — fases 0 a 2

Na versão 1.0.2, as fases 0, 1 e 2 estão implementadas e verificadas conforme
[registro de aceite](../docs/Fases-00-02.md). A estrutura usa seis projetos:
Domain e Application separam o antigo Core, conforme decisão do projeto.

Application referencia Domain; Infrastructure referencia Application; Server
referencia Application e Infrastructure. UnitTests referencia Domain e Application;
IntegrationTests referencia Infrastructure e Server. A solution usa o formato .slnx.

A raiz do Vault deve ser configurada explicitamente e existir; não é criada no
startup. A V1 admite apenas .md após normalização. A segurança rejeita reparse
points, incluindo junctions, e caminhos percent-encoded; veja os limites de I/O
e as evidências no registro de aceite. A fase 3 foi implementada na versão 1.0.4, conforme docs/Fase-03.md. A fase 4 foi implementada na versão 1.0.5, conforme docs/Fase-04.md. As fases 5 a 20 continuam pendentes.

# 2. Nome e Solution

Nome do produto:

``` text
Dietcode.KnowledgeVault
```

Solution:

``` text
Dietcode.KnowledgeVault.slnx
```

Estrutura alvo:

``` text
Dietcode.KnowledgeVault.slnx
│
├── src/
│   ├── Dietcode.KnowledgeVault.Server/
│   ├── Dietcode.KnowledgeVault.Domain/
│   ├── Dietcode.KnowledgeVault.Application/
│   └── Dietcode.KnowledgeVault.Infrastructure/
│
└── tests/
    ├── Dietcode.KnowledgeVault.UnitTests/
    └── Dietcode.KnowledgeVault.IntegrationTests/
```

Não é obrigatório criar toda a complexidade no primeiro commit. A
estrutura pode ser criada desde o início, mas cada projeto deve receber
código apenas quando sua responsabilidade entrar na fase atual.

------------------------------------------------------------------------

# 3. Princípio de desenvolvimento

Cada fase deve terminar com:

``` text
IMPLEMENTAR
    ↓
COMPILAR
    ↓
TESTAR
    ↓
VALIDAR MANUALMENTE
    ↓
DOCUMENTAR
    ↓
COMMIT
```

Não antecipar funcionalidades de fases futuras.

Evitar:

``` text
"já que estou aqui vou implementar também..."
```

O objetivo do faseamento é permitir identificar exatamente onde um
problema foi introduzido.

------------------------------------------------------------------------

# 4. Definição de pronto de cada fase

Uma fase somente será considerada concluída quando:

-   a Solution compilar sem erros;
-   os testes daquela fase passarem;
-   não houver warnings relevantes ignorados;
-   existir validação manual quando aplicável;
-   não houver segredos no código;
-   as responsabilidades estiverem separadas;
-   o README ou documentação técnica estiver atualizado;
-   houver um commit identificável da fase.

Formato sugerido:

``` text
feat: implement knowledge vault phase 01
feat: implement knowledge vault phase 02
...
```

------------------------------------------------------------------------

# 5. FASE 0 --- Preparação do repositório

## Objetivo

Criar apenas a fundação do projeto.

## Implementar

Criar:

``` text
Dietcode.KnowledgeVault/
│
├── src/
├── tests/
├── docs/
├── README.md
├── .gitignore
└── Dietcode.KnowledgeVault.slnx
```

Projetos:

``` text
Dietcode.KnowledgeVault.Domain
Dietcode.KnowledgeVault.Application
Dietcode.KnowledgeVault.Infrastructure
Dietcode.KnowledgeVault.Server
Dietcode.KnowledgeVault.UnitTests
Dietcode.KnowledgeVault.IntegrationTests
```

Target inicial:

``` text
.NET 10
```

## Referências

``` text
Server
 ├── Application
 └── Infrastructure

Application
 └── Domain

Infrastructure
 └── Application

UnitTests
 ├── Domain
 └── Application

IntegrationTests
 ├── Infrastructure
 └── Server
```

Evitar dependência:

``` text
Application -> Infrastructure
Application -> Server
Infrastructure -> Server
```

## Não implementar ainda

-   filesystem;
-   MCP;
-   IIS;
-   autenticação;
-   busca;
-   API;
-   Obsidian;
-   banco.

## Critério de aceite

``` text
dotnet build
```

deve concluir com sucesso.

------------------------------------------------------------------------

# 6. FASE 1 --- Definições do Vault e configuração

## Objetivo

Definir onde o Vault existe e quais regras básicas ele possui.

## Criar

``` text
VaultOptions
```

Exemplo:

``` csharp
public sealed class VaultOptions
{
    public string RootPath { get; set; } = string.Empty;
    public string[] AllowedExtensions { get; set; } = [".md"];
    public long MaxFileSizeBytes { get; set; }
}
```

Configuração:

``` json
{
  "Vault": {
    "RootPath": "D:\\Knowledge\\Vault",
    "AllowedExtensions": [".md"],
    "MaxFileSizeBytes": 2097152
  }
}
```

## Validar no startup

-   `RootPath` obrigatório;
-   caminho deve ser absoluto;
-   diretório deve existir ou a política de criação deve ser explícita;
-   `MaxFileSizeBytes > 0`;
-   extensões devem ser normalizadas.

## Importante

O caminho:

``` text
D:\Knowledge\Vault
```

é configuração, não deve ficar hardcoded nos serviços.

## Testes

-   configuração válida;
-   RootPath vazio;
-   caminho relativo;
-   limite inválido;
-   extensão permitida.

## Critério de aceite

A aplicação deve iniciar com configuração válida e falhar claramente com
configuração inválida.

------------------------------------------------------------------------

# 7. FASE 2 --- Segurança de caminhos

## Objetivo

Criar a camada mais importante antes de qualquer leitura/escrita:
impedir acesso fora do Vault.

## Criar

``` text
IVaultPathResolver
VaultPathResolver
```

Responsabilidade:

``` text
caminho relativo solicitado
          ↓
normalização
          ↓
caminho absoluto
          ↓
validação contra VaultRoot
```

## Deve aceitar

``` text
02 - Trabalho/TAG/URs Vencidas.md
01 - Diario/2026/2026-10-07.md
```

## Deve rejeitar

``` text
../arquivo.md
../../windows/system32
C:\Windows\win.ini
\\servidor\share
/etc/passwd
```

Também testar variações de separadores e caminhos
codificados/normalizados quando aplicável.

## Regra

Depois de resolver:

``` text
FullPath deve permanecer dentro de VaultRoot
```

Não utilizar apenas:

``` csharp
path.Contains("..")
```

como proteção.

## Testes obrigatórios

Esta fase deve possuir cobertura forte.

## Critério de aceite

Nenhum caminho fornecido pelo consumidor consegue resolver para fora do
diretório configurado.

------------------------------------------------------------------------

# 8. FASE 3 --- Leitura e metadados

Implementada na versão 1.0.4. Evidências, contratos e limites: [Fase 3](../docs/Fase-03.md).

## Objetivo

Primeira operação real sobre o Vault.

## Criar

``` csharp
public interface IVaultService
{
    Task<NoteContent> ReadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<NoteInfo> GetInfoAsync(
        string path,
        CancellationToken cancellationToken = default);
}
```

Modelos:

``` text
NoteContent
NoteInfo
```

`NoteInfo` deve possuir inicialmente:

``` text
Name
RelativePath
Extension
Size
CreatedAt
UpdatedAt
```

## Regras

-   somente extensões permitidas;
-   respeitar tamanho máximo;
-   arquivo inexistente deve retornar erro conhecido;
-   encoding padrão UTF-8;
-   não permitir acesso fora do Vault.

## Testes

-   ler MD;
-   Unicode;
-   acentos;
-   arquivo inexistente;
-   extensão proibida;
-   arquivo acima do limite;
-   path traversal.

## Teste manual

Criar:

``` text
D:\Knowledge\Vault\Teste.md
```

e confirmar leitura correta.

------------------------------------------------------------------------

# 9. FASE 4 --- Listagem

Implementada na versão 1.0.5. Listagem somente no nível atual. [Evidências e decisões](../docs/Fase-04.md).

## Objetivo

Permitir navegação pelo Vault.

Adicionar:

``` csharp
Task<IReadOnlyCollection<NoteInfo>> ListAsync(
    string? folder = null,
    CancellationToken cancellationToken = default);
```

## Suportar

``` text
raiz
pasta específica
```

Definir explicitamente se a listagem é:

``` text
somente nível atual
```

ou:

``` text
recursiva
```

Recomendação V1:

``` text
ListAsync = nível atual
SearchAsync = recursivo
```

## Testes

-   raiz;
-   pasta vazia;
-   pasta inexistente;
-   pasta com arquivos;
-   extensão não permitida deve ser ignorada;
-   path traversal.

------------------------------------------------------------------------

# 10. FASE 5 --- Criação de notas

## Objetivo

Permitir que o sistema crie Markdown.

Adicionar:

``` csharp
Task<NoteInfo> CreateAsync(
    string path,
    string content,
    CancellationToken cancellationToken = default);
```

## Regras

-   `.md` apenas na V1;
-   não sobrescrever arquivo existente;
-   criar diretório pai somente se a política permitir;
-   UTF-8;
-   respeitar limite;
-   validar path antes da escrita.

Arquivo existente deve gerar conflito conhecido.

## Testes

-   criar nota;
-   conteúdo Unicode;
-   arquivo existente;
-   extensão inválida;
-   path traversal;
-   conteúdo acima do limite.

## Critério de aceite

Uma nota criada pelo serviço deve aparecer normalmente no Obsidian.

------------------------------------------------------------------------

# 11. FASE 6 --- Append

## Objetivo

Implementar a operação que provavelmente será mais utilizada pelas IAs.

Adicionar:

``` csharp
Task AppendAsync(
    string path,
    string content,
    CancellationToken cancellationToken = default);
```

## Comportamento

Exemplo existente:

``` markdown
# TAG

## Problema
...
```

Append:

``` markdown
## Nova decisão

...
```

Resultado:

``` markdown
# TAG

## Problema
...

## Nova decisão

...
```

## Cuidados

-   preservar conteúdo existente;
-   garantir separação adequada por newline;
-   lock curto durante escrita;
-   não criar arquivo silenciosamente, salvo se isso for uma opção
    explícita.

Recomendação:

``` text
CreateAsync cria
AppendAsync exige existente
```

## Testes

-   append simples;
-   múltiplos appends;
-   Unicode;
-   arquivo inexistente;
-   limite final excedido;
-   concorrência básica.

------------------------------------------------------------------------

# 12. FASE 7 --- Update com controle de concorrência

## Objetivo

Permitir substituição controlada do conteúdo.

Adicionar versionamento lógico baseado em hash:

``` text
SHA-256 do conteúdo
```

Ao ler:

``` json
{
  "path": "...",
  "version": "A87F..."
}
```

Atualização:

``` csharp
Task<NoteInfo> UpdateAsync(
    string path,
    string content,
    string expectedVersion,
    CancellationToken cancellationToken = default);
```

## Regra

Se:

``` text
versão atual != expectedVersion
```

retornar conflito:

``` text
409 / VaultConflict
```

conceitualmente.

## Motivo

Evitar:

``` text
Obsidian altera nota
        +
IA possui versão antiga
        +
IA sobrescreve alteração humana
```

## Testes

-   update válido;
-   version correta;
-   version antiga;
-   arquivo alterado externamente;
-   limite;
-   path traversal.

------------------------------------------------------------------------

# 13. FASE 8 --- Busca textual

## Objetivo

Encontrar conhecimento sem banco e sem índice externo.

Criar:

``` text
ISearchService
SearchService
```

Contrato inicial:

``` csharp
Task<IReadOnlyCollection<SearchResult>> SearchAsync(
    string query,
    CancellationToken cancellationToken = default);
```

Pesquisar:

``` text
nome do arquivo
caminho
conteúdo Markdown
```

Implementação V1:

``` text
Directory.EnumerateFiles
File.ReadAllText
Contains / comparação apropriada
```

Não adicionar nesta fase:

``` text
Lucene
SQLite FTS
ElasticSearch
embeddings
vector database
```

## Resultado

``` text
Path
Title
Snippet
Matches
```

## Testes

-   termo no título;
-   termo no conteúdo;
-   case;
-   acentos;
-   vários resultados;
-   nenhum resultado.

------------------------------------------------------------------------

# 14. FASE 9 --- YAML Frontmatter

## Objetivo

Padronizar categorização das notas.

Estrutura:

``` yaml
---
id: tag-urs-vencidas
title: URs Vencidas
category: trabalho
project: TAG
type: solucao-tecnica
created: 2026-10-05
updated: 2026-10-07
tags:
  - sql
  - tag
  - liquidacao
---
```

Criar modelo:

``` text
NoteMetadata
```

Campos iniciais:

``` text
Id
Title
Category
Project
Type
Created
Updated
Tags
```

## Regras

Frontmatter é opcional para leitura.

Uma nota Markdown sem YAML continua válida.

Não obrigar todas as notas antigas a serem migradas imediatamente.

## Busca

Após esta fase, `SearchResult` pode retornar metadados quando
existentes.

------------------------------------------------------------------------

# 15. FASE 10 --- Estrutura e templates do Obsidian

## Objetivo

Formalizar o Vault humano.

Estrutura inicial:

``` text
Vault/
├── 00 - Inbox/
├── 01 - Diario/
├── 02 - Trabalho/
├── 03 - Projetos/
├── 04 - Estudos/
├── 05 - Familia/
├── 06 - Referencias/
├── 90 - Anexos/
├── 98 - Arquivo/
└── 99 - Templates/
```

Templates:

``` text
Diario.md
Projeto.md
Solucao Tecnica.md
Reuniao.md
Decisao.md
```

## Importante

O código NÃO deve depender desses nomes.

Isto é organização do conteúdo, não estrutura rígida da aplicação.

## Critério de aceite

Obsidian abre o Vault e:

-   lê notas existentes;
-   enxerga notas criadas pelo serviço;
-   alterações no Obsidian são lidas pelo serviço;
-   YAML permanece válido.

------------------------------------------------------------------------

# 16. FASE 11 --- Logs e auditoria básica

## Objetivo

Saber o que o sistema fez.

Registrar:

``` text
timestamp
client
operation
relative path
result
duration
```

Exemplo:

``` text
2026-10-07 14:30
Client: LocalTest
Operation: Append
Path: 02 - Trabalho/TAG/URs Vencidas.md
Result: Success
```

Não registrar:

``` text
tokens
credenciais
segredos
conteúdo completo da nota
```

## Banco?

Não.

V1:

``` text
arquivo de log
```

SQLite continua opcional para uma evolução futura.

------------------------------------------------------------------------

# 17. FASE 12 --- MCP local somente leitura

## Objetivo

Introduzir MCP somente depois que o Core estiver estável.

Primeiras tools:

``` text
list_notes
read_note
get_note_info
search_notes
```

Inicialmente:

``` text
READ ONLY
```

Não disponibilizar escrita ainda.

## Motivo

Validar separadamente:

``` text
MCP
transporte
serialização
contratos
cliente
```

sem risco de modificar o Vault.

## Critério de aceite

Um cliente MCP local deve conseguir:

``` text
listar
pesquisar
ler
```

uma nota real.

------------------------------------------------------------------------

# 18. FASE 13 --- MCP de escrita

## Objetivo

Expor as operações já testadas no Core.

Adicionar:

``` text
create_note
append_note
update_note
create_folder
```

Continuar sem:

``` text
delete_note
```

`update_note` deve exigir controle de versão.

## Permissões iniciais

``` text
READ       sim
SEARCH     sim
CREATE     sim
APPEND     sim
UPDATE     sim
DELETE     não
```

------------------------------------------------------------------------

# 19. FASE 14 --- Autenticação

## Objetivo

Nenhum MCP remoto deve funcionar anonimamente.

Implementar o mecanismo compatível com o transporte MCP e os clientes
que efetivamente serão utilizados.

Perfis conceituais:

``` text
ChatGPT
Claude
Admin
```

Credenciais independentes permitem revogar um cliente sem afetar os
demais.

## Segredos

Nunca armazenar no Vault.

Nunca versionar.

Utilizar mecanismo seguro de configuração/secret store apropriado ao
servidor.

------------------------------------------------------------------------

# 20. FASE 15 --- Publicação no IIS

## Objetivo

Publicar o `Dietcode.KnowledgeVault.Server` no ambiente real.

Arquitetura:

``` text
Internet
   |
HTTPS :443
   |
IIS
   |
Dietcode.KnowledgeVault.Server
   |
VaultService
   |
D:\Knowledge\Vault
```

## Configurar

-   ASP.NET Core Hosting Bundle;
-   Site/Application no IIS;
-   Application Pool;
-   binding HTTPS;
-   certificado;
-   permissões NTFS mínimas;
-   configuração de produção;
-   logs.

A identidade do Application Pool deve possuir acesso somente ao
necessário.

## Não usar

``` text
Caddy
Nginx
Docker
```

na V1, pois o ambiente já possui IIS.

------------------------------------------------------------------------

# 21. FASE 16 --- Exposição externa segura

## Objetivo

Somente agora permitir acesso pela internet.

Configurar:

``` text
DNS / DDNS
HTTPS
porta 443
firewall
autenticação
```

Não expor:

``` text
SMB
SQL Server
RDP
porta interna do ASP.NET
Vault físico
```

## Gate obrigatório

Não publicar enquanto não houver:

-   HTTPS válido;
-   autenticação;
-   proteção contra path traversal;
-   limites de request;
-   logs;
-   permissões NTFS mínimas;
-   backup;
-   DELETE desabilitado.

------------------------------------------------------------------------

# 22. FASE 17 --- ChatGPT

## Objetivo

Conectar o ChatGPT ao MCP remoto quando a configuração/cliente
disponível suportar o transporte e autenticação adotados.

Testar nesta ordem:

``` text
1. list_notes
2. search_notes
3. read_note
4. create_note
5. append_note
6. update_note com versão
```

Teste funcional:

``` text
"Procure a documentação das URs vencidas."
```

Depois:

``` text
"Acrescente esta decisão à nota das URs."
```

Confirmar a alteração diretamente no Obsidian.

------------------------------------------------------------------------

# 23. FASE 18 --- Claude

## Objetivo

Conectar Claude ao mesmo servidor MCP.

Não criar implementação de Vault específica para Claude.

Arquitetura:

``` text
ChatGPT -----+
             |
             +---- MCP ---- Dietcode.KnowledgeVault
             |
Claude ------+
```

Executar os mesmos testes de leitura e escrita.

------------------------------------------------------------------------

# 24. FASE 19 --- Git e backup

## Objetivo

Garantir recuperação.

Configurar repositório privado para o Vault, se aprovado.

`.gitignore` inicial:

``` gitignore
.obsidian/workspace.json
.obsidian/workspace-mobile.json
.trash/
logs/
*.db
```

Estratégia:

``` text
Vault
 ├── Git privado
 ├── backup local
 └── backup externo
```

Lembrar:

> **Sincronização não é backup.**

Testar restauração de uma nota alterada ou removida.

------------------------------------------------------------------------

# 25. FASE 20 --- Hardening e revisão da V1

## Objetivo

Antes de considerar a V1 concluída, revisar o conjunto.

Verificar:

``` text
path traversal
concorrência
limites de arquivo
extensões
encoding
logs
credenciais
IIS
HTTPS
firewall
NTFS
MCP
backup
restore
```

Executar testes de integração ponta a ponta:

``` text
ChatGPT/Claude
      ↓
MCP remoto
      ↓
IIS
      ↓
Dietcode.KnowledgeVault.Server
      ↓
VaultService
      ↓
Markdown
      ↓
Obsidian
```

------------------------------------------------------------------------

# 26. V1 concluída

A V1 estará pronta quando for possível:

``` text
1. criar/editar uma nota no Obsidian;
2. pesquisar essa nota por MCP;
3. ler por IA;
4. criar uma nota pela IA;
5. acrescentar conteúdo;
6. atualizar com controle de concorrência;
7. visualizar imediatamente no Obsidian;
8. recuperar uma alteração via backup/Git;
9. fazer tudo isso remotamente por HTTPS autenticado.
```

------------------------------------------------------------------------

# 27. Funcionalidades deliberadamente posteriores à V1

Não colocar no escopo do James antes da V1:

``` text
SQLite
SQL Server
Redis
RabbitMQ
ElasticSearch
Lucene.NET
embeddings
vector database
RAG avançado
OCR
indexação de PDF
edição de anexos
dashboard web
React
Angular
aplicativo mobile próprio
Docker/Kubernetes
DELETE remoto por IA
```

------------------------------------------------------------------------

# 28. V2 --- Busca avançada

Somente depois de observar limitações reais da busca textual.

Possibilidades:

``` text
SQLite FTS
Lucene.NET
índice próprio
ranking
filtros por YAML
```

Escolher somente depois de medir o Vault real.

------------------------------------------------------------------------

# 29. V3 --- Busca semântica

Possível arquitetura futura:

``` text
Markdown
   |
Indexer
   |
Chunks
   |
Embeddings
   |
Vector Store
   |
Busca semântica
```

Isso permitiria consultas conceituais mesmo quando as palavras não
coincidirem.

Não faz parte da V1.

------------------------------------------------------------------------

# 30. Ordem resumida para o James

``` text
00 - Solution
01 - Configuração
02 - Path Security
03 - Read / Info
04 - List
05 - Create
06 - Append
07 - Update / Concurrency
08 - Search
09 - YAML Frontmatter
10 - Obsidian / Templates
11 - Logs
12 - MCP Read Only
13 - MCP Write
14 - Authentication
15 - IIS
16 - Internet / Security
17 - ChatGPT
18 - Claude
19 - Git / Backup
20 - Hardening
```

Cada número pode virar:

``` text
branch
issue
task
pull request
commit
```

Exemplo:

``` text
feature/01-vault-configuration
feature/02-path-security
feature/03-read-note
feature/04-list-notes
...
```

------------------------------------------------------------------------

# 31. Regra para os prompts enviados ao James

Para evitar que uma implementação contamine a seguinte, o prompt de cada
fase deve informar:

``` text
Implemente SOMENTE a fase X.

Não implemente antecipadamente funcionalidades descritas nas fases seguintes.

Mantenha compatibilidade com as fases anteriores.

Crie/ajuste os testes necessários para esta fase.

Não altere decisões arquiteturais sem justificar.

Ao final:
1. liste arquivos criados;
2. liste arquivos alterados;
3. explique as decisões;
4. informe os testes executados;
5. informe qualquer pendência.
```

Isso torna cada entrega revisável.

------------------------------------------------------------------------

# 32. Primeira sequência recomendada

Não começar pelo MCP.

A primeira rodada para o James deve ser:

``` text
FASE 0
FASE 1
FASE 2
FASE 3
```

Ao final dessas quatro fases já teremos:

``` text
Solution limpa
configuração
segurança de path
leitura real de Markdown
metadados
testes
```

Só então avançar para escrita.

------------------------------------------------------------------------

# 33. Princípio final

> **O conhecimento pertence ao Vault, não à aplicação, ao Obsidian ou à
> IA.**

`Dietcode.KnowledgeVault` é a camada segura que permite que diferentes
clientes trabalhem sobre os mesmos arquivos Markdown.

A implementação deve preservar essa independência:

``` text
Obsidian
ChatGPT
Claude
futuros clientes
      |
      v
Dietcode.KnowledgeVault
      |
      v
Markdown
```

O Markdown continua sendo a fonte de verdade.
