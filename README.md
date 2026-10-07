# Dietcode.KnowledgeVault

Versão atual: **1.0.4**. Seguir [as regras de versionamento](docs/Versionamento.md)
em cada entrega e antes de publicar manualmente.

Servidor .NET 10 para acesso controlado a um Vault Markdown. As fases **0, 1, 2 e 3**
estão implementadas. Leitura e informações de arquivo estão disponíveis via IVaultService; escrita e MCP pertencem às fases seguintes.

## Arquitetura

- **Domain**: notas, metadados, caminhos lógicos, versões e política de tamanho, sem dependências externas.
- **Application**: IVaultService, VaultService, portas INoteReader/IVaultPathResolver, modelos de leitura e configuração VaultOptions.
- **Infrastructure**: VaultPathResolver, FileSystemNoteReader e verificação do handle aberto no Windows.
- **Server**: composição via DI, binding, normalização e validação de configuração no startup.
- **UnitTests**: regras do domínio e normalização de extensões.
- **IntegrationTests**: leitura UTF-8, metadados, limites, filesystem temporário, junctions/hard links e startup ASP.NET Core.

Todos os projetos usam o prefixo Dietcode.KnowledgeVault.
Referências: Application → Domain; Infrastructure → Application;
Server → Application + Infrastructure. UnitTests → Domain + Application;
IntegrationTests → Infrastructure + Server. O antigo Core da Spec foi separado
em Domain e Application, conforme a decisão de manter seis projetos e SOLID.

A interface de resolução fica em Application; o adaptador de filesystem fica em
Infrastructure. O host fornece a implementação por injeção de dependência.
Validação de configuração, regras de domínio e inspeção de filesystem têm
responsabilidades separadas.

## Configuração e execução local

O diretório do Vault **deve existir**. A aplicação não o cria automaticamente.

Os caminhos estão definidos no projeto Server:

| Ambiente | Arquivo | RootPath |
|---|---|---|
| Produção (configuração base) | appsettings.json | C:\Knowledge\Vault |
| Development | appsettings.Development.json | E:\Dev.Dietcode\Dietcode.KnowledgeVault\Vault |

A pasta local Vault foi criada e está ignorada no Git para manter as notas fora
do repositório do código. Em outro checkout, crie a pasta antes de executar.
No servidor de produção, crie C:\Knowledge\Vault e conceda as permissões necessárias
à identidade da aplicação antes de iniciá-la.

Os perfis de execução do projeto já selecionam Development. Para executar localmente:

~~~powershell
dotnet run --project src/Dietcode.KnowledgeVault.Server --launch-profile http
~~~

A configuração base mantém AllowedExtensions = [".md"] e MaxFileSizeBytes = 2097152.
Development sobrescreve somente RootPath. Variáveis de ambiente, como Vault__RootPath,
e argumentos de linha de comando continuam podendo sobrescrever esses valores.

- RootPath: absoluto, existente, diretório e sem reparse points no caminho.
- Extensões: normalizadas (espaços, caixa, ponto inicial, duplicação); apenas .md na V1.
- MaxFileSizeBytes: positivo; padrão 2 MiB. A política de tamanho registrada na DI usa esse valor.
- Configuração inválida impede o startup com erro identificando a opção.
- Não há endpoints de notas, MCP ou autenticação nesta fase. HTTP 404 na raiz é esperado.
- Mudanças de configuração exigem reinício para manter a raiz e os serviços consistentes.

## Segurança de caminhos

IVaultPathResolver.Resolve(null) retorna a raiz validada.
Outros valores devem ser caminhos relativos literais, por exemplo
02 - Trabalho/TAG/URs Vencidas.md. Separadores / e \ são aceitos.

A resolução rejeita caminhos absolutos, UNC fornecido pelo consumidor, traversal,
nomes de dispositivos Windows, streams alternativos, segmentos vazios e nomes
inválidos. Percentuais (%) são rejeitados; o contrato não aceita URL encoding.

O caminho completo precisa ficar sob a raiz incluindo o separador de diretório.
Uma pasta irmã como Vault-backup não passa por compartilhar o prefixo Vault.
São inspecionados a raiz, seus ancestrais e os componentes existentes do caminho;
links simbólicos, junctions e outros reparse points são rejeitados, inclusive os
que apontam para dentro do Vault. A inspeção é repetida em cada resolução.

A resolução não abre arquivos nem cria diretórios. Componentes finais inexistentes
são permitidos para os futuros casos de criação. Resolução de caminho não impõe
extensão de nota: pastas também precisam ser resolvidas; leitura/escrita validarão
as extensões nas próximas fases.

## Leitura e informações de notas

IVaultService.ReadAsync retorna NoteContent (Info e Content). GetInfoAsync retorna
NoteInfo com nome, caminho relativo, extensão, bytes físicos e datas UTC.
Ambos respeitam MaxFileSizeBytes, aceitam CancellationToken e retornam erros conhecidos
para notas ausentes, extensão proibida, falta de acesso e falhas de I/O.
A leitura usa UTF-8 estrito, com ou sem BOM; não altera o arquivo nem interpreta YAML.

A fase 3 usa um adaptador de leitura **Windows**, adequado ao IIS previsto.
Após resolver o caminho, o leitor abre o arquivo somente para leitura e verifica
o caminho final do handle antes de ler conteúdo. Também bloqueia múltiplos hard links.
O caminho é revalidado após a abertura. Fora do Windows, o leitor falha explicitamente.

A validação do resolvedor isolado continua valendo apenas no instante da chamada.
As verificações do leitor não substituem permissões NTFS e isolamento de produção.
Evidências, erros, decisões e limites estão em [Fase 3](docs/Fase-03.md).

## Regras de domínio preservadas

Note conserva o Markdown integral e concatena exatamente o conteúdo do append.
A composição de separadores prevista na fase 6 será feita no caso de uso.
NoteMetadata não reescreve YAML. NoteVersion compara tokens SHA-256, mas o cálculo
do hash e o controle de concorrência durante persistência pertencem às fases futuras.

## Validação

~~~powershell
dotnet restore Dietcode.KnowledgeVault.slnx
dotnet build Dietcode.KnowledgeVault.slnx --no-restore
dotnet test Dietcode.KnowledgeVault.slnx --no-build
~~~

Evidências: [fases 0 a 2](docs/Fases-00-02.md) e [fase 3](docs/Fase-03.md).
