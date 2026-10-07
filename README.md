# Dietcode.KnowledgeVault

Versão atual: **1.0.1**. Seguir [as regras de versionamento](docs/Versionamento.md)
em cada entrega e antes de publicar manualmente.

Estrutura inicial em .NET 10 baseada em `Specs/Projeto_Obsidian_MCP_Vault_IIS.md`.

## Projetos

- `Dietcode.KnowledgeVault.Domain`: documento Markdown imutável (`Note`), metadados opcionais, caminhos relativos, token de versão e política de tamanho.
- `Dietcode.KnowledgeVault.Application`: reservado para casos de uso e portas de armazenamento, busca e auditoria; referencia Domain.
- `Dietcode.KnowledgeVault.Infrastructure`: reservado para adaptadores de arquivos, YAML, hash, busca e logs; referencia Application.
- `Dietcode.KnowledgeVault.Server`: host ASP.NET Core e futura composição de dependências/MCP; referencia Application e Infrastructure. Ainda não expõe operações.
- `Dietcode.KnowledgeVault.UnitTests`: regras do domínio e futuros casos de uso.
- `Dietcode.KnowledgeVault.IntegrationTests`: estrutura para futuros testes de infraestrutura e servidor; ainda sem cenários de integração.

O domínio não depende dos demais projetos nem de bibliotecas externas. A separação prepara Ports and Adapters e inversão de dependência, sem introduzir repositório genérico, mediador ou interfaces sem uso nesta etapa.

## Regras e limites desta etapa

- Caminhos lógicos são relativos, normalizados para `/`, com rejeição de segmentos de navegação, nomes de dispositivos Windows e caracteres inválidos. Notas aceitam apenas `.md`.
- Essa validação **não garante confinamento físico**. A infraestrutura ainda deverá verificar `VaultRoot`, limites de diretório, links simbólicos/junctions e permissões do processo.
- `Note` conserva o Markdown integral, inclusive frontmatter e quebras de linha. Append concatena exatamente o texto recebido, sem inserir separadores.
- O limite padrão é 2 MiB em UTF-8 sem BOM, configurável por `NoteSizePolicy`; substituição e append validam o conteúdo final. A infraestrutura deverá validar também os bytes efetivamente persistidos.
- `NoteMetadata` é uma representação separada de campos opcionais; não altera o conteúdo da nota. Categorias e tipos permanecem abertos. O parser/serializador YAML ainda não foi implementado.
- `NoteVersion` representa um SHA-256 calculado futuramente pelo armazenamento. A comparação lança uma exceção de domínio; ainda não há cálculo de hash, escrita atômica, lock ou proteção contra concorrência externa. A aplicação deverá verificar a versão antes de persistir; o transporte mapeará o conflito.
- Não há acesso ao Vault, autenticação, MCP, sincronização ou publicação nesta etapa.

## Validação

```powershell
dotnet restore Dietcode.KnowledgeVault.slnx
dotnet build Dietcode.KnowledgeVault.slnx --no-restore
dotnet test tests/Dietcode.KnowledgeVault.UnitTests --no-build
```
