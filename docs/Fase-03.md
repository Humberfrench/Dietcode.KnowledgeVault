# Fase 3 — Leitura e informações de notas

Versão: **1.0.4**.

## Entrega

- IVaultService oferece somente ReadAsync e GetInfoAsync, com CancellationToken.
- VaultService valida o caminho lógico e a extensão Markdown na Application.
- INoteReader é a porta de armazenamento; FileSystemNoteReader é o adaptador na Infrastructure.
- NoteContent contém Info e Content. NoteInfo contém Name, RelativePath, Extension,
  Size (bytes físicos), CreatedAt e UpdatedAt (UTC).
- Os serviços são registrados na DI do Server. Nenhum endpoint HTTP/MCP foi adicionado.
- Configurações por ambiente da entrega anterior foram preservadas: Vault local em
  Development e C:\Knowledge\Vault como configuração base de produção.

## Regras

- Apenas .md na V1, sem distinção de caixa. A configuração do startup também restringe
  AllowedExtensions a .md; não existe uma segunda lista divergente no leitor.
- UTF-8 estrito: aceita UTF-8 com ou sem BOM, retira apenas o BOM inicial do texto
  retornado e preserva o restante, inclusive YAML bruto e quebras de linha.
- UTF-16 e sequências UTF-8 inválidas geram NoteReadError.InvalidEncoding.
- GetInfoAsync não decodifica nem lê o conteúdo; aceita informação de um arquivo
  cujo conteúdo seja inválido, mas continua aplicando extensão, segurança e tamanho.
- O limite usa bytes físicos, inclusive BOM, antes da alocação de conteúdo e durante
  a leitura. Ambos os métodos rejeitam arquivos acima do limite configurado.
- Arquivo ou pasta pai ausente: NoteReadError.NotFound.
- Extensão proibida: NoteReadError.ExtensionNotAllowed.
- Falta de acesso: NoteReadError.AccessDenied. Falha de I/O/arquivo bloqueado:
  NoteReadError.Unavailable. Cancelamento permanece OperationCanceledException.
- Caminhos inseguros permanecem VaultPathException e excesso de tamanho permanece
  NoteSizeExceededException. Esses erros ainda não são mapeados para HTTP.

## Abertura segura no Windows

O adaptador suporta Windows, conforme hospedagem prevista em IIS. Em outro sistema
operacional, falha explicitamente antes de abrir o arquivo.

O resolvedor valida o caminho antes da abertura. O arquivo é aberto somente para
leitura, com FileShare.Read, impedindo escritores e exclusão incompatíveis enquanto
o handle estiver aberto. Antes de ler conteúdo, WindowsOpenedFileGuard verifica o
caminho final do handle e os atributos via APIs Windows. O caminho deve coincidir
com o solicitado; diretórios, reparse points e arquivos com múltiplos hard links
são rejeitados. O resolvedor é consultado novamente após a abertura.

Essa verificação reduz o risco de redirecionamento entre resolução e abertura;
foi testada uma junction introduzida precisamente nesse intervalo. Não substitui
as permissões NTFS de produção, que continuam sendo responsabilidade de implantação.
Não representa garantia contra administrador local malicioso ou todos os tipos de
mutação concorrente no filesystem.

## Arquivos criados

- Application/Abstractions/IVaultService.cs
- Application/Abstractions/INoteReader.cs
- Application/Services/VaultService.cs
- Application/Models/NoteContent.cs
- Application/Models/NoteInfo.cs
- Application/Exceptions/NoteReadException.cs
- Infrastructure/FileSystem/FileSystemNoteReader.cs
- Infrastructure/FileSystem/WindowsOpenedFileGuard.cs
- IntegrationTests/FileSystem/NoteReadTests.cs
- docs/Fase-03.md

Os caminhos Application/Infrastructure acima são relativos aos respectivos projetos
Dietcode.KnowledgeVault.* em src; IntegrationTests fica em tests.

## Arquivos atualizados

- Server/Configuration/VaultServiceRegistration.cs: composição dos novos serviços.
- Seis arquivos .csproj: versão 1.0.4, AssemblyVersion 1.0.4 e FileVersion 1.0.4.0.
- README, docs/Versionamento.md e plano por fases: estado e regras da entrega.
- As alterações previamente pendentes em .gitignore e appsettings por ambiente
  acompanham o commit, sem mudar os caminhos escolhidos pelo usuário.

## Evidências

- Build: **0 erros, 0 avisos**.
- **100 testes aprovados**: 27 unitários e 73 de integração; nenhum ignorado.
- Novos cenários: Markdown, Unicode/acentos/emoji, conteúdo vazio, YAML bruto,
  extensão em maiúscula, metadados UTC, integridade dos bytes, BOM, arquivo ausente,
  extensão proibida, traversal, limites em bytes, cancelamento, encoding inválido,
  diretório no lugar de arquivo, arquivo bloqueado, junction, hard link,
  redirecionamento após resolução e limite proveniente da DI.
- Validação manual com configuração real de Development: nota temporária de 64 bytes
  criada em E:\Dev.Dietcode\Dietcode.KnowledgeVault\Vault; ReadAsync e GetInfoAsync
  confirmaram conteúdo Unicode, tamanho e metadados. A nota e o executor temporário
  foram removidos ao final.

## Pendências fora da fase

Sem listagem, criação por serviço, append, update, busca, parser YAML, MCP ou publicação.
O teste de symlink de arquivo continua limitado pelo privilégio do Windows já
registrado nas fases anteriores; junctions e hard links reais foram testados.
Produção, permissões IIS/NTFS e leitura pelo Obsidian não foram validadas nesta entrega.

Commit: feat: implementa leitura e metadados do vault.
