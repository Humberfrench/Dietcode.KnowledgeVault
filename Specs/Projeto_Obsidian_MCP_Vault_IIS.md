# Projeto: Vault pessoal compartilhado entre Obsidian, ChatGPT e Claude

## 1. Objetivo

Criar uma base de conhecimento pessoal e técnica hospedada no servidor
local, mas acessível pela internet de forma segura, para que:

-   **Obsidian** seja a interface humana principal para visualizar,
    editar e organizar as notas.
-   **ChatGPT (Jimmy)** possa pesquisar, ler, criar e atualizar notas
    quando houver uma integração MCP compatível configurada.
-   **Claude** possa usar o mesmo repositório por MCP, quando o cliente
    utilizado suportar essa conexão.
-   Todo o conteúdo principal permaneça em arquivos **Markdown
    (`.md`)**, legíveis mesmo sem Obsidian, ChatGPT ou Claude.
-   O servidor seja a **fonte de verdade** do conteúdo.

A ideia é permitir comandos como:

> "Guarde essa solução no Obsidian em Trabalho/TAG."

> "Procure nas minhas notas como resolvemos o processamento retroativo
> das URs."

> "Acrescente essa decisão na documentação da integração Holanda."

------------------------------------------------------------------------

## 2. Visão geral da arquitetura

``` text
                         INTERNET
                            |
                         HTTPS
                            |
                    IIS
                 (HTTPS / TLS)
                            |
                     Autenticação
                            |
                            v
                    +---------------+
                    |  MCP Server   |
                    |   Vault API   |
                    +-------+-------+
                            |
                    acesso controlado
                            |
                            v
                    +---------------+
                    | Obsidian Vault|
                    |   arquivos MD |
                    +-------+-------+
                            |
              +-------------+-------------+
              |                           |
          Obsidian                    Backup
       no computador              Git / disco / cloud

Clientes externos:

ChatGPT -------- HTTPS/MCP --------+
                                   |
Claude ---------- HTTPS/MCP -------+
```

------------------------------------------------------------------------

## 3. Precisamos de banco de dados?

### Não, para a primeira versão.

O **Vault do Obsidian já é o nosso armazenamento de dados**.

Cada nota é simplesmente um arquivo:

``` text
Vault/
├── 00 - Inbox/
├── 01 - Diario/
├── 02 - Trabalho/
├── 03 - Projetos/
├── 04 - Estudos/
├── 05 - Familia/
├── 06 - Referencias/
└── 99 - Templates/
```

Exemplo:

``` text
02 - Trabalho/TAG/URs Vencidas.md
```

O conteúdo é Markdown normal:

``` markdown
# URs Vencidas

## Processamento retroativo

Em 05/10/2026 foram separados os registros...

## SQL

```sql
SELECT *
FROM EnvioTagURsVencidas
WHERE Processado = 0
  AND Sucesso IS NULL;
```

    ### Vantagens de não usar banco

    - Simplicidade.
    - Arquivos legíveis sem nenhum software especial.
    - Obsidian trabalha diretamente sobre eles.
    - Fácil backup.
    - Fácil versionamento com Git.
    - Fácil migração.
    - Não ficamos presos a uma aplicação específica.
    - Busca textual é simples.
    - Links do Obsidian continuam funcionando.

    ---

    ## 4. Quando um banco poderia ser útil?

    Banco seria **opcional**, numa segunda etapa.

    Poderíamos usar **SQLite** para:

    - índice de busca;
    - cache;
    - auditoria de operações;
    - registrar qual cliente alterou uma nota;
    - registrar data/hora das alterações;
    - indexar embeddings futuramente;
    - controle de versões adicional.

    Exemplo:

    ```text
    vault.db

Tabelas possíveis:

``` text
AuditLog
--------
Id
Date
Client
Operation
Path
Success

SearchIndex
-----------
Path
Title
Tags
UpdatedAt
```

Mas o banco **não deve substituir os arquivos Markdown**.

A fonte principal continua sendo:

``` text
*.md
```

### Recomendação inicial

**Não colocar banco na V1.**

Primeiro fazer:

``` text
Markdown + Obsidian + MCP + HTTPS + autenticação
```

