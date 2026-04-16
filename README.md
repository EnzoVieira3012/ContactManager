# ContactManager API

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red)
![Docker](https://img.shields.io/badge/Docker-Compose-blue)
![JWT](https://img.shields.io/badge/JWT-Authentication-orange)
![Swagger](https://img.shields.io/badge/Swagger-UI-brightgreen)

## 📋 Sobre o Projeto

API REST desenvolvida em **.NET 10** para gerenciamento de contatos, com autenticação JWT, controle de acesso por perfis (Médico/Admin) e persistência em SQL Server via Docker. Projeto criado como parte de um processo seletivo para desenvolvedor .NET Core, atendendo aos requisitos de:

- CRUD completo de contatos (criar, editar, visualizar, listar, desativar, ativar, excluir)
- Validações de idade (maioridade, idade > 0, data não futura)
- Idade calculada dinamicamente
- Separação de regras de negócio (camada de serviços/validações)
- Testes unitários com xUnit, Moq e FluentAssertions
- Documentação interativa com Swagger

## 🚀 Tecnologias Utilizadas

- **.NET 10** – framework principal
- **ASP.NET Core Web API** – construção dos endpoints REST
- **Entity Framework Core 10** – ORM para acesso ao SQL Server
- **Microsoft SQL Server 2022** – banco de dados (executado via Docker)
- **ASP.NET Core Identity** – gerenciamento de usuários e roles
- **JWT (JSON Web Tokens)** – autenticação stateless
- **Swagger / OpenAPI** – documentação interativa
- **xUnit, Moq, FluentAssertions** – testes unitários
- **Docker & Docker Compose** – containerização do banco de dados

## 📦 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

## 🔧 Configuração e Execução

### 1. Clonar o repositório

```bash
git clone https://github.com/EnzoVieira3012/ContactManager.git
cd ContactManager
```

### 2. Configurar variáveis de ambiente

Crie um arquivo `.env` na raiz da solução com o seguinte conteúdo:

```env
# SQL Server
MSSQL_SA_PASSWORD=MyStrong!Passw0rd
MSSQL_PORT=1433

# JWT Authentication (mínimo 32 caracteres)
JWT_KEY=sua-chave-super-secreta-com-32-caracteres-ou-mais

# Admin registration code
ADMIN_REGISTRATION_CODE=Admin@123
```

### 3. Subir o banco de dados com Docker

```bash
docker-compose up -d
```

O SQL Server estará disponível em `localhost:1433`.

### 4. Aplicar as migrations

```bash
cd ContactManager.Infrastructure
dotnet ef database update --startup-project ../ContactManager.API
```

### 5. Executar a API

```bash
cd ../ContactManager.API
dotnet run
```

A API estará rodando em `http://localhost:5028` e `https://localhost:7077`.

### 6. Acessar a documentação Swagger

Abra o navegador em:  
`http://localhost:5028/swagger`

## 🔐 Autenticação e Perfis

- **Médico** – pode criar, editar, visualizar e excluir **apenas seus próprios contatos**.
- **Admin** – pode visualizar, editar e excluir **todos os contatos**, além de gerenciar usuários.

Para registrar um administrador, é necessário enviar o campo `adminCode` com o valor definido em `ADMIN_REGISTRATION_CODE`.

## 📌 Endpoints Principais

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|---------------|
| POST | `/api/Auth/register` | Registro de usuário (Médico ou Admin) | Público |
| POST | `/api/Auth/login` | Login – retorna token JWT | Público |
| POST | `/api/Auth/forgot-password` | Solicitar token de redefinição de senha | Público |
| POST | `/api/Auth/reset-password` | Redefinir senha (admins exigem código extra) | Público |
| POST | `/api/Contato` | Criar novo contato | Médico/Admin |
| GET | `/api/Contato` | Listar contatos ativos (filtrados por perfil) | Médico/Admin |
| GET | `/api/Contato/{id}` | Obter detalhes de um contato | Médico/Admin |
| PUT | `/api/Contato/{id}` | Atualizar contato (verifica permissão) | Médico/Admin |
| PATCH | `/api/Contato/{id}/desativar` | Desativar contato (soft delete) | Médico/Admin |
| PATCH | `/api/Contato/{id}/ativar` | Reativar contato | Médico/Admin |
| DELETE | `/api/Contato/{id}` | Excluir contato fisicamente | Médico/Admin |
| GET | `/health` | Health check da API | Público |
| GET | `/` | Informações gerais da API | Público |

## 📨 Exemplos de Requisições e Respostas

### Registrar Médico

**POST** `/api/Auth/register`

```json
{
  "email": "medico@exemplo.com",
  "password": "Medico@123",
  "fullName": "Dr. João Silva",
  "role": "Medico"
}
```

✅ **Resposta (200 OK)**

```json
{
  "message": "Usuário criado com sucesso"
}
```

### Registrar Administrador (com código)

```json
{
  "email": "admin@exemplo.com",
  "password": "Admin@123",
  "fullName": "Administrador",
  "role": "Admin",
  "adminCode": "Admin@123"
}
```

### Login – Obter Token

**POST** `/api/Auth/login`

```json
{
  "email": "medico@exemplo.com",
  "password": "Medico@123"
}
```

✅ **Resposta (200 OK)**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Criar Contato

**POST** `/api/Contato`  
*Header:* `Authorization: Bearer <token>`

```json
{
  "nome": "Paciente 1",
  "dataNascimento": "1990-05-10T00:00:00",
  "sexo": "M"
}
```

✅ **Resposta (201 Created)**

```json
{
  "id": 1,
  "nome": "Paciente 1",
  "dataNascimento": "1990-05-10T00:00:00",
  "sexo": "M",
  "idade": 35,
  "isActive": true
}
```

### Listar Contatos (apenas ativos)

**GET** `/api/Contato`  
*Header:* `Authorization: Bearer <token>`

✅ **Resposta (200 OK)**

```json
[
  {
    "id": 1,
    "nome": "Paciente 1",
    "dataNascimento": "1990-05-10T00:00:00",
    "sexo": "M",
    "idade": 35,
    "isActive": true
  }
]
```

### Atualizar Contato

**PUT** `/api/Contato/1`

```json
{
  "id": 1,
  "nome": "Paciente Atualizado",
  "dataNascimento": "1985-03-20T00:00:00",
  "sexo": "M"
}
```

✅ **Resposta (200 OK)** – retorna o objeto atualizado.

### Desativar Contato (soft delete)

**PATCH** `/api/Contato/1/desativar`  
*Header:* `Authorization: Bearer <token>`

✅ **Resposta (204 No Content)**

### Reativar Contato

**PATCH** `/api/Contato/1/ativar`  
*Header:* `Authorization: Bearer <token>`

✅ **Resposta (204 No Content)**

### Excluir Contato (fisicamente)

**DELETE** `/api/Contato/1`  
*Header:* `Authorization: Bearer <token>`

✅ **Resposta (204 No Content)`

### Solicitar token para redefinir senha

**POST** `/api/Auth/forgot-password`

```json
{
  "email": "medico@exemplo.com"
}
```

✅ **Resposta (200 OK)**

```json
{
  "token": "CfDJ8...",
  "message": "Use este token para redefinir sua senha."
}
```

### Redefinir senha (médico)

**POST** `/api/Auth/reset-password`

```json
{
  "email": "medico@exemplo.com",
  "token": "token-recebido",
  "newPassword": "NovaSenha@123"
}
```

✅ **Resposta (200 OK)**

```json
{
  "message": "Senha redefinida com sucesso."
}
```

### Health Check

**GET** `/health`

✅ **Resposta (200 OK)**

```json
{
  "status": "healthy",
  "timestamp": "2025-04-16T15:30:00.000Z"
}
```

## 🧪 Executando os Testes Unitários

Os testes cobrem as camadas de serviço e controller, utilizando **xUnit**, **Moq** e **FluentAssertions**. Para executá‑los:

```bash
dotnet test
```

Para ver os resultados detalhados com cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Os testes incluem:

- Criação de contato com dados válidos e inválidos (menor de idade, data futura, idade zero)
- Atualização, obtenção, listagem, desativação, ativação e exclusão de contatos
- Registro de médicos e administradores (com e sem código correto)
- Login com credenciais válidas/inválidas
- Fluxo completo de redefinição de senha (forgot + reset)
- Verificação de permissões (médico vs admin)

## 📂 Estrutura do Projeto

```
ContactManager/
├── ContactManager.API           # Controllers, Program.cs, DTOs específicos da API
├── ContactManager.Application   # Serviços, interfaces, validadores, DTOs compartilhados
├── ContactManager.Domain        # Entidades, enums, interfaces de repositório
├── ContactManager.Infrastructure# DbContext, Migrations, implementação dos repositórios
├── ContactManager.Tests         # Testes unitários (xUnit, Moq)
├── .env                         # Variáveis de ambiente (não versionado)
├── docker-compose.yml           # Configuração do SQL Server
└── ContactManager.slnx          # Arquivo da solução
```

## 📄 Licença

Este projeto está sob a licença MIT.

## 👨‍💻 Autor

**Enzo Vieira**  
- GitHub: [EnzoVieira3012](https://github.com/EnzoVieira3012)  
- E-mail: enzovieira.trabalho@outlook.com  
- Telefone: (11) 95610-6568

---
✨ Desenvolvido com 💙 e .NET 10.