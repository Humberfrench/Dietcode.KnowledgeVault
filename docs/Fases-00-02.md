# Entrega das fases 0, 1 e 2

Versão: **1.0.2**.

## Fase 0 — Fundação

- Solution Dietcode.KnowledgeVault.slnx, seis projetos .NET 10 e referências alinhadas ao SOLID.
- Domain e Application substituem o Core conceitual dos documentos originais.
- README, gitignore, regras de versionamento e testes presentes.
- Domínio previamente implementado preservado.
- Commit desta entrega: feat: conclui fases 0 a 2 do knowledge vault.

## Fase 1 — Configuração

- VaultOptions define RootPath, AllowedExtensions e MaxFileSizeBytes.
- Binding, normalização e ValidateOnStart registrados no host.
- Raiz absoluta e existente obrigatória; nunca criar o Vault implicitamente.
- Extensões normalizadas e limitadas a Markdown na V1.
- Limite de bytes positivo e usado pela política injetada.
- RootPath versionado vazio: o operador deve informar a pasta por configuração.
- Falhas de configuração impedem que o servidor comece a atender.

## Fase 2 — Resolução segura

- Contrato em Application e implementação em Infrastructure.
- Normalização de separadores e validação lógica do domínio.
- Canonicalização por GetFullPath e limite por raiz mais separador.
- Rejeição de traversal, absolutos, UNC de entrada, nomes reservados, ADS,
  controles, segmentos inválidos e percent encoding (incluindo dupla codificação).
- Inspeção dos componentes existentes e dos ancestrais da raiz.
- Bloqueio de junctions/reparse points, inclusive raiz e links internos.
- Revalidação a cada chamada; componente intermediário precisa ser diretório.
- Nenhuma operação de leitura/escrita de conteúdo foi introduzida.

## Evidências em 07/10/2026

- Build da solution: **0 erros, 0 avisos**.
- Testes: **27 unitários + 47 de integração = 74 aprovados**, nenhum ignorado.
- Junctions Windows reais testadas: raiz, ancestral, destino externo, destino interno,
  destino ausente e substituição após construir o resolvedor.
- Startup do executável real com raiz relativa: rejeitado com erro Vault:RootPath.
- Startup do executável real com diretório temporário válido: servidor iniciou em
  loopback; GET na raiz retornou 404, conforme ausência de endpoints.
- Os processos e diretórios temporários dessa validação foram encerrados/removidos.

## Limites e próximas fases

Não foi possível criar um symlink de arquivo neste Windows por ausência de privilégio.
O bloqueio comum por FileAttributes.ReparsePoint foi exercitado com junctions reais;
isso não equivale a executar um teste de symlink de arquivo neste ambiente.

O resolvedor verifica o estado do filesystem no instante da chamada. A fase de I/O
deverá tratar a abertura e as permissões para evitar troca de caminhos entre validação
e uso (TOCTOU). Hard links não são reparse points. ACLs de produção, IIS e confinamento
do processo ainda pertencem às etapas futuras; não foram configurados nem validados.

A configuração do Vault real permanece a cargo do ambiente de execução; a validação
desta entrega usou diretórios temporários. Leitura, criação de notas, Obsidian, MCP,
autenticação e publicação não fazem parte das fases 0 a 2.