Depois avaliamos se existe necessidade real de SQLite.

------------------------------------------------------------------------

# 5. Obsidian: vamos usar?

## Sim.

O Obsidian será a interface principal para nós humanos.

Ele não precisa ser o servidor da solução.

O papel dele será:

``` text
Arquivos Markdown
       ^
       |
   Obsidian
```

O Obsidian poderá:

-   editar notas;
-   navegar entre assuntos;
-   criar links `[[entre notas]]`;
-   usar tags;
-   mostrar backlinks;
-   criar templates;
-   pesquisar;
-   organizar projetos;
-   visualizar Markdown;
-   trabalhar com diagramas Mermaid;
-   abrir anexos.

Mesmo se um dia deixarmos de usar Obsidian, os `.md` continuam nossos.

------------------------------------------------------------------------

# 5.1 Linguagens e tecnologias

  Parte                     Tecnologia
  ------------------------- ---------------------------------------------------
  Servidor MCP              **C# / .NET 10**
  Aplicação                 **ASP.NET Core**
  Hospedagem                **IIS**
  Serviços do Vault         **C# / `System.IO`**
  Testes                    **C# / xUnit**
  Notas                     **Markdown (`.md`)**
  Metadados                 **YAML Frontmatter**
  Configuração              **JSON (`appsettings.json`)**
  Comunicação               **MCP / JSON-RPC conforme protocolo/SDK adotado**
  Interface humana          **Obsidian**
  Scripts administrativos   **PowerShell**, quando necessário
  Banco                     **Nenhum na V1**
  Front-end próprio         **Nenhum na V1**

Não há necessidade inicial de Python, Node.js, TypeScript ou React. **O
Obsidian já será o front-end humano.**

# 5.2 Categorização das notas

A organização terá duas camadas: **pastas**, para navegação humana, e
**YAML Frontmatter**, para categorização e pesquisa.

Exemplo:

``` yaml
---
id: tag-urs-vencidas
title: URs Vencidas
category: trabalho
project: TAG
type: solucao-tecnica
created: 2026-10-05
updated: 2026-10-06
tags:
  - sql
  - tag
  - recebiveis
  - liquidacao
---
```

Categorias iniciais possíveis:

``` text
trabalho
projeto
estudo
referencia
diario
familia
```

Tipos iniciais possíveis:

``` text
solucao-tecnica
documentacao
decisao
reuniao
procedimento
ideia
pesquisa
referencia
diario
```

A taxonomia deve continuar simples e crescer somente quando houver
necessidade real.

# 6. Estrutura recomendada do Vault

``` text
Vault/
│
├── 00 - Inbox/
│   └── Capturas rápidas e conteúdo ainda não classificado
│
├── 01 - Diario/
│   ├── 2026/
│   │   ├── 2026-10-06.md
│   │   └── 2026-10-07.md
│
├── 02 - Trabalho/
│   ├── CredPay/
│   ├── TAG/
│   │   ├── URs Vencidas.md
│   │   └── Liquidação Retroativa.md
│   ├── Holanda/
│   │   ├── README.md
│   │   ├── GeoJSON.md
│   │   ├── Areas.md
│   │   └── Integracao.md
│   ├── CSharp/
│   ├── SQL/
│   └── Infra/
│
├── 03 - Projetos/
│   ├── French/
│   ├── Impressao 3D/
│   └── Outros/
│
├── 04 - Estudos/
│   ├── DotNet/
│   ├── Azure/
│   ├── Machine Learning/
│   └── Arquitetura/
│
├── 05 - Familia/
│
├── 06 - Referencias/
│
├── 90 - Anexos/
│
└── 99 - Templates/
    ├── Diario.md
    ├── Projeto.md
    ├── Solucao Tecnica.md
    └── Reuniao.md
```

Essa estrutura pode mudar. O MCP não deve depender rigidamente dela.

------------------------------------------------------------------------

# 7. Onde ficará fisicamente o Vault?

Exemplo Windows:

``` text
D:\Knowledge\Vault
```

Exemplo Linux:

``` text
/srv/knowledge/vault
```

