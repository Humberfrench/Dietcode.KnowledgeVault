# Fase 4 — Listagem

Versão: **1.0.5**.

## Comportamento

IVaultService.ListAsync(string? folder = null, CancellationToken cancellationToken = default)
retorna IReadOnlyCollection<NoteInfo>.

- null significa raiz. String vazia, ponto e caminhos inválidos são rejeitados.
- Uma pasta relativa seleciona somente seu nível atual; não há recursão.
- Apenas arquivos .md, sem distinção de caixa. Pastas e outras extensões são ignoradas.
- Arquivos ocultos Markdown também são considerados.
- Resultado ordenado por caminho relativo com OrdinalIgnoreCase e desempate Ordinal.
- Pasta vazia retorna coleção vazia.
- Pasta inexistente: NoteListError.FolderNotFound. Arquivo usado como pasta:
  NotDirectory. Acesso negado e falhas de I/O têm códigos próprios.
- Cada nota passa pelo GetInfoAsync seguro já existente: extensão, tamanho,
  handle e regras de links continuam válidos. Conteúdo não é decodificado.
- Nota removida entre enumeração e consulta de informações é ignorada.
- Demais falhas (limite, acesso, arquivo bloqueado ou caminho inseguro) interrompem
  a operação, sem devolver resultado parcial silencioso.
- Cancelamento é observado antes, durante e depois da enumeração.

## SOLID e segurança

INoteLister é uma porta separada de INoteReader. FileSystemNoteLister implementa
a enumeração e reutiliza INoteReader para os metadados. VaultService coordena os
contratos, e o Server registra os adaptadores por DI.

WindowsDirectoryLease abre a pasta sem seguir reparse points e sem compartilhar
exclusão. O handle é validado antes e depois da enumeração, com revalidação do caminho.
Junctions filhas são ignoradas como subpastas; solicitar a própria junction é rejeitado.
O adaptador mantém o suporte Windows da fase 3.

A listagem não é um snapshot transacional: arquivos podem ser adicionados ou removidos
durante a operação. Isso não substitui permissões NTFS nem a segurança de implantação.

## Arquivos

Criados:
- Application/Abstractions/INoteLister.cs
- Application/Exceptions/NoteListException.cs
- Infrastructure/FileSystem/FileSystemNoteLister.cs
- Infrastructure/FileSystem/WindowsDirectoryLease.cs
- IntegrationTests/FileSystem/NoteListTests.cs
- docs/Fase-04.md

Atualizados:
- IVaultService e VaultService: operação ListAsync e dependência de INoteLister.
- WindowsOpenedFileGuard: suporte à verificação do handle de diretório.
- VaultServiceRegistration: registro do lister.
- NoteReadTests: composição das dependências; cenários anteriores preservados.
- Seis .csproj, README, versionamento e plano por fases.

Application e Infrastructure referem-se aos projetos correspondentes em src;
IntegrationTests ao projeto em tests.

## Validação

- Build sem erros ou avisos.
- **117 testes aprovados**: 27 unitários e 90 de integração; nenhum ignorado.
- Raiz, pasta específica, Unicode, separadores, ordem, nível atual, pasta vazia,
  pasta ausente, arquivo no lugar de pasta, extensões ignoradas, metadados sem
  decodificação, limites, cancelamento, traversal e junctions.
- Junction introduzida entre resolução e abertura da pasta também foi rejeitada.
- Conferência manual no Vault local com as configurações reais de Development:
  raiz e pasta específica listadas corretamente; .txt ignorado e sem recursão.
  Arquivos/pasta temporários e executor foram removidos ao final.

Sem endpoints HTTP/MCP, criação de notas por serviço ou busca.
Produção/IIS não foram validados. Próxima etapa: fase 5, criação de notas.

Commit: feat: implementa listagem de notas do vault.
