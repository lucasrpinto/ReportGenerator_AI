# ReportGenerator AI

O ReportGenerator AI é uma API desenvolvida em .NET para geração dinâmica de relatórios a partir de prompts em linguagem natural.

A proposta do projeto é permitir que o usuário informe o tipo de relatório desejado, enquanto a aplicação utiliza recursos de IA para interpretar a solicitação, gerar uma consulta SQL segura para PostgreSQL, executar a busca em modo somente leitura e disponibilizar os dados em formato de pré-visualização, PDF ou Excel.

---

## Objetivo do projeto

Este projeto foi criado com o objetivo de estudar e aplicar conceitos de:

- Desenvolvimento de APIs com .NET
- Arquitetura em camadas
- Integração com IA generativa
- Geração dinâmica de SQL
- Execução segura de consultas em banco de dados
- Exportação de relatórios em PDF e Excel
- Autenticação com JWT
- Integração com PostgreSQL

---

## Funcionalidades

- Cadastro de usuários
- Login com autenticação JWT
- Alteração de senha
- Geração de prévia de relatório dinâmico
- Geração de relatório a partir do histórico
- Consulta de histórico de relatórios gerados
- Geração de SQL com apoio de IA
- Validação de SQL para permitir apenas consultas de leitura
- Execução de consultas em PostgreSQL
- Exportação de relatórios em PDF
- Exportação de relatórios em Excel
- Documentação da API com Swagger

---

## Como funciona

O fluxo principal da aplicação é:

1. O usuário realiza login na API.
2. O usuário envia um prompt solicitando um relatório.
3. A IA interpreta o prompt e gera uma consulta SQL.
4. O backend valida se a consulta é segura.
5. A consulta é executada no PostgreSQL em modo somente leitura.
6. A API retorna uma prévia dos dados.
7. O usuário pode gerar o relatório final em PDF ou Excel.
8. O histórico do relatório é armazenado para consultas futuras.

Exemplo de prompt:

```txt
Liste os clientes que não compram há mais de 30 dias, mostrando nome, última compra e total faturado.
```

---

## Tecnologias utilizadas

- .NET
- ASP.NET Core Web API
- C#
- PostgreSQL
- Npgsql
- JWT Bearer Authentication
- Swagger / Swashbuckle
- QuestPDF
- ClosedXML
- OpenAI API
- FluentValidation
- Arquitetura em camadas

---

## Estrutura do projeto

```txt
src/
├── Relatorios.Api/
│   └── Camada responsável pelos controllers, autenticação, middlewares e configuração da API.
│
├── Relatorios.Application/
│   └── Camada responsável pelos casos de uso, regras de aplicação e contratos internos.
│
├── Relatorios.Contracts/
│   └── Camada responsável pelos requests e responses expostos pela API.
│
├── Relatorios.Domain/
│   └── Camada responsável pelas entidades, modelos de domínio e estruturas de relatório.
│
└── Relatorios.Infrastructure/
    └── Camada responsável por banco de dados, IA, geração de arquivos, segurança e integrações externas.
```

---

## Principais endpoints

### Autenticação

```http
POST /api/auth/register
```

Cria um novo usuário.

```http
POST /api/auth/login
```

Realiza login e retorna um token JWT.

```http
PUT /api/auth/change-password
```

Altera a senha do usuário autenticado.

---

### Relatórios

```http
POST /api/reports/preview-dynamic
```

Gera uma prévia de relatório dinâmico a partir de um prompt.

```http
POST /api/reports/generate-dynamic/from-history
```

Gera um arquivo de relatório em PDF ou Excel com base em um histórico já existente.

```http
GET /api/reports/dynamic-history
```

Lista o histórico de relatórios dinâmicos.

```http
GET /api/reports/dynamic-history/{id}
```

Busca os detalhes de um relatório pelo ID.

---

## Segurança na geração de SQL

Um dos pontos principais do projeto é impedir que a IA gere comandos perigosos para o banco de dados.

A aplicação valida o SQL gerado antes da execução e permite apenas consultas iniciadas com:

```sql
SELECT
```

ou

```sql
WITH ... SELECT
```

Comandos como os abaixo são bloqueados:

```sql
INSERT
UPDATE
DELETE
DROP
CREATE
ALTER
TRUNCATE
MERGE
EXEC
CALL
COPY
VACUUM
ANALYZE
```

Além disso, a aplicação também bloqueia múltiplos comandos, comentários SQL e funções perigosas.

---

## Geração de relatórios

A aplicação suporta geração de relatórios em:

- PDF
- Excel

O relatório em PDF é gerado com QuestPDF.

O relatório em Excel é gerado com ClosedXML, criando uma planilha com os dados e outra com metadados do relatório.

---

## Configuração do ambiente

Antes de executar o projeto, configure as variáveis sensíveis da aplicação.

Exemplo de configurações necessárias:

```json
{
  "Jwt": {
    "SecretKey": "sua-chave-jwt-com-no-minimo-32-caracteres",
    "Issuer": "ReportGeneratorAI",
    "Audience": "ReportGeneratorAI"
  },
  "Postgres": {
    "ConnectionString": "Host=localhost;Port=5432;Database=sua_base;Username=seu_usuario;Password=sua_senha"
  },
  "OpenAi": {
    "ApiKey": "sua-chave-da-openai",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "modelo-utilizado"
  },
  "ReportStorage": {
    "BasePath": "reports"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

Nunca envie chaves de API, senhas ou connection strings reais para o GitHub.

---

## Como executar o projeto

Clone o repositório:

```bash
git clone https://github.com/lucasrpinto/ReportGenerator_AI.git
```

Acesse a pasta do projeto:

```bash
cd ReportGenerator_AI
```

Restaure os pacotes:

```bash
dotnet restore
```

Execute a API:

```bash
dotnet run --project src/Relatorios.Api
```

Acesse o Swagger em ambiente de desenvolvimento:

```txt
https://localhost:porta/swagger
```

---

## Exemplo de uso

Após realizar login e obter o token JWT, envie uma requisição para gerar uma prévia de relatório:

```http
POST /api/reports/preview-dynamic
Authorization: Bearer seu_token
Content-Type: application/json
```

Body:

```json
{
  "prompt": "Liste os pedidos com cliente, valor bruto, valor estornado e valor líquido"
}
```

A API retornará informações como:

```json
{
  "historyId": "00000000-0000-0000-0000-000000000000",
  "sql": "SELECT ...",
  "rowCount": 10,
  "executionTimeMs": 120,
  "columns": [],
  "rows": []
}
```

---

## Status do projeto

Projeto em desenvolvimento.

Funcionalidades já presentes:

- Autenticação JWT
- Geração de SQL com IA
- Validação de SQL seguro
- Execução de consultas PostgreSQL
- Histórico de relatórios
- Exportação PDF
- Exportação Excel
- testes automatizados

---

## Autor

Desenvolvido por Lucas R. Pinto.

GitHub: https://github.com/lucasrpinto
---