O servidor MCP recebe apenas o caminho raiz:

``` text
VaultRoot=/srv/knowledge/vault
```

## Regra fundamental

O processo MCP **só poderá acessar arquivos abaixo de `VaultRoot`**.

Nunca permitir:

``` text
../../../
```

ou acesso arbitrário ao sistema operacional.

------------------------------------------------------------------------

# 8. O servidor que precisamos criar

Podemos criar um projeto pequeno em **ASP.NET Core**.

Sugestão:

``` text
Dietcode.KnowledgeVault.Server
```

Ele terá duas responsabilidades:

1.  manipular o Vault;
2.  expor essas operações via MCP.

Arquitetura:

``` text
Dietcode.KnowledgeVault.Server
│
├── Configuration/
├── Security/
├── Services/
│   ├── VaultService
│   ├── SearchService
│   └── AuditService (opcional)
│
├── MCP/
│   ├── Tools
│   └── Resources
│
└── Program.cs
```

------------------------------------------------------------------------

# 9. Operações necessárias

## 9.1 Listar notas

``` text
list_notes
```

Parâmetros:

``` json
{
  "folder": "02 - Trabalho/TAG"
}
```

------------------------------------------------------------------------

## 9.2 Ler nota

``` text
read_note
```

``` json
{
  "path": "02 - Trabalho/TAG/URs Vencidas.md"
}
```

------------------------------------------------------------------------

## 9.3 Pesquisar notas

``` text
search_notes
```

``` json
{
  "query": "EnvioTagURsVencidas"
}
```

Retorno esperado:

``` json
{
  "results": [
    {
      "path": "02 - Trabalho/TAG/URs Vencidas.md",
      "title": "URs Vencidas"
    }
  ]
}
```

------------------------------------------------------------------------

## 9.4 Criar nota

``` text
create_note
```

``` json
{
  "path": "02 - Trabalho/TAG/Nova Nota.md",
  "content": "# Nova Nota\n\nConteúdo..."
}
```

------------------------------------------------------------------------

## 9.5 Atualizar nota inteira

``` text
update_note
```

Usar com mais cuidado para evitar sobrescrever alterações humanas.

------------------------------------------------------------------------

## 9.6 Acrescentar conteúdo

Esta provavelmente será uma das operações mais usadas:

``` text
append_note
```

Exemplo:

``` json
{
  "path": "02 - Trabalho/TAG/URs Vencidas.md",
  "content": "## Nova decisão\n\n..."
}
```

------------------------------------------------------------------------

## 9.7 Criar diretório

``` text
create_folder
```

Exemplo:

``` json
{
  "path": "02 - Trabalho/Novo Projeto"
}
```

------------------------------------------------------------------------

## 9.8 Obter metadados

``` text
get_note_info
```

Retorna:

``` text
nome
caminho
tamanho
data de criação
última alteração
```

------------------------------------------------------------------------

# 10. DELETE deve existir?

Na primeira versão, **eu não disponibilizaria DELETE para IA**.

Teríamos:

``` text
READ       sim
SEARCH     sim
CREATE     sim
APPEND     sim
UPDATE     sim
DELETE     não
```

Se precisarmos posteriormente:

``` text
move_note
archive_note
```

é mais seguro que apagar.

Podemos criar:

``` text
98 - Arquivo/
```

e mover notas antigas para lá.

------------------------------------------------------------------------

# 11. Concorrência

Existe uma situação importante:

``` text
Obsidian está editando arquivo
          +
IA tenta editar o mesmo arquivo
```

Precisamos evitar que uma alteração apague a outra.

## Estratégia

Ao ler uma nota, calcular:

``` text
ETag / hash
```

Exemplo:

``` text
SHA256(conteúdo)
```

Na atualização:

``` json
{
  "path": "...",
  "expectedVersion": "A87F...",
  "content": "..."
}
```

Se o arquivo mudou desde a leitura:

``` text
409 Conflict
```

A IA deverá reler antes de atualizar.

Para `append_note`, podemos usar escrita atômica e lock curto.

------------------------------------------------------------------------

# 12. Segurança

Essa é uma das partes mais importantes.

## Nunca expor diretamente

