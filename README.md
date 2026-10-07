# Dietcode.KnowledgeVault

Versão atual: **1.0.2**. Seguir [as regras de versionamento](docs/Versionamento.md)
em cada entrega e antes de publicar manualmente.

Servidor .NET 10 para acesso controlado a um Vault Markdown. As fases **0, 1 e 2**
estão implementadas. Leitura/escrita de notas e MCP pertencem às fases seguintes.

## Arquitetura

- **Domain**: notas, metadados, caminhos lógicos, versões e política de tamanho, sem dependências externas.
- **Application**: contrato IVaultPathResolver, configuração VaultOptions e erros conhecidos da aplicação.
- **Infrastructure**: VaultPathResolver e inspeção física de caminhos/reparse points.
- **Server**: composição via DI, binding, normalização e validação de configuração no startup.
- **UnitTests**: regras do domínio e normalização de extensões.
- **IntegrationTests**: filesystem temporário, junctions reais e startup ASP.NET Core.

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
Vault:RootPath fica vazio no arquivo versionado para exigir configuração explícita
em cada ambiente. Não há dependência de um caminho específico da máquina.

Exemplo PowerShell (escolha uma pasta local dedicada):

~~~powershell
New-Item -ItemType Directory -Path 'D:\Knowledge\Vault' -Force
$env:Vault__RootPath = 'D:\Knowledge\Vault'
dotnet run --project src/Dietcode.KnowledgeVault.Server --no-launch-profile --urls http://127.0.0.1:5055
~~~

Alternativamente, forneça --Vault:RootPath=D:\Knowledge\Vault ao executar a aplicação.
Configuração disponível em appsettings.json:

~~~json
{
  "Vault": {
    "RootPath": "",
    "AllowedExtensions": [".md"],
    "MaxFileSizeBytes": 2097152
  }
}
~~~

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

**Limite da garantia:** a inspeção vale no instante da resolução. Um processo
externo com permissão de alterar o filesystem pode trocar componentes depois dela.
Nas fases de I/O, a resolução deverá ser associada à abertura segura e às permissões
NTFS; não reutilizar caminhos resolvidos como autorização permanente. O resolvedor
não implementa isolamento do processo, bloqueio de hard links ou permissões NTFS.

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

Evidências e limites: [aceite das fases 0 a 2](docs/Fases-00-02.md).
