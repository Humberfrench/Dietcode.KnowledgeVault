# Fase 5 — Criação de notas

Versão: **1.0.6**. Implementação e validação automatizada concluídas.
Aceite visual no Obsidian ainda pendente.

## Contrato e decisões

IVaultService.CreateAsync(path, content, cancellationToken) retorna NoteInfo.
INoteCreator é a porta de criação, separada dos contratos de leitura e listagem.
VaultService valida o domínio e a política de tamanho; FileSystemNoteCreator
implementa o filesystem e é registrado na DI do Server.

- Apenas .md na V1, aceitando extensão em maiúsculas.
- UTF-8 estrito sem BOM; conteúdo e quebras de linha preservados.
- Conteúdo vazio é permitido. Unicode inválido é rejeitado.
- Limite de bytes configurado validado antes de gravar.
- **O diretório pai deve existir. Não há criação automática de diretórios.**
- Destino existente gera NoteCreateError.AlreadyExists e permanece intacto.
- ParentNotFound, ExtensionNotAllowed, InvalidEncoding, AccessDenied e Unavailable
  identificam outras falhas de criação. Caminhos inseguros continuam VaultPathException;
  tamanho excessivo continua NoteSizeExceededException.

## Escrita e segurança

A operação mantém handles dos diretórios ancestrais, sem compartilhar exclusão,
e valida caminhos/reparse points. Um arquivo temporário aleatório é criado com
CreateNew na mesma pasta e verificado pelo handle antes da escrita. O conteúdo
completo é gravado e o arquivo fechado antes de publicar com File.Move sem overwrite.

A publicação por rename no mesmo diretório é o ponto de confirmação. Cancelamento
observado antes dele remove o temporário; depois da publicação, a criação retorna
sucesso. Falhas normais anteriores à publicação também removem o temporário.

Tentativas concorrentes de criar o mesmo caminho têm um vencedor e os demais recebem
conflito, sem substituir conteúdo. Arquivos temporários usam .tmp, não aparecem na
listagem Markdown e só são removidos se criados pela própria chamada.

O adaptador é Windows, conforme as fases anteriores. Essas medidas não substituem
NTFS e isolamento de produção. Encerramento abrupto do processo pode deixar temporário
.tmp; limpeza automática após falha do processo e garantia de durabilidade contra
queda de energia não fazem parte desta entrega.

## Arquivos criados

- Application/Abstractions/INoteCreator.cs
- Application/Exceptions/NoteCreateException.cs
- Infrastructure/FileSystem/FileSystemNoteCreator.cs
- Infrastructure/FileSystem/WindowsDirectoryChainLease.cs
- IntegrationTests/FileSystem/NoteCreateTests.cs
- docs/Fase-05.md

## Arquivos alterados

- IVaultService e VaultService: CreateAsync e dependências de criação/tamanho.
- VaultServiceRegistration: registro do criador.
- NoteReadTests: composição adaptada, sem remover cenários anteriores.
- Seis .csproj: versão 1.0.6.
- README, versionamento e plano por fases: estado da entrega e convenção de commits.

Os caminhos abreviados referem-se aos projetos Dietcode.KnowledgeVault.* em src/tests.

## Validação

- Build: **0 erros e 0 avisos**.
- **136 testes aprovados**: 27 unitários e 109 de integração; nenhum ignorado.
- Novos cenários: UTF-8/Unicode, conteúdo vazio, YAML bruto, subpasta existente,
  conflito, pai ausente, traversal, extensão inválida, limite em bytes, Unicode
  inválido, cancelamento antes e após staging, concorrência, destino criado por
  outro escritor e junction introduzida após a resolução.
- Criação real com a configuração Development: nota Teste-Fase-05.md criada pelo
  serviço no Vault local; leitura, informações e listagem conferidas.
- A nota de exemplo permanece no Vault ignorado pelo Git para conferência visual
  no Obsidian. Não foi aberta no Obsidian nesta execução.

Sem append, update, criação de pastas pelo serviço, busca ou MCP.
Próxima etapa após aceite: fase 6, append.

Commit: feat(fase-05): criacao de notas com validacao e publicacao sem sobrescrita