``` text
D:\Knowledge\Vault
```

pela internet.

Também não expor compartilhamento SMB:

``` text
\\servidor\vault
```

publicamente.

A internet só verá:

``` text
HTTPS -> MCP Server
```

------------------------------------------------------------------------

# 13. HTTPS e IIS

Como o servidor já utiliza **Windows + IIS**, o IIS será a camada
pública da solução. Não há necessidade de adicionar Caddy ou Nginx
apenas para atuar como reverse proxy.

``` text
Internet
   |
HTTPS :443
   |
IIS
   |
Dietcode.KnowledgeVault.Server
ASP.NET Core / .NET 10
   |
VaultService
   |
D:\Knowledge\Vault
```

O IIS será responsável por HTTPS/TLS, certificado, bindings de domínio e
publicação da aplicação ASP.NET Core. No servidor será instalado o
**ASP.NET Core Hosting Bundle** compatível com a versão do .NET
utilizada.

O `Dietcode.KnowledgeVault.Server` não precisa ficar diretamente exposto à
internet fora do IIS.

# 14. Domínio

Exemplo:

``` text
vault.seudominio.com.br
```

ou:

``` text
mcp.seudominio.com.br
```

DNS:

``` text
mcp.seudominio.com.br
          |
          v
       IP público
```

Se o IP residencial mudar, usar **Dynamic DNS**.

------------------------------------------------------------------------

# 15. Roteador / Firewall

Se o servidor estiver dentro de casa:

``` text
Internet
   |
Roteador
   |
Porta 443
   |
Reverse Proxy
```

Abrir externamente **somente o necessário**.

Idealmente:

``` text
443 HTTPS
```

Não expor:

``` text
SQL Server
SMB
RDP
SSH
porta interna do ASP.NET
```

sem necessidade.

------------------------------------------------------------------------

# 16. Autenticação

Não deixar MCP anônimo.

Precisamos de autenticação adequada ao cliente MCP escolhido.

Dependendo do suporte dos clientes, podemos usar um mecanismo compatível
como OAuth ou outra autenticação aceita pelo transporte/cliente. Se
usarmos tokens/API keys em alguma camada auxiliar, eles devem ser
tratados como segredo e nunca gravados dentro do Vault.

Exemplo conceitual:

``` text
Cliente
   |
credencial
   |
MCP Server
```

Podemos separar credenciais por cliente:

``` text
ChatGPT
Claude
Admin
```

Assim uma credencial pode ser revogada sem afetar as outras.

------------------------------------------------------------------------

# 17. Permissões

Podemos ter perfis:

  Perfil      Ler   Pesquisar   Criar   Alterar   Excluir
  --------- ----- ----------- ------- --------- ---------
  ChatGPT     Sim         Sim     Sim       Sim       Não
  Claude      Sim         Sim     Sim       Sim       Não
  Admin       Sim         Sim     Sim       Sim       Sim

Também podemos restringir pastas futuramente.

------------------------------------------------------------------------

# 18. Proteção contra Path Traversal

Toda operação deve transformar o caminho solicitado em caminho absoluto
e verificar:

``` text
FullPath começa com VaultRoot
```

Rejeitar:

``` text
../
..\ 
C:\
/etc/
```

A API/MCP jamais deve aceitar um caminho fora do Vault.

------------------------------------------------------------------------

# 19. Tipos de arquivo permitidos

V1:

``` text
.md
```

Opcionalmente leitura de:

``` text
.txt
.json
```

Anexos podem existir no Vault, mas não precisamos permitir escrita
binária inicialmente.

Depois podemos adicionar:

``` text
.png
.jpg
.pdf
```

se houver necessidade.

------------------------------------------------------------------------

# 20. Limite de tamanho

Definir limites.

Exemplo inicial:

``` text
Nota Markdown: máximo 2 MB
Request: máximo 3 MB
```

Evita abuso e erros acidentais.

------------------------------------------------------------------------

# 21. Logs

Mesmo sem banco, o servidor deve ter logs técnicos.

Exemplo:

``` text
2026-10-06 16:20
Client: ChatGPT
Operation: append_note
Path: 02 - Trabalho/TAG/URs Vencidas.md
Result: SUCCESS
```

Nunca registrar:

-   tokens;
-   senhas;
-   chaves privadas;
-   conteúdo sensível completo desnecessariamente.

------------------------------------------------------------------------

# 22. Auditoria

Na V1 podemos usar arquivo:

``` text
logs/audit-2026-10.log
```

Posteriormente, se quisermos consultas melhores:

``` text
SQLite
```

------------------------------------------------------------------------

# 23. Backup

Isso é obrigatório.

O Vault será informação valiosa.

Sugestão:

``` text
Vault
  |
  +---- backup diário
  |
  +---- Git privado
```

Git é particularmente interessante porque Markdown funciona muito bem
com versionamento.

Exemplo:

``` text
alteração errada da IA
        |
        v
git diff
        |
        v
reverter
```

------------------------------------------------------------------------

# 24. Git

Opcional, mas **fortemente recomendado**.

Repositório privado:

``` text
Dietcode.KnowledgeVault
```

`.gitignore`:

``` gitignore
.obsidian/workspace.json
.obsidian/workspace-mobile.json
.trash/
logs/
*.db
```

Podemos decidir se toda a configuração `.obsidian` será versionada ou
apenas parte dela.

------------------------------------------------------------------------

# 25. Obsidian no computador

No PC:

1.  instalar Obsidian;
2.  escolher **Open folder as vault**;
3.  apontar para a pasta sincronizada/montada apropriada do Vault.

Se o servidor for outra máquina, existem várias estratégias.

## Estratégia recomendada

Manter uma cópia local sincronizada de forma controlada ou usar um
mecanismo de sincronização adequado.

Evitar editar arquivos diretamente por SMB através da internet.

Dentro da rede local, SMB pode funcionar, mas
sincronização/versionamento tende a ser mais resiliente.

------------------------------------------------------------------------

# 26. Frontmatter

Podemos padronizar notas com YAML.

Exemplo:

``` yaml
---
title: URs Vencidas
type: technical
project: TAG
created: 2026-10-05
updated: 2026-10-06
tags:
  - tag
  - sql
  - liquidacao
---
```

Isso melhora organização e pesquisa.

------------------------------------------------------------------------

# 27. Templates

## Solução técnica

``` markdown
---
type: technical
created: {{date}}
tags: []
---

# Título

## Problema

## Contexto

## Solução

## Código

## Decisões

## Pendências

## Resultado
```

## Diário

``` markdown
---
type: daily
date: {{date}}
---

# {{date}}

## Trabalho

## Projetos

## Ideias

## Pendências

## Registro
```

------------------------------------------------------------------------

# 28. Política para a IA escrever

Não queremos que a IA transforme o Vault em um depósito de conversa.

A ideia é salvar **conhecimento útil**.

Preferir:

``` text
problema
decisão
solução
query
código
resultado
pendência
contexto importante
```

Evitar:

``` text
transcrição completa do chat
mensagens repetidas
conversa sem valor futuro
```

------------------------------------------------------------------------

# 29. Fluxo esperado - guardar algo

Usuário:

``` text
Jimmy, guarda no Obsidian o que fizemos com as URs.
```

Fluxo:

``` text
1. search_notes("URs")
2. encontra:
   02 - Trabalho/TAG/URs Vencidas.md

3. read_note(...)

4. resume o conhecimento novo

5. append_note(...)

6. confirma a operação
```

------------------------------------------------------------------------

# 30. Fluxo esperado - recuperar algo

Usuário:

``` text
Como fizemos aquela separação das URs retroativas?
```

Fluxo:

``` text
search_notes("URs retroativas")
        |
        v
read_note(...)
        |
        v
resposta baseada na documentação
```

------------------------------------------------------------------------

# 31. Fluxo esperado - nova informação

Usuário:

``` text
Guarda isso em Holanda.
```

Fluxo:

``` text
search_notes("Holanda assunto")
        |
        +-- encontrou nota -> atualizar
        |
        +-- não encontrou -> criar nota
```

------------------------------------------------------------------------

# 32. MCP

O MCP será a camada de integração para os clientes de IA compatíveis.

Conceitualmente:

``` text
ChatGPT
   |
   | MCP
   v
Dietcode.KnowledgeVault.Server
   |
   v
Markdown

Claude
   |
   | MCP
   v
Dietcode.KnowledgeVault.Server
```

Assim não criamos uma API específica para cada IA.

------------------------------------------------------------------------

# 33. Transporte MCP

Para acesso remoto, utilizar o transporte remoto suportado pela versão
atual do MCP e pelos clientes que efetivamente conectaremos,
preferencialmente via HTTPS.

Antes da implementação final, validar a compatibilidade atual de:

``` text
ChatGPT <-> MCP remoto
Claude  <-> MCP remoto
```

O servidor deve ser desenhado para que a camada de acesso ao Vault
(`VaultService`) seja independente do transporte. Assim, se o mecanismo
de conexão mudar, não precisamos reescrever a lógica de arquivos.

------------------------------------------------------------------------

# 34. REST também é necessário?

Não necessariamente.

Podemos ter:

``` text
MCP
 |
VaultService
```

sem expor REST público.

Internamente, a aplicação pode usar serviços C# normais.

Se quisermos administração/testes, podemos adicionar endpoints REST
privados posteriormente.

------------------------------------------------------------------------

# 35. Estrutura da Solution .NET

``` text
Dietcode.KnowledgeVault.sln
│
├── src/
│   ├── Dietcode.KnowledgeVault.Server/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── MCP/
│   │   │   └── Tools/
│   │   ├── Security/
│   │   └── Configuration/
│   │
│   ├── Dietcode.KnowledgeVault.Core/
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   └── Services/
│   │
│   └── Dietcode.KnowledgeVault.Infrastructure/
│       ├── FileSystem/
│       ├── Search/
│       └── Logging/
│
└── tests/
    ├── Dietcode.KnowledgeVault.UnitTests/
    └── Dietcode.KnowledgeVault.IntegrationTests/
```

Para uma V1 muito pequena, também podemos começar com **um único
projeto** e separar depois.

------------------------------------------------------------------------

# 36. Interfaces C

Exemplo conceitual:

``` csharp
public interface IVaultService
{
    Task<string> ReadAsync(string path);
    Task WriteAsync(string path, string content);
    Task AppendAsync(string path, string content);
    Task<IEnumerable<NoteInfo>> ListAsync(string folder);
    Task<IEnumerable<SearchResult>> SearchAsync(string query);
}
```

------------------------------------------------------------------------

# 37. Configuração

`appsettings.json`:

``` json
{
  "Vault": {
    "RootPath": "/srv/knowledge/vault",
    "AllowedExtensions": [".md"],
    "MaxFileSizeMb": 2
  }
}
```

Segredos **não devem ficar no `appsettings.json` versionado**.

Usar variável de ambiente ou cofre de segredos.

------------------------------------------------------------------------

# 38. Docker

**Não é necessário na V1.**

Como o ambiente já possui Windows + IIS e a aplicação será ASP.NET Core,
publicar diretamente no IIS reduz componentes e complexidade. Docker
permanece uma opção futura se surgir necessidade concreta de isolamento
ou portabilidade.

# 39. Publicação no IIS

``` text
Internet
   |
HTTPS :443
   |
IIS
   |
Site / Application Pool
   |
Dietcode.KnowledgeVault.Server (.NET 10)
   |
D:\Knowledge\Vault
```

O IIS substitui Caddy/Nginx na arquitetura.

# 40. Serviços de infraestrutura

A V1 não necessita de Docker Compose nem de serviços auxiliares.

Não precisamos inicialmente de SQL Server, PostgreSQL, Redis, RabbitMQ,
MongoDB ou ElasticSearch. A aplicação ASP.NET Core acessará diretamente
o Vault através do `VaultService`.

# 41. Busca

V1 pode fazer busca textual nos `.md`.

Exemplo:

``` text
Directory.EnumerateFiles(...)
File.ReadAllText(...)
Contains(...)
```

Para um Vault pequeno/médio isso é suficiente.

Depois podemos melhorar com:

``` text
Lucene.NET
SQLite FTS
índice próprio
busca semântica
embeddings
```

Mas **não começar por isso**.

------------------------------------------------------------------------

# 42. Busca semântica futura

Futuramente poderíamos perguntar:

``` text
"qual era aquele problema que tivemos com liquidação?"
```

mesmo sem a palavra exata existir.

Aí podemos adicionar:

``` text
Markdown
   |
Indexer
   |
Embeddings
   |
Vector Store
```

Isso é V2/V3, não requisito inicial.

------------------------------------------------------------------------

# 43. Anexos

Obsidian pode armazenar:

``` text
90 - Anexos/
```

Exemplo:

``` text
imagem.png
documentacao.pdf
diagrama.svg
```

Na primeira versão, as IAs podem trabalhar prioritariamente com `.md`.

Depois adicionamos suporte controlado a anexos.

------------------------------------------------------------------------

# 44. O que precisamos no servidor

Como já existe IIS, a V1 será baseada no ambiente Windows existente.

Necessário:

``` text
IIS
ASP.NET Core Hosting Bundle
.NET 10 Runtime/Hosting
Dietcode.KnowledgeVault.Server publicado
Certificado HTTPS
Git (recomendado)
```

Estrutura sugerida:

``` text
D:\
├── Sites\
│   └── Dietcode.KnowledgeVault.Server\
└── Knowledge\
    └── Vault\
```

A identidade do Application Pool deverá ter somente as permissões
necessárias sobre a pasta do Vault.

Não é necessário na V1: Caddy, Nginx, Docker, SQL Server, PostgreSQL,
Redis, RabbitMQ ou MongoDB.

# 45. O que instalar no computador

``` text
Obsidian
Git (opcional)
mecanismo de sincronização escolhido
```

------------------------------------------------------------------------

# 46. O que NÃO precisamos inicialmente

``` text
SQL Server
PostgreSQL
MongoDB
Redis
RabbitMQ
ElasticSearch
Kubernetes
Azure
AWS
Obsidian Sync pago
```

Nada disso é necessário para a V1.

------------------------------------------------------------------------

# 47. Custo

A aplicação pode funcionar sem custo recorrente de software se usarmos:

``` text
servidor existente
domínio já existente ou DDNS
Let's Encrypt
Caddy
ASP.NET Core
Obsidian Free
Git
```

Pode haver custos externos dependendo do domínio, internet, energia,
backup ou serviços escolhidos.

------------------------------------------------------------------------

# 48. Backup recomendado

Estratégia:

``` text
Vault principal
     |
     +---- Git privado
     |
     +---- cópia diária local
     |
     +---- cópia externa periódica
```

Regra:

**sincronização não é backup.**

------------------------------------------------------------------------

# 49. Recuperação

Precisamos conseguir restaurar:

``` text
arquivo apagado
alteração errada
corrupção
falha de disco
erro da IA
```

Git resolve muito bem alterações textuais.

------------------------------------------------------------------------

# 50. Fases do projeto

## Fase 1 - Vault

-   [ ] Criar pasta do Vault.
-   [ ] Criar estrutura inicial.
-   [ ] Abrir no Obsidian.
-   [ ] Criar templates.
-   [ ] Colocar algumas notas reais.

## Fase 2 - Core

-   [ ] Criar Solution .NET.
-   [ ] Implementar `VaultService`.
-   [ ] Implementar proteção contra path traversal.
-   [ ] Implementar leitura.
-   [ ] Implementar escrita.
-   [ ] Implementar append.
-   [ ] Implementar listagem.
-   [ ] Implementar pesquisa.
-   [ ] Criar testes.

## Fase 3 - MCP

-   [ ] Adicionar servidor MCP.
-   [ ] Expor `search_notes`.
-   [ ] Expor `read_note`.
-   [ ] Expor `create_note`.
-   [ ] Expor `append_note`.
-   [ ] Expor `update_note`.
-   [ ] Testar localmente.

## Fase 4 - Segurança

-   [ ] Configurar autenticação compatível.
-   [ ] Configurar HTTPS.
-   [ ] Configurar IIS.
-   [ ] Limitar acesso ao Vault.
-   [ ] Configurar firewall.
-   [ ] Configurar logs.
-   [ ] Configurar limites de request.

## Fase 5 - Internet

-   [ ] Configurar domínio/DDNS.
-   [ ] Apontar DNS.
-   [ ] Expor somente 443.
-   [ ] Testar de fora da rede.
-   [ ] Fazer revisão de segurança.

## Fase 6 - Clientes

-   [ ] Validar conexão MCP do ChatGPT.
-   [ ] Conectar ChatGPT.
-   [ ] Validar permissões.
-   [ ] Validar conexão MCP do Claude.
-   [ ] Conectar Claude.
-   [ ] Testar leitura.
-   [ ] Testar pesquisa.
-   [ ] Testar escrita.

## Fase 7 - Backup

-   [ ] Criar repositório Git privado.
-   [ ] Configurar backup.
-   [ ] Testar restauração.

------------------------------------------------------------------------

# 51. MVP

O primeiro MVP precisa fazer somente isto:

``` text
1. Obsidian abre o Vault.
2. Criamos uma nota manualmente.
3. MCP pesquisa a nota.
4. MCP lê a nota.
5. MCP cria uma nota.
6. MCP acrescenta conteúdo.
7. Obsidian imediatamente enxerga a alteração.
```

Se isso funcionar, a arquitetura está validada.

------------------------------------------------------------------------

# 52. Segurança mínima antes de colocar na internet

Não publicar enquanto não tivermos:

-   [ ] HTTPS válido.
-   [ ] Autenticação.
-   [ ] Path traversal bloqueado.
-   [ ] Processo sem acesso ao restante do disco.
-   [ ] Porta interna não exposta.
-   [ ] Firewall.
-   [ ] Limite de tamanho.
-   [ ] Logs.
-   [ ] Backup.
-   [ ] DELETE desabilitado para IA.
-   [ ] Segredos fora do código e do Vault.

------------------------------------------------------------------------

# 53. Decisão arquitetural recomendada

## Por que IIS?

O servidor já possui IIS. Usá-lo reduz a quantidade de componentes e
aproveita a infraestrutura existente.

Com IIS já temos HTTPS/TLS, certificados, bindings por domínio,
publicação ASP.NET Core, Application Pools, logs, controle de acesso e
integração natural com Windows.

Portanto, não há motivo para introduzir Caddy ou Nginx na V1 sem uma
necessidade concreta.

**Regra arquitetural: não adicionar infraestrutura sem necessidade
real.**

## V1

``` text
Obsidian
    |
Markdown Files
    |
Dietcode.KnowledgeVault.Server (.NET)
    |
MCP remoto / HTTPS
    |
+---------+---------+
|                   |
ChatGPT           Claude
```

### Banco

**Não.**

### Redis

**Não.**

### Mensageria

**Não.**

### Obsidian

**Sim.**

### Docker

**Não necessário na V1; o servidor já utiliza Windows + IIS.**

### Git

**Fortemente recomendado.**

### HTTPS

**Obrigatório para exposição pública.**

### MCP

**Sim: é a integração principal planejada para as IAs.**

------------------------------------------------------------------------

# 54. Princípio principal

O mais importante da arquitetura é:

> **O conhecimento pertence ao Vault, não à IA.**

ChatGPT, Claude e Obsidian são maneiras diferentes de trabalhar com o
mesmo conjunto de arquivos.

Se amanhã trocarmos qualquer uma dessas ferramentas:

``` text
Vault/*.md
```

continua existindo e continua legível.

------------------------------------------------------------------------

# 55. Próximo passo

O próximo passo recomendado é **não começar pela internet nem pelo
MCP**.

Primeiro:

``` text
1. Criar o Vault.
2. Definir a pasta física no servidor.
3. Abrir no Obsidian.
4. Criar 3 ou 4 notas reais.
5. Criar o projeto Dietcode.KnowledgeVault.Server.
6. Implementar e testar o VaultService localmente.
7. Só depois conectar MCP e expor com segurança.
```

Assim cada camada é validada separadamente e evitamos misturar problema
de arquivo, Obsidian, MCP, autenticação, DNS e rede ao mesmo tempo.
